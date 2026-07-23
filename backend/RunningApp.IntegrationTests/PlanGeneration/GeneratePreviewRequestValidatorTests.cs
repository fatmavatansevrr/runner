using System;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Validation;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

/// <summary>
/// Pure validator unit tests for the generate-preview contract alignment:
/// preferred_days/start_date/long_run_day/target_finish_time_seconds/
/// readiness shape rules, and the explicit-zero-vs-null distinction.
/// </summary>
public sealed class GeneratePreviewRequestValidatorTests
{
    private static GeneratePreviewRequest RaceRequest(
        Weekday[]? preferredDays = null,
        Weekday? longRunDay = Weekday.Sun,
        int? targetFinishTimeSeconds = 3600,
        DateOnly? startDate = null) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = startDate ?? new DateOnly(2026, 7, 20),
        PreferredDays = preferredDays ?? new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
        LongRunDay = longRunDay,
        RaceDate = new DateOnly(2026, 10, 12),
        TargetFinishTimeSeconds = targetFinishTimeSeconds,
    };

    private static GeneratePreviewRequest HabitRequest(
        Weekday[]? preferredDays = null,
        Weekday? longRunDay = null,
        int? targetFinishTimeSeconds = null,
        DateOnly? startDate = null) => new()
    {
        GoalType = GoalType.Habit,
        GoalDistance = GoalDistance.FiveK,
        Level = RunningBackground.Beginner,
        DaysPerWeek = 3,
        Unit = DistanceUnit.Km,
        StartDate = startDate ?? new DateOnly(2026, 7, 20),
        PreferredDays = preferredDays ?? new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat },
        LongRunDay = longRunDay,
        TargetFinishTimeSeconds = targetFinishTimeSeconds,
    };

    // ── A. Race matrix ───────────────────────────────────────────────────

    [Fact]
    public void Race_ValidRequest_Passes() =>
        GeneratePreviewRequestValidator.Validate(RaceRequest());

    [Fact]
    public void Race_MissingPreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(RaceRequest(preferredDays: Array.Empty<Weekday>())));

    [Fact]
    public void Race_MissingStartDate_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(RaceRequest(startDate: default(DateOnly))));

    [Fact]
    public void Race_MissingLongRunDay_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(RaceRequest(longRunDay: null)));

    [Fact]
    public void Race_LongRunDayNotInPreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(
            RaceRequest(preferredDays: new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sat }, longRunDay: Weekday.Sun)));

    [Fact]
    public void Race_PreferredDaysCountMismatch_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(
            RaceRequest(preferredDays: new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri })));

    [Fact]
    public void Race_DuplicatePreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(
            RaceRequest(preferredDays: new[] { Weekday.Mon, Weekday.Mon, Weekday.Fri, Weekday.Sun })));

    [Fact]
    public void Race_NullTargetFinishTimeSeconds_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(RaceRequest(targetFinishTimeSeconds: null)));

    [Fact]
    public void Race_ZeroTargetFinishTimeSeconds_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(RaceRequest(targetFinishTimeSeconds: 0)));

    [Fact]
    public void Race_NegativeTargetFinishTimeSeconds_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(RaceRequest(targetFinishTimeSeconds: -1)));

    // ── B. Habit matrix ──────────────────────────────────────────────────

    [Fact]
    public void Habit_ValidRequest_Passes() =>
        GeneratePreviewRequestValidator.Validate(HabitRequest());

    [Fact]
    public void Habit_MissingPreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(HabitRequest(preferredDays: Array.Empty<Weekday>())));

    [Fact]
    public void Habit_MissingStartDate_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(HabitRequest(startDate: default(DateOnly))));

    [Fact]
    public void Habit_NullLongRunDay_Passes() =>
        GeneratePreviewRequestValidator.Validate(HabitRequest(longRunDay: null));

    [Fact]
    public void Habit_LongRunDayProvidedButNotInPreferredDays_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(
            HabitRequest(preferredDays: new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat }, longRunDay: Weekday.Sun)));

    [Fact]
    public void Habit_NullTargetFinishTimeSeconds_Passes() =>
        GeneratePreviewRequestValidator.Validate(HabitRequest(targetFinishTimeSeconds: null));

    [Fact]
    public void Habit_NonNullTargetFinishTimeSeconds_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(HabitRequest(targetFinishTimeSeconds: 1800)));

    [Fact]
    public void Habit_RaceDateSupplied_Throws()
    {
        var request = HabitRequest();
        request.RaceDate = new DateOnly(2026, 10, 12);
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(request));
    }

    [Fact]
    public void Habit_RaceNameSupplied_Throws()
    {
        var request = HabitRequest();
        request.RaceName = "Some Race";
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(request));
    }

    [Fact]
    public void Race_MissingRaceDate_Throws() =>
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            RaceDate = null,
            TargetFinishTimeSeconds = 3600,
        }));

    // ── C. Readiness: explicit zero vs missing/null ──────────────────────

    [Fact]
    public void ExplicitZero_RecentWeeklyVolumeKm_IsPreserved_AndPasses()
    {
        var request = HabitRequest();
        request.RecentWeeklyVolumeKm = 0;
        GeneratePreviewRequestValidator.Validate(request);
        Assert.Equal(0, request.RecentWeeklyVolumeKm);
    }

    [Fact]
    public void NullRecentWeeklyVolumeKm_IsLeftNull_AndPasses()
    {
        var request = HabitRequest();
        request.RecentWeeklyVolumeKm = null;
        GeneratePreviewRequestValidator.Validate(request);
        Assert.Null(request.RecentWeeklyVolumeKm);
    }

    [Fact]
    public void NegativeRecentWeeklyVolumeKm_Throws()
    {
        var request = HabitRequest();
        request.RecentWeeklyVolumeKm = -1;
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(request));
    }

    [Fact]
    public void ExplicitZero_RecentLongestRunKm_IsPreserved_AndPasses()
    {
        var request = HabitRequest();
        request.RecentLongestRunKm = 0;
        GeneratePreviewRequestValidator.Validate(request);
        Assert.Equal(0, request.RecentLongestRunKm);
    }

    [Fact]
    public void NegativeRecentLongestRunKm_Throws()
    {
        var request = HabitRequest();
        request.RecentLongestRunKm = -1;
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(request));
    }

    [Fact]
    public void ExplicitZero_RecentRunsPerWeek_IsPreserved_AndPasses()
    {
        var request = HabitRequest();
        request.RecentRunsPerWeek = 0;
        GeneratePreviewRequestValidator.Validate(request);
        Assert.Equal(0, request.RecentRunsPerWeek);
    }

    [Fact]
    public void NegativeRecentRunsPerWeek_Throws()
    {
        var request = HabitRequest();
        request.RecentRunsPerWeek = -1;
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(request));
    }

    [Fact]
    public void ExplicitZero_AllThreeReadinessFieldsTogether_Passes()
    {
        var request = HabitRequest();
        request.RecentLongestRunKm = 0;
        request.RecentWeeklyVolumeKm = 0;
        request.RecentRunsPerWeek = 0;
        GeneratePreviewRequestValidator.Validate(request);
        Assert.Equal(0, request.RecentLongestRunKm);
        Assert.Equal(0, request.RecentWeeklyVolumeKm);
        Assert.Equal(0, request.RecentRunsPerWeek);
    }

    [Fact]
    public void MissingReadinessFields_AllNull_Passes()
    {
        var request = HabitRequest();
        request.RecentLongestRunKm = null;
        request.RecentWeeklyVolumeKm = null;
        request.RecentRunsPerWeek = null;
        GeneratePreviewRequestValidator.Validate(request);
    }

    // ── D. Nested RecentRace ─────────────────────────────────────────────

    [Fact]
    public void RecentRace_PositiveFinishTime_Passes()
    {
        var request = HabitRequest();
        request.RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3510, RaceDate = new DateOnly(2026, 6, 1) };
        GeneratePreviewRequestValidator.Validate(request);
    }

    [Fact]
    public void RecentRace_ZeroFinishTime_Throws()
    {
        var request = HabitRequest();
        request.RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 0, RaceDate = new DateOnly(2026, 6, 1) };
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(request));
    }

    [Fact]
    public void RecentRace_NegativeFinishTime_Throws()
    {
        var request = HabitRequest();
        request.RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = -1, RaceDate = new DateOnly(2026, 6, 1) };
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(request));
    }

    [Fact]
    public void RecentRace_FutureRaceDate_Throws()
    {
        var request = HabitRequest();
        var farFuture = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1);
        request.RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3510, RaceDate = farFuture };
        Assert.Throws<ArgumentException>(() => GeneratePreviewRequestValidator.Validate(request));
    }

    [Fact]
    public void RecentRace_TodayRaceDate_Passes()
    {
        var request = HabitRequest();
        request.RecentRace = new RecentRaceInput
        {
            Distance = GoalDistance.TenK,
            FinishTimeSeconds = 3510,
            RaceDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        GeneratePreviewRequestValidator.Validate(request);
    }

    [Fact]
    public void RecentRace_DoesNotOverwriteTargetRaceFields()
    {
        var request = RaceRequest();
        request.RecentRace = new RecentRaceInput { Distance = GoalDistance.FiveK, FinishTimeSeconds = 1200, RaceDate = new DateOnly(2026, 6, 1) };
        var originalRaceDate = request.RaceDate;
        var originalTargetTime = request.TargetFinishTimeSeconds;

        GeneratePreviewRequestValidator.Validate(request);

        // RecentRace is a fully independent field -- validating it (or the
        // request as a whole) must never write into the target-race fields.
        Assert.Equal(originalRaceDate, request.RaceDate);
        Assert.Equal(originalTargetTime, request.TargetFinishTimeSeconds);
    }

    [Fact]
    public void RecentRace_Null_Passes() =>
        GeneratePreviewRequestValidator.Validate(HabitRequest());
}
