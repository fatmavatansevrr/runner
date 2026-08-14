using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.Application.Services;

internal enum LongHorizonMutationFailpoint
{
    AfterOutcomeBeforeSave,
    BeforeCommit,
    AfterCommitBeforeAcknowledgement
}

internal interface ILongHorizonMutationFailureInjector { void MaybeThrow(LongHorizonMutationFailpoint point); }
internal sealed class NoOpLongHorizonMutationFailureInjector : ILongHorizonMutationFailureInjector
{
    internal static readonly NoOpLongHorizonMutationFailureInjector Instance = new();
    public void MaybeThrow(LongHorizonMutationFailpoint point) { }
}
internal sealed class LongHorizonTestMutationFailureInjector(LongHorizonMutationFailpoint failpoint) : ILongHorizonMutationFailureInjector
{
    public void MaybeThrow(LongHorizonMutationFailpoint point) { if (point == failpoint) throw new LongHorizonInjectedMutationFailureException(point); }
}
internal sealed class LongHorizonInjectedMutationFailureException(LongHorizonMutationFailpoint point) : Exception($"Injected rolling mutation failure at {point}.");

public sealed class LongHorizonRollingSessionMutationService : ILongHorizonRollingSessionMutationService
{
    private const int ContractVersion = 1;
    private static readonly HashSet<string> AllowedNotTodayReasons = new(StringComparer.OrdinalIgnoreCase)
    { "fatigue", "soreness", "illness", "schedule", "weather", "other" };

    private readonly AppDbContext _db;
    private readonly ILogger<LongHorizonRollingSessionMutationService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILongHorizonMutationFailureInjector _failureInjector;

    public LongHorizonRollingSessionMutationService(AppDbContext db, ILogger<LongHorizonRollingSessionMutationService> logger, ILoggerFactory loggerFactory)
        : this(db, logger, loggerFactory, NoOpLongHorizonMutationFailureInjector.Instance)
    {
    }

    internal LongHorizonRollingSessionMutationService(AppDbContext db, ILogger<LongHorizonRollingSessionMutationService> logger,
        ILoggerFactory loggerFactory, ILongHorizonMutationFailureInjector failureInjector)
    {
        _db = db;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _failureInjector = failureInjector;
    }

    public async Task<LongHorizonSessionMutationResponse> CompleteAsync(Guid userId, Guid sessionId, LongHorizonCompleteSessionRequest request, CancellationToken ct = default)
    {
        if (request.ContractVersion != ContractVersion) throw new LongHorizonRollingMutationVersionUnsupportedException("Unsupported rolling completion contract version.");
        if (!double.IsFinite(request.ActualDistanceKm) || request.ActualDistanceKm <= 0 || request.ActualDurationMinutes <= 0)
            throw new ArgumentException("Actual distance and duration must be positive.");
        _logger.LogInformation("Rolling completion requested. UserId={UserId} SessionId={SessionId}", userId, sessionId);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var owned = await LoadOwnedAsync(userId, sessionId, ct);
            var session = owned.Session;
            LongHorizonActiveReadModelProvider.EnsureExecutable(session);
            EnsureNotSuperseded(session);
            if (session.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Completed)
            {
                if (SameCompletion(session, request))
                {
                    await tx.RollbackAsync(ct);
                    return Response(owned.PlanId, session, LongHorizonSessionMutationOutcome.IdempotentReplay, owned.Aggregate);
                }
                throw new LongHorizonRollingSessionCompletionConflictException("The rolling session is already completed with different actual values.");
            }
            if (session.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.NotToday)
                throw new LongHorizonRollingSessionOutcomeConflictException("A not-today outcome already owns this rolling session.");

            session.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.Completed;
            session.ActualDistanceKm = request.ActualDistanceKm;
            session.ActualDurationMinutes = request.ActualDurationMinutes;
            session.CompletedAtUtc = DateTime.UtcNow;
            session.OutcomeVersion++;
            await FinalizeWeekIfTerminalAsync(session, ct);
            _failureInjector.MaybeThrow(LongHorizonMutationFailpoint.AfterOutcomeBeforeSave);
            await _db.SaveChangesAsync(ct);
            _failureInjector.MaybeThrow(LongHorizonMutationFailpoint.BeforeCommit);
            await tx.CommitAsync(ct);
            _failureInjector.MaybeThrow(LongHorizonMutationFailpoint.AfterCommitBeforeAcknowledgement);
            _logger.LogInformation("Rolling completion succeeded. UserId={UserId} PlanId={PlanId} SessionId={SessionId} GlobalWeek={GlobalWeek} OutcomeVersion={OutcomeVersion}",
                userId, owned.PlanId, session.Id, session.Week.GlobalWeek, session.OutcomeVersion);
            return Response(owned.PlanId, session, LongHorizonSessionMutationOutcome.Completed, owned.Aggregate);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            _db.ChangeTracker.Clear();
            var current = await LoadOwnedAsync(userId, sessionId, ct);
            if (current.Session.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Completed && SameCompletion(current.Session, request))
                return Response(current.PlanId, current.Session, LongHorizonSessionMutationOutcome.IdempotentReplay, current.Aggregate);
            throw new LongHorizonRollingMutationConcurrencyConflictException("A concurrent rolling-session outcome won; reload the session.");
        }
    }

    public async Task<LongHorizonSessionMutationResponse> MarkNotTodayAsync(Guid userId, Guid sessionId, LongHorizonNotTodayRequest request, CancellationToken ct = default)
    {
        if (request.ContractVersion != ContractVersion) throw new LongHorizonRollingMutationVersionUnsupportedException("Unsupported rolling not-today contract version.");
        var reason = request.Reason?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(reason) || !AllowedNotTodayReasons.Contains(reason))
            throw new ArgumentException("Not-today reason must be one of fatigue, soreness, illness, schedule, weather, or other.");
        _logger.LogInformation("Rolling not-today requested. UserId={UserId} SessionId={SessionId}", userId, sessionId);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var owned = await LoadOwnedAsync(userId, sessionId, ct);
            var session = owned.Session;
            LongHorizonActiveReadModelProvider.EnsureExecutable(session);
            EnsureNotSuperseded(session);
            if (session.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.NotToday)
            {
                if (string.Equals(session.NotTodayReason, reason, StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync(ct);
                    // Phase 4M.3: still run adaptation orchestration on an exact
                    // replay -- ScheduleRepairPersistenceService is itself
                    // idempotent on TriggerSessionId, so this is both the
                    // duplicate-submission replay path AND the recovery path if
                    // a prior attempt's adaptation transaction failed after this
                    // NotToday transaction had already committed (Phase 4M.3 doc §27).
                    return await RunAdaptationAndRespondAsync(owned.PlanId, session, owned.Aggregate, LongHorizonSessionMutationOutcome.IdempotentReplay, ct);
                }
                throw new LongHorizonRollingSessionOutcomeConflictException("The rolling session already has a different not-today outcome.");
            }
            if (session.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Completed)
                throw new LongHorizonRollingSessionOutcomeConflictException("A completion outcome already owns this rolling session.");

            session.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.NotToday;
            session.NotTodayReason = reason;
            session.NotTodayRecordedAtUtc = DateTime.UtcNow;
            session.OutcomeVersion++;
            await FinalizeWeekIfTerminalAsync(session, ct);
            _failureInjector.MaybeThrow(LongHorizonMutationFailpoint.AfterOutcomeBeforeSave);
            await _db.SaveChangesAsync(ct);
            _failureInjector.MaybeThrow(LongHorizonMutationFailpoint.BeforeCommit);
            await tx.CommitAsync(ct);
            _failureInjector.MaybeThrow(LongHorizonMutationFailpoint.AfterCommitBeforeAcknowledgement);
            _logger.LogInformation("Rolling not-today succeeded. UserId={UserId} PlanId={PlanId} SessionId={SessionId} GlobalWeek={GlobalWeek} OutcomeVersion={OutcomeVersion}",
                userId, owned.PlanId, session.Id, session.Week.GlobalWeek, session.OutcomeVersion);
            // Phase 4M.3: the NotToday mutation above is already committed and
            // durable at this point -- schedule-repair orchestration runs in
            // its own separate transaction (ScheduleRepairPersistenceService
            // owns that boundary). See Phase 4M.3 doc §27 for the exact
            // recovery semantics if this second step fails.
            return await RunAdaptationAndRespondAsync(owned.PlanId, session, owned.Aggregate, LongHorizonSessionMutationOutcome.NotToday, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            _db.ChangeTracker.Clear();
            var current = await LoadOwnedAsync(userId, sessionId, ct);
            if (current.Session.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.NotToday && string.Equals(current.Session.NotTodayReason, reason, StringComparison.OrdinalIgnoreCase))
                return await RunAdaptationAndRespondAsync(current.PlanId, current.Session, current.Aggregate, LongHorizonSessionMutationOutcome.IdempotentReplay, ct);
            throw new LongHorizonRollingMutationConcurrencyConflictException("A concurrent rolling-session outcome won; reload the session.");
        }
    }

    /// <summary>Phase 4M.3 -- runs the live schedule-repair orchestration
    /// (Rev3.1 §3/§4.1) against an already-committed-or-replayed NotToday
    /// trigger and merges its outcome into the existing mutation response
    /// shape. Never runs for Complete.</summary>
    private async Task<LongHorizonSessionMutationResponse> RunAdaptationAndRespondAsync(
        Guid planId, LongHorizonRollingSessionState session, LongHorizonRollingPlanState aggregate,
        LongHorizonSessionMutationOutcome outcome, CancellationToken ct)
    {
        var repair = await ScheduleRepairRuntimeOrchestrator.RunAsync(_db, _loggerFactory, session, ct);
        var response = Response(planId, session, outcome, aggregate);
        return response with
        {
            ScheduleRepairAction = repair.Action switch
            {
                LongHorizonScheduleRepairActionKind.Skip => LongHorizonScheduleRepairAction.Skip,
                LongHorizonScheduleRepairActionKind.RescheduleToEmptySlot => LongHorizonScheduleRepairAction.RescheduleToEmptySlot,
                LongHorizonScheduleRepairActionKind.SubstituteFutureEasy => LongHorizonScheduleRepairAction.SubstituteFutureEasy,
                _ => throw new ArgumentOutOfRangeException(nameof(repair), repair.Action, "Unrecognized schedule-repair action."),
            },
            ScheduleRepairSafetyFlag = repair.SafetyFlag,
            ScheduleRepairReplacementSessionId = repair.ReplacementSessionId,
            ScheduleRepairReplacementDate = repair.ReplacementDate,
            ScheduleRepairIsIdempotentReplay = repair.IsIdempotentReplay,
        };
    }

    private static void EnsureNotSuperseded(LongHorizonRollingSessionState session)
    {
        if (session.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Superseded)
            throw new LongHorizonRollingSessionSupersededException("This rolling session has been superseded by a schedule-repair replacement and can no longer be mutated.");
    }

    private async Task<(LongHorizonRollingSessionState Session, LongHorizonRollingPlanState Aggregate, Guid PlanId)> LoadOwnedAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await _db.LongHorizonRollingSessionStates
            .Include(s => s.Week).ThenInclude(w => w.Plan).ThenInclude(p => p.Weeks).ThenInclude(w => w.Sessions)
            .SingleOrDefaultAsync(s => s.Id == sessionId && _db.TrainingPlans.Any(p =>
                p.InternalUserId == userId && p.Status == TrainingPlanStatus.Active && p.ScheduleStrategy == PlanScheduleStrategy.RollingLongHorizon
                && p.LongHorizonRollingPlanStateId == s.Week.PlanStateId), ct);
        if (session is null) throw new LongHorizonRollingSessionNotFoundException("Rolling session not found.");
        var planId = await _db.TrainingPlans.Where(p => p.InternalUserId == userId && p.Status == TrainingPlanStatus.Active
            && p.LongHorizonRollingPlanStateId == session.Week.PlanStateId).Select(p => p.Id).SingleOrDefaultAsync(ct);
        if (planId == Guid.Empty) throw new LongHorizonRollingSessionNotFoundException("Rolling session not found.");
        // Serialize outcome mutation with cancellation through the real plan
        // ownership row. If cancellation committed first, the locked row is
        // observed as non-active and mutation fails; if this lock wins,
        // cancellation waits until this atomic outcome commits.
        var lockedPlan = await _db.TrainingPlans
            .FromSqlInterpolated($"SELECT * FROM \"TrainingPlans\" WHERE \"Id\" = {planId} FOR UPDATE")
            .AsNoTracking().SingleAsync(ct);
        if (lockedPlan.Status != TrainingPlanStatus.Active)
            throw new LongHorizonRollingSessionNotFoundException("Rolling session not found.");
        return (session, session.Week.Plan, planId);
    }

    private async Task FinalizeWeekIfTerminalAsync(LongHorizonRollingSessionState changed, CancellationToken ct)
    {
        var siblings = await _db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == changed.WeekStateId && s.Id != changed.Id).ToListAsync(ct);
        if (siblings.All(s => s.OutcomeStatus != LongHorizonRollingSessionOutcomeStatus.Planned))
        {
            changed.Week.LifecycleState = LongHorizonPersistedLifecycleState.Completed;
            changed.Week.CompletedAtUtc ??= DateTime.UtcNow;
            var evidence = LongHorizonRollingOutcomeEvidenceAdapter.ToCheckpointRows(siblings.Append(changed));
            _logger.LogInformation("Rolling checkpoint evidence ready. PlanStateId={PlanStateId} GlobalWeek={GlobalWeek} EvidenceRows={EvidenceRows}",
                changed.Week.PlanStateId, changed.Week.GlobalWeek, evidence.Count);
        }
    }

    private static bool SameCompletion(LongHorizonRollingSessionState session, LongHorizonCompleteSessionRequest request) =>
        Math.Abs((session.ActualDistanceKm ?? -1) - request.ActualDistanceKm) < 0.000001 && session.ActualDurationMinutes == request.ActualDurationMinutes;

    private static LongHorizonSessionMutationResponse Response(Guid planId, LongHorizonRollingSessionState session,
        LongHorizonSessionMutationOutcome outcome, LongHorizonRollingPlanState aggregate) => new()
    {
        SessionId = session.Id, PlanId = planId, Outcome = outcome, OutcomeVersion = session.OutcomeVersion,
        CheckpointReadiness = LongHorizonActiveReadModelProvider.Readiness(aggregate,
            aggregate.Weeks.Where(w => w.GlobalWeek >= aggregate.CurrentWindowStartWeek && w.GlobalWeek <= aggregate.CurrentWindowEndWeek).SelectMany(w => w.Sessions).ToList()),
        NextWindowActivated = false
    };
}

internal static class LongHorizonRollingOutcomeEvidenceAdapter
{
    internal static IReadOnlyList<LongHorizonTrainingDayEvidenceRow> ToCheckpointRows(IEnumerable<LongHorizonRollingSessionState> sessions) => sessions
        .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal)
        .Select(s => new LongHorizonTrainingDayEvidenceRow(s.Week.GlobalWeek, new TrainingDay
        {
            Id = s.Id,
            Date = DateTime.SpecifyKind(s.AssignedDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            DayType = LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole) ? TrainingDayType.LongRun : TrainingDayType.Easy,
            Status = s.OutcomeStatus switch
            {
                LongHorizonRollingSessionOutcomeStatus.Completed => TrainingDayStatus.Completed,
                LongHorizonRollingSessionOutcomeStatus.NotToday => TrainingDayStatus.Skipped,
                _ => TrainingDayStatus.Planned
            },
            PlannedDistanceKm = s.DistanceKm,
            ActualDistanceKm = s.ActualDistanceKm,
            ActualDurationMin = s.ActualDurationMinutes,
            IsLongRun = LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole)
        })).ToList();
}
