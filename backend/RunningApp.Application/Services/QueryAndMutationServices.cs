// [ignoring loop detection]
using Microsoft.EntityFrameworkCore;
using RunningApp.Application.Adaptation;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Home;
using RunningApp.Application.DTOs.PendingConfirmation;
using RunningApp.Application.DTOs.Profile;
using RunningApp.Application.DTOs.TrainingDay;
using RunningApp.Application.Exceptions;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace RunningApp.Application.Services;

public class QueryAndMutationServices :
    IHomeQueryService,
    ICalendarQueryService,
    ITrainingDayService,
    IWorkoutCompletionService,
    INotTodayService,
    IPendingConfirmationService,
    IProfileService
{
    private readonly AppDbContext _context;
    private readonly IAdaptationEngine _adaptationEngine;
    private readonly ILogger<QueryAndMutationServices> _logger;
    private readonly ILongHorizonActiveReadModelProvider _rollingReadProvider;

    public QueryAndMutationServices(
        AppDbContext context, 
        IAdaptationEngine adaptationEngine,
        ILogger<QueryAndMutationServices> logger,
        ILongHorizonActiveReadModelProvider rollingReadProvider)
    {
        _context = context;
        _adaptationEngine = adaptationEngine;
        _logger = logger;
        _rollingReadProvider = rollingReadProvider;
    }

    // ─── HOME QUERY SERVICE ──────────────────────────────────────────────────
    public async Task<object> GetHomeAsync(Guid internalUserId, CancellationToken ct = default)
    {
        var plan = await _context.TrainingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.InternalUserId == internalUserId && p.Status == TrainingPlanStatus.Active, ct);

        var hasPending = await _context.PendingConfirmations
            .AnyAsync(p => p.InternalUserId == internalUserId && p.Status == "pending", ct);

        if (plan == null)
        {
            return new HomeResponse
            {
                ActivePlan = null,
                TodayWorkout = null,
                DailyTip = await GetDefaultTipAsync(ct),
                WeekSummary = new List<TrainingDayResponse>(),
                HasPendingConfirmations = hasPending
            };
        }

        if (plan.ScheduleStrategy == PlanScheduleStrategy.RollingLongHorizon)
            return await _rollingReadProvider.GetHomeAsync(internalUserId, plan.Id, ct);

        var today = DateTime.UtcNow.Date;

        // Fetch today's workout
        var todayDay = await _context.TrainingDays
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.PlanId == plan.Id && d.Date == today, ct);

        // Loaded once, reused for todayWorkout/weekSummary provenance mapping
        // below and for the plan-level current-week summary — avoids any
        // per-day N+1 TrainingWeek query.
        var weeksById = await _context.TrainingWeeks
            .AsNoTracking()
            .Where(w => w.PlanId == plan.Id)
            .ToDictionaryAsync(w => w.Id, ct);

        TrainingDayResponse todayWorkout;
        if (todayDay != null)
        {
            weeksById.TryGetValue(todayDay.WeekId, out var todayWeek);
            todayWorkout = MapToResponse(todayDay, todayWeek);
        }
        else
        {
            // Read-time synthetic rest day: not persisted, so DayId is null
            // rather than a fake/zero GUID.
            todayWorkout = new TrainingDayResponse
            {
                DayId = null,
                Date = today,
                DayType = TrainingDayType.Rest,
                Status = TrainingDayStatus.Planned,
                Title = "Rest Day",
                Description = "Recovery is part of progress. Rest up!",
                PlannedDistanceKm = 0,
                PlannedDurationMin = 0,
                CanMarkComplete = false,
                CanMarkNotToday = false
            };
        }

        // Determine the plan's current training week from real TrainingWeek
        // boundaries (StartDate), not a Monday-Sunday calendar week — plans
        // can start on any weekday. Reuses weeksById (already loaded above)
        // rather than a second TrainingWeeks query.
        var weeks = weeksById.Values.OrderBy(w => w.WeekNumber).ToList();

        var totalWeeks = weeks.Count;

        DateTime weekStart;
        DateTime weekEnd;
        int currentWeekNumber;
        TrainingWeek? currentWeek = null;
        if (weeks.Count > 0)
        {
            // Last week whose start is on/before today; if today precedes the
            // plan's first week (not started yet), show week 1; if today is
            // past the last week's end (plan finished), show the final week.
            currentWeek = weeks.LastOrDefault(w => w.StartDate.Date <= today) ?? weeks[0];
            currentWeekNumber = currentWeek.WeekNumber;
            weekStart = currentWeek.StartDate.Date;
            weekEnd = weekStart.AddDays(6);
        }
        else
        {
            // Legacy/seeded plans without TrainingWeek rows: fall back to a
            // calendar Monday-Sunday week anchored on today.
            weekStart = today.AddDays(((int)DayOfWeek.Monday - (int)today.DayOfWeek - 7) % 7);
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                weekStart = today.AddDays(-6);
            }
            weekEnd = weekStart.AddDays(6);
            currentWeekNumber = 1;
        }

        var weekDays = await _context.TrainingDays
            .AsNoTracking()
            .Where(d => d.PlanId == plan.Id && d.Date >= weekStart && d.Date <= weekEnd)
            .ToListAsync(ct);

        var weekSummary = new List<TrainingDayResponse>();
        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            var existing = weekDays.FirstOrDefault(d => d.Date == date);
            if (existing != null)
            {
                weeksById.TryGetValue(existing.WeekId, out var existingWeek);
                weekSummary.Add(MapToResponse(existing, existingWeek));
            }
            else
            {
                weekSummary.Add(new TrainingDayResponse
                {
                    DayId = null,
                    Date = date,
                    DayType = TrainingDayType.Rest,
                    Status = TrainingDayStatus.Planned,
                    Title = "Rest Day",
                    Description = "No run scheduled today.",
                    PlannedDistanceKm = 0,
                    PlannedDurationMin = 0,
                    CanMarkComplete = false,
                    CanMarkNotToday = false
                });
            }
        }

        // Fetch tip of the day
        var dailyTip = await GetTipForTypeAsync(todayWorkout.DayType, plan.GoalType, plan.Level, ct);

        var planProgressText = totalWeeks > 0
            ? $"Week {currentWeekNumber} of {totalWeeks}"
            : $"Week {currentWeekNumber}";

        // Phase 4G.6D: independent, entity-derived provenance -- never
        // parsed from planProgressText.
        var (_, currentWeekType, currentRunwayBlock) = MapWeekProvenance(currentWeek);

        return new HomeResponse
        {
            ActivePlan = new ActivePlanSummaryDto
            {
                PlanId = plan.Id,
                GoalType = EnumSnakeCase.ToSnakeCase(plan.GoalType),
                GoalDistance = EnumSnakeCase.ToSnakeCase(plan.GoalDistance),
                RequestedTargetDistanceKm = plan.RequestedTargetDistanceKm,
                Level = EnumSnakeCase.ToSnakeCase(plan.Level),
                ProgressText = planProgressText,
                CurrentWeekNumber = currentWeek?.WeekNumber,
                TotalWeeks = totalWeeks > 0 ? totalWeeks : null,
                CurrentWeekType = currentWeekType,
                CurrentRunwayBlock = currentRunwayBlock
            },
            TodayWorkout = todayWorkout,
            DailyTip = dailyTip,
            WeekSummary = weekSummary,
            HasPendingConfirmations = hasPending
        };
    }

    // ─── CALENDAR QUERY SERVICE ──────────────────────────────────────────────
    public async Task<object> GetCalendarAsync(Guid internalUserId, string month, CancellationToken ct = default)
    {
        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        
        var swPlan = System.Diagnostics.Stopwatch.StartNew();
        var plan = await _context.TrainingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.InternalUserId == internalUserId && p.Status == TrainingPlanStatus.Active, ct);
        swPlan.Stop();

        if (plan == null)
        {
            _logger.LogInformation("GetCalendarAsync: No active plan found. ActivePlanLookupDurationMs={ActivePlanLookupDurationMs}, TotalDurationMs={TotalDurationMs}", 
                swPlan.ElapsedMilliseconds, swTotal.ElapsedMilliseconds);
            return new List<TrainingDayResponse>();
        }

        if (plan.ScheduleStrategy == PlanScheduleStrategy.RollingLongHorizon)
            return await _rollingReadProvider.GetCalendarAsync(internalUserId, plan.Id, month, ct);

        if (!DateTime.TryParseExact($"{month}-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var startOfMonth))
        {
            throw new ArgumentException("Invalid month format. Expected YYYY-MM.");
        }

        // All Date columns are stored as UTC ("timestamp with time zone");
        // DateTime.TryParse produces Kind=Unspecified, which Npgsql rejects.
        startOfMonth = DateTime.SpecifyKind(startOfMonth, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var swQuery = System.Diagnostics.Stopwatch.StartNew();
        // Phase 4G.6D: WeekNumber/WeekType/CatalogPhaseKey are pulled via the
        // TrainingDay.Week navigation in the SAME query (EF translates this
        // to one SQL JOIN — no N+1) rather than a separate per-day lookup.
        // Projected as raw values first (EnumSnakeCase.ToSnakeCase and the
        // runway-only conditional are plain C# calls, not translatable to
        // SQL) and mapped to TrainingDayResponse afterward.
        var rawDays = await _context.TrainingDays
            .AsNoTracking()
            .Where(d => d.PlanId == plan.Id && d.Date >= startOfMonth && d.Date <= endOfMonth)
            .Select(d => new
            {
                d.Id,
                d.Date,
                d.DayType,
                d.Status,
                d.Title,
                d.Description,
                d.PlannedDistanceKm,
                d.PlannedDurationMin,
                d.PlannedPaceMinKm,
                d.Intensity,
                d.ActualDistanceKm,
                d.ActualDurationMin,
                d.IsLongRun,
                d.CanMarkComplete,
                d.CanMarkNotToday,
                WeekNumber = (int?)d.Week.WeekNumber,
                WeekType = (TrainingWeekType?)d.Week.WeekType,
                CatalogPhaseKey = d.Week.CatalogPhaseKey
            })
            .ToListAsync(ct);
        swQuery.Stop();

        var swMap = System.Diagnostics.Stopwatch.StartNew();

        var days = rawDays.Select(d => new TrainingDayResponse
        {
            DayId = d.Id,
            Date = d.Date,
            DayType = d.DayType,
            Status = d.Status,
            Title = d.Title,
            Description = d.Description,
            PlannedDistanceKm = d.PlannedDistanceKm,
            PlannedDurationMin = d.PlannedDurationMin,
            PlannedPaceMinKm = d.PlannedPaceMinKm,
            Intensity = d.Intensity,
            ActualDistanceKm = d.ActualDistanceKm,
            ActualDurationMin = d.ActualDurationMin,
            IsLongRun = d.IsLongRun,
            CanMarkComplete = d.CanMarkComplete,
            CanMarkNotToday = d.CanMarkNotToday,
            WeekNumber = d.WeekNumber,
            WeekType = d.WeekType.HasValue ? EnumSnakeCase.ToSnakeCase(d.WeekType.Value) : null,
            RunwayBlock = d.WeekType == TrainingWeekType.PreparationRunway ? d.CatalogPhaseKey : null
        }).ToList();

        // Use dictionary with grouping to safely handle any potential duplicate dates
        var daysDict = days
            .GroupBy(d => d.Date)
            .ToDictionary(g => g.Key, g => g.First());

        // Include rest days to map the full month calendar view
        var calendarDays = new List<TrainingDayResponse>();
        for (var date = startOfMonth; date <= endOfMonth; date = date.AddDays(1))
        {
            if (daysDict.TryGetValue(date, out var existing))
            {
                calendarDays.Add(existing);
            }
            else
            {
                calendarDays.Add(new TrainingDayResponse
                {
                    DayId = null,
                    Date = date,
                    DayType = TrainingDayType.Rest,
                    Status = TrainingDayStatus.Planned,
                    Title = "Rest Day",
                    Description = "No run scheduled.",
                    PlannedDistanceKm = 0,
                    PlannedDurationMin = 0,
                    CanMarkComplete = false,
                    CanMarkNotToday = false
                });
            }
        }
        swMap.Stop();
        swTotal.Stop();

        _logger.LogInformation(
            "GetCalendarAsync Completed. Month={Month}, PlanId={PlanId}, ActivePlanLookupDurationMs={ActivePlanLookupDurationMs}, TrainingDaysQueryDurationMs={TrainingDaysQueryDurationMs}, DtoMappingDurationMs={DtoMappingDurationMs}, TotalDurationMs={TotalDurationMs}",
            month, plan.Id, swPlan.ElapsedMilliseconds, swQuery.ElapsedMilliseconds, swMap.ElapsedMilliseconds, swTotal.ElapsedMilliseconds);

        return calendarDays;
    }

    // ─── TRAINING DAY SERVICE ────────────────────────────────────────────────
    public async Task<TrainingDayDetailResponse> GetTrainingDayDetailAsync(Guid internalUserId, Guid trainingDayId, CancellationToken ct = default)
    {
        // Phase 4G.6D: .Include(d => d.Week) — one bounded read (day + its
        // single owning week), not the full 15-20 week plan.
        var day = await _context.TrainingDays
            .AsNoTracking()
            .Include(d => d.Week)
            .FirstOrDefaultAsync(d => d.Id == trainingDayId && d.Plan.InternalUserId == internalUserId, ct);

        if (day == null)
        {
            throw new NotFoundAppException("Training day not found.");
        }

        var (weekNumber, weekType, runwayBlock) = MapWeekProvenance(day.Week);

        return new TrainingDayDetailResponse
        {
            DayId = day.Id,
            Date = day.Date,
            DayType = day.DayType,
            Status = day.Status,
            Title = day.Title,
            Description = day.Description,
            PlannedDistanceKm = day.PlannedDistanceKm,
            PlannedDurationMin = day.PlannedDurationMin,
            PlannedPaceMinKm = day.PlannedPaceMinKm,
            Intensity = day.Intensity,
            ActualDistanceKm = day.ActualDistanceKm,
            ActualDurationMin = day.ActualDurationMin,
            IsLongRun = day.IsLongRun,
            CanMarkComplete = day.CanMarkComplete,
            CanMarkNotToday = day.CanMarkNotToday,
            CompletedAt = day.CompletedAt,
            WeekNumber = weekNumber,
            WeekType = weekType,
            RunwayBlock = runwayBlock,
            Source = day.Source.HasValue ? EnumSnakeCase.ToSnakeCase(day.Source.Value) : null,
            AdaptedFromId = day.AdaptedFromId
        };
    }

    // ─── WORKOUT COMPLETION SERVICE ──────────────────────────────────────────
    // PHASE V1-HARDEN.1: closes V1HARDEN0-002 (Complete had no status guard —
    // a repeated/retried call silently overwrote actuals and appended an
    // unbounded number of duplicate WorkoutLog/PlanEvent rows) and the
    // adjacent concurrency gap identified alongside it. See
    // PHASE_V1_HARDEN_1_...md §9/§19-21 for the full design rationale.
    public async Task<CompleteWorkoutResponse> CompleteWorkoutAsync(Guid internalUserId, Guid trainingDayId, CompleteWorkoutRequest request, CancellationToken ct = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var day = await LoadOwnedActiveDayForMutationAsync(internalUserId, trainingDayId, ct);

            if (day.Status == TrainingDayStatus.Completed)
            {
                // Same-mutation retry: exact replay is an idempotent success,
                // not a second logical completion. No new WorkoutLog/PlanEvent
                // row, no re-mutated actuals/timestamp.
                if (IsSameCompletion(day, request))
                {
                    return new CompleteWorkoutResponse { DayId = day.Id, Status = "completed" };
                }

                throw new TrainingDayCompletionConflictException(
                    "This training day is already completed with different actual values. Reload the current result before retrying.");
            }

            if (day.Status == TrainingDayStatus.Missed)
            {
                // NotToday→Complete: rejected by the existing product-state
                // guard (CanMarkComplete/CanMarkNotToday are both already set
                // false the moment a day becomes Missed via a confirmed
                // Not-Today decision — see ConfirmNotTodayDecisionAsync and
                // ResolvePendingConfirmationAsync, both of which already flip
                // the same two flags for the same reason). This is resolved
                // from that existing, pre-established source convention, not
                // invented here.
                throw new TrainingDayTransitionConflictException(
                    "This training day was already recorded as not today and cannot be completed.");
            }

            await ApplyCompletionAsync(day, internalUserId, request, ct);

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new CompleteWorkoutResponse { DayId = day.Id, Status = "completed" };
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent writer committed first. Reload fresh and recover
            // idempotently if the committed outcome is an exact replay of
            // this request; otherwise surface a deterministic 409 — never an
            // uncontrolled 500 and never a silent last-write-wins overwrite.
            await tx.RollbackAsync(ct);
            _context.ChangeTracker.Clear();
            var current = await _context.TrainingDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == trainingDayId, ct);
            if (current != null && current.Status == TrainingDayStatus.Completed && IsSameCompletion(current, request))
            {
                return new CompleteWorkoutResponse { DayId = current.Id, Status = "completed" };
            }

            throw new TrainingDayMutationConcurrencyConflictException(
                "A concurrent update changed this training day. Reload the current result before retrying.");
        }
    }

    /// <summary>
    /// Applies the Completed mutation plus its two side effects (WorkoutLog
    /// append + week.ActualVolumeKm recompute + PlanEvent append) — unchanged
    /// business logic from the pre-V1-HARDEN.1 implementation, now only ever
    /// reached once per logical completion (the caller's status guard above
    /// ensures this never runs a second time for the same day).
    /// </summary>
    private async Task ApplyCompletionAsync(TrainingDay day, Guid internalUserId, CompleteWorkoutRequest request, CancellationToken ct)
    {
        day.Status = TrainingDayStatus.Completed;
        day.ActualDistanceKm = request.ActualDistanceKm;
        day.ActualDurationMin = request.ActualDurationMin;
        day.CompletedAt = DateTime.UtcNow;
        day.CanMarkComplete = false;
        day.CanMarkNotToday = false;
        day.UpdatedAt = DateTime.UtcNow;

        // Log the workout
        var log = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            InternalUserId = internalUserId,
            PlanId = day.PlanId,
            TrainingDayId = day.Id,
            Result = request.ActualDistanceKm >= day.PlannedDistanceKm ? "as_planned" : "shorter",
            ActualDistanceKm = request.ActualDistanceKm,
            ActualDurationMin = request.ActualDurationMin,
            UserNote = request.UserNote,
            CreatedAt = DateTime.UtcNow
        };
        _context.WorkoutLogs.Add(log);

        // Update the week's actual volume
        var week = day.Week;
        var completedDays = await _context.TrainingDays
            .AsNoTracking()
            .Where(d => d.WeekId == week.Id && d.Status == TrainingDayStatus.Completed && d.Id != day.Id)
            .ToListAsync(ct);

        week.ActualVolumeKm = completedDays.Sum(d => d.ActualDistanceKm ?? 0.0) + request.ActualDistanceKm;

        // Log completion event
        var planEvent = new PlanEvent
        {
            Id = Guid.NewGuid(),
            InternalUserId = internalUserId,
            PlanId = day.PlanId,
            TrainingDayId = day.Id,
            EventType = "WorkoutCompleted",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { distance = request.ActualDistanceKm, duration = request.ActualDurationMin }),
            CreatedAt = DateTime.UtcNow
        };
        _context.PlanEvents.Add(planEvent);
    }

    private static bool IsSameCompletion(TrainingDay day, CompleteWorkoutRequest request) =>
        day.Status == TrainingDayStatus.Completed
        && Math.Abs((day.ActualDistanceKm ?? double.MinValue) - request.ActualDistanceKm) < 0.0001
        && day.ActualDurationMin == request.ActualDurationMin;

    /// <summary>
    /// Single authoritative load used by every Core TrainingDay mutation
    /// (Complete, CreateNotTodayDecision, ConfirmNotTodayDecision) —
    /// PHASE V1-HARDEN.1 §9/§18-21/§73. Enforces cross-user ownership (zero
    /// delta from the existing <c>InternalUserId</c> predicate convention),
    /// the plan-state guard (a cancelled/inactive plan's TrainingDay is
    /// never mutable — reported as 404, mirroring
    /// <c>LongHorizonRollingSessionMutationService.LoadOwnedAsync</c>'s own
    /// "not found" treatment of a non-Active owning plan), and serializes
    /// concurrent mutations against the same plan with a Postgres row lock
    /// on the owning TrainingPlan row — the exact same
    /// <c>SELECT ... FOR UPDATE</c> pattern already used for the Long-Horizon
    /// rolling-session mutation service, reused here rather than inventing a
    /// second concurrency mechanism. Must be called inside an open
    /// transaction (the lock is held until that transaction commits/rolls
    /// back).
    /// </summary>
    private async Task<TrainingDay> LoadOwnedActiveDayForMutationAsync(Guid internalUserId, Guid trainingDayId, CancellationToken ct)
    {
        var planId = await _context.TrainingDays
            .AsNoTracking()
            .Where(d => d.Id == trainingDayId && d.Plan.InternalUserId == internalUserId)
            .Select(d => d.PlanId)
            .FirstOrDefaultAsync(ct);

        if (planId == Guid.Empty)
        {
            throw new NotFoundAppException("Training day not found.");
        }

        var lockedPlan = await _context.TrainingPlans
            .FromSqlInterpolated($"SELECT * FROM \"TrainingPlans\" WHERE \"Id\" = {planId} FOR UPDATE")
            .AsNoTracking()
            .SingleAsync(ct);

        if (lockedPlan.Status != TrainingPlanStatus.Active)
        {
            // A cancelled/non-active plan's TrainingDay rows remain readable
            // historical artifacts (zero delta from V1-HARDEN.0 §17's own
            // finding) but are never mutable through this surface.
            throw new NotFoundAppException("Training day not found.");
        }

        var day = await _context.TrainingDays
            .Include(d => d.Week)
            .FirstOrDefaultAsync(d => d.Id == trainingDayId, ct);

        if (day == null)
        {
            throw new NotFoundAppException("Training day not found.");
        }

        return day;
    }

    // ─── NOT TODAY SERVICE ───────────────────────────────────────────────────
    // PHASE V1-HARDEN.1: closes V1HARDEN0-004 (a Completed day could still
    // receive a Not-Today decision, which once confirmed silently reverted
    // it to Missed) and V1HARDEN0-003 (unbounded duplicate Pending decisions
    // for one real tap). The authoritative status guard against reverting a
    // Completed day lives in ConfirmNotTodayDecisionAsync below (the method
    // that actually performs the Completed→Missed mutation and is therefore
    // the one true race-safe guard point — see its own doc comment); this
    // method additionally rejects the common, non-racing case early.
    public async Task<CreateNotTodayDecisionResponse> CreateNotTodayDecisionAsync(Guid internalUserId, Guid trainingDayId, CreateNotTodayDecisionRequest request, CancellationToken ct = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);

        var day = await LoadOwnedActiveDayForMutationAsync(internalUserId, trainingDayId, ct);

        if (day.Status == TrainingDayStatus.Completed)
        {
            throw new TrainingDayTransitionConflictException(
                "This training day is already completed and cannot be marked not today.");
        }

        if (day.Status == TrainingDayStatus.Missed)
        {
            // NotToday→NotToday retry, reached after the day's prior Not-Today
            // decision was already confirmed: return that resolved decision
            // instead of creating a redundant new Pending row.
            var existingResolved = await _context.NotTodayDecisions
                .AsNoTracking()
                .Where(d => d.TrainingDayId == trainingDayId && d.Status == NotTodayDecisionStatus.Confirmed)
                .OrderByDescending(d => d.ConfirmedAt)
                .FirstOrDefaultAsync(ct);

            if (existingResolved != null)
            {
                return new CreateNotTodayDecisionResponse { DecisionId = existingResolved.Id, Status = "confirmed" };
            }

            throw new TrainingDayTransitionConflictException(
                "This training day has already been resolved and cannot be marked not today again.");
        }

        // V1HARDEN0-003: dedupe a retried/duplicated request against the same
        // still-Pending decision rather than creating a second one.
        var existingPending = await _context.NotTodayDecisions
            .Where(d => d.TrainingDayId == trainingDayId && d.Status == NotTodayDecisionStatus.Pending)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existingPending != null)
        {
            await tx.CommitAsync(ct);
            return new CreateNotTodayDecisionResponse { DecisionId = existingPending.Id, Status = "pending" };
        }

        var decision = new NotTodayDecision
        {
            Id = Guid.NewGuid(),
            InternalUserId = internalUserId,
            PlanId = day.PlanId,
            TrainingDayId = day.Id,
            Reason = request.Reason,
            Status = NotTodayDecisionStatus.Pending,
            TriggerSource = TriggerSource.NotToday,
            Action = AdaptationAction.NoChange,
            ResultingStatus = TrainingDayStatus.Missed,
            CreatedAt = DateTime.UtcNow
        };

        _context.NotTodayDecisions.Add(decision);
        await _context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new CreateNotTodayDecisionResponse
        {
            DecisionId = decision.Id,
            Status = "pending"
        };
    }

    // PHASE V1-HARDEN.1: this is the one true authoritative guard point for
    // V1HARDEN0-004 — regardless of the TrainingDay's status when the
    // decision was CREATED, this is the method that actually performs the
    // Missed mutation, so it must re-check the day's current status itself
    // (a concurrent Complete may have committed in between). Locks the
    // owning plan FOR UPDATE (via LoadOwnedActiveDayForMutationAsync) before
    // re-reading the decision fresh, so two concurrent confirms of the SAME
    // decision — or a concurrent Complete racing this confirm — serialize
    // through the same single lock Complete/CreateNotTodayDecision already use.
    public async Task<ConfirmNotTodayDecisionResponse> ConfirmNotTodayDecisionAsync(Guid internalUserId, Guid decisionId, ConfirmNotTodayDecisionRequest request, CancellationToken ct = default)
    {
        var decisionLookup = await _context.NotTodayDecisions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == decisionId && d.InternalUserId == internalUserId, ct);

        if (decisionLookup == null)
        {
            throw new NotFoundAppException("Decision not found.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            // Lock the owning plan first so this confirm serializes against
            // any concurrent Complete/CreateNotTodayDecision/Confirm for the
            // same plan (see LoadOwnedActiveDayForMutationAsync's doc comment).
            var day = await LoadOwnedActiveDayForMutationAsync(internalUserId, decisionLookup.TrainingDayId, ct);

            // Re-read the decision fresh now that the lock is held, so a
            // concurrent confirm of this exact decision cannot also pass the
            // Pending check before this transaction commits.
            var decision = await _context.NotTodayDecisions
                .FirstOrDefaultAsync(d => d.Id == decisionId && d.InternalUserId == internalUserId, ct);

            if (decision == null)
            {
                throw new NotFoundAppException("Decision not found.");
            }

            if (decision.Status == NotTodayDecisionStatus.Confirmed)
            {
                // Same-mutation retry: idempotent success, no re-run
                // adaptation, no duplicate WorkoutMissed PlanEvent, no
                // ConfirmedAt timestamp change.
                return new ConfirmNotTodayDecisionResponse
                {
                    DecisionId = decision.Id,
                    Status = "confirmed",
                    Action = EnumSnakeCase.ToSnakeCase(decision.Action),
                    PlanAdapted = false
                };
            }

            if (decision.Status != NotTodayDecisionStatus.Pending)
            {
                throw new TrainingDayTransitionConflictException(
                    "This not-today decision is no longer pending and cannot be confirmed.");
            }

            if (day.Status == TrainingDayStatus.Completed)
            {
                // V1HARDEN0-004's exact scenario: the day was completed after
                // this decision was created (e.g. a race) but before it was
                // confirmed. Never silently revert a genuinely completed day
                // to Missed — cancel the stale decision instead and reject.
                decision.Status = NotTodayDecisionStatus.Cancelled;
                decision.ConfirmedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                throw new TrainingDayTransitionConflictException(
                    "This training day was completed before the not-today decision could be confirmed; the completed result is preserved.");
            }

            // Ask the adaptation engine what to do. Phase 1: always NoChange —
            // this never reschedules or mutates future training days.
            var adaptation = await _adaptationEngine.EvaluateNotTodayAsync(
                decision.PlanId, decision.TrainingDayId, decision.TriggerSource, decision.Reason, ct);

            decision.Status = NotTodayDecisionStatus.Confirmed;
            decision.ConfirmedAt = DateTime.UtcNow;
            decision.Action = adaptation.Action;

            // Apply missed status to the training day (today only — no future days touched)
            day.Status = TrainingDayStatus.Missed;
            day.CanMarkComplete = false;
            day.CanMarkNotToday = false;
            day.UpdatedAt = DateTime.UtcNow;

            // Log Missed event
            var planEvent = new PlanEvent
            {
                Id = Guid.NewGuid(),
                InternalUserId = internalUserId,
                PlanId = decision.PlanId,
                TrainingDayId = decision.TrainingDayId,
                EventType = "WorkoutMissed",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { reason = decision.Reason }),
                CreatedAt = DateTime.UtcNow
            };
            _context.PlanEvents.Add(planEvent);

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new ConfirmNotTodayDecisionResponse
            {
                DecisionId = decision.Id,
                Status = "confirmed",
                Action = EnumSnakeCase.ToSnakeCase(adaptation.Action),
                PlanAdapted = adaptation.PlanAdapted
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            _context.ChangeTracker.Clear();
            throw new TrainingDayMutationConcurrencyConflictException(
                "A concurrent update changed this training day. Reload the current result before retrying.");
        }
    }

    // ─── PENDING CONFIRMATIONS ──────────────────────────────────────────────
    public async Task<List<PendingConfirmationResponse>> GetPendingConfirmationsAsync(Guid internalUserId, CancellationToken ct = default)
    {
        // Single joined query + direct DTO projection — avoids the previous
        // N+1 (one TrainingDays round-trip per pending confirmation).
        var responses = await _context.PendingConfirmations
            .AsNoTracking()
            .Where(p => p.InternalUserId == internalUserId && p.Status == "pending")
            .Join(
                _context.TrainingDays.AsNoTracking(),
                p => p.TrainingDayId,
                d => d.Id,
                (p, d) => new PendingConfirmationResponse
                {
                    PendingConfirmationId = p.Id,
                    TrainingDayId = p.TrainingDayId,
                    Date = d.Date,
                    DayType = d.DayType,
                    Title = d.Title,
                    PlannedDistanceKm = d.PlannedDistanceKm,
                    PlannedDurationMin = d.PlannedDurationMin
                })
            .ToListAsync(ct);

        return responses;
    }

    public async Task<ResolvePendingConfirmationResponse> ResolvePendingConfirmationAsync(Guid internalUserId, ResolvePendingConfirmationRequest request, CancellationToken ct = default)
    {
        var p = await _context.PendingConfirmations
            .FirstOrDefaultAsync(pc => pc.Id == request.PendingConfirmationId && pc.InternalUserId == internalUserId, ct);

        if (p == null)
        {
            throw new NotFoundAppException("Pending confirmation not found.");
        }

        p.Status = "resolved";
        p.ResolvedAt = DateTime.UtcNow;

        var wasCompleted = request.Resolution.Equals("completed", StringComparison.OrdinalIgnoreCase);

        var day = await _context.TrainingDays.FirstOrDefaultAsync(d => d.Id == p.TrainingDayId, ct);
        if (day != null)
        {
            if (wasCompleted)
            {
                day.Status = TrainingDayStatus.Completed;
                day.ActualDistanceKm = request.ActualDistanceKm ?? day.PlannedDistanceKm;
                day.ActualDurationMin = request.ActualDurationMin ?? day.PlannedDurationMin;
                day.CompletedAt = DateTime.UtcNow;
                day.CanMarkComplete = false;
                day.CanMarkNotToday = false;
                day.UpdatedAt = DateTime.UtcNow;

                var log = new WorkoutLog
                {
                    Id = Guid.NewGuid(),
                    InternalUserId = internalUserId,
                    PlanId = day.PlanId,
                    TrainingDayId = day.Id,
                    Result = "as_planned",
                    ActualDistanceKm = day.ActualDistanceKm,
                    ActualDurationMin = day.ActualDurationMin,
                    UserNote = request.UserNote,
                    CreatedAt = DateTime.UtcNow
                };
                _context.WorkoutLogs.Add(log);
            }
            else
            {
                day.Status = TrainingDayStatus.Missed;
                day.CanMarkComplete = false;
                day.CanMarkNotToday = false;
                day.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Ask the adaptation engine what to do. Phase 1: always NoChange —
        // this never reschedules or mutates future training days.
        var adaptation = await _adaptationEngine.EvaluatePendingConfirmationAsync(
            p.PlanId, p.TrainingDayId, wasCompleted, ct);

        await _context.SaveChangesAsync(ct);

        return new ResolvePendingConfirmationResponse
        {
            PendingConfirmationId = p.Id,
            Status = "resolved",
            PlanAdapted = adaptation.PlanAdapted
        };
    }

    // ─── PROFILE SERVICE ─────────────────────────────────────────────────────
    public async Task<ProfileOverviewResponse> GetProfileOverviewAsync(Guid internalUserId, CancellationToken ct = default)
    {
        var profile = await _context.UserProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.InternalUserId == internalUserId, ct);

        var activePlan = await _context.TrainingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.InternalUserId == internalUserId && p.Status == TrainingPlanStatus.Active, ct);

        if (profile == null)
        {
            return new ProfileOverviewResponse
            {
                Name = "Runner",
                Email = string.Empty,
                Unit = DistanceUnit.Km,
                RunningBackground = activePlan?.Level ?? RunningBackground.Beginner,
                ActivePlanStats = null
            };
        }

        var name  = profile.User?.DisplayName ?? string.Empty;
        var email = profile.User?.Email       ?? string.Empty;

        // RunningBackground is no longer stored on UserProfile after Migration 1.
        // Read it from the active plan snapshot; use Beginner as default when
        // no plan exists.
        var runningBackground = activePlan?.Level ?? RunningBackground.Beginner;

        ProfilePlanStatsDto? stats = null;
        if (activePlan != null)
        {
            var planDays = await _context.TrainingDays
                .AsNoTracking()
                .Where(d => d.PlanId == activePlan.Id)
                .ToListAsync(ct);

            var completedRuns = planDays.Count(d => d.Status == TrainingDayStatus.Completed);
            var totalRuns = planDays.Count(d => d.DayType != TrainingDayType.Rest);
            var completedDist = planDays.Sum(d => d.ActualDistanceKm ?? 0.0);
            var adherence = totalRuns > 0 ? ((double)completedRuns / totalRuns) * 100.0 : 0.0;

            var planName = $"{activePlan.Level} {activePlan.GoalDistance} {activePlan.GoalType} Plan";

            stats = new ProfilePlanStatsDto
            {
                PlanName = planName,
                GoalType = EnumSnakeCase.ToSnakeCase(activePlan.GoalType),
                GoalDistance = EnumSnakeCase.ToSnakeCase(activePlan.GoalDistance),
                RequestedTargetDistanceKm = activePlan.RequestedTargetDistanceKm,
                CompletedRunsCount = completedRuns,
                TotalPlannedRunsCount = totalRuns,
                TotalCompletedDistance = completedDist,
                AdherenceRatePercent = Math.Round(adherence, 1)
            };
        }

        return new ProfileOverviewResponse
        {
            Name = name,
            Email = email,
            Unit = profile.Unit,
            RunningBackground = runningBackground,
            ActivePlanStats = stats
        };
    }

    // ─── PRIVATE HELPERS ─────────────────────────────────────────────────────

    /// <summary>
    /// Backend Integration Phase 4G.6D — the single source-of-truth mapping
    /// from a persisted TrainingWeek to the additive provenance fields every
    /// active-plan response now carries. RunwayBlock is CatalogPhaseKey ONLY
    /// when WeekType is PreparationRunway — a Core week's CatalogPhaseKey
    /// (FOUNDATION/BUILD/RACE_SPECIFIC/TAPER) is never exposed as a runway
    /// block. Never derives anything from week number, plan length, or any
    /// preview/catalog state — <paramref name="week"/> must be the real
    /// persisted entity.
    /// </summary>
    private static (int? weekNumber, string? weekType, string? runwayBlock) MapWeekProvenance(TrainingWeek? week)
    {
        if (week is null) return (null, null, null);
        var weekType = EnumSnakeCase.ToSnakeCase(week.WeekType);
        var runwayBlock = week.WeekType == TrainingWeekType.PreparationRunway ? week.CatalogPhaseKey : null;
        return (week.WeekNumber, weekType, runwayBlock);
    }

    private static TrainingDayResponse MapToResponse(TrainingDay d, TrainingWeek? week = null)
    {
        var (weekNumber, weekType, runwayBlock) = MapWeekProvenance(week);
        return new TrainingDayResponse
        {
            DayId = d.Id,
            Date = d.Date,
            DayType = d.DayType,
            Status = d.Status,
            Title = d.Title,
            Description = d.Description,
            PlannedDistanceKm = d.PlannedDistanceKm,
            PlannedDurationMin = d.PlannedDurationMin,
            PlannedPaceMinKm = d.PlannedPaceMinKm,
            Intensity = d.Intensity,
            ActualDistanceKm = d.ActualDistanceKm,
            ActualDurationMin = d.ActualDurationMin,
            IsLongRun = d.IsLongRun,
            CanMarkComplete = d.CanMarkComplete,
            CanMarkNotToday = d.CanMarkNotToday,
            WeekNumber = weekNumber,
            WeekType = weekType,
            RunwayBlock = runwayBlock
        };
    }

    private async Task<DailyTipResponse> GetTipForTypeAsync(TrainingDayType type, GoalType goal, RunningBackground level, CancellationToken ct)
    {
        var tip = await _context.DailyTipSets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.WorkoutType == type && t.GoalType == goal && t.Level == level, ct);

        if (tip == null)
        {
            tip = await _context.DailyTipSets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.WorkoutType == type, ct);
        }

        if (tip == null)
        {
            return await GetDefaultTipAsync(ct);
        }

        return new DailyTipResponse
        {
            TipKey = tip.TipKey,
            Title = tip.Title,
            Message = tip.Message,
            WorkoutType = tip.WorkoutType.HasValue ? EnumSnakeCase.ToSnakeCase(tip.WorkoutType.Value) : null
        };
    }

    private async Task<DailyTipResponse> GetDefaultTipAsync(CancellationToken ct)
    {
        var defaultTip = await _context.DailyTipSets.AsNoTracking().FirstOrDefaultAsync(t => t.WorkoutType == null, ct);
        if (defaultTip != null)
        {
            return new DailyTipResponse
            {
                TipKey = defaultTip.TipKey,
                Title = defaultTip.Title,
                Message = defaultTip.Message,
                WorkoutType = null
            };
        }

        return new DailyTipResponse
        {
            TipKey = "default_tip",
            Title = "Welcome to Antigravity!",
            Message = "Consistency is the key to running. Take it day by day.",
            WorkoutType = null
        };
    }
}
