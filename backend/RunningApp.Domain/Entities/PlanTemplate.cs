using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class PlanTemplate
{
    public Guid Id { get; set; }
    public string TemplateId { get; set; } = string.Empty; // e.g. "habit_5k_beginner_3day_km_v1"
    public int Version { get; set; }
    public GoalType GoalType { get; set; }
    public GoalDistance GoalDistance { get; set; }
    public RunningBackground Level { get; set; }
    public int DaysPerWeek { get; set; }
    public DistanceUnit Unit { get; set; }
    public string DataJson { get; set; } = string.Empty; // serialized template weeks/days
    public DateTime CreatedAt { get; set; }
    public DateTime? DeprecatedAt { get; set; }
}
