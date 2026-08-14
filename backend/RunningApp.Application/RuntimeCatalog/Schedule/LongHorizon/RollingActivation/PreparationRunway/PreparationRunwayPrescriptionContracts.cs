using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

/// <summary>
/// Phase 4K.8B Part 7 — one week's reference into the existing, unchanged
/// production <see cref="PreparationRunwayPrescribedWeek{TKey}"/> output.
/// <see cref="ProductionWeek"/> IS the original production object -- this
/// type never recalculates or duplicates its numeric values; the scalar
/// properties here are read directly from it for convenient access.
/// </summary>
internal sealed record PreparationRunwayPrescriptionWeekReference<TKey> where TKey : notnull
{
    public required PreparationRunwayPrescriptionId PrescriptionId { get; init; }
    public required PreparationRunwayPrescriptionVersion PrescriptionVersion { get; init; }
    public required int LocalRunwayWeek { get; init; }
    public required int GlobalPlanWeek { get; init; }
    public required string Stage { get; init; }
    public required double WeeklyVolumeKm { get; init; }
    public required double LongRunKm { get; init; }
    public required PreparationRunwayPrescribedWeek<TKey> ProductionWeek { get; init; }
    public required LongHorizonLockedCoreWeekOneTarget TargetLock { get; init; }
    public required int FullOutputIndex { get; init; }
}

/// <summary>
/// Phase 4K.8B Part 6 — the immutable full Runway prescription. The
/// unchanged <c>PreparationRunwayNumericMaterializer</c> output remains the
/// single numeric authority; this contract only wraps it with identity,
/// direction-policy provenance, and lock scope. <see cref="ComputedInternalPending"/>
/// is always true -- this is an internal computation result, never itself
/// an activated roadmap week (Phase 4K.8A §24, Phase 4K.8B Part 15).
/// </summary>
internal sealed record ImmutablePreparationRunwayPrescription<TKey> where TKey : notnull
{
    public required PreparationRunwayPrescriptionId PrescriptionId { get; init; }
    public required PreparationRunwayPrescriptionVersion PrescriptionVersion { get; init; }
    public required int FullRunwayDurationWeeks { get; init; }
    public required int StartGlobalWeek { get; init; }
    public required int EndGlobalWeek { get; init; }
    public required LongHorizonLockedCoreWeekOneTarget LockedCoreWeekOneTarget { get; init; }
    public required PreparationRunwayDirectionPolicy DirectionPolicy { get; init; }
    public required IReadOnlyList<PreparationRunwayPrescriptionWeekReference<TKey>> FullWeekReferences { get; init; }
    public required ReadinessProfile Profile { get; init; }
    public required string NumericProvenance { get; init; }
    public required string CatalogProvenance { get; init; }
    public string? PaceProvenance { get; init; }
    public string? CalendarProvenance { get; init; }
    public bool ComputedInternalPending { get; init; } = true;
    public bool Immutable { get; init; } = true;
}

/// <summary>
/// Phase 4K.8B Part 6/Part 18 (steps 2-7) — validates duration, target-lock
/// presence, local/global coordinates, and delegates terminal-stage
/// validation to <see cref="PreparationRunwayTerminalStageValidator"/>.
/// Unsupported direction (per <see cref="PreparationRunwayDirectionPolicy.OverallSupported"/>)
/// prevents prescription creation entirely (Phase 4K.8A §12).
/// </summary>
internal static class ImmutablePreparationRunwayPrescriptionValidator
{
    private const int MinimumDurationWeeks = 3;
    private const int MaximumDurationWeeks = 8;

    public static void Validate<TKey>(ImmutablePreparationRunwayPrescription<TKey> prescription) where TKey : notnull
    {
        // Step 2: direction-policy support.
        if (!prescription.DirectionPolicy.OverallSupported)
        {
            throw new PreparationRunwayDirectionUnsupportedException(
                "Unsupported Runway direction prevents full-prescription creation (Phase 4K.8A §12) -- " +
                $"weekly={prescription.DirectionPolicy.WeeklyDirection}, longRun={prescription.DirectionPolicy.LongRunDirection}.");
        }

        // Step 3: target-lock validation, plus one-lock-covers-full-range (Phase 4K.8B Part 9).
        LongHorizonCoreTargetLockValidator.Validate(prescription.LockedCoreWeekOneTarget);
        var lockRange = prescription.LockedCoreWeekOneTarget.LockedForActivatedRunwayWeekRange;
        if (lockRange.StartGlobalWeek != prescription.StartGlobalWeek || lockRange.EndGlobalWeek != prescription.EndGlobalWeek)
        {
            throw new PreparationRunwayTargetLockScopeViolationException(
                $"The locked Core Week-1 target's range ({lockRange.StartGlobalWeek}-{lockRange.EndGlobalWeek}) must exactly cover " +
                $"the full Runway global range ({prescription.StartGlobalWeek}-{prescription.EndGlobalWeek}) -- no per-slice lock is permitted.");
        }

        // Step 4: full duration / week-count validation.
        if (prescription.FullRunwayDurationWeeks is < MinimumDurationWeeks or > MaximumDurationWeeks)
        {
            throw new PreparationRunwayFullPrescriptionInvalidException(
                $"FullRunwayDurationWeeks must be {MinimumDurationWeeks}-{MaximumDurationWeeks}, was {prescription.FullRunwayDurationWeeks}.");
        }

        if (prescription.FullWeekReferences.Count != prescription.FullRunwayDurationWeeks)
        {
            throw new PreparationRunwayFullPrescriptionInvalidException(
                "FullWeekReferences count must equal FullRunwayDurationWeeks.");
        }

        // Step 5: local/global coordinate validation.
        var localWeeks = prescription.FullWeekReferences.Select(w => w.LocalRunwayWeek).OrderBy(w => w).ToList();
        if (!localWeeks.SequenceEqual(Enumerable.Range(1, prescription.FullRunwayDurationWeeks)))
        {
            throw new PreparationRunwayFullPrescriptionInvalidException(
                "Local Runway weeks must be exactly 1..FullRunwayDurationWeeks with no duplicates.");
        }

        if (prescription.EndGlobalWeek - prescription.StartGlobalWeek + 1 != prescription.FullRunwayDurationWeeks)
        {
            throw new PreparationRunwayFullPrescriptionInvalidException(
                "EndGlobalWeek - StartGlobalWeek + 1 must equal FullRunwayDurationWeeks.");
        }

        var globalWeeks = new HashSet<int>();
        foreach (var week in prescription.FullWeekReferences)
        {
            var expectedGlobal = prescription.StartGlobalWeek + week.LocalRunwayWeek - 1;
            if (week.GlobalPlanWeek != expectedGlobal)
            {
                throw new PreparationRunwayFullPrescriptionInvalidException(
                    $"Week local={week.LocalRunwayWeek}'s GlobalPlanWeek must equal StartGlobalWeek + local - 1 ({expectedGlobal}), was {week.GlobalPlanWeek}.");
            }

            if (!globalWeeks.Add(week.GlobalPlanWeek))
            {
                throw new PreparationRunwayFullPrescriptionInvalidException($"Duplicate GlobalPlanWeek {week.GlobalPlanWeek}.");
            }
        }

        // Step 6: PreSpecificTransition invariant.
        PreparationRunwayTerminalStageValidator.Validate(prescription.FullRunwayDurationWeeks, prescription.FullWeekReferences);

        // Step 7: full prescription immutability/reference validation -- every
        // reference must originate from the same prescription identity/version.
        if (prescription.FullWeekReferences.Any(w => w.PrescriptionId != prescription.PrescriptionId || w.PrescriptionVersion != prescription.PrescriptionVersion))
        {
            throw new PreparationRunwayFullPrescriptionInvalidException(
                "Every week reference must carry the same PrescriptionId/PrescriptionVersion as the prescription itself.");
        }

        if (!prescription.Immutable || !prescription.ComputedInternalPending)
        {
            throw new PreparationRunwayFullPrescriptionInvalidException(
                "A full prescription must always be Immutable=true and ComputedInternalPending=true (Phase 4K.8B Part 15).");
        }
    }
}

/// <summary>
/// Phase 4K.8B Part 8 — enforces PreSpecificTransition exists exactly once,
/// on the final local Runway week, and that no earlier week claims it
/// (Phase 4K.8A §20). Runs standalone so bounded-slice construction can
/// reuse it without re-deriving the rule.
/// </summary>
internal static class PreparationRunwayTerminalStageValidator
{
    private const string TerminalStageName = "PreSpecificTransition";

    public static void Validate<TKey>(int fullRunwayDurationWeeks, IReadOnlyList<PreparationRunwayPrescriptionWeekReference<TKey>> fullWeekReferences) where TKey : notnull
    {
        var transitionWeeks = fullWeekReferences.Where(w => string.Equals(w.Stage, TerminalStageName, StringComparison.Ordinal)).ToList();

        if (transitionWeeks.Count != 1)
        {
            throw new PreparationRunwayTerminalStageViolationException(
                $"Exactly one week must be {TerminalStageName}, found {transitionWeeks.Count}.");
        }

        if (transitionWeeks[0].LocalRunwayWeek != fullRunwayDurationWeeks)
        {
            throw new PreparationRunwayTerminalStageViolationException(
                $"{TerminalStageName} must occur on local week {fullRunwayDurationWeeks} (the final week), " +
                $"was local week {transitionWeeks[0].LocalRunwayWeek}.");
        }
    }
}
