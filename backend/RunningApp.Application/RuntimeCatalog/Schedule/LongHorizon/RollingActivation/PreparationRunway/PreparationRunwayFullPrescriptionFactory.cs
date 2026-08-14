using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

/// <summary>
/// Phase 4K.8B Part 14 — the narrow boundary through which a completed,
/// unchanged <see cref="PreparationRunwayNumericMaterializer"/> result
/// becomes an <see cref="ImmutablePreparationRunwayPrescription{TKey}"/>.
/// Does not itself orchestrate Core generation, validated-load mapping,
/// JIT evidence, pace resolution, calendar generation, or mixed-window
/// activation -- Phase 4K.8's JIT runtime will call this factory after real
/// full materialization; for this phase, tests supply the authoritative
/// existing materializer output directly.
/// </summary>
internal interface IPreparationRunwayFullPrescriptionFactory<TKey> where TKey : notnull
{
    ImmutablePreparationRunwayPrescription<TKey> Create(
        PreparationRunwayNumericMaterializationResult<TKey> materializationResult,
        double startingWeeklyVolumeKm,
        double startingLongRunKm,
        LongHorizonLockedCoreWeekOneTarget lockedCoreWeekOneTarget,
        int startGlobalWeek,
        ReadinessProfile profile,
        string catalogProvenance,
        string? paceProvenance = null,
        string? calendarProvenance = null);
}

/// <summary>
/// Phase 4K.8B Part 14/Part 18 — implements the validator chain in the
/// required order: (1) normalized direction comparison, (2) direction-policy
/// support, (3) target-lock validation, (4)-(7) delegated to
/// <see cref="ImmutablePreparationRunwayPrescriptionValidator"/> (full
/// duration/week-count, local/global coordinates, PreSpecificTransition,
/// immutability/reference validation).
/// </summary>
internal sealed class PreparationRunwayFullPrescriptionFactory<TKey> : IPreparationRunwayFullPrescriptionFactory<TKey> where TKey : notnull
{
    public ImmutablePreparationRunwayPrescription<TKey> Create(
        PreparationRunwayNumericMaterializationResult<TKey> materializationResult,
        double startingWeeklyVolumeKm,
        double startingLongRunKm,
        LongHorizonLockedCoreWeekOneTarget lockedCoreWeekOneTarget,
        int startGlobalWeek,
        ReadinessProfile profile,
        string catalogProvenance,
        string? paceProvenance = null,
        string? calendarProvenance = null)
    {
        if (!materializationResult.IsSuccess || materializationResult.PrescribedWeeks is null || materializationResult.PrescribedWeeks.Count == 0)
        {
            throw new PreparationRunwayFullPrescriptionInvalidException(
                "A full prescription can only be created from a successful, non-empty materialization result -- " +
                $"got IsSuccess={materializationResult.IsSuccess}, FailureCode={materializationResult.FailureCode}.");
        }

        // Steps 1-2: normalized direction comparison + direction-policy support.
        var directionPolicy = PreparationRunwayDirectionGuard.Evaluate(
            startingWeeklyVolumeKm, lockedCoreWeekOneTarget.TargetWeeklyVolumeKm,
            startingLongRunKm, lockedCoreWeekOneTarget.TargetLongRunKm);

        // Step 3: target-lock validation.
        LongHorizonCoreTargetLockValidator.Validate(lockedCoreWeekOneTarget);

        var weeks = materializationResult.PrescribedWeeks;
        var duration = weeks.Count;
        var endGlobalWeek = startGlobalWeek + duration - 1;

        var seed = string.Join('|',
            "PreparationRunwayPrescription",
            duration.ToString(),
            string.Join(",", weeks.Select(w => w.StructuralWeek.RunwayWeekNumber)),
            $"{startGlobalWeek}-{endGlobalWeek}",
            startingWeeklyVolumeKm.ToString("0.###"),
            startingLongRunKm.ToString("0.###"),
            lockedCoreWeekOneTarget.CreatedByDecisionId.ToString(),
            lockedCoreWeekOneTarget.ContextVersion.VersionId.ToString(),
            profile.ToString(),
            catalogProvenance,
            paceProvenance ?? string.Empty,
            calendarProvenance ?? string.Empty);

        var prescriptionId = new PreparationRunwayPrescriptionId(PreparationRunwayDeterministicIdentity.StableGuid(seed + "|id"));
        var prescriptionVersion = new PreparationRunwayPrescriptionVersion(
            LongHorizonContextVersion.Initial(PreparationRunwayDeterministicIdentity.StableGuid(seed + "|version")));

        var weekReferences = new List<PreparationRunwayPrescriptionWeekReference<TKey>>(duration);
        for (var index = 0; index < duration; index++)
        {
            var week = weeks[index];
            var localWeek = week.StructuralWeek.RunwayWeekNumber;
            weekReferences.Add(new PreparationRunwayPrescriptionWeekReference<TKey>
            {
                PrescriptionId = prescriptionId,
                PrescriptionVersion = prescriptionVersion,
                LocalRunwayWeek = localWeek,
                GlobalPlanWeek = startGlobalWeek + localWeek - 1,
                Stage = week.StructuralWeek.BlockType?.ToString() ?? "UNKNOWN",
                WeeklyVolumeKm = week.PlannedWeeklyVolumeKm,
                LongRunKm = week.PlannedLongRunDistanceKm,
                ProductionWeek = week,
                TargetLock = lockedCoreWeekOneTarget,
                FullOutputIndex = index,
            });
        }

        var prescription = new ImmutablePreparationRunwayPrescription<TKey>
        {
            PrescriptionId = prescriptionId,
            PrescriptionVersion = prescriptionVersion,
            FullRunwayDurationWeeks = duration,
            StartGlobalWeek = startGlobalWeek,
            EndGlobalWeek = endGlobalWeek,
            LockedCoreWeekOneTarget = lockedCoreWeekOneTarget,
            DirectionPolicy = directionPolicy,
            FullWeekReferences = weekReferences,
            Profile = profile,
            NumericProvenance = "PreparationRunwayNumericMaterializer (unchanged), Phase 4K.8A/4K.8B",
            CatalogProvenance = catalogProvenance,
            PaceProvenance = paceProvenance,
            CalendarProvenance = calendarProvenance,
        };

        // Steps 4-7 (full duration, local/global coordinates, PreSpecificTransition,
        // immutability/reference validation) plus the step-2/step-3 checks re-verified structurally.
        ImmutablePreparationRunwayPrescriptionValidator.Validate(prescription);

        return prescription;
    }
}
