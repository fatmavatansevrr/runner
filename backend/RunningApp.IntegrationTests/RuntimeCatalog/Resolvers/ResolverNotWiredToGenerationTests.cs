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
/// Backend Integration Phase 4C regression guard: the new resolver contract
/// (models, interfaces, registry validator) exists and can be exercised
/// independently (see ResolverContractTests / RegistryValidationTests), but
/// PlanServices / PlaceholderPlanGenerationEngine do not consume any of it.
/// Existing generate-preview/confirm behavior, including the Phase 0
/// no-silent-fallback guarantee, must remain byte-for-byte unchanged.
/// </summary>
public sealed class ResolverNotWiredToGenerationTests
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
    public async Task ExistingSupportedSeededTemplate_PreviewWithFitnessEvidence_StillSucceeds_NoResolverInvoked()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);

        // Providing Phase 4B fitness-evidence fields must not change generation
        // behavior in any way -- Phase 4C added no consumer for them.
        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), new GeneratePreviewRequest
        {
            GoalType = GoalType.Habit,
            GoalDistance = GoalDistance.FiveK,
            Level = RunningBackground.NewToRunning,
            DaysPerWeek = 3,
            Unit = DistanceUnit.Km,
            RecentLongestRunKm = 9.0,
            RecentWeeklyVolumeKm = 24.0,
            RecentRunsPerWeek = 4,
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
        });

        Assert.Equal("habit_5k_beginner_3day_km_v1", response.TemplateId);
        Assert.False(response.FallbackUsed);
        Assert.Null(response.FallbackReason);
    }

    [Fact]
    public async Task TenKIntermediate4Day_WithFitnessEvidence_StillReturnsPlanTemplateNotAvailable()
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
            RecentLongestRunKm = 9.0,
            RecentWeeklyVolumeKm = 24.0,
        }));

        Assert.Contains("TEN_K__4D__INTERMEDIATE", ex.Message); // Backend Integration Phase 4E.1: this pilot combination now routes to the catalog flow, which fails with CatalogCandidateNotPublishedException because TEN_K__4D__INTERMEDIATE v10 has status DRAFT, not PLAN_TEMPLATE_NOT_FOUND (the legacy SQL flow's error) -- it never reaches PlaceholderPlanGenerationEngine at all.
        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public void PlanServices_ConstructorDependencies_DoNotIncludeAnyRuntimeConditionResolverType()
    {
        // Structural regression guard: if a future change accidentally injects
        // IRuntimeConditionResolver/IRuntimeConditionResolutionService/etc.
        // into PlanServices without an explicit, documented Phase 4D decision,
        // this test fails loudly instead of the wiring happening silently.
        var constructorParamTypes = typeof(PlanServices)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IRuntimeConditionResolutionService));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IGoalFeasibilityResolver));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IPaceSourceResolver));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(ITimeAdequacyResolver));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(ICoreEntryReadinessResolver));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IPlanModeResolver));
    }

    [Fact]
    public void PlaceholderPlanGenerationEngine_ConstructorDependencies_DoNotIncludeAnyRuntimeConditionResolverType()
    {
        var constructorParamTypes = typeof(PlaceholderPlanGenerationEngine)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IRuntimeConditionResolutionService));
    }
}
