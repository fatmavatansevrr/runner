using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2F -- transactional failure-injection and rollback proof,
/// against real PostgreSQL. Every authoritative Long-Horizon persistence
/// method (SaveActivationSuccessAsync, SaveBlockAsync,
/// SaveRetryRestorationAsync) commits via exactly one EF SaveChangesAsync
/// call (confirmed by direct inspection, Part 1) -- so every pre-commit
/// failpoint shares the identical underlying atomicity guarantee (EF's
/// Unit-of-Work batches all staged entity changes into one PostgreSQL
/// transaction at that single call). This file exercises a representative
/// subset of failpoints across each of the five required operation groups,
/// not the full 14-stage x 5-operation (70+) matrix the phase prompt
/// suggests -- disclosed honestly, not narrowed silently.
/// </summary>
public sealed class LongHorizonPersistenceFailureInjectionTests
{
    // ── Part 24 Infrastructure ───────────────────────────────────────────

    [Fact]
    public void NoOpInjector_NeverThrows()
    {
        var injector = NoOpLongHorizonPersistenceFailureInjector.Instance;
        var ex = Record.Exception(() => injector.MaybeThrow(LongHorizonPersistenceOperation.MixedRunwayCoreActivation, LongHorizonPersistenceFailpoint.BeforeCommit));
        Assert.Null(ex);
    }

    [Fact]
    public void ConfiguredInjector_ThrowsOnlyAtExactOperationAndStage()
    {
        var injector = new LongHorizonTestPersistenceFailureInjector(LongHorizonPersistenceOperation.CoreOnlyActivation, LongHorizonPersistenceFailpoint.AfterContextInsert);

        // Wrong operation -- no throw.
        var wrongOp = Record.Exception(() => injector.MaybeThrow(LongHorizonPersistenceOperation.BlockPersistence, LongHorizonPersistenceFailpoint.AfterContextInsert));
        Assert.Null(wrongOp);

        // Wrong stage -- no throw.
        var wrongStage = Record.Exception(() => injector.MaybeThrow(LongHorizonPersistenceOperation.CoreOnlyActivation, LongHorizonPersistenceFailpoint.BeforeCommit));
        Assert.Null(wrongStage);

        // Exact match -- throws exactly the configured exception type.
        var ex = Assert.Throws<LongHorizonInjectedPersistenceFailureException>(
            () => injector.MaybeThrow(LongHorizonPersistenceOperation.CoreOnlyActivation, LongHorizonPersistenceFailpoint.AfterContextInsert));
        Assert.Equal(LongHorizonPersistenceOperation.CoreOnlyActivation, ex.Operation);
        Assert.Equal(LongHorizonPersistenceFailpoint.AfterContextInsert, ex.Stage);
    }

    [Fact]
    public void ConfiguredInjector_FiresExactlyOnce()
    {
        var injector = new LongHorizonTestPersistenceFailureInjector(LongHorizonPersistenceOperation.BlockPersistence, LongHorizonPersistenceFailpoint.BeforeCommit);
        Assert.Throws<LongHorizonInjectedPersistenceFailureException>(
            () => injector.MaybeThrow(LongHorizonPersistenceOperation.BlockPersistence, LongHorizonPersistenceFailpoint.BeforeCommit));
        // Second call at the same stage does NOT throw again (disarmed).
        var ex = Record.Exception(() => injector.MaybeThrow(LongHorizonPersistenceOperation.BlockPersistence, LongHorizonPersistenceFailpoint.BeforeCommit));
        Assert.Null(ex);
    }

    [Fact]
    public void FailpointTypes_AreInternalOnly()
    {
        Assert.False(typeof(LongHorizonPersistenceOperation).IsPublic);
        Assert.False(typeof(LongHorizonPersistenceFailpoint).IsPublic);
        Assert.False(typeof(ILongHorizonPersistenceFailureInjector).IsPublic);
        Assert.False(typeof(LongHorizonInjectedPersistenceFailureException).IsPublic);
    }

    [Fact]
    public void NoEndpointOrDiRegistrationReferencesFailureInjection()
    {
        var controllerPath = Path.Combine(RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Api", "Controllers", "PlansController.cs");
        var programPath = Path.Combine(RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Api", "Program.cs");
        Assert.DoesNotContain("LongHorizonPersistenceFailpoint", File.ReadAllText(controllerPath));
        Assert.DoesNotContain("PersistenceFailureInjector", File.ReadAllText(programPath));
    }

    // ── Mixed Runway->Core activation rollback (Part 6) ──────────────────

    private static async Task<(Guid PlanStateId, string CatalogRoot, RunningApp.Application.RuntimeCatalog.PlanCatalogCandidateSummary Candidate, DateOnly Date, RollingNumericActivationWindow Window)>
        DriveToPreMixedBoundaryAsync()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(26); // GE=6, Runway=7-14, Core=15-26 -> [5,8]->[9,12]->[13,16] mixed (2+2)
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var window = initialWindow;
        for (var i = 0; i < 2; i++)
        {
            var call = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call.Outcome);
            window = call.Snapshot!.DarkState.CurrentWindow;
            date = date.AddDays(28);
        }
        return (planStateId, catalogRoot, candidate, date, window);
    }

    [Theory]
    [InlineData((int)LongHorizonPersistenceFailpoint.AfterVersionValidation)]
    [InlineData((int)LongHorizonPersistenceFailpoint.AfterContextInsert)]
    [InlineData((int)LongHorizonPersistenceFailpoint.AfterActivationWindowInsert)]
    [InlineData((int)LongHorizonPersistenceFailpoint.BeforeCommit)]
    public async Task MixedActivation_FailureAtStage_RollsBackToExactPriorStateThenReplaySucceedsOnce(int stageValue)
    {
        var stage = (LongHorizonPersistenceFailpoint)stageValue;
        var (planStateId, catalogRoot, candidate, date, window) = await DriveToPreMixedBoundaryAsync();

        using var dbPre = LongHorizonPersistenceTestFixture.NewContext();
        var preSnapshot = await new LongHorizonRollingStateRepository(dbPre).LoadRestartSnapshotAsync(planStateId);
        var preWindow = preSnapshot!.DarkState.CurrentWindow;
        using var verifyPre = LongHorizonPersistenceTestFixture.NewContext();
        var preActivationCount = await verifyPre.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == planStateId);
        var preCoreContextCount = await verifyPre.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == planStateId);
        var plan0 = await verifyPre.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);

        // Attempt the mixed boundary activation with an injected failure -- fresh context, never reused.
        using (var dbFail = LongHorizonPersistenceTestFixture.NewContext())
        {
            var injector = new LongHorizonTestPersistenceFailureInjector(LongHorizonPersistenceOperation.MixedRunwayCoreActivation, stage);
            var failingRepo = new LongHorizonRollingStateRepository(dbFail, injector);
            var continuation = new LongHorizonRollingRestartContinuationService(failingRepo);
            var checkpointRuntime = new LongHorizonRollingCheckpointRuntime();
            var snapshot = await failingRepo.LoadRestartSnapshotAsync(planStateId);
            var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(window, planStateId);
            var checkpointRequest = new LongHorizonRollingCheckpointRequest
            {
                StructuralRoadmap = snapshot!.DarkState.StructuralRoadmap,
                StructuralSkeleton = snapshot.DarkState.StructuralSkeleton,
                LifecycleStates = snapshot.DarkState.LifecycleStates,
                MostRecentlyActivatedWindow = snapshot.DarkState.CurrentWindow,
                TrainingDayEvidence = evidenceRows,
                CheckpointDate = date,
                CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
                LongRunDay = DayOfWeek.Sunday,
                SafetyState = LongHorizonSafetyState.Clear,
                ReadinessProfile = snapshot.DarkState.StructuralRoadmap.Profile,
                PriorValidatedAnchor = LongHorizonCheckpointTestFixture.Prior(20, 8),
                PreviousContextVersion = snapshot.DarkState.ContextVersion,
                GoalType = RunningApp.Domain.Enums.GoalType.Race,
                GoalDistance = RunningApp.Domain.Enums.GoalDistance.TenK,
                Level = RunningApp.Domain.Enums.RunningBackground.Intermediate,
                DaysPerWeek = 4,
            };
            var checkpoint = await checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(checkpointRequest);

            var thrown = await Record.ExceptionAsync(() => continuation.ContinueJitCompositionAsync(
                planStateId, checkpoint.EvidenceSnapshot!, checkpoint.ValidatedLoad!, null, null, null,
                LongHorizonFullLifecycleTestFixture.StartDate, LongHorizonFullLifecycleTestFixture.StartDate.AddDays(26 * 7),
                LongHorizonFullLifecycleTestFixture.PreferredDays, DayOfWeek.Sunday, catalogRoot));

            Assert.IsType<LongHorizonInjectedPersistenceFailureException>(thrown);
        }

        // Fresh context after the failure -- prove exact prior state.
        using var dbPost = LongHorizonPersistenceTestFixture.NewContext();
        var postSnapshot = await new LongHorizonRollingStateRepository(dbPost).LoadRestartSnapshotAsync(planStateId);
        Assert.Equal(preWindow.StartGlobalWeek, postSnapshot!.DarkState.CurrentWindow.StartGlobalWeek);
        Assert.Equal(preWindow.EndGlobalWeek, postSnapshot.DarkState.CurrentWindow.EndGlobalWeek);

        using var verifyPost = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(preActivationCount, await verifyPost.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == planStateId));
        Assert.Equal(preCoreContextCount, await verifyPost.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == planStateId));
        var plan1 = await verifyPost.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
        Assert.Equal(plan0.ActiveContextVersionSequence, plan1.ActiveContextVersionSequence);
        Assert.Equal(plan0.CurrentWindowEndWeek, plan1.CurrentWindowEndWeek);

        // No week beyond the pre-boundary was flipped to Activated.
        for (var week = preWindow.EndGlobalWeek + 1; week <= preWindow.EndGlobalWeek + 4; week++)
        {
            Assert.Equal(RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.LongHorizonNumericLifecycleState.NumericPending,
                postSnapshot.DarkState.LifecycleStates[week]);
        }

        // Replay (no injector) succeeds exactly once.
        var replay = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, preWindow, date, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, replay.Outcome);

        using var verifyReplay = LongHorizonPersistenceTestFixture.NewContext();
        var replayCount = await verifyReplay.LongHorizonActivationWindowRecords.CountAsync(
            a => a.PlanStateId == planStateId && a.StartGlobalWeek == replay.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek);
        Assert.Equal(1, replayCount);
    }

    // ── Core-only activation rollback (Part 7) ───────────────────────────

    [Fact]
    public async Task CoreOnlyActivation_FailureBeforeCommit_RollsBackThenReplaySucceedsOnce()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21); // GE=1, Runway=2-9, Core=10-21
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var window = initialWindow;
        for (var i = 0; i < 2; i++) // [2,5] -> [6,9] (pure Runway; next call is pure Core-only [10,13])
        {
            var call = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call.Outcome);
            window = call.Snapshot!.DarkState.CurrentWindow;
            date = date.AddDays(28);
        }

        using var dbPre = LongHorizonPersistenceTestFixture.NewContext();
        var preSnapshot = await new LongHorizonRollingStateRepository(dbPre).LoadRestartSnapshotAsync(planStateId);
        using var verifyPre = LongHorizonPersistenceTestFixture.NewContext();
        var preCount = await verifyPre.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == planStateId);

        using (var dbFail = LongHorizonPersistenceTestFixture.NewContext())
        {
            var injector = new LongHorizonTestPersistenceFailureInjector(LongHorizonPersistenceOperation.CoreOnlyActivation, LongHorizonPersistenceFailpoint.BeforeCommit);
            var thrown = await Record.ExceptionAsync(() =>
                LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(planStateId, window, date, catalogRoot, candidate, new LongHorizonRollingStateRepository(dbFail, injector)));
            Assert.IsType<LongHorizonInjectedPersistenceFailureException>(thrown);
        }

        using var dbPost = LongHorizonPersistenceTestFixture.NewContext();
        var postSnapshot = await new LongHorizonRollingStateRepository(dbPost).LoadRestartSnapshotAsync(planStateId);
        Assert.Equal(preSnapshot!.DarkState.CurrentWindow.EndGlobalWeek, postSnapshot!.DarkState.CurrentWindow.EndGlobalWeek);

        using var verifyPost = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(preCount, await verifyPost.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == planStateId));

        var replay = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, replay.Outcome);
        Assert.Equal((10, 13), (replay.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek, replay.Snapshot.DarkState.CurrentWindow.EndGlobalWeek));
    }

    // ── Future-only Core refresh rollback (Part 8) ───────────────────────

    [Fact]
    public async Task FutureCoreRefresh_FailureBeforeCommit_V1RemainsAuthoritativeThenReplayCreatesOneV2()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var window = initialWindow;
        for (var i = 0; i < 4; i++) // -> [17,20] Core-only
        {
            var call = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call.Outcome);
            window = call.Snapshot!.DarkState.CurrentWindow;
            date = date.AddDays(28);
        }
        Assert.Equal((17, 20), (window.StartGlobalWeek, window.EndGlobalWeek));

        using var dbPre = LongHorizonPersistenceTestFixture.NewContext();
        var repoPre = new LongHorizonRollingStateRepository(dbPre);
        var snapshotPre = await repoPre.LoadRestartSnapshotAsync(planStateId);
        var v1Before = await repoPre.GetActiveCoreContextAsync(planStateId);
        using var verifyPre = LongHorizonPersistenceTestFixture.NewContext();
        var preContextCount = await verifyPre.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == planStateId);

        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(window, planStateId);
        LongHorizonFutureCoreRefreshRequest BuildRequest() => new()
        {
            PlanStateId = planStateId,
            ExpectedAggregateVersion = snapshotPre!.ConcurrencyVersion,
            RequestedAsOfDate = date,
            TrainingDayEvidence = evidenceRows,
            CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            PlanStartDate = LongHorizonFullLifecycleTestFixture.StartDate,
            RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(25 * 7),
            CatalogRootPath = catalogRoot,
        };

        using (var dbFail = LongHorizonPersistenceTestFixture.NewContext())
        {
            var injector = new LongHorizonTestPersistenceFailureInjector(LongHorizonPersistenceOperation.CoreOnlyActivation, LongHorizonPersistenceFailpoint.BeforeCommit);
            var failingOrchestrator = new LongHorizonFutureCoreRefreshOrchestrator(new LongHorizonRollingStateRepository(dbFail, injector));
            var thrown = await Record.ExceptionAsync(() => failingOrchestrator.RefreshAsync(BuildRequest()));
            Assert.IsType<LongHorizonInjectedPersistenceFailureException>(thrown);
        }

        // Fresh context: V1 remains Active and authoritative, no orphan V2.
        using var dbPost = LongHorizonPersistenceTestFixture.NewContext();
        var repoPost = new LongHorizonRollingStateRepository(dbPost);
        var v1After = await repoPost.GetActiveCoreContextAsync(planStateId);
        Assert.Equal(v1Before!.CoreContextId, v1After!.CoreContextId);
        Assert.Equal(v1Before.ContextVersionSequence, v1After.ContextVersionSequence);

        using var verifyPost = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(preContextCount, await verifyPost.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == planStateId));
        var v1Row = await verifyPost.LongHorizonCoreContextRecords.SingleAsync(c => c.Id == v1Before.CoreContextId);
        Assert.Equal(RunningApp.Domain.Enums.LongHorizonPersistedCoreContextStatus.Active, v1Row.Status);
        Assert.Null(v1Row.SupersededByContextId);

        // Replay without injector: creates exactly one V2.
        var replayOrchestrator = new LongHorizonFutureCoreRefreshOrchestrator(repoPost);
        var replay = await replayOrchestrator.RefreshAsync(BuildRequest());
        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.Refreshed, replay.Outcome);

        using var verifyReplay = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(preContextCount + 1, await verifyReplay.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == planStateId));
    }

    // ── Block persistence rollback (Part 9) ──────────────────────────────

    [Fact]
    public async Task BlockPersistence_FailureBeforeCommit_RollsBackThenReplayCreatesOneBlock()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        using var dbPre = LongHorizonPersistenceTestFixture.NewContext();
        var preSnapshot = await new LongHorizonRollingStateRepository(dbPre).LoadRestartSnapshotAsync(planStateId);
        using var verifyPre = LongHorizonPersistenceTestFixture.NewContext();
        var preBlockCount = await verifyPre.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == planStateId);

        var expectedVersion = preSnapshot!.ConcurrencyVersion;
        const int blockStart = 5;
        const int blockEnd = 8;
        var reason = LongHorizonReasonCode.SafetyReassessmentRequired;
        const string publicReasonCategory = "SafetyReviewRequired";
        const string evidenceFingerprint = "test-rollback-block";
        const bool retryEligible = true;

        using (var dbFail = LongHorizonPersistenceTestFixture.NewContext())
        {
            var injector = new LongHorizonTestPersistenceFailureInjector(LongHorizonPersistenceOperation.BlockPersistence, LongHorizonPersistenceFailpoint.BeforeCommit);
            var failingAdapter = new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(dbFail, injector));
            var thrown = await Record.ExceptionAsync(() => failingAdapter.PersistBlockAsync(
                planStateId, expectedVersion, blockStart, blockEnd, reason, publicReasonCategory, evidenceFingerprint, checkpointDate, retryEligible));
            Assert.IsType<LongHorizonInjectedPersistenceFailureException>(thrown);
        }

        using var verifyPost = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(preBlockCount, await verifyPost.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == planStateId));
        var planAfter = await verifyPost.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
        Assert.NotEqual(RunningApp.Domain.Enums.LongHorizonPersistedLifecycleState.NumericActivationBlocked, planAfter.CurrentLifecycleStatus);

        using var dbReplay = LongHorizonPersistenceTestFixture.NewContext();
        var replayAdapter = new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(dbReplay));
        var replay = await replayAdapter.PersistBlockAsync(
            planStateId, expectedVersion, blockStart, blockEnd, reason, publicReasonCategory, evidenceFingerprint, checkpointDate, retryEligible);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, replay.Outcome);

        using var verifyReplay = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(preBlockCount + 1, await verifyReplay.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == planStateId));
    }

    // ── Retry restoration rollback (Part 10) ─────────────────────────────

    [Fact]
    public async Task RetryRestoration_FailureBeforeCommit_RollsBackThenReplayCreatesOneRetry()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var checkpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        using var dbInit = LongHorizonPersistenceTestFixture.NewContext();
        var initSnapshot = await new LongHorizonRollingStateRepository(dbInit).LoadRestartSnapshotAsync(planStateId);
        var blockAdapter = new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(dbInit));
        var blocked = await blockAdapter.PersistBlockAsync(
            planStateId, initSnapshot!.ConcurrencyVersion, 5, 8,
            LongHorizonReasonCode.SafetyReassessmentRequired, "SafetyReviewRequired", "test-rollback-retry", checkpointDate, true);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, blocked.Outcome);

        using var dbPre = LongHorizonPersistenceTestFixture.NewContext();
        var preSnapshot = await new LongHorizonRollingStateRepository(dbPre).LoadRestartSnapshotAsync(planStateId);
        using var verifyPre = LongHorizonPersistenceTestFixture.NewContext();
        var preRetryCount = await verifyPre.LongHorizonBlockRetryRecords.CountAsync(
            r => r.PlanStateId == planStateId && r.EventType == RunningApp.Domain.Enums.LongHorizonPersistedBlockRetryEventType.RetryRestored);

        var retryDate = checkpointDate.AddDays(1);
        using (var dbFail = LongHorizonPersistenceTestFixture.NewContext())
        {
            var injector = new LongHorizonTestPersistenceFailureInjector(LongHorizonPersistenceOperation.RetryPersistence, LongHorizonPersistenceFailpoint.BeforeCommit);
            var failingRepo = new LongHorizonRollingStateRepository(dbFail, injector);
            var thrown = await Record.ExceptionAsync(() => failingRepo.SaveRetryRestorationAsync(new LongHorizonRollingRetryPersistenceRequest
            {
                PlanStateId = planStateId,
                ExpectedConcurrencyVersion = preSnapshot!.ConcurrencyVersion,
                RestoredGlobalWeekStart = 5,
                RestoredGlobalWeekEnd = 8,
                RetryCheckpointDate = retryDate,
                ChangedEvidenceFingerprint = "changed-fingerprint-rollback-test",
                RelatedDecisionId = Guid.NewGuid(),
                IdempotencyKey = $"retry-rollback-test:{planStateId}",
            }));
            Assert.IsType<LongHorizonInjectedPersistenceFailureException>(thrown);
        }

        using var verifyPost = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(preRetryCount, await verifyPost.LongHorizonBlockRetryRecords.CountAsync(
            r => r.PlanStateId == planStateId && r.EventType == RunningApp.Domain.Enums.LongHorizonPersistedBlockRetryEventType.RetryRestored));
        var planAfter = await verifyPost.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
        Assert.Equal(RunningApp.Domain.Enums.LongHorizonPersistedLifecycleState.NumericActivationBlocked, planAfter.CurrentLifecycleStatus);

        using var dbReplay = LongHorizonPersistenceTestFixture.NewContext();
        var replay = await new LongHorizonRollingStateRepository(dbReplay).SaveRetryRestorationAsync(new LongHorizonRollingRetryPersistenceRequest
        {
            PlanStateId = planStateId,
            ExpectedConcurrencyVersion = preSnapshot!.ConcurrencyVersion,
            RestoredGlobalWeekStart = 5,
            RestoredGlobalWeekEnd = 8,
            RetryCheckpointDate = retryDate,
            ChangedEvidenceFingerprint = "changed-fingerprint-rollback-test",
            RelatedDecisionId = Guid.NewGuid(),
            IdempotencyKey = $"retry-rollback-test-replay:{planStateId}",
        });
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, replay.Outcome);

        using var verifyReplay = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(preRetryCount + 1, await verifyReplay.LongHorizonBlockRetryRecords.CountAsync(
            r => r.PlanStateId == planStateId && r.EventType == RunningApp.Domain.Enums.LongHorizonPersistedBlockRetryEventType.RetryRestored));
    }

    // ── Post-commit acknowledgement failure (Part 14) ────────────────────

    [Fact]
    public async Task PostCommitAcknowledgementFailure_DoesNotRollBack_AndReplayDeduplicates()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        // Horizon 21 (GE=1): the first continuation call is a pure Runway
        // entry, classified InitialPersistence by SaveActivationSuccessAsync's
        // own SegmentsCovered-based operation tagging (no Core segment yet).
        using var dbFail = LongHorizonPersistenceTestFixture.NewContext();
        var injector = new LongHorizonTestPersistenceFailureInjector(
            LongHorizonPersistenceOperation.InitialPersistence, LongHorizonPersistenceFailpoint.AfterCommitBeforeAcknowledgement);
        var failingRepo = new LongHorizonRollingStateRepository(dbFail, injector);
        var thrown = await Record.ExceptionAsync(() =>
            LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(planStateId, initialWindow, date, catalogRoot, candidate, failingRepo));
        Assert.IsType<LongHorizonInjectedPersistenceFailureException>(thrown);

        // The database COMMIT already happened -- a fresh context must see the durable result.
        // This IS the correct "idempotent recovery" behavior for a post-commit
        // acknowledgement failure: the caller does not need to blindly resend
        // the exact same request -- reconstructing from a fresh restart
        // already exposes the committed window, so normal continuation
        // naturally advances to the NEXT window rather than ever re-doing
        // the first one (proven below by the exact-range duplicate check).
        using var dbAfter = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(dbAfter).LoadRestartSnapshotAsync(planStateId);
        Assert.NotNull(reconstructed);
        var committedWindow = reconstructed!.DarkState.CurrentWindow;
        Assert.True(committedWindow.EndGlobalWeek > initialWindow.EndGlobalWeek);

        using var verifyBefore = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(1, await verifyBefore.LongHorizonActivationWindowRecords.CountAsync(
            a => a.PlanStateId == planStateId && a.StartGlobalWeek == committedWindow.StartGlobalWeek && a.EndGlobalWeek == committedWindow.EndGlobalWeek));

        // Direct exact replay of the already-committed operation, reusing its
        // own WindowId/ContextVersion (mirroring exactly how
        // LongHorizonRollingActivationPersistenceAdapter derives IdempotencyKey
        // for the JIT path) -- proves the deterministic idempotency-key
        // mechanism itself deduplicates a resent identical request, which is
        // the real guarantee Part 14 asks for (a naive client that resends
        // the exact prior request after a lost post-commit acknowledgement
        // must not create a second row).
        using var verifyKey = LongHorizonPersistenceTestFixture.NewContext();
        var committedRecord = await verifyKey.LongHorizonActivationWindowRecords.AsNoTracking().SingleAsync(
            a => a.PlanStateId == planStateId && a.StartGlobalWeek == committedWindow.StartGlobalWeek && a.EndGlobalWeek == committedWindow.EndGlobalWeek);

        using var dbReplay = LongHorizonPersistenceTestFixture.NewContext();
        var replayRepo = new LongHorizonRollingStateRepository(dbReplay);
        var replay = await replayRepo.SaveActivationSuccessAsync(new LongHorizonRollingActivationPersistenceRequest
        {
            PlanStateId = planStateId,
            ExpectedConcurrencyVersion = 0,
            ActivatedWindow = committedWindow,
            LifecycleStates = reconstructed.DarkState.LifecycleStates,
            ContextVersion = committedWindow.ContextVersion,
            IdempotencyKey = committedRecord.IdempotencyKey,
        });
        Assert.Equal(LongHorizonRollingPersistenceOutcome.IdempotentReplay, replay.Outcome);

        using var verifyAfter = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(1, await verifyAfter.LongHorizonActivationWindowRecords.CountAsync(
            a => a.PlanStateId == planStateId && a.StartGlobalWeek == committedWindow.StartGlobalWeek && a.EndGlobalWeek == committedWindow.EndGlobalWeek));
    }
}
