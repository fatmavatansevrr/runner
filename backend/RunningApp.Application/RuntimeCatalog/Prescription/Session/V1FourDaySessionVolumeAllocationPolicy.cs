using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal sealed record V1FourDayWeekAllocation(
    int WeekNumber,
    double PlannedWeeklyVolumeKm,
    double LongRunDistanceKm,
    double ResidualVolumeKm,
    IReadOnlyList<double> KeySessionDistancesKm,
    double FirstEasySupportDistanceKm,
    double SecondEasySupportDistanceKm,
    SessionVolumeAllocationTrace Trace)
{
    /// <summary>Back-compat accessor for every pre-FREQ.4 (single-KEY) consumer -- unchanged value for a 1-KEY week.</summary>
    public double KeySessionDistanceKm => KeySessionDistancesKm[0];

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
        FirstEasySupportDistanceKm.Equals(other.FirstEasySupportDistanceKm) &&
        SecondEasySupportDistanceKm.Equals(other.SecondEasySupportDistanceKm) &&
        Trace.Equals(other.Trace);

    public override int GetHashCode() =>
        HashCode.Combine(
            WeekNumber, PlannedWeeklyVolumeKm, LongRunDistanceKm, ResidualVolumeKm,
            KeySessionDistancesKm.Aggregate(17, HashCode.Combine), FirstEasySupportDistanceKm,
            SecondEasySupportDistanceKm, Trace);
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
        var keySessionCount = sessions.Count(s => s.StructuralRole == "KEY_SESSION");
        if (keySessionCount < 1 ||
            sessions.Count(s => s.StructuralRole == "EASY_SUPPORT") != 2 ||
            sessions.Count(s => s.StructuralRole == "LONG_RUN") != 1 ||
            sessions.Count != keySessionCount + 3)
        {
            throw new CatalogSessionPrescriptionInfeasibleException($"Week {weekly.WeekNumber} does not match the V1 multi-key session shape.");
        }

        FourDaySessionDistanceAllocation distances;
        try
        {
            distances = FourDaySessionDistanceAllocationPolicy.Allocate(
                weekly.PlannedWeeklyVolumeKm, longRun.PlannedLongRunDistanceKm, keySessionCount);
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
