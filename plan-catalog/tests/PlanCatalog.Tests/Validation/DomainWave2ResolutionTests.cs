using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Audit;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

public sealed class DomainWave2ResolutionTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static Core.Catalog.CatalogSourceSnapshot LoadSnapshot() =>
        new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog")).LoadSnapshot();

    private static PublishedTemplateBundle BuildBundle(string key, int version)
    {
        var snapshot = LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        return new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher()).Assemble(stamped, key, version);
    }

    [Fact]
    public void RunLayoutSchemaV2_RejectsSequenceOrder_AndLegacyV1AllowsIt()
    {
        var validator = new JsonSchemaNetValidator(Path.Combine(AppContext.BaseDirectory, "TestSchemas"));
        var v2WithLegacyField = """
        {
          "metadata": { "documentType": "RUN_LAYOUT", "schemaVersion": 2, "key": "RUN_LAYOUT_4D", "version": 2, "status": "DRAFT" },
          "runsPerWeek": 4,
          "slots": [
            { "sequenceOrder": 1, "role": "KEY_SESSION" },
            { "role": "EASY_SUPPORT" },
            { "role": "EASY_SUPPORT" },
            { "role": "LONG_RUN" }
          ]
        }
        """;
        var legacyV1 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "layouts", "run-layout-4d.v1.json"));

        Assert.False(validator.Validate(DocumentTypes.RunLayout, v2WithLegacyField).IsValid);
        Assert.True(validator.Validate(DocumentTypes.RunLayout, legacyV1).IsValid);
    }

    [Fact]
    public void RunLayoutV2_DerivesOrderFromArray_AndRejectsLegacyOrderingField()
    {
        var layout = new RunLayoutDefinition
        {
            Metadata = Meta.Of("RUN_LAYOUT", "RUN_LAYOUT_4D", version: 2) with { SchemaVersion = 2 },
            RunsPerWeek = 4,
            Slots =
            [
                new LayoutSlotDefinition { Role = SlotRole.KeySession },
                new LayoutSlotDefinition { Role = SlotRole.EasySupport },
                new LayoutSlotDefinition { Role = SlotRole.EasySupport },
                new LayoutSlotDefinition { Role = SlotRole.LongRun }
            ]
        };
        var reordered = layout with { Slots = layout.Slots.Reverse().ToList() };
        var withLegacyField = layout with
        {
            Slots = [new LayoutSlotDefinition { SequenceOrder = 1, Role = SlotRole.KeySession }]
        };

        Assert.True(RunLayoutValidator.Validate(layout).IsValid);
        Assert.Equal([SlotRole.KeySession, SlotRole.EasySupport, SlotRole.EasySupport, SlotRole.LongRun], layout.Slots.Select(s => s.Role).ToList());
        Assert.Equal([SlotRole.LongRun, SlotRole.EasySupport, SlotRole.EasySupport, SlotRole.KeySession], reordered.Slots.Select(s => s.Role).ToList());
        Assert.Contains(RunLayoutValidator.Validate(withLegacyField).Issues, i => i.Code == "LEGACY_SEQUENCE_ORDER_NOT_ALLOWED_IN_NEW_SCHEMA");
    }

    [Fact]
    public void WorkoutComponents_OptionalForContinuous_RequiredAndOrderedForStructuredWorkouts()
    {
        var easy = Workout("EASY_STANDARD", WorkoutFamily.Easy, components: null);
        var longRun = Workout("LONG_RUN_STANDARD", WorkoutFamily.LongRun, components: null);
        var fartlek = Workout("FARTLEK", WorkoutFamily.Quality,
            Component(1, WorkoutComponentType.WarmUp),
            Component(2, WorkoutComponentType.MainSet),
            Component(3, WorkoutComponentType.Recovery),
            Component(4, WorkoutComponentType.CoolDown));
        var threshold = Workout("THRESHOLD_TEMPO", WorkoutFamily.Quality,
            Component(1, WorkoutComponentType.WarmUp),
            Component(2, WorkoutComponentType.MainSet),
            Component(3, WorkoutComponentType.CoolDown));

        Assert.True(WorkoutDefinitionValidator.Validate(easy).IsValid);
        Assert.True(WorkoutDefinitionValidator.Validate(longRun).IsValid);
        Assert.True(WorkoutDefinitionValidator.Validate(fartlek).IsValid);
        Assert.True(WorkoutDefinitionValidator.Validate(threshold).IsValid);

        Assert.Contains(WorkoutDefinitionValidator.Validate(Workout("FARTLEK", WorkoutFamily.Quality,
            Component(1, WorkoutComponentType.WarmUp),
            Component(2, WorkoutComponentType.MainSet),
            Component(3, WorkoutComponentType.CoolDown))).Issues, i => i.Code == "COMPONENT_SEQUENCE_INVALID");

        Assert.Contains(WorkoutDefinitionValidator.Validate(Workout("THRESHOLD_TEMPO", WorkoutFamily.Quality,
            Component(1, WorkoutComponentType.WarmUp),
            Component(2, WorkoutComponentType.MainSet),
            Component(3, WorkoutComponentType.Recovery),
            Component(4, WorkoutComponentType.CoolDown))).Issues, i => i.Code == "COMPONENT_TYPE_NOT_ALLOWED");
    }

    [Fact]
    public void WorkoutComponents_RejectEmptyDuplicateAndNonSharedVocabulary()
    {
        Assert.Contains(WorkoutDefinitionValidator.Validate(Workout("EASY_STANDARD", WorkoutFamily.Easy, [])).Issues, i => i.Code == "COMPONENTS_EMPTY");
        Assert.Contains(WorkoutDefinitionValidator.Validate(Workout("FARTLEK", WorkoutFamily.Quality,
            Component(1, WorkoutComponentType.WarmUp),
            Component(2, WorkoutComponentType.WarmUp))).Issues, i => i.Code == "COMPONENT_SEQUENCE_INVALID");
        Assert.Contains(WorkoutDefinitionValidator.Validate(Workout("FARTLEK", WorkoutFamily.Quality,
            Component(1, WorkoutComponentType.Strides))).Issues, i => i.Code == "COMPONENT_TYPE_NOT_ALLOWED");
    }

    [Fact]
    public void CandidateBundle_UsesWave2ExactVersionsAndComponentStructures()
    {
        var bundle = BuildBundle("TEN_K__4D__INTERMEDIATE", 5);
        var snapshot = LoadSnapshot();

        Assert.Equal(5, bundle.BundleVersion);
        Assert.Equal(2, bundle.Layout.Version);
        Assert.Equal(3, bundle.LevelModifier.Version);
        Assert.Equal(3, bundle.WorkoutProgression.Version);
        Assert.Contains(bundle.Workouts, w => w.Key == "LONG_RUN_STANDARD" && w.Version == 3);
        Assert.Contains(bundle.Workouts, w => w.Key == "FARTLEK" && w.Version == 3);
        Assert.Contains(bundle.Workouts, w => w.Key == "THRESHOLD_TEMPO" && w.Version == 3);

        var easy = snapshot.Workouts.Single(w => w.Metadata.Key == "EASY_STANDARD" && w.Metadata.Version == 3);
        var longRun = snapshot.Workouts.Single(w => w.Metadata.Key == "LONG_RUN_STANDARD" && w.Metadata.Version == 3);
        var fartlek = snapshot.Workouts.Single(w => w.Metadata.Key == "FARTLEK" && w.Metadata.Version == 3);
        var threshold = snapshot.Workouts.Single(w => w.Metadata.Key == "THRESHOLD_TEMPO" && w.Metadata.Version == 3);

        Assert.Null(easy.Components);
        Assert.Null(longRun.Components);
        Assert.Equal([WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.Recovery, WorkoutComponentType.CoolDown], fartlek.Components!.Select(c => c.ComponentType).ToList());
        Assert.Equal([WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.CoolDown], threshold.Components!.Select(c => c.ComponentType).ToList());
    }

    [Fact]
    public void CandidateClosure_RemovesExactlyFiveWave2Blockers()
    {
        var active = BuildBundle("TEN_K__4D__INTERMEDIATE", 4);
        var candidate = BuildBundle("TEN_K__4D__INTERMEDIATE", 5);

        Assert.Equal(13, BlockerScopeMeasurement.ScopedDecisionCount(Closure(active)));
        Assert.Equal(8, BlockerScopeMeasurement.ScopedDecisionCount(Closure(candidate)));

        var activeBlocking = BlockingEntries(Closure(active));
        var candidateBlocking = BlockingEntries(Closure(candidate));
        var removed = activeBlocking
            .Where(a => !candidateBlocking.Any(c => c.DocumentType == a.DocumentType && c.Key == a.Key && c.JsonPath == a.JsonPath))
            .Select(e => $"{e.Key}:{e.JsonPath}")
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([
            "EASY_STANDARD:$.components",
            "FARTLEK:$.components",
            "LONG_RUN_STANDARD:$.components",
            "RUN_LAYOUT_4D:$.slots[*].sequenceOrder",
            "THRESHOLD_TEMPO:$.components"
        ], removed);
    }

    private static WorkoutDefinition Workout(string key, WorkoutFamily family, params WorkoutComponentDefinition[]? components) => new()
    {
        Metadata = Meta.Of("WORKOUT_DEFINITION", key, version: 3) with { SchemaVersion = 2 },
        Family = family,
        ComplexityTier = 1,
        EligiblePhases = [PhaseKey.Build],
        AllowedPrescriptionModes = [PrescriptionMode.Mixed],
        AllowedDistanceAccountingModes = [DistanceAccountingMode.EstimatedSessionTotal],
        Components = components
    };

    private static WorkoutComponentDefinition Component(int order, WorkoutComponentType type) => new()
    {
        SequenceOrder = order,
        ComponentType = type,
        IntensityDescriptor = "STRUCTURAL"
    };

    private static IEnumerable<BlockerScopeMeasurement.ArtifactIdentity> Closure(PublishedTemplateBundle bundle)
    {
        yield return Id(bundle.Combination);
        yield return Id(bundle.MasterTemplate);
        yield return Id(bundle.Layout);
        yield return Id(bundle.LevelModifier);
        yield return Id(bundle.WorkoutProgression);
        yield return Id(bundle.ProgressionModifier);
        yield return Id(bundle.RulePack);
        yield return Id(bundle.RuntimeConditionValueRegistry);
        yield return Id(bundle.PeakVolumeBandPolicy);
        foreach (var workout in bundle.Workouts)
        {
            yield return Id(workout);
        }
    }

    private static BlockerScopeMeasurement.ArtifactIdentity Id(Contracts.References.CatalogArtifactReference reference) =>
        new(reference.DocumentType, reference.Key, reference.Version);

    private static IReadOnlyList<DomainContentDecision> BlockingEntries(IEnumerable<BlockerScopeMeasurement.ArtifactIdentity> closure)
    {
        var scope = closure.ToHashSet();
        return PilotDomainContentAudit.Entries
            .Where(e => e.IsBlocking && scope.Contains(new BlockerScopeMeasurement.ArtifactIdentity(e.DocumentType, e.Key, e.Version)))
            .ToList();
    }
}
