using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2 Part 21 -- a dark internal adapter proving the full chain:
/// load restart snapshot -> validate -> map to the existing, unmodified
/// Phase 4K.7 checkpoint runtime's input shape -> perform AT MOST ONE
/// requested checkpoint operation -> persist the result transactionally ->
/// return a new restart snapshot. No automatic loop: one call performs
/// exactly one operation, matching the phase's explicit "do not
/// automatically activate a next window after restart" boundary. Not
/// called from any endpoint, background service, or completion handler --
/// independently invokable in tests only.
/// </summary>
internal sealed class LongHorizonRollingRestartContinuationService
{
    private readonly ILongHorizonRollingStateRepository _repository;
    private readonly ILongHorizonRollingCheckpointRuntime _checkpointRuntime;
    private readonly LongHorizonRollingActivationPersistenceAdapter _activationAdapter;
    private readonly LongHorizonRollingBlockPersistenceAdapter _blockAdapter;

    public LongHorizonRollingRestartContinuationService(
        ILongHorizonRollingStateRepository repository,
        ILongHorizonRollingCheckpointRuntime? checkpointRuntime = null)
    {
        _repository = repository;
        _checkpointRuntime = checkpointRuntime ?? new LongHorizonRollingCheckpointRuntime();
        _activationAdapter = new LongHorizonRollingActivationPersistenceAdapter(repository);
        _blockAdapter = new LongHorizonRollingBlockPersistenceAdapter(repository);
    }

    /// <summary>Requests exactly one GE checkpoint operation against the current durable state.</summary>
    public async Task<LongHorizonRollingPersistenceResult> ContinueGeCheckpointAsync(
        Guid planStateId,
        IReadOnlyList<LongHorizonTrainingDayEvidenceRow> trainingDayEvidence,
        DateOnly checkpointDate,
        IReadOnlyList<DayOfWeek> currentAvailability,
        DayOfWeek longRunDay,
        LongHorizonSafetyState safetyState,
        LongHorizonPriorValidatedAnchor? priorValidatedAnchor,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadRestartSnapshotAsync(planStateId, cancellationToken)
            ?? throw new LongHorizonRollingPersistenceCorruptionException($"No durable state exists for plan {planStateId}.");

        var state = snapshot.DarkState;
        var request = new LongHorizonRollingCheckpointRequest
        {
            StructuralRoadmap = state.StructuralRoadmap,
            StructuralSkeleton = state.StructuralSkeleton,
            LifecycleStates = state.LifecycleStates,
            MostRecentlyActivatedWindow = state.CurrentWindow,
            TrainingDayEvidence = trainingDayEvidence,
            CheckpointDate = checkpointDate,
            CurrentAvailability = currentAvailability,
            LongRunDay = longRunDay,
            SafetyState = safetyState,
            ReadinessProfile = state.StructuralRoadmap.Profile,
            PriorValidatedAnchor = priorValidatedAnchor,
            PreviousContextVersion = state.ContextVersion,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
        };

        var result = await _checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(request, cancellationToken);

        if (result.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked)
        {
            var boundaryStart = state.CurrentWindow.EndGlobalWeek + 1;
            var boundaryEnd = Math.Min(boundaryStart + 3, state.StructuralRoadmap.GeneralEnduranceWeeks);
            return await _blockAdapter.PersistBlockAsync(
                planStateId, snapshot.ConcurrencyVersion, boundaryStart, boundaryEnd,
                result.AuthoritativeReason ?? LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved),
                "MoreTrainingDataNeeded", "restart-continuation", checkpointDate, retryEligible: true,
                cancellationToken: cancellationToken);
        }

        return await _activationAdapter.PersistGeCheckpointAsync(planStateId, snapshot.ConcurrencyVersion, result, cancellationToken);
    }

    /// <summary>
    /// Phase 4L.2A -- requests exactly one Runway/Core JIT composition
    /// operation against the current durable state, closing the restart-
    /// continuation gap Phase 4L.2 disclosed as smoke-tested only. Loads the
    /// snapshot's reconstructed ExistingLockedCoreTarget/ExistingRunwayPrescription/
    /// ExistingRunwayCalendarProjection (full-fidelity round-tripped, never
    /// caller-fabricated) and threads them into the real, unmodified Phase
    /// 4K.8C composition orchestrator, exactly as the Phase 4K.9 harness
    /// itself does at the equivalent decision point.
    /// </summary>
    public async Task<LongHorizonRollingPersistenceResult> ContinueJitCompositionAsync(
        Guid planStateId,
        LongHorizonCheckpointEvidenceSnapshot evidenceSnapshot,
        ValidatedSustainableLoad validatedLoad,
        int? exactCompletedFrequency,
        LongHorizonCheckpointDecision? geCheckpointDecision,
        IReadOnlyList<ActivatedNumericWeek>? geActivatedWeeks,
        DateOnly planStartDate,
        DateOnly raceDate,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek longRunDay,
        string catalogRootPath,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<int, LongHorizonNumericLifecycleState>? lifecycleStatesOverride = null,
        string? publishedBundleReleaseVersion = null,
        // Phase 10K-FREQ.6D.19 -- surfaced only for real-Postgres dark-verification
        // callers (matching the same TargetFinishTimeSeconds/TargetFinishTimeSource
        // convention LongHorizonFullLifecycleTestFixture already uses for 4D dark
        // testing): a real, already-persisted 5D LongHorizon plan reaching a
        // GOAL_PACE_TEN_K Core week requires resolved goal-feasibility evidence
        // (CatalogSessionPrescriptionPlanner) that today's real production caller
        // (LongHorizonRollingWindowActivationService) has no persisted source for --
        // TrainingPlan.TargetFinishTimeSeconds exists, but no TargetFinishTimeSource
        // classification is persisted anywhere for a restarted/rolling plan. Wiring
        // that correctly is a genuine product decision (how should a restarted plan
        // reclassify target-time evidence it no longer holds in original form?), not
        // resolved here -- these parameters default to null, so every real production
        // call remains byte-identical to before this phase.
        int? targetFinishTimeSeconds = null,
        TargetFinishTimeSource? targetFinishTimeSource = null,
        RecentRaceInput? recentRace = null)
    {
        var snapshot = await _repository.LoadRestartSnapshotAsync(planStateId, cancellationToken)
            ?? throw new LongHorizonRollingPersistenceCorruptionException($"No durable state exists for plan {planStateId}.");

        var state = snapshot.DarkState;
        var composition = new LongHorizonRollingJitCompositionOrchestrator();
        var result = await composition.ComposeAndActivateNextWindowAsync(new LongHorizonRollingJitCompositionRequest
        {
            StructuralRoadmap = state.StructuralRoadmap,
            // The caller's freshest post-checkpoint LifecycleStates (e.g. reflecting a
            // just-activated GE week within the same rolling pass) take priority over
            // the pre-checkpoint restart snapshot -- matching the Phase 4K.9 harness's
            // own atomic-selection parameter, never a stale copy.
            LifecycleStates = lifecycleStatesOverride ?? state.LifecycleStates,
            PreviousActivatedWindow = state.CurrentWindow,
            EvidenceSnapshot = evidenceSnapshot,
            ValidatedLoad = validatedLoad,
            ExactCompletedFrequency = exactCompletedFrequency,
            GeCheckpointDecision = geCheckpointDecision,
            GeActivatedWeeks = geActivatedWeeks,
            PreviousContextVersion = state.ContextVersion,
            ReadinessProfile = state.StructuralRoadmap.Profile,
            CurrentAvailability = evidenceSnapshot.Availability,
            PreferredDays = preferredDays,
            LongRunDay = longRunDay,
            CheckpointDate = evidenceSnapshot.CheckpointDate,
            PlanStartDate = planStartDate,
            RaceDate = raceDate,
            SafetyState = evidenceSnapshot.SafetyState,
            ExistingLockedCoreTarget = state.RunwayTargetLock,
            ExistingRunwayPrescription = state.RunwayPrescription,
            ExistingRunwayCalendarProjection = state.RunwayCalendarProjection,
            CatalogRootPath = catalogRootPath,
            PublishedBundleReleaseVersion = publishedBundleReleaseVersion,
            Candidate = snapshot.Candidate,
            TargetFinishTimeSeconds = targetFinishTimeSeconds,
            TargetFinishTimeSource = targetFinishTimeSource,
            RecentRace = recentRace,
        }, cancellationToken);

        if (result.Outcome != LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded)
        {
            var window = result.ActivationResult?.ActivationWindow;
            var boundaryStart = window?.StartGlobalWeek ?? state.CurrentWindow.EndGlobalWeek + 1;
            var boundaryEnd = window?.EndGlobalWeek ?? Math.Min(boundaryStart + 3, state.StructuralRoadmap.TotalWeeks);
            var blockResult = await _blockAdapter.PersistBlockAsync(
                planStateId, snapshot.ConcurrencyVersion, boundaryStart, boundaryEnd,
                result.AuthoritativeReason ?? LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitEvidenceConflictUnresolved),
                "PlanTransitionUnavailable", "restart-jit-continuation", evidenceSnapshot.CheckpointDate, retryEligible: true,
                cancellationToken: cancellationToken);
            // This is a Block, not an activation -- the caller's own
            // isBlockAttempt flag was computed earlier from the checkpoint
            // runtime's outcome and cannot see this internal JIT/Runway
            // composition failure, so it must be signaled explicitly here
            // (see LongHorizonRollingPersistenceResult.IsBlock).
            return blockResult with { IsBlock = true };
        }

        // Phase 4L.2C Part 11 -- fail-closed guard for the exact invariant this
        // phase resolves: a committed continuation must advance the durable
        // lifecycle boundary, never re-persist or regress the previous window.
        LongHorizonRunwayContinuationAdvancementValidator.Validate(state.CurrentWindow, result.ActivationResult!.ActivationWindow!);

        return await _activationAdapter.PersistJitCompositionAsync(planStateId, snapshot.ConcurrencyVersion, result, cancellationToken);
    }
}
