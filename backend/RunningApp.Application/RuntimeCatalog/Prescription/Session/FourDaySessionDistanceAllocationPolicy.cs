namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

/// <summary>
/// Production-owned distance-only core shared by the live V1 four-day
/// prescription policy and dark Preparation Runway numeric materialization.
/// It deliberately knows nothing about workouts, pace, dates or persistence.
/// </summary>
internal sealed record FourDaySessionDistanceAllocation(
    double PlannedWeeklyVolumeKm,
    double LongRunDistanceKm,
    double ResidualVolumeKm,
    double KeySessionDistanceKm,
    double FirstEasySupportDistanceKm,
    double SecondEasySupportDistanceKm);

internal static class FourDaySessionDistanceAllocationPolicy
{
    public static FourDaySessionDistanceAllocation Allocate(double weeklyVolumeKm, double longRunDistanceKm)
    {
        var residual = V1FourDaySessionVolumeAllocationPolicy.Round(weeklyVolumeKm - longRunDistanceKm);
        var requiredMinimum = V1FourDaySessionVolumeAllocationPolicy.MinimumKeySessionDistanceKm +
                              (2 * V1FourDaySessionVolumeAllocationPolicy.MinimumEasySupportDistanceKm);
        if (residual + V1FourDaySessionVolumeAllocationPolicy.ToleranceKm < requiredMinimum)
        {
            throw new CatalogSessionPrescriptionInfeasibleException(
                $"Residual volume {residual:0.##}km cannot support V1 key/easy minimums.");
        }

        var key = V1FourDaySessionVolumeAllocationPolicy.Round(Math.Max(
            V1FourDaySessionVolumeAllocationPolicy.MinimumKeySessionDistanceKm, residual * 0.50d));
        var easyResidual = V1FourDaySessionVolumeAllocationPolicy.Round(residual - key);
        if (easyResidual < 2 * V1FourDaySessionVolumeAllocationPolicy.MinimumEasySupportDistanceKm)
        {
            easyResidual = 2 * V1FourDaySessionVolumeAllocationPolicy.MinimumEasySupportDistanceKm;
            key = V1FourDaySessionVolumeAllocationPolicy.Round(residual - easyResidual);
        }

        var firstEasy = V1FourDaySessionVolumeAllocationPolicy.Round(easyResidual / 2d);
        var secondEasy = V1FourDaySessionVolumeAllocationPolicy.Round(residual - key - firstEasy);
        if (secondEasy < V1FourDaySessionVolumeAllocationPolicy.MinimumEasySupportDistanceKm)
        {
            secondEasy = V1FourDaySessionVolumeAllocationPolicy.MinimumEasySupportDistanceKm;
            firstEasy = V1FourDaySessionVolumeAllocationPolicy.Round(residual - key - secondEasy);
        }

        var total = V1FourDaySessionVolumeAllocationPolicy.Round(key + firstEasy + secondEasy + longRunDistanceKm);
        var delta = V1FourDaySessionVolumeAllocationPolicy.Round(weeklyVolumeKm - total);
        if (Math.Abs(delta) > V1FourDaySessionVolumeAllocationPolicy.ToleranceKm)
        {
            secondEasy = V1FourDaySessionVolumeAllocationPolicy.Round(secondEasy + delta);
        }

        if (key <= 0 || firstEasy <= 0 || secondEasy <= 0)
        {
            throw new CatalogSessionPrescriptionInfeasibleException("V1 allocation produced a non-positive session distance.");
        }

        return new FourDaySessionDistanceAllocation(
            weeklyVolumeKm, longRunDistanceKm, residual, key, firstEasy, secondEasy);
    }
}
