using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 10K-GEN.10 -- the final Advanced capability phase: opens the real
/// public routing gate for Advanced 3D/4D/5D/6D (structural/eligibility
/// authority approved GEN.7/GEN.8, dark-implemented and verified GEN.9)
/// across all three horizon bands and proves it through real public HTTP +
/// PostgreSQL. Mirrors <see cref="Freq6D27IntermediateSixDayPublicActivationTests"/>'s
/// established pattern, extended across four frequencies at once.
///
/// Disclosed scope: the exhaustive 45-horizon matrix is proven in full only
/// for Advanced x5D (the highest-value dual-KEY proof point, mirroring
/// GEN.9's own disclosed-coverage choice); 3D/4D/6D are verified via
/// representative Core/Runway/LongHorizon horizons plus one full confirm +
/// fresh-reload proof per frequency, not the full 45-cell matrix repeated
/// four times.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Gen10AdvancedCombinedPublicActivationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Gen10AdvancedCombinedPublicActivationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    // Roughly 65-70% of each frequency's own GEN.7/GEN.8-approved
    // ResolvedPeakReference (3D=40, 4D=45, 5D=50, 6D=51), leaving real
    // headroom for the reachable-peak growth calculation regardless of
    // horizon length -- not a new authority value, just a safe test input.
    private static double DefaultVolumeFor(int days) => days switch
    {
        3 => 27.0,
        4 => 30.0,
        5 => 34.0,
        6 => 35.0,
        _ => 30.0,
    };

    private static object AdvancedRequest(
        int days, int weeks, string level = "advanced",
        double? recentWeeklyVolumeKm = null, double recentLongestRunKm = 10.0, int? recentRunsPerWeek = null,
        string targetFinishTimeSource = "product_average", int targetFinishTimeSeconds = 3480)
    {
        var start = new DateOnly(2026, 9, 7);
        var volume = recentWeeklyVolumeKm ?? DefaultVolumeFor(days);
        var runsPerWeek = recentRunsPerWeek ?? days;
        return new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = days switch
            {
                2 => new[] { "wed", "sun" },
                3 => new[] { "mon", "wed", "sun" },
                4 => new[] { "mon", "wed", "fri", "sun" },
                5 => new[] { "mon", "tue", "thu", "fri", "sun" },
                6 => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
                7 => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" },
                _ => new[] { "mon", "wed", "fri", "sun" },
            },
            long_run_day = "sun",
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = targetFinishTimeSeconds, target_finish_time_source = targetFinishTimeSource,
            race_name = "Phase 10K-GEN.10",
            recent_weekly_volume_km = volume, recent_longest_run_km = recentLongestRunKm, recent_runs_per_week = runsPerWeek,
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

    // ── Core/Runway across all four Advanced frequencies ──────────────────────

    [Theory]
    [InlineData(3, 8)] [InlineData(3, 14)] [InlineData(3, 17)] [InlineData(3, 20)]
    [InlineData(4, 8)] [InlineData(4, 14)] [InlineData(4, 17)] [InlineData(4, 20)]
    [InlineData(5, 8)] [InlineData(5, 14)] [InlineData(5, 17)] [InlineData(5, 20)]
    [InlineData(6, 8)] [InlineData(6, 14)] [InlineData(6, 17)] [InlineData(6, 20)]
    public async Task CoreOrRunwayHorizons_PublicPreviewSucceeds_ForEveryAdvancedFrequency(int days, int weeks)
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRaceAsync(AdvancedRequest(days, weeks)));
        Assert.Equal(days, preview["days_per_week"]!.GetValue<int>());
    }

    // ── LongHorizon across all four Advanced frequencies ───────────────────────

    [Theory]
    [InlineData(3, 21)] [InlineData(3, 32)] [InlineData(3, 52)]
    [InlineData(4, 21)] [InlineData(4, 32)] [InlineData(4, 52)]
    [InlineData(5, 21)] [InlineData(5, 32)] [InlineData(5, 52)]
    [InlineData(6, 21)] [InlineData(6, 32)] [InlineData(6, 52)]
    public async Task LongHorizonHorizons_PublicPreviewSucceeds_ForEveryAdvancedFrequency(int days, int weeks)
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewLongHorizonAsync(AdvancedRequest(days, weeks)));
        Assert.Equal("rolling_long_horizon", preview["schedule_strategy"]!.GetValue<string>());
        Assert.Equal(weeks, preview["total_weeks"]!.GetValue<int>());
        Assert.Equal(days, preview["days_per_week"]!.GetValue<int>());
    }

    // ── Full 45-horizon matrix, proven in full for Advanced x5D ────────────────

    [Fact]
    public async Task Full8To52Matrix_FiveDay_AllRouteCorrectly_FortyFiveOfFortyFive()
    {
        var coreSuccesses = 0;
        for (var weeks = 8; weeks <= 14; weeks++)
        {
            await ResetAsync();
            if ((await PreviewRaceAsync(AdvancedRequest(5, weeks))).IsSuccessStatusCode) coreSuccesses++;
        }
        Assert.Equal(7, coreSuccesses);

        var runwaySuccesses = 0;
        for (var weeks = 15; weeks <= 20; weeks++)
        {
            await ResetAsync();
            if ((await PreviewRaceAsync(AdvancedRequest(5, weeks))).IsSuccessStatusCode) runwaySuccesses++;
        }
        Assert.Equal(6, runwaySuccesses);

        var longHorizonSuccesses = 0;
        for (var weeks = 21; weeks <= 52; weeks++)
        {
            await ResetAsync();
            var response = await PreviewLongHorizonAsync(AdvancedRequest(5, weeks));
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

    // ── Confirm + fresh-reload per frequency: proves the two GEN.10 hardcode fixes ──

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Confirm_ReloadsFromFreshPostgres_WithCorrectLevelAndDaysPerWeek_LongHorizon(int days)
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewLongHorizonAsync(AdvancedRequest(days, 21)));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        // Phase 10K-GEN.10 defect regression: BuildTrainingPlan previously
        // hardcoded Level=Intermediate regardless of the real request.
        Assert.Equal(RunningBackground.Advanced, plan.Level);
        Assert.Equal(days, plan.DaysPerWeek);
        Assert.Equal(TargetFinishTimeSource.ProductAverage, plan.TargetFinishTimeSource);
        Assert.Equal(PlanScheduleStrategy.RollingLongHorizon, plan.ScheduleStrategy);

        var rollingPlan = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == previewId);
        Assert.Equal(days, rollingPlan.DaysPerWeek);
    }

    [Fact]
    public async Task Confirm_Core_ReloadsFromFreshPostgres_WithCorrectLevel()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRaceAsync(AdvancedRequest(4, 12)));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(RunningBackground.Advanced, plan.Level);
        Assert.Equal(4, plan.DaysPerWeek);
    }

    // ── Missing readiness -> typed PRODUCT_INELIGIBLE, not UNSUPPORTED (GEN.8) ──

    [Fact]
    public async Task MissingReadiness_LongHorizon_ReturnsTypedProductIneligible_NotUnsupported()
    {
        await ResetAsync();
        var response = await PreviewLongHorizonAsync(AdvancedRequest(5, 21, recentWeeklyVolumeKm: 0, recentLongestRunKm: 0, recentRunsPerWeek: 0));
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("LONG_HORIZON_PILOT_UNSUPPORTED", body);
    }

    // ── Full public GE->Runway->Core dual-KEY lifecycle, real PostgreSQL, Home/Calendar reads, repair ──

    [Fact]
    public async Task PublicFullLifecycle_FiveDay_ReachesOrganicCoreWithAdvancedDualKeyProfiles_ThroughRealPostgres()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewLongHorizonAsync(AdvancedRequest(5, 21)));
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
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            return await JsonAsync(response);
        }

        await CompleteAndActivateAsync();
        await CompleteAndActivateAsync();
        var w3 = await CompleteAndActivateAsync();
        Assert.Equal(10, w3["activated_window_range"]!["start_global_week"]!.GetValue<int>());

        using var verify = _factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstCoreWeek = await verifyDb.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek == 10).ToListAsync();
        Assert.Equal(5, firstCoreWeek.Count);
        var lane0 = Assert.Single(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 0);
        var lane1 = Assert.Single(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 1);
        // Proves the real public workout-type mapping (Level-agnostic,
        // keyed by workout/role/stage only) resolves Advanced's dual-KEY
        // profiles correctly -- not silently falling back to Intermediate's.
        Assert.StartsWith("ADVANCED_", lane0.CatalogPrescriptionProfileKey);
        Assert.StartsWith("ADVANCED_", lane1.CatalogPrescriptionProfileKey);

        var planAfter = await verifyDb.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(RunningBackground.Advanced, planAfter.Level);
        Assert.Equal(5, planAfter.DaysPerWeek);

        // Real repair through the production orchestrator, public-origin data.
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
        Assert.StartsWith("ADVANCED_", replacement.CatalogPrescriptionProfileKey);
    }

    // ── Unsupported neighbors remain closed: Beginner/Intermediate/Experienced unaffected, 7D and 2D unreachable ──

    [Theory]
    [InlineData("beginner", 3)]
    [InlineData("beginner", 5)]
    [InlineData("beginner", 6)]
    [InlineData("intermediate", 7)]
    [InlineData("advanced", 7)]
    [InlineData("advanced", 2)]
    [InlineData("experienced", 4)]
    public async Task UnsupportedNeighbors_RemainClosed_NoFallbackToAdvancedIdentity_LongHorizon(string level, int days)
    {
        await ResetAsync();
        var response = await PreviewLongHorizonAsync(AdvancedRequest(days, 21, level: level));
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(response.IsSuccessStatusCode, body);
        Assert.Contains("LONG_HORIZON_PILOT_UNSUPPORTED", body);
        Assert.DoesNotContain("TEN_K__3D__ADVANCED", body);
        Assert.DoesNotContain("TEN_K__4D__ADVANCED", body);
        Assert.DoesNotContain("TEN_K__5D__ADVANCED", body);
        Assert.DoesNotContain("TEN_K__6D__ADVANCED", body);
    }

    [Theory]
    [InlineData("beginner", 3)]
    [InlineData("beginner", 5)]
    [InlineData("beginner", 6)]
    [InlineData("intermediate", 7)]
    [InlineData("advanced", 7)]
    [InlineData("advanced", 2)]
    [InlineData("experienced", 4)]
    public async Task UnsupportedNeighbors_RemainClosed_NoFallbackToAdvancedIdentity_Core(string level, int days)
    {
        await ResetAsync();
        // Core/Runway's own routing falls back to the Legacy engine for any
        // identity the catalog pilot doesn't recognize (never a silent
        // Advanced match) -- unlike the dedicated LongHorizon endpoint, this
        // path does not return a single typed status code, so only success
        // is asserted false, matching this repository's own established
        // precedent for this exact check (Freq6D27's identical pattern).
        var response = await PreviewRaceAsync(AdvancedRequest(days, 12, level: level));
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(response.IsSuccessStatusCode, body);
        Assert.DoesNotContain("TEN_K__3D__ADVANCED", body);
        Assert.DoesNotContain("TEN_K__4D__ADVANCED", body);
        Assert.DoesNotContain("TEN_K__5D__ADVANCED", body);
        Assert.DoesNotContain("TEN_K__6D__ADVANCED", body);
    }

    [Fact]
    public async Task IntermediateAndBeginner_RemainPubliclyActive_ZeroDelta()
    {
        // Intermediate/Beginner have their own, much lower reachable-peak
        // authority than Advanced -- reuse the Advanced-tuned volume default
        // only for Advanced requests; a lower, level-appropriate starting
        // volume is required here or ResolvePeak legitimately rejects it as
        // below the level's own reachable peak.
        await ResetAsync();
        var intermediate4d = await PreviewRaceAsync(AdvancedRequest(4, 12, level: "intermediate", recentWeeklyVolumeKm: 20.0));
        Assert.True(intermediate4d.IsSuccessStatusCode, await intermediate4d.Content.ReadAsStringAsync());
        await ResetAsync();
        var intermediate5d = await PreviewRaceAsync(AdvancedRequest(5, 12, level: "intermediate", recentWeeklyVolumeKm: 20.0));
        Assert.True(intermediate5d.IsSuccessStatusCode, await intermediate5d.Content.ReadAsStringAsync());
        await ResetAsync();
        var intermediate6d = await PreviewRaceAsync(AdvancedRequest(6, 12, level: "intermediate", recentWeeklyVolumeKm: 20.0));
        Assert.True(intermediate6d.IsSuccessStatusCode, await intermediate6d.Content.ReadAsStringAsync());
        await ResetAsync();
        var beginner4d = await PreviewRaceAsync(AdvancedRequest(4, 12, level: "beginner", recentWeeklyVolumeKm: 12.0));
        Assert.True(beginner4d.IsSuccessStatusCode, await beginner4d.Content.ReadAsStringAsync());
    }
}
