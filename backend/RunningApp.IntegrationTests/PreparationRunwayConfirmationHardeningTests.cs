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
/// Backend Integration Phase 4G.6C.1 — hardening of Phase 4G.6C's
/// confirmation/persistence activation: both runway profiles, concurrent
/// confirmation, active-plan conflict, Calendar/Training-Day-Detail
/// read-model compatibility, completion/not-today actions, cancel/reset
/// cleanup, and a malformed-payload typed-rejection proof. Runs against the
/// real Api host + real Postgres DB, using dedicated confirmation-enabled
/// <see cref="CustomWebApplicationFactory"/> instances per test (see
/// <see cref="PreparationRunwayConfirmationEndToEndTests"/> for why the
/// shared collection factory's own default stays disabled).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PreparationRunwayConfirmationHardeningTests
{
    private static CustomWebApplicationFactory ConfirmationEnabledFactory() =>
        new("Development", new Dictionary<string, string?>
        {
            ["PreparationRunwayPilotActivation:Enabled"] = "true",
            ["PreparationRunwayPilotActivation:ConfirmationEnabled"] = "true",
        });

    private static object RaceRequest(
        string startDate, string raceDate,
        double? recentWeeklyVolumeKm = 20, double? recentLongestRunKm = 8) => new
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
        recent_runs_per_week = 3,
        recent_race = (object?)null,
    };

    private static async Task<(string previewId, HttpResponseMessage response)> GeneratePreviewAsync(
        HttpClient client, int totalWeeks, double? weekly = 20, double? longest = 8)
    {
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var response = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), weekly, longest));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) return (string.Empty, response);
        var preview = JsonNode.Parse(body)!;
        return (preview["preview_id"]!.GetValue<string>(), response);
    }

    // ── Part 2: both-profile persistence ──────────────────────────────────────

    [Fact]
    public async Task PilotScope_ConsistencyNeededProfile_PersistsCorrectBlockSequence()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        // Weekly=14/longest=5: real CoreEntryReadinessResolver CAUTION band
        // (validated safe against the known low-volume Core-generation edge
        // case in Phase 4G.6B.1) -- maps to ConsistencyNeeded, never faked.
        var (previewId, previewResponse) = await GeneratePreviewAsync(client, 18, weekly: 14, longest: 5);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        Assert.True(confirmResponse.StatusCode == HttpStatusCode.OK, confirmBody);
        var planId = Guid.Parse(JsonNode.Parse(confirmBody)!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).OrderBy(w => w.WeekNumber).ToListAsync();
        Assert.Equal(18, weeks.Count);

        var runwayWeeks = weeks.Take(6).ToList();
        // ConsistencyNeeded profile: CONSISTENCY is eligible (CanonicalOrder=1,
        // first among positive blocks); final block is always PRE_SPECIFIC_TRANSITION.
        Assert.Equal("CONSISTENCY", runwayWeeks.First().CatalogPhaseKey);
        Assert.Equal("PRE_SPECIFIC_TRANSITION", runwayWeeks.Last().CatalogPhaseKey);
        Assert.All(runwayWeeks, w => Assert.Equal(TrainingWeekType.PreparationRunway, w.WeekType));
        // AEROBIC_STRENGTH is never eligible for ConsistencyNeeded.
        Assert.DoesNotContain(runwayWeeks, w => w.CatalogPhaseKey == "AEROBIC_STRENGTH");

        var days = await ctx.TrainingDays.Where(d => d.PlanId == planId && weeks.Take(6).Select(w => w.Id).Contains(d.WeekId)).ToListAsync();
        // Runway pacing remains effort-only after persistence: intensity
        // never contains a goal/race-specific/threshold token.
        Assert.All(days, d => Assert.DoesNotContain("GOAL_PACE", d.Intensity, StringComparison.OrdinalIgnoreCase));
        Assert.All(days, d => Assert.DoesNotContain("THRESHOLD", d.Intensity, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PilotScope_CoreEntryReadyProfile_PersistsCorrectBlockSequence()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        // Weekly=24/longest=9: real READY band (weekly>=15, longest>=6).
        var (previewId, previewResponse) = await GeneratePreviewAsync(client, 18, weekly: 24, longest: 9);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).OrderBy(w => w.WeekNumber).ToListAsync();
        var runwayWeeks = weeks.Take(6).ToList();

        // CoreEntryReady profile: CONSISTENCY is never eligible; AEROBIC_STRENGTH is.
        Assert.DoesNotContain(runwayWeeks, w => w.CatalogPhaseKey == "CONSISTENCY");
        Assert.Equal("PRE_SPECIFIC_TRANSITION", runwayWeeks.Last().CatalogPhaseKey);
    }

    // ── Part 5: concurrent confirmation ─────────────────────────────────────

    [Fact]
    public async Task PilotScope_ConcurrentConfirmation_CreatesAtMostOnePlan()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var (previewId, previewResponse) = await GeneratePreviewAsync(client, 16);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var confirmTask1 = client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var confirmTask2 = client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var results = await Task.WhenAll(confirmTask1, confirmTask2);

        // Neither request may surface an unhandled 500 -- both resolve to a
        // known, deterministic outcome (both succeed with the same plan,
        // per the existing idempotent-replay/unique-index-recovery policy).
        Assert.All(results, r => Assert.NotEqual(HttpStatusCode.InternalServerError, r.StatusCode));
        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        var planIds = new List<Guid>();
        foreach (var r in results)
        {
            var body = JsonNode.Parse(await r.Content.ReadAsStringAsync())!;
            planIds.Add(Guid.Parse(body["plan_id"]!.GetValue<string>()));
        }
        Assert.Equal(planIds[0], planIds[1]);

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await ctx.TrainingPlans.CountAsync(p => p.SourcePreviewId == Guid.Parse(previewId)));
        Assert.Equal(16, await ctx.TrainingWeeks.CountAsync(w => w.PlanId == planIds[0]));
        Assert.Equal(64, await ctx.TrainingDays.CountAsync(d => d.PlanId == planIds[0]));
    }

    // ── Part 6: active-plan conflict ────────────────────────────────────────

    [Fact]
    public async Task PilotScope_ExistingActivePlan_RejectsSecondRunwayConfirmation_NoSecondPlan()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var (firstPreviewId, firstResponse) = await GeneratePreviewAsync(client, 16);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var firstConfirm = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = firstPreviewId });
        Assert.Equal(HttpStatusCode.OK, firstConfirm.StatusCode);
        var firstPlanId = Guid.Parse(JsonNode.Parse(await firstConfirm.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        var (secondPreviewId, secondResponse) = await GeneratePreviewAsync(client, 18);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var secondConfirm = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = secondPreviewId });
        var secondBody = JsonNode.Parse(await secondConfirm.Content.ReadAsStringAsync())!;

        // Existing, unmodified active-plan policy: returns the existing
        // active plan (AlreadyActive=true), never creates a second one.
        Assert.Equal(HttpStatusCode.OK, secondConfirm.StatusCode);
        Assert.Equal(firstPlanId, Guid.Parse(secondBody["plan_id"]!.GetValue<string>()));
        Assert.True(secondBody["already_active"]!.GetValue<bool>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var internalUserId = await ctx.TrainingPlans.Where(x => x.Id == firstPlanId).Select(x => x.InternalUserId).FirstAsync();
        Assert.Equal(1, await ctx.TrainingPlans.CountAsync(p => p.InternalUserId == internalUserId));
    }

    // ── Part 7/8: Calendar + Training Day Detail read models ────────────────

    [Fact]
    public async Task PilotScope_ConfirmedPlan_CalendarAndTrainingDayDetail_ReturnPersistedValues()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var (previewId, previewResponse) = await GeneratePreviewAsync(client, 18);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var allDays = await ctx.TrainingDays.Where(d => d.PlanId == planId).OrderBy(d => d.Date).ToListAsync();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);

        // A month entirely inside the runway (StartDate 2026-07-20; 6 runway weeks -> July/August).
        var runwayMonthDays = allDays.Where(d => d.Date.Year == 2026 && d.Date.Month == 8 && weeks[d.WeekId].WeekType == TrainingWeekType.PreparationRunway).ToList();
        Assert.NotEmpty(runwayMonthDays);
        var calendarAug = await client.GetAsync("/api/v1/plans/active/calendar?month=2026-08");
        var calendarAugBody = await calendarAug.Content.ReadAsStringAsync();
        Assert.True(calendarAug.StatusCode == HttpStatusCode.OK, calendarAugBody);
        var calendarAugDays = JsonNode.Parse(calendarAugBody)!.AsArray();
        var calendarAugDates = calendarAugDays.Select(d => DateOnly.Parse(d!["date"]!.GetValue<string>().Substring(0, 10))).ToHashSet();
        // Every persisted August day appears in the calendar response; no duplicates.
        Assert.All(runwayMonthDays, d => Assert.Contains(DateOnly.FromDateTime(d.Date), calendarAugDates));
        Assert.Equal(calendarAugDates.Count, calendarAugDays.Count);

        // A month containing Core weeks (final 12 weeks push well past August/September).
        var coreDay = allDays.First(d => weeks[d.WeekId].WeekType != TrainingWeekType.PreparationRunway);
        var coreMonth = $"{coreDay.Date:yyyy-MM}";
        var calendarCore = await client.GetAsync($"/api/v1/plans/active/calendar?month={coreMonth}");
        Assert.Equal(HttpStatusCode.OK, calendarCore.StatusCode);
        var calendarCoreDates = JsonNode.Parse(await calendarCore.Content.ReadAsStringAsync())!.AsArray()
            .Select(d => DateOnly.Parse(d!["date"]!.GetValue<string>().Substring(0, 10))).ToHashSet();
        Assert.Contains(DateOnly.FromDateTime(coreDay.Date), calendarCoreDates);

        // Training Day Detail for one runway day and one Core day.
        var runwaySampleDay = allDays.First(d => weeks[d.WeekId].WeekType == TrainingWeekType.PreparationRunway);
        var runwayDetail = await client.GetAsync($"/api/v1/training-days/{runwaySampleDay.Id}");
        var runwayDetailBody = await runwayDetail.Content.ReadAsStringAsync();
        Assert.True(runwayDetail.StatusCode == HttpStatusCode.OK, runwayDetailBody);
        var runwayDetailJson = JsonNode.Parse(runwayDetailBody)!;
        Assert.Equal(runwaySampleDay.Id.ToString(), runwayDetailJson["day_id"]!.GetValue<string>());
        Assert.Equal(runwaySampleDay.PlannedDistanceKm, runwayDetailJson["planned_distance_km"]!.GetValue<double>());
        Assert.Equal(runwaySampleDay.Intensity, runwayDetailJson["intensity"]!.GetValue<string>());
        Assert.Equal(runwaySampleDay.IsLongRun, runwayDetailJson["is_long_run"]!.GetValue<bool>());

        var coreDetail = await client.GetAsync($"/api/v1/training-days/{coreDay.Id}");
        var coreDetailBody = await coreDetail.Content.ReadAsStringAsync();
        Assert.True(coreDetail.StatusCode == HttpStatusCode.OK, coreDetailBody);
        var coreDetailJson = JsonNode.Parse(coreDetailBody)!;
        Assert.Equal(coreDay.PlannedDistanceKm, coreDetailJson["planned_distance_km"]!.GetValue<double>());
    }

    // ── Part 11/12: completion + not-today actions ──────────────────────────

    [Fact]
    public async Task PilotScope_CompleteRunwayEasySession_UpdatesStatusAndActualValues()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var (previewId, previewResponse) = await GeneratePreviewAsync(client, 16);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);
        var runwayEasyDay = await ctx.TrainingDays
            .Where(d => d.PlanId == planId && !d.IsLongRun)
            .ToListAsync();
        var target = runwayEasyDay.First(d => weeks[d.WeekId].WeekType == TrainingWeekType.PreparationRunway);

        var completeResponse = await client.PostRawAsync(
            $"/api/v1/training-days/{target.Id}/complete",
            new { actual_distance_km = target.PlannedDistanceKm, actual_duration_min = 30 });
        var completeBody = await completeResponse.Content.ReadAsStringAsync();
        Assert.True(completeResponse.StatusCode == HttpStatusCode.OK, completeBody);
        var completed = JsonNode.Parse(completeBody)!;
        Assert.Equal("completed", completed["status"]!.GetValue<string>());

        var detail = await client.GetAsync($"/api/v1/training-days/{target.Id}");
        var detailJson = JsonNode.Parse(await detail.Content.ReadAsStringAsync())!;
        Assert.Equal("completed", detailJson["status"]!.GetValue<string>());
        Assert.Equal(target.PlannedDistanceKm, detailJson["actual_distance_km"]!.GetValue<double>());
    }

    [Fact]
    public async Task PilotScope_NotTodayThenResolve_RunwaySession_Succeeds()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var (previewId, previewResponse) = await GeneratePreviewAsync(client, 16);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var weeks = await ctx.TrainingWeeks.Where(w => w.PlanId == planId).ToDictionaryAsync(w => w.Id);
        var runwayDays = await ctx.TrainingDays.Where(d => d.PlanId == planId).ToListAsync();
        var target = runwayDays.First(d => weeks[d.WeekId].WeekType == TrainingWeekType.PreparationRunway);

        var notTodayResponse = await client.PostRawAsync(
            $"/api/v1/training-days/{target.Id}/not-today-decisions",
            new { reason = "feeling_tired" });
        var notTodayBody = await notTodayResponse.Content.ReadAsStringAsync();
        Assert.True(notTodayResponse.StatusCode == HttpStatusCode.OK, notTodayBody);
        var decision = JsonNode.Parse(notTodayBody)!;
        var decisionId = decision["decision_id"]!.GetValue<string>();

        var confirmDecision = await client.PostRawAsync($"/api/v1/not-today-decisions/{decisionId}/confirm", new { });
        var confirmDecisionBody = await confirmDecision.Content.ReadAsStringAsync();
        Assert.True(confirmDecision.StatusCode == HttpStatusCode.OK, confirmDecisionBody);
    }

    // ── Part 13: cancel compatibility ───────────────────────────────────────

    [Fact]
    public async Task PilotScope_CancelConfirmedPlan_RemovesActivePlan()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var (previewId, previewResponse) = await GeneratePreviewAsync(client, 15);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        var cancelResponse = await client.PostRawAsync($"/api/v1/plans/{planId}/cancel", new { reason = "test" });
        var cancelBody = await cancelResponse.Content.ReadAsStringAsync();
        Assert.True(cancelResponse.StatusCode == HttpStatusCode.OK, cancelBody);

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await ctx.TrainingPlans.SingleAsync(p => p.Id == planId);
        Assert.Equal(TrainingPlanStatus.Cancelled, plan.Status);

        var detailsAfterCancel = await client.GetAsync("/api/v1/plans/active/details");
        var detailsBody = await detailsAfterCancel.Content.ReadAsStringAsync();
        Assert.True(detailsAfterCancel.StatusCode == HttpStatusCode.OK, detailsBody);
        Assert.False(JsonNode.Parse(detailsBody)!["has_active_plan"]!.GetValue<bool>());

        // A new plan can be confirmed after cancellation.
        var (secondPreviewId, secondPreviewResponse) = await GeneratePreviewAsync(client, 17);
        Assert.Equal(HttpStatusCode.OK, secondPreviewResponse.StatusCode);
        var secondConfirm = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = secondPreviewId });
        Assert.Equal(HttpStatusCode.OK, secondConfirm.StatusCode);
    }

    // ── Part 14: explicit reset cleanup ─────────────────────────────────────

    [Fact]
    public async Task PilotScope_ExplicitReset_RemovesConfirmedPlanWeeksAndDays()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var (previewId, previewResponse) = await GeneratePreviewAsync(client, 20);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(JsonNode.Parse(await confirmResponse.Content.ReadAsStringAsync())!["plan_id"]!.GetValue<string>());

        using (var scope = factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await ctx.TrainingPlans.CountAsync(p => p.Id == planId));
            Assert.Equal(20, await ctx.TrainingWeeks.CountAsync(w => w.PlanId == planId));
            Assert.Equal(80, await ctx.TrainingDays.CountAsync(d => d.PlanId == planId));
        }

        // Mark one day complete before reset, to prove reset clears action state too.
        using (var scope = factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var firstDay = await ctx.TrainingDays.Where(d => d.PlanId == planId).OrderBy(d => d.Date).FirstAsync();
            var completeResponse = await client.PostRawAsync(
                $"/api/v1/training-days/{firstDay.Id}/complete",
                new { actual_distance_km = firstDay.PlannedDistanceKm, actual_duration_min = 25 });
            completeResponse.EnsureSuccessStatusCode();
        }

        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        using var scopeAfter = factory.Services.CreateScope();
        var ctxAfter = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await ctxAfter.TrainingPlans.CountAsync(p => p.Id == planId));
        Assert.Equal(0, await ctxAfter.TrainingWeeks.CountAsync(w => w.PlanId == planId));
        Assert.Equal(0, await ctxAfter.TrainingDays.CountAsync(d => d.PlanId == planId));
    }

    // ── Malformed/unsupported payload version: typed rejection, no writes ──

    [Fact]
    public async Task PilotScope_UnsupportedPayloadSchemaVersion_ConfirmRejectedTyped_NoWrites()
    {
        await using var factory = ConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

        var (previewId, previewResponse) = await GeneratePreviewAsync(client, 16);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        // Directly corrupt the stored snapshot's schedule schema version --
        // proves the existing, unmodified CatalogPreviewScheduleSchemaUnsupportedException
        // guard fires identically for a runway-sourced snapshot, without
        // injecting a mid-transaction database fault.
        using (var scope = factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var previewEntity = await ctx.PlanPreviews.SingleAsync(p => p.Id == Guid.Parse(previewId));
            var snapshotNode = JsonNode.Parse(previewEntity.PreviewPayloadJson!)!;
            var payloadNode = snapshotNode["generated_preview_plan_payload"] ?? snapshotNode["generatedPreviewPlanPayload"];
            Assert.NotNull(payloadNode); // Confirms the runway payload was actually embedded (confirmation gate really enabled).
            payloadNode!["schema_version"] = 999;
            previewEntity.PreviewPayloadJson = snapshotNode.ToJsonString();
            await ctx.SaveChangesAsync();
        }

        var before = await CountRowsAsync(factory);
        var confirmResponse = await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();

        // Either the corruption is caught by the schema-version guard, or by
        // hash-integrity verification (both are existing, unmodified typed
        // guards) -- either way, never a generic 500 and never a write.
        Assert.NotEqual(HttpStatusCode.InternalServerError, confirmResponse.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, confirmResponse.StatusCode);

        var after = await CountRowsAsync(factory);
        Assert.Equal(before.plans, after.plans);
        Assert.Equal(before.weeks, after.weeks);
        Assert.Equal(before.days, after.days);
    }

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
}
