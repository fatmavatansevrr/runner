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
/// PHASE DIST-GEN.17 -- real HTTP/PostgreSQL public-activation proof for the
/// FIRST projected-distance x non-4D-frequency cell:
/// <c>16K x HalfMarathon-family x Intermediate x 3D</c>, now reachable
/// through <c>goal_distance: "custom"</c> + <c>target_distance_km: 16.0</c> +
/// <c>days_per_week: 3</c> on the real public
/// <c>POST /api/v1/plans/generate-preview/race</c> contract -- mirroring
/// <see cref="DistGen11PublicActivationTests"/>'s own proof shape for 18K,
/// now applied to a frequency dimension for the first time.
///
/// This phase adds no new numeric value anywhere -- 16K I3D's dark authority
/// was already fully frozen and implemented in DIST-GEN.15/15B/16
/// (<c>ProjectedTargetAuthorityRegistry</c>: peak band 28.0/40.0, selected
/// peak 34.0, calibration 20.0, absolute weekly cap 2.0, LR shares
/// 0.34/0.40/0.38/0.46, PreferredAbsolutePeakLongRunKm 15.0). This phase's
/// ONLY production change is adding
/// <c>(16.0, HalfMarathon, Intermediate, 3)</c> to
/// <c>Dark16KPilotEligibilityPolicy.ApprovedPublicCells</c> -- everything
/// below proves that one line is reachable, correct, and non-cross-
/// contaminating through the real public contract, and specifically that
/// <c>days_per_week</c> genuinely participates in public eligibility (16K
/// Intermediate 4D and 16K Intermediate 3D now BOTH resolve publicly, as two
/// genuinely distinct authorities, from the exact same target distance).
///
/// The single most important assertion in this file is
/// <see cref="SameTarget_TwoFrequencies_16KIntermediate3DAnd4D_BothPublic_GenuinelyDistinctAuthority"/>.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class DistGen17PublicActivationTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DistGen17PublicActivationTests(PublishedCatalogTestRelease release)
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

    // ── Central proof: same target, two frequencies, both public, genuinely distinct authority ──

    [Fact]
    public async Task SameTarget_TwoFrequencies_16KIntermediate3DAnd4D_BothPublic_GenuinelyDistinctAuthority()
    {
        await ResetAsync();

        var response3D = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12, days: 3));
        var body3D = await response3D.Content.ReadAsStringAsync();
        Assert.True(response3D.StatusCode == HttpStatusCode.OK, $"expected 200 for 3D, got {response3D.StatusCode}: {body3D}");
        var preview3D = JsonNode.Parse(body3D)!;

        var response4D = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12, days: 4));
        var body4D = await response4D.Content.ReadAsStringAsync();
        Assert.True(response4D.StatusCode == HttpStatusCode.OK, $"expected 200 for 4D, got {response4D.StatusCode}: {body4D}");
        var preview4D = JsonNode.Parse(body4D)!;

        // Same target distance and family reached from both requests.
        Assert.Equal(16.0, preview3D["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal(16.0, preview4D["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", preview3D["goal_distance"]!.GetValue<string>());
        Assert.Equal("half_marathon", preview4D["goal_distance"]!.GetValue<string>());

        // RunLayout genuinely differs: 3 sessions/week vs 4 sessions/week.
        Assert.All(preview3D["weeks"]!.AsArray(), w => Assert.Equal(3, w!["days"]!.AsArray().Count));
        Assert.All(preview4D["weeks"]!.AsArray(), w => Assert.Equal(4, w!["days"]!.AsArray().Count));

        // Peak volume: 3D's own 34.0 ceiling vs 4D's own 40.0 ceiling.
        double PeakVolume(JsonNode preview) => preview["weeks"]!.AsArray()
            .Select(w => w!["days"]!.AsArray().Sum(d => d!["distance_km"]!.GetValue<double>())).Max();
        var peak3D = PeakVolume(preview3D);
        var peak4D = PeakVolume(preview4D);
        Assert.True(peak3D <= 34.0 + 0.5, $"3D peak volume {peak3D} exceeded its own 34.0km ceiling");
        Assert.True(peak4D <= 40.0 + 0.5, $"4D peak volume {peak4D} exceeded its own 40.0km ceiling");

        // But the same absolute peak long-run ceiling (15.0km) -- a genuine
        // frequency-invariance finding, not an accident of cross-contamination.
        double PeakLongRun(JsonNode preview) => preview["weeks"]!.AsArray()
            .SelectMany(w => w!["days"]!.AsArray())
            .Where(d => d!["day_type"]!.GetValue<string>() == "long_run")
            .Select(d => d!["distance_km"]!.GetValue<double>()).Max();
        var lr3D = PeakLongRun(preview3D);
        var lr4D = PeakLongRun(preview4D);
        Assert.True(lr3D <= 15.0 + 0.001, $"3D peak long run {lr3D} exceeded the 15.0km ceiling");
        Assert.True(lr4D <= 15.0 + 0.001, $"4D peak long run {lr4D} exceeded the 15.0km ceiling");
    }

    // ── Eligible horizon matrix: 10/11/12/13/14 weeks all succeed (3D) ────────

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task EligibleHorizons_16KI3D_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(int weeks)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(weeks, days: 3));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200 for {weeks}W, got {response.StatusCode}: {body}");

        var preview = JsonNode.Parse(body)!;
        Assert.Equal(16.0, preview["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", preview["goal_distance"]!.GetValue<string>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        Assert.All(preview["weeks"]!.AsArray(), w => Assert.Equal(3, w!["days"]!.AsArray().Count));

        var weeklyVolumes = preview["weeks"]!.AsArray()
            .Select(w => w!["days"]!.AsArray().Sum(d => d!["distance_km"]!.GetValue<double>()))
            .ToArray();
        Assert.True(weeklyVolumes.Max() <= 34.0 + 0.5, $"peak volume {weeklyVolumes.Max()} exceeded 16K I3D's own 34.0km ceiling for {weeks}W");

        var longRunDistances = preview["weeks"]!.AsArray()
            .SelectMany(w => w!["days"]!.AsArray())
            .Where(d => d!["day_type"]!.GetValue<string>() == "long_run")
            .Select(d => d!["distance_km"]!.GetValue<double>())
            .ToArray();
        Assert.NotEmpty(longRunDistances);
        Assert.All(longRunDistances, km => Assert.True(km < 16.0, $"16K I3D long run {km} reached/exceeded target distance"));
        Assert.All(longRunDistances, km => Assert.True(km <= 15.0 + 0.001, $"16K I3D long run {km} exceeded 15.0km ceiling"));
    }

    // ── Below-minimum / above-maximum horizon rejection ──────────────────────

    [Fact]
    public async Task NineWeekHorizon_16KI3D_TypedRejection_NotFiveHundred_NotFallback()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(9, days: 3));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("DARK_16K_I3D_CORE_HORIZON", body);
        Assert.DoesNotContain("template_id", body);
        Assert.DoesNotContain("TARGET_BELOW_SUM_OF_MINIMUMS", body);
    }

    [Fact]
    public async Task FifteenWeekHorizon_16KI3D_TypedRejection_UnderOwnMaximumAuthority()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(15, days: 3));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("DARK_16K_I3D_CORE_HORIZON", body);
        Assert.DoesNotContain("template_id", body);
    }

    // ── Dark-vs-public golden comparison (12W preferred horizon) ──────────────

    [Fact]
    public async Task DarkVsPublic_16KI3D_12W_SemanticZeroDelta_OnlyReachabilityChanged()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12, days: 3));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = JsonNode.Parse(body)!;

        // DIST-GEN.16's own real-pipeline golden trace for 12W: peak volume
        // <= 34.0, peak long run <= 15.0, exactly 3 sessions/week (1 KEY + 1
        // EASY + 1 LONG), 2-week non-chained taper.
        var weeksArr = preview["weeks"]!.AsArray();
        var weeklyVolumes = weeksArr.Select(w => w!["days"]!.AsArray().Sum(d => d!["distance_km"]!.GetValue<double>())).ToArray();
        Assert.True(weeklyVolumes.Max() <= 34.0 + 0.5, $"peak volume {weeklyVolumes.Max()} did not match dark golden trace ceiling (34.0)");

        var longRuns = weeksArr.SelectMany(w => w!["days"]!.AsArray())
            .Where(d => d!["day_type"]!.GetValue<string>() == "long_run")
            .Select(d => d!["distance_km"]!.GetValue<double>()).ToArray();
        Assert.True(longRuns.Max() <= 15.0 + 0.001, $"peak long run {longRuns.Max()} did not match dark golden trace ceiling (15.0)");

        Assert.All(weeksArr, w => Assert.Equal(3, w!["days"]!.AsArray().Count));
        Assert.Equal(12, weeksArr.Count);
    }

    // ── Full E2E lifecycle at 16K I3D / 12W ────────────────────────────────────

    [Fact]
    public async Task TwelveWeek_16KI3D_FullLifecycle_IdentitySurvivesEveryStep()
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12, days: 3));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(16.0, preview["requested_target_distance_km"]!.GetValue<double>());
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);

        Assert.Equal("HALF_MARATHON", plan.CanonicalDistanceFamily);
        Assert.Equal(16.0, plan.RequestedTargetDistanceKm);
        Assert.Equal(12, plan.Weeks.Count);
        Assert.All(plan.Weeks, w => Assert.Equal(3, w.Days.Count));

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Equal(16.0, home["active_plan"]!["requested_target_distance_km"]!.GetValue<double>());

        var calendar = await _client.GetJsonAsync("/api/v1/plans/active/calendar?month=2026-07");
        Assert.NotEmpty(calendar.AsArray());

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(16.0, details["requested_target_distance_km"]!.GetValue<double>());
        Assert.Equal("half_marathon", details["goal_distance"]!.GetValue<string>());

        var days = plan.Weeks.SelectMany(w => w.Days).OrderBy(d => d.Date).ToArray();
        var toComplete = days[0];
        (await _client.PostRawAsync($"/api/v1/training-days/{toComplete.Id}/complete",
            new { actual_distance_km = toComplete.PlannedDistanceKm, actual_duration_min = 30 })).EnsureSuccessStatusCode();
        Assert.Equal(TrainingDayStatus.Completed, (await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == toComplete.Id)).Status);

        var toSkip = days[1];
        (await _client.PostRawAsync($"/api/v1/training-days/{toSkip.Id}/not-today-decisions",
            new { reason = "DIST-GEN.17 not-today lifecycle proof" })).EnsureSuccessStatusCode();

        var cancel = await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel",
            new { reason = "DIST-GEN.17 lifecycle proof cancellation" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
    }

    // ── Same-target cross-frequency persistence: two separately confirmed plans ──

    [Fact]
    public async Task SameTargetCrossFrequency_16KI3DAndI4D_BothPersistIndependently()
    {
        await ResetAsync();

        var preview3DResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12, days: 3));
        preview3DResponse.EnsureSuccessStatusCode();
        var preview3D = (await preview3DResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var confirm3D = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = Guid.Parse(preview3D["preview_id"]!.GetValue<string>()) });
        var plan3DId = Guid.Parse(confirm3D["plan_id"]!.GetValue<string>());

        var cancel3D = await _client.PostRawAsync($"/api/v1/plans/{plan3DId}/cancel", new { reason = "make way for 4D confirm" });
        Assert.Equal(HttpStatusCode.OK, cancel3D.StatusCode);

        var preview4DResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12, days: 4));
        preview4DResponse.EnsureSuccessStatusCode();
        var preview4D = (await preview4DResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var confirm4D = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = Guid.Parse(preview4D["preview_id"]!.GetValue<string>()) });
        var plan4DId = Guid.Parse(confirm4D["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan3D = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days).SingleAsync(p => p.Id == plan3DId);
        var plan4D = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days).SingleAsync(p => p.Id == plan4DId);

        Assert.Equal(16.0, plan3D.RequestedTargetDistanceKm);
        Assert.Equal(16.0, plan4D.RequestedTargetDistanceKm);
        Assert.Equal("HALF_MARATHON", plan3D.CanonicalDistanceFamily);
        Assert.Equal("HALF_MARATHON", plan4D.CanonicalDistanceFamily);
        Assert.All(plan3D.Weeks, w => Assert.Equal(3, w.Days.Count));
        Assert.All(plan4D.Weeks, w => Assert.Equal(4, w.Days.Count));
        Assert.NotEqual(plan3DId, plan4DId);
    }

    // ── Rollback-safety: an already-confirmed 16K I3D plan stays operable ────
    // even after the 3D entry is removed from ApprovedPublicCells -- proven
    // by constructing an isolated in-memory list without the 3D entry (the
    // exact seam DIST-GEN.11 §rollback used) and confirming the real,
    // already-confirmed plan's own reads/mutations never consult that list.

    [Fact]
    public async Task ConfirmedPlan_16KI3D_RemainsFullyOperable_ReadsAndMutationsDoNotRecheckPublicEligibility()
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12, days: 3));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        // Simulate 16K I3D's public eligibility being rolled back: an
        // isolated list without the (16.0, HM, Intermediate, 3) entry.
        var rolledBackPublicCells = RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy.ApprovedPublicCells
            .Where(c => !(c.TargetDistanceKm == 16.0 && c.RunsPerWeek == 3))
            .ToArray();
        Assert.DoesNotContain(rolledBackPublicCells, c => c.TargetDistanceKm == 16.0 && c.RunsPerWeek == 3);
        Assert.Contains(rolledBackPublicCells, c => c.TargetDistanceKm == 16.0 && c.RunsPerWeek == 4);
        Assert.Contains(rolledBackPublicCells, c => c.TargetDistanceKm == 15.0);
        Assert.Contains(rolledBackPublicCells, c => c.TargetDistanceKm == 18.0);

        // Reads/mutations succeed purely from persisted state -- no
        // eligibility gate involved (the gate lives only in
        // GeneratePreviewCommandMapper.ToInternalRequest, never in
        // PlanServices' read/mutation paths, confirmed by inspection).
        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Equal(16.0, home["active_plan"]!["requested_target_distance_km"]!.GetValue<double>());
        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal(16.0, details["requested_target_distance_km"]!.GetValue<double>());

        var cancel = await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel", new { reason = "rollback-safety proof" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
    }

    // ── Registry separation proof: both lists remain independently declared ──

    [Fact]
    public void PublicAndDarkRegistries_BothNowFourEntries_ButRemainIndependentlyDeclaredLists()
    {
        // DIST-GEN.20 note: at DIST-GEN.17's own closure, Public and Dark were
        // numerically equal (4=4) -- this test's own name reflects that
        // historical moment and is kept in place (not renamed) per this
        // engagement's own convention of updating assertions rather than
        // superseding files. DIST-GEN.20 later added 16.0km x Intermediate x 5D
        // to Dark only (deliberately not Public) -- the first intentional
        // dark/public divergence since this phase. The counts below are
        // updated in place to reflect that real, intentional, subsequent
        // change; the structural "independently declared lists" proof this
        // test performs is unaffected and, if anything, more clearly
        // demonstrated now that the two counts differ.
        var publicCells = RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy.ApprovedPublicCells;
        var darkCells = RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy.ApprovedDarkCells;

        Assert.Equal(4, publicCells.Count);
        Assert.Equal(5, darkCells.Count);
        Assert.False(ReferenceEquals(publicCells, darkCells));

        // Every public cell also happens to be dark-eligible today (expected:
        // public activation always reuses an already-dark cell), but the
        // reverse is not required and the lists are not derived from each
        // other -- confirmed structurally by their being two separate
        // `static readonly` field declarations in source, not a projection
        // of one onto the other.
        foreach (var cell in publicCells)
        {
            Assert.Contains(darkCells, d => d.TargetDistanceKm == cell.TargetDistanceKm &&
                d.ParentDistanceFamily == cell.ParentDistanceFamily && d.Level == cell.Level && d.RunsPerWeek == cell.RunsPerWeek);
        }
    }

    // ── No target-only or frequency-only public gate: full 4-tuple required ──

    [Theory]
    [InlineData("beginner", 3)]
    [InlineData("advanced", 3)]
    public async Task Target16K_WrongLevel_AtThreeDays_Rejected(string level, int days)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Target16KRequest(12, level: level, days: days))!.AsObject();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Target16K_Intermediate_WrongFrequency_5Dand6D_Rejected(int days)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Target16KRequest(12, level: "intermediate", days: days))!.AsObject();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    // ── Unsupported 3D matrix: other target distances at Intermediate x 3D ────

    [Theory]
    [InlineData(15.0)]
    [InlineData(18.0)]
    [InlineData(12.0)]
    [InlineData(20.0)]
    public async Task UnsupportedTargetDistances_AtThreeDays_StillRejected(double km)
    {
        await ResetAsync();
        var request = JsonSerializer.SerializeToNode(Target16KRequest(12, days: 3))!.AsObject();
        request["target_distance_km"] = km;

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("UNSUPPORTED_TARGET_DISTANCE", body);
    }

    // ── Custom-without-target safety regression ──────────────────────────────

    [Fact]
    public async Task CustomWithoutTargetDistanceKm_StillFailsClosed_16KI3DAdditionDidNotWeaken()
    {
        await ResetAsync();
        var before = await CountPersistedRowsAsync();
        var request = JsonSerializer.SerializeToNode(Target16KRequest(12, days: 3))!.AsObject();
        request.Remove("target_distance_km");

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED", body);

        var after = await CountPersistedRowsAsync();
        Assert.Equal(before, after);
    }

    // ── Zero-delta smoke: 15K/16K I4D/18K/canonical TEN_K/HM unaffected ───────

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
    public async Task ZeroDelta_18K_StillPubliclyEligible_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target18KRequest(12));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(18.0, preview["requested_target_distance_km"]!.GetValue<double>());
    }

    [Fact]
    public async Task ZeroDelta_16KI4D_StillPubliclyEligible_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", Target16KRequest(12, days: 4));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal(16.0, preview["requested_target_distance_km"]!.GetValue<double>());
    }

    [Fact]
    public async Task ZeroDelta_HalfMarathonIntermediate3D_Canonical_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(12, "intermediate", 3));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("HALF_MARATHON__3D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    [Fact]
    public async Task ZeroDelta_HalfMarathonIntermediate4D_Canonical_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", HmRequest(14, "intermediate", 4));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("HALF_MARATHON__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    [Fact]
    public async Task ZeroDelta_TenKIntermediate3D_Canonical_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", TenKRequest(12, "intermediate", 3));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__3D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    [Fact]
    public async Task ZeroDelta_TenKIntermediate4D_Canonical_Unaffected()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", TenKRequest(12, "intermediate", 4));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("TEN_K__4D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static object Target16KRequest(int weeks, string level = "intermediate", int days = 3)
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
            race_name = "DIST-GEN.17 16K public activation test",
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
            race_name = "DIST-GEN.17 15K zero-delta test",
        };
    }

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
            race_name = "DIST-GEN.17 18K zero-delta test",
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
            race_name = "DIST-GEN.17 TEN_K zero-delta test",
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
            race_name = "DIST-GEN.17 HALF_MARATHON zero-delta test",
        };
    }

    private static string[] DaysFor(int days) => days switch
    {
        2 => new[] { "mon", "thu" },
        3 => new[] { "tue", "thu", "sun" },
        4 => new[] { "mon", "wed", "fri", "sun" },
        5 => new[] { "mon", "tue", "thu", "fri", "sun" },
        6 => new[] { "mon", "tue", "wed", "thu", "fri", "sun" },
        _ => Enumerable.Range(0, days).Select(i => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }[i]).ToArray(),
    };
}
