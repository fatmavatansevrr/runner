using System.Runtime.CompilerServices;
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
/// Phase 4M.5C -- Rev5 §7a "Multi-Week Window Aggregation" (B-weekly-summary
/// + B1 worst-week-wins + original-week lineage attribution). Real
/// Postgres-backed multi-structural-week fixtures (same hand-built fixture
/// style as WindowCheckpointSummaryAndDecisionTests -- a real DB, real
/// WindowCheckpointEvidenceMapper/WindowExecutionSummaryBuilder/
/// NextWindowLoadDecisionPolicy, exercised through the new
/// WeeklyWindowPartitioner/WeeklyLoadDecisionAggregator authorities exactly
/// as LongHorizonRollingWindowActivationService now calls them), proving the
/// aggregation logic itself independent of the full activate-next-window
/// HTTP/JIT/composition chain (which the pre-existing
/// LongHorizonThreeWindowAnchorThreadingE2ETests /
/// LongHorizonNextWindowDecisionActivationTests suites already exercise
/// end-to-end, updated in this phase for the corrected weekly semantics).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class WeeklyLoadDecisionAggregationTests : IAsyncLifetime
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
            Id = id, TotalWeeks = 30, ReadinessProfile = "CoreEntryReady",
            StartDate = new DateOnly(2026, 8, 10), RaceDate = new DateOnly(2027, 3, 8),
            GoalType = "Race", GoalDistance = "TenK", Level = "Intermediate", DaysPerWeek = 4,
            PreferredDaysCsv = "Monday,Wednesday,Friday,Sunday", LongRunDay = "Sunday",
            CandidateKey = "TEN_K__4D__INTERMEDIATE", CandidateVersion = 10, CatalogRootPath = "test",
            CurrentLifecycleStatus = LongHorizonPersistedLifecycleState.NumericActivated,
            CurrentWindowStartWeek = 1, CurrentWindowEndWeek = 4,
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
            Id = id, PlanStateId = planId, GlobalWeek = globalWeek,
            SegmentType = segment, Stage = stage,
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

    /// <summary>Creates one ordinary structural week (KEY/EASY/EASY/LONG on
    /// Mon/Wed/Fri/Sun) whose four sessions are Completed/NotToday according
    /// to <paramref name="completedCount"/> (0-4, filled Key/Easy/Easy/Long
    /// in that order), matching the same role/date layout
    /// WindowCheckpointSummaryAndDecisionTests already uses.</summary>
    private async Task<Guid> CreateOrdinaryWeekAsync(AppDbContext db, Guid planId, int globalWeek, DateOnly mondayStart, int completedCount)
    {
        var weekId = await CreateWeekAsync(db, planId, globalWeek, mondayStart);
        var roles = new[] { PreparationRunwaySlotRole.KeySession, PreparationRunwaySlotRole.EasySupport, PreparationRunwaySlotRole.EasySupport, PreparationRunwaySlotRole.LongRun };
        var dates = new[] { mondayStart.AddDays(2), mondayStart, mondayStart.AddDays(4), mondayStart.AddDays(6) };
        for (var i = 0; i < 4; i++)
        {
            var outcome = i < completedCount ? LongHorizonRollingSessionOutcomeStatus.Completed : LongHorizonRollingSessionOutcomeStatus.NotToday;
            await CreateSessionAsync(db, weekId, i + 1, roles[i], dates[i], outcome,
                notTodayReason: outcome == LongHorizonRollingSessionOutcomeStatus.NotToday ? "schedule" : null);
        }
        return weekId;
    }

    /// <summary>Creates one ordinary structural week where only the two
    /// EASY_SUPPORT sessions are Completed and KEY/LONG are both NotToday --
    /// the exact per-week shape Section K's core regression requires (2/4
    /// completed, but with Easy completed and Key/Long missing, distinct
    /// from CreateOrdinaryWeekAsync's Key-then-Easy-then-Easy-then-Long
    /// fill order, which would give a different completed set at
    /// completedCount=2).</summary>
    private async Task<Guid> CreateTwoEasyOnlyWeekAsync(AppDbContext db, Guid planId, int globalWeek, DateOnly mondayStart)
    {
        var weekId = await CreateWeekAsync(db, planId, globalWeek, mondayStart);
        await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, mondayStart.AddDays(2), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "schedule");
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, mondayStart, LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, mondayStart.AddDays(4), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, mondayStart.AddDays(6), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "schedule");
        return weekId;
    }

    private static (IReadOnlyList<NextWindowAdaptationResultTestView> WeeklyResults, NextWindowAdaptationResultTestView Aggregated) EvaluateWeekly(
        IReadOnlyList<LongHorizonRollingWeekState> weeks)
    {
        var groups = WeeklyWindowPartitioner.PartitionByStructuralWeekLineage(weeks);
        var weeklyResults = groups
            .Select(g => NextWindowLoadDecisionPolicy.Evaluate(WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(g))))
            .ToList();
        var aggregated = WeeklyLoadDecisionAggregator.AggregateWorstWeekWins(weeklyResults);
        return (
            weeklyResults.Select(r => new NextWindowAdaptationResultTestView(r.LoadDecision.ToString(), r.SafetyReviewRequired)).ToList(),
            new NextWindowAdaptationResultTestView(aggregated.LoadDecision.ToString(), aggregated.SafetyReviewRequired));
    }

    private readonly record struct NextWindowAdaptationResultTestView(string LoadDecision, bool SafetyReviewRequired);

    private async Task<IReadOnlyList<LongHorizonRollingWeekState>> LoadWindowAsync(AppDbContext db, Guid planId, int startWeek, int endWeek) =>
        await db.LongHorizonRollingWeekStates.AsNoTracking().Include(w => w.Sessions)
            .Where(w => w.PlanStateId == planId && w.GlobalWeek >= startWeek && w.GlobalWeek <= endWeek)
            .OrderBy(w => w.GlobalWeek).ToListAsync();

    // ── L1: single structural week -- aggregation is identity ──────────────

    [Fact]
    public async Task L1_SingleStructuralWeek_AggregationIsIdentity_MatchesDirectPolicyEvaluation()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        await CreateOrdinaryWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10), completedCount: 2);
        var weeks = await LoadWindowAsync(db, planId, 1, 1);

        var (weeklyResults, aggregated) = EvaluateWeekly(weeks);

        Assert.Single(weeklyResults);
        Assert.Equal(weeklyResults[0], aggregated);
        Assert.Equal("Maintain", aggregated.LoadDecision);
    }

    // ── L2: four weeks, all Progress ────────────────────────────────────────

    [Fact]
    public async Task L2_FourWeeks_AllProgress_AggregatesToProgressAsPlanned()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        for (var w = 1; w <= 4; w++)
            await CreateOrdinaryWeekAsync(db, planId, w, new DateOnly(2026, 8, 10).AddDays((w - 1) * 7), completedCount: 4);
        var weeks = await LoadWindowAsync(db, planId, 1, 4);

        var (weeklyResults, aggregated) = EvaluateWeekly(weeks);

        Assert.All(weeklyResults, r => Assert.Equal("ProgressAsPlanned", r.LoadDecision));
        Assert.Equal("ProgressAsPlanned", aggregated.LoadDecision);
    }

    // ── L3/L4: recency-blindness (Reduce first vs. Reduce last) ─────────────

    [Fact]
    public async Task L3_ReduceFirst_AggregatesToReduce()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        await CreateOrdinaryWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10), completedCount: 0);
        for (var w = 2; w <= 4; w++)
            await CreateOrdinaryWeekAsync(db, planId, w, new DateOnly(2026, 8, 10).AddDays((w - 1) * 7), completedCount: 4);
        var weeks = await LoadWindowAsync(db, planId, 1, 4);

        var (_, aggregated) = EvaluateWeekly(weeks);
        Assert.Equal("Reduce", aggregated.LoadDecision);
    }

    [Fact]
    public async Task L4_ReduceLast_AggregatesToReduce_SameAsReduceFirst()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        for (var w = 1; w <= 3; w++)
            await CreateOrdinaryWeekAsync(db, planId, w, new DateOnly(2026, 8, 10).AddDays((w - 1) * 7), completedCount: 4);
        await CreateOrdinaryWeekAsync(db, planId, 4, new DateOnly(2026, 8, 10).AddDays(3 * 7), completedCount: 0);
        var weeks = await LoadWindowAsync(db, planId, 1, 4);

        var (_, aggregated) = EvaluateWeekly(weeks);
        // Frozen recency-blindness proof (Rev5 §7a): identical final decision
        // regardless of whether the bad week is first or last.
        Assert.Equal("Reduce", aggregated.LoadDecision);
    }

    // ── L5: Maintain aggregation ─────────────────────────────────────────

    [Fact]
    public async Task L5a_OneMaintainWeek_RestProgress_AggregatesToMaintain()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        await CreateOrdinaryWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10), completedCount: 4);
        await CreateOrdinaryWeekAsync(db, planId, 2, new DateOnly(2026, 8, 10).AddDays(7), completedCount: 2);
        await CreateOrdinaryWeekAsync(db, planId, 3, new DateOnly(2026, 8, 10).AddDays(14), completedCount: 4);
        await CreateOrdinaryWeekAsync(db, planId, 4, new DateOnly(2026, 8, 10).AddDays(21), completedCount: 4);
        var weeks = await LoadWindowAsync(db, planId, 1, 4);

        var (_, aggregated) = EvaluateWeekly(weeks);
        Assert.Equal("Maintain", aggregated.LoadDecision);
    }

    [Fact]
    public async Task L5b_TwoMaintainWeeks_RestProgress_AggregatesToMaintain()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        await CreateOrdinaryWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10), completedCount: 2);
        await CreateOrdinaryWeekAsync(db, planId, 2, new DateOnly(2026, 8, 10).AddDays(7), completedCount: 2);
        await CreateOrdinaryWeekAsync(db, planId, 3, new DateOnly(2026, 8, 10).AddDays(14), completedCount: 4);
        await CreateOrdinaryWeekAsync(db, planId, 4, new DateOnly(2026, 8, 10).AddDays(21), completedCount: 4);
        var weeks = await LoadWindowAsync(db, planId, 1, 4);

        var (_, aggregated) = EvaluateWeekly(weeks);
        Assert.Equal("Maintain", aggregated.LoadDecision);
    }

    // ── L6: mixed severity ───────────────────────────────────────────────

    [Fact]
    public async Task L6_MixedSeverity_ProgressMaintainReduceProgress_AggregatesToReduce()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        await CreateOrdinaryWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10), completedCount: 4);
        await CreateOrdinaryWeekAsync(db, planId, 2, new DateOnly(2026, 8, 10).AddDays(7), completedCount: 2);
        await CreateOrdinaryWeekAsync(db, planId, 3, new DateOnly(2026, 8, 10).AddDays(14), completedCount: 0);
        await CreateOrdinaryWeekAsync(db, planId, 4, new DateOnly(2026, 8, 10).AddDays(21), completedCount: 4);
        var weeks = await LoadWindowAsync(db, planId, 1, 4);

        var (weeklyResults, aggregated) = EvaluateWeekly(weeks);
        Assert.Equal(["ProgressAsPlanned", "Maintain", "Reduce", "ProgressAsPlanned"], weeklyResults.Select(r => r.LoadDecision).ToArray());
        Assert.Equal("Reduce", aggregated.LoadDecision);
    }

    // ── L7: Safety OR aggregation, independent of LoadDecision ──────────────

    [Fact]
    public async Task L7_SafetyOnOneWeek_ORAggregatesTrue_LoadDecisionIndependentlyDetermined()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        // Weeks 1/3/4 fully completed, no safety. Week 2 has a soreness
        // NotToday on an EASY slot (safety=true) but is otherwise 3/4
        // completed with only Easy missing -> weekly decision still
        // ProgressAsPlanned for that week (Rev3 §7's own "only Easy
        // missing" branch), matching Rev5 §7a's own worked H example: the
        // window can be all-ProgressAsPlanned with SafetyReviewRequired
        // still independently true.
        await CreateOrdinaryWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10), completedCount: 4);
        var week2Id = await CreateWeekAsync(db, planId, 2, new DateOnly(2026, 8, 10).AddDays(7));
        await CreateSessionAsync(db, week2Id, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 19), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, week2Id, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 17), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "soreness");
        await CreateSessionAsync(db, week2Id, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 21), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, week2Id, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 23), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateOrdinaryWeekAsync(db, planId, 3, new DateOnly(2026, 8, 10).AddDays(14), completedCount: 4);
        await CreateOrdinaryWeekAsync(db, planId, 4, new DateOnly(2026, 8, 10).AddDays(21), completedCount: 4);
        var weeks = await LoadWindowAsync(db, planId, 1, 4);

        var (weeklyResults, aggregated) = EvaluateWeekly(weeks);
        Assert.Equal(["ProgressAsPlanned", "ProgressAsPlanned", "ProgressAsPlanned", "ProgressAsPlanned"], weeklyResults.Select(r => r.LoadDecision).ToArray());
        Assert.True(weeklyResults[1].SafetyReviewRequired);
        Assert.False(weeklyResults[0].SafetyReviewRequired);
        Assert.Equal("ProgressAsPlanned", aggregated.LoadDecision);
        Assert.True(aggregated.SafetyReviewRequired);
    }

    // ── L8: THE CORE REGRESSION (Section K) ─────────────────────────────

    /// <summary>
    /// Section K -- the primary regression that motivated Revision 5. Every
    /// real structural week: 2 EASY completed, KEY and LONG both NotToday.
    /// Across the whole 16-session window: 8/16 Easy, 0/16 Key, 0/16 Long.
    /// OLD BROKEN direct-multi-week-summary behavior would read
    /// EffectiveCompletedCount=8 >= 4 -> ProgressAsPlanned (role-blind).
    /// Rev5 §7a's correct behavior: each week independently evaluates to
    /// Maintain (2/4 completed), so B1 worst-week-wins yields Maintain for
    /// the whole window -- deterministically, not merely "not
    /// ProgressAsPlanned".
    /// </summary>
    [Fact]
    public async Task L8_EightEasyZeroKeyZeroLong_AggregatesToMaintain_NotProgressAsPlanned()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        for (var w = 1; w <= 4; w++)
            await CreateTwoEasyOnlyWeekAsync(db, planId, w, new DateOnly(2026, 8, 10).AddDays((w - 1) * 7));
        var weeks = await LoadWindowAsync(db, planId, 1, 4);

        var (weeklyResults, aggregated) = EvaluateWeekly(weeks);

        Assert.All(weeklyResults, r => Assert.Equal("Maintain", r.LoadDecision));
        Assert.Equal("Maintain", aggregated.LoadDecision);

        // Deterministic whole-window evidence check, independent of the
        // per-week/aggregation logic under test: 8 Easy / 0 Key / 0 Long.
        var allSessions = weeks.SelectMany(w => w.Sessions).ToList();
        Assert.Equal(8, allSessions.Count(s => s.SessionRole == LongHorizonSessionRoleCodec.ToCanonicalToken(PreparationRunwaySlotRole.EasySupport) && s.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Completed));
        Assert.Equal(0, allSessions.Count(s => s.SessionRole == LongHorizonSessionRoleCodec.ToCanonicalToken(PreparationRunwaySlotRole.KeySession) && s.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Completed));
        Assert.Equal(0, allSessions.Count(s => s.SessionRole == LongHorizonSessionRoleCodec.ToCanonicalToken(PreparationRunwaySlotRole.LongRun) && s.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Completed));
    }

    // ── L9: cross-week RescheduleToEmptySlot lineage ────────────────────

    /// <summary>
    /// A Week 1 KEY marked NotToday, repaired via RescheduleToEmptySlot into
    /// a PHYSICAL Week 2 empty preferred-day slot (simulating what
    /// ScheduleRepairCandidateProvider's window-scoped, not week-scoped,
    /// candidate query -- confirmed in Phase 4M.5B §G -- can legitimately
    /// produce), Completed. Rev5 §7a Weekly Lineage Attribution Rule: the
    /// expectation stays owned by Week 1 regardless of the replacement's
    /// physical date.
    /// </summary>
    [Fact]
    public async Task L9_CrossWeekRescheduleToEmptySlot_ExpectationRemainsWeek1Owned()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1Id = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var week1Key = await CreateSessionAsync(db, week1Id, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "schedule");
        await CreateSessionAsync(db, week1Id, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, week1Id, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, week1Id, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);

        var week2Id = await CreateWeekAsync(db, planId, 2, new DateOnly(2026, 8, 17));
        // Week 2's own 4 sessions, unaffected by Week 1's repair.
        await CreateSessionAsync(db, week2Id, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 19), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, week2Id, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 17), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, week2Id, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 21), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, week2Id, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 23), LongHorizonRollingSessionOutcomeStatus.Completed);
        // Cross-week replacement: physically dated in Week 2's range, but
        // logically satisfies Week 1's missed KEY (AdaptedFrom points at
        // week1Key). RescheduleToEmptySlot creates no Superseded row.
        await CreateSessionAsync(db, week2Id, 5, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 18), LongHorizonRollingSessionOutcomeStatus.Completed, adaptedFromId: week1Key);

        var weeks = await LoadWindowAsync(db, planId, 1, 2);
        var groups = WeeklyWindowPartitioner.PartitionByStructuralWeekLineage(weeks);

        // Week 1's bucket must include the cross-week replacement (5
        // sessions: its own 4 roots + the replacement); Week 2's bucket must
        // NOT include it (still exactly its own 4 roots).
        Assert.Equal(5, groups[0].Count);
        Assert.Equal(4, groups[1].Count);
        Assert.DoesNotContain(groups[1], s => s.AdaptedFromSessionId == week1Key);

        var week1Summary = WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(groups[0]));
        var week2Summary = WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(groups[1]));
        Assert.Equal(4, week1Summary.ExpectedSessionCount); // never inflated to 5
        Assert.True(week1Summary.KeySessionCompleted); // satisfied via the cross-week replacement
        Assert.Equal(4, week1Summary.EffectiveCompletedCount);
        Assert.Equal(4, week2Summary.ExpectedSessionCount); // never gains a 5th expectation
        Assert.Equal(4, week2Summary.EffectiveCompletedCount);

        var (_, aggregated) = EvaluateWeekly(weeks);
        Assert.Equal("ProgressAsPlanned", aggregated.LoadDecision);
    }

    // ── L10: cross-week SubstituteFutureEasy lineage (Section D scenario) ──

    /// <summary>
    /// Section D's exact canonical scenario: Week 1 KEY NotToday, repaired
    /// via SubstituteFutureEasy into a Week 2 EASY slot (Week 2's original
    /// EASY becomes Superseded), replacement Completed. Required outcome:
    /// Week 1's denominator/completion absorbs the KEY via lineage; Week 2's
    /// denominator stays 4 (the Superseded original EASY remains part of
    /// its OWN week's neutral denominator per Rev3.1 §6, not deleted, and
    /// the physical replacement row does not become a 5th Week 2
    /// expectation).
    /// </summary>
    [Fact]
    public async Task L10_CrossWeekSubstituteFutureEasy_Week1AbsorbsCompletion_Week2DenominatorNotInflated()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var week1Id = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var week1Key = await CreateSessionAsync(db, week1Id, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12), LongHorizonRollingSessionOutcomeStatus.NotToday, notTodayReason: "schedule");
        await CreateSessionAsync(db, week1Id, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, week1Id, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, week1Id, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);

        var week2Id = await CreateWeekAsync(db, planId, 2, new DateOnly(2026, 8, 17));
        await CreateSessionAsync(db, week2Id, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 19), LongHorizonRollingSessionOutcomeStatus.Completed);
        // Week 2's original EASY -- Superseded (its slot consumed by the
        // cross-week KEY substitution).
        var week2OriginalEasy = await CreateSessionAsync(db, week2Id, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 17), LongHorizonRollingSessionOutcomeStatus.Planned, LongHorizonPersistedSessionPlanningStatus.Superseded);
        await CreateSessionAsync(db, week2Id, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 21), LongHorizonRollingSessionOutcomeStatus.Completed);
        await CreateSessionAsync(db, week2Id, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 23), LongHorizonRollingSessionOutcomeStatus.Completed);
        // Cross-week KEY replacement: physically Week 2's original EASY
        // slot, logically satisfies Week 1's missed KEY.
        await CreateSessionAsync(db, week2Id, 5, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 17), LongHorizonRollingSessionOutcomeStatus.Completed, adaptedFromId: week1Key);

        var weeks = await LoadWindowAsync(db, planId, 1, 2);
        var groups = WeeklyWindowPartitioner.PartitionByStructuralWeekLineage(weeks);
        var week1Summary = WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(groups[0]));
        var week2Summary = WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(groups[1]));

        // Week 1: source expectation completed via lineage.
        Assert.Equal(4, week1Summary.ExpectedSessionCount);
        Assert.True(week1Summary.KeySessionCompleted);
        Assert.Equal(4, week1Summary.EffectiveCompletedCount);

        // Week 2: denominator not inflated to 5; Superseded semantics
        // unchanged (still counted, still neutral, not Completed).
        Assert.Equal(4, week2Summary.ExpectedSessionCount);
        Assert.Equal(1, week2Summary.SupersededByAdaptationCount);
        Assert.Equal(3, week2Summary.EffectiveCompletedCount); // KEY(own) + Easy(own) + Long(own); Superseded Easy not counted Completed
        Assert.Equal(2, week2Summary.EasyExpectedCount); // both Week 2 EASY roots (Superseded + Active) remain in the denominator
        Assert.Equal(1, week2Summary.EasyCompletedCount); // only the non-Superseded Easy is Completed
        _ = week2OriginalEasy;

        var (_, aggregated) = EvaluateWeekly(weeks);
        // Week1 = ProgressAsPlanned (4/4). Week2 = 3/4, only Easy missing
        // (Key and Long both satisfied by their own week's roots) -> also
        // ProgressAsPlanned. B1 worst-of-two -> ProgressAsPlanned.
        Assert.Equal("ProgressAsPlanned", aggregated.LoadDecision);
    }

    // ── L11: mixed-phase / mixed-stage rolling window ───────────────────

    [Fact]
    public async Task L11_MixedStageWindow_PartitionRemainsCorrect_NoCrossWeekEvidenceContamination()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        // Four real, differently-staged structural weeks within ONE rolling
        // window -- the exact real shape confirmed via captured HTTP data
        // in Phase 4M.5B §F (GeneralEndurance -> AerobicStrength ->
        // AerobicStrength -> PreSpecificTransition).
        await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10), LongHorizonPersistedSegmentType.GeneralEndurance, "GeneralEndurance");
        await CreateWeekAsync(db, planId, 2, new DateOnly(2026, 8, 17), LongHorizonPersistedSegmentType.PreparationRunway, "AerobicStrength");
        await CreateWeekAsync(db, planId, 3, new DateOnly(2026, 8, 24), LongHorizonPersistedSegmentType.PreparationRunway, "AerobicStrength");
        await CreateWeekAsync(db, planId, 4, new DateOnly(2026, 8, 31), LongHorizonPersistedSegmentType.Core, "PreSpecificTransition");

        // Populate each week's 4 sessions with a distinct completion count,
        // so a partitioning defect that leaked sessions across weeks would
        // be caught by summary-count mismatches.
        var roles = new[] { PreparationRunwaySlotRole.KeySession, PreparationRunwaySlotRole.EasySupport, PreparationRunwaySlotRole.EasySupport, PreparationRunwaySlotRole.LongRun };
        var completedCounts = new[] { 4, 3, 1, 0 };
        for (var w = 0; w < 4; w++)
        {
            var weekId = (await db.LongHorizonRollingWeekStates.SingleAsync(x => x.PlanStateId == planId && x.GlobalWeek == w + 1)).Id;
            var mondayStart = new DateOnly(2026, 8, 10).AddDays(w * 7);
            for (var i = 0; i < 4; i++)
            {
                var outcome = i < completedCounts[w] ? LongHorizonRollingSessionOutcomeStatus.Completed : LongHorizonRollingSessionOutcomeStatus.NotToday;
                await CreateSessionAsync(db, weekId, i + 1, roles[i], mondayStart.AddDays(i), outcome,
                    notTodayReason: outcome == LongHorizonRollingSessionOutcomeStatus.NotToday ? "schedule" : null);
            }
        }

        var weeks = await LoadWindowAsync(db, planId, 1, 4);
        var groups = WeeklyWindowPartitioner.PartitionByStructuralWeekLineage(weeks);

        Assert.Equal(4, groups.Count);
        Assert.All(groups, g => Assert.Equal(4, g.Count)); // no cross-week contamination: exactly 4 per week

        var summaries = groups.Select(g => WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(g))).ToList();
        Assert.Equal([4, 3, 1, 0], summaries.Select(s => s.EffectiveCompletedCount).ToArray());

        var (weeklyResults, aggregated) = EvaluateWeekly(weeks);
        // B1 receives exactly one decision per real structural week
        // (4 weeks in, 4 decisions out), independent of stage/phase.
        Assert.Equal(4, weeklyResults.Count);
        Assert.Equal("Reduce", aggregated.LoadDecision); // week 4 (0 completed) is the worst
    }

    // ── Section M: direct multi-week policy invocation must disappear ────

    /// <summary>
    /// Section M structural guard: NextWindowLoadDecisionPolicy must never
    /// be invoked directly against a full-window (multi-week) summary in
    /// production. A source-text scan of the one real call site
    /// (LongHorizonRollingWindowActivationService) is a deliberately blunt
    /// but effective boundary check -- it fails loudly if a future change
    /// reintroduces the old `NextWindowLoadDecisionPolicy.Evaluate(windowSummary)`
    /// pattern (the exact pre-4M.5C bug), and confirms the new weekly path
    /// (WeeklyWindowPartitioner -> per-week NextWindowLoadDecisionPolicy.Evaluate
    /// -> WeeklyLoadDecisionAggregator.AggregateWorstWeekWins) is present.
    /// </summary>
    [Fact]
    public void M_ActivationService_NeverInvokesLoadDecisionPolicyDirectlyAgainstFullWindowSummary()
    {
        var source = File.ReadAllText(ActivationServiceSourcePath());

        Assert.DoesNotContain("NextWindowLoadDecisionPolicy.Evaluate(windowSummary)", source);
        Assert.Contains("WeeklyWindowPartitioner.PartitionByStructuralWeekLineage(windowWeeks)", source);
        Assert.Contains("WeeklyLoadDecisionAggregator.AggregateWorstWeekWins(weeklyResults)", source);

        // windowSummary (whole-window) must still exist -- it remains the
        // window-level authority feeding NextWindowNumericAnchorSelector's
        // EffectiveCompletedCount input (Rev5 §7a: numeric anchor
        // architecture unaffected) -- but the ONLY thing it may feed
        // NextWindowLoadDecisionPolicy.Evaluate with is a per-week group,
        // never itself directly.
        Assert.Contains("windowSummary.EffectiveCompletedCount", source);
    }

    private static string ActivationServiceSourcePath([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!,
            "..", "..", "..", "..", "..", "RunningApp.Application", "Services", "LongHorizonRollingWindowActivationService.cs"));
}
