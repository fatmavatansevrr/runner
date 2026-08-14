using RunningApp.Application.Commands.Plan;
using RunningApp.Application.DTOs.Plan;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Application.Services;

public interface IPlanPreviewService
{
    /// <summary>
    /// INTERNAL-ONLY as of the flow-specific contract refactor — no
    /// controller action calls this directly anymore. Kept for the catalog/
    /// legacy generation pipeline beneath <see cref="GenerateRacePlanPreviewAsync"/>/
    /// <see cref="GenerateHabitPlanPreviewAsync"/>, which build a
    /// <see cref="GeneratePreviewRequest"/> via <c>GeneratePreviewCommandMapper.ToInternalRequest</c>
    /// and delegate here unchanged.
    /// </summary>
    Task<GeneratePreviewResponse> GeneratePreviewAsync(Guid internalUserId, GeneratePreviewRequest request, CancellationToken ct = default);

    Task<GeneratePreviewResponse> GenerateRacePlanPreviewAsync(Guid internalUserId, RacePlanPreviewCommand command, CancellationToken ct = default);

    Task<GeneratePreviewResponse> GenerateHabitPlanPreviewAsync(Guid internalUserId, HabitPlanPreviewCommand command, CancellationToken ct = default);
}

public interface IPlanConfirmationService
{
    Task<ConfirmPlanResponse> ConfirmPlanAsync(Guid internalUserId, ConfirmPlanRequest request, CancellationToken ct = default);
}

public interface ILongHorizonPublicPlanService
{
    Task<RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview.LongHorizonPlanPreviewContract> GeneratePreviewAsync(
        Guid internalUserId, RacePlanPreviewCommand command, CancellationToken ct = default);

    Task<LongHorizonConfirmPlanResponse> ConfirmAsync(
        Guid internalUserId, LongHorizonConfirmPlanRequest request, CancellationToken ct = default);
}

public interface IPlanManagementService
{
    Task<CancelPlanResponse> CancelPlanAsync(Guid internalUserId, Guid planId, CancelPlanRequest request, CancellationToken ct = default);
    Task<PlanDetailsResponse> GetActivePlanDetailsAsync(Guid internalUserId, CancellationToken ct = default);
}
