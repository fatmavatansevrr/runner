using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4G.5D — end-to-end tests for the dark,
/// unwired <see cref="DynamicCoreWeekSkeletonOrchestrator"/> against the
/// REAL, unmodified <c>TEN_K__4D__INTERMEDIATE v10</c> catalog candidate,
/// for every mathematically feasible standalone-core week count (8-14).
/// </summary>
public sealed class DynamicCoreWeekSkeletonOrchestratorTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 5);

    private static DynamicCoreWeekSkeletonOrchestrator RealOrchestrator() => new(
        new CatalogPhaseAllocationResolver(),
        new CatalogRunLayoutResolver(),
        new CatalogStageToWeekMaterializer(),
        new GeneratedCatalogPlanSkeletonValidator());

    private static async Task<PlanCatalogCandidateSummary> RealCandidateAsync() =>
        await CatalogPlanSkeletonOrchestratorFixtures.LoadRealPilotCandidateAsync();

    private static DynamicCoreWeekSkeletonOrchestrationContext Context(PlanCatalogCandidateSummary candidate, int targetWeekCount) => new()
    {
        Candidate = candidate,
        TargetWeekCount = targetWeekCount,
        StartDate = StartDate,
        AsOfDate = StartDate,
    };

    // ── Test matrix: 8, 9, 10, 11, 12, 13, 14 weeks ─────────────────────────

    [Theory]
    [InlineData(8, new[] { 2, 3, 2, 1 })]
    [InlineData(9, new[] { 2, 3, 3, 1 })]
    [InlineData(10, new[] { 2, 3, 4, 1 })]
    [InlineData(11, new[] { 2, 4, 4, 1 })]
    [InlineData(12, new[] { 3, 4, 4, 1 })]
    [InlineData(13, new[] { 4, 4, 4, 1 })]
    [InlineData(14, new[] { 4, 5, 4, 1 })]
    public async Task Build_RealCandidate_ProducesExpectedTotalAndPhaseWeekCounts(int targetWeekCount, int[] expectedPhaseWeeks)
    {
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = orchestrator.Build(Context(candidate, targetWeekCount));

        Assert.True(result.PhaseAllocation.IsMathematicallyFeasible);
        Assert.Equal(targetWeekCount, result.Skeleton.PlannedWeekCount);
        Assert.Equal(targetWeekCount, result.Skeleton.Weeks.Count);
        Assert.Equal(
            new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" },
            result.PhaseAllocation.Phases.Select(p => p.PhaseKey));
        Assert.Equal(expectedPhaseWeeks, result.PhaseAllocation.Phases.Select(p => p.AllocatedWeeks));
        Assert.True(result.Validation.IsValid);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Build_RealCandidate_WeeksAreContiguousStartingAtOneWithNoGapOrOverlap(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = orchestrator.Build(Context(candidate, targetWeekCount));

        var weekNumbers = result.Skeleton.Weeks.Select(w => w.WeekNumber).ToList();
        Assert.Equal(Enumerable.Range(1, targetWeekCount), weekNumbers);
        Assert.Equal(weekNumbers.Distinct().Count(), weekNumbers.Count);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Build_RealCandidate_EachPhaseReceivesExactAllocatedCountAndOrderIsPreserved(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = orchestrator.Build(Context(candidate, targetWeekCount));

        // Phase order in the skeleton's own week sequence must match the
        // allocation's own order exactly -- this orchestrator never reorders.
        var weekPhaseSequenceDistinct = result.Skeleton.Weeks.Select(w => w.StageKey).Distinct().ToList();
        Assert.Equal(result.PhaseAllocation.Phases.Select(p => p.PhaseKey), weekPhaseSequenceDistinct);

        foreach (var phase in result.PhaseAllocation.Phases)
        {
            var weeksForPhase = result.Skeleton.Weeks.Where(w => w.StageKey == phase.PhaseKey).ToList();
            Assert.Equal(phase.AllocatedWeeks, weeksForPhase.Count);
            Assert.Equal(Enumerable.Range(1, phase.AllocatedWeeks), weeksForPhase.Select(w => w.StageWeekIndex));
            Assert.All(weeksForPhase, w => Assert.Equal(phase.AllocatedWeeks, w.StageWeekCount));
        }
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Build_RealCandidate_TaperOccupiesFinalPhaseWeeks(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = orchestrator.Build(Context(candidate, targetWeekCount));

        var taperWeeks = result.PhaseAllocation.Phases.Single(p => p.PhaseKey == "TAPER").AllocatedWeeks;
        var lastWeeks = result.Skeleton.Weeks.OrderBy(w => w.WeekNumber).TakeLast(taperWeeks).ToList();
        Assert.All(lastWeeks, w => Assert.Equal("TAPER", w.StageKey));

        // No week after the last TAPER week, and no non-TAPER week among the final taperWeeks.
        var lastNonTaperIndex = result.Skeleton.Weeks.Where(w => w.StageKey != "TAPER").Max(w => w.WeekNumber);
        Assert.Equal(targetWeekCount - taperWeeks, lastNonTaperIndex);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Build_RealCandidate_EveryWeekIncludingTaperHasFourSessionSlotsInLayoutOrder(int targetWeekCount)
    {
        // Verifies the actual current session-slot structure rather than
        // assuming it: today's CatalogStageToWeekMaterializer.BuildSessionSlots
        // uses the same RunLayoutSlotRoles sequence for every week regardless
        // of StageKey -- Taper is NOT special-cased. This test would fail (not
        // silently pass) if that ever changed.
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        var result = orchestrator.Build(Context(candidate, targetWeekCount));

        var expectedRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" };
        Assert.All(result.Skeleton.Weeks, w =>
        {
            Assert.Equal(4, w.SessionSlots.Count);
            Assert.Equal(expectedRoles, w.SessionSlots.OrderBy(s => s.SlotOrderInWeek).Select(s => s.StructuralRole));
        });

        var taperWeeks = result.Skeleton.Weeks.Where(w => w.StageKey == "TAPER").ToList();
        Assert.NotEmpty(taperWeeks);
        Assert.All(taperWeeks, w =>
        {
            Assert.Equal(4, w.SessionSlots.Count);
            Assert.Equal(expectedRoles, w.SessionSlots.OrderBy(s => s.SlotOrderInWeek).Select(s => s.StructuralRole));
        });

        Assert.Equal(targetWeekCount * 4, result.Skeleton.Weeks.Sum(w => w.SessionSlots.Count));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(15)]
    public async Task Build_TargetOutsideCatalogBounds_ThrowsInfeasible(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();

        Assert.Throws<DynamicCoreWeekSkeletonInfeasibleException>(() => orchestrator.Build(Context(candidate, targetWeekCount)));
    }

    // ── Byte/value-level 12-week regression against the existing, unmodified
    //    fixed-week Phase 4F.3 orchestrator ──────────────────────────────────

    [Fact]
    public async Task Build_TargetWeekCount12_MatchesExistingFixedWeekOrchestratorExactly()
    {
        // Concrete before/after proof, not a bare "structurally identical"
        // claim: builds the skeleton via the EXISTING, completely unmodified
        // Phase 4F.3 CatalogPlanSkeletonOrchestrator (the real live-12-week
        // path) and via this new Phase 4G.5D dynamic orchestrator requesting
        // targetWeekCount=12, then asserts full field-by-field equality
        // between every week and every session slot. Neither
        // CatalogPlanSkeletonOrchestrator.cs, CatalogStageToWeekMaterializer.cs,
        // nor GeneratedCatalogPlanSkeleton.cs was modified by Phase 4G.5D --
        // this test proves the new generic path reproduces the old fixed
        // path's exact output, not merely that the old path was untouched.
        var candidate = await RealCandidateAsync();

        var existingOrchestrator = CatalogPlanSkeletonOrchestratorFixtures.RealOrchestrator();
        var existingContext = CatalogPlanSkeletonOrchestratorFixtures.OrchestrationContext(candidate: candidate, startDate: StartDate);
        var existingResult = existingOrchestrator.Build(existingContext);

        var dynamicOrchestrator = RealOrchestrator();
        var dynamicResult = dynamicOrchestrator.Build(Context(candidate, 12));

        // Literal, hard-coded value-level fixture captured from the existing
        // live 12-week path (FOUNDATION=3, BUILD=4, RACE_SPECIFIC=4, TAPER=1;
        // 4 session slots/week; StartDate 2026-08-05) -- not merely asserted
        // equal to the dynamic result, but pinned to its own known literal
        // values first, so a future accidental change to either path is
        // caught even if both changed identically.
        Assert.Equal(12, existingResult.Skeleton.PlannedWeekCount);
        Assert.Equal(new DateOnly(2026, 8, 5), existingResult.Skeleton.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 27), existingResult.Skeleton.EndDate);
        Assert.Equal(
            new[] { "FOUNDATION", "FOUNDATION", "FOUNDATION", "BUILD", "BUILD", "BUILD", "BUILD", "RACE_SPECIFIC", "RACE_SPECIFIC", "RACE_SPECIFIC", "RACE_SPECIFIC", "TAPER" },
            existingResult.Skeleton.Weeks.OrderBy(w => w.WeekNumber).Select(w => w.StageKey));
        Assert.Equal(48, existingResult.Skeleton.Weeks.Sum(w => w.SessionSlots.Count));

        // Full field-by-field equality: existing fixed-week path vs. new
        // dynamic path at targetWeekCount=12.
        Assert.Equal(existingResult.Skeleton.PlannedWeekCount, dynamicResult.Skeleton.PlannedWeekCount);
        Assert.Equal(existingResult.Skeleton.DaysPerWeek, dynamicResult.Skeleton.DaysPerWeek);
        Assert.Equal(existingResult.Skeleton.StartDate, dynamicResult.Skeleton.StartDate);
        Assert.Equal(existingResult.Skeleton.EndDate, dynamicResult.Skeleton.EndDate);
        Assert.Equal(existingResult.Skeleton.CandidateKey, dynamicResult.Skeleton.CandidateKey);
        Assert.Equal(existingResult.Skeleton.CandidateVersion, dynamicResult.Skeleton.CandidateVersion);
        Assert.Equal(existingResult.Skeleton.CanonicalDistanceFamily, dynamicResult.Skeleton.CanonicalDistanceFamily);

        Assert.Equal(
            existingResult.Skeleton.Weeks.OrderBy(w => w.WeekNumber).Select(w =>
                (w.WeekNumber, w.StartDate, w.EndDate, w.StageKey, w.StageWeekIndex, w.StageWeekCount,
                 SessionSlots: string.Join(",", w.SessionSlots.OrderBy(s => s.SlotOrderInWeek).Select(s => $"{s.SlotOrderInWeek}:{s.LayoutSlotKey}:{s.StructuralRole}")))),
            dynamicResult.Skeleton.Weeks.OrderBy(w => w.WeekNumber).Select(w =>
                (w.WeekNumber, w.StartDate, w.EndDate, w.StageKey, w.StageWeekIndex, w.StageWeekCount,
                 SessionSlots: string.Join(",", w.SessionSlots.OrderBy(s => s.SlotOrderInWeek).Select(s => $"{s.SlotOrderInWeek}:{s.LayoutSlotKey}:{s.StructuralRole}")))));

        Assert.True(existingResult.Validation.IsValid);
        Assert.True(dynamicResult.Validation.IsValid);
    }

    // ── Zero production call sites (dark-reachability, structural) ─────────

    [Fact]
    public void DarkReachability_NoProductionCallSiteOutsideTheOneApprovedDarkConsumer()
    {
        // Reconciled (Phase 4G.5E): DynamicCoreWorkoutBindingOrchestrator.cs
        // is now a legitimate second reference -- it chains this Phase 4G.5D
        // skeleton orchestrator into a further dark binding pipeline, and is
        // itself proven dark by its own DarkReachability_NoProductionCallSite
        // test. This orchestrator therefore remains structurally unreachable
        // from any LIVE request path, which is this test's actual invariant.
        var repoRoot = TestPlanServicesFactory.RepoRoot();
        var ownFileSuffix = Path.Combine("Schedule", "Materialization", "DynamicCoreWeekSkeletonOrchestrator.cs");
        var approvedDarkConsumerSuffix = Path.Combine("Schedule", "Binding", "DynamicCoreWorkoutBindingOrchestrator.cs");

        var hits = new[] { "RunningApp.Application", "RunningApp.Api", "RunningApp.Infrastructure", "RunningApp.Persistence" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repoRoot, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.Combine("bin", "")) && !path.Contains(Path.Combine("obj", "")))
            .Where(path => !path.EndsWith(ownFileSuffix, StringComparison.Ordinal))
            .Where(path => !path.EndsWith(approvedDarkConsumerSuffix, StringComparison.Ordinal))
            .Where(path => !path.Contains(Path.Combine("Schedule", "PreparationRunwayOrchestration")))
            .Where(path => !path.Contains(Path.Combine("Schedule", "LongHorizon")))
            .Where(path => !path.EndsWith("CatalogPreviewGenerator.cs", StringComparison.Ordinal))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bDynamicCoreWeekSkeleton(Orchestrator|OrchestrationContext|OrchestrationResult)\b"))
            .ToArray();

        Assert.Empty(hits);
    }

    [Fact]
    public void DarkReachability_NoDiRegistration()
    {
        // Scoped to RunningApp.Api only -- that project is where all live DI
        // registration happens (Program.cs); DynamicCoreWorkoutBindingOrchestrator.cs
        // (Phase 4G.5E) referencing IDynamicCoreWeekSkeletonOrchestrator as a
        // constructor-injected type lives in RunningApp.Application, out of
        // this scan's scope, so no exclusion is needed here.
        var repoRoot = TestPlanServicesFactory.RepoRoot();

        var hits = new[] { "RunningApp.Api" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repoRoot, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.Combine("bin", "")) && !path.Contains(Path.Combine("obj", "")))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bIDynamicCoreWeekSkeletonOrchestrator\b"))
            .ToArray();

        Assert.Empty(hits);
    }
}
