using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2B-R Parts 3-5 -- reachable Runway-&gt;Core mixed-window boundary
/// shapes, derived analytically from the real structural formula
/// (GE = TotalWeeks-20, Runway = 8 weeks, Core = 12 weeks) and the real
/// greedy 4-week window selector, then proven against real PostgreSQL with a
/// restart between every continuation call.
///
/// Derivation (horizons 25/26/27, reusing Phase 4L.2D's own established
/// window sequence [1-4] initial -&gt; [5-8] GE-remainder+Runway -&gt; [9-12]
/// pure Runway continuation):
///   Horizon 25: GE=5, Runway=6-13 (8wks). After [9-12], Runway remainder is
///     week 13 only (1 week) -&gt; call4 window=[13-16] = 1 Runway + 3 Core.
///   Horizon 26: GE=6, Runway=7-14 (8wks). After [9-12], Runway remainder is
///     13-14 (2 weeks) -&gt; call4 window=[13-16] = 2 Runway + 2 Core.
///   Horizon 27: GE=7, Runway=8-15 (8wks). After [9-12], Runway remainder is
///     13-15 (3 weeks) -&gt; call4 window=[13-16] = 3 Runway + 1 Core.
/// The 21-week horizon (GE=1, Runway=2-9) is the naturally ALIGNED case:
/// [2-5] then [6-9] land exactly on the Runway end, so the next call is a
/// clean Runway-only-&gt;Core-only boundary with no mixing (Part 5).
/// </summary>
public sealed class LongHorizonRunwayCoreMixedRestartMatrixTests
{
    [Theory]
    [InlineData(25, 1, 3)] // 1 Runway + 3 Core
    [InlineData(26, 2, 2)] // 2 Runway + 2 Core
    [InlineData(27, 3, 1)] // 3 Runway + 1 Core
    public async Task RunwayCoreMixedWindow_ReachesNaturalShapeAndPersistsAcrossRestart(int totalWeeks, int expectedRunwayCount, int expectedCoreCount)
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(totalWeeks);
        var date1 = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        var call1 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, date1, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call1.Outcome);

        var date2 = date1.AddDays(28);
        var call2 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, call1.Snapshot!.DarkState.CurrentWindow, date2, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call2.Outcome);
        Assert.Equal((9, 12), (call2.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek, call2.Snapshot.DarkState.CurrentWindow.EndGlobalWeek));

        // Restart before the mixed Runway->Core boundary call.
        using var dbBefore = LongHorizonPersistenceTestFixture.NewContext();
        var beforeSnapshot = await new LongHorizonRollingStateRepository(dbBefore).LoadRestartSnapshotAsync(planStateId);
        var windowBeforeMixed = beforeSnapshot!.DarkState.CurrentWindow;
        var prescriptionIdBefore = beforeSnapshot.DarkState.RunwayPrescription!.PrescriptionId;

        var date3 = date2.AddDays(28);
        var call3 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, windowBeforeMixed, date3, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call3.Outcome);

        // Restart after the mixed boundary; reconstruct fresh.
        using var dbAfter = LongHorizonPersistenceTestFixture.NewContext();
        var afterSnapshot = await new LongHorizonRollingStateRepository(dbAfter).LoadRestartSnapshotAsync(planStateId);
        var mixedWindow = afterSnapshot!.DarkState.CurrentWindow;

        Assert.Equal(13, mixedWindow.StartGlobalWeek);
        Assert.Equal(16, mixedWindow.EndGlobalWeek);
        Assert.Contains(LongHorizonStructuralSegmentType.PreparationRunway, mixedWindow.SegmentsCovered);
        Assert.Contains(LongHorizonStructuralSegmentType.Core, mixedWindow.SegmentsCovered);

        var runwayWeeks = mixedWindow.Weeks.Where(w => w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway).ToList();
        var coreWeeks = mixedWindow.Weeks.Where(w => w.SegmentType == LongHorizonStructuralSegmentType.Core).ToList();
        Assert.Equal(expectedRunwayCount, runwayWeeks.Count);
        Assert.Equal(expectedCoreCount, coreWeeks.Count);

        // No Runway regeneration: same prescription identity as before the mixed call.
        Assert.Equal(prescriptionIdBefore, afterSnapshot.DarkState.RunwayPrescription!.PrescriptionId);

        // Historical Runway weeks (from earlier calls) remain unchanged.
        foreach (var week in beforeSnapshot.DarkState.ActivatedWeeks.Values)
        {
            var reloaded = afterSnapshot.DarkState.ActivatedWeeks[week.GlobalWeekNumber];
            Assert.Equal(week.TotalWeeklyVolumeKm, reloaded.TotalWeeklyVolumeKm);
            Assert.Equal(week.CalendarDates, reloaded.CalendarDates);
        }

        // Exactly one activation-window record owns the full mixed range; atomic commit.
        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var mixedRecord = await verify.LongHorizonActivationWindowRecords.SingleAsync(
            a => a.PlanStateId == planStateId && a.StartGlobalWeek == 13 && a.EndGlobalWeek == 16);
        Assert.NotNull(mixedRecord);

        // Future Core weeks (beyond the mixed window) remain Pending with no sessions.
        var futureCoreWeek = mixedWindow.EndGlobalWeek + 1;
        if (futureCoreWeek <= totalWeeks)
        {
            Assert.Equal(RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.LongHorizonNumericLifecycleState.NumericPending, afterSnapshot.DarkState.LifecycleStates[futureCoreWeek]);
            Assert.False(afterSnapshot.DarkState.ActivatedWeeks.ContainsKey(futureCoreWeek));
        }

        // Next Pending boundary is exact.
        var plan = await verify.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == planStateId);
        Assert.Equal(16, plan.CurrentWindowEndWeek);
        Assert.Equal(13, plan.CurrentWindowStartWeek);
    }

    [Fact]
    public async Task AlignedRunwayOnlyToCoreOnly_HasNoMixedOwnershipAndCoreBeginsCorrectly()
    {
        // 21-week horizon: GE=1, Runway=2-9. [2-5] then [6-9] land exactly on
        // the Runway end -- the natural aligned case (Part 5), no mixing.
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var date1 = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        var call1 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, date1, catalogRoot, candidate);
        var date2 = date1.AddDays(28);
        var call2 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, call1.Snapshot!.DarkState.CurrentWindow, date2, catalogRoot, candidate);
        Assert.Equal((6, 9), (call2.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek, call2.Snapshot.DarkState.CurrentWindow.EndGlobalWeek));

        using var dbBefore = LongHorizonPersistenceTestFixture.NewContext();
        var beforeSnapshot = await new LongHorizonRollingStateRepository(dbBefore).LoadRestartSnapshotAsync(planStateId);
        var finalRunwayWindow = beforeSnapshot!.DarkState.CurrentWindow;
        var targetLockIdBefore = beforeSnapshot.DarkState.RunwayTargetLock!.CreatedByDecisionId;
        var prescriptionIdBefore = beforeSnapshot.DarkState.RunwayPrescription!.PrescriptionId;

        var date3 = date2.AddDays(28);
        var call3 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, finalRunwayWindow, date3, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call3.Outcome);

        using var dbAfter = LongHorizonPersistenceTestFixture.NewContext();
        var afterSnapshot = await new LongHorizonRollingStateRepository(dbAfter).LoadRestartSnapshotAsync(planStateId);
        var coreOnlyWindow = afterSnapshot!.DarkState.CurrentWindow;

        // No artificial mixed ownership: the Core-only window contains no Runway weeks.
        Assert.DoesNotContain(LongHorizonStructuralSegmentType.PreparationRunway, coreOnlyWindow.SegmentsCovered);
        Assert.Equal(LongHorizonStructuralSegmentType.Core, coreOnlyWindow.SegmentsCovered.Single());
        Assert.Equal(10, coreOnlyWindow.StartGlobalWeek);

        // Runway target lock/prescription remain historical and unchanged.
        Assert.Equal(targetLockIdBefore, afterSnapshot.DarkState.RunwayTargetLock!.CreatedByDecisionId);
        Assert.Equal(prescriptionIdBefore, afterSnapshot.DarkState.RunwayPrescription!.PrescriptionId);

        // No calendar overlap: Core weeks start strictly after the last Runway week's dates end.
        var lastRunwayWeek = beforeSnapshot.DarkState.ActivatedWeeks[9];
        var firstCoreWeek = afterSnapshot.DarkState.ActivatedWeeks[10];
        Assert.True(firstCoreWeek.CalendarDates!.Value.Start > lastRunwayWeek.CalendarDates!.Value.End);
    }
}
