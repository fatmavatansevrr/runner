using RunningApp.Domain.Enums;
using System;

namespace RunningApp.Application.DTOs.Plan;

public class GeneratePreviewRequest
{
    public GoalType GoalType { get; set; }
    public GoalDistance GoalDistance { get; set; }
    public RunningBackground Level { get; set; }
    public int DaysPerWeek { get; set; }
    public DistanceUnit Unit { get; set; }
    public string? RaceName { get; set; }
    public DateOnly? RaceDate { get; set; }
    public int? TargetFinishTimeSeconds { get; set; }

    // Onboarding snapshot fields — captured once and frozen onto TrainingPlan.
    // Nullable so existing clients that don't send these continue to work.
    public string? PreferredDays { get; set; }       // JSON array e.g. "[1,3,5]"
    public int? WeeklyAvailability { get; set; }     // hours per week available
    public double? PreferredPace { get; set; }       // min/km comfortable pace

    public string? LongRunDay { get; set; }
    public string? HabitPlanType { get; set; }
    public string? CustomGoalType { get; set; }
    public int? CustomDurationWeeks { get; set; }
    public int? CustomTargetTimeSeconds { get; set; }


}
