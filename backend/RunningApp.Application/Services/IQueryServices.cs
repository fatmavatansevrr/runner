using RunningApp.Application.DTOs.Home;
using RunningApp.Application.DTOs.PendingConfirmation;
using RunningApp.Application.DTOs.Profile;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Application.Services;

public interface ICalendarQueryService
{
    Task<List<TrainingDayResponse>> GetCalendarAsync(Guid internalUserId, string month, CancellationToken ct = default);
}

public interface IPendingConfirmationService
{
    Task<List<PendingConfirmationResponse>> GetPendingConfirmationsAsync(Guid internalUserId, CancellationToken ct = default);
    Task<ResolvePendingConfirmationResponse> ResolvePendingConfirmationAsync(Guid internalUserId, ResolvePendingConfirmationRequest request, CancellationToken ct = default);
}

public interface IProfileService
{
    Task<ProfileOverviewResponse> GetProfileOverviewAsync(Guid internalUserId, CancellationToken ct = default);
}
