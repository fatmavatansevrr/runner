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
/// PHASE DIST-GEN.3 -- real HTTP/PostgreSQL public-activation proof for the
/// one approved 16K/HalfMarathon-family/Intermediate/4D dark pilot, now
/// reachable through <c>goal_distance: "custom"</c> +
/// <c>target_distance_km: 16.0</c> on the real public
/// <c>POST /api/v1/plans/generate-preview/race</c> contract. Uses the exact
/// same <see cref="PublishedCatalogTestRelease"/> + <see cref="CustomWebApplicationFactory"/>
/// harness as <c>Gen4EBeginnerFourDayPublicActivationTests</c>/<c>Hm18PublicActivationAndBoundaryTests</c>/
/// <c>GoalDistanceCustomFailClosedTests</c> -- no dark-only test seam.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class DistGen3PublicActivationTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DistGen3PublicActivationTests(PublishedCatalogTestRelease release)
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

    // ── §16-20 Eligible horizon matrix: 10/11/12/13/14 weeks all succeed ────

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task EligibleHorizons_16K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Pilot16KRequest(weeks));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200 for {weeks}W, got {response.StatusCode}: {body}");

        var preview = JsonNode.Parse(body)!;
        Assert.Equal(16.0, preview["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", preview["goal_distance"]!.GetValue<string>());
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);

        // Weekly volume peak must stay within the frozen 16K band [34,46], never
        // the canonical HM 43.0 or TEN_K 38.0 references.
        var weeklyVolumes = preview["weeks"]!.AsArray()
            .Select(w => w!["days"]!.AsArray().Sum(d => d!["distance_km"]!.GetValue<double>()))
            .ToArray();
        var peakVolume = weeklyVolumes.Max();
        Assert.True(peakVolume <= 46.0 + 0.5, $"peak volume {peakVolume} exceeded 16K band maximum (46.0)");
        Assert.True(peakVolume >= 20.0, $"peak volume {peakVolume} implausibly low");
    }

    // ── §15/§21 Below-minimum / above-maximum horizon rejection ─────────────

    [Fact]
    public async Task NineWeekHorizon_TypedRejection_NotFiveHundred_NotSilentSubstitution()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Pilot16KRequest(9));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("TARGET_BELOW_SUM_OF_MINIMUMS", body);
        Assert.DoesNotContain("template_id", body);
        Assert.Contains("DARK_16K_CORE_HORIZON", body);
    }

    [Fact]
    public async Task FifteenWeekHorizon_TypedRejection_UnderOwnMaximumAuthority_NotHmSixteenWeekCeiling()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Pilot16KRequest(15));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("DARK_16K_CORE_HORIZON", body);
        Assert.DoesNotContain("template_id", body);
    }

    // ── §8 Regression: Custom without target_distance_km is unchanged ───────

    [Fact]
    public async Task CustomWithoutTargetDistanceKm_StillFailsClosed_RootDefectGuardIntact()
    {
        await ResetAsync();
        var before = await CountPersistedRowsAsync();
        var request = JsonSerializer.SerializeToNode(Pilot16KRequest(12))!.AsObject();
        request.Remove("target_distance_km");

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED", body);

        var after = await CountPersistedRowsAsync();
        Assert.Equal(before, after);
    }

    // ── §22 Unsupported custom-distance matrix ───────────────────────────────

    // NOTE: 15.0 was removed from this matrix by PHASE DIST-GEN.7, which
    // publicly activated target_distance_km=15.0 at Intermediate/4D (see
    // DistGen7PublicActivationTests.cs) -- it is no longer an unsupported
    // target and its premise here would now be false. 14.0 was added in its
    // place to keep this a 4-value unsupported matrix.
    //
    // NOTE: 18.0 was removed from this matrix by PHASE DIST-GEN.11, for the
    // identical reason -- DIST-GEN.11 publicly activated
    // target_distance_km=18.0 at Intermediate/4D (see
    // DistGen11PublicActivationTests.cs), so this InlineData case's premise
    // ("18.0 is unsupported") is now false by this phase's own explicit,
    // required purpose. This is the exact same class of stale-literal
    // correction as the 15.0 removal noted immediately above (and as
    // DIST-GEN.10's own report §69 disclosed against
    // Dark15KTargetDistanceProjectionTests.cs). 19.0 was added in its place
    // to keep this a 4-value unsupported matrix; the equivalent, and more
    // complete, real-HTTP coverage of 18.0's own new admission lives in
    // DistGen11PublicActivationTests.cs.
    [Theory]
    [InlineData(12.0)]
    [InlineData(14.0)]
    [InlineData(19.0)]
    [InlineData(20.0)]
    public async Task UnsupportedCustomTargetDistances_AtIntermediateFourDay_TypedRejection_NoSilentSubstitution(double km)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Pilot16KRequest(12))!.AsObject();
        request["target_distance_km"] = km;

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
        Assert.DoesNotContain("HALF_MARATHON__4D__INTERMEDIATE", body);
        Assert.DoesNotContain("TEN_K__4D__INTERMEDIATE", body);
    }

    // ── §75 10-mile-exact numeric-semantics proof ────────────────────────────

    [Fact]
    public async Task TenMileExact_16Point09_NotTreatedAsEligible_TypedRejection()
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Pilot16KRequest(12))!.AsObject();
        request["target_distance_km"] = 16.09;

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    // ── §23 Unsupported 16K-cell matrix: wrong level / wrong frequency ──────

    [Theory]
    [InlineData("beginner", 4)]
    [InlineData("advanced", 4)]
    public async Task Pilot16K_WrongLevel_Rejected(string level, int days)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Pilot16KRequest(12, level: level, days: days))!.AsObject();

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
    public async Task Pilot16K_WrongFrequency_Rejected(int days)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Pilot16KRequest(12, level: "intermediate", days: days))!.AsObject();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    // ── §46 Full E2E lifecycle at 12W: preview -> confirm -> persistence -> ──
    // Home -> Calendar -> PlanDetails -> complete -> not-today -> cancel.

    [Fact]
    public async Task TwelveWeek_FullLifecycle_IdentitySurvivesEveryStep()
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Pilot16KRequest(12));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(16.0, preview["requested_target_distance_km"]!.GetValue<double>());
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);

        Assert.Equal("HALF_MARATHON", plan.CanonicalDistanceFamily);
        Assert.Equal(16.0, plan.RequestedTargetDistanceKm);
        Assert.NotEqual(21.0975, plan.RequestedTargetDistanceKm);
        Assert.NotEqual(5.0, plan.RequestedTargetDistanceKm);
        Assert.Equal(12, plan.Weeks.Count);

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Equal(16.0, home["active_plan"]!["requested_target_distance_km"]!.GetValue<double>());

        var calendar = await _client.GetJsonAsync("/api/v1/plans/active/calendar?month=2026-07");
        Assert.NotEmpty(calendar.AsArray());

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(16.0, details["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", details["goal_distance"]!.GetValue<string>());

        var days = plan.Weeks.SelectMany(w => w.Days).OrderBy(d => d.Date).ToArray();
        var toComplete = days[0];
        (await _client.PostRawAsync($"/api/v1/training-days/{toComplete.Id}/complete",
            new { actual_distance_km = toComplete.PlannedDistanceKm, actual_duration_min = 30 })).EnsureSuccessStatusCode();
        Assert.Equal(TrainingDayStatus.Completed, (await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == toComplete.Id)).Status);

        var toSkip = days[1];
        (await _client.PostRawAsync($"/api/v1/training-days/{toSkip.Id}/not-today-decisions",
            new { reason = "DIST-GEN.3 not-today lifecycle proof" })).EnsureSuccessStatusCode();

        var cancel = await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel",
            new { reason = "DIST-GEN.3 lifecycle proof cancellation" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
    }

    // ── §48-51 Zero-delta smoke: canonical TEN_K/HM/HM-6D unaffected ────────

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

    [Fact]
    public async Task ZeroDelta_HalfMarathonIntermediate6D_Unaffected()
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(HmRequest(14, "intermediate", 6))!.AsObject();
        request["recent_weekly_volume_km"] = 29.5;
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("HALF_MARATHON__6D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static object Pilot16KRequest(int weeks, string level = "intermediate", int days = 4)
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
            race_name = "DIST-GEN.3 16K public pilot activation test",
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
            race_name = "DIST-GEN.3 TEN_K zero-delta test",
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
            race_name = "DIST-GEN.3 HALF_MARATHON zero-delta test",
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
