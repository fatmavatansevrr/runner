using RunningApp.Application.DTOs.Plan;

namespace RunningApp.Application.Services;

public interface IPlanPreviewService
{
    Task<GeneratePreviewResponse> GeneratePreviewAsync(string userId, GeneratePreviewRequest request, CancellationToken ct = default);
}

public interface IPlanConfirmationService
{
    Task<ConfirmPlanResponse> ConfirmPlanAsync(string userId, ConfirmPlanRequest request, CancellationToken ct = default);
}

public interface IPlanManagementService
{
    Task<CancelPlanResponse> CancelPlanAsync(string userId, Guid planId, CancelPlanRequest request, CancellationToken ct = default);
    Task<object> GetActivePlanDetailsAsync(string userId, CancellationToken ct = default);
}
