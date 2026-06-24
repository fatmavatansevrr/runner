namespace RunningApp.Application.DTOs.Bootstrap;

public class BootstrapResponse
{
    public bool IsAuthenticated { get; init; }
    public bool HasProfile { get; init; }
    public bool HasActivePlan { get; init; }
    public bool HasPendingConfirmations { get; init; }
    /// <summary>
    /// Possible values: Welcome | Onboarding | PlanSetup | PendingConfirmation | Home | NoActivePlan
    /// </summary>
    public string NextScreen { get; init; } = "Welcome";
}
