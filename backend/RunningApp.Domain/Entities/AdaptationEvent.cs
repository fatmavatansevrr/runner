using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

/// <summary>
/// Placeholder entity for future Adaptive Engine events.
/// Phase 1: rarely populated; placeholder engine writes no adaptation events.
/// </summary>
public class AdaptationEvent
{
    public Guid Id { get; set; }
    public Guid? InternalUserId { get; set; }  // FK → Users.Id
    public Guid PlanId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public TriggerSource TriggerSource { get; set; }
    public Guid? TriggeredByTrainingDayId { get; set; }
    public AdaptationAction Action { get; set; } = AdaptationAction.NoChange;
    public string? AffectedDaysJson { get; set; }
    public string ExplanationKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DismissedAt { get; set; }
}
