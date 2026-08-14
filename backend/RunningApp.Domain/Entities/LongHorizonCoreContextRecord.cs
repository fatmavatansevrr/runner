using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

/// <summary>
/// Phase 4L.2 -- durable Core context ownership. SelectedCoreWeeksPayloadJson
/// is a bounded, versioned, immutable snapshot of the selected Core weeks
/// (global week, weekly volume, long run, session references) needed to
/// reconstruct without regeneration -- same Decision A rationale as
/// LongHorizonRunwayState.
/// </summary>
public class LongHorizonCoreContextRecord
{
    public Guid Id { get; set; }
    public Guid PlanStateId { get; set; }
    public int ContextVersionSequence { get; set; }
    public int EffectiveFromGlobalWeek { get; set; }
    public int EffectiveToGlobalWeek { get; set; }
    public DateOnly AsOfDate { get; set; }
    public string ConditionResultSummaryJson { get; set; } = null!;
    public string ValidatedLoadAuthoritySummary { get; set; } = null!;
    public string GeneratedCoreResultIdentity { get; set; } = null!;
    public string SelectedCoreWeeksPayloadJson { get; set; } = null!;
    public string? EvidenceFingerprint { get; set; }
    public Guid? SupersededByContextId { get; set; }
    public Guid? CreatedDecisionId { get; set; }
    public LongHorizonPersistedCoreContextStatus Status { get; set; } = LongHorizonPersistedCoreContextStatus.Active;
}
