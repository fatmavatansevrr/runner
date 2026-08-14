using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.1 -- pure domain contracts for the Appsel Adaptation V1 decision
/// model. Canonical source: appsel-adaptation-v1-canonical-spec-.md, Revision
/// 3.1 (frozen). This file is intentionally pure data/vocabulary: no DB
/// access, no calendar generation, no API surface. See
/// PHASE4M_1_ADAPTATION_DOMAIN_CONTRACTS_AND_PURE_DECISION_POLICIES.md for
/// the architecture audit that grounds every reuse decision below.
///
/// Reused from the existing backend rather than reinvented:
///   - <see cref="PreparationRunwaySlotRole"/> (KeySession/EasySupport/LongRun)
///     is the canonical session-role type; see LongHorizonSessionRoleCodec
///     for its established string-token bridge.
///   - <see cref="LongHorizonRollingSessionOutcomeStatus"/> (Planned/
///     Completed/NotToday) is the canonical ExecutionOutcome type -- an
///     exact match to Rev3.1 §5, so no parallel enum was created.
///   - <see cref="LongHorizonPersistedSegmentType"/> (GeneralEndurance/
///     PreparationRunway/Core) is reused as the coarse segment component of
///     <see cref="AdaptationPhaseIdentity"/>; the existing Week entity's
///     free-text Stage carries the finer distinction (Rev3.1 does not
///     require phase identity to be a single flat enum, only that it be
///     typed and comparable -- see PhaseBoundaryConstraint, §3).
/// </summary>
internal enum SessionPlanningStatus
{
    Active,
    Superseded,
}

internal enum ReasonClass
{
    Operational,
    Safety,
}

/// <summary>
/// Phase 4M.1 -- canonical NotToday reason vocabulary for the pure
/// adaptation decision model, per Rev3.1 §4. This is deliberately a NEW,
/// self-contained enum, not a reuse of the live rolling NotToday endpoint's
/// existing string set (<c>LongHorizonRollingSessionMutationService</c>'s
/// <c>{ "fatigue", "soreness", "illness", "schedule", "weather", "other" }</c>)
/// -- that set has no <c>pain_or_discomfort</c> token, which Rev3.1's safety
/// path is built on, and reconciling the two vocabularies is explicitly out
/// of scope for 4M.1 (no NotToday runtime integration). See the phase
/// document's "Repository contradictions / DecisionRequired items" section.
/// </summary>
internal enum NotTodayReasonCode
{
    ScheduleConflict,
    Travel,
    Weather,
    Tired,
    Illness,
    PainOrDiscomfort,
    Personal,
    Other,
}

internal enum ScheduleRepairAction
{
    Skip,
    RescheduleToEmptySlot,
    SubstituteFutureEasy,
}

internal enum NextWindowLoadDecision
{
    ProgressAsPlanned,
    Maintain,
    Reduce,
}

/// <summary>
/// Opaque, comparable phase identity for the PhaseBoundaryConstraint
/// (Rev3.1 §3: "candidate.Phase == triggerSession.Phase", generalized to
/// every phase boundary, not only Taper). Deliberately not a distance- or
/// catalog-specific string comparison -- callers resolve this from whatever
/// real segment/stage data they have (LongHorizonPersistedSegmentType plus
/// the Week's Stage token); the pure policies here only ever compare two
/// instances for equality.
/// </summary>
internal readonly record struct AdaptationPhaseIdentity(LongHorizonPersistedSegmentType Segment, string Stage);

/// <summary>
/// A session eligible to receive a rescheduled/substituted priority session.
/// Structural eligibility (future, same window, same phase, PreferredDay,
/// empty slot, hard-session/long-run separation, other safety constraints)
/// is resolved by the caller before this reaches the pure policies here --
/// <see cref="IsSafetyValid"/> is that caller-resolved fact, not something
/// this layer computes. <see cref="SourceSessionId"/> is null for an empty
/// preferred-day slot (§3 PreferredDayConstraint) and non-null for an
/// existing Active EASY_SUPPORT session being considered for substitution
/// (§3 SingleSessionSubstitution) -- <see cref="CandidateSelectionPolicy"/>
/// treats both uniformly via <see cref="ScheduleRepairCandidate"/>, but
/// <see cref="ScheduleRepairPolicy"/> only ever offers one kind or the other
/// to a given selection call, matching the spec's two-stage rule (try empty
/// slot, then try substitution).
/// <see cref="SourceRole"/> is null for an empty preferred-day slot and must
/// be <see cref="PreparationRunwaySlotRole.EasySupport"/> for a substitution
/// candidate -- <see cref="ScheduleRepairPolicy"/> defensively filters out
/// any other role, since SingleSessionSubstitution (Rev3.1 §3) only ever
/// moves a priority session into an EASY_SUPPORT slot, never into another
/// KEY_SESSION or LONG_RUN slot.
/// </summary>
internal sealed record ScheduleRepairCandidate(
    DateOnly Date,
    AdaptationPhaseIdentity Phase,
    bool IsSafetyValid,
    Guid? SourceSessionId = null,
    SessionPlanningStatus SourcePlanningStatus = SessionPlanningStatus.Active,
    PreparationRunwaySlotRole? SourceRole = null);

/// <summary>
/// The already-resolved facts about the session a user marked NotToday.
/// <see cref="ExecutionOutcome"/> must be
/// <see cref="LongHorizonRollingSessionOutcomeStatus.NotToday"/> --
/// <see cref="ScheduleRepairPolicy"/> fails fast otherwise (Rev3.1 §3, V1
/// non-goal: only explicit NotToday triggers repair, never batch/automatic
/// missed-session inference).
/// </summary>
internal sealed record ScheduleRepairTrigger(
    Guid SessionId,
    PreparationRunwaySlotRole Role,
    AdaptationPhaseIdentity Phase,
    bool IsTaper,
    NotTodayReasonCode Reason,
    LongHorizonRollingSessionOutcomeStatus ExecutionOutcome);

/// <summary>
/// The pure decision produced by <see cref="ScheduleRepairPolicy"/>. Exactly
/// one of <see cref="SelectedEmptySlotDate"/> / <see cref="SubstitutedEasySessionId"/>
/// is set, and only when <see cref="Action"/> requires it -- this layer does
/// not create, persist, or mutate any session; a future phase (4M.2)
/// translates this decision into real writes.
/// </summary>
internal sealed record ScheduleRepairDecision(
    ScheduleRepairAction Action,
    ReasonClass ReasonClass,
    bool SafetyFlag,
    DateOnly? SelectedEmptySlotDate = null,
    Guid? SubstitutedEasySessionId = null);

/// <summary>
/// One logical session's evidence for <see cref="WindowExecutionSummaryBuilder"/>.
/// <see cref="AdaptedFromId"/> is null for an original/root session (Rev3.1
/// §5 lineage rule); non-null for a replacement session, in which case this
/// row never contributes its own entry to <see cref="WindowExecutionSummary.ExpectedSessionCount"/>
/// -- it satisfies its root's original expectation instead.
/// </summary>
internal sealed record LogicalSessionEvidence(
    Guid Id,
    PreparationRunwaySlotRole Role,
    LongHorizonRollingSessionOutcomeStatus ExecutionOutcome,
    SessionPlanningStatus PlanningStatus,
    Guid? AdaptedFromId = null,
    NotTodayReasonCode? NotTodayReason = null);

internal sealed record WindowExecutionSummary(
    int ExpectedSessionCount,
    int EffectiveCompletedCount,
    bool KeySessionExpected,
    bool KeySessionCompleted,
    bool LongRunExpected,
    bool LongRunCompleted,
    int EasyExpectedCount,
    int EasyCompletedCount,
    int UnrecoveredNotTodayCount,
    int SupersededByAdaptationCount,
    bool HasSafetyFlag);

/// <summary>
/// Rev3.1 §7: LoadDecision and SafetyReviewRequired are independent
/// dimensions. SafetyReviewRequired must never override or be folded into
/// LoadDecision -- see NextWindowLoadDecisionPolicy's own doc comment.
/// </summary>
internal sealed record NextWindowAdaptationResult(
    NextWindowLoadDecision LoadDecision,
    bool SafetyReviewRequired);

/// <summary>
/// Thrown by <see cref="WindowExecutionSummaryBuilder"/> when the supplied
/// lineage graph is structurally ambiguous or impossible (Rev3.1 §13.4 /
/// Phase 4M.1 §T): more than one direct replacement child for a single
/// source session, or a cycle in the AdaptedFrom graph. Persistence-level
/// constraints that would prevent such a graph from ever being written
/// belong to Phase 4M.2 -- this is the pure-domain fail-fast boundary only.
/// </summary>
internal sealed class AdaptationLineageInvalidException(string message) : Exception(message);

/// <summary>
/// Thrown by <see cref="ScheduleRepairPolicy"/> when the trigger session's
/// ExecutionOutcome is not NotToday (Rev3.1 §3: schedule repair is only
/// ever triggered by explicit user NotToday, never inferred).
/// </summary>
internal sealed class ScheduleRepairTriggerInvalidException(string message) : ArgumentException(message);
