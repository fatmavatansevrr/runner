using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class TrainingPlan
{
    public Guid Id { get; set; }
    public Guid? InternalUserId { get; set; }  // FK → Users.Id
    public string? TemplateId { get; set; }
    public TrainingPlanStatus Status { get; set; } = TrainingPlanStatus.Active;
    public GoalType GoalType { get; set; }
    public GoalDistance GoalDistance { get; set; }
    public double? GoalDistanceKm { get; set; }
    public RunningBackground Level { get; set; }
    public int DaysPerWeek { get; set; }
    public DistanceUnit Unit { get; set; } = DistanceUnit.Km;
    public string? RaceName { get; set; }
    public DateOnly? RaceDate { get; set; }
    public int? TargetFinishTimeSeconds { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EstimatedEndDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Onboarding snapshot fields — copied from PlanPreview at confirm time.
    // Template changes must never affect these historical values.
    public string? PreferredDays { get; set; }    // JSON array e.g. "[1,3,5]"
    public int? WeeklyAvailability { get; set; }  // hours per week
    public double? PreferredPace { get; set; }    // min/km comfortable pace
    public string? InjuryNotes { get; set; }

    // Race-specific
    /// <summary>Race akışında long run günü. PreferredDays içinde yer almalı.</summary>
    public string? LongRunDay { get; set; }

    // Habit-specific
    /// <summary>GoalType='habit' olunca zorunlu; race akışında NULL.</summary>
    public string? HabitPlanType { get; set; }

    /// <summary>HabitPlanType='custom' olunca zorunlu.</summary>
    public string? CustomGoalType { get; set; }

    /// <summary>Custom plan için kullanıcının belirlediği hafta sayısı.</summary>
    public int? CustomDurationWeeks { get; set; }

    /// <summary>
    /// CustomGoalType='finish_under_time' hedefi (saniye).
    /// Pace hesabında KULLANILMAZ — motivasyon referansı.
    /// TargetFinishTimeSeconds'dan farklıdır (o race pace zone için).
    /// </summary>
    public int? CustomTargetTimeSeconds { get; set; }

    // Navigation properties
    public ICollection<TrainingWeek> Weeks { get; set; } = new List<TrainingWeek>();
}
