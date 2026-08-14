using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Backend Integration Phase 4G.6C.2 — real mid-transaction failure
/// injection (Part 1-6) for the 15-20 week TEN_K Preparation Runway
/// confirmation path, using the test-only interceptor seam in
/// <see cref="TransactionFailureInjection"/>-adjacent types. Every failure
/// here is thrown from INSIDE the real EF/PostgreSQL transaction
/// (a real command about to execute, or a real transaction commit about to
/// happen) — never a mocked repository, never a pre-transaction throw.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PreparationRunwayTransactionAtomicityTests
{
    private static CustomWebApplicationFactory ConfirmationEnabledFactory() =>
        new("Development", new Dictionary<string, string?>
        {
            ["PreparationRunwayPilotActivation:Enabled"] = "true",
            ["PreparationRunwayPilotActivation:ConfirmationEnabled"] = "true",
        });

    private static object RaceRequest(string startDate, string raceDate) => new
    {
        goal_distance = "ten_k",
        level = "intermediate",
        days_per_week = 4,
        unit = "km",
        start_date = startDate,
        preferred_days = new[] { "mon", "wed", "fri", "sun" },
        long_run_day = "sun",
        race_date = raceDate,
        target_finish_time_seconds = 3480,
        target_finish_time_source = "product_average",
        race_name = (string?)null,
        recent_weekly_volume_km = 20,
        recent_longest_run_km = 8,
        recent_runs_per_week = 3,
        recent_race = (object?)null,
    };

    private static async Task<(int previews, int plans, int weeks, int days, int events)> CountRowsAsync(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (
            await ctx.PlanPreviews.CountAsync(),
            await ctx.TrainingPlans.CountAsync(),
            await ctx.TrainingWeeks.CountAsync(),
            await ctx.TrainingDays.CountAsync(),
            await ctx.PlanEvents.CountAsync());
    }

    private static async Task<string> GeneratePreviewAsync(HttpClient client, int totalWeeks)
    {
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var response = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        response.EnsureSuccessStatusCode();
        var preview = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Equal("preparation_runway_preview_confirmable", preview["lifecycle"]!.GetValue<string>());
        return preview["preview_id"]!.GetValue<string>();
    }

    [Theory]
    [InlineData("INSERT INTO \"TrainingWeeks\"", "week-insert")]
    [InlineData("INSERT INTO \"TrainingDays\"", "day-insert")]
    [InlineData("INSERT INTO \"PlanEvents\"", "planevent-insert")]
    public async Task InjectedMidTransactionCommandFailure_RollsBackEverything_PreviewRemainsUnconfirmed(string failWhenSqlContains, string label)
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        // 20 weeks -- the largest payload, giving the most rows any partial
        // write could plausibly leave behind if atomicity were broken.
        var previewId = await GeneratePreviewAsync(client, 20);
        var before = await CountRowsAsync(factory);

        var state = factory.Services.GetRequiredService<TransactionFailureInjectionState>();
        state.Reset();
        state.FailWhenSqlContains = failWhenSqlContains;
        state.FailAfterOccurrence = 1;

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });

        // The injection actually fired (proves this isn't a false-positive
        // "nothing happened" pass).
        Assert.True(state.CommandAttempted, $"[{label}] injected failure point was never reached -- test is not exercising real mid-transaction failure.");

        // Never a generic unhandled crash signature beyond the existing
        // 500 semantics; the injected exception must not leak raw detail.
        var body = await confirmResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("TransactionFailureInjectedException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("[TEST-ONLY INJECTED FAILURE]", body, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.InternalServerError, confirmResponse.StatusCode);

        var after = await CountRowsAsync(factory);
        // Full atomicity: zero net rows across every table this transaction touches.
        Assert.Equal(before, after);

        using (var scope = factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var preview = await ctx.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == Guid.Parse(previewId));
            Assert.Null(preview.ConfirmedPlanId);
        }

        // Clean retry (injection disabled) succeeds deterministically.
        state.Reset();
        var retryResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var retryBody = await retryResponse.Content.ReadAsStringAsync();
        Assert.True(retryResponse.StatusCode == HttpStatusCode.OK, retryBody);
        var retryAfter = await CountRowsAsync(factory);
        Assert.Equal(before.plans + 1, retryAfter.plans);
        Assert.Equal(before.weeks + 20, retryAfter.weeks);
        Assert.Equal(before.days + 80, retryAfter.days);
    }

    [Fact]
    public async Task InjectedCommitFailure_RollsBackEverything_PreviewRemainsUnconfirmed()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var previewId = await GeneratePreviewAsync(client, 18);
        var before = await CountRowsAsync(factory);

        var state = factory.Services.GetRequiredService<TransactionFailureInjectionState>();
        state.Reset();
        state.FailOnCommit = true;

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.Equal(HttpStatusCode.InternalServerError, confirmResponse.StatusCode);

        var after = await CountRowsAsync(factory);
        // Commit never completed -- every row this attempt staged must be gone,
        // proving the real PostgreSQL transaction (not just EF's in-memory
        // change tracker) is what guarantees atomicity here.
        Assert.Equal(before, after);

        using (var scope = factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var preview = await ctx.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == Guid.Parse(previewId));
            Assert.Null(preview.ConfirmedPlanId);
        }

        state.Reset();
        var retryResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        var retryAfter = await CountRowsAsync(factory);
        Assert.Equal(before.plans + 1, retryAfter.plans);
        Assert.Equal(before.weeks + 18, retryAfter.weeks);
        Assert.Equal(before.days + 72, retryAfter.days);
    }

    // ── Part 18: transaction-seam governance ────────────────────────────────

    [Fact]
    public void FailureInjectionSeam_NotReferencedByProductionAssemblies()
    {
        var repo = TestPlanServicesFactory.RepoRoot();
        foreach (var relative in new[] { "backend/RunningApp.Api", "backend/RunningApp.Application", "backend/RunningApp.Persistence", "backend/RunningApp.Infrastructure", "backend/RunningApp.Domain" })
        {
            var root = System.IO.Path.Combine(repo, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
            var hits = System.IO.Directory.GetFiles(root, "*.cs", System.IO.SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}") && !p.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}"))
                .Where(p => System.IO.File.ReadAllText(p).Contains("TransactionFailureInjection", StringComparison.Ordinal));
            Assert.Empty(hits);
        }
    }

    [Fact]
    public async Task FailureInjectionSeam_DefaultsDisabled_NoEffectOnNormalConfirmation()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        // Deliberately do NOT touch TransactionFailureInjectionState -- proves
        // the freshly-constructed singleton state (FailWhenSqlContains=null,
        // FailOnCommit=false) has zero effect on a normal confirm.
        var previewId = await GeneratePreviewAsync(client, 15);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
    }
}
