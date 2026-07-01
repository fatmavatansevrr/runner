using RunningApp.Application.DTOs.TrainingDay;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Application.Services;

public interface ITrainingDayService
{
    Task<TrainingDayDetailResponse> GetTrainingDayDetailAsync(Guid internalUserId, Guid trainingDayId, CancellationToken ct = default);
}

public interface IWorkoutCompletionService
{
    Task<CompleteWorkoutResponse> CompleteWorkoutAsync(Guid internalUserId, Guid trainingDayId, CompleteWorkoutRequest request, CancellationToken ct = default);
}

public interface INotTodayService
{
    Task<CreateNotTodayDecisionResponse> CreateNotTodayDecisionAsync(Guid internalUserId, Guid trainingDayId, CreateNotTodayDecisionRequest request, CancellationToken ct = default);
    Task<ConfirmNotTodayDecisionResponse> ConfirmNotTodayDecisionAsync(Guid internalUserId, Guid decisionId, ConfirmNotTodayDecisionRequest request, CancellationToken ct = default);
}
