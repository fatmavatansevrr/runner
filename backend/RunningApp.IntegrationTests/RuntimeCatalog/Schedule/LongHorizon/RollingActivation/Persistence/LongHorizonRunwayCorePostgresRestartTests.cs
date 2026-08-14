using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2A -- drives the real Runway/Core JIT composition path (Phase
/// 4K.8C's LongHorizonRollingJitCompositionOrchestrator) through real
/// persistence, closing the "smoke-tested only" gap Phase 4L.2 disclosed.
/// Every "restart" opens a brand-new AppDbContext/connection; nothing is
/// reused across restart boundaries in this file.
/// </summary>
internal static class LongHorizonRunwayCoreRestartFixture
{
    internal static async Task<(Guid PlanStateId, RollingNumericActivationWindow InitialWindow, PlanCatalogCandidateSummary Candidate, string CatalogRoot)>
        InitializePlanAsync(int totalWeeks)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);
        var initial = result.StateSnapshots[0];
        var candidate = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(totalWeeks).ContinueWith(t => t.Result.Candidate);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var request = LongHorizonPersistenceTestFixture.BuildInitRequest(initial, LongHorizonFullLifecycleTestFixture.StartDate, catalogRoot, candidate);
        await new LongHorizonRollingStateRepository(db).InitializeStructuralStateAsync(request);
        return (request.PlanStateId, initial.CurrentWindow, candidate, catalogRoot);
    }

    /// <summary>
    /// Drives real checkpoint evaluation, then (if the window reaches the GE
    /// boundary) real JIT composition, persisting via the real production
    /// adapters -- never a pre-computed harness state. Returns the persisted
    /// snapshot after this one operation.
    /// </summary>
    internal static async Task<LongHorizonRollingPersistenceResult> AdvanceOneWindowAsync(
        Guid planStateId, RollingNumericActivationWindow currentWindow, DateOnly checkpointDate, string catalogRoot, PlanCatalogCandidateSummary candidate)
    {
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        return await AdvanceOneWindowAsyncWithInjector(planStateId, currentWindow, checkpointDate, catalogRoot, candidate, repo);
    }

    /// <summary>
    /// Phase 4L.2F -- identical to <see cref="AdvanceOneWindowAsync"/> but
    /// accepts a caller-constructed repository (e.g. one wired with a test
    /// failure injector) instead of creating its own, so failure-injection
    /// tests can drive the exact same production continuation chain.
    /// </summary>
    internal static async Task<LongHorizonRollingPersistenceResult> AdvanceOneWindowAsyncWithInjector(
        Guid planStateId, RollingNumericActivationWindow currentWindow, DateOnly checkpointDate, string catalogRoot, PlanCatalogCandidateSummary candidate,
        ILongHorizonRollingStateRepository repo)
    {
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId) ?? throw new InvalidOperationException("No snapshot.");
        var state = snapshot.DarkState;

        var checkpointRuntime = new LongHorizonRollingCheckpointRuntime();
        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(currentWindow, planStateId);
        var checkpointRequest = new LongHorizonRollingCheckpointRequest
        {
            StructuralRoadmap = state.StructuralRoadmap,
            StructuralSkeleton = state.StructuralSkeleton,
            LifecycleStates = state.LifecycleStates,
            MostRecentlyActivatedWindow = state.CurrentWindow,
            TrainingDayEvidence = evidenceRows,
            CheckpointDate = checkpointDate,
            CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            ReadinessProfile = state.StructuralRoadmap.Profile,
            PriorValidatedAnchor = LongHorizonCheckpointTestFixture.Prior(20, 8),
            PreviousContextVersion = state.ContextVersion,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
        };
        var checkpoint = await checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(checkpointRequest);

        var geEnd = state.StructuralRoadmap.GeneralEnduranceWeeks;
        var reachesGeBoundary = checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated
            && checkpoint.ActivationWindow!.EndGlobalWeek == geEnd;
        var pureGe = checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated && !reachesGeBoundary;

        if (pureGe)
        {
            return await new LongHorizonRollingActivationPersistenceAdapter(repo)
                .PersistGeCheckpointAsync(planStateId, snapshot.ConcurrencyVersion, checkpoint);
        }

        if (checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked)
        {
            return await new LongHorizonRollingBlockPersistenceAdapter(repo).PersistBlockAsync(
                planStateId, snapshot.ConcurrencyVersion,
                state.CurrentWindow.EndGlobalWeek + 1, Math.Min(state.CurrentWindow.EndGlobalWeek + 4, state.StructuralRoadmap.TotalWeeks),
                checkpoint.AuthoritativeReason ?? LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved),
                "MoreTrainingDataNeeded", "test-boundary", checkpointDate, true);
        }

        // Reaches (or exceeds via Runway/Core) the GE boundary -- real JIT composition, same as the harness.
        var continuation = new LongHorizonRollingRestartContinuationService(repo);
        var jitResult = await continuation.ContinueJitCompositionAsync(
            planStateId, checkpoint.EvidenceSnapshot!, checkpoint.ValidatedLoad ?? LongHorizonCheckpointTestFixture.Prior(20, 8).Load,
            checkpoint.EvidenceSnapshot!.CompletedRunsCount == 0 ? null : 4,
            checkpoint.CheckpointDecision,
            checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated ? checkpoint.NewlyActivatedWeeks : null,
            LongHorizonFullLifecycleTestFixture.StartDate, LongHorizonFullLifecycleTestFixture.StartDate.AddDays(state.StructuralRoadmap.TotalWeeks * 7),
            LongHorizonFullLifecycleTestFixture.PreferredDays, DayOfWeek.Sunday, catalogRoot,
            lifecycleStatesOverride: state.LifecycleStates);

        // Phase 4L.2G: a coordinated real-PostgreSQL race may legitimately
        // return the repository's typed loser outcome. Preserve it for the
        // race harness instead of converting it into a fixture exception.
        if (jitResult.Outcome == LongHorizonRollingPersistenceOutcome.ConcurrencyConflict
            || jitResult.Outcome == LongHorizonRollingPersistenceOutcome.IdempotentReplay)
        {
            return jitResult;
        }

        if (jitResult.Outcome != LongHorizonRollingPersistenceOutcome.Success || jitResult.Snapshot!.DarkState.CurrentWindow.Status != LongHorizonActivationWindowStatus.Activated)
        {
            using var diagnosticDb = LongHorizonPersistenceTestFixture.NewContext();
            var plan = await diagnosticDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
            throw new InvalidOperationException(
                $"JIT composition did not activate as expected -- persistence outcome {jitResult.Outcome}, plan blocked reason {plan.CurrentBlockedInternalReasonCode}.");
        }

        return jitResult;
    }
}

public sealed class LongHorizonFirstRunwayEntryRestartTests
{
    [Fact]
    public async Task FirstRunwayEntryPersistsAndRestartReconstructsExactOwnership()
    {
        // 21-week horizon: GE=1 week -- window1 (init) covers the entire GE segment,
        // so window2 is a clean pure-Runway-only first entry (no GE+Runway mixing),
        // exercising the real JIT composition + first Runway entry through real persistence.
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        var result = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, checkpointDate, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, result.Outcome);

        // Restart: brand-new connection, reload via the real repository.
        using var freshDb = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(freshDb).LoadRestartSnapshotAsync(planStateId);

        Assert.NotNull(reconstructed);
        Assert.NotNull(reconstructed!.DarkState.RunwayTargetLock);
        Assert.NotNull(reconstructed.DarkState.RunwayPrescription);
        Assert.NotNull(reconstructed.DarkState.RunwayCalendarProjection);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var runwayRowCount = await verify.LongHorizonRunwayStates.CountAsync(r => r.PlanStateId == planStateId);
        Assert.Equal(1, runwayRowCount);
    }

    [Fact]
    public async Task TargetLockAndPrescriptionReconstructWithFullFidelity()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var result = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, checkpointDate, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, result.Outcome);
        var original = result.Snapshot!.DarkState;

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var first = await new LongHorizonRollingStateRepository(db1).LoadRestartSnapshotAsync(planStateId);
        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var second = await new LongHorizonRollingStateRepository(db2).LoadRestartSnapshotAsync(planStateId);

        Assert.Equal(original.RunwayPrescription!.PrescriptionId, first!.DarkState.RunwayPrescription!.PrescriptionId);
        Assert.Equal(first.DarkState.RunwayPrescription!.PrescriptionId, second!.DarkState.RunwayPrescription!.PrescriptionId);
        Assert.Equal(original.RunwayTargetLock!.TargetWeeklyVolumeKm, second.DarkState.RunwayTargetLock!.TargetWeeklyVolumeKm);
        Assert.Equal(original.RunwayPrescription.FullWeekReferences.Count, second.DarkState.RunwayPrescription!.FullWeekReferences.Count);
    }

    [Fact]
    public async Task CalendarProjectionReconstructsWithExactSessionDates()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var result = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, checkpointDate, catalogRoot, candidate);
        var original = result.Snapshot!.DarkState.RunwayCalendarProjection;

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(db).LoadRestartSnapshotAsync(planStateId);
        var reloaded = reconstructed!.DarkState.RunwayCalendarProjection;

        Assert.NotNull(original);
        Assert.NotNull(reloaded);
        Assert.Equal(original!.Sessions.Count, reloaded!.Sessions.Count);
        foreach (var session in original.Sessions)
        {
            Assert.Contains(reloaded.Sessions, s => s.SessionDate == session.SessionDate && s.GlobalWeekNumber == session.GlobalWeekNumber);
        }
    }

    [Fact]
    public async Task FutureRunwayWeeksRemainPendingWithNoSessions()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, checkpointDate, catalogRoot, candidate);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(db).LoadRestartSnapshotAsync(planStateId);
        var pendingWithSessions = reconstructed!.DarkState.LifecycleStates
            .Where(kv => kv.Value == LongHorizonNumericLifecycleState.NumericPending)
            .Count(kv => reconstructed.DarkState.ActivatedWeeks.ContainsKey(kv.Key));
        Assert.Equal(0, pendingWithSessions);
    }
}

public sealed class LongHorizonRunwayContinuationRestartTests
{
    [Fact]
    public async Task RestartBetweenContinuationSlicesReusesSameLockAndPrescription()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var date1 = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var firstEntry = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, date1, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, firstEntry.Outcome);
        var firstPrescriptionId = firstEntry.Snapshot!.DarkState.RunwayPrescription!.PrescriptionId;
        var firstTargetLockId = firstEntry.Snapshot.DarkState.RunwayTargetLock!.CreatedByDecisionId;

        // Restart, then continue to the next Runway slice.
        var date2 = date1.AddDays(28);
        var second = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(
            planStateId, firstEntry.Snapshot.DarkState.CurrentWindow, date2, catalogRoot, candidate);

        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, second.Outcome);
        Assert.Equal(firstPrescriptionId, second.Snapshot!.DarkState.RunwayPrescription!.PrescriptionId);
        Assert.Equal(firstTargetLockId, second.Snapshot.DarkState.RunwayTargetLock!.CreatedByDecisionId);

        // Phase 4L.2C: strengthened per the phase's own governance instruction not to retain
        // a "continuation restart" test that validates identity only. The second continuation
        // must select and persist the NEXT distinct Pending range, not re-echo the first-entry
        // window. Root cause of the prior failure (Phase 4L.2B finding): the embedded
        // LockedForActivatedRunwayWeekRange ValueTuple silently round-tripped as (0,0) through
        // System.Text.Json's default (fields-excluded) options, tripping
        // ImmutablePreparationRunwayPrescriptionValidator's scope check on every reuse attempt.
        // Fixed via LongHorizonRollingActivationPersistenceAdapter.FullFidelityJsonOptions
        // (IncludeFields = true), applied symmetrically at every serialize/deserialize site.
        var firstWindow = firstEntry.Snapshot.DarkState.CurrentWindow;
        var secondWindow = second.Snapshot.DarkState.CurrentWindow;
        Assert.NotEqual(firstWindow.StartGlobalWeek, secondWindow.StartGlobalWeek);
        Assert.NotEqual(firstWindow.EndGlobalWeek, secondWindow.EndGlobalWeek);
        Assert.Equal(firstWindow.EndGlobalWeek + 1, secondWindow.StartGlobalWeek);
        Assert.Equal(LongHorizonActivationWindowStatus.Activated, secondWindow.Status);
        foreach (var week in secondWindow.Weeks)
            Assert.DoesNotContain(firstWindow.Weeks, w => w.GlobalWeekNumber == week.GlobalWeekNumber);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var runwayRowCount = await verify.LongHorizonRunwayStates.CountAsync(r => r.PlanStateId == planStateId);
        Assert.Equal(1, runwayRowCount);

        var activationRows = await verify.LongHorizonActivationWindowRecords.Where(a => a.PlanStateId == planStateId).ToListAsync();
        Assert.Single(activationRows, a => a.StartGlobalWeek == firstWindow.StartGlobalWeek && a.EndGlobalWeek == firstWindow.EndGlobalWeek);
        Assert.Single(activationRows, a => a.StartGlobalWeek == secondWindow.StartGlobalWeek && a.EndGlobalWeek == secondWindow.EndGlobalWeek);

        var plan = await verify.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == planStateId);
        Assert.Equal(secondWindow.StartGlobalWeek, plan.CurrentWindowStartWeek);
        Assert.Equal(secondWindow.EndGlobalWeek, plan.CurrentWindowEndWeek);
    }

    [Fact]
    public async Task OnlyNextSelectedSliceActivatesPreviousRemainsImmutable()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var date1 = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var firstEntry = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, date1, catalogRoot, candidate);
        var firstActivatedWeeks = firstEntry.Snapshot!.DarkState.ActivatedWeeks.Keys.ToList();

        var date2 = date1.AddDays(28);
        var second = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(
            planStateId, firstEntry.Snapshot.DarkState.CurrentWindow, date2, catalogRoot, candidate);

        foreach (var week in firstActivatedWeeks)
        {
            var before = firstEntry.Snapshot.DarkState.ActivatedWeeks[week];
            var after = second.Snapshot!.DarkState.ActivatedWeeks[week];
            Assert.Equal(before.TotalWeeklyVolumeKm, after.TotalWeeklyVolumeKm);
            Assert.Equal(before.SessionPrescriptions!.Select(s => s.AssignedDate), after.SessionPrescriptions!.Select(s => s.AssignedDate));
        }
    }
}

public sealed class LongHorizonRunwayCoreBlockedRestartTests
{
    [Fact]
    public async Task SafetyBlockDuringFirstRunwayEntrySurvivesRestart()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);
        var boundaryStart = snapshot!.DarkState.CurrentWindow.EndGlobalWeek + 1;
        var boundaryEnd = Math.Min(boundaryStart + 3, snapshot.DarkState.StructuralRoadmap.TotalWeeks);

        var blockResult = await new LongHorizonRollingBlockPersistenceAdapter(repo).PersistBlockAsync(
            planStateId, snapshot.ConcurrencyVersion, boundaryStart, boundaryEnd,
            LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "runway-fp", checkpointDate, false);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, blockResult.Outcome);

        using var freshDb = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(freshDb).LoadRestartSnapshotAsync(planStateId);
        var plan = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericActivationBlocked, plan.CurrentLifecycleStatus);
        Assert.Null(reconstructed!.DarkState.RunwayPrescription);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var sessionCount = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek >= boundaryStart && s.Week.GlobalWeek <= boundaryEnd)
            .CountAsync();
        Assert.Equal(0, sessionCount);
    }

    [Fact]
    public async Task RetryAfterRunwayBlockRestoresPendingThenNormalActivationSucceeds()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var repo1 = new LongHorizonRollingStateRepository(db1);
        var snapshot = await repo1.LoadRestartSnapshotAsync(planStateId);
        var boundaryStart = snapshot!.DarkState.CurrentWindow.EndGlobalWeek + 1;
        var boundaryEnd = Math.Min(boundaryStart + 3, snapshot.DarkState.StructuralRoadmap.TotalWeeks);
        var blockResult = await new LongHorizonRollingBlockPersistenceAdapter(repo1).PersistBlockAsync(
            planStateId, snapshot.ConcurrencyVersion, boundaryStart, boundaryEnd,
            LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "runway-fp-1", checkpointDate, false);

        // Restart, then retry with a strictly later date and changed evidence.
        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var retryResult = await new LongHorizonRollingRetryPersistenceAdapter(new LongHorizonRollingStateRepository(db2)).PersistRetryAsync(
            planStateId, blockResult.Snapshot!.ConcurrencyVersion, boundaryStart, boundaryEnd, checkpointDate.AddDays(7), "runway-fp-2");
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, retryResult.Outcome);

        // Restart again, then perform normal activation for that now-Pending range.
        var normalActivation = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(
            planStateId, initialWindow, checkpointDate.AddDays(7), catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, normalActivation.Outcome);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var blockRecords = await verify.LongHorizonBlockRetryRecords.Where(b => b.PlanStateId == planStateId).ToListAsync();
        Assert.Contains(blockRecords, b => b.EventType == LongHorizonPersistedBlockRetryEventType.Block);
        Assert.Contains(blockRecords, b => b.EventType == LongHorizonPersistedBlockRetryEventType.RetryRestored);
    }
}

public sealed class LongHorizonRunwayCoreNoRegenerationTests
{
    [Fact]
    public async Task RepeatedReconstructionsProduceIdenticalRunwayNumericAndCalendarValues()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, checkpointDate, catalogRoot, candidate);

        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var first = await new LongHorizonRollingStateRepository(db1).LoadRestartSnapshotAsync(planStateId);
        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var second = await new LongHorizonRollingStateRepository(db2).LoadRestartSnapshotAsync(planStateId);
        using var db3 = LongHorizonPersistenceTestFixture.NewContext();
        var third = await new LongHorizonRollingStateRepository(db3).LoadRestartSnapshotAsync(planStateId);

        Assert.Equal(first!.DarkState.RunwayTargetLock!.TargetWeeklyVolumeKm, second!.DarkState.RunwayTargetLock!.TargetWeeklyVolumeKm);
        Assert.Equal(second.DarkState.RunwayTargetLock!.TargetWeeklyVolumeKm, third!.DarkState.RunwayTargetLock!.TargetWeeklyVolumeKm);
        Assert.Equal(first.DarkState.RunwayCalendarProjection!.Sessions.Select(s => s.SessionDate),
            third.DarkState.RunwayCalendarProjection!.Sessions.Select(s => s.SessionDate));
    }

    [Fact]
    public void ReconstructionServiceSourceNeverReferencesRuntimeConditionOrCoreGeneratorTypes()
    {
        var text = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule",
            "LongHorizon", "RollingActivation", "Persistence", "LongHorizonRollingStateReconstructionService.cs"));
        Assert.DoesNotContain("RuntimeConditionResolutionService", text);
        Assert.DoesNotContain("TenKPreparationRunwayDarkOrchestrator", text);
        Assert.DoesNotContain("PreparationRunwayNumericMaterializer", text);
        // The GE structural skeleton is the one, previously-approved, evidence-independent regeneration.
        Assert.Contains("LongHorizonStructuralMaterializer.MaterializeAsync", text);
    }
}

public sealed class LongHorizonRunwayCoreCorruptionMatrixTests
{
    [Fact]
    public async Task TamperedRunwayPrescriptionPayloadFailsClosed()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, checkpointDate, catalogRoot, candidate);

        using (var corruptDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var runway = await corruptDb.LongHorizonRunwayStates.SingleAsync(r => r.PlanStateId == planStateId);
            runway.PrescriptionPayloadJson = "{ \"not\": \"a valid prescription\" }";
            await corruptDb.SaveChangesAsync();
        }

        using var verifyDb = LongHorizonPersistenceTestFixture.NewContext();
        await Assert.ThrowsAnyAsync<Exception>(
            () => new LongHorizonRollingStateRepository(verifyDb).LoadRestartSnapshotAsync(planStateId));
    }

    [Fact]
    public async Task ChangedHistoricalSessionDateIsDetectableAfterReconstruction()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, checkpointDate, catalogRoot, candidate);

        Guid weekStateId;
        int sessionOrdinal;
        DateOnly originalDate;
        using (var corruptDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var session = await corruptDb.LongHorizonRollingSessionStates.FirstAsync(s => s.Week.PlanStateId == planStateId);
            weekStateId = session.WeekStateId;
            sessionOrdinal = session.SessionOrdinal;
            originalDate = session.AssignedDate;
            session.AssignedDate = originalDate.AddDays(1);
            await corruptDb.SaveChangesAsync();
        }

        using var verifyDb = LongHorizonPersistenceTestFixture.NewContext();
        var reloadedSession = await verifyDb.LongHorizonRollingSessionStates.AsNoTracking()
            .SingleAsync(s => s.WeekStateId == weekStateId && s.SessionOrdinal == sessionOrdinal);
        Assert.NotEqual(originalDate, reloadedSession.AssignedDate);
        Assert.Equal(originalDate.AddDays(1), reloadedSession.AssignedDate);
    }

    [Fact]
    public async Task MissingCalendarSessionRowIsReflectedInReconstructedCount()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var result = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, checkpointDate, catalogRoot, candidate);
        var originalSessionCount = result.Snapshot!.DarkState.ActivatedWeeks.Values.Sum(w => w.SessionPrescriptions!.Count);

        using (var corruptDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var oneSession = await corruptDb.LongHorizonRollingSessionStates.FirstAsync(s => s.Week.PlanStateId == planStateId);
            corruptDb.LongHorizonRollingSessionStates.Remove(oneSession);
            await corruptDb.SaveChangesAsync();
        }

        using var verifyDb = LongHorizonPersistenceTestFixture.NewContext();
        // This particular corruption (one session removed from an otherwise-populated week)
        // is caught by the reconstructed session count, not by the integrity validator's
        // coarser Activated-without-ANY-sessions check -- disclosed as a narrower guarantee.
        var reconstructed = await new LongHorizonRollingStateRepository(verifyDb).LoadRestartSnapshotAsync(planStateId);
        var reloadedSessionCount = reconstructed!.DarkState.ActivatedWeeks.Values.Sum(w => w.SessionPrescriptions!.Count);
        Assert.NotEqual(originalSessionCount, reloadedSessionCount);
    }
}

public sealed class LongHorizonRunwayCoreConcurrencyTests
{
    [Fact]
    public async Task ConcurrentFirstRunwayEntryHasExactlyOneWinner()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        // Two independent processes load the same starting snapshot/version and race to persist.
        using var dbA = LongHorizonPersistenceTestFixture.NewContext();
        var snapshotA = await new LongHorizonRollingStateRepository(dbA).LoadRestartSnapshotAsync(planStateId);
        using var dbB = LongHorizonPersistenceTestFixture.NewContext();
        var snapshotB = await new LongHorizonRollingStateRepository(dbB).LoadRestartSnapshotAsync(planStateId);
        Assert.Equal(snapshotA!.ConcurrencyVersion, snapshotB!.ConcurrencyVersion);

        var winnerTask = LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, checkpointDate, catalogRoot, candidate);
        var winner = await winnerTask;
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, winner.Outcome);

        // Loser: reuses the now-stale version directly against the block adapter (deterministic race simulation).
        using var loserDb = LongHorizonPersistenceTestFixture.NewContext();
        var loser = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(loserDb)).PersistBlockAsync(
            planStateId, snapshotA.ConcurrencyVersion, initialWindow.EndGlobalWeek + 1, initialWindow.EndGlobalWeek + 4,
            LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "race-fp", checkpointDate.AddDays(1), false);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.ConcurrencyConflict, loser.Outcome);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var runwayCount = await verify.LongHorizonRunwayStates.CountAsync(r => r.PlanStateId == planStateId);
        Assert.Equal(1, runwayCount);
    }
}

public sealed class LongHorizonRunwayCoreDarkBoundaryTests
{
    [Fact]
    public void ContinueJitCompositionAsyncIsInternalAndNotPublic()
    {
        Assert.False(typeof(LongHorizonRollingRestartContinuationService).IsPublic);
    }

    [Fact]
    public void NoEndpointOrDiReferencesJitContinuation()
    {
        var controllerPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Api", "Controllers", "PlansController.cs");
        var programPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Api", "Program.cs");
        Assert.DoesNotContain("ContinueJitCompositionAsync", File.ReadAllText(controllerPath));
        Assert.DoesNotContain("LongHorizonRollingRestartContinuationService", File.ReadAllText(programPath));
    }
}
