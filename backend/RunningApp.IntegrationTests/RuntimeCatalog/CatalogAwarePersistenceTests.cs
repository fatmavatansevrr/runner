using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog;

/// <summary>
/// Backend Integration Phase 3 — catalog-aware persistence contract. Proves
/// the new additive/nullable fields on TrainingPlan/TrainingWeek/TrainingDay
/// can round-trip through EF Core (InMemory, no live Postgres required),
/// that they default to null (legacy/seeded-template flow unaffected), and
/// that RequestedTargetDistanceKm is never conflated with
/// CanonicalDistanceFamily. Never generates a real personalized plan.
/// </summary>
public sealed class CatalogAwarePersistenceTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static TrainingPlan MinimalPlan() => new()
    {
        Id = Guid.NewGuid(),
        InternalUserId = Guid.NewGuid(),
        Status = TrainingPlanStatus.Active,
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        StartedAt = DateTime.UtcNow,
        EstimatedEndDate = DateTime.UtcNow.AddDays(84),
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task ExistingSqlTemplatePlan_StoresAllCatalogFieldsAsNull()
    {
        await using var context = NewContext();
        var plan = MinimalPlan();
        plan.TemplateId = "habit_5k_beginner_3day_km_v1"; // legacy SQL-template flow

        context.TrainingPlans.Add(plan);
        await context.SaveChangesAsync();

        var reloaded = await context.TrainingPlans.SingleAsync(p => p.Id == plan.Id);
        Assert.Null(reloaded.CatalogCandidateKey);
        Assert.Null(reloaded.CatalogCandidateVersion);
        Assert.Null(reloaded.CatalogCandidateStatusAtGenerationTime);
        Assert.Null(reloaded.CatalogTemplateKey);
        Assert.Null(reloaded.CatalogTemplateVersion);
        Assert.Null(reloaded.CatalogLayoutKey);
        Assert.Null(reloaded.CatalogLayoutVersion);
        Assert.Null(reloaded.CatalogLevelModifierKey);
        Assert.Null(reloaded.CatalogLevelModifierVersion);
        Assert.Null(reloaded.CatalogWorkoutProgressionKey);
        Assert.Null(reloaded.CatalogWorkoutProgressionVersion);
        Assert.Null(reloaded.CatalogProgressionModifierKey);
        Assert.Null(reloaded.CatalogProgressionModifierVersion);
        Assert.Null(reloaded.CatalogRulePackKey);
        Assert.Null(reloaded.CatalogRulePackVersion);
        Assert.Null(reloaded.CatalogPeakVolumeBandPolicyKey);
        Assert.Null(reloaded.CatalogPeakVolumeBandPolicyVersion);
        Assert.Null(reloaded.CatalogRuntimeConditionRegistryKey);
        Assert.Null(reloaded.CatalogRuntimeConditionRegistryVersion);
        Assert.Null(reloaded.CanonicalDistanceFamily);
        Assert.Null(reloaded.RequestedTargetDistanceKm);
        Assert.Equal("habit_5k_beginner_3day_km_v1", reloaded.TemplateId); // legacy field untouched
    }

    [Fact]
    public async Task TrainingPlan_CanStoreFullCatalogProvenance()
    {
        await using var context = NewContext();
        var plan = MinimalPlan();
        plan.CatalogCandidateKey = "TEN_K__4D__INTERMEDIATE";
        plan.CatalogCandidateVersion = 10;
        plan.CatalogCandidateStatusAtGenerationTime = "DRAFT";
        plan.CatalogTemplateKey = "TEN_K_MASTER";
        plan.CatalogTemplateVersion = 6;
        plan.CatalogLayoutKey = "RUN_LAYOUT_4D";
        plan.CatalogLayoutVersion = 2;
        plan.CatalogLevelModifierKey = "INTERMEDIATE_MODIFIER";
        plan.CatalogLevelModifierVersion = 6;
        plan.CatalogWorkoutProgressionKey = "TEN_K_WORKOUT_PROGRESSION_V1";
        plan.CatalogWorkoutProgressionVersion = 5;
        plan.CatalogProgressionModifierKey = "INTERMEDIATE_PROGRESSION_MODIFIER_V1";
        plan.CatalogProgressionModifierVersion = 2;
        plan.CatalogRulePackKey = "APPSEL_RACE_PLAN_V1";
        plan.CatalogRulePackVersion = 4;
        plan.CatalogPeakVolumeBandPolicyKey = "PEAK_VOLUME_BANDS_V1";
        plan.CatalogPeakVolumeBandPolicyVersion = 3;
        plan.CatalogRuntimeConditionRegistryKey = "RUNTIME_CONDITION_VALUES_V1";
        plan.CatalogRuntimeConditionRegistryVersion = 2;
        plan.CanonicalDistanceFamily = "TEN_K";
        plan.RequestedTargetDistanceKm = 8.0;

        context.TrainingPlans.Add(plan);
        await context.SaveChangesAsync();

        var reloaded = await context.TrainingPlans.SingleAsync(p => p.Id == plan.Id);
        Assert.Equal("TEN_K__4D__INTERMEDIATE", reloaded.CatalogCandidateKey);
        Assert.Equal(10, reloaded.CatalogCandidateVersion);
        Assert.Equal("DRAFT", reloaded.CatalogCandidateStatusAtGenerationTime);
        Assert.Equal("TEN_K_MASTER", reloaded.CatalogTemplateKey);
        Assert.Equal(6, reloaded.CatalogTemplateVersion);
        Assert.Equal("RUN_LAYOUT_4D", reloaded.CatalogLayoutKey);
        Assert.Equal(2, reloaded.CatalogLayoutVersion);
        Assert.Equal("INTERMEDIATE_MODIFIER", reloaded.CatalogLevelModifierKey);
        Assert.Equal(6, reloaded.CatalogLevelModifierVersion);
        Assert.Equal("TEN_K_WORKOUT_PROGRESSION_V1", reloaded.CatalogWorkoutProgressionKey);
        Assert.Equal(5, reloaded.CatalogWorkoutProgressionVersion);
        Assert.Equal("INTERMEDIATE_PROGRESSION_MODIFIER_V1", reloaded.CatalogProgressionModifierKey);
        Assert.Equal(2, reloaded.CatalogProgressionModifierVersion);
        Assert.Equal("APPSEL_RACE_PLAN_V1", reloaded.CatalogRulePackKey);
        Assert.Equal(4, reloaded.CatalogRulePackVersion);
        Assert.Equal("PEAK_VOLUME_BANDS_V1", reloaded.CatalogPeakVolumeBandPolicyKey);
        Assert.Equal(3, reloaded.CatalogPeakVolumeBandPolicyVersion);
        Assert.Equal("RUNTIME_CONDITION_VALUES_V1", reloaded.CatalogRuntimeConditionRegistryKey);
        Assert.Equal(2, reloaded.CatalogRuntimeConditionRegistryVersion);
        Assert.Equal("TEN_K", reloaded.CanonicalDistanceFamily);
        Assert.Equal(8.0, reloaded.RequestedTargetDistanceKm);
    }

    [Fact]
    public async Task RequestedTargetDistanceKm_EightCoexistsWith_CanonicalDistanceFamilyTenK_NeverOverwritten()
    {
        await using var context = NewContext();
        var plan = MinimalPlan();
        plan.CanonicalDistanceFamily = "TEN_K";
        plan.RequestedTargetDistanceKm = 8.0;

        context.TrainingPlans.Add(plan);
        await context.SaveChangesAsync();

        var reloaded = await context.TrainingPlans.SingleAsync(p => p.Id == plan.Id);
        Assert.Equal("TEN_K", reloaded.CanonicalDistanceFamily);
        Assert.Equal(8.0, reloaded.RequestedTargetDistanceKm);
        Assert.NotEqual(10.0, reloaded.RequestedTargetDistanceKm); // never overwritten by the family-representative distance

        // Existing GoalDistanceKm (fixed family constant) remains a SEPARATE, unrelated field.
        Assert.Null(reloaded.GoalDistanceKm);
    }

    [Theory]
    [InlineData(15.0, "HALF_MARATHON", 21.1)]
    [InlineData(30.0, "MARATHON", 42.2)]
    public void RequestedTargetDistanceKm_PreservesUserFacingValue_ForOtherCustomDistances(double requestedKm, string family, double familyUpperBoundKm)
    {
        var plan = MinimalPlan();
        plan.CanonicalDistanceFamily = family;
        plan.RequestedTargetDistanceKm = requestedKm;

        Assert.Equal(requestedKm, plan.RequestedTargetDistanceKm);
        Assert.NotEqual(familyUpperBoundKm, plan.RequestedTargetDistanceKm); // never silently coerced to the family's representative distance
        Assert.Equal(family, plan.CanonicalDistanceFamily);
    }

    [Fact]
    public async Task TrainingWeek_CanStoreCatalogPhaseKey_Foundation()
    {
        await using var context = NewContext();
        var plan = MinimalPlan();
        context.TrainingPlans.Add(plan);
        var week = new TrainingWeek { Id = Guid.NewGuid(), PlanId = plan.Id, WeekNumber = 1, StartDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, CatalogPhaseKey = "FOUNDATION" };
        context.TrainingWeeks.Add(week);
        await context.SaveChangesAsync();

        var reloaded = await context.TrainingWeeks.SingleAsync(w => w.Id == week.Id);
        Assert.Equal("FOUNDATION", reloaded.CatalogPhaseKey);
        Assert.Equal(TrainingWeekType.Build, reloaded.WeekType); // default enum value still usable
    }

    [Fact]
    public async Task TrainingWeek_CanStoreCatalogPhaseKey_RaceSpecific()
    {
        await using var context = NewContext();
        var plan = MinimalPlan();
        context.TrainingPlans.Add(plan);
        var week = new TrainingWeek { Id = Guid.NewGuid(), PlanId = plan.Id, WeekNumber = 8, StartDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, CatalogPhaseKey = "RACE_SPECIFIC" };
        context.TrainingWeeks.Add(week);
        await context.SaveChangesAsync();

        var reloaded = await context.TrainingWeeks.SingleAsync(w => w.Id == week.Id);
        Assert.Equal("RACE_SPECIFIC", reloaded.CatalogPhaseKey);
    }

    [Fact]
    public async Task TrainingWeek_NullCatalogPhaseKey_DoesNotBreakLegacyFlow()
    {
        await using var context = NewContext();
        var plan = MinimalPlan();
        context.TrainingPlans.Add(plan);
        var week = new TrainingWeek { Id = Guid.NewGuid(), PlanId = plan.Id, WeekNumber = 1, WeekType = TrainingWeekType.Taper, StartDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
        context.TrainingWeeks.Add(week);
        await context.SaveChangesAsync();

        var reloaded = await context.TrainingWeeks.SingleAsync(w => w.Id == week.Id);
        Assert.Null(reloaded.CatalogPhaseKey);
        Assert.Equal(TrainingWeekType.Taper, reloaded.WeekType);
    }

    private static (TrainingPlan Plan, TrainingWeek Week) MinimalPlanAndWeek()
    {
        var plan = MinimalPlan();
        var week = new TrainingWeek { Id = Guid.NewGuid(), PlanId = plan.Id, WeekNumber = 1, StartDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
        return (plan, week);
    }

    [Theory]
    [InlineData("KEY_SESSION")]
    [InlineData("EASY_SUPPORT")]
    [InlineData("LONG_RUN")]
    public async Task TrainingDay_CanStoreCatalogSlotRole(string slotRole)
    {
        await using var context = NewContext();
        var (plan, week) = MinimalPlanAndWeek();
        context.TrainingPlans.Add(plan);
        context.TrainingWeeks.Add(week);

        var day = new TrainingDay
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            WeekId = week.Id,
            Date = DateTime.UtcNow,
            DayType = TrainingDayType.Easy,
            CatalogSlotRole = slotRole,
        };
        context.TrainingDays.Add(day);
        await context.SaveChangesAsync();

        var reloaded = await context.TrainingDays.SingleAsync(d => d.Id == day.Id);
        Assert.Equal(slotRole, reloaded.CatalogSlotRole);
    }

    [Fact]
    public async Task TrainingDay_CanStoreGoalPaceTenKWorkoutKeyAndVersion_Exactly()
    {
        await using var context = NewContext();
        var (plan, week) = MinimalPlanAndWeek();
        context.TrainingPlans.Add(plan);
        context.TrainingWeeks.Add(week);

        var day = new TrainingDay
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            WeekId = week.Id,
            Date = DateTime.UtcNow,
            DayType = TrainingDayType.Tempo,
            CatalogWorkoutKey = "GOAL_PACE_TEN_K",
            CatalogWorkoutVersion = 2,
            CatalogStageKey = "GOAL_PACE_REHEARSAL",
            CatalogWorkoutFamily = "QUALITY",
            CatalogIntensityKey = "GOAL_PACE",
        };
        context.TrainingDays.Add(day);
        await context.SaveChangesAsync();

        var reloaded = await context.TrainingDays.SingleAsync(d => d.Id == day.Id);
        Assert.Equal("GOAL_PACE_TEN_K", reloaded.CatalogWorkoutKey);
        Assert.Equal(2, reloaded.CatalogWorkoutVersion);
        Assert.Equal("GOAL_PACE_REHEARSAL", reloaded.CatalogStageKey);
        Assert.Equal("QUALITY", reloaded.CatalogWorkoutFamily);
        Assert.Equal("GOAL_PACE", reloaded.CatalogIntensityKey);
    }

    [Fact]
    public async Task TrainingDay_NullCatalogFields_DoesNotBreakLegacyFlow()
    {
        await using var context = NewContext();
        var (plan, week) = MinimalPlanAndWeek();
        context.TrainingPlans.Add(plan);
        context.TrainingWeeks.Add(week);

        var day = new TrainingDay
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            WeekId = week.Id,
            Date = DateTime.UtcNow,
            DayType = TrainingDayType.LongRun,
            Intensity = "z2",
        };
        context.TrainingDays.Add(day);
        await context.SaveChangesAsync();

        var reloaded = await context.TrainingDays.SingleAsync(d => d.Id == day.Id);
        Assert.Equal(TrainingDayType.LongRun, reloaded.DayType);
        Assert.Equal("z2", reloaded.Intensity);
        Assert.Null(reloaded.CatalogWorkoutKey);
        Assert.Null(reloaded.CatalogWorkoutVersion);
        Assert.Null(reloaded.CatalogStageKey);
        Assert.Null(reloaded.CatalogSlotRole);
        Assert.Null(reloaded.CatalogWorkoutFamily);
        Assert.Null(reloaded.CatalogIntensityKey);
    }
}
