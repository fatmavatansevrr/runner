using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.Application.Services;

/// <summary>
/// Phase 4L.4A -- the authenticated public continuation operation. Never a
/// second continuation engine: eligibility/terminality/authorization are new
/// here, but the actual reconstruct-checkpoint-compose-persist chain
/// delegates entirely to the existing, unmodified Phase 4L.2 production
/// runtime (<see cref="LongHorizonRollingStateRepository"/>,
/// <see cref="LongHorizonRollingCheckpointRuntime"/>,
/// <see cref="LongHorizonRollingRestartContinuationService"/>) -- the exact
/// chain Phase 4L.2's own restart-continuation tests already drive, just
/// never previously reachable from a public endpoint.
/// </summary>
public sealed class LongHorizonRollingWindowActivationService : ILongHorizonRollingWindowActivationService
{
    private const int ContractVersion = 1;

    private readonly AppDbContext _db;
    private readonly ILogger<LongHorizonRollingWindowActivationService> _logger;
    private readonly ILongHorizonPersistenceFailureInjector _failureInjector;
    private readonly string? _publishedBundleReleaseVersion;

    /// <summary>
    /// Phase 10K-FREQ.6D.19 -- threads the same real, already-configured
    /// <see cref="RuntimeCatalog.PlanCatalogOptions.PublishedBundleReleaseVersion"/>
    /// every other real catalog call site already uses (appsettings.json,
    /// e.g. "1.1.0") into the JIT composition chain below. Without this, a
    /// real Intermediate x5D LongHorizon plan's Core segment (ProfileBacked
    /// KEY_SESSION) can never generate: <see cref="Prescription.Execution.PublishedTemplateBundleLoader.TryLoadAsync"/>
    /// returns null whenever no release version is configured, and a
    /// ProfileBacked session never falls back to the Legacy path (by design
    /// -- see <see cref="Prescription.Session.CatalogSessionPrescriptionPlanner"/>).
    /// 4D LongHorizon Core is Legacy (no profile) and was never affected by
    /// this gap, which is why it was never previously observed.
    /// </summary>
    public LongHorizonRollingWindowActivationService(
        AppDbContext db, ILogger<LongHorizonRollingWindowActivationService> logger,
        Microsoft.Extensions.Options.IOptions<RuntimeCatalog.PlanCatalogOptions> catalogOptions)
        : this(db, logger, NoOpLongHorizonPersistenceFailureInjector.Instance, catalogOptions.Value.PublishedBundleReleaseVersion)
    {
    }

    /// <summary>Test-only seam (Part 24): threads the existing Phase 4L.2F
    /// failure-injection contract through to the repository so pre-commit
    /// rollback can be proven against the real production persistence chain,
    /// without any production code path ever supplying a non-no-op injector.
    /// <paramref name="publishedBundleReleaseVersion"/> defaults to null,
    /// byte-identical to this constructor's pre-FREQ.6D.19 behavior, for
    /// every existing 4D-only caller of this internal seam.</summary>
    internal LongHorizonRollingWindowActivationService(AppDbContext db, ILogger<LongHorizonRollingWindowActivationService> logger,
        ILongHorizonPersistenceFailureInjector failureInjector, string? publishedBundleReleaseVersion = null)
    {
        _db = db;
        _logger = logger;
        _failureInjector = failureInjector;
        _publishedBundleReleaseVersion = publishedBundleReleaseVersion;
    }

    public async Task<LongHorizonActivateNextWindowResponse> ActivateNextWindowAsync(
        Guid userId, LongHorizonActivateNextWindowRequest request, CancellationToken ct = default)
    {
        if (request.ContractVersion != ContractVersion)
            throw new LongHorizonContinuationVersionUnsupportedException("Unsupported Long-Horizon continuation contract version.");

        var planRow = await _db.TrainingPlans.AsNoTracking().SingleOrDefaultAsync(p =>
            p.InternalUserId == userId && p.Status == TrainingPlanStatus.Active && p.ScheduleStrategy == PlanScheduleStrategy.RollingLongHorizon, ct);
        if (planRow?.LongHorizonRollingPlanStateId is not { } rollingStateId)
            throw new LongHorizonReadStateNotFoundException("Active Long-Horizon rolling plan not found.");
        var planId = planRow.Id;

        _logger.LogInformation("Long-Horizon continuation requested. UserId={UserId} PlanId={PlanId}", userId, planId);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Serializes with cancellation (mirrors LongHorizonRollingSessionMutationService's
            // own row-lock pattern): losing this lock means cancellation already
            // committed, or this activation commits first and wins.
            var lockedPlan = await _db.TrainingPlans
                .FromSqlInterpolated($"SELECT * FROM \"TrainingPlans\" WHERE \"Id\" = {planId} FOR UPDATE")
                .AsNoTracking().SingleAsync(ct);
            if (lockedPlan.Status != TrainingPlanStatus.Active)
                throw new LongHorizonReadStateNotFoundException("Active Long-Horizon rolling plan not found.");

            var aggregate = await _db.LongHorizonRollingPlanStates
                .Include(p => p.Weeks).ThenInclude(w => w.Sessions)
                .SingleOrDefaultAsync(p => p.Id == rollingStateId, ct)
                ?? throw new LongHorizonReadStateCorruptException("Rolling plan state is unavailable.");

            // Fresh terminality revalidation: never trust a client-cached Home read.
            var windowWeeks = aggregate.Weeks
                .Where(w => w.GlobalWeek >= aggregate.CurrentWindowStartWeek && w.GlobalWeek <= aggregate.CurrentWindowEndWeek)
                .ToList();
            var windowSessions = windowWeeks.SelectMany(w => w.Sessions).ToList();
            var readiness = LongHorizonActiveReadModelProvider.Readiness(aggregate, windowSessions);
            var previousRange = new LongHorizonWindowRange { StartGlobalWeek = aggregate.CurrentWindowStartWeek, EndGlobalWeek = aggregate.CurrentWindowEndWeek };

            switch (readiness)
            {
                case LongHorizonCheckpointReadiness.TerminalPlanComplete:
                    await tx.CommitAsync(ct);
                    return TerminalResponse(planId, lockedPlan, previousRange);
                case LongHorizonCheckpointReadiness.CurrentWindowInProgress:
                    await tx.CommitAsync(ct);
                    throw new LongHorizonContinuationInProgressException("The current Long-Horizon window is still in progress.");
                case LongHorizonCheckpointReadiness.ReassessmentRequired:
                    await tx.CommitAsync(ct);
                    if (aggregate.RetryEligible)
                        throw new LongHorizonContinuationRetryRequiredException("The plan is blocked; retry restoration must run before continuation.");
                    throw new LongHorizonContinuationReassessmentRequiredException("The plan requires reassessment before the next window can activate.");
            }

            // readiness == NextWindowActivationReady -- window finality is
            // established (Readiness() above already confirmed no session
            // remains Planned), so the just-checkpointed window's execution
            // state is final. Phase 4M.4A: build the frozen 4M.1
            // WindowExecutionSummary/NextWindowLoadDecisionPolicy authorities
            // from that real persisted state -- BEFORE composing the next
            // window's content, and using the exact same windowSessions
            // already loaded above (no extra query). This whole-window
            // summary remains window-level (Rev5 §7a: numeric anchor
            // architecture is unaffected) -- it feeds ONLY
            // EffectiveCompletedCount into NextWindowNumericAnchorSelector
            // below, and is never fed into NextWindowLoadDecisionPolicy
            // directly (see WeeklyWindowPartitionerBoundaryTests).
            var windowSummary = WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(windowSessions));

            // Phase 4M.5C -- Rev5 §7a "Multi-Week Window Aggregation":
            // NextWindowLoadDecisionPolicy is calibrated for exactly one
            // real structural week (1 KEY + 2 EASY + 1 LONG); a rolling
            // activation window may contain up to four such weeks (4M.5A).
            // Partition this window's evidence back into its real,
            // persisted structural weeks -- attributing every replacement
            // session's evidence to its ORIGINAL root's week, never its own
            // physical week (Rev5 §7a Weekly Lineage Attribution Rule) --
            // evaluate the unmodified policy once per week, then collapse
            // the per-week results with B1 worst-week-wins.
            var weeklySessionGroups = WeeklyWindowPartitioner.PartitionByStructuralWeekLineage(windowWeeks);
            var weeklyResults = weeklySessionGroups
                .Select(weekSessions => NextWindowLoadDecisionPolicy.Evaluate(
                    WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(weekSessions))))
                .ToList();
            var nextWindowResult = WeeklyLoadDecisionAggregator.AggregateWorstWeekWins(weeklyResults);

            // readiness == NextWindowActivationReady -- real continuation, reusing
            // the exact reconstruct -> checkpoint -> compose -> persist chain
            // Phase 4L.2's LongHorizonRunwayCoreRestartFixture already drives in
            // tests only. No window-selection or condition logic is duplicated here.
            var repo = new LongHorizonRollingStateRepository(_db, _failureInjector);
            var snapshot = await repo.LoadRestartSnapshotAsync(rollingStateId, ct)
                ?? throw new LongHorizonReadStateCorruptException($"No durable rolling state exists for plan {rollingStateId}.");
            var state = snapshot.DarkState;

            var currentAvailability = aggregate.PreferredDaysCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(d => Enum.Parse<DayOfWeek>(d, ignoreCase: true)).ToList();
            var longRunDay = Enum.Parse<DayOfWeek>(aggregate.LongRunDay, ignoreCase: true);
            var checkpointDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var evidenceRows = LongHorizonRollingOutcomeEvidenceAdapter.ToCheckpointRows(windowSessions);

            var checkpointRuntime = new LongHorizonRollingCheckpointRuntime();
            var checkpointRequest = new LongHorizonRollingCheckpointRequest
            {
                StructuralRoadmap = state.StructuralRoadmap,
                StructuralSkeleton = state.StructuralSkeleton,
                LifecycleStates = state.LifecycleStates,
                MostRecentlyActivatedWindow = state.CurrentWindow,
                TrainingDayEvidence = evidenceRows,
                CheckpointDate = checkpointDate,
                CurrentAvailability = currentAvailability,
                LongRunDay = longRunDay,
                // No persisted safety/health signal source exists yet for rolling
                // plans (documented simplification -- see phase doc Part 6).
                SafetyState = LongHorizonSafetyState.Clear,
                ReadinessProfile = state.StructuralRoadmap.Profile,
                PriorValidatedAnchor = PriorAnchor(state),
                PreviousContextVersion = state.ContextVersion,
                GoalType = Enum.Parse<GoalType>(aggregate.GoalType),
                GoalDistance = Enum.Parse<GoalDistance>(aggregate.GoalDistance),
                Level = Enum.Parse<RunningBackground>(aggregate.Level),
                DaysPerWeek = aggregate.DaysPerWeek,
            };
            var checkpoint = await checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(checkpointRequest, ct);

            // Phase 4M.4B.2 -- Rev4 §7: select which already-authoritative
            // anchor (this window's freshly-aggregated evidence, or the
            // existing PriorValidatedCheckpointLoad carried above via
            // PriorAnchor(state)) feeds the unmodified GE/JIT composition
            // below. checkpoint.ValidatedLoad is overridden in place (never
            // re-derived) so every downstream consumer -- GE's own audit
            // persistence (ValidatedWeeklyVolumeKm/ValidatedLongRunKm
            // metadata) and the JIT path's actual numeric materialization
            // input alike -- sees exactly one, already-decided anchor. For
            // ProgressAsPlanned this selects checkpoint.ValidatedLoad
            // unchanged, so behavior is byte-for-byte identical to
            // pre-4M.4B.2 (see Phase 4M.4B.2 doc §6/§9 regression proof).
            var selectedAnchor = NextWindowNumericAnchorSelector.Select(
                nextWindowResult.LoadDecision, checkpoint.ValidatedLoad,
                checkpointRequest.PriorValidatedAnchor?.Load, windowSummary.EffectiveCompletedCount);
            checkpoint = checkpoint with { ValidatedLoad = selectedAnchor };

            var geEnd = state.StructuralRoadmap.GeneralEnduranceWeeks;
            var reachesGeBoundary = checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated
                && checkpoint.ActivationWindow!.EndGlobalWeek == geEnd;
            var pureGe = checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated && !reachesGeBoundary;
            var isBlockAttempt = checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked;
            // Phase 4L.4D: the JIT/Runway-boundary path has no evaluator-style
            // Blocked outcome of its own -- insufficient evidence to compose
            // the next Runway/Core window previously surfaced as
            // LongHorizonReadStateCorruptException (409, no durable Block
            // record, unclassifiable, unretryable). Reclassified here as a
            // real, durable, RequiresRegeneratePreview-class Block using the
            // exact same authority (PersistBlockAsync) the GE path already
            // uses, so Home/retry/recovery_requirement all agree.
            var isJitEvidenceUnavailable = !pureGe && !isBlockAttempt
                && (checkpoint.EvidenceSnapshot is null || checkpoint.ValidatedLoad is null);
            if (isJitEvidenceUnavailable) isBlockAttempt = true;

            LongHorizonRollingPersistenceResult persistResult;
            if (pureGe)
            {
                persistResult = await new LongHorizonRollingActivationPersistenceAdapter(repo)
                    .PersistGeCheckpointAsync(rollingStateId, snapshot.ConcurrencyVersion, checkpoint, ct);
            }
            else if (isBlockAttempt)
            {
                var boundaryStart = state.CurrentWindow.EndGlobalWeek + 1;
                var boundaryEnd = Math.Min(boundaryStart + 3, isJitEvidenceUnavailable ? state.StructuralRoadmap.TotalWeeks : geEnd);
                var reason = isJitEvidenceUnavailable
                    ? LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitValidatedLoadUnavailable)
                    : checkpoint.AuthoritativeReason ?? LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved);
                persistResult = await new LongHorizonRollingBlockPersistenceAdapter(repo).PersistBlockAsync(
                    rollingStateId, snapshot.ConcurrencyVersion, boundaryStart, boundaryEnd, reason,
                    "MoreTrainingDataNeeded", LongHorizonFutureCoreRefreshOrchestrator.EvidenceFingerprint(evidenceRows),
                    checkpointDate, retryEligible: true, cancellationToken: ct);
            }
            else
            {
                // GE-boundary handoff or already past GE: real Runway/Core JIT
                // composition, the same chain LongHorizonFutureCoreRefreshOrchestrator
                // uses for its own (future-only) continuation.
                var continuationService = new LongHorizonRollingRestartContinuationService(repo);
                persistResult = await continuationService.ContinueJitCompositionAsync(
                    rollingStateId, checkpoint.EvidenceSnapshot!, checkpoint.ValidatedLoad!,
                    checkpoint.EvidenceSnapshot!.CompletedRunsCount == 0 ? null : aggregate.DaysPerWeek,
                    checkpoint.CheckpointDecision,
                    checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated ? checkpoint.NewlyActivatedWeeks : null,
                    aggregate.StartDate, aggregate.RaceDate, currentAvailability, longRunDay, aggregate.CatalogRootPath,
                    ct, lifecycleStatesOverride: state.LifecycleStates, publishedBundleReleaseVersion: _publishedBundleReleaseVersion,
                    // Phase 10K-FREQ.6D.21 -- the canonical plan-level provenance
                    // (FREQ.6D.20's approved authority), read verbatim from the
                    // already-loaded TrainingPlan row -- never re-derived, never
                    // re-queried against today's CanonicalTargetFinishTimePolicy.
                    targetFinishTimeSeconds: planRow.TargetFinishTimeSeconds,
                    targetFinishTimeSource: planRow.TargetFinishTimeSource);
            }

            switch (persistResult.Outcome)
            {
                case LongHorizonRollingPersistenceOutcome.Success:
                case LongHorizonRollingPersistenceOutcome.IdempotentReplay:
                    await tx.CommitAsync(ct);
                    if (isBlockAttempt || persistResult.IsBlock)
                        throw new LongHorizonContinuationBlockedException("The next Long-Horizon window is blocked pending reassessment.");
                    return await BuildActivatedResponseAsync(planId, lockedPlan, rollingStateId, previousRange,
                        persistResult.Outcome == LongHorizonRollingPersistenceOutcome.IdempotentReplay
                            ? LongHorizonContinuationOutcome.IdempotentReplay
                            : LongHorizonContinuationOutcome.Activated, nextWindowResult, ct);
                case LongHorizonRollingPersistenceOutcome.ConcurrencyConflict:
                    await tx.RollbackAsync(ct);
                    throw new LongHorizonContinuationConcurrencyConflictException("A concurrent Long-Horizon continuation won; reload and retry.");
                default:
                    await tx.RollbackAsync(ct);
                    throw new LongHorizonReadStateCorruptException(persistResult.FailureReason ?? "Long-Horizon continuation persistence failed integrity validation.");
            }
        }
        catch
        {
            // A no-op if this path already committed/rolled back above.
            try { await tx.RollbackAsync(ct); } catch { /* transaction already completed on this path */ }
            throw;
        }
    }

    private async Task<LongHorizonActivateNextWindowResponse> BuildActivatedResponseAsync(
        Guid planId, TrainingPlan planRow, Guid rollingStateId, LongHorizonWindowRange previousRange,
        LongHorizonContinuationOutcome outcome, NextWindowAdaptationResult nextWindowResult, CancellationToken ct)
    {
        var fresh = await _db.LongHorizonRollingPlanStates.AsNoTracking()
            .Include(p => p.Weeks).ThenInclude(w => w.Sessions)
            .SingleAsync(p => p.Id == rollingStateId, ct);
        var activatedSessions = fresh.Weeks
            .Where(w => w.GlobalWeek >= fresh.CurrentWindowStartWeek && w.GlobalWeek <= fresh.CurrentWindowEndWeek)
            .SelectMany(w => w.Sessions).OrderBy(s => s.AssignedDate).ThenBy(s => s.SessionOrdinal).ToList();
        var readiness = LongHorizonActiveReadModelProvider.Readiness(fresh, activatedSessions);
        var nextPending = fresh.Weeks.Where(w => w.LifecycleState == LongHorizonPersistedLifecycleState.NumericPending)
            .OrderBy(w => w.GlobalWeek).FirstOrDefault();

        _logger.LogInformation(
            "Long-Horizon continuation {Outcome}. PlanId={PlanId} PreviousWindow={PreviousStart}-{PreviousEnd} ActivatedWindow={ActivatedStart}-{ActivatedEnd}",
            outcome, planId, previousRange.StartGlobalWeek, previousRange.EndGlobalWeek, fresh.CurrentWindowStartWeek, fresh.CurrentWindowEndWeek);

        return new LongHorizonActivateNextWindowResponse
        {
            PlanId = planId,
            Outcome = outcome,
            PreviousWindowRange = previousRange,
            ActivatedWindowRange = new LongHorizonWindowRange { StartGlobalWeek = fresh.CurrentWindowStartWeek, EndGlobalWeek = fresh.CurrentWindowEndWeek },
            ActivatedGlobalWeeks = Enumerable.Range(fresh.CurrentWindowStartWeek, fresh.CurrentWindowEndWeek - fresh.CurrentWindowStartWeek + 1).ToList(),
            ActivatedSessions = activatedSessions.Select(s => LongHorizonActiveReadModelProvider.Map(planId, s)).ToList(),
            NextPendingGlobalWeek = nextPending?.GlobalWeek,
            CheckpointReadiness = readiness,
            PlanStatus = planRow.Status.ToString(),
            IsTerminal = readiness == LongHorizonCheckpointReadiness.TerminalPlanComplete,
            ActivatedAtUtc = DateTime.UtcNow,
            PublicMessage = outcome == LongHorizonContinuationOutcome.IdempotentReplay
                ? "long_horizon.continuation_already_activated"
                : "long_horizon.continuation_activated",
            NextWindowLoadDecision = nextWindowResult.LoadDecision switch
            {
                NextWindowLoadDecision.ProgressAsPlanned => LongHorizonNextWindowLoadDecision.ProgressAsPlanned,
                NextWindowLoadDecision.Maintain => LongHorizonNextWindowLoadDecision.Maintain,
                NextWindowLoadDecision.Reduce => LongHorizonNextWindowLoadDecision.Reduce,
                _ => throw new ArgumentOutOfRangeException(nameof(nextWindowResult), nextWindowResult.LoadDecision, "Unrecognized next-window load decision."),
            },
            NextWindowSafetyReviewRequired = nextWindowResult.SafetyReviewRequired,
        };
    }

    private static LongHorizonActivateNextWindowResponse TerminalResponse(Guid planId, TrainingPlan planRow, LongHorizonWindowRange previousRange) => new()
    {
        PlanId = planId,
        Outcome = LongHorizonContinuationOutcome.TerminalPlanComplete,
        PreviousWindowRange = previousRange,
        ActivatedWindowRange = null,
        ActivatedGlobalWeeks = Array.Empty<int>(),
        ActivatedSessions = Array.Empty<LongHorizonRollingSessionResponse>(),
        NextPendingGlobalWeek = null,
        CheckpointReadiness = LongHorizonCheckpointReadiness.TerminalPlanComplete,
        PlanStatus = planRow.Status.ToString(),
        IsTerminal = true,
        ActivatedAtUtc = DateTime.UtcNow,
        PublicMessage = "long_horizon.complete",
    };

    private static LongHorizonPriorValidatedAnchor? PriorAnchor(LongHorizonFullDarkLifecycleState state)
    {
        if (state.LatestValidatedLoad is not { } load) return null;
        return new LongHorizonPriorValidatedAnchor(
            load with
            {
                WeeklyLoadSource = LongHorizonEvidenceAuthorityRecord.Create(
                    LongHorizonEvidenceSource.PriorValidatedCheckpointLoad, LongHorizonEvidenceAuthorityStatus.Authoritative,
                    "Prior completed checkpoint carried forward by explicit continuation."),
                LongRunSource = LongHorizonEvidenceAuthorityRecord.Create(
                    LongHorizonEvidenceSource.PriorValidatedCheckpointLoad, LongHorizonEvidenceAuthorityStatus.Authoritative,
                    "Prior completed checkpoint carried forward by explicit continuation."),
            },
            true,
            state.ContextVersion.Sequence);
    }
}
