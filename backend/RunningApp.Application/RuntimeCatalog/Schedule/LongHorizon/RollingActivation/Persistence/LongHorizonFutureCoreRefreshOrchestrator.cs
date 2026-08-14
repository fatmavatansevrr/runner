using System.Security.Cryptography;
using System.Text;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2E -- the future-only Core context refresh capability. Never
/// duplicates window selection, condition resolution, or Core generation:
/// evaluates the explicit eligibility gate (<see cref="LongHorizonCoreRefreshEligibility"/>),
/// then delegates entirely to the existing, unmodified production chain
/// (<see cref="LongHorizonRollingCheckpointRuntime"/> for real evidence
/// aggregation, <see cref="LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync"/>
/// for real condition re-resolution, real Core regeneration, and atomic
/// persistence -- which already creates a new Core-context version and
/// supersedes the prior Active row, per the existing repository behavior).
///
/// Per this phase's own "preferred separation" note and its documented
/// fallback: this implementation uses the existing runtime contract's
/// combined refresh-plus-immediate-bounded-activation authority (refresh
/// context V2 is created atomically together with the next bounded Core
/// activation window it justifies), since no separate "context-only, no
/// activation" persistence path exists in production code and building one
/// was out of this phase's scope. This is documented, not silently assumed.
/// </summary>
internal sealed class LongHorizonFutureCoreRefreshOrchestrator
{
    private readonly ILongHorizonRollingStateRepository _repository;
    private readonly LongHorizonRollingCheckpointRuntime _checkpointRuntime;
    private readonly LongHorizonRollingRestartContinuationService _continuationService;

    public LongHorizonFutureCoreRefreshOrchestrator(ILongHorizonRollingStateRepository repository)
    {
        _repository = repository;
        _checkpointRuntime = new LongHorizonRollingCheckpointRuntime();
        _continuationService = new LongHorizonRollingRestartContinuationService(repository);
    }

    public async Task<LongHorizonFutureCoreRefreshResult> RefreshAsync(
        LongHorizonFutureCoreRefreshRequest request, CancellationToken cancellationToken = default)
    {
        var stages = new List<string>();

        var snapshot = await _repository.LoadRestartSnapshotAsync(request.PlanStateId, cancellationToken)
            ?? throw new LongHorizonRollingPersistenceCorruptionException($"No durable state exists for plan {request.PlanStateId}.");
        stages.Add("SnapshotLoaded");

        if (snapshot.ConcurrencyVersion != request.ExpectedAggregateVersion)
        {
            return new LongHorizonFutureCoreRefreshResult { Outcome = LongHorizonFutureCoreRefreshOutcome.StaleVersion, ValidationStages = [.. stages, "StaleVersion"] };
        }

        var activeContext = await _repository.GetActiveCoreContextAsync(request.PlanStateId, cancellationToken);
        stages.Add("ActiveCoreContextLoaded");

        var evidenceFingerprint = EvidenceFingerprint(request.TrainingDayEvidence);
        var lifecycleBlocked = snapshot.DarkState.CurrentWindow.Status == LongHorizonActivationWindowStatus.Blocked;

        var eligibility = LongHorizonCoreRefreshEligibility.Evaluate(
            snapshot.DarkState.StructuralRoadmap, snapshot.DarkState.LifecycleStates, activeContext,
            request.RequestedAsOfDate, evidenceFingerprint, lifecycleBlocked);
        stages.Add("EligibilityEvaluated");

        if (!eligibility.IsEligible)
        {
            return new LongHorizonFutureCoreRefreshResult
            {
                Outcome = LongHorizonFutureCoreRefreshOutcome.Ineligible,
                PreviousContextId = activeContext?.CoreContextId,
                PreviousContextVersion = activeContext?.ContextVersionSequence,
                Eligibility = eligibility,
                ValidationStages = [.. stages, "IneligibleResult"],
            };
        }

        // Real evidence aggregation, reusing the existing GE checkpoint runtime
        // purely for its (evidence-independent-of-GE) snapshot/validated-load
        // computation -- with GE/Runway fully complete, this deterministically
        // returns the non-error boundary-handoff outcome (Phase 4K.7/4L.2D).
        var checkpointRequest = new LongHorizonRollingCheckpointRequest
        {
            StructuralRoadmap = snapshot.DarkState.StructuralRoadmap,
            StructuralSkeleton = snapshot.DarkState.StructuralSkeleton,
            LifecycleStates = snapshot.DarkState.LifecycleStates,
            MostRecentlyActivatedWindow = snapshot.DarkState.CurrentWindow,
            TrainingDayEvidence = request.TrainingDayEvidence,
            CheckpointDate = request.RequestedAsOfDate,
            CurrentAvailability = request.CurrentAvailability,
            LongRunDay = request.LongRunDay,
            SafetyState = request.SafetyState,
            ReadinessProfile = snapshot.DarkState.StructuralRoadmap.Profile,
            PriorValidatedAnchor = null,
            PreviousContextVersion = snapshot.DarkState.ContextVersion,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
        };
        var checkpoint = await _checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(checkpointRequest, cancellationToken);
        stages.Add("EvidenceAggregation");

        if (checkpoint.Outcome != LongHorizonRollingCheckpointRuntimeOutcome.GeCheckpointCompletedWithoutGeWindowBecauseRunwayBoundaryReached
            || checkpoint.EvidenceSnapshot is null || checkpoint.ValidatedLoad is null)
        {
            return new LongHorizonFutureCoreRefreshResult
            {
                Outcome = LongHorizonFutureCoreRefreshOutcome.CorruptState,
                PreviousContextId = activeContext?.CoreContextId,
                ValidationStages = [.. stages, "UnexpectedCheckpointOutcomeForCoreRefresh"],
            };
        }

        // Delegate entirely to the existing, unmodified JIT composition +
        // persistence chain -- real condition re-resolution, real Core
        // regeneration, atomic V1->V2 context supersession (already proven
        // production behavior), all in one existing authoritative call.
        var persistenceResult = await _continuationService.ContinueJitCompositionAsync(
            request.PlanStateId, checkpoint.EvidenceSnapshot, checkpoint.ValidatedLoad, null, null, null,
            request.PlanStartDate, request.RaceDate, request.CurrentAvailability, request.LongRunDay,
            request.CatalogRootPath, cancellationToken);
        stages.Add("RealConditionResolutionAndCoreRegeneration");

        if (persistenceResult.Outcome != LongHorizonRollingPersistenceOutcome.Success)
        {
            return new LongHorizonFutureCoreRefreshResult
            {
                Outcome = LongHorizonFutureCoreRefreshOutcome.Blocked,
                PreviousContextId = activeContext?.CoreContextId,
                PreviousContextVersion = activeContext?.ContextVersionSequence,
                PersistenceResult = persistenceResult,
                ValidationStages = [.. stages, "CompositionOrPersistenceBlocked"],
            };
        }
        stages.Add("PersistedAtomically");

        var newContext = await _repository.GetActiveCoreContextAsync(request.PlanStateId, cancellationToken);
        if (newContext is null || newContext.CoreContextId == activeContext?.CoreContextId)
        {
            // Replay of an already-persisted V2: no new context was created.
            return new LongHorizonFutureCoreRefreshResult
            {
                Outcome = LongHorizonFutureCoreRefreshOutcome.IdempotentReplay,
                PreviousContextId = activeContext?.CoreContextId,
                NewContextId = newContext?.CoreContextId,
                PreviousContextVersion = activeContext?.ContextVersionSequence,
                NewContextVersion = newContext?.ContextVersionSequence,
                PersistenceResult = persistenceResult,
                ValidationStages = [.. stages, "IdempotentReplayResult"],
            };
        }

        await _repository.SetCoreContextEvidenceFingerprintAsync(newContext.CoreContextId, evidenceFingerprint, cancellationToken);
        stages.Add("EvidenceFingerprintStamped");

        return new LongHorizonFutureCoreRefreshResult
        {
            Outcome = LongHorizonFutureCoreRefreshOutcome.Refreshed,
            PreviousContextId = activeContext?.CoreContextId,
            NewContextId = newContext.CoreContextId,
            PreviousContextVersion = activeContext?.ContextVersionSequence,
            NewContextVersion = newContext.ContextVersionSequence,
            // Note: LongHorizonCoreContextRecord.EffectiveFromGlobalWeek/EffectiveToGlobalWeek
            // (existing, unmodified field) reflects the full regenerated Core
            // candidate's structural range, not the narrower refresh-effective
            // boundary -- the result surfaces the semantically correct
            // refresh boundary (first future Pending week) from eligibility instead.
            EffectiveFromGlobalWeek = eligibility.FirstFuturePendingCoreGlobalWeek,
            EffectiveToGlobalWeek = newContext.EffectiveToGlobalWeek,
            Eligibility = eligibility,
            PersistenceResult = persistenceResult,
            ValidationStages = [.. stages, "RefreshedResult"],
        };
    }

    internal static string EvidenceFingerprint(IReadOnlyList<LongHorizonTrainingDayEvidenceRow> evidence)
    {
        var raw = string.Join(';', evidence.OrderBy(row => row.GlobalWeekNumber).ThenBy(row => row.TrainingDay.Id)
            .Select(row => $"{row.GlobalWeekNumber}:{row.TrainingDay.Id}:{row.TrainingDay.Status}:{row.TrainingDay.ActualDistanceKm}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
