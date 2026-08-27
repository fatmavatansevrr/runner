using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal sealed record V1FourDayWeekAllocation(
    int WeekNumber,
    double PlannedWeeklyVolumeKm,
    double LongRunDistanceKm,
    double ResidualVolumeKm,
    IReadOnlyList<double> KeySessionDistancesKm,
    IReadOnlyList<double> EasySupportDistancesKm,
    SessionVolumeAllocationTrace Trace)
{
    /// <summary>Back-compat accessor for every pre-FREQ.4 (single-KEY) consumer -- unchanged value for a 1-KEY week.</summary>
    public double KeySessionDistanceKm => KeySessionDistancesKm[0];

    /// <summary>Back-compat accessor -- unchanged value for every 2-EASY caller.</summary>
    public double FirstEasySupportDistanceKm => EasySupportDistancesKm[0];

    /// <summary>Back-compat accessor -- unchanged value for every 2-EASY caller.</summary>
    public double SecondEasySupportDistanceKm => EasySupportDistancesKm[1];

    /// <summary>
    /// Explicit record equality: the compiler-generated version compares
    /// <see cref="KeySessionDistancesKm"/> by reference (the standard C#
    /// array/list-in-record gotcha), which would make two allocations with
    /// identical KEY distances compare unequal. Overridden here for real
    /// structural (SequenceEqual) comparison instead.
    /// </summary>
    public bool Equals(V1FourDayWeekAllocation? other) =>
        other is not null &&
        WeekNumber == other.WeekNumber &&
        PlannedWeeklyVolumeKm.Equals(other.PlannedWeeklyVolumeKm) &&
        LongRunDistanceKm.Equals(other.LongRunDistanceKm) &&
        ResidualVolumeKm.Equals(other.ResidualVolumeKm) &&
        KeySessionDistancesKm.SequenceEqual(other.KeySessionDistancesKm) &&
        EasySupportDistancesKm.SequenceEqual(other.EasySupportDistancesKm) &&
        Trace.Equals(other.Trace);

    public override int GetHashCode() =>
        HashCode.Combine(
            WeekNumber, PlannedWeeklyVolumeKm, LongRunDistanceKm, ResidualVolumeKm,
            KeySessionDistancesKm.Aggregate(17, HashCode.Combine),
            EasySupportDistancesKm.Aggregate(17, HashCode.Combine), Trace);
}

/// <summary>
/// Phase 10K-FREQ.4: this policy's name predates its generalization beyond
/// the literal 4-day (1 KEY + 2 EASY + 1 LONG) shape -- it is reused,
/// unmodified in behavior, for any "V1 multi-key" shape sharing the same 2
/// EASY_SUPPORT + 1 LONG_RUN structure (e.g. a hypothetical 5D 2 KEY + 2
/// EASY + 1 LONG layout). A full rename was considered and deliberately
/// deferred -- this is a mechanism-only phase with a wide existing call-site
/// footprint (session prescription, LongHorizon, PreparationRunway), and a
/// rename carries real regression risk disproportionate to a naming
/// concern alone. Flagged here, not silently left unaddressed.
/// </summary>
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
        // Phase 10K-FREQ.6D.26 -- generalized the EASY_SUPPORT count from a
        // hardcoded ==2 (byte-identical for 4D and 5D Core, which both
        // happen to have 2 EASY) to the structural identity every session
        // week already obeys: EASY = total - KEY - LONG. 6D Core has 3 EASY
        // (2 KEY + 3 EASY + 1 LONG); this was previously unreachable for it.
        var keySessionCount = sessions.Count(s => s.StructuralRole == "KEY_SESSION");
        var easySessionCount = sessions.Count(s => s.StructuralRole == "EASY_SUPPORT");
        if (keySessionCount < 1 ||
            easySessionCount < 1 ||
            sessions.Count(s => s.StructuralRole == "LONG_RUN") != 1 ||
            sessions.Count != keySessionCount + easySessionCount + 1)
        {
            throw new CatalogSessionPrescriptionInfeasibleException($"Week {weekly.WeekNumber} does not match the V1 multi-key session shape.");
        }

        FourDaySessionDistanceAllocation distances;
        try
        {
            distances = FourDaySessionDistanceAllocationPolicy.Allocate(
                weekly.PlannedWeeklyVolumeKm, longRun.PlannedLongRunDistanceKm, keySessionCount, easySessionCount);
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
            distances.KeySessionDistancesKm,
            distances.EasySupportDistancesKm,
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
