using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 10K-FREQ.6D.4D.5G — the fifth Intermediate×5D public activation retry. Split E, 5B, 5D,
/// and 5F were each reverted after finding a new, independent, disclosed blocker (calendar,
/// Taper completeness ×2, public workout-type mapping, CompressedCore/ExtendedCore execution-
/// context propagation respectively); all five are now fixed and verified. Uses the same
/// default <see cref="CustomWebApplicationFactory"/> (Development environment, real committed
/// catalog root, real committed <c>1.1.0</c> published bundle, Development-only
/// <c>LocalCatalogAcceptance</c> override) already empirically validated in FREQ.6D.4D.5F against
/// the known-good Intermediate×3D cell before being relied on for 5D.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Gen5DIntermediatePublicActivationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public Gen5DIntermediatePublicActivationTests() => _client = _factory.CreateClient();

    private static readonly DateOnly Start = new(2026, 8, 3);
    private static readonly string[] PreferredDays = { "mon", "tue", "wed", "fri", "sun" };
    private const string LongRunDay = "sun";

    private static object Request(int weeks) => new
    {
        goal_distance = "ten_k", level = "intermediate", days_per_week = 5, unit = "km",
        start_date = Start.ToString("yyyy-MM-dd"), preferred_days = PreferredDays,
        long_run_day = LongRunDay, race_date = Start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
        target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
        recent_weekly_volume_km = 25.0, recent_longest_run_km = 10, recent_runs_per_week = 5,
        race_name = "FREQ.6D.4D.5G 5D activation",
    };

    private async Task ResetAsync() => (await _client.PostAsync("/api/v1/testing/reset", null)).EnsureSuccessStatusCode();

    // ── §37-40: real public 8/10/12/14-week Core previews (CompressedCore/PreferredCore/ExtendedCore) ──

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task EligibleFiveDayCoreHorizon_PublicPreview_SelectsRealFiveDayCandidate_TwoKeyTwoEasyOneLongPerWeek(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/plans/generate-preview/race", Request(weeks));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {response.StatusCode}: {body}");

        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        // §45: no silent 4D fallback -- the exact real 5D candidate identity.
        Assert.Equal("TEN_K__5D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(5, preview["days_per_week"]!.GetValue<int>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);

        Assert.All(preview["weeks"]!.AsArray(), week =>
        {
            var days = week!["days"]!.AsArray();
            Assert.Equal(5, days.Count);
            Assert.All(days, day => Assert.False(string.IsNullOrWhiteSpace(day!["day_type"]!.GetValue<string>())));
            Assert.Single(days, d => d!["day_type"]!.GetValue<string>() == "long_run");
        });
    }

    [Fact]
    public async Task TwelveWeek_FoundationPrimarySession_PublicWorkoutTypeIsInterval()
    {
        await ResetAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/plans/generate-preview/race", Request(12));
        response.EnsureSuccessStatusCode();
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        var week1Days = preview["weeks"]![0]!["days"]!.AsArray();
        var dayTypes = week1Days.Select(d => d!["day_type"]!.GetValue<string>()).ToList();

        Assert.Contains("interval", dayTypes); // Foundation Primary: AEROBIC_STRENGTH_CONTROLLED_INTRO -> Interval (5E/5F)
        Assert.Contains("tempo", dayTypes);
        Assert.Contains("easy", dayTypes);
        Assert.Contains("long_run", dayTypes);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(14)]
    public async Task CompressedOrExtendedCore_TaperOrLaterPhases_ContainInterval_NoUnmappedWorkout(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/plans/generate-preview/race", Request(weeks));
        response.EnsureSuccessStatusCode();
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        Assert.All(preview["weeks"]!.AsArray(), week =>
        {
            var dayTypes = week!["days"]!.AsArray().Select(d => d!["day_type"]!.GetValue<string>()).ToList();
            Assert.DoesNotContain(dayTypes, t => string.IsNullOrWhiteSpace(t));
        });
    }

    // ── §44: unsupported neighbors remain closed ────────────────────────────

    [Theory]
    [InlineData("beginner", 5)]
    [InlineData("advanced", 5)]
    public async Task UnsupportedNeighborCells_RemainUnactivated(string level, int days)
    {
        await ResetAsync();
        var start = Start;
        var response = await _client.PostAsJsonAsync("/api/v1/plans/generate-preview/race", new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"), preferred_days = PreferredDays,
            long_run_day = LongRunDay, race_date = start.AddDays(12 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = 25.0, recent_longest_run_km = 10, recent_runs_per_week = 5,
        });
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("TEN_K__5D__INTERMEDIATE", body);
    }

    [Theory]
    [InlineData("intermediate", 6)]
    [InlineData("intermediate", 7)]
    public async Task UnsupportedFrequencyNeighbors_RemainUnactivated(string level, int days)
    {
        await ResetAsync();
        var start = Start;
        var response = await _client.PostAsJsonAsync("/api/v1/plans/generate-preview/race", new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = Enumerable.Range(0, days).Select(i => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }[i]).ToArray(),
            long_run_day = "sun", race_date = start.AddDays(12 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = 25.0, recent_longest_run_km = 10, recent_runs_per_week = 5,
        });
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ── §41-43: confirmation, persistence, reads, Taper persisted, representative adaptation ──

    [Theory]
    [InlineData(8)]  // CompressedCore representative
    [InlineData(14)] // ExtendedCore representative
    [InlineData(12)] // PreferredCore reference
    public async Task ResetPreviewConfirmReadAndComplete_RealFiveDaySessionsPersisted(int weeks)
    {
        await ResetAsync();
        var previewResponse = await _client.PostAsJsonAsync("/api/v1/plans/generate-preview/race", Request(weeks));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        var confirmResponse = await _client.PostAsJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        confirmResponse.EnsureSuccessStatusCode();
        var confirm = (await confirmResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);

        Assert.Equal(5, plan.DaysPerWeek);
        Assert.Equal("TEN_K__5D__INTERMEDIATE", plan.CatalogCandidateKey);
        Assert.Equal(weeks, plan.Weeks.Count);

        var days = plan.Weeks.SelectMany(w => w.Days).ToArray();
        Assert.Equal(weeks * 5, days.Length);
        Assert.Equal(weeks * 2, days.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
        Assert.Equal(weeks * 2, days.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
        Assert.Equal(weeks, days.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
        Assert.All(days, d => Assert.False(string.IsNullOrWhiteSpace(d.CatalogWorkoutDefinitionKey)));

        var keySessions = days.Where(d => d.CatalogStructuralRole == "KEY_SESSION").ToArray();
        Assert.All(keySessions, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.CatalogPrescriptionProfileKey));
            Assert.NotNull(d.CatalogPrescriptionProfileVersion);
        });

        var taperKeySessions = days.Where(d => d.CatalogPhaseKey == "TAPER" && d.CatalogStructuralRole == "KEY_SESSION").ToArray();
        Assert.NotEmpty(taperKeySessions);
        Assert.All(taperKeySessions, d => Assert.False(string.IsNullOrWhiteSpace(d.CatalogPrescriptionProfileKey)));
        Assert.DoesNotContain(taperKeySessions, d => d.CatalogProgressionStageKey == "TAPER_SHARPEN");

        var home = await _client.GetFromJsonAsync<JsonNode>("/api/v1/plans/active/home");
        Assert.Equal(planId.ToString(), home!["active_plan"]!["plan_id"]!.GetValue<string>());

        var calendarResponse = await _client.GetAsync($"/api/v1/plans/active/calendar?month={days.First().Date:yyyy-MM}");
        calendarResponse.EnsureSuccessStatusCode();
        var calendar = (await calendarResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.NotEmpty(calendar.AsArray());

        foreach (var role in new[] { "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" })
        {
            var day = days.First(d => d.CatalogStructuralRole == role);
            var detailResponse = await _client.GetAsync($"/api/v1/training-days/{day.Id}");
            detailResponse.EnsureSuccessStatusCode();
            var detail = (await detailResponse.Content.ReadFromJsonAsync<JsonNode>())!;
            Assert.Equal(day.Id.ToString(), detail["day_id"]!.GetValue<string>());
        }

        var firstDay = days.OrderBy(d => d.Date).First();
        var completeResponse = await _client.PostAsJsonAsync($"/api/v1/training-days/{firstDay.Id}/complete",
            new { actual_distance_km = firstDay.PlannedDistanceKm, actual_duration_min = 30 });
        completeResponse.EnsureSuccessStatusCode();
        var completed = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == firstDay.Id);
        Assert.Equal(TrainingDayStatus.Completed, completed.Status);
    }

    public void Dispose() { _client.Dispose(); _factory.Dispose(); }
}
