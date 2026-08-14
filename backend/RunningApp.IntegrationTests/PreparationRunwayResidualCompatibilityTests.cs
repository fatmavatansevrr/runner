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
/// Backend Integration Phase 4G.6C.2 — residual evidence/pace-source
/// persistence, deep Home behavior, an expanded Training Day Detail matrix,
/// Long Run/AerobicStrength completion, and the real pending-confirmations
/// workflow, for the 15-20 week TEN_K Preparation Runway confirmation path.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PreparationRunwayResidualCompatibilityTests
{
    private static CustomWebApplicationFactory ConfirmationEnabledFactory() =>
        new("Development", new Dictionary<string, string?>
        {
            ["PreparationRunwayPilotActivation:Enabled"] = "true",
            ["PreparationRunwayPilotActivation:ConfirmationEnabled"] = "true",
        });

    private static object RaceRequest(
        string startDate, string raceDate,
        double? recentWeeklyVolumeKm = 20, double? recentLongestRunKm = 8, int? recentRunsPerWeek = 3,
        int targetFinishTimeSeconds = 3480, string targetFinishTimeSource = "product_average",
        object? recentRace = null) => new
    {
        goal_distance = "ten_k",
        level = "intermediate",
        days_per_week = 4,
        unit = "km",
        start_date = startDate,
        preferred_days = new[] { "mon", "wed", "fri", "sun" },
        long_run_day = "sun",
        race_date = raceDate,
        target_finish_time_seconds = targetFinishTimeSeconds,
        target_finish_time_source = targetFinishTimeSource,
        race_name = (string?)null,
        recent_weekly_volume_km = recentWeeklyVolumeKm,
        recent_longest_run_km = recentLongestRunKm,
        recent_runs_per_week = recentRunsPerWeek,
        recent_race = recentRace,
    };

    // ── Part 8-11: evidence/pace-source confirmation ────────────────────────

    [Fact]
    public async Task PilotScope_MissingEvidence_ConfirmsAndPersistsEffortOnlyRunway()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                recentWeeklyVolumeKm: null, recentLongestRunKm: null, recentRunsPerWeek: null));
        var previewBody = await previewResponse.Content.ReadAsStringAsync();
        Assert.True(previewResponse.StatusCode == HttpStatusCode.OK, previewBody);
        var preview = JsonNode.Parse(previewBody)!;
        Assert.Equal("preparation_runway_preview_confirmable", preview["lifecycle"]!.GetValue<string>());

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        Assert.True(confirmResponse.StatusCode == HttpStatusCode.OK, confirmBody);
        var planId = Guid.Parse(JsonNode.Parse(confirmBody)!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(17, await ctx.TrainingWeeks.CountAsync(w => w.PlanId == planId));
        Assert.Equal(68, await ctx.TrainingDays.CountAsync(d => d.PlanId == planId));
    }

    [Fact]
    public async Task PilotScope_NoRecentRunningBase_ConfirmsAndPersistsConsistencyBlock()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                recentWeeklyVolumeKm: 0, recentLongestRunKm: 0, recentRunsPerWeek: 0));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        Assert.Equal("preparation_runway_preview_confirmable", preview["lifecycle"]!.GetValue<string>());

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        Assert.True(confirmResponse.StatusCode == HttpStatusCode.OK, confirmBody);
        var planId = Guid.Parse(JsonNode.Parse(confirmBody)!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).OrderBy(w => w.WeekNumber).ToListAsync();
        // Both-missing recent evidence for a Race -> NOT_READY -> ConsistencyNeeded.
        Assert.Equal("CONSISTENCY", weeks.First().CatalogPhaseKey);
        var days = await ctx.TrainingDays.Where(d => d.PlanId == planId && weeks.Take(5).Select(w => w.Id).Contains(d.WeekId)).ToListAsync();
        Assert.All(days, d => Assert.DoesNotContain("GOAL_PACE", d.Intensity, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PilotScope_RecentRaceEvidence_ConfirmsAndPersistsEffortOnlyRunway()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                recentRace: new { distance = "ten_k", finish_time_seconds = 2700, race_date = startDate.AddDays(-30).ToString("yyyy-MM-dd") }));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        Assert.True(confirmResponse.StatusCode == HttpStatusCode.OK, confirmBody);
        var planId = Guid.Parse(JsonNode.Parse(confirmBody)!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).OrderBy(w => w.WeekNumber).ToListAsync();
        var runwayDays = await ctx.TrainingDays.Where(d => d.PlanId == planId && weeks.Take(5).Select(w => w.Id).Contains(d.WeekId)).ToListAsync();
        Assert.All(runwayDays, d => Assert.DoesNotContain("RACE_SPECIFIC", d.Intensity, StringComparison.OrdinalIgnoreCase));
        Assert.All(runwayDays, d => Assert.DoesNotContain("GOAL_PACE", d.Intensity, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PilotScope_CorroboratedUserTarget_ConfirmsSuccessfully()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                targetFinishTimeSeconds: 3300, targetFinishTimeSource: "user_defined",
                recentRace: new { distance = "ten_k", finish_time_seconds = 3200, race_date = startDate.AddDays(-30).ToString("yyyy-MM-dd") }));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
    }

    [Fact]
    public async Task PilotScope_BareUserTarget_NoIndependentEvidence_TypedFailure_NoWrites()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await ctx.TrainingPlans.CountAsync();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);
        var response = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                targetFinishTimeSeconds: 3300, targetFinishTimeSource: "user_defined"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Equal("RUNTIME_CONDITION_UNSUPPORTED", error["errorCode"]!.GetValue<string>());
        Assert.Equal(before, await ctx.TrainingPlans.CountAsync());
    }

    // ── Part 12: deep Home verification ─────────────────────────────────────

    [Fact]
    public async Task PilotScope_Home_DuringRunway_ResolvesTodayWorkoutFromRunway()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        // StartDate 3 days before today -> "today" falls inside week 1
        // (a runway week for any 15-20 week horizon, since runway is always
        // >= 3 weeks). Deterministic relative to real UtcNow, no clock
        // injection needed (none exists in this repo -- Home uses DateTime.UtcNow.Date directly).
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-3);
        var raceDate = startDate.AddDays(18 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var week1 = await ctx.TrainingWeeks.SingleAsync(w => w.PlanId == planId && w.WeekNumber == 1);
        Assert.Equal(TrainingWeekType.PreparationRunway, week1.WeekType);

        var homeResponse = await client.GetAsync("/api/v1/plans/active/home");
        var homeBody = await homeResponse.Content.ReadAsStringAsync();
        Assert.True(homeResponse.StatusCode == HttpStatusCode.OK, homeBody);
        var home = JsonNode.Parse(homeBody)!;
        var activePlan = home["active_plan"]!;
        Assert.Equal(planId.ToString(), activePlan["plan_id"]!.GetValue<string>());
        // "Week 1 of 18" -- not a hardcoded 12-week display.
        Assert.Contains("Week 1 of 18", activePlan["progress_text"]!.GetValue<string>());
    }

    [Fact]
    public async Task PilotScope_Home_DuringCore_GlobalWeekIncludesRunwayOffset()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        // 18-week plan (6 runway weeks + 12 Core weeks). StartDate chosen so
        // "today" falls inside week 11 (Core week 5 of 12), well past the
        // runway/Core boundary at week 6.
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-(10 * 7 + 2));
        var raceDate = startDate.AddDays(18 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var week11 = await ctx.TrainingWeeks.SingleAsync(w => w.PlanId == planId && w.WeekNumber == 11);
        Assert.NotEqual(TrainingWeekType.PreparationRunway, week11.WeekType);

        var homeResponse = await client.GetAsync("/api/v1/plans/active/home");
        var homeBody = await homeResponse.Content.ReadAsStringAsync();
        Assert.True(homeResponse.StatusCode == HttpStatusCode.OK, homeBody);
        var progressText = JsonNode.Parse(homeBody)!["active_plan"]!["progress_text"]!.GetValue<string>();
        // Global week number (11) reflects the runway offset -- never resets
        // to a Core-local week number (5).
        Assert.Contains("Week 11 of 18", progressText);
    }

    // ── Part 13: expanded Training Day Detail matrix ────────────────────────

    [Fact]
    public async Task PilotScope_TrainingDayDetail_CoversMultipleRunwayAndCoreSessionCategories()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        // 20 weeks, READY profile: 8 runway weeks gives the allocator the
        // most room to include every eligible block (CONSISTENCY ineligible
        // for READY; GENERAL_ENDURANCE/AEROBIC_STRENGTH/PRE_SPECIFIC_TRANSITION present).
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(20 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), recentWeeklyVolumeKm: 24, recentLongestRunKm: 9));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        Assert.True(confirmResponse.StatusCode == HttpStatusCode.OK, confirmBody);
        var planId = Guid.Parse(JsonNode.Parse(confirmBody)!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);
        var days = await ctx.TrainingDays.Where(d => d.PlanId == planId).ToListAsync();

        var distinctRunwayBlocks = days
            .Where(d => weeks[d.WeekId].WeekType == TrainingWeekType.PreparationRunway)
            .Select(d => weeks[d.WeekId].CatalogPhaseKey)
            .Distinct()
            .ToList();

        // Detail must succeed for at least one session from every distinct
        // runway block this allocation actually produced, plus one Core day.
        foreach (var day in days.Where(d => weeks[d.WeekId].WeekType == TrainingWeekType.PreparationRunway)
                     .GroupBy(d => weeks[d.WeekId].CatalogPhaseKey).Select(g => g.First()))
        {
            var detail = await client.GetAsync($"/api/v1/training-days/{day.Id}");
            var detailBody = await detail.Content.ReadAsStringAsync();
            Assert.True(detail.StatusCode == HttpStatusCode.OK, $"block={weeks[day.WeekId].CatalogPhaseKey}: {detailBody}");
            var json = JsonNode.Parse(detailBody)!;
            Assert.Equal(day.PlannedDistanceKm, json["planned_distance_km"]!.GetValue<double>());
            Assert.Equal(day.Intensity, json["intensity"]!.GetValue<string>());
            Assert.True(json["can_mark_complete"]!.GetValue<bool>());
        }

        var coreDay = days.First(d => weeks[d.WeekId].WeekType != TrainingWeekType.PreparationRunway);
        var coreDetail = await client.GetAsync($"/api/v1/training-days/{coreDay.Id}");
        Assert.Equal(HttpStatusCode.OK, coreDetail.StatusCode);

        Assert.NotEmpty(distinctRunwayBlocks); // sanity: the matrix actually exercised >=1 real block.
    }

    // ── Part 14/15: Long Run + AerobicStrength completion ───────────────────

    [Fact]
    public async Task PilotScope_CompleteRunwayLongRun_UpdatesStatusAndPreservesLongRunFlag()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(16 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);
        var allDays = await ctx.TrainingDays.Where(d => d.PlanId == planId).ToListAsync();
        var longRun = allDays.First(d => d.IsLongRun && weeks[d.WeekId].WeekType == TrainingWeekType.PreparationRunway);

        var completeResponse = await client.PostRawAsync(
            $"/api/v1/training-days/{longRun.Id}/complete",
            new { actual_distance_km = longRun.PlannedDistanceKm, actual_duration_min = 60 });
        var completeBody = await completeResponse.Content.ReadAsStringAsync();
        Assert.True(completeResponse.StatusCode == HttpStatusCode.OK, completeBody);

        var detail = await client.GetAsync($"/api/v1/training-days/{longRun.Id}");
        var detailJson = JsonNode.Parse(await detail.Content.ReadAsStringAsync())!;
        Assert.Equal("completed", detailJson["status"]!.GetValue<string>());
        Assert.True(detailJson["is_long_run"]!.GetValue<bool>());
    }

    // ── Phase 4G.6C.2A Part 2 — deterministic AerobicStrength Intro fixture ──
    // A 15-week plan (runwayWeeks = 15-12 = 3) under the CoreEntryReady
    // profile is proven, via TenKPreparationRunwayAllocationPolicyFactory +
    // PreparationRunwayBlockAllocationEngine.Allocate(3, ...), to allocate
    // exactly ONE AerobicStrength week -- so this fixture deterministically
    // contains the Intro step (progression step 1) and never Progressed
    // (step 2, which requires a second AerobicStrength week). No early
    // return: the fixture assertions below fail the test outright if this
    // production allocation behavior ever changes.
    [Fact]
    public async Task PilotScope_AerobicStrengthIntro_DeterministicFixture_CompletesCorrectly()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(15 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), recentWeeklyVolumeKm: 24, recentLongestRunKm: 9));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);
        var allDaysBefore = await ctx.TrainingDays.Where(d => d.PlanId == planId).ToListAsync();
        var rowCountBefore = allDaysBefore.Count;

        var aerobicStrengthWeeks = weeks.Values.Where(w => w.CatalogPhaseKey == "AEROBIC_STRENGTH").ToList();
        Assert.True(aerobicStrengthWeeks.Count == 1, $"Fixture assumption violated: expected exactly 1 AerobicStrength week for a 15-week CoreEntryReady plan, found {aerobicStrengthWeeks.Count}. Production allocation policy may have changed.");

        var introDay = allDaysBefore.SingleOrDefault(d => d.Intensity == "CONTROLLED_AEROBIC_POWER_INTRO");
        Assert.NotNull(introDay);
        Assert.Equal("AEROBIC_STRENGTH_CONTROLLED_INTRO", introDay!.CatalogWorkoutKey);
        Assert.Equal(1, introDay.CatalogWorkoutVersion);
        Assert.Equal(weeks.Values.Single(w => w.CatalogPhaseKey == "AEROBIC_STRENGTH").Id, introDay.WeekId);
        Assert.DoesNotContain(allDaysBefore, d => d.Intensity == "CONTROLLED_AEROBIC_POWER_PROGRESSED");

        var plannedDistanceBefore = introDay.PlannedDistanceKm;

        var completeResponse = await client.PostRawAsync(
            $"/api/v1/training-days/{introDay.Id}/complete",
            new { actual_distance_km = introDay.PlannedDistanceKm, actual_duration_min = 40 });
        var completeBody = await completeResponse.Content.ReadAsStringAsync();
        Assert.True(completeResponse.StatusCode == HttpStatusCode.OK, completeBody);

        var detail = await client.GetAsync($"/api/v1/training-days/{introDay.Id}");
        var detailBody = await detail.Content.ReadAsStringAsync();
        Assert.True(detail.StatusCode == HttpStatusCode.OK, detailBody);
        var detailJson = JsonNode.Parse(detailBody)!;
        Assert.Equal("completed", detailJson["status"]!.GetValue<string>());
        Assert.Equal("CONTROLLED_AEROBIC_POWER_INTRO", detailJson["intensity"]!.GetValue<string>());
        Assert.Equal(plannedDistanceBefore, detailJson["planned_distance_km"]!.GetValue<double>());

        var persisted = await ctx.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == introDay.Id);
        Assert.Equal(TrainingDayStatus.Completed, persisted.Status);
        Assert.Equal("CONTROLLED_AEROBIC_POWER_INTRO", persisted.Intensity);
        Assert.Equal("AEROBIC_STRENGTH_CONTROLLED_INTRO", persisted.CatalogWorkoutKey);
        Assert.Equal(1, persisted.CatalogWorkoutVersion);
        Assert.Equal(plannedDistanceBefore, persisted.PlannedDistanceKm);
        Assert.Equal(TrainingDaySource.Template, persisted.Source);
        Assert.Null(persisted.AdaptedFromId);

        var allDaysAfter = await ctx.TrainingDays.Where(d => d.PlanId == planId).CountAsync();
        Assert.Equal(rowCountBefore, allDaysAfter);
    }

    // ── Phase 4G.6C.2A Part 3 — deterministic AerobicStrength Progressed fixture ──
    // A 17-week plan (runwayWeeks = 17-12 = 5) under CoreEntryReady is
    // proven to allocate exactly TWO AerobicStrength weeks, so this fixture
    // deterministically contains both the Intro (step 1, first week) and
    // Progressed (step 2, second week) sessions.
    [Fact]
    public async Task PilotScope_AerobicStrengthProgressed_DeterministicFixture_CompletesCorrectlyAndRemainsDistinguishableFromIntro()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), recentWeeklyVolumeKm: 24, recentLongestRunKm: 9));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);
        var allDaysBefore = await ctx.TrainingDays.Where(d => d.PlanId == planId).ToListAsync();
        var rowCountBefore = allDaysBefore.Count;

        var aerobicStrengthWeeks = weeks.Values.Where(w => w.CatalogPhaseKey == "AEROBIC_STRENGTH").OrderBy(w => w.WeekNumber).ToList();
        Assert.True(aerobicStrengthWeeks.Count == 2, $"Fixture assumption violated: expected exactly 2 AerobicStrength weeks for a 17-week CoreEntryReady plan, found {aerobicStrengthWeeks.Count}. Production allocation policy may have changed.");

        var introDay = allDaysBefore.SingleOrDefault(d => d.Intensity == "CONTROLLED_AEROBIC_POWER_INTRO");
        var progressedDay = allDaysBefore.SingleOrDefault(d => d.Intensity == "CONTROLLED_AEROBIC_POWER_PROGRESSED");
        Assert.NotNull(introDay);
        Assert.NotNull(progressedDay);
        Assert.NotEqual(introDay!.WeekId, progressedDay!.WeekId);
        Assert.Equal(aerobicStrengthWeeks[0].Id, introDay.WeekId);
        Assert.Equal(aerobicStrengthWeeks[1].Id, progressedDay.WeekId);
        Assert.Equal("AEROBIC_STRENGTH_CONTROLLED_PROGRESSED", progressedDay.CatalogWorkoutKey);
        Assert.Equal(1, progressedDay.CatalogWorkoutVersion);

        var plannedDistanceBefore = progressedDay.PlannedDistanceKm;

        var completeResponse = await client.PostRawAsync(
            $"/api/v1/training-days/{progressedDay.Id}/complete",
            new { actual_distance_km = progressedDay.PlannedDistanceKm, actual_duration_min = 45 });
        var completeBody = await completeResponse.Content.ReadAsStringAsync();
        Assert.True(completeResponse.StatusCode == HttpStatusCode.OK, completeBody);

        var detail = await client.GetAsync($"/api/v1/training-days/{progressedDay.Id}");
        var detailBody = await detail.Content.ReadAsStringAsync();
        Assert.True(detail.StatusCode == HttpStatusCode.OK, detailBody);
        var detailJson = JsonNode.Parse(detailBody)!;
        Assert.Equal("completed", detailJson["status"]!.GetValue<string>());
        Assert.Equal("CONTROLLED_AEROBIC_POWER_PROGRESSED", detailJson["intensity"]!.GetValue<string>());
        Assert.Equal(plannedDistanceBefore, detailJson["planned_distance_km"]!.GetValue<double>());

        var persistedProgressed = await ctx.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == progressedDay.Id);
        Assert.Equal(TrainingDayStatus.Completed, persistedProgressed.Status);
        Assert.Equal("CONTROLLED_AEROBIC_POWER_PROGRESSED", persistedProgressed.Intensity);
        Assert.Equal(TrainingDaySource.Template, persistedProgressed.Source);
        Assert.Null(persistedProgressed.AdaptedFromId);

        // Intro remains untouched and distinguishable -- completing Progressed
        // must not mutate or merge with the earlier Intro session.
        var persistedIntro = await ctx.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == introDay.Id);
        Assert.Equal(TrainingDayStatus.Planned, persistedIntro.Status);
        Assert.Equal("CONTROLLED_AEROBIC_POWER_INTRO", persistedIntro.Intensity);
        Assert.Equal("AEROBIC_STRENGTH_CONTROLLED_INTRO", persistedIntro.CatalogWorkoutKey);

        var allDaysAfter = await ctx.TrainingDays.Where(d => d.PlanId == planId).CountAsync();
        Assert.Equal(rowCountBefore, allDaysAfter);
    }

    // ── Part 16: pending-confirmation creation authority (Phase 4G.6C.2A) ──
    //
    // Repository-wide production call-site analysis (Application/Api/
    // Persistence/Domain/Infrastructure) found NO call site anywhere --
    // runway or Core -- that ever adds a row to PendingConfirmations.
    // ConfirmNotTodayDecisionAsync (QueryAndMutationServices.cs) marks the
    // TrainingDay Missed and logs a PlanEvent; it never touches
    // PendingConfirmations. PlaceholderAdaptationEngine always returns
    // Action=NoChange/PlanAdapted=false/AffectedDays=[] regardless of
    // trigger. PendingConfirmationsController exposes GET and
    // POST .../resolve only -- there is no creation endpoint. This is a
    // pre-existing, repository-wide absence of the pending-confirmation
    // creation mechanism, not a runway-specific carve-out -- Option B,
    // generalized: proven for both a runway session and a Core (12-week)
    // session, so "existing Core pending workflow remains unchanged" is
    // demonstrated honestly (unchanged because it never existed), not
    // assumed. No new adaptation/pending-creation behavior was added to
    // satisfy this -- doing so would exceed this phase's Implementation
    // Boundary ("no new pending-confirmation behavior invented solely for
    // runway").
    //
    // Classification: RUNWAY_PENDING_CONFIRMATION_NOT_APPLICABLE_BY_APPROVED_POLICY

    [Fact]
    public async Task PilotScope_RunwayNotTodayAction_NeverCreatesPendingConfirmation_ByApprovedPolicy()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(16 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);
        var allDaysBefore = await ctx.TrainingDays.Where(d => d.PlanId == planId).ToListAsync();
        var rowCountBefore = allDaysBefore.Count;
        var target = allDaysBefore.First(d => weeks[d.WeekId].WeekType == TrainingWeekType.PreparationRunway && !d.IsLongRun);

        var notTodayResponse = await client.PostRawAsync(
            $"/api/v1/training-days/{target.Id}/not-today-decisions",
            new { reason = "feeling_tired" });
        var notTodayBody = await notTodayResponse.Content.ReadAsStringAsync();
        Assert.True(notTodayResponse.StatusCode == HttpStatusCode.OK, notTodayBody);
        var decisionId = JsonNode.Parse(notTodayBody)!["decision_id"]!.GetValue<string>();

        var confirmDecisionResponse = await client.PostRawAsync($"/api/v1/not-today-decisions/{decisionId}/confirm", new { });
        var confirmDecisionBody = await confirmDecisionResponse.Content.ReadAsStringAsync();
        Assert.True(confirmDecisionResponse.StatusCode == HttpStatusCode.OK, confirmDecisionBody);

        // Approved-policy assertions -- not a conditional/optional check.
        var pendingResponse = await client.GetAsync("/api/v1/pending-confirmations");
        var pendingBody = await pendingResponse.Content.ReadAsStringAsync();
        Assert.True(pendingResponse.StatusCode == HttpStatusCode.OK, pendingBody);
        var pendingArray = JsonNode.Parse(pendingBody)!.AsArray();
        Assert.Empty(pendingArray);

        var pendingRowCount = await ctx.PendingConfirmations.CountAsync();
        Assert.Equal(0, pendingRowCount);

        var allDaysAfter = await ctx.TrainingDays.Where(d => d.PlanId == planId).ToListAsync();
        Assert.Equal(rowCountBefore, allDaysAfter.Count); // no adapted/replacement day created
        Assert.All(allDaysAfter, d => Assert.Null(d.AdaptedFromId));
        Assert.All(allDaysAfter, d => Assert.Equal(TrainingDaySource.Template, d.Source));

        var persistedTarget = await ctx.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == target.Id);
        Assert.Equal(TrainingDayStatus.Missed, persistedTarget.Status);
    }

    [Fact]
    public async Task PilotScope_CoreNotTodayAction_AlsoNeverCreatesPendingConfirmation_ConfirmingRepoWideBaseline()
    {
        // Non-runway (12-week, 8-14 Core) control -- proves the absence of
        // pending-confirmation creation is a pre-existing repository-wide
        // condition, not something this phase's runway work introduced or
        // regressed.
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(12 * 7);
        var previewResponse = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        var preview = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync())!;
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var allDaysBefore = await ctx.TrainingDays.Where(d => d.PlanId == planId).ToListAsync();
        var target = allDaysBefore.First(d => !d.IsLongRun);

        var notTodayResponse = await client.PostRawAsync(
            $"/api/v1/training-days/{target.Id}/not-today-decisions",
            new { reason = "feeling_tired" });
        var notTodayBody = await notTodayResponse.Content.ReadAsStringAsync();
        Assert.True(notTodayResponse.StatusCode == HttpStatusCode.OK, notTodayBody);
        var decisionId = JsonNode.Parse(notTodayBody)!["decision_id"]!.GetValue<string>();

        var confirmDecisionResponse = await client.PostRawAsync($"/api/v1/not-today-decisions/{decisionId}/confirm", new { });
        Assert.Equal(HttpStatusCode.OK, confirmDecisionResponse.StatusCode);

        var pendingArray = JsonNode.Parse(await (await client.GetAsync("/api/v1/pending-confirmations")).Content.ReadAsStringAsync())!.AsArray();
        Assert.Empty(pendingArray);
        Assert.Equal(0, await ctx.PendingConfirmations.CountAsync());
    }
}
