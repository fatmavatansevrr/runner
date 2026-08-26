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
/// Phase 10K-FREQ.6D.8 — real PostgreSQL confirmation for Intermediate×5D
/// Preparation Runway (§26-33 of the phase prompt). Mirrors the exact
/// established <see cref="PreparationRunwayConfirmationEndToEndTests"/>
/// pattern (dedicated confirmation-enabled <see cref="CustomWebApplicationFactory"/>
/// instance, not the shared collection factory) with DaysPerWeek=5. No
/// production code is touched by this file.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PreparationRunwayFiveDayConfirmationEndToEndTests
{
    private static readonly string[] FiveDays = ["mon", "tue", "thu", "fri", "sun"];

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
        days_per_week = 5,
        unit = "km",
        start_date = startDate,
        preferred_days = FiveDays,
        long_run_day = "sun",
        race_date = raceDate,
        target_finish_time_seconds = 3480,
        target_finish_time_source = "product_average",
        race_name = (string?)null,
        recent_weekly_volume_km = 24,
        recent_longest_run_km = 9,
        recent_runs_per_week = 4,
        recent_race = (object?)null,
    };

    private static async Task<(int previews, int plans, int weeks, int days)> CountRowsAsync(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (
            await ctx.PlanPreviews.CountAsync(),
            await ctx.TrainingPlans.CountAsync(),
            await ctx.TrainingWeeks.CountAsync(),
            await ctx.TrainingDays.CountAsync());
    }

    // ── §26-30: real PostgreSQL confirmation, persisted structure/boundary/lineage ──

    [Theory]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(17)]
    public async Task FiveDayPilotScope_PreviewThenConfirm_PersistsExactRunwayAndCoreStructure(int totalWeeks)
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        var previewBody = await previewResponse.Content.ReadAsStringAsync();
        Assert.True(previewResponse.StatusCode == HttpStatusCode.OK, previewBody);
        var preview = JsonNode.Parse(previewBody)!;
        Assert.Equal("TEN_K__5D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal("preparation_runway_preview_confirmable", preview["lifecycle"]!.GetValue<string>());
        var previewId = preview["preview_id"]!.GetValue<string>();

        var before = await CountRowsAsync(factory);

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        Assert.True(confirmResponse.StatusCode == HttpStatusCode.OK, confirmBody);
        var confirmed = JsonNode.Parse(confirmBody)!;
        var planId = Guid.Parse(confirmed["plan_id"]!.GetValue<string>());

        var after = await CountRowsAsync(factory);
        Assert.Equal(before.plans + 1, after.plans);
        Assert.Equal(before.weeks + totalWeeks, after.weeks);
        // §27/§28: every full week (Runway or Core) persists exactly 5 TrainingDays.
        Assert.Equal(before.days + (totalWeeks * 5), after.days);

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var plans = await ctx.TrainingPlans.Where(p => p.Id == planId).ToListAsync();
        Assert.Single(plans);
        Assert.Equal(TrainingPlanStatus.Active, plans[0].Status);
        Assert.Equal(GoalType.Race, plans[0].GoalType);
        Assert.Equal(GoalDistance.TenK, plans[0].GoalDistance);
        Assert.Equal(5, plans[0].DaysPerWeek);

        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).OrderBy(w => w.WeekNumber).ToListAsync();
        Assert.Equal(totalWeeks, weeks.Count);
        Assert.Equal(Enumerable.Range(1, totalWeeks), weeks.Select(w => w.WeekNumber));

        var runwayWeekCount = totalWeeks - 12;
        var runwayWeeks = weeks.Take(runwayWeekCount).ToList();
        var coreWeeks = weeks.Skip(runwayWeekCount).ToList();

        Assert.All(runwayWeeks, w => Assert.Equal(TrainingWeekType.PreparationRunway, w.WeekType));
        Assert.Equal("PRE_SPECIFIC_TRANSITION", runwayWeeks.Last().CatalogPhaseKey);
        Assert.All(coreWeeks, w => Assert.NotEqual(TrainingWeekType.PreparationRunway, w.WeekType));
        Assert.Equal("FOUNDATION", coreWeeks.First().CatalogPhaseKey);
        Assert.Equal(TrainingWeekType.Base, coreWeeks.First().WeekType);

        var days = await ctx.TrainingDays.Where(d => d.PlanId == planId).ToListAsync();
        Assert.Equal(totalWeeks * 5, days.Count);
        Assert.All(days, d => Assert.True(d.PlannedDistanceKm >= 0));
        Assert.All(days, d => Assert.False(string.IsNullOrEmpty(d.Intensity)));
        Assert.Equal(days.Count, days.Select(d => d.Date).Distinct().Count());
        // §27/§28: exactly 5 TrainingDays per week, no overlap/gap.
        Assert.All(weeks, w => Assert.Equal(5, days.Count(d => d.WeekId == w.Id)));

        // §27: persisted full Runway week role cardinality — 1 KEY + 3 EASY + 1 LONG.
        foreach (var week in runwayWeeks)
        {
            var weekDays = days.Where(d => d.WeekId == week.Id).ToList();
            Assert.Equal(1, weekDays.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
            Assert.Equal(3, weekDays.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
            Assert.Equal(1, weekDays.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
        }

        // §28: persisted Core week role cardinality — 2 KEY + 2 EASY + 1 LONG.
        foreach (var week in coreWeeks)
        {
            var weekDays = days.Where(d => d.WeekId == week.Id).ToList();
            Assert.Equal(2, weekDays.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
            Assert.Equal(2, weekDays.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
            Assert.Equal(1, weekDays.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
        }

        // §29: permanent DB-backed regression — exact last-Runway/first-Core transition.
        var lastRunwayDays = days.Where(d => d.WeekId == runwayWeeks.Last().Id).ToList();
        var firstCoreDays = days.Where(d => d.WeekId == coreWeeks.First().Id).ToList();
        Assert.Equal(1, lastRunwayDays.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
        Assert.Equal(3, lastRunwayDays.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, lastRunwayDays.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
        Assert.Equal(2, firstCoreDays.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
        Assert.Equal(2, firstCoreDays.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, firstCoreDays.Count(d => d.CatalogStructuralRole == "LONG_RUN"));

        // §30: ProfileBacked Core KEY sessions carry exact profile lineage; no
        // new Runway-specific persistence fields expected — same columns as 4D.
        var coreKeySessions = days.Where(d => coreWeeks.Select(w => w.Id).Contains(d.WeekId) && d.CatalogStructuralRole == "KEY_SESSION").ToList();
        Assert.All(coreKeySessions, d =>
        {
            Assert.False(string.IsNullOrEmpty(d.CatalogPrescriptionProfileKey));
            Assert.True(d.CatalogPrescriptionProfileVersion is > 0);
        });

        // Idempotent confirm.
        var secondConfirm = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.Equal(HttpStatusCode.OK, secondConfirm.StatusCode);
        var secondConfirmed = JsonNode.Parse(await secondConfirm.Content.ReadAsStringAsync())!;
        Assert.Equal(planId, Guid.Parse(secondConfirmed["plan_id"]!.GetValue<string>()));
        var afterSecondConfirm = await CountRowsAsync(factory);
        Assert.Equal(after.plans, afterSecondConfirm.plans);
    }

    // ── §31-33: active home / calendar / training-day detail read surfaces ───

    [Fact]
    public async Task FiveDayConfirmedPlan_ReadableViaHomeCalendarAndTrainingDayDetail()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);

        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        var previewId = preview["preview_id"]!.GetValue<string>();

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        // §31: active home.
        var homeResponse = await client.GetAsync("/api/v1/plans/active/home");
        var homeBody = await homeResponse.Content.ReadAsStringAsync();
        Assert.True(homeResponse.StatusCode == HttpStatusCode.OK, homeBody);

        // §33: active plan details — total weeks unaffected by the 5-session-per-week shape.
        var detailsResponse = await client.GetAsync("/api/v1/plans/active/details");
        var detailsBody = await detailsResponse.Content.ReadAsStringAsync();
        Assert.True(detailsResponse.StatusCode == HttpStatusCode.OK, detailsBody);
        var details = JsonNode.Parse(detailsBody)!;
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Equal(17, details["total_weeks"]!.GetValue<int>());
        Assert.Equal(17, details["weeks"]!.AsArray().Count);

        // §32: active calendar — first month of the plan, expect no rendering
        // failure from a 5-session week (never a 4-session assumption).
        var calendarResponse = await client.GetAsync($"/api/v1/plans/active/calendar?month={startDate:yyyy-MM}");
        var calendarBody = await calendarResponse.Content.ReadAsStringAsync();
        Assert.True(calendarResponse.StatusCode == HttpStatusCode.OK, calendarBody);

        // §33: representative training-day detail reads — one of each real
        // role reached through the Runway segment (KEY/EASY/LONG) and, if the
        // horizon reached Core within this scope, one of Core's dual KEY.
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var representativeDays = await ctx.TrainingDays
            .Where(d => d.PlanId == planId)
            .GroupBy(d => d.CatalogStructuralRole)
            .Select(g => g.First())
            .ToListAsync();
        Assert.NotEmpty(representativeDays);

        foreach (var day in representativeDays)
        {
            var detailResponse = await client.GetAsync($"/api/v1/training-days/{day.Id}");
            var detailBody = await detailResponse.Content.ReadAsStringAsync();
            Assert.True(detailResponse.StatusCode == HttpStatusCode.OK, $"role={day.CatalogStructuralRole}: {detailBody}");
            var detail = JsonNode.Parse(detailBody)!;
            Assert.Equal(day.Id.ToString(), detail["day_id"]!.GetValue<string>());
        }
    }

    // ── §14/§29 boundary at the shortest (15-week) and longest (20-week) horizons ──

    [Theory]
    [InlineData(15)]
    [InlineData(20)]
    public async Task FiveDayPilotScope_ReloadedFromPostgres_AssertsPermanentRunwayCoreBoundaryRegression(int totalWeeks)
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        var previewId = preview["preview_id"]!.GetValue<string>();

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        // Reload from a brand-new scope/context instance -- a genuine re-read, not the write-path context.
        using var reloadScope = factory.Services.CreateScope();
        var reloadCtx = reloadScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reloadedWeeks = await reloadCtx.TrainingWeeks.AsNoTracking()
            .Where(w => w.PlanId == planId).OrderBy(w => w.WeekNumber).ToListAsync();
        var reloadedDays = await reloadCtx.TrainingDays.AsNoTracking()
            .Where(d => d.PlanId == planId).ToListAsync();

        var runwayWeekCount = totalWeeks - 12;
        var lastRunwayWeek = reloadedWeeks[runwayWeekCount - 1];
        var firstCoreWeek = reloadedWeeks[runwayWeekCount];

        var lastRunwayRoles = reloadedDays.Where(d => d.WeekId == lastRunwayWeek.Id)
            .GroupBy(d => d.CatalogStructuralRole).ToDictionary(g => g.Key!, g => g.Count());
        var firstCoreRoles = reloadedDays.Where(d => d.WeekId == firstCoreWeek.Id)
            .GroupBy(d => d.CatalogStructuralRole).ToDictionary(g => g.Key!, g => g.Count());

        Assert.Equal(1, lastRunwayRoles["KEY_SESSION"]);
        Assert.Equal(3, lastRunwayRoles["EASY_SUPPORT"]);
        Assert.Equal(1, lastRunwayRoles["LONG_RUN"]);
        Assert.Equal(2, firstCoreRoles["KEY_SESSION"]);
        Assert.Equal(2, firstCoreRoles["EASY_SUPPORT"]);
        Assert.Equal(1, firstCoreRoles["LONG_RUN"]);
    }
}
