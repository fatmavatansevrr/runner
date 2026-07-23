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
/// Backend Integration Phase 4D.2 regression guard: now that a CONCRETE
/// PaceSourceResolver exists, re-prove it is not invoked anywhere in live
/// generation. Existing generate-preview/confirm behavior, including Phase
/// 0's no-silent-fallback guarantee, must remain byte-for-byte unchanged.
/// </summary>
public sealed class PaceSourceResolverNotWiredToGenerationTests
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
    public async Task ExistingSupportedSeededTemplate_PreviewWithPaceEvidence_StillSucceeds_ResolverNeverInvoked()
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
            StartDate = new DateOnly(2026, 7, 20),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri },
            RecentRace = new RecentRaceInput { Distance = GoalDistance.FiveK, FinishTimeSeconds = 1450, RaceDate = new DateOnly(2026, 6, 15) },
        });

        Assert.Equal("habit_5k_beginner_3day_km_v1", response.TemplateId);
        Assert.False(response.FallbackUsed);
        Assert.Null(response.FallbackReason);
    }

    [Fact]
    public async Task TenKIntermediate4Day_WithPaceEvidence_StillReturnsPlanTemplateNotAvailable()
    {
        await using var context = NewSeededContext();
        var service = NewServices(context);

        var ex = await Assert.ThrowsAsync<RunningApp.Application.Exceptions.PlanTemplateNotAvailableException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            RaceDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(84),
            TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Sat },
            LongRunDay = Weekday.Sat,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.FiveK, FinishTimeSeconds = 1450, RaceDate = new DateOnly(2026, 6, 15) },
        }));

        Assert.Contains("goal_distance=TenK", ex.Message); // Phase 4F.8.2: current repository stays non-live (v10 DRAFT + activation disabled), so this reaches the exact legacy template boundary.
        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public void PlanServices_ConstructorDependencies_DoNotIncludePaceSourceResolver()
    {
        var constructorParamTypes = typeof(PlanServices)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(PaceSourceResolver));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IPaceSourceResolver));
    }

    [Fact]
    public void PlaceholderPlanGenerationEngine_ConstructorDependencies_DoNotIncludePaceSourceResolver()
    {
        var constructorParamTypes = typeof(PlaceholderPlanGenerationEngine)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(PaceSourceResolver));
        Assert.DoesNotContain(constructorParamTypes, t => t == typeof(IPaceSourceResolver));
    }

    [Fact]
    public void GeneratePreviewResponse_HasNoPaceSourceResolutionProperty()
    {
        var responseProperties = typeof(GeneratePreviewResponse).GetProperties().Select(p => p.PropertyType).ToList();

        Assert.DoesNotContain(responseProperties, t => t == typeof(RuntimeConditionResolutionResult));
        Assert.DoesNotContain(responseProperties, t => t == typeof(ResolverDecisionTrace));
    }
}
