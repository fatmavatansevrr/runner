using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

internal enum CatalogNumericRuleAuthority
{
    CatalogFixed,
    CatalogBoundedRuntimeSelected,
    AcceptedProductDefault,
    RuntimeUserDerived,
    TechnicalDeterministicRule,
    MissingDecision,
    Inconsistent
}

internal enum CatalogEvidenceBasis
{
    EvidenceBacked,
    EvidenceInformed,
    ProductPracticeInformed,
    NotAnEvidenceQuestion
}

internal enum CatalogDecisionStatus
{
    CanonicalConfirmed,
    ExplicitProductDefault,
    TechnicalDeterministicRule,
    Deferred
}

internal enum CatalogVolumeClamp
{
    None,
    LowerBound,
    UpperBound,
    ConservativeClamp,
    TaperReduction
}

internal enum PeakBandClassification
{
    WithinTypicalPeakBand,
    BelowTypicalPeakButValid,
    UpperBoundConstrained
}

internal enum LongRunCompatibilityClass
{
    Balanced,
    Acceptable,
    HighShare,
    Inconsistent,
    Missing
}

internal sealed record CatalogVolumeBounds(double MinimumKm, double MaximumKm, string SourceArtifactKey, int SourceArtifactVersion);

internal sealed record StartingVolumeDecision(
    double? ReportedRecentWeeklyVolumeKm,
    PrescriptionInputState InputState,
    double SelectedStartingVolumeKm,
    WeeklyVolumeAnchorSource AnchorSource,
    CatalogVolumeClamp AppliedClamp,
    CatalogEvidenceBasis EvidenceBasis,
    CatalogDecisionStatus DecisionStatus,
    string Provenance);

internal sealed record ReachablePeakDecision(
    double StartingVolumeKm,
    double ReachablePeakKm,
    double SelectedPeakKm,
    PeakBandClassification Classification,
    double PreferredMaxWeeklyIncreasePercent,
    double HardMaxWeeklyIncreasePercent,
    double AbsoluteWeeklyIncrementCapKm,
    CatalogEvidenceBasis EvidenceBasis,
    CatalogDecisionStatus DecisionStatus,
    string Provenance);

internal sealed record TaperVolumeDecision(
    double Multiplier,
    double ReductionPercent,
    string AcceptedRange,
    CatalogEvidenceBasis EvidenceBasis,
    CatalogDecisionStatus DecisionStatus,
    string Provenance);

internal sealed record LongRunWeeklyShareDecision(
    double PreferredMinimumShare,
    double PreferredMaximumShare,
    double SelectionShare,
    double HardCapShare,
    CatalogEvidenceBasis EvidenceBasis,
    CatalogDecisionStatus DecisionStatus,
    string Provenance);

public sealed record CatalogPeakVolumeBand(
    string DistanceFamily,
    string Experience,
    int RunsPerWeek,
    double MinimumKm,
    double MaximumKm,
    string SourceArtifactKey,
    int SourceArtifactVersion);

internal sealed record CatalogWeeklyVolumePlan
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required double FirstWeekVolumeKm { get; init; }
    public required double PeakVolumeKm { get; init; }
    public required StartingVolumeDecision StartingVolumeDecision { get; init; }
    public required ReachablePeakDecision ReachablePeakDecision { get; init; }
    public required TaperVolumeDecision TaperVolumeDecision { get; init; }
    public required CatalogVolumeBounds CatalogBounds { get; init; }
    public required IReadOnlyList<CatalogWeeklyVolumeWeek> Weeks { get; init; }
    public required IReadOnlyList<WeeklyVolumeDecisionTrace> DecisionTrace { get; init; }
    public required CatalogVolumeValidationResult ValidationResult { get; init; }
}

internal sealed record CatalogWeeklyVolumeWeek
{
    public required int WeekNumber { get; init; }
    public required string PhaseKey { get; init; }
    public string? ProgressionStageKey { get; init; }
    public required double PlannedWeeklyVolumeKm { get; init; }
    public double? PreviousWeekVolumeKm { get; init; }
    public required double ChangeKm { get; init; }
    public double? ChangePercent { get; init; }
    public required string VolumeClassification { get; init; }
    public required bool IsRecoveryOrDeloadWeek { get; init; }
    public required bool IsTaperWeek { get; init; }
    public required WeeklyVolumeAnchorSource AnchorSource { get; init; }
    public required CatalogVolumeBounds CatalogBounds { get; init; }
    public required CatalogVolumeClamp AppliedClamp { get; init; }
    public required string DecisionReason { get; init; }
    public required string SourceArtifactKey { get; init; }
    public required int SourceArtifactVersion { get; init; }
    public required string Provenance { get; init; }
}

internal sealed record CatalogLongRunProgression
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required IReadOnlyList<CatalogLongRunWeek> Weeks { get; init; }
    public required IReadOnlyList<LongRunDecisionTrace> DecisionTrace { get; init; }
    public required CatalogVolumeValidationResult ValidationResult { get; init; }
    public required LongRunWeeklyShareDecision WeeklyShareDecision { get; init; }
}

internal sealed record CatalogLongRunWeek
{
    public required int WeekNumber { get; init; }
    public required string PhaseKey { get; init; }
    public required double PlannedLongRunDistanceKm { get; init; }
    public required double PlannedWeeklyVolumeKm { get; init; }
    public required double LongRunShareOfWeeklyVolume { get; init; }
    public required LongRunAnchorSource LongRunAnchorSource { get; init; }
    public required PrescriptionInputState RecentLongestRunState { get; init; }
    public required CatalogVolumeClamp CompatibilityClamp { get; init; }
    public required CatalogVolumeBounds CatalogBounds { get; init; }
    public required double ChangeFromPreviousWeekKm { get; init; }
    public double? ChangeFromPreviousWeekPercent { get; init; }
    public required string DecisionReason { get; init; }
    public required string SourceArtifactKey { get; init; }
    public required int SourceArtifactVersion { get; init; }
    public required string Provenance { get; init; }
}

internal sealed record WeeklyVolumeDecisionTrace(
    int WeekNumber,
    string PhaseKey,
    double? ReportedRecentWeeklyVolumeKm,
    PrescriptionInputState InputState,
    WeeklyVolumeAnchorSource AnchorSource,
    double UnclampedValueKm,
    double LowerBoundKm,
    double UpperBoundKm,
    CatalogVolumeClamp AppliedClamp,
    double PeakTargetKm,
    double? PriorWeekValueKm,
    string ChangeRule,
    string RecoveryOrTaperRule,
    string RoundingRule,
    double FinalValueKm,
    CatalogNumericRuleAuthority RuleAuthority);

internal sealed record LongRunDecisionTrace(
    int WeekNumber,
    string PhaseKey,
    double? ReportedRecentLongestRunKm,
    PrescriptionInputState InputState,
    LongRunAnchorSource AnchorSource,
    bool LowConfidence,
    string WeeklyVolumeCompatibilityRule,
    double LowerBoundKm,
    double UpperBoundKm,
    CatalogVolumeClamp AppliedClamp,
    double? PreviousLongRunKm,
    string ProgressionRule,
    string TaperRule,
    string RoundingRule,
    double FinalValueKm,
    CatalogNumericRuleAuthority RuleAuthority);

internal sealed record CatalogVolumeValidationResult(bool IsValid, IReadOnlyList<string> Errors);

internal sealed record CatalogVolumeAndLongRunPlan(CatalogWeeklyVolumePlan WeeklyVolumePlan, CatalogLongRunProgression LongRunProgression);

internal sealed record CatalogVolumePlanningRequest(
    PlanCatalogCandidateSummary Candidate,
    BoundCatalogPlan BoundPlan,
    CatalogPlanPrescriptionContext PrescriptionContext,
    CatalogPeakVolumeBand PeakVolumeBand);
