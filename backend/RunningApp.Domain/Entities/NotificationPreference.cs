namespace RunningApp.Domain.Entities;

/// <summary>Optional Phase 1 entity.</summary>
public class NotificationPreference
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ReminderStyle { get; set; } = "balanced";
    public bool WorkoutRemindersEnabled { get; set; }
    public bool EveningReminderEnabled { get; set; }
    public TimeOnly? ReminderTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
