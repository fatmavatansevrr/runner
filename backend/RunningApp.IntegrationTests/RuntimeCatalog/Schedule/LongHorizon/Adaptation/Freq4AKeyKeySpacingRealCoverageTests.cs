using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 10K-FREQ.4A -- real-PostgreSQL coverage closure for the KEY-to-KEY
/// spacing rule FREQ.4 §C added to <see cref="ScheduleRepairSpacingValidator"/>.
/// FREQ.4 disclosed no dedicated test existed for this, citing "no
/// lightweight in-memory test-construction precedent" -- that claim was
/// checked here and found WRONG: real fixture-construction helpers already
/// exist (<see cref="ScheduleRepairRuntimeOrchestratorTests"/>'s
/// CreatePlanAsync/CreateWeekAsync/CreateSessionAsync, Phase 4M.3), using
/// the exact real-Postgres pattern this whole test-file family already
/// uses. This file mirrors that pattern with an adjustable-preferred-days
/// local helper (kept local rather than editing the shared file, to stay
/// additive-only).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Freq4AKeyKeySpacingRealCoverageTests : IAsyncLifetime
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

    // ── Fixture, mirroring ScheduleRepairRuntimeOrchestratorTests's exact pattern,
    //    but with adjustable preferred days (needed to construct a genuine
    //    KEY<->KEY violation -- the standard Mon/Wed/Fri/Sun 4D vocabulary has a
    //    minimum 2-day inter-preferred-day gap by construction, so it can never
    //    violate a 2-day minimum; a hypothetical 2-KEY layout needs tighter days) ──

    private async Task<Guid> CreatePlanAsync(AppDbContext db, string preferredDaysCsv)
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingPlanStates.Add(new LongHorizonRollingPlanState
        {
            Id = id, TotalWeeks = 22, ReadinessProfile = "CoreEntryReady",
            StartDate = new DateOnly(2026, 8, 10), RaceDate = new DateOnly(2027, 1, 11),
            GoalType = "Race", GoalDistance = "TenK", Level = "Intermediate", DaysPerWeek = 4,
            PreferredDaysCsv = preferredDaysCsv, LongRunDay = "Sunday",
            CandidateKey = "TEN_K__4D__INTERMEDIATE", CandidateVersion = 10, CatalogRootPath = "test",
            CurrentLifecycleStatus = LongHorizonPersistedLifecycleState.NumericActivated,
            CurrentWindowStartWeek = 1, CurrentWindowEndWeek = 2,
            ActiveContextVersionSequence = 1, ActiveContextVersionId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        _createdPlanIds.Add(id);
        return id;
    }

    private static async Task<Guid> CreateWeekAsync(AppDbContext db, Guid planId, int globalWeek, DateOnly mondayStart)
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingWeekStates.Add(new LongHorizonRollingWeekState
        {
            Id = id, PlanStateId = planId, GlobalWeek = globalWeek, SegmentType = LongHorizonPersistedSegmentType.Core, Stage = "Build",
            StructuralStartDate = mondayStart, StructuralEndDate = mondayStart.AddDays(6),
            LifecycleState = LongHorizonPersistedLifecycleState.NumericActivated, WeeklyVolumeKm = 25, LongRunKm = 8,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<Guid> CreateSessionAsync(AppDbContext db, Guid weekStateId, int ordinal, PreparationRunwaySlotRole role, DateOnly date)
    {
        var id = Guid.NewGuid();
        db.LongHorizonRollingSessionStates.Add(new LongHorizonRollingSessionState
        {
            Id = id, WeekStateId = weekStateId, SessionOrdinal = ordinal,
            SessionRole = LongHorizonSessionRoleCodec.ToCanonicalToken(role), WorkoutKey = "STANDARD", WorkoutVersion = 6,
            DistanceKm = 7, AssignedDate = date, ActivationContextVersionSequence = 1, Provenance = "generated_from_initial_profile",
            OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.Planned, PlanningStatus = LongHorizonPersistedSessionPlanningStatus.Active,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<LongHorizonRollingSessionState> MarkNotTodayAsync(AppDbContext db, Guid sessionId, string reason)
    {
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

    [Fact]
    public async Task GetEmptySlotCandidates_SecondKeySessionTooCloseToRemainingActiveKey_IsFlaggedSpacingInvalid()
    {
        using var db = NewDb();
        // Preferred days Mon/Tue/Wed/Sun -- deliberately includes adjacent
        // days (Mon-Tue, Tue-Wed) so a real 1-day-apart candidate can exist,
        // unlike the standard 4D Mon/Wed/Fri/Sun vocabulary where the
        // minimum inter-preferred-day gap is exactly 2 days by construction.
        var planId = await CreatePlanAsync(db, "Monday,Tuesday,Wednesday,Sunday");
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));

        // KEY #1 stays Active on Wednesday 8/12.
        await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12));
        // KEY #2 (trigger) starts on Monday 8/10 -- earliest in the week, so
        // every other preferred day in the week is a candidate.
        var key2Id = await CreateSessionAsync(db, week1, 2, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var trigger = await MarkNotTodayAsync(db, key2Id, "schedule");

        var candidates = ScheduleRepairCandidateProvider.GetEmptySlotCandidates(trigger.Week.Plan, trigger);

        // Tuesday 8/11 is 1 day from the still-Active KEY on Wednesday 8/12 --
        // violates the new 2-day KEY<->KEY minimum. Real, structurally
        // eligible candidate (correct day, unoccupied, in-window), but
        // must be flagged spacing-invalid, not silently accepted.
        var tuesday = Assert.Single(candidates, c => c.Date == new DateOnly(2026, 8, 11));
        Assert.False(tuesday.IsSafetyValid);

        // Sunday 8/16 is 4 days from the Active KEY -- must remain valid.
        var sunday = Assert.Single(candidates, c => c.Date == new DateOnly(2026, 8, 16));
        Assert.True(sunday.IsSafetyValid);
    }

    [Fact]
    public async Task RealRepairPipeline_SkipsKeyKeySpacingInvalidCandidate_SelectsNextValidOne()
    {
        // Same scenario as above, but through the REAL entry point
        // (ScheduleRepairRuntimeOrchestrator -> ScheduleRepairPolicy /
        // CandidateSelectionPolicy's real "skip, don't disqualify" search),
        // not a unit-isolated call to the validator or candidate provider.
        using var db = NewDb();
        var planId = await CreatePlanAsync(db, "Monday,Tuesday,Wednesday,Sunday");
        var week1 = await CreateWeekAsync(db, planId, 1, new DateOnly(2026, 8, 10));
        await CreateSessionAsync(db, week1, 1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 12)); // KEY #1, stays Active
        var key2Id = await CreateSessionAsync(db, week1, 2, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10)); // KEY #2, trigger
        var trigger = await MarkNotTodayAsync(db, key2Id, "schedule");

        var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(db, NewLoggerFactory(), trigger, default);

        // Tuesday 8/11 is the chronologically-earliest candidate but is
        // KEY<->KEY spacing-invalid; the real pipeline must skip it and
        // select the next structurally-valid candidate (Sunday 8/16), never
        // silently accept the spacing-invalid one.
        Assert.Equal(LongHorizonScheduleRepairActionKind.RescheduleToEmptySlot, outcome.Action);
        Assert.Equal(new DateOnly(2026, 8, 16), outcome.ReplacementDate);
    }
}
