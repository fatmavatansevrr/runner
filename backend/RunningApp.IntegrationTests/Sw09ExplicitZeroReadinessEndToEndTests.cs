using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Acceptance test SW-09 — the same supported 12-week 10K/Intermediate/4-day
/// product-average request as SW-02 (<see cref="Sw02ProductAverageEndToEndTests"/>),
/// but with all three readiness quantities explicitly reported as zero
/// (recent_longest_run_km/recent_weekly_volume_km/recent_runs_per_week = 0)
/// instead of omitted. Regression guard for a bug where the flow-specific
/// validators rejected explicit zero with HTTP 400 ("must be positive"),
/// treating 0 as invalid instead of preserving the documented 0-vs-null
/// contract (0 = explicitly reported zero readiness, null = unknown).
/// Runs against the real Api host + real Postgres DB, same as
/// <see cref="Sw02ProductAverageEndToEndTests"/>.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Sw09ExplicitZeroReadinessEndToEndTests
{
    private readonly HttpClient _client;

    public Sw09ExplicitZeroReadinessEndToEndTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task ResetAsync()
    {
        var response = await _client.PostRawAsync("/api/v1/testing/reset");
        response.EnsureSuccessStatusCode();
    }

    private static readonly object Sw09Request = new
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
        recent_weekly_volume_km = 0,
        recent_longest_run_km = 0,
        recent_runs_per_week = 0,
        recent_race = (object?)null,
    };

    [Fact]
    public async Task Sw09Request_ExplicitZeroReadiness_IsNotRejectedByTransportValidation()
    {
        await ResetAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Sw09Request);

        // The key invariant this test protects: validation must not reject
        // explicit zero. Whatever the runtime/prescription pipeline ultimately
        // decides (200 with a generated plan, or a typed 4xx from a real
        // domain rule) is a separate concern from transport-shape validation.
        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution()
    {
        await ResetAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Sw09Request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));

        var weeks = preview["weeks"]!.AsArray();
        Assert.Equal(12, weeks.Count);

        var allDays = weeks.SelectMany(w => w!["days"]!.AsArray()).ToList();
        Assert.Equal(48, allDays.Count);
        Assert.DoesNotContain(allDays, d => d!["day_type"]!.GetValue<string>() == "rest");
        Assert.False(preview["fallback_used"]!.GetValue<bool>());

        // Week 1 total distance reflects the documented explicit-zero starting
        // volume (12km, per V1MissingReadinessStartingVolumePolicy), not the
        // missing-readiness default (16km) and not a silently-injected value
        // like the SW-02 evidence-informed anchor (20km).
        var week1TotalKm = weeks[0]!["days"]!.AsArray().Sum(d => d!["distance_km"]!.GetValue<double>());
        Assert.Equal(12d, week1TotalKm, precision: 1);
    }
}
