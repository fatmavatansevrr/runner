using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Persistence;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
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

    private static object RaceRequest(string startDate, string raceDate, string targetSource = "product_average", string goalDistance = "ten_k", string level = "intermediate") => new
    {
        goal_distance = goalDistance,
        level = level,
        days_per_week = 4,
        unit = "km",
        start_date = startDate,
        preferred_days = new[] { "mon", "wed", "fri", "sun" },
        long_run_day = "sun",
        race_date = raceDate,
        target_finish_time_seconds = 3480,
        target_finish_time_source = targetSource,
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

    private static IReadOnlyList<string> ExpectedPhaseKeys(int weeks)
    {
        var counts = weeks switch
        {
            8 => new[] { 2, 3, 2, 1 }, 9 => new[] { 2, 3, 3, 1 },
            10 => new[] { 2, 3, 4, 1 }, 11 => new[] { 2, 4, 4, 1 },
            12 => new[] { 3, 4, 4, 1 }, 13 => new[] { 4, 4, 4, 1 },
            14 => new[] { 4, 5, 4, 1 },
            _ => throw new ArgumentOutOfRangeException(nameof(weeks)),
        };
        var phases = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" };
        return phases.SelectMany((phase, i) => Enumerable.Repeat(phase, counts[i])).ToArray();
    }

    private async Task AssertPersistedPhaseAndSessionMatrixAsync(Guid planId, int weeks, DateOnly startDate, JsonArray publicWeeks)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var graph = await ctx.TrainingPlans.AsNoTracking()
            .Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);
        var persistedWeeks = graph.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        var expectedPhases = ExpectedPhaseKeys(weeks);
        var publicDaysByDate = publicWeeks
            .SelectMany(w => w!["days"]!.AsArray())
            .ToDictionary(d => DateOnly.Parse(d!["date"]!.GetValue<string>()[..10]));

        Assert.Equal(Enumerable.Range(1, weeks), persistedWeeks.Select(w => w.WeekNumber));
        Assert.Equal(expectedPhases, persistedWeeks.Select(w => w.CatalogPhaseKey));
        Assert.Equal("TAPER", persistedWeeks[^1].CatalogPhaseKey);
        Assert.Single(persistedWeeks, w => w.CatalogPhaseKey == "TAPER");

        foreach (var phaseGroup in persistedWeeks.GroupBy(w => w.CatalogPhaseKey))
        {
            var phaseWeeks = phaseGroup.OrderBy(w => w.WeekNumber).ToArray();
            Assert.Equal(Enumerable.Range(1, phaseWeeks.Length), phaseWeeks.Select((_, i) => i + 1));
            Assert.All(phaseWeeks, _ => Assert.Equal(phaseWeeks.Length, expectedPhases.Count(p => p == phaseGroup.Key)));
        }

        foreach (var week in persistedWeeks)
        {
            var days = week.Days.OrderBy(d => d.Date).ToArray();
            Assert.Equal(4, days.Length);
            Assert.Equal(4, days.Select(d => d.Id).Distinct().Count());
            Assert.Equal(4, days.Select(d => d.Date.Date).Distinct().Count());
            Assert.Equal(1, days.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
            Assert.Equal(2, days.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
            Assert.Equal(1, days.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
            Assert.All(days, d => Assert.Equal(week.Id, d.WeekId));
            var windowStart = startDate.AddDays((week.WeekNumber - 1) * 7);
            Assert.All(days, d => Assert.InRange(DateOnly.FromDateTime(d.Date), windowStart, windowStart.AddDays(6)));
            Assert.All(days, d =>
            {
                var publicDay = publicDaysByDate[DateOnly.FromDateTime(d.Date)];
                Assert.Equal(publicDay!["distance_km"]!.GetValue<double>(), d.PlannedDistanceKm, 6);
                Assert.Equal(publicDay["duration_min"]!.GetValue<int>(), d.PlannedDurationMin);
                Assert.Equal(publicDay["intensity"]?.GetValue<string>(), d.Intensity);
            });
        }
    }

    // ── A. Verified 8-week overshoot regression case ─────────────────────────

    [Fact]
    public async Task VerifiedEightWeekRegressionCase_ReturnsEightWeeks_NotOldTwelveWeekSchedule()
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", RaceRequest("2026-07-20", "2026-09-14"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(8, preview["weeks"]!.AsArray().Count);
    }

    [Fact]
    public async Task VerifiedEightWeekRegressionCase_NoLegacyFallback_PersistsPreviewOnly()
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        await _client.PostRawAsync("/api/v1/plans/generate-preview/race", RaceRequest("2026-07-20", "2026-09-14"));

        var after = await CountRowsAsync();
        Assert.Equal(before.previews + 1, after.previews);
        Assert.Equal(before.plans, after.plans);
        Assert.Equal(before.weeks, after.weeks);
        Assert.Equal(before.days, after.days);
    }

    // ── B-D, F-G. 9, 10, 11, 13, 14 weeks: same typed rejection ──────────────

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(12)]
    public async Task ActivatedStandaloneCoreHorizon_CompletesPublicPersistenceAndReadModels(int weeks)
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var startDate = new System.DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(weeks * 7);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        Assert.Equal(weeks * 4, preview["weeks"]!.AsArray().Sum(week => week!["days"]!.AsArray().Count));
        Assert.False(preview["fallback_used"]!.GetValue<bool>());
        var expectedPublicWeekTypes = ExpectedPhaseKeys(weeks).Select(p => p switch
        {
            "FOUNDATION" => "base", "BUILD" => "build", "RACE_SPECIFIC" => "peak", "TAPER" => "taper",
            _ => throw new InvalidOperationException(),
        });
        Assert.Equal(expectedPublicWeekTypes, preview["weeks"]!.AsArray().Select(w => w!["week_type"]!.GetValue<string>()));

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new
        {
            preview_id = preview["preview_id"]!.GetValue<string>()
        });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());
        Assert.NotEqual(Guid.Empty, planId);

        var after = await CountRowsAsync();
        Assert.Equal(before.previews + 1, after.previews);
        Assert.Equal(before.plans + 1, after.plans);
        Assert.Equal(before.weeks + weeks, after.weeks);
        Assert.Equal(before.days + (weeks * 4), after.days);
        await AssertPersistedPhaseAndSessionMatrixAsync(planId, weeks, startDate, preview["weeks"]!.AsArray());

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Equal(weeks, details["weeks"]!.AsArray().Count);

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.NotNull(home["active_plan"]);
        Assert.Equal(7, home["week_summary"]!.AsArray().Count);

        var month = startDate.ToString("yyyy-MM");
        var calendar = await _client.GetAsync($"/api/v1/plans/active/calendar?month={month}");
        calendar.EnsureSuccessStatusCode();
        Assert.NotEmpty((await calendar.Content.ReadFromJsonAsync<JsonNode>())!.AsArray());

        await ResetAsync();
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
    public async Task PartialDayHorizon_UsesFullWeeksWithoutRoundingUp()
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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(11, preview["weeks"]!.AsArray().Count);
    }

    [Theory]
    [InlineData(55, 7, 6, "Unsupported", 422, "CATALOG_LIVE_PILOT_REQUEST_UNSUPPORTED")]
    [InlineData(56, 8, 0, "CompressedCore", 200, null)]
    [InlineData(57, 8, 1, "CompressedCore", 200, null)]
    [InlineData(62, 8, 6, "CompressedCore", 200, null)]
    [InlineData(83, 11, 6, "CompressedCore", 200, null)]
    [InlineData(84, 12, 0, "PreferredCore", 200, null)]
    [InlineData(85, 12, 1, "ExtendedCore", 200, null)]
    [InlineData(90, 12, 6, "ExtendedCore", 200, null)]
    [InlineData(91, 13, 0, "ExtendedCore", 200, null)]
    [InlineData(92, 13, 1, "ExtendedCore", 200, null)]
    [InlineData(97, 13, 6, "ExtendedCore", 200, null)]
    [InlineData(98, 14, 0, "ExtendedCore", 200, null)]
    [InlineData(99, 14, 1, "PreparationRunwayPlusCore", 422, "PLAN_HORIZON_COMPOSITION_REQUIRED")]
    [InlineData(104, 14, 6, "PreparationRunwayPlusCore", 422, "PLAN_HORIZON_COMPOSITION_REQUIRED")]
    // 105 (15w0d) and 106 (15w1d) removed: Phase 4G.6B activates 15-20 full
    // weeks for this exact pilot identity via the Preparation Runway preview
    // route (200, not 422) -- see PreparationRunwayPreview15To20WeekEndToEndTests.
    public async Task PublicPartialDayMatrix_UsesCanonicalFullWeeksAndRemainder(
        int elapsedDays, int fullWeeks, int remainingDays, string expectedMode,
        int expectedStatus, string? expectedErrorCode)
    {
        await ResetAsync();
        var before = await CountRowsAsync();
        var startDate = new System.DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(elapsedDays);
        Assert.Equal(elapsedDays, raceDate.DayNumber - startDate.DayNumber); // exclusive elapsed-day semantics

        var decision = RaceHorizonPolicy.Decide(startDate, raceDate);
        Assert.Equal(fullWeeks, decision.AvailableFullWeeks);
        Assert.Equal(remainingDays, decision.LeadingPartialDays);
        Assert.Equal(expectedMode, decision.Mode.ToString());

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        Assert.Equal(expectedStatus, (int)response.StatusCode);

        var after = await CountRowsAsync();
        if (expectedStatus == 200)
        {
            var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
            Assert.Equal(fullWeeks, preview["weeks"]!.AsArray().Count);
            Assert.Equal(fullWeeks * 4, preview["weeks"]!.AsArray().Sum(w => w!["days"]!.AsArray().Count));
            Assert.Equal(before.previews + 1, after.previews); // catalog generation reached the preview boundary
            Assert.Equal(before.plans, after.plans);
            Assert.Equal(before.weeks, after.weeks);
            Assert.Equal(before.days, after.days);
        }
        else
        {
            var error = await response.Content.ReadFromJsonAsync<JsonNode>();
            Assert.Equal(expectedErrorCode, error!["errorCode"]!.GetValue<string>());
            Assert.Equal(before, after);
        }
    }

    // ── H. 21+ weeks: existing PLAN_HORIZON_COMPOSITION_REQUIRED unchanged ──
    // (15-20 weeks for this exact pilot identity is now activated -- see
    // PreparationRunwayPreview15To20WeekEndToEndTests.)

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    public async Task TwentyOnePlusWeeks_StillReturnsPlanHorizonCompositionRequired_NotCoreHorizonUnsupported(int weeks)
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
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task NotEvaluatedPaceSource_FailsClosedBeforeScheduling_ForEveryActivatedHorizon(int weeks)
    {
        await ResetAsync();
        var before = await CountRowsAsync();
        var startDate = new System.DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(weeks * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), "user_defined"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("RUNTIME_CONDITION_UNSUPPORTED", error!["errorCode"]!.GetValue<string>());
        Assert.Contains("PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE", error["message"]!.GetValue<string>());
        Assert.Equal(before, await CountRowsAsync());
    }
}
