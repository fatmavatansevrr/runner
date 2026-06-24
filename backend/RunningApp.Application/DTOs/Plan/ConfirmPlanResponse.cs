using System;

namespace RunningApp.Application.DTOs.Plan;

public class ConfirmPlanResponse
{
    public Guid PlanId { get; set; }
    public string Status { get; set; } = string.Empty;
}
