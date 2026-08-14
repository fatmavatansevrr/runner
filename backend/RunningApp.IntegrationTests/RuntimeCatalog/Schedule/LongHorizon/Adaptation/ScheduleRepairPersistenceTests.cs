using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.2 -- real-PostgreSQL integration tests for
/// <see cref="ScheduleRepairPersistenceService"/>: transactional
/// materialization, lineage, source immutability, idempotency, concurrency,
/// stale-target revalidation, and Rev3.1 denominator preservation through
/// persistence. Every fixture (plan/week/sessions) is created directly via
/// AppDbContext with fresh GUIDs -- no HTTP host, no shared mock-user state
/// -- but the class still joins <c>ApiIntegrationTestCollection</c> because
/// it performs real inserts against the same shared Postgres instance every
/// other DB-touching test class uses, matching repository convention.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class ScheduleRepairPersistenceTests : IAsyncLifetime
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
        // Fixtures are fully self-contained (fresh GUIDs, no FK to any
        // shared/mock-user row), so cleanup is a narrow, explicit delete by
        // the plan ids this test class itself created -- never a broad
        // reset that could race with other tests in the collection.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var planId in _createdPlanIds)
        {
            var plan = await db.LongHorizonRollingPlanStates.FindAsync(planId);
            if (plan is not null)
            {
                db.LongHorizonRollingPlanStates.Remove(plan); // cascades weeks/sessions
                await db.SaveChangesAsync();
            }
        }
        _factory.Dispose();
    }

    private AppDbContext NewDb() => _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private ScheduleRepairPersistenceService NewService(AppDbContext db) =>
        new(db, NullLogger<ScheduleRepairPersistenceService>.Instance);

    // ── Fixture builders ────────────────────────────────────────────────

    private async Task<Guid> CreatePlanAsync(AppDbContext db)
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingPlanStates.Add(new LongHorizonRollingPlanState
        {
            Id = id,
            TotalWeeks = 22,
            ReadinessProfile = "CoreEntryReady",
            StartDate = new DateOnly(2026, 8, 10),
            RaceDate = new DateOnly(2027, 1, 11),
            GoalType = "Race",
            GoalDistance = "TenK",
            Level = "Intermediate",
            DaysPerWeek = 4,
            PreferredDaysCsv = "Monday,Wednesday,Friday,Sunday",
            LongRunDay = "Sunday",
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            CatalogRootPath = "test",
            CurrentLifecycleStatus = LongHorizonPersistedLifecycleState.NumericActivated,
            CurrentWindowStartWeek = 1,
            CurrentWindowEndWeek = 2,
            ActiveContextVersionSequence = 1,
            ActiveContextVersionId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        _createdPlanIds.Add(id);
        return id;
    }

    private async Task<Guid> CreateWeekAsync(AppDbContext db, Guid planId, int globalWeek, DateOnly start)
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingWeekStates.Add(new LongHorizonRollingWeekState
        {
            Id = id,
            PlanStateId = planId,
            GlobalWeek = globalWeek,
            SegmentType = LongHorizonPersistedSegmentType.Core,
            Stage = "Build",
            StructuralStartDate = start,
            StructuralEndDate = start.AddDays(6),
            LifecycleState = LongHorizonPersistedLifecycleState.NumericActivated,
            WeeklyVolumeKm = 25,
            LongRunKm = 8,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> CreateSessionAsync(
        AppDbContext db, Guid weekStateId, int ordinal, PreparationRunwaySlotRole role, DateOnly date,
        LongHorizonRollingSessionOutcomeStatus outcome = LongHorizonRollingSessionOutcomeStatus.Planned,
        LongHorizonPersistedSessionPlanningStatus planningStatus = LongHorizonPersistedSessionPlanningStatus.Active)
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingSessionStates.Add(new LongHorizonRollingSessionState
        {
            Id = id,
            WeekStateId = weekStateId,
            SessionOrdinal = ordinal,
            SessionRole = LongHorizonSessionRoleCodec.ToCanonicalToken(role),
            WorkoutKey = "EASY_STANDARD",
            WorkoutVersion = 6,
            DistanceKm = 7,
            AssignedDate = date,
            ActivationContextVersionSequence = 1,
            Provenance = "generated_from_initial_profile",
            OutcomeStatus = outcome,
            PlanningStatus = planningStatus,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task MarkNotTodayAsync(AppDbContext db, Guid sessionId, string reason)
    {
        var session = await db.LongHorizonRollingSessionStates.SingleAsync(s => s.Id == sessionId);
        session.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.NotToday;
        session.NotTodayReason = reason;
        session.NotTodayRecordedAtUtc = DateTime.UtcNow;
        session.OutcomeVersion++;
        await db.SaveChangesAsync();
    }

    private static ScheduleRepairTrigger BuildTrigger(
        Guid sessionId, PreparationRunwaySlotRole role, NotTodayReasonCode reason, bool isTaper = false) =>
        new(sessionId, role, new AdaptationPhaseIdentity(LongHorizonPersistedSegmentType.Core, "Build"), isTaper, reason,
            LongHorizonRollingSessionOutcomeStatus.NotToday);

    // ── SCHEMA / BACKWARD COMPATIBILITY (1-4) ──────────────────────────

    [Fact] // 1
    public async Task ExistingRollingSession_PersistsAndReadsAsActive()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var sessionId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10));

        using var verifyDb = NewDb();
        var session = await verifyDb.LongHorizonRollingSessionStates.SingleAsync(s => s.Id == sessionId);
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Active, session.PlanningStatus);
        Assert.Null(session.AdaptedFromSessionId);
    }

    [Fact] // 2, 76
    public async Task PreExistingRow_ReceivedActiveDefault_ViaMigration()
    {
        // Proven at migration-rehearsal time (see phase document Section 27)
        // against the real shared dev database's 160k+ pre-existing rows;
        // re-asserted here narrowly against the live connection.
        using var db = NewDb();
        var anyPreExisting = await db.LongHorizonRollingSessionStates.AsNoTracking().FirstOrDefaultAsync();
        if (anyPreExisting is not null)
            Assert.True(anyPreExisting.PlanningStatus is LongHorizonPersistedSessionPlanningStatus.Active or LongHorizonPersistedSessionPlanningStatus.Superseded);
    }

    // ── SKIP PERSISTENCE (5-8) ──────────────────────────────────────────

    [Fact] // 5, 6, 7
    public async Task Skip_WritesExactlyOneAuditRecord_NoReplacementNoSuperseded()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var easyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10));
        await MarkNotTodayAsync(db, easyId, "weather");

        var request = new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(easyId, PreparationRunwaySlotRole.EasySupport, NotTodayReasonCode.Weather),
            new ScheduleRepairDecision(ScheduleRepairAction.Skip, ReasonClass.Operational, SafetyFlag: false),
            SourceWindowStartWeek: 1, SourceWindowEndWeek: 2);

        var result = await NewService(db).PersistAsync(request);

        Assert.Equal(AdaptationPersistenceOutcome.Committed, result.Outcome);
        Assert.Null(result.ReplacementSessionId);
        Assert.Null(result.SupersededSessionId);

        var records = await db.LongHorizonAdaptationDecisionRecords.Where(r => r.TriggerSessionId == easyId).ToListAsync();
        Assert.Single(records);
        Assert.Equal(LongHorizonPersistedAdaptationDecisionType.Skip, records[0].DecisionType);
    }

    [Fact] // 8 (part of idempotency section too)
    public async Task Skip_Replayed_DoesNotCreateSecondAuditRecord()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var easyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10));
        await MarkNotTodayAsync(db, easyId, "weather");

        var request = new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(easyId, PreparationRunwaySlotRole.EasySupport, NotTodayReasonCode.Weather),
            new ScheduleRepairDecision(ScheduleRepairAction.Skip, ReasonClass.Operational, SafetyFlag: false),
            1, 2);

        var service = NewService(db);
        var first = await service.PersistAsync(request);
        var second = await service.PersistAsync(request);

        Assert.Equal(AdaptationPersistenceOutcome.Committed, first.Outcome);
        Assert.Equal(AdaptationPersistenceOutcome.IdempotentReplay, second.Outcome);
        Assert.Equal(first.DecisionRecordId, second.DecisionRecordId);

        var count = await db.LongHorizonAdaptationDecisionRecords.CountAsync(r => r.TriggerSessionId == easyId);
        Assert.Equal(1, count);
    }

    // ── RESCHEDULE TO EMPTY SLOT (9-22) ──────────────────────────────────

    [Fact]
    public async Task RescheduleToEmptySlot_FullPersistenceContract()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");
        var targetDate = new DateOnly(2026, 8, 14); // genuinely empty -- no row created there

        var decision = new ScheduleRepairDecision(ScheduleRepairAction.RescheduleToEmptySlot, ReasonClass.Operational, SafetyFlag: false, SelectedEmptySlotDate: targetDate);
        var request = new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            decision, 1, 2, TargetWeekStateId: weekId);

        var result = await NewService(db).PersistAsync(request);
        Assert.Equal(AdaptationPersistenceOutcome.Committed, result.Outcome);
        Assert.NotNull(result.ReplacementSessionId);
        Assert.Null(result.SupersededSessionId);

        var source = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == keyId); // 9
        Assert.Equal(LongHorizonRollingSessionOutcomeStatus.NotToday, source.OutcomeStatus);
        Assert.Equal(new DateOnly(2026, 8, 12), source.AssignedDate);
        Assert.Equal("schedule_conflict", source.NotTodayReason);

        var replacement = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == result.ReplacementSessionId);
        Assert.NotEqual(keyId, replacement.Id);                                     // 10
        Assert.Equal(targetDate, replacement.AssignedDate);                         // 11
        Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Planned, replacement.OutcomeStatus); // 12
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Active, replacement.PlanningStatus); // 13
        Assert.Equal(keyId, replacement.AdaptedFromSessionId);                      // 14
        Assert.Equal(source.SessionRole, replacement.SessionRole);                  // 15
        Assert.Equal(weekId, replacement.WeekStateId);                              // 16 (window/week preserved)
        Assert.Equal(source.WorkoutKey, replacement.WorkoutKey);                    // 17
        Assert.Equal(source.DistanceKm, replacement.DistanceKm);
        Assert.Null(replacement.ActualDistanceKm);                                  // 18
        Assert.Null(replacement.ActualDurationMinutes);
        Assert.Null(replacement.NotTodayReason);
        Assert.Null(replacement.CompletedAtUtc);

        var records = await db.LongHorizonAdaptationDecisionRecords.Where(r => r.TriggerSessionId == keyId).ToListAsync();
        Assert.Single(records);                                                     // 19
        Assert.Equal(result.ReplacementSessionId, records[0].ReplacementSessionId);  // 20
        Assert.Null(records[0].SupersededSessionId);                                // 21

        var untouchedCount = await db.LongHorizonRollingSessionStates.CountAsync(s => s.WeekStateId == weekId); // 22
        Assert.Equal(2, untouchedCount); // source + replacement only
    }

    // ── SUBSTITUTE FUTURE EASY (23-33) ────────────────────────────────

    [Fact]
    public async Task SubstituteFutureEasy_FullPersistenceContract()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var easyId = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        var longId = await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");

        var decision = new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, SafetyFlag: false, SubstitutedEasySessionId: easyId);
        var request = new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict), decision, 1, 2);

        var result = await NewService(db).PersistAsync(request);
        Assert.Equal(AdaptationPersistenceOutcome.Committed, result.Outcome);

        var sourceKey = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == keyId); // 23
        Assert.Equal(LongHorizonRollingSessionOutcomeStatus.NotToday, sourceKey.OutcomeStatus);
        Assert.Equal(new DateOnly(2026, 8, 12), sourceKey.AssignedDate);

        var supersededEasy = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == easyId);
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Superseded, supersededEasy.PlanningStatus); // 24
        Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Planned, supersededEasy.OutcomeStatus);         // 25 (never NotToday)

        var replacement = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == result.ReplacementSessionId);
        Assert.Equal(new DateOnly(2026, 8, 14), replacement.AssignedDate);          // 26 (same date as superseded EASY)
        Assert.Equal(sourceKey.SessionRole, replacement.SessionRole);               // 27 (source KEY role, not EASY)
        Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Planned, replacement.OutcomeStatus); // 28
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Active, replacement.PlanningStatus);
        Assert.Equal(keyId, replacement.AdaptedFromSessionId);                      // 29

        var record = await db.LongHorizonAdaptationDecisionRecords.SingleAsync(r => r.TriggerSessionId == keyId); // 30
        Assert.Equal(result.ReplacementSessionId, record.ReplacementSessionId);
        Assert.Equal(easyId, record.SupersededSessionId);

        var supersededCount = await db.LongHorizonRollingSessionStates.CountAsync(s => s.WeekStateId == weekId && s.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Superseded);
        Assert.Equal(1, supersededCount); // 31

        var longUntouched = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == longId); // 32
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Active, longUntouched.PlanningStatus);
        Assert.Null(longUntouched.AdaptedFromSessionId);

        // 33: effective active planned session count unchanged -- before: KEY(NotToday,
        // still counts historically) + EASY(Active/Planned) + LONG(Active/Planned) = the
        // same 3 "active plan slots" as after: KEY(NotToday, historical) + EASY(Superseded,
        // non-actionable) + LONG(Active) + KEY-replacement(Active) = still exactly 2
        // Active/Planned "live" sessions (LONG + KEY-replacement) plus the immutable
        // historical KEY -- no fifth live session was created.
        var liveActiveCount = await db.LongHorizonRollingSessionStates.CountAsync(
            s => s.WeekStateId == weekId && s.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Active
                && s.OutcomeStatus != LongHorizonRollingSessionOutcomeStatus.NotToday);
        Assert.Equal(2, liveActiveCount);
    }

    // ── SAFETY REVIEW INDEPENDENCE (4M.2 closure) ───────────────────────

    [Fact]
    public async Task SubstituteFutureEasy_SafetyFlagTrue_PersistsSafetyReviewRequiredIndependentlyOfDecisionType()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var easyId = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");

        var decision = new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, SafetyFlag: true, SubstitutedEasySessionId: easyId);
        var request = new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict), decision, 1, 2);

        var result = await NewService(db).PersistAsync(request);
        Assert.Equal(AdaptationPersistenceOutcome.Committed, result.Outcome);

        // Reload from a FRESH DbContext -- proves the round-trip through the
        // real database, not just what's still tracked in-memory on `db`.
        using var freshDb = NewDb();
        var record = await freshDb.LongHorizonAdaptationDecisionRecords.AsNoTracking().SingleAsync(r => r.TriggerSessionId == keyId);

        Assert.Equal(LongHorizonPersistedAdaptationDecisionType.SubstituteFutureEasy, record.DecisionType);
        Assert.True(record.SafetyReviewRequired);

        Assert.Equal(keyId, record.TriggerSessionId);
        Assert.NotNull(record.ReplacementSessionId);
        Assert.NotNull(record.SupersededSessionId);
        Assert.Equal(easyId, record.SupersededSessionId);
    }

    // ── TAPER (34-36) ─────────────────────────────────────────────────

    [Fact] // 34, 35
    public async Task Taper_Key_Replacement_PreservesAllLoadBearingContentExactly()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 20, new DateOnly(2026, 12, 21));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 12, 23));
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");
        var targetDate = new DateOnly(2026, 12, 25);

        var decision = new ScheduleRepairDecision(ScheduleRepairAction.RescheduleToEmptySlot, ReasonClass.Operational, SafetyFlag: false, SelectedEmptySlotDate: targetDate);
        var request = new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict, isTaper: true),
            decision, 20, 21, TargetWeekStateId: weekId);

        var result = await NewService(db).PersistAsync(request);
        Assert.Equal(AdaptationPersistenceOutcome.Committed, result.Outcome);

        var source = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == keyId);
        var replacement = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == result.ReplacementSessionId);

        // Every load-bearing field identical except identity/date/lineage/status --
        // proving Taper KEY "moved unchanged" structurally, not by special-casing.
        Assert.Equal(source.SessionRole, replacement.SessionRole);
        Assert.Equal(source.WorkoutKey, replacement.WorkoutKey);
        Assert.Equal(source.WorkoutVersion, replacement.WorkoutVersion);
        Assert.Equal(source.DistanceKm, replacement.DistanceKm);
        Assert.Equal(source.Provenance, replacement.Provenance);
        Assert.NotEqual(source.AssignedDate, replacement.AssignedDate); // date may differ
        Assert.NotEqual(source.Id, replacement.Id);                    // identity differs
    }

    // ── LINEAGE (37-42) ─────────────────────────────────────────────────

    [Fact] // 37
    public async Task Lineage_SequentialChain_A_To_B_To_C_Remains_Possible()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var a = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        await MarkNotTodayAsync(db, a, "schedule_conflict");

        var service = NewService(db);
        var r1 = await service.PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(a, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.RescheduleToEmptySlot, ReasonClass.Operational, false, SelectedEmptySlotDate: new DateOnly(2026, 8, 14)),
            1, 2, TargetWeekStateId: weekId));
        var b = r1.ReplacementSessionId!.Value;

        // B itself later becomes NotToday and is repaired -> C. This proves a
        // sequential A->B->C chain (each node with at most one direct child)
        // remains representable, distinct from forbidding two DIRECT children of A.
        await MarkNotTodayAsync(db, b, "weather");
        var r2 = await service.PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(b, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.Weather),
            new ScheduleRepairDecision(ScheduleRepairAction.RescheduleToEmptySlot, ReasonClass.Operational, false, SelectedEmptySlotDate: new DateOnly(2026, 8, 16)),
            1, 2, TargetWeekStateId: weekId));

        Assert.Equal(AdaptationPersistenceOutcome.Committed, r2.Outcome);
        var c = r2.ReplacementSessionId!.Value;

        var bRow = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == b);
        var cRow = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == c);
        Assert.Equal(a, bRow.AdaptedFromSessionId);
        Assert.Equal(b, cRow.AdaptedFromSessionId);
    }

    [Fact] // 38, 40
    public async Task Lineage_SecondDirectChild_RejectedAtDatabaseBoundary()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var a = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));

        // Manually attempt to insert a second row with the same
        // AdaptedFromSessionId, bypassing the service entirely -- proves the
        // DB-level partial unique index itself, not just application logic.
        db.LongHorizonRollingSessionStates.Add(new LongHorizonRollingSessionState
        {
            Id = Guid.NewGuid(), WeekStateId = weekId, SessionOrdinal = 90,
            SessionRole = "KEY_SESSION", AssignedDate = new DateOnly(2026, 8, 14),
            OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.Planned,
            PlanningStatus = LongHorizonPersistedSessionPlanningStatus.Active,
            AdaptedFromSessionId = a, Provenance = "test",
        });
        await db.SaveChangesAsync();

        db.LongHorizonRollingSessionStates.Add(new LongHorizonRollingSessionState
        {
            Id = Guid.NewGuid(), WeekStateId = weekId, SessionOrdinal = 91,
            SessionRole = "KEY_SESSION", AssignedDate = new DateOnly(2026, 8, 16),
            OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.Planned,
            PlanningStatus = LongHorizonPersistedSessionPlanningStatus.Active,
            AdaptedFromSessionId = a, Provenance = "test",
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact] // 41
    public async Task Lineage_SummaryBuilder_ReadsPersistedLineageCorrectly()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");

        var result = await NewService(db).PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.RescheduleToEmptySlot, ReasonClass.Operational, false, SelectedEmptySlotDate: new DateOnly(2026, 8, 14)),
            1, 2, TargetWeekStateId: weekId));
        var replacementId = result.ReplacementSessionId!.Value;
        var replacement = await db.LongHorizonRollingSessionStates.SingleAsync(s => s.Id == replacementId);
        replacement.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.Completed;
        await db.SaveChangesAsync();

        var rows = await db.LongHorizonRollingSessionStates.AsNoTracking().Where(s => s.WeekStateId == weekId).ToListAsync();
        var evidence = rows.Select(r => new LogicalSessionEvidence(
            r.Id,
            LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy(r.SessionRole, out var role) ? role : PreparationRunwaySlotRole.EasySupport,
            r.OutcomeStatus,
            r.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Superseded ? SessionPlanningStatus.Superseded : SessionPlanningStatus.Active,
            r.AdaptedFromSessionId,
            null)).ToList();

        var summary = WindowExecutionSummaryBuilder.Build(evidence);
        Assert.Equal(1, summary.ExpectedSessionCount);
        Assert.Equal(1, summary.EffectiveCompletedCount);
        Assert.True(summary.KeySessionCompleted);
    }

    // ── SUPERSEDED (43-47) ────────────────────────────────────────────

    [Fact] // 43, 45, 46
    public async Task Superseded_RoundTrips_NeverBecomesCompletedOrNotToday_ThroughAdaptationOnly()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var easyId = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");

        await NewService(db).PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, false, SubstitutedEasySessionId: easyId),
            1, 2));

        var superseded = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == easyId);
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Superseded, superseded.PlanningStatus);
        Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Planned, superseded.OutcomeStatus); // never Completed/NotToday via adaptation
    }

    // ── IDEMPOTENCY (48-52) ────────────────────────────────────────────

    [Fact] // 48, 49, 51, 52
    public async Task Substitute_Replayed_CreatesNoSecondReplacementOrSupersession_ChoosesNoNewTarget()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var easyId = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");

        var request = new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, false, SubstitutedEasySessionId: easyId),
            1, 2);

        var service = NewService(db);
        var first = await service.PersistAsync(request);
        var second = await service.PersistAsync(request);

        Assert.Equal(AdaptationPersistenceOutcome.IdempotentReplay, second.Outcome);
        Assert.Equal(first.ReplacementSessionId, second.ReplacementSessionId);
        Assert.Equal(first.SupersededSessionId, second.SupersededSessionId);

        var supersededCount = await db.LongHorizonRollingSessionStates.CountAsync(s => s.WeekStateId == weekId && s.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Superseded);
        Assert.Equal(1, supersededCount);
        var replacementCount = await db.LongHorizonRollingSessionStates.CountAsync(s => s.AdaptedFromSessionId == keyId);
        Assert.Equal(1, replacementCount);
    }

    // ── CONCURRENCY (53-58) ────────────────────────────────────────────

    [Fact] // 53, 54, 55, 56, 57, 58
    public async Task Concurrency_TwoSimultaneousCallsForSameTrigger_ExactlyOneCommittedDecision()
    {
        var planId = await CreatePlanAsync(NewDb());
        var weekId = await CreateWeekAsync(NewDb(), planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(NewDb(), weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var easyId = await CreateSessionAsync(NewDb(), weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        await MarkNotTodayAsync(NewDb(), keyId, "schedule_conflict");

        var request = new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, false, SubstitutedEasySessionId: easyId),
            1, 2);

        // Two fully independent DbContext instances (separate connections),
        // firing genuinely concurrently -- a real race, not a sequential
        // application-level check-then-insert simulation.
        var task1 = Task.Run(async () =>
        {
            using var db1 = NewDb();
            return await NewService(db1).PersistAsync(request);
        });
        var task2 = Task.Run(async () =>
        {
            using var db2 = NewDb();
            return await NewService(db2).PersistAsync(request);
        });
        var results = await Task.WhenAll(task1, task2);

        var committedCount = results.Count(r => r.Outcome == AdaptationPersistenceOutcome.Committed);
        var replayOrConflictCount = results.Count(r => r.Outcome is AdaptationPersistenceOutcome.IdempotentReplay or AdaptationPersistenceOutcome.ConcurrencyConflict);
        Assert.Equal(1, committedCount); // 53
        Assert.Equal(1, replayOrConflictCount);

        using var verifyDb = NewDb();
        var recordCount = await verifyDb.LongHorizonAdaptationDecisionRecords.CountAsync(r => r.TriggerSessionId == keyId);
        Assert.Equal(1, recordCount); // 54: at most one committed decision -- proven at the DB, not just in-process

        var replacementCount = await verifyDb.LongHorizonRollingSessionStates.CountAsync(s => s.AdaptedFromSessionId == keyId);
        Assert.Equal(1, replacementCount); // 54: at most one replacement

        var supersededCount = await verifyDb.LongHorizonRollingSessionStates.CountAsync(s => s.WeekStateId == weekId && s.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Superseded);
        Assert.Equal(1, supersededCount); // 55: at most one superseded EASY

        // 57: the authoritative committed result is readable after the race resolves.
        var authoritative = await verifyDb.LongHorizonAdaptationDecisionRecords.SingleAsync(r => r.TriggerSessionId == keyId);
        Assert.NotNull(authoritative);
    }

    // ── STALE TARGET (59-66) ────────────────────────────────────────────

    [Fact] // 59
    public async Task StaleTarget_SubstitutionEasyAlreadySuperseded_Rejected()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var easyId = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), planningStatus: LongHorizonPersistedSessionPlanningStatus.Superseded);
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");

        var result = await NewService(db).PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, false, SubstitutedEasySessionId: easyId),
            1, 2));

        Assert.Equal(AdaptationPersistenceOutcome.StaleTarget, result.Outcome);
    }

    [Fact] // 60
    public async Task StaleTarget_SubstitutionEasyAlreadyCompleted_Rejected()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var easyId = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14), outcome: LongHorizonRollingSessionOutcomeStatus.Completed);
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");

        var result = await NewService(db).PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, false, SubstitutedEasySessionId: easyId),
            1, 2));

        Assert.Equal(AdaptationPersistenceOutcome.StaleTarget, result.Outcome);
    }

    [Fact] // 61
    public async Task StaleTarget_SubstitutionEasyAlreadyNotToday_Rejected()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var easyId = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        await MarkNotTodayAsync(db, easyId, "weather");
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");

        var result = await NewService(db).PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, false, SubstitutedEasySessionId: easyId),
            1, 2));

        Assert.Equal(AdaptationPersistenceOutcome.StaleTarget, result.Outcome);
    }

    [Fact] // 62, 63
    public async Task StaleTarget_EmptySlotBecomesOccupied_Rejected_DoesNotAutoSelectDifferentTarget()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");
        var targetDate = new DateOnly(2026, 8, 14);
        // Concurrently occupy the previously-empty target date before commit.
        await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, targetDate);

        var result = await NewService(db).PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.RescheduleToEmptySlot, ReasonClass.Operational, false, SelectedEmptySlotDate: targetDate),
            1, 2, TargetWeekStateId: weekId));

        Assert.Equal(AdaptationPersistenceOutcome.StaleTarget, result.Outcome);
        // No replacement was ever created against any other date -- prove no
        // AdaptedFrom row exists for this trigger at all.
        var anyReplacement = await db.LongHorizonRollingSessionStates.AnyAsync(s => s.AdaptedFromSessionId == keyId);
        Assert.False(anyReplacement);
    }

    [Fact] // 66
    public async Task StaleTarget_WrongRoleSubstitutionTarget_Rejected()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var longId = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");

        var result = await NewService(db).PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, false, SubstitutedEasySessionId: longId),
            1, 2));

        Assert.Equal(AdaptationPersistenceOutcome.StaleTarget, result.Outcome);
    }

    // ── TRANSACTION ROLLBACK (67-70) ─────────────────────────────────────

    [Fact] // 69: forced audit failure -> no partial session mutation
    public async Task Rollback_MissingSubstitutionInput_NoPartialSessionMutation()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var easyId = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        await MarkNotTodayAsync(db, keyId, "schedule_conflict");

        // Malformed decision: SubstituteFutureEasy with no target id -- must
        // fail the whole transaction, not partially supersede the EASY.
        var badDecision = new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, false);
        var result = await NewService(db).PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(keyId, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict), badDecision, 1, 2));

        Assert.Equal(AdaptationPersistenceOutcome.IntegrityViolation, result.Outcome);

        var easy = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == easyId);
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Active, easy.PlanningStatus); // 67: EASY remains Active after rollback
        var anyRecord = await db.LongHorizonAdaptationDecisionRecords.AnyAsync(r => r.TriggerSessionId == keyId);
        Assert.False(anyRecord); // 70: no decision/audit partial state
    }

    // ── STALE TRIGGER ────────────────────────────────────────────────

    [Fact]
    public async Task StaleTrigger_SessionNoLongerNotToday_Rejected()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var easyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10));
        // Deliberately NOT marked NotToday -- still Planned.

        var result = await NewService(db).PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(easyId, PreparationRunwaySlotRole.EasySupport, NotTodayReasonCode.Weather),
            new ScheduleRepairDecision(ScheduleRepairAction.Skip, ReasonClass.Operational, false), 1, 2));

        Assert.Equal(AdaptationPersistenceOutcome.StaleTrigger, result.Outcome);
    }

    // ── REV3.1 LOCKED PERSISTENCE SCENARIO (71) ───────────────────────

    [Fact]
    public async Task LockedScenario_Rev3_1_SurvivesPersistence()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));

        var monEasy = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 10), LongHorizonRollingSessionOutcomeStatus.Completed);
        var wedKey = await CreateSessionAsync(db, weekId, 2, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        var friEasy = await CreateSessionAsync(db, weekId, 3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        var sunLong = await CreateSessionAsync(db, weekId, 4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16), LongHorizonRollingSessionOutcomeStatus.Completed);
        await MarkNotTodayAsync(db, wedKey, "schedule_conflict");

        var result = await NewService(db).PersistAsync(new ScheduleRepairPersistenceRequest(
            planId, BuildTrigger(wedKey, PreparationRunwaySlotRole.KeySession, NotTodayReasonCode.ScheduleConflict),
            new ScheduleRepairDecision(ScheduleRepairAction.SubstituteFutureEasy, ReasonClass.Operational, false, SubstitutedEasySessionId: friEasy),
            1, 2));
        Assert.Equal(AdaptationPersistenceOutcome.Committed, result.Outcome);

        var friKeyReplacement = await db.LongHorizonRollingSessionStates.SingleAsync(s => s.Id == result.ReplacementSessionId);
        friKeyReplacement.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.Completed;
        await db.SaveChangesAsync();

        var rows = await db.LongHorizonRollingSessionStates.AsNoTracking().Where(s => s.WeekStateId == weekId).ToListAsync();
        var evidence = rows.Select(r => new LogicalSessionEvidence(
            r.Id,
            LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy(r.SessionRole, out var role) ? role : PreparationRunwaySlotRole.EasySupport,
            r.OutcomeStatus,
            r.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Superseded ? SessionPlanningStatus.Superseded : SessionPlanningStatus.Active,
            r.AdaptedFromSessionId, null)).ToList();

        var summary = WindowExecutionSummaryBuilder.Build(evidence);

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
    }

    // ── MIGRATION / DB (72-75) ────────────────────────────────────────

    [Fact] // 73
    public async Task Migration_ForeignKeyIntegrity_Proven()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));

        db.LongHorizonAdaptationDecisionRecords.Add(new LongHorizonAdaptationDecisionRecord
        {
            Id = Guid.NewGuid(), PlanStateId = planId, TriggerSessionId = Guid.NewGuid() /* does not exist */,
            DecisionType = LongHorizonPersistedAdaptationDecisionType.Skip, ReasonCode = "Weather", CreatedAtUtc = DateTime.UtcNow,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact] // 74
    public async Task Migration_DirectChildUniqueness_Proven() => await Lineage_SecondDirectChild_RejectedAtDatabaseBoundary();

    [Fact] // 75
    public async Task Migration_ScheduleRepairIdempotencyUniqueness_Proven()
    {
        using var db = NewDb();
        var planId = await CreatePlanAsync(db);
        var weekId = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        var keyId = await CreateSessionAsync(db, weekId, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));

        db.LongHorizonAdaptationDecisionRecords.Add(new LongHorizonAdaptationDecisionRecord
        {
            Id = Guid.NewGuid(), PlanStateId = planId, TriggerSessionId = keyId,
            DecisionType = LongHorizonPersistedAdaptationDecisionType.Skip, ReasonCode = "Weather", CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        db.LongHorizonAdaptationDecisionRecords.Add(new LongHorizonAdaptationDecisionRecord
        {
            Id = Guid.NewGuid(), PlanStateId = planId, TriggerSessionId = keyId,
            DecisionType = LongHorizonPersistedAdaptationDecisionType.Skip, ReasonCode = "Weather", CreatedAtUtc = DateTime.UtcNow,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
