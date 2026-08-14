using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Entities;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Phase4G5MPaceSourceMatrixTests
{
    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
            new RuntimeConditionResolutionResultConverter(),
        },
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Phase4G5MPaceSourceMatrixTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() =>
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    private static Dictionary<string, object?> Request(
        int weeks,
        string targetSource = "product_average",
        int targetSeconds = 3480,
        object? recentRace = null) => new()
    {
        ["goal_distance"] = "ten_k",
        ["level"] = "intermediate",
        ["days_per_week"] = 4,
        ["unit"] = "km",
        ["start_date"] = "2026-07-20",
        ["preferred_days"] = new[] { "mon", "wed", "fri", "sun" },
        ["long_run_day"] = "sun",
        ["race_name"] = "Phase 4G.5M pace matrix",
        ["race_date"] = new DateOnly(2026, 7, 20).AddDays(weeks * 7).ToString("yyyy-MM-dd"),
        ["target_finish_time_seconds"] = targetSeconds,
        ["target_finish_time_source"] = targetSource,
        ["recent_weekly_volume_km"] = 20,
        ["recent_longest_run_km"] = 8,
        ["recent_runs_per_week"] = 3,
        ["recent_race"] = recentRace,
    };

    private static object RecentRace(int finishTimeSeconds = 1700, string raceDate = "2026-07-01") => new
    {
        distance = "five_k",
        finish_time_seconds = finishTimeSeconds,
        race_date = raceDate,
    };

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task ProductAverageTargetTime_SucceedsAndPrescriptionMatchesPersistence(int weeks)
    {
        await AssertSuccessfulSourceAsync(
            Request(weeks), weeks,
            expectedPaceSource: "TARGET_TIME",
            expectedPaceReason: "TARGET_FINISH_TIME_PROVIDED",
            expectedFeasibility: "CHALLENGING",
            expectedFeasibilityReason: "PACE_SOURCE_TARGET_TIME_PRODUCT_AVERAGE_ACCEPTED");
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task RecentRaceEvidence_SucceedsAndPrescriptionMatchesPersistence(int weeks)
    {
        await AssertSuccessfulSourceAsync(
            Request(weeks, "user_defined", recentRace: RecentRace()), weeks,
            expectedPaceSource: "RECENT_RACE",
            expectedPaceReason: "RECENT_RACE_RESULT_PROVIDED",
            expectedFeasibility: "REALISTIC",
            expectedFeasibilityReason: "WITHIN_REALISTIC_BAND");
    }

    [Fact]
    public async Task StaleRecentRace_RemainsTraceOnlyAndDoesNotBecomeAFalsePublicFailure()
    {
        await ResetAsync();
        var (response, preview, snapshot) = await GenerateWithSnapshotAsync(
            Request(12, "user_defined", recentRace: RecentRace(raceDate: "2025-01-01")));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pace = Assert.Single(snapshot.ResolverResults, r => r.ConditionType == "PACE_SOURCE_IN");
        Assert.Equal("RECENT_RACE", pace.OutputValue);
        Assert.Equal("NOT_USABLE_AS_PACE_ANCHOR", pace.Metadata["paceRecencyConfidence"]);
        Assert.False(preview["fallback_used"]!.GetValue<bool>());
        await ResetAsync();
    }

    [Fact]
    public async Task EvaluatedUnsupportedRecentRace_UsesEstablishedSafeStageFallbackRatherThanResolverFailure()
    {
        await ResetAsync();
        var (response, preview, snapshot) = await GenerateWithSnapshotAsync(
            Request(12, "user_defined", recentRace: RecentRace(finishTimeSeconds: 2000)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var feasibility = Assert.Single(snapshot.ResolverResults, r => r.ConditionType == "GOAL_FEASIBILITY_IN");
        Assert.Equal("UNSUPPORTED", feasibility.OutputValue);
        Assert.Equal("EXCEEDS_CHALLENGING_BAND", feasibility.ReasonCode);
        Assert.DoesNotContain(snapshot.GeneratedPreviewPlanPayload!.Weeks.SelectMany(w => w.Sessions),
            s => s.Provenance.SourceProgressionStepKey == "GOAL_PACE_REHEARSAL");
        Assert.False(preview["fallback_used"]!.GetValue<bool>()); // field means legacy-template fallback
        await ResetAsync();
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task UserDefinedTargetWithoutIndependentEvidence_FailsClosedWithNoPersistence(int weeks)
    {
        await ResetAsync();
        var before = await CountsAsync();
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(weeks, "user_defined"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("RUNTIME_CONDITION_UNSUPPORTED", error!["errorCode"]!.GetValue<string>());
        Assert.Contains("PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE", error["message"]!.GetValue<string>());
        Assert.Equal(before, await CountsAsync());
    }

    [Theory]
    [InlineData("zero_target")]
    [InlineData("invalid_recent_race")]
    [InlineData("future_recent_race")]
    public async Task InvalidPaceEvidence_IsValidationErrorWithNoPersistence(string kind)
    {
        await ResetAsync();
        var before = await CountsAsync();
        var request = kind switch
        {
            "zero_target" => Request(12, "user_defined", 0),
            "invalid_recent_race" => Request(12, "user_defined", recentRace: RecentRace(0)),
            "future_recent_race" => Request(12, "user_defined", recentRace: RecentRace(raceDate: "2099-01-01")),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
        Assert.Equal(before, await CountsAsync());
    }

    [Fact]
    public async Task UnsupportedPaceSourceToken_IsModelBinding400WithNoPersistence()
    {
        await ResetAsync();
        var before = await CountsAsync();
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(12, "unsupported_source"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await CountsAsync());
    }

    private async Task AssertSuccessfulSourceAsync(
        Dictionary<string, object?> request,
        int weeks,
        string expectedPaceSource,
        string expectedPaceReason,
        string expectedFeasibility,
        string expectedFeasibilityReason)
    {
        await ResetAsync();
        var (response, preview, snapshot) = await GenerateWithSnapshotAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(preview["fallback_used"]!.GetValue<bool>());
        Assert.Equal(weeks, preview["weeks"]!.AsArray().Count);

        var pace = Assert.Single(snapshot.ResolverResults, r => r.ConditionType == "PACE_SOURCE_IN");
        Assert.Equal(expectedPaceSource, pace.OutputValue);
        Assert.Equal(expectedPaceReason, pace.ReasonCode);
        var feasibility = Assert.Single(snapshot.ResolverResults, r => r.ConditionType == "GOAL_FEASIBILITY_IN");
        Assert.Equal(expectedFeasibility, feasibility.OutputValue);
        Assert.Equal(expectedFeasibilityReason, feasibility.ReasonCode);

        var payload = snapshot.GeneratedPreviewPlanPayload!;
        Assert.All(payload.Weeks.SelectMany(w => w.Sessions), session =>
        {
            Assert.True(session.TargetDistanceKm is > 0 || session.EstimatedDistanceKm is > 0);
            Assert.NotNull(session.PacePrescription);
            Assert.False(string.IsNullOrWhiteSpace(session.PacePrescription.EffortLabel));
            Assert.Equal(Enumerable.Range(1, session.Segments.Count), session.Segments.Select(s => s.SegmentOrder));
        });

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new
        {
            preview_id = preview["preview_id"]!.GetValue<string>()
        });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var days = await db.TrainingDays.AsNoTracking().Where(d => d.PlanId == planId).ToArrayAsync();
        Assert.Equal(weeks * 4, days.Length);
        var publicByDate = preview["weeks"]!.AsArray().SelectMany(w => w!["days"]!.AsArray())
            .ToDictionary(d => DateOnly.Parse(d!["date"]!.GetValue<string>()[..10]));
        foreach (var day in days)
        {
            var publicDay = publicByDate[DateOnly.FromDateTime(day.Date)];
            Assert.Equal(day.PlannedDistanceKm, publicDay!["distance_km"]!.GetValue<double>(), 6);
            Assert.Equal(day.PlannedDurationMin, publicDay["duration_min"]!.GetValue<int>());
            Assert.Equal(day.Intensity, publicDay["intensity"]?.GetValue<string>());
            var prescription = JsonNode.Parse(day.CatalogPrescriptionJson!)!;
            Assert.Equal(1, prescription["schema_version"]!.GetValue<int>());
            Assert.NotNull(prescription["pace"]);
            Assert.NotNull(prescription["segments"]);
            Assert.NotNull(prescription["provenance"]);
        }
        await ResetAsync();
    }

    private async Task<(HttpResponseMessage response, JsonNode preview, CatalogPreviewSnapshot snapshot)>
        GenerateWithSnapshotAsync(Dictionary<string, object?> request)
    {
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return (response, preview, null!);
        }
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == previewId);
        var snapshot = JsonSerializer.Deserialize<CatalogPreviewSnapshot>(row.PreviewPayloadJson, SnapshotOptions)!;
        return (response, preview, snapshot);
    }

    private async Task<(int previews, int plans, int weeks, int days)> CountsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.PlanPreviews.CountAsync(), await db.TrainingPlans.CountAsync(),
            await db.TrainingWeeks.CountAsync(), await db.TrainingDays.CountAsync());
    }
}
