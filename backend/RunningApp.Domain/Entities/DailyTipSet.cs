using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class DailyTipSet
{
    public Guid Id { get; set; }
    public string TipKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public TrainingDayType? WorkoutType { get; set; }
    public RunningBackground? Level { get; set; }
    public GoalType? GoalType { get; set; }
    public string Language { get; set; } = "en";
    public DateTime CreatedAt { get; set; }
}
