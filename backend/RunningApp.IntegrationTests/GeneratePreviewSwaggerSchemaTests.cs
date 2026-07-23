using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Proves Swagger exposes two separate, flow-specific request schemas
/// (<c>GenerateRacePlanPreviewRequest</c>/<c>GenerateHabitPlanPreviewRequest</c>)
/// generated from each DTO's real C# contract (required members, nullable
/// value types, enum converters, the nested RecentRace object) — never a
/// hard-coded example payload, and never one shared monolithic schema with
/// unrelated fields hidden via a schema filter. The old single
/// <c>GeneratePreviewRequest</c> type is no longer bound by any controller
/// action, so Swashbuckle no longer emits a schema for it at all.
/// </summary>
public sealed class GeneratePreviewSwaggerSchemaTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GeneratePreviewSwaggerSchemaTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<JsonNode> GetSwaggerAsync() =>
        (await _client.GetFromJsonAsync<JsonNode>("/swagger/v1/swagger.json"))!;

    private async Task<JsonNode> GetSchemaAsync(string schemaName)
    {
        var swagger = await GetSwaggerAsync();
        return swagger["components"]!["schemas"]![schemaName]!;
    }

    [Fact]
    public async Task OldMonolithicSchema_NoLongerExists()
    {
        var swagger = await GetSwaggerAsync();
        var schemas = swagger["components"]!["schemas"]!.AsObject();

        Assert.False(schemas.ContainsKey("GeneratePreviewRequest"));
    }

    [Fact]
    public async Task BothOperations_AreRegistered_AtDistinctPaths()
    {
        var swagger = await GetSwaggerAsync();
        var paths = swagger["paths"]!.AsObject();

        Assert.True(paths.ContainsKey("/api/v1/plans/generate-preview/race"));
        Assert.True(paths.ContainsKey("/api/v1/plans/generate-preview/habit"));
        Assert.False(paths.ContainsKey("/api/v1/plans/generate-preview"));
    }

    // ── Race schema ──────────────────────────────────────────────────────

    [Fact]
    public async Task RaceSchema_HasNoExampleValue()
    {
        var schema = await GetSchemaAsync("GenerateRacePlanPreviewRequest");
        Assert.Null(schema["example"]);
    }

    [Fact]
    public async Task RaceSchema_ListsRaceAndSharedFieldsAsRequired()
    {
        var schema = await GetSchemaAsync("GenerateRacePlanPreviewRequest");
        var required = schema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();

        foreach (var field in new[]
        {
            "goal_distance", "level", "days_per_week", "unit", "start_date", "preferred_days",
            "long_run_day", "race_date", "target_finish_time_seconds", "target_finish_time_source",
        })
        {
            Assert.Contains(field, required);
        }
    }

    [Fact]
    public async Task RaceSchema_DoesNotExposeHabitOrCustomOrLegacyFields()
    {
        var schema = await GetSchemaAsync("GenerateRacePlanPreviewRequest");
        var properties = schema["properties"]!.AsObject();

        foreach (var field in new[]
        {
            "goal_type", "weekly_availability", "preferred_pace", "habit_plan_type",
            "custom_goal_type", "custom_duration_weeks", "custom_target_time_seconds",
        })
        {
            Assert.False(properties.ContainsKey(field), $"{field} should not be on the race schema.");
        }
    }

    [Fact]
    public async Task RaceSchema_TargetFinishTimeSource_IsAnEnumWithCanonicalValues()
    {
        var swagger = await GetSwaggerAsync();
        var schema = swagger["components"]!["schemas"]!["GenerateRacePlanPreviewRequest"]!;
        var sourceRef = schema["properties"]!["target_finish_time_source"]!["$ref"]?.GetValue<string>();
        Assert.NotNull(sourceRef);

        var schemas = swagger["components"]!["schemas"]!.AsObject();
        var sourceSchemaName = sourceRef!.Split('/').Last();
        var sourceSchema = schemas[sourceSchemaName]!;
        var enumValues = sourceSchema["enum"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();

        Assert.Contains("product_average", enumValues);
        Assert.Contains("user_defined", enumValues);
        Assert.Equal(2, enumValues.Count);
    }

    [Fact]
    public async Task RaceSchema_RecentRace_IsNestedObject_ReferencingItsOwnSchema()
    {
        var swagger = await GetSwaggerAsync();
        var requestSchema = swagger["components"]!["schemas"]!["GenerateRacePlanPreviewRequest"]!;

        var recentRaceRef = requestSchema["properties"]!["recent_race"]!["$ref"]!.GetValue<string>();
        Assert.EndsWith("/RecentRaceInput", recentRaceRef);

        var recentRaceSchema = swagger["components"]!["schemas"]!["RecentRaceInput"]!;
        Assert.NotNull(recentRaceSchema["properties"]!["distance"]);
        Assert.NotNull(recentRaceSchema["properties"]!["finish_time_seconds"]);
        Assert.NotNull(recentRaceSchema["properties"]!["race_date"]);
    }

    [Fact]
    public async Task RaceSchema_ReadinessFieldsAreNullable()
    {
        var schema = await GetSchemaAsync("GenerateRacePlanPreviewRequest");

        foreach (var field in new[] { "recent_weekly_volume_km", "recent_longest_run_km", "recent_runs_per_week" })
        {
            var property = schema["properties"]![field]!;
            var isNullable = (property["nullable"]?.GetValue<bool>() ?? false) || property["type"] is null;
            Assert.True(isNullable, $"{field} should be nullable in the schema.");
        }
    }

    [Fact]
    public async Task PreferredDays_IsAnArraySchema_OnBothOperations()
    {
        var raceSchema = await GetSchemaAsync("GenerateRacePlanPreviewRequest");
        var habitSchema = await GetSchemaAsync("GenerateHabitPlanPreviewRequest");

        Assert.Equal("array", raceSchema["properties"]!["preferred_days"]!["type"]!.GetValue<string>());
        Assert.Equal("array", habitSchema["properties"]!["preferred_days"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public async Task WeekdayEnum_ExposesAllSevenCanonicalValues()
    {
        var swagger = await GetSwaggerAsync();
        var schemas = swagger["components"]!["schemas"]!.AsObject();

        var weekdaySchema = schemas.FirstOrDefault(kv => kv.Key.Contains("Weekday")).Value;
        Assert.NotNull(weekdaySchema);

        var enumValues = weekdaySchema!["enum"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();
        foreach (var day in new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" })
        {
            Assert.Contains(day, enumValues);
        }
    }

    [Fact]
    public async Task RaceSchema_ContainsNoAcceptanceTestBusinessValues()
    {
        var schemaJson = (await GetSchemaAsync("GenerateRacePlanPreviewRequest")).ToJsonString();

        Assert.DoesNotContain("Local 10K", schemaJson);
        Assert.DoesNotContain("2026-07-20", schemaJson);
        Assert.DoesNotContain("2026-10-12", schemaJson);
        Assert.DoesNotContain("3600", schemaJson);
        Assert.DoesNotContain("3480", schemaJson);
    }

    // ── Habit schema ─────────────────────────────────────────────────────

    [Fact]
    public async Task HabitSchema_HasNoExampleValue()
    {
        var schema = await GetSchemaAsync("GenerateHabitPlanPreviewRequest");
        Assert.Null(schema["example"]);
    }

    [Fact]
    public async Task HabitSchema_ListsSharedFieldsAsRequired_LongRunDayNotRequired()
    {
        var schema = await GetSchemaAsync("GenerateHabitPlanPreviewRequest");
        var required = schema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();

        foreach (var field in new[] { "goal_distance", "level", "days_per_week", "unit", "start_date", "preferred_days" })
        {
            Assert.Contains(field, required);
        }

        Assert.DoesNotContain("long_run_day", required);
    }

    [Fact]
    public async Task HabitSchema_DoesNotExposeRaceOrTargetOrRecentRaceOrCustomFields()
    {
        var schema = await GetSchemaAsync("GenerateHabitPlanPreviewRequest");
        var properties = schema["properties"]!.AsObject();

        foreach (var field in new[]
        {
            "goal_type", "race_name", "race_date", "target_finish_time_seconds", "target_finish_time_source",
            "recent_race", "recent_weekly_volume_km", "recent_longest_run_km", "recent_runs_per_week",
            "custom_goal_type", "custom_duration_weeks", "custom_target_time_seconds", "weekly_availability", "preferred_pace",
        })
        {
            Assert.False(properties.ContainsKey(field), $"{field} should not be on the habit schema.");
        }
    }
}
