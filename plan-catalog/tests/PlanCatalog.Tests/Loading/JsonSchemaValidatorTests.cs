using PlanCatalog.Contracts;
using PlanCatalog.Infrastructure.Schema;
using Xunit;

namespace PlanCatalog.Tests.Loading;

public sealed class JsonSchemaValidatorTests
{
    private static string SchemasDirectory() => Path.Combine(AppContext.BaseDirectory, "TestSchemas");

    [Fact]
    public void ValidRuntimeConditionValueRegistry_Passes()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        var json = """
        {
          "metadata": { "documentType": "RUNTIME_CONDITION_VALUE_REGISTRY", "schemaVersion": 1, "key": "RUNTIME_CONDITION_VALUES_V1", "version": 1, "status": "PUBLISHED" },
          "conditionValueSets": [ { "conditionType": "GOAL_FEASIBILITY_IN", "allowedValues": ["REALISTIC"] } ]
        }
        """;

        var result = validator.Validate(DocumentTypes.RuntimeConditionValueRegistry, json);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void MissingRequiredField_Fails()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        var json = """
        {
          "metadata": { "documentType": "RUNTIME_CONDITION_VALUE_REGISTRY", "schemaVersion": 1, "key": "RUNTIME_CONDITION_VALUES_V1", "version": 1, "status": "PUBLISHED" }
        }
        """;

        var result = validator.Validate(DocumentTypes.RuntimeConditionValueRegistry, json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void LevelModifierWithDeprecatedField_Fails()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        var json = """
        {
          "metadata": { "documentType": "LEVEL_MODIFIER", "schemaVersion": 1, "key": "INTERMEDIATE_MODIFIER", "version": 1, "status": "PUBLISHED" },
          "experience": "INTERMEDIATE",
          "eligibleWorkoutKeys": ["EASY_STANDARD"],
          "progressionModifier": { "documentType": "PROGRESSION_MODIFIER", "key": "INTERMEDIATE_PROGRESSION_MODIFIER_V1", "version": 1 },
          "maximumHardSessionsPerWeek": 2
        }
        """;

        var result = validator.Validate(DocumentTypes.LevelModifier, json);
        Assert.False(result.IsValid);
    }
}
