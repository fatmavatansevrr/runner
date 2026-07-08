using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Repositories;
using Xunit;

namespace PlanCatalog.Tests.Golden;

/// <summary>10K / 4D / Intermediate structural golden test — see brief §20 Adım 10. No user-specific plan is generated.</summary>
public sealed class PilotCatalogStructuralTests
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

    private static Core.Catalog.CatalogSourceSnapshot LoadPilotSnapshot() =>
        new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog")).LoadSnapshot();

    [Fact]
    public void PilotCatalog_PassesGraphValidation()
    {
        var snapshot = LoadPilotSnapshot();
        var result = CatalogGraphValidator.Validate(snapshot);

        Assert.True(result.IsValid, string.Join("\n", result.Issues.Select(i => $"{i.Code}: {i.Message} ({i.JsonPath})")));
    }

    [Fact]
    public void TenKMaster_DefaultCoreIsTwelveWeeks()
    {
        var snapshot = LoadPilotSnapshot();
        // TEN_K_MASTER v2 is the version currently referenced by the TEN_K__4D__INTERMEDIATE combination
        // (v1 remains in catalog/ only because it is already published/immutable in earlier releases).
        var master = snapshot.PlanTemplates.Single(t => t.Metadata.Key == "TEN_K_MASTER" && t.Metadata.Version == 2);

        Assert.Equal(12, master.CoreCycle.DefaultWeeks);
        Assert.Equal(8, master.CoreCycle.MinimumWeeks);
        Assert.Equal(14, master.CoreCycle.MaximumWeeks);
        Assert.Equal(12, master.Phases.Sum(p => p.PreferredWeeks));
    }

    [Fact]
    public void RunLayout4D_HasExactlyOneLongRunAndOneKeySession()
    {
        var snapshot = LoadPilotSnapshot();
        var layout = snapshot.RunLayouts.Single(l => l.Metadata.Key == "RUN_LAYOUT_4D");

        Assert.Equal(1, layout.Slots.Count(s => s.Role == SlotRole.LongRun));
        Assert.Equal(1, layout.Slots.Count(s => s.Role == SlotRole.KeySession));
        Assert.Equal(2, layout.Slots.Count(s => s.Role == SlotRole.EasySupport));
    }

    [Fact]
    public void WorkoutProgressionStages_ContainNoConcreteWeekField()
    {
        var properties = typeof(Core.Models.WorkoutProgressionStageDefinition).GetProperties();
        Assert.DoesNotContain(properties, p => p.Name is "Week" or "WeekNumber" or "CalendarDate" or "ScheduledDate");
    }

    [Fact]
    public void ProgressionModifier_BelongsToIntermediateExperience()
    {
        var snapshot = LoadPilotSnapshot();
        var progressionModifier = snapshot.ProgressionModifiers.Single(p => p.Metadata.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1");

        Assert.Equal(RunningExperience.Intermediate, progressionModifier.Experience);
    }

    [Fact]
    public void PeakVolumeBandPolicy_HasTenKIntermediateFourDayTuple()
    {
        var snapshot = LoadPilotSnapshot();
        // v1 is restored to its original historically-published content; the active/corrected policy is
        // v2 — see artifacts/audits/peak-volume-policy-immutability-remediation.md.
        var policy = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 2);

        Assert.Contains(policy.Entries, e => e.DistanceFamily == DistanceFamily.TenK && e.Experience == RunningExperience.Intermediate && e.RunsPerWeek == 4);
    }

    [Fact]
    public void EffectiveWorkoutSet_ForPilotCombination_IsNotEmpty()
    {
        var snapshot = LoadPilotSnapshot();
        var levelModifier = snapshot.LevelModifiers.Single(l => l.Metadata.Key == "INTERMEDIATE_MODIFIER" && l.Metadata.Version == 1);
        var progression = snapshot.WorkoutProgressions.Single(p => p.Metadata.Key == "TEN_K_WORKOUT_PROGRESSION_V1" && p.Metadata.Version == 1);

        var candidateKeys = progression.PhaseProgressions.SelectMany(p => p.Stages).SelectMany(s => s.WorkoutCandidateKeys ?? []).Distinct();
        var effectiveSet = candidateKeys.Where(k => levelModifier.EligibleWorkoutKeys is not null && levelModifier.EligibleWorkoutKeys.Contains(k) && snapshot.FindWorkout(k) is not null).ToList();

        Assert.NotEmpty(effectiveSet);
    }

    [Fact]
    public void RuntimeConditionVocabulary_UsedByProgression_IsValid()
    {
        var snapshot = LoadPilotSnapshot();
        var registry = snapshot.RuntimeConditionValueRegistries.Single(r => r.Metadata.Key == "RUNTIME_CONDITION_VALUES_V1");
        var progression = snapshot.WorkoutProgressions.Single(p => p.Metadata.Key == "TEN_K_WORKOUT_PROGRESSION_V1" && p.Metadata.Version == 1);

        var usedConditions = progression.PhaseProgressions.SelectMany(p => p.Stages).SelectMany(s => s.Requires);

        foreach (var condition in usedConditions)
        {
            var valueSet = registry.ConditionValueSets.Single(v => v.ConditionType == condition.ConditionType);
            Assert.All(condition.AllowedValues, v => Assert.Contains(v, valueSet.AllowedValues));
        }
    }
}
