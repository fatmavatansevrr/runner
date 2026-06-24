using RunningApp.Domain.Enums;
using System;

namespace RunningApp.Application.DTOs.PendingConfirmation;

public class PendingConfirmationResponse
{
    public Guid PendingConfirmationId { get; set; }
    public Guid TrainingDayId { get; set; }
    public DateTime Date { get; set; }
    public TrainingDayType DayType { get; set; }
    public string Title { get; set; } = string.Empty;
    public double PlannedDistanceKm { get; set; }
    public int PlannedDurationMin { get; set; }
}
