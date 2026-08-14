using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.Application.Services;

public sealed class LongHorizonActiveReadModelProvider : ILongHorizonActiveReadModelProvider
{
    private readonly AppDbContext _db;
    private readonly ILogger<LongHorizonActiveReadModelProvider> _logger;

    public LongHorizonActiveReadModelProvider(AppDbContext db, ILogger<LongHorizonActiveReadModelProvider> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<LongHorizonHomeResponse> GetHomeAsync(Guid userId, Guid planId, CancellationToken ct = default)
    {
        var aggregate = await LoadAggregateAsync(userId, planId, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sessions = ActiveSessions(aggregate).OrderBy(s => s.AssignedDate).ThenBy(s => s.SessionOrdinal).ToList();
        var windowSessions = sessions.Where(s => s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek
            && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek).ToList();
        // Phase 4L.4C: only queried when Blocked, so the common-case read stays a single round trip.
        LongHorizonBlockRetryRecord? lastBlock = aggregate.CurrentLifecycleStatus == LongHorizonPersistedLifecycleState.NumericActivationBlocked
            ? await _db.LongHorizonBlockRetryRecords.AsNoTracking()
                .Where(b => b.PlanStateId == aggregate.Id && b.EventType == LongHorizonPersistedBlockRetryEventType.Block)
                .OrderByDescending(b => b.CreatedAtUtc).FirstOrDefaultAsync(ct)
            : null;
        var summary = Summary(planId, aggregate, sessions, lastBlock);
        _logger.LogInformation("Rolling Home read. UserId={UserId} PlanId={PlanId} CurrentWeek={CurrentWeek} Readiness={Readiness}",
            userId, planId, summary.CurrentGlobalWeek, summary.CheckpointReadiness);
        return new LongHorizonHomeResponse
        {
            ActivePlan = summary,
            TodayWorkout = sessions.Where(s => s.AssignedDate == today).Select(s => Map(planId, s)).FirstOrDefault(),
            NextExecutableWorkout = sessions.Where(s => s.AssignedDate > today && s.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Planned)
                .Select(s => Map(planId, s)).FirstOrDefault(),
            CurrentWindowSessions = windowSessions.Select(s => Map(planId, s)).ToList(),
            HasPendingConfirmations = false
        };
    }

    public async Task<LongHorizonCalendarResponse> GetCalendarAsync(Guid userId, Guid planId, string month, CancellationToken ct = default)
    {
        if (!DateOnly.TryParseExact($"{month}-01", "yyyy-MM-dd", out var start))
            throw new ArgumentException("Invalid month format. Expected YYYY-MM.");
        var end = start.AddMonths(1).AddDays(-1);
        var aggregate = await LoadAggregateAsync(userId, planId, ct);
        var entries = ActiveSessions(aggregate)
            .Where(s => s.AssignedDate >= start && s.AssignedDate <= end)
            .OrderBy(s => s.AssignedDate).ThenBy(s => s.SessionOrdinal)
            .Select(s => Map(planId, s)).ToList();
        _logger.LogInformation("Rolling Calendar read. UserId={UserId} PlanId={PlanId} Month={Month} EntryCount={EntryCount}", userId, planId, month, entries.Count);
        return new LongHorizonCalendarResponse { PlanId = planId, Month = month, Sessions = entries };
    }

    public async Task<LongHorizonRollingSessionDetailResponse> GetSessionDetailAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var row = await _db.LongHorizonRollingSessionStates.AsNoTracking()
            .Include(s => s.Week).ThenInclude(w => w.Plan)
            .Where(s => s.Id == sessionId)
            .Join(_db.TrainingPlans.AsNoTracking().Where(p => p.InternalUserId == userId && p.Status == TrainingPlanStatus.Active && p.ScheduleStrategy == PlanScheduleStrategy.RollingLongHorizon),
                s => s.Week.PlanStateId, p => p.LongHorizonRollingPlanStateId, (s, p) => new { Session = s, PlanId = p.Id })
            .SingleOrDefaultAsync(ct);
        if (row is null) throw new LongHorizonRollingSessionNotFoundException("Rolling session not found.");
        EnsureExecutable(row.Session);
        _logger.LogInformation("Rolling session detail read. UserId={UserId} PlanId={PlanId} SessionId={SessionId} GlobalWeek={GlobalWeek}", userId, row.PlanId, sessionId, row.Session.Week.GlobalWeek);
        return new LongHorizonRollingSessionDetailResponse
        {
            Session = Map(row.PlanId, row.Session),
            PublicDescription = PublicDescription(row.Session.SessionRole)
        };
    }

    internal static LongHorizonRollingSessionResponse Map(Guid planId, LongHorizonRollingSessionState session)
    {
        double? actualPace = session.ActualDistanceKm is > 0 && session.ActualDurationMinutes.HasValue
            ? session.ActualDurationMinutes.Value / session.ActualDistanceKm.Value : null;
        return new LongHorizonRollingSessionResponse
        {
            SessionId = session.Id, PlanId = planId, GlobalWeek = session.Week.GlobalWeek,
            Phase = Segment(session.Week.SegmentType), Stage = session.Week.Stage,
            AssignedDate = session.AssignedDate, WorkoutRole = session.SessionRole,
            WorkoutKey = session.WorkoutKey, WorkoutVersion = session.WorkoutVersion,
            PlannedDistanceKm = session.DistanceKm, Outcome = Outcome(session.OutcomeStatus),
            ActualDistanceKm = session.ActualDistanceKm, ActualDurationMinutes = session.ActualDurationMinutes,
            ActualPaceMinutesPerKm = actualPace, CompletedAtUtc = session.CompletedAtUtc,
            NotTodayReasonCategory = session.NotTodayReason, NotTodayRecordedAtUtc = session.NotTodayRecordedAtUtc,
            IsLongRun = LongHorizonSessionRoleCodec.IsLongRun(session.SessionRole),
            // Phase 4M.3: a Superseded session must never masquerade as a
            // normal actionable Planned session on any read surface, including
            // the direct-by-id detail lookup (which, unlike Home/Calendar,
            // intentionally still returns the row itself for provenance).
            MutationAllowed = session.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Planned
                && session.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Active,
            PublicProvenance = PublicProvenance(session.Provenance)
        };
    }

    internal static LongHorizonCheckpointReadiness Readiness(LongHorizonRollingPlanState aggregate, IReadOnlyCollection<LongHorizonRollingSessionState> sessions)
    {
        if (aggregate.CurrentLifecycleStatus == LongHorizonPersistedLifecycleState.Completed)
            return LongHorizonCheckpointReadiness.TerminalPlanComplete;
        if (aggregate.CurrentBlockedPublicReasonCategory is not null || aggregate.Weeks.Any(w => w.LifecycleState == LongHorizonPersistedLifecycleState.NumericActivationBlocked))
            return LongHorizonCheckpointReadiness.ReassessmentRequired;
        if (sessions.Count == 0 || sessions.Any(s => s.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Planned))
            return LongHorizonCheckpointReadiness.CurrentWindowInProgress;
        return aggregate.CurrentWindowEndWeek >= aggregate.TotalWeeks
            ? LongHorizonCheckpointReadiness.TerminalPlanComplete
            : LongHorizonCheckpointReadiness.NextWindowActivationReady;
    }

    internal static void EnsureExecutable(LongHorizonRollingSessionState session)
    {
        if (session.Week.LifecycleState is not (LongHorizonPersistedLifecycleState.NumericActivated or LongHorizonPersistedLifecycleState.Completed))
            throw new LongHorizonRollingSessionNotExecutableException("Rolling session is not executable.");
    }

    private async Task<LongHorizonRollingPlanState> LoadAggregateAsync(Guid userId, Guid planId, CancellationToken ct)
    {
        var rollingId = await _db.TrainingPlans.AsNoTracking()
            .Where(p => p.Id == planId && p.InternalUserId == userId && p.Status == TrainingPlanStatus.Active && p.ScheduleStrategy == PlanScheduleStrategy.RollingLongHorizon)
            .Select(p => p.LongHorizonRollingPlanStateId).SingleOrDefaultAsync(ct);
        if (!rollingId.HasValue) throw new LongHorizonReadStateNotFoundException("Active rolling plan not found.");
        var aggregate = await _db.LongHorizonRollingPlanStates.AsNoTracking()
            .Include(p => p.Weeks).ThenInclude(w => w.Sessions)
            .SingleOrDefaultAsync(p => p.Id == rollingId.Value, ct);
        return aggregate ?? throw new LongHorizonReadStateCorruptException("Rolling plan state is unavailable.");
    }

    private static List<LongHorizonRollingSessionState> ActiveSessions(LongHorizonRollingPlanState aggregate) => aggregate.Weeks
        .Where(w => w.LifecycleState is LongHorizonPersistedLifecycleState.NumericActivated or LongHorizonPersistedLifecycleState.Completed)
        .SelectMany(w => w.Sessions)
        // Phase 4M.3: a Superseded session is not actionable (Rev3.1) -- Home
        // and Calendar must never present it as a live workout. Its row
        // remains queryable directly (GetSessionDetailAsync) for provenance;
        // only these list surfaces filter it out.
        .Where(s => s.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Active)
        .ToList();

    private static LongHorizonActivePlanSummaryResponse Summary(Guid planId, LongHorizonRollingPlanState aggregate,
        IReadOnlyCollection<LongHorizonRollingSessionState> sessions, LongHorizonBlockRetryRecord? lastBlock = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var week = aggregate.Weeks.OrderBy(w => w.GlobalWeek).LastOrDefault(w => w.StructuralStartDate <= today)
            ?? aggregate.Weeks.OrderBy(w => w.GlobalWeek).First();
        var nextPending = aggregate.Weeks.Where(w => w.LifecycleState == LongHorizonPersistedLifecycleState.NumericPending).OrderBy(w => w.GlobalWeek).FirstOrDefault();
        var readiness = Readiness(aggregate, sessions);
        LongHorizonRecoveryRequirement? recoveryRequirement = readiness == LongHorizonCheckpointReadiness.ReassessmentRequired && lastBlock is not null
            ? RecoveryRequirement(lastBlock.InternalReasonCode) : null;
        return new LongHorizonActivePlanSummaryResponse
        {
            PlanId = planId, GoalType = Snake(aggregate.GoalType), GoalDistance = Snake(aggregate.GoalDistance), Level = Snake(aggregate.Level),
            TotalWeeks = aggregate.TotalWeeks, CurrentGlobalWeek = week.GlobalWeek, CurrentPhase = Segment(week.SegmentType), CurrentStage = week.Stage,
            CurrentWindowStartWeek = aggregate.CurrentWindowStartWeek, CurrentWindowEndWeek = aggregate.CurrentWindowEndWeek,
            NextPendingGlobalWeek = nextPending?.GlobalWeek, ActivatedSessionCount = sessions.Count,
            TerminalSessionCount = sessions.Count(s => s.OutcomeStatus != LongHorizonRollingSessionOutcomeStatus.Planned),
            CheckpointReadiness = readiness, RecoveryRequirement = recoveryRequirement,
            BlockedPublicReasonCategory = readiness == LongHorizonCheckpointReadiness.ReassessmentRequired ? aggregate.CurrentBlockedPublicReasonCategory : null,
            Status = aggregate.CurrentLifecycleStatus.ToString(), PublicMessage = PublicMessage(readiness)
        };
    }

    /// <summary>Phase 4L.4C -- maps the internal recovery-authority classification onto the public enum.</summary>
    internal static LongHorizonRecoveryRequirement RecoveryRequirement(string internalReasonCode) =>
        LongHorizonBlockRecoveryClassification.Classify(internalReasonCode) switch
        {
            LongHorizonBlockRecoveryClass.RecoverableWithElapsedCalendarTime => LongHorizonRecoveryRequirement.CalendarWindowPending,
            LongHorizonBlockRecoveryClass.OperationalSupportRequired => LongHorizonRecoveryRequirement.OperationalSupportRequired,
            _ => LongHorizonRecoveryRequirement.RegeneratePreviewRequired,
        };

    private static string PublicDescription(string role) => LongHorizonSessionRoleCodec.IsLongRun(role)
        ? "A steady endurance-focused long run." : "Complete the assigned session at the prescribed effort.";
    private static string PublicProvenance(string value) => value.Contains("checkpoint", StringComparison.OrdinalIgnoreCase)
        ? "updated_after_completed_training" : "generated_from_initial_profile";
    private static string Segment(LongHorizonPersistedSegmentType value) => value switch
    {
        LongHorizonPersistedSegmentType.GeneralEndurance => "general_endurance",
        LongHorizonPersistedSegmentType.PreparationRunway => "preparation_runway",
        _ => "core"
    };
    private static string Outcome(LongHorizonRollingSessionOutcomeStatus value) => value switch
    {
        LongHorizonRollingSessionOutcomeStatus.NotToday => "not_today",
        LongHorizonRollingSessionOutcomeStatus.Completed => "completed",
        _ => "planned"
    };
    private static string Snake(string value) => string.Concat(value.Select((character, index) =>
        index > 0 && char.IsUpper(character) ? "_" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));
    private static string PublicMessage(LongHorizonCheckpointReadiness value) => value switch
    {
        LongHorizonCheckpointReadiness.NextWindowActivationReady => "long_horizon.next_window_ready",
        LongHorizonCheckpointReadiness.ReassessmentRequired => "long_horizon.reassessment_required",
        LongHorizonCheckpointReadiness.TerminalPlanComplete => "long_horizon.complete",
        _ => "long_horizon.current_window_in_progress"
    };
}
