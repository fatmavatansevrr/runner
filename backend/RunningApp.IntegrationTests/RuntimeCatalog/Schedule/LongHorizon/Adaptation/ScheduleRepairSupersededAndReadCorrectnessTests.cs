using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.Identity;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.Services;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.3 -- real end-to-end HTTP tests: live NotToday through the real
/// controller, Superseded-session execution guards (Complete/NotToday
/// rejection), and public read correctness (Home/Calendar/detail must not
/// present a Superseded session as an active, actionable workout).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class ScheduleRepairSupersededAndReadCorrectnessTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>Builds a full, HTTP-reachable rolling plan owned by the mock
    /// current user: resets any prior state for that user (same convention as
    /// LongHorizonActiveReadAndMutationTests.ConfirmAsync), then a real
    /// TrainingPlans row (Active, RollingLongHorizon) plus a real
    /// LongHorizonRollingPlanState graph -- gives deterministic role/date
    /// control the real generation pipeline doesn't, while still exercising
    /// the actual owned-session HTTP path.</summary>
    private async Task<(Guid PlanId, Guid RollingId, Guid KeyId, Guid EasyId, Guid LongId)> BuildOwnedPlanAsync()
    {
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var syncService = scope.ServiceProvider.GetRequiredService<IUserSynchronizationService>();
        var user = await syncService.SynchronizeAsync(MockIdentityProvider.MockUserId, "Mock User", "mock@local.dev", null, false);
        var userId = user.Id;

        var rollingId = Guid.NewGuid();
        db.LongHorizonRollingPlanStates.Add(new LongHorizonRollingPlanState
        {
            Id = rollingId, TotalWeeks = 22, ReadinessProfile = "CoreEntryReady",
            StartDate = new DateOnly(2026, 8, 10), RaceDate = new DateOnly(2027, 1, 11),
            GoalType = "Race", GoalDistance = "TenK", Level = "Intermediate", DaysPerWeek = 4,
            PreferredDaysCsv = "Monday,Wednesday,Friday,Sunday", LongRunDay = "Sunday",
            CandidateKey = "TEN_K__4D__INTERMEDIATE", CandidateVersion = 10, CatalogRootPath = "test",
            CurrentLifecycleStatus = LongHorizonPersistedLifecycleState.NumericActivated,
            CurrentWindowStartWeek = 1, CurrentWindowEndWeek = 2,
            ActiveContextVersionSequence = 1, ActiveContextVersionId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow,
        });
        var weekId = Guid.NewGuid();
        db.LongHorizonRollingWeekStates.Add(new LongHorizonRollingWeekState
        {
            Id = weekId, PlanStateId = rollingId, GlobalWeek = 1,
            SegmentType = LongHorizonPersistedSegmentType.Core, Stage = "Build",
            StructuralStartDate = new DateOnly(2026, 8, 10), StructuralEndDate = new DateOnly(2026, 8, 16),
            LifecycleState = LongHorizonPersistedLifecycleState.NumericActivated, WeeklyVolumeKm = 25, LongRunKm = 8,
        });
        Guid Session(int ordinal, PreparationRunwaySlotRole role, DateOnly date)
        {
            var id = Guid.NewGuid();
            db.LongHorizonRollingSessionStates.Add(new LongHorizonRollingSessionState
            {
                Id = id, WeekStateId = weekId, SessionOrdinal = ordinal,
                SessionRole = LongHorizonSessionRoleCodec.ToCanonicalToken(role), WorkoutKey = "STANDARD", WorkoutVersion = 6,
                DistanceKm = 7, AssignedDate = date, ActivationContextVersionSequence = 1, Provenance = "generated_from_initial_profile",
                OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.Planned, PlanningStatus = LongHorizonPersistedSessionPlanningStatus.Active,
            });
            return id;
        }
        var keyId = Session(1, PreparationRunwaySlotRole.KeySession, new DateOnly(2026, 8, 10));
        var easyId = Session(2, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 12));
        // Friday is also occupied so no empty preferred-day slot exists --
        // this forces the SubstituteFutureEasy branch (not RescheduleToEmptySlot).
        Session(3, PreparationRunwaySlotRole.EasySupport, new DateOnly(2026, 8, 14));
        var longId = Session(4, PreparationRunwaySlotRole.LongRun, new DateOnly(2026, 8, 16));

        var planId = Guid.NewGuid();
        db.TrainingPlans.Add(new TrainingPlan
        {
            Id = planId, InternalUserId = userId, Status = TrainingPlanStatus.Active,
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate,
            DaysPerWeek = 4, StartedAt = DateTime.UtcNow, EstimatedEndDate = DateTime.UtcNow.AddDays(154),
            CreatedAt = DateTime.UtcNow, ScheduleStrategy = PlanScheduleStrategy.RollingLongHorizon,
            LongHorizonRollingPlanStateId = rollingId,
        });

        await db.SaveChangesAsync();
        return (planId, rollingId, keyId, easyId, longId);
    }

    /// <summary>
    /// End-to-end fresh-DbContext proof (Phase 4M.3 final confirmation §B):
    /// drives the REAL application path (real HTTP request -> real
    /// TrainingDaysController -> real LongHorizonRollingSessionMutationService.MarkNotTodayAsync
    /// -> real ScheduleRepairRuntimeOrchestrator, including the
    /// ChangeTracker.Clear() call inside it), then -- only after that whole
    /// request has fully returned and this test's own prior scope/DbContext
    /// is gone -- opens a BRAND NEW DI scope/AppDbContext and reloads every
    /// relevant row from PostgreSQL. Proves the source NotToday evidence and
    /// the adaptation persistence side both survive as real committed rows,
    /// not just as in-memory state from the request that produced them.
    /// </summary>
    [Fact]
    public async Task EndToEnd_FreshDbContext_ProvesSourceNotTodayAndAdaptationBothPersistTogether()
    {
        var (_, rollingId, keyId, easyId, _) = await BuildOwnedPlanAsync();

        // Real end-to-end call: real HTTP request through the real controller
        // and the real MarkNotTodayAsync method (not a direct orchestrator call).
        var response = await _client.PostRawAsync($"/api/v1/training-days/rolling/{keyId}/not-today", new { reason = "schedule" });
        response.EnsureSuccessStatusCode();
        // The HttpClient call above has fully completed and returned -- the
        // scoped DbContext that served that request is already disposed by
        // the framework. Everything below opens an entirely new scope.

        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1/7: reload the source trigger session from PostgreSQL via a fresh context.
        var source = await freshDb.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == keyId);
        Assert.Equal(LongHorizonRollingSessionOutcomeStatus.NotToday, source.OutcomeStatus);
        Assert.Equal("schedule", source.NotTodayReason);
        Assert.NotNull(source.NotTodayRecordedAtUtc);

        // 9: the AdaptationDecisionRecord exists, keyed to the real trigger.
        var record = await freshDb.LongHorizonAdaptationDecisionRecords.AsNoTracking().SingleAsync(r => r.TriggerSessionId == keyId);
        Assert.NotNull(record.ReplacementSessionId);
        // Both Wed(8/12) and Fri(8/14) are occupied in this fixture (see
        // BuildOwnedPlanAsync), so no empty slot exists -- forces
        // SubstituteFutureEasy, choosing the earlier EASY (8/12 = easyId).
        Assert.Equal(easyId, record.SupersededSessionId);

        // 8 (Substitute branch): replacement exists, Active/Planned, correctly linked; target EASY is Superseded.
        var replacement = await freshDb.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == record.ReplacementSessionId);
        Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Planned, replacement.OutcomeStatus);
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Active, replacement.PlanningStatus);
        Assert.Equal(keyId, replacement.AdaptedFromSessionId);

        var supersededTarget = await freshDb.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == easyId);
        Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Superseded, supersededTarget.PlanningStatus);

        _ = rollingId;
    }

    [Fact]
    public async Task Substitution_SupersedesEasy_ThenSuperseded_RejectsCompleteAndNotToday()
    {
        var (_, _, keyId, easyId, _) = await BuildOwnedPlanAsync();

        var repair = await _client.PostRawAsync($"/api/v1/training-days/rolling/{keyId}/not-today", new { reason = "schedule" });
        repair.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var easy = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == easyId);
            Assert.Equal(LongHorizonPersistedSessionPlanningStatus.Superseded, easy.PlanningStatus);
        }

        var completeAttempt = await _client.PostRawAsync($"/api/v1/training-days/rolling/{easyId}/complete",
            new { actual_distance_km = 7.0, actual_duration_minutes = 40 });
        Assert.Equal(HttpStatusCode.Conflict, completeAttempt.StatusCode);
        Assert.Contains("LONG_HORIZON_ROLLING_SESSION_SUPERSEDED", await completeAttempt.Content.ReadAsStringAsync());

        var notTodayAttempt = await _client.PostRawAsync($"/api/v1/training-days/rolling/{easyId}/not-today", new { reason = "weather" });
        Assert.Equal(HttpStatusCode.Conflict, notTodayAttempt.StatusCode);
        Assert.Contains("LONG_HORIZON_ROLLING_SESSION_SUPERSEDED", await notTodayAttempt.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Home_DoesNotExposeSupersededEasyAsActiveWorkout()
    {
        var (planId, _, keyId, easyId, _) = await BuildOwnedPlanAsync();
        (await _client.PostRawAsync($"/api/v1/training-days/rolling/{keyId}/not-today", new { reason = "schedule" })).EnsureSuccessStatusCode();

        var home = await _client.GetAsync("/api/v1/plans/active/home");
        var body = await home.Content.ReadAsStringAsync();
        Assert.DoesNotContain(easyId.ToString(), body);
        _ = planId;
    }

    [Fact]
    public async Task Calendar_DoesNotExposeSupersededEasyAsActiveWorkout()
    {
        var (_, _, keyId, easyId, _) = await BuildOwnedPlanAsync();
        (await _client.PostRawAsync($"/api/v1/training-days/rolling/{keyId}/not-today", new { reason = "schedule" })).EnsureSuccessStatusCode();

        var calendar = await _client.GetAsync("/api/v1/plans/active/calendar?month=2026-08");
        var body = await calendar.Content.ReadAsStringAsync();
        Assert.DoesNotContain(easyId.ToString(), body);
    }

    [Fact]
    public async Task SessionDetail_ForSupersededRow_NeverReportsMutationAllowedTrue()
    {
        var (_, _, keyId, easyId, _) = await BuildOwnedPlanAsync();
        (await _client.PostRawAsync($"/api/v1/training-days/rolling/{keyId}/not-today", new { reason = "schedule" })).EnsureSuccessStatusCode();

        // The Superseded row's own id must remain fetchable (provenance), but
        // never report itself as actionable.
        var detail = await _client.GetAsync($"/api/v1/training-days/rolling/{easyId}");
        detail.EnsureSuccessStatusCode();
        var body = await detail.Content.ReadAsStringAsync();
        Assert.Contains("\"mutation_allowed\":false", body);
    }
}
