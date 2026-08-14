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
    public async Task<CompleteWorkoutResponse> CompleteWorkoutAsync(Guid internalUserId, Guid trainingDayId, CompleteWorkoutRequest request, CancellationToken ct = default)
    {
        var day = await _context.TrainingDays
            .Include(d => d.Week)
            .FirstOrDefaultAsync(d => d.Id == trainingDayId && d.Plan.InternalUserId == internalUserId, ct);

        if (day == null)
        {
            throw new NotFoundAppException("Training day not found.");
        }

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

        await _context.SaveChangesAsync(ct);

        return new CompleteWorkoutResponse
        {
            DayId = day.Id,
            Status = "completed"
        };
    }

    // ─── NOT TODAY SERVICE ───────────────────────────────────────────────────
    public async Task<CreateNotTodayDecisionResponse> CreateNotTodayDecisionAsync(Guid internalUserId, Guid trainingDayId, CreateNotTodayDecisionRequest request, CancellationToken ct = default)
    {
        var day = await _context.TrainingDays
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == trainingDayId && d.Plan.InternalUserId == internalUserId, ct);

        if (day == null)
        {
            throw new NotFoundAppException("Training day not found.");
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

        return new CreateNotTodayDecisionResponse
        {
            DecisionId = decision.Id,
            Status = "pending"
        };
    }

    public async Task<ConfirmNotTodayDecisionResponse> ConfirmNotTodayDecisionAsync(Guid internalUserId, Guid decisionId, ConfirmNotTodayDecisionRequest request, CancellationToken ct = default)
    {
        var decision = await _context.NotTodayDecisions
            .FirstOrDefaultAsync(d => d.Id == decisionId && d.InternalUserId == internalUserId, ct);

        if (decision == null)
        {
            throw new NotFoundAppException("Decision not found.");
        }

        // Ask the adaptation engine what to do. Phase 1: always NoChange —
        // this never reschedules or mutates future training days.
        var adaptation = await _adaptationEngine.EvaluateNotTodayAsync(
            decision.PlanId, decision.TrainingDayId, decision.TriggerSource, decision.Reason, ct);

        decision.Status = NotTodayDecisionStatus.Confirmed;
        decision.ConfirmedAt = DateTime.UtcNow;
        decision.Action = adaptation.Action;

        // Apply missed status to the training day (today only — no future days touched)
        var day = await _context.TrainingDays.FirstOrDefaultAsync(d => d.Id == decision.TrainingDayId, ct);
        if (day != null)
        {
            day.Status = TrainingDayStatus.Missed;
            day.CanMarkComplete = false;
            day.CanMarkNotToday = false;
            day.UpdatedAt = DateTime.UtcNow;
        }

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

        return new ConfirmNotTodayDecisionResponse
        {
            DecisionId = decision.Id,
            Status = "confirmed",
            Action = EnumSnakeCase.ToSnakeCase(adaptation.Action),
            PlanAdapted = adaptation.PlanAdapted
        };
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
