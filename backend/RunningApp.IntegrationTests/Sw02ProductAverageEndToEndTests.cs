using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Acceptance test SW-02 — the exact 10K/Intermediate/4-day/12-week
/// product-average request that used to be rejected with HTTP 422
/// (RUNTIME_CONDITION_UNSUPPORTED/PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE).
/// See PHASE4D_4_1_PRODUCT_AVERAGE_TARGET_TIME_GOAL_FEASIBILITY_CLASSIFICATION.md.
/// Runs against the real Api host + real Postgres DB (same as
/// UserJourneyTests), in Development (where the local-acceptance override
/// treats the DRAFT TEN_K__4D__INTERMEDIATE candidate as published for
/// routing purposes only — the real candidate/catalog artifacts are never
/// modified).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Sw02ProductAverageEndToEndTests
{
    private readonly HttpClient _client;

    public Sw02ProductAverageEndToEndTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task ResetAsync()
    {
        var response = await _client.PostRawAsync("/api/v1/testing/reset");
        response.EnsureSuccessStatusCode();
    }

    private static readonly object Sw02Request = new
    {
        goal_distance = "ten_k",
        level = "intermediate",
        days_per_week = 4,
        unit = "km",
        start_date = "2026-07-20",
        preferred_days = new[] { "mon", "wed", "fri", "sun" },
        long_run_day = "sun",
        race_name = (string?)null,
        race_date = "2026-10-12",
        target_finish_time_seconds = 3480,
        target_finish_time_source = "product_average",
        recent_weekly_volume_km = 20,
        recent_longest_run_km = 8,
        recent_runs_per_week = 3,
        recent_race = (object?)null,
    };

    [Fact]
    public async Task Sw02Request_ReturnsHttp200_With12Weeks_4SessionsPerWeek_48TotalSessions_NoRestRows()
    {
        await ResetAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Sw02Request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));

        var weeks = preview["weeks"]!.AsArray();
        Assert.Equal(12, weeks.Count);

        var allDays = weeks.SelectMany(w => w!["days"]!.AsArray()).ToList();
        Assert.Equal(48, allDays.Count);

        foreach (var week in weeks)
        {
            Assert.Equal(4, week!["days"]!.AsArray().Count);
        }

        Assert.DoesNotContain(allDays, d => d!["day_type"]!.GetValue<string>() == "rest");

        // No legacy fallback: FallbackUsed stays false for a catalog-routed
        // preview (the legacy SQL path is never reached for this identity).
        Assert.False(preview["fallback_used"]!.GetValue<bool>());
    }
}
