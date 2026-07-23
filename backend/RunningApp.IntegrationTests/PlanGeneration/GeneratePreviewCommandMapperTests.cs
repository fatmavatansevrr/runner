using System;
using RunningApp.Application.Commands.Plan;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

/// <summary>
/// Proves the validated-DTO → typed-command mapping boundary
/// (<see cref="GeneratePreviewCommandMapper"/>): a Race request maps to a
/// <see cref="RacePlanPreviewCommand"/> whose race fields are genuinely
/// non-nullable (no null-forgiving operator needed by any caller), and a
/// Habit request maps to a <see cref="HabitPlanPreviewCommand"/> that has
/// no race-only fields to accidentally read at all.
/// </summary>
public sealed class GeneratePreviewCommandMapperTests
{
    [Fact]
    public void RaceRequest_MapsToRacePlanPreviewCommand_WithNonNullRequiredFields()
    {
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            RaceDate = new DateOnly(2026, 10, 12),
            TargetFinishTimeSeconds = 3480,
            RaceName = "Local 10K",
        };

        var command = GeneratePreviewCommandMapper.ToCommand(request);

        var raceCommand = Assert.IsType<RacePlanPreviewCommand>(command);
        // These are non-nullable CLR members on RacePlanPreviewCommand --
        // simply compiling and reading them here (no `!`, no null-check) is
        // itself part of the proof that mapping produces genuinely
        // guaranteed-non-null required values, not raw nullable transport
        // fields pushed further down.
        Assert.Equal(new DateOnly(2026, 10, 12), raceCommand.RaceDate);
        Assert.Equal(Weekday.Sun, raceCommand.LongRunDay);
        Assert.Equal(3480, raceCommand.TargetFinishTimeSeconds);
        Assert.Equal("Local 10K", raceCommand.RaceName);
    }

    [Fact]
    public void HabitRequest_MapsToHabitPlanPreviewCommand_WithNoRaceOnlyFields()
    {
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Habit,
            GoalDistance = GoalDistance.FiveK,
            Level = RunningBackground.Beginner,
            DaysPerWeek = 3,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat },
        };

        var command = GeneratePreviewCommandMapper.ToCommand(request);

        var habitCommand = Assert.IsType<HabitPlanPreviewCommand>(command);
        // HabitPlanPreviewCommand has no RaceDate/RaceName/TargetFinishTimeSeconds
        // property at all -- there is nothing to accidentally read, which is
        // exactly the "invalid states harder to represent" goal. LongRunDay
        // remains optional (nullable) for Habit.
        Assert.Null(habitCommand.LongRunDay);
        Assert.Equal(GoalType.Habit, habitCommand.GoalType);
    }

    [Fact]
    public void HabitRequest_WithLongRunDay_PreservesIt()
    {
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Habit,
            GoalDistance = GoalDistance.FiveK,
            Level = RunningBackground.Beginner,
            DaysPerWeek = 3,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat },
            LongRunDay = Weekday.Sat,
        };

        var command = GeneratePreviewCommandMapper.ToCommand(request);

        var habitCommand = Assert.IsType<HabitPlanPreviewCommand>(command);
        Assert.Equal(Weekday.Sat, habitCommand.LongRunDay);
    }

    [Fact]
    public void SharedFields_AreCarriedThroughUnchanged()
    {
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Habit,
            GoalDistance = GoalDistance.FiveK,
            Level = RunningBackground.Beginner,
            DaysPerWeek = 3,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat },
            RecentWeeklyVolumeKm = 0,
            RecentLongestRunKm = null,
            RecentRunsPerWeek = 4,
        };

        var command = GeneratePreviewCommandMapper.ToCommand(request);

        Assert.Equal(request.GoalDistance, command.GoalDistance);
        Assert.Equal(request.Level, command.Level);
        Assert.Equal(request.DaysPerWeek, command.DaysPerWeek);
        Assert.Equal(request.StartDate, command.StartDate);
        Assert.Equal(request.PreferredDays, command.PreferredDays);
        Assert.Equal(0, command.RecentWeeklyVolumeKm);
        Assert.Null(command.RecentLongestRunKm);
        Assert.Equal(4, command.RecentRunsPerWeek);
    }
}
