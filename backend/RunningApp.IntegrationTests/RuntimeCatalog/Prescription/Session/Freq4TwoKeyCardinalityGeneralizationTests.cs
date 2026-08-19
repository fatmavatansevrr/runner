using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Session;

/// <summary>
/// Phase 10K-FREQ.4 -- synthetic N=2 KEY_SESSION coverage for the mechanisms
/// generalized in this phase. No RUN_LAYOUT_5D exists yet (deliberately not
/// created by this phase), so these exercise the generalized mechanisms
/// directly with hand-constructed 2-KEY input, not through the public API
/// or a real candidate/orchestration pipeline.
/// </summary>
public sealed class Freq4TwoKeyCardinalityGeneralizationTests
{
    // ── Section A: FourDaySessionDistanceAllocationPolicy, N=2 KEY ────────

    [Fact]
    public void FourDaySessionDistanceAllocation_KeySessionCountTwo_SplitsKeyShareEvenlyAndReconciles()
    {
        var result = FourDaySessionDistanceAllocationPolicy.Allocate(weeklyVolumeKm: 24d, longRunDistanceKm: 9.5d, keySessionCount: 2);

        Assert.Equal(2, result.KeySessionDistancesKm.Count);
        Assert.All(result.KeySessionDistancesKm, k => Assert.True(k > 0));
        Assert.All(result.KeySessionDistancesKm, k => Assert.Equal(0d, k * 2d % 1d)); // 0.5km granularity
        var total = result.KeySessionDistancesKm.Sum() + result.FirstEasySupportDistanceKm + result.SecondEasySupportDistanceKm + result.LongRunDistanceKm;
        Assert.Equal(24d, total);
        // Back-compat accessor still resolves to the first instance.
        Assert.Equal(result.KeySessionDistancesKm[0], result.KeySessionDistanceKm);
    }

    [Fact]
    public void FourDaySessionDistanceAllocation_KeySessionCountOne_IsByteIdenticalToPreFreq4Default()
    {
        var viaDefault = FourDaySessionDistanceAllocationPolicy.Allocate(24d, 9.5d);
        var viaExplicitOne = FourDaySessionDistanceAllocationPolicy.Allocate(24d, 9.5d, keySessionCount: 1);

        Assert.Equal(viaDefault, viaExplicitOne);
        Assert.Single(viaDefault.KeySessionDistancesKm);
    }

    [Fact]
    public void FourDaySessionDistanceAllocation_KeySessionCountTwo_RequiredMinimumDoubles()
    {
        // requiredMinimum = 2*3.0 (KEY) + 2*1.5 (EASY) = 9.0km; a residual
        // just below that must fail closed for keySessionCount=2 even though
        // it would have been feasible for keySessionCount=1 (min 3.0+3.0=6.0km).
        Assert.Throws<CatalogSessionPrescriptionInfeasibleException>(() =>
            FourDaySessionDistanceAllocationPolicy.Allocate(weeklyVolumeKm: 8.5d, longRunDistanceKm: 0d, keySessionCount: 2));
    }

    // ── Section B: DatedGeneratedCatalogPlanSkeletonValidator, N=2 KEY ────

    [Fact]
    public void SkeletonValidator_TwoKeySessionsCorrectlySpaced_NoRoleCountOrSeparationError()
    {
        var skeleton = BuildSkeleton(
            keyDates: [new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 8)],   // Mon, Thu -- 3 days apart
            longDate: new DateOnly(2026, 1, 11),                              // Sun -- 3 days from second KEY
            easyDates: [new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 9)]); // Tue, Fri

        var result = new DatedGeneratedCatalogPlanSkeletonValidator().Validate(
            skeleton,
            preferredDays: [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday],
            longRunDayPreference: DayOfWeek.Sunday);

        Assert.DoesNotContain(DatedGeneratedCatalogPlanSkeletonValidationError.RoleCountIncorrect, result.Errors);
        Assert.DoesNotContain(DatedGeneratedCatalogPlanSkeletonValidationError.KeySessionLongRunSeparationViolated, result.Errors);
        Assert.DoesNotContain(DatedGeneratedCatalogPlanSkeletonValidationError.KeySessionKeySessionSeparationViolated, result.Errors);
    }

    [Fact]
    public void SkeletonValidator_TwoKeySessionsTooClose_FlagsKeySessionKeySessionSeparationViolated()
    {
        var skeleton = BuildSkeleton(
            keyDates: [new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6)], // Mon, Tue -- 1 day apart (< 2-day minimum)
            longDate: new DateOnly(2026, 1, 11),
            easyDates: [new DateOnly(2026, 1, 8), new DateOnly(2026, 1, 9)]);

        var result = new DatedGeneratedCatalogPlanSkeletonValidator().Validate(
            skeleton,
            preferredDays: [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday],
            longRunDayPreference: DayOfWeek.Sunday);

        Assert.Contains(DatedGeneratedCatalogPlanSkeletonValidationError.KeySessionKeySessionSeparationViolated, result.Errors);
    }

    [Fact]
    public void SkeletonValidator_SecondKeySessionTooCloseToLongRun_IsNoLongerSilentlySkipped()
    {
        // Regression coverage for the pre-FREQ.4 FirstOrDefault bug (FREQ.3 §F):
        // the SECOND KEY session (not the first) violates KEY<->LONG separation.
        // Before FREQ.4 this would never have been checked at all.
        var skeleton = BuildSkeleton(
            keyDates: [new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10)], // Mon (fine), Sat (1 day from Sun long run)
            longDate: new DateOnly(2026, 1, 11),                             // Sun
            easyDates: [new DateOnly(2026, 1, 7), new DateOnly(2026, 1, 8)]);

        var result = new DatedGeneratedCatalogPlanSkeletonValidator().Validate(
            skeleton,
            preferredDays: [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday],
            longRunDayPreference: DayOfWeek.Sunday);

        Assert.Contains(DatedGeneratedCatalogPlanSkeletonValidationError.KeySessionLongRunSeparationViolated, result.Errors);
    }

    private static DatedGeneratedCatalogPlanSkeleton BuildSkeleton(
        IReadOnlyList<DateOnly> keyDates, DateOnly longDate, IReadOnlyList<DateOnly> easyDates)
    {
        var weekStart = new DateOnly(2026, 1, 5);
        var slots = new List<DatedGeneratedCatalogSessionSlotSkeleton>();
        var order = 0;
        foreach (var date in keyDates)
            slots.Add(Slot(order++, "KEY_SESSION", date));
        foreach (var date in easyDates)
            slots.Add(Slot(order++, "EASY_SUPPORT", date));
        slots.Add(Slot(order, "LONG_RUN", longDate));

        var week = new DatedGeneratedCatalogWeekSkeleton(
            WeekNumber: 1, StartDate: weekStart, EndDate: weekStart.AddDays(6),
            PhaseKey: "BUILD", PhaseWeekIndex: 1, PhaseWeekCount: 1,
            SessionSlots: slots,
            Provenance: new CatalogWeekCalendarProvenance(1, "BUILD", 1, CatalogCalendarAssignmentPolicy.RaceHardConstraint));

        return new DatedGeneratedCatalogPlanSkeleton(
            SchemaVersion: DatedGeneratedCatalogPlanSkeleton.CurrentSchemaVersion,
            StartDate: weekStart, EndDate: weekStart.AddDays(6), PlannedWeekCount: 1,
            Weeks: [week],
            Provenance: new CatalogCalendarMaterializationProvenance(
                "TEST_CANDIDATE", 1, weekStart, weekStart, [DayOfWeek.Monday], DayOfWeek.Sunday,
                CatalogCalendarDayMaterializerVersion.V1, 1, new Dictionary<string, PlanCatalogReference>()));
    }

    private static DatedGeneratedCatalogSessionSlotSkeleton Slot(int order, string role, DateOnly date) =>
        new(order, $"SLOT_{order}", role, date, date.DayOfWeek,
            new CatalogSessionCalendarProvenance($"SLOT_{order}", role, date.DayOfWeek, date, "TEST"));

    // ── Section D: WindowExecutionSummaryBuilder, N=2 KEY roots ────────────

    [Fact]
    public void WindowExecutionSummaryBuilder_TwoKeyRoots_OneCompleted_ReportsCountsNotLossyBoolean()
    {
        var evidence = new[]
        {
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.ScheduleConflict),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.LongRun, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active),
        };

        var summary = WindowExecutionSummaryBuilder.Build(evidence);

        Assert.Equal(2, summary.KeySessionExpectedCount);
        Assert.Equal(1, summary.KeySessionCompletedCount);
        // The lossy pre-FREQ.4 boolean now correctly reports "not fully satisfied"
        // for a 1-of-2 week, rather than being indistinguishable from 0-of-2.
        Assert.True(summary.KeySessionExpected);
        Assert.False(summary.KeySessionCompleted);
        Assert.Equal(4, summary.EffectiveCompletedCount);
    }

    [Fact]
    public void WindowExecutionSummaryBuilder_OneKeyRoot_MatchesPreFreq4BooleanSemanticsExactly()
    {
        var completedEvidence = new[] { new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active) };
        var missedEvidence = new[] { new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.ScheduleConflict) };

        var completedSummary = WindowExecutionSummaryBuilder.Build(completedEvidence);
        var missedSummary = WindowExecutionSummaryBuilder.Build(missedEvidence);

        Assert.True(completedSummary.KeySessionExpected);
        Assert.True(completedSummary.KeySessionCompleted);
        Assert.True(missedSummary.KeySessionExpected);
        Assert.False(missedSummary.KeySessionCompleted);
    }

    // ── Section E: NextWindowLoadDecisionPolicy, N=2 KEY count-aware branch ─

    [Fact]
    public void OnlyEasyMissingBranch_TwoKeyOneMissingBothEasyDone_FourCompleted_DoesNotProgressAsPlanned()
    {
        // 4 of 5 completed: both EASY done, LONG done, ONE of two KEYs done
        // (the other KEY missing). Pre-FREQ.4-equivalent single-KEY logic
        // would have called this "only Easy missing" (since the lossy
        // boolean can't see a partial KEY completion) and wrongly returned
        // ProgressAsPlanned. Post-FREQ.4 it must not.
        //
        // Phase 10K-FREQ.6D.4D Split D: EffectiveCompletedCount corrected
        // from this test's original value of 3 to 4 -- the role fields below
        // (2 EASY + 1 LONG + 1 KEY = 4) always summed to 4, not 3; the
        // original literal predates the real, frozen FREQ.6 24-row table
        // (which places the role-aware branch at count 4 for a genuine
        // 5-session week, not count 3) and NextWindowLoadDecisionPolicy now
        // validates this sum for any ExpectedSessionCount==5 summary,
        // fail-closed, rather than silently accepting an inconsistent one.
        // The originally-asserted outcome (Maintain) is unchanged and is now
        // exactly FREQ.6 §6 row 12/18 (count=4, sole miss one KEY lane).
        var summary = new WindowExecutionSummary(
            ExpectedSessionCount: 5,
            EffectiveCompletedCount: 4,
            KeySessionExpectedCount: 2,
            KeySessionCompletedCount: 1,
            LongRunExpected: true,
            LongRunCompleted: true,
            EasyExpectedCount: 2,
            EasyCompletedCount: 2,
            UnrecoveredNotTodayCount: 1,
            SupersededByAdaptationCount: 0,
            HasSafetyFlag: false);

        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);

        Assert.Equal(NextWindowLoadDecision.Maintain, result.LoadDecision);
    }

    [Fact]
    public void OnlyEasyMissingBranch_SingleKeyExpected_ThreeCompletedOnlyEasyMissing_StillProgressesAsPlanned()
    {
        // Pre-FREQ.4 no-op check: single-KEY week, KEY+Long done, one Easy
        // missing -- must still classify as ProgressAsPlanned exactly as before.
        var summary = new WindowExecutionSummary(
            ExpectedSessionCount: 4,
            EffectiveCompletedCount: 3,
            KeySessionExpectedCount: 1,
            KeySessionCompletedCount: 1,
            LongRunExpected: true,
            LongRunCompleted: true,
            EasyExpectedCount: 2,
            EasyCompletedCount: 1,
            UnrecoveredNotTodayCount: 1,
            SupersededByAdaptationCount: 0,
            HasSafetyFlag: false);

        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);

        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, result.LoadDecision);
    }
}
