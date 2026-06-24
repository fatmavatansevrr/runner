// [ignoring loop detection]
using RunningApp.Domain.Enums;
using System;
using System.Collections.Generic;

namespace RunningApp.Application.DTOs.Plan;

public class PlanDetailsResponse
{
    public Guid PlanId { get; set; }
    public string? TemplateId { get; set; }
    public string Status { get; set; } = string.Empty;
    public GoalType GoalType { get; set; }
    public GoalDistance GoalDistance { get; set; }
    public RunningBackground Level { get; set; }
    public int DaysPerWeek { get; set; }
    public DistanceUnit Unit { get; set; }
    public string? RaceName { get; set; }
    public DateTime? RaceDate { get; set; }
    public int? TargetFinishTimeSeconds { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EstimatedEndDate { get; set; }
    public int TotalWeeks { get; set; }
    public int CompletedWeeksCount { get; set; }
    public double TotalPlannedDistance { get; set; }
    public double TotalCompletedDistance { get; set; }
    public List<PlanWeekDetailDto> Weeks { get; set; } = new();
}

public class PlanWeekDetailDto
{
    public Guid WeekId { get; set; }
    public int WeekNumber { get; set; }
    public TrainingWeekType WeekType { get; set; }
    public double PlannedVolumeKm { get; set; }
    public double ActualVolumeKm { get; set; }
    public bool IsRecoveryWeek { get; set; }
    public DateTime StartDate { get; set; }
    public List<PlanDayDetailDto> Days { get; set; } = new();
}

public class PlanDayDetailDto
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
