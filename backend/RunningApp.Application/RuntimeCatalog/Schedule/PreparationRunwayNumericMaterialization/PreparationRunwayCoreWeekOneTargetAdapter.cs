using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

/// <summary>
/// Reads the runway boundary from the existing deterministic Core
/// prescription output. It does not independently calculate or assume a Core
/// Week 1 value.
/// </summary>
internal static class PreparationRunwayCoreWeekOneTargetAdapter
{
    public static PreparationRunwayCoreWeekOneNumericTarget FromAuthoritativeCoreBehavior(
        CatalogVolumeAndLongRunPlan volumePlan)
    {
        var weekly = volumePlan.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).FirstOrDefault();
        var longRun = volumePlan.LongRunProgression.Weeks.OrderBy(w => w.WeekNumber).FirstOrDefault();
        if (weekly is null || longRun is null)
        {
            throw new InvalidOperationException("Authoritative Core Week 1 numeric prescription is unavailable.");
        }

        var allocation = FourDaySessionDistanceAllocationPolicy.Allocate(
            weekly.PlannedWeeklyVolumeKm, longRun.PlannedLongRunDistanceKm);
        var slots = new[]
        {
            new PreparationRunwayCoreWeekOneSlotTarget(PreparationRunwaySlotRole.KeySession, 1, allocation.KeySessionDistanceKm),
            new PreparationRunwayCoreWeekOneSlotTarget(PreparationRunwaySlotRole.EasySupport, 1, allocation.FirstEasySupportDistanceKm),
            new PreparationRunwayCoreWeekOneSlotTarget(PreparationRunwaySlotRole.EasySupport, 2, allocation.SecondEasySupportDistanceKm),
            new PreparationRunwayCoreWeekOneSlotTarget(PreparationRunwaySlotRole.LongRun, 1, allocation.LongRunDistanceKm),
        };

        return new PreparationRunwayCoreWeekOneNumericTarget(
            volumePlan.WeeklyVolumePlan.CandidateKey,
            volumePlan.WeeklyVolumePlan.CandidateVersion,
            weekly.PlannedWeeklyVolumeKm,
            longRun.PlannedLongRunDistanceKm,
            slots,
            "TEN_K__4D__INTERMEDIATE v10 / current deterministic Core prescription pipeline",
            $"CatalogVolumeAndLongRunPlanner + V1FourDaySessionVolumeAllocationPolicy; Core week {weekly.WeekNumber}; " +
            $"weekly source {weekly.SourceArtifactKey} v{weekly.SourceArtifactVersion}");
    }
}
