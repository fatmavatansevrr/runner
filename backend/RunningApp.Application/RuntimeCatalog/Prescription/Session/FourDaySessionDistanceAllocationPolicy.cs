namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

/// <summary>
/// Production-owned distance-only core shared by the live V1 four-day
/// prescription policy and dark Preparation Runway numeric materialization.
/// It deliberately knows nothing about workouts, pace, dates or persistence.
///
/// Phase 10K-FREQ.4: generalized from a single KEY_SESSION instance to
/// <paramref name="keySessionCount"/> instances of the "V1 multi-key" session
/// shape (still exactly 2 EASY_SUPPORT + 1 LONG_RUN, unchanged) -- e.g. a
/// hypothetical Intermediate 5D layout (2 KEY + 2 EASY + 1 LONG). The KEY
/// share of weekly volume is split evenly across all KEY instances (equal
/// split is the minimal, unopinionated generalization; asymmetric KEY1/KEY2
/// allocation is a future product decision, not decided here). For
/// <c>keySessionCount == 1</c> (every existing 4D caller, via the default
/// parameter) this is a byte-identical no-op versus the pre-FREQ.4 algorithm
/// -- verified by regression, not merely asserted.
/// </summary>
internal sealed record FourDaySessionDistanceAllocation(
    double PlannedWeeklyVolumeKm,
    double LongRunDistanceKm,
    double ResidualVolumeKm,
    IReadOnlyList<double> KeySessionDistancesKm,
    double FirstEasySupportDistanceKm,
    double SecondEasySupportDistanceKm)
{
    /// <summary>Back-compat accessor for every pre-FREQ.4 (single-KEY) consumer -- unchanged value for <c>keySessionCount == 1</c>.</summary>
    public double KeySessionDistanceKm => KeySessionDistancesKm[0];

    /// <summary>Explicit structural equality for <see cref="KeySessionDistancesKm"/> -- see the identical note on <see cref="V1FourDayWeekAllocation"/>.</summary>
    public bool Equals(FourDaySessionDistanceAllocation? other) =>
        other is not null &&
        PlannedWeeklyVolumeKm.Equals(other.PlannedWeeklyVolumeKm) &&
        LongRunDistanceKm.Equals(other.LongRunDistanceKm) &&
        ResidualVolumeKm.Equals(other.ResidualVolumeKm) &&
        KeySessionDistancesKm.SequenceEqual(other.KeySessionDistancesKm) &&
        FirstEasySupportDistanceKm.Equals(other.FirstEasySupportDistanceKm) &&
        SecondEasySupportDistanceKm.Equals(other.SecondEasySupportDistanceKm);

    public override int GetHashCode() =>
        HashCode.Combine(
            PlannedWeeklyVolumeKm, LongRunDistanceKm, ResidualVolumeKm,
            KeySessionDistancesKm.Aggregate(17, HashCode.Combine), FirstEasySupportDistanceKm, SecondEasySupportDistanceKm);
}

internal static class FourDaySessionDistanceAllocationPolicy
{
    public static FourDaySessionDistanceAllocation Allocate(double weeklyVolumeKm, double longRunDistanceKm, int keySessionCount = 1)
    {
        if (keySessionCount < 1)
        {
            throw new CatalogSessionPrescriptionInfeasibleException("V1 multi-key allocation requires at least one KEY_SESSION instance.");
        }

        var residual = V1FourDaySessionVolumeAllocationPolicy.Round(weeklyVolumeKm - longRunDistanceKm);
        var requiredMinimum = (keySessionCount * V1FourDaySessionVolumeAllocationPolicy.MinimumKeySessionDistanceKm) +
                              (2 * V1FourDaySessionVolumeAllocationPolicy.MinimumEasySupportDistanceKm);
        if (residual + V1FourDaySessionVolumeAllocationPolicy.ToleranceKm < requiredMinimum)
        {
            throw new CatalogSessionPrescriptionInfeasibleException(
                $"Residual volume {residual:0.##}km cannot support V1 key/easy minimums.");
        }

        var keyTotal = V1FourDaySessionVolumeAllocationPolicy.Round(Math.Max(
            keySessionCount * V1FourDaySessionVolumeAllocationPolicy.MinimumKeySessionDistanceKm, residual * 0.50d));
        var easyResidual = V1FourDaySessionVolumeAllocationPolicy.Round(residual - keyTotal);
        if (easyResidual < 2 * V1FourDaySessionVolumeAllocationPolicy.MinimumEasySupportDistanceKm)
        {
            easyResidual = 2 * V1FourDaySessionVolumeAllocationPolicy.MinimumEasySupportDistanceKm;
            keyTotal = V1FourDaySessionVolumeAllocationPolicy.Round(residual - easyResidual);
        }

        var firstEasy = V1FourDaySessionVolumeAllocationPolicy.Round(easyResidual / 2d);
        var secondEasy = V1FourDaySessionVolumeAllocationPolicy.Round(residual - keyTotal - firstEasy);
        if (secondEasy < V1FourDaySessionVolumeAllocationPolicy.MinimumEasySupportDistanceKm)
        {
            secondEasy = V1FourDaySessionVolumeAllocationPolicy.MinimumEasySupportDistanceKm;
            firstEasy = V1FourDaySessionVolumeAllocationPolicy.Round(residual - keyTotal - secondEasy);
        }

        var total = V1FourDaySessionVolumeAllocationPolicy.Round(keyTotal + firstEasy + secondEasy + longRunDistanceKm);
        var delta = V1FourDaySessionVolumeAllocationPolicy.Round(weeklyVolumeKm - total);
        if (Math.Abs(delta) > V1FourDaySessionVolumeAllocationPolicy.ToleranceKm)
        {
            secondEasy = V1FourDaySessionVolumeAllocationPolicy.Round(secondEasy + delta);
        }

        var keyDistances = SplitEvenly(keyTotal, keySessionCount);
        if (keyDistances.Any(k => k <= 0) || firstEasy <= 0 || secondEasy <= 0)
        {
            throw new CatalogSessionPrescriptionInfeasibleException("V1 allocation produced a non-positive session distance.");
        }

        return new FourDaySessionDistanceAllocation(
            weeklyVolumeKm, longRunDistanceKm, residual, keyDistances, firstEasy, secondEasy);
    }

    /// <summary>
    /// Splits <paramref name="total"/> (already 0.5km-rounded) into
    /// <paramref name="count"/> shares, each a 0.5km multiple, summing
    /// exactly to <paramref name="total"/>. The last share absorbs any
    /// rounding remainder deterministically. For <c>count == 1</c> this is
    /// the identity function -- <c>[total]</c>, unchanged.
    /// </summary>
    private static IReadOnlyList<double> SplitEvenly(double total, int count)
    {
        if (count == 1) return [total];
        var baseShare = V1FourDaySessionVolumeAllocationPolicy.Round(total / count);
        var shares = new double[count];
        for (var i = 0; i < count - 1; i++) shares[i] = baseShare;
        shares[count - 1] = V1FourDaySessionVolumeAllocationPolicy.Round(total - (baseShare * (count - 1)));
        return shares;
    }
}
