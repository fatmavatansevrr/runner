using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

public sealed class RuntimeConditionValueRegistryValidatorTests
{
    private static RuntimeConditionValueRegistryDefinition Valid() => new()
    {
        Metadata = Meta.Of(DocumentTypes.RuntimeConditionValueRegistry, "RUNTIME_CONDITION_VALUES_V1", status: CatalogStatus.Published),
        ConditionValueSets =
        [
            new RuntimeConditionValueSet { ConditionType = RuntimeConditionType.GoalFeasibilityIn, AllowedValues = new HashSet<string> { "REALISTIC", "CHALLENGING" } }
        ]
    };

    [Fact]
    public void Valid_Passes()
    {
        Assert.True(RuntimeConditionValueRegistryValidator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void DuplicateConditionType_Fails()
    {
        var registry = Valid() with
        {
            ConditionValueSets =
            [
                new RuntimeConditionValueSet { ConditionType = RuntimeConditionType.GoalFeasibilityIn, AllowedValues = new HashSet<string> { "REALISTIC" } },
                new RuntimeConditionValueSet { ConditionType = RuntimeConditionType.GoalFeasibilityIn, AllowedValues = new HashSet<string> { "CHALLENGING" } }
            ]
        };

        var result = RuntimeConditionValueRegistryValidator.Validate(registry);
        Assert.Contains(result.Issues, i => i.Code == "RCVR_DUPLICATE_CONDITION_TYPE");
    }

    [Fact]
    public void EmptyAllowedValues_Fails()
    {
        var registry = Valid() with
        {
            ConditionValueSets = [new RuntimeConditionValueSet { ConditionType = RuntimeConditionType.PlanModeIn, AllowedValues = new HashSet<string>() }]
        };

        var result = RuntimeConditionValueRegistryValidator.Validate(registry);
        Assert.Contains(result.Issues, i => i.Code == "RCVR_ALLOWED_VALUES_EMPTY");
    }

    [Fact]
    public void LowerCaseValue_Fails()
    {
        var registry = Valid() with
        {
            ConditionValueSets = [new RuntimeConditionValueSet { ConditionType = RuntimeConditionType.PlanModeIn, AllowedValues = new HashSet<string> { "standard" } }]
        };

        var result = RuntimeConditionValueRegistryValidator.Validate(registry);
        Assert.Contains(result.Issues, i => i.Code == "RCVR_VALUE_NOT_UPPER_SNAKE_CASE");
    }
}
