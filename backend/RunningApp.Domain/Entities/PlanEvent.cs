using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class PlanEvent
{
    public Guid Id { get; set; }
    public Guid? InternalUserId { get; set; }  // FK → Users.Id
    public Guid PlanId { get; set; }
    public Guid? TrainingDayId { get; set; }
    public string EventType { get; set; } = string.Empty; // "PlanGenerated" | "WorkoutCompleted" | "WorkoutMissed" | etc.
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
