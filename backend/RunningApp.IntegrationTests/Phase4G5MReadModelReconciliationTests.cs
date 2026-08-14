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

/// <summary>
/// Phase 4G.5M real-host reconciliation. Public read models intentionally do
/// not expose internal catalog phase/provenance/prescription fields; those are
/// compared between the frozen snapshot and persistence, while public models
/// are compared on their complete shared DTO surface.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Phase4G5MReadModelReconciliationTests
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

    public Phase4G5MReadModelReconciliationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() =>
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    private static object Request(DateOnly start, int weeks) => new
    {
        goal_distance = "ten_k",
        level = "intermediate",
        days_per_week = 4,
        unit = "km",
        start_date = start.ToString("yyyy-MM-dd"),
        preferred_days = new[] { "mon", "wed", "fri", "sun" },
        long_run_day = "sun",
        race_name = "Phase 4G.5M reconciliation",
        race_date = start.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
        target_finish_time_seconds = 3480,
        target_finish_time_source = "product_average",
        recent_weekly_volume_km = 20,
        recent_longest_run_km = 8,
        recent_runs_per_week = 3,
        recent_race = (object?)null,
    };

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task PreviewSnapshotPersistenceAndAllReadModels_HaveIdenticalSharedSemantics(int weeks)
    {
        await ResetAsync();
        var start = new DateOnly(2026, 7, 20);

        var previewResponse = await _client.PostRawAsync(
            "/api/v1/plans/generate-preview/race", Request(start, weeks));
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        var confirm = await _client.PostJsonAsync(
            "/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        TrainingPlan plan;
        PlanPreview previewRow;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            plan = await db.TrainingPlans.AsNoTracking()
                .Include(p => p.Weeks).ThenInclude(w => w.Days)
                .SingleAsync(p => p.Id == planId);
            previewRow = await db.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == previewId);
        }

        Assert.Equal(planId, previewRow.ConfirmedPlanId);
        var snapshot = JsonSerializer.Deserialize<CatalogPreviewSnapshot>(previewRow.PreviewPayloadJson, SnapshotOptions);
        Assert.NotNull(snapshot);
        Assert.True(CatalogPreviewSnapshotVerifier.Verify(snapshot!));
        var payload = Assert.IsType<RunningApp.Application.RuntimeCatalog.Schedule.GeneratedCatalogPlanPayload>(
            snapshot.GeneratedPreviewPlanPayload);
        Assert.Equal(weeks, payload.PlannedWeekCount);
        Assert.Equal(start, payload.StartDate);
        Assert.Equal(plan.CatalogCandidateKey, snapshot.CandidateKey);
        Assert.Equal(plan.CatalogCandidateVersion, snapshot.CandidateVersion);

        var persistedDays = plan.Weeks.SelectMany(w => w.Days).OrderBy(d => d.Date).ToArray();
        var payloadDays = payload.Weeks.SelectMany(w => w.Sessions).OrderBy(d => d.Date).ToArray();
        Assert.Equal(persistedDays.Select(d => DateOnly.FromDateTime(d.Date)), payloadDays.Select(d => d.Date));
        Assert.Equal(preview["weeks"]!.AsArray().SelectMany(w => w!["days"]!.AsArray()).Count(), persistedDays.Length);

        foreach (var persisted in persistedDays)
        {
            var source = Assert.Single(payloadDays, d => d.Date == DateOnly.FromDateTime(persisted.Date));
            Assert.Equal(source.Provenance.SourceWorkoutKey, persisted.CatalogWorkoutDefinitionKey);
            Assert.Equal(source.Provenance.SourceWorkoutVersion, persisted.CatalogWorkoutDefinitionVersion);
            Assert.Equal(source.Provenance.SourceLayoutSlotRole, persisted.CatalogStructuralRole);
            Assert.Equal(source.TargetDistanceKm ?? source.EstimatedDistanceKm, persisted.PlannedDistanceKm);
            Assert.Equal(source.TargetDurationMinutes ?? source.EstimatedDurationMinutes ?? 0, persisted.PlannedDurationMin);
            Assert.Equal(source.PlannedIntensity, persisted.Intensity);

            var prescription = JsonNode.Parse(persisted.CatalogPrescriptionJson!)!;
            Assert.Equal("CATALOG_SESSION_PRESCRIPTION_SNAPSHOT", prescription["schema_key"]!.GetValue<string>());
            Assert.Equal(source.WorkoutType.ToString(), prescription["workout_type"]!.GetValue<string>());
            Assert.Equal(source.PrescriptionBasis.ToString(), prescription["prescription_basis"]!.GetValue<string>());
            Assert.Equal(source.Segments.Count, prescription["segments"]!.AsArray().Count);
            Assert.Equal(ToSnake(source.PacePrescription.PaceType.ToString()),
                prescription["pace"]!["pace_type"]!.GetValue<string>());
        }

        await AssertDetailsAsync(plan, persistedDays);
        await AssertCalendarAsync(planId, persistedDays);
        await AssertTrainingDayDetailsAsync(persistedDays);
        await AssertHomeAsync(plan, persistedDays);

        await ResetAsync();
    }

    private async Task AssertDetailsAsync(TrainingPlan plan, IReadOnlyCollection<TrainingDay> persistedDays)
    {
        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Equal(plan.Id.ToString(), details["plan_id"]!.GetValue<string>());
        Assert.Equal("active", details["status"]!.GetValue<string>());
        Assert.Equal("race", details["goal_type"]!.GetValue<string>());
        Assert.Equal("ten_k", details["goal_distance"]!.GetValue<string>());
        Assert.Equal("intermediate", details["level"]!.GetValue<string>());
        Assert.Equal(plan.DaysPerWeek, details["days_per_week"]!.GetValue<int>());
        Assert.Equal(plan.RaceDate!.Value.ToString("yyyy-MM-dd"), details["race_date"]!.GetValue<string>());
        Assert.Equal(plan.LongRunDay, details["long_run_day"]!.GetValue<string>());
        Assert.Equal(plan.Weeks.Count, details["total_weeks"]!.GetValue<int>());

        var detailWeeks = details["weeks"]!.AsArray();
        Assert.Equal(plan.Weeks.Count, detailWeeks.Count);
        Assert.Equal(plan.StartedAt.Date, DateTime.Parse(detailWeeks[0]!["start_date"]!.GetValue<string>()).Date);
        var detailDays = detailWeeks.SelectMany(w => w!["days"]!.AsArray()).ToArray();
        Assert.Equal(persistedDays.Select(d => d.Id).Order(), detailDays.Select(d => DayId(d!)).Order());
        foreach (var node in detailDays)
        {
            AssertSharedDay(node!, persistedDays.Single(d => d.Id == DayId(node!)));
        }
    }

    private async Task AssertCalendarAsync(Guid planId, IReadOnlyCollection<TrainingDay> persistedDays)
    {
        var first = DateOnly.FromDateTime(persistedDays.Min(d => d.Date));
        var last = DateOnly.FromDateTime(persistedDays.Max(d => d.Date));
        var month = new DateOnly(first.Year, first.Month, 1);
        var calendarSessions = new List<JsonNode>();
        while (month <= new DateOnly(last.Year, last.Month, 1))
        {
            var response = await _client.GetAsync($"/api/v1/plans/active/calendar?month={month:yyyy-MM}");
            response.EnsureSuccessStatusCode();
            var returned = (await response.Content.ReadFromJsonAsync<JsonNode>())!.AsArray();
            Assert.All(returned, d => Assert.Equal(month.Month, DateTime.Parse(d!["date"]!.GetValue<string>()).Month));
            calendarSessions.AddRange(returned.Where(d => d!["day_id"] is not null).Select(d => d!));
            month = month.AddMonths(1);
        }

        Assert.Equal(persistedDays.Select(d => d.Id).Order(), calendarSessions.Select(DayId).Order());
        Assert.Equal(calendarSessions.Count, calendarSessions.Select(DayId).Distinct().Count());
        foreach (var node in calendarSessions)
        {
            AssertSharedDay(node, persistedDays.Single(d => d.Id == DayId(node)));
        }

        Assert.NotEqual(Guid.Empty, planId); // calendar DTO has no plan-id field by contract
    }

    private async Task AssertTrainingDayDetailsAsync(IReadOnlyCollection<TrainingDay> persistedDays)
    {
        var selected = new List<TrainingDay>
        {
            persistedDays.First(d => d.CatalogStructuralRole == "KEY_SESSION"),
            persistedDays.First(d => d.CatalogStructuralRole == "EASY_SUPPORT"),
            persistedDays.First(d => d.CatalogStructuralRole == "LONG_RUN"),
            persistedDays.Where(d => d.CatalogPhaseKey == "TAPER").OrderBy(d => d.Date).Last(),
        };
        var boundary = persistedDays.FirstOrDefault(d => d.Date.Day == 1 || d.Date.Day == DateTime.DaysInMonth(d.Date.Year, d.Date.Month));
        if (boundary is not null) selected.Add(boundary);

        foreach (var persisted in selected.DistinctBy(d => d.Id))
        {
            var detail = await _client.GetJsonAsync($"/api/v1/training-days/{persisted.Id}");
            AssertSharedDay(detail, persisted);
            Assert.Equal(persisted.CompletedAt?.ToString("O"), detail["completed_at"]?.GetValue<string>());
        }
    }

    private async Task AssertHomeAsync(TrainingPlan plan, IReadOnlyCollection<TrainingDay> persistedDays)
    {
        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Equal(plan.Id.ToString(), home["active_plan"]!["plan_id"]!.GetValue<string>());

        var today = DateTime.UtcNow.Date;
        var weeks = plan.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        var current = weeks.LastOrDefault(w => w.StartDate.Date <= today) ?? weeks[0];
        Assert.Equal($"Week {current.WeekNumber} of {weeks.Length}", home["active_plan"]!["progress_text"]!.GetValue<string>());
        var weekPersisted = persistedDays.Where(d => d.WeekId == current.Id).ToArray();
        var homeRealDays = home["week_summary"]!.AsArray().Where(d => d!["day_id"] is not null).Select(d => d!).ToArray();
        Assert.Equal(weekPersisted.Select(d => d.Id).Order(), homeRealDays.Select(DayId).Order());
        foreach (var node in homeRealDays)
        {
            AssertSharedDay(node, weekPersisted.Single(d => d.Id == DayId(node)));
        }

        var todayPersisted = persistedDays.SingleOrDefault(d => d.Date.Date == today);
        if (todayPersisted is null)
        {
            Assert.Null(home["today_workout"]!["day_id"]);
            Assert.Equal("rest", home["today_workout"]!["day_type"]!.GetValue<string>());
        }
        else
        {
            AssertSharedDay(home["today_workout"]!, todayPersisted);
        }
    }

    private static Guid DayId(JsonNode node) => Guid.Parse(node["day_id"]!.GetValue<string>());

    private static void AssertSharedDay(JsonNode node, TrainingDay day)
    {
        Assert.Equal(day.Id, DayId(node));
        Assert.Equal(day.Date.Date, DateTime.Parse(node["date"]!.GetValue<string>()).Date);
        Assert.Equal(ToSnake(day.DayType.ToString()), node["day_type"]!.GetValue<string>());
        Assert.Equal(ToSnake(day.Status.ToString()), node["status"]!.GetValue<string>());
        Assert.Equal(day.Title, node["title"]!.GetValue<string>());
        Assert.Equal(day.Description, node["description"]!.GetValue<string>());
        Assert.Equal(day.PlannedDistanceKm, node["planned_distance_km"]!.GetValue<double>(), 6);
        Assert.Equal(day.PlannedDurationMin, node["planned_duration_min"]!.GetValue<int>());
        Assert.Equal(day.PlannedPaceMinKm, node["planned_pace_min_km"]?.GetValue<double>());
        Assert.Equal(day.Intensity, node["intensity"]?.GetValue<string>());
        Assert.Equal(day.IsLongRun, node["is_long_run"]!.GetValue<bool>());
        Assert.Equal(day.CanMarkComplete, node["can_mark_complete"]!.GetValue<bool>());
        Assert.Equal(day.CanMarkNotToday, node["can_mark_not_today"]!.GetValue<bool>());
    }

    private static string ToSnake(string value) =>
        string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
