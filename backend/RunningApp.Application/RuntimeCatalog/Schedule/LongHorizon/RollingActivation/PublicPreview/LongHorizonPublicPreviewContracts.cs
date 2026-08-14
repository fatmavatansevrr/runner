namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview;

/// <summary>
/// Phase 4L.1 Part 4 — the public lifecycle taxonomy. Deliberately narrower
/// than <see cref="LongHorizonNumericLifecycleState"/>: internal states with
/// no public meaning (target-lock/context-refresh/prescription-created)
/// never appear here, and <c>StructurallyPlanned</c> collapses into
/// <see cref="Pending"/> (both mean "not yet executable", a distinction the
/// public contract has no product reason to expose).
/// </summary>
public enum LongHorizonPublicLifecycleStatus
{
    Available,
    Pending,
    Blocked,
    Completed,
    Missed,
}

/// <summary>Public-safe rename of <see cref="LongHorizonStructuralSegmentType"/> -- same values, public-facing name.</summary>
public enum LongHorizonPublicPhase
{
    GeneralEndurance,
    PreparationRunway,
    Core,
}

public enum LongHorizonPreviewReadiness
{
    ReadyForPublicPreview,
    PublicPreviewBlocked,
}

public enum LongHorizonConfirmationReadiness
{
    NotReadyForConfirmation,
    ReadyForRollingPersistence,
    ReadyForLegacyFullPersistence,
}

/// <summary>Phase 4L.1 Part 9 -- exactly one primary public category per blocked state.</summary>
public enum LongHorizonPublicBlockedReasonCategory
{
    MoreTrainingDataNeeded,
    CompleteCurrentWeek,
    UpdateAvailability,
    SafetyReviewRequired,
    PaceInformationNeeded,
    PlanTransitionUnavailable,
}

/// <summary>Phase 4L.1 Part 21 -- explains behavior, never implementation.</summary>
public enum LongHorizonPublicProvenance
{
    GeneratedFromRecentTraining,
    GeneratedFromInitialProfile,
    UpdatedAfterCompletedTraining,
    AwaitingMoreTrainingData,
}

public sealed record LongHorizonExecutableSessionContract
{
    public required DateOnly SessionDate { get; init; }
    public required DayOfWeek Weekday { get; init; }
    public required string SessionRole { get; init; }
    public required double DistanceKm { get; init; }
    public required bool IsLongRun { get; init; }
    public required LongHorizonPublicLifecycleStatus ExecutableStatus { get; init; }
}

public sealed record LongHorizonExecutableWeekContract
{
    public required int GlobalWeek { get; init; }
    public required LongHorizonPublicPhase Phase { get; init; }
    public required string Stage { get; init; }
    public required DateOnly WeekStartDate { get; init; }
    public required DateOnly WeekEndDate { get; init; }
    public required double WeeklyVolumeKm { get; init; }
    public required double LongRunVolumeKm { get; init; }
    public required LongHorizonPublicLifecycleStatus LifecycleStatus { get; init; }
    public required IReadOnlyList<LongHorizonExecutableSessionContract> Sessions { get; init; }
    public required LongHorizonPublicProvenance PublicProvenanceSummary { get; init; }
}

/// <summary>
/// Phase 4L.1 Part 6 -- StructuralStartDate/EndDate are pure calendar
/// arithmetic from the plan's own StartDate + (GlobalWeek-1)*7, never a
/// value read off internally computed session/materialization output --
/// this is what keeps them safe to expose even for Pending weeks.
/// </summary>
public sealed record LongHorizonStructuralRoadmapWeekContract
{
    public required int GlobalWeek { get; init; }
    public required LongHorizonPublicPhase Phase { get; init; }
    public string? Stage { get; init; }
    public required LongHorizonPublicLifecycleStatus LifecycleStatus { get; init; }
    public required bool IsExecutable { get; init; }
    public required DateOnly StructuralStartDate { get; init; }
    public required DateOnly StructuralEndDate { get; init; }
    public required bool NumericDetailsAvailable { get; init; }
    public required string PublicSummary { get; init; }
}

public sealed record LongHorizonBlockedStateContract
{
    public LongHorizonPublicLifecycleStatus Status { get; init; } = LongHorizonPublicLifecycleStatus.Blocked;
    public required LongHorizonPublicBlockedReasonCategory ReasonCategory { get; init; }
    public required bool RetryEligible { get; init; }
    public required string NextActionKey { get; init; }
    public required DateOnly LastEvaluatedDate { get; init; }
}

/// <summary>
/// Phase 4L.1 Part 5 -- the public plan-level preview contract. Carries no
/// internal target-lock/prescription/context IDs, no full future numeric
/// schedule, no internal diagnostic trace, no persistence entity leakage
/// (enforced by <see cref="LongHorizonPublicPreviewContractValidator"/> and
/// the dedicated leakage-guard tests).
/// </summary>
public sealed record LongHorizonPlanPreviewContract
{
    public int ContractVersion { get; init; } = 1;
    public required Guid PreviewId { get; init; }
    public RunningApp.Domain.Enums.PlanScheduleStrategy ScheduleStrategy { get; init; } = RunningApp.Domain.Enums.PlanScheduleStrategy.RollingLongHorizon;
    public DateTime GeneratedAtUtc { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public required string GoalType { get; init; }
    public required string GoalDistance { get; init; }
    public required int TotalWeeks { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EstimatedEndDate { get; init; }
    public required DateOnly RaceDate { get; init; }
    public required int DaysPerWeek { get; init; }
    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDay { get; init; }
    public required string ReadinessProfile { get; init; }
    public required int CurrentWindowStartWeek { get; init; }
    public required int CurrentWindowEndWeek { get; init; }
    public required int CurrentExecutableWeekCount { get; init; }
    public required IReadOnlyList<LongHorizonStructuralRoadmapWeekContract> StructuralRoadmap { get; init; }
    public required IReadOnlyList<LongHorizonExecutableWeekContract> CurrentExecutableWeeks { get; init; }
    public required LongHorizonPreviewReadiness PreviewReadiness { get; init; }
    public required LongHorizonConfirmationReadiness ConfirmationReadiness { get; init; }
    public required IReadOnlyList<string> PublicWarnings { get; init; }
    public required LongHorizonPublicProvenance ProvenanceSummary { get; init; }
    public LongHorizonBlockedStateContract? BlockedState { get; init; }
}
