using RunningApp.Application.DTOs.Plan;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Application.Services;

public interface IPlanPreviewService
{
    Task<GeneratePreviewResponse> GeneratePreviewAsync(Guid internalUserId, GeneratePreviewRequest request, CancellationToken ct = default);
}

public interface IPlanConfirmationService
{
    Task<ConfirmPlanResponse> ConfirmPlanAsync(Guid internalUserId, ConfirmPlanRequest request, CancellationToken ct = default);
}

public interface IPlanManagementService
{
    Task<CancelPlanResponse> CancelPlanAsync(Guid internalUserId, Guid planId, CancelPlanRequest request, CancellationToken ct = default);
    Task<PlanDetailsResponse> GetActivePlanDetailsAsync(Guid internalUserId, CancellationToken ct = default);
}
