using System;

namespace RunningApp.Application.DTOs.PendingConfirmation;

public class ResolvePendingConfirmationResponse
{
    public Guid PendingConfirmationId { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Always false in Phase 1 — resolving a pending confirmation never
    /// mutates future training days.
    /// </summary>
    public bool PlanAdapted { get; set; }
}
