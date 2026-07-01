using RunningApp.Application.DTOs.Bootstrap;

namespace RunningApp.Application.Services;

/// <summary>
/// Determines where the app should route on startup.
/// Phase 1: uses mock userId until real auth is wired.
/// </summary>
public interface IBootstrapService
{
    Task<BootstrapResponse> GetBootstrapAsync(Guid internalUserId, CancellationToken ct = default);
}
