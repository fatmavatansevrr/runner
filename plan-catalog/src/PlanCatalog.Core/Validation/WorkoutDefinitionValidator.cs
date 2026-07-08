using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

public static class WorkoutDefinitionValidator
{
    public static ValidationResult Validate(WorkoutDefinition workout)
    {
        var issues = new List<ValidationIssue>();

        if (workout.ComplexityTier < 1)
        {
            issues.Add(new ValidationIssue("WD_COMPLEXITY_TIER_TOO_LOW", ValidationSeverity.Error,
                "ComplexityTier must be >= 1.", "$.complexityTier"));
        }

        if (workout.EligiblePhases.Count == 0)
        {
            issues.Add(new ValidationIssue("WD_ELIGIBLE_PHASES_EMPTY", ValidationSeverity.Error,
                "EligiblePhases cannot be empty.", "$.eligiblePhases"));
        }

        if (workout.AllowedPrescriptionModes.Count == 0)
        {
            issues.Add(new ValidationIssue("WD_PRESCRIPTION_MODES_EMPTY", ValidationSeverity.Error,
                "AllowedPrescriptionModes cannot be empty.", "$.allowedPrescriptionModes"));
        }

        if (workout.AllowedDistanceAccountingModes is { Count: 0 })
        {
            issues.Add(new ValidationIssue("WD_DISTANCE_ACCOUNTING_MODES_EMPTY", ValidationSeverity.Error,
                "AllowedDistanceAccountingModes, when present, cannot be an empty list.", "$.allowedDistanceAccountingModes"));
        }

        return new ValidationResult(issues);
    }
}
