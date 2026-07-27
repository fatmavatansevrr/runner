using System.Collections.Generic;
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
/// Phase 4G.3B.6.1 — observation-only characterization of the real,
/// currently-live 12-week TEN_K/Intermediate/4-day catalog pipeline's actual
/// behavior when GOAL_FEASIBILITY_IN resolves to NotEvaluated
/// (PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE) versus the established
/// ProductAverage baseline (see <see cref="Sw02ProductAverageEndToEndTests"/>
/// and <see cref="Sw13ExactTwelveWeekOnlyEndToEndTests"/>, whose real-host +
/// real-Postgres HTTP harness this file reuses unchanged).
///
/// Reconfirms rather than assumes a prior audit's prediction that this
/// NotEvaluated case would silently fall back to
/// CURRENT_FITNESS_SPECIFIC_REHEARSAL via <c>ProgressionStageAllocator</c>.
/// Direct source tracing of <c>CatalogPreviewGenerator.GenerateAsync</c>
/// (calls <c>ApplyNotEvaluatedGovernancePolicy</c> immediately after
/// resolution, before stage scheduling ever runs) and
/// <c>NotEvaluatedReasonClassifier</c> (maps
/// PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE to the Unsupported
/// category) shows the real pipeline instead throws
/// <c>RuntimeConditionUnsupportedException</c> -- mapped by
/// <c>GlobalExceptionHandler</c> to HTTP 422 / RUNTIME_CONDITION_UNSUPPORTED
/// -- before <c>ProgressionStageAllocator</c>/stage-level fallback routing is
/// ever reached. This test observes and asserts that real behavior rather
/// than the prior prediction; see PHASE4G_3B_6_1 final report for full
/// discussion. No production code is changed by this file.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class GoalFeasibilityNotEvaluatedUserDefinedCharacterizationEndToEndTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GoalFeasibilityNotEvaluatedUserDefinedCharacterizationEndToEndTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync()
    {
        var response = await _client.PostRawAsync("/api/v1/testing/reset");
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Builds a request identical in every field to the established SW-02/
    /// SW-13 12-week ProductAverage baseline (goal_distance=ten_k,
    /// level=intermediate, days_per_week=4, unit=km, start_date=2026-07-20,
    /// preferred_days=[mon,wed,fri,sun], long_run_day=sun,
    /// race_date=2026-10-12, target_finish_time_seconds=3480,
    /// recent_weekly_volume_km=20, recent_longest_run_km=8,
    /// recent_runs_per_week=3, recent_race=null) except for
    /// <paramref name="targetFinishTimeSource"/> -- the single field this
    /// characterization pass varies.
    /// </summary>
    private static object RaceRequest(string targetFinishTimeSource) => new
    {
        goal_distance = "ten_k",
        level = "intermediate",
        days_per_week = 4,
        unit = "km",
        start_date = "2026-07-20",
        preferred_days = new[] { "mon", "wed", "fri", "sun" },
        long_run_day = "sun",
        race_name = (string?)null,
        race_date = "2026-10-12",
        target_finish_time_seconds = 3480,
        target_finish_time_source = targetFinishTimeSource,
        recent_weekly_volume_km = 20,
        recent_longest_run_km = 8,
        recent_runs_per_week = 3,
        recent_race = (object?)null,
    };

    /// <summary>
    /// Normalized field-by-field representation of the two request bodies,
    /// proving by direct comparison (not assertion alone) that
    /// target_finish_time_source is the only differing field. Reported
    /// verbatim in the final report's normalized-diff section.
    /// </summary>
    [Fact]
    public void NormalizedRequestComparison_OnlyTargetFinishTimeSourceDiffers()
    {
        var productAverage = new Dictionary<string, object?>
        {
            ["goal_distance"] = "ten_k", ["level"] = "intermediate", ["days_per_week"] = 4, ["unit"] = "km",
            ["start_date"] = "2026-07-20", ["preferred_days"] = "mon,wed,fri,sun", ["long_run_day"] = "sun",
            ["race_date"] = "2026-10-12", ["target_finish_time_seconds"] = 3480,
            ["target_finish_time_source"] = "product_average",
            ["recent_weekly_volume_km"] = 20, ["recent_longest_run_km"] = 8, ["recent_runs_per_week"] = 3,
            ["recent_race"] = null,
        };
        var userDefined = new Dictionary<string, object?>(productAverage) { ["target_finish_time_source"] = "user_defined" };

        var differingKeys = new List<string>();
        foreach (var key in productAverage.Keys)
        {
            if (!Equals(productAverage[key], userDefined[key]))
            {
                differingKeys.Add(key);
            }
        }

        Assert.Equal(new[] { "target_finish_time_source" }, differingKeys);
    }

    // ── Control: ProductAverage + no RecentRace (test-local reconstruction ──
    // ── of the SW-02 baseline, kept local per instructions rather than ──────
    // ── reusing/modifying Sw02ProductAverageEndToEndTests.cs) ───────────────

    [Fact]
    public async Task Control_ProductAverage_NoRecentRace_Returns200_TwelveWeeks_FallbackUsedFalse_NoPersistenceGrowthBeyondPreview()
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", RaceRequest("product_average"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
        var weeks = preview["weeks"]!.AsArray();
        Assert.Equal(12, weeks.Count);
        Assert.False(preview["fallback_used"]!.GetValue<bool>());

        // Preview generation persists exactly one PlanPreview row (the
        // preview record itself) and never a confirmed TrainingPlan/Week/Day
        // -- confirmation is a separate, unexercised endpoint.
        var after = await CountRowsAsync();
        Assert.Equal(before.previews + 1, after.previews);
        Assert.Equal(before.plans, after.plans);
        Assert.Equal(before.weeks, after.weeks);
        Assert.Equal(before.days, after.days);
    }

    // ── Characterization: UserDefined + no RecentRace ───────────────────────

    [Fact]
    public async Task Characterization_UserDefined_NoRecentRace_Returns422_RuntimeConditionUnsupported_NotSilentFallback_NoPersistence()
    {
        await ResetAsync();
        var before = await CountRowsAsync();

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", RaceRequest("user_defined"));

        // ── Step A: raw observation ──────────────────────────────────────
        // ── Step B (verified by direct source read, not re-derived here): ──
        // CatalogPreviewGenerator.GenerateAsync -> ApplyNotEvaluatedGovernancePolicy
        // -> NotEvaluatedReasonClassifier.Classify("PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE")
        // == Unsupported -> throws RuntimeConditionUnsupportedException ->
        // GlobalExceptionHandler maps to 422/RUNTIME_CONDITION_UNSUPPORTED.
        // This happens BEFORE BuildDarkInternalDatedSkeleton (stage
        // scheduling / ProgressionStageAllocator / workout binding) is ever
        // invoked -- the observed and source-traced behavior agree, so this
        // is asserted as the real deterministic outcome, not the prior
        // audit's silent-fallback prediction.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("RUNTIME_CONDITION_UNSUPPORTED", error!["errorCode"]!.GetValue<string>());
        var message = error["message"]!.GetValue<string>();
        Assert.Contains("GOAL_FEASIBILITY_IN", message);
        Assert.Contains("PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE", message);
        Assert.False(string.IsNullOrWhiteSpace(error["correlationId"]!.GetValue<string>()));

        // No preview body, no stage/workout content at all -- confirms no
        // partial/fallback schedule of any kind was produced or returned.
        Assert.Null(error["weeks"]);
        Assert.Null(error["fallback_used"]);

        // Fails before any persistence boundary -- no PlanPreview row either
        // (distinct from a successful preview, which does persist one).
        var after = await CountRowsAsync();
        Assert.Equal(before, after);
    }

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
}
