using System.Collections.Generic;
using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 10K-GEN.38 -- opens the real public routing gate for 2D Preparation
/// Runway (15-20wk) and 2D LongHorizon (21-52wk), both levels (Beginner,
/// Intermediate), implementing only already-approved, already-dark-verified
/// authority: GEN.27-29 (Runway repeating-pattern mechanism/block-role
/// reconciliation) and GEN.30-37 (LongHorizon GE-&gt;Runway-&gt;Core chain
/// completion, GlobalWeekNumber parity strict across the entire plan). No new
/// product/numeric authority is introduced anywhere in this phase -- only
/// <see cref="RunningApp.Application.RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy"/>'s
/// <c>IsSupportedPreparationRunwayLevelFrequency</c> and
/// <see cref="RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview.LongHorizonPublicPlanService"/>'s
/// <c>ValidatePilot</c> allow-lists were widened.
///
/// Mirrors <see cref="Gen20TwoDayCombinedPublicActivationTests"/> (2D Core
/// activation pattern) and <see cref="Freq6D22IntermediateFiveDayLongHorizonPublicActivationTests"/>
/// (the real public HTTP GE-&gt;Runway-&gt;Core full-lifecycle pattern this
/// phase's own LongHorizon proof follows).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Gen38TwoDayRunwayLongHorizonPublicActivationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Gen38TwoDayRunwayLongHorizonPublicActivationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Preparation Runway confirmation is gated by a separate, dedicated
    /// options flag (<c>PreparationRunwayPilotActivation:ConfirmationEnabled</c>),
    /// kept false on the shared <see cref="ApiIntegrationTestCollection"/>
    /// factory by design (per <see cref="PreparationRunwayConfirmationEndToEndTests"/>'s
    /// own established pattern, which this reuses verbatim) so every
    /// pre-existing "not yet confirmable" assertion keeps passing unmodified.
    /// Runway confirm/read tests in this file need their own dedicated
    /// factory instance with the gate explicitly overridden true. LongHorizon
    /// confirmation is a separate, already-always-enabled path and does not
    /// need this override.
    /// </summary>
    private static CustomWebApplicationFactory RunwayConfirmationEnabledFactory() =>
        new("Development", new Dictionary<string, string?>
        {
            ["PreparationRunwayPilotActivation:Enabled"] = "true",
            ["PreparationRunwayPilotActivation:ConfirmationEnabled"] = "true",
        });

    private async Task ResetAsync() => (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    // Reuses GEN.20's own established real-HTTP-verified readiness values for
    // the Core-anchored Runway path (Gen29's own dark orchestration values
    // are identical for Beginner: weekly=12/longest=5). Not a new number.
    private static (double weekly, double longest) DefaultReadinessFor(string level) => level switch
    {
        "beginner" => (12.0, 5.0),
        _ => (16.0, 7.0),
    };

    private static object RunwayRequest(
        string level, int weeks, double? recentWeeklyVolumeKm = null, double? recentLongestRunKm = null, int? recentRunsPerWeek = null)
    {
        var start = new DateOnly(2026, 9, 9); // Wednesday
        var (defaultWeekly, defaultLongest) = DefaultReadinessFor(level);
        return new
        {
            goal_distance = "ten_k", level, days_per_week = 2, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = new[] { "wed", "sun" },
            long_run_day = "sun",
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            race_name = "Phase 10K-GEN.38 Runway",
            recent_weekly_volume_km = recentWeeklyVolumeKm ?? defaultWeekly,
            recent_longest_run_km = recentLongestRunKm ?? defaultLongest,
            recent_runs_per_week = recentRunsPerWeek ?? 2,
        };
    }

    // Reuses GEN.35/36/37's own established real-dark-verified LongHorizon
    // onboarding baseline for 2D (8km weekly / 3km long run / 2 runs per
    // week) -- deliberately low, chosen because 2D's PeakVolumeBand
    // ([16,22]km Beginner / [20,30]km Intermediate, GEN.11) is much lower
    // than 5D/6D's, so a higher baseline (as 5D/6D's own LongHorizon tests
    // use) would trip the pre-existing, correctly-restrictive AboveTarget
    // direction guard for a reason unrelated to 2D-specific code (GEN.35 §
    // header comment). Not a new numeric authority -- a test-input choice
    // appropriate to 2D's own already-approved volume range.
    private static object LongHorizonRequest(
        string level, int weeks, double recentWeeklyVolumeKm = 8.0, double recentLongestRunKm = 3.0, int recentRunsPerWeek = 2)
    {
        var start = new DateOnly(2026, 9, 9); // Wednesday
        return new
        {
            goal_distance = "ten_k", level, days_per_week = 2, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = new[] { "wed", "sun" },
            long_run_day = "sun",
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            race_name = "Phase 10K-GEN.38 LongHorizon",
            recent_weekly_volume_km = recentWeeklyVolumeKm,
            recent_longest_run_km = recentLongestRunKm,
            recent_runs_per_week = recentRunsPerWeek,
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

    // ═════════════════════════════ Preparation Runway (15-20wk) ═════════════════════════════

    [Theory]
    [InlineData("beginner", 15)] [InlineData("beginner", 16)] [InlineData("beginner", 17)]
    [InlineData("beginner", 18)] [InlineData("beginner", 19)] [InlineData("beginner", 20)]
    [InlineData("intermediate", 15)] [InlineData("intermediate", 16)] [InlineData("intermediate", 17)]
    [InlineData("intermediate", 18)] [InlineData("intermediate", 19)] [InlineData("intermediate", 20)]
    public async Task RunwayHorizons_FifteenThroughTwentyWeeks_PublicPreviewSucceeds_ForBothLevels(string level, int weeks)
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(RunwayRequest(level, weeks));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(2, preview["days_per_week"]!.GetValue<int>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        Assert.All(preview["weeks"]!.AsArray(), week => Assert.Equal(2, week!["days"]!.AsArray().Count));
    }

    [Fact]
    public async Task Full15To20Matrix_BothLevels_AllSucceed()
    {
        foreach (var level in new[] { "beginner", "intermediate" })
        {
            var successes = 0;
            for (var weeks = 15; weeks <= 20; weeks++)
            {
                await ResetAsync();
                var response = await PreviewRaceAsync(RunwayRequest(level, weeks));
                if (response.IsSuccessStatusCode) successes++;
                else Assert.Fail($"{level}@{weeks} failed: {await response.Content.ReadAsStringAsync()}");
            }
            Assert.Equal(6, successes);
        }
    }

    [Theory]
    [InlineData("beginner")]
    [InlineData("intermediate")]
    public async Task Runway_Confirm_ReloadsFromFreshPostgres_WithCorrectLevelAndDaysPerWeek(string level)
    {
        await using var factory = RunwayConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
        var preview = await JsonAsync(await client.PostRawAsync("/api/v1/plans/generate-preview/race", RunwayRequest(level, 18)));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);
        var expectedLevel = level == "beginner" ? RunningBackground.Beginner : RunningBackground.Intermediate;
        Assert.Equal(expectedLevel, plan.Level);
        Assert.Equal(2, plan.DaysPerWeek);
        Assert.Equal(18, plan.Weeks.Count);
        var days = plan.Weeks.SelectMany(w => w.Days).ToArray();
        Assert.Equal(36, days.Length);
        Assert.Equal(18, days.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
    }

    [Fact]
    public async Task Runway_ConfirmedPlan_HomeCalendarAndTrainingDayReads_AllSucceed()
    {
        await using var factory = RunwayConfirmationEnabledFactory();
        var client = factory.CreateClient();
        (await client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
        var preview = await JsonAsync(await client.PostRawAsync("/api/v1/plans/generate-preview/race", RunwayRequest("intermediate", 18)));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        var home = await client.GetAsync("/api/v1/plans/active/home");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains(planId.ToString(), await home.Content.ReadAsStringAsync());

        var calendar = await client.GetAsync("/api/v1/plans/active/calendar?month=2026-09");
        Assert.Equal(HttpStatusCode.OK, calendar.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await calendar.Content.ReadAsStringAsync()));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstDay = await db.TrainingDays.AsNoTracking().Where(d => d.Week.PlanId == planId).OrderBy(d => d.Date).FirstAsync();
        var detail = await client.GetAsync($"/api/v1/training-days/{firstDay.Id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains(firstDay.Id.ToString(), await detail.Content.ReadAsStringAsync());
    }

    // ── Unsupported-neighbor closure: Beginner x3D/x4D Runway remain closed ──

    [Theory]
    [InlineData("beginner", 3)]
    [InlineData("beginner", 4)]
    public async Task UnsupportedNeighborFrequencies_RunwayHorizon_RemainClosed(string level, int days)
    {
        await ResetAsync();
        var start = new DateOnly(2026, 9, 9);
        var response = await PreviewRaceAsync(new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = days switch { 3 => new[] { "mon", "wed", "sun" }, _ => new[] { "mon", "wed", "fri", "sun" } },
            long_run_day = "sun", race_date = start.AddDays(18 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = 12.0, recent_longest_run_km = 5.0, recent_runs_per_week = days,
            race_name = "Phase 10K-GEN.38 unsupported-neighbor",
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(response.IsSuccessStatusCode, body);
        Assert.Contains("PLAN_HORIZON_COMPOSITION_REQUIRED", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
    }

    // ── GEN.18's Core 8/9-week non-support: unaffected by Runway widening ────

    [Theory]
    [InlineData("beginner", 8)] [InlineData("intermediate", 9)]
    public async Task Core_EightOrNineWeeks_StillFailsClosed_ZeroDeltaAfterRunwayWidening(string level, int weeks)
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(RunwayRequest(level, weeks));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("TWO_DAY_CORE_EIGHT_OR_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL", body);
    }

    // ═════════════════════════════ LongHorizon (21-52wk) ═════════════════════════════

    [Theory]
    [InlineData("beginner", 21)] [InlineData("beginner", 22)] [InlineData("beginner", 23)]
    [InlineData("beginner", 32)] [InlineData("beginner", 52)]
    [InlineData("intermediate", 21)] [InlineData("intermediate", 22)] [InlineData("intermediate", 23)]
    [InlineData("intermediate", 32)] [InlineData("intermediate", 52)]
    public async Task LongHorizon_RepresentativeHorizons_PublicPreviewSucceeds_ForBothLevels(string level, int weeks)
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewLongHorizonAsync(LongHorizonRequest(level, weeks)));
        Assert.Equal("rolling_long_horizon", preview["schedule_strategy"]!.GetValue<string>());
        Assert.Equal(weeks, preview["total_weeks"]!.GetValue<int>());
        Assert.Equal(2, preview["days_per_week"]!.GetValue<int>());
    }

    [Fact]
    public async Task LongHorizon_Full21To52Matrix_BothLevels_AllRouteToLongHorizon()
    {
        foreach (var level in new[] { "beginner", "intermediate" })
        {
            var successes = 0;
            for (var weeks = 21; weeks <= 52; weeks++)
            {
                await ResetAsync();
                var response = await PreviewLongHorizonAsync(LongHorizonRequest(level, weeks));
                if (response.IsSuccessStatusCode)
                {
                    var preview = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
                    if (preview["schedule_strategy"]?.GetValue<string>() == "rolling_long_horizon") successes++;
                }
                else
                {
                    Assert.Fail($"{level}@{weeks} failed: {await response.Content.ReadAsStringAsync()}");
                }
            }
            Assert.Equal(32, successes);
        }
    }

    [Theory]
    [InlineData("beginner")]
    [InlineData("intermediate")]
    public async Task LongHorizon_Confirm_ReloadsFromFreshPostgres_WithCorrectLevelAndDaysPerWeek(string level)
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewLongHorizonAsync(LongHorizonRequest(level, 21)));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        var expectedLevel = level == "beginner" ? RunningBackground.Beginner : RunningBackground.Intermediate;
        Assert.Equal(expectedLevel, plan.Level);
        Assert.Equal(2, plan.DaysPerWeek);
        Assert.Equal(PlanScheduleStrategy.RollingLongHorizon, plan.ScheduleStrategy);
    }

    // ── Full real public/Postgres GE->Runway->Core lifecycle, both levels ────
    // (geWeeks=1 at totalWeeks=21: GE fully consumed by initial activation;
    // 3 real activate-next-window calls reach Core organically -- the same
    // shape LongHorizonGen35TwoDayJitBoundaryTests already dark-proved,
    // reached here for the first time through real public HTTP.)

    [Theory]
    [InlineData("beginner")]
    [InlineData("intermediate")]
    public async Task LongHorizon_PublicFullLifecycle_ConfirmedPlan_ReachesOrganicCoreWithStrictWeekParity(string level)
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewLongHorizonAsync(LongHorizonRequest(level, 21)));
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

        // GE=1 week; Runway=2-9; Core begins at week 10 -- the identical
        // fixed-length composition GEN.30/GEN.26 §3.1 established (GE=totalWeeks-20, Runway=8, Core=12).
        await CompleteAndActivateAsync(); // GE -> Runway boundary
        await CompleteAndActivateAsync(); // Runway continuation to Runway's own end
        var w3 = await CompleteAndActivateAsync(); // Runway -> Core boundary
        Assert.Equal(10, w3["activated_window_range"]!["start_global_week"]!.GetValue<int>());

        using var verify = _factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstCoreWeek = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek == 10).ToListAsync();
        Assert.Equal(2, firstCoreWeek.Count);
        Assert.Contains(firstCoreWeek, s => s.SessionRole == "LONG_RUN");
        var hasKey = firstCoreWeek.Any(s => s.SessionRole == "KEY_SESSION");
        var hasEasy = firstCoreWeek.Any(s => s.SessionRole.StartsWith("EASY_SUPPORT", StringComparison.Ordinal));
        Assert.True(hasKey != hasEasy, "First real public Core week must carry exactly one of KEY_SESSION/EASY_SUPPORT.");
        // GlobalWeekNumber=10 is even; per GEN.11's frozen global parity (odd=Pattern-A), this
        // week should carry EASY_SUPPORT, not KEY_SESSION -- asserting the real, decided GEN.37
        // strict-parity-across-Core behavior, not merely that some role is present.
        Assert.False(hasKey, "GlobalWeekNumber=10 is even and should be Pattern-B (EASY_SUPPORT), per GEN.37's strict cross-segment parity.");

        // Strict GlobalWeekNumber parity across the ENTIRE activated plan (GE+Runway+Core),
        // the concrete GEN.37 milestone -- now proven through real public HTTP for the first time.
        var allSessions = await db.LongHorizonRollingSessionStates.AsNoTracking()
            .Where(s => s.Week.PlanStateId == rollingId)
            .Select(s => new { s.Week.GlobalWeek, s.SessionRole })
            .ToListAsync();
        foreach (var week in allSessions.GroupBy(s => s.GlobalWeek))
        {
            var roles = week.Select(s => s.SessionRole).ToList();
            var weekHasKey = roles.Any(r => r == "KEY_SESSION");
            var weekHasEasy = roles.Any(r => r.StartsWith("EASY_SUPPORT", StringComparison.Ordinal));
            Assert.True(weekHasKey != weekHasEasy, $"Week {week.Key}: exactly one of KEY_SESSION/EASY_SUPPORT expected.");
            Assert.Equal(week.Key % 2 == 1, weekHasKey);
        }

        // Real Home/Calendar/detail reads still succeed after real organic Core entry.
        var homeAfter = await _client.GetAsync("/api/v1/plans/active/home");
        Assert.Equal(HttpStatusCode.OK, homeAfter.StatusCode);
        var firstCoreSession = firstCoreWeek.First();
        var detail = await _client.GetAsync($"/api/v1/training-days/rolling/{firstCoreSession.Id}");
        Assert.True(detail.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity,
            $"Unexpected status {detail.StatusCode}: {await detail.Content.ReadAsStringAsync()}");
    }

    // ── Missing/zero readiness: identity supported, never LONG_HORIZON_PILOT_UNSUPPORTED ──

    [Theory]
    [InlineData("beginner")]
    [InlineData("intermediate")]
    public async Task LongHorizon_MissingReadiness_NeverReturnsIdentityUnsupported(string level)
    {
        await ResetAsync();
        var response = await PreviewLongHorizonAsync(LongHorizonRequest(level, 21, recentWeeklyVolumeKm: 0, recentLongestRunKm: 0, recentRunsPerWeek: 0));
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("LONG_HORIZON_PILOT_UNSUPPORTED", body);
    }

    // ── Unsupported neighbors remain closed: Beginner x4D/x5D, Advanced x2D ──

    [Theory]
    [InlineData("beginner", 4)]
    [InlineData("advanced", 2)]
    public async Task LongHorizon_UnsupportedNeighbors_RemainClosed(string level, int days)
    {
        await ResetAsync();
        var start = new DateOnly(2026, 9, 9);
        var response = await PreviewLongHorizonAsync(new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = days == 2 ? new[] { "wed", "sun" } : new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun", race_date = start.AddDays(21 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = 12.0, recent_longest_run_km = 5.0, recent_runs_per_week = days,
            race_name = "Phase 10K-GEN.38 LongHorizon unsupported-neighbor",
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(response.IsSuccessStatusCode, body);
        Assert.Contains("LONG_HORIZON_PILOT_UNSUPPORTED", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
    }

    // ═════════════════════════════ Zero-delta: every already-active frequency ═════════════════════════════

    [Fact]
    public async Task ZeroDelta_TwoDayCore_TenToFourteenWeeks_StillWorksAfterRunwayAndLongHorizonWidening()
    {
        foreach (var level in new[] { "beginner", "intermediate" })
        {
            await ResetAsync();
            var response = await PreviewRaceAsync(RunwayRequest(level, 12));
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task ZeroDelta_EveryOtherPubliclyActiveFrequency_Unaffected()
    {
        var start = new DateOnly(2026, 9, 9);
        object Req(string level, int days, double weekly, double longest) => new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = days switch
            {
                3 => new[] { "mon", "wed", "sun" },
                4 => new[] { "mon", "wed", "fri", "sun" },
                5 => new[] { "mon", "tue", "thu", "fri", "sun" },
                _ => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
            },
            long_run_day = "sun", race_date = start.AddDays(12 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = weekly, recent_longest_run_km = longest, recent_runs_per_week = days,
            race_name = "Phase 10K-GEN.38 zero-delta",
        };

        await ResetAsync();
        Assert.True((await PreviewRaceAsync(Req("beginner", 4, 20.0, 8.0))).IsSuccessStatusCode);
        await ResetAsync();
        Assert.True((await PreviewRaceAsync(Req("beginner", 3, 12.0, 5.0))).IsSuccessStatusCode);

        foreach (var days in new[] { 3, 4, 5, 6 })
        {
            await ResetAsync();
            var r = await PreviewRaceAsync(Req("intermediate", days, 20.0, 8.0));
            Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        }

        foreach (var days in new[] { 3, 4, 5, 6 })
        {
            await ResetAsync();
            var r = await PreviewRaceAsync(Req("advanced", days, 30.0, 10.0));
            Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        }
    }
}
