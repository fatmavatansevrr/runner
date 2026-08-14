using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Domain.Enums;
using PreparationRunwayNs = RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>Phase 4K.8C's two typed outcomes.</summary>
internal enum LongHorizonRollingJitCompositionOutcome
{
    CompositionAndActivationSucceeded,
    CompositionBlocked,
}

/// <summary>
/// Phase 4K.8C Part 1 — the composition orchestrator's request. Deliberately
/// does NOT accept a Core Week-1 target, available Core weeks, or resolved
/// condition results as caller inputs (Phase 4K.8's own request did) --
/// those are produced internally, for real, by this orchestrator.
/// </summary>
internal sealed record LongHorizonRollingJitCompositionRequest
{
    public required LongHorizonStructuralRoadmap StructuralRoadmap { get; init; }
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public required RollingNumericActivationWindow PreviousActivatedWindow { get; init; }
    public required LongHorizonCheckpointEvidenceSnapshot EvidenceSnapshot { get; init; }
    public required ValidatedSustainableLoad ValidatedLoad { get; init; }
    public int? ExactCompletedFrequency { get; init; }
    public LongHorizonCheckpointDecision? GeCheckpointDecision { get; init; }
    public IReadOnlyList<ActivatedNumericWeek>? GeActivatedWeeks { get; init; }
    public required LongHorizonContextVersion PreviousContextVersion { get; init; }
    public required ReadinessProfile ReadinessProfile { get; init; }
    public required IReadOnlyList<DayOfWeek> CurrentAvailability { get; init; }
    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDay { get; init; }
    public required DateOnly CheckpointDate { get; init; }
    public required DateOnly PlanStartDate { get; init; }
    public required DateOnly RaceDate { get; init; }
    public int? TargetFinishTimeSeconds { get; init; }
    public TargetFinishTimeSource? TargetFinishTimeSource { get; init; }
    public RecentRaceInput? RecentRace { get; init; }
    public required LongHorizonSafetyState SafetyState { get; init; }

    /// <summary>Reused, never regenerated, on Runway-continuation windows.</summary>
    public LongHorizonLockedCoreWeekOneTarget? ExistingLockedCoreTarget { get; init; }
    public PreparationRunwayNs.ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType>? ExistingRunwayPrescription { get; init; }
    public LongHorizonLockedRunwayCalendarProjection? ExistingRunwayCalendarProjection { get; init; }

    /// <summary>Real catalog dependencies the unchanged production pipeline already requires.</summary>
    public required string CatalogRootPath { get; init; }
    public required RunningApp.Application.RuntimeCatalog.PlanCatalogCandidateSummary Candidate { get; init; }
}

/// <summary>
/// Phase 4K.8C Part 6 — a bounded selection of the real generated Core
/// weeks that leave the internal composition boundary. Unselected weeks
/// remain internal (never attached to pending roadmap weeks, never
/// persisted, never checkpoint evidence).
/// </summary>
internal sealed record LongHorizonBoundedCorePrescriptionSelection
{
    public required Guid CoreContextId { get; init; }
    public required LongHorizonContextVersion CoreContextVersion { get; init; }
    public required string FullCoreResultProvenance { get; init; }
    public required int SelectedStartGlobalWeek { get; init; }
    public required int SelectedEndGlobalWeek { get; init; }
    public required IReadOnlyList<LongHorizonJitCoreCandidateWeek> SelectedWeeks { get; init; }
    public bool NonExecutableUntilActivation { get; init; } = true;
}

/// <summary>Phase 4K.8C Part 15 — the composition orchestrator's result. Never exposed publicly.</summary>
internal sealed record LongHorizonRollingJitCompositionResult
{
    public required LongHorizonRollingJitCompositionOutcome Outcome { get; init; }
    public LongHorizonRollingJitActivationResult? ActivationResult { get; init; }
    public IReadOnlyList<RuntimeConditionResolutionResult>? ResolvedConditionResults { get; init; }
    public string? CoreGenerationProvenance { get; init; }
    public PreparationRunwayCoreWeekOneNumericTarget? ExtractedCoreWeekOneTarget { get; init; }
    public LongHorizonBoundedCorePrescriptionSelection? BoundedCoreSelection { get; init; }
    public TenKPreparationRunwayDarkOrchestrationResult? RealCompositionResult { get; init; }
    public IReadOnlyList<LongHorizonActivatedSessionCalendarProjection>? ActivatedSessionCalendarProjection { get; init; }
    public LongHorizonLockedRunwayCalendarProjection? FullRunwayCalendarProjection { get; init; }
    public Guid? CalendarProjectionId { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public LongHorizonReasonCode? AuthoritativeReason { get; init; }
    public string? InternalDiagnostic { get; init; }
    public required IReadOnlyList<string> ValidationStages { get; init; }
}
