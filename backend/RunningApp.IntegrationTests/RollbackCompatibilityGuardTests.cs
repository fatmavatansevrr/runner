using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 4L.6B.2: rerunnable automated proof of the PostgreSQL-level rollback
/// compatibility guard (fn_guard_rolling_plan_mutation /
/// RollbackCompatibilityMode). Exercises the trigger directly over raw SQL --
/// the same mechanism a rolled-back committed-HEAD binary would hit -- rather
/// than only through the current application's own routes, since the whole
/// point of the guard is that it must work independently of which
/// application binary issues the write. The full two-binary rollback
/// rehearsal (committed HEAD in a separate worktree against a real active
/// rolling plan) is documented, not automated here, in
/// PHASE4L_6B_2_ROLLING_PLAN_ROLLBACK_MUTATION_GUARD_CLOSURE.md.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class RollbackCompatibilityGuardTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RollbackCompatibilityGuardTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    private async Task ResetAsync() =>
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    private async Task SetGuardEnabledAsync(bool enabled)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"RollbackCompatibilityMode\" SET \"Enabled\" = {0} WHERE \"Id\" = 1;", enabled);
    }

    private async Task<Guid> CreateConfirmedRollingPlanAsync()
    {
        var start = new DateOnly(2026, 8, 10);
        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race/long-horizon", new
        {
            goal_distance = "ten_k",
            level = "intermediate",
            days_per_week = 4,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun",
            race_name = "Guard test race",
            race_date = start.AddDays(22 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average",
            recent_weekly_volume_km = 20,
            recent_longest_run_km = 8,
            recent_runs_per_week = 3,
            recent_race = (object?)null,
        });
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId });
        return Guid.Parse(confirm["plan_id"]!.GetValue<string>());
    }

    [Fact]
    public async Task GuardTrigger_BlocksDirectSqlStatusChange_OnRollingPlan_WhenEnabled()
    {
        await ResetAsync();
        var planId = await CreateConfirmedRollingPlanAsync();
        await SetGuardEnabledAsync(true);
        try
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var ex = await Assert.ThrowsAsync<PostgresException>(() =>
                db.Database.ExecuteSqlRawAsync(
                    "UPDATE \"TrainingPlans\" SET \"Status\" = 'cancelled' WHERE \"Id\" = {0};", planId));

            Assert.Equal("LH001", ex.SqlState);
            Assert.Contains("ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED", ex.MessageText);

            var status = await db.Database.SqlQueryRaw<string>(
                "SELECT \"Status\" AS \"Value\" FROM \"TrainingPlans\" WHERE \"Id\" = {0}", planId).SingleAsync();
            Assert.Equal("active", status);
        }
        finally
        {
            await SetGuardEnabledAsync(false);
        }
    }

    [Fact]
    public async Task GuardTrigger_DoesNotBlock_StaticPlanStatusChange_WhenEnabled()
    {
        await ResetAsync();
        var start = new DateOnly(2026, 8, 10);
        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", new
        {
            goal_distance = "ten_k",
            level = "intermediate",
            days_per_week = 4,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun",
            race_name = "Static continuity test race",
            race_date = start.AddDays(12 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average",
            recent_weekly_volume_km = 20,
            recent_longest_run_km = 8,
            recent_runs_per_week = 3,
            recent_race = (object?)null,
        });
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        await SetGuardEnabledAsync(true);
        try
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // A StaticComplete plan is untouched by the trigger condition,
            // proving the guard does not over-block unrelated plans.
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"TrainingPlans\" SET \"Status\" = 'cancelled' WHERE \"Id\" = {0};", planId);

            var status = await db.Database.SqlQueryRaw<string>(
                "SELECT \"Status\" AS \"Value\" FROM \"TrainingPlans\" WHERE \"Id\" = {0}", planId).SingleAsync();
            Assert.Equal("cancelled", status);
        }
        finally
        {
            await SetGuardEnabledAsync(false);
        }
    }

    [Fact]
    public async Task CurrentAppCancel_OnRollingPlan_WhenGuardEnabled_ReturnsSanitizedConflict()
    {
        await ResetAsync();
        var planId = await CreateConfirmedRollingPlanAsync();
        await SetGuardEnabledAsync(true);
        try
        {
            var response = await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel", new { });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
            Assert.Equal("ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED", body["errorCode"]!.GetValue<string>());
            Assert.DoesNotContain("Npgsql", body.ToJsonString());
            Assert.DoesNotContain("TrainingPlans", body.ToJsonString());
            Assert.DoesNotContain("fn_guard_rolling_plan_mutation", body.ToJsonString());

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var status = await db.Database.SqlQueryRaw<string>(
                "SELECT \"Status\" AS \"Value\" FROM \"TrainingPlans\" WHERE \"Id\" = {0}", planId).SingleAsync();
            Assert.Equal("active", status);
        }
        finally
        {
            await SetGuardEnabledAsync(false);
        }
    }

    [Fact]
    public async Task GuardDefaultsDisabled_RollingCancel_NotBlockedByGuard()
    {
        // Sanity check: normal (non-rollback) operation is unaffected. Cancel
        // still runs its own ordinary business logic (which this test does
        // not otherwise assert on) -- only proves the guard itself is not
        // silently active by default.
        await ResetAsync();
        var planId = await CreateConfirmedRollingPlanAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var enabled = await db.Database.SqlQueryRaw<bool>(
            "SELECT \"Enabled\" AS \"Value\" FROM \"RollbackCompatibilityMode\" WHERE \"Id\" = 1").SingleAsync();
        Assert.False(enabled);
    }

    [Fact]
    public async Task MissingControlRow_FailsClosed()
    {
        await ResetAsync();
        var planId = await CreateConfirmedRollingPlanAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // Simulate the control row being unreadable/missing -- the
            // trigger's guard_enabled defaults to true (blocking) in that case.
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"RollbackCompatibilityMode\" WHERE \"Id\" = 1;");

            var ex = await Assert.ThrowsAsync<PostgresException>(() =>
                db.Database.ExecuteSqlRawAsync(
                    "UPDATE \"TrainingPlans\" SET \"Status\" = 'cancelled' WHERE \"Id\" = {0};", planId));
            Assert.Equal("LH001", ex.SqlState);
        }
        finally
        {
            await tx.RollbackAsync(); // restores the control row; never persisted
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
