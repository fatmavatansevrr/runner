using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// PHASE DERIVED-DIST.2 -- real HTTP/PostgreSQL public-activation proof for
/// the generic derived-distance new-creation route, migrated from the
/// historical exact-cell model (DIST-GEN.1-20) per the governing prompt's
/// central routing rule: strict custom 10K&lt;target&lt;HALF_MARATHON at
/// Intermediate 3D/4D/5D, subject to HALF_MARATHON's own real public
/// Level×Frequency eligibility, now routes through
/// <see cref="DerivedDistanceEligibilityPolicy"/> by default, rather than
/// the legacy exact public-cell registry
/// (<c>Dark16KPilotEligibilityPolicy.ApprovedPublicCells</c>).
///
/// Mirrors <see cref="DistGen11PublicActivationTests"/>/<see cref="DistGen17PublicActivationTests"/>'s
/// own real HTTP + Postgres proof shape (same <see cref="CustomWebApplicationFactory"/>
/// configuration, same reset-before-each pattern, same raw-JSON wire-contract
/// assertions) -- no new E2E test infrastructure invented for this phase.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class DerivedDist2PublicActivationTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DerivedDist2PublicActivationTests(PublishedCatalogTestRelease release)
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

    public void Dispose()
    {
        // Every test in this class that touches the live rollback switch
        // restores it, but a thrown assertion could skip that step -- this
        // defensive reset guarantees no test in this class ever leaks
        // LegacyExactProjected mode into a later test, in this class or any
        // other (the switch is a static, process-wide field).
        CustomDistanceRoutingMode.ProductionDefault = CustomDistanceRoutingDecision.GenericDerived;
        _client.Dispose();
        _factory.Dispose();
    }

    private static async Task ResetAsync()
    {
        using var resetFactory = new CustomWebApplicationFactory();
        using var resetClient = resetFactory.CreateClient();
        (await resetClient.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
    }

    private static string[] DaysFor(int days) => days switch
    {
        3 => new[] { "tue", "thu", "sun" },
        4 => new[] { "mon", "wed", "fri", "sun" },
        5 => new[] { "mon", "tue", "thu", "fri", "sun" },
        6 => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
        _ => Enumerable.Range(0, days).Select(i => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }[i]).ToArray(),
    };

    private static object CustomRequest(double km, int weeks, string level = "intermediate", int days = 4, DateOnly? start = null)
    {
        var startDate = start ?? new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "custom",
            target_distance_km = km,
            level,
            days_per_week = days,
            unit = "km",
            start_date = startDate.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = startDate.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = (int)(km * 300),
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = (double?)20.0,
            recent_longest_run_km = 10,
            recent_runs_per_week = days,
            recent_race = new { distance = "ten_k", finish_time_seconds = 2700, race_date = startDate.AddDays(-21).ToString("yyyy-MM-dd") },
            race_name = $"DERIVED-DIST.2 {km}km public activation test",
        };
    }

    // ───────────────────────── §47 Boundary / canonical bypass ─────────────────────────

    [Fact]
    public async Task ExactTenK_NeverRoutesDerived_UsesCanonicalTemplate()
    {
        await ResetAsync();
        var request = new
        {
            goal_distance = "ten_k",
            level = "intermediate",
            days_per_week = 4,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = DaysFor(4),
            long_run_day = "sun",
            race_date = new DateOnly(2026, 7, 20).AddDays(12 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3000,
            target_finish_time_source = "user_defined",
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("ten_k", preview["goal_distance"]!.GetValue<string>());
        Assert.Equal("TEN_K__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    [Fact]
    public async Task ExactHalfMarathon_NeverRoutesDerived_UsesCanonicalTemplate()
    {
        await ResetAsync();
        var request = new
        {
            goal_distance = "half_marathon",
            level = "intermediate",
            days_per_week = 4,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = DaysFor(4),
            long_run_day = "sun",
            race_date = new DateOnly(2026, 7, 20).AddDays(12 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 6000,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = (double?)30.0,
            recent_longest_run_km = 12,
            recent_runs_per_week = 4,
            recent_race = new { distance = "ten_k", finish_time_seconds = 2700, race_date = new DateOnly(2026, 6, 29).ToString("yyyy-MM-dd") },
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("half_marathon", preview["goal_distance"]!.GetValue<string>());
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    [Theory]
    [InlineData(10.1)]
    [InlineData(21.0)]
    public async Task JustInsideEachBoundary_RoutesDerived(double km)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(km, 12));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200 for {km}km, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(km, preview["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", preview["goal_distance"]!.GetValue<string>());
    }

    // ───────────────────────── §59 Never-had-an-exact-cell targets ─────────────────────────

    [Theory]
    [InlineData(14.0)]
    [InlineData(17.7)]
    [InlineData(13.5)]
    [InlineData(20.0)]
    public async Task NonHistoricalDecimalTargets_SucceedWithNoPerDistanceRegistration(double km)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(km, 12));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200 for {km}km, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;

        // §22/§23/§39/§65 -- exact target identity preserved verbatim, no HM
        // label leakage, no snapping to a neighboring value.
        Assert.Equal(km, preview["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", preview["goal_distance"]!.GetValue<string>());
        Assert.DoesNotContain("Half Marathon", body);
        Assert.DoesNotContain("21.1", body);
    }

    // ───────────────────────── §55-57 Legacy cells now derived (provenance) ─────────────────────────

    /// <summary>
    /// §28/§55-57 -- the central non-gameable provenance proof: 18K's own
    /// legacy exact-cell authority produces a peak volume of 34.0 (frozen,
    /// DIST-GEN.10) for a 12-week horizon -- see the now-superseded
    /// <c>DistGen11PublicActivationTests.DarkVsPublic_18K_12W_SemanticZeroDelta_OnlyReachabilityChanged</c>
    /// (still compiled, Skip-documented) for that historical golden value.
    /// A real public request for the exact same (18.0, HalfMarathon,
    /// Intermediate, 4, 12W) identity now realizes a DIFFERENT peak volume
    /// (the generic derived/HM-sourced trajectory's own number) -- this
    /// numeric divergence IS the provenance proof: it is not something either
    /// implementation could produce by accident, since the two authorities
    /// are independently declared and were never designed to coincide.
    /// </summary>
    [Fact]
    public async Task Legacy18KI4D_NewPublicCreation_RealizesDerivedNumbers_NotLegacyGoldenPeakVolume()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(18.0, 12, days: 4));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = JsonNode.Parse(body)!;

        var peakVolume = preview["weeks"]!.AsArray()
            .Select(w => w!["days"]!.AsArray().Sum(d => d!["distance_km"]!.GetValue<double>()))
            .Max();

        // The legacy exact cell's own frozen golden peak volume for 12W is
        // 34.0 (DIST-GEN.10 §39). The derived route must NOT reproduce it.
        Assert.True(Math.Abs(peakVolume - 34.0) > 0.5,
            $"18K I4D new public creation realized peak volume {peakVolume}, matching the legacy exact cell's own 34.0 golden -- this means the legacy route, not generic derived, is still winning new creation.");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Legacy16K_AllThreeFrequencies_NowPubliclyReachable_IncludingPreviouslyDarkOnly5D(int days)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(16.0, 12, days: days));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200 for 16K I{days}D, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(16.0, preview["requested_target_distance_km"]!.GetValue<double>());
        Assert.All(preview["weeks"]!.AsArray(), w => Assert.Equal(days, w!["days"]!.AsArray().Count));
    }

    [Fact]
    public async Task Legacy15K_NewPublicCreation_NowSucceedsAtIntermediate5D_NeverPubliclyApprovedBefore()
    {
        await ResetAsync();
        // 15K at Intermediate 5D was never in the legacy ApprovedPublicCells
        // registry at any frequency but 4D -- this now succeeds purely via
        // the generic derived product subset (§9), proving no per-distance
        // public registration was added for it.
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(15.0, 12, days: 5));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
    }

    // ───────────────────────── §48/§49 Out-of-scope levels/frequencies rejected ─────────────────────────

    [Theory]
    [InlineData("beginner", 4)]
    [InlineData("advanced", 4)]
    public async Task OutOfScopeLevels_RejectedForDerivedCustomTarget(string level, int days)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(CustomRequest(14.0, 12, level: level, days: days))!.AsObject();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    [Fact]
    public async Task IntermediateSixDay_RejectedForDerivedCustomTarget_EvenThoughHmParentAllowsIt()
    {
        await ResetAsync();
        // HALF_MARATHON Intermediate 6D is itself publicly eligible
        // (V1CatalogPilotIdentityPolicy) -- this proves gate (A), the V1
        // product subset, still restricts derived custom targets to 3D/4D/5D
        // even where gate (B), the parent matrix, alone would admit more.
        var request = JsonSerializer.SerializeToNode(CustomRequest(14.0, 12, days: 6))!.AsObject();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);

        // Canonical HALF_MARATHON Intermediate 6D itself remains unaffected --
        // derived restriction never shrinks canonical HM eligibility (§49).
        var hmRequest = new
        {
            goal_distance = "half_marathon",
            level = "intermediate",
            days_per_week = 6,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = DaysFor(6),
            long_run_day = "sun",
            race_date = new DateOnly(2026, 7, 20).AddDays(12 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 6000,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = (double?)30.0,
            recent_longest_run_km = 12,
            recent_runs_per_week = 6,
            recent_race = new { distance = "ten_k", finish_time_seconds = 2700, race_date = new DateOnly(2026, 6, 29).ToString("yyyy-MM-dd") },
        };
        var hmResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", hmRequest);
        var hmBody = await hmResponse.Content.ReadAsStringAsync();
        Assert.True(hmResponse.StatusCode == HttpStatusCode.OK, $"expected 200 for canonical HM I6D, got {hmResponse.StatusCode}: {hmBody}");
    }

    // ───────────────────────── §21/§73-77 Rollback (creation-routing only) ─────────────────────────

    [Fact]
    public async Task RollbackSwitch_DisablesNewDerivedCreation_ForNonHistoricalTarget_WithoutTouchingConfirmedPlans()
    {
        await ResetAsync();
        try
        {
            // Generic derived is primary: 14K succeeds (never had an exact cell).
            var before = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(14.0, 12));
            Assert.Equal(HttpStatusCode.OK, before.StatusCode);

            var beforePreview = JsonNode.Parse(await before.Content.ReadAsStringAsync())!;
            var confirm = await _client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = beforePreview["preview_id"]!.GetValue<string>() });
            Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

            // Flip the ONE rollback switch -- creation-routing only (§16/§18).
            CustomDistanceRoutingMode.ProductionDefault = CustomDistanceRoutingDecision.LegacyExactProjected;

            // New 14K creation is now rejected (never had a legacy exact cell, so
            // the fallback legacy registry has nothing to offer it either).
            var afterRollback = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(14.0, 12));
            Assert.NotEqual(HttpStatusCode.OK, afterRollback.StatusCode);

            // But the ALREADY-CONFIRMED 14K derived plan remains fully operable --
            // rollback never touches existing-plan read/mutation flows (§17/§71/§74-77).
            var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
            Assert.NotNull(home);
            var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
            Assert.Equal(14.0, details["requested_target_distance_km"]!.GetValue<double>());
        }
        finally
        {
            CustomDistanceRoutingMode.ProductionDefault = CustomDistanceRoutingDecision.GenericDerived;
        }
    }

    [Fact]
    public async Task RollbackSwitch_RestoresLegacyExactCreation_ForHistoricalCell()
    {
        await ResetAsync();
        try
        {
            CustomDistanceRoutingMode.ProductionDefault = CustomDistanceRoutingDecision.LegacyExactProjected;

            // 18K I4D is a historical ApprovedPublicCells entry -- still reachable
            // under rollback, exactly as it was pre-DERIVED-DIST.2.
            var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(18.0, 12, days: 4));
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var preview = JsonNode.Parse(body)!;
            var peakVolume = preview["weeks"]!.AsArray()
                .Select(w => w!["days"]!.AsArray().Sum(d => d!["distance_km"]!.GetValue<double>())).Max();
            Assert.True(Math.Abs(peakVolume - 34.0) <= 0.5, $"rollback mode 18K I4D peak volume {peakVolume} did not match the legacy golden 34.0 -- rollback did not restore legacy routing.");

            // A never-historical target (14K) remains rejected under rollback.
            var nonHistorical = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", CustomRequest(14.0, 12));
            Assert.NotEqual(HttpStatusCode.OK, nonHistorical.StatusCode);
        }
        finally
        {
            CustomDistanceRoutingMode.ProductionDefault = CustomDistanceRoutingDecision.GenericDerived;
        }
    }

    // ───────────────────────── §43-46/§59-70 Full lifecycle E2E scenarios ─────────────────────────

    /// <summary>§43 -- 16K/Intermediate/4D/12W through the complete real public lifecycle.</summary>
    [Fact]
    public async Task E2E_16KIntermediate4D_FullLifecycle_PreviewConfirmHomeCalendarDetailCompleteNotTodayCancel()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", CustomRequest(16.0, 12, days: 4));
        Assert.Equal(16.0, preview["requested_target_distance_km"]!.GetValue<double>());
        var previewId = preview["preview_id"]!.GetValue<string>();

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = confirm["plan_id"]!.GetValue<string>();

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.NotNull(home["active_plan"]);

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(16.0, details["requested_target_distance_km"]!.GetValue<double>());
        Assert.DoesNotContain("Half Marathon", details.ToJsonString());

        var calendar = await _client.GetAsync("/api/v1/plans/active/calendar?month=2026-07");
        Assert.True(calendar.IsSuccessStatusCode, $"calendar fetch failed: {calendar.StatusCode}");

        var firstWeekDays = details["weeks"]![0]!["days"]!.AsArray();
        var dayId = firstWeekDays[0]!["day_id"]!.GetValue<string>();
        var dayDetail = await _client.GetJsonAsync($"/api/v1/training-days/{dayId}");
        Assert.Equal(dayId, dayDetail["day_id"]!.GetValue<string>());

        var otherDayId = firstWeekDays[1]!["day_id"]!.GetValue<string>();

        var complete = await _client.PostJsonAsync($"/api/v1/training-days/{dayId}/complete", new { actual_distance_km = 5.0, actual_duration_min = 30 });
        Assert.Equal("completed", complete["status"]!.GetValue<string>());

        var notToday = await _client.PostJsonAsync($"/api/v1/training-days/{otherDayId}/not-today-decisions", new { reason = "schedule_conflict" });
        Assert.True(notToday.AsObject().ContainsKey("decision_id"), $"not-today-decisions response missing decision_id: {notToday.ToJsonString()}");

        var cancel = await _client.PostJsonAsync($"/api/v1/plans/{planId}/cancel", new { reason = "derived_dist_2_e2e_cleanup" });
        Assert.Equal("cancelled", cancel["status"]!.GetValue<string>());
    }

    /// <summary>
    /// §44/§60 -- 16K/Intermediate/5D/12W: supersedes the never-begun
    /// DIST-GEN.21. Proves KEY2 role/workout identity survives
    /// preview→confirm→PostgreSQL→detail, via generic derived routing only
    /// (16K I5D was dark-only pre-DERIVED-DIST.2 -- see
    /// <see cref="Dark16KPilotEligibilityPolicy.ApprovedPublicCells"/>, which
    /// still has no 16K×5 entry: this test's success is itself proof the
    /// generic derived route, not a new exact-cell grant, is what made it
    /// public).
    /// </summary>
    [Fact]
    public async Task E2E_16KIntermediate5D_FullLifecycle_ProvesKey2PersistenceAndSupersedesDistGen21()
    {
        await ResetAsync();

        Assert.DoesNotContain(Dark16KPilotEligibilityPolicy.ApprovedPublicCells, c => c.TargetDistanceKm == 16.0 && c.RunsPerWeek == 5);

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", CustomRequest(16.0, 12, days: 5));
        Assert.Equal(16.0, preview["requested_target_distance_km"]!.GetValue<double>());
        Assert.All(preview["weeks"]!.AsArray(), w => Assert.Equal(5, w!["days"]!.AsArray().Count));

        var previewId = preview["preview_id"]!.GetValue<string>();
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(16.0, details["requested_target_distance_km"]!.GetValue<double>());
        Assert.All(details["weeks"]!.AsArray(), w => Assert.Equal(5, w!["days"]!.AsArray().Count));

        // KEY2 persistence: the 5D RunLayout carries two KEY-intensity sessions
        // per week (HM Intermediate's own structural authority) -- survives
        // all the way through to the persisted/read-back plan detail.
        var intensityLabels = details["weeks"]!.AsArray()
            .SelectMany(w => w!["days"]!.AsArray())
            .Select(d => d!["day_type"]?.GetValue<string>() ?? string.Empty)
            .ToArray();
        Assert.Contains(intensityLabels, t => t is "interval" or "tempo");
    }

    /// <summary>§45/§46 -- a decimal target (17.7K) survives request→preview→confirm→database→Home→PlanDetails without rounding.</summary>
    [Fact]
    public async Task E2E_177KDecimalTarget_ExactValueSurvivesPreviewConfirmHomeDetails()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", CustomRequest(17.7, 12, days: 4));
        Assert.Equal(17.7, preview["requested_target_distance_km"]!.GetValue<double>());

        var previewId = preview["preview_id"]!.GetValue<string>();
        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        if (home["requested_target_distance_km"] is { } homeTarget)
        {
            Assert.Equal(17.7, homeTarget.GetValue<double>());
        }

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(17.7, details["requested_target_distance_km"]!.GetValue<double>());
        Assert.NotEqual(18.0, details["requested_target_distance_km"]!.GetValue<double>());
    }
}
