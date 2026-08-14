namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2 Part 19 -- the restart snapshot. Deliberately wraps the exact
/// same <see cref="LongHorizonFullDarkLifecycleState"/> the Phase 4K.9 dark
/// runtimes already consume, rather than inventing a parallel shape --
/// "sufficient to call the existing Phase 4K runtimes without caller
/// fabrication" is satisfied structurally, not by field-by-field parity.
/// </summary>
internal sealed record LongHorizonRollingRestartSnapshot
{
    public required Guid PlanStateId { get; init; }
    public required DateOnly PlanStartDate { get; init; }
    public required LongHorizonFullDarkLifecycleState DarkState { get; init; }
    public required uint ConcurrencyVersion { get; init; }
    public required string CatalogRootPath { get; init; }
    public required RunningApp.Application.RuntimeCatalog.PlanCatalogCandidateSummary Candidate { get; init; }
}

internal enum LongHorizonRollingPersistenceOutcome
{
    Success,
    ConcurrencyConflict,
    IdempotentReplay,
    IntegrityViolation,
}

internal sealed record LongHorizonRollingPersistenceResult
{
    public required LongHorizonRollingPersistenceOutcome Outcome { get; init; }
    public LongHorizonRollingRestartSnapshot? Snapshot { get; init; }
    public string? FailureReason { get; init; }

    /// <summary>
    /// Phase 4M.4B.2A -- true when this Success/IdempotentReplay outcome
    /// represents a durable Block record, not a real window activation.
    /// <see cref="LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync"/>
    /// can decide internally (after the caller has already computed its own
    /// isBlockAttempt flag from the checkpoint runtime's outcome) that JIT/
    /// Runway composition itself failed and a Block must be persisted
    /// instead of an activation. Without this flag the caller has no way to
    /// distinguish that from a genuine activation, and reports "activated"
    /// while the plan's window never advances.
    /// </summary>
    public bool IsBlock { get; init; }
}

/// <summary>Phase 4L.2 Part 22 -- initial structural persistence request.</summary>
internal sealed record LongHorizonRollingInitializationRequest
{
    public required LongHorizonStructuralRoadmap StructuralRoadmap { get; init; }
    public required DateOnly PlanStartDate { get; init; }
    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDay { get; init; }
    public required RollingNumericActivationWindow InitialWindow { get; init; }
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public required IReadOnlyDictionary<int, ActivatedNumericWeek> ActivatedWeeks { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public required string CatalogRootPath { get; init; }
    public required RunningApp.Application.RuntimeCatalog.PlanCatalogCandidateSummary Candidate { get; init; }

    /// <summary>Deterministic identity: replaying the same roadmap+window for the same plan anchor must not duplicate rows.</summary>
    public required Guid PlanStateId { get; init; }
}

/// <summary>Phase 4L.2 Part 23 -- activation-success persistence request. Never recomputes numeric/calendar fields.</summary>
internal sealed record LongHorizonRollingActivationPersistenceRequest
{
    public required Guid PlanStateId { get; init; }
    public required uint ExpectedConcurrencyVersion { get; init; }
    public required RollingNumericActivationWindow ActivatedWindow { get; init; }
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public Guid? CheckpointDecisionId { get; init; }
    public LongHorizonCheckpointRecordInput? Checkpoint { get; init; }
    public LongHorizonRunwayStateInput? RunwayState { get; init; }
    public LongHorizonCoreContextInput? CoreContext { get; init; }
    public required string IdempotencyKey { get; init; }
}

internal sealed record LongHorizonCheckpointRecordInput
{
    public required DateOnly AsOfDate { get; init; }
    public required int SourceWindowStartWeek { get; init; }
    public required int SourceWindowEndWeek { get; init; }
    public required string EvidenceFingerprint { get; init; }
    public double? ValidatedWeeklyVolumeKm { get; init; }
    public double? ValidatedLongRunKm { get; init; }
    public int? CompletedFrequency { get; init; }
    public required string AuthorityClassification { get; init; }
    public required RunningApp.Domain.Enums.LongHorizonPersistedCheckpointDecision Decision { get; init; }
    public string? AuthoritativeReasonCode { get; init; }
}

internal sealed record LongHorizonRunwayStateInput
{
    public required Guid TargetLockId { get; init; }
    public required int TargetContextVersionSequence { get; init; }
    public required int LockedRunwayStartGlobalWeek { get; init; }
    public required int LockedRunwayEndGlobalWeek { get; init; }
    public required double CoreWeekOneWeeklyTargetKm { get; init; }
    public required double CoreWeekOneLongRunTargetKm { get; init; }
    public required string FullPrescriptionId { get; init; }
    public required int FullPrescriptionVersion { get; init; }
    public required string PrescriptionPayloadJson { get; init; }
    public required string CalendarCompositionIdentity { get; init; }
    public string? CalendarProjectionPayloadJson { get; init; }
    public string? TargetLockPayloadJson { get; init; }
}

internal sealed record LongHorizonCoreContextInput
{
    public required Guid CoreContextId { get; init; }
    public required int ContextVersionSequence { get; init; }
    public required int EffectiveFromGlobalWeek { get; init; }
    public required int EffectiveToGlobalWeek { get; init; }
    public required DateOnly AsOfDate { get; init; }
    public required string ConditionResultSummaryJson { get; init; }
    public required string ValidatedLoadAuthoritySummary { get; init; }
    public required string GeneratedCoreResultIdentity { get; init; }
    public required string SelectedCoreWeeksPayloadJson { get; init; }
    public string? EvidenceFingerprint { get; init; }
}

/// <summary>Phase 4L.2 Part 24 -- block persistence request. Creates no executable sessions.</summary>
internal sealed record LongHorizonRollingBlockPersistenceRequest
{
    public required Guid PlanStateId { get; init; }
    public required uint ExpectedConcurrencyVersion { get; init; }
    public required int BlockedGlobalWeekStart { get; init; }
    public required int BlockedGlobalWeekEnd { get; init; }
    public required string PublicReasonCategory { get; init; }
    public required string InternalReasonCode { get; init; }
    public required string EvidenceFingerprint { get; init; }
    public required DateOnly CheckpointDate { get; init; }
    public required bool RetryEligible { get; init; }
    public Guid? RelatedDecisionId { get; init; }
    public required string IdempotencyKey { get; init; }
}

/// <summary>Phase 4L.2 Part 25 -- retry restoration persistence request.</summary>
internal sealed record LongHorizonRollingRetryPersistenceRequest
{
    public required Guid PlanStateId { get; init; }
    public required uint ExpectedConcurrencyVersion { get; init; }
    public required int RestoredGlobalWeekStart { get; init; }
    public required int RestoredGlobalWeekEnd { get; init; }
    public required DateOnly RetryCheckpointDate { get; init; }
    public required string ChangedEvidenceFingerprint { get; init; }
    public Guid? RelatedDecisionId { get; init; }
    public required string IdempotencyKey { get; init; }
}
