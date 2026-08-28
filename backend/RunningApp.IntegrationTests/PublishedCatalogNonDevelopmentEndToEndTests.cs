using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Entities;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PublishedCatalogNonDevelopmentEndToEndTests :
    IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly PublishedCatalogTestRelease _release;
    private readonly CustomWebApplicationFactory _enabledFactory;
    private readonly HttpClient _enabledClient;

    public PublishedCatalogNonDevelopmentEndToEndTests(PublishedCatalogTestRelease release)
    {
        _release = release;
        _enabledFactory = Factory(release.ReleaseRoot, enabled: true);
        _enabledClient = _enabledFactory.CreateClient();
    }

    [Theory]
    [InlineData(8, 32)]
    [InlineData(12, 48)]
    [InlineData(13, 52)]
    [InlineData(14, 56)]
    public async Task ProductionEnvironment_PublishedLifecycle_NormalGate_PreviewConfirmAndReadsSucceed(
        int weeks, int sessions)
    {
        await ResetAsync();
        var start = new DateOnly(2026, 7, 20);
        var response = await _enabledClient.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(start, weeks));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.False(preview["fallback_used"]!.GetValue<bool>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        Assert.Equal(sessions, preview["weeks"]!.AsArray().Sum(w => w!["days"]!.AsArray().Count));

        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());
        var confirm = await _enabledClient.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        TrainingPlan plan;
        using (var scope = _enabledFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            plan = await db.TrainingPlans.AsNoTracking()
                .Include(p => p.Weeks).ThenInclude(w => w.Days)
                .SingleAsync(p => p.Id == planId);
        }

        Assert.Equal("TEN_K__4D__INTERMEDIATE", plan.CatalogCandidateKey);
        Assert.Equal(10, plan.CatalogCandidateVersion);
        Assert.Equal("PUBLISHED", plan.CatalogCandidateStatusAtGenerationTime);
        Assert.Equal(weeks, plan.Weeks.Count);
        var days = plan.Weeks.SelectMany(w => w.Days).ToArray();
        Assert.Equal(sessions, days.Length);

        var details = await _enabledClient.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(planId.ToString(), details["plan_id"]!.GetValue<string>());
        Assert.Equal(weeks, details["total_weeks"]!.GetValue<int>());
        var home = await _enabledClient.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Equal(planId.ToString(), home["active_plan"]!["plan_id"]!.GetValue<string>());

        var firstMonth = days.Min(d => d.Date).ToString("yyyy-MM");
        var calendar = await _enabledClient.GetJsonAsync($"/api/v1/plans/active/calendar?month={firstMonth}");
        Assert.Contains(calendar.AsArray(), d => d!["day_id"] is not null);
        var representative = days.OrderBy(d => d.Date).First();
        var day = await _enabledClient.GetJsonAsync($"/api/v1/training-days/{representative.Id}");
        Assert.Equal(representative.Id.ToString(), day["day_id"]!.GetValue<string>());
    }

    [Fact]
    public async Task ProductionEnvironment_BoundariesNotEvaluatedAndScopeContainmentRemainFailClosed()
    {
        await ResetAsync();
        var baseline = await PlanGraphCountsAsync(_enabledFactory);
        var start = new DateOnly(2026, 7, 20);

        var composition = await _enabledClient.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(start, 14, extraDays: 1));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, composition.StatusCode);
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED",
            (await composition.Content.ReadFromJsonAsync<JsonNode>())!["errorCode"]!.GetValue<string>());
        await AssertNoPlanGraphPersistenceAsync(_enabledFactory, baseline);

        var notEvaluated = await _enabledClient.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(start, 12, targetSource: "user_defined"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, notEvaluated.StatusCode);
        var error = (await notEvaluated.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal("RUNTIME_CONDITION_UNSUPPORTED", error["errorCode"]!.GetValue<string>());
        Assert.Contains("PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE", error.ToJsonString());
        await AssertNoPlanGraphPersistenceAsync(_enabledFactory, baseline);

        // Phase 10K-GEN.10 widened Advanced x4D to a real, distinct pilot
        // identity, so it's no longer a valid "unsupported Level" mutation
        // here; Experienced remains genuinely unwidened at every frequency.
        var unsupported = await _enabledClient.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(start, 12, level: "experienced"));
        Assert.NotEqual(HttpStatusCode.OK, unsupported.StatusCode);
        await AssertNoPlanGraphPersistenceAsync(_enabledFactory, baseline);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task FlagOff_PublishedCandidate_UsesEstablishedLegacyFailureWithoutCatalogPersistence(int weeks)
    {
        await ResetAsync();
        using var offFactory = Factory(_release.ReleaseRoot, enabled: false);
        using var client = offFactory.CreateClient();
        var baseline = await PlanGraphCountsAsync(offFactory);
        var response = await client.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(new DateOnly(2026, 7, 20), weeks));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal("PLAN_TEMPLATE_NOT_FOUND", error["errorCode"]!.GetValue<string>());
        await AssertNoPlanGraphPersistenceAsync(offFactory, baseline);
    }

    [Fact]
    public async Task FrozenPreviewAndActivePlanRemainUsableAfterFlagDisable()
    {
        await ResetAsync();
        var previewResponse = await _enabledClient.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(new DateOnly(2026, 7, 20), 12));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        using var offFactory = Factory(_release.ReleaseRoot, enabled: false);
        using var offClient = offFactory.CreateClient();
        var confirm = await offClient.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = confirm["plan_id"]!.GetValue<string>();
        Assert.Equal(planId, (await offClient.GetJsonAsync("/api/v1/plans/active/details"))["plan_id"]!.GetValue<string>());
        Assert.Equal(planId, (await offClient.GetJsonAsync("/api/v1/plans/active/home"))["active_plan"]!["plan_id"]!.GetValue<string>());
        Assert.NotEmpty((await offClient.GetJsonAsync("/api/v1/plans/active/calendar?month=2026-07")).AsArray());
    }

    [Fact]
    public async Task FrozenPreviewAndActivePlanRemainUsableAfterV10Retirement()
    {
        // Preview retirement: the newly retired root rejects new routing, but
        // confirmation of the already frozen snapshot remains valid.
        await ResetAsync();
        var previewResponse = await _enabledClient.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(new DateOnly(2026, 7, 20), 12));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        using var retiredRelease = new PublishedCatalogTestRelease(retireV10: true);
        using var retiredFactory = Factory(retiredRelease.ReleaseRoot, enabled: true);
        using var retiredClient = retiredFactory.CreateClient();

        var newPreview = await retiredClient.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(new DateOnly(2026, 7, 20), 12));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, newPreview.StatusCode);
        var retiredError = (await newPreview.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal("CATALOG_LIVE_PILOT_REQUEST_UNSUPPORTED",
            retiredError["errorCode"]!.GetValue<string>());

        var confirm = await retiredClient.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = confirm["plan_id"]!.GetValue<string>();
        Assert.Equal(planId, (await retiredClient.GetJsonAsync("/api/v1/plans/active/details"))["plan_id"]!.GetValue<string>());
        Assert.NotEmpty((await retiredClient.GetJsonAsync("/api/v1/plans/active/calendar?month=2026-07")).AsArray());

        // Post-confirmation retirement: create and confirm while v10 is live,
        // then switch the reader host to a release where v10 is retired.
        await ResetAsync();
        var livePreviewResponse = await _enabledClient.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(new DateOnly(2026, 7, 20), 12));
        livePreviewResponse.EnsureSuccessStatusCode();
        var livePreview = (await livePreviewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var liveConfirm = await _enabledClient.PostJsonAsync(
            "/api/v1/plans/confirm",
            new { preview_id = Guid.Parse(livePreview["preview_id"]!.GetValue<string>()) });
        var livePlanId = liveConfirm["plan_id"]!.GetValue<string>();

        using var postConfirmationRetiredRelease = new PublishedCatalogTestRelease(retireV10: true);
        using var postConfirmationRetiredFactory = Factory(postConfirmationRetiredRelease.ReleaseRoot, enabled: true);
        using var postConfirmationRetiredClient = postConfirmationRetiredFactory.CreateClient();
        Assert.Equal(livePlanId,
            (await postConfirmationRetiredClient.GetJsonAsync("/api/v1/plans/active/details"))["plan_id"]!.GetValue<string>());
        Assert.Equal(livePlanId,
            (await postConfirmationRetiredClient.GetJsonAsync("/api/v1/plans/active/home"))["active_plan"]!["plan_id"]!.GetValue<string>());
        Assert.NotEmpty((await postConfirmationRetiredClient.GetJsonAsync(
            "/api/v1/plans/active/calendar?month=2026-07")).AsArray());
    }

    private static CustomWebApplicationFactory Factory(string root, bool enabled) => new(
        "Production",
        new Dictionary<string, string?>
        {
            ["Auth:Provider"] = "Mock",
            ["PlanCatalog:CatalogRootPath"] = root,
            ["CatalogLivePilot:Enabled"] = enabled.ToString(),
            ["LocalCatalogAcceptance:Enabled"] = "false",
            ["LocalCatalogAcceptance:TreatPilotCandidateAsPublished"] = "false",
            ["LocalCatalogAcceptance:EnableCatalogRoute"] = "false",
            // Phase 4L.6B: this suite boots the real host under the
            // "Production" environment name only to exercise catalog-tier
            // Production-only routing/fallback behavior — it deliberately
            // keeps Mock auth and the test Postgres connection for
            // practicality. Full production security posture (Mock auth
            // rejected, no localhost DB fallback) is covered separately by
            // ProductionConfigurationValidatorTests, which use this same
            // environment name without disabling the gate.
            ["ProductionConfigurationValidation:Enabled"] = "false",
        });

    private static object Request(
        DateOnly start,
        int weeks,
        int extraDays = 0,
        string targetSource = "product_average",
        string level = "intermediate") => new
    {
        goal_distance = "ten_k",
        level,
        days_per_week = 4,
        unit = "km",
        start_date = start.ToString("yyyy-MM-dd"),
        preferred_days = new[] { "mon", "wed", "fri", "sun" },
        long_run_day = "sun",
        race_name = "Phase 4G.5O published release",
        race_date = start.AddDays(weeks * 7 + extraDays).ToString("yyyy-MM-dd"),
        target_finish_time_seconds = 3480,
        target_finish_time_source = targetSource,
        recent_weekly_volume_km = 20,
        recent_longest_run_km = 8,
        recent_runs_per_week = 3,
        recent_race = (object?)null,
    };

    private static async Task ResetAsync()
    {
        using var resetFactory = new CustomWebApplicationFactory();
        using var resetClient = resetFactory.CreateClient();
        (await resetClient.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
    }

    private static async Task<(int Plans, int Weeks, int Days)> PlanGraphCountsAsync(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (
            await db.TrainingPlans.CountAsync(),
            await db.TrainingWeeks.CountAsync(),
            await db.TrainingDays.CountAsync());
    }

    private static async Task AssertNoPlanGraphPersistenceAsync(
        CustomWebApplicationFactory factory,
        (int Plans, int Weeks, int Days) baseline)
    {
        Assert.Equal(baseline, await PlanGraphCountsAsync(factory));
    }

    public void Dispose()
    {
        _enabledClient.Dispose();
        _enabledFactory.Dispose();
    }
}
