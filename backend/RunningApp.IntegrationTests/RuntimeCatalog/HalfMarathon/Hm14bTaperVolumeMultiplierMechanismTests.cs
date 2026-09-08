using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.HalfMarathon;

/// <summary>
/// Phase HM.1.4B — proves the fixed, non-chained multi-week taper mechanism
/// (<see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/>'s new taper
/// branch, <see cref="CatalogVolumeAndLongRunPlanner.ResolveTaperDecision"/>'s
/// generalized validation, and <see cref="VolumeProgressionVerifier"/>'s
/// matching fix) against the real code, for the exact
/// <c>HALF_MARATHON × INTERMEDIATE × 4D × 14W</c> cell <c>HM.1.4A</c> found
/// would otherwise compound 43 → 22.8 → 12.1km (≈72% cumulative reduction).
///
/// Dark-only: the custom <see cref="VolumeSafetyPolicy"/> instances built
/// here are TEST-LOCAL, not new named static fields in the production
/// <see cref="VolumeSafetyPolicy"/> registry, and are never reachable from
/// any real identity/routing gate (mirroring
/// <see cref="Hm2Step1cFailClosedDistanceBlindFallbackTests"/>'s and
/// <c>Phase4F7BVolumeAndLongRunTests.Hm2Step1c_...</c>'s own established
/// synthetic-HALF_MARATHON-request pattern). Only the two already-frozen
/// taper multipliers (<see cref="HalfMarathonIntermediate4DTaperVolumePolicy"/>)
/// and the other already-frozen fields for this cell (HM.1.4/HM.1.5:
/// ResolvedPeakReferenceKm=43.0, GoldenFixtureStartingVolumeKm=25.0,
/// GoldenFixtureNonTaperTransitions=11, growth rates 0.07/0.08/2.5) are used
/// as real authority; the four long-run-share fields populated below are
/// explicit TEST-ONLY plumbing (reused from <see cref="VolumeSafetyPolicy.Default"/>
/// purely so the full <c>Build()</c> pipeline can execute end-to-end) and are
/// NOT asserted anywhere as HM authority — those four fields remain
/// genuinely unfrozen for HM per <c>HM.1.5 §3</c>.
/// </summary>
public sealed class Hm14bTaperVolumeMultiplierMechanismTests
{
    private static readonly DateOnly AsOfDate = new(2026, 7, 20);

    // ── A: the required real test — two distinct, non-compounded taper targets ──

    [Fact]
    public void HalfMarathonIntermediate4D14W_TwoWeekTaper_ProducesTwoDistinctNonCompoundedTargets()
    {
        var plan = BuildHmPlan();
        var weeks = plan.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).ToList();

        var preTaperWeek = weeks.Single(w => w.WeekNumber == 12);
        var taperWeek1 = weeks.Single(w => w.WeekNumber == 13);
        var taperWeek2 = weeks.Single(w => w.WeekNumber == 14);

        // Fixed pre-taper reference: HM.1.4/HM.1.5's own already-frozen
        // ResolvedPeakReferenceKm = 43.0, reproduced exactly by the last
        // RaceSpecific week's interpolation (index == denominator).
        Assert.Equal(43.0d, preTaperWeek.PlannedWeeklyVolumeKm);
        Assert.True(preTaperWeek.IsTaperWeek == false);

        // Each taper week computed INDEPENDENTLY against 43.0 -- not chained.
        Assert.Equal(30.0d, taperWeek1.PlannedWeeklyVolumeKm);   // 43.0 * 0.70 = 30.1 -> round(0.5) = 30.0
        Assert.Equal(18.5d, taperWeek2.PlannedWeeklyVolumeKm);   // 43.0 * 0.43 = 18.49 -> round(0.5) = 18.5
        Assert.True(taperWeek1.IsTaperWeek);
        Assert.True(taperWeek2.IsTaperWeek);
        Assert.NotEqual(taperWeek1.PlannedWeeklyVolumeKm, taperWeek2.PlannedWeeklyVolumeKm);

        // NOT the HM.1.4A-confirmed compounded defect (43 -> 22.8/23.0 -> 12.1/12.0, ~72.1%).
        Assert.NotEqual(23.0d, taperWeek1.PlannedWeeklyVolumeKm);
        Assert.NotEqual(12.0d, taperWeek2.PlannedWeeklyVolumeKm);
        var cumulativeReduction = (preTaperWeek.PlannedWeeklyVolumeKm - taperWeek2.PlannedWeeklyVolumeKm) / preTaperWeek.PlannedWeeklyVolumeKm;
        Assert.InRange(cumulativeReduction, 0.55, 0.59); // ~57.0%, within the 41-60% evidence envelope for the deepest cut.
        Assert.True(cumulativeReduction < 0.70);          // nowhere near the confirmed 72.1% compounding defect.

        // Taper week 2's own unclamped computation does not depend on taper week 1's result:
        // recomputing week 1 in isolation would not change week 2 -- proven structurally by
        // BuildWeeklyPlan's own pre-taper-anchor capture (traced directly, not merely asserted here).
        Assert.Equal(0.43d, plan.WeeklyVolumePlan.TaperVolumeDecision.Multiplier); // final/deepest week's own multiplier
        Assert.Equal(0.57d, plan.WeeklyVolumePlan.TaperVolumeDecision.ReductionPercent);
    }

    [Fact]
    public void HalfMarathonIntermediate4D14W_TaperDecisionTrace_RecordsNonChainedMechanismDistinctFromLegacyString()
    {
        var plan = BuildHmPlan();
        var week13Trace = plan.WeeklyVolumePlan.DecisionTrace.Single(t => t.WeekNumber == 13);
        var week14Trace = plan.WeeklyVolumePlan.DecisionTrace.Single(t => t.WeekNumber == 14);

        Assert.Contains("non_chained", week13Trace.ChangeRule);
        Assert.Contains("non_chained", week14Trace.ChangeRule);
        Assert.Contains("week_1_of_2", week13Trace.ChangeRule);
        Assert.Contains("week_2_of_2", week14Trace.ChangeRule);
        Assert.DoesNotContain("0.53", week13Trace.ChangeRule);
        Assert.DoesNotContain("canonical_taper_multiplier_0.53_from_previous_week", week14Trace.ChangeRule);
    }

    // ── B: zero-delta proof for every existing 10K 1-week-taper policy ──

    public static IEnumerable<object[]> AllExisting10KPolicies()
    {
        yield return new object[] { "Default", VolumeSafetyPolicy.Default };
        yield return new object[] { "ThreeDayIntermediate", VolumeSafetyPolicy.ThreeDayIntermediate };
        yield return new object[] { "FiveDayIntermediate", VolumeSafetyPolicy.FiveDayIntermediate };
        yield return new object[] { "SixDayIntermediate", VolumeSafetyPolicy.SixDayIntermediate };
        yield return new object[] { "Advanced3D", VolumeSafetyPolicy.Advanced3D };
        yield return new object[] { "Advanced4D", VolumeSafetyPolicy.Advanced4D };
        yield return new object[] { "Advanced5D", VolumeSafetyPolicy.Advanced5D };
        yield return new object[] { "Advanced6D", VolumeSafetyPolicy.Advanced6D };
        yield return new object[] { "Beginner2D", VolumeSafetyPolicy.Beginner2D };
        yield return new object[] { "Intermediate2D", VolumeSafetyPolicy.Intermediate2D };
        yield return new object[] { "ThreeDayBeginner", VolumeSafetyPolicy.ThreeDayBeginner };
        yield return new object[] { "BeginnerFourDay", VolumeSafetyPolicy.BeginnerFourDay };
    }

    [Theory]
    [MemberData(nameof(AllExisting10KPolicies))]
    public void EveryExisting10KPolicy_ResolvedTaperVolumeMultipliers_IsSingleElementListOfItsOwnScalar_ZeroDelta(string _, VolumeSafetyPolicy policy)
    {
        var resolved = policy.ResolvedTaperVolumeMultipliers;

        Assert.Single(resolved);
        Assert.Equal(policy.TaperVolumeMultiplier, resolved[0]);
        Assert.Equal(0.53d, resolved[0]); // every existing 10K instance's own value, unchanged.
    }

    [Fact]
    public void LiveTwelveWeekTenKPilot_TaperWeek_IsExactlyTwentyKm_ByteIdenticalToPreHm14bBehavior()
    {
        // Same live fixture Phase4F7BVolumeAndLongRunTests.Build(12) and
        // Phase4G3B0VolumeSafetyPolicyEquivalenceTests both already exercise
        // (RecentWeeklyVolumeKm=24, VolumeSafetyPolicy.Default): reproduced
        // independently here to assert the exact taper-week numeric value
        // (20.0km, matching HM.1.4A's own cited "38km to 20km" trace) and its
        // exact legacy decision-trace string, both unaffected by this phase.
        var bound = BoundPlan(12, phaseOf: DefaultElevenPlusOneTaperPhase);
        var candidate = TenKCandidate();
        var request = TenKRequest(r => r.RecentWeeklyVolumeKm = 24);
        var prescription = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request, AsOfDate, candidate, TenKInputSnapshot(),
            new[] { Result("PACE_SOURCE_IN", "RECENT_RACE"), Result("GOAL_FEASIBILITY_IN", "REALISTIC") },
            bound, WorkoutDefinitions()));

        var plan = new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(
            candidate, bound, prescription, new CatalogPeakVolumeBand("TEN_K", "INTERMEDIATE", 4, 30, 42, "PEAK_VOLUME_BANDS_V1", 3)));

        var taperWeek = plan.WeeklyVolumePlan.Weeks.Single(w => w.IsTaperWeek);
        Assert.Equal(20.0d, taperWeek.PlannedWeeklyVolumeKm);
        Assert.Equal("canonical_taper_multiplier_0.53_from_previous_week",
            plan.WeeklyVolumePlan.DecisionTrace.Single(t => t.WeekNumber == taperWeek.WeekNumber).ChangeRule);
        Assert.Equal(0.53d, plan.WeeklyVolumePlan.TaperVolumeDecision.Multiplier);
        Assert.Equal(0.47d, plan.WeeklyVolumePlan.TaperVolumeDecision.ReductionPercent);
    }

    // ── C: ResolveTaperDecision's generalized validation ──

    [Fact]
    public void TaperMultiplierCount_MismatchedAgainstActualTaperWeekCount_ThrowsTypedException()
    {
        var bound = BoundPlan(12, phaseOf: DefaultElevenPlusOneTaperPhase); // exactly 1 taper week
        var candidate = TenKCandidate();
        var request = TenKRequest();
        var prescription = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request, AsOfDate, candidate, TenKInputSnapshot(),
            new[] { Result("PACE_SOURCE_IN", "RECENT_RACE"), Result("GOAL_FEASIBILITY_IN", "REALISTIC") },
            bound, WorkoutDefinitions()));

        var mismatchedPolicy = VolumeSafetyPolicy.Default with
        {
            TaperVolumeMultipliers = HalfMarathonIntermediate4DTaperVolumePolicy.OrderedMultipliers // 2 multipliers, but bound has 1 taper week
        };

        var ex = Assert.Throws<CatalogVolumeInvalidTaperRuleException>(() =>
            new CatalogVolumeAndLongRunPlanner(mismatchedPolicy).Build(new CatalogVolumePlanningRequest(
                candidate, bound, prescription, new CatalogPeakVolumeBand("TEN_K", "INTERMEDIATE", 4, 30, 42, "PEAK_VOLUME_BANDS_V1", 3))));

        Assert.Contains("2 taper multiplier", ex.Message);
        Assert.Contains("1 taper week", ex.Message);
    }

    [Fact]
    public void TaperMultipliers_NonMonotonicallyDeepening_ThrowsTypedException()
    {
        // Week 1 reduction (0.70) would exceed the final week's own reduction
        // (0.57) -- an inverted/implausible schedule (deepest cut NOT at
        // race week) -- must fail closed, not silently accepted.
        var invalidMultipliers = new List<double> { 0.30d, 0.43d }; // reductions: 0.70, 0.57 -- inverted
        var ex = Assert.Throws<CatalogVolumeInvalidTaperRuleException>(() => BuildHmPlan(invalidMultipliers));

        Assert.Contains("must be a genuine, positive reduction strictly smaller than the final taper week's reduction", ex.Message);
    }

    [Fact]
    public void TaperMultipliers_FrozenHmPair_PassesValidation_FinalWeekWithinEnvelope_EarlierWeekMonotonicallyShallower()
    {
        // The exact frozen pair does not throw -- proven by BuildHmPlan()
        // itself succeeding in test A above; this test names the two
        // validation properties explicitly for clarity.
        var plan = BuildHmPlan();
        Assert.InRange(plan.WeeklyVolumePlan.TaperVolumeDecision.ReductionPercent, 0.41, 0.60);
    }

    // ── D: VolumeProgressionVerifier no longer blind to the compounding defect ──

    [Fact]
    public void VolumeProgressionVerifier_CorrectlyValidatesGenuineNonChainedTwoWeekTaper_NoFalseNegative()
    {
        var policy = HmTestPolicy();
        var plan = SyntheticPlan(policy,
            Week(12, "RACE_SPECIFIC", 43.0, isTaper: false),
            Week(13, "TAPER", 30.0, isTaper: true),  // 43.0 * 0.70 = 30.1 -> 30.0
            Week(14, "TAPER", 18.5, isTaper: true)); // 43.0 * 0.43 = 18.49 -> 18.5, NOT chained from 30.0

        var result = VolumeProgressionVerifier.Verify(plan, policy);

        Assert.Equal(VolumeProgressionOutcome.Pass, result.Outcome);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void VolumeProgressionVerifier_FlagsTheExactHm14aCompoundingDefect_NoLongerBlind()
    {
        // Reproduces HM.1.4A's own confirmed defect shape: week 2's taper
        // target computed by chaining off week 1's OWN already-reduced
        // output (30.0 * 0.43 = 12.9 -> 13.0) instead of the fixed pre-taper
        // reference (43.0 * 0.43 = 18.49 -> 18.5). HM.1.4A proved the
        // PRE-HM.1.4B verifier would report zero violation for exactly this
        // shape (it used the same chained formula the planner did). The
        // fixed verifier must now catch it.
        var policy = HmTestPolicy();
        var plan = SyntheticPlan(policy,
            Week(12, "RACE_SPECIFIC", 43.0, isTaper: false),
            Week(13, "TAPER", 30.0, isTaper: true),
            Week(14, "TAPER", 13.0, isTaper: true)); // the compounded/chained value, NOT the correct 18.5

        var result = VolumeProgressionVerifier.Verify(plan, policy);

        Assert.Equal(VolumeProgressionOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Contains("TAPER_MULTIPLIER_VIOLATION") && f.Contains("week 13->14"));
    }

    // ── shared HM fixture construction ──

    private static VolumeSafetyPolicy HmTestPolicy(IReadOnlyList<double>? taperMultipliers = null) => new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,   // HM.1.5 frozen
        HardMaxWeeklyIncreaseRatio: 0.08d,        // HM.1.5 frozen
        AbsoluteWeeklyIncrementCapKm: 2.5d,       // HM.1.5 frozen
        GoldenFixtureStartingVolumeKm: 25.0d,     // HM.1.5 frozen
        ResolvedPeakReference: new ResolvedPeakReference(HalfMarathonIntermediate4DTaperVolumePolicy.ResolvedPeakReferenceKm, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope), // HM.1.4/1.5 frozen: 43.0
        GoldenFixtureNonTaperTransitions: 11,     // HM.1.5 frozen
        TaperVolumeMultiplier: (taperMultipliers ?? HalfMarathonIntermediate4DTaperVolumePolicy.OrderedMultipliers)[^1],
        // TEST-ONLY plumbing below -- NOT frozen HM authority (HM.1.5 §3: blocked on a disclosed
        // long-run-share progression-mechanism gap). Reused from VolumeSafetyPolicy.Default purely
        // so the full Build() pipeline (which unconditionally computes a long-run progression) can
        // execute; never asserted as HM authority anywhere in this test file.
        LongRunPreferredMinimumShare: 0.30d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.33d,
        LongRunHardCapShare: 0.40d,
        RoundingIncrementKm: 0.5d,                // retained, HM.1.4
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: taperMultipliers ?? HalfMarathonIntermediate4DTaperVolumePolicy.OrderedMultipliers); // HM.1.4B frozen: [0.70, 0.43]

    private static CatalogVolumeAndLongRunPlan BuildHmPlan(IReadOnlyList<double>? taperMultipliers = null)
    {
        var policy = HmTestPolicy(taperMultipliers);
        var candidate = HmCandidate();
        var bound = BoundPlan(14, phaseOf: HmThreeFourFiveTwoPhase);
        var request = HmRequest();
        var inputSnapshot = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 21.0975d,
            CanonicalDistanceFamily = "HALF_MARATHON",
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.HalfMarathon,
            GoalDistanceKm = 21.0975d,
            StartDate = AsOfDate,
            RaceDate = AsOfDate.AddDays(14 * 7),
            TargetFinishTimeSeconds = 6000,
            DaysPerWeek = 4,
            Level = RunningBackground.Intermediate
        };
        var prescription = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request, AsOfDate, candidate, inputSnapshot,
            new[] { Result("PACE_SOURCE_IN", "RECENT_RACE"), Result("GOAL_FEASIBILITY_IN", "REALISTIC") },
            bound, WorkoutDefinitions()));

        return new CatalogVolumeAndLongRunPlanner(policy).Build(new CatalogVolumePlanningRequest(
            candidate, bound, prescription, new CatalogPeakVolumeBand("HALF_MARATHON", "INTERMEDIATE", 4, 36, 50, "PEAK_VOLUME_BANDS_V1", 1)));
    }

    private static string HmThreeFourFiveTwoPhase(int week) => week switch
    {
        <= 3 => "FOUNDATION",
        <= 7 => "BUILD",
        <= 12 => "RACE_SPECIFIC",
        _ => "TAPER"
    };

    private static string DefaultElevenPlusOneTaperPhase(int week) => week switch
    {
        <= 2 => "FOUNDATION",
        12 => "TAPER",
        _ => week >= 8 ? "RACE_SPECIFIC" : "BUILD"
    };

    private static GeneratePreviewRequest HmRequest() => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.HalfMarathon,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = AsOfDate,
        RaceDate = AsOfDate.AddDays(14 * 7),
        TargetFinishTimeSeconds = 6000,
        PreferredDays = new[] { Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Sat },
        LongRunDay = Weekday.Sat,
        RecentWeeklyVolumeKm = 25, // matches the frozen GoldenFixtureStartingVolumeKm exactly (clean scenario)
        RecentLongestRunKm = 9,
        RecentRunsPerWeek = 4,
        RecentRace = new RecentRaceInput { Distance = GoalDistance.HalfMarathon, FinishTimeSeconds = 6000, RaceDate = AsOfDate.AddDays(-21) }
    };

    private static GeneratePreviewRequest TenKRequest(Action<GeneratePreviewRequest>? configure = null)
    {
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = AsOfDate,
            RaceDate = AsOfDate.AddDays(12 * 7),
            TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Sat },
            LongRunDay = Weekday.Sat,
            RecentWeeklyVolumeKm = 24,
            RecentLongestRunKm = 9,
            RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = AsOfDate.AddDays(-21) }
        };
        configure?.Invoke(request);
        return request;
    }

    private static ResolverInputSnapshot TenKInputSnapshot() => new()
    {
        RequestedTargetDistanceKm = 10d,
        CanonicalDistanceFamily = "TEN_K",
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        GoalDistanceKm = 10d,
        StartDate = AsOfDate,
        RaceDate = AsOfDate.AddDays(12 * 7),
        TargetFinishTimeSeconds = 3000,
        DaysPerWeek = 4,
        Level = RunningBackground.Intermediate
    };

    private static RuntimeConditionResolutionResult Result(string conditionType, string outputValue) =>
        RuntimeConditionResolutionResult.Evaluated(conditionType, outputValue, "TEST");

    private static PlanCatalogCandidateSummary TenKCandidate() => new()
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
        PhaseAllocations = new[] { new PlanCatalogPhaseAllocation("FOUNDATION", 2), new PlanCatalogPhaseAllocation("BUILD", 5), new PlanCatalogPhaseAllocation("RACE_SPECIFIC", 4), new PlanCatalogPhaseAllocation("TAPER", 1) },
        SlotRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" }
    };

    /// <summary>
    /// Dark-only, test-local candidate identity -- mirrors
    /// <c>Phase4F7BVolumeAndLongRunTests.Hm2Step1c_...</c>'s and
    /// <see cref="Hm2Step1cFailClosedDistanceBlindFallbackTests"/>'s own
    /// established synthetic-HALF_MARATHON pattern. Never reachable through
    /// any real public/Runway identity gate.
    /// </summary>
    private static PlanCatalogCandidateSummary HmCandidate() => new()
    {
        CandidateKey = "HALF_MARATHON__4D__INTERMEDIATE__HM14B_TEST_ONLY",
        CandidateVersion = 1,
        CandidateStatus = "DRAFT",
        CanonicalDistanceFamily = "HALF_MARATHON",
        Level = "INTERMEDIATE",
        DaysPerWeek = 4,
        CoreCycle = new PlanCatalogCoreCycle(14, 14, 14),
        MasterTemplate = new PlanCatalogReference("HALF_MARATHON_MASTER_TEST_ONLY", 1),
        Layout = new PlanCatalogReference("FOUR_DAY_STANDARD", 2),
        LevelModifier = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
        WorkoutProgression = new PlanCatalogReference("HALF_MARATHON_WORKOUT_PROGRESSION_TEST_ONLY", 1),
        ProgressionModifier = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2),
        RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
        PeakVolumeBandPolicy = new PlanCatalogReference("PEAK_VOLUME_BANDS_V1", 1),
        RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2),
        DependencyStatuses = new Dictionary<string, string>(),
        ReferencedWorkouts = new[] { new PlanCatalogReference("EASY_STANDARD", 4), new PlanCatalogReference("LONG_RUN_STANDARD", 4) },
        PhaseKeys = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" },
        PhaseAllocations = new[] { new PlanCatalogPhaseAllocation("FOUNDATION", 3), new PlanCatalogPhaseAllocation("BUILD", 4), new PlanCatalogPhaseAllocation("RACE_SPECIFIC", 5), new PlanCatalogPhaseAllocation("TAPER", 2) },
        SlotRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" }
    };

    private static IReadOnlyDictionary<string, CatalogWorkoutDefinitionSummary> WorkoutDefinitions() =>
        new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal)
        {
            ["EASY_STANDARD"] = Definition("EASY_STANDARD", 4, "EASY", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }),
            ["LONG_RUN_STANDARD"] = Definition("LONG_RUN_STANDARD", 4, "LONG_RUN", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }),
        };

    private static CatalogWorkoutDefinitionSummary Definition(string key, int version, string family, IReadOnlyList<string> phases) => new()
    {
        Key = key,
        Version = version,
        Status = "DRAFT",
        Family = family,
        EligiblePhases = phases,
        AllowedPrescriptionModes = new[] { "DISTANCE" },
        AllowedDistanceAccountingModes = new[] { "EXACT_SESSION_TOTAL" },
        Components = Array.Empty<CatalogWorkoutComponentSummary>()
    };

    private static BoundCatalogPlan BoundPlan(int weeks, Func<int, string> phaseOf)
    {
        var boundWeeks = Enumerable.Range(1, weeks).Select(week =>
        {
            var phase = phaseOf(week);
            var stage = phase == "TAPER" ? "TAPER_SHARPEN" : $"{phase}_STAGE";
            var monday = AsOfDate.AddDays((week - 1) * 7);
            return new BoundCatalogWeek
            {
                WeekNumber = week,
                PhaseKey = phase,
                Sessions = new[]
                {
                    Session(week, monday, phase, "KEY_SESSION", stage, "EASY_STANDARD"),
                    Session(week, monday.AddDays(2), phase, "EASY_SUPPORT", null, "EASY_STANDARD"),
                    Session(week, monday.AddDays(4), phase, "EASY_SUPPORT", null, "EASY_STANDARD"),
                    Session(week, monday.AddDays(6), phase, "LONG_RUN", null, "LONG_RUN_STANDARD")
                }
            };
        }).ToList();

        return new BoundCatalogPlan
        {
            CandidateKey = "HM14B_TEST_ONLY",
            CandidateVersion = 1,
            BinderVersion = "CATALOG_WORKOUT_BINDER_V1",
            Weeks = boundWeeks,
            Trace = new WorkoutBindingDecisionTrace { Steps = Array.Empty<WorkoutBindingDecisionTraceStep>() }
        };
    }

    private static BoundCatalogSession Session(int week, DateOnly date, string phase, string role, string? stage, string workoutKey) => new()
    {
        WeekNumber = week,
        Date = date,
        PhaseKey = phase,
        ProgressionStageKey = stage,
        StructuralRole = role,
        WorkoutDefinitionKey = workoutKey,
        WorkoutDefinitionVersion = 4,
        BindingMode = role == "KEY_SESSION" ? CatalogWorkoutBindingMode.StageControlled : CatalogWorkoutBindingMode.FixedDefault,
        BindingPolicyKey = "HM14B_TEST_ONLY",
        BindingPolicyVersion = 1,
        SourceArtifactKey = "HM14B_TEST_ONLY",
        SourceArtifactVersion = 1,
        ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned,
        FallbackOrigin = null,
        BindingReason = "TEST"
    };

    // ── VolumeProgressionVerifier synthetic-plan helpers (mirrors VolumeProgressionVerifierTests' own pattern) ──

    private static CatalogWeeklyVolumePlan SyntheticPlan(VolumeSafetyPolicy policy, params CatalogWeeklyVolumeWeek[] weeks) => new()
    {
        CandidateKey = "SYNTHETIC_HM14B",
        CandidateVersion = 1,
        FirstWeekVolumeKm = weeks[0].PlannedWeeklyVolumeKm,
        PeakVolumeKm = weeks.Where(w => !w.IsTaperWeek).Max(w => w.PlannedWeeklyVolumeKm),
        StartingVolumeDecision = new StartingVolumeDecision(weeks[0].PlannedWeeklyVolumeKm, PrescriptionInputState.Available, weeks[0].PlannedWeeklyVolumeKm, WeeklyVolumeAnchorSource.RecentFourWeekAverage, CatalogVolumeClamp.None, CatalogEvidenceBasis.EvidenceInformed, CatalogDecisionStatus.CanonicalConfirmed, "SYNTHETIC"),
        ReachablePeakDecision = new ReachablePeakDecision(weeks[0].PlannedWeeklyVolumeKm, weeks.Where(w => !w.IsTaperWeek).Max(w => w.PlannedWeeklyVolumeKm), weeks.Where(w => !w.IsTaperWeek).Max(w => w.PlannedWeeklyVolumeKm), PeakBandClassification.WithinTypicalPeakBand, policy.PreferredMaxWeeklyIncreaseRatio, policy.HardMaxWeeklyIncreaseRatio, policy.AbsoluteWeeklyIncrementCapKm, CatalogEvidenceBasis.EvidenceInformed, CatalogDecisionStatus.CanonicalConfirmed, "SYNTHETIC"),
        TaperVolumeDecision = new TaperVolumeDecision(policy.ResolvedTaperVolumeMultipliers[^1], 1d - policy.ResolvedTaperVolumeMultipliers[^1], "SYNTHETIC", CatalogEvidenceBasis.EvidenceInformed, CatalogDecisionStatus.ExplicitProductDefault, "SYNTHETIC"),
        CatalogBounds = new CatalogVolumeBounds(0, 1000, "SYNTHETIC", 1),
        Weeks = weeks,
        DecisionTrace = [],
        ValidationResult = new CatalogVolumeValidationResult(true, []),
    };

    private static CatalogWeeklyVolumeWeek Week(int weekNumber, string phaseKey, double volumeKm, bool isTaper) => new()
    {
        WeekNumber = weekNumber, PhaseKey = phaseKey, PlannedWeeklyVolumeKm = volumeKm, ChangeKm = 0,
        VolumeClassification = isTaper ? "TAPER" : "BUILDING", IsRecoveryOrDeloadWeek = false, IsTaperWeek = isTaper,
        AnchorSource = WeeklyVolumeAnchorSource.RecentFourWeekAverage, CatalogBounds = new CatalogVolumeBounds(0, 1000, "SYNTHETIC", 1),
        AppliedClamp = CatalogVolumeClamp.None, DecisionReason = "SYNTHETIC", SourceArtifactKey = "SYNTHETIC", SourceArtifactVersion = 1, Provenance = "SYNTHETIC",
    };
}
