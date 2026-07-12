using PlanCatalog.Core.Models;
using PlanCatalog.Contracts.Enums;

namespace PlanCatalog.Core.Validation;

public static class WorkoutDefinitionValidator
{
    private static readonly IReadOnlySet<WorkoutComponentType> SharedComponentVocabulary = new HashSet<WorkoutComponentType>
    {
        WorkoutComponentType.WarmUp,
        WorkoutComponentType.MainSet,
        WorkoutComponentType.Recovery,
        WorkoutComponentType.CoolDown
    };

    public static ValidationResult Validate(WorkoutDefinition workout)
    {
        var issues = new List<ValidationIssue>();

        if (workout.Metadata.SchemaVersion >= 3)
        {
            if (workout.ComplexityTier is not null)
            {
                issues.Add(new ValidationIssue("LEGACY_COMPLEXITY_TIER_NOT_ALLOWED_IN_NEW_SCHEMA", ValidationSeverity.Error,
                    "complexityTier is a legacy field and must be omitted from WorkoutDefinition schemaVersion 3+.", "$.complexityTier"));
            }
        }
        else if (workout.ComplexityTier is null or < 1)
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

        ValidateComponents(workout, issues);

        return new ValidationResult(issues);
    }

    private static void ValidateComponents(WorkoutDefinition workout, List<ValidationIssue> issues)
    {
        if (workout.Components is { Count: 0 })
        {
            issues.Add(new ValidationIssue("COMPONENTS_EMPTY", ValidationSeverity.Error,
                "Components, when present, cannot be an empty list.", "$.components"));
        }

        if (workout.Components is not null)
        {
            foreach (var component in workout.Components)
            {
                if (!SharedComponentVocabulary.Contains(component.ComponentType))
                {
                    issues.Add(new ValidationIssue("COMPONENT_TYPE_NOT_ALLOWED", ValidationSeverity.Error,
                        $"ComponentType {component.ComponentType} is not in the shared Wave 2 vocabulary.", "$.components"));
                }
            }

            var componentTypes = workout.Components.Select(c => c.ComponentType).ToList();
            if (componentTypes.Distinct().Count() != componentTypes.Count)
            {
                issues.Add(new ValidationIssue("COMPONENT_SEQUENCE_INVALID", ValidationSeverity.Error,
                    "Duplicate component types are not valid in reusable WorkoutDefinition structural components.", "$.components"));
            }
        }

        switch (workout.Metadata.Key)
        {
            case "EASY_STANDARD":
            case "LONG_RUN_STANDARD":
                if (workout.Metadata.SchemaVersion >= 2 && workout.Components is not null)
                {
                    issues.Add(new ValidationIssue("COMPONENTS_NOT_ALLOWED_FOR_CONTINUOUS_WORKOUT", ValidationSeverity.Error,
                        $"{workout.Metadata.Key} is a continuous workout in schemaVersion 2+ and must omit components.", "$.components"));
                }
                break;
            case "FARTLEK":
                if (workout.Metadata.SchemaVersion >= 2)
                {
                    RequireExactComponents(workout, [WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.Recovery, WorkoutComponentType.CoolDown], issues);
                }
                break;
            case "THRESHOLD_TEMPO":
                if (workout.Metadata.SchemaVersion >= 2)
                {
                    RequireExactComponents(workout, [WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.CoolDown], issues);
                    if (workout.Components?.Any(c => c.ComponentType == WorkoutComponentType.Recovery) == true)
                    {
                        issues.Add(new ValidationIssue("COMPONENT_TYPE_NOT_ALLOWED", ValidationSeverity.Error,
                            "THRESHOLD_TEMPO represents a continuous-tempo format and must not include RECOVERY.", "$.components"));
                    }
                }
                break;
        }
    }

    private static void RequireExactComponents(
        WorkoutDefinition workout,
        IReadOnlyList<WorkoutComponentType> expected,
        List<ValidationIssue> issues)
    {
        if (workout.Components is null)
        {
            issues.Add(new ValidationIssue("COMPONENTS_REQUIRED_FOR_STRUCTURED_WORKOUT", ValidationSeverity.Error,
                $"{workout.Metadata.Key} requires structural components.", "$.components"));
            return;
        }

        var actual = workout.Components.Select(c => c.ComponentType).ToList();
        if (!actual.SequenceEqual(expected))
        {
            issues.Add(new ValidationIssue("COMPONENT_SEQUENCE_INVALID", ValidationSeverity.Error,
                $"{workout.Metadata.Key} components must be exactly {string.Join(", ", expected)} in authored array order.", "$.components"));
        }
    }
}
