using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.4A -- real-PostgreSQL integration tests for the live
/// checkpoint chain: WindowCheckpointEvidenceMapper (real persisted rows)
/// -> WindowExecutionSummaryBuilder (frozen 4M.1) -> NextWindowLoadDecisionPolicy
/// (frozen 4M.1). Uses the same hand-built LongHorizonRollingPlanState
/// fixture style as Phase 4M.2/4M.3's own tests -- no TrainingPlans/real
/// generation needed, since this chain only ever reads
/// LongHorizonRollingSessionStates/-WeekStates/-PlanStates.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class WindowCheckpointSummaryAndDecisionTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private readonly List<Guid> _createdPlanIds = [];

    public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
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

    private async Task<Guid> CreatePlanAsync(AppDbContext db)
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
            CurrentWindowStartWeek = 1, CurrentWindowEndWeek = 1,
            ActiveContextVersionSequence = 1, ActiveContextVersionId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        _createdPlanIds.Add(id);
        return id;
    }

    private async Task<Guid> CreateWeekAsync(AppDbContext db, Guid planId, int globalWeek, DateOnly mondayStart)
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingWeekStates.Add(new LongHorizonRollingWeekState
        {
            Id = id, PlanStateId = planId, GlobalWeek = globalWeek,
            SegmentType = LongHorizonPersistedSegmentType.Core, Stage = "Build",
            StructuralStartDate = mondayStart, StructuralEndDate = mondayStart.AddDays(6),
            LifecycleState = LongHorizonPersistedLifecycleState.NumericActivated, WeeklyVolumeKm = 25, LongRunKm = 8,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> CreateSessionAsync(AppDbContext db, Guid weekId, int ordinal, PreparationRunwaySlotRole role, DateOnly date,
        LongHorizonRollingSessionOutcomeStatus outcome, LongHorizonPersistedSessionPlanningStatus planningStatus = LongHorizonPersistedSessionPlanningStatus.Active,
        string? notTodayReason = null, Guid? adaptedFromId = null)
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingSessionStates.Add(new LongHorizonRollingSessionState
        {
            Id = id, WeekStateId = weekId, SessionOrdinal = ordinal,
            SessionRole = LongHorizonSessionRoleCodec.ToCanonicalToken(role), WorkoutKey = "STANDARD", WorkoutVersion = 6,
            DistanceKm = 7, AssignedDate = date, ActivationContextVersionSequence = 1, Provenance = "generated_from_initial_profile",
            OutcomeStatus = outcome, PlanningStatus = planningStatus, NotTodayReason = notTodayReason,
            NotTodayRecordedAtUtc = outcome == LongHorizonRollingSessionOutcomeStatus.NotToday ? DateTime.UtcNow : null,
            AdaptedFromSessionId = adaptedFromId,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static (WindowExecutionSummary Summary, NextWindowAdaptationResultTestView Decision) Evaluate(IReadOnlyList<LongHorizonRollingSessionState> sessions)
    {
        var summary = WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(sessions));
        var decision = NextWindowLoadDecisionPolicy.Evaluate(summary);
        return (summary, new NextWindowAdaptationResultTestView(decision.LoadDecision.ToString(), decision.SafetyReviewRequired));
    }

    private readonly record struct NextWindowAdaptationResultTestView(string LoadDecision, bool SafetyReviewRequired);

    // ── WINDOW SUMMARY (1-9) ─────────────────────────────────────────────

    [Fact]
    public async Task AllFourCompleted_Expected4_EffectiveCompleted4()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var key = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Completed);
        var easy1 = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.Completed);
        var easy2 = await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        var longRun = await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (summary, decision) = Evaluate(sessions);

        Assert.Equal(4, summary.ExpectedSessionCount);
        Assert.Equal(4, summary.EffectiveCompletedCount);
        Assert.Equal("ProgressAsPlanned", decision.LoadDecision);
        _ = (key, easy1, easy2, longRun);
    }

    [Fact]
    public async Task OneEasyUnrecoveredNotToday_Expected4_EffectiveCompleted3_EasyExpected2Completed1()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "illness");
        await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (summary, decision) = Evaluate(sessions);

        Assert.Equal(4, summary.ExpectedSessionCount);
        Assert.Equal(3, summary.EffectiveCompletedCount);
        Assert.Equal(2, summary.EasyExpectedCount);
        Assert.Equal(1, summary.EasyCompletedCount);
        Assert.Equal(1, summary.UnrecoveredNotTodayCount);
        Assert.Equal("ProgressAsPlanned", decision.LoadDecision); // 3/4, only EASY missing
    }

    [Fact]
    public async Task KeyNotTodayRepairedByCompletedReplacement_KeyCompletedTrue_NoUnrecovered()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyNotToday = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "schedule");
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);
        // KEY replacement lands on Friday, Completed, AdaptedFrom the NotToday KEY.
        await CreateSessionAsync(db, weekId, 5, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed, adaptedFromId: keyNotToday);
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (summary, _) = Evaluate(sessions);

        Assert.True(summary.KeySessionExpected);
        Assert.True(summary.KeySessionCompleted);
        Assert.Equal(0, summary.UnrecoveredNotTodayCount);
    }

    [Fact]
    public async Task LongNotTodayRepairedByCompletedReplacement_LongCompletedTrue_NoUnrecovered()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.Completed);
        var longNotToday = await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "weather");
        await CreateSessionAsync(db, weekId, 5, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed, adaptedFromId: longNotToday);
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (summary, _) = Evaluate(sessions);

        Assert.True(summary.LongRunExpected);
        Assert.True(summary.LongRunCompleted);
        Assert.Equal(0, summary.UnrecoveredNotTodayCount);
    }

    /// <summary>Section E's exact locked reference scenario.</summary>
    [Fact]
    public async Task LockedRev31Scenario_ProducesExactCanonicalSummary()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        // Mon EASY -> Completed
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.Completed);
        // Wed KEY -> NotToday
        var wedKey = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "schedule");
        // Fri EASY -> Superseded (its slot consumed by the KEY replacement)
        var friEasy = await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Planned, LongHorizonPersistedSessionPlanningStatus.Superseded);
        // Fri KEY replacement, AdaptedFrom Wed KEY -> Completed
        await CreateSessionAsync(db, weekId, 5, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed, adaptedFromId: wedKey);
        // Sun LONG -> Completed
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (summary, _) = Evaluate(sessions);

        Assert.Equal(4, summary.ExpectedSessionCount);
        Assert.Equal(3, summary.EffectiveCompletedCount);
        Assert.True(summary.KeySessionExpected);
        Assert.True(summary.KeySessionCompleted);
        Assert.True(summary.LongRunExpected);
        Assert.True(summary.LongRunCompleted);
        Assert.Equal(2, summary.EasyExpectedCount);
        Assert.Equal(1, summary.EasyCompletedCount);
        Assert.Equal(1, summary.SupersededByAdaptationCount);
        Assert.Equal(0, summary.UnrecoveredNotTodayCount);
        _ = friEasy;

        // Fresh-DbContext reproduction (items 31/32): same summary from a
        // brand new scope reading the same persisted rows.
        using var freshDb = NewDb();
        var freshSessions = await freshDb.LongHorizonRollingSessionStates.AsNoTracking().Where(s => s.WeekStateId == weekId).ToListAsync();
        var (freshSummary, freshDecision) = Evaluate(freshSessions);
        Assert.Equal(summary, freshSummary);
        // 3/4 effective, KEY and LONG both satisfied, only EASY missing -> ProgressAsPlanned.
        Assert.Equal("ProgressAsPlanned", freshDecision.LoadDecision);
    }

    // ── SAFETY (10-14) ───────────────────────────────────────────────────

    [Fact]
    public async Task NoSafetyDecisions_HasSafetyFlagFalse()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (summary, decision) = Evaluate(sessions);
        Assert.False(summary.HasSafetyFlag);
        Assert.False(decision.SafetyReviewRequired);
    }

    [Fact]
    public async Task OneSorenessNotToday_HasSafetyFlagTrue_SafetyReviewRequiredTrue()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "soreness");
        await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (summary, decision) = Evaluate(sessions);
        Assert.True(summary.HasSafetyFlag);
        Assert.True(decision.SafetyReviewRequired);
        // safety true + 3/4 (only EASY missing) -> ProgressAsPlanned + SafetyReviewRequired=true (independent)
        Assert.Equal("ProgressAsPlanned", decision.LoadDecision);
    }

    [Fact]
    public async Task SafetyTrue_MaintainScenario_BothIndependentlyTrue()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        // 2/4 effective (Maintain), one of which is a soreness NotToday (Safety).
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "soreness");
        await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "weather");
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (summary, decision) = Evaluate(sessions);
        Assert.Equal(2, summary.EffectiveCompletedCount);
        Assert.True(decision.SafetyReviewRequired);
        Assert.Equal("Maintain", decision.LoadDecision);
    }

    [Fact]
    public async Task SafetyTrue_ReduceScenario_BothIndependentlyTrue()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        // 1/4 effective (Reduce), the completed one has no safety implication,
        // but one of the misses is soreness.
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "soreness");
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "weather");
        await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "fatigue");
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (summary, decision) = Evaluate(sessions);
        Assert.Equal(1, summary.EffectiveCompletedCount);
        Assert.True(decision.SafetyReviewRequired);
        Assert.Equal("Reduce", decision.LoadDecision);
    }

    // ── LOAD DECISION MATRIX (15-22) ────────────────────────────────────

    [Theory]
    [InlineData(0, "Reduce")]
    [InlineData(1, "Reduce")]
    [InlineData(2, "Maintain")]
    [InlineData(4, "ProgressAsPlanned")]
    public async Task LoadDecisionMatrix_ByEffectiveCompletedCount(int completedCount, string expectedDecision)
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var roles = new[] { PreparationRunwaySlotRole.KeySession, PreparationRunwaySlotRole.EasySupport, PreparationRunwaySlotRole.EasySupport, PreparationRunwaySlotRole.LongRun };
        var dates = new[] { new DateOnly(2026, 8, 12), new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 14), new DateOnly(2026, 8, 16) };
        for (var i = 0; i < 4; i++)
        {
            var outcome = i < completedCount ? LongHorizonRollingSessionOutcomeStatus.Completed : LongHorizonRollingSessionOutcomeStatus.NotToday;
            await CreateSessionAsync(db, weekId, i + 1, roles[i], dates[i], outcome, notTodayReason: outcome == LongHorizonRollingSessionOutcomeStatus.NotToday ? "schedule" : null);
        }
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (_, decision) = Evaluate(sessions);
        Assert.Equal(expectedDecision, decision.LoadDecision);
    }

    [Fact]
    public async Task ThreeCompleted_OnlyEasyMissing_ProgressAsPlanned()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "schedule");
        await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();
        var (_, decision) = Evaluate(sessions);
        Assert.Equal("ProgressAsPlanned", decision.LoadDecision);
    }

    [Fact]
    public async Task ThreeCompleted_MissingKey_Maintain()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "schedule");
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();
        var (_, decision) = Evaluate(sessions);
        Assert.Equal("Maintain", decision.LoadDecision);
    }

    [Fact]
    public async Task ThreeCompleted_MissingLong_Maintain()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "schedule");
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();
        var (_, decision) = Evaluate(sessions);
        Assert.Equal("Maintain", decision.LoadDecision);
    }

    [Fact]
    public async Task SupersededByAdaptationCountAlone_DoesNotAlterDecision()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        // EASY superseded to satisfy the KEY replacement: the superseded
        // EASY's own expected slot stays neutral (never completed), so this
        // is the same 3/4-only-EASY-missing shape as the locked Rev3.1
        // scenario -- SupersededByAdaptationCount=1 must not itself push the
        // decision toward Reduce/Maintain beyond what 3/4 already implies.
        var keyNotToday = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "schedule");
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Planned, LongHorizonPersistedSessionPlanningStatus.Superseded);
        await CreateSessionAsync(db, weekId, 5, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.Completed, adaptedFromId: keyNotToday);
        await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);
        var sessions = await db.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekId).ToListAsync();

        var (summary, decision) = Evaluate(sessions);
        Assert.Equal(1, summary.SupersededByAdaptationCount);
        Assert.Equal(3, summary.EffectiveCompletedCount);
        Assert.Equal("ProgressAsPlanned", decision.LoadDecision);
    }
}
