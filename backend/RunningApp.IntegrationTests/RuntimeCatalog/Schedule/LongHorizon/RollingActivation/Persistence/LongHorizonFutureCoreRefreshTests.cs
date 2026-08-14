using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2E -- future-only Core context refresh capability, proven
/// against real PostgreSQL. Drives horizon 25 (Core = 14-25) through GE,
/// Runway, the mixed Runway->Core boundary, and one pure Core-only window
/// [17-20] using the existing production harness, then exercises the new
/// <see cref="LongHorizonFutureCoreRefreshOrchestrator"/> for the next
/// window instead of plain continuation -- proving the explicit eligibility
/// gate, real condition re-resolution, real Core regeneration, V1 immutable
/// history, V2 future-only ownership, and restart reconstruction.
/// </summary>
public sealed class LongHorizonFutureCoreRefreshTests
{
    private static async Task<(Guid PlanStateId, string CatalogRoot, RunningApp.Application.RuntimeCatalog.PlanCatalogCandidateSummary Candidate, DateOnly Date, RollingNumericActivationWindow Window)>
        DriveToFirstCoreOnlyWindowAsync()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var window = initialWindow;

        // [5,8] mixed GE+Runway -> [9,12] Runway -> [13,16] mixed Runway+Core -> [17,20] Core-only.
        for (var i = 0; i < 4; i++)
        {
            var call = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call.Outcome);
            window = call.Snapshot!.DarkState.CurrentWindow;
            date = date.AddDays(28);
        }
        Assert.Equal((17, 20), (window.StartGlobalWeek, window.EndGlobalWeek));
        return (planStateId, catalogRoot, candidate, date, window);
    }

    [Fact]
    public async Task EligibleRefresh_CreatesV2AndPersistsAcrossRestart()
    {
        var (planStateId, catalogRoot, _, date, window) = await DriveToFirstCoreOnlyWindowAsync();

        using var dbBefore = LongHorizonPersistenceTestFixture.NewContext();
        var repoBefore = new LongHorizonRollingStateRepository(dbBefore);
        var snapshotBefore = await repoBefore.LoadRestartSnapshotAsync(planStateId);
        var activeContextBefore = await repoBefore.GetActiveCoreContextAsync(planStateId);
        Assert.NotNull(activeContextBefore);

        var refreshOrchestrator = new LongHorizonFutureCoreRefreshOrchestrator(repoBefore);
        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(window, planStateId);
        var refreshRequest = new LongHorizonFutureCoreRefreshRequest
        {
            PlanStateId = planStateId,
            ExpectedAggregateVersion = snapshotBefore!.ConcurrencyVersion,
            RequestedAsOfDate = date,
            TrainingDayEvidence = evidenceRows,
            CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            PlanStartDate = LongHorizonFullLifecycleTestFixture.StartDate,
            RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(25 * 7),
            CatalogRootPath = catalogRoot,
        };

        var result = await refreshOrchestrator.RefreshAsync(refreshRequest);

        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.Refreshed, result.Outcome);
        Assert.NotNull(result.NewContextId);
        Assert.NotEqual(activeContextBefore!.CoreContextId, result.NewContextId);
        Assert.True(result.NewContextVersion > result.PreviousContextVersion);
        Assert.Equal(21, result.EffectiveFromGlobalWeek);

        // Restart: fresh context, reconstruct.
        using var dbAfter = LongHorizonPersistenceTestFixture.NewContext();
        var repoAfter = new LongHorizonRollingStateRepository(dbAfter);
        var snapshotAfter = await repoAfter.LoadRestartSnapshotAsync(planStateId);
        var activeContextAfter = await repoAfter.GetActiveCoreContextAsync(planStateId);

        Assert.NotNull(activeContextAfter);
        Assert.Equal(result.NewContextId, activeContextAfter!.CoreContextId);

        // V1 (the prior active context) remains persisted, now Superseded, and points to V2.
        var v1Row = await dbAfter.LongHorizonCoreContextRecords.SingleAsync(c => c.Id == activeContextBefore.CoreContextId);
        Assert.Equal(RunningApp.Domain.Enums.LongHorizonPersistedCoreContextStatus.Superseded, v1Row.Status);
        Assert.Equal(result.NewContextId, v1Row.SupersededByContextId);

        // Historical weeks 17-20 remain owned by their original numeric/session values (unchanged).
        foreach (var week in snapshotBefore.DarkState.ActivatedWeeks.Values.Where(w => w.GlobalWeekNumber is >= 17 and <= 20))
        {
            var reloaded = snapshotAfter!.DarkState.ActivatedWeeks[week.GlobalWeekNumber];
            Assert.Equal(week.TotalWeeklyVolumeKm, reloaded.TotalWeeklyVolumeKm);
            Assert.Equal(week.CalendarDates, reloaded.CalendarDates);
        }

        // Future suffix beyond the new window remains Pending.
        Assert.Equal(RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.LongHorizonNumericLifecycleState.NumericPending,
            snapshotAfter!.DarkState.LifecycleStates[25]);
    }

    [Fact]
    public async Task IneligibleRefresh_DuringRunway_Rejects()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        // Only one call: still inside the GE+Runway mixed window [5,8], nowhere near Core.
        var call1 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, date, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call1.Outcome);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);

        var refreshOrchestrator = new LongHorizonFutureCoreRefreshOrchestrator(repo);
        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(call1.Snapshot!.DarkState.CurrentWindow, planStateId);
        var result = await refreshOrchestrator.RefreshAsync(new LongHorizonFutureCoreRefreshRequest
        {
            PlanStateId = planStateId,
            ExpectedAggregateVersion = snapshot!.ConcurrencyVersion,
            RequestedAsOfDate = date.AddDays(28),
            TrainingDayEvidence = evidenceRows,
            CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            PlanStartDate = LongHorizonFullLifecycleTestFixture.StartDate,
            RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(25 * 7),
            CatalogRootPath = catalogRoot,
        });

        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.Ineligible, result.Outcome);
        Assert.Equal(LongHorizonCoreRefreshEligibilityOutcome.RejectedRunwayStillPending, result.Eligibility!.Outcome);
    }

    [Fact]
    public async Task IneligibleRefresh_SameAsOfDate_Rejects()
    {
        var (planStateId, catalogRoot, _, date, window) = await DriveToFirstCoreOnlyWindowAsync();

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);
        var activeContext = await repo.GetActiveCoreContextAsync(planStateId);

        var refreshOrchestrator = new LongHorizonFutureCoreRefreshOrchestrator(repo);
        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(window, planStateId);
        var result = await refreshOrchestrator.RefreshAsync(new LongHorizonFutureCoreRefreshRequest
        {
            PlanStateId = planStateId,
            ExpectedAggregateVersion = snapshot!.ConcurrencyVersion,
            RequestedAsOfDate = activeContext!.AsOfDate, // same date as the active context -- not strictly later.
            TrainingDayEvidence = evidenceRows,
            CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            PlanStartDate = LongHorizonFullLifecycleTestFixture.StartDate,
            RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(25 * 7),
            CatalogRootPath = catalogRoot,
        });

        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.Ineligible, result.Outcome);
        Assert.Equal(LongHorizonCoreRefreshEligibilityOutcome.RejectedSameOrEarlierAsOfDate, result.Eligibility!.Outcome);
    }

    [Fact]
    public async Task NextActivationAfterRefresh_UsesV2AndRestarts()
    {
        var (planStateId, catalogRoot, candidate, date, window) = await DriveToFirstCoreOnlyWindowAsync();

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);
        var refreshOrchestrator = new LongHorizonFutureCoreRefreshOrchestrator(repo);
        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(window, planStateId);
        var refreshResult = await refreshOrchestrator.RefreshAsync(new LongHorizonFutureCoreRefreshRequest
        {
            PlanStateId = planStateId,
            ExpectedAggregateVersion = snapshot!.ConcurrencyVersion,
            RequestedAsOfDate = date,
            TrainingDayEvidence = evidenceRows,
            CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            PlanStartDate = LongHorizonFullLifecycleTestFixture.StartDate,
            RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(25 * 7),
            CatalogRootPath = catalogRoot,
        });
        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.Refreshed, refreshResult.Outcome);

        // Restart, reconstruct, then continue normally -- next activation must
        // use V2 (the refreshed context), not regenerate or duplicate.
        using var dbRestart = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(dbRestart).LoadRestartSnapshotAsync(planStateId);
        var v2Window = reconstructed!.DarkState.CurrentWindow;
        Assert.Equal((21, 24), (v2Window.StartGlobalWeek, v2Window.EndGlobalWeek));

        var nextDate = date.AddDays(28);
        var nextCall = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, v2Window, nextDate, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, nextCall.Outcome);
        Assert.Equal((25, 25), (nextCall.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek, nextCall.Snapshot.DarkState.CurrentWindow.EndGlobalWeek));

        using var dbFinal = LongHorizonPersistenceTestFixture.NewContext();
        var final = await new LongHorizonRollingStateRepository(dbFinal).LoadRestartSnapshotAsync(planStateId);
        Assert.All(final!.DarkState.LifecycleStates.Values,
            state => Assert.Equal(RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.LongHorizonNumericLifecycleState.NumericActivated, state));
    }

    [Fact]
    public async Task RefreshReplay_WithSameAggregateVersion_IsRejectedAsStale()
    {
        var (planStateId, catalogRoot, _, date, window) = await DriveToFirstCoreOnlyWindowAsync();

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);
        var refreshOrchestrator = new LongHorizonFutureCoreRefreshOrchestrator(repo);
        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(window, planStateId);
        var request = new LongHorizonFutureCoreRefreshRequest
        {
            PlanStateId = planStateId,
            ExpectedAggregateVersion = snapshot!.ConcurrencyVersion,
            RequestedAsOfDate = date,
            TrainingDayEvidence = evidenceRows,
            CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            PlanStartDate = LongHorizonFullLifecycleTestFixture.StartDate,
            RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(25 * 7),
            CatalogRootPath = catalogRoot,
        };

        var first = await refreshOrchestrator.RefreshAsync(request);
        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.Refreshed, first.Outcome);

        // Exact replay with the SAME (now-stale) expected version -- must not duplicate V2.
        using var dbReplay = LongHorizonPersistenceTestFixture.NewContext();
        var replayOrchestrator = new LongHorizonFutureCoreRefreshOrchestrator(new LongHorizonRollingStateRepository(dbReplay));
        var replay = await replayOrchestrator.RefreshAsync(request);
        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.StaleVersion, replay.Outcome);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var contextCount = await verify.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == planStateId && c.Id == first.NewContextId);
        Assert.Equal(1, contextCount);
    }

    [Fact]
    public async Task ConcurrentIdenticalRefresh_HasExactlyOneWinner()
    {
        var (planStateId, catalogRoot, _, date, window) = await DriveToFirstCoreOnlyWindowAsync();

        using var dbA = LongHorizonPersistenceTestFixture.NewContext();
        var repoA = new LongHorizonRollingStateRepository(dbA);
        var snapshotA = await repoA.LoadRestartSnapshotAsync(planStateId);
        using var dbB = LongHorizonPersistenceTestFixture.NewContext();
        var repoB = new LongHorizonRollingStateRepository(dbB);
        var snapshotB = await repoB.LoadRestartSnapshotAsync(planStateId);
        Assert.Equal(snapshotA!.ConcurrencyVersion, snapshotB!.ConcurrencyVersion);

        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(window, planStateId);
        LongHorizonFutureCoreRefreshRequest BuildRequest(uint version) => new()
        {
            PlanStateId = planStateId,
            ExpectedAggregateVersion = version,
            RequestedAsOfDate = date,
            TrainingDayEvidence = evidenceRows,
            CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            PlanStartDate = LongHorizonFullLifecycleTestFixture.StartDate,
            RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(25 * 7),
            CatalogRootPath = catalogRoot,
        };

        var winner = await new LongHorizonFutureCoreRefreshOrchestrator(repoA).RefreshAsync(BuildRequest(snapshotA.ConcurrencyVersion));
        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.Refreshed, winner.Outcome);

        var loser = await new LongHorizonFutureCoreRefreshOrchestrator(repoB).RefreshAsync(BuildRequest(snapshotB.ConcurrencyVersion));
        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.StaleVersion, loser.Outcome);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var activeCount = await verify.LongHorizonCoreContextRecords.CountAsync(
            c => c.PlanStateId == planStateId && c.Status == RunningApp.Domain.Enums.LongHorizonPersistedCoreContextStatus.Active);
        Assert.Equal(1, activeCount);

        // Loser reloads winner state.
        using var dbReload = LongHorizonPersistenceTestFixture.NewContext();
        var reloaded = await new LongHorizonRollingStateRepository(dbReload).GetActiveCoreContextAsync(planStateId);
        Assert.Equal(winner.NewContextId, reloaded!.CoreContextId);
    }
}
