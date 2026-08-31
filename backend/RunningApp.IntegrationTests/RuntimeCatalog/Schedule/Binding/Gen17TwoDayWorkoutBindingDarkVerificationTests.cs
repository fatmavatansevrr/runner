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
/// Phase 10K-GEN.17 -- dark verification (no public HTTP, public gate
/// deliberately untouched) for the FULL workout-content binding pipeline
/// (skeleton -&gt; stage allocation -&gt; calendar -&gt; workout binding) against the
/// real <c>TEN_K__2D__BEGINNER v1</c> / <c>TEN_K__2D__INTERMEDIATE v1</c>
/// catalog candidates, implementing GEN.14/GEN.15/GEN.16's frozen
/// Pattern-A-week-denominated lane capacity + halved-exposure mechanism.
///
/// Per GEN.14 §2, lane 0's real capacity for a 2D phase is the count of
/// Pattern-A (KEY_SESSION-carrying) weeks in that phase, not the phase's
/// literal calendar-week count. The real TEN_K_MASTER v11 phase-allocation
/// bounds (FOUNDATION [2,3,4], BUILD [3,4,5], RACE_SPECIFIC [2,4,4], TAPER
/// [1,1,1]), combined with the frozen global odd/even week-ordinal pattern
/// (GEN.11 §1/§11) and GEN.16's signed-off halved RACE_SPECIFIC minimums
/// (TEN_K_SPECIFIC_INTRO=1 Compressible-floor-1, GOAL_PACE_REHEARSAL=1
/// Protected/FixedExposure), together produce exactly ONE real capacity
/// shortfall: the 8-week Core minimum horizon allocates RACE_SPECIFIC to
/// its own 2-week minimum (weeks 6-7), which contains only 1 real Pattern-A
/// week -- one fewer than RACE_SPECIFIC's 2 top-level stages' combined
/// halved minimum (2), and neither top-level stage has compression headroom
/// left to give (TEN_K_SPECIFIC_INTRO already floored to 1 by halving;
/// GOAL_PACE_REHEARSAL is Protected). This is a genuine, disclosed,
/// deterministic capacity gap -- present for BOTH levels identically, since
/// both reuse the same shared single-lane v5-lineage source numbers per
/// GEN.14's own "never cross-level" halving rule applied to identical
/// underlying content. It is NOT patched around here (no invented
/// compression-floor change, no invented RACE_SPECIFIC re-allocation) --
/// see the GEN.17 phase report for the full disclosure. 12-week and 14-week
/// Core horizons are unaffected and fully bind end-to-end for both levels.
/// </summary>
public sealed class Gen17TwoDayWorkoutBindingDarkVerificationTests
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

    public static IEnumerable<object[]> LevelsByHorizonAndGoalFeasibility()
    {
        foreach (var candidateKey in new[] { "TEN_K__2D__BEGINNER", "TEN_K__2D__INTERMEDIATE" })
        {
            foreach (var weeks in new[] { 12, 14 })
            {
                yield return new object[] { candidateKey, weeks, "REALISTIC" };
                yield return new object[] { candidateKey, weeks, null! };
            }
        }
    }

    // ── 12/14-week Core horizons: full pipeline binds end-to-end, both levels ──

    [Theory]
    [MemberData(nameof(LevelsByHorizonAndGoalFeasibility))]
    public async Task BindAsync_RealTwoDayCandidate_EverySlotResolvesToANonNullCatalogWorkoutId(string candidateKey, int targetWeekCount, string? goalFeasibility)
    {
        var candidate = await CandidateAsync(candidateKey);
        var orchestrator = RealOrchestrator();

        var result = await orchestrator.BindAsync(Context(candidate, targetWeekCount, goalFeasibility));

        var sessions = result.BoundPlan.Weeks.SelectMany(w => w.Sessions).ToList();
        Assert.Equal(targetWeekCount * 2, sessions.Count);
        Assert.All(sessions, s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        Assert.All(sessions, s => Assert.True(s.WorkoutDefinitionVersion > 0));

        var keySessions = sessions.Where(s => s.StructuralRole == "KEY_SESSION").ToList();
        var easySupport = sessions.Where(s => s.StructuralRole == "EASY_SUPPORT").ToList();
        var longRuns = sessions.Where(s => s.StructuralRole == "LONG_RUN").ToList();

        // Exactly the odd-numbered (Pattern A) weeks carry KEY_SESSION, per GEN.11's
        // frozen global week-ordinal alternation; TAPER's week is always KEY_SESSION
        // (GEN.11 §5's structural override) -- so total KEY_SESSION count is the real
        // Pattern-A week count for this horizon, not targetWeekCount/2 rounded blindly.
        Assert.Equal(targetWeekCount, longRuns.Count);
        Assert.Equal(targetWeekCount, keySessions.Count + easySupport.Count);
        Assert.All(keySessions, s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        Assert.All(easySupport, s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        Assert.All(longRuns, s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
    }

    [Theory]
    [InlineData("TEN_K__2D__BEGINNER")]
    [InlineData("TEN_K__2D__INTERMEDIATE")]
    public async Task BindAsync_RealTwoDayCandidate_TwelveWeeks_TaperSharpenIdentityIsEasyStandard(string candidateKey)
    {
        var candidate = await CandidateAsync(candidateKey);
        var orchestrator = RealOrchestrator();

        var result = await orchestrator.BindAsync(Context(candidate, 12, "REALISTIC"));
        var taperKeySession = result.BoundPlan.Weeks.Single(w => w.WeekNumber == 12)
            .Sessions.Single(s => s.StructuralRole == "KEY_SESSION");

        Assert.Equal("EASY_STANDARD", taperKeySession.WorkoutDefinitionKey);
    }

    // ── Disclosed real capacity gap: 8-week Core minimum fails closed, both levels ──

    [Theory]
    [InlineData("TEN_K__2D__BEGINNER")]
    [InlineData("TEN_K__2D__INTERMEDIATE")]
    public async Task BindAsync_RealTwoDayCandidate_EightWeeks_FailsClosed_RaceSpecificCapacityInsufficient(string candidateKey)
    {
        var candidate = await CandidateAsync(candidateKey);
        var orchestrator = RealOrchestrator();

        var ex = await Assert.ThrowsAsync<DynamicCoreWorkoutBindingFailedException>(
            () => orchestrator.BindAsync(Context(candidate, 8, "REALISTIC")));

        Assert.IsType<ProgressionPhaseCapacityInsufficientException>(ex.InnerException);
        Assert.Contains("RACE_SPECIFIC", ex.Message);
    }

    // ── Zero-delta: the real, already-PUBLICLY_ACTIVE dual-KEY 6D pipeline is unaffected ──

    [Fact]
    public async Task ExistingSixDayCandidate_FullBindingPipeline_RemainsUnaffected()
    {
        var candidate = await CandidateAsync("TEN_K__6D__INTERMEDIATE");
        Assert.Null(candidate.WeeklyPatternRoles);
        Assert.Null(candidate.PatternPeriodWeeks);

        var orchestrator = RealOrchestrator();
        var sixDayPreferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var context = Context(candidate, 12, "REALISTIC");
        context = new DynamicCoreWorkoutBindingContext
        {
            Candidate = context.Candidate,
            TargetWeekCount = context.TargetWeekCount,
            StartDate = context.StartDate,
            AsOfDate = context.AsOfDate,
            PreferredDays = sixDayPreferredDays,
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults = context.ConditionResults,
            WorkoutDefinitionLoader = context.WorkoutDefinitionLoader,
        };
        var result = await orchestrator.BindAsync(context);

        var sessions = result.BoundPlan.Weeks.SelectMany(w => w.Sessions).ToList();
        Assert.Equal(12 * 6, sessions.Count);
        Assert.All(sessions, s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
    }
}
