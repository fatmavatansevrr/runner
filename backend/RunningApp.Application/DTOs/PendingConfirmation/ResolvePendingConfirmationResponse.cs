using System;

namespace RunningApp.Application.DTOs.PendingConfirmation;

public class ResolvePendingConfirmationResponse
{
    public Guid PendingConfirmationId { get; set; }
    public string Status { get; set; } = string.Empty;
}
