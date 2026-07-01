namespace RunningApp.Domain.Entities;

/// <summary>Optional Phase 1 entity.</summary>
public class NotificationPreference
{
    public Guid Id { get; set; }
    public Guid? InternalUserId { get; set; }  // FK → Users.Id
    public string ReminderStyle { get; set; } = "balanced";
    public bool WorkoutRemindersEnabled { get; set; }
    public bool EveningReminderEnabled { get; set; }
    public TimeOnly? ReminderTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
