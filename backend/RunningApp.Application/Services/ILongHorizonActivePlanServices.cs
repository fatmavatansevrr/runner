using RunningApp.Application.DTOs.Plan;

namespace RunningApp.Application.Services;

public interface ILongHorizonActiveReadModelProvider
{
    Task<LongHorizonHomeResponse> GetHomeAsync(Guid internalUserId, Guid planId, CancellationToken ct = default);
    Task<LongHorizonCalendarResponse> GetCalendarAsync(Guid internalUserId, Guid planId, string month, CancellationToken ct = default);
    Task<LongHorizonRollingSessionDetailResponse> GetSessionDetailAsync(Guid internalUserId, Guid sessionId, CancellationToken ct = default);
}

public interface ILongHorizonRollingSessionMutationService
{
    Task<LongHorizonSessionMutationResponse> CompleteAsync(Guid internalUserId, Guid sessionId, LongHorizonCompleteSessionRequest request, CancellationToken ct = default);
    Task<LongHorizonSessionMutationResponse> MarkNotTodayAsync(Guid internalUserId, Guid sessionId, LongHorizonNotTodayRequest request, CancellationToken ct = default);
}

public interface ILongHorizonRollingWindowActivationService
{
    Task<LongHorizonActivateNextWindowResponse> ActivateNextWindowAsync(Guid internalUserId, LongHorizonActivateNextWindowRequest request, CancellationToken ct = default);
}

public interface ILongHorizonRollingRetryContinuationService
{
    Task<LongHorizonRetryContinuationResponse> RetryAsync(Guid internalUserId, LongHorizonRetryContinuationRequest request, CancellationToken ct = default);
}
