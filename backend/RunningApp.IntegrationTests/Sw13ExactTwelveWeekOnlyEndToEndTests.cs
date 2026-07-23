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
/// Acceptance test SW-13 — Phase 4G.2 exact-12-week-only temporary safety
/// policy. Regression guard for two verified bugs: an 8-week horizon request
/// (StartDate=2026-07-20/RaceDate=2026-09-14) that returned HTTP 200 with a
/// fixed 12-week schedule overshooting the race by ~4 weeks (final session
/// 2026-10-11 vs. requested race date 2026-09-14), and the earlier-verified
/// 20-week undershoot (see Sw12LongHorizonFailClosedEndToEndTests). Proves
/// the catalog phase allocator's fixed ~12-week output is only ever exposed
/// through the one horizon it actually matches. Runs against the real Api
/// host + real Postgres DB, same as <see cref="Sw02ProductAverageEndToEndTests"/>.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Sw13ExactTwelveWeekOnlyEndToEndTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Sw13ExactTwelveWeekOnlyEndToEndTests(CustomWebApplicationFactory factory)
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

    // ── A. Verified 8-week overshoot regression case ─────────────────────────

    [Fact]
    public async Task VerifiedEightWeekRegressionCase_StartDate20260720_RaceDate20260914_Returns422_NoOldTwelveWeekSchedule()
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", RaceRequest("2026-07-20", "2026-09-14"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_CORE_HORIZON_UNSUPPORTED", error!["errorCode"]!.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(error["correlationId"]!.GetValue<string>()));

        // Old bug's exact signature must be structurally impossible: no
        // preview body, no session dated 2026-10-11 (the old fixed 12-week
        // schedule's final session), at all.
        Assert.Null(error["weeks"]);

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task VerifiedEightWeekRegressionCase_NoLegacyFallback_NoPersistence()
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        await _client.PostRawAsync("/api/v1/plans/generate-preview/race", RaceRequest("2026-07-20", "2026-09-14"));

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    // ── B-D, F-G. 9, 10, 11, 13, 14 weeks: same typed rejection ──────────────

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task InRangeButNotExactTwelve_ReturnsPlanCoreHorizonUnsupported_NoPersistence(int weeks)
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
        Assert.Equal("PLAN_CORE_HORIZON_UNSUPPORTED", error!["errorCode"]!.GetValue<string>());

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    // ── E. Exact 12 weeks remains fully supported ────────────────────────────

    [Fact]
    public async Task ExactTwelveWeekRequest_Returns200_FullyAlignedToRaceDate()
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

    // ── I. Below-minimum horizon: separate, pre-existing behavior, never ────
    // ── mapped to PLAN_CORE_HORIZON_UNSUPPORTED ──────────────────────────────

    [Fact]
    public async Task BelowMinimumHorizon_SevenWeeks_IsNotMappedToPlanCoreHorizonUnsupported()
    {
        await ResetAsync();

        var startDate = new System.DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(7 * 7);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        // Whatever the pre-existing below-minimum behavior is (this task
        // does not change it), it must NOT be PLAN_CORE_HORIZON_UNSUPPORTED
        // — that error code is reserved for the nominal 8-14 range.
        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            var error = await response.Content.ReadFromJsonAsync<JsonNode>();
            Assert.NotEqual("PLAN_CORE_HORIZON_UNSUPPORTED", error!["errorCode"]!.GetValue<string>());
        }
    }

    // ── J. Defensive race-date alignment invariant ───────────────────────────
    // A real, naturally-producible edge case (no internal mocking needed):
    // a horizon that rounds UP to the "exact 12 weeks" classification
    // (RaceHorizonPolicy.CalculateAvailableWeeks ceilings) but whose raw
    // day-gap is short of the fixed candidate's actual 84-day (12*7)
    // allocation. The catalog phase allocator still emits its fixed 84-day
    // schedule regardless, which would end AFTER this shorter RaceDate —
    // exactly the invariant CatalogRaceDateAlignmentInvalidException exists
    // to catch as a backstop, independent of the upstream horizon guard.

    [Fact]
    public async Task ShortfallWithinExactTwelveClassification_TriggersDefensiveAlignmentInvariant_NoPersistence()
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var startDate = new System.DateOnly(2026, 7, 20);
        // 78 days: ceil(78/7) = 12 (classifies as ExactStandaloneCoreSupported),
        // but the fixed candidate always builds exactly 84 days (12*7) —
        // 6 days longer than this RaceDate, so the generated schedule would
        // end 6 days after the race.
        var raceDate = startDate.AddDays(78);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("CATALOG_RACE_DATE_ALIGNMENT_INVALID", error!["errorCode"]!.GetValue<string>());

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    // ── H. 15+ weeks: existing PLAN_HORIZON_COMPOSITION_REQUIRED unchanged ──

    [Theory]
    [InlineData(15)]
    [InlineData(20)]
    public async Task FifteenPlusWeeks_StillReturnsPlanHorizonCompositionRequired_NotCoreHorizonUnsupported(int weeks)
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
}
