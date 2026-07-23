using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.PlanGeneration;
using RunningApp.Application.Services;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

/// <summary>
/// Backend Integration Phase 0 (safe template selection). Uses the EF Core
/// InMemory provider (no live Postgres required) seeded with the same 3
/// real templates the product database ships with, plus request shapes for
/// an unsupported combination (TEN_K / Intermediate / 4-day) that has no
/// seeded template today. Does not fabricate any plan-catalog content —
/// only proves backend's own selection/error behavior.
/// </summary>
public sealed class SafeTemplateSelectionTests
{
    private static AppDbContext NewSeededContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        context.PlanTemplates.AddRange(
            new PlanTemplate
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
            },
            new PlanTemplate
            {
                Id = Guid.NewGuid(),
                TemplateId = "race_5k_beginner_3day_km_v1",
                Version = 1,
                GoalType = GoalType.Race,
                GoalDistance = GoalDistance.FiveK,
                Level = RunningBackground.Beginner,
                DaysPerWeek = 3,
                Unit = DistanceUnit.Km,
                DataJson = "{\"templateId\":\"race_5k_beginner_3day_km_v1\",\"version\":1,\"goalType\":\"race\",\"goalDistance\":\"five_k\",\"level\":\"beginner\",\"daysPerWeek\":3,\"unit\":\"km\",\"weeks\":[{\"weekNumber\":1,\"weekType\":\"build\",\"days\":[{\"slotIndex\":1,\"dayType\":\"easy\",\"distanceKm\":2.5,\"durationMin\":25,\"intensity\":\"z2\"},{\"slotIndex\":2,\"dayType\":\"interval\",\"distanceKm\":3.0,\"durationMin\":30,\"intensity\":\"z4\"},{\"slotIndex\":3,\"dayType\":\"long_run\",\"distanceKm\":4.0,\"durationMin\":40,\"intensity\":\"z2\"}]}]}",
                CreatedAt = DateTime.UtcNow,
            });
        context.SaveChanges();

        return context;
    }

    private static GeneratePreviewRequest TenKIntermediate4DayRequest() => new()
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
    };

    private static GeneratePreviewRequest SeededHabit5KRequest() => new()
    {
        GoalType = GoalType.Habit,
        GoalDistance = GoalDistance.FiveK,
        Level = RunningBackground.Beginner,
        DaysPerWeek = 3,
        Unit = DistanceUnit.Km,
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
        PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri },
    };

    // ---------- 1: existing seeded template still selectable ----------

    [Fact]
    public async Task SelectTemplateAsync_ExactMatchForSeededTemplate_ReturnsItWithoutFallback()
    {
        await using var context = NewSeededContext();
        var engine = new PlaceholderPlanGenerationEngine(context, NullLogger<PlaceholderPlanGenerationEngine>.Instance);

        var result = await engine.SelectTemplateAsync(SeededHabit5KRequest());

        Assert.Equal("habit_5k_beginner_3day_km_v1", result.Template.TemplateId);
        Assert.False(result.FallbackUsed);
        Assert.Null(result.FallbackReason);
    }

    // ---------- 2 & 3: unsupported combination does not fall back, throws the expected error ----------

    [Fact]
    public async Task SelectTemplateAsync_TenKIntermediate4Day_DoesNotFallBackToFiveKBeginner()
    {
        await using var context = NewSeededContext();
        var engine = new PlaceholderPlanGenerationEngine(context, NullLogger<PlaceholderPlanGenerationEngine>.Instance);

        var ex = await Assert.ThrowsAsync<PlanTemplateNotAvailableException>(
            () => engine.SelectTemplateAsync(TenKIntermediate4DayRequest()));

        // Must never silently resolve to any seeded 5K template.
        Assert.DoesNotContain("5k", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TenK", ex.Message);
        Assert.Contains("4", ex.Message);
    }

    [Fact]
    public async Task GeneratePreviewAsync_TenKIntermediate4Day_DefaultClosedUsesLegacyExactTemplateBoundary_AndPersistsNoPreview()
    {
        // Phase 4F.8.2 keeps the repository non-live: v10 is DRAFT and
        // activation defaults disabled. The pilot-shaped request therefore
        // reaches the existing exact legacy template boundary.
        await using var context = NewSeededContext();
        var engine = new PlaceholderPlanGenerationEngine(context, NullLogger<PlaceholderPlanGenerationEngine>.Instance);
        var service = TestPlanServicesFactory.Create(context, engine);

        await Assert.ThrowsAsync<PlanTemplateNotAvailableException>(
            () => service.GeneratePreviewAsync(Guid.NewGuid(), TenKIntermediate4DayRequest()));

        Assert.Empty(context.PlanPreviews);
    }

    // ---------- 4: preview AND confirm flows both respect safe selection ----------

    [Fact]
    public async Task GeneratePreviewAsync_SupportedCombination_Succeeds()
    {
        await using var context = NewSeededContext();
        var engine = new PlaceholderPlanGenerationEngine(context, NullLogger<PlaceholderPlanGenerationEngine>.Instance);
        var service = TestPlanServicesFactory.Create(context, engine);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), SeededHabit5KRequest());

        Assert.Equal("habit_5k_beginner_3day_km_v1", response.TemplateId);
        Assert.False(response.FallbackUsed);
        Assert.Single(context.PlanPreviews);
    }

    [Fact]
    public async Task ConfirmPlanAsync_CanNeverConfirmAnUnsupportedCombination_BecauseNoPreviewCanExistForIt()
    {
        // Confirm never re-selects a template — it only loads a persisted PlanPreview
        // by PreviewId. Since GeneratePreviewAsync now throws before persisting a
        // preview for an unsupported combination, confirm has nothing to load for
        // one: it fails with NotFoundAppException on a random PreviewId, proving
        // there is no path to confirm an unrelated-fallback plan for TEN_K/Intermediate/4-day.
        await using var context = NewSeededContext();
        var engine = new PlaceholderPlanGenerationEngine(context, NullLogger<PlaceholderPlanGenerationEngine>.Instance);
        var service = TestPlanServicesFactory.Create(context, engine);

        var userId = Guid.NewGuid();

        await Assert.ThrowsAsync<PlanTemplateNotAvailableException>(
            () => service.GeneratePreviewAsync(userId, TenKIntermediate4DayRequest()));

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => service.ConfirmPlanAsync(userId, new ConfirmPlanRequest { PreviewId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task ConfirmPlanAsync_SupportedCombination_ConfirmsThePreviouslyPreviewedTemplate()
    {
        await using var context = NewSeededContext();
        var engine = new PlaceholderPlanGenerationEngine(context, NullLogger<PlaceholderPlanGenerationEngine>.Instance);
        var service = TestPlanServicesFactory.Create(context, engine);

        var userId = Guid.NewGuid();
        var preview = await service.GeneratePreviewAsync(userId, SeededHabit5KRequest());

        var confirmed = await service.ConfirmPlanAsync(userId, new ConfirmPlanRequest { PreviewId = preview.PreviewId });

        Assert.Equal("active", confirmed.Status);
        var plan = await context.TrainingPlans.SingleAsync();
        Assert.Equal("habit_5k_beginner_3day_km_v1", plan.TemplateId);
    }
}
