namespace RunningApp.Domain.Entities;

public class PendingConfirmation
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public Guid TrainingDayId { get; set; }
    public string Status { get; set; } = "pending"; // "pending" | "resolved"
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
