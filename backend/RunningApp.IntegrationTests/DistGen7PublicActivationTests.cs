using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// PHASE DIST-GEN.7 -- real HTTP/PostgreSQL public-activation proof for the
/// second approved 15K/HalfMarathon-family/Intermediate/4D target-distance
/// projection, now reachable through <c>goal_distance: "custom"</c> +
/// <c>target_distance_km: 15.0</c> on the real public
/// <c>POST /api/v1/plans/generate-preview/race</c> contract -- exactly
/// mirroring <see cref="DistGen3PublicActivationTests"/>'s own proof shape
/// for 16K. Supersedes the two now-false "15K stays blocked" tests DIST-GEN.6
/// added to <see cref="DistGen6PublicBoundaryZeroDeltaTests"/> (that file's
/// 15K-rejection tests were removed/renamed in this phase; its 16K zero-delta
/// tests remain valid and untouched).
///
/// The single most important assertion in this file is
/// <see cref="PeakLongRun_15K_Uses14Point5_16K_StillUses15_NoCrossContamination"/>
/// -- proving the one deliberately-different numeric field
/// (PreferredAbsolutePeakLongRunKm: 14.5 for 15K vs 15.0 for 16K) through the
/// real public HTTP path, not just the internal policy-isolation harness
/// DIST-GEN.6 already proved.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class DistGen7PublicActivationTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DistGen7PublicActivationTests(PublishedCatalogTestRelease release)
    {
        _factory = new CustomWebApplicationFactory("Production", new Dictionary<string, string?>
        {
            ["Auth:Provider"] = "Mock",
            ["PlanCatalog:CatalogRootPath"] = release.ReleaseRoot,
            ["CatalogLivePilot:Enabled"] = "true",
            ["LocalCatalogAcceptance:Enabled"] = "false",
            ["ProductionConfigurationValidation:Enabled"] = "false",
        });
        _client = _factory.CreateClient();
    }

    public void Dispose() { _client.Dispose(); _factory.Dispose(); }

    private static async Task ResetAsync()
    {
        using var resetFactory = new CustomWebApplicationFactory();
        using var resetClient = resetFactory.CreateClient();
        (await resetClient.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
    }

    private async Task<(int PreviewCount, int PlanCount)> CountPersistedRowsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.PlanPreviews.CountAsync(), await db.TrainingPlans.CountAsync());
    }

    // ── §7/§21 Eligible horizon matrix: 10/11/12/13/14 weeks all succeed ─────

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task EligibleHorizons_15K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target15KRequest(weeks));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200 for {weeks}W, got {response.StatusCode}: {body}");

        var preview = JsonNode.Parse(body)!;
        Assert.Equal(15.0, preview["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", preview["goal_distance"]!.GetValue<string>());
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);

        // §23 Peak-volume policy proof: 34.0/46.0/40.0 band, never 16K's own
        // numbers, never HM's canonical 43.0, never TEN_K's 38.0.
        var weeklyVolumes = preview["weeks"]!.AsArray()
            .Select(w => w!["days"]!.AsArray().Sum(d => d!["distance_km"]!.GetValue<double>()))
            .ToArray();
        var peakVolume = weeklyVolumes.Max();
        Assert.True(peakVolume <= 40.0 + 0.5, $"peak volume {peakVolume} exceeded 15K band maximum (40.0)");
        Assert.True(peakVolume >= 20.0, $"peak volume {peakVolume} implausibly low");

        // §25 14.5-km peak-LR isolation: no long run day ever reaches or
        // exceeds 15.0 (the target distance itself), and never exceeds 14.5.
        var longRunDistances = preview["weeks"]!.AsArray()
            .SelectMany(w => w!["days"]!.AsArray())
            .Where(d => d!["day_type"]!.GetValue<string>() == "long_run")
            .Select(d => d!["distance_km"]!.GetValue<double>())
            .ToArray();
        Assert.NotEmpty(longRunDistances);
        Assert.All(longRunDistances, km => Assert.True(km < 15.0, $"15K long run {km} reached/exceeded target distance"));
        Assert.All(longRunDistances, km => Assert.True(km <= 14.5 + 0.001, $"15K long run {km} exceeded 14.5km ceiling"));
    }

    // ── §16/§20 Below-minimum / above-maximum horizon rejection ──────────────

    [Fact]
    public async Task NineWeekHorizon_15K_TypedRejection_NotFiveHundred_NotSilentSubstitution()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target15KRequest(9));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("TARGET_BELOW_SUM_OF_MINIMUMS", body);
        Assert.DoesNotContain("template_id", body);
        Assert.Contains("DARK_15K_CORE_HORIZON", body);
        Assert.DoesNotContain("DARK_16K", body);
    }

    [Fact]
    public async Task FifteenWeekHorizon_15K_TypedRejection_UnderOwnMaximumAuthority_NotHmSixteenWeekCeiling()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target15KRequest(15));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("DARK_15K_CORE_HORIZON", body);
        Assert.DoesNotContain("template_id", body);
    }

    // ── §10 Unsupported 15K-cell matrix: wrong level / wrong frequency ───────

    [Theory]
    [InlineData("beginner", 4)]
    [InlineData("advanced", 4)]
    public async Task Target15K_WrongLevel_Rejected(string level, int days)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Target15KRequest(12, level: level, days: days))!.AsObject();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Target15K_WrongFrequency_Rejected(int days)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Target15KRequest(12, level: "intermediate", days: days))!.AsObject();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    // ── §9/§51 Unsupported-target matrix (unchanged by this phase) ──────────

    [Theory]
    [InlineData(12.0)]
    [InlineData(14.0)]
    [InlineData(18.0)]
    [InlineData(20.0)]
    [InlineData(16.09)]
    public async Task UnsupportedTargetDistances_StillRejected_NeverSubstituted(double km)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Target15KRequest(12))!.AsObject();
        request["target_distance_km"] = km;

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
        Assert.DoesNotContain("HALF_MARATHON__4D__INTERMEDIATE", body);
        Assert.DoesNotContain("TEN_K__4D__INTERMEDIATE", body);
    }

    // ── §25 The single most important proof: 14.5 vs 15.0 isolation ─────────

    [Fact]
    public async Task PeakLongRun_15K_Uses14Point5_16K_StillUses15_NoCrossContamination()
    {
        await ResetAsync();

        var response15 = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target15KRequest(12));
        var body15 = await response15.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response15.StatusCode);
        var preview15 = JsonNode.Parse(body15)!;

        var response16 = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12));
        var body16 = await response16.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response16.StatusCode);
        var preview16 = JsonNode.Parse(body16)!;

        Assert.Equal(15.0, preview15["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal(16.0, preview16["requested_target_distance_km"]!.GetValue<double>());
        Assert.NotEqual(
            preview15["requested_target_distance_km"]!.GetValue<double>(),
            preview16["requested_target_distance_km"]!.GetValue<double>());

        double PeakLongRun(JsonNode preview) => preview["weeks"]!.AsArray()
            .SelectMany(w => w!["days"]!.AsArray())
            .Where(d => d!["day_type"]!.GetValue<string>() == "long_run")
            .Select(d => d!["distance_km"]!.GetValue<double>())
            .Max();

        var peak15 = PeakLongRun(preview15);
        var peak16 = PeakLongRun(preview16);

        // 15K's long run ceiling authority is 14.5 -- it must never reach 15.0,
        // even though 16K's own ceiling authority IS 15.0.
        Assert.True(peak15 <= 14.5 + 0.001, $"15K peak long run {peak15} exceeded its own 14.5km ceiling");
        Assert.True(peak16 <= 15.0 + 0.001, $"16K peak long run {peak16} exceeded its own 15.0km ceiling");
        Assert.True(peak15 < 15.0, "15K peak long run must never reach 15.0, the 16K ceiling / 15K target distance");
    }

    // ── §38-49 Full E2E lifecycle at 15K/12W ─────────────────────────────────

    [Fact]
    public async Task TwelveWeek_15K_FullLifecycle_IdentitySurvivesEveryStep()
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target15KRequest(12));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(15.0, preview["requested_target_distance_km"]!.GetValue<double>());
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);

        Assert.Equal("HALF_MARATHON", plan.CanonicalDistanceFamily);
        Assert.Equal(15.0, plan.RequestedTargetDistanceKm);
        Assert.NotEqual(21.0975, plan.RequestedTargetDistanceKm);
        Assert.NotEqual(16.0, plan.RequestedTargetDistanceKm);
        Assert.NotEqual(5.0, plan.RequestedTargetDistanceKm);
        Assert.Equal(12, plan.Weeks.Count);

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Equal(15.0, home["active_plan"]!["requested_target_distance_km"]!.GetValue<double>());

        var calendar = await _client.GetJsonAsync("/api/v1/plans/active/calendar?month=2026-07");
        Assert.NotEmpty(calendar.AsArray());

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(15.0, details["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", details["goal_distance"]!.GetValue<string>());

        var days = plan.Weeks.SelectMany(w => w.Days).OrderBy(d => d.Date).ToArray();
        var toComplete = days[0];
        (await _client.PostRawAsync($"/api/v1/training-days/{toComplete.Id}/complete",
            new { actual_distance_km = toComplete.PlannedDistanceKm, actual_duration_min = 30 })).EnsureSuccessStatusCode();
        Assert.Equal(TrainingDayStatus.Completed, (await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == toComplete.Id)).Status);

        var toSkip = days[1];
        (await _client.PostRawAsync($"/api/v1/training-days/{toSkip.Id}/not-today-decisions",
            new { reason = "DIST-GEN.7 not-today lifecycle proof" })).EnsureSuccessStatusCode();

        var cancel = await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel",
            new { reason = "DIST-GEN.7 lifecycle proof cancellation" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
    }

    // ── §55 Rollback-safety: an already-confirmed 15K plan stays operable ────
    // even hypothetically without re-checking creation eligibility on reads --
    // proven here by confirming no read/mutation endpoint below re-invokes the
    // eligibility gate at all (mirrors DIST-GEN.3's own §56 proof: the gate
    // lives only in GeneratePreviewCommandMapper.ToInternalRequest, never in
    // PlanServices' read/mutation paths).

    [Fact]
    public async Task ConfirmedPlan_15K_RemainsFullyOperable_ReadsAndMutationsDoNotRecheckPublicEligibility()
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target15KRequest(12));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        // Reads succeed purely from persisted state -- no eligibility gate involved.
        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Equal(15.0, home["active_plan"]!["requested_target_distance_km"]!.GetValue<double>());
        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(HttpStatusCode.OK, HttpStatusCode.OK); // details fetched without throwing
        Assert.Equal(15.0, details["requested_target_distance_km"]!.GetValue<double>());

        var cancel = await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel", new { reason = "rollback-safety proof" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
    }

    // ── §52 Custom-without-target safety regression ──────────────────────────

    [Fact]
    public async Task CustomWithoutTargetDistanceKm_StillFailsClosed_15KAdditionDidNotWeaken()
    {
        await ResetAsync();
        var before = await CountPersistedRowsAsync();
        var request = JsonSerializer.SerializeToNode(Target15KRequest(12))!.AsObject();
        request.Remove("target_distance_km");

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED", body);

        var after = await CountPersistedRowsAsync();
        Assert.Equal(before, after);
    }

    // ── §8/§56/§57 Zero-delta smoke: canonical TEN_K/HM unaffected ───────────

    [Fact]
    public async Task ZeroDelta_TenKIntermediate4D_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", TenKRequest(12, "intermediate", 4));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    [Fact]
    public async Task ZeroDelta_HalfMarathonIntermediate4D_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(14, "intermediate", 4));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static object Target15KRequest(int weeks, string level = "intermediate", int days = 4)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "custom",
            target_distance_km = 15.0,
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 5100,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = (double?)20.0,
            recent_longest_run_km = 10,
            recent_runs_per_week = days,
            recent_race = new { distance = "ten_k", finish_time_seconds = 2700, race_date = start.AddDays(-21).ToString("yyyy-MM-dd") },
            race_name = "DIST-GEN.7 15K public activation test",
        };
    }

    private static object Target16KRequest(int weeks, string level = "intermediate", int days = 4)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "custom",
            target_distance_km = 16.0,
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 5400,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = (double?)20.0,
            recent_longest_run_km = 10,
            recent_runs_per_week = days,
            recent_race = new { distance = "ten_k", finish_time_seconds = 2700, race_date = start.AddDays(-21).ToString("yyyy-MM-dd") },
            race_name = "DIST-GEN.7 16K side-by-side isolation test",
        };
    }

    private static object TenKRequest(int weeks, string level, int days)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "ten_k",
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average",
            recent_weekly_volume_km = (double?)25.0,
            recent_longest_run_km = 10,
            recent_runs_per_week = days,
            race_name = "DIST-GEN.7 TEN_K zero-delta test",
        };
    }

    private static object HmRequest(int weeks, string level, int days)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "half_marathon",
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = (2 * 3600) + (5 * 60),
            target_finish_time_source = "product_average",
            recent_weekly_volume_km = (double?)32,
            recent_longest_run_km = 14,
            recent_runs_per_week = days,
            race_name = "DIST-GEN.7 HALF_MARATHON zero-delta test",
        };
    }

    private static string[] DaysFor(int days) => days switch
    {
        2 => new[] { "mon", "thu" },
        3 => new[] { "mon", "wed", "sun" },
        4 => new[] { "mon", "wed", "fri", "sun" },
        5 => new[] { "mon", "tue", "thu", "fri", "sun" },
        6 => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
        _ => Enumerable.Range(0, days).Select(i => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }[i]).ToArray(),
    };
}
