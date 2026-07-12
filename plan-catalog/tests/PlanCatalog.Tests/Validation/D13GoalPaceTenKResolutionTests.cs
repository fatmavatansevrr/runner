using System.Text.Json;
using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Core.Audit;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>D13: GOAL_PACE_TEN_K resolution for the TEN_K/4D/INTERMEDIATE pilot candidate.</summary>
public sealed class D13GoalPaceTenKResolutionTests
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

    private static PublishedTemplateBundle BuildBundle(int version)
    {
        var snapshot = LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        return new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher()).Assemble(stamped, "TEN_K__4D__INTERMEDIATE", version);
    }

    [Fact]
    public void GoalPaceTenKV2_IsByteIdenticalInContentToV1_OnlyVersionChanged()
    {
        var v1 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "workouts", "goal-pace-ten-k.v1.json"));
        var v2 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "workouts", "goal-pace-ten-k.v2.json"));

        using var v1Doc = JsonDocument.Parse(v1);
        using var v2Doc = JsonDocument.Parse(v2);

        Assert.Equal(v1Doc.RootElement.GetProperty("family").GetString(), v2Doc.RootElement.GetProperty("family").GetString());
        Assert.Equal(v1Doc.RootElement.GetProperty("complexityTier").GetInt32(), v2Doc.RootElement.GetProperty("complexityTier").GetInt32());
        Assert.Equal(v1Doc.RootElement.GetProperty("eligiblePhases").ToString(), v2Doc.RootElement.GetProperty("eligiblePhases").ToString());
        Assert.Equal(v1Doc.RootElement.GetProperty("allowedPrescriptionModes").ToString(), v2Doc.RootElement.GetProperty("allowedPrescriptionModes").ToString());
        Assert.Equal(v1Doc.RootElement.GetProperty("components").ToString(), v2Doc.RootElement.GetProperty("components").ToString());
        Assert.Equal(2, v2Doc.RootElement.GetProperty("metadata").GetProperty("version").GetInt32());
    }

    [Fact]
    public void GoalPaceTenK_IsScopedToRaceSpecificPhaseOnly()
    {
        var snapshot = LoadSnapshot();
        var workout = snapshot.Workouts.Single(w => w.Metadata.Key == "GOAL_PACE_TEN_K" && w.Metadata.Version == 2);

        Assert.Equal([PlanCatalog.Contracts.Enums.PhaseKey.RaceSpecific], workout.EligiblePhases);
    }

    [Fact]
    public void GoalPaceRehearsalStage_ExistsOnlyUnderRaceSpecificPhase_NeverFoundationBuildOrTaper()
    {
        var snapshot = LoadSnapshot();
        var progression = snapshot.WorkoutProgressions.Single(p => p.Metadata.Key == "TEN_K_WORKOUT_PROGRESSION_V1" && p.Metadata.Version == 5);

        foreach (var phase in progression.PhaseProgressions)
        {
            var hasGoalPaceStage = phase.Stages.Any(s => s.StageKey == "GOAL_PACE_REHEARSAL");
            if (phase.PhaseKey == PlanCatalog.Contracts.Enums.PhaseKey.RaceSpecific)
            {
                Assert.True(hasGoalPaceStage);
            }
            else
            {
                Assert.False(hasGoalPaceStage);
            }
        }
    }

    [Fact]
    public void GoalPaceRehearsalStage_IsNotEveryWeek_BoundedExposures()
    {
        var snapshot = LoadSnapshot();
        var progression = snapshot.WorkoutProgressions.Single(p => p.Metadata.Key == "TEN_K_WORKOUT_PROGRESSION_V1" && p.Metadata.Version == 5);
        var stage = progression.PhaseProgressions.Single(p => p.PhaseKey == PlanCatalog.Contracts.Enums.PhaseKey.RaceSpecific)
            .Stages.Single(s => s.StageKey == "GOAL_PACE_REHEARSAL");

        Assert.True(stage.MinimumExposures >= 1);
        Assert.True(stage.MaximumExposures < progression.PhaseProgressions.Single(p => p.PhaseKey == PlanCatalog.Contracts.Enums.PhaseKey.RaceSpecific).Stages.Count * 10);
        Assert.Equal(1, stage.MinimumExposures);
        Assert.Equal(2, stage.MaximumExposures);
    }

    [Fact]
    public void GoalPaceRehearsalStage_SharesTheSameSingleKeySessionSlot_AsOtherRaceSpecificStages()
    {
        // Stages within a phaseProgression are sequential alternatives for the SAME weekly KEY_SESSION
        // slot (RunLayoutValidator/TemplateCombinationValidator cap KEY_SESSION count at
        // MaximumHardSessionsPerWeek=1, and there is exactly one KEY_SESSION slot in RUN_LAYOUT_4D) --
        // not additional slots. Proven structurally: GOAL_PACE_REHEARSAL is one of three RelativeOrder
        // stages (TEN_K_SPECIFIC_INTRO -> GOAL_PACE_REHEARSAL -> CURRENT_FITNESS_SPECIFIC_REHEARSAL) in
        // the same RACE_SPECIFIC phaseProgression, not a parallel/independent progression.
        var snapshot = LoadSnapshot();
        var raceSpecific = snapshot.WorkoutProgressions.Single(p => p.Metadata.Key == "TEN_K_WORKOUT_PROGRESSION_V1" && p.Metadata.Version == 5)
            .PhaseProgressions.Single(p => p.PhaseKey == PlanCatalog.Contracts.Enums.PhaseKey.RaceSpecific);

        Assert.Equal(["TEN_K_SPECIFIC_INTRO", "GOAL_PACE_REHEARSAL", "CURRENT_FITNESS_SPECIFIC_REHEARSAL"],
            raceSpecific.Stages.OrderBy(s => s.RelativeOrder).Select(s => s.StageKey).ToList());

        var layout = snapshot.RunLayouts.Single(l => l.Metadata.Key == "RUN_LAYOUT_4D" && l.Metadata.Version == 2);
        Assert.Equal(1, layout.Slots.Count(s => s.Role == Contracts.Enums.SlotRole.KeySession));
    }

    [Fact]
    public void MaximumHardSessionsPerWeek_RemainsOne_AllowSecondHardStimulus_RemainsFalse()
    {
        var snapshot = LoadSnapshot();
        var modifier = snapshot.ProgressionModifiers.Single(m => m.Metadata.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1" && m.Metadata.Version == 2);

        Assert.Equal(1, modifier.MaximumHardSessionsPerWeek);
        Assert.False(modifier.AllowSecondHardStimulus);
        Assert.True(modifier.AllowGoalPaceRehearsal);
    }

    [Fact]
    public void TaperPhase_NeverReferencesGoalPaceTenK()
    {
        var snapshot = LoadSnapshot();
        var taper = snapshot.WorkoutProgressions.Single(p => p.Metadata.Key == "TEN_K_WORKOUT_PROGRESSION_V1" && p.Metadata.Version == 5)
            .PhaseProgressions.Single(p => p.PhaseKey == PlanCatalog.Contracts.Enums.PhaseKey.Taper);

        Assert.DoesNotContain(taper.Stages, s => s.WorkoutCandidates?.Any(c => c.Key == "GOAL_PACE_TEN_K") == true);
    }

    [Fact]
    public void V10Bundle_ReferencesExactCascadeVersions_AndDoesNotBumpUnrelatedArtifacts()
    {
        var v9 = BuildBundle(9);
        var v10 = BuildBundle(10);

        Assert.Equal(10, v10.BundleVersion);
        Assert.Equal(2, v10.Workouts.Single(w => w.Key == "GOAL_PACE_TEN_K").Version);
        Assert.Equal(5, v10.WorkoutProgression.Version);
        Assert.Equal(6, v10.MasterTemplate.Version);
        Assert.Equal(6, v10.LevelModifier.Version);

        Assert.Equal(v9.Layout.Version, v10.Layout.Version);
        Assert.Equal(v9.RulePack.Version, v10.RulePack.Version);
        Assert.Equal(v9.ProgressionModifier.Version, v10.ProgressionModifier.Version);
        Assert.Equal(v9.RuntimeConditionValueRegistry.Version, v10.RuntimeConditionValueRegistry.Version);
        Assert.Equal(v9.PeakVolumeBandPolicy.Version, v10.PeakVolumeBandPolicy.Version);
        Assert.Equal(v9.Workouts.Single(w => w.Key == "EASY_STANDARD").Version, v10.Workouts.Single(w => w.Key == "EASY_STANDARD").Version);
        Assert.Equal(v9.Workouts.Single(w => w.Key == "FARTLEK").Version, v10.Workouts.Single(w => w.Key == "FARTLEK").Version);
        Assert.Equal(v9.Workouts.Single(w => w.Key == "LONG_RUN_STANDARD").Version, v10.Workouts.Single(w => w.Key == "LONG_RUN_STANDARD").Version);
        Assert.Equal(v9.Workouts.Single(w => w.Key == "THRESHOLD_TEMPO").Version, v10.Workouts.Single(w => w.Key == "THRESHOLD_TEMPO").Version);
    }

    [Fact]
    public void V10CandidateBuild_IsByteIdenticalAcrossThreeBuilds()
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var json1 = serializer.Serialize(BuildBundle(10));
        var json2 = serializer.Serialize(BuildBundle(10));
        var json3 = serializer.Serialize(BuildBundle(10));

        Assert.Equal(json1, json2);
        Assert.Equal(json2, json3);
        Assert.Equal(BuildBundle(10).BundleContentHash, BuildBundle(10).BundleContentHash);
    }

    [Fact]
    public void CandidateClosure_RemovesD13FromV10Candidate_ZeroBlockersRemain()
    {
        var v9Remaining = BlockingEntries(Closure(BuildBundle(9))).Select(e => $"{e.Key}:{e.JsonPath}").OrderBy(x => x).ToList();
        var v10Remaining = BlockingEntries(Closure(BuildBundle(10))).Select(e => $"{e.Key}:{e.JsonPath}").OrderBy(x => x).ToList();

        Assert.Equal(1, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(9))));
        Assert.Equal(0, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(10))));

        Assert.Contains(v9Remaining, e => e.StartsWith("GOAL_PACE_TEN_K:"));
        Assert.Empty(v10Remaining);
    }

    [Fact]
    public void PriorCandidates_AreUnchangedByD13Resolution()
    {
        var v8 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "combinations", "ten-k-4d-intermediate.v8.json"));
        var v9 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "combinations", "ten-k-4d-intermediate.v9.json"));

        using var v9Doc = JsonDocument.Parse(v9);
        Assert.Equal(5, v9Doc.RootElement.GetProperty("levelModifier").GetProperty("version").GetInt32());
        Assert.Contains("\"version\": 8", v8);
    }

    [Fact]
    public void D2D3D4Decisions_RemainUnchangedInV10Closure()
    {
        var snapshot = LoadSnapshot();
        var modifier = snapshot.ProgressionModifiers.Single(m => m.Metadata.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1" && m.Metadata.Version == 2);
        Assert.Null(modifier.MaximumComplexityTier);

        var registry = snapshot.RuntimeConditionValueRegistries.Single(r => r.Metadata.Key == "RUNTIME_CONDITION_VALUES_V1" && r.Metadata.Version == 2);
        Assert.Contains(registry.ConditionValueSets, s => s.ConditionType == Contracts.Enums.RuntimeConditionType.PaceSourceIn && s.AllowedValues.Contains("RECENT_RACE"));

        var policy = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 3);
        Assert.Equal(3, policy.Entries.Count);
        Assert.All(policy.Entries, e => Assert.Equal(Contracts.Enums.RunningExperience.Intermediate, e.Experience));
    }

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
