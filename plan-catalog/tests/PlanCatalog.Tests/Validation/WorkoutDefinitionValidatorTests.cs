using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Core.Enums;
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

    [Fact]
    public void DraftFartlek_WithNestedRecoveryOwnership_Passes()
    {
        var result = WorkoutDefinitionValidator.Validate(Structured("FARTLEK", CatalogStatus.Draft,
            WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.CoolDown));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DraftFartlek_WithStructuralRecovery_IsRejectedDeterministically()
    {
        var result = WorkoutDefinitionValidator.Validate(Structured("FARTLEK", CatalogStatus.Draft,
            WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.Recovery, WorkoutComponentType.CoolDown));

        Assert.Contains(result.Issues, issue => issue.Code == "WD_RECOVERY_OWNERSHIP_DUPLICATED");
    }

    [Fact]
    public void ValidatedHistoricalFartlek_WithStructuralRecovery_RemainsReplayable()
    {
        var result = WorkoutDefinitionValidator.Validate(Structured("FARTLEK", CatalogStatus.Validated,
            WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.Recovery, WorkoutComponentType.CoolDown));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void IndependentRecoveryComponent_IsNotGloballyProhibited()
    {
        var result = WorkoutDefinitionValidator.Validate(Structured("RECOVERY_SESSION", CatalogStatus.Draft,
            WorkoutComponentType.Recovery));

        Assert.True(result.IsValid);
    }

    private static WorkoutDefinition Structured(string key, CatalogStatus status, params WorkoutComponentType[] types) => new()
    {
        Metadata = Meta.Of("WORKOUT_DEFINITION", key, status: status) with { SchemaVersion = 3 },
        Family = WorkoutFamily.Quality,
        ComplexityTier = null,
        EligiblePhases = [PhaseKey.Build],
        AllowedPrescriptionModes = [PrescriptionMode.EffortBased],
        Components = types.Select((type, index) => new WorkoutComponentDefinition
        {
            SequenceOrder = index + 1,
            ComponentType = type,
            IntensityDescriptor = "TEST"
        }).ToList()
    };
}
