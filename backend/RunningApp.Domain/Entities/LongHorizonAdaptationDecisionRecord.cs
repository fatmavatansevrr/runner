using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

/// <summary>
/// Phase 4M.2 -- one immutable durable audit record per committed Adaptation
/// V1 schedule-repair decision (Skip / RescheduleToEmptySlot /
/// SubstituteFutureEasy). Deliberately named distinctly from the unrelated,
/// pre-existing legacy static-plan <see cref="AdaptationEvent"/> entity (a
/// different subsystem entirely -- see PHASE4M_2's own audit section).
///
/// Scope note (Rev3.1 §9 / Phase 4M.2 §G): this table persists
/// schedule-repair decisions only. Next-window load decisions
/// (ProgressAsPlanned/Maintain/Reduce) are a structurally different
/// category -- no per-window uniqueness/idempotency semantics exist here
/// for them, and they are not persisted by this phase at all (out of
/// scope). If a future phase persists them, it must not reuse
/// <see cref="TriggerSessionId"/>'s unique index for that different
/// category without adding an explicit decision-category scope.
///
/// <see cref="TriggerSessionId"/> carries a unique index (see
/// AppDbContext) -- this is both the natural Rev3.1 idempotency key ("one
/// trigger NotToday session -> at most one committed schedule-repair
/// decision") and the mechanism a concurrent duplicate attempt fails
/// against at the database level, not merely an application-level check.
/// </summary>
public class LongHorizonAdaptationDecisionRecord
{
    public Guid Id { get; set; }
    public Guid PlanStateId { get; set; }

    /// <summary>Rev3.1's abstract "SourceWindowId" has no concrete row of
    /// its own in this persistence model -- a Long-Horizon "window" is the
    /// plan aggregate's own [CurrentWindowStartWeek, CurrentWindowEndWeek]
    /// global-week range, not a separate table (confirmed by repository
    /// audit, Phase 4M.2 §B). These two ints are that range at decision
    /// time -- a technical persistence-shape choice, not a product
    /// decision; see the phase document's DecisionRequired section for why
    /// this does not require product sign-off.</summary>
    public int SourceWindowStartWeek { get; set; }
    public int SourceWindowEndWeek { get; set; }

    public Guid TriggerSessionId { get; set; }
    public LongHorizonPersistedAdaptationDecisionType DecisionType { get; set; }
    public bool SafetyReviewRequired { get; set; }

    /// <summary>The canonical Phase 4M.1 NotTodayReasonCode value the pure
    /// decision was made from, stored as its string name (matching the
    /// existing repository convention for persisted enum-like values, e.g.
    /// LongHorizonRollingSessionState.OutcomeStatus). Never a raw live
    /// endpoint token -- see §S / DecisionRequired: the live
    /// fatigue/soreness/... vocabulary mapping is Phase 4M.3's concern.</summary>
    public string ReasonCode { get; set; } = null!;

    public Guid? ReplacementSessionId { get; set; }
    public Guid? SupersededSessionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int ContractVersion { get; set; } = 1;
}
