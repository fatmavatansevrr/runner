using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Projection;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Golden;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split E — the real, authored Intermediate×5D catalog content
/// (`RUN_LAYOUT_5D`, `TEN_K__5D__INTERMEDIATE`, `TEN_K_WORKOUT_PROGRESSION_V1 v6`,
/// `TEN_K_MASTER v7`, `INTERMEDIATE_MODIFIER v7`, `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v3`)
/// proven end-to-end against the real catalog source tree — no synthetic fixture stands in
/// for any assertion in this file. This is the real-reachability closure Split B's own
/// synthetic-fixture proof explicitly deferred to "whenever real 5D content exists."
/// </summary>
public sealed class Intermediate5DRealCatalogIntegrationTests
{
    private static readonly SystemTextJsonCanonicalSerializer Serializer = new();
    private static readonly Sha256ContentHasher Hasher = new();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static CatalogSourceSnapshot LoadSnapshot() =>
        new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog")).LoadSnapshot();

    private static readonly (string Key, int Version)[] AllEightProfiles =
    [
        ("INTERMEDIATE_5D_FOUNDATION_PRIMARY", 1),
        ("INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED", 1),
        ("INTERMEDIATE_5D_BUILD_PRIMARY", 1),
        ("INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED", 1),
        ("INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY", 1),
        ("INTERMEDIATE_5D_RACE_SPECIFIC_SECONDARY_CONTROLLED", 1),
        ("INTERMEDIATE_5D_TAPER_PRIMARY", 1),
        ("INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED", 1),
    ];

    [Fact]
    public void RunLayout5D_ExistsAndHasFiveSlots_TwoKeyTwoEasyOneLong()
    {
        var snapshot = LoadSnapshot();
        var layout = snapshot.RunLayouts.Single(l => l.Metadata.Key == "RUN_LAYOUT_5D" && l.Metadata.Version == 1);

        Assert.Equal(5, layout.RunsPerWeek);
        Assert.Equal(5, layout.Slots.Count);
        Assert.Equal(2, layout.Slots.Count(s => s.Role == PlanCatalog.Contracts.Enums.SlotRole.KeySession));
        Assert.Equal(2, layout.Slots.Count(s => s.Role == PlanCatalog.Contracts.Enums.SlotRole.EasySupport));
        Assert.Equal(1, layout.Slots.Count(s => s.Role == PlanCatalog.Contracts.Enums.SlotRole.LongRun));
        // Canonical slot order: first KEY occurrence must precede the second (structural
        // ordinal 0 -> LaneOrdinal 0 through the real RunningApp binder, Split A).
        var keyIndices = layout.Slots.Select((s, i) => (s, i)).Where(x => x.s.Role == PlanCatalog.Contracts.Enums.SlotRole.KeySession).Select(x => x.i).ToList();
        Assert.Equal(2, keyIndices.Count);
        Assert.True(keyIndices[0] < keyIndices[1]);
    }

    [Fact]
    public void Combination_ResolvesExactPinnedDependencies_NoLatestNoNearest()
    {
        var snapshot = LoadSnapshot();
        var combination = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__5D__INTERMEDIATE" && c.Metadata.Version == 1);

        Assert.Equal(("TEN_K_MASTER", 7), (combination.MasterTemplate.Key, combination.MasterTemplate.Version));
        Assert.Equal(("RUN_LAYOUT_5D", 1), (combination.Layout.Key, combination.Layout.Version));
        Assert.Equal(("INTERMEDIATE_MODIFIER", 7), (combination.LevelModifier.Key, combination.LevelModifier.Version));
        Assert.Equal(("APPSEL_RACE_PLAN_V1", 4), (combination.RulePack.Key, combination.RulePack.Version));

        var master = snapshot.FindPlanTemplate(combination.MasterTemplate)!;
        Assert.Equal(("TEN_K_WORKOUT_PROGRESSION_V1", 6), (master.WorkoutProgression.Key, master.WorkoutProgression.Version));

        // Never coerces to the real 4D identity.
        Assert.NotEqual("TEN_K__4D__INTERMEDIATE", combination.Metadata.Key);
        Assert.NotEqual("RUN_LAYOUT_4D", combination.Layout.Key);
    }

    [Fact]
    public void RealCatalog_PassesFullGraphValidation()
    {
        var result = CatalogGraphValidator.Validate(LoadSnapshot());
        Assert.True(result.IsValid, string.Join("\n", result.Issues.Select(i => $"{i.Code}: {i.Message} ({i.JsonPath})")));
    }

    [Fact]
    public void Progression_BothLanesAuthoredForEveryPhase_WithExactProfileCandidates()
    {
        var snapshot = LoadSnapshot();
        var progression = snapshot.FindWorkoutProgression(new() { DocumentType = "WORKOUT_PROGRESSION", Key = "TEN_K_WORKOUT_PROGRESSION_V1", Version = 6 })!;

        foreach (var phase in progression.PhaseProgressions)
        {
            Assert.Equal(2, phase.EffectiveLanes.Count);
            Assert.Contains(phase.EffectiveLanes, l => l.LaneOrdinal == 0);
            Assert.Contains(phase.EffectiveLanes, l => l.LaneOrdinal == 1);

            foreach (var lane in phase.EffectiveLanes)
            {
                var stage = Assert.Single(lane.Stages);
                var candidate = Assert.Single(stage.PrescriptionProfileCandidates!);
                var profile = snapshot.FindPrescriptionProfile(candidate.Key, candidate.Version);
                Assert.NotNull(profile);

                var laneDoseResult = PrescriptionProfileLaneDoseValidator.Validate(lane.LaneOrdinal, profile!);
                Assert.True(laneDoseResult.IsValid, string.Join(", ", laneDoseResult.Issues.Select(i => i.Message)));
            }
        }
    }

    [Fact]
    public void AllEightRealProfiles_ReachableFromRealProgression_NoneDeadContent()
    {
        var snapshot = LoadSnapshot();
        var progression = snapshot.FindWorkoutProgression(new() { DocumentType = "WORKOUT_PROGRESSION", Key = "TEN_K_WORKOUT_PROGRESSION_V1", Version = 6 })!;

        var closure = PrescriptionProfileClosureResolver.ComputeExactClosureRefs(progression);

        foreach (var (key, version) in AllEightProfiles)
        {
            Assert.Contains(closure, r => r.Key == key && r.Version == version);
        }
        Assert.Equal(8, closure.Count);
    }

    private static PublishedTemplateBundle AssembleRealBundle()
    {
        var snapshot = LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(Serializer, Hasher, snapshot);
        var progression = stamped.FindWorkoutProgression(new() { DocumentType = "WORKOUT_PROGRESSION", Key = "TEN_K_WORKOUT_PROGRESSION_V1", Version = 6 })!;
        var dependencies = PrescriptionProjectionDependencyResolver.ResolveForProgression(progression);
        var assembler = new CatalogBundleAssembler(Serializer, Hasher);
        return assembler.Assemble(stamped, "TEN_K__5D__INTERMEDIATE", 1, dependencies);
    }

    [Fact]
    public void RealBundle_ExecutionPrescriptionsNonNull_AllEightProfilesPresent()
    {
        var bundle = AssembleRealBundle();

        Assert.NotNull(bundle.ExecutionPrescriptions);
        Assert.Equal(8, bundle.ExecutionPrescriptions!.Count);
        foreach (var (key, version) in AllEightProfiles)
        {
            Assert.Contains(bundle.ExecutionPrescriptions, p => p.SourceProfile.Key == key && p.SourceProfile.Version == version);
        }
    }

    [Fact]
    public void RealBundle_AssembledTwice_DeterministicHashAndOrder()
    {
        var bundle1 = AssembleRealBundle();
        var bundle2 = AssembleRealBundle();

        Assert.Equal(bundle1.BundleContentHash, bundle2.BundleContentHash);
        Assert.Equal(
            bundle1.ExecutionPrescriptions!.Select(p => (p.SourceProfile.Key, p.SourceProfile.Version)),
            bundle2.ExecutionPrescriptions!.Select(p => (p.SourceProfile.Key, p.SourceProfile.Version)));
    }

    [Fact]
    public void RealBundle_BuildSTapeS_ContentFidelity_RecoveryAndDoseCategoryPreserved()
    {
        var bundle = AssembleRealBundle();

        var bldS = bundle.ExecutionPrescriptions!.Single(p => p.SourceProfile.Key == "INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED");
        var bldComponent = bldS.Components.Single(c => c.ComponentType == PlanCatalog.Contracts.Enums.WorkoutComponentType.MainSet);
        Assert.Equal(10, bldComponent.RepetitionCount);
        Assert.Equal(60, bldComponent.Work.Value);
        Assert.Equal(60, bldComponent.Recovery!.Value);
        Assert.Equal(9, bldComponent.Recovery!.RecoveryCount);
        Assert.Equal(PlanCatalog.Contracts.Enums.ExecutableRecoveryPlacement.BetweenRepetitions, bldComponent.Recovery!.Placement);
        Assert.Equal(PlanCatalog.Contracts.Enums.ExecutablePrescriptionDoseCategory.SecondaryControlled, bldS.DoseCategory);
        Assert.Equal("FARTLEK", bldS.SourceWorkout.Key);
        Assert.Equal(5, bldS.SourceWorkout.Version);

        var tapS = bundle.ExecutionPrescriptions!.Single(p => p.SourceProfile.Key == "INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED");
        var tapComponent = tapS.Components.Single(c => c.ComponentType == PlanCatalog.Contracts.Enums.WorkoutComponentType.MainSet);
        Assert.Equal(6, tapComponent.RepetitionCount);
        Assert.Equal(20, tapComponent.Work.Value);
        Assert.Equal(100, tapComponent.Recovery!.Value);
        Assert.Equal(5, tapComponent.Recovery!.RecoveryCount);
        Assert.Equal(PlanCatalog.Contracts.Enums.ExecutableRecoveryPlacement.BetweenRepetitions, tapComponent.Recovery!.Placement);
        Assert.Equal(PlanCatalog.Contracts.Enums.ExecutablePrescriptionDoseCategory.SecondaryControlled, tapS.DoseCategory);
    }

    [Fact]
    public void Legacy4DCombination_RemainsExecutionPrescriptionsNull_ZeroDelta()
    {
        var snapshot = LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(Serializer, Hasher, snapshot);
        var assembler = new CatalogBundleAssembler(Serializer, Hasher);

        var bundle = assembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 10);

        Assert.Null(bundle.ExecutionPrescriptions);
    }
}
