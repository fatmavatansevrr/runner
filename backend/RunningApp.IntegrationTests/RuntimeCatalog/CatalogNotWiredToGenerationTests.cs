using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.PlanGeneration;
using RunningApp.Application.Services;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog;

/// <summary>
/// Backend Integration Phase 1 regression guard: the new Process A catalog
/// loader and distance-family resolver exist and can be exercised
/// independently (see PlanCatalogBundleLoaderTests / CanonicalDistanceFamilyResolverTests),
/// but plan generation (PlaceholderPlanGenerationEngine / PlanServices) does
/// not consume either of them yet. TEN_K / INTERMEDIATE / 4-day requests
/// must still fail with PLAN_TEMPLATE_NOT_FOUND, and existing supported
/// seeded templates must still succeed with no silent fallback, exactly as
/// in Phase 0.
/// </summary>
public sealed class CatalogNotWiredToGenerationTests
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

    private static PlanServices NewServices(AppDbContext context) =>
        RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.Create(context);

    [Fact]
    public async Task ExistingSupportedSeededTemplate_PreviewStillSucceeds_WithFallbackFieldsFalseNull()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), new GeneratePreviewRequest
        {
            GoalType = GoalType.Habit,
            GoalDistance = GoalDistance.FiveK,
            Level = RunningBackground.Beginner,
            DaysPerWeek = 3,
            Unit = DistanceUnit.Km,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri },
        });

        Assert.Equal("habit_5k_beginner_3day_km_v1", response.TemplateId);
        Assert.False(response.FallbackUsed);
        Assert.Null(response.FallbackReason);
    }

    [Fact]
    public async Task TenKIntermediate4Day_StillReturnsPlanTemplateNotAvailable_CatalogNotYetWired()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);

        // Phase 4F.8.2 keeps the repository non-live: v10 is DRAFT and
        // activation defaults disabled. The pilot-shaped request reaches the
        // existing exact legacy template boundary with no silent fallback.
        var ex = await Assert.ThrowsAsync<RunningApp.Application.Exceptions.PlanTemplateNotAvailableException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            RaceDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(84),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            TargetFinishTimeSeconds = 3600,
        }));

        Assert.Contains("goal_distance=TenK", ex.Message);
        Assert.Empty(context.PlanPreviews);
    }
}
