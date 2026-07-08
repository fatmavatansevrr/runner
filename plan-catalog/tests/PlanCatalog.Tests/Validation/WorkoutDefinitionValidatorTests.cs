using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

public sealed class WorkoutDefinitionValidatorTests
{
    private static WorkoutDefinition Valid() => new()
    {
        Metadata = Meta.Of("WORKOUT_DEFINITION", "EASY_STANDARD"),
        Family = WorkoutFamily.Easy,
        ComplexityTier = 1,
        EligiblePhases = [PhaseKey.Foundation, PhaseKey.Build],
        AllowedPrescriptionModes = [PrescriptionMode.EffortBased],
        Components = [new WorkoutComponentDefinition { SequenceOrder = 1, ComponentType = WorkoutComponentType.MainSet, IntensityDescriptor = "EASY" }]
    };

    [Fact]
    public void Valid_Passes()
    {
        Assert.True(WorkoutDefinitionValidator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void EmptyEligiblePhases_Fails()
    {
        var result = WorkoutDefinitionValidator.Validate(Valid() with { EligiblePhases = [] });
        Assert.Contains(result.Issues, i => i.Code == "WD_ELIGIBLE_PHASES_EMPTY");
    }

    [Fact]
    public void EmptyPrescriptionModes_Fails()
    {
        var result = WorkoutDefinitionValidator.Validate(Valid() with { AllowedPrescriptionModes = [] });
        Assert.Contains(result.Issues, i => i.Code == "WD_PRESCRIPTION_MODES_EMPTY");
    }

    [Fact]
    public void ComplexityTierBelowOne_Fails()
    {
        var result = WorkoutDefinitionValidator.Validate(Valid() with { ComplexityTier = 0 });
        Assert.Contains(result.Issues, i => i.Code == "WD_COMPLEXITY_TIER_TOO_LOW");
    }
}
