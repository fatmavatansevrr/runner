using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 4F.9.2 relational-validation correction — real-PostgreSQL coverage
/// for the widened <c>CK_TrainingPlans_LongRunDay</c> check constraint
/// (migration <c>FixLongRunDayCheckConstraintFullDayNames</c>). Unrelated to
/// Phase 4F catalog behavior: this is a pre-existing legacy schema/app-layer
/// mismatch (the app writes full weekday names; the original constraint only
/// allowed 3-letter abbreviations) that was only exposed once a real
/// PostgreSQL connection became available for relational testing.
/// </summary>
public sealed class LongRunDayCheckConstraintTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LongRunDayCheckConstraintTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static TrainingPlan MinimalPlan(string? longRunDay) => new()
    {
        Id = Guid.NewGuid(),
        InternalUserId = null, // avoid FK_TrainingPlans_Users_InternalUserId; irrelevant to this constraint
        Status = TrainingPlanStatus.Cancelled, // avoid colliding with the one-active-plan-per-user invariant
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartedAt = DateTime.UtcNow,
        EstimatedEndDate = DateTime.UtcNow.AddDays(56),
        CreatedAt = DateTime.UtcNow,
        LongRunDay = longRunDay,
    };

    [Theory]
    [InlineData("Monday")]
    [InlineData("Tuesday")]
    [InlineData("Wednesday")]
    [InlineData("Thursday")]
    [InlineData("Friday")]
    [InlineData("Saturday")]
    [InlineData("Sunday")]
    public async Task FullWeekdayName_SatisfiesConstraint(string day)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = MinimalPlan(day);
        ctx.TrainingPlans.Add(plan);

        await ctx.SaveChangesAsync();

        var reloaded = await ctx.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == plan.Id);
        Assert.Equal(day, reloaded.LongRunDay);
    }

    [Theory]
    [InlineData("Mon")]
    [InlineData("Tue")]
    [InlineData("Wed")]
    [InlineData("Thu")]
    [InlineData("Fri")]
    [InlineData("Sat")]
    [InlineData("Sun")]
    public async Task LegacyThreeLetterAbbreviation_RemainsValidForBackwardCompatibility(string day)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = MinimalPlan(day);
        ctx.TrainingPlans.Add(plan);

        await ctx.SaveChangesAsync();

        var reloaded = await ctx.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == plan.Id);
        Assert.Equal(day, reloaded.LongRunDay);
    }

    [Fact]
    public async Task NullLongRunDay_RemainsValid()
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = MinimalPlan(null);
        ctx.TrainingPlans.Add(plan);

        await ctx.SaveChangesAsync();

        var reloaded = await ctx.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == plan.Id);
        Assert.Null(reloaded.LongRunDay);
    }

    [Theory]
    [InlineData("monday")]     // lowercase — RunningDay.Normalize's job, not the DB's; DB is case-sensitive
    [InlineData("Someday")]
    [InlineData("")]
    public async Task InvalidLongRunDay_StillViolatesConstraint(string invalidValue)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = MinimalPlan(invalidValue);
        ctx.TrainingPlans.Add(plan);

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task ConfirmedLegacyPlan_WithFullWeekdayLongRunDay_PersistsAndRoundTrips()
    {
        // End-to-end proof (real Postgres) that the exact scenario the
        // migration fixes — a legacy confirm writing a full weekday name —
        // persists cleanly through the real application write path, not just
        // a direct entity insert.
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = MinimalPlan("Saturday");
        plan.Status = TrainingPlanStatus.Active;
        ctx.TrainingPlans.Add(plan);
        await ctx.SaveChangesAsync();

        using var freshScope = _factory.Services.CreateScope();
        var freshCtx = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reloaded = await freshCtx.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == plan.Id);
        Assert.Equal("Saturday", reloaded.LongRunDay);
    }
}
