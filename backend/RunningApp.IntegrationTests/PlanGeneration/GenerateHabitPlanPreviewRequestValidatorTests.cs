using System;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Validation;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

public sealed class GenerateHabitPlanPreviewRequestValidatorTests
{
    private static GenerateHabitPlanPreviewRequest ValidRequest(
        Weekday[]? preferredDays = null,
        Weekday? longRunDay = null,
        DateOnly? startDate = null,
        int daysPerWeek = 3) => new()
    {
        GoalDistance = GoalDistance.FiveK,
        Level = RunningBackground.Beginner,
        DaysPerWeek = daysPerWeek,
        Unit = DistanceUnit.Km,
        StartDate = startDate ?? new DateOnly(2026, 7, 20),
        PreferredDays = preferredDays ?? new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat },
        LongRunDay = longRunDay,
    };

    [Fact]
    public void ValidRequest_Passes() => GenerateHabitPlanPreviewRequestValidator.Validate(ValidRequest());

    [Fact]
    public void NullLongRunDay_Passes() => GenerateHabitPlanPreviewRequestValidator.Validate(ValidRequest(longRunDay: null));

    [Fact]
    public void LongRunDayProvidedAndInPreferredDays_Passes() =>
        GenerateHabitPlanPreviewRequestValidator.Validate(ValidRequest(longRunDay: Weekday.Sat));

    [Fact]
    public void LongRunDayNotInPreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateHabitPlanPreviewRequestValidator.Validate(
            ValidRequest(preferredDays: new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat }, longRunDay: Weekday.Sun)));

    [Fact]
    public void MissingStartDate_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateHabitPlanPreviewRequestValidator.Validate(ValidRequest(startDate: default(DateOnly))));

    [Fact]
    public void EmptyPreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateHabitPlanPreviewRequestValidator.Validate(ValidRequest(preferredDays: Array.Empty<Weekday>())));

    [Fact]
    public void DuplicatePreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateHabitPlanPreviewRequestValidator.Validate(
            ValidRequest(preferredDays: new[] { Weekday.Mon, Weekday.Mon, Weekday.Sat })));

    [Fact]
    public void PreferredDaysCountMismatch_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateHabitPlanPreviewRequestValidator.Validate(
            ValidRequest(preferredDays: new[] { Weekday.Mon, Weekday.Wed }, daysPerWeek: 3)));
}
