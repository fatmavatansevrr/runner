using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Running Background V2 / V2.1 — provider-independent contract tests for
/// the four-canonical-value <see cref="RunningBackground"/> model. As of
/// V2.1, legacy aliases ("new_to_running", "used_to_run",
/// "running_regularly") are REJECTED at the public request boundary and
/// remain readable ONLY at two narrowly-scoped historical boundaries:
/// EF/Postgres persistence (RunningBackgroundCompatibilityConverter) and
/// internal preview-snapshot JSON (RunningBackgroundJsonConverter, applied
/// only to ResolverInputSnapshot.Level). Real relational read-compat is
/// exercised in <see cref="ResetEndpointRelationalScenarioTests"/>-style
/// tests elsewhere; this file focuses on the converter contracts themselves.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class RunningBackgroundV2Tests
{
    // Matches the production DTO-serialization setup (RunningApp.Api/Program.cs):
    // a global JsonStringEnumConverter in the Converters list, which takes
    // precedence over a type-level [JsonConverter] attribute for any enum
    // WITHOUT its own property-level override.
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    // Bare-value RunningBackground deserialization has no property-level
    // attribute to fall back on, so it relies purely on the type-level
    // [JsonConverter] attribute — which only wins when there is no
    // competing entry in Converters. This mirrors how GeneratedCatalogPlanPayload
    // snapshot round-tripping (which builds its own minimal options) behaves.
    private static readonly JsonSerializerOptions BareValueOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    [Theory]
    [InlineData("beginner", RunningBackground.Beginner)]
    [InlineData("intermediate", RunningBackground.Intermediate)]
    [InlineData("advanced", RunningBackground.Advanced)]
    [InlineData("experienced", RunningBackground.Experienced)]
    public void CanonicalValue_Deserializes(string wireValue, RunningBackground expected)
    {
        var result = JsonSerializer.Deserialize<RunningBackground>($"\"{wireValue}\"", BareValueOptions);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(RunningBackground.Beginner, "beginner")]
    [InlineData(RunningBackground.Intermediate, "intermediate")]
    [InlineData(RunningBackground.Advanced, "advanced")]
    [InlineData(RunningBackground.Experienced, "experienced")]
    public void CanonicalValue_SerializesConsistently(RunningBackground value, string expectedWire)
    {
        var json = JsonSerializer.Serialize(value, BareValueOptions);
        Assert.Equal($"\"{expectedWire}\"", json);
    }

    [Theory]
    [InlineData("new_to_running")]
    [InlineData("used_to_run")]
    [InlineData("running_regularly")]
    public void LegacyAlias_BareTypeLevelConverter_IsRejected(string legacyWireValue)
    {
        // Running Background V2.1: the type-level converter on
        // RunningBackground (RunningBackgroundCanonicalJsonConverter) is the
        // default for any bare/unannotated use and accepts only the four
        // canonical values. Legacy aliases are no longer a current product
        // option and must not decode anywhere without an explicit,
        // documented historical-compat override.
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RunningBackground>($"\"{legacyWireValue}\"", BareValueOptions));
        Assert.Contains("Unknown RunningBackground value", ex.Message);
    }

    [Theory]
    [InlineData("new_to_running", RunningBackground.Beginner)]
    [InlineData("used_to_run", RunningBackground.Beginner)]
    [InlineData("running_regularly", RunningBackground.Intermediate)]
    public void LegacyAlias_HistoricalCompatConverter_DeserializesThroughCompatibilityPath(string legacyWireValue, RunningBackground expected)
    {
        // The historical-compat converter is never the type-level default
        // (see LegacyAlias_BareTypeLevelConverter_IsRejected above) — it is
        // only reachable when a type explicitly opts in via a property-level
        // [JsonConverter(typeof(RunningBackgroundJsonConverter))] attribute,
        // exactly as ResolverInputSnapshot.Level does for reading pre-V2
        // stored preview snapshot JSON. Exercised directly here since that
        // property has no public setter path to drive it through
        // JsonSerializer<T> conveniently in a unit test.
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var converter = new RunningApp.Domain.Enums.RunningBackgroundJsonConverter();
        var bytes = System.Text.Encoding.UTF8.GetBytes($"\"{legacyWireValue}\"");
        var reader = new Utf8JsonReader(bytes);
        reader.Read();
        var result = converter.Read(ref reader, typeof(RunningBackground), options);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void UnknownValue_ThrowsTypedJsonException()
    {
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RunningBackground>("\"totally_unrelated_string\"", BareValueOptions));
        Assert.Contains("Unknown RunningBackground value", ex.Message);
    }

    [Fact]
    public void EmptyOrNullValue_ThrowsTypedJsonException()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RunningBackground>("\"\"", BareValueOptions));
    }

    [Theory]
    [InlineData("new_to_running")]
    [InlineData("used_to_run")]
    [InlineData("running_regularly")]
    public void GeneratePreviewRequest_LegacyAliasInLevel_IsRejectedWithTypedValidationError(string legacyWireValue)
    {
        // Running Background V2.1: the public request boundary no longer
        // accepts legacy aliases at all — this is the behavior change from
        // the old GeneratePreviewRequest_LegacyAliasInLevel_DeserializesToCanonicalEnum
        // test (which asserted the opposite). There is no versioned legacy
        // endpoint that still accepts these values.
        var json = $"{{\"goal_type\":\"race\",\"goal_distance\":\"ten_k\",\"level\":\"{legacyWireValue}\",\"days_per_week\":4,\"unit\":\"km\"}}";

        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<GeneratePreviewRequest>(json, Options));
        Assert.Contains("removed in Running Background V2.1", ex.Message);
    }

    [Fact]
    public void GeneratePreviewRequest_UnknownLevel_ThrowsTypedJsonException()
    {
        var json = "{\"goal_type\":\"race\",\"goal_distance\":\"ten_k\",\"level\":\"elite\",\"days_per_week\":4,\"unit\":\"km\"}";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<GeneratePreviewRequest>(json, Options));
    }

    [Theory]
    [InlineData(RunningBackground.Advanced)]
    [InlineData(RunningBackground.Experienced)]
    public void UnwidenedNonIntermediateLevels_AreNotSilentlyCoercedToIntermediate(RunningBackground level)
    {
        // The pilot identity policy's exact-allow-list is the single source
        // of truth for "does this level reach the catalog pilot" — assert
        // directly that these still-untested levels do not. Beginner is
        // covered separately below: GEN.4E deliberately widened Beginner at
        // 4D (not silently coerced to Intermediate -- routed to its own
        // TEN_K__4D__BEGINNER candidate), so it is no longer a member of
        // this "remains unsupported" set.
        var isSupported = RunningApp.Application.RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, level, 4);

        Assert.False(isSupported);
    }

    [Fact]
    public void IntermediateLevel_ReachesExistingPilotMapping()
    {
        var isSupported = RunningApp.Application.RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 4);

        Assert.True(isSupported);
        Assert.Equal("INTERMEDIATE", RunningApp.Application.RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy.CatalogLevel);
    }

    [Fact]
    public void BeginnerLevel_ReachesItsOwnPilotMapping_AtFourDaysOnly()
    {
        // GEN.4E: Beginner is now a genuinely-widened, distinct pilot
        // identity at 4D -- resolves to its own candidate, never Intermediate's.
        Assert.True(RunningApp.Application.RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 4));
        Assert.False(RunningApp.Application.RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 3));
        var resolved = RunningApp.Application.RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy.ResolveCandidate(RunningBackground.Beginner, 4);
        Assert.Equal("TEN_K__4D__BEGINNER", resolved.CandidateKey);
    }

    private static AppDbContext NewPostgresContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres")
            .Options);

    [Theory]
    [InlineData("new_to_running", RunningBackground.Beginner)]
    [InlineData("used_to_run", RunningBackground.Beginner)]
    [InlineData("running_regularly", RunningBackground.Intermediate)]
    [InlineData("beginner", RunningBackground.Beginner)]
    [InlineData("advanced", RunningBackground.Advanced)]
    [InlineData("experienced", RunningBackground.Experienced)]
    public async System.Threading.Tasks.Task ExistingStoredLegacyOrCanonicalPlan_RemainsReadable_RealPostgres(
        string storedTextValue, RunningBackground expected)
    {
        var planId = Guid.NewGuid();
        await using (var seedCtx = NewPostgresContext())
        {
            // Insert the raw legacy/canonical text directly (bypassing EF's
            // enum conversion) to simulate a row written before or after
            // this migration, exactly as it would exist in Postgres.
            await seedCtx.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""TrainingPlans"" (""Id"", ""Status"", ""GoalType"", ""GoalDistance"", ""Level"",
                    ""DaysPerWeek"", ""Unit"", ""StartedAt"", ""EstimatedEndDate"", ""CreatedAt"")
                VALUES ({planId}, 'active', 'race', 'ten_k', {storedTextValue},
                    4, 'km', now(), now() + interval '56 days', now())");
        }

        await using var readCtx = NewPostgresContext();
        var plan = await readCtx.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(expected, plan.Level);

        await using var cleanupCtx = NewPostgresContext();
        await cleanupCtx.Database.ExecuteSqlInterpolatedAsync($@"DELETE FROM ""TrainingPlans"" WHERE ""Id"" = {planId}");
    }
}
