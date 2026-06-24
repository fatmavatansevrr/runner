using RunningApp.Domain.Enums;
using System;

namespace RunningApp.Application.DTOs.Profile;

public class ProfileOverviewResponse
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DistanceUnit Unit { get; set; }
    public RunningBackground RunningBackground { get; set; }
    public ProfilePlanStatsDto? ActivePlanStats { get; set; }
}

public class ProfilePlanStatsDto
{
    public string PlanName { get; set; } = string.Empty;
    public string GoalType { get; set; } = string.Empty;
    public string GoalDistance { get; set; } = string.Empty;
    public int CompletedRunsCount { get; set; }
    public int TotalPlannedRunsCount { get; set; }
    public double TotalCompletedDistance { get; set; }
    public double AdherenceRatePercent { get; set; }
}
