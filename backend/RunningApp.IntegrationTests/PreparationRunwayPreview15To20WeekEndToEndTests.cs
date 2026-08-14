using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Backend Integration Phase 4G.6B — scoped public preview activation for
/// the 15-20 week TEN_K__4D__INTERMEDIATE Preparation Runway pipeline. Runs
/// against the real Api host + real Postgres DB, mirroring the established
/// Sw02/Sw12/Sw13 acceptance-test conventions. Proves the exact pilot-scoped
/// horizon matrix, non-pilot containment, confirmation containment, and
/// zero-write persistence for a successful runway preview.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PreparationRunwayPreview15To20WeekEndToEndTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PreparationRunwayPreview15To20WeekEndToEndTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync()
    {
        var response = await _client.PostRawAsync("/api/v1/testing/reset");
        response.EnsureSuccessStatusCode();
    }

    private static object RaceRequest(
        string startDate, string raceDate,
        string goalDistance = "ten_k", string level = "intermediate", int daysPerWeek = 4,
        double? recentWeeklyVolumeKm = 20, double? recentLongestRunKm = 8, int? recentRunsPerWeek = 3,
        string[]? preferredDays = null, string longRunDay = "sun",
        int targetFinishTimeSeconds = 3480, string targetFinishTimeSource = "product_average",
        object? recentRace = null) => new
    {
        goal_distance = goalDistance,
        level = level,
        days_per_week = daysPerWeek,
        unit = "km",
        start_date = startDate,
        preferred_days = preferredDays ?? new[] { "mon", "wed", "fri", "sun" },
        long_run_day = longRunDay,
        race_date = raceDate,
        target_finish_time_seconds = targetFinishTimeSeconds,
        target_finish_time_source = targetFinishTimeSource,
        race_name = (string?)null,
        recent_weekly_volume_km = recentWeeklyVolumeKm,
        recent_longest_run_km = recentLongestRunKm,
        recent_runs_per_week = recentRunsPerWeek,
        recent_race = recentRace,
    };

    private async Task<(int previews, int plans, int weeks, int days)> CountRowsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (
            await ctx.PlanPreviews.CountAsync(),
            await ctx.TrainingPlans.CountAsync(),
            await ctx.TrainingWeeks.CountAsync(),
            await ctx.TrainingDays.CountAsync());
    }

    // ── 15-20 week proof matrix (Provided evidence, READY-leaning profile) ──

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    public async Task PilotScope_FifteenToTwentyWeeks_Returns200_WithExactWeekAndSessionCounts(int totalWeeks)
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        var weeks = preview["weeks"]!.AsArray();
        Assert.Equal(totalWeeks, weeks.Count);

        var allDays = weeks.SelectMany(w => w!["days"]!.AsArray()).ToList();
        Assert.Equal(totalWeeks * 4, allDays.Count);

        // Global week numbering 1..N, chronological.
        Assert.Equal(Enumerable.Range(1, totalWeeks), weeks.Select(w => w!["week_number"]!.GetValue<int>()));

        // Runway/Core boundary: the first (totalWeeks-12) weeks carry a
        // runway_block; the final 12 do not.
        var runwayWeekCount = totalWeeks - 12;
        var runwayWeeks = weeks.Take(runwayWeekCount).ToList();
        var coreWeeks = weeks.Skip(runwayWeekCount).ToList();
        Assert.All(runwayWeeks, w => Assert.False(string.IsNullOrEmpty(w!["runway_block"]?.GetValue<string>())));
        Assert.All(coreWeeks, w => Assert.True(w!["runway_block"] is null || w["runway_block"]!.GetValue<string?>() is null));

        // Final runway block is PreSpecificTransition; first Core week type is not PreparationRunway.
        Assert.Equal("PRE_SPECIFIC_TRANSITION", runwayWeeks.Last()!["runway_block"]!.GetValue<string>());
        Assert.NotEqual("preparation_runway", coreWeeks.First()!["week_type"]!.GetValue<string>().ToLowerInvariant());

        // Non-confirmable/non-persistable lifecycle, explicit and typed.
        Assert.Equal("preparation_runway_preview_not_confirmable", preview["lifecycle"]!.GetValue<string>());

        // Distances and pace/effort present for every session.
        Assert.All(allDays, d => Assert.True(d!["distance_km"]!.GetValue<double>() >= 0));
        Assert.All(allDays, d => Assert.False(string.IsNullOrEmpty(d!["intensity"]!.GetValue<string>())));

        // Chronological order.
        var dates = allDays.Select(d => DateOnly.Parse(d!["date"]!.GetValue<string>().Substring(0, 10))).ToList();
        Assert.Equal(dates.OrderBy(d => d), dates);

        var after = await CountRowsAsync();
        // Preview row created (visible/inspectable), but no TrainingPlan/Week/Day.
        Assert.Equal(before.previews + 1, after.previews);
        Assert.Equal(before.plans, after.plans);
        Assert.Equal(before.weeks, after.weeks);
        Assert.Equal(before.days, after.days);
    }

    [Fact]
    public async Task PilotScope_MissingEvidence_StillSucceeds()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                recentWeeklyVolumeKm: null, recentLongestRunKm: null, recentRunsPerWeek: null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(17, preview["weeks"]!.AsArray().Count);
    }

    // ── Phase 4G.6B.2, Part 5: NoRecentRunningBase public-contract decision ──
    // Decision: Option A -- the existing public request contract already
    // distinguishes Missing (omitted/null) from NoRecentRunningBase
    // (explicit 0) through CatalogPrescriptionInputNormalizer.NormalizeDistance
    // (null -> NotProvided/Missing; any provided value including 0 ->
    // Available, and PreparationRunwayStartingLoadEvidenceAdapter further
    // classifies Available+0 as NoRecentRunningBase, distinct from
    // Available+>0 as Provided). No new request field is required.

    [Fact]
    public async Task PilotScope_ExplicitZeroRecentEvidence_NoRecentRunningBase_StillSucceeds()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);

        // Explicit 0 (not omitted/null) -- reaches PrescriptionInputState.Available
        // with Kilometers=0, distinct from the Missing/omitted case above.
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                recentWeeklyVolumeKm: 0, recentLongestRunKm: 0, recentRunsPerWeek: 0));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = System.Text.Json.Nodes.JsonNode.Parse(body)!;
        Assert.Equal(17, preview["weeks"]!.AsArray().Count);
        Assert.Equal("preparation_runway_preview_not_confirmable", preview["lifecycle"]!.GetValue<string>());
    }

    // ── Other-candidate / scope containment ──────────────────────────────────

    [Theory]
    [InlineData("five_k", "intermediate", 4)]
    [InlineData("ten_k", "beginner", 4)]
    [InlineData("ten_k", "intermediate", 3)]
    public async Task OutOfPilotScope_FifteenToTwentyWeeks_StillReturns422_PlanHorizonCompositionRequired(string goalDistance, string level, int daysPerWeek)
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);

        // target_finish_time_seconds/source use user_defined here (not the
        // ten_k-canonical product_average value RaceRequest defaults to) so
        // this exercises horizon/candidate-identity containment specifically,
        // not the unrelated product-average-vs-distance validation rule.
        object request = new
        {
            goal_distance = goalDistance,
            level,
            days_per_week = daysPerWeek,
            unit = "km",
            start_date = startDate.ToString("yyyy-MM-dd"),
            preferred_days = daysPerWeek == 3 ? new[] { "mon", "wed", "fri" } : new[] { "mon", "wed", "fri", "sun" },
            long_run_day = daysPerWeek == 3 ? "fri" : "sun",
            race_date = raceDate.ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480,
            target_finish_time_source = "user_defined",
            race_name = (string?)null,
            recent_weekly_volume_km = 20,
            recent_longest_run_km = 8,
            recent_runs_per_week = 3,
            recent_race = (object?)null,
        };

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", error!["errorCode"]!.GetValue<string>());
    }

    // ── 8-14 week regression: unchanged ──────────────────────────────────────

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task PilotScope_EightToFourteenWeeks_RemainOnExistingCorePath_NotPreparationRunwayLifecycle(int weeks)
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(weeks * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);
        Assert.Equal("core_confirmable", preview["lifecycle"]!.GetValue<string>());
        Assert.All(preview["weeks"]!.AsArray(), w => Assert.True(w!["runway_block"] is null));
    }

    // ── 21+ weeks: still unsupported for pilot scope ─────────────────────────

    [Fact]
    public async Task PilotScope_TwentyOneWeeks_StillReturns422_PlanHorizonCompositionRequired()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(21 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", error!["errorCode"]!.GetValue<string>());
    }

    // ── Phase 4G.6B.2: 52 / 53+ week public containment ───────────────────────

    [Theory]
    [InlineData(52)]
    [InlineData(53)]
    [InlineData(60)]
    public async Task PilotScope_FiftyTwoAndAbove_StillReturns422_PlanHorizonCompositionRequired_NoOrchestration(int weeks)
    {
        await ResetAsync();
        var before = await CountRowsAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(weeks * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var error = System.Text.Json.Nodes.JsonNode.Parse(body)!;
        // Same approved error as every other unsupported horizon -- no
        // General-Endurance-staged or Core-only fallback exists at 52/53+.
        Assert.Equal("PLAN_HORIZON_COMPOSITION_REQUIRED", error["errorCode"]!.GetValue<string>());
        Assert.Null(error["weeks"]);

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    // ── Phase 4G.6B.2: invalid long-run-day public HTTP proof ─────────────────

    [Fact]
    public async Task PilotScope_LongRunDayNotInPreferredDays_ReturnsTypedValidationError_NoOrchestrationOrPersistence()
    {
        await ResetAsync();
        var before = await CountRowsAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(18 * 7);

        // PreferredDays = mon/wed/fri/sun, but long_run_day = "tue" -- not a
        // member of PreferredDays. Reuses the existing, unmodified
        // GenerateRacePlanPreviewRequestValidator rule -- no runway-specific
        // duplicate validation was added.
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"), longRunDay: "tue"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());

        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

    // ── Confirmation containment: zero writes ────────────────────────────────

    [Fact]
    public async Task RunwayPreview_ConfirmIsRejected_NoTrainingPlanWeekOrDayWritten()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(18 * 7);

        var previewResponse = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = preview["preview_id"]!.GetValue<string>();

        var before = await CountRowsAsync();

        var confirmResponse = await _client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, confirmResponse.StatusCode);
        var error = await confirmResponse.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("CATALOG_PREVIEW_NOT_PERSISTABLE", error!["errorCode"]!.GetValue<string>());

        var after = await CountRowsAsync();
        Assert.Equal(before.plans, after.plans);
        Assert.Equal(before.weeks, after.weeks);
        Assert.Equal(before.days, after.days);
    }

    // ── Phase 4G.6B.1: evidence/pace-source HTTP matrix ───────────────────────

    [Fact]
    public async Task PilotScope_RecentRaceEvidence_Returns200_EffortOnlyRunwayPacing()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(18 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                recentRace: new { distance = "ten_k", finish_time_seconds = 2700, race_date = startDate.AddDays(-30).ToString("yyyy-MM-dd") }));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = System.Text.Json.Nodes.JsonNode.Parse(body)!;
        Assert.Equal(18, preview["weeks"]!.AsArray().Count);
        Assert.Equal("preparation_runway_preview_not_confirmable", preview["lifecycle"]!.GetValue<string>());

        var runwayWeeks = preview["weeks"]!.AsArray().Where(w => w!["runway_block"] is not null).ToList();
        var runwayDays = runwayWeeks.SelectMany(w => w!["days"]!.AsArray());
        // Runway pacing must stay effort-only regardless of pace-evidence
        // source -- never a race-specific/goal-pace label.
        Assert.All(runwayDays, d =>
        {
            var intensity = d!["intensity"]!.GetValue<string>();
            Assert.DoesNotContain("GOAL_PACE", intensity, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RACE_SPECIFIC", intensity, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task PilotScope_UserDefinedTargetTime_WithCorroboratingRecentRace_Returns200()
    {
        // A user-defined target is a GOAL, never independent evidence on its
        // own (see GoalFeasibilityResolver/PHASE4D_4_1) -- an "approved"
        // user-defined target in this system means it is backed by real
        // recent-race evidence that classifies it REALISTIC/CHALLENGING, not
        // that the bare target time is trusted at face value. This is a
        // pre-existing, unrelated governance decision, not something this
        // phase changes.
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(18 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                targetFinishTimeSeconds: 3300, targetFinishTimeSource: "user_defined",
                recentRace: new { distance = "ten_k", finish_time_seconds = 3200, race_date = startDate.AddDays(-30).ToString("yyyy-MM-dd") }));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = System.Text.Json.Nodes.JsonNode.Parse(body)!;
        Assert.Equal(18, preview["weeks"]!.AsArray().Count);

        var runwayDays = preview["weeks"]!.AsArray().Where(w => w!["runway_block"] is not null).SelectMany(w => w!["days"]!.AsArray());
        Assert.All(runwayDays, d => Assert.DoesNotContain("GOAL_PACE", d!["intensity"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PilotScope_UserDefinedTargetTime_NoIndependentEvidence_TypedFailure()
    {
        // Existing, pre-4G.6B governance behavior (unchanged by this phase):
        // a user-defined target with no recent-race evidence has no approved
        // classification rule and fails typed, not silently -- documented
        // here as the "unsupported/infeasible user target" fixture the task
        // asks for.
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(18 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                targetFinishTimeSeconds: 3300, targetFinishTimeSource: "user_defined"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("RUNTIME_CONDITION_UNSUPPORTED", error!["errorCode"]!.GetValue<string>());
    }

    [Fact]
    public async Task PilotScope_CautionEvidenceBand_Returns200_ConsistencyProfile()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(18 * 7);

        // Weekly volume in [8,15) with a strong longest-run triggers the
        // real CoreEntryReadinessResolver's CAUTION classification (neither
        // the READY nor NOT_READY thresholds), not a faked profile. 14km
        // (just under the 15km READY threshold) avoids an unrelated,
        // pre-existing Core volume-progression edge case at very low
        // starting weekly volumes (e.g. 10km cannot always build a full
        // 12-week taper without a residual-volume shortfall).
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                recentWeeklyVolumeKm: 14, recentLongestRunKm: 5));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = System.Text.Json.Nodes.JsonNode.Parse(body)!;
        var weeks = preview["weeks"]!.AsArray();
        Assert.Equal(18, weeks.Count);

        var runwayWeeks = weeks.Where(w => w!["runway_block"] is not null).ToList();
        var coreWeeks = weeks.Where(w => w!["runway_block"] is null).ToList();
        // CAUTION maps to the ConsistencyNeeded profile (same as NOT_READY) --
        // the final runway block is always PRE_SPECIFIC_TRANSITION, and no
        // Core week ever carries a runway_block.
        Assert.Equal("PRE_SPECIFIC_TRANSITION", runwayWeeks.Last()!["runway_block"]!.GetValue<string>());
        Assert.All(coreWeeks, w => Assert.NotEqual("preparation_runway", w!["week_type"]!.GetValue<string>().ToLowerInvariant()));

        // No internal orchestration trace/stage/failure-code detail leaks
        // into the public response body.
        Assert.DoesNotContain("OrchestrationTrace", body, StringComparison.Ordinal);
        Assert.DoesNotContain("FailureCode", body, StringComparison.Ordinal);
    }

    // ── Phase 4G.6B.1: calendar HTTP matrix ────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task PilotScope_LeadingPartialDays_EmergeFromDatesOnly_NoPartialWeekOrMisalignment(int leadingPartialDays)
    {
        await ResetAsync();
        var before = await CountRowsAsync();
        // 18 full weeks + a remainder, derived purely from StartDate/RaceDate
        // (never submitted directly) -- e.g. leadingPartialDays=3 means
        // RaceDate is 18*7+3 days after StartDate.
        var startDate = new DateOnly(2026, 8, 5); // a Wednesday
        var raceDate = startDate.AddDays(18 * 7 + leadingPartialDays);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = System.Text.Json.Nodes.JsonNode.Parse(body)!;
        var weeks = preview["weeks"]!.AsArray();

        // Exact full-week horizon and total counts, unaffected by remainder.
        Assert.Equal(18, weeks.Count);
        var allDays = weeks.SelectMany(w => w!["days"]!.AsArray()).ToList();
        Assert.Equal(18 * 4, allDays.Count);

        // No workout occurs inside the leading alignment span; first runway
        // session begins at or after StartDate + LeadingPartialDays.
        var firstSessionDate = allDays.Select(d => DateOnly.Parse(d!["date"]!.GetValue<string>().Substring(0, 10))).Min();
        Assert.True(firstSessionDate >= startDate.AddDays(leadingPartialDays));

        // No partial week: every week has exactly 4 sessions.
        Assert.All(weeks, w => Assert.Equal(4, w!["days"]!.AsArray().Count));

        // Final Core boundary (last 12 weeks) remains correct regardless of remainder.
        var runwayWeekCount = 18 - 12;
        Assert.Equal("PRE_SPECIFIC_TRANSITION", weeks[runwayWeekCount - 1]!["runway_block"]!.GetValue<string>());
        Assert.True(weeks[runwayWeekCount]!["runway_block"] is null);

        Assert.Equal("preparation_runway_preview_not_confirmable", preview["lifecycle"]!.GetValue<string>());

        var after = await CountRowsAsync();
        Assert.Equal(before.previews + 1, after.previews);
        Assert.Equal(before.plans, after.plans);
        Assert.Equal(before.weeks, after.weeks);
        Assert.Equal(before.days, after.days);
    }

    [Fact]
    public async Task PilotScope_ReorderedPreferredDays_ProducesSameSemanticSchedule()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(16 * 7);

        var forward = RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
            preferredDays: new[] { "mon", "wed", "fri", "sun" });
        var reversed = RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
            preferredDays: new[] { "sun", "fri", "wed", "mon" });

        var firstBody = await (await _client.PostRawAsync("/api/v1/plans/generate-preview/race", forward)).Content.ReadAsStringAsync();
        var secondBody = await (await _client.PostRawAsync("/api/v1/plans/generate-preview/race", reversed)).Content.ReadAsStringAsync();

        var first = System.Text.Json.Nodes.JsonNode.Parse(firstBody)!.AsObject();
        var second = System.Text.Json.Nodes.JsonNode.Parse(secondBody)!.AsObject();
        first.Remove("preview_id");
        second.Remove("preview_id");
        Assert.Equal(first.ToJsonString(), second.ToJsonString());

        var allowedDays = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var allDays = first["weeks"]!.AsArray().SelectMany(w => w!["days"]!.AsArray()).ToList();
        Assert.Equal(16 * 4, allDays.Count);
        Assert.All(allDays, d => Assert.Contains(DateOnly.Parse(d!["date"]!.GetValue<string>().Substring(0, 10)).DayOfWeek, allowedDays));
        Assert.Equal(allDays.Count, allDays.Select(d => d!["date"]!.GetValue<string>()).Distinct().Count());
    }

    [Fact]
    public async Task PilotScope_SaturdayLongRun_AllLongRunsLandOnSaturday()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(17 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"),
                preferredDays: new[] { "tue", "thu", "sat", "sun" }, longRunDay: "sat"));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = System.Text.Json.Nodes.JsonNode.Parse(body)!;
        var longRuns = preview["weeks"]!.AsArray().SelectMany(w => w!["days"]!.AsArray())
            .Where(d => d!["day_type"]!.GetValue<string>().Equals("long_run", StringComparison.OrdinalIgnoreCase));

        Assert.NotEmpty(longRuns);
        Assert.All(longRuns, d => Assert.Equal(DayOfWeek.Saturday, DateOnly.Parse(d!["date"]!.GetValue<string>().Substring(0, 10)).DayOfWeek));
    }

    [Fact]
    public async Task PilotScope_MonthAndYearCrossing_Returns200_WithCorrectWeekCount()
    {
        await ResetAsync();
        // StartDate mid-week, spanning a year boundary.
        var startDate = new DateOnly(2026, 12, 16);
        var raceDate = startDate.AddDays(17 * 7);

        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race",
            RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd")));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var preview = System.Text.Json.Nodes.JsonNode.Parse(body)!;
        Assert.Equal(17, preview["weeks"]!.AsArray().Count);
    }

    // ── Determinism ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RepeatedRequests_ProduceStructurallyIdenticalOutput_ExcludingPreviewId()
    {
        await ResetAsync();
        var startDate = new DateOnly(2026, 7, 20);
        var raceDate = startDate.AddDays(16 * 7);
        var request = RaceRequest(startDate.ToString("yyyy-MM-dd"), raceDate.ToString("yyyy-MM-dd"));

        var first = (await (await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request)).Content.ReadFromJsonAsync<JsonNode>())!;
        var second = (await (await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request)).Content.ReadFromJsonAsync<JsonNode>())!;

        first.AsObject().Remove("preview_id");
        second.AsObject().Remove("preview_id");
        Assert.Equal(first.ToJsonString(), second.ToJsonString());
    }
}
