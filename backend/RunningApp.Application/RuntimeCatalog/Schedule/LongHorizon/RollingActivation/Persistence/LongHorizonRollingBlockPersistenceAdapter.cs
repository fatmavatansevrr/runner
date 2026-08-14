namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2 Part 24 -- translates a real blocked checkpoint/JIT reason
/// into a durable block record. Creates no executable sessions.
/// </summary>
internal sealed class LongHorizonRollingBlockPersistenceAdapter
{
    private readonly ILongHorizonRollingStateRepository _repository;

    public LongHorizonRollingBlockPersistenceAdapter(ILongHorizonRollingStateRepository repository) => _repository = repository;

    public Task<LongHorizonRollingPersistenceResult> PersistBlockAsync(
        Guid planStateId,
        uint expectedConcurrencyVersion,
        int blockedGlobalWeekStart,
        int blockedGlobalWeekEnd,
        LongHorizonReasonCode reason,
        string publicReasonCategory,
        string evidenceFingerprint,
        DateOnly checkpointDate,
        bool retryEligible,
        Guid? relatedDecisionId = null,
        CancellationToken cancellationToken = default)
    {
        var request = new LongHorizonRollingBlockPersistenceRequest
        {
            PlanStateId = planStateId,
            ExpectedConcurrencyVersion = expectedConcurrencyVersion,
            BlockedGlobalWeekStart = blockedGlobalWeekStart,
            BlockedGlobalWeekEnd = blockedGlobalWeekEnd,
            PublicReasonCategory = publicReasonCategory,
            InternalReasonCode = reason.Code,
            EvidenceFingerprint = evidenceFingerprint,
            CheckpointDate = checkpointDate,
            RetryEligible = retryEligible,
            RelatedDecisionId = relatedDecisionId,
            IdempotencyKey = $"block:{planStateId}:{blockedGlobalWeekStart}-{blockedGlobalWeekEnd}:{checkpointDate:O}",
        };

        return _repository.SaveBlockAsync(request, cancellationToken);
    }
}
