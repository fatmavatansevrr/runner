using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 10K-GEN.20 -- "PHASE J": opens the real public routing gate for
/// Beginner x2D and Intermediate x2D, Core ONLY, restricted to exactly the
/// 10-14 week range GEN.18 confirmed representable. Implements only
/// already-approved authority (GEN.11 identity/product decisions,
/// GEN.14/GEN.16 Halving mechanism, GEN.12/GEN.17 dark implementation,
/// GEN.18's confirmed 10-14 week boundary) -- no new product/numeric
/// authority. Deliberately does NOT widen Preparation Runway or LongHorizon
/// for 2D (GEN.19's confirmed, still-open repeating-pattern architecture
/// gap) -- see <see cref="RunwayHorizon_TwoDay_FailsClosed_NeverReachesRunwayOrLongHorizon"/>
/// and <see cref="LongHorizonEndpoint_TwoDay_FailsClosed_NeverReachesLongHorizon"/>.
///
/// Mirrors <see cref="Gen10AdvancedCombinedPublicActivationTests"/>'s
/// established pattern for a combined public-activation phase.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Gen20TwoDayCombinedPublicActivationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Gen20TwoDayCombinedPublicActivationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    // GEN.17/GEN.18's own dark-verification values: mid-band recent evidence,
    // well clear of the missing/zero-readiness branch, per level. Beginner
    // GoldenFixtureStartingVolumeKm=11.0/ResolvedPeakReference=19.0km;
    // Intermediate 13.5/25.0km (GEN.11 §3).
    private static (double weekly, double longest) DefaultReadinessFor(string level) => level switch
    {
        "beginner" => (12.0, 5.0),
        _ => (16.0, 7.0),
    };

    private static object TwoDayRequest(
        string level, int weeks, double? recentWeeklyVolumeKm = null, double? recentLongestRunKm = null, int? recentRunsPerWeek = null)
    {
        var start = new DateOnly(2026, 9, 9); // Wednesday
        var (defaultWeekly, defaultLongest) = DefaultReadinessFor(level);
        return new
        {
            goal_distance = "ten_k", level, days_per_week = 2, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = new[] { "wed", "sun" },
            long_run_day = "sun",
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            race_name = "Phase 10K-GEN.20",
            recent_weekly_volume_km = recentWeeklyVolumeKm ?? defaultWeekly,
            recent_longest_run_km = recentLongestRunKm ?? defaultLongest,
            recent_runs_per_week = recentRunsPerWeek ?? 2,
        };
    }

    private async Task<HttpResponseMessage> PreviewRaceAsync(object request) =>
        await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

    private async Task<HttpResponseMessage> PreviewLongHorizonAsync(object request) =>
        await _client.PostRawAsync("/api/v1/plans/generate-preview/race/long-horizon", request);

    private static async Task<JsonNode> JsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonNode.Parse(body)!;
    }

    // ── Real HTTP/PostgreSQL Core activation, both levels, 10-14 weeks ────────

    [Theory]
    [InlineData("beginner", 10)] [InlineData("beginner", 11)] [InlineData("beginner", 12)]
    [InlineData("beginner", 13)] [InlineData("beginner", 14)]
    [InlineData("intermediate", 10)] [InlineData("intermediate", 11)] [InlineData("intermediate", 12)]
    [InlineData("intermediate", 13)] [InlineData("intermediate", 14)]
    public async Task CoreHorizons_TenThroughFourteenWeeks_PublicPreviewSucceeds_ForBothLevels(string level, int weeks)
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(TwoDayRequest(level, weeks));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(2, preview["days_per_week"]!.GetValue<int>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        var expectedTemplate = level == "beginner" ? "TEN_K__2D__BEGINNER" : "TEN_K__2D__INTERMEDIATE";
        Assert.Equal(expectedTemplate, preview["template_id"]!.GetValue<string>());
        Assert.All(preview["weeks"]!.AsArray(), week => Assert.Equal(2, week!["days"]!.AsArray().Count));
    }

    // ── GEN.18's formal 8/9-week non-support: fail-closed, correctly classified ──

    [Theory]
    [InlineData("beginner", 8)] [InlineData("beginner", 9)]
    [InlineData("intermediate", 8)] [InlineData("intermediate", 9)]
    public async Task EightOrNineWeeks_FailsClosed_WithGen18TypedRejection_NotSilentFallbackNotGenericError(string level, int weeks)
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(TwoDayRequest(level, weeks));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("TWO_DAY_CORE_EIGHT_OR_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL", body);
        Assert.Contains("GEN.18", body);
        // Not a silent fallback to a 10-week plan, and not the generic/opaque
        // internal-error shape the real allocator's own
        // ProgressionPhaseCapacityInsufficientException would otherwise
        // surface as (GEN.17 §6/GEN.18 §1.1's diagnosed mechanism).
        Assert.DoesNotContain("INTERNAL_ERROR", body);
        Assert.DoesNotContain("\"weeks\":10", body.Replace(" ", ""));
    }

    [Fact]
    public async Task Full8To14Matrix_BothLevels_ExactlyFiveSuccessesTwoFailures()
    {
        foreach (var level in new[] { "beginner", "intermediate" })
        {
            var successes = 0;
            var eightNineFailures = 0;
            for (var weeks = 8; weeks <= 14; weeks++)
            {
                await ResetAsync();
                var response = await PreviewRaceAsync(TwoDayRequest(level, weeks));
                if (response.IsSuccessStatusCode)
                {
                    successes++;
                }
                else if (weeks is 8 or 9)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (body.Contains("TWO_DAY_CORE_EIGHT_OR_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL"))
                    {
                        eightNineFailures++;
                    }
                }
            }
            Assert.Equal(5, successes);
            Assert.Equal(2, eightNineFailures);
        }
    }

    // ── Confirm + fresh-PostgreSQL-reload, both levels ─────────────────────────

    [Theory]
    [InlineData("beginner")]
    [InlineData("intermediate")]
    public async Task Confirm_ReloadsFromFreshPostgres_WithCorrectLevelAndDaysPerWeek(string level)
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRaceAsync(TwoDayRequest(level, 12)));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);
        var expectedLevel = level == "beginner" ? RunningBackground.Beginner : RunningBackground.Intermediate;
        Assert.Equal(expectedLevel, plan.Level);
        Assert.Equal(2, plan.DaysPerWeek);
        var expectedTemplate = level == "beginner" ? "TEN_K__2D__BEGINNER" : "TEN_K__2D__INTERMEDIATE";
        Assert.Equal(expectedTemplate, plan.CatalogCandidateKey);
        Assert.Equal(12, plan.Weeks.Count);
        var days = plan.Weeks.SelectMany(w => w.Days).ToArray();
        Assert.Equal(24, days.Length);
        Assert.Equal(12, days.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
        Assert.Equal(12, days.Count(d => d.CatalogStructuralRole is "KEY_SESSION" or "EASY_SUPPORT"));
    }

    // ── Home / Calendar / TrainingDay detail reads, real PostgreSQL ────────────

    [Fact]
    public async Task ConfirmedPlan_HomeCalendarAndTrainingDayReads_AllSucceed()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRaceAsync(TwoDayRequest("intermediate", 12)));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        var home = await _client.GetAsync("/api/v1/plans/active/home");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        var homeBody = await home.Content.ReadAsStringAsync();
        Assert.Contains(planId.ToString(), homeBody);

        var calendar = await _client.GetAsync("/api/v1/plans/active/calendar?month=2026-09");
        Assert.Equal(HttpStatusCode.OK, calendar.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await calendar.Content.ReadAsStringAsync()));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstDay = await db.TrainingDays.AsNoTracking()
            .Where(d => d.Week.PlanId == planId)
            .OrderBy(d => d.Date).FirstAsync();
        var detail = await _client.GetAsync($"/api/v1/training-days/{firstDay.Id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains(firstDay.Id.ToString(), await detail.Content.ReadAsStringAsync());
    }

    // ── Missing/zero readiness -> typed PRODUCT_INELIGIBLE (GEN.11 §7) ─────────

    [Theory]
    [InlineData("beginner")]
    [InlineData("intermediate")]
    public async Task MissingOrZeroReadiness_ReturnsTypedProductIneligible_NotGenericUnsupported(string level)
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(TwoDayRequest(level, 12, recentWeeklyVolumeKm: 0, recentLongestRunKm: 0, recentRunsPerWeek: 0));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("TWO_DAY_MISSING_OR_ZERO_READINESS_NOT_ELIGIBLE", body);
    }

    // ── Runway (15-20) and LongHorizon (21+) must remain unreachable for 2D ────

    [Theory]
    [InlineData("beginner", 15)] [InlineData("beginner", 20)]
    [InlineData("intermediate", 15)] [InlineData("intermediate", 20)]
    public async Task RunwayHorizon_TwoDay_FailsClosed_NeverReachesRunwayOrLongHorizon(string level, int weeks)
    {
        // GEN.19's confirmed architecture gap: Preparation Runway has no
        // repeating-pattern mechanism for 2D. This phase deliberately did NOT
        // widen IsSupportedPreparationRunwayLevelFrequency for 2D, so a 2D
        // request at a Runway-range horizon must fall through to the
        // existing, unmodified PlanHorizonCompositionRequiredException --
        // never a 200, never a silent attempt to use the unwired dark
        // Runway/LongHorizon path.
        await ResetAsync();
        var response = await PreviewRaceAsync(TwoDayRequest(level, weeks));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("PLAN_HORIZON_COMPOSITION_REQUIRED", body);
        Assert.DoesNotContain("TEN_K__2D__BEGINNER", body);
        Assert.DoesNotContain("TEN_K__2D__INTERMEDIATE", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
    }

    [Theory]
    [InlineData("beginner", 21)] [InlineData("beginner", 32)] [InlineData("beginner", 52)]
    [InlineData("intermediate", 21)] [InlineData("intermediate", 32)] [InlineData("intermediate", 52)]
    public async Task LongHorizonEndpoint_TwoDay_FailsClosed_NeverReachesLongHorizon(string level, int weeks)
    {
        // LongHorizonPublicPlanService.ValidatePilot was NOT touched by this
        // phase -- it still only admits Intermediate 4/5/6 and Advanced
        // 3/4/5/6, so a real 2D LongHorizon HTTP request must be rejected
        // with the existing typed LONG_HORIZON_PILOT_UNSUPPORTED error, never
        // silently routed into the unwired LongHorizon dark path GEN.19
        // found (LongHorizonStructuralMaterializer's daysPerWeek gate,
        // LongHorizonFullNumericOrchestrator's narrower gate, etc).
        await ResetAsync();
        var response = await PreviewLongHorizonAsync(TwoDayRequest(level, weeks));
        Assert.False(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("LONG_HORIZON_PILOT_UNSUPPORTED", body);
        Assert.DoesNotContain("TEN_K__2D__BEGINNER", body);
        Assert.DoesNotContain("TEN_K__2D__INTERMEDIATE", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
        Assert.DoesNotContain("rolling_long_horizon", body);
    }

    // ── Unsupported neighbors: Advanced x2D, Experienced x2D remain unreachable ──

    [Theory]
    [InlineData("advanced")]
    [InlineData("experienced")]
    public async Task UnsupportedNeighborLevels_TwoDay_RemainClosed_NoFallbackToTwoDayIdentity(string level)
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(TwoDayRequest(level, 12));
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(response.IsSuccessStatusCode, body);
        Assert.DoesNotContain("TEN_K__2D__BEGINNER", body);
        Assert.DoesNotContain("TEN_K__2D__INTERMEDIATE", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
    }

    // ── Zero-delta: every already-PUBLICLY_ACTIVE frequency unaffected ─────────

    [Fact]
    public async Task ExistingPubliclyActiveFrequencies_ZeroDelta()
    {
        await ResetAsync();
        var beginner4d = await PreviewRaceAsync(new
        {
            goal_distance = "ten_k", level = "beginner", days_per_week = 4, unit = "km",
            start_date = "2026-09-09", preferred_days = new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun", race_date = "2026-12-02",
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = 20.0, recent_longest_run_km = 8.0, recent_runs_per_week = 4,
            race_name = "GEN.20 zero-delta",
        });
        Assert.True(beginner4d.IsSuccessStatusCode, await beginner4d.Content.ReadAsStringAsync());

        foreach (var days in new[] { 3, 4, 5, 6 })
        {
            await ResetAsync();
            var intermediate = await PreviewRaceAsync(new
            {
                goal_distance = "ten_k", level = "intermediate", days_per_week = days, unit = "km",
                start_date = "2026-09-09",
                preferred_days = days switch
                {
                    3 => new[] { "mon", "wed", "sun" },
                    4 => new[] { "mon", "wed", "fri", "sun" },
                    5 => new[] { "mon", "tue", "thu", "fri", "sun" },
                    _ => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
                },
                long_run_day = "sun", race_date = "2026-12-02",
                target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
                recent_weekly_volume_km = 20.0, recent_longest_run_km = 8.0, recent_runs_per_week = days,
                race_name = "GEN.20 zero-delta",
            });
            Assert.True(intermediate.IsSuccessStatusCode, await intermediate.Content.ReadAsStringAsync());
        }

        foreach (var days in new[] { 3, 4, 5, 6 })
        {
            await ResetAsync();
            var advanced = await PreviewRaceAsync(new
            {
                goal_distance = "ten_k", level = "advanced", days_per_week = days, unit = "km",
                start_date = "2026-09-09",
                preferred_days = days switch
                {
                    3 => new[] { "mon", "wed", "sun" },
                    4 => new[] { "mon", "wed", "fri", "sun" },
                    5 => new[] { "mon", "tue", "thu", "fri", "sun" },
                    _ => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
                },
                long_run_day = "sun", race_date = "2026-12-02",
                target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
                recent_weekly_volume_km = 30.0, recent_longest_run_km = 10.0, recent_runs_per_week = days,
                race_name = "GEN.20 zero-delta",
            });
            Assert.True(advanced.IsSuccessStatusCode, await advanced.Content.ReadAsStringAsync());
        }
    }
}
