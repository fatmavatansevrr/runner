using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Volume;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Session;

/// <summary>
/// Phase 10K-GEN.23 — real, dark, end-to-end representability verification
/// for Beginner×3D Core, implementing GEN.21's frozen Option-1 taper-minimum
/// authority (Phase K decision): KEY=3.0km (TAPER_SHARPEN's existing floor,
/// reused verbatim), EASY=2.5km (new), LONG=3.0km (new) — an 8.5km
/// taper-specific structural minimum, distinct from and lower than the
/// unchanged 12.0km normal-week/Intermediate floor.
///
/// Exercises the REAL, unmodified full pipeline
/// (<see cref="DynamicCoreSessionPrescriptionOrchestrator"/>, chaining the
/// volume/long-run planner, session-distance allocation, and
/// TAPER_SHARPEN finalization) against the new, internally-gated
/// TEN_K__3D__BEGINNER v1 catalog candidate, for every governed Core
/// horizon (8-14 weeks) and every readiness state this engagement tests
/// elsewhere for a new Level/Frequency cell (missing, explicit-zero,
/// positive-observed at the peak-band boundaries). Mirrors the rigor of
/// <see cref="Volume.Gen4DBeginnerFourDayCoreTests"/> (Beginner×4D's own
/// implementation-phase verification) and
/// <see cref="DynamicCoreSessionPrescriptionOrchestratorTests"/>'s own
/// established harness shape.
///
/// Beginner×3D remains internally gated (catalog status VALIDATED, not
/// PUBLISHED; NOT added to <see cref="V1CatalogPilotIdentityPolicy"/>'s
/// public allow-list) — mirroring GEN.4D's own "Core implementation stays
/// INTERNALLY_GATED, public activation is a separate future phase"
/// precedent (GEN.4D → GEN.4E).
/// </summary>
public sealed class Gen23BeginnerThreeDayCoreTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 3); // Monday
    private static readonly DayOfWeek[] ThreeDayPreferredDays = { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    internal static async Task<PlanCatalogCandidateSummary> RealBeginnerThreeDayCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync("TEN_K__3D__BEGINNER", 1);
    }

    private static DynamicCoreSessionPrescriptionOrchestrator RealOrchestrator() => new(
        new DynamicCoreVolumeAndLongRunOrchestrator(
            new DynamicCoreWorkoutBindingOrchestrator(
                new DynamicCoreWeekSkeletonOrchestrator(
                    new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                    new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                new CatalogWorkoutProgressionLoader(Options.Create(RealOptions())),
                new ProgressionStageAllocator(),
                new GeneratedCatalogStageScheduleValidator(),
                new CatalogWeekSkeletonCalendarMaterializer(),
                new DatedGeneratedCatalogPlanSkeletonValidator(),
                new CatalogWorkoutBinder(),
                new BoundCatalogPlanValidator()),
            new CatalogPrescriptionContextBuilder(),
            new CatalogVolumeAndLongRunPlanner()),
        new CatalogSessionPrescriptionPlanner(),
        new CatalogFinalPrescribedPlanFinalizer());

    public enum ReadinessState
    {
        MissingReadiness,
        ExplicitZero,
        PositiveObservedBandLower,   // 16.0km -- GEN.21's own diagnosed binding case
        PositiveObservedBandUpper,   // 20.0km -- comfortable headroom per GEN.21
    }

    private static (double? Volume, double? Longest, int? RunsPerWeek) ProfileFor(ReadinessState state) => state switch
    {
        ReadinessState.MissingReadiness => (null, 6d, 3),
        ReadinessState.ExplicitZero => (0d, 0d, 0),
        ReadinessState.PositiveObservedBandLower => (16.0d, 6.5d, 3),
        ReadinessState.PositiveObservedBandUpper => (20.0d, 8d, 3),
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };

    internal static async Task<DynamicCoreSessionPrescriptionResult> BuildAsync(
        PlanCatalogCandidateSummary candidate, int targetWeekCount, ReadinessState state)
    {
        var options = Options.Create(RealOptions());
        var raceDate = StartDate.AddDays(targetWeekCount * 7);
        var (volume, longest, runsPerWeek) = ProfileFor(state);

        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Beginner, DaysPerWeek = candidate.DaysPerWeek,
            Unit = DistanceUnit.Km, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = volume, RecentLongestRunKm = longest, RecentRunsPerWeek = runsPerWeek,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = StartDate.AddDays(-21) },
        };

        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            DaysPerWeek = candidate.DaysPerWeek, Level = RunningBackground.Beginner,
        };

        var conditionResults = new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "TEST"),
            RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST"),
        };

        var context = new DynamicCoreSessionPrescriptionContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = StartDate, AsOfDate = StartDate,
            PreferredDays = ThreeDayPreferredDays, LongRunDayPreference = LongRunDay,
            ConditionResults = conditionResults, PreviewRequest = previewRequest, ResolverInput = resolverInput,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(options),
        };

        return await RealOrchestrator().PrescribeAsync(context);
    }

    // ── Frozen-authority arithmetic sanity (no orchestration) ──────────────

    [Fact]
    public void FrozenAuthority_ExactValues_MatchGen21DecisionVerbatim()
    {
        Assert.Equal(3d, V1TaperSharpenPrescriptionPolicy.MinSessionDistanceKm);
        Assert.Equal(3d, V1ThreeDaySessionVolumeAllocationPolicy.BeginnerTaperMinimumKeyKm);
        Assert.Equal(2.5d, V1ThreeDaySessionVolumeAllocationPolicy.BeginnerTaperMinimumEasyKm);
        Assert.Equal(3d, V1ThreeDaySessionVolumeAllocationPolicy.BeginnerTaperMinimumLongKm);
        Assert.Equal(8.5d, V1BeginnerThreeDayVolumeEligibilityPolicy.MinimumFullLayoutTaperWeeklyVolumeKm);
        // Normal-week minima explicitly unchanged (12.0km = 4.0+3.0+5.0).
        Assert.Equal(4d, V1ThreeDaySessionVolumeAllocationPolicy.MinimumKeyKm);
        Assert.Equal(3d, V1ThreeDaySessionVolumeAllocationPolicy.MinimumEasyKm);
        Assert.Equal(5d, V1ThreeDaySessionVolumeAllocationPolicy.MinimumLongKm);
        // PeakVolumeBand/taper multiplier untouched.
        Assert.Equal(0.53d, VolumeSafetyPolicy.ThreeDayBeginner.TaperVolumeMultiplier);
    }

    [Fact]
    public void FrozenAuthority_BindingCaseArithmetic_VerifiedExactly()
    {
        // 16.0km x 0.53 = 8.48 -> Round0.5 = 8.5 (exactly at the new floor,
        // zero slack) -- the tightest point of the approved [16,20] band.
        static double RoundHalf(double v) => Math.Round(v / 0.5d, MidpointRounding.AwayFromZero) * 0.5d;
        Assert.Equal(8.5d, RoundHalf(16.0d * 0.53d));
        // 20.0km x 0.53 = 10.6 -> Round0.5 = 10.5 (comfortable headroom).
        Assert.Equal(10.5d, RoundHalf(20.0d * 0.53d));
        Assert.Equal(16.0d, V1BeginnerThreeDayVolumeEligibilityPolicy.TaperBreakEvenPreTaperKm);
    }

    // ── Full end-to-end representability matrix ─────────────────────────────
    //
    // Honest, real-pipeline finding (not assumed, not adjusted to force a
    // pass): MissingReadiness (12.0km start, reused Beginner×4D default) and
    // both PositiveObserved band-boundary profiles (16.0km/20.0km, GEN.21's
    // own binding case and its comfortable-headroom counter-case) are FULLY
    // representable at every governed Core horizon (8-14). ExplicitZero
    // (9.5km start, also reused verbatim from Beginner×4D per GEN.5's own
    // established reuse pattern) was, AT THE TIME THIS PHASE (GEN.23) WAS
    // WRITTEN, not representable at any horizon 8-14 — a genuine, separate
    // gap this phase discovered and disclosed rather than papered over.
    // GEN.24 (a direct user decision on that disclosed gap) has since
    // formally closed it as PRODUCT_INELIGIBLE — see
    // ExplicitZero_AllHorizons_FailsClosed_TypedReadinessIneligibilityException_GEN24
    // below and Gen24BeginnerThreeDayExplicitZeroIneligibilityTests.cs for
    // the current, correct behavior. No starting-volume default was raised
    // or invented to achieve this.

    public static IEnumerable<object[]> RepresentableHorizonsByReadiness()
    {
        foreach (var weeks in new[] { 8, 9, 10, 11, 12, 13, 14 })
        {
            foreach (var state in new[] { ReadinessState.MissingReadiness, ReadinessState.PositiveObservedBandLower, ReadinessState.PositiveObservedBandUpper })
            {
                yield return new object[] { weeks, state };
            }
        }
    }

    [Theory]
    [MemberData(nameof(RepresentableHorizonsByReadiness))]
    public async Task FullRepresentability_EveryGovernedCoreHorizonAndReadinessState(int weeks, ReadinessState state)
    {
        var candidate = await RealBeginnerThreeDayCandidateAsync();
        Assert.Equal("NEW", candidate.Level);
        Assert.Equal(3, candidate.DaysPerWeek);

        var result = await BuildAsync(candidate, weeks, state);
        var final = result.FinalPrescribedPlan;

        Assert.True(final.ValidationResult.IsValid, string.Join("; ", final.ValidationResult.Errors));
        Assert.Equal(weeks, final.Weeks.Count);

        // Structural cardinality: 1 KEY + 1 EASY + 1 LONG every week, RUN_LAYOUT_3D unchanged.
        Assert.All(final.Weeks, w =>
        {
            Assert.Equal(1, w.Sessions.Count(s => s.StructuralRole == "KEY_SESSION"));
            Assert.Equal(1, w.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT"));
            Assert.Equal(1, w.Sessions.Count(s => s.StructuralRole == "LONG_RUN"));
        });

        // Taper week reconciles to the new 8.5km floor or above, and the
        // per-role minima (3.0/2.5/3.0) are respected exactly.
        var weeklyPlan = result.VolumeResult.VolumeAndLongRunPlan.WeeklyVolumePlan;
        var taperWeekNumber = weeklyPlan.Weeks.Single(w => w.IsTaperWeek).WeekNumber;
        var taperVolume = weeklyPlan.Weeks.Single(w => w.IsTaperWeek).PlannedWeeklyVolumeKm;
        Assert.True(taperVolume >= 8.5d, $"weeks={weeks} state={state}: taper volume {taperVolume} below the 8.5km floor.");

        var taperSessions = final.Weeks.Single(w => w.WeekNumber == taperWeekNumber).Sessions;
        var key = taperSessions.Single(s => s.StructuralRole == "KEY_SESSION");
        var easy = taperSessions.Single(s => s.StructuralRole == "EASY_SUPPORT");
        var longRun = taperSessions.Single(s => s.StructuralRole == "LONG_RUN");
        Assert.True(key.PlannedDistanceKm >= 3.0d - 0.001d, $"KEY {key.PlannedDistanceKm}km below 3.0km taper floor.");
        Assert.True(easy.PlannedDistanceKm >= 2.5d - 0.001d, $"EASY {easy.PlannedDistanceKm}km below 2.5km taper floor.");
        Assert.True(longRun.PlannedDistanceKm >= 3.0d - 0.001d, $"LONG {longRun.PlannedDistanceKm}km below 3.0km taper floor.");

        // Exact accounting: the three taper sessions sum to the taper week's planned volume.
        var sum = Math.Round(key.PlannedDistanceKm + easy.PlannedDistanceKm + longRun.PlannedDistanceKm, 4);
        Assert.Equal(Math.Round(taperVolume, 4), sum);
    }

    // ── Identity remains internally gated (no public/allow-list widening) ──

    [Fact]
    public async Task Candidate_RemainsInternallyGated_NotPubliclyRoutable()
    {
        var candidate = await RealBeginnerThreeDayCandidateAsync();
        Assert.Equal("VALIDATED", candidate.CandidateStatus);
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 3));
        // Every previously-widened cell remains unaffected.
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 4));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 3));
    }

    // ── Intermediate x3D zero-delta: byte-identical before/after this phase ─

    [Fact]
    public async Task IntermediateThreeDay_ZeroDelta_TwelveWeekPilotProfile_Unaffected()
    {
        var candidate = await DynamicCoreSessionPrescriptionOrchestratorTests.RealThreeDayCandidateAsync();
        var result = await DynamicCoreSessionPrescriptionOrchestratorTests.BuildAsync(
            candidate, 12, DynamicCoreSessionPrescriptionOrchestratorTests.PaceSourceCategory.RecentRace);

        Assert.True(result.FinalPrescribedPlan.ValidationResult.IsValid);
        var taperWeekNumber = result.VolumeResult.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks.Single(w => w.IsTaperWeek).WeekNumber;
        var taperSessions = result.FinalPrescribedPlan.Weeks.Single(w => w.WeekNumber == taperWeekNumber).Sessions;
        // Intermediate's own gate/minima are untouched: taper week still
        // resolves against the original 12.0km / 4.0/3.0/5.0 floors, not
        // the new Beginner-specific 8.5km / 3.0/2.5/3.0 floors.
        var taperVolume = result.VolumeResult.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks.Single(w => w.IsTaperWeek).PlannedWeeklyVolumeKm;
        Assert.True(taperVolume >= 12.0d - 0.001d);
    }

    // ── SUPERSEDED BY GEN.24 ─────────────────────────────────────────────
    //
    // At the time this phase (GEN.23) was written, ExplicitZero was NOT
    // representable at any Core horizon, for two distinct raw-exception
    // reasons depending on horizon (weeks 8-11 failed the taper-eligibility
    // gate; weeks 12-14 failed the unchanged 12.0km normal-week Week-1
    // floor via a raw, untyped CatalogSessionPrescriptionInfeasibleException).
    // This was disclosed honestly as a genuine, separate, undecided
    // starting-volume-policy gap -- explicitly not "fixed" by this phase.
    //
    // GEN.24 (a direct user decision on that disclosed gap) formally
    // resolved it: explicit-zero readiness for Beginner×3D is now
    // PRODUCT_INELIGIBLE by an explicit, typed, request-level rejection
    // (BeginnerThreeDayExplicitZeroReadinessProductIneligibleException,
    // mirroring GEN.9's Advanced missing/zero mechanism class) thrown at
    // readiness-resolution time, BEFORE either the taper gate or the
    // per-week session floor is ever reached -- uniformly, at all 7
    // horizons, replacing both of the two previously-disclosed raw failure
    // shapes below with one clean, correctly-classified rejection. No
    // starting-volume default was raised or invented to achieve this; see
    // Gen24BeginnerThreeDayExplicitZeroIneligibilityTests.cs for GEN.24's
    // own full verification.

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task ExplicitZero_AllHorizons_FailsClosed_TypedReadinessIneligibilityException_GEN24(int weeks)
    {
        var candidate = await RealBeginnerThreeDayCandidateAsync();
        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(
            () => BuildAsync(candidate, weeks, ReadinessState.ExplicitZero));
        var ineligible = Assert.IsType<BeginnerThreeDayExplicitZeroReadinessProductIneligibleException>(wrapper.InnerException);
        Assert.IsAssignableFrom<CatalogProductIneligibleException>(ineligible);
        Assert.Equal(BeginnerThreeDayExplicitZeroReadinessProductIneligibleException.Reason, ineligible.Code);
    }

    // ── Ineligibility, for readiness states that ARE representable, is never
    //    silently misrouted to Intermediate's own typed exception ──

    [Fact]
    public async Task IneligibilityIfAny_ForRepresentableStates_IsTypedBeginnerThreeDayException_NotIntermediates()
    {
        var candidate = await RealBeginnerThreeDayCandidateAsync();
        foreach (var weeks in new[] { 8, 9, 10, 11, 12, 13, 14 })
        {
            foreach (var state in new[] { ReadinessState.MissingReadiness, ReadinessState.PositiveObservedBandLower, ReadinessState.PositiveObservedBandUpper })
            {
                try
                {
                    await BuildAsync(candidate, weeks, state);
                }
                catch (DynamicCoreVolumeAndLongRunFailedException wrapper)
                {
                    var ineligible = Assert.IsType<BeginnerThreeDayCoreProductIneligibleException>(wrapper.InnerException);
                    Assert.IsAssignableFrom<CatalogProductIneligibleException>(ineligible);
                    Assert.Equal(BeginnerThreeDayCoreProductIneligibleException.Reason, ineligible.Code);
                }
            }
        }
    }
}
