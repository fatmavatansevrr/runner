using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

/// <summary>Phase 4L.2 -- an immutable checkpoint decision snapshot. Raw TrainingDay-equivalent evidence remains the dark harness's own source facts; this is the durable decision, not a duplicate of raw evidence.</summary>
public class LongHorizonCheckpointRecord
{
    public Guid Id { get; set; }
    public Guid PlanStateId { get; set; }
    public DateOnly AsOfDate { get; set; }
    public int SourceWindowStartWeek { get; set; }
    public int SourceWindowEndWeek { get; set; }
    public string EvidenceFingerprint { get; set; } = null!;
    public double? ValidatedWeeklyVolumeKm { get; set; }
    public double? ValidatedLongRunKm { get; set; }
    public int? CompletedFrequency { get; set; }
    public string AuthorityClassification { get; set; } = null!;
    public LongHorizonPersistedCheckpointDecision Decision { get; set; }
    public string? AuthoritativeReasonCode { get; set; }
    public int ContextVersionSequence { get; set; }
    public int PersistenceVersion { get; set; } = 1;
}
