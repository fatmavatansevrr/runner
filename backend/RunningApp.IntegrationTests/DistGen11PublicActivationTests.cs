using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// PHASE DIST-GEN.11 -- real HTTP/PostgreSQL public-activation proof for the
/// third approved 18K/HalfMarathon-family/Intermediate/4D target-distance
/// projection, now reachable through <c>goal_distance: "custom"</c> +
/// <c>target_distance_km: 18.0</c> on the real public
/// <c>POST /api/v1/plans/generate-preview/race</c> contract -- exactly
/// mirroring <see cref="DistGen7PublicActivationTests"/>'s own proof shape
/// for 15K (which itself mirrored <c>DistGen3PublicActivationTests</c>'s
/// proof shape for 16K).
///
/// This phase adds no new numeric value anywhere -- 18K's dark authority was
/// already fully frozen and implemented in DIST-GEN.9/10
/// (<c>TargetDistance18KHorizonPolicy</c>: 10/12/14 core weeks;
/// <c>VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K</c>:
/// PreferredAbsolutePeakLongRunKm=16.0, peak volume band 34.0/46.0/40.0). This
/// phase's ONLY production change is adding
/// <c>(18.0, HalfMarathon, Intermediate, 4)</c> to
/// <c>Dark16KPilotEligibilityPolicy.ApprovedPublicCells</c> -- everything
/// below proves that one line is reachable, correct, and non-cross-
/// contaminating through the real public contract.
///
/// The single most important assertion in this file is
/// <see cref="ThreeWayIsolation_15K16K18K_PublicPeakLongRun_NoCrossContamination"/>
/// -- proving all three deliberately-different numeric ceilings (14.5/15.0/16.0)
/// through the real public HTTP path, not just the internal policy-isolation
/// harness DIST-GEN.10 already proved.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class DistGen11PublicActivationTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DistGen11PublicActivationTests(PublishedCatalogTestRelease release)
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

    public void Dispose() { _client.Dispose(); _factory.Dispose(); }

    private static async Task ResetAsync()
    {
        using var resetFactory = new CustomWebApplicationFactory();
        using var resetClient = resetFactory.CreateClient();
        (await resetClient.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
    }

    private async Task<(int PreviewCount, int PlanCount)> CountPersistedRowsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.PlanPreviews.CountAsync(), await db.TrainingPlans.CountAsync());
    }

    // ── Eligible horizon matrix: 10/11/12/13/14 weeks all succeed ────────────

    // SUPERSEDED BY PHASE DERIVED-DIST.2 (§13/§17/§28/§67 EXPECTED_MODEL_CHANGE):
    // new public 18K I4D creation now routes through the generic derived
    // engine (HM-sourced trajectory, HM's own ~19.0km parent long-run
    // authority), not this exact cell's own narrower 16.0km ceiling / 10-14-week
    // DARK_18K_CORE_HORIZON authority / DIST-GEN.10 golden peak-volume trace.
    // The 18K exact-cell registry and its own horizon/volume/peak-LR authority
    // remain fully compiled, tested and reachable for legacy/rollback use (see
    // Dark18KTargetDistanceProjectionTests, unmodified and still green) -- only
    // NEW public creation no longer resolves to it by default. See
    // DerivedDist2PublicActivationTests for the current, correct public
    // 15K/16K/18K-via-derived provenance proofs and realized peak-LR capture.
    [Theory(Skip = "Superseded by DERIVED-DIST.2: 18K I4D new public creation now routes via generic derived, not this exact cell's own authority. See DerivedDist2PublicActivationTests.")]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task EligibleHorizons_18K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target18KRequest(weeks));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200 for {weeks}W, got {response.StatusCode}: {body}");

        var preview = JsonNode.Parse(body)!;
        Assert.Equal(18.0, preview["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", preview["goal_distance"]!.GetValue<string>());
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);

        // Peak-volume policy proof: DIST-GEN.10's own golden trace predicts
        // 31.0/32.5/34.0/34.0/34.0 for 10-14W -- never 16K's/15K's own numbers
        // (identical by shared/converged authority, not coincidence this
        // phase introduces), never HM's canonical 43.0, never TEN_K's 38.0.
        var weeklyVolumes = preview["weeks"]!.AsArray()
            .Select(w => w!["days"]!.AsArray().Sum(d => d!["distance_km"]!.GetValue<double>()))
            .ToArray();
        var peakVolume = weeklyVolumes.Max();
        var expectedPeak = weeks switch
        {
            10 => 31.0,
            11 => 32.5,
            12 => 34.0,
            13 => 34.0,
            14 => 34.0,
            _ => throw new ArgumentOutOfRangeException(nameof(weeks)),
        };
        Assert.True(Math.Abs(peakVolume - expectedPeak) <= 0.5, $"peak volume {peakVolume} did not match DIST-GEN.10's own golden trace ({expectedPeak}) for {weeks}W");

        // 18K's own peak-LR ceiling authority is 16.0 -- long runs must never
        // reach or exceed 18.0 (the target distance itself), and never exceed 16.0.
        var longRunDistances = preview["weeks"]!.AsArray()
            .SelectMany(w => w!["days"]!.AsArray())
            .Where(d => d!["day_type"]!.GetValue<string>() == "long_run")
            .Select(d => d!["distance_km"]!.GetValue<double>())
            .ToArray();
        Assert.NotEmpty(longRunDistances);
        Assert.All(longRunDistances, km => Assert.True(km < 18.0, $"18K long run {km} reached/exceeded target distance"));
        Assert.All(longRunDistances, km => Assert.True(km <= 16.0 + 0.001, $"18K long run {km} exceeded 16.0km ceiling"));
    }

    // ── Below-minimum / above-maximum horizon rejection ──────────────────────

    // SUPERSEDED BY PHASE DERIVED-DIST.2: 9W now rejects via the generic
    // derived horizon authority's own reason code (DERIVED_DISTANCE_CORE_HORIZON_*),
    // not DARK_18K_CORE_HORIZON -- same 10/12/14-week bounds, different (shared,
    // non-per-distance) authority, per DERIVED-DIST.1's frozen horizon decision.
    [Fact(Skip = "Superseded by DERIVED-DIST.2: new public 18K I4D 9W rejection now carries the generic DERIVED_DISTANCE_CORE_HORIZON_* reason code, not DARK_18K_CORE_HORIZON. See DerivedDist2PublicActivationTests.")]
    public async Task NineWeekHorizon_18K_TypedRejection_NotFiveHundred_NotSilentSubstitution()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target18KRequest(9));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("TARGET_BELOW_SUM_OF_MINIMUMS", body);
        Assert.DoesNotContain("template_id", body);
        Assert.Contains("DARK_18K_CORE_HORIZON", body);
        Assert.DoesNotContain("DARK_15K", body);
        Assert.DoesNotContain("DARK_16K", body);
    }

    // SUPERSEDED BY PHASE DERIVED-DIST.2: same rationale as the 9W test above.
    [Fact(Skip = "Superseded by DERIVED-DIST.2: new public 18K I4D 15W rejection now carries the generic DERIVED_DISTANCE_CORE_HORIZON_* reason code, not DARK_18K_CORE_HORIZON. See DerivedDist2PublicActivationTests.")]
    public async Task FifteenWeekHorizon_18K_TypedRejection_UnderOwnMaximumAuthority()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target18KRequest(15));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("DARK_18K_CORE_HORIZON", body);
        Assert.DoesNotContain("template_id", body);
    }

    // ── Unsupported 18K-cell matrix: wrong level / wrong frequency ───────────

    [Theory]
    [InlineData("beginner", 4)]
    [InlineData("advanced", 4)]
    public async Task Target18K_WrongLevel_Rejected(string level, int days)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Target18KRequest(12, level: level, days: days))!.AsObject();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    // SUPERSEDED (partially) BY PHASE DERIVED-DIST.2 §9/§13: 18K at Intermediate
    // 3D/5D is now ACCEPTED (derived V1 product subset admits Intermediate
    // 3D/4D/5D for ANY in-range target, not merely the historically-registered
    // 4D exact cell) -- removed from this rejection matrix; see
    // DerivedDist2PublicActivationTests for the current positive proof.
    // Intermediate 6D remains correctly rejected (out of V1 derived scope,
    // §9/§48) -- kept here unchanged.
    [Theory]
    [InlineData(6)]
    public async Task Target18K_WrongFrequency_Rejected(int days)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Target18KRequest(12, level: "intermediate", days: days))!.AsObject();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    // ── Unsupported-target matrix (nearby, non-approved values) ──────────────

    // SUPERSEDED BY PHASE DERIVED-DIST.2 §8/§14: every one of these values is
    // strictly inside the generic derived interval (10K, HALF_MARATHON) at
    // Intermediate 4D, so all are now DELIBERATELY ACCEPTED (arbitrary decimal
    // target support, the whole point of §8 -- "no per-distance registration
    // required"). This is the intended removal of the old "only an exact
    // registered cell is accepted" model, not a regression. See
    // DerivedDist2PublicActivationTests for the current decimal-acceptance and
    // persistence proofs (17.7K in particular).
    [Theory(Skip = "Superseded by DERIVED-DIST.2: every value here is now a deliberately-accepted arbitrary decimal derived target (§8/§14), not an unsupported one. See DerivedDist2PublicActivationTests.")]
    [InlineData(12.0)]
    [InlineData(14.0)]
    [InlineData(17.0)]
    [InlineData(19.0)]
    [InlineData(20.0)]
    [InlineData(16.09)]
    [InlineData(17.9)]
    [InlineData(18.1)]
    [InlineData(18.01)]
    [InlineData(18.09)]
    public async Task UnsupportedTargetDistances_StillRejected_NeverSubstituted_NeverSnapped(double km)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Target18KRequest(12))!.AsObject();
        request["target_distance_km"] = km;

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
        Assert.DoesNotContain("HALF_MARATHON__4D__INTERMEDIATE", body);
        Assert.DoesNotContain("TEN_K__4D__INTERMEDIATE", body);
    }

    // ── The single most important proof: 14.5 vs 15.0 vs 16.0 isolation ─────

    [Fact]
    public async Task ThreeWayIsolation_15K16K18K_PublicPeakLongRun_NoCrossContamination()
    {
        await ResetAsync();

        var response15 = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target15KRequest(12));
        var body15 = await response15.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response15.StatusCode);
        var preview15 = JsonNode.Parse(body15)!;

        var response16 = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12));
        var body16 = await response16.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response16.StatusCode);
        var preview16 = JsonNode.Parse(body16)!;

        var response18 = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target18KRequest(12));
        var body18 = await response18.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response18.StatusCode);
        var preview18 = JsonNode.Parse(body18)!;

        var dist15 = preview15["requested_target_distance_km"]!.GetValue<double>();
        var dist16 = preview16["requested_target_distance_km"]!.GetValue<double>();
        var dist18 = preview18["requested_target_distance_km"]!.GetValue<double>();
        Assert.Equal(15.0, dist15);
        Assert.Equal(16.0, dist16);
        Assert.Equal(18.0, dist18);
        Assert.NotEqual(dist15, dist16);
        Assert.NotEqual(dist16, dist18);
        Assert.NotEqual(dist15, dist18);

        double PeakLongRun(JsonNode preview) => preview["weeks"]!.AsArray()
            .SelectMany(w => w!["days"]!.AsArray())
            .Where(d => d!["day_type"]!.GetValue<string>() == "long_run")
            .Select(d => d!["distance_km"]!.GetValue<double>())
            .Max();

        var peak15 = PeakLongRun(preview15);
        var peak16 = PeakLongRun(preview16);
        var peak18 = PeakLongRun(preview18);

        // Ceiling authority: 14.5 / 15.0 / 16.0 respectively -- none may leak
        // into another target's ceiling, even though the OBSERVED peak (13.5)
        // coincides across all three at this readiness level (DIST-GEN.10 §40's
        // own predicted finding: authority differs even when observed output
        // does not, since the ceiling never binds at this readiness).
        Assert.True(peak15 <= 14.5 + 0.001, $"15K peak long run {peak15} exceeded its own 14.5km ceiling");
        Assert.True(peak16 <= 15.0 + 0.001, $"16K peak long run {peak16} exceeded its own 15.0km ceiling");
        Assert.True(peak18 <= 16.0 + 0.001, $"18K peak long run {peak18} exceeded its own 16.0km ceiling");
        Assert.True(peak15 < 15.0, "15K peak long run must never reach 15.0");
        Assert.True(peak16 < 16.0, "16K peak long run must never reach 16.0");
        Assert.True(peak18 < 18.0, "18K peak long run must never reach 18.0");
    }

    // ── Dark-vs-public golden comparison (12W preferred horizon) ─────────────

    // SUPERSEDED BY PHASE DERIVED-DIST.2: this test's whole premise ("public
    // reachability is the ONLY delta vs. the dark exact-cell golden trace") no
    // longer holds -- new public 18K I4D creation now realizes the generic
    // derived/HM-sourced trajectory's own numbers (peak volume 32.0, not the
    // exact cell's own 34.0), a genuinely different, intentionally-adopted
    // model (DERIVED-DIST.1's frozen long-run/volume decision), not merely a
    // reachability change. The dark exact-cell golden trace itself is
    // unaffected and still independently provable via Dark18KTargetDistanceProjectionTests.
    [Fact(Skip = "Superseded by DERIVED-DIST.2: new public 18K I4D now realizes the generic derived model's own numbers, not the exact cell's golden trace. Dark18KTargetDistanceProjectionTests still proves the legacy trace unchanged.")]
    public async Task DarkVsPublic_18K_12W_SemanticZeroDelta_OnlyReachabilityChanged()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target18KRequest(12));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = JsonNode.Parse(body)!;

        // DIST-GEN.10's own real-pipeline golden trace for 12W (§39 of that
        // report): peak volume 34.0, peak long run 13.5, phase split
        // 2/3/5/2 (FOUNDATION/BUILD/RACE_SPECIFIC/TAPER).
        var weeksArr = preview["weeks"]!.AsArray();
        var weeklyVolumes = weeksArr.Select(w => w!["days"]!.AsArray().Sum(d => d!["distance_km"]!.GetValue<double>())).ToArray();
        Assert.True(Math.Abs(weeklyVolumes.Max() - 34.0) <= 0.5, $"peak volume {weeklyVolumes.Max()} did not match dark golden trace (34.0)");

        var longRuns = weeksArr.SelectMany(w => w!["days"]!.AsArray())
            .Where(d => d!["day_type"]!.GetValue<string>() == "long_run")
            .Select(d => d!["distance_km"]!.GetValue<double>()).ToArray();
        Assert.True(Math.Abs(longRuns.Max() - 13.5) <= 0.5, $"peak long run {longRuns.Max()} did not match dark golden trace (13.5)");

        // 4 sessions/week at every horizon -- generic allocator, unaffected by
        // target distance (structure zero-delta from the dark implementation).
        Assert.All(weeksArr, w => Assert.Equal(4, w!["days"]!.AsArray().Count));
        Assert.Equal(12, weeksArr.Count);
    }

    // ── Full E2E lifecycle at 18K/12W ─────────────────────────────────────────

    [Fact]
    public async Task TwelveWeek_18K_FullLifecycle_IdentitySurvivesEveryStep()
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target18KRequest(12));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(18.0, preview["requested_target_distance_km"]!.GetValue<double>());
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);

        Assert.Equal("HALF_MARATHON", plan.CanonicalDistanceFamily);
        Assert.Equal(18.0, plan.RequestedTargetDistanceKm);
        Assert.NotEqual(21.0975, plan.RequestedTargetDistanceKm);
        Assert.NotEqual(16.0, plan.RequestedTargetDistanceKm);
        Assert.NotEqual(15.0, plan.RequestedTargetDistanceKm);
        Assert.NotEqual(5.0, plan.RequestedTargetDistanceKm);
        Assert.Equal(12, plan.Weeks.Count);

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Equal(18.0, home["active_plan"]!["requested_target_distance_km"]!.GetValue<double>());

        var calendar = await _client.GetJsonAsync("/api/v1/plans/active/calendar?month=2026-07");
        Assert.NotEmpty(calendar.AsArray());

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(18.0, details["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", details["goal_distance"]!.GetValue<string>());

        var days = plan.Weeks.SelectMany(w => w.Days).OrderBy(d => d.Date).ToArray();
        var toComplete = days[0];
        (await _client.PostRawAsync($"/api/v1/training-days/{toComplete.Id}/complete",
            new { actual_distance_km = toComplete.PlannedDistanceKm, actual_duration_min = 30 })).EnsureSuccessStatusCode();
        Assert.Equal(TrainingDayStatus.Completed, (await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == toComplete.Id)).Status);

        var toSkip = days[1];
        (await _client.PostRawAsync($"/api/v1/training-days/{toSkip.Id}/not-today-decisions",
            new { reason = "DIST-GEN.11 not-today lifecycle proof" })).EnsureSuccessStatusCode();

        var cancel = await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel",
            new { reason = "DIST-GEN.11 lifecycle proof cancellation" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
    }

    // ── Rollback-safety: an already-confirmed 18K plan stays operable ────────
    // even after 18.0 is removed from ApprovedPublicCells -- proven here by
    // constructing an isolated in-memory list without the 18.0 entry (the
    // exact mechanism the governing prompt names as an acceptable seam) and
    // confirming the real, already-confirmed plan's own reads/mutations never
    // consult that list at all (mirrors DIST-GEN.7 §55's identical finding).

    [Fact]
    public async Task ConfirmedPlan_18K_RemainsFullyOperable_ReadsAndMutationsDoNotRecheckPublicEligibility()
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target18KRequest(12));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        // Simulate 18.0's public eligibility being rolled back: an isolated
        // list without the 18.0 entry, exactly as the real rollback procedure
        // (removing the one ApprovedPublicCells line) would leave behind.
        var rolledBackPublicCells = RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy.ApprovedPublicCells
            .Where(c => c.TargetDistanceKm != 18.0)
            .ToArray();
        Assert.DoesNotContain(rolledBackPublicCells, c => c.TargetDistanceKm == 18.0);
        Assert.Contains(rolledBackPublicCells, c => c.TargetDistanceKm == 15.0);
        Assert.Contains(rolledBackPublicCells, c => c.TargetDistanceKm == 16.0);

        // Reads/mutations succeed purely from persisted state -- no
        // eligibility gate involved (the gate lives only in
        // GeneratePreviewCommandMapper.ToInternalRequest, never in
        // PlanServices' read/mutation paths, confirmed by inspection).
        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Equal(18.0, home["active_plan"]!["requested_target_distance_km"]!.GetValue<double>());
        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(18.0, details["requested_target_distance_km"]!.GetValue<double>());

        var cancel = await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel", new { reason = "rollback-safety proof" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
    }

    // ── Custom-without-target safety regression ──────────────────────────────

    [Fact]
    public async Task CustomWithoutTargetDistanceKm_StillFailsClosed_18KAdditionDidNotWeaken()
    {
        await ResetAsync();
        var before = await CountPersistedRowsAsync();
        var request = JsonSerializer.SerializeToNode(Target18KRequest(12))!.AsObject();
        request.Remove("target_distance_km");

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED", body);

        var after = await CountPersistedRowsAsync();
        Assert.Equal(before, after);
    }

    // ── Zero-delta smoke: 15K/16K/canonical TEN_K/HM unaffected ──────────────

    [Fact]
    public async Task ZeroDelta_15K_StillPubliclyEligible_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target15KRequest(12));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(15.0, preview["requested_target_distance_km"]!.GetValue<double>());
    }

    [Fact]
    public async Task ZeroDelta_16K_StillPubliclyEligible_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(16.0, preview["requested_target_distance_km"]!.GetValue<double>());
    }

    [Fact]
    public async Task ZeroDelta_TenKIntermediate4D_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", TenKRequest(12, "intermediate", 4));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    [Fact]
    public async Task ZeroDelta_HalfMarathonIntermediate4D_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(14, "intermediate", 4));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static object Target18KRequest(int weeks, string level = "intermediate", int days = 4)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "custom",
            target_distance_km = 18.0,
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 6300,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = (double?)20.0,
            recent_longest_run_km = 10,
            recent_runs_per_week = days,
            recent_race = new { distance = "ten_k", finish_time_seconds = 2700, race_date = start.AddDays(-21).ToString("yyyy-MM-dd") },
            race_name = "DIST-GEN.11 18K public activation test",
        };
    }

    private static object Target15KRequest(int weeks, string level = "intermediate", int days = 4)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "custom",
            target_distance_km = 15.0,
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 5100,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = (double?)20.0,
            recent_longest_run_km = 10,
            recent_runs_per_week = days,
            recent_race = new { distance = "ten_k", finish_time_seconds = 2700, race_date = start.AddDays(-21).ToString("yyyy-MM-dd") },
            race_name = "DIST-GEN.11 15K side-by-side isolation test",
        };
    }

    private static object Target16KRequest(int weeks, string level = "intermediate", int days = 4)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "custom",
            target_distance_km = 16.0,
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 5400,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = (double?)20.0,
            recent_longest_run_km = 10,
            recent_runs_per_week = days,
            recent_race = new { distance = "ten_k", finish_time_seconds = 2700, race_date = start.AddDays(-21).ToString("yyyy-MM-dd") },
            race_name = "DIST-GEN.11 16K side-by-side isolation test",
        };
    }

    private static object TenKRequest(int weeks, string level, int days)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "ten_k",
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average",
            recent_weekly_volume_km = (double?)25.0,
            recent_longest_run_km = 10,
            recent_runs_per_week = days,
            race_name = "DIST-GEN.11 TEN_K zero-delta test",
        };
    }

    private static object HmRequest(int weeks, string level, int days)
    {
        var start = new DateOnly(2026, 7, 20);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "half_marathon",
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = (2 * 3600) + (5 * 60),
            target_finish_time_source = "product_average",
            recent_weekly_volume_km = (double?)32,
            recent_longest_run_km = 14,
            recent_runs_per_week = days,
            race_name = "DIST-GEN.11 HALF_MARATHON zero-delta test",
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
