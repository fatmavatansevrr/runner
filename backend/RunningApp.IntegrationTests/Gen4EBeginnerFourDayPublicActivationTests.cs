using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Gen4EBeginnerFourDayPublicActivationTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Gen4EBeginnerFourDayPublicActivationTests(PublishedCatalogTestRelease release)
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

    [Theory]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public async Task EligibleFourDayCoreHorizon_PublicPreviewHasExactlyFourRoles(int weeks)
    {
        // GEN.4C.4's frozen missing-readiness matrix: all of 8-14 ELIGIBLE.
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(weeks));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        Assert.All(preview["weeks"]!.AsArray(), week => Assert.Equal(4, week!["days"]!.AsArray().Count));
        Assert.Equal("TEN_K__4D__BEGINNER", preview["template_id"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)] [InlineData(12)]
    public async Task ExplicitZeroShortCore_ReturnsTypedProductIneligibility(int weeks)
    {
        // GEN.4C.3's frozen explicit-zero matrix: 8-12 PRODUCT_INELIGIBLE.
        // Week 8 specifically also exercises the GEN.4E fix that scoped
        // LivePlanPreviewRouting's known-infeasible-eight-week short-circuit
        // to Intermediate only, so Beginner's week-8 case reaches the real
        // typed exception instead of the Intermediate-shared generic one.
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(weeks, weekly: 0));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BEGINNER_FOUR_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
    }

    [Theory]
    [InlineData(13)] [InlineData(14)]
    public async Task ExplicitZeroAtOrAboveBreakEven_Generates(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(weeks, weekly: 0));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TwelveWeek_ResetPreviewConfirmReadAndComplete_PersistsExactlyFortyEightDays()
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(12, weekly: 20));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);
        Assert.Equal(4, plan.DaysPerWeek);
        Assert.Equal("TEN_K__4D__BEGINNER", plan.CatalogCandidateKey);
        Assert.Equal(12, plan.Weeks.Count);
        var days = plan.Weeks.SelectMany(w => w.Days).ToArray();
        Assert.Equal(48, days.Length);
        Assert.Equal(12, days.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
        Assert.Equal(24, days.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
        Assert.Equal(12, days.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
        Assert.All(days, d => Assert.False(string.IsNullOrWhiteSpace(d.CatalogWorkoutDefinitionKey)));
        Assert.DoesNotContain(days, d => d.CatalogWorkoutDefinitionKey is "FARTLEK" or "THRESHOLD_TEMPO");

        Assert.Equal(planId.ToString(), (await _client.GetJsonAsync("/api/v1/plans/active/home"))["active_plan"]!["plan_id"]!.GetValue<string>());
        Assert.NotEmpty((await _client.GetJsonAsync("/api/v1/plans/active/calendar?month=2026-07")).AsArray());
        foreach (var role in new[] { "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" })
        {
            var day = days.First(d => d.CatalogStructuralRole == role);
            var detail = await _client.GetJsonAsync($"/api/v1/training-days/{day.Id}");
            Assert.Equal(day.Id.ToString(), detail["day_id"]!.GetValue<string>());
        }
        var completed = days.OrderBy(d => d.Date).First();
        (await _client.PostRawAsync($"/api/v1/training-days/{completed.Id}/complete",
            new { actual_distance_km = completed.PlannedDistanceKm, actual_duration_min = 30 })).EnsureSuccessStatusCode();
        Assert.Equal(TrainingDayStatus.Completed, (await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == completed.Id)).Status);
    }

    [Theory]
    [InlineData(15)] [InlineData(20)] [InlineData(21)] [InlineData(52)]
    public async Task NonCoreFourDayBeginnerHorizons_RemainUnactivated(int weeks)
    {
        // Runway (15-20) and LongHorizon (21+) must not be widened for
        // Beginner by this phase -- same containment GEN.3B established for
        // 3D at these horizons.
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(weeks));
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Theory]
    [InlineData(15)] [InlineData(20)]
    public async Task RunwayHorizonBeginner_TypedRejection_NoSilentIntermediateCoercion(int weeks)
    {
        // FREQ.2's finding: PreparationRunway's numeric pipeline is
        // architecturally hardcoded to Intermediate x4D specifically (its
        // scope gate checks Level == Intermediate, not just DaysPerWeek).
        // FREQ.2A: confirm Beginner x4D (publicly active) gets a typed 422
        // rejection at Runway horizons, never a 200 with silently-
        // substituted Intermediate content.
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(weeks));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("PLAN_HORIZON_COMPOSITION_REQUIRED", body);
        Assert.DoesNotContain("TEN_K__4D__INTERMEDIATE", body);
    }

    [Theory]
    [InlineData("beginner", 3)]
    [InlineData("advanced", 4)]
    [InlineData("beginner", 5)]
    public async Task WrongCombination_NeverNearestMatches(string level, int days)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(12, level: level, days: days));
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task IntermediateFourDayAndThreeDay_ZeroRegression()
    {
        // GEN.4E must not change Intermediate's existing public behavior.
        await ResetAsync();
        var fourDay = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(12, level: "intermediate", days: 4, weekly: 20));
        Assert.Equal(HttpStatusCode.OK, fourDay.StatusCode);
        var fourDayBody = (await fourDay.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal("TEN_K__4D__INTERMEDIATE", fourDayBody["template_id"]!.GetValue<string>());

        await ResetAsync();
        var threeDay = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(12, level: "intermediate", days: 3, weekly: 20));
        Assert.Equal(HttpStatusCode.OK, threeDay.StatusCode);
        var threeDayBody = (await threeDay.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal("TEN_K__3D__INTERMEDIATE", threeDayBody["template_id"]!.GetValue<string>());
    }

    private static async Task ResetAsync()
    {
        using var resetFactory = new CustomWebApplicationFactory();
        using var resetClient = resetFactory.CreateClient();
        (await resetClient.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
    }

    private static object Request(int weeks, double? weekly = null, string level = "beginner", int days = 4)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = days == 4 ? new[] { "mon", "wed", "fri", "sun" } : days == 3
            ? new[] { "mon", "wed", "sun" }
            : Enumerable.Range(0, days).Select(i => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }[i]).ToArray();
        return new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"), preferred_days = preferred,
            long_run_day = preferred[^1], race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = weekly, recent_longest_run_km = 8, recent_runs_per_week = days,
            race_name = "GEN.4E Beginner 4D activation"
        };
    }

    public void Dispose() { _client.Dispose(); _factory.Dispose(); }
}
