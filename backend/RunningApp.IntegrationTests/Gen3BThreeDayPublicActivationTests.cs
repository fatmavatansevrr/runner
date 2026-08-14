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
public sealed class Gen3BThreeDayPublicActivationTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Gen3BThreeDayPublicActivationTests(PublishedCatalogTestRelease release)
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
    public async Task EligibleThreeDayCoreHorizon_PublicPreviewHasExactlyThreeRoles(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(weeks));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        Assert.All(preview["weeks"]!.AsArray(), week => Assert.Equal(3, week!["days"]!.AsArray().Count));
        Assert.Equal("TEN_K__3D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    public async Task ExplicitZeroShortCore_ReturnsTypedProductIneligibility(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(weeks, weekly: 0));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
    }

    [Theory]
    [InlineData(2)] [InlineData(4)]
    public async Task ThreeDayPreferredDayCountMismatch_IsValidationError(int preferredCount)
    {
        await ResetAsync();
        var start = new DateOnly(2026, 7, 20);
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", new
        {
            goal_distance = "ten_k", level = "intermediate", days_per_week = 3, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"), preferred_days = new[] { "mon", "wed", "fri", "sun" }.Take(preferredCount),
            long_run_day = "mon", race_date = start.AddDays(84).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ThreeDayPreferredDayMembership_IsValidated(bool duplicateDays)
    {
        await ResetAsync();
        var start = new DateOnly(2026, 7, 20);
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", new
        {
            goal_distance = "ten_k", level = "intermediate", days_per_week = 3, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = duplicateDays ? new[] { "mon", "mon", "sun" } : new[] { "mon", "wed", "sun" },
            long_run_day = duplicateDays ? "sun" : "fri",
            race_date = start.AddDays(84).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TwelveWeek_ResetPreviewConfirmReadAndComplete_PersistsExactlyThirtySixDays()
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
        Assert.Equal(3, plan.DaysPerWeek);
        Assert.Equal("TEN_K__3D__INTERMEDIATE", plan.CatalogCandidateKey);
        Assert.Equal(12, plan.Weeks.Count);
        var days = plan.Weeks.SelectMany(w => w.Days).ToArray();
        Assert.Equal(36, days.Length);
        Assert.Equal(12, days.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
        Assert.Equal(12, days.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
        Assert.Equal(12, days.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
        Assert.All(days, d => Assert.False(string.IsNullOrWhiteSpace(d.CatalogWorkoutDefinitionKey)));

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
    public async Task NonCoreThreeDayHorizons_RemainUnactivated(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(weeks));
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Theory]
    [InlineData("beginner", 3)]
    [InlineData("advanced", 3)]
    [InlineData("intermediate", 5)]
    public async Task WrongCombination_NeverNearestMatches(string level, int days)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Request(12, level: level, days: days));
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task ResetAsync()
    {
        using var resetFactory = new CustomWebApplicationFactory();
        using var resetClient = resetFactory.CreateClient();
        (await resetClient.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
    }

    private static object Request(int weeks, double? weekly = null, string level = "intermediate", int days = 3)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = days == 3 ? new[] { "mon", "wed", "sun" } : Enumerable.Range(0, days).Select(i => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }[i]).ToArray();
        return new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"), preferred_days = preferred,
            long_run_day = preferred[^1], race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = weekly, recent_longest_run_km = 8, recent_runs_per_week = 3,
            race_name = "GEN.3B 3D activation"
        };
    }

    public void Dispose() { _client.Dispose(); _factory.Dispose(); }
}
