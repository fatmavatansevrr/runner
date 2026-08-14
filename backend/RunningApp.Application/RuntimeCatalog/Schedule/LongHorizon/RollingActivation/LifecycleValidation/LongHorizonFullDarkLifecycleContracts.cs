using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal enum LongHorizonFullDarkLifecycleOutcome
{
    CompletedSuccessfully,
    BlockedAsExpected,
    RetryRecoveredAndCompleted,
    ValidationFailed,
}

internal enum LongHorizonLifecycleAuditEventType
{
    StructuralRoadmapCreated,
    InitialWindowActivated,
    CheckpointSnapshotCreated,
    ValidatedLoadCreated,
    GrowthDecisionMade,
    MaintenanceDecisionMade,
    WindowBlocked,
    BlockRetryRequested,
    BlockRestoredToPending,
    GeWindowActivated,
    RunwayTargetLocked,
    RunwayPrescriptionCreated,
    RunwaySliceActivated,
    CoreContextCreated,
    CoreContextRefreshed,
    MixedWindowActivated,
    CoreWindowActivated,
    CalendarProjectionAligned,
    LifecycleCompleted,
}

internal sealed record LongHorizonLifecycleSessionOutcome
{
    public required TrainingDayStatus Status { get; init; }
    public double ActualDistanceMultiplier { get; init; } = 1d;
    public double? ExplicitActualDistanceKm { get; init; }
}

internal sealed record LongHorizonLifecycleWindowEvidence
{
    public required int ActivationOrdinal { get; init; }
    public required DateOnly CheckpointDate { get; init; }
    public required IReadOnlyList<LongHorizonLifecycleSessionOutcome> SessionOutcomes { get; init; }
    public required LongHorizonSafetyState SafetyState { get; init; }
    public required IReadOnlyList<DayOfWeek> Availability { get; init; }
}

internal sealed record LongHorizonLifecycleScenario
{
    public required Guid ScenarioId { get; init; }
    public required int TotalWeeks { get; init; }
    public required ReadinessProfile ReadinessProfile { get; init; }
    public required LongHorizonGeEntryBaselineInput InitialOnboardingEvidence { get; init; }
    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDay { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly RaceDate { get; init; }
    public int? TargetFinishTimeSeconds { get; init; }
    public TargetFinishTimeSource? TargetFinishTimeSource { get; init; }
    public RecentRaceInput? RecentRace { get; init; }
    public required IReadOnlyDictionary<int, LongHorizonLifecycleWindowEvidence> EvidenceByActivationOrdinal { get; init; }
    public LongHorizonPriorValidatedAnchor? InitialPriorValidatedAnchor { get; init; }
    public int? ExpectedBlockedActivationOrdinal { get; init; }
    public IReadOnlyList<LongHorizonLifecycleWindowEvidence> RetryEvidence { get; init; } = [];
    public required LongHorizonFullDarkLifecycleOutcome ExpectedFinalOutcome { get; init; }
    public required string CatalogRootPath { get; init; }
    public required RunningApp.Application.RuntimeCatalog.PlanCatalogCandidateSummary Candidate { get; init; }
}

internal sealed record LongHorizonLifecycleAuditEvent
{
    public required LongHorizonLifecycleAuditEventType EventType { get; init; }
    public required Guid ScenarioId { get; init; }
    public (int Start, int End)? GlobalWindow { get; init; }
    public LongHorizonContextVersion? ContextVersion { get; init; }
    public Guid? DecisionId { get; init; }
    public required string Authority { get; init; }
    public required string Result { get; init; }
    public LongHorizonReasonCode? Reason { get; init; }
}

internal sealed record LongHorizonFullDarkLifecycleState
{
    public required LongHorizonStructuralRoadmap StructuralRoadmap { get; init; }
    public required LongHorizonGeneratedStructuralSkeleton StructuralSkeleton { get; init; }
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public required IReadOnlyDictionary<int, ActivatedNumericWeek> ActivatedWeeks { get; init; }
    public required RollingNumericActivationWindow CurrentWindow { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public LongHorizonCheckpointEvidenceSnapshot? LatestEvidenceSnapshot { get; init; }
    public ValidatedSustainableLoad? LatestValidatedLoad { get; init; }
    public required IReadOnlyList<LongHorizonCheckpointDecision> CheckpointDecisions { get; init; }
    public LongHorizonLockedCoreWeekOneTarget? RunwayTargetLock { get; init; }
    public ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType>? RunwayPrescription { get; init; }
    public LongHorizonLockedRunwayCalendarProjection? RunwayCalendarProjection { get; init; }
    public required IReadOnlyList<Guid> CoreContextIds { get; init; }
    public required IReadOnlyList<LongHorizonLifecycleAuditEvent> AuditEvents { get; init; }
    public required int ActivationInvocationCount { get; init; }
    public required int FullRunwayMaterializationCount { get; init; }
    public required DateOnly LastCheckpointDate { get; init; }
}

internal sealed record LongHorizonFullDarkLifecycleValidationResult
{
    public required LongHorizonFullDarkLifecycleOutcome Outcome { get; init; }
    public LongHorizonFullDarkLifecycleState? FinalState { get; init; }
    public LongHorizonReasonCode? AuthoritativeReason { get; init; }
    public string? InternalDiagnostic { get; init; }
    public required IReadOnlyList<LongHorizonFullDarkLifecycleState> StateSnapshots { get; init; }
    public required IReadOnlyList<string> ValidationStages { get; init; }
}

internal interface ILongHorizonFullDarkLifecycleHarness
{
    Task<LongHorizonFullDarkLifecycleValidationResult> RunLifecycleAsync(
        LongHorizonLifecycleScenario scenario,
        CancellationToken cancellationToken = default);
}
