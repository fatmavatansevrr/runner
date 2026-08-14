using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.1 -- exhaustive pure-decision test matrix for the Appsel
/// Adaptation V1 domain model (appsel-adaptation-v1-canonical-spec-.md,
/// Revision 3.1). No database, no HTTP host, no shared state -- every test
/// calls the pure static policies directly, so this class does not join
/// <c>ApiIntegrationTestCollection</c> and runs fully in parallel with the
/// rest of the suite.
/// </summary>
public sealed class PlanAdaptationV1DecisionTests
{
    private static readonly AdaptationPhaseIdentity PhaseA = new(LongHorizonPersistedSegmentType.Core, "Build");
    private static readonly AdaptationPhaseIdentity PhaseB = new(LongHorizonPersistedSegmentType.Core, "Taper");
    private static readonly DateOnly D1 = new(2026, 8, 10);
    private static readonly DateOnly D2 = new(2026, 8, 12);
    private static readonly DateOnly D3 = new(2026, 8, 14);

    private static ScheduleRepairTrigger Trigger(
        PreparationRunwaySlotRole role,
        NotTodayReasonCode reason,
        AdaptationPhaseIdentity? phase = null,
        bool isTaper = false,
        LongHorizonRollingSessionOutcomeStatus outcome = LongHorizonRollingSessionOutcomeStatus.NotToday) =>
        new(Guid.NewGuid(), role, phase ?? PhaseA, isTaper, reason, outcome);

    private static ScheduleRepairCandidate EmptySlot(DateOnly date, bool valid = true, AdaptationPhaseIdentity? phase = null) =>
        new(date, phase ?? PhaseA, valid);

    private static ScheduleRepairCandidate EasyCandidate(DateOnly date, bool valid = true, AdaptationPhaseIdentity? phase = null,
        SessionPlanningStatus status = SessionPlanningStatus.Active, PreparationRunwaySlotRole role = PreparationRunwaySlotRole.EasySupport) =>
        new(date, phase ?? PhaseA, valid, Guid.NewGuid(), status, role);

    // ── REASON CLASSIFICATION (1-10) ────────────────────────────────────────

    [Fact] // 1
    public void PainOrDiscomfort_ClassifiesAsSafety() =>
        Assert.Equal(ReasonClass.Safety, ReasonClassificationPolicy.Classify(NotTodayReasonCode.PainOrDiscomfort));

    [Fact] // 2
    public void PainOrDiscomfort_BlocksRepair() =>
        Assert.True(ReasonClassificationPolicy.BlocksReschedule(NotTodayReasonCode.PainOrDiscomfort));

    [Fact] // 3
    public void PainOrDiscomfort_TriggersSafetyFlag() =>
        Assert.True(ReasonClassificationPolicy.TriggersSafetyFlag(NotTodayReasonCode.PainOrDiscomfort));

    [Fact] // 4
    public void Illness_ClassifiesAsOperational() =>
        Assert.Equal(ReasonClass.Operational, ReasonClassificationPolicy.Classify(NotTodayReasonCode.Illness));

    [Fact] // 5
    public void Illness_BlocksRepair() =>
        Assert.True(ReasonClassificationPolicy.BlocksReschedule(NotTodayReasonCode.Illness));

    [Fact] // 6
    public void Illness_DoesNotTriggerSafetyFlag() =>
        Assert.False(ReasonClassificationPolicy.TriggersSafetyFlag(NotTodayReasonCode.Illness));

    [Theory] // 7-10
    [InlineData(NotTodayReasonCode.ScheduleConflict)]
    [InlineData(NotTodayReasonCode.Travel)]
    [InlineData(NotTodayReasonCode.Weather)]
    [InlineData(NotTodayReasonCode.Tired)]
    internal void OperationalReasons_ClassifyOperationalAndDoNotBlockRepair(NotTodayReasonCode reason)
    {
        Assert.Equal(ReasonClass.Operational, ReasonClassificationPolicy.Classify(reason));
        Assert.False(ReasonClassificationPolicy.BlocksReschedule(reason));
        Assert.False(ReasonClassificationPolicy.TriggersSafetyFlag(reason));
    }

    // ── SCHEDULE REPAIR — EASY (11-13) ───────────────────────────────────────

    [Fact] // 11
    public void Easy_NotToday_Skips()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.EasySupport, NotTodayReasonCode.Weather),
            [EmptySlot(D1)], [EasyCandidate(D1)]);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 12, 13
    public void Easy_NotToday_IgnoresCandidatesEntirely()
    {
        // Candidates deliberately valid/available -- if EASY inspected them
        // it would reschedule/substitute; it must Skip regardless.
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.EasySupport, NotTodayReasonCode.Weather),
            [EmptySlot(D1), EmptySlot(D2)], [EasyCandidate(D3)]);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
        Assert.Null(decision.SelectedEmptySlotDate);
        Assert.Null(decision.SubstitutedEasySessionId);
    }

    // ── SCHEDULE REPAIR — KEY (14-20) ────────────────────────────────────────

    [Fact] // 14
    public void Key_EarliestValidEmptyCandidate_Reschedules()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            [EmptySlot(D2), EmptySlot(D1)], []);
        Assert.Equal(ScheduleRepairAction.RescheduleToEmptySlot, decision.Action);
        Assert.Equal(D1, decision.SelectedEmptySlotDate);
    }

    [Fact] // 15
    public void Key_NoEmptyCandidate_SubstitutesFutureEasy()
    {
        var easy = EasyCandidate(D1);
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            [], [easy]);
        Assert.Equal(ScheduleRepairAction.SubstituteFutureEasy, decision.Action);
        Assert.Equal(easy.SourceSessionId, decision.SubstitutedEasySessionId);
    }

    [Fact] // 16
    public void Key_NoCandidateAtAll_Skips()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict), [], []);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 17
    public void Key_Pain_SkipsDespiteValidCandidate()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.PainOrDiscomfort),
            [EmptySlot(D1)], [EasyCandidate(D1)]);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
        Assert.True(decision.SafetyFlag);
    }

    [Fact] // 18
    public void Key_Illness_SkipsDespiteValidCandidate()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.Illness),
            [EmptySlot(D1)], [EasyCandidate(D1)]);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
        Assert.False(decision.SafetyFlag);
    }

    [Fact] // 19
    public void Key_CannotSubstituteLong()
    {
        var longCandidate = EasyCandidate(D1, role: PreparationRunwaySlotRole.LongRun);
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            [], [longCandidate]);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 20
    public void Key_CannotSubstituteKey()
    {
        var keyCandidate = EasyCandidate(D1, role: PreparationRunwaySlotRole.KeySession);
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            [], [keyCandidate]);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    // ── SCHEDULE REPAIR — LONG (21-28) ───────────────────────────────────────

    [Fact] // 21
    public void Long_EarliestValidEmptyCandidate_Reschedules()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.LongRun, NotTodayReasonCode.Tired),
            [EmptySlot(D2), EmptySlot(D1)], []);
        Assert.Equal(ScheduleRepairAction.RescheduleToEmptySlot, decision.Action);
        Assert.Equal(D1, decision.SelectedEmptySlotDate);
    }

    [Fact] // 22
    public void Long_NoEmptyCandidate_SubstitutesFutureEasy()
    {
        var easy = EasyCandidate(D1);
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.LongRun, NotTodayReasonCode.Tired), [], [easy]);
        Assert.Equal(ScheduleRepairAction.SubstituteFutureEasy, decision.Action);
        Assert.Equal(easy.SourceSessionId, decision.SubstitutedEasySessionId);
    }

    [Fact] // 23
    public void Long_NoCandidateAtAll_Skips()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.LongRun, NotTodayReasonCode.Tired), [], []);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 24
    public void Long_Pain_Skips()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.LongRun, NotTodayReasonCode.PainOrDiscomfort),
            [EmptySlot(D1)], []);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
        Assert.True(decision.SafetyFlag);
    }

    [Fact] // 25
    public void Long_Illness_Skips()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.LongRun, NotTodayReasonCode.Illness),
            [EmptySlot(D1)], []);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
        Assert.False(decision.SafetyFlag);
    }

    [Fact] // 26
    public void Long_InTaper_SkipsRegardlessOfCandidates()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.LongRun, NotTodayReasonCode.ScheduleConflict, isTaper: true),
            [EmptySlot(D1)], [EasyCandidate(D1)]);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 27
    public void Long_CannotSubstituteKey()
    {
        var keyCandidate = EasyCandidate(D1, role: PreparationRunwaySlotRole.KeySession);
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.LongRun, NotTodayReasonCode.Tired), [], [keyCandidate]);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 28
    public void Long_CannotSubstituteLong()
    {
        var longCandidate = EasyCandidate(D1, role: PreparationRunwaySlotRole.LongRun);
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.LongRun, NotTodayReasonCode.Tired), [], [longCandidate]);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    // ── TAPER (29-32) ─────────────────────────────────────────────────────

    [Fact] // 29
    public void Taper_Easy_Skips()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.EasySupport, NotTodayReasonCode.Weather, isTaper: true),
            [EmptySlot(D1)], []);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 30
    public void Taper_Long_Skips()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.LongRun, NotTodayReasonCode.Weather, isTaper: true),
            [EmptySlot(D1)], []);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 31
    public void Taper_Key_MaySelectValidCandidate()
    {
        // TaperProtectionRule allows a KEY rehearsal to be moved (unchanged)
        // during Taper -- only EASY/LONG are hard-Skip in Taper.
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict, isTaper: true),
            [EmptySlot(D1)], []);
        Assert.Equal(ScheduleRepairAction.RescheduleToEmptySlot, decision.Action);
    }

    [Fact] // 32
    public void Taper_Key_DecisionCarriesNoContentModification()
    {
        // Phase 4M.1 boundary: the pure decision surface offers only a date
        // (RescheduleToEmptySlot) or a target session id (SubstituteFutureEasy)
        // -- there is no field through which distance/duration/intensity could
        // be modified, so the "moved unchanged" invariant is structurally
        // satisfied by the contract shape itself. Exact materialized-content
        // equality enforcement is explicitly deferred to Phase 4M.2.
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict, isTaper: true),
            [EmptySlot(D1)], []);
        Assert.Equal(D1, decision.SelectedEmptySlotDate);
    }

    // ── CANDIDATE SELECTION (33-37) ──────────────────────────────────────────

    [Fact] // 33
    public void CandidateSelection_EarliestChronologicalValidWins()
    {
        var candidates = new[] { EmptySlot(D3), EmptySlot(D1), EmptySlot(D2) };
        var result = CandidateSelectionPolicy.SelectEarliestValid(candidates);
        Assert.Equal(D1, result!.Date);
    }

    [Fact] // 34, 35
    public void CandidateSelection_SkipsInvalidThenSelectsNextValid()
    {
        var candidates = new[] { EmptySlot(D1, valid: false), EmptySlot(D2, valid: true), EmptySlot(D3, valid: true) };
        var result = CandidateSelectionPolicy.SelectEarliestValid(candidates);
        Assert.Equal(D2, result!.Date);
    }

    [Fact] // 36
    public void CandidateSelection_NoValidCandidate_ReturnsNull()
    {
        var candidates = new[] { EmptySlot(D1, valid: false), EmptySlot(D2, valid: false) };
        Assert.Null(CandidateSelectionPolicy.SelectEarliestValid(candidates));
    }

    [Fact] // 37
    public void CandidateSelection_InputOrderingDoesNotChangeDeterministicResult()
    {
        var a = new[] { EmptySlot(D3), EmptySlot(D1), EmptySlot(D2) };
        var b = new[] { EmptySlot(D2), EmptySlot(D3), EmptySlot(D1) };
        Assert.Equal(CandidateSelectionPolicy.SelectEarliestValid(a)!.Date, CandidateSelectionPolicy.SelectEarliestValid(b)!.Date);
    }

    // ── TRIGGER VALIDATION (38) ───────────────────────────────────────────

    [Fact] // 38
    public void NonNotTodayTrigger_IsRejected()
    {
        var trigger = Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict,
            outcome: LongHorizonRollingSessionOutcomeStatus.Planned);
        Assert.Throws<ScheduleRepairTriggerInvalidException>(() => ScheduleRepairPolicy.Evaluate(trigger, [], []));
    }

    // ── WINDOW EXECUTION SUMMARY (39-54) ─────────────────────────────────

    private static LogicalSessionEvidence Session(
        PreparationRunwaySlotRole role,
        LongHorizonRollingSessionOutcomeStatus outcome,
        SessionPlanningStatus status = SessionPlanningStatus.Active,
        Guid? adaptedFromId = null,
        NotTodayReasonCode? reason = null) =>
        new(Guid.NewGuid(), role, outcome, status, adaptedFromId, reason);

    [Fact] // 39
    public void Summary_Normal4Of4Completion()
    {
        var summary = WindowExecutionSummaryBuilder.Build([
            Session(PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Completed),
            Session(PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed),
            Session(PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Completed),
            Session(PreparationRunwaySlotRole.LongRun, LongHorizonRollingSessionOutcomeStatus.Completed),
        ]);
        Assert.Equal(4, summary.ExpectedSessionCount);
        Assert.Equal(4, summary.EffectiveCompletedCount);
    }

    [Fact] // 40
    public void Summary_3Of4_OnlyEasyMissing()
    {
        var summary = WindowExecutionSummaryBuilder.Build([
            Session(PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Completed),
            Session(PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed),
            Session(PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.NotToday),
            Session(PreparationRunwaySlotRole.LongRun, LongHorizonRollingSessionOutcomeStatus.Completed),
        ]);
        Assert.Equal(3, summary.EffectiveCompletedCount);
        Assert.True(summary.KeySessionCompleted);
        Assert.True(summary.LongRunCompleted);
        Assert.Equal(1, summary.EasyCompletedCount);
    }

    [Fact] // 41
    public void Summary_RecoveredKey_CountsAsCompleted()
    {
        var keyMissed = Guid.NewGuid();
        var summary = WindowExecutionSummaryBuilder.Build([
            new LogicalSessionEvidence(keyMissed, PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.ScheduleConflict),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active, AdaptedFromId: keyMissed),
        ]);
        Assert.True(summary.KeySessionCompleted);
        Assert.Equal(1, summary.EffectiveCompletedCount);
        Assert.Equal(1, summary.ExpectedSessionCount); // one logical root; the replacement does not add a second expectation (see test 44)
    }

    [Fact] // 42
    public void Summary_RecoveredLong_CountsAsCompleted()
    {
        var longMissed = Guid.NewGuid();
        var summary = WindowExecutionSummaryBuilder.Build([
            new LogicalSessionEvidence(longMissed, PreparationRunwaySlotRole.LongRun, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.Tired),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.LongRun, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active, AdaptedFromId: longMissed),
        ]);
        Assert.True(summary.LongRunCompleted);
        Assert.Equal(1, summary.EffectiveCompletedCount);
    }

    [Fact] // 43, 44
    public void Summary_ReplacementLineage_CountsAsOneExpectedSession_NotTwo()
    {
        var missed = Guid.NewGuid();
        var summary = WindowExecutionSummaryBuilder.Build([
            new LogicalSessionEvidence(missed, PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.ScheduleConflict),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active, AdaptedFromId: missed),
        ]);
        Assert.Equal(1, summary.ExpectedSessionCount); // one logical KEY expectation, not two rows
    }

    [Fact] // 45, 46, 47, 48, 49
    public void Summary_SupersededEasy_RemainsInDenominator_NeverCompletedOrNotToday_IncrementsSupersededCount()
    {
        var summary = WindowExecutionSummaryBuilder.Build([
            Session(PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Planned, SessionPlanningStatus.Superseded),
        ]);
        Assert.Equal(1, summary.ExpectedSessionCount);
        Assert.Equal(1, summary.EasyExpectedCount);
        Assert.Equal(0, summary.EasyCompletedCount);
        Assert.Equal(0, summary.UnrecoveredNotTodayCount);
        Assert.Equal(1, summary.SupersededByAdaptationCount);
    }

    [Fact] // 50
    public void Summary_SupersededCount_IsInformationalOnly_DoesNotWorsenLoadDecision()
    {
        // 4/4-equivalent: Key, Long and both Easys effectively satisfied
        // (one Easy directly, one Easy superseded in favor of a recovered
        // Key) must not be dragged down by the superseded count.
        var missedKey = Guid.NewGuid();
        var summary = WindowExecutionSummaryBuilder.Build([
            Session(PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Completed),
            new LogicalSessionEvidence(missedKey, PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.ScheduleConflict),
            Session(PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Planned, SessionPlanningStatus.Superseded),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active, AdaptedFromId: missedKey),
            Session(PreparationRunwaySlotRole.LongRun, LongHorizonRollingSessionOutcomeStatus.Completed),
        ]);
        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, result.LoadDecision);
    }

    [Fact] // 51
    public void Summary_RecoveredNotToday_IsNotUnrecovered()
    {
        var missed = Guid.NewGuid();
        var summary = WindowExecutionSummaryBuilder.Build([
            new LogicalSessionEvidence(missed, PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.ScheduleConflict),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active, AdaptedFromId: missed),
        ]);
        Assert.Equal(0, summary.UnrecoveredNotTodayCount);
    }

    [Fact] // 52
    public void Summary_UnrecoveredNotToday_IsCounted()
    {
        var summary = WindowExecutionSummaryBuilder.Build([
            Session(PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, reason: NotTodayReasonCode.Illness),
        ]);
        Assert.Equal(1, summary.UnrecoveredNotTodayCount);
    }

    [Fact] // 53
    public void Summary_PainInWindow_SetsHasSafetyFlag()
    {
        var summary = WindowExecutionSummaryBuilder.Build([
            Session(PreparationRunwaySlotRole.LongRun, LongHorizonRollingSessionOutcomeStatus.NotToday, reason: NotTodayReasonCode.PainOrDiscomfort),
        ]);
        Assert.True(summary.HasSafetyFlag);
    }

    [Fact] // 54
    public void Summary_PriorSuccessfulRepair_RemainsCompletedAfterLaterSafetyEvent()
    {
        var missedKey = Guid.NewGuid();
        var summary = WindowExecutionSummaryBuilder.Build([
            new LogicalSessionEvidence(missedKey, PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.ScheduleConflict),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active, AdaptedFromId: missedKey),
            Session(PreparationRunwaySlotRole.LongRun, LongHorizonRollingSessionOutcomeStatus.NotToday, reason: NotTodayReasonCode.PainOrDiscomfort),
        ]);
        Assert.True(summary.KeySessionCompleted);
        Assert.True(summary.HasSafetyFlag);
    }

    // ── REV3.1 LOCKED SCENARIO (55) ──────────────────────────────────────

    [Fact] // 55
    public void LockedScenario_Rev3_1_ExactExpectedValues()
    {
        var wedKey = Guid.NewGuid();
        var friEasyOriginal = Guid.NewGuid();

        var summary = WindowExecutionSummaryBuilder.Build([
            Session(PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Completed),                                   // Mon Easy Completed
            new LogicalSessionEvidence(wedKey, PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.ScheduleConflict), // Wed Key NotToday
            new LogicalSessionEvidence(friEasyOriginal, PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Planned, SessionPlanningStatus.Superseded), // Fri Easy Superseded
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active, AdaptedFromId: wedKey), // Fri Key replacement Completed
            Session(PreparationRunwaySlotRole.LongRun, LongHorizonRollingSessionOutcomeStatus.Completed),                                        // Sun Long Completed
        ]);

        Assert.Equal(4, summary.ExpectedSessionCount);
        Assert.Equal(3, summary.EffectiveCompletedCount);
        Assert.True(summary.KeySessionExpected);
        Assert.True(summary.KeySessionCompleted);
        Assert.True(summary.LongRunExpected);
        Assert.True(summary.LongRunCompleted);
        Assert.Equal(2, summary.EasyExpectedCount);
        Assert.Equal(1, summary.EasyCompletedCount);
        Assert.Equal(1, summary.SupersededByAdaptationCount);
        Assert.Equal(0, summary.UnrecoveredNotTodayCount);
    }

    // ── LINEAGE VALIDATION (56-59) ────────────────────────────────────────

    [Fact] // 56
    public void Lineage_OneDirectReplacementChild_IsValid()
    {
        var source = Guid.NewGuid();
        var summary = WindowExecutionSummaryBuilder.Build([
            new LogicalSessionEvidence(source, PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.ScheduleConflict),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active, AdaptedFromId: source),
        ]);
        Assert.Equal(1, summary.ExpectedSessionCount);
    }

    [Fact] // 57, 59
    public void Lineage_TwoDirectReplacementChildren_FailsFast_DoesNotSilentlyChooseFirst()
    {
        var source = Guid.NewGuid();
        var evidence = new[]
        {
            new LogicalSessionEvidence(source, PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, NotTodayReason: NotTodayReasonCode.ScheduleConflict),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active, AdaptedFromId: source),
            new LogicalSessionEvidence(Guid.NewGuid(), PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.Completed, SessionPlanningStatus.Active, AdaptedFromId: source),
        };
        Assert.Throws<AdaptationLineageInvalidException>(() => WindowExecutionSummaryBuilder.Build(evidence));
    }

    [Fact] // 58
    public void Lineage_Cycle_FailsFast()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var evidence = new[]
        {
            new LogicalSessionEvidence(a, PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, AdaptedFromId: b, NotTodayReason: NotTodayReasonCode.ScheduleConflict),
            new LogicalSessionEvidence(b, PreparationRunwaySlotRole.KeySession, LongHorizonRollingSessionOutcomeStatus.NotToday, SessionPlanningStatus.Active, AdaptedFromId: a, NotTodayReason: NotTodayReasonCode.ScheduleConflict),
        };
        Assert.Throws<AdaptationLineageInvalidException>(() => WindowExecutionSummaryBuilder.Build(evidence));
    }

    // ── NEXT WINDOW LOAD DECISION (60-70) ─────────────────────────────────

    private static WindowExecutionSummary FourSessionSummary(int effectiveCompleted, bool keyCompleted, bool longCompleted, int easyCompleted, bool safetyFlag = false) =>
        new(
            ExpectedSessionCount: 4,
            EffectiveCompletedCount: effectiveCompleted,
            KeySessionExpected: true,
            KeySessionCompleted: keyCompleted,
            LongRunExpected: true,
            LongRunCompleted: longCompleted,
            EasyExpectedCount: 2,
            EasyCompletedCount: easyCompleted,
            UnrecoveredNotTodayCount: 4 - effectiveCompleted,
            SupersededByAdaptationCount: 0,
            HasSafetyFlag: safetyFlag);

    [Fact] // 60
    public void LoadDecision_0Of4_Reduces() =>
        Assert.Equal(NextWindowLoadDecision.Reduce, NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(0, false, false, 0)).LoadDecision);

    [Fact] // 61
    public void LoadDecision_1Of4_Reduces() =>
        Assert.Equal(NextWindowLoadDecision.Reduce, NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(1, false, false, 1)).LoadDecision);

    [Fact] // 62
    public void LoadDecision_2Of4_Maintains() =>
        Assert.Equal(NextWindowLoadDecision.Maintain, NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(2, true, true, 0)).LoadDecision);

    [Fact] // 63
    public void LoadDecision_3Of4_OnlyEasyMissing_ProgressesAsPlanned() =>
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(3, true, true, 1)).LoadDecision);

    [Fact] // 64
    public void LoadDecision_3Of4_KeyMissing_Maintains() =>
        Assert.Equal(NextWindowLoadDecision.Maintain, NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(3, false, true, 2)).LoadDecision);

    [Fact] // 65
    public void LoadDecision_3Of4_LongMissing_Maintains() =>
        Assert.Equal(NextWindowLoadDecision.Maintain, NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(3, true, false, 2)).LoadDecision);

    [Fact] // 66
    public void LoadDecision_4Of4_ProgressesAsPlanned() =>
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(4, true, true, 2)).LoadDecision);

    [Fact] // 67
    public void LoadDecision_0Of4_DoesNotOutrank2Of4()
    {
        var zero = NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(0, false, false, 0)).LoadDecision;
        var two = NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(2, true, true, 0)).LoadDecision;
        // Reduce is strictly more conservative than Maintain, which is
        // strictly more conservative than ProgressAsPlanned -- 0/4 must
        // never resolve to an equal-or-better decision than 2/4.
        Assert.Equal(NextWindowLoadDecision.Reduce, zero);
        Assert.Equal(NextWindowLoadDecision.Maintain, two);
        Assert.NotEqual(NextWindowLoadDecision.ProgressAsPlanned, zero);
    }

    [Theory] // 68, 69, 70
    [InlineData(4, true, true, 2, NextWindowLoadDecision.ProgressAsPlanned)]
    [InlineData(2, true, true, 0, NextWindowLoadDecision.Maintain)]
    [InlineData(0, false, false, 0, NextWindowLoadDecision.Reduce)]
    internal void SafetyReviewRequired_IsIndependentOfLoadDecision(int completed, bool key, bool longRun, int easy, NextWindowLoadDecision expectedLoad)
    {
        var summary = FourSessionSummary(completed, key, longRun, easy, safetyFlag: true);
        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);
        Assert.Equal(expectedLoad, result.LoadDecision);
        Assert.True(result.SafetyReviewRequired);
    }

    // ── GENERAL INVARIANTS (71-75) ────────────────────────────────────────

    [Fact] // 71
    public void NoKeyLongCrossSubstitution_EitherDirection()
    {
        var keyToLong = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.Tired),
            [], [EasyCandidate(D1, role: PreparationRunwaySlotRole.LongRun)]);
        var longToKey = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.LongRun, NotTodayReasonCode.Tired),
            [], [EasyCandidate(D1, role: PreparationRunwaySlotRole.KeySession)]);
        Assert.Equal(ScheduleRepairAction.Skip, keyToLong.Action);
        Assert.Equal(ScheduleRepairAction.Skip, longToKey.Action);
    }

    [Fact] // 72
    public void NoCrossPhaseCandidate()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.Tired, phase: PhaseA),
            [EmptySlot(D1, phase: PhaseB)], // wrong phase -- must be ignored
            []);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 72 (substitution side)
    public void NoCrossPhaseCandidate_Substitution()
    {
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.Tired, phase: PhaseA),
            [], [EasyCandidate(D1, phase: PhaseB)]);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 73
    public void NoCrossWindowCandidate_CallerScopedCandidateListIsTheOnlyEligibleSet()
    {
        // 4M.1's structural-eligibility boundary means "same window" is
        // enforced by the caller only ever including same-window candidates
        // in the list passed to the policy -- this test proves the policy
        // has no way to reach outside the supplied list (an empty list
        // representing "no candidates in this window" always Skips).
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.Tired), [], []);
        Assert.Equal(ScheduleRepairAction.Skip, decision.Action);
    }

    [Fact] // 74
    public void NoPreferredDayEscape_OnlySuppliedCandidatesAreEverConsidered()
    {
        // The policy never invents a candidate date; it can only select from
        // (or ignore) what the caller supplied -- proven by asserting the
        // selected date is always contained in the input list.
        var candidates = new[] { EmptySlot(D2), EmptySlot(D1) };
        var decision = ScheduleRepairPolicy.Evaluate(
            Trigger(PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.Tired), candidates, []);
        Assert.Contains(decision.SelectedEmptySlotDate, candidates.Select(c => (DateOnly?)c.Date));
    }

    [Fact] // 75
    public void SupersededSession_NonActionable_RepresentedInEvidenceContract()
    {
        // Superseded sessions carry no ExecutionOutcome transition path in
        // this pure model -- LogicalSessionEvidence still requires an
        // ExecutionOutcome value (Planned, since a superseded session was
        // removed from the active plan before ever being acted on), and the
        // builder never reads a Superseded root's ExecutionOutcome as
        // Completed/NotToday evidence (see WindowExecutionSummaryBuilder,
        // which special-cases PlanningStatus.Superseded before ever
        // inspecting ExecutionOutcome). Runtime enforcement that an actual
        // persisted Superseded row cannot transition to Completed/NotToday
        // belongs to a later phase (4M.2+).
        var summary = WindowExecutionSummaryBuilder.Build([
            Session(PreparationRunwaySlotRole.EasySupport, LongHorizonRollingSessionOutcomeStatus.Planned, SessionPlanningStatus.Superseded),
        ]);
        Assert.Equal(0, summary.EasyCompletedCount);
        Assert.Equal(0, summary.UnrecoveredNotTodayCount);
        Assert.Equal(1, summary.SupersededByAdaptationCount);
    }
}
