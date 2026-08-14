using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2B-R Part 6 -- Core-only restart matrix, representative subset
/// (not the full 7-point matrix). Continues horizon 25 (Core = 14-25, 12
/// weeks) past its mixed Runway-&gt;Core boundary [13-16] through two more
/// pure Core-only windows [17-20], [21-24], and a natural final 1-week Core
/// window [25-25] -- proving Core-only restart continuation, mid-Core
/// restart, final 1-week Core restart, and terminal completion, all against
/// real PostgreSQL with a restart between every call.
/// </summary>
public sealed class LongHorizonCoreOnlyRestartMatrixTests
{
    [Fact]
    public async Task CoreOnlyContinuation_RestartsThroughToFinalOneWeekTerminalCompletion()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var window = initialWindow;

        // Drive through GE -> mixed Runway+Core [13,16] (3 calls: [5,8],
        // [9,12], [13,16] -- matching the already-proven shape-discovery
        // sequence for horizon 25).
        for (var i = 0; i < 3; i++)
        {
            var call = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call.Outcome);
            window = call.Snapshot!.DarkState.CurrentWindow;
            date = date.AddDays(28);
        }
        Assert.Equal((13, 16), (window.StartGlobalWeek, window.EndGlobalWeek));

        // Restart immediately before the first pure Core-only continuation.
        using var dbBeforeCore = LongHorizonPersistenceTestFixture.NewContext();
        var beforeCore = await new LongHorizonRollingStateRepository(dbBeforeCore).LoadRestartSnapshotAsync(planStateId);
        var preCoreWindow = beforeCore!.DarkState.CurrentWindow;

        // First pure Core-only window [17,20].
        var call5 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, preCoreWindow, date, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call5.Outcome);
        Assert.Equal((17, 20), (call5.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek, call5.Snapshot.DarkState.CurrentWindow.EndGlobalWeek));
        Assert.Equal(LongHorizonStructuralSegmentType.Core, call5.Snapshot.DarkState.CurrentWindow.SegmentsCovered.Single());
        date = date.AddDays(28);

        // Restart mid-Core; reconstruct.
        using var dbMidCore = LongHorizonPersistenceTestFixture.NewContext();
        var midCore = await new LongHorizonRollingStateRepository(dbMidCore).LoadRestartSnapshotAsync(planStateId);
        var midCoreWindow = midCore!.DarkState.CurrentWindow;
        Assert.Equal((17, 20), (midCoreWindow.StartGlobalWeek, midCoreWindow.EndGlobalWeek));

        // Second pure Core-only window [21,24].
        var call6 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, midCoreWindow, date, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call6.Outcome);
        Assert.Equal((21, 24), (call6.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek, call6.Snapshot.DarkState.CurrentWindow.EndGlobalWeek));
        date = date.AddDays(28);

        // Restart before the final 1-week terminal Core window.
        using var dbBeforeFinal = LongHorizonPersistenceTestFixture.NewContext();
        var beforeFinal = await new LongHorizonRollingStateRepository(dbBeforeFinal).LoadRestartSnapshotAsync(planStateId);
        var beforeFinalWindow = beforeFinal!.DarkState.CurrentWindow;

        // Final 1-week Core window [25,25] -- terminal completion.
        var call7 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, beforeFinalWindow, date, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call7.Outcome);
        Assert.Equal((25, 25), (call7.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek, call7.Snapshot.DarkState.CurrentWindow.EndGlobalWeek));

        // Restart after terminal completion; reconstruct fresh and verify.
        using var dbFinal = LongHorizonPersistenceTestFixture.NewContext();
        var final = await new LongHorizonRollingStateRepository(dbFinal).LoadRestartSnapshotAsync(planStateId);

        // No Pending or Blocked weeks remain; every week 1-25 is Activated.
        Assert.Equal(25, final!.DarkState.LifecycleStates.Count);
        Assert.All(final.DarkState.LifecycleStates.Values,
            state => Assert.Equal(RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.LongHorizonNumericLifecycleState.NumericActivated, state));

        // No duplicate activation records for any window; exact ranges only.
        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var records = await verify.LongHorizonActivationWindowRecords.Where(a => a.PlanStateId == planStateId).ToListAsync();
        var distinctRanges = records.Select(r => (r.StartGlobalWeek, r.EndGlobalWeek)).Distinct().ToList();
        Assert.Equal(records.Count, distinctRanges.Count);
        Assert.Contains((25, 25), distinctRanges);

        // No further activation occurs on repeated reconstruction (idempotent).
        using var dbRepeat = LongHorizonPersistenceTestFixture.NewContext();
        var repeat = await new LongHorizonRollingStateRepository(dbRepeat).LoadRestartSnapshotAsync(planStateId);
        Assert.Equal(final.DarkState.CurrentWindow.EndGlobalWeek, repeat!.DarkState.CurrentWindow.EndGlobalWeek);
    }
}
