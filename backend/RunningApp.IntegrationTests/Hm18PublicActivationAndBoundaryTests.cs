using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase HM.18 -- real HTTP/PostgreSQL end-to-end proof for the four HM.17
/// blockers, the 8-cell public gate widening, the mandatory Advanced 5D
/// dual-KEY round-trip, and the negative/boundary acceptance matrix. Uses the
/// exact same <see cref="PublishedCatalogTestRelease"/> + <see cref="CustomWebApplicationFactory"/>
/// harness as <c>Gen4EBeginnerFourDayPublicActivationTests</c> -- the real
/// public preview/confirm/read/mutate HTTP surface, real PostgreSQL
/// persistence, no dark-only test seam.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Hm18PublicActivationAndBoundaryTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly PublishedCatalogTestRelease _release;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Hm18PublicActivationAndBoundaryTests(PublishedCatalogTestRelease release)
    {
        _release = release;
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

    public void Dispose() => _factory.Dispose();

    // ============================================================================
    // Boundary / negative acceptance matrix (HM.17 §26 blockers 1 & 2). No case
    // may surface as a raw 500.
    // ============================================================================

    [Fact]
    public async Task NineWeekHorizon_ReturnsTypedTooShort_NotComposeRequired_NotFiveHundred()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(9, level: "intermediate", days: 4));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("PLAN_CORE_HORIZON_TOO_SHORT", body);
        Assert.DoesNotContain("PLAN_HORIZON_COMPOSITION_REQUIRED", body);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(16)]
    public async Task BoundaryWeeks_TenAndSixteen_AreEligible(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(weeks, level: "intermediate", days: 4));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", body["template_id"]!.GetValue<string>());
    }

    [Fact]
    public async Task SeventeenWeekHorizon_ReturnsTypedStartTooEarly_WithRecommendedStartDate()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(17, level: "intermediate", days: 4));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_START_TOO_EARLY", body!["errorCode"]!.GetValue<string>());
        Assert.NotNull(body["recommendedStartDate"]);
        // 16 weeks = 112 days before RaceDate.
        var start = new DateOnly(2026, 7, 20);
        var raceDate = start.AddDays(17 * 7);
        var expectedRecommended = raceDate.AddDays(-16 * 7);
        Assert.Equal(expectedRecommended.ToString("yyyy-MM-dd"), body["recommendedStartDate"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("intermediate", 2)]
    [InlineData("beginner", 2)]
    [InlineData("beginner", 6)]
    [InlineData("experienced", 6)]
    // HM-X1.4 -- ("intermediate", 6) and ("advanced", 6) are no longer genuinely-unsupported
    // examples: HM-X1.4 publicly activated exactly those two cells (see
    // HmX14IntermediateAdvancedSixDayPublicActivationTests for their own real HTTP/PostgreSQL
    // acceptance proof). Replaced here with ("beginner", 6) and ("experienced", 6), which remain
    // genuinely unsupported, mirroring HM-X1.3's own §36 test-drift diagnosis/fix pattern
    // (re-pointing a stale "unsupported example" InlineData row, not weakening the assertion).
    public async Task UnsupportedHmFrequency_ReturnsTypedRejection_NotSilentLegacyFallthrough(string level, int days)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(14, level: level, days: days));
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("HM_FREQUENCY_NOT_SUPPORTED_V1", body);
    }

    [Fact]
    public async Task MissingReadiness_ReturnsTypedProductIneligible_NotFiveHundred()
    {
        // HM.18 -- discovered by this phase's own acceptance testing: HALF_MARATHON's
        // real, pre-existing volume-planning authority requires a positive observed
        // RecentWeeklyVolumeKm (no missing/zero fallback exists for HALF_MARATHON,
        // unlike TEN_K). Before this phase's CatalogPreviewGenerator fix, this
        // surfaced as an uncaught 500; it must now be a typed 422.
        await ResetAsync();
        var request = HmRequest(14, level: "intermediate", days: 4);
        var withoutVolume = JsonSerializer.SerializeToNode(request)!.AsObject();
        withoutVolume["recent_weekly_volume_km"] = null;
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", withoutVolume);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("CATALOG_VOLUME_INVALID_READINESS_INPUT", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
    }

    [Fact]
    public async Task BeginnerFiveDay_ReturnsTypedProductIneligible_NotTemplateNotFound()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(14, level: "beginner", days: 5));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("PRODUCT_INELIGIBLE", body);
    }

    // ============================================================================
    // Catalog-source invariant (HM.17 §26 blocker 4) across all 8 frozen cells.
    // ============================================================================

    [Theory]
    [InlineData("beginner", 3)]
    [InlineData("beginner", 4)]
    [InlineData("intermediate", 3)]
    [InlineData("intermediate", 4)]
    [InlineData("intermediate", 5)]
    [InlineData("advanced", 3)]
    [InlineData("advanced", 4)]
    [InlineData("advanced", 5)]
    public async Task AllEightPublicCells_PreviewIsCatalogSourced_AndConfirmPersistsCatalogRows(string level, int days)
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(14, level: level, days: days));
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedPreview = await db.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == previewId);
        using var doc = JsonDocument.Parse(storedPreview.PreviewPayloadJson);
        Assert.True(doc.RootElement.TryGetProperty("generation_source", out var gs), "Stored preview snapshot has no generation_source field.");
        Assert.Equal("CATALOG", gs.GetString(), ignoreCase: true);

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);
        var days_ = plan.Weeks.SelectMany(w => w.Days).ToArray();
        Assert.NotEmpty(days_);
        Assert.All(days_, d => Assert.Equal("CATALOG", d.GenerationSource, ignoreCase: true));
    }

    // ============================================================================
    // Mandatory acceptance proof: Advanced 5D dual-KEY round trip.
    // ============================================================================

    [Fact]
    public async Task Advanced5D_DualKeyLane_RoundTrip_DistinctIdentitiesAndIndependentStateAfterCompleteAndNotToday()
    {
        await ResetAsync();

        // 1. Generate preview via the real public preview path.
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(14, level: "advanced", days: 5));
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal("HALF_MARATHON__5D__ADVANCED", preview["template_id"]!.GetValue<string>());
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        // 2. Assert the preview's own generation_source is catalog.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedPreview = await db.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == previewId);
        using (var doc = JsonDocument.Parse(storedPreview.PreviewPayloadJson))
        {
            Assert.True(doc.RootElement.TryGetProperty("generation_source", out var gs));
            Assert.Equal("CATALOG", gs.GetString(), ignoreCase: true);
        }

        // 3. Confirm the plan via the real confirm path.
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        // 4. Inspect the persisted data directly (real repository/DB layer).
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);
        Assert.Equal("HALF_MARATHON__5D__ADVANCED", plan.CatalogCandidateKey);
        Assert.Equal(14, plan.Weeks.Count);

        // Locate a Build or Race-Specific week carrying two distinct KEY_SESSION rows.
        var dualKeyWeek = plan.Weeks
            .OrderBy(w => w.WeekNumber)
            .FirstOrDefault(w => w.Days.Count(d => d.CatalogStructuralRole == "KEY_SESSION") == 2);
        Assert.True(dualKeyWeek is not null, "No week with two KEY_SESSION rows was found -- the dual-KEY-lane shape did not survive round-trip.");
        var keySessions = dualKeyWeek!.Days.Where(d => d.CatalogStructuralRole == "KEY_SESSION").OrderBy(d => d.Date).ToArray();
        Assert.Equal(2, keySessions.Length);

        // 6. Prove they are distinct identities with distinct workout content.
        Assert.NotEqual(keySessions[0].Id, keySessions[1].Id);
        Assert.NotEqual(keySessions[0].Date, keySessions[1].Date);
        var distinctWorkoutContent = keySessions[0].CatalogWorkoutDefinitionKey != keySessions[1].CatalogWorkoutDefinitionKey
            || keySessions[0].CatalogProgressionStageKey != keySessions[1].CatalogProgressionStageKey;
        Assert.True(distinctWorkoutContent, $"KEY1 ({keySessions[0].CatalogWorkoutDefinitionKey}/{keySessions[0].CatalogProgressionStageKey}) and " +
            $"KEY2 ({keySessions[1].CatalogWorkoutDefinitionKey}/{keySessions[1].CatalogProgressionStageKey}) must carry distinct workout content.");
        Assert.All(new[] { keySessions[0], keySessions[1] }, d => Assert.Equal("CATALOG", d.GenerationSource, ignoreCase: true));

        // 5. Read Home, Calendar, Training-Day detail for the confirmed plan.
        Assert.Equal(planId.ToString(), (await _client.GetJsonAsync("/api/v1/plans/active/home"))["active_plan"]!["plan_id"]!.GetValue<string>());
        var calendarMonth = keySessions[0].Date.ToString("yyyy-MM");
        Assert.NotEmpty((await _client.GetJsonAsync($"/api/v1/plans/active/calendar?month={calendarMonth}")).AsArray());
        foreach (var key in keySessions)
        {
            var detail = await _client.GetJsonAsync($"/api/v1/training-days/{key.Id}");
            Assert.Equal(key.Id.ToString(), detail["day_id"]!.GetValue<string>());
        }

        // 7. Complete KEY1 (real mutation path).
        (await _client.PostRawAsync($"/api/v1/training-days/{keySessions[0].Id}/complete",
            new { actual_distance_km = keySessions[0].PlannedDistanceKm, actual_duration_min = 30 })).EnsureSuccessStatusCode();

        // 8. Mark KEY2 "Not Today" (real mutation path, create + confirm).
        var decision = await _client.PostJsonAsync(
            $"/api/v1/training-days/{keySessions[1].Id}/not-today-decisions",
            new { reason = "feeling_tired" });
        var decisionId = decision["decision_id"]!.GetValue<string>();
        await _client.PostJsonAsync($"/api/v1/not-today-decisions/{decisionId}/confirm", new { });

        // 9. Re-read and prove independent state tracking.
        var key1After = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == keySessions[0].Id);
        var key2After = await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == keySessions[1].Id);
        Assert.Equal(TrainingDayStatus.Completed, key1After.Status);
        Assert.Equal(TrainingDayStatus.Missed, key2After.Status);
        // Completing KEY1 must not have touched KEY2's own identity/content, and vice versa.
        Assert.Equal(keySessions[0].CatalogWorkoutDefinitionKey, key1After.CatalogWorkoutDefinitionKey);
        Assert.Equal(keySessions[1].CatalogWorkoutDefinitionKey, key2After.CatalogWorkoutDefinitionKey);
    }

    // ============================================================================
    // Lighter round-trip proof for Beginner 3D / Intermediate 4D (governing
    // prompt §17 -- steps 1-5 only).
    // ============================================================================

    [Theory]
    [InlineData("beginner", 3, "HALF_MARATHON__3D__BEGINNER")]
    [InlineData("intermediate", 4, "HALF_MARATHON__4D__INTERMEDIATE")]
    public async Task LightRoundTrip_PreviewConfirmReadPersist(string level, int days, string expectedCandidateKey)
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(14, level: level, days: days));
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(expectedCandidateKey, preview["template_id"]!.GetValue<string>());
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);
        Assert.Equal(expectedCandidateKey, plan.CatalogCandidateKey);
        Assert.Equal(14, plan.Weeks.Count);
        Assert.NotEmpty(plan.Weeks.SelectMany(w => w.Days));

        Assert.Equal(planId.ToString(), (await _client.GetJsonAsync("/api/v1/plans/active/home"))["active_plan"]!["plan_id"]!.GetValue<string>());
    }

    // ============================================================================
    // TEN_K zero-delta spot checks (HM.18 must not change TEN_K's own real,
    // already-live public behavior).
    // ============================================================================

    [Fact]
    public async Task TenKZeroDelta_FourteenWeekIntermediateFourDay_StillWorksIdentically()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", TenKRequest(14, level: "intermediate", days: 4));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal("TEN_K__4D__INTERMEDIATE", body["template_id"]!.GetValue<string>());
    }

    [Fact]
    public async Task TenKZeroDelta_FifteenWeekHorizon_StillThrowsCompositionRequired_NotStartTooEarly()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", TenKRequest(21, level: "intermediate", days: 3));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", body!["errorCode"]!.GetValue<string>());
        Assert.Null(body["recommendedStartDate"]);
    }

    private static async Task ResetAsync()
    {
        using var resetFactory = new CustomWebApplicationFactory();
        using var resetClient = resetFactory.CreateClient();
        (await resetClient.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
    }

    private static object HmRequest(int weeks, string level, int days)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "half_marathon", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"), preferred_days = preferred,
            long_run_day = preferred[^1], race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = (2 * 3600) + (5 * 60), target_finish_time_source = "product_average",
            // HALF_MARATHON's own real, frozen volume-planning authority
            // (HM.1.5/HM.7/HM.10/HM.13/HM.15-16) requires a positive OBSERVED
            // RecentWeeklyVolumeKm for every level/frequency -- there is no
            // approved missing/zero-readiness starting-volume fallback for
            // HALF_MARATHON (unlike TEN_K's LevelConservativeDefault). A null
            // value here is a genuinely different, valid negative-path input
            // (see the dedicated readiness-insufficient test), not this
            // request helper's own default.
            recent_weekly_volume_km = (double?)32, recent_longest_run_km = 14, recent_runs_per_week = days,
            race_name = "HM.18 activation test"
        };
    }

    private static object TenKRequest(int weeks, string level, int days)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "ten_k", level, days_per_week = days, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"), preferred_days = preferred,
            long_run_day = preferred[^1], race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = (double?)null, recent_longest_run_km = 8, recent_runs_per_week = days,
            race_name = "HM.18 zero-delta TEN_K check"
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
