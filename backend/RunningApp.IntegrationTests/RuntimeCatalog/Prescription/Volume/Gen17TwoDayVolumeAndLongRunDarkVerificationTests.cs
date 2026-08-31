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
/// Phase 10K-GEN.17 -- dark verification (no public HTTP, public gate
/// untouched) confirming <see cref="VolumeSafetyPolicy.Beginner2D"/>/
/// <see cref="VolumeSafetyPolicy.Intermediate2D"/> (implemented in GEN.12,
/// disclosed there as "implemented and unit-reachable, but not yet
/// dark-verified end-to-end through a real volume/long-run plan, because
/// that requires workout-content binding to succeed first") are now fully
/// wired end-to-end through the real <see cref="DynamicCoreVolumeAndLongRunOrchestrator"/>
/// pipeline for both <c>TEN_K__2D__BEGINNER v1</c> / <c>TEN_K__2D__INTERMEDIATE v1</c>,
/// now that GEN.17's ProgressionStageAllocator fix unblocks binding. Only
/// 12/14-week Core horizons are exercised -- the 8-week horizon is a
/// disclosed, genuine RACE_SPECIFIC capacity gap at the binding layer (see
/// <see cref="RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Binding.Gen17TwoDayWorkoutBindingDarkVerificationTests"/>),
/// not something this test works around.
/// </summary>
public sealed class Gen17TwoDayVolumeAndLongRunDarkVerificationTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 5); // Wednesday
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays = new[] { DayOfWeek.Wednesday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync(string key)
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(key, 1);
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

    private static async Task<DynamicCoreVolumeAndLongRunResult> BuildAsync(
        PlanCatalogCandidateSummary candidate, int targetWeekCount, double recentWeeklyVolumeKm, double recentLongestRunKm)
    {
        var options = Options.Create(RealOptions());
        var raceDate = StartDate.AddDays(targetWeekCount * 7);

        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            Level = candidate.Level == "NEW" ? RunningBackground.Beginner : RunningBackground.Intermediate,
            DaysPerWeek = candidate.DaysPerWeek,
            Unit = DistanceUnit.Km, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Wed, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = StartDate.AddDays(-21) },
            RecentWeeklyVolumeKm = recentWeeklyVolumeKm,
            RecentLongestRunKm = recentLongestRunKm,
            RecentRunsPerWeek = 2,
        };

        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            DaysPerWeek = candidate.DaysPerWeek,
            Level = candidate.Level == "NEW" ? RunningBackground.Beginner : RunningBackground.Intermediate,
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
            PreferredDays = PreferredDays,
            LongRunDayPreference = LongRunDay,
            ConditionResults = conditionResults,
            PreviewRequest = previewRequest,
            ResolverInput = resolverInput,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(options),
        };

        return await RealOrchestrator().PlanAsync(context);
    }

    public static IEnumerable<object[]> LevelsByHorizon()
    {
        foreach (var candidateKey in new[] { "TEN_K__2D__BEGINNER", "TEN_K__2D__INTERMEDIATE" })
        {
            foreach (var weeks in new[] { 12, 14 })
            {
                yield return new object[] { candidateKey, weeks };
            }
        }
    }

    [Theory]
    [MemberData(nameof(LevelsByHorizon))]
    public async Task PlanAsync_RealTwoDayCandidate_ProducesValidPlan_WithinGen11FrozenPeakBand(string candidateKey, int targetWeekCount)
    {
        var candidate = await CandidateAsync(candidateKey);
        var isBeginner = candidateKey == "TEN_K__2D__BEGINNER";

        // Mid-band recent evidence, well clear of any missing/zero-readiness branch.
        var result = await BuildAsync(candidate, targetWeekCount,
            recentWeeklyVolumeKm: isBeginner ? 12 : 16, recentLongestRunKm: isBeginner ? 5 : 7);

        var weeklyPlan = result.VolumeAndLongRunPlan.WeeklyVolumePlan;
        var longRunPlan = result.VolumeAndLongRunPlan.LongRunProgression;

        Assert.True(weeklyPlan.ValidationResult.IsValid, string.Join("; ", weeklyPlan.ValidationResult.Errors));
        Assert.True(longRunPlan.ValidationResult.IsValid, string.Join("; ", longRunPlan.ValidationResult.Errors));
        Assert.Equal(targetWeekCount, weeklyPlan.Weeks.Count);
        Assert.Equal(targetWeekCount, longRunPlan.Weeks.Count);

        // GEN.11 §2/§3-frozen PeakVolumeBand: Beginner [16,22]km, Intermediate [20,30]km.
        var (bandMin, bandMax) = isBeginner ? (16.0, 22.0) : (20.0, 30.0);
        Assert.True(weeklyPlan.PeakVolumeKm <= bandMax + 0.001, $"Peak {weeklyPlan.PeakVolumeKm} exceeds band max {bandMax}.");
        Assert.True(weeklyPlan.PeakVolumeKm >= 0, "Peak volume must be non-negative.");

        var taperWeek = weeklyPlan.Weeks.Single(w => w.IsTaperWeek);
        Assert.True(taperWeek.PlannedWeeklyVolumeKm < weeklyPlan.PeakVolumeKm);

        Assert.All(weeklyPlan.Weeks, w => Assert.True(w.PlannedWeeklyVolumeKm > 0));
        Assert.All(longRunPlan.Weeks, w => Assert.True(w.PlannedLongRunDistanceKm > 0));

        // GEN.11 §6's frozen 55% preferred / 60% hard-cap long-run share (Beginner2D/Intermediate2D).
        var policy = isBeginner ? VolumeSafetyPolicy.Beginner2D : VolumeSafetyPolicy.Intermediate2D;
        Assert.All(longRunPlan.Weeks, w =>
            Assert.True(w.PlannedLongRunDistanceKm <= w.PlannedWeeklyVolumeKm * policy.LongRunHardCapShare + 0.5001));
    }

    [Theory]
    [InlineData("TEN_K__2D__BEGINNER")]
    [InlineData("TEN_K__2D__INTERMEDIATE")]
    public async Task PlanAsync_RealTwoDayCandidate_ZeroReadiness_ProductIneligible(string candidateKey)
    {
        var candidate = await CandidateAsync(candidateKey);

        // GEN.11 §7's frozen missing/zero-readiness authority: PRODUCT_INELIGIBLE, no default.
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => BuildAsync(candidate, 12, recentWeeklyVolumeKm: 0, recentLongestRunKm: 0));

        var rootCause = ex.InnerException ?? ex;
        Assert.Contains("TwoDayMissingOrZeroReadiness", rootCause.GetType().Name);
    }
}
