using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class TrainingDay
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public Guid WeekId { get; set; }
    public DateTime Date { get; set; }
    public TrainingDayType DayType { get; set; }
    public TrainingDayStatus Status { get; set; } = TrainingDayStatus.Planned;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Planned fields
    public double PlannedDistanceKm { get; set; }
    public int PlannedDurationMin { get; set; }
    public double? PlannedPaceMinKm { get; set; }
    public string? Intensity { get; set; } // e.g. "z2"

    // Actual fields — intentional denormalization for fast calendar/stats reads.
    // WorkoutLog stores the append-only history; TrainingDay stores the latest result.
    public double? ActualDistanceKm { get; set; }
    public int? ActualDurationMin { get; set; }
    public double? ActualPaceMinKm { get; set; }

    public bool IsLongRun { get; set; }
    public DateTime? OriginalDate { get; set; }
    public TrainingDayType? OriginalType { get; set; }
    public bool CanMarkComplete { get; set; } = true;
    public bool CanMarkNotToday { get; set; } = true;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Adaptation metadata — supports future adaptive engine debugging.
    public TrainingDaySource? Source { get; set; }
    public Guid? AdaptedFromId { get; set; }
    public int? PlanVersion { get; set; }

    // Navigation
    public TrainingPlan Plan { get; set; } = null!;
    public TrainingWeek Week { get; set; } = null!;
    public TrainingDay? AdaptedFrom { get; set; }
}
