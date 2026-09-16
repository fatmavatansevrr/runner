using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.Services;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// PHASE DIST-GEN.0.1 -- real HTTP/PostgreSQL proof that the already-discovered,
/// currently-live product defect (PHASE_DIST_GEN_0_...md §2/§46: a request
/// identifying its goal distance as <c>GoalDistance.Custom</c> previously
/// silently resolved to a 5.0km plan via <c>GoalDistanceKm.Resolve</c>'s
/// unconditional <c>_ =&gt; 5.0</c> fallback) now fails closed with a typed,
/// non-500, non-200 HTTP 422 (<c>GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED</c>)
/// instead. This is a SAFETY/DEFECT-CORRECTION test suite -- it deliberately
/// does NOT exercise any 16K/custom-distance numeric authority (none exists;
/// none should exist after this phase).
///
/// The current wire format has no field that carries an arbitrary decimal km
/// value (confirmed: <c>GenerateRacePlanPreviewRequest</c>/
/// <c>GenerateHabitPlanPreviewRequest</c>/<c>RecentRaceInput</c> are all
/// GoalDistance-enum-only), so every "arbitrary distance" scenario below is
/// expressed the only way the real request/frontend seam allows today:
/// <c>goal_distance: "custom"</c> (varying level/frequency/target-time-source
/// to prove this isn't a single-cell coincidence).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class GoalDistanceCustomFailClosedTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GoalDistanceCustomFailClosedTests(PublishedCatalogTestRelease release)
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

    // Deliberately NOT resolving ICurrentUserAccessor here -- the real
    // FirebaseIdentityProvider (behind the Mock auth provider) requires an
    // active HTTP request context and throws outside one. ApiIntegrationTestCollection
    // serializes every test in this collection against the one shared
    // Postgres test database, and ResetAsync (a real HTTP call, so it DOES
    // have a request context) clears every row for the one fixed mock
    // identity every test in this suite runs as -- so a plain whole-table
    // count before/after is an equally precise, real proof that no row was
    // created by the failed-closed request in between.
    private async Task<(int PreviewCount, int PlanCount)> CountPersistedRowsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var previewCount = await db.PlanPreviews.CountAsync();
        var planCount = await db.TrainingPlans.CountAsync();
        return (previewCount, planCount);
    }

    // ── §16 core proof: the exact 16K-shaped defect reproduction ──────────────
    // A user "typing 16" today can only ever reach the backend as
    // goal_distance=custom (the frontend discards the actual number -- see
    // race_details_page.dart's own _mapDistanceTextToEnum). This must never
    // produce a 200, must never produce a plan generated "as if for a 5K",
    // and must never be a raw 500.
    [Fact]
    public async Task RacePreview_GoalDistanceCustom_FailsClosed_NeverFiveKPlan_NeverFiveHundred()
    {
        await ResetAsync();
        var before = await CountPersistedRowsAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRaceRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
        // The specific defect this phase closes: no silent 5K substitution
        // (the error body's own "only X, Y, Z are supported" message legitimately
        // mentions five_k, so check for the SILENT-FALLBACK shape, not the string).
        Assert.DoesNotContain("\"goal_distance_km\":5", body);
        Assert.DoesNotContain("template_id", body);
        Assert.DoesNotContain("preview_id", body);

        var after = await CountPersistedRowsAsync();
        Assert.Equal(before, after); // no PlanPreview, no TrainingPlan row created.
    }

    // ── Additional arbitrary-distance proofs (§17): different level/frequency/ ──
    // target-time-source combinations, standing in for a user having typed
    // 8K/12K/18K -- all must fail identically. Proves this is a distance-
    // identity gate, not a single-cell coincidence tied to the 16K pilot cell.
    [Theory]
    [InlineData("intermediate", 4, "product_average")]
    [InlineData("intermediate", 3, "user_defined")]
    [InlineData("beginner", 4, "user_defined")]
    [InlineData("advanced", 5, "user_defined")]
    public async Task RacePreview_GoalDistanceCustom_AcrossLevelFrequencyAndTimeSource_AlwaysFailsClosedIdentically(
        string level, int days, string targetFinishTimeSource)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(CustomRaceRequest(level: level, days: days))!.AsObject();
        request["target_finish_time_source"] = targetFinishTimeSource;
        // user_defined tolerates an arbitrary target time; product_average
        // would otherwise require CanonicalTargetFinishTimePolicy's own
        // (unrelated, already-correct) null-for-Custom rejection -- either
        // way, THIS gate must fire first.
        request["target_finish_time_seconds"] = targetFinishTimeSource == "product_average" ? 1500 : 9999;

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED", body);
    }

    // ── Habit-plan equivalent (§25 requirement #4): independent proof, not ──────
    // merely assumed to be covered because the race path is fixed.
    [Fact]
    public async Task HabitPreview_GoalDistanceCustom_FailsClosed_IndependentOfRacePath()
    {
        await ResetAsync();
        var before = await CountPersistedRowsAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/habit", new
        {
            goal_distance = "custom",
            level = "beginner",
            days_per_week = 3,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = new[] { "mon", "wed", "fri" },
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED", body);
        Assert.DoesNotContain("preview_id", body);

        var after = await CountPersistedRowsAsync();
        Assert.Equal(before, after);
    }

    // ── RecentRaceInput audit (§6 requirement #5): a Custom recent-race distance ──
    // must not silently become 5K anywhere in pace/feasibility computation, even
    // when the PRIMARY goal_distance is a normal, fully-supported TEN_K request.
    [Fact]
    public async Task RacePreview_RecentRaceDistanceCustom_WithOtherwiseValidPrimaryGoalDistance_FailsClosed()
    {
        await ResetAsync();
        var before = await CountPersistedRowsAsync();

        var request = JsonSerializer.SerializeToNode(TenKRequest(12, "intermediate", 4))!.AsObject();
        request["recent_race"] = new JsonObject
        {
            ["distance"] = "custom",
            ["finish_time_seconds"] = 3600,
            ["race_date"] = "2026-01-01",
        };

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED", body);
        Assert.Contains("recent_race", body, StringComparison.OrdinalIgnoreCase);

        var after = await CountPersistedRowsAsync();
        Assert.Equal(before, after);
    }

    // ── RaceHorizonPolicy unreachability proof (§18): GoalDistance.Custom is ──
    // rejected before RaceHorizonPolicy.DecideForDistance ever runs, so it can
    // never fall through to TEN_K's borrowed 8/12/14-week bounds. Proven here by
    // using horizons that WOULD trigger HALF_MARATHON's own distinct
    // below-minimum/composition-required typed errors if this request were
    // HALF_MARATHON -- the response must be the goal-distance rejection, never
    // a horizon-classification error of any kind.
    [Theory]
    [InlineData(5)]   // would be PLAN_CORE_HORIZON_TOO_SHORT for HALF_MARATHON, BelowMinimum-legacy for TEN_K.
    [InlineData(30)]  // would be PLAN_HORIZON_COMPOSITION_REQUIRED for both families.
    public async Task RacePreview_GoalDistanceCustom_NeverReachesHorizonClassification(int weeks)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(CustomRaceRequest())!.AsObject();
        var start = new DateOnly(2026, 7, 20);
        request["race_date"] = start.AddDays(weeks * 7).ToString("yyyy-MM-dd");

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED", body);
        Assert.DoesNotContain("PLAN_CORE_HORIZON_TOO_SHORT", body);
        Assert.DoesNotContain("PLAN_HORIZON_COMPOSITION_REQUIRED", body);
        Assert.DoesNotContain("PLAN_HORIZON_START_TOO_EARLY", body);
        Assert.DoesNotContain("PLAN_CORE_HORIZON_UNSUPPORTED", body);
    }

    // ── Zero-delta regression (§19-21): the new fail-closed gate must not ──────
    // affect any currently-supported canonical distance. TEN_K and both a
    // simple and a 6D HALF_MARATHON cell (the newly-public HM-X1.4 6D cell)
    // must still succeed exactly as before this phase.
    [Fact]
    public async Task ZeroDelta_TenKIntermediate4D_StillSucceeds()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", TenKRequest(12, "intermediate", 4));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
    }

    [Fact]
    public async Task ZeroDelta_HalfMarathonIntermediate4D_StillSucceeds()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(14, "intermediate", 4));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
    }

    [Fact]
    public async Task ZeroDelta_HalfMarathonIntermediate6D_StillSucceeds()
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(HmRequest(14, "intermediate", 6))!.AsObject();
        request["recent_weekly_volume_km"] = 29.5;
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("HALF_MARATHON__6D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
    }

    [Fact]
    public async Task ZeroDelta_HabitFiveK_StillSucceeds()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/habit", new
        {
            goal_distance = "five_k",
            level = "beginner",
            days_per_week = 3,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = new[] { "mon", "wed", "fri" },
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static object CustomRaceRequest(string level = "intermediate", int days = 4)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "custom",
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(12 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 9999,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = (double?)20.0,
            recent_longest_run_km = 10,
            recent_runs_per_week = days,
            race_name = "DIST-GEN.0.1 custom-distance fail-closed test",
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
            race_name = "DIST-GEN.0.1 TEN_K zero-delta test",
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
            race_name = "DIST-GEN.0.1 HALF_MARATHON zero-delta test",
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
