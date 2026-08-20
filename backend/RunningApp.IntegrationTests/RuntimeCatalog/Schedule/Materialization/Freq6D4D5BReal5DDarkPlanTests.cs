using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase 10K-FREQ.6D.4D.5B — dark, real, unrouted proof that the generalized
/// <see cref="CatalogWeekSkeletonCalendarMaterializer"/> lets the REAL
/// TEN_K__5D__INTERMEDIATE v1 candidate (the real RUN_LAYOUT_5D/dual-lane
/// progression/8-profile chain authored in FREQ.6D.4D.5) materialize through
/// the real skeleton -> stage-allocation -> calendar -> workout-binding
/// pipeline (<see cref="DynamicCoreWorkoutBindingOrchestrator"/>, the same
/// composition <see cref="RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogPreviewGenerator"/>
/// uses in production, stopping short of session-prescription/execution-
/// index resolution, which is out of this phase's scope). Loads the
/// candidate via <see cref="ICatalogCandidateEligibilityGate.LoadForInternalDryRunAsync"/>
/// (no lifecycle-status check, no public-routing consultation at all --
/// public activation stays fully dark, per this phase's own §47).
/// </summary>
public sealed class Freq6D4D5BReal5DDarkPlanTests
{
    private const string CandidateKey = "TEN_K__5D__INTERMEDIATE";
    private const int CandidateVersion = 1;

    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    private static async Task<PlanCatalogCandidateSummary> RealFiveDayCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(CandidateKey, CandidateVersion);
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

    // The real RUN_LAYOUT_5D shape's own eligible 5-of-7 weekday set used
    // throughout this phase's own worked examples (FREQ.6D.4D.5A §9).
    private static readonly DayOfWeek[] PreferredDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    private static DynamicCoreWorkoutBindingContext Context(
        PlanCatalogCandidateSummary candidate, int targetWeekCount, DateOnly startDate,
        DayOfWeek[]? preferredDays = null, DayOfWeek? longRunDay = null) => new()
    {
        Candidate = candidate,
        TargetWeekCount = targetWeekCount,
        StartDate = startDate,
        AsOfDate = startDate,
        PreferredDays = preferredDays ?? PreferredDays,
        LongRunDayPreference = longRunDay ?? LongRunDay,
        ConditionResults = new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
            RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "WITHIN_REALISTIC_BAND"),
        },
        WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(RealOptions())),
    };

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task RealFiveDayCandidate_DarkMaterialization_ProducesTwoDistinctKeySessionsPerWeek(int targetWeekCount)
    {
        var candidate = await RealFiveDayCandidateAsync();
        Assert.Equal(5, candidate.DaysPerWeek);

        var result = await RealOrchestrator().BindAsync(Context(candidate, targetWeekCount, new DateOnly(2026, 8, 3)));

        var datedSkeleton = result.DatedSkeleton;
        Assert.Equal(targetWeekCount, datedSkeleton.Weeks.Count);

        Assert.All(datedSkeleton.Weeks, week =>
        {
            Assert.Equal(5, week.SessionSlots.Count);

            var keySlots = week.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").OrderBy(s => s.SlotOrderInWeek).ToList();
            var easySlots = week.SessionSlots.Where(s => s.StructuralRole == "EASY_SUPPORT").ToList();
            var longSlot = week.SessionSlots.Single(s => s.StructuralRole == "LONG_RUN");

            Assert.Equal(2, keySlots.Count);
            Assert.Equal(2, easySlots.Count);
            Assert.NotEqual(keySlots[0].SessionDate, keySlots[1].SessionDate);

            // FREQ.6D.4D.5A frozen rule: KEY<->KEY >= 2 calendar days.
            Assert.True(Math.Abs(keySlots[0].SessionDate.DayNumber - keySlots[1].SessionDate.DayNumber) >= 2);

            // Existing, unchanged KEY<->LONG rule, independently for both KEY instances.
            Assert.All(keySlots, key => Assert.True(Math.Abs(key.SessionDate.DayNumber - longSlot.SessionDate.DayNumber) >= 2));

            Assert.Equal(LongRunDay, longSlot.SessionDayOfWeek);
        });

        // Independently re-validated by the real, unmodified, already-N>=1-
        // generalized output validator (FREQ.4) -- materializer and
        // validator must independently agree (defense in depth).
        var validator = new DatedGeneratedCatalogPlanSkeletonValidator();
        var validation = validator.Validate(datedSkeleton, PreferredDays, LongRunDay);
        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));

        // No silent 4D coercion: the real candidate/layout identity survived intact.
        Assert.Equal(CandidateKey, candidate.CandidateKey);
        Assert.Equal(CandidateVersion, candidate.CandidateVersion);
        Assert.NotEqual("TEN_K__4D__INTERMEDIATE", candidate.CandidateKey);

        // Real workout binding also succeeded (the layer immediately after
        // calendar materialization) -- proves this isn't calendar-only luck.
        Assert.Equal(targetWeekCount, result.BoundPlan.Weeks.Count);
    }

    [Fact]
    public async Task RealFiveDayCandidate_LaneOrdinalNotDerivedFromDate_FirstCanonicalSlotAlwaysEarlierDate()
    {
        var candidate = await RealFiveDayCandidateAsync();
        var result = await RealOrchestrator().BindAsync(Context(candidate, 12, new DateOnly(2026, 8, 3)));

        Assert.All(result.DatedSkeleton.Weeks, week =>
        {
            var keySlots = week.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").OrderBy(s => s.SlotOrderInWeek).ToList();
            // The lower SlotOrderInWeek slot (RUN_LAYOUT_5D's LaneOrdinal 0,
            // sequenceOrder=1) always receives the chronologically earlier
            // date, per the reused SlotOrderInWeek-ascending tie-break
            // (FREQ.6D.4D.5A §19/§20) -- a byproduct of slot order, not a
            // date-derived lane assignment.
            Assert.True(keySlots[0].SessionDate.DayNumber < keySlots[1].SessionDate.DayNumber);
        });

        // Real downstream binding: LaneOrdinal identity survives from the
        // structural binder (Split A), unrelated to and unaffected by this
        // calendar-date assignment.
        var laneOrdinals = result.BoundPlan.Weeks.SelectMany(w => w.Sessions)
            .Where(s => s.StructuralRole == "KEY_SESSION")
            .Select(s => s.LaneOrdinal)
            .Distinct()
            .OrderBy(o => o)
            .ToList();
        Assert.Equal(new int?[] { 0, 1 }, laneOrdinals);
    }

    [Fact]
    public async Task RealFiveDayCandidate_DeterministicAcrossRepeatedRuns()
    {
        var candidate = await RealFiveDayCandidateAsync();
        var context = Context(candidate, 12, new DateOnly(2026, 8, 3));

        var first = await RealOrchestrator().BindAsync(context);
        var second = await RealOrchestrator().BindAsync(context);

        var firstDates = first.DatedSkeleton.Weeks.SelectMany(w => w.SessionSlots.Select(s => (w.WeekNumber, s.SlotOrderInWeek, s.SessionDate)));
        var secondDates = second.DatedSkeleton.Weeks.SelectMany(w => w.SessionSlots.Select(s => (w.WeekNumber, s.SlotOrderInWeek, s.SessionDate)));

        Assert.Equal(firstDates, secondDates);
    }

    [Fact]
    public async Task RealFiveDayCandidate_TaperTwoKeyWeek_SpacingEnforced()
    {
        var candidate = await RealFiveDayCandidateAsync();
        var result = await RealOrchestrator().BindAsync(Context(candidate, 12, new DateOnly(2026, 8, 3)));

        var taperWeeks = result.DatedSkeleton.Weeks.Where(w => w.PhaseKey == "TAPER").ToList();
        Assert.NotEmpty(taperWeeks);
        Assert.All(taperWeeks, week =>
        {
            var keySlots = week.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").ToList();
            Assert.Equal(2, keySlots.Count);
            Assert.True(Math.Abs(keySlots[0].SessionDate.DayNumber - keySlots[1].SessionDate.DayNumber) >= 2);
        });
    }

    [Theory]
    [InlineData(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday }, DayOfWeek.Sunday)]
    [InlineData(new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday }, DayOfWeek.Sunday)]
    [InlineData(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday }, DayOfWeek.Sunday)]
    [InlineData(new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }, DayOfWeek.Sunday)]
    public async Task RealFiveDayCandidate_5ARepresentativePreferredDayMatrix_ProducesLegalAssignment(DayOfWeek[] preferredDays, DayOfWeek longRunDay)
    {
        // 4 of the 5 representative patterns FREQ.6D.4D.5A §9 hand-verified,
        // including the genuine combinatorial counterexample
        // (Tue/Wed/Thu/Sat/Sun, LONG=Sun) that actively rejected the
        // stricter >=3 alternative -- proving K2 (>=2, the approved rule) is
        // legal for it, across a real 12-week recurring plan, against the
        // REAL materializer, not a hand trace. (Mon/Wed/Thu/Sat/Sun is
        // covered separately below -- see
        // CrossWeekRefinesFiveA_MonWedThuSatSun_IsActuallyInfeasibleAcrossMultipleWeeks.)
        var candidate = await RealFiveDayCandidateAsync();
        var result = await RealOrchestrator().BindAsync(Context(candidate, 12, new DateOnly(2026, 8, 3), preferredDays, longRunDay));

        Assert.Equal(12, result.DatedSkeleton.Weeks.Count);
        var validator = new DatedGeneratedCatalogPlanSkeletonValidator();
        Assert.True(validator.Validate(result.DatedSkeleton, preferredDays, longRunDay).IsValid);
    }

    [Fact]
    public async Task CrossWeekRefinesFiveA_MonWedThuSatSun_IsActuallyInfeasibleAcrossMultipleWeeks()
    {
        // FREQ.6D.4D.5A §9 hand-verified Mon/Wed/Thu/Sat/Sun (LONG=Sun) as
        // feasible -- but that check was single-week-local, matching its own
        // explicitly disclosed scope ("a representative check, not an
        // exhaustive proof... appropriate for a decision phase, not an
        // implementation phase"). Real multi-week testing here found a
        // genuine cross-week interaction 5A did not check: Monday (offset 0)
        // is the only KEY candidate that pairs safely with either Wednesday
        // or Thursday within a single week, but Monday is only 1 calendar
        // day after the PRECEDING week's own Sunday long run (cross-week
        // distance = 1 < 2) -- so Monday is cross-week-unsafe for every week
        // except the first. That leaves only {Wednesday, Thursday} for weeks
        // 2+, and abs(Wed-Thu) = 1 < 2 violates the frozen KEY<->KEY rule.
        // No legal recurring 5D assignment exists for this exact preferred/
        // long-run combination -- a real, newly-discovered refinement of
        // 5A's feasibility picture, not a defect: the algorithm correctly
        // fails closed and typed rather than silently accepting an unsafe
        // week 1 and breaking down later.
        var candidate = await RealFiveDayCandidateAsync();
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday };

        var ex = await Assert.ThrowsAsync<DynamicCoreWorkoutBindingFailedException>(
            () => RealOrchestrator().BindAsync(Context(candidate, 12, new DateOnly(2026, 8, 3), days, DayOfWeek.Sunday)));

        Assert.IsType<CatalogPreferredDayConfigurationUnsafeException>(ex.InnerException);

        // A single, isolated week (no predecessor to conflict with) DOES
        // have a legal local assignment -- confirming this is genuinely a
        // cross-week effect, not a same-week regression. The full dynamic
        // Core pipeline has its own 8-week minimum-horizon feasibility floor
        // (unrelated to calendar spacing), so this isolation check goes
        // straight at the calendar materializer alone, the same way
        // CatalogWeekSkeletonCalendarMaterializerTests already does.
        var singleWeekSkeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(
            new DateOnly(2026, 8, 3), weekCount: 1,
            slotRoleOrder: new[] { "KEY_SESSION", "EASY_SUPPORT", "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" });
        var singleWeekContext = CatalogCalendarAssignmentFixtures.BuildContext(singleWeekSkeleton, days, DayOfWeek.Sunday);
        var singleWeekResult = new CatalogWeekSkeletonCalendarMaterializer().Materialize(singleWeekContext);
        Assert.Single(singleWeekResult.Weeks);
    }

    [Fact]
    public async Task RealFiveDayCandidate_NoLegalAssignment_ThrowsTypedFailure_NeverSilentCoercion()
    {
        // Mon/Tue/Wed/Thu/Fri preferred, LONG=Tuesday: after excluding
        // Mon/Wed (too close to Tue's LONG_RUN under the unchanged
        // KEY<->LONG rule), only {Thu, Fri} remain KEY-eligible, and
        // abs(Thu-Fri)=1 < 2 -- no legal KEY pair exists. Must fail typed
        // and closed, never drop a KEY, never move LONG, never use an
        // unpreferred date, never fall back to a 4D candidate/layout.
        var candidate = await RealFiveDayCandidateAsync();
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        var ex = await Assert.ThrowsAsync<DynamicCoreWorkoutBindingFailedException>(
            () => RealOrchestrator().BindAsync(Context(candidate, 12, new DateOnly(2026, 8, 3), days, DayOfWeek.Tuesday)));

        Assert.IsType<CatalogPreferredDayConfigurationUnsafeException>(ex.InnerException);
        Assert.DoesNotContain("TEN_K__4D__INTERMEDIATE", ex.Message);
    }
}
