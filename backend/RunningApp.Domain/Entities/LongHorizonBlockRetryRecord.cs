using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

/// <summary>Phase 4L.2 -- immutable block/retry audit trail. Never overwritten; each transition is a new row.</summary>
public class LongHorizonBlockRetryRecord
{
    public Guid Id { get; set; }
    public Guid PlanStateId { get; set; }
    public LongHorizonPersistedBlockRetryEventType EventType { get; set; }
    public int BlockedGlobalWeekStart { get; set; }
    public int BlockedGlobalWeekEnd { get; set; }
    public string PublicReasonCategory { get; set; } = null!;
    public string InternalReasonCode { get; set; } = null!;
    public string EvidenceFingerprint { get; set; } = null!;
    public DateOnly CheckpointDate { get; set; }
    public bool RetryEligible { get; set; }
    public Guid? RelatedDecisionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
