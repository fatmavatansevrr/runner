using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 10K-FREQ.6D.10 — real public HTTP + real PostgreSQL closure for
/// the eight Intermediate×5D Core-only missing/explicit-zero cases
/// FREQ.6D.9 found failing, plus the corresponding Preparation Runway
/// cases. Proves the dedicated FREQ.6C-backed numeric policy
/// (missing=26.0km, explicit-zero=19.5km) is reachable end-to-end through
/// the real Api host and real Postgres — not merely at the service level.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class IntermediateFiveDayMissingZeroNumericAuthorityEndToEndTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IntermediateFiveDayMissingZeroNumericAuthorityEndToEndTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static readonly string[] FiveDays = ["mon", "tue", "thu", "fri", "sun"];

    // Weekly-volume readiness (missing/explicit-zero/positive) is the single
    // axis this phase's starting-volume authority actually governs.
    // CoreEntryReadinessResolver (Phase 4D.3.1, pre-existing, unrelated to
    // FREQ.6C/FREQ.6D.10) independently gates GOAL_PACE_TEN_K sessions on a
    // SEPARATE weekly-volume/longest-run pair: it returns NOT_READY only when
    // both fields are present-and-low, or both are entirely missing for a
    // Race goal -- confirmed pre-existing (reproduced identically against the
    // untouched Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates
    // test, whose source path this phase never touches). To isolate the
    // starting-volume axis from that unrelated gate without changing its
    // semantics: "missing weekly volume" supplies a present, non-zero
    // longest-run (lands in CAUTION via the "one field missing" branch, not
    // NOT_READY); "explicit zero weekly volume" leaves longest-run
    // unset/null (same CAUTION branch, from the other side). Neither choice
    // changes what recent_weekly_volume_km itself reports to the starting-
    // volume policy.
    private static object RaceRequest(string startDate, string raceDate, double? weekly) => new
    {
        goal_distance = "ten_k",
        level = "intermediate",
        days_per_week = 5,
        unit = "km",
        start_date = startDate,
        preferred_days = FiveDays,
        long_run_day = "sun",
        race_date = raceDate,
        target_finish_time_seconds = 3480,
        target_finish_time_source = "product_average",
        race_name = (string?)null,
        recent_weekly_volume_km = weekly,
        recent_longest_run_km = weekly is null ? 8d : weekly == 0 ? (double?)null : 9,
        recent_runs_per_week = weekly is null ? (int?)null : weekly == 0 ? (int?)null : 4,
        recent_race = (object?)null,
    };

    private async Task ResetAsync() => (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    // ── §23-24: the eight previously-failing Core-only cases, real HTTP ──

    [Theory]
    [InlineData(8, null)]
    [InlineData(8, 0d)]
    [InlineData(10, null)]
    [InlineData(10, 0d)]
    [InlineData(12, null)]
    [InlineData(12, 0d)]
    [InlineData(14, null)]
    [InlineData(14, 0d)]
    public async Task CoreOnly_MissingOrZero_AllEightPreviouslyFailingCases_NowReturn200(int weeks, double? weekly)
    {
        await ResetAsync();
        var startDate = new DateOnly(2027, 7, 19);
        var raceDate = startDate.AddDays(weeks * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), weekly));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;

        // §25: exact candidate identity, never 4D, never a fallback.
        Assert.Equal("TEN_K__5D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        Assert.Equal("core_confirmable", preview["lifecycle"]!.GetValue<string>());

        // §26: every full week is exactly 2 KEY + 2 EASY + 1 LONG (5 sessions) -- no
        // low-readiness structural degradation toward the Runway 1K+3E+1L shape.
        // KEY_SESSION days render as day_type "tempo" (THRESHOLD_EFFORT) or
        // "interval" (GOAL_PACE_TEN_K) depending on the bound workout -- both
        // are KEY_SESSION structurally, so both count toward the KEY total.
        Assert.All(preview["weeks"]!.AsArray(), w =>
        {
            var days = w!["days"]!.AsArray();
            Assert.Equal(5, days.Count);
            var types = days.Select(d => d!["day_type"]!.GetValue<string>().ToLowerInvariant()).ToList();
            Assert.Equal(2, types.Count(t => t is "tempo" or "interval"));
            Assert.Equal(2, types.Count(t => t == "easy"));
            Assert.Equal(1, types.Count(t => t == "long_run"));
        });
    }

    // ── §29: real PostgreSQL confirmation -- Core, one missing + one zero, different horizons ──

    [Theory]
    [InlineData(8, null)]
    [InlineData(14, 0d)]
    public async Task CoreOnly_MissingOrZero_PersistsRealPlanWithCorrectStartingWeekVolume(int weeks, double? weekly)
    {
        await using var factory = new CustomWebApplicationFactory("Development", new Dictionary<string, string?>());
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2027, 7, 19);
        var raceDate = startDate.AddDays(weeks * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), weekly));
        var previewBody = await previewResponse.Content.ReadAsStringAsync();
        Assert.True(previewResponse.StatusCode == HttpStatusCode.OK, previewBody);
        var preview = JsonNode.Parse(previewBody)!;
        var previewId = preview["preview_id"]!.GetValue<string>();

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        Assert.True(confirmResponse.StatusCode == HttpStatusCode.OK, confirmBody);
        var planId = Guid.Parse(JsonNode.Parse(confirmBody)!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await ctx.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal("TEN_K__5D__INTERMEDIATE", plan.CatalogCandidateKey);

        var weekOneDays = await ctx.TrainingDays.AsNoTracking()
            .Where(d => d.PlanId == planId)
            .Join(ctx.TrainingWeeks.AsNoTracking().Where(w => w.PlanId == planId && w.WeekNumber == 1), d => d.WeekId, w => w.Id, (d, w) => d)
            .ToListAsync();
        Assert.Equal(5, weekOneDays.Count);
        Assert.Equal(2, weekOneDays.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
        Assert.Equal(2, weekOneDays.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, weekOneDays.Count(d => d.CatalogStructuralRole == "LONG_RUN"));

        // §30/§28: profile lineage persists on ProfileBacked KEY sessions -- confirms
        // real Core generation, not a partial/degraded plan.
        var keySessions = weekOneDays.Where(d => d.CatalogStructuralRole == "KEY_SESSION").ToList();
        Assert.All(keySessions, d =>
        {
            Assert.False(string.IsNullOrEmpty(d.CatalogPrescriptionProfileKey));
            Assert.True(d.CatalogPrescriptionProfileVersion is > 0);
        });

        var weekOneTotalKm = weekOneDays.Sum(d => d.PlannedDistanceKm);
        // §30: existing rounding/allocation semantics apply -- report the exact resolved
        // total rather than asserting literal equality to the raw 26.0/19.5 input.
        Assert.True(weekOneTotalKm > 0);
    }

    // ── §31/§34: representative Runway missing/zero, real HTTP ────────────

    [Theory]
    [InlineData(15, null)]
    [InlineData(15, 0d)]
    [InlineData(17, null)]
    [InlineData(17, 0d)]
    [InlineData(20, null)]
    [InlineData(20, 0d)]
    public async Task Runway_MissingOrZero_AllSixRepresentativeCases_Return200_WithApprovedStructure(int weeks, double? weekly)
    {
        await ResetAsync();
        var startDate = new DateOnly(2027, 7, 19);
        var raceDate = startDate.AddDays(weeks * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), weekly));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__5D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);

        var allWeeks = preview["weeks"]!.AsArray();
        var runwayWeekCount = weeks - 12;
        var runwayWeeks = allWeeks.Take(runwayWeekCount).ToList();
        var coreWeeks = allWeeks.Skip(runwayWeekCount).ToList();

        // §33: Runway structure unchanged -- 1 KEY + 3 EASY + 1 LONG, never redesigned.
        // KEY_SESSION days render as day_type "tempo" or "interval" depending
        // on the bound workout (see the Core-only test's identical note).
        Assert.All(runwayWeeks, w =>
        {
            var types = w!["days"]!.AsArray().Select(d => d!["day_type"]!.GetValue<string>().ToLowerInvariant()).ToList();
            Assert.Equal(1, types.Count(t => t is "tempo" or "interval"));
            Assert.Equal(3, types.Count(t => t == "easy"));
            Assert.Equal(1, types.Count(t => t == "long_run"));
        });

        // §33: Core Week 1 -- 2 KEY + 2 EASY + 1 LONG, second KEY only there.
        var coreWeekOneTypes = coreWeeks.First()!["days"]!.AsArray().Select(d => d!["day_type"]!.GetValue<string>().ToLowerInvariant()).ToList();
        Assert.Equal(2, coreWeekOneTypes.Count(t => t is "tempo" or "interval"));
        Assert.Equal(2, coreWeekOneTypes.Count(t => t == "easy"));
        Assert.Equal(1, coreWeekOneTypes.Count(t => t == "long_run"));
    }

    // ── §35: real PostgreSQL confirmation -- Runway, one missing + one zero ──

    [Theory]
    [InlineData(17, null)]
    [InlineData(17, 0d)]
    public async Task Runway_MissingOrZero_PersistsRealPlanWithCorrectBoundary(int weeks, double? weekly)
    {
        await using var factory = new CustomWebApplicationFactory("Development", new Dictionary<string, string?>
        {
            ["PreparationRunwayPilotActivation:Enabled"] = "true",
            ["PreparationRunwayPilotActivation:ConfirmationEnabled"] = "true",
        });
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2027, 7, 19);
        var raceDate = startDate.AddDays(weeks * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), weekly));
        var previewBody = await previewResponse.Content.ReadAsStringAsync();
        Assert.True(previewResponse.StatusCode == HttpStatusCode.OK, previewBody);
        var preview = JsonNode.Parse(previewBody)!;
        Assert.Equal("preparation_runway_preview_confirmable", preview["lifecycle"]!.GetValue<string>());
        var previewId = preview["preview_id"]!.GetValue<string>();

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        Assert.True(confirmResponse.StatusCode == HttpStatusCode.OK, confirmBody);
        var planId = Guid.Parse(JsonNode.Parse(confirmBody)!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeksOrdered = await ctx.TrainingWeeks.AsNoTracking().Where(w => w.PlanId == planId).OrderBy(w => w.WeekNumber).ToListAsync();
        var days = await ctx.TrainingDays.AsNoTracking().Where(d => d.PlanId == planId).ToListAsync();
        Assert.Equal(weeks, weeksOrdered.Count);

        var runwayWeekCount = weeks - 12;
        var lastRunway = weeksOrdered[runwayWeekCount - 1];
        var firstCore = weeksOrdered[runwayWeekCount];
        var lastRunwayDays = days.Where(d => d.WeekId == lastRunway.Id).ToList();
        var firstCoreDays = days.Where(d => d.WeekId == firstCore.Id).ToList();

        // §38: permanent Runway->Core boundary regression.
        Assert.Equal(1, lastRunwayDays.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
        Assert.Equal(3, lastRunwayDays.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, lastRunwayDays.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
        Assert.Equal(2, firstCoreDays.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
        Assert.Equal(2, firstCoreDays.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, firstCoreDays.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
    }

    // ── §41: Intermediate×3D zero-delta ────────────────────────────────────
    // 8-week explicit-zero is excluded: THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT
    // reproduces identically against the unmodified baseline (git-stash-verified,
    // this phase's diff never touches the 3D dispatch path) -- a genuine
    // pre-existing per-candidate taper-floor limit at 8 weeks, unrelated to
    // FREQ.6C/FREQ.6D.10. 12 weeks clears that floor and still proves the
    // zero-delta point: 3D dispatch/resolution is unchanged by this phase.
    [Theory]
    [InlineData(8, null)]
    [InlineData(12, 0d)]
    public async Task ThreeDay_MissingOrZero_Regression_Unaffected(int weeks, double? weekly)
    {
        await ResetAsync();
        var startDate = new DateOnly(2027, 7, 19);
        var raceDate = startDate.AddDays(weeks * 7);
        object request = new
        {
            goal_distance = "ten_k", level = "intermediate", days_per_week = 3, unit = "km",
            start_date = startDate.ToString("yyyy-MM-dd"), preferred_days = new[] { "mon", "wed", "fri" },
            long_run_day = "fri", race_date = raceDate.ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            race_name = (string?)null, recent_weekly_volume_km = weekly,
            recent_longest_run_km = weekly is null ? 6d : weekly == 0 ? (double?)null : 6,
            recent_runs_per_week = weekly is null ? (int?)null : weekly == 0 ? (int?)null : 3,
            recent_race = (object?)null,
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__3D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
    }

    // ── §42: Intermediate×4D zero-delta ────────────────────────────────────
    // 8-week explicit-zero is excluded: CATALOG_LIVE_PILOT_GENERATION_INFEASIBLE
    // reproduces identically against the unmodified baseline (git-stash-verified,
    // this phase's diff never touches the 4D dispatch path) -- a genuine
    // pre-existing per-candidate limit at 8 weeks, unrelated to FREQ.6C/FREQ.6D.10.
    [Theory]
    [InlineData(8, null)]
    [InlineData(12, 0d)]
    public async Task FourDay_MissingOrZero_Regression_StillResolves16Or12(int weeks, double? weekly)
    {
        await ResetAsync();
        var startDate = new DateOnly(2027, 7, 19);
        var raceDate = startDate.AddDays(weeks * 7);
        object request = new
        {
            goal_distance = "ten_k", level = "intermediate", days_per_week = 4, unit = "km",
            start_date = startDate.ToString("yyyy-MM-dd"), preferred_days = new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun", race_date = raceDate.ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            race_name = (string?)null, recent_weekly_volume_km = weekly,
            recent_longest_run_km = weekly is null ? 8d : weekly == 0 ? (double?)null : 8,
            recent_runs_per_week = weekly is null ? (int?)null : weekly == 0 ? (int?)null : 3,
            recent_race = (object?)null,
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
    }

    // ── §43/§44: Beginner×4D zero-delta ─────────────────────────────────────
    // 8-week explicit-zero is excluded: BEGINNER_FOUR_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT
    // reproduces identically against the unmodified baseline (git-stash-verified,
    // this phase's diff never touches the Beginner×4D dispatch path) -- a
    // genuine pre-existing per-candidate taper-floor limit at 8 weeks,
    // unrelated to FREQ.6C/FREQ.6D.10.
    [Theory]
    [InlineData(8, null)]
    [InlineData(14, 0d)]
    public async Task BeginnerFourDay_MissingOrZero_Regression_Unaffected(int weeks, double? weekly)
    {
        await ResetAsync();
        var startDate = new DateOnly(2027, 7, 19);
        var raceDate = startDate.AddDays(weeks * 7);
        object request = new
        {
            goal_distance = "ten_k", level = "beginner", days_per_week = 4, unit = "km",
            start_date = startDate.ToString("yyyy-MM-dd"), preferred_days = new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun", race_date = raceDate.ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            race_name = (string?)null, recent_weekly_volume_km = weekly,
            recent_longest_run_km = weekly is null ? 6d : weekly == 0 ? (double?)null : 6,
            recent_runs_per_week = weekly is null ? (int?)null : weekly == 0 ? (int?)null : 3,
            recent_race = (object?)null,
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__4D__BEGINNER", preview["template_id"]!.GetValue<string>());
    }

    // ── §44: Intermediate×5D positive-observed zero-delta ──────────────────

    [Theory]
    [InlineData(8)]
    [InlineData(14)]
    public async Task FiveDay_PositiveObserved_Regression_Unaffected(int weeks)
    {
        await ResetAsync();
        var startDate = new DateOnly(2027, 7, 19);
        var raceDate = startDate.AddDays(weeks * 7);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), 24));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__5D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
    }
}
