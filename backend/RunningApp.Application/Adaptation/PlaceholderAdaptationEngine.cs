using RunningApp.Domain.Enums;

namespace RunningApp.Application.Adaptation;

/// <summary>
/// Phase 1 placeholder. Always returns NoChange with supportive messaging.
/// Does NOT mutate any future training days.
/// Replace this with the real engine in a future phase.
/// </summary>
public sealed class PlaceholderAdaptationEngine : IAdaptationEngine
{
    public Task<AdaptationDecision> EvaluateNotTodayAsync(
        Guid planId,
        Guid trainingDayId,
        TriggerSource trigger,
        string? reason,
        CancellationToken ct = default)
    {
        var decision = new AdaptationDecision
        {
            Action = AdaptationAction.NoChange,
            TriggerSource = trigger,
            PlanAdapted = false,
            Title = "No problem",
            Message = "Taking a rest today won't define your progress.",
            SupportiveText = "Your plan will continue from here.",
            AffectedDays = []
        };

        return Task.FromResult(decision);
    }

    public Task<AdaptationDecision> EvaluatePendingConfirmationAsync(
        Guid planId,
        Guid trainingDayId,
        bool wasCompleted,
        CancellationToken ct = default)
    {
        var decision = new AdaptationDecision
        {
            Action = AdaptationAction.NoChange,
            TriggerSource = TriggerSource.PendingConfirmation,
            PlanAdapted = false,
            Title = wasCompleted ? "Great, logged!" : "No worries",
            Message = wasCompleted
                ? "That run has been marked as completed."
                : "One missed run doesn't define your progress.",
            SupportiveText = "Your plan continues from here.",
            AffectedDays = []
        };

        return Task.FromResult(decision);
    }
}
