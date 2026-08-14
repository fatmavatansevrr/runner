using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.2 -- the transactional materialization boundary for an
/// already-resolved Phase 4M.1 <see cref="ScheduleRepairDecision"/>. Never
/// reimplements <see cref="ScheduleRepairPolicy"/> -- it only proves the
/// already-decided action is still safe to commit against current
/// persisted state, and persists it atomically. See this type's own phase
/// document (PHASE4M_2_...) for the full architecture audit.
///
/// Concurrency strategy (Phase 4M.2 §I): a fast idempotency check runs
/// before taking any lock; the authoritative check-and-mutate sequence
/// then runs inside one transaction that row-locks the owning
/// <c>LongHorizonRollingPlanState</c> (mirroring the exact
/// <c>FOR UPDATE</c> pattern already established by
/// <c>LongHorizonRollingWindowActivationService</c> and
/// <c>LongHorizonRollingSessionMutationService</c>), so two concurrent
/// attempts for the same trigger serialize on that lock rather than racing.
/// The database-level unique index on
/// <c>LongHorizonAdaptationDecisionRecord.TriggerSessionId</c> is the final,
/// independent backstop: even if the row lock strategy were bypassed, a
/// duplicate INSERT still fails at the database, never silently succeeds.
/// </summary>
internal sealed class ScheduleRepairPersistenceService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ScheduleRepairPersistenceService> _logger;

    public ScheduleRepairPersistenceService(AppDbContext db, ILogger<ScheduleRepairPersistenceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AdaptationPersistenceResult> PersistAsync(ScheduleRepairPersistenceRequest request, CancellationToken ct = default)
    {
        // Fast path: no lock needed to observe an already-committed decision.
        var precheck = await FindExistingAsync(request.Trigger.SessionId, ct);
        if (precheck is not null)
            return ReplayResult(precheck);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Row-lock the owning plan aggregate -- serializes concurrent
            // adaptation attempts within this plan, matching the repository's
            // own established FOR UPDATE convention.
            // LongHorizonRollingPlanState maps the Postgres system column
            // "xmin" as a concurrency-token shadow property (AppDbContext),
            // so a raw-SQL projection into that entity type must select it
            // explicitly -- "SELECT *" alone does not include system columns.
            await _db.LongHorizonRollingPlanStates
                .FromSqlInterpolated($"SELECT *, xmin FROM \"LongHorizonRollingPlanStates\" WHERE \"Id\" = {request.PlanStateId} FOR UPDATE")
                .AsNoTracking().SingleAsync(ct);

            // Re-check idempotency now that the lock is held: a concurrent
            // committer may have finished between the pre-check and this lock.
            var existing = await FindExistingAsync(request.Trigger.SessionId, ct);
            if (existing is not null)
            {
                await tx.CommitAsync(ct); // read-only under the lock; nothing to roll back
                return ReplayResult(existing);
            }

            var triggerSession = await _db.LongHorizonRollingSessionStates
                .SingleOrDefaultAsync(s => s.Id == request.Trigger.SessionId, ct);
            if (triggerSession is null)
            {
                await tx.RollbackAsync(ct);
                return new AdaptationPersistenceResult(AdaptationPersistenceOutcome.IntegrityViolation, FailureReason: "Trigger session not found.");
            }
            if (triggerSession.OutcomeStatus != LongHorizonRollingSessionOutcomeStatus.NotToday)
            {
                await tx.RollbackAsync(ct);
                return new AdaptationPersistenceResult(AdaptationPersistenceOutcome.StaleTrigger, FailureReason: "Trigger session is no longer NotToday.");
            }

            Guid? replacementId = null;
            Guid? supersededId = null;

            switch (request.Decision.Action)
            {
                case ScheduleRepairAction.Skip:
                    break;

                case ScheduleRepairAction.RescheduleToEmptySlot:
                {
                    var outcome = await TryRescheduleToEmptySlotAsync(request, triggerSession, ct);
                    if (outcome.Result is { } failure)
                    {
                        await tx.RollbackAsync(ct);
                        return failure;
                    }
                    replacementId = outcome.ReplacementId;
                    break;
                }

                case ScheduleRepairAction.SubstituteFutureEasy:
                {
                    var outcome = await TrySubstituteFutureEasyAsync(request, triggerSession, ct);
                    if (outcome.Result is { } failure)
                    {
                        await tx.RollbackAsync(ct);
                        return failure;
                    }
                    replacementId = outcome.ReplacementId;
                    supersededId = outcome.SupersededId;
                    break;
                }

                default:
                    await tx.RollbackAsync(ct);
                    return new AdaptationPersistenceResult(AdaptationPersistenceOutcome.IntegrityViolation, FailureReason: "Unrecognized schedule-repair action.");
            }

            var record = new LongHorizonAdaptationDecisionRecord
            {
                Id = Guid.NewGuid(),
                PlanStateId = request.PlanStateId,
                SourceWindowStartWeek = request.SourceWindowStartWeek,
                SourceWindowEndWeek = request.SourceWindowEndWeek,
                TriggerSessionId = request.Trigger.SessionId,
                DecisionType = ToPersistedDecisionType(request.Decision.Action),
                SafetyReviewRequired = request.Decision.SafetyFlag,
                ReasonCode = request.Trigger.Reason.ToString(),
                ReplacementSessionId = replacementId,
                SupersededSessionId = supersededId,
                CreatedAtUtc = DateTime.UtcNow,
                ContractVersion = 1,
            };

            try
            {
                _db.LongHorizonAdaptationDecisionRecords.Add(record);
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
            {
                // Defensive backstop only: the plan-level row lock above
                // should already make this unreachable in practice, but the
                // DB uniqueness constraint is the real, independent
                // correctness guarantee -- never trust the lock alone.
                await tx.RollbackAsync(ct);
                _db.ChangeTracker.Clear();
                var winner = await FindExistingAsync(request.Trigger.SessionId, ct);
                return winner is not null
                    ? ReplayResult(winner)
                    : new AdaptationPersistenceResult(AdaptationPersistenceOutcome.ConcurrencyConflict, FailureReason: "Concurrent commit detected but authoritative result could not be resolved.");
            }

            await tx.CommitAsync(ct);
            _logger.LogInformation(
                "Adaptation schedule-repair committed. PlanId={PlanId} TriggerSessionId={TriggerSessionId} DecisionType={DecisionType} ReplacementSessionId={ReplacementSessionId} SupersededSessionId={SupersededSessionId} SafetyReviewRequired={SafetyReviewRequired}",
                request.PlanStateId, request.Trigger.SessionId, record.DecisionType, replacementId, supersededId, record.SafetyReviewRequired);
            return new AdaptationPersistenceResult(AdaptationPersistenceOutcome.Committed, record.Id, replacementId, supersededId);
        }
        catch
        {
            try { await tx.RollbackAsync(ct); } catch { /* transaction already completed on this path */ }
            throw;
        }
    }

    private async Task<(AdaptationPersistenceResult? Result, Guid? ReplacementId)> TryRescheduleToEmptySlotAsync(
        ScheduleRepairPersistenceRequest request, LongHorizonRollingSessionState triggerSession, CancellationToken ct)
    {
        if (request.TargetWeekStateId is not { } weekStateId || request.Decision.SelectedEmptySlotDate is not { } targetDate)
            return (new AdaptationPersistenceResult(AdaptationPersistenceOutcome.IntegrityViolation, FailureReason: "TargetWeekStateId and SelectedEmptySlotDate are required for RescheduleToEmptySlot."), null);

        // Revalidate at commit time: the slot must still be genuinely empty
        // (no row of any kind at that date in that week). 4M.2 never
        // re-selects a different candidate if this fails.
        var occupied = await _db.LongHorizonRollingSessionStates
            .AnyAsync(s => s.WeekStateId == weekStateId && s.AssignedDate == targetDate, ct);
        if (occupied)
            return (new AdaptationPersistenceResult(AdaptationPersistenceOutcome.StaleTarget, FailureReason: "The selected empty slot is no longer empty."), null);

        var ordinal = await NextOrdinalAsync(weekStateId, ct);
        var replacement = BuildReplacement(triggerSession, weekStateId, targetDate, ordinal);
        _db.LongHorizonRollingSessionStates.Add(replacement);
        return (null, replacement.Id);
    }

    private async Task<(AdaptationPersistenceResult? Result, Guid? ReplacementId, Guid? SupersededId)> TrySubstituteFutureEasyAsync(
        ScheduleRepairPersistenceRequest request, LongHorizonRollingSessionState triggerSession, CancellationToken ct)
    {
        if (request.Decision.SubstitutedEasySessionId is not { } targetId)
            return (new AdaptationPersistenceResult(AdaptationPersistenceOutcome.IntegrityViolation, FailureReason: "SubstitutedEasySessionId is required for SubstituteFutureEasy."), null, null);

        var target = await _db.LongHorizonRollingSessionStates.SingleOrDefaultAsync(s => s.Id == targetId, ct);
        if (target is null)
            return (new AdaptationPersistenceResult(AdaptationPersistenceOutcome.StaleTarget, FailureReason: "Substitution target session not found."), null, null);

        // Revalidate at commit time (Phase 4M.2 §Q): target must still be an
        // Active, Planned, EASY_SUPPORT session. Any other observed state
        // means the decision is stale -- reject, never silently reselect.
        if (target.PlanningStatus != LongHorizonPersistedSessionPlanningStatus.Active
            || target.OutcomeStatus != LongHorizonRollingSessionOutcomeStatus.Planned
            || !LongHorizonSessionRoleCodec.IsEasySupport(target.SessionRole))
        {
            return (new AdaptationPersistenceResult(AdaptationPersistenceOutcome.StaleTarget, FailureReason: "Substitution target is no longer an eligible Active/Planned EASY_SUPPORT session."), null, null);
        }

        target.PlanningStatus = LongHorizonPersistedSessionPlanningStatus.Superseded;

        var ordinal = await NextOrdinalAsync(target.WeekStateId, ct);
        var replacement = BuildReplacement(triggerSession, target.WeekStateId, target.AssignedDate, ordinal);
        _db.LongHorizonRollingSessionStates.Add(replacement);
        return (null, replacement.Id, target.Id);
    }

    /// <summary>Phase 4M.2 §L explicit copy map. Preserves the source
    /// (trigger) session's own load-bearing workout content unchanged --
    /// this is also what makes the Taper-KEY "moved unchanged" invariant
    /// (§M) hold structurally: no field here is ever derived from
    /// IsTaper, so a Taper KEY replacement is byte-for-byte identical
    /// content to its trigger except for identity/date/lineage/status.
    /// Execution-event fields (ActualDistanceKm, ActualDurationMinutes,
    /// CompletedAtUtc, NotTodayReason, NotTodayRecordedAtUtc) are
    /// deliberately never copied -- the replacement is a fresh Planned
    /// session, not a clone of the source's execution history.</summary>
    private static LongHorizonRollingSessionState BuildReplacement(
        LongHorizonRollingSessionState source, Guid weekStateId, DateOnly assignedDate, int sessionOrdinal) => new()
    {
        Id = Guid.NewGuid(),
        WeekStateId = weekStateId,
        SessionOrdinal = sessionOrdinal,
        SessionRole = source.SessionRole,
        WorkoutKey = source.WorkoutKey,
        WorkoutVersion = source.WorkoutVersion,
        DistanceKm = source.DistanceKm,
        AssignedDate = assignedDate,
        ActivationContextVersionSequence = source.ActivationContextVersionSequence,
        Provenance = source.Provenance,
        OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.Planned,
        PlanningStatus = LongHorizonPersistedSessionPlanningStatus.Active,
        AdaptedFromSessionId = source.Id,
        OutcomeVersion = 0,
    };

    private async Task<int> NextOrdinalAsync(Guid weekStateId, CancellationToken ct)
    {
        var max = await _db.LongHorizonRollingSessionStates
            .Where(s => s.WeekStateId == weekStateId)
            .Select(s => (int?)s.SessionOrdinal)
            .MaxAsync(ct);
        return (max ?? 0) + 1;
    }

    private async Task<LongHorizonAdaptationDecisionRecord?> FindExistingAsync(Guid triggerSessionId, CancellationToken ct) =>
        await _db.LongHorizonAdaptationDecisionRecords.AsNoTracking()
            .SingleOrDefaultAsync(r => r.TriggerSessionId == triggerSessionId, ct);

    private static AdaptationPersistenceResult ReplayResult(LongHorizonAdaptationDecisionRecord existing) =>
        new(AdaptationPersistenceOutcome.IdempotentReplay, existing.Id, existing.ReplacementSessionId, existing.SupersededSessionId);

    private static LongHorizonPersistedAdaptationDecisionType ToPersistedDecisionType(ScheduleRepairAction action) => action switch
    {
        ScheduleRepairAction.Skip => LongHorizonPersistedAdaptationDecisionType.Skip,
        ScheduleRepairAction.RescheduleToEmptySlot => LongHorizonPersistedAdaptationDecisionType.RescheduleToEmptySlot,
        ScheduleRepairAction.SubstituteFutureEasy => LongHorizonPersistedAdaptationDecisionType.SubstituteFutureEasy,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unrecognized schedule-repair action."),
    };
}
