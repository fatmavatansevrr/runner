using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

internal enum PreparationRunwayLoadEvidenceState
{
    Provided,
    Missing,
    NoRecentRunningBase,
}

internal enum PreparationRunwayQuantityUnit { Kilometers }

internal enum PreparationRunwayNumericRuleClassification
{
    DirectCanonicalRule,
    ReusedCoreBehavior,
    EvidenceInformedProductDefault,
    ProductDefault,
    ProvisionalProductDefault,
    RuntimeEvidence,
}

internal sealed record PreparationRunwayStartingLoadEvidence(
    PreparationRunwayLoadEvidenceState WeeklyVolumeState,
    double? RecentWeeklyVolumeKm,
    PreparationRunwayLoadEvidenceState LongestRunState,
    double? RecentLongestRunKm,
    int? RecentRunsPerWeek,
    string SourceProvenance);

internal sealed record PreparationRunwayCoreWeekOneSlotTarget(
    PreparationRunwaySlotRole Role,
    int RoleOrdinal,
    double DistanceKm);

internal sealed record PreparationRunwayCoreWeekOneNumericTarget(
    string CandidateKey,
    int CandidateVersion,
    double WeeklyVolumeKm,
    double LongRunDistanceKm,
    IReadOnlyList<PreparationRunwayCoreWeekOneSlotTarget> OrderedSlots,
    string SourceArtifact,
    string SourceProvenance);

internal sealed record PreparationRunwayNumericPolicy(
    string PolicyKey,
    int PolicyVersion,
    double MissingWeeklyVolumeDefaultKm,
    double NoRecentRunningBaseDefaultKm,
    double PreferredMaxWeeklyIncreaseRatio,
    double HardMaxWeeklyIncreaseRatio,
    double AbsoluteWeeklyIncrementCapKm,
    double LongRunPreferredMinimumShare,
    double LongRunPreferredMaximumShare,
    double LongRunSelectionShare,
    double LongRunHardCapShare,
    double RoundingIncrementKm,
    double ContinuityToleranceKm,
    string RoundingRule,
    IReadOnlyDictionary<string, PreparationRunwayNumericRuleClassification> RuleClassifications,
    double LongRunShareTolerance);

internal sealed record PreparationRunwayNumericMaterializationRequest<TKey>(
    PreparationRunwayAllocationProfile Profile,
    IReadOnlyList<PreparationRunwayMaterializedWeek<TKey>> MaterializedWeeks,
    PreparationRunwayStartingLoadEvidence StartingLoadEvidence,
    PreparationRunwayCoreWeekOneNumericTarget CoreWeekOneTarget,
    PreparationRunwayNumericPolicy Policy,
    PreparationRunwayQuantityUnit Unit) where TKey : notnull;

internal sealed record PreparationRunwayPrescribedSlot<TKey>(
    PreparationRunwayMaterializedWorkoutSlot<TKey> StructuralSlot,
    double PlannedDistanceKm,
    PreparationRunwayQuantityUnit Unit,
    string QuantityProvenance) where TKey : notnull;

internal sealed record PreparationRunwayNumericDecisionTrace(
    int RunwayWeekNumber,
    string BlockType,
    double UnroundedWeeklyVolumeKm,
    double RoundedWeeklyVolumeKm,
    double? PreviousWeeklyVolumeKm,
    double WeeklyChangeKm,
    double? WeeklyChangeRatio,
    double UnroundedLongRunKm,
    double RoundedLongRunKm,
    double LongRunShare,
    string TrajectoryRule,
    string AppliedConstraint,
    string RoundingRule,
    IReadOnlyList<string> ValueSources);

internal sealed record PreparationRunwayPrescribedWeek<TKey>(
    PreparationRunwayMaterializedWeek<TKey> StructuralWeek,
    double PlannedWeeklyVolumeKm,
    double PlannedLongRunDistanceKm,
    IReadOnlyList<PreparationRunwayPrescribedSlot<TKey>> OrderedSlots,
    PreparationRunwayNumericDecisionTrace NumericTrace) where TKey : notnull;

internal sealed record PreparationRunwayCoreContinuityAnalysis(
    double WeeklyVolumeChangeKm,
    double LongRunChangeKm,
    double KeySessionChangeKm,
    IReadOnlyList<double> EasySupportChangesKm,
    bool SessionCountMatches,
    bool IsWithinTolerance,
    double ToleranceKm,
    string SourceProvenance);

internal enum PreparationRunwayNumericMaterializationFailureCode
{
    InvalidNumericMaterializationRequest,
    MissingStartingLoadPolicy,
    InsufficientStartingLoadEvidence,
    InvalidRecentWeeklyVolume,
    InvalidRecentLongestRun,
    StartingLongRunExceedsWeeklyVolume,
    CoreWeekOneTargetUnavailable,
    RunwayProgressionInfeasible,
    WeeklyChangeLimitExceeded,
    LongRunShareViolation,
    LongRunContinuityViolation,
    SlotDistributionInfeasible,
    RoundingInvariantViolation,
    CoreEntryContinuityViolation,
    NumericMaterializationInvariantViolation,
}

internal sealed record PreparationRunwayNumericMaterializationResult<TKey>(
    bool IsSuccess,
    IReadOnlyList<PreparationRunwayPrescribedWeek<TKey>>? PrescribedWeeks,
    PreparationRunwayCoreContinuityAnalysis? ContinuityAnalysis,
    PreparationRunwayNumericMaterializationFailureCode? FailureCode,
    string? FailureReason,
    IReadOnlyList<string> Trace) where TKey : notnull
{
    public static PreparationRunwayNumericMaterializationResult<TKey> Success(
        IReadOnlyList<PreparationRunwayPrescribedWeek<TKey>> weeks,
        PreparationRunwayCoreContinuityAnalysis continuity,
        IReadOnlyList<string> trace) => new(true, weeks, continuity, null, null, trace);

    public static PreparationRunwayNumericMaterializationResult<TKey> Failure(
        PreparationRunwayNumericMaterializationFailureCode code,
        string reason,
        IReadOnlyList<string> trace) => new(false, null, null, code, reason, trace);
}
