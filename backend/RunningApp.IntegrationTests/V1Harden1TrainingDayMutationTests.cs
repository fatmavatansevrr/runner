using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// PHASE V1-HARDEN.1 — real HTTP/real-PostgreSQL proof that the Core
/// TrainingDay Complete/Not-Today mutations are safe under repeated,
/// retried, conflicting, and concurrent requests, and that a cancelled
/// plan's days can no longer be mutated. Closes V1HARDEN0-002
/// (Complete idempotency/duplicate-log), V1HARDEN0-003 (duplicate Pending
/// Not-Today decisions) and V1HARDEN0-004 (a Completed day silently
/// reverted to Missed). Mirrors <see cref="FiveK1PublicActivationEndToEndTests"/>'s
/// own real HTTP + Postgres proof shape (same <see cref="CustomWebApplicationFactory"/>
/// configuration, same reset-before-each pattern) — no new E2E test
/// infrastructure invented. Uses the 5K Intermediate 4D cell per the
/// governing prompt's own guidance (mutation behavior is plan-distance
/// agnostic); one TEN_K regression test at the bottom proves the guard is
/// generic, not FIVE_K-specific.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class V1Harden1TrainingDayMutationTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public V1Harden1TrainingDayMutationTests(PublishedCatalogTestRelease release)
    {
        _factory = new CustomWebApplicationFactory("Production", new Dictionary<string, string?>
        {
            ["Auth:Provider"] = "Mock",
            ["PlanCatalog:CatalogRootPath"] = release.ReleaseRoot,
            ["CatalogLivePilot:Enabled"] = "true",
            ["LocalCatalogAcceptance:Enabled"] = "false",
            ["ProductionConfigurationValidation:Enabled"] = "false",
        });
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static async Task ResetAsync()
    {
        using var resetFactory = new CustomWebApplicationFactory();
        using var resetClient = resetFactory.CreateClient();
        (await resetClient.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
    }

    private static object FiveKRequest(DateOnly start) => new
    {
        goal_distance = "five_k",
        level = "intermediate",
        days_per_week = 4,
        unit = "km",
        start_date = start.ToString("yyyy-MM-dd"),
        preferred_days = new[] { "tue", "thu", "sat", "sun" },
        long_run_day = "sun",
        race_date = start.AddDays(10 * 7).ToString("yyyy-MM-dd"),
        target_finish_time_seconds = 1200,
        target_finish_time_source = "user_defined",
        recent_weekly_volume_km = 25.0,
        recent_longest_run_km = 8,
        recent_runs_per_week = 4,
        recent_race = new { distance = "five_k", finish_time_seconds = 1250, race_date = start.AddDays(-21).ToString("yyyy-MM-dd") },
        race_name = "V1-HARDEN.1 mutation test",
    };

    private static object TenKRequest(DateOnly start) => new
    {
        goal_distance = "ten_k",
        level = "intermediate",
        days_per_week = 4,
        unit = "km",
        start_date = start.ToString("yyyy-MM-dd"),
        preferred_days = new[] { "tue", "thu", "sat", "sun" },
        long_run_day = "sun",
        race_date = start.AddDays(10 * 7).ToString("yyyy-MM-dd"),
        target_finish_time_seconds = 2700,
        target_finish_time_source = "user_defined",
        recent_weekly_volume_km = 30.0,
        recent_longest_run_km = 10,
        recent_runs_per_week = 4,
        recent_race = new { distance = "ten_k", finish_time_seconds = 2800, race_date = start.AddDays(-21).ToString("yyyy-MM-dd") },
        race_name = "V1-HARDEN.1 mutation test (TEN_K regression)",
    };

    /// <summary>Confirms a fresh plan and returns (planId, a mutable scheduled TrainingDayId).</summary>
    private async Task<(Guid PlanId, Guid DayId)> CreateActivePlanWithScheduledDayAsync(object? request = null)
    {
        await ResetAsync();
        var start = new DateOnly(2026, 8, 4); // Tuesday
        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request ?? FiveKRequest(start));
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        var calendar = await _client.GetJsonAsync($"/api/v1/plans/active/calendar?month={start:yyyy-MM}");
        var day = calendar.AsArray().First(d => d!["day_id"] is not null && d["day_type"]!.GetValue<string>() != "rest");
        var dayId = Guid.Parse(day!["day_id"]!.GetValue<string>());
        return (planId, dayId);
    }

    private AppDbContext NewDbContext(out IServiceScope scope)
    {
        scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private static object CompleteBody(double distanceKm = 5.0, int durationMin = 30) => new
    {
        actual_distance_km = distanceKm,
        actual_duration_min = durationMin,
        user_note = (string?)null,
    };

    // ───────────────────────── Complete retry (V1HARDEN0-002) ─────────────────────────

    [Fact]
    public async Task CompleteRetry_SameValues_IsIdempotent_NoDuplicateLogOrEvent()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();
        var body = CompleteBody();

        var r1 = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", body);
        var r2 = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", body);

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logCount = await db.WorkoutLogs.CountAsync(l => l.TrainingDayId == dayId);
        var eventCount = await db.PlanEvents.CountAsync(e => e.TrainingDayId == dayId && e.EventType == "WorkoutCompleted");
        var day = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == dayId);

        Assert.Equal(1, logCount);
        Assert.Equal(1, eventCount);
        Assert.Equal(TrainingDayStatus.Completed, day.Status);
    }

    [Fact]
    public async Task CompleteRetry_DifferentValues_Returns409_DoesNotOverwriteOriginal()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();

        var r1 = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", CompleteBody(5.0, 30));
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        var r2 = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", CompleteBody(8.0, 50));
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
        var body2 = await r2.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("TRAINING_DAY_COMPLETION_CONFLICT", body2!["errorCode"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == dayId);
        Assert.Equal(5.0, day.ActualDistanceKm);
        Assert.Equal(1, await db.WorkoutLogs.CountAsync(l => l.TrainingDayId == dayId));
    }

    // ───────────────────────── Completed→NotToday protection (V1HARDEN0-004) ─────────────────────────

    [Fact]
    public async Task CompletedDay_CannotBeRevertedByNotToday_CreateIsRejected()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();
        (await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", CompleteBody())).EnsureSuccessStatusCode();

        var r = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/not-today-decisions", new { reason = "feeling_tired" });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        var body = await r.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("TRAINING_DAY_TRANSITION_CONFLICT", body!["errorCode"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == dayId);
        Assert.Equal(TrainingDayStatus.Completed, day.Status);
        Assert.Equal(0, await db.NotTodayDecisions.CountAsync(d => d.TrainingDayId == dayId));
    }

    [Fact]
    public async Task CompletedDay_CannotBeRevertedByNotToday_RaceBetweenCreateAndComplete_ConfirmIsRejected()
    {
        // V1HARDEN0-004's exact repro: decision created while day was still
        // Planned, then the day is completed BEFORE the decision is
        // confirmed. ConfirmNotTodayDecisionAsync -- the method that
        // actually performs the Missed mutation -- must still refuse.
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();

        var createResp = await _client.PostJsonAsync($"/api/v1/training-days/{dayId}/not-today-decisions", new { reason = "feeling_tired" });
        var decisionId = Guid.Parse(createResp["decision_id"]!.GetValue<string>());

        (await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", CompleteBody())).EnsureSuccessStatusCode();

        var confirmResp = await _client.PostRawAsync($"/api/v1/not-today-decisions/{decisionId}/confirm", new { });
        Assert.Equal(HttpStatusCode.Conflict, confirmResp.StatusCode);
        var body = await confirmResp.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("TRAINING_DAY_TRANSITION_CONFLICT", body!["errorCode"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == dayId);
        var decision = await db.NotTodayDecisions.AsNoTracking().SingleAsync(d => d.Id == decisionId);
        Assert.Equal(TrainingDayStatus.Completed, day.Status); // preserved, not reverted
        Assert.Equal(NotTodayDecisionStatus.Cancelled, decision.Status);
        Assert.Equal(0, await db.PlanEvents.CountAsync(e => e.TrainingDayId == dayId && e.EventType == "WorkoutMissed"));
    }

    // ───────────────────────── NotToday→Complete (resolved from source, not a product-decision punt) ─────────────────────────

    [Fact]
    public async Task MissedDay_CannotBeCompleted_NotTodayToCompleteRejected()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();

        var createResp = await _client.PostJsonAsync($"/api/v1/training-days/{dayId}/not-today-decisions", new { reason = "feeling_tired" });
        var decisionId = Guid.Parse(createResp["decision_id"]!.GetValue<string>());
        (await _client.PostRawAsync($"/api/v1/not-today-decisions/{decisionId}/confirm", new { })).EnsureSuccessStatusCode();

        var completeResp = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", CompleteBody());
        Assert.Equal(HttpStatusCode.Conflict, completeResp.StatusCode);
        var body = await completeResp.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("TRAINING_DAY_TRANSITION_CONFLICT", body!["errorCode"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == dayId);
        Assert.Equal(TrainingDayStatus.Missed, day.Status);
        Assert.Equal(0, await db.WorkoutLogs.CountAsync(l => l.TrainingDayId == dayId));
    }

    // ───────────────────────── NotToday retry / duplicate Pending (V1HARDEN0-003) ─────────────────────────

    [Fact]
    public async Task NotTodayRetry_Pending_DoesNotCreateDuplicateDecision()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();

        var r1 = await _client.PostJsonAsync($"/api/v1/training-days/{dayId}/not-today-decisions", new { reason = "feeling_tired" });
        var r2 = await _client.PostJsonAsync($"/api/v1/training-days/{dayId}/not-today-decisions", new { reason = "feeling_tired" });

        Assert.Equal(Guid.Parse(r1["decision_id"]!.GetValue<string>()), Guid.Parse(r2["decision_id"]!.GetValue<string>()));
        Assert.Equal("pending", r2["status"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.NotTodayDecisions.CountAsync(d => d.TrainingDayId == dayId));
    }

    [Fact]
    public async Task NotTodayConfirmRetry_IsIdempotent_NoDuplicateMissedEvent()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();
        var createResp = await _client.PostJsonAsync($"/api/v1/training-days/{dayId}/not-today-decisions", new { reason = "feeling_tired" });
        var decisionId = Guid.Parse(createResp["decision_id"]!.GetValue<string>());

        var c1 = await _client.PostRawAsync($"/api/v1/not-today-decisions/{decisionId}/confirm", new { });
        var c2 = await _client.PostRawAsync($"/api/v1/not-today-decisions/{decisionId}/confirm", new { });

        Assert.Equal(HttpStatusCode.OK, c1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, c2.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.PlanEvents.CountAsync(e => e.TrainingDayId == dayId && e.EventType == "WorkoutMissed"));
    }

    // ───────────────────────── Cancelled-plan guard ─────────────────────────

    [Fact]
    public async Task CancelledPlan_CompleteIsBlocked()
    {
        var (planId, dayId) = await CreateActivePlanWithScheduledDayAsync();
        (await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel", new { reason = (string?)null })).EnsureSuccessStatusCode();

        var r = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", CompleteBody());
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == dayId);
        Assert.Equal(TrainingDayStatus.Planned, day.Status);
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(TrainingPlanStatus.Cancelled, plan.Status);
    }

    [Fact]
    public async Task CancelledPlan_NotTodayIsBlocked()
    {
        var (planId, dayId) = await CreateActivePlanWithScheduledDayAsync();
        (await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel", new { reason = (string?)null })).EnsureSuccessStatusCode();

        var r = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/not-today-decisions", new { reason = "feeling_tired" });
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.NotTodayDecisions.CountAsync(d => d.TrainingDayId == dayId));
    }

    // ───────────────────────── Real-Postgres concurrency proof ─────────────────────────

    [Fact]
    public async Task ConcurrentIdenticalCompletes_ProduceOneLogicalTransition()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();
        var body = CompleteBody();

        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();

        var t1 = clientA.PostRawAsync($"/api/v1/training-days/{dayId}/complete", body);
        var t2 = clientB.PostRawAsync($"/api/v1/training-days/{dayId}/complete", body);
        var results = await Task.WhenAll(t1, t2);

        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.WorkoutLogs.CountAsync(l => l.TrainingDayId == dayId));
        Assert.Equal(1, await db.PlanEvents.CountAsync(e => e.TrainingDayId == dayId && e.EventType == "WorkoutCompleted"));
        var day = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == dayId);
        Assert.Equal(TrainingDayStatus.Completed, day.Status);
    }

    [Fact]
    public async Task ConcurrentConflictingCompletes_ExactlyOneWins_NoLastWriteWinsCorruption()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();

        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();

        var t1 = clientA.PostRawAsync($"/api/v1/training-days/{dayId}/complete", CompleteBody(5.0, 30));
        var t2 = clientB.PostRawAsync($"/api/v1/training-days/{dayId}/complete", CompleteBody(8.0, 50));
        var results = await Task.WhenAll(t1, t2);

        var okCount = results.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, okCount);
        Assert.Equal(1, conflictCount);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.WorkoutLogs.CountAsync(l => l.TrainingDayId == dayId));
        var day = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == dayId);
        Assert.Equal(TrainingDayStatus.Completed, day.Status);
        Assert.True(day.ActualDistanceKm == 5.0 || day.ActualDistanceKm == 8.0);
    }

    [Fact]
    public async Task ConcurrentCompleteVsConfirmNotToday_ExactlyOneDeterministicFinalState()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();
        var createResp = await _client.PostJsonAsync($"/api/v1/training-days/{dayId}/not-today-decisions", new { reason = "feeling_tired" });
        var decisionId = Guid.Parse(createResp["decision_id"]!.GetValue<string>());

        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();

        var tComplete = clientA.PostRawAsync($"/api/v1/training-days/{dayId}/complete", CompleteBody());
        var tConfirm = clientB.PostRawAsync($"/api/v1/not-today-decisions/{decisionId}/confirm", new { });
        await Task.WhenAll(tComplete, tConfirm);
        var completeResp = await tComplete;
        var confirmResp = await tConfirm;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == dayId);
        var decision = await db.NotTodayDecisions.AsNoTracking().SingleAsync(d => d.Id == decisionId);

        // Exactly one deterministic protected final state -- never both
        // "sort of completed and sort of missed" and never an uncontrolled 500.
        Assert.NotEqual(HttpStatusCode.InternalServerError, completeResp.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, confirmResp.StatusCode);

        if (day.Status == TrainingDayStatus.Completed)
        {
            Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, confirmResp.StatusCode);
            Assert.Equal(NotTodayDecisionStatus.Cancelled, decision.Status);
            Assert.Equal(1, await db.WorkoutLogs.CountAsync(l => l.TrainingDayId == dayId));
            Assert.Equal(0, await db.PlanEvents.CountAsync(e => e.TrainingDayId == dayId && e.EventType == "WorkoutMissed"));
        }
        else
        {
            Assert.Equal(TrainingDayStatus.Missed, day.Status);
            Assert.Equal(HttpStatusCode.Conflict, completeResp.StatusCode);
            Assert.Equal(HttpStatusCode.OK, confirmResp.StatusCode);
            Assert.Equal(NotTodayDecisionStatus.Confirmed, decision.Status);
            Assert.Equal(0, await db.WorkoutLogs.CountAsync(l => l.TrainingDayId == dayId));
            Assert.Equal(1, await db.PlanEvents.CountAsync(e => e.TrainingDayId == dayId && e.EventType == "WorkoutMissed"));
        }
    }

    // ───────────────────────── Valid flows still work ─────────────────────────

    [Fact]
    public async Task ScheduledDay_Complete_Succeeds()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();
        var r = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", CompleteBody());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task ScheduledDay_NotToday_Succeeds()
    {
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync();
        var r = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/not-today-decisions", new { reason = "feeling_tired" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    // ───────────────────────── TEN_K regression: guard is generic, not FIVE_K-specific ─────────────────────────

    [Fact]
    public async Task TenK_CompleteRetry_SameValues_IsAlsoIdempotent()
    {
        var start = new DateOnly(2026, 8, 4);
        var (_, dayId) = await CreateActivePlanWithScheduledDayAsync(TenKRequest(start));
        var body = CompleteBody(10.0, 55);

        (await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", body)).EnsureSuccessStatusCode();
        var r2 = await _client.PostRawAsync($"/api/v1/training-days/{dayId}/complete", body);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.WorkoutLogs.CountAsync(l => l.TrainingDayId == dayId));
    }
}
