using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.2 -- pure input/output contracts for
/// <see cref="ScheduleRepairPersistenceService"/>. Mirrors the shape of the
/// existing <c>LongHorizonRollingPersistenceOutcome</c>/<c>LongHorizonRollingPersistenceResult</c>
/// pattern (LongHorizonRollingPersistenceContracts.cs) already established
/// for the rolling-activation persistence boundary, extended with the two
/// outcome values this phase's own scope requires
/// (<see cref="AdaptationPersistenceOutcome.StaleTrigger"/> and
/// <see cref="AdaptationPersistenceOutcome.StaleTarget"/> -- see Phase
/// 4M.2 §Q) that the existing enum has no equivalent for.
/// </summary>
internal enum AdaptationPersistenceOutcome
{
    /// <summary>The decision was newly committed in this call.</summary>
    Committed,

    /// <summary>A schedule-repair decision for this TriggerSessionId was
    /// already committed (by this call or an earlier/concurrent one); the
    /// authoritative existing result is returned, no new mutation applied.</summary>
    IdempotentReplay,

    /// <summary>The trigger session no longer has ExecutionOutcome ==
    /// NotToday (or does not exist) at commit time.</summary>
    StaleTrigger,

    /// <summary>The already-decided target (empty slot date, or
    /// substitution EASY session) is no longer valid to commit against --
    /// occupied, no longer Active/Planned, wrong role, or not found. 4M.2
    /// never re-selects a different target; it only ever accepts or
    /// rejects the one 4M.1 already decided (Phase 4M.2 §P/§AB).</summary>
    StaleTarget,

    /// <summary>A concurrent attempt for the same TriggerSessionId lost the
    /// race at the database uniqueness boundary and the winner's result
    /// could not be resolved as an ordinary idempotent replay.</summary>
    ConcurrencyConflict,

    /// <summary>An internal invariant was violated (e.g. a required
    /// structural input was missing) -- never a raw provider exception.</summary>
    IntegrityViolation,
}

internal sealed record AdaptationPersistenceResult(
    AdaptationPersistenceOutcome Outcome,
    Guid? DecisionRecordId = null,
    Guid? ReplacementSessionId = null,
    Guid? SupersededSessionId = null,
    string? FailureReason = null);

/// <summary>
/// The already-resolved Phase 4M.1 decision, plus the minimal additional
/// structural context 4M.2's persistence boundary needs that 4M.1's pure
/// contracts do not carry (4M.1 never touches persistence, so it has no
/// concept of a concrete WeekState row -- see Phase 4M.2 §B/DecisionRequired
/// for why this is a technical extension, not a product decision).
/// <see cref="TargetWeekStateId"/> is required only for
/// <see cref="ScheduleRepairAction.RescheduleToEmptySlot"/> (there is no
/// existing session row at an empty slot to derive it from); for
/// <see cref="ScheduleRepairAction.SubstituteFutureEasy"/> the target week
/// is derived directly from the substitution target session's own row.
/// </summary>
internal sealed record ScheduleRepairPersistenceRequest(
    Guid PlanStateId,
    ScheduleRepairTrigger Trigger,
    ScheduleRepairDecision Decision,
    int SourceWindowStartWeek,
    int SourceWindowEndWeek,
    Guid? TargetWeekStateId = null);
