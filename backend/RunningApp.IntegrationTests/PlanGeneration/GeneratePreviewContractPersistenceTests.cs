using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Services;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

/// <summary>
/// End-to-end mapping tests for the generate-preview contract alignment,
/// exercised through the real legacy SQL path (<see cref="PlanServices.GeneratePreviewAsync"/>)
/// against the repository's existing seeded template
/// ("habit_5k_beginner_3day_km_v1"), asserting that the request's own
/// serialized JSON -- as persisted on <see cref="PlanPreview.RequestPayloadJson"/> --
/// preserves the 0-vs-null distinction and the nested RecentRace shape.
/// </summary>
public sealed class GeneratePreviewContractPersistenceTests
{
    private static AppDbContext NewSeededContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.PlanTemplates.Add(new PlanTemplate
        {
            Id = Guid.NewGuid(),
            TemplateId = "habit_5k_beginner_3day_km_v1",
            Version = 1,
            GoalType = GoalType.Habit,
            GoalDistance = GoalDistance.FiveK,
            Level = RunningBackground.Beginner,
            DaysPerWeek = 3,
            Unit = DistanceUnit.Km,
            DataJson = "{\"templateId\":\"habit_5k_beginner_3day_km_v1\",\"version\":1,\"goalType\":\"habit\",\"goalDistance\":\"five_k\",\"level\":\"beginner\",\"daysPerWeek\":3,\"unit\":\"km\",\"weeks\":[{\"weekNumber\":1,\"weekType\":\"build\",\"days\":[{\"slotIndex\":1,\"dayType\":\"easy\",\"distanceKm\":2.0,\"durationMin\":20,\"intensity\":\"z2\"},{\"slotIndex\":2,\"dayType\":\"easy\",\"distanceKm\":2.5,\"durationMin\":25,\"intensity\":\"z2\"},{\"slotIndex\":3,\"dayType\":\"long_run\",\"distanceKm\":3.0,\"durationMin\":30,\"intensity\":\"z2\"}]}]}",
            CreatedAt = DateTime.UtcNow,
        });
        context.SaveChanges();
        return context;
    }

    private static PlanServices NewServices(AppDbContext context) => TestPlanServicesFactory.Create(context);

    private static GeneratePreviewRequest HabitRequest() => new()
    {
        GoalType = GoalType.Habit,
        GoalDistance = GoalDistance.FiveK,
        Level = RunningBackground.Beginner,
        DaysPerWeek = 3,
        Unit = DistanceUnit.Km,
        StartDate = new DateOnly(2026, 7, 20),
        PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat },
    };

    [Fact]
    public async Task EndToEnd_HabitFiveKBeginner3Day_MapsSuccessfully()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), HabitRequest());

        Assert.Equal("habit_5k_beginner_3day_km_v1", response.TemplateId);
        Assert.NotEmpty(response.Weeks);
        Assert.False(response.FallbackUsed);
    }

    [Fact]
    public async Task ExplicitZero_RecentWeeklyVolumeKm_SurvivesPersistence_AsZero_NotNull()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);
        var request = HabitRequest();
        request.RecentWeeklyVolumeKm = 0;

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        var preview = await context.PlanPreviews.SingleAsync(p => p.Id == response.PreviewId);
        using var doc = JsonDocument.Parse(preview.RequestPayloadJson);
        var volumeProperty = doc.RootElement.GetProperty("recent_weekly_volume_km");
        Assert.Equal(JsonValueKind.Number, volumeProperty.ValueKind);
        Assert.Equal(0, volumeProperty.GetDouble());
    }

    [Fact]
    public async Task ExplicitZero_RecentLongestRunKm_SurvivesPersistence_AsZero_NotNull()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);
        var request = HabitRequest();
        request.RecentLongestRunKm = 0;

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        var preview = await context.PlanPreviews.SingleAsync(p => p.Id == response.PreviewId);
        using var doc = JsonDocument.Parse(preview.RequestPayloadJson);
        var property = doc.RootElement.GetProperty("recent_longest_run_km");
        Assert.Equal(JsonValueKind.Number, property.ValueKind);
        Assert.Equal(0, property.GetDouble());
    }

    [Fact]
    public async Task ExplicitZero_RecentRunsPerWeek_SurvivesPersistence_AsZero_NotNull()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);
        var request = HabitRequest();
        request.RecentRunsPerWeek = 0;

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        var preview = await context.PlanPreviews.SingleAsync(p => p.Id == response.PreviewId);
        using var doc = JsonDocument.Parse(preview.RequestPayloadJson);
        var property = doc.RootElement.GetProperty("recent_runs_per_week");
        Assert.Equal(JsonValueKind.Number, property.ValueKind);
        Assert.Equal(0, property.GetInt32());
    }

    [Fact]
    public async Task MissingReadiness_SurvivesPersistence_AsNull_NotZero()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);
        var request = HabitRequest();
        request.RecentWeeklyVolumeKm = null;
        request.RecentLongestRunKm = null;
        request.RecentRunsPerWeek = null;

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        var preview = await context.PlanPreviews.SingleAsync(p => p.Id == response.PreviewId);
        using var doc = JsonDocument.Parse(preview.RequestPayloadJson);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("recent_weekly_volume_km").ValueKind);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("recent_longest_run_km").ValueKind);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("recent_runs_per_week").ValueKind);
    }

    [Fact]
    public async Task NestedRecentRace_SurvivesPersistence_AsNestedObject()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);
        var request = HabitRequest();
        request.RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3510, RaceDate = new DateOnly(2026, 6, 1) };

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        var preview = await context.PlanPreviews.SingleAsync(p => p.Id == response.PreviewId);
        using var doc = JsonDocument.Parse(preview.RequestPayloadJson);
        var recentRace = doc.RootElement.GetProperty("recent_race");
        Assert.Equal(JsonValueKind.Object, recentRace.ValueKind);
        Assert.Equal(3510, recentRace.GetProperty("finish_time_seconds").GetInt32());
    }

    [Fact]
    public async Task RecentRace_Absent_SurvivesPersistence_AsExplicitNull_NeverLeaksIntoTargetRaceFields()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);
        var request = HabitRequest();

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        var preview = await context.PlanPreviews.SingleAsync(p => p.Id == response.PreviewId);
        using var doc = JsonDocument.Parse(preview.RequestPayloadJson);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("recent_race").ValueKind);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("race_date").ValueKind);
    }

    [Fact]
    public async Task InvalidPreferredDaysCount_Throws_BeforeAnyPersistence()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);
        var request = HabitRequest();
        request.PreferredDays = new[] { Weekday.Mon, Weekday.Wed };

        await Assert.ThrowsAsync<ArgumentException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public async Task RaceRequest_MissingLongRunDay_Throws_BeforeAnyPersistence()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);
        var request = HabitRequest();
        request.GoalType = GoalType.Race;
        request.RaceDate = new DateOnly(2026, 10, 12);
        request.TargetFinishTimeSeconds = 3600;
        request.LongRunDay = null;

        await Assert.ThrowsAsync<ArgumentException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Empty(context.PlanPreviews);
    }
}
