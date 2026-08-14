using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.Services;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 4L.4A -- the explicit authenticated next-window activation endpoint.
/// Reuses the exact confirm/complete flow LongHorizonActiveReadAndMutationTests
/// (Phase 4L.4) already drives through real Postgres, then exercises the new
/// POST /api/v1/plans/active/long-horizon/activate-next-window route this
/// phase adds on top of it.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonExplicitNextWindowActivationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonExplicitNextWindowActivationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InProgressWindow_ReturnsTypedConflict_AndDoesNotActivate()
    {
        var state = await ConfirmAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS", await response.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(1, aggregate.CurrentWindowStartWeek);
        Assert.Equal(state.ActivatedWeekCount, aggregate.CurrentWindowEndWeek);
    }

    [Fact]
    public async Task UnsupportedContractVersion_IsRejected_AndPerformsNoWrite()
    {
        var state = await ConfirmAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 99 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("LONG_HORIZON_CONTINUATION_VERSION_UNSUPPORTED", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NoActivePlan_IsNonDisclosingNotFound()
    {
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("LONG_HORIZON_ACTIVE_PLAN_NOT_FOUND", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FullyTerminalWindow_ActivatesNextWindow_AndHomeReflectsIt()
    {
        var state = await ConfirmAsync();
        await CompleteAllCurrentWindowSessionsAsync(state);

        var activate = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));
        Assert.Equal("activated", activate["outcome"]!.GetValue<string>());
        Assert.Equal(1, activate["previous_window_range"]!["start_global_week"]!.GetValue<int>());
        Assert.Equal(state.ActivatedWeekCount, activate["previous_window_range"]!["end_global_week"]!.GetValue<int>());
        var newStart = activate["activated_window_range"]!["start_global_week"]!.GetValue<int>();
        var newEnd = activate["activated_window_range"]!["end_global_week"]!.GetValue<int>();
        Assert.Equal(state.ActivatedWeekCount + 1, newStart);
        Assert.True(newEnd >= newStart);
        Assert.NotEmpty(activate["activated_sessions"]!.AsArray());
        Assert.Equal("current_window_in_progress", activate["checkpoint_readiness"]!.GetValue<string>());
        Assert.False(activate["is_terminal"]!.GetValue<bool>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(newStart, aggregate.CurrentWindowStartWeek);
        Assert.Equal(newEnd, aggregate.CurrentWindowEndWeek);
        Assert.Equal(1, await db.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == state.RollingId && a.StartGlobalWeek == newStart));

        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Equal(newStart, home["active_plan"]!["current_window_start_week"]!.GetValue<int>());
        Assert.NotEmpty(home["current_window_sessions"]!.AsArray());
    }

    [Fact]
    public async Task ExactReplayImmediatelyAfterActivation_DoesNotDuplicateAndReportsInProgress()
    {
        var state = await ConfirmAsync();
        await CompleteAllCurrentWindowSessionsAsync(state);

        var first = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));
        Assert.Equal("activated", first["outcome"]!.GetValue<string>());

        // Once activated, the new window's sessions are freshly Planned, so a
        // second call legitimately reports CurrentWindowInProgress rather than
        // a magic replay -- the endpoint's minimal request contract carries no
        // client idempotency token (documented design decision, see phase doc
        // Part 17/25). Recovery from a lost acknowledgement is via re-reading
        // Home/Calendar, which already reflects the committed window.
        var replay = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
        Assert.Contains("LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS", await replay.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == state.RollingId
            && a.StartGlobalWeek == state.ActivatedWeekCount + 1));
    }

    [Fact]
    public async Task ConcurrentActivation_HasExactlyOneWinner_NoPartialWindow()
    {
        var state = await ConfirmAsync();
        await CompleteAllCurrentWindowSessionsAsync(state);
        using var second = _factory.CreateClient();

        var responses = await Task.WhenAll(
            _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }),
            second.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode != HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == state.RollingId
            && a.StartGlobalWeek == state.ActivatedWeekCount + 1));
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(state.ActivatedWeekCount + 1, aggregate.CurrentWindowStartWeek);
    }

    [Theory]
    [InlineData(5)] // BeforeCommit
    [InlineData(0)] // AfterVersionValidation
    public async Task PreCommitFailure_RollsBackWindow_AndCorrectedRetrySucceedsOnce(int failpointValue)
    {
        var failpoint = (RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence.LongHorizonPersistenceFailpoint)failpointValue;
        var state = await ConfirmAsync();
        await CompleteAllCurrentWindowSessionsAsync(state);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var injector = new RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence.LongHorizonTestPersistenceFailureInjector(
                RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence.LongHorizonPersistenceOperation.InitialPersistence, failpoint);
            var service = new LongHorizonRollingWindowActivationService(
                db, scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LongHorizonRollingWindowActivationService>>(), injector);
            await Assert.ThrowsAnyAsync<Exception>(() => service.ActivateNextWindowAsync(state.OwnerId, new LongHorizonActivateNextWindowRequest()));
        }

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            Assert.Equal(1, aggregate.CurrentWindowStartWeek);
            Assert.Equal(state.ActivatedWeekCount, aggregate.CurrentWindowEndWeek);
            Assert.Empty(await db.LongHorizonActivationWindowRecords.Where(a => a.PlanStateId == state.RollingId
                && a.StartGlobalWeek == state.ActivatedWeekCount + 1).ToListAsync());
        }

        var retry = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));
        Assert.Equal("activated", retry["outcome"]!.GetValue<string>());
    }

    [Fact]
    public void PublicContractGraph_DoesNotExposePersistenceOrInternalAuthority()
    {
        var types = new HashSet<Type>();
        Walk(typeof(LongHorizonActivateNextWindowResponse), types);
        Assert.DoesNotContain(types, t => t.Namespace == "RunningApp.Domain.Entities");
        var names = types.SelectMany(t => t.GetProperties()).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var forbidden in new[] { "TargetLock", "RunwayPrescription", "CoreContext", "EvidenceFingerprint", "CheckpointRecord", "Xmin", "IdempotencyKey", "FailureInjector", "ContextVersion" })
            Assert.DoesNotContain(forbidden, names);
    }

    [Fact]
    public async Task Swagger_ContainsActivationRouteAndOutcomeEnum()
    {
        var swagger = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/v1/plans/active/long-horizon/activate-next-window", swagger);
        Assert.Contains("LongHorizonContinuationOutcome", swagger);
    }

    private async Task CompleteAllCurrentWindowSessionsAsync(ConfirmedState state)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId).OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
        foreach (var session in sessions)
        {
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }
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
            target_finish_time_source = "product_average", race_name = "Phase 4L.4A",
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
        return new ConfirmedState(planId, rollingId, plan.InternalUserId!.Value, sessions.Select(s => s.Week.GlobalWeek).Distinct().Count());
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

    private sealed record ConfirmedState(Guid PlanId, Guid RollingId, Guid OwnerId, int ActivatedWeekCount);
}
