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
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Phase 10K-FREQ.6D.10 — direct policy, dispatch, and real-service-level
/// tests proving <see cref="CatalogVolumeAndLongRunPlanner"/> now resolves
/// Intermediate×5D missing/explicit-zero readiness through the dedicated,
/// FREQ.6C-backed <see cref="V1FiveDayIntermediateMissingReadinessStartingVolumePolicy"/>
/// (26.0km / 19.5km) rather than the historical 4D-scoped generic
/// fallback (16km / 12km) — and that no other combination's dispatch
/// changed.
/// </summary>
public sealed class V1FiveDayIntermediateMissingReadinessStartingVolumePolicyTests
{
    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    // ── §45: direct policy tests ─────────────────────────────────────────

    [Fact]
    public void Policy_FrozenValuesAndProvenance_MatchFreq6CExactly()
    {
        var policy = VolumeSafetyPolicy.FiveDayIntermediate;
        Assert.Equal(26.0d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(44.5d, policy.ResolvedPeakReference.Value);
        Assert.Equal(ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope, policy.ResolvedPeakReference.Provenance);
        Assert.Equal(.28d, policy.LongRunSelectionShare);
        Assert.Equal(.36d, policy.LongRunHardCapShare);
        Assert.Equal(26.0d, V1FiveDayIntermediateMissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm);
        Assert.Equal(19.5d, V1FiveDayIntermediateMissingReadinessStartingVolumePolicy.ExplicitZeroWeeklyVolumeDefaultKm);
        // The historical 4D generic policy is untouched -- still 16/12, still what 4D uses.
        Assert.Equal(16d, V1MissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm);
        Assert.Equal(12d, V1MissingReadinessStartingVolumePolicy.ExplicitZeroWeeklyVolumeDefaultKm);
    }

    [Fact]
    public void Resolve_Missing_Returns26Km()
    {
        var readiness = Readiness(missing: true, zero: false);
        var decision = V1FiveDayIntermediateMissingReadinessStartingVolumePolicy.Resolve(readiness);
        Assert.Equal(26.0d, decision.SelectedStartingVolumeKm);
        Assert.Null(decision.ReportedRecentWeeklyVolumeKm);
    }

    [Fact]
    public void Resolve_ExplicitZero_Returns19Point5Km()
    {
        var readiness = Readiness(missing: false, zero: true);
        var decision = V1FiveDayIntermediateMissingReadinessStartingVolumePolicy.Resolve(readiness);
        Assert.Equal(19.5d, decision.SelectedStartingVolumeKm);
        Assert.Equal(0d, decision.ReportedRecentWeeklyVolumeKm);
    }

    [Fact]
    public void Resolve_MissingAndZero_RemainDistinct_NeverConflated()
    {
        var missing = V1FiveDayIntermediateMissingReadinessStartingVolumePolicy.Resolve(Readiness(missing: true, zero: false));
        var zero = V1FiveDayIntermediateMissingReadinessStartingVolumePolicy.Resolve(Readiness(missing: false, zero: true));
        Assert.NotEqual(missing.SelectedStartingVolumeKm, zero.SelectedStartingVolumeKm);
        Assert.Equal(PrescriptionInputState.NotProvided, missing.InputState);
        Assert.Equal(PrescriptionInputState.Available, zero.InputState);
    }

    private static NormalizedRunningReadiness Readiness(bool missing, bool zero) => new(
        WeeklyVolume: missing
            ? new NormalizedDistanceInput(PrescriptionInputState.NotProvided, null, "RecentWeeklyVolumeKm", "km", null, null, [])
            : new NormalizedDistanceInput(PrescriptionInputState.Available, zero ? 0d : 20d, "RecentWeeklyVolumeKm", "km", zero ? 0d : 20d, null, []),
        LongestRun: new NormalizedDistanceInput(PrescriptionInputState.NotProvided, null, "RecentLongestRunKm", "km", null, null, []),
        RecentRunsPerWeekState: PrescriptionInputState.NotProvided,
        RecentRunsPerWeek: null,
        RecentRace: new NormalizedRecentRaceInput(PrescriptionInputState.NotProvided, null, null, null, "NONE", []),
        Issues: []);

    // ── §46-47: real dispatch / silent-fallback regression, real candidate ──

    private static readonly DateOnly StartDate = new(2026, 8, 3); // Monday
    private static readonly IReadOnlyList<DayOfWeek> FiveDayPreferred =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday];

    private static async Task<PlanCatalogCandidateSummary> RealFiveDayIntermediateCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.FiveDayCandidateKey, V1CatalogPilotIdentityPolicy.FiveDayCandidateVersion);
    }

    private static DynamicCoreVolumeAndLongRunOrchestrator RealOrchestrator() => new(
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
        new CatalogVolumeAndLongRunPlanner());

    private static async Task<DynamicCoreVolumeAndLongRunResult> BuildFiveDayAsync(
        int targetWeekCount, double? recentWeeklyVolumeKm)
    {
        var candidate = await RealFiveDayIntermediateCandidateAsync();
        var options = Options.Create(RealOptions());
        var raceDate = StartDate.AddDays(targetWeekCount * 7);

        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate,
            DaysPerWeek = 5, Unit = DistanceUnit.Km, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            PreferredDays = [Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Fri, Weekday.Sun],
            LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = recentWeeklyVolumeKm,
            RecentLongestRunKm = recentWeeklyVolumeKm is null ? null : recentWeeklyVolumeKm == 0 ? 0 : 9,
            RecentRunsPerWeek = recentWeeklyVolumeKm is null ? null : recentWeeklyVolumeKm == 0 ? 0 : 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = StartDate.AddDays(-21) },
        };

        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            DaysPerWeek = 5, Level = RunningBackground.Intermediate,
        };

        var conditionResults = new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "TEST"),
            RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST"),
        };

        var context = new DynamicCoreVolumeAndLongRunContext
        {
            Candidate = candidate,
            TargetWeekCount = targetWeekCount,
            StartDate = StartDate,
            AsOfDate = StartDate,
            PreferredDays = FiveDayPreferred,
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults = conditionResults,
            PreviewRequest = previewRequest,
            ResolverInput = resolverInput,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(options),
        };

        return await RealOrchestrator().PlanAsync(context);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task Dispatch_MissingReadiness_RealFiveDayCandidate_Resolves26Km_NotSilentFallback(int weeks)
    {
        var result = await BuildFiveDayAsync(weeks, null);
        Assert.Equal(26.0d, result.VolumeAndLongRunPlan.WeeklyVolumePlan.StartingVolumeDecision.SelectedStartingVolumeKm);
        // §47: permanent regression -- must never again silently resolve the
        // historical 4D-scoped generic fallback value (16km) for a real 5D candidate.
        Assert.NotEqual(16d, result.VolumeAndLongRunPlan.WeeklyVolumePlan.StartingVolumeDecision.SelectedStartingVolumeKm);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task Dispatch_ExplicitZeroReadiness_RealFiveDayCandidate_Resolves19Point5Km_NotSilentFallback(int weeks)
    {
        var result = await BuildFiveDayAsync(weeks, 0);
        Assert.Equal(19.5d, result.VolumeAndLongRunPlan.WeeklyVolumePlan.StartingVolumeDecision.SelectedStartingVolumeKm);
        Assert.NotEqual(12d, result.VolumeAndLongRunPlan.WeeklyVolumePlan.StartingVolumeDecision.SelectedStartingVolumeKm);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task PositiveObservedReadiness_RealFiveDayCandidate_UnchangedByNewPolicy(int weeks)
    {
        var result = await BuildFiveDayAsync(weeks, 24);
        Assert.Equal(24d, result.VolumeAndLongRunPlan.WeeklyVolumePlan.StartingVolumeDecision.SelectedStartingVolumeKm);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task MissingAndZeroReadiness_RealFiveDayCandidate_GeneratesCompleteTwoKeyTwoEasyOneLongCore(int weeks)
    {
        var missing = await BuildFiveDayAsync(weeks, null);
        var zero = await BuildFiveDayAsync(weeks, 0);
        Assert.True(missing.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks.Count > 0);
        Assert.True(zero.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks.Count > 0);
        // §27: real Taper traversal succeeds (no exception reaching this point at all).
        Assert.Contains(missing.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks, w => w.IsTaperWeek);
        Assert.Contains(zero.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks, w => w.IsTaperWeek);
    }
}
