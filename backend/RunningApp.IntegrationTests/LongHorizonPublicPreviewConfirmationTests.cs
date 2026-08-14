using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview;
using RunningApp.Application.Services;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonPublicPreviewConfirmationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonPublicPreviewConfirmationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() =>
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    private static object RaceRequest(int weeks, string distance = "ten_k", string level = "intermediate", int days = 4)
    {
        var start = new DateOnly(2026, 9, 7);
        return new
        {
            goal_distance = distance,
            level,
            days_per_week = days,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = days == 3
                ? new[] { "mon", "wed", "sun" }
                : new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun",
            race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average",
            race_name = "Phase 4L.3",
            recent_weekly_volume_km = 20.0,
            recent_longest_run_km = 8.0,
            recent_runs_per_week = 4,
        };
    }

    private async Task<JsonNode> PreviewAsync(int weeks = 21)
    {
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race/long-horizon", RaceRequest(weeks));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonNode.Parse(body)!;
    }

    [Theory]
    [InlineData(21)]
    [InlineData(25)]
    [InlineData(52)]
    public async Task SupportedBoundaries_ReturnRealPublicSafePreview(int weeks)
    {
        await ResetAsync();
        var preview = await PreviewAsync(weeks);

        Assert.Equal("rolling_long_horizon", preview["schedule_strategy"]!.GetValue<string>());
        Assert.Equal(weeks, preview["total_weeks"]!.GetValue<int>());
        Assert.Equal(weeks, preview["structural_roadmap"]!.AsArray().Count);
        Assert.NotEmpty(preview["current_executable_weeks"]!.AsArray());
        Assert.DoesNotContain(preview["structural_roadmap"]!.AsArray(), week =>
            week!["lifecycle_status"]!.GetValue<string>() == "pending" &&
            (week["assigned_date"] is not null || week["distance_km"] is not null || week["weekly_volume_km"] is not null));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var previewId = preview["preview_id"]!.GetValue<Guid>();
        Assert.True(await db.PlanPreviews.AnyAsync(p => p.Id == previewId));
        Assert.False(await db.TrainingPlans.AnyAsync(p => p.SourcePreviewId == previewId));
        Assert.False(await db.LongHorizonRollingPlanStates.AnyAsync(p => p.Id == previewId));
    }

    [Fact]
    public async Task TwentyWeeks_RemainsOnExistingRoute_AndDedicatedRouteRejects()
    {
        await ResetAsync();
        var existing = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", RaceRequest(20));
        Assert.Equal(HttpStatusCode.OK, existing.StatusCode);

        var dedicated = await _client.PostRawAsync("/api/v1/plans/generate-preview/race/long-horizon", RaceRequest(20));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, dedicated.StatusCode);
    }

    [Fact]
    public async Task FiftyThreeWeeks_UsesExactSupportedWindowError()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race/long-horizon", RaceRequest(53));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("five_k", "intermediate", 4)]
    [InlineData("ten_k", "beginner", 4)]
    [InlineData("ten_k", "intermediate", 3)]
    public async Task UnsupportedPilotCombination_Rejects(string distance, string level, int days)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race/long-horizon", RaceRequest(21, distance, level, days));
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity });
    }

    [Fact]
    public async Task Confirm_PersistsOneRollingPlan_AllStructuralWeeks_AndNoFakeStaticRows()
    {
        await ResetAsync();
        var preview = await PreviewAsync(21);
        var previewId = preview["preview_id"]!.GetValue<Guid>();

        var confirm = await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId, contract_version = 1 });
        var body = await confirm.Content.ReadAsStringAsync();
        Assert.True(confirm.IsSuccessStatusCode, body);
        var result = JsonNode.Parse(body)!;
        Assert.Equal("rolling_long_horizon", result["schedule_strategy"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.SingleAsync(p => p.SourcePreviewId == previewId);
        Assert.Equal(PlanScheduleStrategy.RollingLongHorizon, plan.ScheduleStrategy);
        Assert.Equal(21, await db.LongHorizonRollingWeekStates.CountAsync(w => w.PlanStateId == previewId));
        Assert.True(await db.LongHorizonRollingSessionStates.AnyAsync(s => s.Week!.PlanStateId == previewId));
        Assert.Equal(0, await db.TrainingWeeks.CountAsync(w => w.PlanId == plan.Id));
        Assert.Equal(0, await db.TrainingDays.CountAsync(d => d.PlanId == plan.Id));
        Assert.Equal(plan.Id, (await db.PlanPreviews.SingleAsync(p => p.Id == previewId)).ConfirmedPlanId);
    }

    [Fact]
    public async Task ExactReplay_ReturnsExistingPlan_WithoutDuplicates()
    {
        await ResetAsync();
        var previewId = (await PreviewAsync())["preview_id"]!.GetValue<Guid>();
        var first = await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId });
        var second = await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId });
        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();
        var firstJson = JsonNode.Parse(await first.Content.ReadAsStringAsync())!;
        var secondJson = JsonNode.Parse(await second.Content.ReadAsStringAsync())!;
        Assert.Equal(firstJson["plan_id"]!.GetValue<Guid>(), secondJson["plan_id"]!.GetValue<Guid>());
        Assert.Equal("already_confirmed", secondJson["outcome"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.TrainingPlans.CountAsync(p => p.SourcePreviewId == previewId));
        Assert.Equal(1, await db.LongHorizonRollingPlanStates.CountAsync(p => p.Id == previewId));
    }

    [Fact]
    public async Task HomeAndCalendar_ReturnRollingReadModels_ForRollingPlan()
    {
        await ResetAsync();
        var previewId = (await PreviewAsync())["preview_id"]!.GetValue<Guid>();
        (await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId })).EnsureSuccessStatusCode();

        var home = await _client.GetAsync("/api/v1/plans/active/home");
        var calendar = await _client.GetAsync("/api/v1/plans/active/calendar?month=2026-09");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Equal(HttpStatusCode.OK, calendar.StatusCode);
        Assert.Contains("rolling_long_horizon", await home.Content.ReadAsStringAsync());
        Assert.Contains("rolling_long_horizon", await calendar.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CrossUserConfirmation_IsIndistinguishableFromMissing()
    {
        await ResetAsync();
        var previewId = (await PreviewAsync())["preview_id"]!.GetValue<Guid>();
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILongHorizonPublicPlanService>();
        await Assert.ThrowsAsync<LongHorizonPreviewNotFoundException>(() =>
            service.ConfirmAsync(Guid.NewGuid(), new LongHorizonConfirmPlanRequest { PreviewId = previewId }));
    }

    [Fact]
    public async Task ExpiredAndCorruptAuthority_FailClosed()
    {
        await ResetAsync();
        var expiredId = (await PreviewAsync())["preview_id"]!.GetValue<Guid>();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.PlanPreviews.SingleAsync(p => p.Id == expiredId);
            row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        var expired = await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = expiredId });
        Assert.Equal(HttpStatusCode.Gone, expired.StatusCode);

        var corruptId = (await PreviewAsync())["preview_id"]!.GetValue<Guid>();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.PlanPreviews.SingleAsync(p => p.Id == corruptId);
            row.StructuralRoadmapFingerprint = "tampered";
            await db.SaveChangesAsync();
        }
        var corrupt = await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = corruptId });
        Assert.Equal(HttpStatusCode.Conflict, corrupt.StatusCode);
    }

    [Fact]
    public async Task MissingPreviewAndUnsupportedConfirmationVersion_MapSafely()
    {
        await ResetAsync();
        var missing = await _client.PostRawAsync(
            "/api/v1/plans/confirm/long-horizon", new { preview_id = Guid.NewGuid(), contract_version = 1 });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Contains("LONG_HORIZON_PREVIEW_NOT_FOUND", await missing.Content.ReadAsStringAsync());

        var previewId = (await PreviewAsync())["preview_id"]!.GetValue<Guid>();
        var incompatible = await _client.PostRawAsync(
            "/api/v1/plans/confirm/long-horizon", new { preview_id = previewId, contract_version = 2 });
        Assert.Equal(HttpStatusCode.Conflict, incompatible.StatusCode);
        Assert.Contains("LONG_HORIZON_PREVIEW_STALE", await incompatible.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TwoPreviewsForOneUser_EnforceOneActivePlan()
    {
        await ResetAsync();
        var firstId = (await PreviewAsync())["preview_id"]!.GetValue<Guid>();
        var secondId = (await PreviewAsync(25))["preview_id"]!.GetValue<Guid>();
        (await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = firstId })).EnsureSuccessStatusCode();
        var loser = await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = secondId });
        Assert.Equal(HttpStatusCode.Conflict, loser.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ownerId = await db.PlanPreviews.Where(p => p.Id == firstId).Select(p => p.InternalUserId).SingleAsync();
        Assert.Equal(1, await db.TrainingPlans.CountAsync(p => p.InternalUserId == ownerId && p.Status == TrainingPlanStatus.Active));
        Assert.Null((await db.PlanPreviews.SingleAsync(p => p.Id == secondId)).ConfirmedPlanId);
    }

    [Fact]
    public async Task ConcurrentSamePreview_HasOnePlanAndIdempotentLoser()
    {
        await ResetAsync();
        var previewId = (await PreviewAsync())["preview_id"]!.GetValue<Guid>();
        using var secondClient = _factory.CreateClient();

        var requests = new[]
        {
            _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId }),
            secondClient.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId }),
        };
        var responses = await Task.WhenAll(requests);
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.TrainingPlans.CountAsync(p => p.SourcePreviewId == previewId));
        Assert.Equal(1, await db.LongHorizonRollingPlanStates.CountAsync(p => p.Id == previewId));
        Assert.Equal(21, await db.LongHorizonRollingWeekStates.CountAsync(w => w.PlanStateId == previewId));
    }

    [Theory]
    [InlineData(1)] // AfterRollingInitialization
    [InlineData(2)] // AfterPlanOwnership
    [InlineData(3)] // AfterPreviewStatusUpdate
    [InlineData(4)] // BeforeCommit
    public async Task PreCommitFailure_RollsBackEveryOwnedRow_AndPreviewRemainsReusable(
        int failpointValue)
    {
        var failpoint = (LongHorizonConfirmationFailpoint)failpointValue;
        await ResetAsync();
        var previewId = (await PreviewAsync())["preview_id"]!.GetValue<Guid>();
        Guid ownerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ownerId = (await db.PlanPreviews.Where(p => p.Id == previewId).Select(p => p.InternalUserId).SingleAsync())!.Value;
            var service = new LongHorizonPublicPlanService(
                db,
                scope.ServiceProvider.GetRequiredService<ICatalogCandidateEligibilityGate>(),
                scope.ServiceProvider.GetRequiredService<IOptions<PlanCatalogOptions>>(),
                scope.ServiceProvider.GetRequiredService<IOptions<LongHorizonGenerationOptions>>(),
                scope.ServiceProvider.GetRequiredService<ILogger<LongHorizonPublicPlanService>>(),
                new LongHorizonTestConfirmationFailureInjector(failpoint));
            await Assert.ThrowsAsync<LongHorizonInjectedConfirmationFailureException>(() =>
                service.ConfirmAsync(ownerId, new LongHorizonConfirmPlanRequest { PreviewId = previewId }));
        }

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.TrainingPlans.AnyAsync(p => p.SourcePreviewId == previewId));
            Assert.False(await db.LongHorizonRollingPlanStates.AnyAsync(p => p.Id == previewId));
            Assert.Null((await db.PlanPreviews.SingleAsync(p => p.Id == previewId)).ConfirmedPlanId);
        }

        var retry = await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId });
        retry.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PostCommitAcknowledgementLoss_RetryRecoversExistingPlan()
    {
        await ResetAsync();
        var previewId = (await PreviewAsync())["preview_id"]!.GetValue<Guid>();
        Guid ownerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ownerId = (await db.PlanPreviews.Where(p => p.Id == previewId).Select(p => p.InternalUserId).SingleAsync())!.Value;
            var service = new LongHorizonPublicPlanService(
                db,
                scope.ServiceProvider.GetRequiredService<ICatalogCandidateEligibilityGate>(),
                scope.ServiceProvider.GetRequiredService<IOptions<PlanCatalogOptions>>(),
                scope.ServiceProvider.GetRequiredService<IOptions<LongHorizonGenerationOptions>>(),
                scope.ServiceProvider.GetRequiredService<ILogger<LongHorizonPublicPlanService>>(),
                new LongHorizonTestConfirmationFailureInjector(LongHorizonConfirmationFailpoint.AfterCommitBeforeAcknowledgement));
            await Assert.ThrowsAsync<LongHorizonInjectedConfirmationFailureException>(() =>
                service.ConfirmAsync(ownerId, new LongHorizonConfirmPlanRequest { PreviewId = previewId }));
        }

        var retry = await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = previewId });
        retry.EnsureSuccessStatusCode();
        var json = JsonNode.Parse(await retry.Content.ReadAsStringAsync())!;
        Assert.Equal("already_confirmed", json["outcome"]!.GetValue<string>());
    }

    [Fact]
    public async Task Swagger_ContainsDedicatedPublicRoutes_AndNoInternalSnapshot()
    {
        var swagger = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/v1/plans/generate-preview/race/long-horizon", swagger);
        Assert.Contains("/api/v1/plans/confirm/long-horizon", swagger);
        Assert.DoesNotContain("LongHorizonServerPreviewSnapshot", swagger);
    }
}
