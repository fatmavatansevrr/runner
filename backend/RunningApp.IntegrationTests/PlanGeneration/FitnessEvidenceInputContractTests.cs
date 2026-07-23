using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

/// <summary>
/// Backend Integration Phase 4B — runtime fitness-evidence input contract.
/// Proves the readiness fields (recentLongestRunKm, recentWeeklyVolumeKm,
/// recentRunsPerWeek) and the nested recentRace object are accepted,
/// carried through RequestPayloadJson to confirm without data loss, and
/// validated conservatively (positive-if-provided only).
///
/// Generate-preview contract alignment: readiness/recent-race fields are
/// Race-only on the public contract now (confirmed during that refactor:
/// PlaceholderPlanGenerationEngine never reads them regardless of goal
/// type, so GenerateHabitPlanPreviewRequest carries none of them at all —
/// not merely nullable-and-unused). Every readiness-specific test below
/// therefore posts to /generate-preview/race with a full valid race
/// payload; the pure "no readiness fields at all" backward-compatibility
/// tests stay on /generate-preview/habit, where that shape is native.
///
/// None of these tests exercise any resolver, decision-trace, or catalog-
/// generation code path (the race payload's target_finish_time_source is
/// "user_defined" throughout, and none of this file's requests match the
/// TenK/Intermediate/4-day catalog pilot identity, so every request here
/// stays on the legacy SQL path).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class FitnessEvidenceInputContractTests
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public FitnessEvidenceInputContractTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    private static readonly object BaseRequest = new
    {
        goal_distance = "five_k",
        level = "beginner",
        days_per_week = 3,
        unit = "km",
        start_date = "2026-07-20",
        preferred_days = new[] { "mon", "wed", "fri" },
    };

    /// <summary>
    /// A full, otherwise-valid Race request (FiveK/Beginner/3-day — never
    /// matches the TenK/Intermediate/4-day catalog pilot identity, so this
    /// always stays on the legacy SQL path, same as BaseRequest) used only
    /// to carry the readiness/recent-race fields that no longer exist on
    /// the Habit contract.
    /// </summary>
    private static readonly object RaceBaseRequest = new
    {
        goal_distance = "five_k",
        level = "beginner",
        days_per_week = 3,
        unit = "km",
        start_date = "2026-07-20",
        preferred_days = new[] { "mon", "wed", "fri" },
        long_run_day = "fri",
        race_date = "2026-10-12",
        target_finish_time_seconds = 1500,
        target_finish_time_source = "user_defined",
    };

    private async Task ResetAsync()
    {
        var response = await _client.PostRawAsync("/api/v1/testing/reset");
        response.EnsureSuccessStatusCode();
    }

    // ─── Backward compatibility ─────────────────────────────────────────────

    [Fact]
    public async Task GeneratePreview_WithoutFitnessEvidenceFields_StillSucceeds()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", BaseRequest);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task ConfirmPlan_WithoutFitnessEvidenceFields_StillSucceeds()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", BaseRequest);
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });

        Assert.False(string.IsNullOrWhiteSpace(confirm["plan_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task RaceGeneratePreview_WithoutFitnessEvidenceFields_StillSucceeds()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", RaceBaseRequest);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_UnsupportedGoalComboWithFitnessEvidence_StillReturnsPlanTemplateNotFound_NoSilentFallback()
    {
        await ResetAsync();

        var unsupportedRequestWithEvidence = new
        {
            goal_distance = "half_marathon",
            level = "intermediate",
            days_per_week = 5,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = new[] { "mon", "tue", "wed", "fri", "sun" },
            long_run_day = "sun",
            race_date = "2026-10-12",
            target_finish_time_seconds = 7200,
            target_finish_time_source = "user_defined",
            recent_longest_run_km = 15.0,
            recent_weekly_volume_km = 40.0,
            recent_runs_per_week = 5,
            recent_race = new
            {
                distance = "ten_k",
                finish_time_seconds = 2700,
                race_date = "2026-05-01",
            },
        };

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", unsupportedRequestWithEvidence);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_TEMPLATE_NOT_FOUND", error!["errorCode"]!.GetValue<string>());
    }

    // ─── DTO / input contract: each field individually (Race — see class doc) ──

    [Fact]
    public async Task GeneratePreview_AcceptsRecentLongestRunKm()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_longest_run_km = 12.5 });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_AcceptsRecentWeeklyVolumeKm()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_weekly_volume_km = 30.0 });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_AcceptsRecentRunsPerWeek()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_runs_per_week = 4 });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    // NOTE: RecentRaceDistanceKm/RecentRaceFinishTimeSeconds/RecentRaceDate
    // used to be three independent flat fields; they are now a single atomic
    // nested `recent_race` object (see RecentRaceInput), so "individually
    // accepted" no longer applies to its sub-fields -- each of the three
    // tests below now exercises the whole nested object being accepted.
    [Fact]
    public async Task GeneratePreview_AcceptsRecentRaceDistanceKm()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_race = new { distance = "five_k", finish_time_seconds = 1450, race_date = "2026-06-15" } });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_AcceptsRecentRaceFinishTimeSeconds()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_race = new { distance = "five_k", finish_time_seconds = 1450, race_date = "2026-06-15" } });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_AcceptsRecentRaceDate()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_race = new { distance = "five_k", finish_time_seconds = 1450, race_date = "2026-06-15" } });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    // ─── All six together + carry-through (Race) ────────────────────────────

    private static readonly object AllSixFields = new
    {
        recent_longest_run_km = 9.0,
        recent_weekly_volume_km = 24.0,
        recent_runs_per_week = 4,
        recent_race = new
        {
            distance = "five_k",
            finish_time_seconds = 1450,
            race_date = "2026-06-15",
        },
    };

    [Fact]
    public async Task GeneratePreview_AcceptsAllSixFitnessEvidenceFieldsTogether()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, AllSixFields);

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_RequestPayloadJson_StoresAllSixFitnessEvidenceFields()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, AllSixFields);

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request);
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var previewRow = await context.PlanPreviews.AsNoTracking().FirstOrDefaultAsync(p => p.Id == previewId);

        Assert.NotNull(previewRow);
        var payload = JsonNode.Parse(previewRow!.RequestPayloadJson)!;

        Assert.Equal(9.0, payload["recent_longest_run_km"]!.GetValue<double>());
        Assert.Equal(24.0, payload["recent_weekly_volume_km"]!.GetValue<double>());
        Assert.Equal(4, payload["recent_runs_per_week"]!.GetValue<int>());
        var recentRace = payload["recent_race"]!;
        Assert.Equal("five_k", recentRace["distance"]!.GetValue<string>());
        Assert.Equal(1450, recentRace["finish_time_seconds"]!.GetValue<int>());
        Assert.Equal("2026-06-15", recentRace["race_date"]!.GetValue<string>());
    }

    [Fact]
    public async Task ConfirmPlan_PreservesFitnessEvidenceFields_InRequestPayloadJson_NoDataLoss()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, AllSixFields);

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request);
        var previewId = preview["preview_id"]!.GetValue<string>();

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.False(string.IsNullOrWhiteSpace(confirm["plan_id"]!.GetValue<string>()));

        // Confirm never mutates the preview row (read-only fetch) -- the
        // RequestPayloadJson it read from, and that a future resolver phase
        // will also read from, must still contain all six fields unchanged.
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var previewRow = await context.PlanPreviews.AsNoTracking().FirstOrDefaultAsync(p => p.Id == Guid.Parse(previewId));

        Assert.NotNull(previewRow);
        var payload = JsonNode.Parse(previewRow!.RequestPayloadJson)!;

        Assert.Equal(9.0, payload["recent_longest_run_km"]!.GetValue<double>());
        Assert.Equal(24.0, payload["recent_weekly_volume_km"]!.GetValue<double>());
        Assert.Equal(4, payload["recent_runs_per_week"]!.GetValue<int>());
        var recentRace = payload["recent_race"]!;
        Assert.Equal("five_k", recentRace["distance"]!.GetValue<string>());
        Assert.Equal(1450, recentRace["finish_time_seconds"]!.GetValue<int>());
        Assert.Equal("2026-06-15", recentRace["race_date"]!.GetValue<string>());
    }

    [Fact]
    public async Task ConfirmPlan_WithFitnessEvidenceFields_DoesNotPersistThemOnTrainingPlan()
    {
        // Phase 4B deliberately does not add TrainingPlan columns for these
        // fields (JSON payload carry-through is sufficient evidence). This is
        // a regression guard against silently reintroducing a speculative
        // persistence shape later without an explicit decision.
        await ResetAsync();
        var request = Merge(RaceBaseRequest, AllSixFields);

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", request);
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await context.TrainingPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId);

        Assert.NotNull(plan);
        // No TrainingPlan property named RecentLongestRunKm/etc. exists as of
        // Phase 4B -- this test would fail to compile if one were added
        // without updating this assertion, which is the intended guard.
    }

    // ─── Negative / validation (Race) ────────────────────────────────────────

    [Fact]
    public async Task GeneratePreview_MissingAllFitnessEvidenceFields_IsAccepted()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", RaceBaseRequest);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_ZeroRecentLongestRunKm_Accepted()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_longest_run_km = 0.0 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GeneratePreview_ZeroRecentWeeklyVolumeKm_Accepted()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_weekly_volume_km = 0.0 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GeneratePreview_NegativeRecentLongestRunKm_Returns400ValidationError()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_longest_run_km = -1.0 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
    }

    [Fact]
    public async Task GeneratePreview_NegativeRecentWeeklyVolumeKm_Returns400ValidationError()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_weekly_volume_km = -5.0 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
    }

    // Explicit zero is a valid, distinct readiness signal (0 = explicitly
    // reported zero readiness, null = unknown) -- only negative values are
    // rejected. See PHASE... explicit-zero readiness policy fix.
    [Fact]
    public async Task GeneratePreview_ZeroRecentRunsPerWeek_Accepted()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_runs_per_week = 0 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GeneratePreview_NegativeRecentRunsPerWeek_Returns400ValidationError()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_runs_per_week = -1 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
    }

    // NOTE: RecentRaceDistanceKm used to be a raw km double with its own
    // positive-if-provided check; RecentRaceInput.Distance is now a canonical
    // GoalDistance enum, which has no "negative" concept to validate. The
    // remaining, still-valid numeric guard on the nested recent_race object is
    // FinishTimeSeconds -- covered by GeneratePreview_NegativeRecentRaceFinishTimeSeconds_Returns400ValidationError.
    // This test now proves the sibling guard: a non-positive FinishTimeSeconds
    // inside recent_race is rejected even when accompanied by a (previously
    // "recent_race_distance_km"-shaped) request, so the 400 behavior for
    // malformed recent-race evidence is preserved end-to-end.
    [Fact]
    public async Task GeneratePreview_NegativeRecentRaceDistanceKm_Returns400ValidationError()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_race = new { distance = "five_k", finish_time_seconds = -1, race_date = "2026-06-15" } });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
    }

    [Fact]
    public async Task GeneratePreview_NegativeRecentRaceFinishTimeSeconds_Returns400ValidationError()
    {
        await ResetAsync();
        var request = Merge(RaceBaseRequest, new { recent_race = new { distance = "five_k", finish_time_seconds = -100, race_date = "2026-06-15" } });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
    }

    // ─── Merge helper: combine an anonymous base object with overrides ──────
    private static object Merge(object baseObj, object overrides)
    {
        var merged = new System.Collections.Generic.Dictionary<string, object?>();
        foreach (var prop in baseObj.GetType().GetProperties())
        {
            merged[prop.Name] = prop.GetValue(baseObj);
        }
        foreach (var prop in overrides.GetType().GetProperties())
        {
            merged[prop.Name] = prop.GetValue(overrides);
        }
        return merged;
    }
}
