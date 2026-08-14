namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2 Part 25 -- persists an explicit Blocked -> Pending retry
/// restoration. The repository itself enforces the later-checkpoint and
/// changed-evidence requirements (see
/// <see cref="LongHorizonRollingStateRepository.SaveRetryRestorationAsync"/>);
/// this adapter is a thin, evidence-carrying entry point. Direct
/// Blocked -> Activated is never reachable through this type.
/// </summary>
internal sealed class LongHorizonRollingRetryPersistenceAdapter
{
    private readonly ILongHorizonRollingStateRepository _repository;

    public LongHorizonRollingRetryPersistenceAdapter(ILongHorizonRollingStateRepository repository) => _repository = repository;

    public Task<LongHorizonRollingPersistenceResult> PersistRetryAsync(
        Guid planStateId,
        uint expectedConcurrencyVersion,
        int restoredGlobalWeekStart,
        int restoredGlobalWeekEnd,
        DateOnly retryCheckpointDate,
        string changedEvidenceFingerprint,
        Guid? relatedDecisionId = null,
        CancellationToken cancellationToken = default)
    {
        var request = new LongHorizonRollingRetryPersistenceRequest
        {
            PlanStateId = planStateId,
            ExpectedConcurrencyVersion = expectedConcurrencyVersion,
            RestoredGlobalWeekStart = restoredGlobalWeekStart,
            RestoredGlobalWeekEnd = restoredGlobalWeekEnd,
            RetryCheckpointDate = retryCheckpointDate,
            ChangedEvidenceFingerprint = changedEvidenceFingerprint,
            RelatedDecisionId = relatedDecisionId,
            IdempotencyKey = $"retry:{planStateId}:{restoredGlobalWeekStart}-{restoredGlobalWeekEnd}:{retryCheckpointDate:O}",
        };

        return _repository.SaveRetryRestorationAsync(request, cancellationToken);
    }
}
