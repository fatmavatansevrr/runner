using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// PHASE DIST-GEN.6 -- real HTTP/PostgreSQL public-boundary proof that adding
/// the second, dark-only 15K x INTERMEDIATE x 4D target-distance projection
/// changed NOTHING about the public activation surface DIST-GEN.3 already
/// proved, at the time DIST-GEN.6 was written: a real public HTTP request for
/// <c>target_distance_km: 16.0</c> must still succeed with 200 and the exact
/// same identity/behavior as before that phase.
///
/// PHASE DIST-GEN.7 UPDATE: this class's original two 15K-stays-blocked tests
/// (<c>PublicPreview_TargetDistance15_StillRejected_400_UnsupportedTargetDistance_NoPersistence</c>
/// and <c>PublicPreview_TargetDistance15_RejectedAtEveryEligibleDarkHorizon_NeverLeaksThroughByHorizon</c>)
/// were REMOVED here, not merely left stale: DIST-GEN.7 deliberately flipped
/// 15K from publicly-blocked to publicly-admitted at Intermediate/4D, so their
/// premise is now intentionally false. The superseding proof -- that 15.0 IS
/// now admitted, at every eligible horizon, with correct identity -- lives in
/// <see cref="DistGen7PublicActivationTests"/>. This class's remaining tests
/// (16K zero-delta, Custom-without-target regression, TEN_K/HM zero-delta)
/// were never about 15K's own reachability and remain fully valid, unchanged.
/// Uses the exact same <see cref="PublishedCatalogTestRelease"/> +
/// <see cref="CustomWebApplicationFactory"/> harness as
/// <see cref="DistGen3PublicActivationTests"/>/<see cref="DistGen7PublicActivationTests"/>.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class DistGen6PublicBoundaryZeroDeltaTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DistGen6PublicBoundaryZeroDeltaTests(PublishedCatalogTestRelease release)
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

    // NOTE (DIST-GEN.7): the original two "15K stays blocked" tests that lived
    // here were removed -- PHASE DIST-GEN.7 deliberately admitted
    // target_distance_km=15.0 at Intermediate/4D publicly. See
    // DistGen7PublicActivationTests.EligibleHorizons_15K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume
    // and NineWeekHorizon_15K_TypedRejection_.../FifteenWeekHorizon_15K_TypedRejection_...
    // for the superseding (now-true) proof of 15K's real public horizon boundary.

    // ── Zero-delta: 16K public activation is completely unchanged by this phase ──

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task PublicPreview_TargetDistance16_StillSucceeds_200_IdenticalIdentity(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(weeks, targetDistanceKm: 16.0));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200 for {weeks}W, got {response.StatusCode}: {body}");

        var preview = JsonNode.Parse(body)!;
        Assert.Equal(16.0, preview["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", preview["goal_distance"]!.GetValue<string>());
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
    }

    [Fact]
    public async Task PublicPreview_TargetDistance16_9WeekHorizon_StillTypedRejection_Unchanged()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(9, targetDistanceKm: 16.0));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("DARK_16K_CORE_HORIZON", body);
        Assert.DoesNotContain("DARK_15K", body);
    }

    // ── Original DIST-GEN.0.1 root-defect regression: Custom with no target_distance_km ──

    [Fact]
    public async Task GoalDistanceCustom_NoTargetDistanceKm_StillFailsClosed_422()
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Request(12, targetDistanceKm: 16.0))!.AsObject();
        request.Remove("target_distance_km");

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED", body);
    }

    // ── Zero-delta smoke: canonical TEN_K/HM unaffected by the 15K registry addition ──

    [Fact]
    public async Task ZeroDelta_TenKIntermediate4D_Unaffected()
    {
        await ResetAsync();
        var start = new DateOnly(2026, 7, 20);
        var preferred = new[] { "mon", "wed", "fri", "sun" };
        var request = new
        {
            goal_distance = "ten_k",
            level = "intermediate",
            days_per_week = 4,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(12 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average",
            recent_weekly_volume_km = (double?)25.0,
            recent_longest_run_km = 10,
            recent_runs_per_week = 4,
            race_name = "DIST-GEN.6 TEN_K zero-delta test",
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
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
        var start = new DateOnly(2026, 7, 20);
        var preferred = new[] { "mon", "wed", "fri", "sun" };
        var request = new
        {
            goal_distance = "half_marathon",
            level = "intermediate",
            days_per_week = 4,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(14 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = (2 * 3600) + (5 * 60),
            target_finish_time_source = "product_average",
            recent_weekly_volume_km = (double?)32,
            recent_longest_run_km = 14,
            recent_runs_per_week = 4,
            race_name = "DIST-GEN.6 HALF_MARATHON zero-delta test",
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static object Request(int weeks, double targetDistanceKm, string level = "intermediate", int days = 4)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = new[] { "mon", "wed", "fri", "sun" };
        return new
        {
            goal_distance = "custom",
            target_distance_km = targetDistanceKm,
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
            race_name = "DIST-GEN.6 public boundary zero-delta test",
        };
    }
}
