using System;

namespace RunningApp.Application.DTOs.Plan;

public class ConfirmPlanResponse
{
    public Guid PlanId { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// True when this response refers to a plan that was already active
    /// before this call — confirm did not create a new plan because the
    /// single-active-plan rule was already satisfied.
    /// </summary>
    public bool AlreadyActive { get; set; }
}
