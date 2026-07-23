using System;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Validation;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

public sealed class GenerateRacePlanPreviewRequestValidatorTests
{
    private static GenerateRacePlanPreviewRequest ValidRequest(
        GoalDistance goalDistance = GoalDistance.TenK,
        int? targetFinishTimeSeconds = 3480,
        TargetFinishTimeSource targetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
        Weekday[]? preferredDays = null,
        Weekday longRunDay = Weekday.Sun,
        DateOnly? raceDate = null,
        DateOnly? startDate = null,
        int daysPerWeek = 4) => new()
    {
        GoalDistance = goalDistance,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = daysPerWeek,
        Unit = DistanceUnit.Km,
        StartDate = startDate ?? new DateOnly(2026, 7, 20),
        PreferredDays = preferredDays ?? new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
        LongRunDay = longRunDay,
        RaceDate = raceDate ?? new DateOnly(2026, 10, 12),
        TargetFinishTimeSeconds = targetFinishTimeSeconds!.Value,
        TargetFinishTimeSource = targetFinishTimeSource,
    };

    [Fact]
    public void ValidProductAverageRequest_TenK3480_Passes() =>
        GenerateRacePlanPreviewRequestValidator.Validate(ValidRequest());

    [Fact]
    public void ValidUserDefinedRequest_Passes() =>
        GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(targetFinishTimeSeconds: 3600, targetFinishTimeSource: TargetFinishTimeSource.UserDefined));

    // ── source/value consistency matrix ──────────────────────────────────

    [Fact]
    public void ProductAverage_TenK_3600_Mismatch_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(goalDistance: GoalDistance.TenK, targetFinishTimeSeconds: 3600, targetFinishTimeSource: TargetFinishTimeSource.ProductAverage)));

    [Fact]
    public void ProductAverage_FiveK_3480_Mismatch_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(goalDistance: GoalDistance.FiveK, targetFinishTimeSeconds: 3480, targetFinishTimeSource: TargetFinishTimeSource.ProductAverage)));

    [Fact]
    public void ProductAverage_FiveK_1680_Matches_Passes() =>
        GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(goalDistance: GoalDistance.FiveK, targetFinishTimeSeconds: 1680, targetFinishTimeSource: TargetFinishTimeSource.ProductAverage));

    [Fact]
    public void ProductAverage_HalfMarathon_7500_Matches_Passes() =>
        GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(goalDistance: GoalDistance.HalfMarathon, targetFinishTimeSeconds: 7500, targetFinishTimeSource: TargetFinishTimeSource.ProductAverage));

    [Fact]
    public void ProductAverage_Marathon_15660_Matches_Passes() =>
        GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(goalDistance: GoalDistance.Marathon, targetFinishTimeSeconds: 15660, targetFinishTimeSource: TargetFinishTimeSource.ProductAverage));

    [Fact]
    public void ProductAverage_Custom_NoCanonicalValue_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(goalDistance: GoalDistance.Custom, targetFinishTimeSeconds: 3480, targetFinishTimeSource: TargetFinishTimeSource.ProductAverage)));

    [Fact]
    public void UserDefined_ArbitraryValue_NeverCoercedOrRejectedByCanonicalCheck() =>
        GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(goalDistance: GoalDistance.TenK, targetFinishTimeSeconds: 9999, targetFinishTimeSource: TargetFinishTimeSource.UserDefined));

    // ── required-field / shape matrix (unchanged rules, new type) ────────

    [Fact]
    public void ZeroTargetFinishTimeSeconds_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(targetFinishTimeSeconds: 0, targetFinishTimeSource: TargetFinishTimeSource.UserDefined)));

    [Fact]
    public void NegativeTargetFinishTimeSeconds_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(targetFinishTimeSeconds: -1, targetFinishTimeSource: TargetFinishTimeSource.UserDefined)));

    [Fact]
    public void MissingStartDate_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(ValidRequest(startDate: default(DateOnly))));

    [Fact]
    public void MissingRaceDate_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(ValidRequest(raceDate: default(DateOnly))));

    [Fact]
    public void EmptyPreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(ValidRequest(preferredDays: Array.Empty<Weekday>())));

    [Fact]
    public void DuplicatePreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(preferredDays: new[] { Weekday.Mon, Weekday.Mon, Weekday.Fri, Weekday.Sun })));

    [Fact]
    public void PreferredDaysCountMismatch_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(preferredDays: new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri }, daysPerWeek: 4)));

    [Fact]
    public void LongRunDayNotInPreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(
            ValidRequest(preferredDays: new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sat }, longRunDay: Weekday.Sun)));

    [Fact]
    public void RecentRace_FutureDate_Throws()
    {
        var request = ValidRequest();
        request.RecentRace = new RecentRaceInput
        {
            Distance = GoalDistance.TenK,
            FinishTimeSeconds = 3510,
            RaceDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1),
        };
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(request));
    }

    [Fact]
    public void RecentRace_NonPositiveFinishTime_Throws()
    {
        var request = ValidRequest();
        request.RecentRace = new RecentRaceInput
        {
            Distance = GoalDistance.TenK,
            FinishTimeSeconds = 0,
            RaceDate = new DateOnly(2026, 6, 1),
        };
        Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(request));
    }

    [Fact]
    public void ExplicitZero_RecentWeeklyVolumeKm_Passes()
    {
        var request = ValidRequest();
        request.RecentWeeklyVolumeKm = 0;
        GenerateRacePlanPreviewRequestValidator.Validate(request);
        Assert.Equal(0, request.RecentWeeklyVolumeKm);
    }

    [Fact]
    public void ExplicitZero_RecentLongestRunKm_Passes()
    {
        var request = ValidRequest();
        request.RecentLongestRunKm = 0;
        GenerateRacePlanPreviewRequestValidator.Validate(request);
        Assert.Equal(0, request.RecentLongestRunKm);
    }

    [Fact]
    public void ExplicitZero_RecentRunsPerWeek_Passes()
    {
        var request = ValidRequest();
        request.RecentRunsPerWeek = 0;
        GenerateRacePlanPreviewRequestValidator.Validate(request);
        Assert.Equal(0, request.RecentRunsPerWeek);
    }

    [Fact]
    public void ExplicitZero_AllThreeReadinessFieldsTogether_Passes()
    {
        var request = ValidRequest();
        request.RecentLongestRunKm = 0;
        request.RecentWeeklyVolumeKm = 0;
        request.RecentRunsPerWeek = 0;
        GenerateRacePlanPreviewRequestValidator.Validate(request);
        Assert.Equal(0, request.RecentLongestRunKm);
        Assert.Equal(0, request.RecentWeeklyVolumeKm);
        Assert.Equal(0, request.RecentRunsPerWeek);
    }

    [Fact]
    public void NegativeRecentLongestRunKm_Throws()
    {
        var request = ValidRequest();
        request.RecentLongestRunKm = -1;
        var ex = Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(request));
        Assert.Contains("RecentLongestRunKm", ex.Message);
    }

    [Fact]
    public void NegativeRecentWeeklyVolumeKm_Throws()
    {
        var request = ValidRequest();
        request.RecentWeeklyVolumeKm = -0.1;
        var ex = Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(request));
        Assert.Contains("RecentWeeklyVolumeKm", ex.Message);
    }

    [Fact]
    public void NegativeRecentRunsPerWeek_Throws()
    {
        var request = ValidRequest();
        request.RecentRunsPerWeek = -1;
        var ex = Assert.Throws<ArgumentException>(() => GenerateRacePlanPreviewRequestValidator.Validate(request));
        Assert.Contains("RecentRunsPerWeek", ex.Message);
    }

    [Fact]
    public void NullReadinessFields_LeftNull_Passes()
    {
        var request = ValidRequest();
        Assert.Null(request.RecentWeeklyVolumeKm);
        Assert.Null(request.RecentLongestRunKm);
        Assert.Null(request.RecentRunsPerWeek);
        GenerateRacePlanPreviewRequestValidator.Validate(request);
    }
}
