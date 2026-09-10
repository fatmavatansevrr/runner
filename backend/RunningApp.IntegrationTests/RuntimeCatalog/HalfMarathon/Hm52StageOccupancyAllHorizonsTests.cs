using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;
using Xunit.Abstractions;

namespace RunningApp.IntegrationTests.RuntimeCatalog.HalfMarathon;

/// <summary>
/// Backend Integration Phase HM.5.2 — Section 8/9/13 required proof: real stage occupancy for
/// every 10-16W horizon against the real, unmodified <see cref="CatalogPhaseAllocationResolver"/>
/// (phase-level) and <see cref="ProgressionStageAllocator"/> (stage-level), using the real
/// catalog artifacts (post-HM.5.2 catalog edit restoring BUILD_FASTER_THAN_HM_INTRO to its
/// frozen HM.4 0/1 authority). Deliberately stops BEFORE the volume/long-run planner, isolating
/// the STRUCTURAL stage-allocation question this phase is scoped to from the separately-known,
/// out-of-scope HM.5.3 volume-planner blockers (12W/15W long-run-share; 16W peak-volume
/// overshoot) that <see cref="Hm2FullDarkVerticalSliceTests"/> already documents.
/// </summary>
public sealed class Hm52StageOccupancyAllHorizonsTests
{
    private readonly ITestOutputHelper _output;

    public Hm52StageOccupancyAllHorizonsTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey,
            V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateVersion);
    }

    private static async Task<CatalogWorkoutProgressionDefinition> ProgressionAsync(PlanCatalogCandidateSummary candidate)
    {
        var progressionLoader = new CatalogWorkoutProgressionLoader(Options.Create(OptionsValue));
        return await progressionLoader.LoadAsync(candidate.WorkoutProgression);
    }

    private static GeneratedCatalogPlanSkeleton Skeleton(PlanCatalogCandidateSummary candidate, int targetWeekCount, DateOnly startDate)
    {
        // DynamicCoreWeekSkeletonOrchestrator (Phase 4G.5D) — the generic, any-horizon skeleton
        // path, unlike the fixed-week CatalogPlanSkeletonOrchestrator used by
        // ProgressionStageAllocatorTests for the 10K default candidate.
        var orchestrator = new DynamicCoreWeekSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator());

        var context = new DynamicCoreWeekSkeletonOrchestrationContext
        {
            Candidate = candidate,
            TargetWeekCount = targetWeekCount,
            StartDate = startDate,
            AsOfDate = startDate,
        };

        return orchestrator.Build(context).Skeleton;
    }

    private static IReadOnlyList<RuntimeConditionResolutionResult> RealisticGoalFeasibility() => new[]
    {
        RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST_EVALUATED"),
        RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "TEST_EVALUATED"),
    };

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    public async Task EveryHorizon_StageAllocationSucceedsWithNoStageExceedingFrozenBounds(int targetWeekCount)
    {
        var candidate = await CandidateAsync();
        var progression = await ProgressionAsync(candidate);
        var skeleton = Skeleton(candidate, targetWeekCount, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = RealisticGoalFeasibility(),
        });

        var validation = new GeneratedCatalogStageScheduleValidator().Validate(schedule, skeleton);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
        Assert.Equal(targetWeekCount, schedule.Weeks.Count);

        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());
        _output.WriteLine($"{targetWeekCount}W: " + string.Join(", ", byStage.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}")));

        // BUILD_FASTER_THAN_HM_INTRO never exceeds its frozen Maximum=1 at any horizon.
        Assert.True(byStage.GetValueOrDefault("BUILD_FASTER_THAN_HM_INTRO", 0) <= 1);
    }

    /// <summary>Section 8/10 — the frozen 14W golden Build occupancy must resolve exactly 1/3, restored to HM.4's real authored 0/1/1 authority (no longer held at the HM.5.1 1/1 workaround).</summary>
    [Fact]
    public async Task FourteenWeekHorizon_BuildPhaseResolvesFrozenOneAndThreeOccupancy_UnderHm4RealMinimumZeroAuthority()
    {
        var candidate = await CandidateAsync();
        var progression = await ProgressionAsync(candidate);

        // Confirm the catalog itself now carries HM.4's real 0/1 minimum/preferred authority,
        // not HM.5.1's disclosed 1/1 workaround.
        var buildStage = progression.PhaseProgressions.Single(p => p.PhaseKey == "BUILD")
            .Stages.Single(s => s.ProgressionStageKey == "BUILD_FASTER_THAN_HM_INTRO");
        Assert.Equal(0, buildStage.MinimumExposures);
        Assert.Equal(1, buildStage.PreferredExposures);
        Assert.Equal(1, buildStage.MaximumExposures);

        var skeleton = Skeleton(candidate, 14, new DateOnly(2026, 1, 5));
        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = RealisticGoalFeasibility(),
        });

        var buildWeeks = schedule.Weeks.Where(w => w.PhaseKey == "BUILD").ToList();
        var byStage = buildWeeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(1, byStage.GetValueOrDefault("BUILD_FASTER_THAN_HM_INTRO", 0));
        Assert.Equal(3, byStage.GetValueOrDefault("BUILD_THRESHOLD_DEVELOPMENT", 0));
    }
}
