using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

/// <summary>
/// JSON-boundary contract tests for <see cref="GeneratePreviewRequest"/>,
/// using the exact serializer options the real API wires up in
/// <c>RunningApp.Api/Program.cs</c> (snake_case property names, global
/// <see cref="JsonStringEnumConverter"/> with
/// <see cref="JsonNamingPolicy.SnakeCaseLower"/>).
/// </summary>
public sealed class GeneratePreviewRequestJsonContractTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    private const string RaceJson = """
        {
          "goal_type": "race",
          "goal_distance": "ten_k",
          "level": "intermediate",
          "days_per_week": 4,
          "unit": "km",
          "preferred_days": ["mon", "wed", "fri", "sun"],
          "long_run_day": "sun",
          "start_date": "2026-07-20",
          "race_name": "Local 10K",
          "race_date": "2026-10-12",
          "target_finish_time_seconds": 3600,
          "recent_weekly_volume_km": 20,
          "recent_longest_run_km": 8,
          "recent_runs_per_week": 3,
          "recent_race": {
            "distance": "ten_k",
            "finish_time_seconds": 3510,
            "race_date": "2026-06-01"
          }
        }
        """;

    private const string HabitJson = """
        {
          "goal_type": "habit",
          "goal_distance": "five_k",
          "level": "beginner",
          "days_per_week": 3,
          "unit": "km",
          "preferred_days": ["mon", "wed", "sat"],
          "long_run_day": null,
          "start_date": "2026-07-20",
          "race_name": null,
          "race_date": null,
          "target_finish_time_seconds": null,
          "recent_weekly_volume_km": null,
          "recent_longest_run_km": null,
          "recent_runs_per_week": null,
          "recent_race": null
        }
        """;

    [Fact]
    public void RaceJson_DeserializesToFullyPopulatedTypedRequest()
    {
        var request = JsonSerializer.Deserialize<GeneratePreviewRequest>(RaceJson, Options)!;

        Assert.Equal(GoalType.Race, request.GoalType);
        Assert.Equal(GoalDistance.TenK, request.GoalDistance);
        Assert.Equal(RunningBackground.Intermediate, request.Level);
        Assert.Equal(4, request.DaysPerWeek);
        Assert.Equal(new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun }, request.PreferredDays);
        Assert.Equal(Weekday.Sun, request.LongRunDay);
        Assert.Equal(new DateOnly(2026, 7, 20), request.StartDate);
        Assert.Equal("Local 10K", request.RaceName);
        Assert.Equal(new DateOnly(2026, 10, 12), request.RaceDate);
        Assert.Equal(3600, request.TargetFinishTimeSeconds);
        Assert.Equal(20, request.RecentWeeklyVolumeKm);
        Assert.Equal(8, request.RecentLongestRunKm);
        Assert.Equal(3, request.RecentRunsPerWeek);
        Assert.NotNull(request.RecentRace);
        Assert.Equal(GoalDistance.TenK, request.RecentRace!.Distance);
        Assert.Equal(3510, request.RecentRace.FinishTimeSeconds);
        Assert.Equal(new DateOnly(2026, 6, 1), request.RecentRace.RaceDate);
    }

    [Fact]
    public void HabitJson_DeserializesWithNullOptionalFieldsPreserved()
    {
        var request = JsonSerializer.Deserialize<GeneratePreviewRequest>(HabitJson, Options)!;

        Assert.Equal(GoalType.Habit, request.GoalType);
        Assert.Equal(new[] { Weekday.Mon, Weekday.Wed, Weekday.Sat }, request.PreferredDays);
        Assert.Null(request.LongRunDay);
        Assert.Equal(new DateOnly(2026, 7, 20), request.StartDate);
        Assert.Null(request.TargetFinishTimeSeconds);
        Assert.Null(request.RecentWeeklyVolumeKm);
        Assert.Null(request.RecentLongestRunKm);
        Assert.Null(request.RecentRunsPerWeek);
        Assert.Null(request.RecentRace);
    }

    [Theory]
    [InlineData("beginner", RunningBackground.Beginner)]
    [InlineData("intermediate", RunningBackground.Intermediate)]
    [InlineData("advanced", RunningBackground.Advanced)]
    [InlineData("experienced", RunningBackground.Experienced)]
    public void CanonicalBackgroundValues_AreAccepted_AndNeverCoerced(string wireValue, RunningBackground expected)
    {
        var json = HabitJson.Replace("\"level\": \"beginner\"", $"\"level\": \"{wireValue}\"");
        var request = JsonSerializer.Deserialize<GeneratePreviewRequest>(json, Options)!;
        Assert.Equal(expected, request.Level);
    }

    [Theory]
    [InlineData("new_to_running")]
    [InlineData("used_to_run")]
    [InlineData("running_regularly")]
    public void LegacyBackgroundAliases_AreRejected(string legacyAlias)
    {
        var json = HabitJson.Replace("\"level\": \"beginner\"", $"\"level\": \"{legacyAlias}\"");
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<GeneratePreviewRequest>(json, Options));
    }

    [Fact]
    public void AdvancedLevel_NeverCoercedToIntermediate()
    {
        var json = HabitJson.Replace("\"level\": \"beginner\"", "\"level\": \"advanced\"");
        var request = JsonSerializer.Deserialize<GeneratePreviewRequest>(json, Options)!;
        Assert.Equal(RunningBackground.Advanced, request.Level);
        Assert.NotEqual(RunningBackground.Intermediate, request.Level);
    }

    [Fact]
    public void ExperiencedLevel_NeverCoercedToIntermediate()
    {
        var json = HabitJson.Replace("\"level\": \"beginner\"", "\"level\": \"experienced\"");
        var request = JsonSerializer.Deserialize<GeneratePreviewRequest>(json, Options)!;
        Assert.Equal(RunningBackground.Experienced, request.Level);
        Assert.NotEqual(RunningBackground.Intermediate, request.Level);
    }

    [Fact]
    public void UnknownWeekdayToken_FailsDeserialization()
    {
        var json = RaceJson.Replace("\"mon\", \"wed\", \"fri\", \"sun\"", "\"Monday\", \"wed\", \"fri\", \"sun\"");
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<GeneratePreviewRequest>(json, Options));
    }

    /// <summary>
    /// Requirement 10 of the professional request-model redesign: a
    /// non-nullable value type (int/DateOnly/enum) alone does not prove a
    /// JSON property was supplied — only the C# `required` modifier
    /// (enforced natively by System.Text.Json at deserialization,
    /// independent of any CLR default value) does. These seven fields are
    /// `required` on <see cref="GeneratePreviewRequest"/> for exactly this
    /// reason; omitting any one of them — even ones backed by an int or
    /// DateOnly whose CLR default (0, DateOnly.MinValue) could otherwise be
    /// mistaken for "not sent" — must fail deserialization outright.
    /// </summary>
    [Theory]
    [InlineData("goal_type")]
    [InlineData("goal_distance")]
    [InlineData("level")]
    [InlineData("days_per_week")]
    [InlineData("unit")]
    [InlineData("preferred_days")]
    [InlineData("start_date")]
    public void OmittingSharedRequiredField_FailsDeserialization_RegardlessOfClrType(string propertyName)
    {
        var node = JsonNode.Parse(RaceJson)!.AsObject();
        node.Remove(propertyName);

        var ex = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<GeneratePreviewRequest>(node.ToJsonString(), Options));

        Assert.Contains(propertyName, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RoundTrip_SerializeThenDeserialize_PreservesShape()
    {
        var original = JsonSerializer.Deserialize<GeneratePreviewRequest>(RaceJson, Options)!;
        var roundTripped = JsonSerializer.Deserialize<GeneratePreviewRequest>(
            JsonSerializer.Serialize(original, Options), Options)!;

        Assert.Equal(original.PreferredDays, roundTripped.PreferredDays);
        Assert.Equal(original.LongRunDay, roundTripped.LongRunDay);
        Assert.Equal(original.StartDate, roundTripped.StartDate);
        Assert.Equal(original.RecentRace!.Distance, roundTripped.RecentRace!.Distance);
        Assert.Equal(original.RecentRace.FinishTimeSeconds, roundTripped.RecentRace.FinishTimeSeconds);
    }
}
