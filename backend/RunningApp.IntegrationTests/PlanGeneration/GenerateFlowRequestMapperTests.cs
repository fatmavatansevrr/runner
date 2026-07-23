using System;
using RunningApp.Application.Commands.Plan;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

/// <summary>
/// Proves the public flow-specific DTO → typed command mapping
/// (<see cref="GeneratePreviewCommandMapper.ToCommand(GenerateRacePlanPreviewRequest)"/>/
/// <see cref="GeneratePreviewCommandMapper.ToCommand(GenerateHabitPlanPreviewRequest)"/>),
/// including that <see cref="TargetFinishTimeSource"/> survives the mapping
/// untouched.
/// </summary>
public sealed class GenerateFlowRequestMapperTests
{
    [Fact]
    public void RaceRequest_MapsToRacePlanPreviewCommand_WithTargetFinishTimeSourcePreserved()
    {
        var request = new GenerateRacePlanPreviewRequest
        {
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            RaceDate = new DateOnly(2026, 10, 12),
            TargetFinishTimeSeconds = 3480,
            TargetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
            RaceName = "Local 10K",
        };

        var command = GeneratePreviewCommandMapper.ToCommand(request);

        Assert.Equal(GoalType.Race, command.GoalType);
        Assert.Equal(3480, command.TargetFinishTimeSeconds);
        Assert.Equal(TargetFinishTimeSource.ProductAverage, command.TargetFinishTimeSource);
        Assert.Equal(new DateOnly(2026, 10, 12), command.RaceDate);
        Assert.Equal(Weekday.Sun, command.LongRunDay);
        Assert.Equal("Local 10K", command.RaceName);
    }

    [Fact]
    public void HabitRequest_MapsToHabitPlanPreviewCommand_NoRaceFields()
    {
        var request = new GenerateHabitPlanPreviewRequest
        {
            GoalDistance = GoalDistance.FiveK,
            Level = RunningBackground.Beginner,
            DaysPerWeek = 3,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat },
        };

        var command = GeneratePreviewCommandMapper.ToCommand(request);

        Assert.Equal(GoalType.Habit, command.GoalType);
        Assert.Null(command.LongRunDay);
        // HabitPlanPreviewCommand has no RaceDate/TargetFinishTimeSeconds/
        // TargetFinishTimeSource property at all -- there is nothing further
        // to assert null on; the type itself is the guarantee.
    }

    [Fact]
    public void RaceCommand_RoundTrips_ThroughInternalRequest_WithTargetFinishTimeSource()
    {
        var request = new GenerateRacePlanPreviewRequest
        {
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            RaceDate = new DateOnly(2026, 10, 12),
            TargetFinishTimeSeconds = 3480,
            TargetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
        };
        var command = GeneratePreviewCommandMapper.ToCommand(request);

        var internalRequest = GeneratePreviewCommandMapper.ToInternalRequest(command);

        Assert.Equal(TargetFinishTimeSource.ProductAverage, internalRequest.TargetFinishTimeSource);
        Assert.Equal(3480, internalRequest.TargetFinishTimeSeconds);
        Assert.Equal(GoalType.Race, internalRequest.GoalType);
    }

    [Fact]
    public void HabitCommand_RoundTrips_ThroughInternalRequest_NullTargetFinishTimeSource()
    {
        var request = new GenerateHabitPlanPreviewRequest
        {
            GoalDistance = GoalDistance.FiveK,
            Level = RunningBackground.Beginner,
            DaysPerWeek = 3,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat },
        };
        var command = GeneratePreviewCommandMapper.ToCommand(request);

        var internalRequest = GeneratePreviewCommandMapper.ToInternalRequest(command);

        Assert.Null(internalRequest.TargetFinishTimeSource);
        Assert.Null(internalRequest.TargetFinishTimeSeconds);
        Assert.Equal(GoalType.Habit, internalRequest.GoalType);
    }
}
