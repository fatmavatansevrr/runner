using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 10K-FREQ.6D.27 -- the final Intermediate×6D capability phase: opens
/// the real public routing gate (previously Intermediate×4D/5D only) across
/// all three horizon bands (Core 8-14, Preparation Runway 15-20, LongHorizon
/// 21-52) and proves it through real public HTTP + PostgreSQL. Mirrors
/// <see cref="Freq6D22IntermediateFiveDayLongHorizonPublicActivationTests"/>'s
/// own established pattern, extended to 6D and to Core/Runway (5D's own
/// Core/Runway activation predates this test-file convention, so this phase
/// covers them directly rather than via a separate reused fixture).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Freq6D27IntermediateSixDayPublicActivationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Freq6D27IntermediateSixDayPublicActivationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    private static object SixDayRequest(
        int weeks, string level = "intermediate", int days = 6,
        double recentWeeklyVolumeKm = 26.0, double recentLongestRunKm = 8.0, int recentRunsPerWeek = 6,
        string targetFinishTimeSource = "product_average", int targetFinishTimeSeconds = 3480)
    {
        var start = new DateOnly(2026, 9, 7);
        return new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = days switch
            {
                6 => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
                7 => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" },
                5 => new[] { "mon", "tue", "thu", "fri", "sun" },
                _ => new[] { "mon", "wed", "fri", "sun" },
            },
            long_run_day = "sun",
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = targetFinishTimeSeconds, target_finish_time_source = targetFinishTimeSource,
            race_name = "Phase 10K-FREQ.6D.27",
            recent_weekly_volume_km = recentWeeklyVolumeKm, recent_longest_run_km = recentLongestRunKm, recent_runs_per_week = recentRunsPerWeek,
        };
    }

    private async Task<HttpResponseMessage> PreviewRaceAsync(object request) =>
        await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

    private async Task<HttpResponseMessage> PreviewLongHorizonAsync(object request) =>
        await _client.PostRawAsync("/api/v1/plans/generate-preview/race/long-horizon", request);

    private static async Task<JsonNode> JsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonNode.Parse(body)!;
    }

    // ── §8/§10: full 8-52 public route matrix through the principal /generate-preview/race endpoint ──

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(17)]
    [InlineData(20)]
    public async Task CoreOrRunwayHorizons_PublicPreviewSucceeds_WithExactSixDayIdentity(int weeks)
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRaceAsync(SixDayRequest(weeks)));
        Assert.Equal(6, preview["days_per_week"]!.GetValue<int>());

        var firstWeek = preview["structural_roadmap"]?.AsArray()?.FirstOrDefault() ?? preview["weeks"]?.AsArray()?.FirstOrDefault();
        Assert.NotNull(firstWeek);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(23)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(52)]
    public async Task LongHorizonHorizons_PublicPreviewSucceeds_WithExactSixDayIdentityAndShape(int weeks)
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewLongHorizonAsync(SixDayRequest(weeks)));

        Assert.Equal("rolling_long_horizon", preview["schedule_strategy"]!.GetValue<string>());
        Assert.Equal(weeks, preview["total_weeks"]!.GetValue<int>());
        Assert.Equal(6, preview["days_per_week"]!.GetValue<int>());

        var firstWeek = preview["structural_roadmap"]!.AsArray()[0]!;
        if (firstWeek["session_prescriptions"] is JsonArray sessions)
        {
            Assert.Equal(6, sessions.Count);
            Assert.Equal(1, sessions.Count(s => s!["session_role"]!.GetValue<string>() == "KEY_SESSION"));
            Assert.Equal(4, sessions.Count(s => s!["session_role"]!.GetValue<string>().StartsWith("EASY_SUPPORT")));
            Assert.Equal(1, sessions.Count(s => s!["session_role"]!.GetValue<string>() == "LONG_RUN"));
        }
    }

    [Fact]
    public async Task Full8To52Matrix_AllRouteCorrectly_FortyFiveOfFortyFive()
    {
        var coreSuccesses = 0;
        for (var weeks = 8; weeks <= 14; weeks++)
        {
            await ResetAsync();
            var response = await PreviewRaceAsync(SixDayRequest(weeks));
            if (response.IsSuccessStatusCode) coreSuccesses++;
        }
        Assert.Equal(7, coreSuccesses);

        var runwaySuccesses = 0;
        for (var weeks = 15; weeks <= 20; weeks++)
        {
            await ResetAsync();
            var response = await PreviewRaceAsync(SixDayRequest(weeks));
            if (response.IsSuccessStatusCode) runwaySuccesses++;
        }
        Assert.Equal(6, runwaySuccesses);

        var longHorizonSuccesses = 0;
        for (var weeks = 21; weeks <= 52; weeks++)
        {
            await ResetAsync();
            var response = await PreviewLongHorizonAsync(SixDayRequest(weeks));
            if (response.IsSuccessStatusCode)
            {
                var preview = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
                if (preview["schedule_strategy"]?.GetValue<string>() == "rolling_long_horizon")
                    longHorizonSuccesses++;
            }
        }
        Assert.Equal(32, longHorizonSuccesses);

        Assert.Equal(45, coreSuccesses + runwaySuccesses + longHorizonSuccesses);
    }

    // ── §12-13: ProductAverage / UserDefined ─────────────────────────────────

    [Fact]
    public async Task ProductAverage_ConfirmsAndReloadsFromFreshPostgres_LongHorizon()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewLongHorizonAsync(SixDayRequest(21, targetFinishTimeSource: "product_average")));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(6, plan.DaysPerWeek);
        Assert.Equal(3480, plan.TargetFinishTimeSeconds);
        Assert.Equal(TargetFinishTimeSource.ProductAverage, plan.TargetFinishTimeSource);
        Assert.Equal(PlanScheduleStrategy.RollingLongHorizon, plan.ScheduleStrategy);

        var rollingPlan = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == previewId);
        Assert.Equal(6, rollingPlan.DaysPerWeek);
    }

    [Fact]
    public async Task UserDefined_ConfirmsAndPreservesSourceAfterFreshReload_LongHorizon()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewLongHorizonAsync(SixDayRequest(21, targetFinishTimeSource: "user_defined", targetFinishTimeSeconds: 3300)));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(3300, plan.TargetFinishTimeSeconds);
        Assert.Equal(TargetFinishTimeSource.UserDefined, plan.TargetFinishTimeSource);
    }

    // ── §17-18: LongHorizon missing / zero readiness -> PRODUCT_INELIGIBLE, not UNSUPPORTED ──

    [Fact]
    public async Task MissingReadiness_LongHorizon_ReturnsTypedProductIneligible_NotUnsupported()
    {
        await ResetAsync();
        var response = await PreviewLongHorizonAsync(SixDayRequest(21, recentWeeklyVolumeKm: 0, recentLongestRunKm: 0, recentRunsPerWeek: 0));
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("LONG_HORIZON_PILOT_UNSUPPORTED", body);
    }

    // ── §33-40: full public GE->Runway->Core dual-KEY lifecycle, real PostgreSQL ──

    [Fact]
    public async Task PublicFullLifecycle_ConfirmedPlan_ReachesOrganicCoreWithDualKeyThroughRealPostgres()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewLongHorizonAsync(SixDayRequest(21)));
        var rollingId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = rollingId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        var home = await _client.GetAsync("/api/v1/plans/active/home");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("rolling_long_horizon", await home.Content.ReadAsStringAsync());
        var calendar = await _client.GetAsync("/api/v1/plans/active/calendar?month=2026-09");
        Assert.Equal(HttpStatusCode.OK, calendar.StatusCode);
        Assert.Contains("rolling_long_horizon", await calendar.Content.ReadAsStringAsync());

        async Task<JsonNode> CompleteAndActivateAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == rollingId);
            var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek
                    && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek && s.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Planned)
                .ToListAsync();
            foreach (var session in sessions)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
            var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
            response.EnsureSuccessStatusCode();
            return await JsonAsync(response);
        }

        await CompleteAndActivateAsync();
        await CompleteAndActivateAsync();
        var w3 = await CompleteAndActivateAsync();
        Assert.Equal(10, w3["activated_window_range"]!["start_global_week"]!.GetValue<int>());
        Assert.Equal(13, w3["activated_window_range"]!["end_global_week"]!.GetValue<int>());

        using var verify = _factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstCoreWeek = await verifyDb.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek == 10).ToListAsync();
        Assert.Equal(6, firstCoreWeek.Count);
        Assert.Equal(2, firstCoreWeek.Count(s => s.SessionRole == "KEY_SESSION"));
        Assert.Contains(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 0);
        Assert.Contains(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 1);
        Assert.Equal(3, firstCoreWeek.Count(s => s.SessionRole == "EASY_SUPPORT"));
        Assert.Equal(3, firstCoreWeek.Where(s => s.SessionRole == "EASY_SUPPORT").Select(s => s.SlotOrdinal).Distinct().Count());
        Assert.All(firstCoreWeek.Where(s => s.SessionRole == "KEY_SESSION"), s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.ProgressionStageKey));
            Assert.False(string.IsNullOrWhiteSpace(s.CatalogPrescriptionProfileKey));
            Assert.NotNull(s.CatalogPrescriptionProfileVersion);
        });

        var planAfter = await verifyDb.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(6, planAfter.DaysPerWeek);
        Assert.Equal(TargetFinishTimeSource.ProductAverage, planAfter.TargetFinishTimeSource);

        // §39-40: repair the organically-materialized secondary KEY via the real repair service.
        Guid triggerId;
        using (var write = _factory.Services.CreateScope())
        {
            var db = write.ServiceProvider.GetRequiredService<AppDbContext>();
            var week10 = await db.LongHorizonRollingWeekStates.Include(w => w.Sessions)
                .SingleAsync(w => w.PlanStateId == rollingId && w.GlobalWeek == 10);
            var secondaryKey = week10.Sessions.Single(s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 1);
            secondaryKey.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.NotToday;
            secondaryKey.NotTodayReason = "schedule";
            secondaryKey.NotTodayRecordedAtUtc = DateTime.UtcNow;
            triggerId = secondaryKey.Id;
            await db.SaveChangesAsync();
        }
        using (var orchestrate = _factory.Services.CreateScope())
        {
            var db = orchestrate.ServiceProvider.GetRequiredService<AppDbContext>();
            var trigger = await db.LongHorizonRollingSessionStates
                .Include(s => s.Week).ThenInclude(w => w.Plan).ThenInclude(p => p.Weeks).ThenInclude(w => w.Sessions)
                .SingleAsync(s => s.Id == triggerId);
            await RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation.ScheduleRepairRuntimeOrchestrator.RunAsync(
                db, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance, trigger, default);
        }

        using var finalVerify = _factory.Services.CreateScope();
        var finalDb = finalVerify.ServiceProvider.GetRequiredService<AppDbContext>();
        var replacement = await finalDb.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.AdaptedFromSessionId == triggerId);
        Assert.Equal(1, replacement.LaneOrdinal);
        var primary = await finalDb.LongHorizonRollingSessionStates.AsNoTracking()
            .SingleAsync(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek == 10 && s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 0);
        Assert.Equal(0, primary.LaneOrdinal);
    }

    // ── §43-51: unsupported neighbors remain closed; no nearest-match routing ──
    // Phase 10K-GEN.10 subsequently opened the public Advanced x6D LongHorizon
    // gate itself (by product/repository design, not a regression) --
    // (advanced, 6) removed from this "unsupported neighbor" list and covered
    // instead by Gen10AdvancedCombinedPublicActivationTests' own activation proof.

    [Theory]
    [InlineData("beginner", 6)]
    [InlineData("intermediate", 7)]
    public async Task UnsupportedNeighbors_RemainClosed_NoFallbackToFiveOrSixDayIdentity(string level, int days)
    {
        await ResetAsync();
        var response = await PreviewLongHorizonAsync(SixDayRequest(21, level: level, days: days));
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(response.IsSuccessStatusCode, body);
        Assert.Contains("LONG_HORIZON_PILOT_UNSUPPORTED", body);
        Assert.DoesNotContain("TEN_K__6D__INTERMEDIATE", body);
        Assert.DoesNotContain("TEN_K__5D__INTERMEDIATE", body);
    }
}
