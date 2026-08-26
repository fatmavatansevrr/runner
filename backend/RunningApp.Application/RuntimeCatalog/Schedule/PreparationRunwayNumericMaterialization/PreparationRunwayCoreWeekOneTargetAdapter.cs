using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

/// <summary>
/// Reads the runway boundary from the existing deterministic Core
/// prescription output. It does not independently calculate or assume a Core
/// Week 1 value.
///
/// Phase 10K-FREQ.6D.7: the per-slot breakdown's KEY_SESSION count is now
/// read from the real, authoritative Core Week 1 prescribed sessions
/// (<paramref name="finalPrescribedPlan"/>) rather than assumed to be
/// exactly one. This is still the exact existing "V1 multi-key" allocation
/// authority (<see cref="FourDaySessionDistanceAllocationPolicy"/> --
/// already the same shared allocator <c>V1FourDaySessionVolumeAllocationPolicy</c>
/// uses for live Core prescription itself), not a new algorithm; for every
/// existing Intermediate 4D caller the real Core Week 1 always has exactly
/// one KEY_SESSION, so this is byte-for-byte unchanged there.
/// </summary>
internal static class PreparationRunwayCoreWeekOneTargetAdapter
{
    public static PreparationRunwayCoreWeekOneNumericTarget FromAuthoritativeCoreBehavior(
        CatalogVolumeAndLongRunPlan volumePlan, CatalogPrescribedPlan finalPrescribedPlan)
    {
        var weekly = volumePlan.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).FirstOrDefault();
        var longRun = volumePlan.LongRunProgression.Weeks.OrderBy(w => w.WeekNumber).FirstOrDefault();
        var firstPrescribedWeek = finalPrescribedPlan.Weeks.OrderBy(w => w.WeekNumber).FirstOrDefault();
        if (weekly is null || longRun is null || firstPrescribedWeek is null)
        {
            throw new InvalidOperationException("Authoritative Core Week 1 numeric prescription is unavailable.");
        }

        var keySessionCount = firstPrescribedWeek.Sessions.Count(s => s.StructuralRole == "KEY_SESSION");
        var allocation = FourDaySessionDistanceAllocationPolicy.Allocate(
            weekly.PlannedWeeklyVolumeKm, longRun.PlannedLongRunDistanceKm, keySessionCount);
        var slots = new List<PreparationRunwayCoreWeekOneSlotTarget>(allocation.KeySessionDistancesKm.Count + 3);
        for (var i = 0; i < allocation.KeySessionDistancesKm.Count; i++)
        {
            slots.Add(new PreparationRunwayCoreWeekOneSlotTarget(PreparationRunwaySlotRole.KeySession, i + 1, allocation.KeySessionDistancesKm[i]));
        }
        slots.Add(new PreparationRunwayCoreWeekOneSlotTarget(PreparationRunwaySlotRole.EasySupport, 1, allocation.FirstEasySupportDistanceKm));
        slots.Add(new PreparationRunwayCoreWeekOneSlotTarget(PreparationRunwaySlotRole.EasySupport, 2, allocation.SecondEasySupportDistanceKm));
        slots.Add(new PreparationRunwayCoreWeekOneSlotTarget(PreparationRunwaySlotRole.LongRun, 1, allocation.LongRunDistanceKm));

        return new PreparationRunwayCoreWeekOneNumericTarget(
            volumePlan.WeeklyVolumePlan.CandidateKey,
            volumePlan.WeeklyVolumePlan.CandidateVersion,
            weekly.PlannedWeeklyVolumeKm,
            longRun.PlannedLongRunDistanceKm,
            slots,
            $"{volumePlan.WeeklyVolumePlan.CandidateKey} v{volumePlan.WeeklyVolumePlan.CandidateVersion} / current deterministic Core prescription pipeline",
            $"CatalogVolumeAndLongRunPlanner + FourDaySessionDistanceAllocationPolicy(keySessionCount={keySessionCount}); Core week {weekly.WeekNumber}; " +
            $"weekly source {weekly.SourceArtifactKey} v{weekly.SourceArtifactVersion}");
    }
}
