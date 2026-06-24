using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class TrainingWeek
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int WeekNumber { get; set; }
    public TrainingWeekType WeekType { get; set; } = TrainingWeekType.Build;
    public double PlannedVolumeKm { get; set; }
    public double ActualVolumeKm { get; set; }
    public bool IsRecoveryWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public TrainingPlan Plan { get; set; } = null!;
    public ICollection<TrainingDay> Days { get; set; } = new List<TrainingDay>();
}
