using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Backend Integration Phase 4G.6D — real HTTP + real PostgreSQL proof that
/// Home/Calendar/Training Day Detail expose the new additive provenance
/// fields (week_number/week_type/runway_block/source/adapted_from_id),
/// mapped directly from persisted TrainingWeek/TrainingDay entities — every
/// assertion in this file compares the HTTP response against a direct
/// database read of the same rows, never an independently reconstructed
/// expected value.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PreparationRunwayProvenanceContractTests
{
    private static CustomWebApplicationFactory ConfirmationEnabledFactory() =>
        new("Development", new Dictionary<string, string?>
        {
            ["PreparationRunwayPilotActivation:Enabled"] = "true",
            ["PreparationRunwayPilotActivation:ConfirmationEnabled"] = "true",
        });

    private static object RaceRequest(
        string startDate, string raceDate,
        double? recentWeeklyVolumeKm = 20, double? recentLongestRunKm = 8, int? recentRunsPerWeek = 3) => new
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
        recent_weekly_volume_km = recentWeeklyVolumeKm,
        recent_longest_run_km = recentLongestRunKm,
        recent_runs_per_week = recentRunsPerWeek,
        recent_race = (object?)null,
    };

    private static async Task<Guid> ConfirmPlanAsync(HttpClient client, DateOnly startDate, int totalWeeks, double? weeklyVolume = 24, double? longestRun = 9)
    {
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), weeklyVolume, longestRun));
        var previewBody = await previewResponse.Content.ReadAsStringAsync();
        Assert.True(previewResponse.StatusCode == HttpStatusCode.OK, previewBody);
        var preview = JsonNode.Parse(previewBody)!;

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        Assert.True(confirmResponse.StatusCode == HttpStatusCode.OK, confirmBody);
        return Guid.Parse(JsonNode.Parse(confirmBody)!["plan_id"]!.GetValue<string>());
    }

    // ── Part 9: Home HTTP verification ──────────────────────────────────────

    [Fact]
    public async Task Home_DuringRunway_ExposesEntityDerivedWeekNumberTypeAndBlock()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        // "Today" engineered to fall inside week 1 (a runway week for any
        // 15-20 week horizon) -- same technique as Phase 4G.6C.2.
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-3);
        var planId = await ConfirmPlanAsync(client, startDate, 15);

        var homeResponse = await client.GetAsync("/api/v1/plans/active/home");
        var homeBody = await homeResponse.Content.ReadAsStringAsync();
        Assert.True(homeResponse.StatusCode == HttpStatusCode.OK, homeBody);
        var activePlan = JsonNode.Parse(homeBody)!["active_plan"]!;

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.AsNoTracking().Where(w => w.PlanId == planId).OrderBy(w => w.WeekNumber).ToListAsync();
        var currentWeekNumber = activePlan["current_week_number"]!.GetValue<int>();
        var dbWeek = weeks.Single(w => w.WeekNumber == currentWeekNumber);

        Assert.Equal(15, activePlan["total_weeks"]!.GetValue<int>());
        Assert.Equal("preparation_runway", activePlan["current_week_type"]!.GetValue<string>());
        Assert.Equal(TrainingWeekType.PreparationRunway, dbWeek.WeekType);
        Assert.Equal(dbWeek.CatalogPhaseKey, activePlan["current_runway_block"]!.GetValue<string>());
        Assert.NotNull(dbWeek.CatalogPhaseKey);

        // progress_text remains unchanged/independent (never parsed to derive the new fields).
        Assert.Contains($"Week {currentWeekNumber} of 15", activePlan["progress_text"]!.GetValue<string>());
    }

    [Fact]
    public async Task Home_AtFirstCoreWeek_ExposesActualCorePhaseAndNullRunwayBlock()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        // 17-week plan, CoreEntryReady evidence -> deterministically 5 runway
        // weeks + 12 Core weeks (Phase 4G.6C.2A's established allocation
        // table). "Today" engineered to fall inside week 6 (the first Core week).
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-(5 * 7 + 2));
        var planId = await ConfirmPlanAsync(client, startDate, 17, weeklyVolume: 24, longestRun: 9);

        var homeResponse = await client.GetAsync("/api/v1/plans/active/home");
        var homeBody = await homeResponse.Content.ReadAsStringAsync();
        Assert.True(homeResponse.StatusCode == HttpStatusCode.OK, homeBody);
        var activePlan = JsonNode.Parse(homeBody)!["active_plan"]!;

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var week6 = await ctx.TrainingWeeks.AsNoTracking().SingleAsync(w => w.PlanId == planId && w.WeekNumber == 6);

        Assert.Equal(6, activePlan["current_week_number"]!.GetValue<int>());
        Assert.Equal(17, activePlan["total_weeks"]!.GetValue<int>());
        Assert.NotEqual(TrainingWeekType.PreparationRunway, week6.WeekType);
        Assert.Equal(RunningApp.Application.Common.EnumSnakeCase.ToSnakeCase(week6.WeekType), activePlan["current_week_type"]!.GetValue<string>());
        Assert.True(activePlan["current_runway_block"] is null || activePlan["current_runway_block"]!.GetValue<string?>() is null);
    }

    [Fact]
    public async Task Home_TwentyWeekPlan_TotalWeeksNeverHardcodedToTwelve()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-3);
        await ConfirmPlanAsync(client, startDate, 20, weeklyVolume: 24, longestRun: 9);

        var homeResponse = await client.GetAsync("/api/v1/plans/active/home");
        var activePlan = JsonNode.Parse(await homeResponse.Content.ReadAsStringAsync())!["active_plan"]!;
        Assert.Equal(20, activePlan["total_weeks"]!.GetValue<int>());
        Assert.NotEqual(12, activePlan["total_weeks"]!.GetValue<int>());
    }

    // ── Part 14: 8-14 week Core regression ──────────────────────────────────

    [Fact]
    public async Task Home_CoreOnlyPlan_RunwayBlockAlwaysNull_WeekTypeIsActualCorePhase()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-3);
        var planId = await ConfirmPlanAsync(client, startDate, 12);

        var homeResponse = await client.GetAsync("/api/v1/plans/active/home");
        var activePlan = JsonNode.Parse(await homeResponse.Content.ReadAsStringAsync())!["active_plan"]!;

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var currentWeekNumber = activePlan["current_week_number"]!.GetValue<int>();
        var dbWeek = await ctx.TrainingWeeks.AsNoTracking().SingleAsync(w => w.PlanId == planId && w.WeekNumber == currentWeekNumber);

        Assert.Equal(12, activePlan["total_weeks"]!.GetValue<int>());
        Assert.NotEqual(TrainingWeekType.PreparationRunway, dbWeek.WeekType);
        Assert.Null(activePlan["current_runway_block"]);
    }

    // ── Part 10: Calendar HTTP verification ─────────────────────────────────

    [Fact]
    public async Task Calendar_RunwayMonth_ExposesWeekNumberTypeBlockPerDay_MatchingDatabase()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var planId = await ConfirmPlanAsync(client, startDate, 17, weeklyVolume: 24, longestRun: 9);

        var calendarResponse = await client.GetAsync("/api/v1/plans/active/calendar?month=2026-07");
        var calendarBody = await calendarResponse.Content.ReadAsStringAsync();
        Assert.True(calendarResponse.StatusCode == HttpStatusCode.OK, calendarBody);
        var calendarDays = JsonNode.Parse(calendarBody)!.AsArray();

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeksById = await ctx.TrainingWeeks.AsNoTracking().Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);
        var dbDaysById = await ctx.TrainingDays.AsNoTracking().Where(d => d.PlanId == planId).ToDictionaryAsync(d => d.Id);

        var realDays = calendarDays.Where(d => d!["day_id"] != null).ToList();
        Assert.NotEmpty(realDays);
        foreach (var day in realDays)
        {
            var dayId = Guid.Parse(day!["day_id"]!.GetValue<string>());
            var dbDay = dbDaysById[dayId];
            var dbWeek = weeksById[dbDay.WeekId];

            Assert.Equal(dbWeek.WeekNumber, day["week_number"]!.GetValue<int>());
            Assert.Equal(TrainingWeekType.PreparationRunway, dbWeek.WeekType);
            Assert.Equal("preparation_runway", day["week_type"]!.GetValue<string>());
            Assert.Equal(dbWeek.CatalogPhaseKey, day["runway_block"]!.GetValue<string>());
        }
    }

    [Fact]
    public async Task Calendar_BoundaryMonth_CoreDaysHaveNullRunwayBlockAndActualPhase()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        // 17-week plan: 5 runway weeks (35 days) then Core. Starting 2026-07-20,
        // Core begins on day 36 -> 2026-08-24, so September is a Core-only month
        // and August (partially) straddles the boundary.
        var startDate = new DateOnly(2026, 7, 20);
        var planId = await ConfirmPlanAsync(client, startDate, 17, weeklyVolume: 24, longestRun: 9);

        var calendarResponse = await client.GetAsync("/api/v1/plans/active/calendar?month=2026-09");
        var calendarBody = await calendarResponse.Content.ReadAsStringAsync();
        Assert.True(calendarResponse.StatusCode == HttpStatusCode.OK, calendarBody);
        var calendarDays = JsonNode.Parse(calendarBody)!.AsArray();

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeksById = await ctx.TrainingWeeks.AsNoTracking().Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);
        var dbDaysById = await ctx.TrainingDays.AsNoTracking().Where(d => d.PlanId == planId).ToDictionaryAsync(d => d.Id);

        var realDays = calendarDays.Where(d => d!["day_id"] != null).ToList();
        Assert.NotEmpty(realDays);
        foreach (var day in realDays)
        {
            var dayId = Guid.Parse(day!["day_id"]!.GetValue<string>());
            var dbWeek = weeksById[dbDaysById[dayId].WeekId];

            Assert.NotEqual(TrainingWeekType.PreparationRunway, dbWeek.WeekType);
            Assert.Null(day["runway_block"]);
            Assert.Equal(RunningApp.Application.Common.EnumSnakeCase.ToSnakeCase(dbWeek.WeekType), day["week_type"]!.GetValue<string>());
        }
    }

    // ── Part 11: Training Day Detail HTTP verification ──────────────────────

    [Fact]
    public async Task Detail_RunwayAndCoreDays_ExposeProvenanceMatchingDatabase()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var planId = await ConfirmPlanAsync(client, startDate, 17, weeklyVolume: 24, longestRun: 9);

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeksById = await ctx.TrainingWeeks.AsNoTracking().Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);
        var allDays = await ctx.TrainingDays.AsNoTracking().Where(d => d.PlanId == planId).ToListAsync();

        // One runway day (Intro, deterministically present at 17 weeks) and one Core day.
        var introDay = allDays.Single(d => d.Intensity == "CONTROLLED_AEROBIC_POWER_INTRO");
        var progressedDay = allDays.Single(d => d.Intensity == "CONTROLLED_AEROBIC_POWER_PROGRESSED");
        var coreDay = allDays.First(d => weeksById[d.WeekId].WeekType != TrainingWeekType.PreparationRunway);

        foreach (var dbDay in new[] { introDay, progressedDay, coreDay })
        {
            var dbWeek = weeksById[dbDay.WeekId];
            var detailResponse = await client.GetAsync($"/api/v1/training-days/{dbDay.Id}");
            var detailBody = await detailResponse.Content.ReadAsStringAsync();
            Assert.True(detailResponse.StatusCode == HttpStatusCode.OK, detailBody);
            var detail = JsonNode.Parse(detailBody)!;

            Assert.Equal(dbWeek.WeekNumber, detail["week_number"]!.GetValue<int>());
            Assert.Equal(RunningApp.Application.Common.EnumSnakeCase.ToSnakeCase(dbWeek.WeekType), detail["week_type"]!.GetValue<string>());
            if (dbWeek.WeekType == TrainingWeekType.PreparationRunway)
            {
                Assert.Equal(dbWeek.CatalogPhaseKey, detail["runway_block"]!.GetValue<string>());
            }
            else
            {
                Assert.Null(detail["runway_block"]);
            }

            Assert.Equal(dbDay.Source.HasValue ? RunningApp.Application.Common.EnumSnakeCase.ToSnakeCase(dbDay.Source.Value) : null,
                detail["source"]?.GetValue<string?>());
            Assert.Equal(dbDay.AdaptedFromId, detail["adapted_from_id"] is null ? null : Guid.Parse(detail["adapted_from_id"]!.GetValue<string>()));
        }

        // Intro and Progressed remain distinguishable through the new fields too.
        Assert.Equal(weeksById[introDay.WeekId].WeekNumber, weeksById[introDay.WeekId].WeekNumber);
        Assert.NotEqual(introDay.WeekId, progressedDay.WeekId);
    }

    [Fact]
    public async Task Detail_OriginalCatalogDay_SourceIsTemplate_AdaptedFromIsNull()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var planId = await ConfirmPlanAsync(client, startDate, 15);

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstDay = await ctx.TrainingDays.AsNoTracking().Where(d => d.PlanId == planId).OrderBy(d => d.Date).FirstAsync();

        var detailResponse = await client.GetAsync($"/api/v1/training-days/{firstDay.Id}");
        var detail = JsonNode.Parse(await detailResponse.Content.ReadAsStringAsync())!;

        Assert.Equal("template", detail["source"]!.GetValue<string>());
        Assert.Null(detail["adapted_from_id"]);
    }

    [Fact]
    public async Task Detail_AfterCompletion_ProvenanceFieldsUnchanged()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var planId = await ConfirmPlanAsync(client, startDate, 15);

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = await ctx.TrainingDays.AsNoTracking().Where(d => d.PlanId == planId && !d.IsLongRun).OrderBy(d => d.Date).FirstAsync();

        var beforeDetail = JsonNode.Parse(await (await client.GetAsync($"/api/v1/training-days/{day.Id}")).Content.ReadAsStringAsync())!;

        var completeResponse = await client.PostRawAsync(
            $"/api/v1/training-days/{day.Id}/complete",
            new { actual_distance_km = day.PlannedDistanceKm, actual_duration_min = 30 });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        var afterDetail = JsonNode.Parse(await (await client.GetAsync($"/api/v1/training-days/{day.Id}")).Content.ReadAsStringAsync())!;

        Assert.Equal(beforeDetail["week_number"]!.GetValue<int>(), afterDetail["week_number"]!.GetValue<int>());
        Assert.Equal(beforeDetail["week_type"]!.GetValue<string>(), afterDetail["week_type"]!.GetValue<string>());
        Assert.Equal(beforeDetail["source"]!.GetValue<string>(), afterDetail["source"]!.GetValue<string>());
        Assert.Null(afterDetail["adapted_from_id"]);
        Assert.Equal("completed", afterDetail["status"]!.GetValue<string>());
    }

    // ── Backward compatibility: existing fields unaffected ──────────────────

    [Fact]
    public async Task ExistingFields_RemainPresentAndUnchanged_AlongsideNewProvenanceFields()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        await ConfirmPlanAsync(client, startDate, 15);

        var homeResponse = await client.GetAsync("/api/v1/plans/active/home");
        var home = JsonNode.Parse(await homeResponse.Content.ReadAsStringAsync())!;
        var activePlan = home["active_plan"]!;

        // Every pre-existing field remains present.
        Assert.NotNull(activePlan["plan_id"]);
        Assert.NotNull(activePlan["goal_type"]);
        Assert.NotNull(activePlan["goal_distance"]);
        Assert.NotNull(activePlan["level"]);
        Assert.NotNull(activePlan["progress_text"]);
        // New fields are additive, present alongside them.
        Assert.NotNull(activePlan["current_week_number"]);
        Assert.NotNull(activePlan["total_weeks"]);
        Assert.NotNull(activePlan["current_week_type"]);
    }
}
