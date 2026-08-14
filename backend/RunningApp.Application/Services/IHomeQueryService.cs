namespace RunningApp.Application.Services;

public interface IHomeQueryService
{
    Task<object> GetHomeAsync(Guid internalUserId, CancellationToken ct = default);
}
