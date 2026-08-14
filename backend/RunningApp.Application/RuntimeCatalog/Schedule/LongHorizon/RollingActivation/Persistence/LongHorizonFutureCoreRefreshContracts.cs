namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>Phase 4L.2E Part 4 -- internal request for a future-only Core context refresh. Never a public DTO.</summary>
internal sealed record LongHorizonFutureCoreRefreshRequest
{
    public required Guid PlanStateId { get; init; }
    public required uint ExpectedAggregateVersion { get; init; }
    public required DateOnly RequestedAsOfDate { get; init; }
    public required IReadOnlyList<LongHorizonTrainingDayEvidenceRow> TrainingDayEvidence { get; init; }
    public required IReadOnlyList<DayOfWeek> CurrentAvailability { get; init; }
    public required DayOfWeek LongRunDay { get; init; }
    public required LongHorizonSafetyState SafetyState { get; init; }
    public required DateOnly PlanStartDate { get; init; }
    public required DateOnly RaceDate { get; init; }
    public required string CatalogRootPath { get; init; }
}

/// <summary>Phase 4L.2E Part 11 -- typed outcomes for a future-only Core context refresh.</summary>
internal enum LongHorizonFutureCoreRefreshOutcome
{
    Refreshed,
    IdempotentReplay,
    Ineligible,
    Blocked,
    StaleVersion,
    CorruptState,
}

/// <summary>Phase 4L.2E Part 11 -- result of a future-only Core context refresh attempt.</summary>
internal sealed record LongHorizonFutureCoreRefreshResult
{
    public required LongHorizonFutureCoreRefreshOutcome Outcome { get; init; }
    public Guid? PreviousContextId { get; init; }
    public Guid? NewContextId { get; init; }
    public int? PreviousContextVersion { get; init; }
    public int? NewContextVersion { get; init; }
    public int? EffectiveFromGlobalWeek { get; init; }
    public int? EffectiveToGlobalWeek { get; init; }
    public LongHorizonCoreRefreshEligibilityResult? Eligibility { get; init; }
    public LongHorizonRollingPersistenceResult? PersistenceResult { get; init; }
    public required IReadOnlyList<string> ValidationStages { get; init; }
}
