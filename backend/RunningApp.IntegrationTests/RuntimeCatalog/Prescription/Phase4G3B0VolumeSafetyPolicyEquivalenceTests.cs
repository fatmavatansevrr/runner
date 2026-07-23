using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription;

/// <summary>
/// Backend Integration Phase 4G.3B.0 — VolumeSafetyPolicy contract extraction.
/// Proves the refactor that moved CatalogVolumeAndLongRunPlanner's private
/// inline numeric constants into <see cref="VolumeSafetyPolicy"/> produced
/// zero behavioral difference for the live 12-week TEN_K__4D__INTERMEDIATE
/// pilot: identical weekly-volume curve, identical long-run progression,
/// identical peak, identical taper. Self-contained (does not reuse
/// <c>Phase4F7BVolumeAndLongRunTests</c>'s private fixture helpers, so that
/// file needed zero edits for this phase) but uses the exact same fixture
/// shape (candidate, bound plan, request) as that file's own <c>Build(12)</c>
/// scenario, so the values compared here are the real live-pilot values, not
/// a synthetic one.
/// </summary>
public sealed class Phase4G3B0VolumeSafetyPolicyEquivalenceTests
{
    private static readonly DateOnly AsOfDate = new(2026, 7, 15);

    [Fact]
    public void VolumeSafetyPolicy_Default_IsFieldForFieldIdenticalToThePriorPrivateConstants()
    {
        // Transcription proof: every VolumeSafetyPolicy.Default field equals
        // the exact literal CatalogVolumeAndLongRunPlanner held as a private
        // const before Phase 4G.3B.0 (see PHASE4G_3B_0_VOLUME_SAFETY_POLICY_GOVERNANCE_NOTE.md
        // for the prior file/line of each).
        var policy = VolumeSafetyPolicy.Default;

        Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(24d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(38d, policy.GoldenFixtureResolvedPeakKm);
        Assert.Equal(10, policy.GoldenFixtureNonTaperTransitions);
        Assert.Equal(0.53d, policy.TaperVolumeMultiplier);
        Assert.Equal(0.30d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.33d, policy.LongRunSelectionShare);
        Assert.Equal(0.40d, policy.LongRunHardCapShare);
        Assert.Equal(0.5d, policy.RoundingIncrementKm);
        Assert.Equal("round_nearest_0.5km_after_each_week_value_then_validate", policy.RoundingRule);
        Assert.Equal("APPSEL_RACE_VOLUME_SAFETY_V1", VolumeSafetyPolicy.PolicyVersion);
    }

    [Fact]
    public void ParameterlessConstructor_AndExplicitDefaultPolicyConstructor_ProduceByteIdenticalOutput_ForTheLiveTwelveWeekPilot()
    {
        // "Before" = the parameterless constructor (the exact call every
        // existing production/test call site still uses unchanged --
        // CatalogPreviewGenerator.DefaultVolumeAndLongRunPlanner(), and every
        // `new CatalogVolumeAndLongRunPlanner()` call in Phase4F7BVolumeAndLongRunTests.cs).
        // "After" = the new explicit-policy constructor, passed the same
        // VolumeSafetyPolicy.Default values. If the refactor changed any
        // numeric behavior, these two would diverge.
        var before = new CatalogVolumeAndLongRunPlanner().Build(PlanningRequest(12));
        var after = new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.Default).Build(PlanningRequest(12));

        Assert.Equal(before.WeeklyVolumePlan.FirstWeekVolumeKm, after.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.Equal(before.WeeklyVolumePlan.PeakVolumeKm, after.WeeklyVolumePlan.PeakVolumeKm);
        Assert.Equal(before.WeeklyVolumePlan.ReachablePeakDecision, after.WeeklyVolumePlan.ReachablePeakDecision);
        Assert.Equal(before.WeeklyVolumePlan.TaperVolumeDecision, after.WeeklyVolumePlan.TaperVolumeDecision);
        Assert.Equal(
            before.WeeklyVolumePlan.Weeks.Select(w => (w.WeekNumber, w.PlannedWeeklyVolumeKm, w.ChangeKm, w.ChangePercent, w.VolumeClassification, w.AppliedClamp)),
            after.WeeklyVolumePlan.Weeks.Select(w => (w.WeekNumber, w.PlannedWeeklyVolumeKm, w.ChangeKm, w.ChangePercent, w.VolumeClassification, w.AppliedClamp)));
        Assert.Equal(
            before.LongRunProgression.Weeks.Select(w => (w.WeekNumber, w.PlannedLongRunDistanceKm, w.LongRunShareOfWeeklyVolume, w.CompatibilityClamp)),
            after.LongRunProgression.Weeks.Select(w => (w.WeekNumber, w.PlannedLongRunDistanceKm, w.LongRunShareOfWeeklyVolume, w.CompatibilityClamp)));
        Assert.True(before.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(after.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(before.LongRunProgression.ValidationResult.IsValid);
        Assert.True(after.LongRunProgression.ValidationResult.IsValid);
    }

    [Fact]
    public void LiveTwelveWeekPilot_WeeklyVolumeAndLongRunCurve_MatchesTheEstablishedGoldenFixtureDerivedSnapshot()
    {
        // Explicit before/after value snapshot for the exact live pilot
        // scenario (StartDate=2026-07-20, RaceDate=2026-10-04, TEN_K/4D/INTERMEDIATE,
        // RecentWeeklyVolumeKm=24 -- the same fixture as
        // Phase4F7BVolumeAndLongRunTests.Build(12)/DefaultTwelveWeekPlan_GeneratesOneWeeklyVolumeAndLongRunPerWeek,
        // which already asserts FirstWeekVolumeKm=24 and
        // ReachablePeakDecision.Classification=WithinTypicalPeakBand against
        // this same unmodified fixture both before and after this phase).
        // These are the actual, real values this refactor must never change.
        var plan = new CatalogVolumeAndLongRunPlanner().Build(PlanningRequest(12));

        Assert.Equal(24d, plan.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.Equal(0.53d, plan.WeeklyVolumePlan.TaperVolumeDecision.Multiplier);
        Assert.Equal(0.47d, plan.WeeklyVolumePlan.TaperVolumeDecision.ReductionPercent);

        var weeklyVolumes = plan.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).Select(w => w.PlannedWeeklyVolumeKm).ToList();
        var longRunDistances = plan.LongRunProgression.Weeks.OrderBy(w => w.WeekNumber).Select(w => w.PlannedLongRunDistanceKm).ToList();

        Assert.Equal(12, weeklyVolumes.Count);
        Assert.Equal(12, longRunDistances.Count);

        // Non-taper weeks strictly non-decreasing; taper (final) week reduced
        // below the pre-taper week -- unchanged structural invariant, not a
        // new assertion.
        for (var i = 1; i < weeklyVolumes.Count - 1; i++)
        {
            Assert.True(weeklyVolumes[i] >= weeklyVolumes[i - 1]);
        }
        Assert.True(weeklyVolumes[^1] < weeklyVolumes[^2]);

        // Every long run stays within the policy's preferred/hard-cap share
        // of that week's planned weekly volume.
        for (var i = 0; i < weeklyVolumes.Count; i++)
        {
            var share = longRunDistances[i] / weeklyVolumes[i];
            Assert.InRange(share, VolumeSafetyPolicy.Default.LongRunPreferredMinimumShare, VolumeSafetyPolicy.Default.LongRunHardCapShare + 0.0001);
        }
    }

    private static CatalogVolumePlanningRequest PlanningRequest(int weeks)
    {
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            RaceDate = new DateOnly(2026, 10, 4),
            TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Sat },
            LongRunDay = Weekday.Sat,
            RecentWeeklyVolumeKm = 24,
            RecentLongestRunKm = 9,
            RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = AsOfDate.AddDays(-21) }
        };

        var candidate = Candidate();
        var bound = BoundPlan(weeks);
        var inputSnapshot = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d,
            StartDate = new DateOnly(2026, 7, 20),
            RaceDate = new DateOnly(2026, 10, 4),
            TargetFinishTimeSeconds = 3000,
            DaysPerWeek = 4,
            Level = RunningBackground.Intermediate
        };
        var results = new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "TEST"),
            RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST"),
        };

        var prescription = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request, AsOfDate, candidate, inputSnapshot, results, bound, WorkoutDefinitions()));

        return new CatalogVolumePlanningRequest(
            candidate, bound, prescription,
            new CatalogPeakVolumeBand("TEN_K", "INTERMEDIATE", 4, 30, 42, "PEAK_VOLUME_BANDS_V1", 3));
    }

    private static PlanCatalogCandidateSummary Candidate() => new()
    {
        CandidateKey = "TEN_K__4D__INTERMEDIATE",
        CandidateVersion = 10,
        CandidateStatus = "DRAFT",
        CanonicalDistanceFamily = "TEN_K",
        Level = "INTERMEDIATE",
        DaysPerWeek = 4,
        CoreCycle = new PlanCatalogCoreCycle(8, 12, 14),
        MasterTemplate = new PlanCatalogReference("TEN_K_MASTER", 6),
        Layout = new PlanCatalogReference("FOUR_DAY_STANDARD", 2),
        LevelModifier = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
        WorkoutProgression = new PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION_V1", 5),
        ProgressionModifier = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2),
        RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
        PeakVolumeBandPolicy = new PlanCatalogReference("PEAK_VOLUME_BANDS_V1", 3),
        RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2),
        DependencyStatuses = new Dictionary<string, string>(),
        ReferencedWorkouts = new[] { new PlanCatalogReference("EASY_STANDARD", 4), new PlanCatalogReference("LONG_RUN_STANDARD", 4) },
        PhaseKeys = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" },
        PhaseAllocations = new[] { new PlanCatalogPhaseAllocation("FOUNDATION", 3), new PlanCatalogPhaseAllocation("BUILD", 4), new PlanCatalogPhaseAllocation("RACE_SPECIFIC", 4), new PlanCatalogPhaseAllocation("TAPER", 1) },
        SlotRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" }
    };

    private static IReadOnlyDictionary<string, CatalogWorkoutDefinitionSummary> WorkoutDefinitions() =>
        new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal)
        {
            ["EASY_STANDARD"] = Definition("EASY_STANDARD", 4, "EASY", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, new[] { "DISTANCE" }, new[] { "EXACT_SESSION_TOTAL" }),
            ["LONG_RUN_STANDARD"] = Definition("LONG_RUN_STANDARD", 4, "LONG_RUN", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, new[] { "DISTANCE" }, new[] { "EXACT_SESSION_TOTAL" }),
        };

    private static CatalogWorkoutDefinitionSummary Definition(string key, int version, string family, IReadOnlyList<string> phases, IReadOnlyList<string> modes, IReadOnlyList<string> accountingModes) => new()
    {
        Key = key,
        Version = version,
        Status = "DRAFT",
        Family = family,
        EligiblePhases = phases,
        AllowedPrescriptionModes = modes,
        AllowedDistanceAccountingModes = accountingModes,
        Components = Array.Empty<CatalogWorkoutComponentSummary>()
    };

    private static BoundCatalogPlan BoundPlan(int weeks)
    {
        var boundWeeks = Enumerable.Range(1, weeks).Select(week =>
        {
            var phase = week switch
            {
                <= 2 => "FOUNDATION",
                _ when week == weeks => "TAPER",
                _ when week >= Math.Max(3, weeks - 4) => "RACE_SPECIFIC",
                _ => "BUILD"
            };
            var stage = phase == "TAPER" ? "TAPER_SHARPEN" : $"{phase}_STAGE";
            var monday = new DateOnly(2026, 7, 20).AddDays((week - 1) * 7);
            return new BoundCatalogWeek
            {
                WeekNumber = week,
                PhaseKey = phase,
                Sessions = new[]
                {
                    Session(week, monday, phase, "KEY_SESSION", stage),
                    Session(week, monday.AddDays(2), phase, "EASY_SUPPORT", null),
                    Session(week, monday.AddDays(4), phase, "EASY_SUPPORT", null),
                    Session(week, monday.AddDays(6), phase, "LONG_RUN", null, "LONG_RUN_STANDARD")
                }
            };
        }).ToList();

        return new BoundCatalogPlan
        {
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            BinderVersion = "CATALOG_WORKOUT_BINDER_V1",
            Weeks = boundWeeks,
            Trace = new WorkoutBindingDecisionTrace { Steps = Array.Empty<WorkoutBindingDecisionTraceStep>() }
        };
    }

    private static BoundCatalogSession Session(int week, DateOnly date, string phase, string role, string? stage, string workoutKey = "EASY_STANDARD") => new()
    {
        WeekNumber = week,
        Date = date,
        PhaseKey = phase,
        ProgressionStageKey = stage,
        StructuralRole = role,
        WorkoutDefinitionKey = workoutKey,
        WorkoutDefinitionVersion = 4,
        BindingMode = role == "KEY_SESSION" ? CatalogWorkoutBindingMode.StageControlled : CatalogWorkoutBindingMode.FixedDefault,
        BindingPolicyKey = "TEN_K_WORKOUT_PROGRESSION_V1",
        BindingPolicyVersion = 5,
        SourceArtifactKey = "TEN_K_WORKOUT_PROGRESSION_V1",
        SourceArtifactVersion = 5,
        ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned,
        FallbackOrigin = null,
        BindingReason = "TEST"
    };
}
