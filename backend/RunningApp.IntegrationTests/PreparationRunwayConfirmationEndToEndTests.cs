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
/// Backend Integration Phase 4G.6C — scoped confirmation and persistence
/// activation for the 15-20 week TEN_K Preparation Runway pilot. Runs
/// against the real Api host + real Postgres DB.
///
/// Deliberately does NOT use the shared <see cref="ApiIntegrationTestCollection"/>
/// factory (whose appsettings.Development.json default is
/// <c>PreparationRunwayPilotActivation:ConfirmationEnabled=false</c> — kept
/// false by default specifically so every pre-existing 4G.6B/4G.6B.1/4G.6B.2
/// test asserting <c>preparation_runway_preview_not_confirmable</c>/
/// <c>CATALOG_PREVIEW_NOT_PERSISTABLE</c> continues to pass unmodified).
/// This file owns a dedicated <see cref="CustomWebApplicationFactory"/>
/// instance per test with the confirmation gate explicitly overridden true,
/// proving confirmation activation is real and independently toggleable
/// without touching the shared default.
///
/// Still declares <see cref="ApiIntegrationTestCollection"/> membership
/// (without taking the shared factory as a constructor parameter -- every
/// test here builds its own) purely to serialize against every other HTTP
/// test class in that collection: this file's own dedicated
/// <see cref="CustomWebApplicationFactory"/> instances share the SAME real
/// Postgres database and single mock user as the rest of the suite, and
/// running outside the collection caused real concurrent-reset/row-count
/// races against those other tests.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PreparationRunwayConfirmationEndToEndTests
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

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    public async Task PilotScope_PreviewThenConfirm_PersistsOneTrainingPlanWithExactWeekAndDayCounts(int totalWeeks)
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

        // Lifecycle explicitly confirmable -- never inferred from payload presence.
        Assert.Equal("preparation_runway_preview_confirmable", preview["lifecycle"]!.GetValue<string>());
        var previewId = preview["preview_id"]!.GetValue<string>();

        var before = await CountRowsAsync(factory);

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        Assert.True(confirmResponse.StatusCode == HttpStatusCode.OK, confirmBody);
        var confirmed = JsonNode.Parse(confirmBody)!;
        var planId = Guid.Parse(confirmed["plan_id"]!.GetValue<string>());

        var after = await CountRowsAsync(factory);
        // Exactly one TrainingPlan; exact week/day counts; no partial writes.
        Assert.Equal(before.plans + 1, after.plans);
        Assert.Equal(before.weeks + totalWeeks, after.weeks);
        Assert.Equal(before.days + (totalWeeks * 4), after.days);

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var plans = await ctx.TrainingPlans.Where(p => p.Id == planId).ToListAsync();
        Assert.Single(plans);
        Assert.Equal(TrainingPlanStatus.Active, plans[0].Status);
        Assert.Equal(GoalType.Race, plans[0].GoalType);
        Assert.Equal(GoalDistance.TenK, plans[0].GoalDistance);

        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).OrderBy(w => w.WeekNumber).ToListAsync();
        Assert.Equal(totalWeeks, weeks.Count);
        Assert.Equal(Enumerable.Range(1, totalWeeks), weeks.Select(w => w.WeekNumber));

        var runwayWeekCount = totalWeeks - 12;
        var runwayWeeks = weeks.Take(runwayWeekCount).ToList();
        var coreWeeks = weeks.Skip(runwayWeekCount).ToList();

        // Runway weeks: honest WeekType, never Foundation/Build/Taper.
        Assert.All(runwayWeeks, w => Assert.Equal(TrainingWeekType.PreparationRunway, w.WeekType));
        Assert.Equal("PRE_SPECIFIC_TRANSITION", runwayWeeks.Last().CatalogPhaseKey);

        // Core weeks: never PreparationRunway; first Core week is Foundation.
        Assert.All(coreWeeks, w => Assert.NotEqual(TrainingWeekType.PreparationRunway, w.WeekType));
        Assert.Equal("FOUNDATION", coreWeeks.First().CatalogPhaseKey);
        Assert.Equal(TrainingWeekType.Base, coreWeeks.First().WeekType);


        var days = await ctx.TrainingDays.Where(d => d.PlanId == planId).ToListAsync();
        Assert.Equal(totalWeeks * 4, days.Count);
        Assert.All(days, d => Assert.True(d.PlannedDistanceKm >= 0));
        Assert.All(days, d => Assert.False(string.IsNullOrEmpty(d.Intensity)));
        // No date duplicated across the whole plan.
        Assert.Equal(days.Count, days.Select(d => d.Date).Distinct().Count());
        // Every week has exactly 4 days -- no overlap/gap across the runway/Core boundary.
        Assert.All(weeks, w => Assert.Equal(4, days.Count(d => d.WeekId == w.Id)));

        // Confirm is idempotent: a second confirm of the same preview
        // returns the same plan, creates no duplicate.
        var secondConfirm = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.Equal(HttpStatusCode.OK, secondConfirm.StatusCode);
        var secondConfirmed = JsonNode.Parse(await secondConfirm.Content.ReadAsStringAsync())!;
        Assert.Equal(planId, Guid.Parse(secondConfirmed["plan_id"]!.GetValue<string>()));

        var afterSecondConfirm = await CountRowsAsync(factory);
        Assert.Equal(after.plans, afterSecondConfirm.plans);
    }

    [Fact]
    public async Task PilotScope_ConfirmedPlan_ReadableViaActivePlanDetails()
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

        var detailsResponse = await client.GetAsync("/api/v1/plans/active/details");
        var detailsBody = await detailsResponse.Content.ReadAsStringAsync();
        Assert.True(detailsResponse.StatusCode == HttpStatusCode.OK, detailsBody);
        var details = JsonNode.Parse(detailsBody)!;

        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Equal(17, details["total_weeks"]!.GetValue<int>());
        Assert.Equal(17, details["weeks"]!.AsArray().Count);

        var homeResponse = await client.GetAsync("/api/v1/plans/active/home");
        var homeBody = await homeResponse.Content.ReadAsStringAsync();
        Assert.True(homeResponse.StatusCode == HttpStatusCode.OK, homeBody);
    }

    [Fact]
    public async Task PilotScope_EightToFourteenWeeks_RemainCoreConfirmable_AndConfirmUnchanged()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(12 * 7);

        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        Assert.Equal("core_confirmable", preview["lifecycle"]!.GetValue<string>());

        var previewId = preview["preview_id"]!.GetValue<string>();
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).ToListAsync();
        Assert.Equal(12, weeks.Count);
        Assert.All(weeks, w => Assert.NotEqual(TrainingWeekType.PreparationRunway, w.WeekType));
    }

    [Fact]
    public async Task PilotScope_TwentyOneWeeks_StillReturns422_NoPersistence()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var before = await CountRowsAsync(factory);
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(21 * 7);

        var response = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var after = await CountRowsAsync(factory);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task DisabledConfirmationGate_PreviewSucceeds_ButRemainsNotConfirmable_NoWrites()
    {
        // Independent factory instance with ConfirmationEnabled overridden to
        // false -- proves confirmation-only rollback, distinct from the
        // preview gate (which stays enabled here, matching the exact
        // documented independent-rollback semantics). Matches the shared
        // collection factory's own appsettings.Development.json default, but
        // spelled out explicitly here so this test's intent is self-evident.
        await using var factory = new CustomWebApplicationFactory("Development", new Dictionary<string, string?>
        {
            ["PreparationRunwayPilotActivation:Enabled"] = "true",
            ["PreparationRunwayPilotActivation:ConfirmationEnabled"] = "false",
        });
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var before = await CountRowsAsync(factory);

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(16 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        var previewBody = await previewResponse.Content.ReadAsStringAsync();
        Assert.True(previewResponse.StatusCode == HttpStatusCode.OK, previewBody);
        var preview = JsonNode.Parse(previewBody)!;
        Assert.Equal("preparation_runway_preview_not_confirmable", preview["lifecycle"]!.GetValue<string>());

        var previewId = preview["preview_id"]!.GetValue<string>();
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, confirmResponse.StatusCode);
        var error = JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!;
        Assert.Equal("CATALOG_PREVIEW_NOT_PERSISTABLE", error["errorCode"]!.GetValue<string>());

        var after = await CountRowsAsync(factory);
        // A PlanPreview row is always created for a successful preview
        // (confirmable or not -- unchanged, pre-existing convention). No
        // TrainingPlan/Week/Day is ever written when the confirmation gate
        // is disabled.
        Assert.Equal(before.previews + 1, after.previews);
        Assert.Equal(before.plans, after.plans);
        Assert.Equal(before.weeks, after.weeks);
        Assert.Equal(before.days, after.days);
    }
}
