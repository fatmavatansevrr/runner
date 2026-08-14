using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.Application.Services;

/// <summary>
/// Phase 4L.4B -- the authenticated public Blocked-to-Pending retry
/// restoration. Reuses the existing, unmodified Phase 4L.2 retry authority
/// (<see cref="LongHorizonRollingStateRepository.SaveRetryRestorationAsync"/>
/// via <see cref="LongHorizonRollingRetryPersistenceAdapter"/>) -- no
/// parallel retry persistence model. Never activates a window: it only
/// restores lifecycle eligibility. A later, separate
/// <see cref="LongHorizonRollingWindowActivationService"/> call is required
/// to actually activate.
/// </summary>
public sealed class LongHorizonRollingRetryContinuationService : ILongHorizonRollingRetryContinuationService
{
    private const int ContractVersion = 1;

    private readonly AppDbContext _db;
    private readonly ILogger<LongHorizonRollingRetryContinuationService> _logger;
    private readonly ILongHorizonPersistenceFailureInjector _failureInjector;

    public LongHorizonRollingRetryContinuationService(AppDbContext db, ILogger<LongHorizonRollingRetryContinuationService> logger)
        : this(db, logger, NoOpLongHorizonPersistenceFailureInjector.Instance)
    {
    }

    /// <summary>Test-only seam, mirrors LongHorizonRollingWindowActivationService's own pattern.</summary>
    internal LongHorizonRollingRetryContinuationService(AppDbContext db, ILogger<LongHorizonRollingRetryContinuationService> logger,
        ILongHorizonPersistenceFailureInjector failureInjector)
    {
        _db = db;
        _logger = logger;
        _failureInjector = failureInjector;
    }

    public async Task<LongHorizonRetryContinuationResponse> RetryAsync(
        Guid userId, LongHorizonRetryContinuationRequest request, CancellationToken ct = default)
    {
        if (request.ContractVersion != ContractVersion)
            throw new LongHorizonContinuationVersionUnsupportedException("Unsupported Long-Horizon retry contract version.");

        var planRow = await _db.TrainingPlans.AsNoTracking().SingleOrDefaultAsync(p =>
            p.InternalUserId == userId && p.Status == TrainingPlanStatus.Active && p.ScheduleStrategy == PlanScheduleStrategy.RollingLongHorizon, ct);
        if (planRow?.LongHorizonRollingPlanStateId is not { } rollingStateId)
            throw new LongHorizonReadStateNotFoundException("Active Long-Horizon rolling plan not found.");
        var planId = planRow.Id;

        _logger.LogInformation("Long-Horizon retry requested. UserId={UserId} PlanId={PlanId}", userId, planId);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Same row-lock convention as activation/completion/cancellation.
            var lockedPlan = await _db.TrainingPlans
                .FromSqlInterpolated($"SELECT * FROM \"TrainingPlans\" WHERE \"Id\" = {planId} FOR UPDATE")
                .AsNoTracking().SingleAsync(ct);
            if (lockedPlan.Status != TrainingPlanStatus.Active)
                throw new LongHorizonReadStateNotFoundException("Active Long-Horizon rolling plan not found.");

            var aggregate = await _db.LongHorizonRollingPlanStates
                .Include(p => p.Weeks)
                .SingleOrDefaultAsync(p => p.Id == rollingStateId, ct)
                ?? throw new LongHorizonReadStateCorruptException("Rolling plan state is unavailable.");

            if (aggregate.CurrentLifecycleStatus != LongHorizonPersistedLifecycleState.NumericActivationBlocked)
            {
                await tx.CommitAsync(ct);
                _logger.LogInformation("Long-Horizon retry ineligible: no current blocked boundary. PlanId={PlanId}", planId);
                throw new LongHorizonNoBlockedBoundaryException("The plan has no current blocked boundary to retry.");
            }
            if (!aggregate.RetryEligible)
            {
                await tx.CommitAsync(ct);
                _logger.LogInformation("Long-Horizon retry ineligible: current block is not retry-eligible. PlanId={PlanId}", planId);
                throw new LongHorizonRetryNotEligibleException("The current block is not retry-eligible.");
            }

            var lastBlock = await _db.LongHorizonBlockRetryRecords.AsNoTracking()
                .Where(b => b.PlanStateId == rollingStateId && b.EventType == LongHorizonPersistedBlockRetryEventType.Block)
                .OrderByDescending(b => b.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);
            if (lastBlock is null)
            {
                await tx.CommitAsync(ct);
                throw new LongHorizonNoBlockedBoundaryException("No durable block record exists to retry.");
            }

            // Phase 4L.4C: meaningful recovery classification -- retry no
            // longer implies recoverability when the block's evidence is
            // durable and immutable. Reclassifies the LAST block's own
            // reason code (source of truth) rather than trusting the
            // persisted RetryEligible flag alone, since that flag's
            // producer (the Runway/Core JIT composition path, existing
            // Phase 4L.2 code, never modified here) always sets it true.
            var recoveryClass = LongHorizonBlockRecoveryClassification.Classify(lastBlock.InternalReasonCode);
            if (!LongHorizonBlockRecoveryClassification.IsRetryEligibleWithoutNewEvidence(recoveryClass))
            {
                await tx.CommitAsync(ct); // no mutation attempted -- no meaningless state churn.
                _logger.LogInformation("Long-Horizon retry rejected: durable evidence cannot change. PlanId={PlanId} ReasonCode={ReasonCode} RecoveryClass={RecoveryClass}",
                    planId, lastBlock.InternalReasonCode, recoveryClass);
                throw recoveryClass == LongHorizonBlockRecoveryClass.OperationalSupportRequired
                    ? new LongHorizonOperationalSupportRequiredException("This plan requires operational support before it can continue.")
                    : new LongHorizonRegeneratePreviewRequiredException("This plan's blocked evidence cannot change; cancel this plan and confirm a new preview to continue.");
            }

            var repo = new LongHorizonRollingStateRepository(_db, _failureInjector);
            var snapshot = await repo.LoadRestartSnapshotAsync(rollingStateId, ct)
                ?? throw new LongHorizonReadStateCorruptException($"No durable rolling state exists for plan {rollingStateId}.");

            var retryCheckpointDate = DateOnly.FromDateTime(DateTime.UtcNow);
            // Server-derived, never caller-supplied. Deterministically differs
            // from the block's own fingerprint once RetryCheckpointDate
            // genuinely advances (the repository separately enforces that this
            // date is strictly later than the last checkpoint date) -- the same
            // "evidence identity includes checkpoint date" convention the
            // existing internal LongHorizonBlockedActivationRetryService/
            // LongHorizonFullDarkLifecycleHarness.EvidenceIdentity already use.
            var retryFingerprint = $"{lastBlock.EvidenceFingerprint}|retry:{retryCheckpointDate:yyyy-MM-dd}";

            var persistResult = await new LongHorizonRollingRetryPersistenceAdapter(repo).PersistRetryAsync(
                rollingStateId, snapshot.ConcurrencyVersion, lastBlock.BlockedGlobalWeekStart, lastBlock.BlockedGlobalWeekEnd,
                retryCheckpointDate, retryFingerprint, relatedDecisionId: lastBlock.Id, cancellationToken: ct);

            switch (persistResult.Outcome)
            {
                case LongHorizonRollingPersistenceOutcome.Success:
                case LongHorizonRollingPersistenceOutcome.IdempotentReplay:
                    await tx.CommitAsync(ct);
                    return await BuildResponseAsync(planId, lockedPlan, rollingStateId,
                        lastBlock.BlockedGlobalWeekStart, lastBlock.BlockedGlobalWeekEnd,
                        persistResult.Outcome == LongHorizonRollingPersistenceOutcome.IdempotentReplay
                            ? LongHorizonRetryOutcome.IdempotentReplay
                            : LongHorizonRetryOutcome.RestoredToPending, ct);
                case LongHorizonRollingPersistenceOutcome.ConcurrencyConflict:
                    await tx.RollbackAsync(ct);
                    throw new LongHorizonContinuationConcurrencyConflictException("A concurrent Long-Horizon retry won; reload and retry.");
                default:
                    await tx.RollbackAsync(ct);
                    throw new LongHorizonRetryNotEligibleException(persistResult.FailureReason ?? "Retry is not currently eligible.");
            }
        }
        catch
        {
            try { await tx.RollbackAsync(ct); } catch { /* transaction already completed on this path */ }
            throw;
        }
    }

    private async Task<LongHorizonRetryContinuationResponse> BuildResponseAsync(
        Guid planId, TrainingPlan planRow, Guid rollingStateId, int restoredStart, int restoredEnd,
        LongHorizonRetryOutcome outcome, CancellationToken ct)
    {
        var fresh = await _db.LongHorizonRollingPlanStates.AsNoTracking()
            .Include(p => p.Weeks).ThenInclude(w => w.Sessions)
            .SingleAsync(p => p.Id == rollingStateId, ct);
        var windowSessions = fresh.Weeks
            .Where(w => w.GlobalWeek >= fresh.CurrentWindowStartWeek && w.GlobalWeek <= fresh.CurrentWindowEndWeek)
            .SelectMany(w => w.Sessions).ToList();
        var readiness = LongHorizonActiveReadModelProvider.Readiness(fresh, windowSessions);
        var nextPending = fresh.Weeks.Where(w => w.LifecycleState == LongHorizonPersistedLifecycleState.NumericPending)
            .OrderBy(w => w.GlobalWeek).FirstOrDefault();

        _logger.LogInformation("Long-Horizon retry {Outcome}. PlanId={PlanId} RestoredBoundary={Start}-{End}",
            outcome, planId, restoredStart, restoredEnd);

        return new LongHorizonRetryContinuationResponse
        {
            PlanId = planId,
            Outcome = outcome,
            RestoredWindowRange = new LongHorizonWindowRange { StartGlobalWeek = restoredStart, EndGlobalWeek = restoredEnd },
            CurrentWindowRange = new LongHorizonWindowRange { StartGlobalWeek = fresh.CurrentWindowStartWeek, EndGlobalWeek = fresh.CurrentWindowEndWeek },
            NextPendingGlobalWeek = nextPending?.GlobalWeek,
            CheckpointReadiness = readiness,
            PlanStatus = planRow.Status.ToString(),
            RetriedAtUtc = DateTime.UtcNow,
            PublicMessage = outcome == LongHorizonRetryOutcome.IdempotentReplay
                ? "long_horizon.retry_already_restored"
                : "long_horizon.retry_restored_to_pending",
        };
    }
}
