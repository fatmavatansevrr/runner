namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2 Part 15 -- durable persistence for the dark Long-Horizon
/// rolling plan subsystem. Never exposes EF entities beyond this
/// implementation; every method takes/returns Application-layer contracts.
/// Not registered for public DI by this phase (dark, unwired).
/// </summary>
internal interface ILongHorizonRollingStateRepository
{
    Task<LongHorizonRollingRestartSnapshot> InitializeStructuralStateAsync(
        LongHorizonRollingInitializationRequest request, CancellationToken cancellationToken = default);

    Task<LongHorizonRollingRestartSnapshot?> LoadRestartSnapshotAsync(
        Guid planStateId, CancellationToken cancellationToken = default);

    Task<LongHorizonRollingPersistenceResult> SaveActivationSuccessAsync(
        LongHorizonRollingActivationPersistenceRequest request, CancellationToken cancellationToken = default);

    Task<LongHorizonRollingPersistenceResult> SaveBlockAsync(
        LongHorizonRollingBlockPersistenceRequest request, CancellationToken cancellationToken = default);

    Task<LongHorizonRollingPersistenceResult> SaveRetryRestorationAsync(
        LongHorizonRollingRetryPersistenceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Phase 4L.2E -- the plan's current Active Core context row, or null if none exists yet.</summary>
    Task<LongHorizonActiveCoreContextSnapshot?> GetActiveCoreContextAsync(
        Guid planStateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 4L.2E -- stamps the evidence fingerprint that justified a refresh
    /// onto the newly created Active Core context row, so a future refresh
    /// eligibility check can compare against it. A narrow, explicit
    /// post-persist update, never touching activation/session/lifecycle rows.
    /// </summary>
    Task SetCoreContextEvidenceFingerprintAsync(
        Guid coreContextId, string evidenceFingerprint, CancellationToken cancellationToken = default);
}

/// <summary>Phase 4L.2E -- read-only projection of the plan's currently Active LongHorizonCoreContextRecord row.</summary>
internal sealed record LongHorizonActiveCoreContextSnapshot
{
    public required Guid CoreContextId { get; init; }
    public required int ContextVersionSequence { get; init; }
    public required int EffectiveFromGlobalWeek { get; init; }
    public required int EffectiveToGlobalWeek { get; init; }
    public required DateOnly AsOfDate { get; init; }
    public required string ConditionResultSummaryJson { get; init; }
    public string? EvidenceFingerprint { get; init; }
}
