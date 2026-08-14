using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.Services;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonActiveReadAndMutationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonActiveReadAndMutationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity()
    {
        var state = await ConfirmAsync();
        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Equal("rolling_long_horizon", home["schedule_strategy"]!.GetValue<string>());
        Assert.Equal(1, home["contract_version"]!.GetValue<int>());
        Assert.Null(home["today_workout"]);
        Assert.NotNull(home["next_executable_workout"]);
        Assert.Equal(state.ActivatedSessionCount, home["current_window_sessions"]!.AsArray().Count);

        var calendar = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/calendar?month=2026-09"));
        Assert.Equal("rolling_long_horizon", calendar["schedule_strategy"]!.GetValue<string>());
        Assert.NotEmpty(calendar["sessions"]!.AsArray());
        Assert.DoesNotContain("numeric_pending", calendar.ToJsonString(), StringComparison.OrdinalIgnoreCase);

        var detail = await JsonAsync(await _client.GetAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}"));
        Assert.Equal(state.FirstSessionId, detail["session"]!["session_id"]!.GetValue<Guid>());
        Assert.Equal("rolling_long_horizon", detail["session"]!["schedule_strategy"]!.GetValue<string>());
        Assert.DoesNotContain("context", detail.ToJsonString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("target_lock", detail.ToJsonString(), StringComparison.OrdinalIgnoreCase);

        var details = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/details"));
        Assert.Equal("rolling_long_horizon", details["schedule_strategy"]!.GetValue<string>());
        Assert.Equal(21, details["rolling_structural_weeks"]!.AsArray().Count);
        Assert.Contains(details["rolling_structural_weeks"]!.AsArray(), w =>
            w!["lifecycle_status"]!.GetValue<string>() == "numeric_pending" && !w["numeric_details_available"]!.GetValue<bool>());
        Assert.Empty(details["weeks"]!.AsArray());
    }

    [Fact]
    public async Task Completion_PersistsActuals_PreservesPlan_AndExactReplayIsIdempotent()
    {
        var state = await ConfirmAsync();
        var request = new { contract_version = 1, actual_distance_km = 5.25, actual_duration_minutes = 31 };
        var first = await JsonAsync(await _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete", request));
        var replay = await JsonAsync(await _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete", request));
        Assert.Equal("completed", first["outcome"]!.GetValue<string>());
        Assert.Equal("idempotent_replay", replay["outcome"]!.GetValue<string>());
        Assert.False(first["next_window_activated"]!.GetValue<bool>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == state.FirstSessionId);
        Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Completed, session.OutcomeStatus);
        Assert.Equal(5.25, session.ActualDistanceKm);
        Assert.Equal(31, session.ActualDurationMinutes);
        Assert.Equal(state.PlannedDistanceKm, session.DistanceKm);
        Assert.Equal(state.AssignedDate, session.AssignedDate);
        Assert.Equal(state.ActivatedWeekCount, await db.LongHorizonRollingWeekStates.CountAsync(w => w.PlanStateId == state.RollingId && w.LifecycleState == LongHorizonPersistedLifecycleState.NumericActivated));
        Assert.Equal(0, await db.TrainingDays.CountAsync(d => d.PlanId == state.PlanId));
    }

    [Fact]
    public async Task ConflictingCompletion_IsTypedConflict_AndDoesNotOverwriteWinner()
    {
        var state = await ConfirmAsync();
        (await _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete",
            new { actual_distance_km = 5.0, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        var conflict = await _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete",
            new { actual_distance_km = 7.0, actual_duration_minutes = 40 });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Contains("LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT", await conflict.Content.ReadAsStringAsync());
    }

    /// <summary>Phase 4M.3: schedule repair is now live, so a repairable
    /// trigger (KEY_SESSION/LONG_RUN, operational reason, valid candidate)
    /// legitimately produces a replacement session -- this test's own
    /// pre-4M.3 name/assertion ("never reschedules") described the era before
    /// this phase wired adaptation into the endpoint. What must still hold
    /// unconditionally: the source NotToday session itself is durable and
    /// idempotent -- never re-marked, its own reason/date never change, and a
    /// replay never creates a second replacement. Next-window activation
    /// still never happens automatically (that remains 4M.4 scope).</summary>
    [Fact]
    public async Task NotToday_SourceIsDurableAndIdempotent_ReplayNeverDuplicatesAdaptation()
    {
        var state = await ConfirmAsync();
        var request = new { reason = "fatigue" };
        var first = await JsonAsync(await _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/not-today", request));
        var replay = await JsonAsync(await _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/not-today", request));
        Assert.Equal("not_today", first["outcome"]!.GetValue<string>());
        Assert.Equal("idempotent_replay", replay["outcome"]!.GetValue<string>());
        Assert.False(first["next_window_activated"]!.GetValue<bool>());
        Assert.Equal(first["schedule_repair_action"]!.GetValue<string>(), replay["schedule_repair_action"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == state.FirstSessionId);
        Assert.Equal(LongHorizonRollingSessionOutcomeStatus.NotToday, row.OutcomeStatus);
        Assert.Equal("fatigue", row.NotTodayReason);
        Assert.Equal(state.AssignedDate, row.AssignedDate);
        // At most one replacement/audit record for this trigger regardless of
        // how many times the request was replayed.
        Assert.Equal(1, await db.LongHorizonAdaptationDecisionRecords.CountAsync(r => r.TriggerSessionId == state.FirstSessionId));
        var sessionCountAfterFirst = await db.LongHorizonRollingSessionStates.CountAsync(s => s.Week.PlanStateId == state.RollingId);
        Assert.Equal(sessionCountAfterFirst, await db.LongHorizonRollingSessionStates.CountAsync(s => s.Week.PlanStateId == state.RollingId));
    }

    [Fact]
    public async Task CompletionVersusNotToday_HasExactlyOneDurableOutcome()
    {
        var state = await ConfirmAsync();
        using var second = _factory.CreateClient();
        var responses = await Task.WhenAll(
            _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete", new { actual_distance_km = 5.0, actual_duration_minutes = 30 }),
            second.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/not-today", new { reason = "schedule" }));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);

        using var scope = _factory.Services.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<AppDbContext>().LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == state.FirstSessionId);
        Assert.Contains(row.OutcomeStatus, new[] { LongHorizonRollingSessionOutcomeStatus.Completed, LongHorizonRollingSessionOutcomeStatus.NotToday });
    }

    [Fact]
    public async Task ConcurrentIdenticalCompletion_ProducesOneOutcomeAndReplay()
    {
        var state = await ConfirmAsync();
        using var second = _factory.CreateClient();
        var payload = new { actual_distance_km = 5.0, actual_duration_minutes = 30 };
        var responses = await Task.WhenAll(
            _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete", payload),
            second.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete", payload));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var outcomes = await Task.WhenAll(responses.Select(JsonAsync));
        Assert.Contains(outcomes, j => j["outcome"]!.GetValue<string>() == "completed");
        Assert.Contains(outcomes, j => j["outcome"]!.GetValue<string>() == "idempotent_replay");
    }

    [Fact]
    public async Task CrossUserAndUnknownSession_AreNonDisclosing()
    {
        var state = await ConfirmAsync();
        using var scope = _factory.Services.CreateScope();
        var read = scope.ServiceProvider.GetRequiredService<ILongHorizonActiveReadModelProvider>();
        var mutate = scope.ServiceProvider.GetRequiredService<ILongHorizonRollingSessionMutationService>();
        await Assert.ThrowsAsync<LongHorizonRollingSessionNotFoundException>(() => read.GetSessionDetailAsync(Guid.NewGuid(), state.FirstSessionId));
        await Assert.ThrowsAsync<LongHorizonRollingSessionNotFoundException>(() => mutate.CompleteAsync(Guid.NewGuid(), state.FirstSessionId,
            new LongHorizonCompleteSessionRequest { ActualDistanceKm = 5, ActualDurationMinutes = 30 }));
        var missing = await _client.GetAsync($"/api/v1/training-days/rolling/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task CompletingEverySessionInAWeek_MakesOnlyThatWeekTerminal_AndEvidenceReady()
    {
        var state = await ConfirmAsync();
        List<(Guid Id, double Distance)> weekSessions;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            weekSessions = await db.LongHorizonRollingSessionStates.AsNoTracking()
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek == 1)
                .OrderBy(s => s.SessionOrdinal).Select(s => new ValueTuple<Guid, double>(s.Id, s.DistanceKm)).ToListAsync();
        }
        foreach (var session in weekSessions)
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                new { actual_distance_km = session.Distance, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();

        using var verifyScope = _factory.Services.CreateScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(LongHorizonPersistedLifecycleState.Completed,
            (await verify.LongHorizonRollingWeekStates.AsNoTracking().SingleAsync(w => w.PlanStateId == state.RollingId && w.GlobalWeek == 1)).LifecycleState);
        Assert.Equal(state.ActivatedWeekCount - 1,
            await verify.LongHorizonRollingWeekStates.CountAsync(w => w.PlanStateId == state.RollingId && w.LifecycleState == LongHorizonPersistedLifecycleState.NumericActivated));
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericPending,
            (await verify.LongHorizonRollingWeekStates.AsNoTracking().SingleAsync(w => w.PlanStateId == state.RollingId && w.GlobalWeek == 2)).LifecycleState);
        Assert.Empty(await verify.LongHorizonCheckpointRecords.Where(c => c.PlanStateId == state.RollingId).ToListAsync());
        var persisted = await verify.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek == 1).ToListAsync();
        var evidence = LongHorizonRollingOutcomeEvidenceAdapter.ToCheckpointRows(persisted);
        Assert.Equal(persisted.Count, evidence.Count);
        Assert.All(evidence, row => Assert.Equal(TrainingDayStatus.Completed, row.TrainingDay.Status));
    }

    [Fact]
    public async Task Cancellation_RemovesActiveReadsAndProhibitsMutation_WhileRetainingHistory()
    {
        var state = await ConfirmAsync();
        (await _client.PostRawAsync($"/api/v1/plans/{state.PlanId}/cancel", new { reason = "test" })).EnsureSuccessStatusCode();
        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Null(home["active_plan"]);
        var mutation = await _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete",
            new { actual_distance_km = 5.0, actual_duration_minutes = 30 });
        Assert.Equal(HttpStatusCode.NotFound, mutation.StatusCode);
        using var scope = _factory.Services.CreateScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<AppDbContext>().LongHorizonRollingPlanStates.AnyAsync(p => p.Id == state.RollingId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task PreCommitFailure_RollsBackOutcome_AndCorrectedRetrySucceeds(int failpointValue)
    {
        var failpoint = (LongHorizonMutationFailpoint)failpointValue;
        var state = await ConfirmAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var service = new LongHorizonRollingSessionMutationService(
                scope.ServiceProvider.GetRequiredService<AppDbContext>(),
                scope.ServiceProvider.GetRequiredService<ILogger<LongHorizonRollingSessionMutationService>>(),
                scope.ServiceProvider.GetRequiredService<ILoggerFactory>(),
                new LongHorizonTestMutationFailureInjector(failpoint));
            await Assert.ThrowsAsync<LongHorizonInjectedMutationFailureException>(() => service.CompleteAsync(state.OwnerId, state.FirstSessionId,
                new LongHorizonCompleteSessionRequest { ActualDistanceKm = 5, ActualDurationMinutes = 30 }));
        }
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var row = await verifyScope.ServiceProvider.GetRequiredService<AppDbContext>().LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == state.FirstSessionId);
            Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Planned, row.OutcomeStatus);
            Assert.Null(row.ActualDistanceKm);
        }
        (await _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete",
            new { actual_distance_km = 5.0, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PostCommitAcknowledgementLoss_IsRecoveredByExactReplay()
    {
        var state = await ConfirmAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var service = new LongHorizonRollingSessionMutationService(
                scope.ServiceProvider.GetRequiredService<AppDbContext>(),
                scope.ServiceProvider.GetRequiredService<ILogger<LongHorizonRollingSessionMutationService>>(),
                scope.ServiceProvider.GetRequiredService<ILoggerFactory>(),
                new LongHorizonTestMutationFailureInjector(LongHorizonMutationFailpoint.AfterCommitBeforeAcknowledgement));
            await Assert.ThrowsAsync<LongHorizonInjectedMutationFailureException>(() => service.CompleteAsync(state.OwnerId, state.FirstSessionId,
                new LongHorizonCompleteSessionRequest { ActualDistanceKm = 5, ActualDurationMinutes = 30 }));
        }
        var replay = await JsonAsync(await _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete",
            new { actual_distance_km = 5.0, actual_duration_minutes = 30 }));
        Assert.Equal("idempotent_replay", replay["outcome"]!.GetValue<string>());
    }

    [Fact]
    public async Task MutationVersusCancellation_IsSerializedAndLeavesCoherentInactivePlan()
    {
        var state = await ConfirmAsync();
        using var second = _factory.CreateClient();
        var results = await Task.WhenAll(
            _client.PostRawAsync($"/api/v1/training-days/rolling/{state.FirstSessionId}/complete", new { actual_distance_km = 5.0, actual_duration_minutes = 30 }),
            second.PostRawAsync($"/api/v1/plans/{state.PlanId}/cancel", new { reason = "race" }));
        Assert.Equal(HttpStatusCode.OK, results[1].StatusCode);
        Assert.Contains(results[0].StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.NotFound });
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(TrainingPlanStatus.Cancelled, (await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == state.PlanId)).Status);
        var row = await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == state.FirstSessionId);
        Assert.Contains(row.OutcomeStatus, new[] { LongHorizonRollingSessionOutcomeStatus.Planned, LongHorizonRollingSessionOutcomeStatus.Completed });
    }

    [Fact]
    public async Task Swagger_ContainsRollingReadAndMutationContracts_WithoutPersistenceEntities()
    {
        var swagger = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/v1/training-days/rolling/{sessionId}", swagger);
        Assert.Contains("/api/v1/training-days/rolling/{sessionId}/complete", swagger);
        Assert.Contains("/api/v1/training-days/rolling/{sessionId}/not-today", swagger);
        Assert.Contains("LongHorizonCheckpointReadiness", swagger);
        Assert.DoesNotContain("LongHorizonRollingSessionState", swagger);
    }

    [Fact]
    public void PublicContractGraphs_DoNotExposeRollingPersistenceOrInternalAuthority()
    {
        var roots = new[] { typeof(LongHorizonHomeResponse), typeof(LongHorizonCalendarResponse),
            typeof(LongHorizonRollingSessionDetailResponse), typeof(LongHorizonSessionMutationResponse) };
        var types = new HashSet<Type>();
        foreach (var root in roots) Walk(root, types);
        Assert.DoesNotContain(types, t => t.Namespace == "RunningApp.Domain.Entities");
        var names = types.SelectMany(t => t.GetProperties()).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var forbidden in new[] { "TargetLock", "RunwayPrescription", "CoreContext", "EvidenceFingerprint", "CheckpointRecord", "Xmin", "IdempotencyKey", "FailureInjector" })
            Assert.DoesNotContain(forbidden, names);
    }

    private async Task<ConfirmedState> ConfirmAsync()
    {
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
        var start = new DateOnly(2026, 9, 7);
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race/long-horizon", new
        {
            goal_distance = "ten_k", level = "intermediate", days_per_week = 4, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"), preferred_days = new[] { "mon", "wed", "fri", "sun" }, long_run_day = "sun",
            race_date = start.AddDays(21 * 7).ToString("yyyy-MM-dd"), target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average", race_name = "Phase 4L.4",
            recent_weekly_volume_km = 20.0, recent_longest_run_km = 8.0, recent_runs_per_week = 4
        });
        var preview = await JsonAsync(previewResponse);
        var rollingId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = rollingId }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId).OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
        var first = sessions[0];
        return new ConfirmedState(planId, rollingId, plan.InternalUserId!.Value, first.Id, first.DistanceKm, first.AssignedDate, sessions.Count,
            sessions.Select(s => s.Week.GlobalWeek).Distinct().Count());
    }

    private static async Task<JsonNode> JsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonNode.Parse(body)!;
    }

    private static void Walk(Type type, ISet<Type> seen)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(DateOnly) || type == typeof(DateTime) || !seen.Add(type)) return;
        if (type.IsArray) { Walk(type.GetElementType()!, seen); return; }
        if (type.IsGenericType) foreach (var argument in type.GetGenericArguments()) Walk(argument, seen);
        foreach (var property in type.GetProperties()) Walk(property.PropertyType, seen);
    }

    private sealed record ConfirmedState(Guid PlanId, Guid RollingId, Guid OwnerId, Guid FirstSessionId, double PlannedDistanceKm,
        DateOnly AssignedDate, int ActivatedSessionCount, int ActivatedWeekCount);
}
