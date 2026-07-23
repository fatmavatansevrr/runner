using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4F.4 — proves the dark internal skeleton
/// wiring inside <see cref="CatalogPreviewGenerator.GenerateAsync"/>: the
/// Phase 4F.3 orchestrator is invoked only after routing/candidate-selection/
/// eligibility/resolver success, its result is never surfaced anywhere
/// public, and its failure aborts preview generation through the existing
/// typed error taxonomy. Uses a controlled, in-memory PUBLISHED candidate
/// fixture (cloned from the real v10 catalog data via the documented
/// internal-dry-run entry point, status overridden only in memory) — never
/// touches or changes the real catalog JSON's DRAFT status.
/// </summary>
public sealed class Phase4F4DarkSkeletonWiringTests
{
    /// <summary>Counts invocations without changing behavior — proves "executed only once" / "not invoked" assertions.</summary>
    private sealed class CountingSkeletonOrchestrator : ICatalogPlanSkeletonOrchestrator
    {
        private readonly ICatalogPlanSkeletonOrchestrator _inner;
        public int InvocationCount { get; private set; }
        public CatalogPlanSkeletonOrchestrationContext? LastContext { get; private set; }

        public CountingSkeletonOrchestrator(ICatalogPlanSkeletonOrchestrator inner) => _inner = inner;

        public CatalogPlanSkeletonOrchestrationResult Build(CatalogPlanSkeletonOrchestrationContext context)
        {
            InvocationCount++;
            LastContext = context;
            return _inner.Build(context);
        }
    }

    private sealed class ThrowingSkeletonOrchestrator : ICatalogPlanSkeletonOrchestrator
    {
        private readonly Exception _exception;
        public ThrowingSkeletonOrchestrator(Exception exception) => _exception = exception;
        public CatalogPlanSkeletonOrchestrationResult Build(CatalogPlanSkeletonOrchestrationContext context) => throw _exception;
    }

    /// <summary>Counts calls, proving the eligibility gate/loader is never invoked twice for one GenerateAsync call.</summary>
    private sealed class CountingEligibilityGate : ICatalogCandidateEligibilityGate
    {
        private readonly PlanCatalogCandidateSummary _fixedResult;
        public int PublicLoadCount { get; private set; }

        public CountingEligibilityGate(PlanCatalogCandidateSummary fixedResult) => _fixedResult = fixedResult;

        public Task<PlanCatalogCandidateSummary> LoadForPublicPreviewAsync(string candidateKey, int candidateVersion, CancellationToken ct = default)
        {
            PublicLoadCount++;
            return Task.FromResult(_fixedResult);
        }

        public Task<PlanCatalogCandidateSummary> LoadForInternalDryRunAsync(string candidateKey, int candidateVersion, CancellationToken ct = default) =>
            Task.FromResult(_fixedResult);
    }

    /// <summary>Real gate that always throws CatalogCandidateNotPublishedException — exercises the real, unmodified DRAFT v10 candidate.</summary>
    private static ICatalogCandidateEligibilityGate RealDraftGate()
    {
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return new CatalogCandidateEligibilityGate(bundleLoader);
    }

    private static RuntimeConditionResolutionService RealOrchestration() =>
        new(new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());

    private static ICatalogPlanSkeletonOrchestrator RealSkeletonOrchestrator() => new CatalogPlanSkeletonOrchestrator(
        new CatalogPhaseAllocationResolver(),
        new CatalogRunLayoutResolver(),
        new CatalogStageToWeekContextFactory(),
        new CatalogStageToWeekMaterializer(),
        new GeneratedCatalogPlanSkeletonValidator());

    /// <summary>Loads the real v10 candidate's real catalog data via the documented dry-run entry point, then clones it in memory with CandidateStatus/DependencyStatuses overridden to PUBLISHED — never touches the real catalog JSON file.</summary>
    private static async Task<PlanCatalogCandidateSummary> LoadControlledPublishedCandidateAsync()
    {
        var real = await CatalogPlanSkeletonOrchestratorFixtures.LoadRealPilotCandidateAsync();
        return new PlanCatalogCandidateSummary
        {
            CandidateKey = real.CandidateKey,
            CandidateVersion = real.CandidateVersion,
            CandidateStatus = "PUBLISHED",
            CanonicalDistanceFamily = real.CanonicalDistanceFamily,
            Level = real.Level,
            DaysPerWeek = real.DaysPerWeek,
            CoreCycle = real.CoreCycle,
            MasterTemplate = real.MasterTemplate,
            Layout = real.Layout,
            LevelModifier = real.LevelModifier,
            WorkoutProgression = real.WorkoutProgression,
            ProgressionModifier = real.ProgressionModifier,
            RulePack = real.RulePack,
            PeakVolumeBandPolicy = real.PeakVolumeBandPolicy,
            RuntimeConditionValueRegistry = real.RuntimeConditionValueRegistry,
            DependencyStatuses = real.DependencyStatuses.Keys.ToDictionary(k => k, _ => "PUBLISHED"),
            ReferencedWorkouts = real.ReferencedWorkouts,
            PhaseKeys = real.PhaseKeys,
            PhaseAllocations = real.PhaseAllocations,
            SlotRoles = real.SlotRoles,
        };
    }

    private static GeneratePreviewRequest PilotRequest(DateOnly raceDate) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = raceDate.AddDays(-84),
        RaceDate = raceDate,
        // Backend Integration Phase 4F.5: a race-plan preview now also
        // dark-materializes a calendar-day assignment, which requires
        // PreferredDays/LongRunDay -- Mon/Wed/Fri/Sun with long run on Sunday
        // is a known-safe combination (see CatalogWeekSkeletonCalendarMaterializerTests).
        PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
        LongRunDay = Weekday.Sun,
    };

    // ─────────────────────────── Wiring placement ───────────────────────────

    [Fact]
    public async Task DarkSkeleton_InvokedAfterEligibilityAndResolverSuccess_ForControlledPublishedCandidate()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new CountingEligibilityGate(candidate);
        var counting = new CountingSkeletonOrchestrator(RealSkeletonOrchestrator());
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), counting);
        var asOfDate = new DateOnly(2026, 1, 5);
        var raceDate = asOfDate.AddDays(84);

        var snapshot = await generator.GenerateAsync(PilotRequest(raceDate), asOfDate);

        Assert.Equal(1, counting.InvocationCount);
        Assert.NotNull(snapshot);
    }

    [Fact]
    public async Task DarkSkeleton_ReceivesFrozenAsOfDate()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new CountingEligibilityGate(candidate);
        var counting = new CountingSkeletonOrchestrator(RealSkeletonOrchestrator());
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), counting);
        var asOfDate = new DateOnly(2026, 3, 2);
        var raceDate = asOfDate.AddDays(84);

        await generator.GenerateAsync(PilotRequest(raceDate), asOfDate);

        Assert.Equal(asOfDate, counting.LastContext!.AsOfDate);
        Assert.Equal(asOfDate, counting.LastContext!.StartDate);
    }

    [Fact]
    public async Task DarkSkeleton_ReceivesAlreadySelectedCandidate_PinnedMasterTemplateAndRunLayout()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new CountingEligibilityGate(candidate);
        var counting = new CountingSkeletonOrchestrator(RealSkeletonOrchestrator());
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), counting);
        var asOfDate = new DateOnly(2026, 1, 5);

        await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);

        Assert.Same(candidate, counting.LastContext!.Candidate);
        Assert.Equal(candidate.MasterTemplate, counting.LastContext!.ExpectedMasterTemplate);
        Assert.Equal(candidate.Layout, counting.LastContext!.ExpectedRunLayout);
        Assert.Equal(candidate.CandidateKey, counting.LastContext!.ExpectedCandidateKey);
        Assert.Equal(candidate.CandidateVersion, counting.LastContext!.ExpectedCandidateVersion);
    }

    [Fact]
    public async Task DarkSkeleton_NotInvoked_WhenCandidateStatusIsDraft()
    {
        var counting = new CountingSkeletonOrchestrator(RealSkeletonOrchestrator());
        var generator = new CatalogPreviewGenerator(RealDraftGate(), RealOrchestration(), counting);

        await Assert.ThrowsAsync<CatalogCandidateNotPublishedException>(() =>
            generator.GenerateAsync(PilotRequest(new DateOnly(2026, 4, 1)), new DateOnly(2026, 1, 5)));

        Assert.Equal(0, counting.InvocationCount);
    }

    [Fact]
    public async Task DarkSkeleton_NotInvoked_WhenResolverOutcomeBlocksPreview()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new CountingEligibilityGate(candidate);
        var counting = new CountingSkeletonOrchestrator(RealSkeletonOrchestrator());
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), counting);
        // Race goal without a RaceDate -- TimeAdequacyResolver throws ArgumentException,
        // converted to PlanPreviewGenerationFailedException before ever reaching governance
        // policy or dark skeleton invocation.
        var request = PilotRequest(new DateOnly(2026, 4, 1));
        request.RaceDate = null;

        await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(request, new DateOnly(2026, 1, 5)));

        Assert.Equal(0, counting.InvocationCount);
    }

    // ─────────────────────────────── No reruns ───────────────────────────────

    [Fact]
    public async Task NoReruns_EligibilityGateCalledExactlyOnce()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new CountingEligibilityGate(candidate);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator());
        var asOfDate = new DateOnly(2026, 1, 5);

        await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);

        Assert.Equal(1, gate.PublicLoadCount);
    }

    [Fact]
    public void SkeletonOrchestrator_HasNoDatabaseHttpOrClockDependency()
    {
        var ctor = typeof(CatalogPlanSkeletonOrchestrator).GetConstructors(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Single();
        var paramTypeNames = ctor.GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();

        Assert.DoesNotContain(paramTypeNames, t => t.Contains("DbContext") || t.Contains("HttpContext") || t.Contains("Clock"));
    }

    // ───────────────────────────── Dark success ──────────────────────────────

    [Fact]
    public async Task DarkSkeleton_Success_Produces12WeekSkeleton_NotSurfacedAnywhere()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new CountingEligibilityGate(candidate);
        var counting = new CountingSkeletonOrchestrator(RealSkeletonOrchestrator());
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), counting);
        var asOfDate = new DateOnly(2026, 1, 5);

        var snapshot = await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);

        // The orchestrator ran and (by not throwing) proves it built a valid
        // 12-week skeleton -- but nothing about it reached the snapshot.
        Assert.Equal(1, counting.InvocationCount);
        Assert.NotNull(snapshot.GeneratedPreviewPlanPayload);
        Assert.DoesNotContain(snapshot.GetType().GetProperties(), p => p.Name.Contains("Skeleton"));
    }

    [Fact]
    public async Task DarkSkeleton_Success_SnapshotAndHashStructurallyUnchanged_FromPre4F4Shape()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new CountingEligibilityGate(candidate);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator());
        var asOfDate = new DateOnly(2026, 1, 5);

        var snapshot = await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);

        Assert.Equal(4, snapshot.ResolverResults.Count);
        Assert.Empty(snapshot.SelectedStageKeys);
        Assert.Empty(snapshot.FallbackStagesUsed);
        Assert.NotNull(snapshot.GeneratedPreviewPlanPayload);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.ContentHash));
        Assert.Equal(8, snapshot.ReferencedArtifacts.Count);
    }

    // ────────────────────────────── Failure paths ─────────────────────────────

    [Theory]
    [MemberData(nameof(TypedSkeletonFailures))]
    public async Task DarkSkeleton_Failure_AbortsPreviewGeneration_PreservingTypedCause(Exception cause)
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new CountingEligibilityGate(candidate);
        var throwing = new ThrowingSkeletonOrchestrator(cause);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), throwing);
        var asOfDate = new DateOnly(2026, 1, 5);

        var ex = await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate));

        Assert.Same(cause, ex.InnerException);
        Assert.Contains("CATALOG_INTERNAL_SKELETON_MATERIALIZATION_FAILED", ex.Message);
    }

    public static TheoryData<Exception> TypedSkeletonFailures => new()
    {
        new CatalogPhaseAllocationSourceMissingException("phase allocation missing"),
        new CatalogPhaseAllocationInvalidException("phase allocation invalid"),
        new CatalogPhaseAllocationTotalMismatchException("phase allocation total mismatch"),
        new CatalogMasterTemplateReferenceMismatchException("master template mismatch"),
        new CatalogRunLayoutReferenceMismatchException("run layout mismatch"),
        new CatalogRunLayoutSlotInvalidException("run layout slot invalid"),
        new CatalogSkeletonContextInvalidException("context invalid"),
        new CatalogPlanSkeletonOrchestrationFailedException("materializer/validator failed"),
    };

    [Fact]
    public async Task DarkSkeleton_Failure_NoRetryRerunsResolverOrchestration()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new CountingEligibilityGate(candidate);
        var throwing = new ThrowingSkeletonOrchestrator(new CatalogPhaseAllocationInvalidException("boom"));
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), throwing);
        var asOfDate = new DateOnly(2026, 1, 5);

        await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate));

        // Exactly one eligibility load -- no retry loop re-ran candidate selection.
        Assert.Equal(1, gate.PublicLoadCount);
    }

    // ─────────────────────── Current real candidate behavior ───────────────────

    [Fact]
    public async Task RealDraftCandidate_StillFailsAtPublishedOnlyGate_NeverInvokesSkeletonOrchestration()
    {
        var counting = new CountingSkeletonOrchestrator(RealSkeletonOrchestrator());
        var generator = new CatalogPreviewGenerator(RealDraftGate(), RealOrchestration(), counting);

        await Assert.ThrowsAsync<CatalogCandidateNotPublishedException>(() =>
            generator.GenerateAsync(PilotRequest(new DateOnly(2026, 4, 1)), new DateOnly(2026, 1, 5)));

        Assert.Equal(0, counting.InvocationCount);
    }
}
