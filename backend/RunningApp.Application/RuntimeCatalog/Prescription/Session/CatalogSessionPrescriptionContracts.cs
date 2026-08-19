using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal enum CatalogPrescriptionMode { Distance, Mixed, PaceBased }

internal enum CatalogDistanceAccountingMode { ExactSessionTotal, EstimatedSessionTotal, PrescribedTotal }

internal enum CatalogPacePrescriptionKind { ExactPace, PaceRange, RelativeToGoalPace, EffortOnly, Unresolved }

internal enum CatalogDurationKind { Prescribed, DerivedFromSegments, EstimatedFromPace, Unresolved }

internal enum CatalogSessionPrescriptionStatus { Complete, BaselinePrescribedSharpeningPending, FinalPrescriptionComplete }

internal enum CatalogPaceSourceSelection
{
    RecentRaceDerived,
    TargetGoalDerived,
    PreferredPaceContext,
    EstimatedCurrentFitness,
    LevelEffortDefault,
    EffortOnly,
    Unresolved
}

internal sealed record CatalogDistancePrescription(double PlannedDistanceKm, string AccountingMode, string RoundingRule);

internal sealed record CatalogDurationPrescription(CatalogDurationKind Kind, TimeSpan? Duration, string Provenance);

internal sealed record CatalogPacePrescription(
    CatalogPacePrescriptionKind Kind,
    double? SecondsPerKilometer,
    double? FasterBoundSecondsPerKilometer,
    double? SlowerBoundSecondsPerKilometer,
    CatalogPaceSourceSelection Source,
    string Confidence,
    string EffortLabel,
    string DerivationTrace);

internal sealed record CatalogPrescriptionSegment(
    int SequenceOrder,
    string ComponentType,
    string IntensityDescriptor,
    double DistanceKm,
    TimeSpan? Duration,
    CatalogPacePrescription PacePrescription,
    bool CountsTowardSessionDistance);

internal sealed record SessionPrescriptionDecisionTrace(
    int WeekNumber,
    DateOnly Date,
    string WorkoutDefinitionKey,
    double PlannedWeeklyVolumeKm,
    double ReservedLongRunKm,
    double ResidualVolumeKm,
    double AllocatedSessionDistanceKm,
    string AllocationPolicy,
    string SelectedPrescriptionMode,
    string SelectedComponentPolicy,
    string RawPaceSource,
    string GoalFeasibility,
    CatalogPaceSourceSelection SelectedPaceSource,
    IReadOnlyList<string> RejectedPaceSources,
    string RoundingRule,
    string DistanceAccountingMode,
    string DurationSemantics,
    string? FallbackOrigin,
    IReadOnlyList<string> UnresolvedFields);

internal sealed record SessionVolumeAllocationTrace(
    int WeekNumber,
    double PlannedWeeklyVolumeKm,
    double LongRunDistanceKm,
    double ResidualVolumeKm,
    double KeySessionDistanceKm,
    double FirstEasySupportDistanceKm,
    double SecondEasySupportDistanceKm,
    string PolicyKey,
    int PolicyVersion);

internal sealed record CatalogWorkoutPrescription
{
    public required CatalogPrescriptionMode PrescriptionMode { get; init; }
    public required CatalogDistanceAccountingMode DistanceAccountingMode { get; init; }
    public required CatalogDistancePrescription DistancePrescription { get; init; }
    public required CatalogDurationPrescription DurationPrescription { get; init; }
    public required CatalogPacePrescription PacePrescription { get; init; }
    public required string EffortGuidance { get; init; }
    public required IReadOnlyList<CatalogPrescriptionSegment> OrderedSegments { get; init; }
    public required CatalogSessionPrescriptionStatus Status { get; init; }
}

internal sealed record CatalogPrescribedSession
{
    public required int WeekNumber { get; init; }
    public required DateOnly Date { get; init; }
    public required string PhaseKey { get; init; }
    public string? ProgressionStageKey { get; init; }
    public required string StructuralRole { get; init; }
    public required string WorkoutDefinitionKey { get; init; }
    public required int WorkoutDefinitionVersion { get; init; }
    public required double PlannedDistanceKm { get; init; }
    public TimeSpan? PlannedDuration { get; init; }
    public TimeSpan? EstimatedDuration { get; init; }
    public required CatalogWorkoutPrescription Prescription { get; init; }
    public required string BindingProvenance { get; init; }
    public required string PaceSourceProvenance { get; init; }
    public required string VolumeAllocationProvenance { get; init; }
    public string? FallbackProvenance { get; init; }
    public required SessionPrescriptionDecisionTrace DecisionTrace { get; init; }
    public required CatalogSessionPrescriptionValidationResult ValidationResult { get; init; }

    /// <summary>
    /// Backend Integration Phase 10K-FREQ.6D.4D Split C — the discriminated Legacy/ProfileBacked
    /// classification for this session (see <see cref="CatalogSessionPrescriptionSource"/>),
    /// additive alongside the legacy fields above (which remain always-computed and unchanged for
    /// every existing consumer). For a Legacy session this wraps the same
    /// <see cref="CatalogWorkoutPrescription"/> already carried by <see cref="Prescription"/>. For
    /// a ProfileBacked session this carries the exact, unmodified
    /// <c>ExecutableWorkoutPrescription</c> resolved from the bundle — never rebuilt/recomputed.
    /// Split D (persistence) decides how/whether to prefer this over the legacy fields once
    /// TrainingDay gains durable lineage columns; this split only makes the resolved value
    /// observable.
    /// </summary>
    public required CatalogSessionPrescriptionSource PrescriptionSource { get; init; }
}

internal sealed record CatalogPrescribedWeek
{
    public required int WeekNumber { get; init; }
    public required string PhaseKey { get; init; }
    public required double PlannedWeeklyVolumeKm { get; init; }
    public required double AccountedWeeklyDistanceKm { get; init; }
    public required SessionVolumeAllocationTrace AllocationTrace { get; init; }
    public required IReadOnlyList<CatalogPrescribedSession> Sessions { get; init; }
}

internal sealed record CatalogPrescribedPlan
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required IReadOnlyList<CatalogPrescribedWeek> Weeks { get; init; }
    public required IReadOnlyList<CatalogPrescribedSession> Sessions { get; init; }
    public required CatalogSessionPrescriptionValidationResult ValidationResult { get; init; }
}

internal sealed record CatalogSessionPrescriptionValidationResult(bool IsValid, IReadOnlyList<string> Errors);

internal sealed record CatalogSessionPrescriptionRequest(
    PlanCatalogCandidateSummary Candidate,
    BoundCatalogPlan BoundPlan,
    CatalogPlanPrescriptionContext PrescriptionContext,
    Volume.CatalogVolumeAndLongRunPlan VolumePlan,
    IReadOnlyDictionary<string, CatalogWorkoutDefinitionSummary> WorkoutDefinitions,
    // Backend Integration Phase 10K-FREQ.6D.4D Split C: optional, additive. Every real current
    // caller omits this (defaults null), so every existing candidate resolves purely Legacy,
    // byte-identical to before this split — no production caller yet sources a real published
    // bundle (no RUN_LAYOUT_5D/combination exists to publish one for, see the Split-B/Split-C
    // reports' own disclosed gap). When present, ProfileBacked bound sessions resolve their exact
    // executable prescription against it; when absent, an explicitly ProfileBacked session fails
    // closed rather than silently degrading to Legacy (Section 4 of the Split-C prompt).
    ExecutionPrescriptionIndex? ExecutionIndex = null);

internal enum TaperSharpenCapabilityClassification
{
    SupportedByExistingEasyStandardSchema,
    SupportedByAdditiveRuntimePrescriptionContract,
    CatalogSchemaExtensionRequired,
    CatalogWorkoutRevisionRequired,
    StateInconsistent
}

internal sealed record TaperSharpenCapabilityResult(
    TaperSharpenCapabilityClassification Classification,
    string Basis,
    string RuntimeEffect);

internal sealed record TaperSharpenComponentSelection(
    string EasyBaselineComponentType,
    string SharpeningComponentType,
    string RecoveryComponentType,
    double EasyBaselineDistanceKm,
    double SharpeningDistanceKm,
    double RecoveryDistanceKm,
    string Placement,
    string DoseRule);

internal sealed record TaperSharpenPrescriptionDecision(
    string PolicyKey,
    int PolicyVersion,
    TaperSharpenCapabilityResult Capability,
    TaperSharpenComponentSelection ComponentSelection,
    string PaceAndEffortRule,
    string DistanceAccountingRule,
    string DurationRule,
    string Provenance);

internal sealed record CatalogFinalPrescribedPlanValidationResult(bool IsValid, IReadOnlyList<string> Errors);

internal sealed record CatalogFinalPrescriptionRequest(
    PlanCatalogCandidateSummary Candidate,
    BoundCatalogPlan BoundPlan,
    Volume.CatalogVolumeAndLongRunPlan VolumePlan,
    CatalogPrescribedPlan BaselinePlan);
