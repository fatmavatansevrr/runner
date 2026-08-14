using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2 -- every test here opens a FRESH <see cref="AppDbContext"/>
/// against the real configured PostgreSQL database per operation, exactly
/// as production does per-request, so that "restart" is proven from durable
/// DB state (a new context/connection reading back what a prior context
/// wrote), never from an in-memory fixture or a still-tracked entity graph.
/// </summary>
internal static class LongHorizonPersistenceTestFixture
{
    internal const string ConnectionString = "Host=localhost;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres";

    internal static AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    internal static LongHorizonRollingInitializationRequest BuildInitRequest(LongHorizonFullDarkLifecycleState state, DateOnly startDate, string catalogRoot, PlanCatalogCandidateSummary candidate) => new()
    {
        PlanStateId = Guid.NewGuid(),
        StructuralRoadmap = state.StructuralRoadmap,
        PlanStartDate = startDate,
        PreferredDays = LongHorizonFullLifecycleTestFixture.PreferredDays,
        LongRunDay = DayOfWeek.Sunday,
        InitialWindow = state.CurrentWindow,
        LifecycleStates = state.LifecycleStates,
        ActivatedWeeks = state.ActivatedWeeks,
        ContextVersion = state.ContextVersion,
        CatalogRootPath = catalogRoot,
        Candidate = candidate,
    };

    internal static IReadOnlyList<LongHorizonTrainingDayEvidenceRow> BuildCompletedEvidenceRows(RollingNumericActivationWindow window, Guid seed)
    {
        var sessions = window.Weeks.SelectMany(week => week.SessionPrescriptions!.Select(session => (week, session))).ToList();
        return sessions.Select((pair, index) => new LongHorizonTrainingDayEvidenceRow(pair.week.GlobalWeekNumber, new TrainingDay
        {
            Id = Guid.NewGuid(),
            Date = pair.session.AssignedDate!.Value.ToDateTime(TimeOnly.MinValue),
            DayType = pair.session.SessionRole.Contains("LONG", StringComparison.OrdinalIgnoreCase) ? TrainingDayType.LongRun : TrainingDayType.Easy,
            Status = TrainingDayStatus.Completed,
            PlannedDistanceKm = pair.session.DistanceKm,
            ActualDistanceKm = pair.session.DistanceKm,
            ActualDurationMin = 30,
            IsLongRun = pair.session.SessionRole.Contains("LONG", StringComparison.OrdinalIgnoreCase),
            CompletedAt = pair.session.AssignedDate.Value.ToDateTime(TimeOnly.MinValue).AddHours(1),
        })).ToList();
    }
}

public sealed class LongHorizonInitialPersistenceTests
{
    [Theory]
    [InlineData(21)]
    [InlineData(52)]
    public async Task InitializationPersistsExactStructuralWeekCount(int totalWeeks)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(totalWeeks).ContinueWith(t => t.Result.Candidate);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(
            initial, LongHorizonFullLifecycleTestFixture.StartDate,
            Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog"), candidate);

        var snapshot = await repo.InitializeStructuralStateAsync(request);

        Assert.Equal(totalWeeks, snapshot.DarkState.StructuralRoadmap.TotalWeeks);
        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var weekCount = await verify.LongHorizonRollingWeekStates.CountAsync(w => w.PlanStateId == request.PlanStateId);
        Assert.Equal(totalWeeks, weekCount);
    }

    [Fact]
    public async Task OnlyFirstSelectedWindowBecomesExecutable()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(29);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(29).ContinueWith(t => t.Result.Candidate);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(
            initial, LongHorizonFullLifecycleTestFixture.StartDate, Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog"), candidate);

        var snapshot = await repo.InitializeStructuralStateAsync(request);

        var activatedCount = snapshot.DarkState.LifecycleStates.Values.Count(s => s == LongHorizonNumericLifecycleState.NumericActivated);
        Assert.True(activatedCount is > 0 and <= 4);
    }

    [Fact]
    public async Task FuturePendingWeeksHaveNoSessionRows()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(29);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(29).ContinueWith(t => t.Result.Candidate);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(
            initial, LongHorizonFullLifecycleTestFixture.StartDate, Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog"), candidate);
        await repo.InitializeStructuralStateAsync(request);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var pendingWeeksWithSessions = await verify.LongHorizonRollingWeekStates
            .Where(w => w.PlanStateId == request.PlanStateId && w.LifecycleState == LongHorizonPersistedLifecycleState.NumericPending)
            .SelectMany(w => w.Sessions)
            .CountAsync();
        Assert.Equal(0, pendingWeeksWithSessions);
    }

    [Fact]
    public async Task InitializationReplayIsIdempotent()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(21);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(21).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var repo1 = new LongHorizonRollingStateRepository(db1);
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
        await repo1.InitializeStructuralStateAsync(request);

        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var repo2 = new LongHorizonRollingStateRepository(db2);
        await repo2.InitializeStructuralStateAsync(request);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var weekCount = await verify.LongHorizonRollingWeekStates.CountAsync(w => w.PlanStateId == request.PlanStateId);
        Assert.Equal(21, weekCount);
    }
}

public sealed class LongHorizonRestartReconstructionTests
{
    private static async Task<(Guid PlanStateId, LongHorizonRollingRestartSnapshot Snapshot)> InitializeAsync(int totalWeeks)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(totalWeeks).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
        var snapshot = await repo.InitializeStructuralStateAsync(request);
        return (request.PlanStateId, snapshot);
    }

    [Fact]
    public async Task FullRestartSnapshotReconstructsFromFreshConnection()
    {
        var (planStateId, initialSnapshot) = await InitializeAsync(29);

        using var freshDb = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(freshDb);
        var reconstructed = await repo.LoadRestartSnapshotAsync(planStateId);

        Assert.NotNull(reconstructed);
        Assert.Equal(29, reconstructed!.DarkState.StructuralRoadmap.TotalWeeks);
        Assert.Equal(initialSnapshot.DarkState.LifecycleStates.Count, reconstructed.DarkState.LifecycleStates.Count);
    }

    [Fact]
    public async Task NextPendingBoundaryDerivesCorrectly()
    {
        var (planStateId, _) = await InitializeAsync(29);
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(db).LoadRestartSnapshotAsync(planStateId);

        var firstPending = reconstructed!.DarkState.LifecycleStates
            .Where(kv => kv.Value == LongHorizonNumericLifecycleState.NumericPending || kv.Value == LongHorizonNumericLifecycleState.StructurallyPlanned)
            .Min(kv => kv.Key);
        Assert.Equal(reconstructed.DarkState.CurrentWindow.EndGlobalWeek + 1, firstPending);
    }

    [Fact]
    public async Task ActivatedNumericWeeksReconstructWithExactValues()
    {
        var (planStateId, initialSnapshot) = await InitializeAsync(21);
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(db).LoadRestartSnapshotAsync(planStateId);

        foreach (var (week, original) in initialSnapshot.DarkState.ActivatedWeeks)
        {
            var reloaded = reconstructed!.DarkState.ActivatedWeeks[week];
            Assert.Equal(original.TotalWeeklyVolumeKm, reloaded.TotalWeeklyVolumeKm);
            Assert.Equal(original.LongRunKm, reloaded.LongRunKm);
            Assert.Equal(original.SessionPrescriptions!.Count, reloaded.SessionPrescriptions!.Count);
            foreach (var session in original.SessionPrescriptions!)
            {
                Assert.Contains(reloaded.SessionPrescriptions!, s => s.AssignedDate == session.AssignedDate && s.DistanceKm == session.DistanceKm);
            }
        }
    }

    [Fact]
    public async Task AggregateVersionReconstructs()
    {
        var (planStateId, initialSnapshot) = await InitializeAsync(21);
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(db).LoadRestartSnapshotAsync(planStateId);
        Assert.Equal(initialSnapshot.ConcurrencyVersion, reconstructed!.ConcurrencyVersion);
    }

    [Fact]
    public async Task NoNumericOrCalendarRegenerationOccursOnReconstruction()
    {
        var (planStateId, initialSnapshot) = await InitializeAsync(21);
        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var first = await new LongHorizonRollingStateRepository(db1).LoadRestartSnapshotAsync(planStateId);
        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var second = await new LongHorizonRollingStateRepository(db2).LoadRestartSnapshotAsync(planStateId);

        foreach (var week in initialSnapshot.DarkState.ActivatedWeeks.Keys)
        {
            Assert.Equal(first!.DarkState.ActivatedWeeks[week].TotalWeeklyVolumeKm, second!.DarkState.ActivatedWeeks[week].TotalWeeklyVolumeKm);
            Assert.Equal(
                first.DarkState.ActivatedWeeks[week].SessionPrescriptions!.Select(s => s.AssignedDate),
                second.DarkState.ActivatedWeeks[week].SessionPrescriptions!.Select(s => s.AssignedDate));
        }
    }
}

public sealed class LongHorizonRestartContinuationTests
{
    [Fact]
    public async Task RestartAfterInitialGeContinuesToNextWindow()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(29);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(29).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        Guid planStateId;
        using (var initDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
            await new LongHorizonRollingStateRepository(initDb).InitializeStructuralStateAsync(request);
            planStateId = request.PlanStateId;
        }

        // Simulate process restart: brand-new AppDbContext, brand-new repository/continuation service instance.
        using var restartDb = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(restartDb);
        var continuation = new LongHorizonRollingRestartContinuationService(repo);

        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(initial.CurrentWindow, planStateId);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        var persistResult = await continuation.ContinueGeCheckpointAsync(
            planStateId, evidenceRows, checkpointDate, LongHorizonFullLifecycleTestFixture.PreferredDays,
            DayOfWeek.Sunday, LongHorizonSafetyState.Clear, LongHorizonCheckpointTestFixture.Prior(20, 8));

        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, persistResult.Outcome);
        Assert.True(persistResult.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek > initial.CurrentWindow.EndGlobalWeek);
    }

    [Fact]
    public async Task RestartAfterRepeatedGeContinuesAcrossTwoSeparateRestarts()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(29);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(29).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        Guid planStateId;
        using (var initDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
            await new LongHorizonRollingStateRepository(initDb).InitializeStructuralStateAsync(request);
            planStateId = request.PlanStateId;
        }

        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        LongHorizonRollingRestartSnapshot? afterFirst;
        using (var db1 = LongHorizonPersistenceTestFixture.NewContext())
        {
            var continuation1 = new LongHorizonRollingRestartContinuationService(new LongHorizonRollingStateRepository(db1));
            var evidence1 = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(initial.CurrentWindow, planStateId);
            var r1 = await continuation1.ContinueGeCheckpointAsync(
                planStateId, evidence1, checkpointDate, LongHorizonFullLifecycleTestFixture.PreferredDays,
                DayOfWeek.Sunday, LongHorizonSafetyState.Clear, LongHorizonCheckpointTestFixture.Prior(20, 8));
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, r1.Outcome);
            afterFirst = r1.Snapshot;
        }

        // Second independent restart.
        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var continuation2 = new LongHorizonRollingRestartContinuationService(new LongHorizonRollingStateRepository(db2));
        var evidence2 = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(afterFirst!.DarkState.CurrentWindow, planStateId);
        var checkpointDate2 = checkpointDate.AddDays(28);
        var r2 = await continuation2.ContinueGeCheckpointAsync(
            planStateId, evidence2, checkpointDate2, LongHorizonFullLifecycleTestFixture.PreferredDays,
            DayOfWeek.Sunday, LongHorizonSafetyState.Clear, null);

        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, r2.Outcome);
        Assert.True(r2.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek > afterFirst.DarkState.CurrentWindow.EndGlobalWeek);
    }
}

public sealed class LongHorizonBlockRetryPersistenceTests
{
    private static async Task<(Guid PlanStateId, LongHorizonRollingRestartSnapshot Snapshot)> InitializeAsync(int totalWeeks)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(totalWeeks).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
        var snapshot = await repo.InitializeStructuralStateAsync(request);
        return (request.PlanStateId, snapshot);
    }

    [Fact]
    public async Task BlockPersistsAndRestartRetainsBlockedState()
    {
        var (planStateId, snapshot) = await InitializeAsync(21);
        var blockStart = snapshot.DarkState.CurrentWindow.EndGlobalWeek + 1;
        var blockEnd = Math.Min(blockStart + 3, snapshot.DarkState.StructuralRoadmap.TotalWeeks);

        using (var db = LongHorizonPersistenceTestFixture.NewContext())
        {
            var adapter = new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(db));
            var result = await adapter.PersistBlockAsync(
                planStateId, snapshot.ConcurrencyVersion, blockStart, blockEnd,
                LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "fingerprint-1",
                LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30), retryEligible: false);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, result.Outcome);
        }

        using var freshDb = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(freshDb).LoadRestartSnapshotAsync(planStateId);
        var plan = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericActivationBlocked, plan.CurrentLifecycleStatus);
        Assert.Equal("SafetyReviewRequired", plan.CurrentBlockedPublicReasonCategory);
        Assert.NotNull(reconstructed);
    }

    [Fact]
    public async Task BlockCreatesNoExecutableSessions()
    {
        var (planStateId, snapshot) = await InitializeAsync(21);
        var blockStart = snapshot.DarkState.CurrentWindow.EndGlobalWeek + 1;
        var blockEnd = Math.Min(blockStart + 3, snapshot.DarkState.StructuralRoadmap.TotalWeeks);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var adapter = new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(db));
        await adapter.PersistBlockAsync(
            planStateId, snapshot.ConcurrencyVersion, blockStart, blockEnd,
            LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "fp", LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30), false);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var sessionCount = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek >= blockStart && s.Week.GlobalWeek <= blockEnd)
            .CountAsync();
        Assert.Equal(0, sessionCount);
    }

    [Fact]
    public async Task RetrySameDateRejects()
    {
        var (planStateId, snapshot) = await InitializeAsync(21);
        var blockStart = snapshot.DarkState.CurrentWindow.EndGlobalWeek + 1;
        var blockEnd = Math.Min(blockStart + 3, snapshot.DarkState.StructuralRoadmap.TotalWeeks);
        var blockDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30);

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var blockResult = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(db1)).PersistBlockAsync(
            planStateId, snapshot.ConcurrencyVersion, blockStart, blockEnd,
            LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved), "MoreTrainingDataNeeded", "fp-a", blockDate, true);

        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var retryResult = await new LongHorizonRollingRetryPersistenceAdapter(new LongHorizonRollingStateRepository(db2)).PersistRetryAsync(
            planStateId, blockResult.Snapshot!.ConcurrencyVersion, blockStart, blockEnd, blockDate, "fp-b");

        Assert.Equal(LongHorizonRollingPersistenceOutcome.IntegrityViolation, retryResult.Outcome);
        Assert.Contains("strictly later", retryResult.FailureReason);
    }

    [Fact]
    public async Task RetryUnchangedEvidenceRejects()
    {
        var (planStateId, snapshot) = await InitializeAsync(21);
        var blockStart = snapshot.DarkState.CurrentWindow.EndGlobalWeek + 1;
        var blockEnd = Math.Min(blockStart + 3, snapshot.DarkState.StructuralRoadmap.TotalWeeks);
        var blockDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30);

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var blockResult = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(db1)).PersistBlockAsync(
            planStateId, snapshot.ConcurrencyVersion, blockStart, blockEnd,
            LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved), "MoreTrainingDataNeeded", "same-fingerprint", blockDate, true);

        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var retryResult = await new LongHorizonRollingRetryPersistenceAdapter(new LongHorizonRollingStateRepository(db2)).PersistRetryAsync(
            planStateId, blockResult.Snapshot!.ConcurrencyVersion, blockStart, blockEnd, blockDate.AddDays(1), "same-fingerprint");

        Assert.Equal(LongHorizonRollingPersistenceOutcome.IntegrityViolation, retryResult.Outcome);
        Assert.Contains("unchanged", retryResult.FailureReason);
    }

    [Fact]
    public async Task ValidLaterRetrySucceedsAndRestoresPending()
    {
        var (planStateId, snapshot) = await InitializeAsync(21);
        var blockStart = snapshot.DarkState.CurrentWindow.EndGlobalWeek + 1;
        var blockEnd = Math.Min(blockStart + 3, snapshot.DarkState.StructuralRoadmap.TotalWeeks);
        var blockDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30);

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var blockResult = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(db1)).PersistBlockAsync(
            planStateId, snapshot.ConcurrencyVersion, blockStart, blockEnd,
            LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved), "MoreTrainingDataNeeded", "fp-before", blockDate, true);

        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var retryResult = await new LongHorizonRollingRetryPersistenceAdapter(new LongHorizonRollingStateRepository(db2)).PersistRetryAsync(
            planStateId, blockResult.Snapshot!.ConcurrencyVersion, blockStart, blockEnd, blockDate.AddDays(7), "fp-after");

        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, retryResult.Outcome);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var plan = await verify.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericPending, plan.CurrentLifecycleStatus);
        var restoredWeek = await verify.LongHorizonRollingWeekStates.AsNoTracking().SingleAsync(w => w.PlanStateId == planStateId && w.GlobalWeek == blockStart);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericPending, restoredWeek.LifecycleState);
    }

    [Fact]
    public async Task BlockHistoryRemainsImmutableAfterRetry()
    {
        var (planStateId, snapshot) = await InitializeAsync(21);
        var blockStart = snapshot.DarkState.CurrentWindow.EndGlobalWeek + 1;
        var blockEnd = Math.Min(blockStart + 3, snapshot.DarkState.StructuralRoadmap.TotalWeeks);
        var blockDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30);

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var blockResult = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(db1)).PersistBlockAsync(
            planStateId, snapshot.ConcurrencyVersion, blockStart, blockEnd,
            LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved), "MoreTrainingDataNeeded", "fp-x", blockDate, true);

        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        await new LongHorizonRollingRetryPersistenceAdapter(new LongHorizonRollingStateRepository(db2)).PersistRetryAsync(
            planStateId, blockResult.Snapshot!.ConcurrencyVersion, blockStart, blockEnd, blockDate.AddDays(7), "fp-y");

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var blockRecord = await verify.LongHorizonBlockRetryRecords.AsNoTracking()
            .SingleAsync(b => b.PlanStateId == planStateId && b.EventType == LongHorizonPersistedBlockRetryEventType.Block);
        Assert.Equal("fp-x", blockRecord.EvidenceFingerprint);
    }
}

public sealed class LongHorizonConcurrencyAndIdempotencyTests
{
    [Fact]
    public async Task StaleConcurrencyVersionRejectsBlock()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(21);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(21).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        Guid planStateId;
        uint staleVersion;
        using (var db = LongHorizonPersistenceTestFixture.NewContext())
        {
            var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
            var snap = await new LongHorizonRollingStateRepository(db).InitializeStructuralStateAsync(request);
            planStateId = request.PlanStateId;
            staleVersion = snap.ConcurrencyVersion;
        }

        var blockStart = initial.CurrentWindow.EndGlobalWeek + 1;
        var blockEnd = Math.Min(blockStart + 3, initial.StructuralRoadmap.TotalWeeks);

        // Winner: first block using the correct (still-current) version.
        using (var winnerDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var winner = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(winnerDb)).PersistBlockAsync(
                planStateId, staleVersion, blockStart, blockEnd,
                LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved), "MoreTrainingDataNeeded", "fp1", LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30), true);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, winner.Outcome);
        }

        // Loser: reuses the now-stale version (a second concurrent writer that read before the winner committed).
        // Uses a distinct CheckpointDate so this exercises the concurrency check specifically, not the idempotency short-circuit.
        using var loserDb = LongHorizonPersistenceTestFixture.NewContext();
        var loser = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(loserDb)).PersistBlockAsync(
            planStateId, staleVersion, blockStart, blockEnd,
            LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "fp2", LongHorizonFullLifecycleTestFixture.StartDate.AddDays(31), false);

        Assert.Equal(LongHorizonRollingPersistenceOutcome.ConcurrencyConflict, loser.Outcome);
    }

    [Fact]
    public async Task DuplicateIdempotencyKeyReturnsPriorResultSafely()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(21);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(21).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
        var snapshot = await new LongHorizonRollingStateRepository(db1).InitializeStructuralStateAsync(request);

        var blockStart = initial.CurrentWindow.EndGlobalWeek + 1;
        var blockEnd = Math.Min(blockStart + 3, initial.StructuralRoadmap.TotalWeeks);
        var blockDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30);

        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var adapter = new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(db2));
        var first = await adapter.PersistBlockAsync(request.PlanStateId, snapshot.ConcurrencyVersion, blockStart, blockEnd,
            LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "fp", blockDate, false);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, first.Outcome);

        using var db3 = LongHorizonPersistenceTestFixture.NewContext();
        var replay = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(db3)).PersistBlockAsync(
            request.PlanStateId, snapshot.ConcurrencyVersion, blockStart, blockEnd,
            LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "fp", blockDate, false);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.IdempotentReplay, replay.Outcome);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var blockRecordCount = await verify.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == request.PlanStateId);
        Assert.Equal(1, blockRecordCount);
    }
}

public sealed class LongHorizonCorruptionAndIntegrityTests
{
    [Fact]
    public async Task DirectBlockedToActivatedIsRejected()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(21);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(21).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
        var snapshot = await new LongHorizonRollingStateRepository(db1).InitializeStructuralStateAsync(request);

        // No repository method exists to move Blocked directly to Activated --
        // the only path is SaveRetryRestorationAsync (Blocked -> Pending) followed
        // by a separate SaveActivationSuccessAsync call. Prove the retry-guard
        // rejects a retry attempt when there is no prior block record at all.
        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var retry = await new LongHorizonRollingRetryPersistenceAdapter(new LongHorizonRollingStateRepository(db2)).PersistRetryAsync(
            request.PlanStateId, snapshot.ConcurrencyVersion, initial.CurrentWindow.EndGlobalWeek + 1,
            Math.Min(initial.CurrentWindow.EndGlobalWeek + 4, initial.StructuralRoadmap.TotalWeeks),
            LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30), "fp");

        Assert.Equal(LongHorizonRollingPersistenceOutcome.IntegrityViolation, retry.Outcome);
        Assert.Contains("not currently Blocked", retry.FailureReason);
    }

    [Fact]
    public async Task MissingWeekRowFailsReconstruction()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(21);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(21).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
        await new LongHorizonRollingStateRepository(db1).InitializeStructuralStateAsync(request);

        using (var corruptDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var lastWeek = await corruptDb.LongHorizonRollingWeekStates.SingleAsync(w => w.PlanStateId == request.PlanStateId && w.GlobalWeek == 21);
            corruptDb.LongHorizonRollingWeekStates.Remove(lastWeek);
            await corruptDb.SaveChangesAsync();
        }

        using var verifyDb = LongHorizonPersistenceTestFixture.NewContext();
        await Assert.ThrowsAsync<LongHorizonRollingPersistenceCorruptionException>(
            () => new LongHorizonRollingStateRepository(verifyDb).LoadRestartSnapshotAsync(request.PlanStateId));
    }

    [Fact]
    public async Task ActivatedWeekWithoutSessionsFailsReconstruction()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(21);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(21).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
        await new LongHorizonRollingStateRepository(db1).InitializeStructuralStateAsync(request);

        using (var corruptDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var activatedWeek = await corruptDb.LongHorizonRollingWeekStates.Include(w => w.Sessions)
                .FirstAsync(w => w.PlanStateId == request.PlanStateId && w.LifecycleState == LongHorizonPersistedLifecycleState.NumericActivated);
            corruptDb.LongHorizonRollingSessionStates.RemoveRange(activatedWeek.Sessions);
            await corruptDb.SaveChangesAsync();
        }

        using var verifyDb = LongHorizonPersistenceTestFixture.NewContext();
        await Assert.ThrowsAsync<LongHorizonRollingPersistenceCorruptionException>(
            () => new LongHorizonRollingStateRepository(verifyDb).LoadRestartSnapshotAsync(request.PlanStateId));
    }

    [Fact]
    public async Task UnknownPersistenceVersionFailsClosed()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(21);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(21).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
        await new LongHorizonRollingStateRepository(db1).InitializeStructuralStateAsync(request);

        using (var corruptDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var plan = await corruptDb.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == request.PlanStateId);
            plan.PersistenceContractVersion = 999;
            await corruptDb.SaveChangesAsync();
        }

        using var verifyDb = LongHorizonPersistenceTestFixture.NewContext();
        await Assert.ThrowsAsync<LongHorizonRollingPersistenceCorruptionException>(
            () => new LongHorizonRollingStateRepository(verifyDb).LoadRestartSnapshotAsync(request.PlanStateId));
    }

    [Fact]
    public async Task NoAutoRepairOccursAfterCorruptionDetection()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(21);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(21).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
        await new LongHorizonRollingStateRepository(db1).InitializeStructuralStateAsync(request);

        using (var corruptDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var lastWeek = await corruptDb.LongHorizonRollingWeekStates.SingleAsync(w => w.PlanStateId == request.PlanStateId && w.GlobalWeek == 21);
            corruptDb.LongHorizonRollingWeekStates.Remove(lastWeek);
            await corruptDb.SaveChangesAsync();
        }

        // Attempt twice -- no repair happens between attempts (still 20 rows, still throws both times).
        using var attempt1 = LongHorizonPersistenceTestFixture.NewContext();
        await Assert.ThrowsAsync<LongHorizonRollingPersistenceCorruptionException>(() => new LongHorizonRollingStateRepository(attempt1).LoadRestartSnapshotAsync(request.PlanStateId));

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var weekCount = await verify.LongHorizonRollingWeekStates.CountAsync(w => w.PlanStateId == request.PlanStateId);
        Assert.Equal(20, weekCount);
    }
}

public sealed class LongHorizonPersistencePublicLeakageAndWiringTests
{
    [Fact]
    public void NoEntityIsInPublicDtoNamespace()
    {
        var entityTypes = new[]
        {
            typeof(LongHorizonRollingPlanState), typeof(LongHorizonRollingWeekState), typeof(LongHorizonRollingSessionState),
            typeof(LongHorizonActivationWindowRecord), typeof(LongHorizonCheckpointRecord), typeof(LongHorizonRunwayState),
            typeof(LongHorizonCoreContextRecord), typeof(LongHorizonBlockRetryRecord),
        };
        Assert.All(entityTypes, t => Assert.DoesNotContain("DTOs", t.Namespace));
    }

    [Fact]
    public void RepositoryAndAdaptersAreInternal()
    {
        Assert.False(typeof(LongHorizonRollingStateRepository).IsPublic);
        Assert.False(typeof(LongHorizonRollingActivationPersistenceAdapter).IsPublic);
        Assert.False(typeof(LongHorizonRollingBlockPersistenceAdapter).IsPublic);
        Assert.False(typeof(LongHorizonRollingRetryPersistenceAdapter).IsPublic);
        Assert.False(typeof(LongHorizonRollingRestartContinuationService).IsPublic);
    }

    [Fact]
    public void NoEndpointReferencesThePersistenceSubsystem()
    {
        var controllerPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Api", "Controllers", "PlansController.cs");
        var text = File.ReadAllText(controllerPath);
        Assert.DoesNotContain(nameof(LongHorizonRollingStateRepository), text);
        Assert.DoesNotContain(nameof(LongHorizonRollingRestartContinuationService), text);
    }

    [Fact]
    public void NoPublicDiRegistrationExists()
    {
        var programPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Api", "Program.cs");
        var text = File.ReadAllText(programPath);
        Assert.DoesNotContain(nameof(LongHorizonRollingStateRepository), text);
    }

    [Fact]
    public void ExistingTrainingPlanTableIsNeverWrittenByThisSubsystem()
    {
        var repoText = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule",
            "LongHorizon", "RollingActivation", "Persistence", "LongHorizonRollingStateRepository.cs"));
        Assert.DoesNotContain("_db.TrainingPlans.Add", repoText);
        Assert.DoesNotContain("_db.TrainingDays.Add", repoText);
    }
}
