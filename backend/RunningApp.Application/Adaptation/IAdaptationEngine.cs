using RunningApp.Domain.Enums;

namespace RunningApp.Application.Adaptation;

/// <summary>
/// Interface for the Adaptive Engine.
/// Phase 1: only PlaceholderAdaptationEngine implements this.
/// Real implementation comes in a future phase.
/// </summary>
public interface IAdaptationEngine
{
    Task<AdaptationDecision> EvaluateNotTodayAsync(
        Guid planId,
        Guid trainingDayId,
        TriggerSource trigger,
        string? reason,
        CancellationToken ct = default);

    Task<AdaptationDecision> EvaluatePendingConfirmationAsync(
        Guid planId,
        Guid trainingDayId,
        bool wasCompleted,
        CancellationToken ct = default);
}
