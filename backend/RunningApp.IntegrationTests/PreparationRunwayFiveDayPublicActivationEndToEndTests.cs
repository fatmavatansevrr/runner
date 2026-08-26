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
/// Phase 10K-FREQ.6D.8 — real public HTTP + real PostgreSQL confirmation for
/// the Intermediate×5D Preparation Runway (15-20 weeks), closing the gap
/// FREQ.6D.7 explicitly disclosed as not yet performed. Mirrors the exact
/// established Intermediate×4D Runway harness
/// (<see cref="PreparationRunwayPreview15To20WeekEndToEndTests"/>/
/// <see cref="PreparationRunwayConfirmationEndToEndTests"/>) with
/// DaysPerWeek=5 and the FREQ.6D.6-approved 1 KEY + 3 EASY + 1 LONG Runway /
/// 2 KEY + 2 EASY + 1 LONG Core shape, against the real Api host and real
/// Postgres. No production code is touched by this file.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PreparationRunwayFiveDayPublicActivationEndToEndTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PreparationRunwayFiveDayPublicActivationEndToEndTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static readonly string[] FiveDays = ["mon", "tue", "thu", "fri", "sun"];

    private static object RaceRequest(
        string startDate, string raceDate,
        string goalDistance = "ten_k", string level = "intermediate", int daysPerWeek = 5,
        double? recentWeeklyVolumeKm = 24, double? recentLongestRunKm = 9, int? recentRunsPerWeek = 4,
        string[]? preferredDays = null, string longRunDay = "sun",
        int targetFinishTimeSeconds = 3480, string targetFinishTimeSource = "product_average",
        object? recentRace = null) => new
    {
        goal_distance = goalDistance,
        level = level,
        days_per_week = daysPerWeek,
        unit = "km",
        start_date = startDate,
        preferred_days = preferredDays ?? FiveDays,
        long_run_day = longRunDay,
        race_date = raceDate,
        target_finish_time_seconds = targetFinishTimeSeconds,
        target_finish_time_source = targetFinishTimeSource,
        race_name = (string?)null,
        recent_weekly_volume_km = recentWeeklyVolumeKm,
        recent_longest_run_km = recentLongestRunKm,
        recent_runs_per_week = recentRunsPerWeek,
        recent_race = recentRace,
    };

    private async Task ResetAsync() => (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

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

    private static void AssertRunwayWeekShape(JsonNode week)
    {
        var days = week["days"]!.AsArray();
        Assert.Equal(5, days.Count);
        var dayTypes = days.Select(d => d!["day_type"]!.GetValue<string>().ToLowerInvariant()).ToList();
        Assert.Equal(1, dayTypes.Count(t => t == "tempo"));
        Assert.Equal(3, dayTypes.Count(t => t == "easy"));
        Assert.Equal(1, dayTypes.Count(t => t == "long_run"));
    }

    // ── §6-11: public 15-20 week HTTP E2E, exact identity + exact structure ──

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    public async Task FiveDayPilotScope_FifteenToTwentyWeeks_Returns200_ExactCandidateAndExactStructure(int totalWeeks)
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;

        // §19: exact candidate identity, never the 4D candidate, never a fallback.
        Assert.Equal("TEN_K__5D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(5, preview["days_per_week"]!.GetValue<int>());

        var weeks = preview["weeks"]!.AsArray();
        Assert.Equal(totalWeeks, weeks.Count);
        Assert.Equal(Enumerable.Range(1, totalWeeks), weeks.Select(w => w!["week_number"]!.GetValue<int>()));

        var allDays = weeks.SelectMany(w => w!["days"]!.AsArray()).ToList();
        // §11.H: every full week is exactly 5 sessions (1K+3E+1L Runway, 2K+2E+1L Core).
        Assert.Equal(totalWeeks * 5, allDays.Count);

        var runwayWeekCount = totalWeeks - 12;
        var runwayWeeks = weeks.Take(runwayWeekCount).ToList();
        var coreWeeks = weeks.Skip(runwayWeekCount).ToList();
        Assert.All(runwayWeeks, w => Assert.False(string.IsNullOrEmpty(w!["runway_block"]?.GetValue<string>())));
        Assert.All(coreWeeks, w => Assert.True(w!["runway_block"] is null || w["runway_block"]!.GetValue<string?>() is null));
        Assert.Equal("PRE_SPECIFIC_TRANSITION", runwayWeeks.Last()!["runway_block"]!.GetValue<string>());
        Assert.NotEqual("preparation_runway", coreWeeks.First()!["week_type"]!.GetValue<string>().ToLowerInvariant());

        // §13: every full Runway week is exactly 1 KEY (tempo) + 3 EASY + 1 LONG.
        Assert.All(runwayWeeks, AssertRunwayWeekShape);

        // §14: Core Week 1 (first Core week) is exactly 2 KEY + 2 EASY + 1 LONG.
        var coreWeekOneDays = coreWeeks.First()!["days"]!.AsArray();
        Assert.Equal(5, coreWeekOneDays.Count);
        var coreWeekOneTypes = coreWeekOneDays.Select(d => d!["day_type"]!.GetValue<string>().ToLowerInvariant()).ToList();
        Assert.Equal(2, coreWeekOneTypes.Count(t => t == "tempo"));
        Assert.Equal(2, coreWeekOneTypes.Count(t => t == "easy"));
        Assert.Equal(1, coreWeekOneTypes.Count(t => t == "long_run"));

        Assert.Equal("preparation_runway_preview_not_confirmable", preview["lifecycle"]!.GetValue<string>());

        // §24: every day has a resolved, non-empty public workout type/intensity — no mapping gap.
        Assert.All(allDays, d => Assert.False(string.IsNullOrEmpty(d!["day_type"]!.GetValue<string>())));
        Assert.All(allDays, d => Assert.True(d!["distance_km"]!.GetValue<double>() >= 0));
        Assert.All(allDays, d => Assert.False(string.IsNullOrEmpty(d!["intensity"]!.GetValue<string>())));

        var dates = allDays.Select(d => DateOnly.Parse(d!["date"]!.GetValue<string>().Substring(0, 10))).ToList();
        Assert.Equal(dates.OrderBy(d => d), dates);

        var after = await CountRowsAsync();
        Assert.Equal(before.previews + 1, after.previews);
        Assert.Equal(before.plans, after.plans);
    }

    // ── §16-18: readiness matrix ──────────────────────────────────────────────

    [Fact]
    public async Task MissingReadinessEvidence_ResolvesViaFreq6CAuthority_Returns200()
    {
        // Phase 10K-FREQ.6D.8 disclosed this as a real, pre-existing Core
        // volume-feasibility blocker (missing-readiness Intermediate x5D
        // Core Week 1 residual volume could not satisfy the 2-KEY_SESSION
        // minimum). Phase 10K-FREQ.6D.9 reconciled the numeric authority
        // (missing=26.0km, explicit-zero=19.5km, already approved by
        // FREQ.6C but never wired to any 5D call site) and Phase
        // 10K-FREQ.6D.10 wired it into both the Core dispatcher
        // (CatalogVolumeAndLongRunPlanner) and the Preparation Runway
        // numeric policy factory (TenKPreparationRunwayNumericPolicyFactory).
        // This now succeeds end-to-end. recent_longest_run_km is supplied
        // (non-zero) alongside a missing recent_weekly_volume_km so
        // CoreEntryReadinessResolver (Phase 4D.3.1, a separate, pre-existing,
        // unrelated gate on GOAL_PACE_TEN_K sessions) lands in its non-
        // blocking CAUTION band rather than NOT_READY -- it only rejects
        // when BOTH fields are missing/low, never when just one is missing.
        await ResetAsync();
        var startDate = new DateOnly(2027, 7, 19);
        var raceDate = startDate.AddDays(17 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                recentWeeklyVolumeKm: null, recentLongestRunKm: 8, recentRunsPerWeek: null));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__5D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(17, preview["weeks"]!.AsArray().Count);
    }

    [Fact]
    public async Task ExplicitZeroReadinessEvidence_ResolvesViaFreq6CAuthority_Returns200()
    {
        // Same FREQ.6D.8->6D.9->6D.10 resolution as
        // MissingReadinessEvidence_ResolvesViaFreq6CAuthority_Returns200
        // above, for the explicit-zero axis (resolves 19.5km). Here
        // recent_longest_run_km is left unset (rather than 0) for the same
        // CoreEntryReadinessResolver-isolation reason: "one field missing"
        // lands in CAUTION from the other side, without changing what
        // recent_weekly_volume_km=0 itself reports to the starting-volume
        // policy.
        await ResetAsync();
        var startDate = new DateOnly(2027, 7, 19);
        var raceDate = startDate.AddDays(17 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                recentWeeklyVolumeKm: 0, recentLongestRunKm: null, recentRunsPerWeek: null));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__5D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(17, preview["weeks"]!.AsArray().Count);
    }

    [Fact]
    public async Task PositiveObservedReadiness_Returns200_UsesExistingClampGrowthRounding()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(18 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                recentWeeklyVolumeKm: 30, recentLongestRunKm: 11, recentRunsPerWeek: 5));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(18, preview["weeks"]!.AsArray().Count);
        var runwayWeeks = preview["weeks"]!.AsArray().Where(w => w!["runway_block"] is not null).ToList();
        Assert.All(runwayWeeks, AssertRunwayWeekShape);
    }

    // ── §20: calendar (PreferredDays / LongRunDay) ────────────────────────────

    [Fact]
    public async Task PreferredDaysAndLongRunDay_Honored_AcrossFiveDayRunwayAndCore()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(18 * 7);
        var preferred = new[] { "tue", "wed", "thu", "sat", "sun" };

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                preferredDays: preferred, longRunDay: "sun"));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        var allowedDays = new HashSet<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        var allDays = preview["weeks"]!.AsArray().SelectMany(w => w!["days"]!.AsArray()).ToList();
        Assert.All(allDays, d => Assert.Contains(DateOnly.Parse(d!["date"]!.GetValue<string>().Substring(0, 10)).DayOfWeek, allowedDays));

        var longRuns = allDays.Where(d => d!["day_type"]!.GetValue<string>().Equals("long_run", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(longRuns);
        Assert.All(longRuns, d => Assert.Equal(DayOfWeek.Sunday, DateOnly.Parse(d!["date"]!.GetValue<string>().Substring(0, 10)).DayOfWeek));

        // No two sessions share a calendar date (deterministic assignment, no collision).
        Assert.Equal(allDays.Count, allDays.Select(d => d!["date"]!.GetValue<string>()).Distinct().Count());
    }

    // ── §35-36: LongHorizon negative tests remain closed ──────────────────────

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    public async Task TwentyOneWeeksAndAbove_StillReturns422_DoesNotEnterRunwayPath(int totalWeeks)
    {
        await ResetAsync();
        var before = await CountRowsAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(totalWeeks * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", error!["errorCode"]!.GetValue<string>());

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    // ── §37: unsupported neighbors remain closed ──────────────────────────────

    [Theory]
    [InlineData("ten_k", "beginner", 5)]
    [InlineData("ten_k", "advanced", 5)]
    [InlineData("ten_k", "intermediate", 6)]
    [InlineData("ten_k", "intermediate", 7)]
    public async Task UnsupportedNeighbors_FifteenToTwentyWeeks_StillReturns422(string goalDistance, string level, int daysPerWeek)
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);

        object request = new
        {
            goal_distance = goalDistance,
            level,
            days_per_week = daysPerWeek,
            unit = "km",
            start_date = startDate.ToString("yyyy-MM-dd"),
            preferred_days = daysPerWeek switch
            {
                6 => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
                7 => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" },
                _ => FiveDays,
            },
            long_run_day = "sun",
            race_date = raceDate.ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480,
            target_finish_time_source = "user_defined",
            race_name = (string?)null,
            recent_weekly_volume_km = 20,
            recent_longest_run_km = 8,
            recent_runs_per_week = 4,
            recent_race = (object?)null,
        };

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", error!["errorCode"]!.GetValue<string>());
    }

    // ── §38: Intermediate×4D Runway zero-delta ────────────────────────────────

    [Fact]
    public async Task FourDayRunway_Regression_StructureRemainsOneKeyTwoEasyOneLong()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                daysPerWeek: 4, preferredDays: ["mon", "wed", "fri", "sun"],
                recentWeeklyVolumeKm: 20, recentLongestRunKm: 8, recentRunsPerWeek: 3));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());

        var runwayWeeks = preview["weeks"]!.AsArray().Where(w => w!["runway_block"] is not null).ToList();
        Assert.All(runwayWeeks, w =>
        {
            var days = w!["days"]!.AsArray();
            Assert.Equal(4, days.Count);
            var types = days.Select(d => d!["day_type"]!.GetValue<string>().ToLowerInvariant()).ToList();
            Assert.Equal(1, types.Count(t => t == "tempo"));
            Assert.Equal(2, types.Count(t => t == "easy"));
            Assert.Equal(1, types.Count(t => t == "long_run"));
        });
    }

    // ── §39: Intermediate×5D Core-only zero-delta ─────────────────────────────

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task FiveDayCoreOnly_Regression_RemainsCoreConfirmable(int totalWeeks)
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(totalWeeks * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__5D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(totalWeeks, preview["weeks"]!.AsArray().Count);
        Assert.Equal("core_confirmable", preview["lifecycle"]!.GetValue<string>());
        Assert.All(preview["weeks"]!.AsArray(), w => Assert.True(w!["runway_block"] is null));
    }
}
