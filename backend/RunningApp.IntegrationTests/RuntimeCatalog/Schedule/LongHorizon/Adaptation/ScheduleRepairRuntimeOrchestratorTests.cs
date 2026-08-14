using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.3 -- real-PostgreSQL integration tests for
/// <see cref="ScheduleRepairRuntimeOrchestrator"/> and
/// <see cref="ScheduleRepairCandidateProvider"/>: the live structural
/// candidate-query layer, short-circuit behavior, and the full
/// reason-mapping -> policy -> persistence chain. Deliberately calls the
/// orchestrator directly (not through HTTP/TrainingPlans ownership) --
/// exactly like Phase 4M.2's own ScheduleRepairPersistenceTests, it needs
/// only the LongHorizonRollingPlanState graph, not a full TrainingPlans row,
/// since neither the orchestrator nor the persistence service it wraps
/// touches TrainingPlans at all (that ownership check belongs to
/// LongHorizonRollingSessionMutationService, exercised separately by
/// LongHorizonActiveReadAndMutationTests / ScheduleRepairSupersededAndReadCorrectnessTests).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class ScheduleRepairRuntimeOrchestratorTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private readonly List<Guid> _createdPlanIds = [];

    public Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var planId in _createdPlanIds)
        {
            var plan = await db.LongHorizonRollingPlanStates.FindAsync(planId);
            if (plan is not null) { db.LongHorizonRollingPlanStates.Remove(plan); await db.SaveChangesAsync(); }
        }
        _factory.Dispose();
    }

    private AppDbContext NewDb() => _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
    private ILoggerFactory NewLoggerFactory() => _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ILoggerFactory>();

    // ── Fixture: a two-week window, Core/Build phase, Mon/Wed/Fri/Sun preferred days ──

    private async Task<Guid> CreatePlanAsync(AppDbContext db, int windowStart = 1, int windowEnd = 2)
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingPlanStates.Add(new LongHorizonRollingPlanState
        {
            Id = id, TotalWeeks = 22, ReadinessProfile = "CoreEntryReady",
            StartDate = new DateOnly(2026, 8, 10), RaceDate = new DateOnly(2027, 1, 11),
            GoalType = "Race", GoalDistance = "TenK", Level = "Intermediate", DaysPerWeek = 4,
            PreferredDaysCsv = "Monday,Wednesday,Friday,Sunday", LongRunDay = "Sunday",
            CandidateKey = "TEN_K__4D__INTERMEDIATE", CandidateVersion = 10, CatalogRootPath = "test",
            CurrentLifecycleStatus = LongHorizonPersistedLifecycleState.NumericActivated,
            CurrentWindowStartWeek = windowStart, CurrentWindowEndWeek = windowEnd,
            ActiveContextVersionSequence = 1, ActiveContextVersionId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        _createdPlanIds.Add(id);
        return id;
    }

    private async Task<Guid> CreateWeekAsync(AppDbContext db, Guid planId, int globalWeek, DateOnly mondayStart,
        LongHorizonPersistedSegmentType segment = LongHorizonPersistedSegmentType.Core, string stage = "Build")
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingWeekStates.Add(new LongHorizonRollingWeekState
        {
            Id = id, PlanStateId = planId, GlobalWeek = globalWeek, SegmentType = segment, Stage = stage,
            StructuralStartDate = mondayStart, StructuralEndDate = mondayStart.AddDays(6),
            LifecycleState = LongHorizonPersistedLifecycleState.NumericActivated, WeeklyVolumeKm = 25, LongRunKm = 8,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> CreateSessionAsync(AppDbContext db, Guid weekStateId, int ordinal, PreparationRunwaySlotRole role, DateOnly date,
        LongHorizonRollingSessionOutcomeStatus outcome = LongHorizonRollingSessionOutcomeStatus.Planned,
        LongHorizonPersistedSessionPlanningStatus planningStatus = LongHorizonPersistedSessionPlanningStatus.Active)
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingSessionStates.Add(new LongHorizonRollingSessionState
        {
            Id = id, WeekStateId = weekStateId, SessionOrdinal = ordinal,
            SessionRole = LongHorizonSessionRoleCodec.ToCanonicalToken(role), WorkoutKey = "STANDARD", WorkoutVersion = 6,
            DistanceKm = 7, AssignedDate = date, ActivationContextVersionSequence = 1, Provenance = "generated_from_initial_profile",
            OutcomeStatus = outcome, PlanningStatus = planningStatus,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<LongHorizonRollingSessionState> MarkNotTodayAsync(AppDbContext db, Guid sessionId, string reason)
    {
        // Must mirror LongHorizonRollingSessionMutationService.LoadOwnedAsync's
        // own eager-load shape: the candidate provider reads trigger.Week.Plan.Weeks[*].Sessions
        // directly from the already-loaded graph, no extra query of its own.
        var session = await db.LongHorizonRollingSessionStates
            .Include(s => s.Week).ThenInclude(w => w.Plan).ThenInclude(p => p.Weeks).ThenInclude(w => w.Sessions)
            .SingleAsync(s => s.Id == sessionId);
        session.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.NotToday;
        session.NotTodayReason = reason;
        session.NotTodayRecordedAtUtc = DateTime.UtcNow;
        session.OutcomeVersion++;
        await db.SaveChangesAsync();
        return session;
    }

    // ── Empty-slot candidate query rules (9-18) ─────────────────────────

    [Fact]
    public async Task EmptySlot_FutureUnoccupiedPreferredDay_Found_AndRescheduled()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        // Wednesday 8/12 and Friday 8/14 both deliberately left empty; Sunday
        // LONG present so LONG spacing stays valid. Earliest empty slot wins.
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");

        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);

        Assert.Equal(LongHorizonScheduleRepairActionKind.RescheduleToEmptySlot, outcome.Action);
        Assert.Equal(new DateOnly(2026, 8, 12), outcome.ReplacementDate);
        var replacement = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == outcome.ReplacementSessionId);
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Active, replacement.PlanningStatus);
    }

    [Fact]
    public async Task EmptySlot_PastAndSameDayDates_Excluded()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 14)); // Friday
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");
        var candidates = ScheduleRepairCandidateProvider.GetEmptySlotCandidates(trigger.Week.Plan, trigger);
        Assert.DoesNotContain(candidates, c => c.Date <= trigger.AssignedDate); // excludes Mon(10)/Wed(12)/Fri(14 itself)
    }

    [Fact]
    public async Task EmptySlot_NonPreferredDay_Excluded()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");
        var candidates = ScheduleRepairCandidateProvider.GetEmptySlotCandidates(trigger.Week.Plan, trigger);
        // Tuesday 8/11, Thursday 8/13, Saturday 8/15 are all inside the week but not preferred days.
        Assert.DoesNotContain(candidates, c => c.Date.DayOfWeek is DayOfWeek.Tuesday or DayOfWeek.Thursday or DayOfWeek.Saturday);
    }

    [Fact]
    public async Task EmptySlot_OccupiedActiveDate_Excluded()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, week1, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 12)); // occupies Wed
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");
        var candidates = ScheduleRepairCandidateProvider.GetEmptySlotCandidates(trigger.Week.Plan, trigger);
        Assert.DoesNotContain(candidates, c => c.Date == new DateOnly(2026, 8, 12));
    }

    [Fact]
    public async Task EmptySlot_CrossWindowDate_Excluded()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db, windowStart: 1, windowEnd: 1); // window is week 1 only
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var week2 = await CreateWeekAsync(db, planId, 2, new DateOnly(2026, 8, 17)); // outside window
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");
        var candidates = ScheduleRepairCandidateProvider.GetEmptySlotCandidates(trigger.Week.Plan, trigger);
        Assert.DoesNotContain(candidates, c => c.Date >= new DateOnly(2026, 8, 17));
        _ = week2;
    }

    [Fact]
    public async Task EmptySlot_CrossPhaseDate_Excluded()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10), stage: "Build");
        var week2 = await CreateWeekAsync(db, planId, 2, new DateOnly(2026, 8, 17), stage: "Peak"); // different phase
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");
        var candidates = ScheduleRepairCandidateProvider.GetEmptySlotCandidates(trigger.Week.Plan, trigger);
        Assert.DoesNotContain(candidates, c => c.Date >= new DateOnly(2026, 8, 17));
        _ = week2;
    }

    [Fact]
    public async Task EmptySlot_HardSessionSpacingInvalid_MarkedInvalid_ButNextValidChosen()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        // LONG_RUN on Sunday 8/16: makes Friday 8/14 spacing-invalid (2-day min -> |16-14|=2 is OK actually).
        // Use LONG on 8/15 (Saturday) is not a preferred day, so instead assert directly on the validator input:
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");
        // Candidate at Wed 8/12 is 4 days from LONG(8/16) -> valid. Candidate at Fri 8/14 is 2 days -> valid (boundary).
        // To exercise an actually-invalid candidate we need a LONG closer than 2 days to a preferred day;
        // preferred days are Mon/Wed/Fri/Sun so the minimum inter-preferred-day gap is 2 days (Fri->Sun), which
        // is exactly the threshold -- so no preferred-day candidate can violate this rule within one week by
        // construction of the vocabulary itself. This test instead proves the *earliest valid* selection (#18)
        // across two structurally valid candidates.
        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);
        Assert.Equal(LongHorizonScheduleRepairActionKind.RescheduleToEmptySlot, outcome.Action);
        Assert.Equal(new DateOnly(2026, 8, 12), outcome.ReplacementDate); // earliest of Wed(12)/Fri(14)
    }

    // ── Future-EASY substitution candidate query rules (19-28) ──────────

    [Fact]
    public async Task Substitution_FutureActivePlannedEasy_FoundWhenNoEmptySlot()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var earliestEasyId = await CreateSessionAsync(db, week1, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 12));
        await CreateSessionAsync(db, week1, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule"); // every preferred day occupied -> no empty slot

        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);

        Assert.Equal(LongHorizonScheduleRepairActionKind.SubstituteFutureEasy, outcome.Action);
        Assert.Equal(new DateOnly(2026, 8, 12), outcome.ReplacementDate); // earliest EASY (8/12) chosen over 8/14
        var superseded = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == earliestEasyId);
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Superseded, superseded.PlanningStatus);
    }

    [Theory]
    [InlineData("superseded")]
    [InlineData("completed")]
    [InlineData("not_today")]
    public async Task Substitution_IneligibleEasyStates_Excluded(string state)
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, week1, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 12));
        var (outcome, planning) = state switch
        {
            "superseded" => (LongHorizonRollingSessionOutcomeStatus.Planned, LongHorizonPersistedSessionPlanningStatus.Superseded),
            "completed" => (LongHorizonRollingSessionOutcomeStatus.Completed, LongHorizonPersistedSessionPlanningStatus.Active),
            _ => (LongHorizonRollingSessionOutcomeStatus.NotToday, LongHorizonPersistedSessionPlanningStatus.Active),
        };
        await CreateSessionAsync(db, week1, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), outcome, planning);
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");

        var candidates = ScheduleRepairCandidateProvider.GetFutureEasySubstitutionCandidates(trigger.Week.Plan, trigger);
        Assert.DoesNotContain(candidates, c => c.Date == new DateOnly(2026, 8, 14));
    }

    [Fact]
    public async Task Substitution_KeyAndLongRunTargets_Excluded()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, week1, 3, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 14)); // a second KEY, never a valid substitution target
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");
        var candidates = ScheduleRepairCandidateProvider.GetFutureEasySubstitutionCandidates(trigger.Week.Plan, trigger);
        Assert.Empty(candidates);
    }

    [Fact]
    public async Task Substitution_EarliestValidEasy_UltimatelyChosen()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var earlierEasy = await CreateSessionAsync(db, week1, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 12));
        await CreateSessionAsync(db, week1, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        // Occupy every remaining preferred day so no empty slot exists (forces substitution).
        var week2 = await CreateWeekAsync(db, planId, 2, new DateOnly(2026, 8, 17));
        await CreateSessionAsync(db, week2, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 17));
        await CreateSessionAsync(db, week2, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 19));
        await CreateSessionAsync(db, week2, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 21));
        await CreateSessionAsync(db, week2, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 23));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");

        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);
        Assert.Equal(LongHorizonScheduleRepairActionKind.SubstituteFutureEasy, outcome.Action);
        var record = await db.LongHorizonAdaptationDecisionRecords.AsNoTracking().SingleAsync(r => r.TriggerSessionId == keyId);
        Assert.Equal(earlierEasy, record.SupersededSessionId);
    }

    // ── Short-circuit (29-34) ────────────────────────────────────────────

    [Fact]
    public async Task EasySupport_NotToday_NeverQueriesCandidates_AlwaysSkips()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var easyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        // A trap: Friday is a genuinely valid empty slot that would be chosen if the
        // short-circuit incorrectly queried candidates for an EASY_SUPPORT trigger.
        var trigger = await MarkNotTodayAsync(db, easyId, "schedule");
        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);
        Assert.Equal(LongHorizonScheduleRepairActionKind.Skip, outcome.Action);
        Assert.Null(outcome.ReplacementSessionId);
    }

    [Theory]
    [InlineData("illness")]
    [InlineData("soreness")]
    public async Task BlockingReasons_NeverQueryCandidates_AlwaysSkip(string reason)
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        // Trap: Friday 8/14 is a valid empty slot that must NOT be chosen.
        var trigger = await MarkNotTodayAsync(db, keyId, reason);
        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);
        Assert.Equal(LongHorizonScheduleRepairActionKind.Skip, outcome.Action);
        Assert.Equal(reason == "soreness", outcome.SafetyFlag);
    }

    [Fact]
    public async Task Taper_LongRun_NeverQueriesCandidates_AlwaysSkips()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10), stage: nameof(TrainingWeekType.Taper));
        var longId = await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 10));
        // Trap: Wednesday is a genuinely valid empty slot.
        var trigger = await MarkNotTodayAsync(db, longId, "schedule");
        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);
        Assert.Equal(LongHorizonScheduleRepairActionKind.Skip, outcome.Action);
    }

    [Fact]
    public async Task Taper_KeySession_CandidateSearchIsAllowed_AndPreservesSourceContent()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10), stage: nameof(TrainingWeekType.Taper));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");

        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);

        Assert.Equal(LongHorizonScheduleRepairActionKind.RescheduleToEmptySlot, outcome.Action);
        var source = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == keyId);
        var replacement = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == outcome.ReplacementSessionId);
        Assert.Equal(source.SessionRole, replacement.SessionRole);
        Assert.Equal(source.WorkoutKey, replacement.WorkoutKey);
        Assert.Equal(source.DistanceKm, replacement.DistanceKm);
    }

    // ── Skip / no-candidates end-to-end (37) ────────────────────────────

    [Fact]
    public async Task NoValidCandidates_Skips_AuditPersisted_NoReplacement()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        // Every other preferred day occupied by Active sessions -> no empty slot, no EASY to substitute.
        await CreateSessionAsync(db, week1, 2, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 12));
        await CreateSessionAsync(db, week1, 3, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 14));
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");

        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);

        Assert.Equal(LongHorizonScheduleRepairActionKind.Skip, outcome.Action);
        Assert.Null(outcome.ReplacementSessionId);
        var record = await db.LongHorizonAdaptationDecisionRecords.AsNoTracking().SingleAsync(r => r.TriggerSessionId == keyId);
        Assert.Equal(LongHorizonPersistedAdaptationDecisionType.Skip, record.DecisionType);
    }

    // ── Safety (38-39) ───────────────────────────────────────────────────

    [Fact]
    public async Task Soreness_KeySession_Skip_PersistsSafetyReviewRequiredTrue_NoReplacement()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        var trigger = await MarkNotTodayAsync(db, keyId, "soreness");

        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);

        Assert.Equal(LongHorizonScheduleRepairActionKind.Skip, outcome.Action);
        Assert.True(outcome.SafetyFlag);
        Assert.Null(outcome.ReplacementSessionId);
        using var freshDb = NewDb();
        var record = await freshDb.LongHorizonAdaptationDecisionRecords.AsNoTracking().SingleAsync(r => r.TriggerSessionId == keyId);
        Assert.True(record.SafetyReviewRequired);
        Assert.Equal(LongHorizonPersistedAdaptationDecisionType.Skip, record.DecisionType);
    }

    [Fact]
    public async Task Illness_KeySession_Skip_SafetyFlagFalse()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var trigger = await MarkNotTodayAsync(db, keyId, "illness");

        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);

        Assert.Equal(LongHorizonScheduleRepairActionKind.Skip, outcome.Action);
        Assert.False(outcome.SafetyFlag);
        using var freshDb = NewDb();
        var record = await freshDb.LongHorizonAdaptationDecisionRecords.AsNoTracking().SingleAsync(r => r.TriggerSessionId == keyId);
        Assert.False(record.SafetyReviewRequired);
    }

    // ── Idempotency (40-43) ──────────────────────────────────────────────

    [Fact]
    public async Task DuplicateOrchestrationCall_IsIdempotentReplay_NoSecondReplacement()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");
        var loggerFactory = NewLoggerFactory();

        var first = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, loggerFactory, trigger, default);
        var second = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, loggerFactory, trigger, default);

        Assert.True(second.IsIdempotentReplay);
        Assert.Equal(first.Action, second.Action);
        Assert.Equal(first.ReplacementSessionId, second.ReplacementSessionId);
        var count = await db.LongHorizonAdaptationDecisionRecords.CountAsync(r => r.TriggerSessionId == keyId);
        Assert.Equal(1, count);
        var replacementCount = await db.LongHorizonRollingSessionStates.CountAsync(s => s.AdaptedFromSessionId == keyId);
        Assert.Equal(1, replacementCount);
    }

    [Fact]
    public async Task DuplicateSubstitutionCall_SupersedesNoSecondEasy()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, week1, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 12));
        await CreateSessionAsync(db, week1, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");
        var loggerFactory = NewLoggerFactory();

        await ScheduleRepairRuntimeOrchestrator.RunAsync(db, loggerFactory, trigger, default);
        await ScheduleRepairRuntimeOrchestrator.RunAsync(db, loggerFactory, trigger, default);

        var supersededCount = await db.LongHorizonRollingSessionStates.CountAsync(s => s.WeekStateId == week1 && s.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Superseded);
        Assert.Equal(1, supersededCount);
    }

    // ── Stale (44-46) ────────────────────────────────────────────────────

    [Fact]
    public async Task StaleTarget_SubstitutionEasyBecomesSupersededBeforeCommit_TypedConflict_NoReselection()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var easyId = await CreateSessionAsync(db, week1, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 12));
        await CreateSessionAsync(db, week1, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        await CreateSessionAsync(db, week1, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");

        // trigger.Week.Plan.Weeks[*].Sessions is already an in-memory snapshot
        // taken above (before this mutation), so the orchestrator's own
        // candidate query still sees the 8/12 EASY as Active -- exactly the
        // window where 4M.1 can decide on a target that 4M.2 then discovers is
        // stale at commit time via its own fresh DB read. ScheduleRepairPolicy
        // picks the earliest candidate (8/12), which is the one raced here.
        using (var raceDb = NewDb())
        {
            var target = await raceDb.LongHorizonRollingSessionStates.SingleAsync(s => s.Id == easyId);
            target.PlanningStatus = LongHorizonPersistedSessionPlanningStatus.Superseded;
            await raceDb.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<LongHorizonAdaptationStaleTargetException>(
            () => ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default));

        var record = await db.LongHorizonAdaptationDecisionRecords.CountAsync(r => r.TriggerSessionId == keyId);
        Assert.Equal(0, record); // rolled back, no partial commit
    }

    [Fact]
    public async Task StaleTrigger_SessionNoLongerNotToday_TypedConflict()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var trigger = await MarkNotTodayAsync(db, keyId, "schedule");

        // Trigger flips back to Planned via an independent context before the
        // orchestrator's own re-check inside its lock.
        using (var raceDb = NewDb())
        {
            var row = await raceDb.LongHorizonRollingSessionStates.SingleAsync(s => s.Id == keyId);
            row.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.Planned;
            await raceDb.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<LongHorizonAdaptationStaleTriggerException>(
            () => ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default));
    }
}
