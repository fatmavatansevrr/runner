using RunningApp.Domain.Enums;

namespace RunningApp.Application.DTOs.Plan;

public enum LongHorizonCheckpointReadiness
{
    CurrentWindowInProgress,
    CurrentWindowComplete,
    NextWindowActivationReady,
    ReassessmentRequired,
    TerminalPlanComplete
}

public sealed record LongHorizonRollingSessionResponse
{
    public int ContractVersion { get; init; } = 1;
    public required Guid SessionId { get; init; }
    public required Guid PlanId { get; init; }
    public PlanScheduleStrategy ScheduleStrategy { get; init; } = PlanScheduleStrategy.RollingLongHorizon;
    public required int GlobalWeek { get; init; }
    public required string Phase { get; init; }
    public required string Stage { get; init; }
    public required DateOnly AssignedDate { get; init; }
    public required string WorkoutRole { get; init; }
    public string? WorkoutKey { get; init; }
    public int? WorkoutVersion { get; init; }
    public required double PlannedDistanceKm { get; init; }
    public int? PlannedDurationMinutes { get; init; }
    public double? PlannedPaceMinutesPerKm { get; init; }
    public string? PlannedIntensity { get; init; }
    public required string Outcome { get; init; }
    public double? ActualDistanceKm { get; init; }
    public int? ActualDurationMinutes { get; init; }
    public double? ActualPaceMinutesPerKm { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? NotTodayReasonCategory { get; init; }
    public DateTime? NotTodayRecordedAtUtc { get; init; }
    public required bool IsLongRun { get; init; }
    public required bool MutationAllowed { get; init; }
    public required string PublicProvenance { get; init; }
}

public sealed record LongHorizonActivePlanSummaryResponse
{
    public required Guid PlanId { get; init; }
    public PlanScheduleStrategy ScheduleStrategy { get; init; } = PlanScheduleStrategy.RollingLongHorizon;
    public required string GoalType { get; init; }
    public required string GoalDistance { get; init; }
    public required string Level { get; init; }
    public required int TotalWeeks { get; init; }
    public required int CurrentGlobalWeek { get; init; }
    public required string CurrentPhase { get; init; }
    public required string CurrentStage { get; init; }
    public required int CurrentWindowStartWeek { get; init; }
    public required int CurrentWindowEndWeek { get; init; }
    public int? NextPendingGlobalWeek { get; init; }
    public required int ActivatedSessionCount { get; init; }
    public required int TerminalSessionCount { get; init; }
    public required LongHorizonCheckpointReadiness CheckpointReadiness { get; init; }
    // Phase 4L.4C: only meaningful when CheckpointReadiness == ReassessmentRequired.
    public LongHorizonRecoveryRequirement? RecoveryRequirement { get; init; }
    public string? BlockedPublicReasonCategory { get; init; }
    public required string Status { get; init; }
    public required string PublicMessage { get; init; }
}

/// <summary>
/// Phase 4L.4C -- the public recovery-authority classification for a Blocked
/// plan. See LongHorizonBlockRecoveryClassification (internal) for the
/// underlying reason-code mapping this is derived from.
/// </summary>
public enum LongHorizonRecoveryRequirement
{
    None,
    CalendarWindowPending,
    RegeneratePreviewRequired,
    OperationalSupportRequired,
}

public sealed record LongHorizonHomeResponse
{
    public int ContractVersion { get; init; } = 1;
    public PlanScheduleStrategy ScheduleStrategy { get; init; } = PlanScheduleStrategy.RollingLongHorizon;
    public required LongHorizonActivePlanSummaryResponse ActivePlan { get; init; }
    public LongHorizonRollingSessionResponse? TodayWorkout { get; init; }
    public LongHorizonRollingSessionResponse? NextExecutableWorkout { get; init; }
    public required IReadOnlyList<LongHorizonRollingSessionResponse> CurrentWindowSessions { get; init; }
    public required bool HasPendingConfirmations { get; init; }
}

public sealed record LongHorizonCalendarResponse
{
    public int ContractVersion { get; init; } = 1;
    public PlanScheduleStrategy ScheduleStrategy { get; init; } = PlanScheduleStrategy.RollingLongHorizon;
    public required Guid PlanId { get; init; }
    public required string Month { get; init; }
    public required IReadOnlyList<LongHorizonRollingSessionResponse> Sessions { get; init; }
}

public sealed record LongHorizonRollingSessionDetailResponse
{
    public int ContractVersion { get; init; } = 1;
    public required LongHorizonRollingSessionResponse Session { get; init; }
    public required string PublicDescription { get; init; }
}

public sealed record LongHorizonCompleteSessionRequest
{
    public int ContractVersion { get; init; } = 1;
    public required double ActualDistanceKm { get; init; }
    public required int ActualDurationMinutes { get; init; }
}

public sealed record LongHorizonNotTodayRequest
{
    public int ContractVersion { get; init; } = 1;
    public required string Reason { get; init; }
}

public enum LongHorizonSessionMutationOutcome
{
    Completed,
    NotToday,
    IdempotentReplay
}

/// <summary>
/// Phase 4M.3 -- the schedule-repair action Rev3.1's ScheduleRepairPolicy
/// decided for this NotToday event, if any. Deliberately a separate public
/// enum from the internal 4M.1 ScheduleRepairAction: an internal type cannot
/// be exposed on a public DTO, and this keeps the wire contract independent
/// of the pure-domain vocabulary's own evolution.
/// </summary>
public enum LongHorizonScheduleRepairAction
{
    Skip,
    RescheduleToEmptySlot,
    SubstituteFutureEasy
}

public sealed record LongHorizonSessionMutationResponse
{
    public int ContractVersion { get; init; } = 1;
    public required Guid SessionId { get; init; }
    public required Guid PlanId { get; init; }
    public PlanScheduleStrategy ScheduleStrategy { get; init; } = PlanScheduleStrategy.RollingLongHorizon;
    public required LongHorizonSessionMutationOutcome Outcome { get; init; }
    public required int OutcomeVersion { get; init; }
    public required LongHorizonCheckpointReadiness CheckpointReadiness { get; init; }
    public required bool NextWindowActivated { get; init; }

    // Phase 4M.3 -- populated only for a NotToday/IdempotentReplay outcome
    // that reached schedule-repair orchestration; always null for Completed.
    // ScheduleRepairSafetyFlag is the single-event schedule-repair-level
    // safety signal (Rev3.1 §4.1), NOT the window-level NextWindowAdaptationResult.SafetyReviewRequired
    // checkpoint signal -- that remains out of scope until Phase 4M.4 and is
    // never computed or exposed here.
    public LongHorizonScheduleRepairAction? ScheduleRepairAction { get; init; }
    public bool? ScheduleRepairSafetyFlag { get; init; }
    public Guid? ScheduleRepairReplacementSessionId { get; init; }
    public DateOnly? ScheduleRepairReplacementDate { get; init; }
    public bool? ScheduleRepairIsIdempotentReplay { get; init; }
}

// Phase 4L.4A: explicit authenticated next-window activation / public continuation.

public sealed record LongHorizonActivateNextWindowRequest
{
    public int ContractVersion { get; init; } = 1;
}

/// <summary>
/// Success-path outcomes only (always HTTP 200). Non-success continuation
/// states (in-progress, reassessment required, blocked, retry required,
/// stale) are surfaced as typed error responses per the existing
/// GlobalExceptionHandler convention -- see LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS,
/// LONG_HORIZON_REASSESSMENT_REQUIRED, LONG_HORIZON_CONTINUATION_BLOCKED,
/// LONG_HORIZON_RETRY_REQUIRED -- rather than a single enum carrying both
/// success and error states behind one status code.
/// </summary>
public enum LongHorizonContinuationOutcome
{
    Activated,
    IdempotentReplay,
    TerminalPlanComplete
}

public sealed record LongHorizonWindowRange
{
    public required int StartGlobalWeek { get; init; }
    public required int EndGlobalWeek { get; init; }
}

/// <summary>
/// Phase 4M.4A -- the window-level Rev3.1 §7 load-decision signal for the
/// window that was just checkpointed. Deliberately a separate public enum
/// from the internal 4M.1 NextWindowLoadDecision (same reasoning as
/// LongHorizonScheduleRepairAction in 4M.3: an internal type cannot be
/// exposed on a public DTO). "Reduce" here is purely symbolic (Rev3.1: a
/// signal that next-window progression must be constrained downward) -- no
/// numeric translation exists yet; see PHASE4M_4A doc.
/// </summary>
public enum LongHorizonNextWindowLoadDecision
{
    ProgressAsPlanned,
    Maintain,
    Reduce
}

public sealed record LongHorizonActivateNextWindowResponse
{
    public int ContractVersion { get; init; } = 1;
    public required Guid PlanId { get; init; }
    public PlanScheduleStrategy ScheduleStrategy { get; init; } = PlanScheduleStrategy.RollingLongHorizon;
    public required LongHorizonContinuationOutcome Outcome { get; init; }
    public LongHorizonWindowRange? PreviousWindowRange { get; init; }
    public LongHorizonWindowRange? ActivatedWindowRange { get; init; }
    public required IReadOnlyList<int> ActivatedGlobalWeeks { get; init; }
    public required IReadOnlyList<LongHorizonRollingSessionResponse> ActivatedSessions { get; init; }
    public int? NextPendingGlobalWeek { get; init; }
    public required LongHorizonCheckpointReadiness CheckpointReadiness { get; init; }
    public required string PlanStatus { get; init; }
    public required bool IsTerminal { get; init; }
    public required DateTime ActivatedAtUtc { get; init; }
    public required string PublicMessage { get; init; }

    // Phase 4M.4A -- populated only for Activated/IdempotentReplay (the
    // window that was just checkpointed produced a real decision); always
    // null for TerminalPlanComplete. NextWindowSafetyReviewRequired is the
    // WINDOW-level signal (Rev3.1 §7) -- explicitly distinct from 4M.3's
    // per-decision ScheduleRepairSafetyFlag on LongHorizonSessionMutationResponse.
    // Independent from NextWindowLoadDecision: never used to override it.
    public LongHorizonNextWindowLoadDecision? NextWindowLoadDecision { get; init; }
    public bool? NextWindowSafetyReviewRequired { get; init; }
}

// Phase 4L.4B: public Blocked -> Pending retry restoration.

public sealed record LongHorizonRetryContinuationRequest
{
    public int ContractVersion { get; init; } = 1;
}

/// <summary>Success-path outcomes only, matching the activation endpoint's own
/// scoping (see LongHorizonContinuationOutcome). NoBlockedBoundary/RetryNotEligible
/// are typed error responses, not in-body "ok" states.</summary>
public enum LongHorizonRetryOutcome
{
    RestoredToPending,
    IdempotentReplay
}

public sealed record LongHorizonRetryContinuationResponse
{
    public int ContractVersion { get; init; } = 1;
    public required Guid PlanId { get; init; }
    public PlanScheduleStrategy ScheduleStrategy { get; init; } = PlanScheduleStrategy.RollingLongHorizon;
    public required LongHorizonRetryOutcome Outcome { get; init; }
    public required LongHorizonWindowRange RestoredWindowRange { get; init; }
    public required LongHorizonWindowRange CurrentWindowRange { get; init; }
    public int? NextPendingGlobalWeek { get; init; }
    public required LongHorizonCheckpointReadiness CheckpointReadiness { get; init; }
    public required string PlanStatus { get; init; }
    public required DateTime RetriedAtUtc { get; init; }
    public required string PublicMessage { get; init; }
}
