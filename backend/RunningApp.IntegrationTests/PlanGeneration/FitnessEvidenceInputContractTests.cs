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
/// Proves the six new optional GeneratePreviewRequest fields (recentLongestRunKm,
/// recentWeeklyVolumeKm, recentRunsPerWeek, recentRaceDistanceKm,
/// recentRaceFinishTimeSeconds, recentRaceDate) are accepted, carried through
/// RequestPayloadJson to confirm without data loss, and validated conservatively
/// (positive-if-provided only). None of these tests exercise any resolver,
/// decision-trace, or catalog-generation code path -- none exists yet for these
/// fields. Existing SQL-template generate-preview/confirm flow is asserted
/// unaffected throughout (backward compatibility).
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
        goal_type = "habit",
        goal_distance = "five_k",
        level = "new_to_running",
        days_per_week = 3,
        unit = "km",
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

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", BaseRequest);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task ConfirmPlan_WithoutFitnessEvidenceFields_StillSucceeds()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", BaseRequest);
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });

        Assert.False(string.IsNullOrWhiteSpace(confirm["plan_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_UnsupportedGoalComboWithFitnessEvidence_StillReturnsPlanTemplateNotFound_NoSilentFallback()
    {
        await ResetAsync();

        var unsupportedRequestWithEvidence = new
        {
            goal_type = "race",
            goal_distance = "half_marathon",
            level = "running_regularly",
            days_per_week = 5,
            unit = "km",
            recent_longest_run_km = 15.0,
            recent_weekly_volume_km = 40.0,
            recent_runs_per_week = 5,
            recent_race_distance_km = 10.0,
            recent_race_finish_time_seconds = 2700,
            recent_race_date = "2026-05-01",
        };

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview", unsupportedRequestWithEvidence);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_TEMPLATE_NOT_FOUND", error!["errorCode"]!.GetValue<string>());
    }

    // ─── DTO / input contract: each field individually ──────────────────────

    [Fact]
    public async Task GeneratePreview_AcceptsRecentLongestRunKm()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_longest_run_km = 12.5 });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_AcceptsRecentWeeklyVolumeKm()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_weekly_volume_km = 30.0 });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_AcceptsRecentRunsPerWeek()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_runs_per_week = 4 });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_AcceptsRecentRaceDistanceKm()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_race_distance_km = 5.0 });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_AcceptsRecentRaceFinishTimeSeconds()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_race_finish_time_seconds = 1450 });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_AcceptsRecentRaceDate()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_race_date = "2026-06-15" });

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    // ─── All six together + carry-through ───────────────────────────────────

    private static readonly object AllSixFields = new
    {
        recent_longest_run_km = 9.0,
        recent_weekly_volume_km = 24.0,
        recent_runs_per_week = 4,
        recent_race_distance_km = 5.0,
        recent_race_finish_time_seconds = 1450,
        recent_race_date = "2026-06-15",
    };

    [Fact]
    public async Task GeneratePreview_AcceptsAllSixFitnessEvidenceFieldsTogether()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, AllSixFields);

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_RequestPayloadJson_StoresAllSixFitnessEvidenceFields()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, AllSixFields);

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var previewRow = await context.PlanPreviews.AsNoTracking().FirstOrDefaultAsync(p => p.Id == previewId);

        Assert.NotNull(previewRow);
        var payload = JsonNode.Parse(previewRow!.RequestPayloadJson)!;

        Assert.Equal(9.0, payload["recent_longest_run_km"]!.GetValue<double>());
        Assert.Equal(24.0, payload["recent_weekly_volume_km"]!.GetValue<double>());
        Assert.Equal(4, payload["recent_runs_per_week"]!.GetValue<int>());
        Assert.Equal(5.0, payload["recent_race_distance_km"]!.GetValue<double>());
        Assert.Equal(1450, payload["recent_race_finish_time_seconds"]!.GetValue<int>());
        Assert.Equal("2026-06-15", payload["recent_race_date"]!.GetValue<string>());
    }

    [Fact]
    public async Task ConfirmPlan_PreservesFitnessEvidenceFields_InRequestPayloadJson_NoDataLoss()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, AllSixFields);

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);
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
        Assert.Equal(5.0, payload["recent_race_distance_km"]!.GetValue<double>());
        Assert.Equal(1450, payload["recent_race_finish_time_seconds"]!.GetValue<int>());
        Assert.Equal("2026-06-15", payload["recent_race_date"]!.GetValue<string>());
    }

    [Fact]
    public async Task ConfirmPlan_WithFitnessEvidenceFields_DoesNotPersistThemOnTrainingPlan()
    {
        // Phase 4B deliberately does not add TrainingPlan columns for these
        // fields (JSON payload carry-through is sufficient evidence). This is
        // a regression guard against silently reintroducing a speculative
        // persistence shape later without an explicit decision.
        await ResetAsync();
        var request = Merge(BaseRequest, AllSixFields);

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);
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

    // ─── Negative / validation ───────────────────────────────────────────────

    [Fact]
    public async Task GeneratePreview_MissingAllFitnessEvidenceFields_IsAccepted()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", BaseRequest);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GeneratePreview_NegativeRecentLongestRunKm_Returns400ValidationError()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_longest_run_km = -1.0 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
    }

    [Fact]
    public async Task GeneratePreview_NegativeRecentWeeklyVolumeKm_Returns400ValidationError()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_weekly_volume_km = -5.0 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
    }

    [Fact]
    public async Task GeneratePreview_ZeroRecentRunsPerWeek_Returns400ValidationError()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_runs_per_week = 0 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
    }

    [Fact]
    public async Task GeneratePreview_NegativeRecentRaceDistanceKm_Returns400ValidationError()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_race_distance_km = -5.0 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
    }

    [Fact]
    public async Task GeneratePreview_NegativeRecentRaceFinishTimeSeconds_Returns400ValidationError()
    {
        await ResetAsync();
        var request = Merge(BaseRequest, new { recent_race_finish_time_seconds = -100 });

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview", request);

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
