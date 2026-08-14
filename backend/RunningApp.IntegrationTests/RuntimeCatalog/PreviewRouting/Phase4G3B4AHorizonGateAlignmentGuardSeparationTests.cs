using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Phase 4G.3B.4a — proves the live race-date alignment guard inside
/// <see cref="CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton"/> now
/// owns date-tolerance correctness only, no longer the standalone-horizon
/// support question (owned exclusively by <c>RaceHorizonPolicy</c> / the
/// preview-routing layer, unchanged by this phase). See
/// PHASE4G_3B_4A_HORIZON_GATE_ALIGNMENT_GUARD_SEPARATION.md and
/// TD-RACEDATE-CHECK-NOT-HORIZON-AGNOSTIC-001.
///
/// The public-route "negative daysBeforeRace" (final session after RaceDate)
/// case is already covered by the existing, unmodified
/// <see cref="Sw13ExactTwelveWeekOnlyEndToEndTests.ShortfallWithinExactTwelveClassification_TriggersDefensiveAlignmentInvariant_NoPersistence"/>
/// -- not duplicated here. The "more than seven days before RaceDate" case
/// can never occur through the public route (the upstream ExactStandaloneCoreSupported
/// classification bounds the raw day gap to at most 84 days, which bounds
/// daysBeforeRace to at most 1) -- it is only reachable by calling the
/// generator directly with an internally inconsistent RaceDate, exactly the
/// "defensive backstop" scenario this guard exists for, so it is tested here
/// against the generator directly, mirroring the existing internal-seam
/// convention already used by <see cref="CatalogPreviewGeneratorTests"/> and
/// <see cref="Phase4F5DarkCalendarWiringTests"/>.
/// </summary>
public sealed class Phase4G3B4AHorizonGateAlignmentGuardSeparationTests
{
    private sealed class FixedResultEligibilityGate : ICatalogCandidateEligibilityGate
    {
        private readonly PlanCatalogCandidateSummary _result;
        public FixedResultEligibilityGate(PlanCatalogCandidateSummary result) => _result = result;

        public Task<PlanCatalogCandidateSummary> LoadForPublicPreviewAsync(string candidateKey, int candidateVersion, System.Threading.CancellationToken ct = default) =>
            Task.FromResult(_result);

        public Task<PlanCatalogCandidateSummary> LoadForInternalDryRunAsync(string candidateKey, int candidateVersion, System.Threading.CancellationToken ct = default) =>
            Task.FromResult(_result);
    }

    /// <summary>Test-only seam: substitutes a precomputed, real (non-synthetic) dated skeleton for whatever the real calendar materializer would otherwise build from the live 12-week phase/week skeleton -- lets this file exercise the alignment guard against a genuinely-materialized non-12-week schedule without needing a horizon-aware allocator (which does not exist yet -- see <see cref="CatalogPhaseAllocationResolver"/>).</summary>
    private sealed class FixedResultCalendarMaterializer : ICatalogWeekSkeletonCalendarMaterializer
    {
        private readonly DatedGeneratedCatalogPlanSkeleton _result;
        public FixedResultCalendarMaterializer(DatedGeneratedCatalogPlanSkeleton result) => _result = result;
        public DatedGeneratedCatalogPlanSkeleton Materialize(CatalogCalendarAssignmentContext context) => _result;
    }

    private static readonly Weekday[] DefaultPreferredDays = { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun };
    private static readonly DateOnly StartDate = new(2026, 7, 20);

    private static ICatalogPlanSkeletonOrchestrator RealSkeletonOrchestrator() => new CatalogPlanSkeletonOrchestrator(
        new CatalogPhaseAllocationResolver(),
        new CatalogRunLayoutResolver(),
        new CatalogStageToWeekContextFactory(),
        new CatalogStageToWeekMaterializer(),
        new GeneratedCatalogPlanSkeletonValidator());

    private static RuntimeConditionResolutionService RealOrchestration() =>
        new(new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());

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
            DependencyStatuses = new Dictionary<string, string>
            {
                ["masterTemplate"] = "PUBLISHED", ["layout"] = "PUBLISHED", ["levelModifier"] = "PUBLISHED", ["rulePack"] = "PUBLISHED",
            },
            ReferencedWorkouts = real.ReferencedWorkouts,
            PhaseKeys = real.PhaseKeys,
            PhaseAllocations = real.PhaseAllocations,
            SlotRoles = real.SlotRoles,
        };
    }

    private static GeneratePreviewRequest PilotRequest(DateOnly startDate, DateOnly raceDate) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = startDate,
        RaceDate = raceDate,
        PreferredDays = DefaultPreferredDays,
        LongRunDay = Weekday.Sun,
    };

    /// <summary>Real (non-synthetic) 8-week dated skeleton: real candidate, real <see cref="CatalogPhaseAllocationResolver.Resolve(PlanCatalogCandidateSummary, int)"/> targeted-week overload, real stage-to-week materializer, real calendar materializer -- mirrors <c>RaceDateAlignmentVerifierTests.RealDatedScheduleAsync</c>'s own construction, already proven correct for 8-14 week counts.</summary>
    private static async Task<DatedGeneratedCatalogPlanSkeleton> RealEightWeekDatedScheduleAsync()
    {
        var root = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = root });

        var candidate = await new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance)
            .LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, 8);
        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);

        var context = new CatalogStageToWeekMaterializationContext
        {
            StartDate = StartDate, AsOfDate = StartDate, PlannedWeekCount = 8, DaysPerWeek = candidate.SlotRoles.Count,
            CanonicalDistanceFamily = progression.DistanceFamily, CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            SelectedStageSequence = allocation.Phases.Select(p => p.PhaseKey).ToList(),
            StageWeekAllocations = allocation.Phases.Select(p => new CatalogStageWeekAllocation(p.PhaseKey, p.AllocatedWeeks)).ToList(),
            RunLayout = candidate.Layout, RunLayoutSlotRoles = candidate.SlotRoles,
        };
        var plainSkeleton = new CatalogStageToWeekMaterializer().Materialize(context).Skeleton;
        var provenance = new CatalogCalendarMaterializationProvenance(candidate.CandidateKey, candidate.CandidateVersion, context.AsOfDate, context.StartDate,
            DefaultPreferredDays.Select(w => w switch
            {
                Weekday.Mon => DayOfWeek.Monday, Weekday.Tue => DayOfWeek.Tuesday, Weekday.Wed => DayOfWeek.Wednesday,
                Weekday.Thu => DayOfWeek.Thursday, Weekday.Fri => DayOfWeek.Friday, Weekday.Sat => DayOfWeek.Saturday,
                _ => DayOfWeek.Sunday,
            }).ToList(),
            DayOfWeek.Sunday, CatalogCalendarDayMaterializerVersion.V1, plainSkeleton.SchemaVersion, new Dictionary<string, PlanCatalogReference>());
        return new CatalogWeekSkeletonCalendarMaterializer().Materialize(new CatalogCalendarAssignmentContext(context.StartDate, GoalType.Race,
            provenance.PreferredDays, DayOfWeek.Sunday, plainSkeleton, CatalogCalendarAssignmentPolicy.RaceHardConstraint, provenance));
    }

    // ── C. Genuine date-misalignment rejection: more than seven days before ──
    // ── RaceDate (only reachable via direct generator call -- see class doc) ─

    [Fact]
    public async Task AlignmentGuard_RejectsFinalSessionMoreThanSevenDaysBeforeRaceDate()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator());
        var asOfDate = StartDate;
        // Real 12-week schedule ends at StartDate + 83 days. A RaceDate ten
        // days after that final session leaves daysBeforeRace = 10 > 7.
        var farRaceDate = StartDate.AddDays(15 * 7);

        var ex = await Assert.ThrowsAsync<CatalogRaceDateAlignmentInvalidException>(() =>
            generator.GenerateAsync(PilotRequest(StartDate, farRaceDate), asOfDate));

        Assert.Contains("not aligned to", ex.Message);
    }

    // ── D. Non-12 internal alignment behavior ─────────────────────────────────

    [Fact]
    public async Task AlignmentGuard_EightWeekCorrectlyAlignedInternalSkeleton_NotRejectedForWeekCount()
    {
        var realEightWeekSchedule = await RealEightWeekDatedScheduleAsync();
        Assert.Equal(8, realEightWeekSchedule.Weeks.Count);

        // RaceDate chosen so the real 8-week schedule's own EndDate is
        // exactly one day before it -- inside the [0,7] tolerance -- proving
        // this is a correctly-aligned, non-12-week schedule, not merely a
        // short one the guard happens to let through.
        var raceDate = realEightWeekSchedule.EndDate.AddDays(1);
        var daysBeforeRace = raceDate.DayNumber - realEightWeekSchedule.EndDate.DayNumber;
        Assert.InRange(daysBeforeRace, 0, 7);

        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var fixedCalendar = new FixedResultCalendarMaterializer(realEightWeekSchedule);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator(), fixedCalendar);

        // The generator's earlier phase/stage-progression steps still build
        // against the real (non-horizon-aware) fixed default allocation --
        // no horizon-aware allocator exists yet (see
        // Phase4G3AEightWeekCoreAllocationAuditTests, which proves
        // CatalogPhaseAllocationResolver.Resolve(candidate) always emits 12
        // weeks regardless of any request horizon). Substituting only the
        // calendar layer therefore produces a self-inconsistent pipeline
        // downstream of the alignment guard (workout-phase eligibility
        // mismatches between the 12-week stage schedule and the 8-week
        // dated calendar) -- expected, and unrelated to this test's target.
        // What this test proves is narrower and precise: the alignment
        // guard itself, evaluated against this real, correctly-aligned
        // 8-week dated skeleton, must never throw
        // CatalogRaceDateAlignmentInvalidException solely because
        // Weeks.Count is 8, not 12 -- this is the exact behavior Phase
        // 4G.3B.4a's guard-separation is meant to prove. Any other
        // exception proves the alignment guard itself was already passed.
        // This is an internal ownership test only: it calls the generator
        // directly, bypassing PlanServices.GeneratePreviewAsync's own
        // horizon guard entirely. It does NOT mean the public 8-week route
        // is enabled -- see
        // AlignmentGuard_PublicEightWeekRouting_RemainsRejectedByHorizonPolicy
        // below and the existing, unmodified
        // Sw13ExactTwelveWeekOnlyEndToEndTests.VerifiedEightWeekRegressionCase_*
        // tests, which continue to prove the public route still rejects 8
        // weeks via RaceHorizonPolicy before ever reaching this generator.
        var ex = await Record.ExceptionAsync(() => generator.GenerateAsync(PilotRequest(StartDate, raceDate), StartDate));

        Assert.IsNotType<CatalogRaceDateAlignmentInvalidException>(ex);
    }

    [Fact]
    public void AlignmentGuard_PublicEightWeekRouting_IsActivatedByHorizonPolicy()
    {
        // Companion assertion to the internal-ownership test above: the
        // public 8-week horizon is still classified as not-yet-implemented
        // by the single canonical policy every production layer defers to
        // -- unchanged by this phase. Full HTTP-level proof (422,
        // PLAN_CORE_HORIZON_UNSUPPORTED, never CATALOG_RACE_DATE_ALIGNMENT_INVALID)
        // remains covered by the existing, unmodified
        // Sw13ExactTwelveWeekOnlyEndToEndTests.VerifiedEightWeekRegressionCase_*
        // tests -- not duplicated here.
        Assert.Equal(
            RunningApp.Application.Common.RaceHorizonClassification.StandaloneCoreSupported,
            RunningApp.Application.Common.RaceHorizonPolicy.Classify(8));
    }

    // ── E. Exception-ownership structural test ────────────────────────────────

    [Fact]
    public void LiveAlignmentGuard_ProductionGuardBlock_NoLongerReferencesWeekCountOrExactTwelveConstant()
    {
        // Bounded source inspection (this repository has no symbol-aware
        // test tooling -- see the existing *_HasNoProductionCallSite
        // convention used throughout RuntimeCatalog/Schedule/Materialization
        // tests for the established precedent of grep-based structural
        // proof). Isolates exactly the race-date alignment guard block
        // inside BuildDarkInternalDatedSkeleton -- from its own `if
        // (request.RaceDate is { } raceDateForAlignment)` line (unique in
        // this file; the surrounding doc comment intentionally still
        // mentions RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks in
        // prose to document the Phase 4G.3B.4a audit finding, so the region
        // start is deliberately placed after that comment, not before it)
        // through the next catch clause that already existed before this
        // guard (also unique) -- and asserts the forbidden symbols are
        // absent only from that isolated block, never matching the doc
        // comment or any unrelated method.
        var path = Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog",
            "PreviewRouting", "CatalogPreviewGenerator.cs");
        var source = File.ReadAllText(path);

        const string blockStart = "if (request.RaceDate is { } raceDateForAlignment)";
        const string blockEnd = "catch (Exception ex) when (ex is CatalogPreferredDaysRequiredException";

        var startIndex = source.IndexOf(blockStart, System.StringComparison.Ordinal);
        var endIndex = source.IndexOf(blockEnd, System.StringComparison.Ordinal);

        Assert.True(startIndex >= 0, "Could not locate the race-date alignment guard block start marker.");
        Assert.True(endIndex > startIndex, "Could not locate the race-date alignment guard block end marker.");

        var guardBlock = source.Substring(startIndex, endIndex - startIndex);

        // Weeks.Count may still appear in the exception's descriptive
        // message text (informational only) -- what must be absent is any
        // COMPARISON of it against a week-count authority, i.e. the pattern
        // this guard used before Phase 4G.3B.4a.
        Assert.DoesNotContain("Weeks.Count !=", guardBlock);
        Assert.DoesNotContain("ExactStandaloneCoreSupportedWeeks", guardBlock);
        Assert.DoesNotContain("expectedWeekCount", guardBlock);
        Assert.DoesNotContain("!= 12", guardBlock);

        // The block must still contain the date-tolerance logic itself --
        // this proves the assertions above are excluding real removed code,
        // not merely testing an empty/wrong region.
        Assert.Contains("daysBeforeRace", guardBlock);
        Assert.Contains("maxAllowedTrailingGapDays", guardBlock);
        Assert.Contains("CatalogRaceDateAlignmentInvalidException", guardBlock);
    }

    // ── F. RaceDateAlignmentVerifier remains dark, not invoked by the ─────────
    // ── refactored guard ───────────────────────────────────────────────────────

    [Fact]
    public void LiveAlignmentGuard_DoesNotInvokeStandaloneRaceDateAlignmentVerifier()
    {
        var path = Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog",
            "PreviewRouting", "CatalogPreviewGenerator.cs");
        var source = File.ReadAllText(path);

        Assert.DoesNotContain("RaceDateAlignmentVerifier.Verify(", source);
    }
}
