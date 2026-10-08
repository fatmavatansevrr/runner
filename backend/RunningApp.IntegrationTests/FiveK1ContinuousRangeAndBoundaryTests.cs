using System.Net;
using System.Text.Json.Nodes;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// PHASE FIVE-K.1 — real HTTP proof of exact-5K routing precedence and the
/// user-reachable 5K-through-Half-Marathon continuous-range closure, for the
/// Intermediate 4D/5D cells FIVE-K.0B approved. Exercises the real public
/// request path exactly as DerivedDist2PublicActivationTests/DistGen11PublicActivationTests
/// already do — no new boundary-check logic was added anywhere in production
/// code for this phase (CustomDistanceNewCreationRouter, NearestHigherCanonicalPlanResolver,
/// and DerivedDistanceEligibilityPolicy are all deliberately untouched, per the
/// governing prompt's explicit "do not modify" instruction) — these tests prove
/// that restraint was correct by exercising the real, unmodified routing chain.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class FiveK1ContinuousRangeAndBoundaryTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FiveK1ContinuousRangeAndBoundaryTests(PublishedCatalogTestRelease release)
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

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static async Task ResetAsync()
    {
        using var resetFactory = new CustomWebApplicationFactory();
        using var resetClient = resetFactory.CreateClient();
        (await resetClient.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
    }

    private static string[] DaysFor(int days) => days switch
    {
        4 => new[] { "tue", "thu", "sat", "sun" },
        _ => new[] { "mon", "tue", "thu", "sat", "sun" },
    };

    private static object ExactFiveKRequest(int weeks = 10, int days = 4) => new
    {
        goal_distance = "five_k",
        level = "intermediate",
        days_per_week = days,
        unit = "km",
        start_date = "2026-08-03",
        preferred_days = DaysFor(days),
        long_run_day = DaysFor(days)[^1],
        race_date = new DateOnly(2026, 8, 3).AddDays(weeks * 7).ToString("yyyy-MM-dd"),
        target_finish_time_seconds = 1200,
        target_finish_time_source = "user_defined",
        recent_weekly_volume_km = days == 4 ? 25.0 : 28.0,
        recent_longest_run_km = 8,
        recent_runs_per_week = days,
        recent_race = new { distance = "five_k", finish_time_seconds = 1250, race_date = "2026-07-13" },
    };

    private static object CustomRequest(double km, int weeks = 12, int days = 4) => new
    {
        goal_distance = "custom",
        target_distance_km = km,
        level = "intermediate",
        days_per_week = days,
        unit = "km",
        start_date = "2026-07-20",
        preferred_days = DaysFor(days),
        long_run_day = DaysFor(days)[^1],
        race_date = new DateOnly(2026, 7, 20).AddDays(weeks * 7).ToString("yyyy-MM-dd"),
        target_finish_time_seconds = (int)(km * 300),
        target_finish_time_source = "user_defined",
        recent_weekly_volume_km = 20.0,
        recent_longest_run_km = 10,
        recent_runs_per_week = days,
        recent_race = new { distance = "ten_k", finish_time_seconds = 2700, race_date = "2026-06-29" },
    };

    // ───────────────────────── Exact 5.0 vs 5.1 boundary ─────────────────────────

    [Fact]
    public async Task ExactFiveK_RoutesCanonical_NeverTenKDerived()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", ExactFiveKRequest());
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("five_k", preview["goal_distance"]!.GetValue<string>());
        Assert.Equal("FIVE_K__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    [Fact]
    public async Task JustAbove_FivePointOne_RoutesTenKDerived_NeverCanonicalFiveK()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(5.1));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(5.1, preview["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("ten_k", preview["goal_distance"]!.GetValue<string>());
        Assert.DoesNotContain("FIVE_K__", body);
    }

    [Fact]
    public async Task FourPointNine_RemainsUnsupported_NeverDerivedFromFiveKOrClampedToFiveK()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(4.9));
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    public async Task SubFiveK_And_Invalid_RemainUnsupported(double km)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(km));
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ───────────────────────── Whole-km 5-21 matrix (Intermediate 4D) ─────────────────────────

    [Theory]
    [InlineData(5.0, "five_k", "FIVE_K__4D__INTERMEDIATE")]
    [InlineData(6.0, "ten_k", null)]
    [InlineData(7.0, "ten_k", null)]
    [InlineData(8.0, "ten_k", null)]
    [InlineData(9.0, "ten_k", null)]
    [InlineData(10.0, "ten_k", "TEN_K__4D__INTERMEDIATE")]
    [InlineData(11.0, "half_marathon", null)]
    [InlineData(13.0, "half_marathon", null)]
    [InlineData(16.0, "half_marathon", null)]
    [InlineData(21.0, "half_marathon", null)]
    public async Task WholeKilometreMatrix_FiveThroughTwentyOne_RoutesToExpectedFamily(double km, string expectedGoalDistance, string? expectedCanonicalTemplateId)
    {
        await ResetAsync();
        var request = Math.Abs(km - 5.0) < 0.001 ? ExactFiveKRequest()
            : Math.Abs(km - 10.0) < 0.001
                ? new
                {
                    goal_distance = "ten_k",
                    level = "intermediate",
                    days_per_week = 4,
                    unit = "km",
                    start_date = "2026-07-20",
                    preferred_days = DaysFor(4),
                    long_run_day = "sun",
                    race_date = new DateOnly(2026, 7, 20).AddDays(12 * 7).ToString("yyyy-MM-dd"),
                    target_finish_time_seconds = 3000,
                    target_finish_time_source = "user_defined",
                }
                : (object)CustomRequest(km);
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200 for {km}km, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(expectedGoalDistance, preview["goal_distance"]!.GetValue<string>());
        if (expectedCanonicalTemplateId is not null)
        {
            Assert.Equal(expectedCanonicalTemplateId, preview["template_id"]!.GetValue<string>());
        }
    }

    // ───────────────────────── Exact HM control (21.0975, never rounded) ─────────────────────────

    [Fact]
    public async Task ExactHalfMarathon_CanonicalConstant_RoutesCanonical_NeverRoundedOrDerived()
    {
        await ResetAsync();
        var request = new
        {
            goal_distance = "half_marathon",
            level = "intermediate",
            days_per_week = 4,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = DaysFor(4),
            long_run_day = "sun",
            race_date = new DateOnly(2026, 7, 20).AddDays(14 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 6000,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = (double?)30.0,
            recent_longest_run_km = 12,
            recent_runs_per_week = 4,
            recent_race = new { distance = "ten_k", finish_time_seconds = 2700, race_date = "2026-06-29" },
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }
}
