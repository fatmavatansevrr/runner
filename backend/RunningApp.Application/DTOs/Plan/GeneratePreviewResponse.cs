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
    /// Always false as of Phase 0 (safe template selection): a request with
    /// no exact matching seeded template now fails with
    /// <c>PLAN_TEMPLATE_NOT_FOUND</c> instead of generating a preview from an
    /// unrelated fallback template. Field kept for API back-compatibility.
    /// </summary>
    public bool FallbackUsed { get; set; }

    /// <summary>Always null as of Phase 0 — see <see cref="FallbackUsed"/>.</summary>
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
