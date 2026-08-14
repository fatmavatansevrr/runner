using RunningApp.Domain.Enums;
using System;

namespace RunningApp.Application.DTOs.TrainingDay;

public class TrainingDayDetailResponse
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
    public DateTime? CompletedAt { get; set; }

    // ── Backend Integration Phase 4G.6D — additive, mapped from the day's
    // owning TrainingWeek and the day's own persisted Source/AdaptedFromId.
    // Never fabricated: RunwayBlock is null for every Core day; Source is
    // the real persisted enum value, never assumed to always be Template. ──
    public int? WeekNumber { get; set; }
    public string? WeekType { get; set; }
    public string? RunwayBlock { get; set; }
    public string? Source { get; set; }
    public Guid? AdaptedFromId { get; set; }
}
