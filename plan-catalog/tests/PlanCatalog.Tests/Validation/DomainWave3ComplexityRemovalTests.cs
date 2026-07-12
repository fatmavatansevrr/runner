using System.Text.Json;
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

public sealed class DomainWave3ComplexityRemovalTests
{
    private static readonly string[] Wave3WorkoutFiles =
    [
        "easy-standard.v4.json",
        "fartlek.v4.json",
        "long-run-standard.v4.json",
        "threshold-tempo.v4.json"
    ];

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

    private static PublishedTemplateBundle BuildBundle(int version)
    {
        var snapshot = LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        return new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher()).Assemble(stamped, "TEN_K__4D__INTERMEDIATE", version);
    }

    [Fact]
    public void WorkoutSchemaV3_RejectsComplexityTier_AndLegacyV2RequiresIt()
    {
        var validator = new JsonSchemaNetValidator(Path.Combine(AppContext.BaseDirectory, "TestSchemas"));
        var v3WithComplexity = """
        {
          "metadata": { "documentType": "WORKOUT_DEFINITION", "schemaVersion": 3, "key": "EASY_STANDARD", "version": 4, "status": "DRAFT" },
          "family": "EASY",
          "complexityTier": 1,
          "eligiblePhases": ["FOUNDATION"],
          "allowedPrescriptionModes": ["DISTANCE"]
        }
        """;
        var legacyV2 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "workouts", "easy-standard.v3.json"));
        var legacyV2MissingComplexity = legacyV2.Replace("\r\n  \"complexityTier\": 1,", string.Empty).Replace("\n  \"complexityTier\": 1,", string.Empty);

        Assert.False(validator.Validate(DocumentTypes.WorkoutDefinition, v3WithComplexity).IsValid);
        Assert.True(validator.Validate(DocumentTypes.WorkoutDefinition, legacyV2).IsValid);
        Assert.False(validator.Validate(DocumentTypes.WorkoutDefinition, legacyV2MissingComplexity).IsValid);
    }

    [Fact]
    public void WorkoutValidator_EnforcesLegacyComplexityOnlyOnLegacySchemas()
    {
        var v3 = Workout("EASY_STANDARD", 4, schemaVersion: 3);
        var v3WithLegacyField = v3 with { ComplexityTier = 1 };
        var legacyMissing = Workout("EASY_STANDARD", 3, schemaVersion: 2);

        Assert.True(WorkoutDefinitionValidator.Validate(v3).IsValid);
        Assert.Contains(WorkoutDefinitionValidator.Validate(v3WithLegacyField).Issues, i => i.Code == "LEGACY_COMPLEXITY_TIER_NOT_ALLOWED_IN_NEW_SCHEMA");
        Assert.Contains(WorkoutDefinitionValidator.Validate(legacyMissing).Issues, i => i.Code == "WD_COMPLEXITY_TIER_TOO_LOW");
    }

    [Fact]
    public void Wave3WorkoutArtifacts_OmitComplexityTier_AndKeepWave2ComponentsUnchanged()
    {
        foreach (var file in Wave3WorkoutFiles)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "workouts", file)));
            Assert.False(doc.RootElement.TryGetProperty("complexityTier", out _));
            Assert.Equal(3, doc.RootElement.GetProperty("metadata").GetProperty("schemaVersion").GetInt32());
        }

        var snapshot = LoadSnapshot();
        Assert.Null(Workout(snapshot, "EASY_STANDARD", 4).Components);
        Assert.Null(Workout(snapshot, "LONG_RUN_STANDARD", 4).Components);
        Assert.Equal(
            [WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.Recovery, WorkoutComponentType.CoolDown],
            Workout(snapshot, "FARTLEK", 4).Components!.Select(c => c.ComponentType).ToList());
        Assert.Equal(
            [WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.CoolDown],
            Workout(snapshot, "THRESHOLD_TEMPO", 4).Components!.Select(c => c.ComponentType).ToList());
    }

    [Fact]
    public void Wave3Bundle_ContainsNoWorkoutComplexityTier_AndUsesExactCascadeVersions()
    {
        var bundle = BuildBundle(6);
        var serializer = new SystemTextJsonCanonicalSerializer();
        var json = serializer.Serialize(bundle);

        Assert.DoesNotContain("complexityTier", json);
        Assert.Equal(6, bundle.BundleVersion);
        Assert.Equal(5, bundle.MasterTemplate.Version);
        Assert.Equal(2, bundle.Layout.Version);
        Assert.Equal(4, bundle.LevelModifier.Version);
        Assert.Equal(4, bundle.WorkoutProgression.Version);
        Assert.Contains(bundle.Workouts, w => w.Key == "EASY_STANDARD" && w.Version == 4);
        Assert.Contains(bundle.Workouts, w => w.Key == "LONG_RUN_STANDARD" && w.Version == 4);
        Assert.Contains(bundle.Workouts, w => w.Key == "FARTLEK" && w.Version == 4);
        Assert.Contains(bundle.Workouts, w => w.Key == "THRESHOLD_TEMPO" && w.Version == 4);
        Assert.Contains(bundle.Workouts, w => w.Key == "GOAL_PACE_TEN_K" && w.Version == 1);
    }

    [Fact]
    public void CandidateClosure_ReducesWave3BlockersFromEightToFour()
    {
        Assert.Equal(13, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(4))));
        Assert.Equal(8, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(5))));
        Assert.Equal(4, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(6))));

        var remaining = BlockingEntries(Closure(BuildBundle(6))).Select(e => $"{e.Key}:{e.JsonPath}").OrderBy(x => x).ToList();
        Assert.Equal([
            "GOAL_PACE_TEN_K:$.eligiblePhases, $.complexityTier, $.allowedPrescriptionModes, $.components",
            "INTERMEDIATE_PROGRESSION_MODIFIER_V1:$.maximumComplexityTier, $.maximumHardSessionsPerWeek, $.mainSetDoseMultiplier, $.allowGoalPaceRehearsal, $.allowSecondHardStimulus",
            "PEAK_VOLUME_BANDS_V1:$.entries[TEN_K,NEW|ADVANCED|EXPERIENCED,3|4|5]",
            "RUNTIME_CONDITION_VALUES_V1:$.conditionValueSets[PACE_SOURCE_IN,TIME_ADEQUACY_IN,CORE_ENTRY_READINESS_IN]"
        ], remaining);
    }

    [Fact]
    public void Wave3EvidenceReview_DowngradesD8AndD12ToExplicitProductDefaults()
    {
        var entries = PilotDomainContentAudit.Entries
            .Where(e => e.DocumentType == DocumentTypes.WorkoutDefinition && e.Version == 4 && e.JsonPath == "$.components")
            .OrderBy(e => e.Key)
            .ToList();

        Assert.Equal(["FARTLEK", "THRESHOLD_TEMPO"], entries.Select(e => e.Key).ToList());
        Assert.All(entries, e => Assert.Equal(ContentDecisionStatus.ExplicitProductDefault, e.Classification));
        Assert.DoesNotContain(entries, e => e.IsBlocking);
        Assert.Contains(entries, e => e.Key == "FARTLEK" && e.CurrentValue == "WARM_UP, MAIN_SET, RECOVERY, COOL_DOWN");
        Assert.Contains(entries, e => e.Key == "THRESHOLD_TEMPO" && e.CurrentValue == "WARM_UP, MAIN_SET, COOL_DOWN");
    }

    [Fact]
    public void ComplexityTierRemoval_DoesNotIntroduceReplacementTaxonomyFields()
    {
        var prohibited = new[] { "difficulty", "difficultyTier", "complexity", "tier", "workoutLevel" };
        foreach (var file in Wave3WorkoutFiles)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "workouts", file)));
            foreach (var field in prohibited)
            {
                Assert.False(doc.RootElement.TryGetProperty(field, out _), $"{file} contains replacement field {field}.");
            }
        }
    }

    private static WorkoutDefinition Workout(string key, int version, int schemaVersion) => new()
    {
        Metadata = Meta.Of("WORKOUT_DEFINITION", key, version: version) with { SchemaVersion = schemaVersion },
        Family = WorkoutFamily.Easy,
        EligiblePhases = [PhaseKey.Build],
        AllowedPrescriptionModes = [PrescriptionMode.Mixed]
    };

    private static WorkoutDefinition Workout(Core.Catalog.CatalogSourceSnapshot snapshot, string key, int version) =>
        snapshot.Workouts.Single(w => w.Metadata.Key == key && w.Metadata.Version == version);

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
