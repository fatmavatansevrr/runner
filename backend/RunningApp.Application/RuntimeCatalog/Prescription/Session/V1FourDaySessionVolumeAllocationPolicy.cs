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

        FourDaySessionDistanceAllocation distances;
        try
        {
            distances = FourDaySessionDistanceAllocationPolicy.Allocate(
                weekly.PlannedWeeklyVolumeKm, longRun.PlannedLongRunDistanceKm);
        }
        catch (CatalogSessionPrescriptionInfeasibleException exception)
        {
            var detail = exception.Message.StartsWith("V1 ", StringComparison.Ordinal)
                ? exception.Message[3..]
                : char.ToLowerInvariant(exception.Message[0]) + exception.Message[1..];
            throw new CatalogSessionPrescriptionInfeasibleException($"Week {weekly.WeekNumber} {detail}");
        }

        return new V1FourDayWeekAllocation(
            weekly.WeekNumber,
            weekly.PlannedWeeklyVolumeKm,
            longRun.PlannedLongRunDistanceKm,
            distances.ResidualVolumeKm,
            distances.KeySessionDistanceKm,
            distances.FirstEasySupportDistanceKm,
            distances.SecondEasySupportDistanceKm,
            new SessionVolumeAllocationTrace(
                weekly.WeekNumber,
                weekly.PlannedWeeklyVolumeKm,
                longRun.PlannedLongRunDistanceKm,
                distances.ResidualVolumeKm,
                distances.KeySessionDistanceKm,
                distances.FirstEasySupportDistanceKm,
                distances.SecondEasySupportDistanceKm,
                PolicyKey,
                PolicyVersion));
    }

    internal static double Round(double value) => Math.Round(value / RoundingIncrementKm, MidpointRounding.AwayFromZero) * RoundingIncrementKm;
}
