using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
/// Backend Integration Phase 4G.5E — end-to-end tests for the dark, unwired
/// <see cref="DynamicCoreWorkoutBindingOrchestrator"/> against the REAL,
/// unmodified <c>TEN_K__4D__INTERMEDIATE v10</c> catalog candidate, for
/// every mathematically feasible standalone-core week count (8-14), both
/// with GOAL_PACE_REHEARSAL eligible (REALISTIC) and ineligible (NotEvaluated,
/// triggering the CURRENT_FITNESS_SPECIFIC_REHEARSAL fallback).
/// </summary>
public sealed class DynamicCoreWorkoutBindingOrchestratorTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 3); // Monday
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    private static async Task<PlanCatalogCandidateSummary> RealCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), Microsoft.Extensions.Logging.Abstractions.NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
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

    // ── Test matrix: 8-14 weeks, both GOAL_PACE_REHEARSAL branches ──────────

    public static IEnumerable<object[]> WeekCountsByGoalFeasibility()
    {
        foreach (var weeks in new[] { 8, 9, 10, 11, 12, 13, 14 })
        {
            yield return new object[] { weeks, "REALISTIC" }; // GOAL_PACE_REHEARSAL eligible (primary: GOAL_PACE_TEN_K)
            yield return new object[] { weeks, null! };       // NotEvaluated -> fallback (CURRENT_FITNESS_SPECIFIC_REHEARSAL)
        }
    }

    [Theory]
    [MemberData(nameof(WeekCountsByGoalFeasibility))]
    public async Task BindAsync_RealCandidate_EverySlotResolvesToANonNullCatalogWorkoutId(int targetWeekCount, string? goalFeasibility)
    {
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = await orchestrator.BindAsync(Context(candidate, targetWeekCount, goalFeasibility));

        var sessions = result.BoundPlan.Weeks.SelectMany(w => w.Sessions).ToList();
        Assert.Equal(targetWeekCount * 4, sessions.Count);
        Assert.All(sessions, s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        Assert.All(sessions, s => Assert.True(s.WorkoutDefinitionVersion > 0));

        var keySessions = sessions.Where(s => s.StructuralRole == "KEY_SESSION").ToList();
        var easySupport = sessions.Where(s => s.StructuralRole == "EASY_SUPPORT").ToList();
        var longRuns = sessions.Where(s => s.StructuralRole == "LONG_RUN").ToList();

        Assert.Equal(targetWeekCount, keySessions.Count);
        Assert.Equal(targetWeekCount * 2, easySupport.Count);
        Assert.Equal(targetWeekCount, longRuns.Count);
        Assert.All(keySessions, s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        Assert.All(easySupport, s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        Assert.All(longRuns, s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
    }

    [Theory]
    [MemberData(nameof(WeekCountsByGoalFeasibility))]
    public async Task BindAsync_RealCandidate_EasySupportAndLongRunUseFixedDefaults(int targetWeekCount, string? goalFeasibility)
    {
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = await orchestrator.BindAsync(Context(candidate, targetWeekCount, goalFeasibility));
        var sessions = result.BoundPlan.Weeks.SelectMany(w => w.Sessions).ToList();

        Assert.All(sessions.Where(s => s.StructuralRole == "EASY_SUPPORT"), s =>
        {
            Assert.Equal("EASY_STANDARD", s.WorkoutDefinitionKey);
            Assert.Equal(CatalogWorkoutBindingMode.FixedDefault, s.BindingMode);
            Assert.Null(s.ProgressionStageKey);
        });

        Assert.All(sessions.Where(s => s.StructuralRole == "LONG_RUN"), s =>
        {
            Assert.Equal("LONG_RUN_STANDARD", s.WorkoutDefinitionKey);
            Assert.Equal(CatalogWorkoutBindingMode.FixedDefault, s.BindingMode);
            Assert.Null(s.ProgressionStageKey);
        });
    }

    [Theory]
    [MemberData(nameof(WeekCountsByGoalFeasibility))]
    public async Task BindAsync_RealCandidate_TaperSharpenIdentityIsEasyStandard_NeverChanged(int targetWeekCount, string? goalFeasibility)
    {
        // AUD-507/AUD-508: TAPER_SHARPEN's workout identity remains EASY_STANDARD.
        // This test would fail (not silently pass) if a different workout were
        // ever bound to TAPER_SHARPEN, or if TAPER_SHARPEN stopped appearing.
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = await orchestrator.BindAsync(Context(candidate, targetWeekCount, goalFeasibility));
        var taperSharpenSessions = result.BoundPlan.Weeks.SelectMany(w => w.Sessions)
            .Where(s => s.ProgressionStageKey == "TAPER_SHARPEN")
            .ToList();

        Assert.NotEmpty(taperSharpenSessions);
        Assert.All(taperSharpenSessions, s =>
        {
            Assert.Equal("EASY_STANDARD", s.WorkoutDefinitionKey);
            Assert.Equal(CatalogWorkoutBindingMode.StageControlled, s.BindingMode);
            Assert.Equal("TAPER", s.PhaseKey);
        });
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task BindAsync_GoalFeasibilityRealistic_GoalPaceRehearsalBindsGoalPaceTenK(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = await orchestrator.BindAsync(Context(candidate, targetWeekCount, "REALISTIC"));
        var goalPaceSessions = result.BoundPlan.Weeks.SelectMany(w => w.Sessions)
            .Where(s => s.ProgressionStageKey == "GOAL_PACE_REHEARSAL")
            .ToList();

        Assert.NotEmpty(goalPaceSessions);
        Assert.All(goalPaceSessions, s => Assert.Equal("GOAL_PACE_TEN_K", s.WorkoutDefinitionKey));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task BindAsync_GoalFeasibilityNotEvaluated_UsesCurrentFitnessSpecificRehearsalFallback_GeneralizesTheSame12WeekEligibilityLogic(int targetWeekCount)
    {
        // Verifies TD-NOTEVALUATED-FALLBACK-001's existing, unchanged fallback
        // mechanism generalizes across 8-14 weeks using the exact same
        // eligibility logic already established for 12 weeks -- this test
        // does not attempt to resolve or reinterpret that TD.
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = await orchestrator.BindAsync(Context(candidate, targetWeekCount, null));
        var fallbackSessions = result.BoundPlan.Weeks.SelectMany(w => w.Sessions)
            .Where(s => s.ProgressionStageKey == "CURRENT_FITNESS_SPECIFIC_REHEARSAL")
            .ToList();

        Assert.NotEmpty(fallbackSessions);
        Assert.All(fallbackSessions, s =>
        {
            Assert.Equal("THRESHOLD_TEMPO", s.WorkoutDefinitionKey);
            Assert.Equal("GOAL_PACE_REHEARSAL", s.FallbackOrigin);
        });

        // No session in this run ever resolves to the primary GOAL_PACE_TEN_K candidate.
        var goalPaceTenKSessions = result.BoundPlan.Weeks.SelectMany(w => w.Sessions)
            .Where(s => s.WorkoutDefinitionKey == "GOAL_PACE_TEN_K")
            .ToList();
        Assert.Empty(goalPaceTenKSessions);
    }

    [Theory]
    [MemberData(nameof(WeekCountsByGoalFeasibility))]
    public async Task BindAsync_RealCandidate_ProducesValidBoundPlan(int targetWeekCount, string? goalFeasibility)
    {
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = await orchestrator.BindAsync(Context(candidate, targetWeekCount, goalFeasibility));

        Assert.Equal(targetWeekCount, result.BoundPlan.Weeks.Count);
        Assert.Equal(targetWeekCount, result.PhaseAllocation.TargetWeeks);
        Assert.True(result.PhaseAllocation.IsMathematicallyFeasible);
    }

    // ── Byte/value-level 12-week regression against the existing binding pipeline ──

    [Fact]
    public async Task BindAsync_TargetWeekCount12_MatchesExistingFixedWeekBindingPipelineExactly()
    {
        // Concrete before/after proof: builds the bound plan via the EXISTING,
        // completely unmodified fixed-week pipeline (CatalogPlanSkeletonOrchestrator
        // -> CatalogWeekSkeletonCalendarMaterializer -> ProgressionStageAllocator
        // -> CatalogWorkoutBinder, exactly as CatalogWorkoutBinderTests's own
        // RealFixtureAsync helper already does) and via this new Phase 4G.5E
        // dynamic orchestrator requesting targetWeekCount=12, then asserts full
        // field-by-field equality between every bound session. No existing
        // production file was modified by Phase 4G.5E.
        var candidate = await RealCandidateAsync();

        // Existing fixed-week pipeline (mirrors CatalogWorkoutBinderTests.RealFixtureAsync).
        var progression = await new CatalogWorkoutProgressionLoader(Options.Create(RealOptions())).LoadAsync(candidate.WorkoutProgression);

        var existingSkeletonOrchestrator = new CatalogPlanSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
            new CatalogStageToWeekContextFactory(), new CatalogStageToWeekMaterializer(),
            new GeneratedCatalogPlanSkeletonValidator());
        var existingSkeleton = existingSkeletonOrchestrator.Build(new CatalogPlanSkeletonOrchestrationContext
        {
            Candidate = candidate, ExpectedCandidateKey = candidate.CandidateKey, ExpectedCandidateVersion = candidate.CandidateVersion,
            ExpectedMasterTemplate = candidate.MasterTemplate, ExpectedRunLayout = candidate.Layout, StartDate = StartDate, AsOfDate = StartDate,
        }).Skeleton;

        var existingProvenance = new CatalogCalendarMaterializationProvenance(
            existingSkeleton.CandidateKey, existingSkeleton.CandidateVersion, StartDate, StartDate, PreferredDays, LongRunDay,
            CatalogCalendarDayMaterializerVersion.V1, existingSkeleton.SchemaVersion, existingSkeleton.DependencyVersions);
        var existingDatedSkeleton = new CatalogWeekSkeletonCalendarMaterializer().Materialize(new CatalogCalendarAssignmentContext(
            StartDate, RunningApp.Domain.Enums.GoalType.Race, PreferredDays, LongRunDay, existingSkeleton,
            CatalogCalendarAssignmentPolicy.RaceHardConstraint, existingProvenance));

        var existingStageSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            Progression = progression, Skeleton = existingSkeleton, ConditionResults = GoalFeasibilityResult("REALISTIC"),
        });

        var existingBoundPlan = await new CatalogWorkoutBinder().BindAsync(new CatalogWorkoutBindingContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            DatedSkeleton = existingDatedSkeleton, StageSchedule = existingStageSchedule, Progression = progression,
            ReferencedWorkouts = candidate.ReferencedWorkouts,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(RealOptions())),
        });

        // New Phase 4G.5E dynamic orchestrator at targetWeekCount=12.
        var dynamicOrchestrator = RealOrchestrator();
        var dynamicResult = await dynamicOrchestrator.BindAsync(Context(candidate, 12, "REALISTIC"));

        // Literal, hard-coded value-level fixture captured from the existing
        // live 12-week binding pipeline.
        Assert.Equal(48, existingBoundPlan.Weeks.Sum(w => w.Sessions.Count));
        Assert.Equal(12, existingBoundPlan.Weeks.Count);
        var existingWeek12TaperSession = existingBoundPlan.Weeks.Single(w => w.WeekNumber == 12).Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
        Assert.Equal("TAPER_SHARPEN", existingWeek12TaperSession.ProgressionStageKey);
        Assert.Equal("EASY_STANDARD", existingWeek12TaperSession.WorkoutDefinitionKey);

        // Full field-by-field equality: existing fixed-week pipeline vs. new
        // dynamic pipeline at targetWeekCount=12.
        static IEnumerable<(int WeekNumber, DateOnly Date, string PhaseKey, string? ProgressionStageKey, string StructuralRole,
            string WorkoutDefinitionKey, int WorkoutDefinitionVersion, CatalogWorkoutBindingMode BindingMode, string BindingReason, string? FallbackOrigin)>
            Flatten(BoundCatalogPlan plan) => plan.Weeks.SelectMany(w => w.Sessions).OrderBy(s => s.WeekNumber).ThenBy(s => s.Date)
                .Select(s => (s.WeekNumber, s.Date, s.PhaseKey, s.ProgressionStageKey, s.StructuralRole, s.WorkoutDefinitionKey, s.WorkoutDefinitionVersion, s.BindingMode, s.BindingReason, s.FallbackOrigin));

        Assert.Equal(Flatten(existingBoundPlan), Flatten(dynamicResult.BoundPlan));
    }

    // ── Zero production call sites (dark-reachability, structural) ─────────

    [Fact]
    public void DarkReachability_NoProductionCallSiteOutsideTheOneApprovedDarkConsumer()
    {
        // Reconciled (Phase 4G.5F): DynamicCoreVolumeAndLongRunOrchestrator.cs
        // is now a legitimate second reference -- it chains this Phase 4G.5E
        // binding orchestrator into a further dark volume/long-run planning
        // pipeline, and is itself proven dark by its own
        // DarkReachability_NoProductionCallSite test. This orchestrator
        // therefore remains structurally unreachable from any LIVE request
        // path, which is this test's actual invariant.
        var repoRoot = TestPlanServicesFactory.RepoRoot();
        var ownFileSuffix = Path.Combine("Schedule", "Binding", "DynamicCoreWorkoutBindingOrchestrator.cs");
        var approvedDarkConsumerSuffix = Path.Combine("Prescription", "Volume", "DynamicCoreVolumeAndLongRunOrchestrator.cs");

        var hits = new[] { "RunningApp.Application", "RunningApp.Api", "RunningApp.Infrastructure", "RunningApp.Persistence" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repoRoot, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.Combine("bin", "")) && !path.Contains(Path.Combine("obj", "")))
            .Where(path => !path.EndsWith(ownFileSuffix, StringComparison.Ordinal))
            .Where(path => !path.EndsWith(approvedDarkConsumerSuffix, StringComparison.Ordinal))
            .Where(path => !path.Contains(Path.Combine("Schedule", "PreparationRunwayOrchestration")))
            // Phase 4I.6A adds a second approved dark consumer (LongHorizonFullNumericOrchestrator),
            // reusing the exact same real Core pipeline construction for its own 21-52 week numeric join.
            .Where(path => !path.Contains(Path.Combine("Schedule", "LongHorizon")))
            .Where(path => !path.EndsWith("CatalogPreviewGenerator.cs", StringComparison.Ordinal))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bDynamicCoreWorkoutBinding(Orchestrator|Context|Result)\b"))
            .ToArray();

        Assert.Empty(hits);
    }

    [Fact]
    public void DarkReachability_NoDiRegistration()
    {
        var repoRoot = TestPlanServicesFactory.RepoRoot();

        var hits = new[] { "RunningApp.Api" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repoRoot, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.Combine("bin", "")) && !path.Contains(Path.Combine("obj", "")))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bIDynamicCoreWorkoutBindingOrchestrator\b"))
            .ToArray();

        Assert.Empty(hits);
    }
}
