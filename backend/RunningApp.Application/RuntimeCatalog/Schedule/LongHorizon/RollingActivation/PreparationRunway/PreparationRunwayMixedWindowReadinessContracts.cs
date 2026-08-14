using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

/// <summary>
/// Phase 4K.8B Part 16 — the minimal contract Phase 4K.8's future mixed-
/// window activation will need: one bounded Runway slice plus externally
/// supplied GE/Core references for the same activation window. This phase
/// does not activate it -- no window is materialized or exposed here.
/// </summary>
internal sealed record PreparationRunwayBoundedExposureSelection<TKey> where TKey : notnull
{
    public required BoundedPreparationRunwayPrescriptionSlice<TKey> SelectedSlice { get; init; }
    public IReadOnlyList<ActivatedNumericWeek>? PrecedingGeReferences { get; init; }
    public IReadOnlyList<ActivatedNumericWeek>? FollowingCoreReferences { get; init; }
    public required int WholeWindowStartGlobalWeek { get; init; }
    public required int WholeWindowEndGlobalWeek { get; init; }
    public required IReadOnlyList<LongHorizonStructuralSegmentType> SegmentsCovered { get; init; }
    public required Guid TargetLockId { get; init; }
    public required LongHorizonContextVersion ActivationContextVersion { get; init; }
    public bool AtomicityRequired { get; init; } = true;
}

/// <summary>Phase 4K.8B Part 16 — Runway references come only from one bounded slice; atomicity is always required for a mixed window.</summary>
internal static class PreparationRunwayBoundedExposureSelectionValidator
{
    public static void Validate<TKey>(PreparationRunwayBoundedExposureSelection<TKey> selection) where TKey : notnull
    {
        if (!selection.AtomicityRequired)
        {
            throw new PreparationRunwayBoundedSliceInvalidException(
                "AtomicityRequired must always be true for a mixed-segment window (Phase 4K.1/4K.4).");
        }

        if (selection.WholeWindowEndGlobalWeek < selection.WholeWindowStartGlobalWeek)
        {
            throw new PreparationRunwayBoundedSliceInvalidException(
                "WholeWindowEndGlobalWeek must be >= WholeWindowStartGlobalWeek.");
        }

        if (!selection.SegmentsCovered.Contains(LongHorizonStructuralSegmentType.PreparationRunway))
        {
            throw new PreparationRunwayBoundedSliceInvalidException(
                "SegmentsCovered must include PreparationRunway when a bounded Runway slice is selected.");
        }

        if (selection.SelectedSlice.TargetLockId != selection.TargetLockId)
        {
            throw new PreparationRunwayBoundedSliceInvalidException(
                "The selection's TargetLockId must match the selected slice's own TargetLockId.");
        }
    }
}
