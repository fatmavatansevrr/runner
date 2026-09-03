using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 10K-GEN.25 -- "PHASE M": opens the real public routing gate for
/// Beginner x3D Core, restricted to exactly the 8-14 week range (the
/// candidate's own TEN_K_MASTER-inherited CoreCycle bounds), and only for
/// missing-readiness and positive-observed-readiness requests. Implements
/// only already-approved authority (GEN.21's diagnosed taper-minimum lever,
/// GEN.23's implemented taper-specific 3.0/2.5/3.0km minima triple and
/// representability proof, GEN.24's explicit-zero PRODUCT_INELIGIBLE
/// decision) -- no new product/numeric authority.
///
/// Explicit-zero readiness is NOT made to "work" here: GEN.24's
/// BeginnerThreeDayExplicitZeroReadinessProductIneligibleException is final
/// authority for this identity shape, and this phase's own job is to prove
/// that rejection surfaces through a REAL HTTP response with the real typed
/// reason code, not a silent fallback and not a generic 4xx.
///
/// Mirrors <see cref="Gen20TwoDayCombinedPublicActivationTests"/>'s
/// established pattern for a combined public-activation phase.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Gen25BeginnerThreeDayCorePublicActivationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Gen25BeginnerThreeDayCorePublicActivationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    // GEN.23's own real profile points (Gen23BeginnerThreeDayCoreTests.ProfileFor):
    // missing=null, explicit-zero=0, band lower=16.0km, band upper=20.0km.
    private static object ThreeDayBeginnerRequest(
        int weeks, double? recentWeeklyVolumeKm, double? recentLongestRunKm = 6.0, int? recentRunsPerWeek = 3)
    {
        var start = new DateOnly(2026, 9, 9); // Wednesday
        return new
        {
            goal_distance = "ten_k", level = "beginner", days_per_week = 3, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = new[] { "mon", "wed", "sun" },
            long_run_day = "sun",
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            race_name = "Phase 10K-GEN.25",
            recent_weekly_volume_km = recentWeeklyVolumeKm,
            recent_longest_run_km = recentLongestRunKm,
            recent_runs_per_week = recentRunsPerWeek,
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

    // ── Real HTTP/PostgreSQL Core activation, 8-14 weeks, missing readiness ───

    [Theory]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public async Task CoreHorizons_EightThroughFourteenWeeks_PublicPreviewSucceeds_MissingReadiness(int weeks)
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(ThreeDayBeginnerRequest(weeks, recentWeeklyVolumeKm: null));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(3, preview["days_per_week"]!.GetValue<int>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        Assert.Equal("TEN_K__3D__BEGINNER", preview["template_id"]!.GetValue<string>());
        Assert.All(preview["weeks"]!.AsArray(), week => Assert.Equal(3, week!["days"]!.AsArray().Count));
    }

    // ── Real HTTP/PostgreSQL Core activation, 8-14 weeks, positive-observed
    //    readiness at both band boundaries (16.0km / 20.0km) ───────────────

    [Theory]
    [InlineData(8, 16.0)] [InlineData(9, 16.0)] [InlineData(10, 16.0)] [InlineData(11, 16.0)]
    [InlineData(12, 16.0)] [InlineData(13, 16.0)] [InlineData(14, 16.0)]
    [InlineData(8, 20.0)] [InlineData(9, 20.0)] [InlineData(10, 20.0)] [InlineData(11, 20.0)]
    [InlineData(12, 20.0)] [InlineData(13, 20.0)] [InlineData(14, 20.0)]
    public async Task CoreHorizons_EightThroughFourteenWeeks_PublicPreviewSucceeds_PositiveObservedReadiness(int weeks, double weeklyVolume)
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(ThreeDayBeginnerRequest(weeks, recentWeeklyVolumeKm: weeklyVolume));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        Assert.Equal("TEN_K__3D__BEGINNER", preview["template_id"]!.GetValue<string>());
    }

    [Fact]
    public async Task Full8To14Matrix_MissingAndBothPositiveObservedProfiles_AllTwentyOneSucceed()
    {
        var successes = 0;
        foreach (var weekly in new double?[] { null, 16.0, 20.0 })
        {
            for (var weeks = 8; weeks <= 14; weeks++)
            {
                await ResetAsync();
                var response = await PreviewRaceAsync(ThreeDayBeginnerRequest(weeks, weekly));
                if (response.IsSuccessStatusCode)
                {
                    successes++;
                }
            }
        }
        Assert.Equal(21, successes);
    }

    // ── Explicit-zero readiness: GEN.24's typed PRODUCT_INELIGIBLE rejection
    //    must surface through the real HTTP response at every governed
    //    horizon -- not a silent fallback, not a generic 4xx, and not
    //    swallowed into an opaque error. ─────────────────────────────────

    [Theory]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public async Task ExplicitZero_AllGovernedHorizons_FailsClosed_WithGen24TypedRejection_NotSilentFallbackNotGenericError(int weeks)
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(ThreeDayBeginnerRequest(weeks, recentWeeklyVolumeKm: 0, recentLongestRunKm: 0, recentRunsPerWeek: 0));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BEGINNER_THREE_DAY_EXPLICIT_ZERO_READINESS_NOT_ELIGIBLE", body);
        // Not the generic/opaque 500 shape, not silently routed to the
        // sibling GEN.23 taper-volume exception, and not a silent fallback
        // to a successful plan.
        Assert.DoesNotContain("INTERNAL_ERROR", body);
        Assert.DoesNotContain("BEGINNER_THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT", body);
        Assert.DoesNotContain("\"template_id\":\"TEN_K__3D__BEGINNER\"", body.Replace(" ", ""));
    }

    [Fact]
    public async Task Full8To14Matrix_ExplicitZero_ExactlySevenTypedRejections_ZeroSuccesses()
    {
        var rejections = 0;
        var successes = 0;
        for (var weeks = 8; weeks <= 14; weeks++)
        {
            await ResetAsync();
            var response = await PreviewRaceAsync(ThreeDayBeginnerRequest(weeks, recentWeeklyVolumeKm: 0, recentLongestRunKm: 0, recentRunsPerWeek: 0));
            if (response.IsSuccessStatusCode)
            {
                successes++;
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.UnprocessableEntity &&
                    body.Contains("BEGINNER_THREE_DAY_EXPLICIT_ZERO_READINESS_NOT_ELIGIBLE"))
                {
                    rejections++;
                }
            }
        }
        Assert.Equal(0, successes);
        Assert.Equal(7, rejections);
    }

    // ── Confirm + fresh-PostgreSQL-reload ──────────────────────────────────

    [Fact]
    public async Task Confirm_ReloadsFromFreshPostgres_WithCorrectLevelAndDaysPerWeek()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRaceAsync(ThreeDayBeginnerRequest(12, recentWeeklyVolumeKm: null)));
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId, contract_version = 1 }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);
        Assert.Equal(RunningBackground.Beginner, plan.Level);
        Assert.Equal(3, plan.DaysPerWeek);
        Assert.Equal("TEN_K__3D__BEGINNER", plan.CatalogCandidateKey);
        Assert.Equal(12, plan.Weeks.Count);
        var days = plan.Weeks.SelectMany(w => w.Days).ToArray();
        Assert.Equal(36, days.Length);
        Assert.Equal(12, days.Count(d => d.CatalogStructuralRole == "LONG_RUN"));
        Assert.Equal(12, days.Count(d => d.CatalogStructuralRole == "KEY_SESSION"));
        Assert.Equal(12, days.Count(d => d.CatalogStructuralRole == "EASY_SUPPORT"));
    }

    // ── Home / Calendar / TrainingDay detail reads, real PostgreSQL ────────

    [Fact]
    public async Task ConfirmedPlan_HomeCalendarAndTrainingDayReads_AllSucceed()
    {
        await ResetAsync();
        var preview = await JsonAsync(await PreviewRaceAsync(ThreeDayBeginnerRequest(12, recentWeeklyVolumeKm: 16.0)));
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

    // ── Runway (15-20) and LongHorizon (21+) must remain unreachable ──────

    [Theory]
    [InlineData(15)] [InlineData(20)]
    public async Task RunwayHorizon_BeginnerThreeDay_FailsClosed_NeverReachesRunwayOrLongHorizon(int weeks)
    {
        // Beginner x3D Preparation Runway was never designed or approved by
        // any prior phase (GEN.21/GEN.23/GEN.24 are all Core-only). This
        // phase deliberately did NOT widen IsSupportedPreparationRunwayLevelFrequency
        // for Beginner x3D, so a request at a Runway-range horizon must fall
        // through to the existing, unmodified PlanHorizonCompositionRequiredException.
        await ResetAsync();
        var response = await PreviewRaceAsync(ThreeDayBeginnerRequest(weeks, recentWeeklyVolumeKm: null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("PLAN_HORIZON_COMPOSITION_REQUIRED", body);
        Assert.DoesNotContain("TEN_K__3D__BEGINNER", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
    }

    [Theory]
    [InlineData(21)] [InlineData(32)] [InlineData(52)]
    public async Task LongHorizonEndpoint_BeginnerThreeDay_FailsClosed_NeverReachesLongHorizon(int weeks)
    {
        // LongHorizonPublicPlanService.ValidatePilot was NOT touched by this
        // phase -- it still only admits Intermediate 4/5/6 and Advanced
        // 3/4/5/6, so a real Beginner x3D LongHorizon HTTP request must be
        // rejected with the existing typed LONG_HORIZON_PILOT_UNSUPPORTED
        // error.
        await ResetAsync();
        var response = await PreviewLongHorizonAsync(ThreeDayBeginnerRequest(weeks, recentWeeklyVolumeKm: null));
        Assert.False(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("LONG_HORIZON_PILOT_UNSUPPORTED", body);
        Assert.DoesNotContain("TEN_K__3D__BEGINNER", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
        Assert.DoesNotContain("rolling_long_horizon", body);
    }

    // ── Zero-delta: Beginner x4D/x2D, Intermediate (all supported
    //    frequencies), Advanced 3D-6D all unaffected -- real HTTP ─────────

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
            race_name = "GEN.25 zero-delta Beginner 4D",
        });
        Assert.True(beginner4d.IsSuccessStatusCode, await beginner4d.Content.ReadAsStringAsync());

        await ResetAsync();
        var beginner2d = await PreviewRaceAsync(new
        {
            goal_distance = "ten_k", level = "beginner", days_per_week = 2, unit = "km",
            start_date = "2026-09-09", preferred_days = new[] { "wed", "sun" },
            long_run_day = "sun", race_date = "2026-12-02",
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = 12.0, recent_longest_run_km = 5.0, recent_runs_per_week = 2,
            race_name = "GEN.25 zero-delta Beginner 2D",
        });
        Assert.True(beginner2d.IsSuccessStatusCode, await beginner2d.Content.ReadAsStringAsync());

        foreach (var days in new[] { 2, 3, 4, 5, 6 })
        {
            await ResetAsync();
            var intermediate = await PreviewRaceAsync(new
            {
                goal_distance = "ten_k", level = "intermediate", days_per_week = days, unit = "km",
                start_date = "2026-09-09",
                preferred_days = days switch
                {
                    2 => new[] { "wed", "sun" },
                    3 => new[] { "mon", "wed", "sun" },
                    4 => new[] { "mon", "wed", "fri", "sun" },
                    5 => new[] { "mon", "tue", "thu", "fri", "sun" },
                    _ => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
                },
                long_run_day = "sun", race_date = "2026-12-02",
                target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
                recent_weekly_volume_km = 20.0, recent_longest_run_km = 8.0, recent_runs_per_week = days,
                race_name = "GEN.25 zero-delta Intermediate",
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
                race_name = "GEN.25 zero-delta Advanced",
            });
            Assert.True(advanced.IsSuccessStatusCode, await advanced.Content.ReadAsStringAsync());
        }
    }

    // ── Zero-delta: Beginner x4D's own explicit-zero handling untouched ───

    [Theory]
    [InlineData(8, false)]  // taper 7.5km, ineligible -- BeginnerFourDayCoreProductIneligibleException, unchanged
    [InlineData(13, true)]  // taper 9.5km, eligible -- 9.5km start resolves unchanged
    public async Task BeginnerFourDay_ExplicitZeroHandling_IsCompletelyUnaffected_ZeroDelta(int weeks, bool eligible)
    {
        await ResetAsync();
        var request = new
        {
            goal_distance = "ten_k", level = "beginner", days_per_week = 4, unit = "km",
            start_date = "2026-09-09", preferred_days = new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun", race_date = new DateOnly(2026, 9, 9).AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = 0, recent_longest_run_km = 0, recent_runs_per_week = 0,
            race_name = "GEN.25 zero-delta Beginner 4D explicit-zero",
        };
        var response = await PreviewRaceAsync(request);
        if (!eligible)
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("BEGINNER_FOUR_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT", body);
            // Never the new Beginner x3D-specific exception this phase's
            // parent authority (GEN.24) added.
            Assert.DoesNotContain("BEGINNER_THREE_DAY_EXPLICIT_ZERO_READINESS_NOT_ELIGIBLE", body);
        }
        else
        {
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        }
    }

    // ── Unsupported neighbors: Beginner x5D/x6D/x7D remain unreachable ────

    [Theory]
    [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public async Task UnsupportedNeighborFrequencies_BeginnerFiveSixSevenDay_RemainClosed(int days)
    {
        await ResetAsync();
        var preferred = days switch
        {
            5 => new[] { "mon", "tue", "thu", "fri", "sun" },
            6 => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
            _ => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" },
        };
        var response = await PreviewRaceAsync(new
        {
            goal_distance = "ten_k", level = "beginner", days_per_week = days, unit = "km",
            start_date = "2026-09-09", preferred_days = preferred,
            long_run_day = "sun", race_date = "2026-12-02",
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = 20.0, recent_longest_run_km = 8.0, recent_runs_per_week = days,
            race_name = "GEN.25 unsupported-neighbor Beginner",
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(response.IsSuccessStatusCode, body);
        Assert.DoesNotContain("TEN_K__3D__BEGINNER", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
    }

    // ── Unsupported neighbor levels at 3D: Advanced x3D unaffected (already
    //    publicly active, GEN.10), Experienced x3D remains unreachable ────

    [Fact]
    public async Task AdvancedThreeDay_RemainsPubliclyActive_ZeroDelta()
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(new
        {
            goal_distance = "ten_k", level = "advanced", days_per_week = 3, unit = "km",
            start_date = "2026-09-09", preferred_days = new[] { "mon", "wed", "sun" },
            long_run_day = "sun", race_date = "2026-12-02",
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = 30.0, recent_longest_run_km = 10.0, recent_runs_per_week = 3,
            race_name = "GEN.25 zero-delta Advanced 3D",
        });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExperiencedThreeDay_RemainsClosed_NoFallbackToBeginnerThreeDayIdentity()
    {
        await ResetAsync();
        var response = await PreviewRaceAsync(new
        {
            goal_distance = "ten_k", level = "experienced", days_per_week = 3, unit = "km",
            start_date = "2026-09-09", preferred_days = new[] { "mon", "wed", "sun" },
            long_run_day = "sun", race_date = "2026-12-02",
            target_finish_time_seconds = 3480, target_finish_time_source = "product_average",
            recent_weekly_volume_km = 20.0, recent_longest_run_km = 8.0, recent_runs_per_week = 3,
            race_name = "GEN.25 unsupported-neighbor Experienced 3D",
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(response.IsSuccessStatusCode, body);
        Assert.DoesNotContain("TEN_K__3D__BEGINNER", body);
        Assert.DoesNotContain("INTERNAL_ERROR", body);
    }
}
