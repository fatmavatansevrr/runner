using RunningApp.Domain.Enums;
using System;
using System.Collections.Generic;

namespace RunningApp.Application.DTOs.Plan;

public class GeneratePreviewResponse
{
    public Guid PreviewId { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public GoalType GoalType { get; set; }
    public GoalDistance GoalDistance { get; set; }
    public RunningBackground Level { get; set; }
    public int DaysPerWeek { get; set; }
    public DistanceUnit Unit { get; set; }
    public List<PreviewWeekDto> Weeks { get; set; } = new();

    /// <summary>
    /// True when no seeded template exactly matched the request and the
    /// engine fell back to a default template. Debug/development-only
    /// signal — production UI is not required to surface this.
    /// </summary>
    public bool FallbackUsed { get; set; }

    /// <summary>Human-readable explanation of the fallback, if any.</summary>
    public string? FallbackReason { get; set; }
}

public class PreviewWeekDto
{
    public int WeekNumber { get; set; }
    public TrainingWeekType WeekType { get; set; }
    public List<PreviewDayDto> Days { get; set; } = new();
}

public class PreviewDayDto
{
    public int SlotIndex { get; set; }
    public TrainingDayType DayType { get; set; }
    public double DistanceKm { get; set; }
    public int DurationMin { get; set; }
    public string Intensity { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
