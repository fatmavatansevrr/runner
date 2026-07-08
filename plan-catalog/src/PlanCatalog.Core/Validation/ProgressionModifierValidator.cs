using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

public static class ProgressionModifierValidator
{
    public static ValidationResult Validate(ProgressionModifierDefinition modifier)
    {
        var issues = new List<ValidationIssue>();

        if (modifier.MaximumComplexityTier < 1)
        {
            issues.Add(new ValidationIssue("PM_COMPLEXITY_TIER_TOO_LOW", ValidationSeverity.Error,
                "MaximumComplexityTier must be >= 1.", "$.maximumComplexityTier"));
        }

        if (modifier.MaximumHardSessionsPerWeek < 0)
        {
            issues.Add(new ValidationIssue("PM_HARD_SESSION_CAP_NEGATIVE", ValidationSeverity.Error,
                "MaximumHardSessionsPerWeek must be >= 0.", "$.maximumHardSessionsPerWeek"));
        }

        if (modifier.MainSetDoseMultiplier <= 0)
        {
            issues.Add(new ValidationIssue("PM_DOSE_MULTIPLIER_NOT_POSITIVE", ValidationSeverity.Error,
                "MainSetDoseMultiplier must be > 0.", "$.mainSetDoseMultiplier"));
        }

        if (!modifier.AllowSecondHardStimulus && modifier.MaximumHardSessionsPerWeek > 1)
        {
            issues.Add(new ValidationIssue("PM_HARD_SESSION_CAP_EXCEEDS_SINGLE_STIMULUS", ValidationSeverity.Error,
                "MaximumHardSessionsPerWeek cannot exceed 1 when AllowSecondHardStimulus is false.", "$.maximumHardSessionsPerWeek"));
        }

        return new ValidationResult(issues);
    }
}
