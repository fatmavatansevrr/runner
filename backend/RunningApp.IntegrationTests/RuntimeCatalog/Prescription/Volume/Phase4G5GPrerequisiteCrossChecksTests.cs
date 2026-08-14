using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
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
/// Backend Integration Phase 4G.5G — prerequisite cross-checks against
/// Phase 4G.5F's output, required to be confirmed before building the pace
/// prescription layer on top of it. Item 1: extends Phase 4G.5F's
/// re-verification (which covered only weekly-volume increase caps) to
/// LongRunProgressionVerifier's own share-cap checks, across the same 6
/// runner profiles x 7 horizons. Item 2: confirms taper remains meaningful
/// (reduced volume relative to the preceding non-taper week) specifically
/// at compressed horizons (8-11 weeks).
/// </summary>
public sealed class Phase4G5GPrerequisiteCrossChecksTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 3);
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    private static async Task<PlanCatalogCandidateSummary> RealCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
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
        new RunningApp.Application.RuntimeCatalog.Prescription.CatalogPrescriptionContextBuilder(),
        new CatalogVolumeAndLongRunPlanner());

    public enum RunnerProfile
    {
        LowVolumeIntermediate, CurrentPilotProfile, HigherVolumeIntermediate,
        RecentLongRunMissing, RecentVolumeMissing, ExplicitZeroEvidence,
    }

    private static void ApplyProfile(GeneratePreviewRequest request, RunnerProfile profile)
    {
        switch (profile)
        {
            case RunnerProfile.LowVolumeIntermediate: request.RecentWeeklyVolumeKm = 14; request.RecentLongestRunKm = 6; request.RecentRunsPerWeek = 3; break;
            case RunnerProfile.CurrentPilotProfile: request.RecentWeeklyVolumeKm = 24; request.RecentLongestRunKm = 9; request.RecentRunsPerWeek = 4; break;
            case RunnerProfile.HigherVolumeIntermediate: request.RecentWeeklyVolumeKm = 35; request.RecentLongestRunKm = 14; request.RecentRunsPerWeek = 5; break;
            case RunnerProfile.RecentLongRunMissing: request.RecentWeeklyVolumeKm = 24; request.RecentLongestRunKm = null; request.RecentRunsPerWeek = 4; break;
            case RunnerProfile.RecentVolumeMissing: request.RecentWeeklyVolumeKm = null; request.RecentLongestRunKm = 9; request.RecentRunsPerWeek = 4; break;
            case RunnerProfile.ExplicitZeroEvidence: request.RecentWeeklyVolumeKm = 0; request.RecentLongestRunKm = 0; request.RecentRunsPerWeek = 0; break;
        }
    }

    private static async Task<DynamicCoreVolumeAndLongRunResult> BuildAsync(PlanCatalogCandidateSummary candidate, int targetWeekCount, RunnerProfile profile)
    {
        var options = Options.Create(RealOptions());
        var raceDate = StartDate.AddDays(targetWeekCount * 7);

        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate, DaysPerWeek = candidate.DaysPerWeek,
            Unit = DistanceUnit.Km, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun }, LongRunDay = Weekday.Sun,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = StartDate.AddDays(-21) },
        };
        ApplyProfile(previewRequest, profile);

        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            DaysPerWeek = candidate.DaysPerWeek, Level = RunningBackground.Intermediate,
        };

        var conditionResults = new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "TEST"),
            RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST"),
        };

        var context = new DynamicCoreVolumeAndLongRunContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = StartDate, AsOfDate = StartDate,
            PreferredDays = PreferredDays, LongRunDayPreference = LongRunDay, ConditionResults = conditionResults,
            PreviewRequest = previewRequest, ResolverInput = resolverInput,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(options),
        };

        return await RealOrchestrator().PlanAsync(context);
    }

    public static IEnumerable<object[]> WeekCountsByProfile()
    {
        foreach (var weeks in new[] { 8, 9, 10, 11, 12, 13, 14 })
        {
            foreach (RunnerProfile profile in Enum.GetValues(typeof(RunnerProfile)))
            {
                yield return new object[] { weeks, profile };
            }
        }
    }

    // ── Prerequisite 1: LongRunProgressionVerifier at the same 6x7 scale ────

    [Theory]
    [MemberData(nameof(WeekCountsByProfile))]
    public async Task Prerequisite1_LongRunProgressionVerifier_PassesAcrossAllSixProfilesAndSevenHorizons(int targetWeekCount, RunnerProfile profile)
    {
        var candidate = await RealCandidateAsync();
        var result = await BuildAsync(candidate, targetWeekCount, profile);

        var verification = LongRunProgressionVerifier.Verify(
            result.VolumeAndLongRunPlan.WeeklyVolumePlan, result.VolumeAndLongRunPlan.LongRunProgression, VolumeSafetyPolicy.Default);

        Assert.Equal(LongRunProgressionOutcome.Pass, verification.Outcome);
        Assert.DoesNotContain(verification.Findings, f => f.StartsWith("EXCEEDS_WEEKLY_VOLUME") || f.StartsWith("HARD_CAP_SHARE_VIOLATION"));
    }

    // ── Prerequisite 2: taper remains meaningful at compressed horizons (8-11) ──

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public async Task Prerequisite2_CompressedHorizon_TaperVolumeIsReducedRelativeToPrecedingWeek(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var result = await BuildAsync(candidate, targetWeekCount, RunnerProfile.CurrentPilotProfile);
        var weeks = result.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).ToList();

        var taperWeek = weeks.Single(w => w.IsTaperWeek);
        var precedingWeek = weeks.Single(w => w.WeekNumber == taperWeek.WeekNumber - 1);

        Assert.True(taperWeek.PlannedWeeklyVolumeKm < precedingWeek.PlannedWeeklyVolumeKm,
            $"targetWeekCount={targetWeekCount}: taper week {taperWeek.WeekNumber} volume {taperWeek.PlannedWeeklyVolumeKm}km " +
            $"is not below preceding week {precedingWeek.WeekNumber}'s volume {precedingWeek.PlannedWeeklyVolumeKm}km.");

        // Quantify the reduction against the policy's own TaperVolumeMultiplier.
        var expectedTaperKm = precedingWeek.PlannedWeeklyVolumeKm * VolumeSafetyPolicy.Default.TaperVolumeMultiplier;
        Assert.True(Math.Abs(taperWeek.PlannedWeeklyVolumeKm - expectedTaperKm) < 1.0,
            $"targetWeekCount={targetWeekCount}: taper volume {taperWeek.PlannedWeeklyVolumeKm}km deviates from the policy's " +
            $"TaperVolumeMultiplier-derived expectation {expectedTaperKm:0.##}km by more than the rounding tolerance.");
    }
}
