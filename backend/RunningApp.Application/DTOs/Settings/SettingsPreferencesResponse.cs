namespace RunningApp.Application.DTOs.Settings;

public class SettingsPreferencesResponse
{
    public string ReminderStyle { get; set; } = "balanced";
    public bool WorkoutRemindersEnabled { get; set; } = true;
    public bool EveningReminderEnabled { get; set; } = true;
    public string ReminderTime { get; set; } = "08:00";
}
