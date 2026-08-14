using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.1 -- pure ScheduleRepairPolicy (Rev3.1 §3). Implements
/// EasySupportRule, KeySessionRule, LongRunRule and TaperProtectionRule.
/// Never queries a database or calendar, never persists, never creates or
/// mutates a real session -- callers supply already structurally-eligible
/// candidates (future, same window, same phase, PreferredDay/empty or
/// Active-EASY, hard-session/long-run separation already validated); this
/// policy adds one further defensive check of its own (phase-boundary
/// equality, §F/PhaseBoundaryConstraint) rather than trusting the boundary
/// blindly, since that invariant is safety/product-critical and cheap to
/// re-check here.
/// </summary>
internal static class ScheduleRepairPolicy
{
    public static ScheduleRepairDecision Evaluate(
        ScheduleRepairTrigger trigger,
        IReadOnlyList<ScheduleRepairCandidate> emptySlotCandidates,
        IReadOnlyList<ScheduleRepairCandidate> futureEasyCandidates)
    {
        if (trigger.ExecutionOutcome != Domain.Enums.LongHorizonRollingSessionOutcomeStatus.NotToday)
        {
            throw new ScheduleRepairTriggerInvalidException(
                "ScheduleRepairPolicy may only evaluate a session whose ExecutionOutcome is NotToday.");
        }

        var reasonClass = ReasonClassificationPolicy.Classify(trigger.Reason);
        var safetyFlag = ReasonClassificationPolicy.TriggersSafetyFlag(trigger.Reason);

        return trigger.Role switch
        {
            PreparationRunwaySlotRole.EasySupport => Skip(reasonClass, safetyFlag),
            PreparationRunwaySlotRole.KeySession => EvaluateKeySession(trigger, reasonClass, safetyFlag, emptySlotCandidates, futureEasyCandidates),
            PreparationRunwaySlotRole.LongRun => EvaluateLongRun(trigger, reasonClass, safetyFlag, emptySlotCandidates, futureEasyCandidates),
            _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger.Role, "Unrecognized session role."),
        };
    }

    private static ScheduleRepairDecision Skip(ReasonClass reasonClass, bool safetyFlag) =>
        new(ScheduleRepairAction.Skip, reasonClass, safetyFlag);

    private static ScheduleRepairDecision EvaluateKeySession(
        ScheduleRepairTrigger trigger,
        ReasonClass reasonClass,
        bool safetyFlag,
        IReadOnlyList<ScheduleRepairCandidate> emptySlotCandidates,
        IReadOnlyList<ScheduleRepairCandidate> futureEasyCandidates)
    {
        if (ReasonClassificationPolicy.BlocksReschedule(trigger.Reason))
            return Skip(reasonClass, safetyFlag);

        return TryRepair(trigger, reasonClass, safetyFlag, emptySlotCandidates, futureEasyCandidates);
    }

    private static ScheduleRepairDecision EvaluateLongRun(
        ScheduleRepairTrigger trigger,
        ReasonClass reasonClass,
        bool safetyFlag,
        IReadOnlyList<ScheduleRepairCandidate> emptySlotCandidates,
        IReadOnlyList<ScheduleRepairCandidate> futureEasyCandidates)
    {
        if (ReasonClassificationPolicy.BlocksReschedule(trigger.Reason))
            return Skip(reasonClass, safetyFlag);

        // TaperProtectionRule: during Taper, LONG_RUN always Skips, even for
        // an otherwise-repairable operational reason -- adaptation must
        // never increase planned load during Taper.
        if (trigger.IsTaper)
            return Skip(reasonClass, safetyFlag);

        return TryRepair(trigger, reasonClass, safetyFlag, emptySlotCandidates, futureEasyCandidates);
    }

    /// <summary>Shared RescheduleToEmptySlot-then-SubstituteFutureEasy
    /// sequence for KEY_SESSION and LONG_RUN. Both candidate lists are
    /// defensively filtered to the trigger's own phase before selection --
    /// see PhaseBoundaryConstraint in this type's own doc comment.</summary>
    private static ScheduleRepairDecision TryRepair(
        ScheduleRepairTrigger trigger,
        ReasonClass reasonClass,
        bool safetyFlag,
        IReadOnlyList<ScheduleRepairCandidate> emptySlotCandidates,
        IReadOnlyList<ScheduleRepairCandidate> futureEasyCandidates)
    {
        var samePhaseEmptySlots = emptySlotCandidates.Where(c => c.Phase == trigger.Phase).ToList();
        var emptySlot = CandidateSelectionPolicy.SelectEarliestValid(samePhaseEmptySlots);
        if (emptySlot is not null)
            return new ScheduleRepairDecision(ScheduleRepairAction.RescheduleToEmptySlot, reasonClass, safetyFlag, SelectedEmptySlotDate: emptySlot.Date);

        var samePhaseEasy = futureEasyCandidates
            .Where(c => c.Phase == trigger.Phase
                && c.SourcePlanningStatus == SessionPlanningStatus.Active
                && c.SourceRole == PreparationRunwaySlotRole.EasySupport)
            .ToList();
        var easyCandidate = CandidateSelectionPolicy.SelectEarliestValid(samePhaseEasy);
        if (easyCandidate is not null)
        {
            return new ScheduleRepairDecision(
                ScheduleRepairAction.SubstituteFutureEasy, reasonClass, safetyFlag,
                SubstitutedEasySessionId: easyCandidate.SourceSessionId
                    ?? throw new InvalidOperationException("A future-EASY substitution candidate must carry SourceSessionId."));
        }

        return Skip(reasonClass, safetyFlag);
    }
}
