using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Binding;

/// <summary>
/// Phase 10K-GEN.18 (Phase H) -- exhaustive per-week-count 2D Core
/// representability boundary verification, both levels, weeks 8 through 14
/// individually (not just the 12/14 endpoints GEN.17 already confirmed).
/// Reuses GEN.17's exact real pipeline harness
/// (<see cref="Gen17TwoDayWorkoutBindingDarkVerificationTests"/>'s own
/// orchestrator construction) -- no new production code, verification only.
///
/// Purpose: confirm the exact boundary of the disclosed GEN.17 §6 capacity
/// gap (RACE_SPECIFIC's halved-minimum stage count vs. its real Pattern-A
/// week count at short horizons) rather than assuming week counts 9-11
/// behave like the already-tested 12/14 endpoints.
/// </summary>
public sealed class Gen18TwoDayCoreRepresentabilityBoundaryTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 5); // Wednesday
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays = new[] { DayOfWeek.Wednesday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync(string key)
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), Microsoft.Extensions.Logging.Abstractions.NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(key, 1);
    }

    private static DynamicCoreWorkoutBindingOrchestrator RealOrchestrator() => new(
        new DynamicCoreWeekSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
        new CatalogWorkoutProgressionLoader(Options.Create(RealOptions())),
        new ProgressionStageAllocator(),
        new GeneratedCatalogStageScheduleValidator(),
        new CatalogWeekSkeletonCalendarMaterializer(),
        new DatedGeneratedCatalogPlanSkeletonValidator(),
        new CatalogWorkoutBinder(),
        new BoundCatalogPlanValidator());

    private static IReadOnlyList<RuntimeConditionResolutionResult> GoalFeasibilityResult(string? outputValue) =>
        outputValue is null
            ? new[] { RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "TEST_NOT_EVALUATED") }
            : new[] { RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", outputValue, "TEST_EVALUATED") };

    private static DynamicCoreWorkoutBindingContext Context(PlanCatalogCandidateSummary candidate, int targetWeekCount, string? goalFeasibility) => new()
    {
        Candidate = candidate,
        TargetWeekCount = targetWeekCount,
        StartDate = StartDate,
        AsOfDate = StartDate,
        PreferredDays = PreferredDays,
        LongRunDayPreference = LongRunDay,
        ConditionResults = GoalFeasibilityResult(goalFeasibility),
        WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(RealOptions())),
    };

    public static IEnumerable<object[]> AllWeekCountsBothLevels()
    {
        foreach (var candidateKey in new[] { "TEN_K__2D__BEGINNER", "TEN_K__2D__INTERMEDIATE" })
        {
            for (var weeks = 8; weeks <= 14; weeks++)
            {
                yield return new object[] { candidateKey, weeks };
            }
        }
    }

    /// <summary>
    /// Per-week-count classification: records, for every week 8-14 and both
    /// levels, whether the real binding pipeline succeeds or fails, and if
    /// it fails, the exact exception type/message -- so a genuinely
    /// different failure mode (not GEN.17's RACE_SPECIFIC capacity
    /// shortfall) would be caught here rather than assumed identical.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllWeekCountsBothLevels))]
    public async Task BindAsync_RealTwoDayCandidate_EveryWeekCountEightThroughFourteen_ClassifiedExplicitly(string candidateKey, int weeks)
    {
        var candidate = await CandidateAsync(candidateKey);
        var orchestrator = RealOrchestrator();

        // Weeks 8 and 9: confirmed capacity-insufficient (GEN.17's diagnosed
        // RACE_SPECIFIC shortfall extends one week further than GEN.17 itself
        // tested). Weeks 10-14: confirmed representable.
        if (weeks is 8 or 9)
        {
            var ex = await Assert.ThrowsAsync<DynamicCoreWorkoutBindingFailedException>(
                () => orchestrator.BindAsync(Context(candidate, weeks, "REALISTIC")));

            Assert.IsType<ProgressionPhaseCapacityInsufficientException>(ex.InnerException);
            Assert.Contains("RACE_SPECIFIC", ex.Message);
        }
        else
        {
            var result = await orchestrator.BindAsync(Context(candidate, weeks, "REALISTIC"));

            var sessions = result.BoundPlan.Weeks.SelectMany(w => w.Sessions).ToList();
            Assert.Equal(weeks * 2, sessions.Count);
            Assert.All(sessions, s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
            Assert.All(sessions, s => Assert.True(s.WorkoutDefinitionVersion > 0));

            var longRuns = sessions.Where(s => s.StructuralRole == "LONG_RUN").ToList();
            var keySessions = sessions.Where(s => s.StructuralRole == "KEY_SESSION").ToList();
            var easySupport = sessions.Where(s => s.StructuralRole == "EASY_SUPPORT").ToList();
            Assert.Equal(weeks, longRuns.Count);
            Assert.Equal(weeks, keySessions.Count + easySupport.Count);
        }
    }

    /// <summary>
    /// Same-goal-feasibility-null (fallback CURRENT_FITNESS_SPECIFIC_REHEARSAL)
    /// branch, for the two boundary week counts only (8/9 fail identically
    /// regardless of goal-feasibility branch since the shortfall is at the
    /// RACE_SPECIFIC phase-capacity level, before either top-level stage's
    /// own eligibility is what matters -- both GOAL_PACE_REHEARSAL and its
    /// fallback CURRENT_FITNESS_SPECIFIC_REHEARSAL declare minimumExposures=1,
    /// so the combined RACE_SPECIFIC minimum is 2 either way).
    /// </summary>
    [Theory]
    [InlineData("TEN_K__2D__BEGINNER", 8)]
    [InlineData("TEN_K__2D__BEGINNER", 9)]
    [InlineData("TEN_K__2D__INTERMEDIATE", 8)]
    [InlineData("TEN_K__2D__INTERMEDIATE", 9)]
    public async Task BindAsync_RealTwoDayCandidate_EightAndNineWeeks_FailsClosed_FallbackGoalBranchToo(string candidateKey, int weeks)
    {
        var candidate = await CandidateAsync(candidateKey);
        var orchestrator = RealOrchestrator();

        var ex = await Assert.ThrowsAsync<DynamicCoreWorkoutBindingFailedException>(
            () => orchestrator.BindAsync(Context(candidate, weeks, null)));

        Assert.IsType<ProgressionPhaseCapacityInsufficientException>(ex.InnerException);
        Assert.Contains("RACE_SPECIFIC", ex.Message);
    }
}
