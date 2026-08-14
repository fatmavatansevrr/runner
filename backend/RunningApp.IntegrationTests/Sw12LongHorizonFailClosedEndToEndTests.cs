using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Acceptance test SW-12 — fail-closed safety fix for long-horizon race
/// requests. Regression guard for the verified bug: a request with
/// StartDate=2026-05-25/RaceDate=2026-10-12 (~20 weeks) used to return HTTP
/// 200 with a 12-week plan silently anchored to StartDate and truncated ~8
/// weeks short of RaceDate (final session 2026-08-16, fallback_used=false).
/// Long-horizon preparation + race-core composition is not yet implemented;
/// until it is, any horizon above the approved standalone maximum (14 weeks)
/// must fail closed with a typed HTTP 422, never a truncated 200. Runs
/// against the real Api host + real Postgres DB, same as
/// <see cref="Sw02ProductAverageEndToEndTests"/>.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Sw12LongHorizonFailClosedEndToEndTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Sw12LongHorizonFailClosedEndToEndTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync()
    {
        var response = await _client.PostRawAsync("/api/v1/testing/reset");
        response.EnsureSuccessStatusCode();
    }

    private static object RaceRequest(string startDate, string raceDate, string goalDistance = "ten_k", string level = "intermediate", int daysPerWeek = 4) => new
    {
        goal_distance = goalDistance,
        level = level,
        days_per_week = daysPerWeek,
        unit = "km",
        start_date = startDate,
        preferred_days = new[] { "mon", "wed", "fri", "sun" },
        long_run_day = "sun",
        race_date = raceDate,
        // 3480 is the ten_k canonical product-average value; non-ten_k
        // identities must use user_defined here or the request 400s on
        // CanonicalTargetFinishTimePolicy mismatch before ever reaching the
        // horizon check this file exists to test.
        target_finish_time_seconds = 3480,
        target_finish_time_source = goalDistance == "ten_k" ? "product_average" : "user_defined",
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

    // ── C. Verified regression case ──────────────────────────────────────────

    [Fact]
    public async Task VerifiedRegressionCase_NonPilotIdentity_StartDate20260525_RaceDate20261012_Returns422_NoTruncatedTwelveWeekPlan()
    {
        // Backend Integration Phase 4G.6B: this exact 20-week span is now
        // ACTIVATED for the pilot identity (ten_k/intermediate/4d) via the
        // Preparation Runway preview route -- see
        // PreparationRunwayPreview15To20WeekEndToEndTests for that new,
        // successful behavior. Retargeted to a non-pilot identity
        // (five_k/beginner) to keep guarding the original, identity-agnostic
        // "no silent 12-week truncation" regression.
        await ResetAsync();
        var before = await CountRowsAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", RaceRequest("2026-05-25", "2026-10-12", goalDistance: "five_k", level: "beginner"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", error!["errorCode"]!.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(error["correlationId"]!.GetValue<string>()));

        // The old bug's exact signature — a 12-week preview silently
        // truncated to end 2026-08-16 — must be structurally impossible: no
        // preview body is returned at all on this path.
        Assert.Null(error["weeks"]);

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task VerifiedRegressionCase_NonPilotIdentity_NoLegacyFallback_NoPreviewOrPlanPersistence()
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        await _client.PostRawAsync("/api/v1/plans/generate-preview/race", RaceRequest("2026-05-25", "2026-10-12", goalDistance: "five_k", level: "beginner"));

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    // ── B. Unsupported long horizons ─────────────────────────────────────────
    // 15/16/20 (pilot identity) removed from this theory: Phase 4G.6B
    // activates them -- see PreparationRunwayPreview15To20WeekEndToEndTests.
    // 21 and 24 remain unsupported for every identity, pilot or not.

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    public async Task UnsupportedLongHorizon_Returns422_PlanHorizonCompositionRequired(int weeks)
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var startDate = new System.DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(weeks * 7);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", error!["errorCode"]!.GetValue<string>());

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(20)]
    public async Task UnsupportedLongHorizon_NonPilotIdentity_Returns422_PlanHorizonCompositionRequired(int weeks)
    {
        // The same 15-20 week range remains unsupported for any identity
        // OTHER than the exact activated pilot (ten_k/intermediate/4d).
        await ResetAsync();
        var before = await CountRowsAsync();

        var startDate = new System.DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(weeks * 7);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), goalDistance: "five_k", level: "beginner"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", error!["errorCode"]!.GetValue<string>());

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    // ── D. Existing 12-week case remains HTTP 200 ────────────────────────────

    [Fact]
    public async Task TwelveWeekRequest_StartDate20260720_RaceDate20261012_Returns200_FinalSessionAlignedToRaceDate()
    {
        await ResetAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", RaceRequest("2026-07-20", "2026-10-12"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        var weeks = preview["weeks"]!.AsArray();
        Assert.Equal(12, weeks.Count);

        var allDays = weeks.SelectMany(w => w!["days"]!.AsArray()).ToList();
        Assert.Equal(48, allDays.Count);

        var finalSessionDate = allDays
            .Select(d => System.DateOnly.Parse(d!["date"]!.GetValue<string>().Substring(0, 10)))
            .Max();
        Assert.Equal(new System.DateOnly(2026, 10, 11), finalSessionDate);

        Assert.False(preview["fallback_used"]!.GetValue<bool>());
    }

    // ── A. Nominal 8-14 week range, except exact 12: fails closed with the ──
    // ── distinct PLAN_CORE_HORIZON_UNSUPPORTED error (Phase 4G.2) ───────────
    // Superseded by Sw13ExactTwelveWeekOnlyEndToEndTests.InRangeButNotExactTwelve_ReturnsPlanCoreHorizonUnsupported,
    // kept here as a boundary sanity check specifically for 8 and 14.

    [Theory]
    [InlineData(8)]
    [InlineData(14)]
    public async Task InRangeBoundaryHorizon_Returns200_WithExactWeekCount(int weeks)
    {
        await ResetAsync();

        var startDate = new System.DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(weeks * 7);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
    }
}
