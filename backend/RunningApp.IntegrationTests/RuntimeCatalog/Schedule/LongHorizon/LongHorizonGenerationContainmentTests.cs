using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.3 — proves that introducing the new, dark, unwired
/// <c>LongHorizonCompositionResolver</c> changes zero live HTTP behavior.
/// <c>PlanServices</c>/<c>CatalogPreviewGenerator</c> continue to call only
/// <c>RaceHorizonPolicy</c>/<c>CoreHorizonClassifier</c> exactly as before
/// this phase; nothing in the live request path references the new
/// resolver. <see cref="Sw12LongHorizonFailClosedEndToEndTests"/> already
/// covers 21/24 weeks for the pre-4I.3 baseline; this file extends that
/// exact same real Api host + real Postgres pattern to also cover 52 weeks
/// (the new resolver's own upper bound) and 53 weeks (the outer rejection
/// boundary), confirming both remain identically 422
/// <c>PLAN_HORIZON_COMPOSITION_REQUIRED</c> with zero persistence — the
/// same reason code as every other 15+ week request, not a new one. 21-52
/// is composition-ELIGIBLE per the dark resolver, but public generation
/// remains completely inactive; this test is the live proof of that.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonGenerationContainmentTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonGenerationContainmentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync()
    {
        var response = await _client.PostRawAsync("/api/v1/testing/reset");
        response.EnsureSuccessStatusCode();
    }

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

    private async Task<(int previews, int plans, int weeks, int days)> CountRowsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (
            await ctx.PlanPreviews.CountAsync(),
            await ctx.TrainingPlans.CountAsync(),
            await ctx.TrainingWeeks.CountAsync(),
            await ctx.TrainingDays.CountAsync());
    }

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    [InlineData(52)]
    public async Task CompositionEligibleLongHorizon_StillReturns422_NoPreviewMaterialized_NoPersistence(int weeks)
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var startDate = new System.DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(weeks * 7);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        // Even for the exact pilot identity (ten_k/intermediate/4d), 21-52
        // weeks is composition-eligible per the new dark resolver but
        // generation is NOT activated -- the same public 422 as before
        // this phase, never the pilot's HTTP 200 preview path (that
        // remains scoped to 15-20 weeks only).
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", error!["errorCode"]!.GetValue<string>());
        Assert.Null(error["weeks"]); // no preview schedule materialized

        var after = await CountRowsAsync();
        Assert.Equal(before, after); // no TrainingPlan/TrainingWeek/TrainingDay/PlanPreview persistence
    }

    [Fact]
    public async Task AboveSupportedWindow_53Weeks_Returns422_SameReasonAsEveryOther15PlusRequest()
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var startDate = new System.DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(53 * 7);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        // Public HTTP behavior for 53+ is unchanged by this phase: the live
        // gate does not yet distinguish 53+ from any other 15+ horizon, so
        // the observed public reason remains PLAN_HORIZON_COMPOSITION_REQUIRED
        // today -- PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW exists only inside
        // the new dark resolver's own decision (see
        // LongHorizonCompositionResolverTests.AboveFiftyTwo_IsUnsupported),
        // not yet wired to any public-facing error mapping.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", error!["errorCode"]!.GetValue<string>());

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }
}
