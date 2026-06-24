using System;

namespace RunningApp.Application.DTOs.PendingConfirmation;

public class ResolvePendingConfirmationRequest
{
    public Guid PendingConfirmationId { get; set; }
    public string Resolution { get; set; } = string.Empty; // "completed" | "missed"
    public double? ActualDistanceKm { get; set; }
    public int? ActualDurationMin { get; set; }
    public string? UserNote { get; set; }
}
