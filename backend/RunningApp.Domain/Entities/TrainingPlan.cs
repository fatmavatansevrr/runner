using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class TrainingPlan
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public TrainingPlanStatus Status { get; set; } = TrainingPlanStatus.Active;
    public GoalType GoalType { get; set; }
    public GoalDistance GoalDistance { get; set; }
    public double? GoalDistanceKm { get; set; }
    public RunningBackground Level { get; set; }
    public int DaysPerWeek { get; set; }
    public DistanceUnit Unit { get; set; } = DistanceUnit.Km;
    public string? RaceName { get; set; }
    public DateTime? RaceDate { get; set; }
    public int? TargetFinishTimeSeconds { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EstimatedEndDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<TrainingWeek> Weeks { get; set; } = new List<TrainingWeek>();
}
