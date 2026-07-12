using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.PlanGeneration;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.Services;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.5 regression guard: now that a CONCRETE
/// orchestration service exists (and is registered in DI for the first
/// time), re-prove it is not invoked anywhere in live generation. Existing
/// generate-preview/confirm behavior, including Phase 0's no-silent-fallback
/// guarantee, must remain byte-for-byte unchanged.
/// </summary>
public sealed class RuntimeConditionResolutionServiceNotWiredToGenerationTests
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
    public async Task ExistingSupportedSeededTemplate_PreviewStillSucceeds_OrchestrationNeverInvoked()
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
            TargetFinishTimeSeconds = 3000,
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
            RecentWeeklyVolumeKm = 24.0,
            RecentLongestRunKm = 9.0,
        });

        Assert.Equal("habit_5k_beginner_3day_km_v1", response.TemplateId);
        Assert.False(response.FallbackUsed);
        Assert.Null(response.FallbackReason);
    }

    [Fact]
    public async Task TenKIntermediate4Day_StillReturnsPlanTemplateNotAvailable_OrchestrationNeverInvoked()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);

        var ex = await Assert.ThrowsAsync<RunningApp.Application.Exceptions.CatalogCandidateNotPublishedException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.RunningRegularly,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            TargetFinishTimeSeconds = 3000,
        }));

        Assert.Contains("TEN_K__4D__INTERMEDIATE", ex.Message); // Backend Integration Phase 4E.1: this pilot combination now routes to the catalog flow, which fails with CatalogCandidateNotPublishedException because TEN_K__4D__INTERMEDIATE v10 has status DRAFT, not PLAN_TEMPLATE_NOT_FOUND (the legacy SQL flow's error) -- it never reaches PlaceholderPlanGenerationEngine at all.
        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public void PlanServices_ConstructorDependencies_DoNotIncludeOrchestrationServiceOrAnyResolver()
    {
        var constructorParamTypes = typeof(PlanServices)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(RuntimeConditionResolutionService));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IRuntimeConditionResolutionService));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(ITimeAdequacyResolver));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IPaceSourceResolver));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(ICoreEntryReadinessResolver));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IGoalFeasibilityResolver));
    }

    [Fact]
    public void PlaceholderPlanGenerationEngine_ConstructorDependencies_DoNotIncludeOrchestrationServiceOrAnyResolver()
    {
        var constructorParamTypes = typeof(PlaceholderPlanGenerationEngine)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(RuntimeConditionResolutionService));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IRuntimeConditionResolutionService));
    }

    [Fact]
    public void GeneratePreviewResponse_HasNoOrchestrationOrResolverProperty()
    {
        var responseProperties = typeof(GeneratePreviewResponse).GetProperties().Select(p => p.PropertyType).ToList();

        Assert.DoesNotContain(responseProperties, t => t == typeof(RuntimeConditionResolutionResult));
        Assert.DoesNotContain(responseProperties, t => t == typeof(ResolverDecisionTrace));
    }
}
