using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

internal static class LongHorizonConcurrentOperationHarness
{
    internal sealed record Attempt<T>(T? Result, Exception? Exception, Guid ContextInstanceId, int BackendProcessId);
    internal sealed record Race<T>(Attempt<T> Left, Attempt<T> Right);

    internal static async Task<Race<T>> RunAsync<T>(
        Func<ILongHorizonRollingStateRepository, Task<T>> left,
        Func<ILongHorizonRollingStateRepository, Task<T>> right,
        Func<ILongHorizonRollingStateRepository, ILongHorizonRollingStateRepository>? wrapLeft = null,
        Func<ILongHorizonRollingStateRepository, ILongHorizonRollingStateRepository>? wrapRight = null)
    {
        using var barrier = new Barrier(2);
        await using var leftDb = LongHorizonPersistenceTestFixture.NewContext();
        await using var rightDb = LongHorizonPersistenceTestFixture.NewContext();
        // Keep both physical pooled connections leased for the complete race;
        // otherwise sequential PID probes may return the same returned-to-pool
        // backend even though the later SaveChanges calls are independent.
        await leftDb.Database.OpenConnectionAsync();
        await rightDb.Database.OpenConnectionAsync();
        var leftPid = await leftDb.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        var rightPid = await rightDb.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        Assert.NotEqual(leftDb.ContextId.InstanceId, rightDb.ContextId.InstanceId);
        Assert.NotEqual(leftPid, rightPid);

        ILongHorizonRollingStateRepository leftRepo = new LongHorizonRollingStateRepository(
            leftDb, constraintMutation: new CoordinatedSaveMutation(barrier));
        ILongHorizonRollingStateRepository rightRepo = new LongHorizonRollingStateRepository(
            rightDb, constraintMutation: new CoordinatedSaveMutation(barrier));
        leftRepo = wrapLeft?.Invoke(leftRepo) ?? leftRepo;
        rightRepo = wrapRight?.Invoke(rightRepo) ?? rightRepo;

        async Task<Attempt<T>> CaptureAsync(Func<ILongHorizonRollingStateRepository, Task<T>> action,
            ILongHorizonRollingStateRepository repository, AppDbContext db, int pid)
        {
            try { return new Attempt<T>(await action(repository), null, db.ContextId.InstanceId, pid); }
            catch (Exception exception) { return new Attempt<T>(default, exception, db.ContextId.InstanceId, pid); }
        }

        var leftTask = Task.Run(() => CaptureAsync(left, leftRepo, leftDb, leftPid));
        var rightTask = Task.Run(() => CaptureAsync(right, rightRepo, rightDb, rightPid));
        await Task.WhenAll(leftTask, rightTask);
        return new Race<T>(leftTask.Result, rightTask.Result);
    }

    private sealed class CoordinatedSaveMutation(Barrier barrier) : ILongHorizonPersistenceConstraintMutation
    {
        public void Stage(AppDbContext db, LongHorizonPersistenceOperation operation, Guid planStateId)
        {
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(30)))
                throw new TimeoutException($"Race contender did not reach SaveChanges for {operation}.");
        }
    }
}

public sealed class LongHorizonRemainingConcurrencyTests
{
    [Theory]
    [InlineData(25, 1, 3)]
    [InlineData(26, 2, 2)]
    [InlineData(27, 3, 1)]
    public async Task ConcurrentMixedActivation_AllNaturalShapes_HaveOneDurableWinner(int totalWeeks, int runwayWeeks, int coreWeeks)
    {
        var state = await DriveAsync(totalWeeks, 2);
        var beforeVersion = await VersionAsync(state.PlanStateId);
        var race = await LongHorizonConcurrentOperationHarness.RunAsync(
            repo => LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate, repo),
            repo => LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate, repo));

        AssertOneSafeWinner(race.Left, race.Right);
        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        var snapshot = await new LongHorizonRollingStateRepository(fresh).LoadRestartSnapshotAsync(state.PlanStateId);
        Assert.NotNull(snapshot);
        Assert.NotEqual(beforeVersion, snapshot!.ConcurrencyVersion);
        Assert.Equal(runwayWeeks, snapshot.DarkState.CurrentWindow.Weeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway));
        Assert.Equal(coreWeeks, snapshot.DarkState.CurrentWindow.Weeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.Core));
        Assert.Equal(1, await fresh.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == state.PlanStateId
            && a.StartGlobalWeek == snapshot.DarkState.CurrentWindow.StartGlobalWeek && a.EndGlobalWeek == snapshot.DarkState.CurrentWindow.EndGlobalWeek));
        Assert.Equal(1, await fresh.LongHorizonRunwayStates.CountAsync(r => r.PlanStateId == state.PlanStateId));
        AssertNoDuplicateSessions(fresh, state.PlanStateId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentCoreOnly_SameOrDifferentKey_HasOneWinnerAndNextRangeRemainsActivatable(bool differentKey)
    {
        var state = await DriveAsync(21, 2);
        var race = await LongHorizonConcurrentOperationHarness.RunAsync(
            repo => LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate, repo),
            repo => LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate, repo),
            wrapRight: differentKey ? repo => new ActivationKeyOverrideRepository(repo, ":different-decision") : null);
        AssertOneSafeWinner(race.Left, race.Right);

        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        var winner = await new LongHorizonRollingStateRepository(fresh).LoadRestartSnapshotAsync(state.PlanStateId);
        Assert.Equal((10, 13), (winner!.DarkState.CurrentWindow.StartGlobalWeek, winner.DarkState.CurrentWindow.EndGlobalWeek));
        AssertNoDuplicateSessions(fresh, state.PlanStateId);
        var next = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(
            state.PlanStateId, winner.DarkState.CurrentWindow, state.Date.AddDays(28), state.CatalogRoot, state.Candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, next.Outcome);
        Assert.Equal((14, 17), (next.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek, next.Snapshot.DarkState.CurrentWindow.EndGlobalWeek));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentFutureCoreRefresh_IdenticalOrDifferentEvidence_HasOneV2AndPreservesV1(bool differentEvidence)
    {
        var state = await DriveAsync(25, 4);
        await using var loadA = LongHorizonPersistenceTestFixture.NewContext();
        await using var loadB = LongHorizonPersistenceTestFixture.NewContext();
        var snapshotA = await new LongHorizonRollingStateRepository(loadA).LoadRestartSnapshotAsync(state.PlanStateId);
        var snapshotB = await new LongHorizonRollingStateRepository(loadB).LoadRestartSnapshotAsync(state.PlanStateId);
        Assert.Equal(snapshotA!.ConcurrencyVersion, snapshotB!.ConcurrencyVersion);
        var v1 = await loadA.LongHorizonCoreContextRecords.AsNoTracking().SingleAsync(c => c.PlanStateId == state.PlanStateId && c.Status == LongHorizonPersistedCoreContextStatus.Active);
        var contextCountBefore = await loadA.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == state.PlanStateId);
        var v1Weeks = await HistoricalFingerprintAsync(loadA, state.PlanStateId, 20);
        var requestA = BuildRefresh(state, snapshotA, "A");
        var requestB = BuildRefresh(state, snapshotB, differentEvidence ? "B-different" : "A");

        var race = await LongHorizonConcurrentOperationHarness.RunAsync(
            repo => new LongHorizonFutureCoreRefreshOrchestrator(repo).RefreshAsync(requestA),
            repo => new LongHorizonFutureCoreRefreshOrchestrator(repo).RefreshAsync(requestB));
        Assert.Equal(1, new[] { race.Left.Result?.Outcome, race.Right.Result?.Outcome }.Count(o => o == LongHorizonFutureCoreRefreshOutcome.Refreshed));

        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(contextCountBefore + 1, await fresh.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == state.PlanStateId));
        Assert.Equal(1, await fresh.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == state.PlanStateId && c.Status == LongHorizonPersistedCoreContextStatus.Active));
        var v1After = await fresh.LongHorizonCoreContextRecords.SingleAsync(c => c.Id == v1.Id);
        Assert.Equal(LongHorizonPersistedCoreContextStatus.Superseded, v1After.Status);
        Assert.NotNull(v1After.SupersededByContextId);
        Assert.Equal(v1Weeks, await HistoricalFingerprintAsync(fresh, state.PlanStateId, 20));
        AssertNoDuplicateSessions(fresh, state.PlanStateId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentBlock_SameOrDifferentReason_HasOneAuthoritativeBlock(bool differentReason)
    {
        var initialized = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        await using var load = LongHorizonPersistenceTestFixture.NewContext();
        var snapshot = await new LongHorizonRollingStateRepository(load).LoadRestartSnapshotAsync(initialized.PlanStateId);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var race = await LongHorizonConcurrentOperationHarness.RunAsync(
            repo => new LongHorizonRollingBlockPersistenceAdapter(repo).PersistBlockAsync(initialized.PlanStateId, snapshot!.ConcurrencyVersion, 5, 8,
                LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "block-a", date, true),
            repo => new LongHorizonRollingBlockPersistenceAdapter(repo).PersistBlockAsync(initialized.PlanStateId, snapshot!.ConcurrencyVersion, 5, 8,
                differentReason ? LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved) : LongHorizonReasonCode.SafetyReassessmentRequired,
                differentReason ? "MoreTrainingDataNeeded" : "SafetyReviewRequired", "block-b", date, true));
        AssertOneSafeWinner(race.Left, race.Right);
        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        var plan = await fresh.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == initialized.PlanStateId);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericActivationBlocked, plan.CurrentLifecycleStatus);
        Assert.Equal(1, await fresh.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == initialized.PlanStateId && b.EventType == LongHorizonPersistedBlockRetryEventType.Block));
        Assert.Equal(0, await fresh.LongHorizonRollingSessionStates.CountAsync(s => s.Week.PlanStateId == initialized.PlanStateId && s.Week.GlobalWeek >= 5));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentRetry_SameOrDifferentDecision_HasOnePendingTransition(bool differentDecision)
    {
        var blocked = await CreateBlockedAsync();
        var requestA = BuildRetry(blocked, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var requestB = BuildRetry(blocked, differentDecision ? Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") : requestA.RelatedDecisionId!.Value);
        var race = await LongHorizonConcurrentOperationHarness.RunAsync(
            repo => repo.SaveRetryRestorationAsync(requestA), repo => repo.SaveRetryRestorationAsync(requestB));
        AssertOneSafeWinner(race.Left, race.Right);
        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        var plan = await fresh.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == blocked.PlanStateId);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericPending, plan.CurrentLifecycleStatus);
        Assert.Equal(1, await fresh.LongHorizonBlockRetryRecords.CountAsync(r => r.PlanStateId == blocked.PlanStateId && r.EventType == LongHorizonPersistedBlockRetryEventType.RetryRestored));
    }

    [Fact]
    public async Task ActivationVersusBlock_HasOneCoherentCommitOrderedWinner()
    {
        var state = await DriveAsync(21, 2);
        var version = await VersionAsync(state.PlanStateId);
        var race = await LongHorizonConcurrentOperationHarness.RunAsync(
            repo => LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate, repo),
            repo => new LongHorizonRollingBlockPersistenceAdapter(repo).PersistBlockAsync(state.PlanStateId, version, 10, 13,
                LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "race-block", state.Date, true));
        AssertOneSafeWinner(race.Left, race.Right);
        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        var plan = await fresh.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == state.PlanStateId);
        var sessions = await fresh.LongHorizonRollingSessionStates.CountAsync(s => s.Week.PlanStateId == state.PlanStateId && s.Week.GlobalWeek >= 10 && s.Week.GlobalWeek <= 13);
        var blocks = await fresh.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == state.PlanStateId && b.EventType == LongHorizonPersistedBlockRetryEventType.Block);
        Assert.True((plan.CurrentLifecycleStatus == LongHorizonPersistedLifecycleState.NumericActivated && sessions > 0 && blocks == 0)
            || (plan.CurrentLifecycleStatus == LongHorizonPersistedLifecycleState.NumericActivationBlocked && sessions == 0 && blocks == 1));
    }

    [Fact]
    public async Task ActivationVersusRetry_StaleActivationCanNeverBypassBlockedLifecycle_InEitherCommitOrder()
    {
        var initialized = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var mirror = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var mirrorAdvance = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(
            mirror.PlanStateId, mirror.InitialWindow, date, mirror.CatalogRoot, mirror.Candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, mirrorAdvance.Outcome);

        await using var staleDb = LongHorizonPersistenceTestFixture.NewContext();
        var stale = await new LongHorizonRollingStateRepository(staleDb).LoadRestartSnapshotAsync(initialized.PlanStateId);
        var staleActivation = new LongHorizonRollingActivationPersistenceRequest
        {
            PlanStateId = initialized.PlanStateId,
            ExpectedConcurrencyVersion = stale!.ConcurrencyVersion,
            ActivatedWindow = mirrorAdvance.Snapshot!.DarkState.CurrentWindow,
            LifecycleStates = mirrorAdvance.Snapshot.DarkState.LifecycleStates,
            ContextVersion = mirrorAdvance.Snapshot.DarkState.CurrentWindow.ContextVersion,
            IdempotencyKey = $"stale-activation:{initialized.PlanStateId}:5-8",
        };

        await using var blockDb = LongHorizonPersistenceTestFixture.NewContext();
        var blockedResult = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(blockDb)).PersistBlockAsync(
            initialized.PlanStateId, stale.ConcurrencyVersion, 5, 8, LongHorizonReasonCode.SafetyReassessmentRequired,
            "SafetyReviewRequired", "activation-retry-race", date, true);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, blockedResult.Outcome);

        // Activation reaches persistence while still Blocked: lifecycle validation rejects it before any write.
        await using (var blockedActivationDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var rejected = await new LongHorizonRollingStateRepository(blockedActivationDb).SaveActivationSuccessAsync(staleActivation);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.IntegrityViolation, rejected.Outcome);
        }

        var retry = BuildRetry(new BlockedState(initialized.PlanStateId, blockedResult.Snapshot!.ConcurrencyVersion, date), Guid.NewGuid());
        await using (var retryDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var restored = await new LongHorizonRollingStateRepository(retryDb).SaveRetryRestorationAsync(retry);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, restored.Outcome);
        }

        // Retry wins first: the pre-block xmin is still stale and cannot overwrite the restored Pending state.
        await using (var staleAfterRetryDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var rejected = await new LongHorizonRollingStateRepository(staleAfterRetryDb).SaveActivationSuccessAsync(staleActivation);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.ConcurrencyConflict, rejected.Outcome);
        }

        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        var plan = await fresh.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == initialized.PlanStateId);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericPending, plan.CurrentLifecycleStatus);
        Assert.Equal(0, await fresh.LongHorizonRollingSessionStates.CountAsync(s => s.Week.PlanStateId == initialized.PlanStateId && s.Week.GlobalWeek >= 5));
        Assert.Equal(1, await fresh.LongHorizonBlockRetryRecords.CountAsync(r => r.PlanStateId == initialized.PlanStateId && r.EventType == LongHorizonPersistedBlockRetryEventType.Block));
        Assert.Equal(1, await fresh.LongHorizonBlockRetryRecords.CountAsync(r => r.PlanStateId == initialized.PlanStateId && r.EventType == LongHorizonPersistedBlockRetryEventType.RetryRestored));
    }

    [Fact]
    public async Task CrossPlanParallelIdenticalShapes_CommitIndependentlyAndFailureIsIsolated()
    {
        var a = await DriveAsync(21, 2);
        var b = await DriveAsync(21, 2);
        using var gate = new Barrier(2);
        async Task<LongHorizonRollingPersistenceResult> AdvanceAsync(Boundary state)
        {
            gate.SignalAndWait();
            return await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate);
        }
        var results = await Task.WhenAll(Task.Run(() => AdvanceAsync(a)), Task.Run(() => AdvanceAsync(b)));
        Assert.All(results, r => Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, r.Outcome));
        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(1, await fresh.LongHorizonActivationWindowRecords.CountAsync(x => x.PlanStateId == a.PlanStateId && x.StartGlobalWeek == 10));
        Assert.Equal(1, await fresh.LongHorizonActivationWindowRecords.CountAsync(x => x.PlanStateId == b.PlanStateId && x.StartGlobalWeek == 10));
        Assert.NotEqual((await fresh.LongHorizonCoreContextRecords.SingleAsync(c => c.PlanStateId == a.PlanStateId && c.Status == LongHorizonPersistedCoreContextStatus.Active)).Id,
            (await fresh.LongHorizonCoreContextRecords.SingleAsync(c => c.PlanStateId == b.PlanStateId && c.Status == LongHorizonPersistedCoreContextStatus.Active)).Id);
    }

    [Theory]
    [InlineData(26, 2, (int)LongHorizonPersistenceOperation.MixedRunwayCoreActivation)]
    [InlineData(21, 2, (int)LongHorizonPersistenceOperation.CoreOnlyActivation)]
    public async Task ActivationCommitSucceededAcknowledgementLost_FreshReplayDeduplicatesAndNextOperationStillWorks(
        int totalWeeks, int advances, int operationValue)
    {
        var state = await DriveAsync(totalWeeks, advances);
        await using (var failed = LongHorizonPersistenceTestFixture.NewContext())
        {
            var injector = new LongHorizonTestPersistenceFailureInjector(
                (LongHorizonPersistenceOperation)operationValue, LongHorizonPersistenceFailpoint.AfterCommitBeforeAcknowledgement);
            await Assert.ThrowsAsync<LongHorizonInjectedPersistenceFailureException>(() =>
                LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(
                    state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate,
                    new LongHorizonRollingStateRepository(failed, injector)));
        }

        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        var committed = await new LongHorizonRollingStateRepository(fresh).LoadRestartSnapshotAsync(state.PlanStateId);
        Assert.NotNull(committed);
        var record = await fresh.LongHorizonActivationWindowRecords.AsNoTracking().SingleAsync(a => a.PlanStateId == state.PlanStateId
            && a.StartGlobalWeek == committed.DarkState.CurrentWindow.StartGlobalWeek && a.EndGlobalWeek == committed.DarkState.CurrentWindow.EndGlobalWeek);
        var replay = await new LongHorizonRollingStateRepository(fresh).SaveActivationSuccessAsync(new LongHorizonRollingActivationPersistenceRequest
        {
            PlanStateId = state.PlanStateId, ExpectedConcurrencyVersion = 0, ActivatedWindow = committed.DarkState.CurrentWindow,
            LifecycleStates = committed.DarkState.LifecycleStates, ContextVersion = committed.DarkState.CurrentWindow.ContextVersion,
            IdempotencyKey = record.IdempotencyKey,
        });
        Assert.Equal(LongHorizonRollingPersistenceOutcome.IdempotentReplay, replay.Outcome);
        Assert.Equal(1, await fresh.LongHorizonActivationWindowRecords.CountAsync(a => a.IdempotencyKey == record.IdempotencyKey));

        if (committed.DarkState.CurrentWindow.EndGlobalWeek < totalWeeks)
        {
            var next = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(state.PlanStateId, committed.DarkState.CurrentWindow,
                state.Date.AddDays(28), state.CatalogRoot, state.Candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, next.Outcome);
            Assert.True(next.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek > committed.DarkState.CurrentWindow.EndGlobalWeek);
        }
    }

    [Fact]
    public async Task BlockAndRetry_ExactReplayAndAcknowledgementLoss_DoNotDoubleAdvance()
    {
        var initialized = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        await using var initialDb = LongHorizonPersistenceTestFixture.NewContext();
        var initial = await new LongHorizonRollingStateRepository(initialDb).LoadRestartSnapshotAsync(initialized.PlanStateId);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var blockRequest = new LongHorizonRollingBlockPersistenceRequest
        {
            PlanStateId = initialized.PlanStateId, ExpectedConcurrencyVersion = initial!.ConcurrencyVersion,
            BlockedGlobalWeekStart = 5, BlockedGlobalWeekEnd = 8, InternalReasonCode = LongHorizonReasonCode.SafetyReassessmentRequired.ToString(),
            PublicReasonCategory = "SafetyReviewRequired", EvidenceFingerprint = "ack-block", CheckpointDate = date,
            RetryEligible = true, RelatedDecisionId = Guid.NewGuid(), IdempotencyKey = $"block:{initialized.PlanStateId}:5-8:{date:O}",
        };

        await using (var failed = LongHorizonPersistenceTestFixture.NewContext())
        {
            var injector = new LongHorizonTestPersistenceFailureInjector(LongHorizonPersistenceOperation.BlockPersistence, LongHorizonPersistenceFailpoint.AfterCommitBeforeAcknowledgement);
            await Assert.ThrowsAsync<LongHorizonInjectedPersistenceFailureException>(() =>
                new LongHorizonRollingStateRepository(failed, injector).SaveBlockAsync(blockRequest));
        }
        await using var replayBlockDb = LongHorizonPersistenceTestFixture.NewContext();
        var blockReplay = await new LongHorizonRollingStateRepository(replayBlockDb).SaveBlockAsync(blockRequest);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.IdempotentReplay, blockReplay.Outcome);
        var blocked = await new LongHorizonRollingStateRepository(replayBlockDb).LoadRestartSnapshotAsync(initialized.PlanStateId);

        var retryRequest = new LongHorizonRollingRetryPersistenceRequest
        {
            PlanStateId = initialized.PlanStateId, ExpectedConcurrencyVersion = blocked!.ConcurrencyVersion,
            RestoredGlobalWeekStart = 5, RestoredGlobalWeekEnd = 8, RetryCheckpointDate = date.AddDays(1),
            ChangedEvidenceFingerprint = "ack-retry-changed", RelatedDecisionId = Guid.NewGuid(),
            IdempotencyKey = $"retry:{initialized.PlanStateId}:5-8:{date.AddDays(1):O}",
        };
        await using (var failed = LongHorizonPersistenceTestFixture.NewContext())
        {
            var injector = new LongHorizonTestPersistenceFailureInjector(LongHorizonPersistenceOperation.RetryPersistence, LongHorizonPersistenceFailpoint.AfterCommitBeforeAcknowledgement);
            await Assert.ThrowsAsync<LongHorizonInjectedPersistenceFailureException>(() =>
                new LongHorizonRollingStateRepository(failed, injector).SaveRetryRestorationAsync(retryRequest));
        }
        await using var replayRetryDb = LongHorizonPersistenceTestFixture.NewContext();
        var retryReplay = await new LongHorizonRollingStateRepository(replayRetryDb).SaveRetryRestorationAsync(retryRequest);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.IdempotentReplay, retryReplay.Outcome);
        Assert.Equal(1, await replayRetryDb.LongHorizonBlockRetryRecords.CountAsync(r => r.PlanStateId == initialized.PlanStateId && r.EventType == LongHorizonPersistedBlockRetryEventType.Block));
        Assert.Equal(1, await replayRetryDb.LongHorizonBlockRetryRecords.CountAsync(r => r.PlanStateId == initialized.PlanStateId && r.EventType == LongHorizonPersistedBlockRetryEventType.RetryRestored));
    }

    [Fact]
    public async Task ConcurrentTerminalContinuation_HasOneFinalWindowAndPostTerminalContinuationIsNoOp()
    {
        var state = await DriveAsync(25, 5); // current [21,24], next is terminal [25,25]
        var race = await LongHorizonConcurrentOperationHarness.RunAsync(
            repo => LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate, repo),
            repo => LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate, repo));
        AssertOneSafeWinner(race.Left, race.Right);
        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        var terminal = await new LongHorizonRollingStateRepository(fresh).LoadRestartSnapshotAsync(state.PlanStateId);
        Assert.Equal((25, 25), (terminal!.DarkState.CurrentWindow.StartGlobalWeek, terminal.DarkState.CurrentWindow.EndGlobalWeek));
        Assert.DoesNotContain(terminal.DarkState.LifecycleStates.Values, s => s is LongHorizonNumericLifecycleState.NumericPending or LongHorizonNumericLifecycleState.NumericActivationBlocked);
        Assert.Equal(1, await fresh.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == state.PlanStateId && a.StartGlobalWeek == 25));
        AssertNoDuplicateSessions(fresh, state.PlanStateId);
    }

    [Fact]
    public async Task SequentialV2ThenV3_UsesLaterContextAndNeverRewritesV2History()
    {
        var state = await DriveAsync(25, 4);
        await using var db = LongHorizonPersistenceTestFixture.NewContext();
        var snapshot = await new LongHorizonRollingStateRepository(db).LoadRestartSnapshotAsync(state.PlanStateId);
        var v2 = await new LongHorizonFutureCoreRefreshOrchestrator(new LongHorizonRollingStateRepository(db)).RefreshAsync(BuildRefresh(state, snapshot!, "v2"));
        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.Refreshed, v2.Outcome);
        var v2History = await HistoricalFingerprintAsync(db, state.PlanStateId, v2.EffectiveFromGlobalWeek!.Value + 3);

        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        var afterV2 = await new LongHorizonRollingStateRepository(fresh).LoadRestartSnapshotAsync(state.PlanStateId);
        var later = state with { Date = state.Date.AddDays(28), Window = afterV2!.DarkState.CurrentWindow };
        var v3 = await new LongHorizonFutureCoreRefreshOrchestrator(new LongHorizonRollingStateRepository(fresh)).RefreshAsync(BuildRefresh(later, afterV2, "v3-different"));
        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.Refreshed, v3.Outcome);
        Assert.True(v3.NewContextVersion > v2.NewContextVersion);
        Assert.Equal(v2History, await HistoricalFingerprintAsync(fresh, state.PlanStateId, v2.EffectiveFromGlobalWeek.Value + 3));
    }

    private static async Task<Boundary> DriveAsync(int totalWeeks, int advances)
    {
        var initialized = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(totalWeeks);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var window = initialized.InitialWindow;
        for (var index = 0; index < advances; index++)
        {
            var call = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(initialized.PlanStateId, window, date, initialized.CatalogRoot, initialized.Candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call.Outcome);
            window = call.Snapshot!.DarkState.CurrentWindow;
            date = date.AddDays(28);
        }
        return new Boundary(initialized.PlanStateId, initialized.Candidate, initialized.CatalogRoot, date, window);
    }

    private static async Task<BlockedState> CreateBlockedAsync()
    {
        var initialized = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        await using var db = LongHorizonPersistenceTestFixture.NewContext();
        var snapshot = await new LongHorizonRollingStateRepository(db).LoadRestartSnapshotAsync(initialized.PlanStateId);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var result = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(db)).PersistBlockAsync(
            initialized.PlanStateId, snapshot!.ConcurrencyVersion, 5, 8, LongHorizonReasonCode.SafetyReassessmentRequired,
            "SafetyReviewRequired", "race-blocked", date, true);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, result.Outcome);
        return new BlockedState(initialized.PlanStateId, result.Snapshot!.ConcurrencyVersion, date);
    }

    private static LongHorizonRollingRetryPersistenceRequest BuildRetry(BlockedState state, Guid decisionId) => new()
    {
        PlanStateId = state.PlanStateId, ExpectedConcurrencyVersion = state.Version, RestoredGlobalWeekStart = 5,
        RestoredGlobalWeekEnd = 8, RetryCheckpointDate = state.Date.AddDays(1), ChangedEvidenceFingerprint = "changed-race",
        RelatedDecisionId = decisionId, IdempotencyKey = $"retry:{state.PlanStateId}:5-8:{state.Date.AddDays(1):O}",
    };

    private static LongHorizonFutureCoreRefreshRequest BuildRefresh(Boundary state, LongHorizonRollingRestartSnapshot snapshot, string evidenceSalt)
    {
        var rows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(state.Window, state.PlanStateId).ToList();
        if (evidenceSalt != "A") rows[0].TrainingDay.ActualDistanceKm += 0.5;
        return new LongHorizonFutureCoreRefreshRequest
        {
            PlanStateId = state.PlanStateId, ExpectedAggregateVersion = snapshot.ConcurrencyVersion, RequestedAsOfDate = state.Date,
            TrainingDayEvidence = rows, CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday, SafetyState = LongHorizonSafetyState.Clear,
            PlanStartDate = LongHorizonFullLifecycleTestFixture.StartDate,
            RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(25 * 7), CatalogRootPath = state.CatalogRoot,
        };
    }

    private static async Task<uint> VersionAsync(Guid planId)
    {
        await using var db = LongHorizonPersistenceTestFixture.NewContext();
        return await db.LongHorizonRollingPlanStates.Where(p => p.Id == planId).Select(p => EF.Property<uint>(p, "xmin")).SingleAsync();
    }

    private static async Task<string> HistoricalFingerprintAsync(AppDbContext db, Guid planId, int throughWeek) => string.Join('|',
        await db.LongHorizonRollingWeekStates.AsNoTracking().Where(w => w.PlanStateId == planId && w.GlobalWeek <= throughWeek)
            .OrderBy(w => w.GlobalWeek).Select(w => $"{w.GlobalWeek}:{w.LifecycleState}:{w.WeeklyVolumeKm}:{w.LongRunKm}:{w.ActivationContextVersionSequence}").ToListAsync());

    private static void AssertOneSafeWinner<T>(LongHorizonConcurrentOperationHarness.Attempt<T> left, LongHorizonConcurrentOperationHarness.Attempt<T> right)
        where T : class
    {
        var outcomes = new[] { Outcome(left.Result), Outcome(right.Result) };
        Assert.Equal(1, outcomes.Count(o => o == LongHorizonRollingPersistenceOutcome.Success));
        var loser = outcomes[0] == LongHorizonRollingPersistenceOutcome.Success ? right : left;
        Assert.True(Outcome(loser.Result) is LongHorizonRollingPersistenceOutcome.ConcurrencyConflict or LongHorizonRollingPersistenceOutcome.IdempotentReplay
            || IsSafeUniqueViolation(loser.Exception), $"Unsafe loser: {loser.Exception ?? (object?)Outcome(loser.Result)}");
    }

    private static LongHorizonRollingPersistenceOutcome? Outcome<T>(T? result) => result switch
    {
        LongHorizonRollingPersistenceResult persistence => persistence.Outcome,
        _ => null,
    };

    private static bool IsSafeUniqueViolation(Exception? exception) => exception is DbUpdateException { InnerException: PostgresException postgres }
        && postgres.SqlState == PostgresErrorCodes.UniqueViolation;

    private static void AssertNoDuplicateSessions(AppDbContext db, Guid planId)
    {
        var duplicates = db.LongHorizonRollingSessionStates.Where(s => s.Week.PlanStateId == planId)
            .AsEnumerable().GroupBy(s => new { s.WeekStateId, s.SessionOrdinal }).Count(g => g.Count() > 1);
        Assert.Equal(0, duplicates);
    }

    private sealed record Boundary(Guid PlanStateId, PlanCatalogCandidateSummary Candidate, string CatalogRoot, DateOnly Date, RollingNumericActivationWindow Window);
    private sealed record BlockedState(Guid PlanStateId, uint Version, DateOnly Date);

    private sealed class ActivationKeyOverrideRepository(ILongHorizonRollingStateRepository inner, string suffix) : ILongHorizonRollingStateRepository
    {
        public Task<LongHorizonRollingRestartSnapshot> InitializeStructuralStateAsync(LongHorizonRollingInitializationRequest request, CancellationToken cancellationToken = default) => inner.InitializeStructuralStateAsync(request, cancellationToken);
        public Task<LongHorizonRollingRestartSnapshot?> LoadRestartSnapshotAsync(Guid planStateId, CancellationToken cancellationToken = default) => inner.LoadRestartSnapshotAsync(planStateId, cancellationToken);
        public Task<LongHorizonRollingPersistenceResult> SaveActivationSuccessAsync(LongHorizonRollingActivationPersistenceRequest request, CancellationToken cancellationToken = default)
            => inner.SaveActivationSuccessAsync(request with { IdempotencyKey = request.IdempotencyKey + suffix }, cancellationToken);
        public Task<LongHorizonRollingPersistenceResult> SaveBlockAsync(LongHorizonRollingBlockPersistenceRequest request, CancellationToken cancellationToken = default) => inner.SaveBlockAsync(request, cancellationToken);
        public Task<LongHorizonRollingPersistenceResult> SaveRetryRestorationAsync(LongHorizonRollingRetryPersistenceRequest request, CancellationToken cancellationToken = default) => inner.SaveRetryRestorationAsync(request, cancellationToken);
        public Task<LongHorizonActiveCoreContextSnapshot?> GetActiveCoreContextAsync(Guid planStateId, CancellationToken cancellationToken = default) => inner.GetActiveCoreContextAsync(planStateId, cancellationToken);
        public Task SetCoreContextEvidenceFingerprintAsync(Guid coreContextId, string evidenceFingerprint, CancellationToken cancellationToken = default) => inner.SetCoreContextEvidenceFingerprintAsync(coreContextId, evidenceFingerprint, cancellationToken);
    }
}
