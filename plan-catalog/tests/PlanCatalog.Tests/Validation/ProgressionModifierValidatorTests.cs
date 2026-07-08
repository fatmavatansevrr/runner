using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

public sealed class ProgressionModifierValidatorTests
{
    private static ProgressionModifierDefinition Valid() => new()
    {
        Metadata = Meta.Of("PROGRESSION_MODIFIER", "INTERMEDIATE_PROGRESSION_MODIFIER_V1"),
        Experience = RunningExperience.Intermediate,
        MaximumComplexityTier = 2,
        MaximumHardSessionsPerWeek = 2,
        MainSetDoseMultiplier = 1.0m,
        AllowGoalPaceRehearsal = true,
        AllowSecondHardStimulus = true
    };

    [Fact]
    public void Valid_Passes()
    {
        Assert.True(ProgressionModifierValidator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void ComplexityTierBelowOne_Fails()
    {
        var result = ProgressionModifierValidator.Validate(Valid() with { MaximumComplexityTier = 0 });
        Assert.Contains(result.Issues, i => i.Code == "PM_COMPLEXITY_TIER_TOO_LOW");
    }

    [Fact]
    public void NonPositiveDoseMultiplier_Fails()
    {
        var result = ProgressionModifierValidator.Validate(Valid() with { MainSetDoseMultiplier = 0m });
        Assert.Contains(result.Issues, i => i.Code == "PM_DOSE_MULTIPLIER_NOT_POSITIVE");
    }

    [Fact]
    public void SingleStimulusWithCapAboveOne_Fails()
    {
        var result = ProgressionModifierValidator.Validate(Valid() with { AllowSecondHardStimulus = false, MaximumHardSessionsPerWeek = 2 });
        Assert.Contains(result.Issues, i => i.Code == "PM_HARD_SESSION_CAP_EXCEEDS_SINGLE_STIMULUS");
    }
}
