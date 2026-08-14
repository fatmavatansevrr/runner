using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

/// <summary>Phase 4L.2 -- one durable structural row per global week. Exactly one row per (PlanStateId, GlobalWeek).</summary>
public class LongHorizonRollingWeekState
{
    public Guid Id { get; set; }
    public Guid PlanStateId { get; set; }
    public int GlobalWeek { get; set; }
    public LongHorizonPersistedSegmentType SegmentType { get; set; }
    public string Stage { get; set; } = null!;
    public DateOnly StructuralStartDate { get; set; }
    public DateOnly StructuralEndDate { get; set; }
    public LongHorizonPersistedLifecycleState LifecycleState { get; set; }
    public double? WeeklyVolumeKm { get; set; }
    public double? LongRunKm { get; set; }
    public int? ActivationContextVersionSequence { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? BlockedReasonCode { get; set; }
    public Guid? BlockedDecisionId { get; set; }

    public LongHorizonRollingPlanState Plan { get; set; } = null!;
    public ICollection<LongHorizonRollingSessionState> Sessions { get; set; } = new List<LongHorizonRollingSessionState>();
}
