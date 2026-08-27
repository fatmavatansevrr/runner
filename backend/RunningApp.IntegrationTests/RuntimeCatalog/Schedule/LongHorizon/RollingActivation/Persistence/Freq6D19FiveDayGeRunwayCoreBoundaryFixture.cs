using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Enums;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 10K-FREQ.6D.19 -- the 5D analogue of <see cref="LongHorizonRunwayCoreRestartFixture"/>
/// (Phase 4L.2A), driving the real production GE-checkpoint-continuation +
/// JIT-composition chain for an Intermediate x5D LongHorizon plan through
/// real PostgreSQL, one restart (fresh <see cref="AppDbContext"/>) per call.
/// Reuses <see cref="LongHorizonRollingInitialActivationFiveDayFixture"/>
/// (FREQ.6D.15/18's own 5D activation fixture) for initialization and the
/// generic, already-daysPerWeek-agnostic <see cref="LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows"/>
/// for evidence -- no fabricated Core rows are ever constructed; every Core
/// session this phase observes is produced by the real
/// <see cref="LongHorizonRollingJitCompositionOrchestrator"/> reached via
/// <see cref="LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync"/>.
/// </summary>
internal static class Freq6D19FiveDayGeRunwayCoreBoundaryFixture
{
    internal static readonly DateOnly StartDate = LongHorizonRollingInitialActivationFiveDayFixture.StartDate;
    internal static readonly IReadOnlyList<DayOfWeek> PreferredDays = LongHorizonRollingInitialActivationFiveDayFixture.PreferredDays;

    internal static async Task<(Guid PlanStateId, RollingNumericActivationWindow InitialWindow, PlanCatalogCandidateSummary Candidate, string CatalogRoot)>
        InitializePlanAsync(int totalWeeks)
    {
        var candidate = await LongHorizonRollingInitialActivationFiveDayFixture.LoadFiveDayCandidateAsync();
        var request = LongHorizonRollingInitialActivationFiveDayFixture.BuildActivationRequest(totalWeeks);
        var runtime = new LongHorizonRollingInitialActivationRuntime();
        var result = await runtime.BuildInitialActivationAsync(request);
        if (result.Status != LongHorizonRollingInitialActivationStatus.Approved)
            throw new InvalidOperationException($"Initial 5D activation not approved: {result.Failure?.Reason} {result.Failure?.Code} {result.Failure?.Message}");

        var planStateId = Guid.NewGuid();
        var initRequest = new LongHorizonRollingInitializationRequest
        {
            PlanStateId = planStateId,
            StructuralRoadmap = result.StructuralRoadmap!,
            PlanStartDate = StartDate,
            PreferredDays = PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            InitialWindow = result.ActivationWindow!,
            LifecycleStates = result.StructuralRoadmap!.Weeks.ToDictionary(w => w.GlobalWeekNumber, w => w.NumericLifecycleState),
            ActivatedWeeks = result.ActivatedNumericWeeks.ToDictionary(w => w.GlobalWeekNumber, w => w),
            ContextVersion = result.ContextVersion!,
            CatalogRootPath = request.CatalogRoot,
            Candidate = candidate,
            DaysPerWeek = 5,
        };

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        await new LongHorizonRollingStateRepository(db).InitializeStructuralStateAsync(initRequest);
        return (planStateId, result.ActivationWindow!, candidate, request.CatalogRoot);
    }

    /// <summary>
    /// Drives one real checkpoint evaluation + (if the GE boundary is
    /// reached or already past) one real JIT composition call, persisting
    /// via the real production adapters -- the exact same chain
    /// <see cref="LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector"/>
    /// drives for 4D, parameterized to DaysPerWeek=5 throughout.
    /// </summary>
    internal static async Task<LongHorizonRollingPersistenceResult> AdvanceOneWindowAsync(
        Guid planStateId, RollingNumericActivationWindow currentWindow, DateOnly checkpointDate, string catalogRoot, PlanCatalogCandidateSummary candidate)
    {
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
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
            CurrentAvailability = PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            ReadinessProfile = state.StructuralRoadmap.Profile,
            PriorValidatedAnchor = LongHorizonCheckpointTestFixture.Prior(26, 8),
            PreviousContextVersion = state.ContextVersion,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 5,
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
                "MoreTrainingDataNeeded", "freq6d19-boundary", checkpointDate, true);
        }

        // Reaches (or is already past) the GE boundary -- real JIT composition, same chain as 4D.
        var continuation = new LongHorizonRollingRestartContinuationService(repo);
        var jitResult = await continuation.ContinueJitCompositionAsync(
            planStateId, checkpoint.EvidenceSnapshot!, checkpoint.ValidatedLoad ?? LongHorizonCheckpointTestFixture.Prior(26, 8).Load,
            checkpoint.EvidenceSnapshot!.CompletedRunsCount == 0 ? null : 5,
            checkpoint.CheckpointDecision,
            checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated ? checkpoint.NewlyActivatedWeeks : null,
            StartDate, StartDate.AddDays(state.StructuralRoadmap.TotalWeeks * 7),
            PreferredDays, DayOfWeek.Sunday, catalogRoot,
            lifecycleStatesOverride: state.LifecycleStates,
            // Real, already-configured release (backend/RunningApp.Api/appsettings.json
            // PlanCatalogOptions:PublishedBundleReleaseVersion) -- Intermediate x5D Core's
            // KEY_SESSION is ProfileBacked and requires this exact real published bundle
            // (plan-catalog/artifacts/appsel-plan-catalog/1.1.0/bundles/TEN_K__5D__INTERMEDIATE.v1.json,
            // which already exists) to resolve; never invented for this test.
            publishedBundleReleaseVersion: "1.1.0",
            // Reuses the exact same TargetFinishTimeSeconds/ProductAverage convention
            // LongHorizonFullLifecycleTestFixture already uses for 4D dark verification
            // (not a new value or new product classification) -- a Core week containing
            // a GOAL_PACE_TEN_K workout requires resolved goal-feasibility evidence that
            // the real production caller has no persisted source for today (see this
            // phase's own report: a real product decision, not fixed here).
            targetFinishTimeSeconds: 3480,
            targetFinishTimeSource: TargetFinishTimeSource.ProductAverage);

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
