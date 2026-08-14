using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

/// <summary>Phase 4L.2 -- one immutable durable record per attempted authoritative activation decision (success or block).</summary>
public class LongHorizonActivationWindowRecord
{
    public Guid Id { get; set; }
    public Guid PlanStateId { get; set; }
    public int StartGlobalWeek { get; set; }
    public int EndGlobalWeek { get; set; }
    public LongHorizonPersistedActivationOutcome Outcome { get; set; }
    public int ContextVersionSequence { get; set; }
    public Guid ContextVersionId { get; set; }
    public Guid? CheckpointDecisionId { get; set; }
    public Guid? CoreContextId { get; set; }
    public string? RunwayPrescriptionId { get; set; }
    public Guid? TargetLockId { get; set; }
    public DateTime ActivatedAtUtc { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public string? FailureReasonCode { get; set; }
    public int ContractVersion { get; set; } = 1;
}
