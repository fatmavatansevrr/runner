using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Prescription;

internal enum PrescriptionInputState { Available, NotProvided, Incomplete, Invalid, Inconsistent, Stale, NotApplicable }

internal enum PrescriptionRuleOwnership
{
    CatalogFixed,
    CatalogBoundedRuntimeSelected,
    RuntimeUserDerived,
    RuntimeDefaulted,
    DerivedDisplayValue,
    Deferred,
    NotApplicable,
    Inconsistent
}

internal enum WeeklyVolumeAnchorSource { RecentFourWeekAverage, LevelConservativeDefault, NoRecentRunningDefault, CatalogMinimumDefault, Unresolved }

internal enum LongRunAnchorSource { Recent30DayLongestRun, WeeklyVolumeDerived, LevelConservativeDefault, Unresolved }

internal enum PrescriptionPaceSource
{
    RecentRace,
    TargetGoal,
    EstimatedCurrentFitness,
    PreferredPace,
    LevelEffortDefault,
    EffortOnly,
    Unresolved
}

internal enum WorkoutMeasurementBasis
{
    DistanceFirst,
    DurationFirst,
    ComponentFirst,
    CatalogModeSelected,
    Unresolved
}

internal enum TaperSharpenCapability
{
    Supported,
    SupportedWithRuntimeModeSelection,
    ContractExtensionRequired,
    CatalogRevisionRequired,
    Inconsistent
}

internal sealed class CatalogPrescriptionContractException : InvalidOperationException
{
    public string Code { get; }

    public CatalogPrescriptionContractException(string code, string message) : base(message)
    {
        Code = code;
    }
}

internal sealed record NormalizedDistanceInput(
    PrescriptionInputState State,
    double? Kilometers,
    string SourceField,
    string OriginalUnit,
    double? OriginalValue,
    int? WindowDays,
    IReadOnlyList<string> Issues);

internal sealed record NormalizedRecentRaceInput(
    PrescriptionInputState State,
    double? DistanceKm,
    int? FinishTimeSeconds,
    DateOnly? RaceDate,
    string RecencyState,
    IReadOnlyList<string> Issues);

internal sealed record NormalizedRunningReadiness(
    NormalizedDistanceInput WeeklyVolume,
    NormalizedDistanceInput LongestRun,
    PrescriptionInputState RecentRunsPerWeekState,
    int? RecentRunsPerWeek,
    NormalizedRecentRaceInput RecentRace,
    IReadOnlyList<string> Issues);

internal sealed record WeeklyVolumeAnchorDecision(
    WeeklyVolumeAnchorSource Source,
    IReadOnlyList<string> AvailableInputs,
    IReadOnlyList<string> RejectedAlternatives,
    string DefaultReason,
    string CatalogBoundsReference);

internal sealed record LongRunAnchorDecision(
    LongRunAnchorSource Source,
    IReadOnlyList<string> AvailableInputs,
    IReadOnlyList<string> RejectedAlternatives,
    string ConstraintReference);

internal sealed record ResolvedPrescriptionPaceSource(
    PrescriptionPaceSource Source,
    string RawConditionType,
    RuntimeConditionResolutionStatus RawStatus,
    string? RawOutputValue,
    string GoalFeasibilityValue,
    string Reason);

internal sealed record PrescriptionTraceRecord(
    string Subject,
    string RawValue,
    string NormalizedValue,
    string State,
    string Source,
    string Reason);

internal sealed record CatalogRuleOwnershipRecord(
    string WorkoutKey,
    string Field,
    string CatalogValueOrBound,
    PrescriptionRuleOwnership Ownership,
    string RequiredInput,
    string LaterPhase);

internal sealed record WorkoutPrescriptionRuleInventory(
    string WorkoutKey,
    int WorkoutVersion,
    string Family,
    IReadOnlyList<string> CatalogFixedFields,
    IReadOnlyList<string> CatalogBoundedFields,
    IReadOnlyList<string> CatalogAbsentFields,
    IReadOnlyList<string> RuntimeUserDerivedFields,
    IReadOnlyList<string> PhaseOrStageContextFields,
    IReadOnlyList<string> WeeklyVolumeContextFields,
    IReadOnlyList<string> PaceSourceContextFields,
    IReadOnlyList<string> FutureProductDecisionFields,
    WorkoutMeasurementBasis MeasurementBasis);

internal sealed record CatalogPrescriptionInputSnapshot
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required GoalType GoalType { get; init; }
    public required GoalDistance GoalDistance { get; init; }
    public required double GoalDistanceKm { get; init; }
    public required RunningBackground Level { get; init; }
    public required int DaysPerWeek { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? RaceDate { get; init; }
    public int? WeeksUntilRace { get; init; }
    public int? TargetFinishTimeSeconds { get; init; }
    public required DistanceUnit Unit { get; init; }
    public double? PreferredPace { get; init; }
    public required NormalizedRunningReadiness Readiness { get; init; }
    public required PlanCatalogReference LevelModifier { get; init; }
    public required PlanCatalogReference ProgressionModifier { get; init; }
    public required PlanCatalogReference PeakVolumeBandPolicy { get; init; }
    public required PlanCatalogReference RuntimeConditionValueRegistry { get; init; }
    public required string SourceCandidateProvenance { get; init; }
}

internal sealed record CatalogSessionPrescriptionContext
{
    public required int WeekNumber { get; init; }
    public required DateOnly Date { get; init; }
    public required string PhaseKey { get; init; }
    public string? ProgressionStageKey { get; init; }
    public required string StructuralRole { get; init; }
    public required string WorkoutDefinitionKey { get; init; }
    public required int WorkoutDefinitionVersion { get; init; }

    /// <summary>
    /// Phase 10K-FREQ.6D.4D.5D — verbatim from <see cref="RunningApp.Application.RuntimeCatalog.Schedule.Binding.BoundCatalogSession.PrescriptionProfileKey"/>/
    /// <see cref="RunningApp.Application.RuntimeCatalog.Schedule.Binding.BoundCatalogSession.PrescriptionProfileVersion"/>
    /// (never re-derived from ProgressionStageKey/WorkoutDefinitionKey/DoseCategory/LaneOrdinal —
    /// those authorities already terminated in Split A/B). Both null (Legacy), both non-null
    /// (ProfileBacked), or exactly one non-null (invalid partial lineage) — the identical
    /// three-way classification <see cref="RunningApp.Application.RuntimeCatalog.Prescription.Session.CatalogSessionPrescriptionPlanner"/>
    /// already applies downstream (Split C); not a second, incompatible rule.
    /// </summary>
    public string? PrescriptionProfileKey { get; init; }
    public int? PrescriptionProfileVersion { get; init; }
    public required WorkoutMeasurementBasis PrimaryMeasurementBasis { get; init; }
    public required IReadOnlyList<string> AllowedPrescriptionModes { get; init; }
    public required WeeklyVolumeAnchorDecision WeeklyVolumeAnchor { get; init; }
    public LongRunAnchorDecision? LongRunAnchor { get; init; }
    public required ResolvedPrescriptionPaceSource PaceSource { get; init; }
    public required string GoalFeasibilityValue { get; init; }
    public string? FallbackOrigin { get; init; }
    public required string BindingPolicyProvenance { get; init; }
    public required string SourceArtifactProvenance { get; init; }
}

internal sealed record CatalogPlanPrescriptionContext
{
    public required CatalogPrescriptionInputSnapshot InputSnapshot { get; init; }
    public required string BoundPlanIdentity { get; init; }
    public required NormalizedRunningReadiness PlanLevelReadiness { get; init; }
    public required WeeklyVolumeAnchorDecision WeeklyVolumeAnchor { get; init; }
    public required LongRunAnchorDecision LongRunAnchor { get; init; }
    public required ResolvedPrescriptionPaceSource PaceSource { get; init; }
    public required IReadOnlyList<IReadOnlyList<CatalogSessionPrescriptionContext>> WeekContexts { get; init; }
    public required IReadOnlyList<CatalogSessionPrescriptionContext> SessionContexts { get; init; }
    public required IReadOnlyList<WorkoutPrescriptionRuleInventory> CatalogRuleInventory { get; init; }
    public required IReadOnlyList<CatalogRuleOwnershipRecord> OwnershipMatrix { get; init; }
    public required IReadOnlyList<PrescriptionTraceRecord> DecisionTrace { get; init; }
    public required CatalogPrescriptionValidationResult ValidationResult { get; init; }
    public required TaperSharpenCapability TaperSharpenCapability { get; init; }
}

internal sealed record CatalogPrescriptionValidationResult(bool IsValid, IReadOnlyList<string> Errors);
