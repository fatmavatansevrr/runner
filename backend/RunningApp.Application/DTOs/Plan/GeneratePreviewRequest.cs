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
    public DateTime? RaceDate { get; set; }
    public int? TargetFinishTimeSeconds { get; set; }
}
