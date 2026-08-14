namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

/// <summary>
/// Phase 4K.8B Part 11 — an exact, bounded 1-4-week reference window into an
/// <see cref="ImmutablePreparationRunwayPrescription{TKey}"/>. Never carries
/// its own numeric values -- <see cref="WeekReferences"/> are the identical
/// (record-equal) instances from the full prescription's
/// <c>FullWeekReferences</c>. Constructing a slice never transitions any
/// lifecycle state (<see cref="NonExecutableUntilActivation"/> is always
/// true here).
/// </summary>
internal sealed record BoundedPreparationRunwayPrescriptionSlice<TKey> where TKey : notnull
{
    public required PreparationRunwayPrescriptionId PrescriptionId { get; init; }
    public required PreparationRunwayPrescriptionVersion PrescriptionVersion { get; init; }
    public required Guid SliceId { get; init; }
    public required int RequestedStartLocalWeek { get; init; }
    public required int RequestedEndLocalWeek { get; init; }
    public required int RequestedStartGlobalWeek { get; init; }
    public required int RequestedEndGlobalWeek { get; init; }
    public required int ActualWeekCount { get; init; }
    public required IReadOnlyList<PreparationRunwayPrescriptionWeekReference<TKey>> WeekReferences { get; init; }
    public required Guid TargetLockId { get; init; }
    public required LongHorizonContextVersion TargetLockVersion { get; init; }
    public required int SourceFullDuration { get; init; }
    public required string BoundedExposureProvenance { get; init; }
    public bool NonExecutableUntilActivation { get; init; } = true;
}

/// <summary>
/// Phase 4K.8B Part 12 — the only way a bounded slice may be created. Never
/// invokes the numeric materializer, never recalculates any value, never
/// mutates the source prescription -- it filters and re-exposes the same
/// week-reference instances.
/// </summary>
internal static class PreparationRunwayBoundedSliceFactory
{
    private const int MaximumSliceSizeWeeks = 4;

    public static BoundedPreparationRunwayPrescriptionSlice<TKey> CreateSlice<TKey>(
        ImmutablePreparationRunwayPrescription<TKey> prescription,
        int startLocalWeek,
        int endLocalWeek) where TKey : notnull
    {
        ImmutablePreparationRunwayPrescriptionValidator.Validate(prescription);

        if (startLocalWeek < 1 || endLocalWeek > prescription.FullRunwayDurationWeeks || endLocalWeek < startLocalWeek)
        {
            throw new PreparationRunwayBoundedSliceInvalidException(
                $"Requested local range ({startLocalWeek}-{endLocalWeek}) must be inside 1..{prescription.FullRunwayDurationWeeks}.");
        }

        var size = endLocalWeek - startLocalWeek + 1;
        if (size > MaximumSliceSizeWeeks)
        {
            throw new PreparationRunwayBoundedSliceInvalidException(
                $"Slice size must be 1-{MaximumSliceSizeWeeks} weeks, requested {size}.");
        }

        var references = prescription.FullWeekReferences
            .Where(w => w.LocalRunwayWeek >= startLocalWeek && w.LocalRunwayWeek <= endLocalWeek)
            .OrderBy(w => w.LocalRunwayWeek)
            .ToList();

        if (references.Count != size)
        {
            throw new PreparationRunwayBoundedSliceInvalidException(
                $"Expected {size} contiguous week references, found {references.Count}.");
        }

        for (var i = 1; i < references.Count; i++)
        {
            if (references[i].LocalRunwayWeek != references[i - 1].LocalRunwayWeek + 1)
            {
                throw new PreparationRunwayBoundedSliceInvalidException("Slice week references must be contiguous.");
            }
        }

        var seed = $"{prescription.PrescriptionId.Value}|{prescription.PrescriptionVersion.Version.VersionId}|slice|{startLocalWeek}-{endLocalWeek}";
        var slice = new BoundedPreparationRunwayPrescriptionSlice<TKey>
        {
            PrescriptionId = prescription.PrescriptionId,
            PrescriptionVersion = prescription.PrescriptionVersion,
            SliceId = PreparationRunwayDeterministicIdentity.StableGuid(seed),
            RequestedStartLocalWeek = startLocalWeek,
            RequestedEndLocalWeek = endLocalWeek,
            RequestedStartGlobalWeek = references[0].GlobalPlanWeek,
            RequestedEndGlobalWeek = references[^1].GlobalPlanWeek,
            ActualWeekCount = references.Count,
            WeekReferences = references,
            TargetLockId = prescription.LockedCoreWeekOneTarget.CreatedByDecisionId,
            TargetLockVersion = prescription.LockedCoreWeekOneTarget.ContextVersion,
            SourceFullDuration = prescription.FullRunwayDurationWeeks,
            BoundedExposureProvenance =
                $"Bounded slice of prescription {prescription.PrescriptionId.Value} v{prescription.PrescriptionVersion.Version.Sequence}, local weeks {startLocalWeek}-{endLocalWeek}.",
        };

        PreparationRunwayBoundedSliceEquivalenceValidator.Validate(prescription, slice);
        return slice;
    }
}

/// <summary>
/// Phase 4K.8B Part 13 — proves every slice week reference is exactly the
/// corresponding full-prescription week reference, with no recomputed or
/// replaced field. Runs inside <see cref="PreparationRunwayBoundedSliceFactory"/>
/// itself, not only in tests.
/// </summary>
internal static class PreparationRunwayBoundedSliceEquivalenceValidator
{
    public static void Validate<TKey>(
        ImmutablePreparationRunwayPrescription<TKey> prescription,
        BoundedPreparationRunwayPrescriptionSlice<TKey> slice) where TKey : notnull
    {
        if (slice.PrescriptionId != prescription.PrescriptionId || slice.PrescriptionVersion != prescription.PrescriptionVersion)
        {
            throw new PreparationRunwaySliceEquivalenceViolationException(
                "A slice must reference the exact prescription identity/version it was created from.");
        }

        foreach (var sliceWeek in slice.WeekReferences)
        {
            var fullWeek = prescription.FullWeekReferences.SingleOrDefault(w => w.LocalRunwayWeek == sliceWeek.LocalRunwayWeek);
            if (fullWeek is null)
            {
                throw new PreparationRunwaySliceEquivalenceViolationException(
                    $"Slice local week {sliceWeek.LocalRunwayWeek} has no corresponding full-prescription week reference.");
            }

            if (!Equals(fullWeek, sliceWeek))
            {
                throw new PreparationRunwaySliceEquivalenceViolationException(
                    $"Slice local week {sliceWeek.LocalRunwayWeek} is not value-equal to the full-prescription week reference -- " +
                    "no field may be recomputed or replaced.");
            }
        }
    }
}
