using System.Security.Cryptography;
using System.Text;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal sealed record LongHorizonBlockedActivationRetryRequest
{
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public required IReadOnlyList<int> BlockedGlobalWeeks { get; init; }
    public required DateOnly PreviousCheckpointDate { get; init; }
    public required DateOnly RetryCheckpointDate { get; init; }
    public required Guid PreviousDecisionId { get; init; }
    public required string PreviousEvidenceIdentity { get; init; }
    public required string RetryEvidenceIdentity { get; init; }
}

internal sealed record LongHorizonBlockedActivationRetryResult
{
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public required Guid RetryDecisionId { get; init; }
    public required IReadOnlyList<int> RestoredGlobalWeeks { get; init; }
}

internal interface ILongHorizonBlockedActivationRetryService
{
    LongHorizonBlockedActivationRetryResult RestorePendingEligibility(LongHorizonBlockedActivationRetryRequest request);
}

internal sealed class LongHorizonBlockedActivationRetryService : ILongHorizonBlockedActivationRetryService
{
    public LongHorizonBlockedActivationRetryResult RestorePendingEligibility(LongHorizonBlockedActivationRetryRequest request)
    {
        if (request.BlockedGlobalWeeks.Count == 0 || request.BlockedGlobalWeeks.Distinct().Count() != request.BlockedGlobalWeeks.Count)
            throw new LongHorizonIllegalLifecycleTransitionException("Retry requires a non-empty unique blocked-week selection.");
        if (request.RetryCheckpointDate <= request.PreviousCheckpointDate)
            throw new LongHorizonIllegalLifecycleTransitionException("Retry checkpoint date must be strictly later than the blocked decision date.");
        if (string.Equals(request.PreviousEvidenceIdentity, request.RetryEvidenceIdentity, StringComparison.Ordinal))
            throw new LongHorizonIllegalLifecycleTransitionException("Retry requires changed or newly resolved evidence/context.");

        var seed = string.Join('|', request.PreviousDecisionId, request.RetryCheckpointDate.ToString("yyyy-MM-dd"),
            request.RetryEvidenceIdentity, string.Join(',', request.BlockedGlobalWeeks.OrderBy(x => x)));
        var retryDecisionId = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(seed)).AsSpan(0, 16));
        if (retryDecisionId == request.PreviousDecisionId)
            throw new LongHorizonIllegalLifecycleTransitionException("Retry must have a new deterministic decision identity.");

        var lifecycle = new Dictionary<int, LongHorizonNumericLifecycleState>(request.LifecycleStates);
        foreach (var week in request.BlockedGlobalWeeks.OrderBy(x => x))
        {
            if (!lifecycle.TryGetValue(week, out var current))
                throw new LongHorizonIllegalLifecycleTransitionException($"Blocked retry week {week} is outside the lifecycle.");
            LongHorizonNumericLifecycleTransitionValidator.ValidateBlockedRecoveryTransition(
                current, LongHorizonNumericLifecycleState.NumericPending, hasNewCheckpointDecision: true);
            lifecycle[week] = LongHorizonNumericLifecycleState.NumericPending;
        }

        return new LongHorizonBlockedActivationRetryResult
        {
            LifecycleStates = lifecycle,
            RetryDecisionId = retryDecisionId,
            RestoredGlobalWeeks = request.BlockedGlobalWeeks.OrderBy(x => x).ToList(),
        };
    }
}
