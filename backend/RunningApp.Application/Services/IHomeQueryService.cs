using RunningApp.Application.DTOs.Home;

namespace RunningApp.Application.Services;

public interface IHomeQueryService
{
    Task<HomeResponse> GetHomeAsync(Guid internalUserId, CancellationToken ct = default);
}
