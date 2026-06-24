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
    Task<List<TrainingDayResponse>> GetCalendarAsync(string userId, string month, CancellationToken ct = default);
}

public interface IPendingConfirmationService
{
    Task<List<PendingConfirmationResponse>> GetPendingConfirmationsAsync(string userId, CancellationToken ct = default);
    Task<ResolvePendingConfirmationResponse> ResolvePendingConfirmationAsync(string userId, ResolvePendingConfirmationRequest request, CancellationToken ct = default);
}

public interface IProfileService
{
    Task<ProfileOverviewResponse> GetProfileOverviewAsync(string userId, CancellationToken ct = default);
}
