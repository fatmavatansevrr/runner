using RunningApp.Application.DTOs.TrainingDay;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Application.Services;

public interface ITrainingDayService
{
    Task<TrainingDayDetailResponse> GetTrainingDayDetailAsync(string userId, Guid trainingDayId, CancellationToken ct = default);
}

public interface IWorkoutCompletionService
{
    Task<CompleteWorkoutResponse> CompleteWorkoutAsync(string userId, Guid trainingDayId, CompleteWorkoutRequest request, CancellationToken ct = default);
}

public interface INotTodayService
{
    Task<CreateNotTodayDecisionResponse> CreateNotTodayDecisionAsync(string userId, Guid trainingDayId, CreateNotTodayDecisionRequest request, CancellationToken ct = default);
    Task<ConfirmNotTodayDecisionResponse> ConfirmNotTodayDecisionAsync(string userId, Guid decisionId, ConfirmNotTodayDecisionRequest request, CancellationToken ct = default);
}
