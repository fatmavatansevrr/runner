using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2B-R Part 17 -- one focused concurrency race for the Runway-&gt;Core
/// mixed-window boundary activation (not the full concurrency matrix Parts
/// 18-23, which remain out of this phase's completed scope). Mirrors the
/// existing Phase 4L.2A ConcurrentFirstRunwayEntryHasExactlyOneWinner
/// deterministic-race pattern: two independently-loaded snapshots at the same
/// starting xmin/version race for the same mixed-boundary operation.
/// </summary>
public sealed class LongHorizonRunwayCoreMixedConcurrencyTests
{
    [Fact]
    public async Task ConcurrentMixedRunwayToCoreActivationHasExactlyOneWinner()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var window = initialWindow;

        // Drive to immediately before the mixed Runway->Core boundary [13,16].
        for (var i = 0; i < 3; i++)
        {
            var call = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call.Outcome);
            window = call.Snapshot!.DarkState.CurrentWindow;
            date = date.AddDays(28);
        }
        Assert.Equal((13, 16), (window.StartGlobalWeek, window.EndGlobalWeek));

        // Two independent processes load the same starting snapshot/version.
        using var dbA = LongHorizonPersistenceTestFixture.NewContext();
        var snapshotA = await new LongHorizonRollingStateRepository(dbA).LoadRestartSnapshotAsync(planStateId);
        using var dbB = LongHorizonPersistenceTestFixture.NewContext();
        var snapshotB = await new LongHorizonRollingStateRepository(dbB).LoadRestartSnapshotAsync(planStateId);
        Assert.Equal(snapshotA!.ConcurrencyVersion, snapshotB!.ConcurrencyVersion);

        using var dbCountBefore = LongHorizonPersistenceTestFixture.NewContext();
        var coreContextCountBefore = await dbCountBefore.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == planStateId);

        var winner = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, winner.Outcome);

        // Loser: reuses the now-stale version directly against the block adapter (deterministic race simulation).
        using var loserDb = LongHorizonPersistenceTestFixture.NewContext();
        var loser = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(loserDb)).PersistBlockAsync(
            planStateId, snapshotA.ConcurrencyVersion, window.EndGlobalWeek + 1, window.EndGlobalWeek + 4,
            LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "mixed-race-fp", date.AddDays(1), false);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.ConcurrencyConflict, loser.Outcome);

        // Exactly one activation-window record owns the mixed range -- no split ownership.
        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var records = await verify.LongHorizonActivationWindowRecords.Where(
            a => a.PlanStateId == planStateId && a.StartGlobalWeek == 13 && a.EndGlobalWeek == 16).ToListAsync();
        Assert.Single(records);

        // Exactly one NEW Core context row was created by the race (no duplicate ownership from the loser).
        var coreContextCountAfter = await verify.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == planStateId);
        Assert.Equal(1, coreContextCountAfter - coreContextCountBefore);

        // Loser reloads winner state on next reconstruction.
        using var dbReload = LongHorizonPersistenceTestFixture.NewContext();
        var reloaded = await new LongHorizonRollingStateRepository(dbReload).LoadRestartSnapshotAsync(planStateId);
        Assert.Equal(winner.Snapshot!.DarkState.CurrentWindow.EndGlobalWeek, reloaded!.DarkState.CurrentWindow.EndGlobalWeek);
    }
}
