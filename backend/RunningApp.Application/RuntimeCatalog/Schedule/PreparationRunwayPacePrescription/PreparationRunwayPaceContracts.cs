using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;

internal enum PreparationRunwayPaceEvidenceState
{
    ProvidedRecentRace,
    TargetTimeUserProvided,
    TargetTimeProductAverage,
    MissingPaceEvidence,
    PaceSourceUnsupported,

    /// <summary>
    /// A target finish time was requested (PACE_SOURCE_IN=TARGET_TIME), but
    /// GOAL_FEASIBILITY_IN resolved UNSUPPORTED because CoreEntryReadiness
    /// was NOT_READY -- so CatalogPrescriptionContextBuilder correctly leaves
    /// the authoritative Core pace source Unresolved rather than deriving a
    /// numeric target-goal pace from an infeasible goal. This is exactly the
    /// population the runway's ConsistencyNeeded profile exists to serve, so
    /// it is a normal, expected evidence state here -- never a materializer
    /// failure, and distinct from a genuinely unsupported/broken source.
    /// </summary>
    TargetTimeRequestedGoalInfeasible,
}

internal sealed record PreparationRunwayPaceContext(
    string CandidateKey,
    int CandidateVersion,
    ResolvedPrescriptionPaceSource ResolvedPaceSource,
    PreparationRunwayPaceEvidenceState EvidenceState,
    TargetFinishTimeSource? TargetFinishTimeSource,
    string EvidenceProvenance,
    string ResolverDecision);

internal sealed record PreparationRunwayWorkoutPaceRule(
    string WorkoutId,
    int WorkoutVersion,
    CatalogPacePrescriptionKind PermittedRepresentation,
    CatalogPaceSourceSelection SelectedSource,
    string EffortLabel,
    string SemanticBasis);

internal sealed record PreparationRunwayPacePolicy(
    string PolicyKey,
    int PolicyVersion,
    IReadOnlyList<PreparationRunwayWorkoutPaceRule> WorkoutRules,
    string ContinuityRule);

internal sealed record PreparationRunwayCoreWeekOnePaceSlotTarget(
    PreparationRunwaySlotRole Role,
    int RoleOrdinal,
    string WorkoutId,
    int WorkoutVersion,
    CatalogPacePrescription PacePrescription,
    string PaceSourceProvenance);

internal sealed record PreparationRunwayCoreWeekOnePaceTarget(
    string CandidateKey,
    int CandidateVersion,
    IReadOnlyList<PreparationRunwayCoreWeekOnePaceSlotTarget> OrderedSlots,
    string SourceProvenance);

internal sealed record PreparationRunwayPaceMaterializationRequest<TKey>(
    PreparationRunwayCalendarCompositionResult<TKey> DatedCombinedPlan,
    PreparationRunwayPaceContext PaceContext,
    PreparationRunwayCoreWeekOnePaceTarget CoreWeekOnePaceTarget,
    PreparationRunwayPacePolicy Policy) where TKey : notnull;

internal sealed record PreparationRunwayPaceProvenance(
    PreparationRunwayPaceEvidenceState EvidenceState,
    PrescriptionPaceSource OriginalResolvedSource,
    TargetFinishTimeSource? TargetFinishTimeSource,
    string ResolverDecision,
    string WorkoutRule,
    string DerivationAndRounding,
    string ContinuityComparison);

internal sealed record PreparationRunwayPacedSlot<TKey>(
    PreparationRunwayDatedSlot<TKey> OriginalSlot,
    CatalogPacePrescription PacePrescription,
    PreparationRunwayPaceProvenance PaceProvenance) where TKey : notnull;

internal sealed record PreparationRunwayPacedWeek<TKey>(
    PreparationRunwayDatedWeek<TKey> OriginalWeek,
    IReadOnlyList<PreparationRunwayPacedSlot<TKey>> StructuralOrderedSlots,
    IReadOnlyList<PreparationRunwayPacedSlot<TKey>> ChronologicalSlots) where TKey : notnull;

internal sealed record PreparationRunwayPaceContinuityCheck(
    PreparationRunwaySlotRole Role,
    int RoleOrdinal,
    CatalogPacePrescription RunwayPace,
    CatalogPacePrescription CoreWeekOnePace,
    bool IsCompatible,
    string Comparison);

internal sealed record PreparationRunwayPaceContinuityAnalysis(
    IReadOnlyList<PreparationRunwayPaceContinuityCheck> SlotChecks,
    bool AllTransitionSlotsCompatible,
    bool NoRaceSpecificPace,
    bool NoGoalPace,
    bool NoThresholdPace,
    bool IsValid,
    string Provenance);

internal enum PreparationRunwayPaceMaterializationFailureCode
{
    InvalidPaceMaterializationRequest,
    PaceContextUnavailable,
    PaceSourceUnsupported,
    PaceEvidenceContradiction,
    CoreWeekOnePaceTargetUnavailable,
    WorkoutPacePolicyMissing,
    WorkoutPacePolicyIncompatible,
    RaceSpecificPaceNotAllowedInRunway,
    GoalPaceNotAllowedInRunway,
    ThresholdPaceNotAllowedInRunway,
    AerobicStrengthPaceUnresolved,
    PaceRangeInvalid,
    PaceContinuityViolation,
    DistanceMutationDetected,
    DateMutationDetected,
    PaceMaterializationInvariantViolation,
}

internal sealed record PreparationRunwayPaceMaterializationResult<TKey>(
    bool IsSuccess,
    IReadOnlyList<PreparationRunwayPacedWeek<TKey>>? PacedRunwayWeeks,
    PreparationRunwayCoreWeekOnePaceTarget? UnchangedCoreWeekOneTarget,
    PreparationRunwayPaceContinuityAnalysis? ContinuityAnalysis,
    PreparationRunwayPaceMaterializationFailureCode? FailureCode,
    string? FailureReason,
    IReadOnlyList<string> Trace) where TKey : notnull
{
    public static PreparationRunwayPaceMaterializationResult<TKey> Success(
        IReadOnlyList<PreparationRunwayPacedWeek<TKey>> weeks,
        PreparationRunwayCoreWeekOnePaceTarget coreTarget,
        PreparationRunwayPaceContinuityAnalysis continuity,
        IReadOnlyList<string> trace) =>
        new(true, weeks, coreTarget, continuity, null, null, trace);

    public static PreparationRunwayPaceMaterializationResult<TKey> Failure(
        PreparationRunwayPaceMaterializationFailureCode code,
        string reason,
        IReadOnlyList<string> trace) =>
        new(false, null, null, null, code, reason, trace);
}
