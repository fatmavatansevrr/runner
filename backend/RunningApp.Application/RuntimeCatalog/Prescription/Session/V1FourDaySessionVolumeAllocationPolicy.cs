using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal sealed record V1FourDayWeekAllocation(
    int WeekNumber,
    double PlannedWeeklyVolumeKm,
    double LongRunDistanceKm,
    double ResidualVolumeKm,
    double KeySessionDistanceKm,
    double FirstEasySupportDistanceKm,
    double SecondEasySupportDistanceKm,
    SessionVolumeAllocationTrace Trace);

internal static class V1FourDaySessionVolumeAllocationPolicy
{
    public const string PolicyKey = "V1_FOUR_DAY_SESSION_VOLUME_ALLOCATION_POLICY";
    public const int PolicyVersion = 1;
    public const double MinimumEasySupportDistanceKm = 1.5d;
    public const double MinimumKeySessionDistanceKm = 3d;
    public const double RoundingIncrementKm = 0.5d;
    public const double ToleranceKm = 0.001d;

    public static V1FourDayWeekAllocation Allocate(
        CatalogWeeklyVolumeWeek weekly,
        CatalogLongRunWeek longRun,
        IReadOnlyList<BoundCatalogSession> sessions)
    {
        if (sessions.Count != 4 ||
            sessions.Count(s => s.StructuralRole == "KEY_SESSION") != 1 ||
            sessions.Count(s => s.StructuralRole == "EASY_SUPPORT") != 2 ||
            sessions.Count(s => s.StructuralRole == "LONG_RUN") != 1)
        {
            throw new CatalogSessionPrescriptionInfeasibleException($"Week {weekly.WeekNumber} does not match the V1 four-day session shape.");
        }

        var residual = Round(weekly.PlannedWeeklyVolumeKm - longRun.PlannedLongRunDistanceKm);
        var requiredMinimum = MinimumKeySessionDistanceKm + (2 * MinimumEasySupportDistanceKm);
        if (residual + ToleranceKm < requiredMinimum)
        {
            throw new CatalogSessionPrescriptionInfeasibleException(
                $"Week {weekly.WeekNumber} residual volume {residual:0.##}km cannot support V1 key/easy minimums.");
        }

        var key = Round(Math.Max(MinimumKeySessionDistanceKm, residual * 0.50d));
        var easyResidual = Round(residual - key);
        if (easyResidual < 2 * MinimumEasySupportDistanceKm)
        {
            easyResidual = 2 * MinimumEasySupportDistanceKm;
            key = Round(residual - easyResidual);
        }

        var firstEasy = Round(easyResidual / 2d);
        var secondEasy = Round(residual - key - firstEasy);
        if (secondEasy < MinimumEasySupportDistanceKm)
        {
            secondEasy = MinimumEasySupportDistanceKm;
            firstEasy = Round(residual - key - secondEasy);
        }

        var total = Round(key + firstEasy + secondEasy + longRun.PlannedLongRunDistanceKm);
        var delta = Round(weekly.PlannedWeeklyVolumeKm - total);
        if (Math.Abs(delta) > ToleranceKm)
        {
            secondEasy = Round(secondEasy + delta);
        }

        if (key <= 0 || firstEasy <= 0 || secondEasy <= 0)
        {
            throw new CatalogSessionPrescriptionInfeasibleException($"Week {weekly.WeekNumber} allocation produced a non-positive session distance.");
        }

        return new V1FourDayWeekAllocation(
            weekly.WeekNumber,
            weekly.PlannedWeeklyVolumeKm,
            longRun.PlannedLongRunDistanceKm,
            residual,
            key,
            firstEasy,
            secondEasy,
            new SessionVolumeAllocationTrace(
                weekly.WeekNumber,
                weekly.PlannedWeeklyVolumeKm,
                longRun.PlannedLongRunDistanceKm,
                residual,
                key,
                firstEasy,
                secondEasy,
                PolicyKey,
                PolicyVersion));
    }

    internal static double Round(double value) => Math.Round(value / RoundingIncrementKm, MidpointRounding.AwayFromZero) * RoundingIncrementKm;
}
