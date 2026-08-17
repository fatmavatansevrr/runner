using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal static class V1ThreeDaySessionVolumeAllocationPolicy
{
    public const string PolicyKey = "V1_TEN_K_INTERMEDIATE_3D_SESSION_ALLOCATION_POLICY";
    public const int PolicyVersion = 1;
    public const double MinimumKeyKm = 4d;
    public const double MinimumEasyKm = 3d;
    public const double MinimumLongKm = 5d;
    private const double KeyShare = 0.35d;
    private const double EasyShare = 0.25d;
    private const double LongHardCap = 0.42d;

    public static V1FourDayWeekAllocation Allocate(CatalogWeeklyVolumeWeek weekly, CatalogLongRunWeek longRun, IReadOnlyList<BoundCatalogSession> sessions)
    {
        if (sessions.Count != 3 || sessions.Count(s => s.StructuralRole == "KEY_SESSION") != 1 ||
            sessions.Count(s => s.StructuralRole == "EASY_SUPPORT") != 1 || sessions.Count(s => s.StructuralRole == "LONG_RUN") != 1)
            throw new CatalogSessionPrescriptionInfeasibleException($"Week {weekly.WeekNumber} does not match RUN_LAYOUT_3D.");

        var volume = weekly.PlannedWeeklyVolumeKm;
        if (volume < MinimumKeyKm + MinimumEasyKm + MinimumLongKm)
            throw new CatalogSessionPrescriptionInfeasibleException($"Week {weekly.WeekNumber} is below the 12km 3D direct-prescription floor.");

        var targets = new[] { volume * KeyShare, volume * EasyShare, volume * 0.40d };
        var current = new[] { Round(targets[0]), Round(targets[1]), longRun.PlannedLongRunDistanceKm };
        current[0] = Math.Max(MinimumKeyKm, current[0]);
        current[1] = Math.Max(MinimumEasyKm, current[1]);
        if (current[2] < MinimumLongKm || current[2] / volume > LongHardCap + 0.0001d ||
            Math.Abs(current[2] * 2d - Math.Round(current[2] * 2d)) > 0.0001d)
            throw new CatalogSessionPrescriptionInfeasibleException("Resolved 3D long run violates the minimum, 42% hard cap, or 0.5km granularity.");

        while (Math.Abs(current.Sum() - volume) > 0.001d)
        {
            var direction = current.Sum() < volume ? 0.5d : -0.5d;
            var candidates = Enumerable.Range(0, 3)
                .Select(i => new { Index = i, Value = current[i] + direction })
                .Where(c => c.Value >= new[] { MinimumKeyKm, MinimumEasyKm, MinimumLongKm }[c.Index])
                .Where(c => c.Index != 2)
                .OrderBy(c => Math.Abs(c.Value - targets[c.Index]))
                .ThenBy(c => c.Index)
                .ToList();
            if (candidates.Count == 0) throw new CatalogSessionPrescriptionInfeasibleException("No valid deterministic 3D reconciliation adjustment exists.");
            current[candidates[0].Index] = candidates[0].Value;
        }

        if (current[2] / volume > LongHardCap + 0.0001d)
            throw new CatalogSessionPrescriptionInfeasibleException("Reconciled 3D long run exceeds the 42% hard cap.");

        return new V1FourDayWeekAllocation(weekly.WeekNumber, volume, current[2], Round(volume-current[2]), new[] { current[0] }, current[1], 0d,
            new SessionVolumeAllocationTrace(weekly.WeekNumber, volume, current[2], Round(volume-current[2]), current[0], current[1], 0d, PolicyKey, PolicyVersion));
    }

    private static double Round(double value) => Math.Round(value * 2d, MidpointRounding.AwayFromZero) / 2d;
}
