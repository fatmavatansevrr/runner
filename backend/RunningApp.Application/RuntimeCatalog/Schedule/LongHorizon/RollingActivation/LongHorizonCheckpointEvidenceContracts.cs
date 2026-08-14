namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>Phase 4K.3 §13/§10 — whether an unresolved safety-critical signal exists. Fails closed: absence of information is never treated as Clear.</summary>
internal enum LongHorizonSafetyState
{
    Clear,
    UnresolvedSafetyCritical,
}

/// <summary>Phase 4K.5 Part 6 — whether a <see cref="ValidatedSustainableLoad"/> could be computed.</summary>
internal enum LongHorizonValidationStatus
{
    Valid,
    Unavailable,
}

/// <summary>
/// Phase 4K.5 Part 5 — a typed policy/runtime-boundary representation of
/// Phase 4K.3's approved conceptual checkpoint evidence snapshot. This
/// contract does not aggregate from persisted <c>TrainingDay</c> rows
/// itself (explicitly out of scope for this phase) -- it is the shape a
/// future aggregator must produce.
/// </summary>
internal sealed record LongHorizonCheckpointEvidenceSnapshot
{
    public required Guid CheckpointId { get; init; }
    public required DateOnly CheckpointDate { get; init; }
    public required int ActivatedWindowStartWeek { get; init; }
    public required int ActivatedWindowEndWeek { get; init; }
    public required bool WindowCalendarPeriodEnded { get; init; }
    public required bool AllSessionsTerminal { get; init; }
    public required IReadOnlyList<double> ActualWeeklyVolumesKm { get; init; }
    public IReadOnlyDictionary<int, double> ActualWeeklyVolumeByGlobalWeek { get; init; } = new Dictionary<int, double>();
    public IReadOnlyList<int> UsableNonRecoveryEvidenceWeeks { get; init; } = [];
    public IReadOnlyList<int> ExcludedRecoveryWeeks { get; init; } = [];
    public bool GrowthConfidenceSatisfied { get; init; }
    public required IReadOnlyList<double> CompletedLongRunsKm { get; init; }
    public required int CompletedRunsCount { get; init; }
    public required int PlannedRunsCount { get; init; }
    public required double AdherenceRatePercent { get; init; }
    public required int MissedSessionCount { get; init; }
    public double? PriorValidatedWeeklyLoadKm { get; init; }
    public double? PriorValidatedLongRunKm { get; init; }
    public required IReadOnlyList<DayOfWeek> Availability { get; init; }
    public required LongHorizonSafetyState SafetyState { get; init; }
    public required LongHorizonStructuralSegmentType CurrentSegment { get; init; }
    public int? WeeksUntilRunway { get; init; }
    public required LongHorizonEvidenceAuthorityRecord EvidenceSourceMetadata { get; init; }
    public LongHorizonContextVersion? ContextVersion { get; init; }
}

/// <summary>
/// Phase 4K.5 Part 5 — validates the checkpoint snapshot invariants that
/// don't require database access: no future value leakage, and adherence
/// scoped to counts that are internally consistent.
/// </summary>
internal static class LongHorizonCheckpointEvidenceSnapshotValidator
{
    public static void Validate(LongHorizonCheckpointEvidenceSnapshot snapshot)
    {
        if (snapshot.ActivatedWindowEndWeek < snapshot.ActivatedWindowStartWeek)
        {
            throw new LongHorizonCheckpointDecisionInvalidException(
                "ActivatedWindowEndWeek must be >= ActivatedWindowStartWeek.");
        }

        if (snapshot.CompletedRunsCount > snapshot.PlannedRunsCount)
        {
            throw new LongHorizonCheckpointDecisionInvalidException(
                "CompletedRunsCount cannot exceed PlannedRunsCount.");
        }

        if (snapshot.AdherenceRatePercent is < 0 or > 100)
        {
            throw new LongHorizonCheckpointDecisionInvalidException("AdherenceRatePercent must be within [0, 100].");
        }

        // WindowCalendarPeriodEnded and AllSessionsTerminal are independently
        // valid in any combination here -- the terminal-window rule (Phase
        // 4K.3 §6, requiring BOTH before a Growth decision) is enforced by
        // LongHorizonCheckpointDecisionValidator, not this snapshot-shape check.
    }
}
