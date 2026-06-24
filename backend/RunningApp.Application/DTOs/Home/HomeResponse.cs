using RunningApp.Domain.Enums;
using System;
using System.Collections.Generic;

namespace RunningApp.Application.DTOs.Home;

public class HomeResponse
{
    public ActivePlanSummaryDto? ActivePlan { get; set; }
    public TrainingDayResponse? TodayWorkout { get; set; }
    public DailyTipResponse? DailyTip { get; set; }
    public List<TrainingDayResponse> WeekSummary { get; set; } = new();
    public bool HasPendingConfirmations { get; set; }
}

public class ActivePlanSummaryDto
{
    public Guid PlanId { get; set; }
    public string GoalType { get; set; } = string.Empty;
    public string GoalDistance { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string ProgressText { get; set; } = string.Empty;
}

public class TrainingDayResponse
{
    public Guid DayId { get; set; }
    public DateTime Date { get; set; }
    public TrainingDayType DayType { get; set; }
    public TrainingDayStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double PlannedDistanceKm { get; set; }
    public int PlannedDurationMin { get; set; }
    public double? PlannedPaceMinKm { get; set; }
    public string? Intensity { get; set; }
    public double? ActualDistanceKm { get; set; }
    public int? ActualDurationMin { get; set; }
    public bool IsLongRun { get; set; }
    public bool CanMarkComplete { get; set; }
    public bool CanMarkNotToday { get; set; }
}

public class DailyTipResponse
{
    public string TipKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? WorkoutType { get; set; }
}
