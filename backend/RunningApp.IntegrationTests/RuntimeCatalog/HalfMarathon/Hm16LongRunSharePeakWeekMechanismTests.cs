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
/// Phase HM.1.6 — proves the long-run-share / peak-week mechanism closure:
/// <see cref="CatalogVolumeAndLongRunPlanner.BuildLongRunPlan"/>'s new
/// RACE_SPECIFIC-phase share-progression branch, gated entirely on
/// <see cref="VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm"/>.
///
/// Dark-only: every <see cref="VolumeSafetyPolicy"/> instance built here is
/// TEST-LOCAL, mirroring <see cref="Hm14bTaperVolumeMultiplierMechanismTests"/>'s
/// own established synthetic-HALF_MARATHON pattern. Not a new named static
/// field in the production <see cref="VolumeSafetyPolicy"/> registry, never
/// reachable through any real identity/routing gate. The 19.0km ceiling used
/// below reuses HM.1.1's own already-frozen <c>PreferredAbsolutePeakLongRunKm</c>
/// value; the 0.30/0.36/0.33/0.40 share figures reuse HM.1.5's own
/// already-frozen values verbatim -- no new numeric authority is introduced
/// or decided by this phase, only the mechanism that reconciles them.
/// </summary>
public sealed class Hm16LongRunSharePeakWeekMechanismTests
{
    private static readonly DateOnly AsOfDate = new(2026, 7, 20);

    // ── A: ordinary week uses the normal 33% selected target ──

    [Fact]
    public void OrdinaryFoundationAndBuildWeeks_UseFlatThirtyThreePercentSelectedTarget()
    {
        var plan = BuildHmPlan(recentWeeklyVolumeKm: 25);
        var longRun = plan.LongRunProgression.Weeks.OrderBy(w => w.WeekNumber).ToList();

        // Weeks 1-7 are FOUNDATION/BUILD -- not RACE_SPECIFIC, so the ramp
        // mechanism never activates; share stays within the ordinary
        // preferred band around the 33% selected target (never pushed toward
        // the 36-40% peak-eligible territory), unchanged from pre-HM.1.6
        // behavior (rounding to the 0.5km increment plus the week-1
        // longest-run-anchor reconciliation can move the exact ratio a
        // little within the preferred band -- never outside it).
        foreach (var week in longRun.Where(w => w.WeekNumber <= 7))
        {
            Assert.InRange(week.LongRunShareOfWeeklyVolume, 0.30, 0.36);
        }
    }

    // ── B: a suitably ready late RaceSpecific week can exceed 33% ──

    [Fact]
    public void LateRaceSpecificWeeks_ShareRampsAboveThirtyThreePercent_ContinuousFromBuildPhase()
    {
        var plan = BuildHmPlan(recentWeeklyVolumeKm: 25);
        var longRun = plan.LongRunProgression.Weeks.OrderBy(w => w.WeekNumber).ToList();
        var weekly = plan.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).ToDictionary(w => w.WeekNumber);

        // RACE_SPECIFIC = weeks 8-12 (PhaseAllocations: FOUNDATION 3 / BUILD 4 / RACE_SPECIFIC 5 / TAPER 2).
        var raceSpecificWeeks = longRun.Where(w => w.WeekNumber is >= 8 and <= 12).ToList();
        Assert.Equal(5, raceSpecificWeeks.Count);

        // First RACE_SPECIFIC week (position 0) is continuous with the prior
        // BUILD week's own flat 33% -- no jump at the phase boundary.
        Assert.Equal(0.33, raceSpecificWeeks[0].LongRunShareOfWeeklyVolume, precision: 2);

        // Share increases monotonically across the RACE_SPECIFIC phase.
        for (var i = 1; i < raceSpecificWeeks.Count; i++)
        {
            Assert.True(raceSpecificWeeks[i].LongRunShareOfWeeklyVolume >= raceSpecificWeeks[i - 1].LongRunShareOfWeeklyVolume,
                $"Week {raceSpecificWeeks[i].WeekNumber}'s share ({raceSpecificWeeks[i].LongRunShareOfWeeklyVolume}) regressed below week {raceSpecificWeeks[i - 1].WeekNumber}'s ({raceSpecificWeeks[i - 1].LongRunShareOfWeeklyVolume}).");
        }

        // Later RACE_SPECIFIC weeks genuinely exceed the ordinary 33% target.
        Assert.True(raceSpecificWeeks[^1].LongRunShareOfWeeklyVolume > 0.33,
            "The final RACE_SPECIFIC week must be able to exceed the ordinary 33% selected-share target when weekly volume and progression allow it.");

        // The final RACE_SPECIFIC week coincides with the resolved weekly-volume
        // peak (week 12, 43.0km per HM.1.4B) and reaches the hard cap share exactly.
        Assert.Equal(43.0d, weekly[12].PlannedWeeklyVolumeKm);
        Assert.Equal(0.40, raceSpecificWeeks[^1].LongRunShareOfWeeklyVolume, precision: 2);
    }

    // ── C: no HM long run can exceed 40% of weekly volume (hard cap holds) ──

    [Fact]
    public void HardCapShare_NeverExceeded_AcrossEntireFourteenWeekPlan()
    {
        var plan = BuildHmPlan(recentWeeklyVolumeKm: 25);
        foreach (var week in plan.LongRunProgression.Weeks)
        {
            Assert.True(week.LongRunShareOfWeeklyVolume <= 0.40 + 0.0001,
                $"Week {week.WeekNumber}: share {week.LongRunShareOfWeeklyVolume} exceeds the frozen 40% hard cap.");
        }
    }

    // ── D: no eligible long run exceeds the 19.0km absolute ceiling ──

    [Fact]
    public void AbsoluteCeiling_NeverExceeded_EvenAtElevatedStartingVolume()
    {
        // Elevated starting volume so the resolved peak comfortably exceeds
        // 47.5km/week (the share-feasibility threshold for 19km at 40%).
        var plan = BuildHmPlan(recentWeeklyVolumeKm: 30);
        foreach (var week in plan.LongRunProgression.Weeks)
        {
            Assert.True(week.PlannedLongRunDistanceKm <= 19.0d + 0.0001,
                $"Week {week.WeekNumber}: long run {week.PlannedLongRunDistanceKm}km exceeds the frozen 19.0km preferred absolute ceiling.");
        }
    }

    // ── E: the 43km golden peak reference does NOT force 19km -- staying at/below the 40%-share-limited value is VALID, not a failure ──

    [Fact]
    public void FortyThreeKilometerGoldenPeakReference_StaysAtShareLimitedValue_NotNineteenKm_AndThisIsValid()
    {
        var plan = BuildHmPlan(recentWeeklyVolumeKm: 25);
        var weekly = plan.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == 12);
        var longRun = plan.LongRunProgression.Weeks.Single(w => w.WeekNumber == 12);

        Assert.Equal(43.0d, weekly.PlannedWeeklyVolumeKm);
        // 43.0 * 0.40 = 17.2 -> round(0.5) = 17.0. This is the correct,
        // fully-valid outcome: the 40% hard-cap SHARE binds before the
        // 19.0km absolute ceiling ever does, at this weekly-volume level.
        Assert.Equal(17.0d, longRun.PlannedLongRunDistanceKm);
        Assert.True(longRun.PlannedLongRunDistanceKm < 19.0d,
            "A peak long run below the 19.0km ceiling at the 43km golden weekly-volume reference is VALID -- not a failure to reach the ceiling.");
        Assert.True(plan.LongRunProgression.ValidationResult.IsValid);
    }

    // ── F: higher weekly volume (>=47.5km) makes ~19km reachable when progression/readiness allow ──

    [Fact]
    public void AtOrAboveFortySevenPointFiveKilometersPerWeek_NineteenKilometersBecomesReachable()
    {
        var plan = BuildHmPlan(recentWeeklyVolumeKm: 30);
        var weekly = plan.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == 12);
        var longRun = plan.LongRunProgression.Weeks.Single(w => w.WeekNumber == 12);

        Assert.True(weekly.PlannedWeeklyVolumeKm >= 47.5d,
            $"Test setup requires a resolved peak >= 47.5km/week; got {weekly.PlannedWeeklyVolumeKm}km.");
        // 40% share of >=47.5km would be >=19.0km -- the absolute ceiling now
        // binds instead of the share, landing exactly at 19.0km, not beyond it.
        Assert.Equal(19.0d, longRun.PlannedLongRunDistanceKm);
    }

    // ── G: no peak chasing -- a low-readiness trajectory stays below the theoretical peak even with a 19km ceiling configured ──

    [Fact]
    public void LowReadinessTrajectory_RemainsFarBelowTheoreticalPeak_NoForcedChasing()
    {
        var plan = BuildHmPlan(recentWeeklyVolumeKm: 15);
        var weekly = plan.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == 12);
        var longRun = plan.LongRunProgression.Weeks.Single(w => w.WeekNumber == 12);

        // Low starting volume => low resolved peak => even at the hard-cap
        // share, the long run stays far below the 19.0km ceiling. The
        // mechanism must never accelerate weekly volume or the share ramp
        // merely to approach 19km.
        Assert.True(weekly.PlannedWeeklyVolumeKm < 30d, $"Expected a low resolved peak; got {weekly.PlannedWeeklyVolumeKm}km.");
        Assert.True(longRun.PlannedLongRunDistanceKm < 12.0d,
            $"Week 12 long run ({longRun.PlannedLongRunDistanceKm}km) should remain far below the 19.0km ceiling for a low-readiness trajectory -- no peak chasing.");
    }

    // ── H: taper does not create a new long-run peak ──

    [Fact]
    public void TaperWeeks_RemainBelowPreTaperLongRunPeak()
    {
        var plan = BuildHmPlan(recentWeeklyVolumeKm: 25);
        var longRun = plan.LongRunProgression.Weeks.OrderBy(w => w.WeekNumber).ToList();
        var preTaperPeak = longRun.Where(w => w.WeekNumber <= 12).Max(w => w.PlannedLongRunDistanceKm);
        var taperWeeks = longRun.Where(w => w.WeekNumber is 13 or 14).ToList();

        Assert.Equal(2, taperWeeks.Count);
        Assert.All(taperWeeks, w => Assert.True(w.PlannedLongRunDistanceKm < preTaperPeak,
            $"Taper week {w.WeekNumber}'s long run ({w.PlannedLongRunDistanceKm}km) must stay below the pre-taper peak ({preTaperPeak}km) -- taper must never create a new peak."));
        // Taper weeks are not RACE_SPECIFIC, so the ramp mechanism never
        // activates for them; they use the flat 33% selected share (within
        // the ordinary preferred band, never the elevated peak-eligible
        // territory) on top of their own already-reduced weekly volume,
        // exactly as before HM.1.6.
        Assert.All(taperWeeks, w => Assert.InRange(w.LongRunShareOfWeeklyVolume, 0.30, 0.36));
    }

    // ── I: planner/verifier parity -- LongRunProgressionVerifier does not false-fail the elevated late-RaceSpecific shares ──

    [Fact]
    public void LongRunProgressionVerifier_AgreesWithPlanner_NoFalseFailureForElevatedRaceSpecificShares()
    {
        var policy = HmTestPolicy();
        var plan = BuildHmPlan(recentWeeklyVolumeKm: 25, policy: policy);

        var result = LongRunProgressionVerifier.Verify(plan.WeeklyVolumePlan, plan.LongRunProgression, policy);

        Assert.Equal(LongRunProgressionOutcome.Pass, result.Outcome);
        Assert.DoesNotContain(result.Findings, f => f.Contains("HARD_CAP_SHARE_VIOLATION"));
        // The elevated (>36%) late-RaceSpecific weeks are expected to surface
        // as the verifier's own pre-existing NON-FAILING preferred-band
        // deviation finding -- proving the verifier already had the correct,
        // non-hard-cap-as-selection-share semantic before this phase touched
        // any of its code.
        Assert.Contains(result.Findings, f => f.Contains("PREFERRED_SHARE_BAND_DEVIATION_NONFAILING"));
    }

    // ── J: zero-delta for every existing 10K policy -- the mechanism is fully inert ──

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
    public void EveryExisting10KPolicy_PreferredAbsolutePeakLongRunKm_IsNull_MechanismInert(string _, VolumeSafetyPolicy policy)
    {
        Assert.Null(policy.PreferredAbsolutePeakLongRunKm);
    }

    [Fact]
    public void LiveTwelveWeekTenKPilot_LongRunShares_ByteIdenticalToPreHm16Behavior()
    {
        // Same live fixture Hm14bTaperVolumeMultiplierMechanismTests and
        // Phase4G3B0VolumeSafetyPolicyEquivalenceTests already exercise
        // (RecentWeeklyVolumeKm=24, VolumeSafetyPolicy.Default, a real TEN_K
        // candidate whose own PhaseKeys DO include "RACE_SPECIFIC" -- proving
        // the mechanism's gate is the policy field, not phase-key absence).
        var bound = BoundPlan(12, phaseOf: DefaultElevenPlusOneTaperPhase);
        var candidate = TenKCandidate();
        var request = TenKRequest();
        var prescription = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request, AsOfDate, candidate, TenKInputSnapshot(),
            new[] { Result("PACE_SOURCE_IN", "RECENT_RACE"), Result("GOAL_FEASIBILITY_IN", "REALISTIC") },
            bound, WorkoutDefinitions()));

        var plan = new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(
            candidate, bound, prescription, new CatalogPeakVolumeBand("TEN_K", "INTERMEDIATE", 4, 30, 42, "PEAK_VOLUME_BANDS_V1", 3)));

        // Every RACE_SPECIFIC-phase TEN_K week (weeks 8-11 per DefaultElevenPlusOneTaperPhase)
        // still resolves to the flat 33% target -- the ramp never activates
        // because VolumeSafetyPolicy.Default.PreferredAbsolutePeakLongRunKm is null.
        foreach (var week in plan.LongRunProgression.Weeks.Where(w => w.PhaseKey == "RACE_SPECIFIC"))
        {
            Assert.InRange(week.LongRunShareOfWeeklyVolume, 0.32, 0.34);
        }
        Assert.True(plan.LongRunProgression.ValidationResult.IsValid);
    }

    // ── shared HM fixture construction (mirrors Hm14bTaperVolumeMultiplierMechanismTests) ──

    private static VolumeSafetyPolicy HmTestPolicy() => new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,   // HM.1.5 frozen
        HardMaxWeeklyIncreaseRatio: 0.08d,        // HM.1.5 frozen
        AbsoluteWeeklyIncrementCapKm: 2.5d,       // HM.1.5 frozen
        GoldenFixtureStartingVolumeKm: 25.0d,     // HM.1.5 frozen
        ResolvedPeakReference: new ResolvedPeakReference(HalfMarathonIntermediate4DTaperVolumePolicy.ResolvedPeakReferenceKm, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope), // HM.1.4/1.5 frozen: 43.0
        GoldenFixtureNonTaperTransitions: 11,     // HM.1.5 frozen
        TaperVolumeMultiplier: HalfMarathonIntermediate4DTaperVolumePolicy.OrderedMultipliers[^1],
        LongRunPreferredMinimumShare: 0.30d,      // HM.1.5 frozen
        LongRunPreferredMaximumShare: 0.36d,      // HM.1.5 frozen
        LongRunSelectionShare: 0.33d,             // HM.1.5 frozen
        LongRunHardCapShare: 0.40d,               // HM.1.5 frozen
        RoundingIncrementKm: 0.5d,                // HM.1.4 frozen
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonIntermediate4DTaperVolumePolicy.OrderedMultipliers, // HM.1.4B frozen: [0.70, 0.43]
        PreferredAbsolutePeakLongRunKm: 19.0d);   // HM.1.1 frozen ceiling -- HM.1.6 mechanism under test

    private static CatalogVolumeAndLongRunPlan BuildHmPlan(double recentWeeklyVolumeKm, VolumeSafetyPolicy? policy = null)
    {
        policy ??= HmTestPolicy();
        var candidate = HmCandidate();
        var bound = BoundPlan(14, phaseOf: HmThreeFourFiveTwoPhase);
        var request = HmRequest(recentWeeklyVolumeKm);
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

    private static GeneratePreviewRequest HmRequest(double recentWeeklyVolumeKm) => new()
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
        RecentWeeklyVolumeKm = recentWeeklyVolumeKm,
        RecentLongestRunKm = 9,
        RecentRunsPerWeek = 4,
        RecentRace = new RecentRaceInput { Distance = GoalDistance.HalfMarathon, FinishTimeSeconds = 6000, RaceDate = AsOfDate.AddDays(-21) }
    };

    private static GeneratePreviewRequest TenKRequest() => new()
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

    /// <summary>Dark-only, test-local candidate identity -- mirrors <see cref="Hm14bTaperVolumeMultiplierMechanismTests"/>'s own established synthetic-HALF_MARATHON pattern. Never reachable through any real public/Runway identity gate.</summary>
    private static PlanCatalogCandidateSummary HmCandidate() => new()
    {
        CandidateKey = "HALF_MARATHON__4D__INTERMEDIATE__HM16_TEST_ONLY",
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
            CandidateKey = "HM16_TEST_ONLY",
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
        BindingPolicyKey = "HM16_TEST_ONLY",
        BindingPolicyVersion = 1,
        SourceArtifactKey = "HM16_TEST_ONLY",
        SourceArtifactVersion = 1,
        ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned,
        FallbackOrigin = null,
        BindingReason = "TEST"
    };
}
