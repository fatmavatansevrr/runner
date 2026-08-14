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

    // ── Backend Integration Phase 4G.6D — additive Preparation Runway/Core
    // provenance fields, mapped directly from the plan's current persisted
    // TrainingWeek (never parsed from ProgressText, never inferred from
    // TotalWeeks). CurrentRunwayBlock is null for every Core week and for
    // every plan with no TrainingWeek rows (legacy/seeded plans) — see
    // PHASE4G_6D_...md §6 for the exact entity mapping. ──

    /// <summary>The current TrainingWeek's global WeekNumber. Null only when the plan has no persisted TrainingWeek rows.</summary>
    public int? CurrentWeekNumber { get; set; }

    /// <summary>Total persisted TrainingWeek count for this plan. Null only when the plan has no persisted TrainingWeek rows.</summary>
    public int? TotalWeeks { get; set; }

    /// <summary>The current week's persisted TrainingWeekType, snake_case (e.g. "base", "preparation_runway"). Null only when the plan has no persisted TrainingWeek rows.</summary>
    public string? CurrentWeekType { get; set; }

    /// <summary>The current week's exact persisted CatalogPhaseKey, but ONLY when CurrentWeekType is "preparation_runway" — always null for a Core week, never CatalogPhaseKey's Core-phase value (FOUNDATION/BUILD/etc).</summary>
    public string? CurrentRunwayBlock { get; set; }
}

public class TrainingDayResponse
{
    /// <summary>Null for a synthetic rest/no-session day not backed by a persisted TrainingDay row.</summary>
    public Guid? DayId { get; set; }
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

    // ── Backend Integration Phase 4G.6D — additive, mapped from the day's
    // owning TrainingWeek. Null for a synthetic (non-persisted) rest day,
    // since there is no owning TrainingWeek to map from. ──
    public int? WeekNumber { get; set; }
    public string? WeekType { get; set; }
    public string? RunwayBlock { get; set; }
}

public class DailyTipResponse
{
    public string TipKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? WorkoutType { get; set; }
}
