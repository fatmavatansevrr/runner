namespace RunningApp.Domain.Entities;

public class WorkoutLog
{
    public Guid Id { get; set; }
    public Guid? InternalUserId { get; set; }  // FK → Users.Id
    public Guid? PlanId { get; set; }
    public Guid TrainingDayId { get; set; }
    public string Result { get; set; } = string.Empty; // "as_planned" | "shorter" | "exceeded"
    public double? ActualDistanceKm { get; set; }
    public int? ActualDurationMin { get; set; }
    public string? UserNote { get; set; }
    public DateTime CreatedAt { get; set; }
}
