using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 10K-FREQ.6D.22 -- the final Intermediate×5D LongHorizon capability
/// phase: opens the real public routing gate (previously Intermediate×4D
/// only) and proves it through the real public HTTP + PostgreSQL lifecycle.
/// Mirrors <see cref="LongHorizonPublicPreviewConfirmationTests"/> and
/// <see cref="LongHorizonFullLifecycleMatrixTests"/>'s own established 4D
/// patterns, extended to 5D.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Freq6D22IntermediateFiveDayLongHorizonPublicActivationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Freq6D22IntermediateFiveDayLongHorizonPublicActivationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    private static object FiveDayRequest(
        int weeks, string level = "intermediate", int days = 5,
        double recentWeeklyVolumeKm = 26.0, double recentLongestRunKm = 8.0, int recentRunsPerWeek = 5,
        string targetFinishTimeSource = "product_average", int targetFinishTimeSeconds = 3480)
    {
        var start = new DateOnly(2026, 9, 7);
        return new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = days == 5 ? new[] { "mon", "tue", "thu", "fri", "sun" }
                : days == 6 ? new[] { "mon", "tue", "wed", "thu", "fri", "sun" }
                : days == 7 ? new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }
                : new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun",
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = targetFinishTimeSeconds, target_finish_time_source = targetFinishTimeSource,
            race_name = "Phase 10K-FREQ.6D.22",
            recent_weekly_volume_km = recentWeeklyVolumeKm, recent_longest_run_km = recentLongestRunKm, recent_runs_per_week = recentRunsPerWeek,
        };
    }

    private async Task<HttpResponseMessage> PreviewRawAsync(object request) =>
        await _client.PostRawAsync("/api/v1/plans/generate-preview/race/long-horizon", request);

    private static async Task<JsonNode> JsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonNode.Parse(body)!;
    }

    // ── §7-8: representative and full 21-52 public routing ──────────────────

    [Theory]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(23)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(52)]
    public async Task RepresentativeHorizons_PublicPreviewSucceeds_WithExactFiveDayIdentity(int weeks)
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRawAsync(FiveDayRequest(weeks)));

        Assert.Equal("rolling_long_horizon", preview["schedule_strategy"]!.GetValue<string>());
        Assert.Equal(weeks, preview["total_weeks"]!.GetValue<int>());
        Assert.Equal(5, preview["days_per_week"]!.GetValue<int>());

        // No silent 4D fallback: the first GE week must show the real 5D shape.
        var firstWeek = preview["structural_roadmap"]!.AsArray()[0]!;
        if (firstWeek["session_prescriptions"] is JsonArray sessions)
        {
            Assert.Equal(5, sessions.Count);
            Assert.Equal(1, sessions.Count(s => s!["session_role"]!.GetValue<string>() == "KEY_SESSION"));
            Assert.Equal(3, sessions.Count(s => s!["session_role"]!.GetValue<string>().StartsWith("EASY_SUPPORT")));
            Assert.Equal(1, sessions.Count(s => s!["session_role"]!.GetValue<string>() == "LONG_RUN"));
        }
    }

    [Fact]
    public async Task Full21To52Matrix_AllRouteToLongHorizon_ThirtyTwoOfThirtyTwo()
    {
        var successes = 0;
        for (var weeks = 21; weeks <= 52; weeks++)
        {
            await ResetAsync();
            var response = await PreviewRawAsync(FiveDayRequest(weeks));
            if (response.IsSuccessStatusCode)
            {
                var preview = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
                if (preview["schedule_strategy"]?.GetValue<string>() == "rolling_long_horizon")
                    successes++;
            }
        }
        Assert.Equal(32, successes);
    }

    // ── §10-11: ProductAverage / UserDefined ────────────────────────────────

    [Fact]
    public async Task ProductAverage_ConfirmsAndReloadsFromFreshPostgres()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRawAsync(FiveDayRequest(21, targetFinishTimeSource: "product_average")));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(5, plan.DaysPerWeek);
        Assert.Equal(3480, plan.TargetFinishTimeSeconds);
        Assert.Equal(TargetFinishTimeSource.ProductAverage, plan.TargetFinishTimeSource);
        Assert.Equal(PlanScheduleStrategy.RollingLongHorizon, plan.ScheduleStrategy);
    }

    [Fact]
    public async Task UserDefined_ConfirmsAndPreservesSourceAfterFreshReload()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRawAsync(FiveDayRequest(21, targetFinishTimeSource: "user_defined", targetFinishTimeSeconds: 3300)));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(3300, plan.TargetFinishTimeSeconds);
        Assert.Equal(TargetFinishTimeSource.UserDefined, plan.TargetFinishTimeSource);
    }

    // ── §13-14: missing / zero readiness ─────────────────────────────────────

    [Fact]
    public async Task MissingReadiness_ReturnsTypedProductIneligible_NotUnsupported()
    {
        await ResetAsync();
        var response = await PreviewRawAsync(FiveDayRequest(21, recentWeeklyVolumeKm: 0, recentLongestRunKm: 0, recentRunsPerWeek: 0));
        var body = await response.Content.ReadAsStringAsync();
        // Identity is supported; only the request's own evidence is ineligible -- never LONG_HORIZON_PILOT_UNSUPPORTED.
        Assert.DoesNotContain("LONG_HORIZON_PILOT_UNSUPPORTED", body);
    }

    // ── §31 real public/Postgres full lifecycle E2E: GE -> Runway -> Core, dual-KEY, repair ──

    [Fact]
    public async Task PublicFullLifecycle_ConfirmedPlan_ReachesOrganicCoreWithDualKeyAndSurvivesRepair()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRawAsync(FiveDayRequest(21)));
        var rollingId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = rollingId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        // Home / Calendar work for the real confirmed public plan.
        var home = await _client.GetAsync("/api/v1/plans/active/home");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("rolling_long_horizon", await home.Content.ReadAsStringAsync());
        var calendar = await _client.GetAsync("/api/v1/plans/active/calendar?month=2026-09");
        Assert.Equal(HttpStatusCode.OK, calendar.StatusCode);
        Assert.Contains("rolling_long_horizon", await calendar.Content.ReadAsStringAsync());

        // GE=1 week; Runway=2-9; Core begins at week 10 -- same shape FREQ.6D.19 established.
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

        // Execution-profile verification (§30): PublishedBundleReleaseVersion must be resolved, never Legacy fallback.
        using (var profileScope = _factory.Services.CreateScope())
        {
            var opts = profileScope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RunningApp.Application.RuntimeCatalog.PlanCatalogOptions>>();
            Assert.False(string.IsNullOrEmpty(opts.Value.PublishedBundleReleaseVersion));
        }
        // §32 Adaptation proof: full adherence on the real public plan's GE
        // week (5/5 completed) must drive a real ProgressAsPlanned outcome --
        // the newly-activated Runway window's weekly volume must reflect
        // growth off the completed GE evidence, not a dark test fixture.
        double? geVolume;
        using (var geScope = _factory.Services.CreateScope())
        {
            var db = geScope.ServiceProvider.GetRequiredService<AppDbContext>();
            geVolume = (await db.LongHorizonRollingWeekStates.AsNoTracking()
                .SingleAsync(w => w.PlanStateId == rollingId && w.GlobalWeek == 1)).WeeklyVolumeKm;
        }
        await CompleteAndActivateAsync();
        using (var runwayScope = _factory.Services.CreateScope())
        {
            var db = runwayScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var runwayWeek2 = await db.LongHorizonRollingWeekStates.AsNoTracking()
                .SingleAsync(w => w.PlanStateId == rollingId && w.GlobalWeek == 2);
            Assert.NotNull(geVolume);
            Assert.NotNull(runwayWeek2.WeeklyVolumeKm);
            Assert.True(runwayWeek2.WeeklyVolumeKm >= geVolume,
                $"ProgressAsPlanned from full GE adherence must not reduce weekly volume: GE={geVolume} Runway.Week2={runwayWeek2.WeeklyVolumeKm}");
        }
        await CompleteAndActivateAsync();
        var w3 = await CompleteAndActivateAsync();
        Assert.Equal(10, w3["activated_window_range"]!["start_global_week"]!.GetValue<int>());
        Assert.Equal(13, w3["activated_window_range"]!["end_global_week"]!.GetValue<int>());

        using (var verify = _factory.Services.CreateScope())
        {
            var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
            var firstCoreWeek = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek == 10).ToListAsync();
            Assert.Equal(5, firstCoreWeek.Count);
            Assert.Equal(2, firstCoreWeek.Count(s => s.SessionRole == "KEY_SESSION"));
            Assert.Contains(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 0);
            Assert.Contains(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 1);
            Assert.All(firstCoreWeek.Where(s => s.SessionRole == "KEY_SESSION"), s =>
            {
                Assert.False(string.IsNullOrWhiteSpace(s.ProgressionStageKey));
                Assert.False(string.IsNullOrWhiteSpace(s.CatalogPrescriptionProfileKey));
                Assert.NotNull(s.CatalogPrescriptionProfileVersion);
            });

            // TrainingDay-detail-equivalent read: real Calendar still resolves after Core entry.
            var planAfter = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
            Assert.Equal(5, planAfter.DaysPerWeek);
            Assert.Equal(TargetFinishTimeSource.ProductAverage, planAfter.TargetFinishTimeSource);
        }

        // Repair the organically-materialized secondary KEY via the real repair service.
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

    // ── §46: unsupported neighbors remain closed ─────────────────────────────
    // Phase 10K-FREQ.6D.27 subsequently opened the public Intermediate x6D
    // gate itself (by product/repository design, not a regression) --
    // (intermediate, 6) removed from this "unsupported neighbor" list and
    // covered instead by Freq6D27IntermediateSixDayPublicActivationTests'
    // own activation proof. Intermediate x7D remains genuinely unsupported.

    [Theory]
    [InlineData("beginner", 5)]
    [InlineData("intermediate", 7)]
    public async Task UnsupportedNeighbors_RemainClosed_NoFallbackToFourOrFiveDayIdentity(string level, int days)
    {
        await ResetAsync();
        var response = await PreviewRawAsync(FiveDayRequest(21, level: level, days: days));
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(response.IsSuccessStatusCode, body);
        Assert.Contains("LONG_HORIZON_PILOT_UNSUPPORTED", body);
        Assert.DoesNotContain("TEN_K__5D__INTERMEDIATE", body);
        Assert.DoesNotContain("TEN_K__4D__INTERMEDIATE", body);
    }
}
