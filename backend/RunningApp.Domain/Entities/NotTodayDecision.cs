using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class NotTodayDecision
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public Guid TrainingDayId { get; set; }
    public string? Reason { get; set; } // "need_rest" | "no_time" | "feeling_tired" | "other"
    public NotTodayDecisionStatus Status { get; set; } = NotTodayDecisionStatus.Pending;
    public TriggerSource TriggerSource { get; set; } = TriggerSource.NotToday;
    public AdaptationAction Action { get; set; } = AdaptationAction.NoChange;
    public TrainingDayStatus ResultingStatus { get; set; } = TrainingDayStatus.Missed;
    public string? DecisionPayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}
