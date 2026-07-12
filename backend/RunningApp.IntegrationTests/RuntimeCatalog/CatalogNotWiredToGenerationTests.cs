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
            Level = RunningBackground.NewToRunning,
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
            Level = RunningBackground.NewToRunning,
            DaysPerWeek = 3,
            Unit = DistanceUnit.Km,
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

        // Even though IPlanCatalogBundleLoader can successfully load
        // TEN_K__4D__INTERMEDIATE v10 (see PlanCatalogBundleLoaderTests), plan
        // generation does not consult it: this request must still fail exactly
        // as it did at the end of Phase 0, with no silent fallback.
        var ex = await Assert.ThrowsAsync<RunningApp.Application.Exceptions.CatalogCandidateNotPublishedException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.RunningRegularly,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
        }));

        Assert.Contains("TEN_K__4D__INTERMEDIATE", ex.Message); // Backend Integration Phase 4E.1: this pilot combination now routes to the catalog flow, which fails with CatalogCandidateNotPublishedException because TEN_K__4D__INTERMEDIATE v10 has status DRAFT, not PLAN_TEMPLATE_NOT_FOUND (the legacy SQL flow's error) -- it never reaches PlaceholderPlanGenerationEngine at all.
        Assert.Empty(context.PlanPreviews);
    }
}
