using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

public static class LevelModifierValidator
{
    public static ValidationResult Validate(LevelModifierDefinition levelModifier, CatalogSourceSnapshot snapshot)
    {
        var issues = new List<ValidationIssue>();

        var progressionModifier = snapshot.FindProgressionModifier(levelModifier.ProgressionModifier);
        if (progressionModifier is null)
        {
            issues.Add(new ValidationIssue("LM_PROGRESSION_MODIFIER_MISSING", ValidationSeverity.Error,
                $"Referenced ProgressionModifier '{levelModifier.ProgressionModifier.Key}' v{levelModifier.ProgressionModifier.Version} was not found.", "$.progressionModifier"));
        }
        else if (progressionModifier.Experience != levelModifier.Experience)
        {
            issues.Add(new ValidationIssue("LM_PROGRESSION_MODIFIER_EXPERIENCE_MISMATCH", ValidationSeverity.Error,
                $"ProgressionModifier experience '{progressionModifier.Experience}' does not match level modifier experience '{levelModifier.Experience}'.", "$.progressionModifier"));
        }

        if (levelModifier.EligibleWorkoutKeys is not null)
        {
            var missingWorkoutKeys = levelModifier.EligibleWorkoutKeys.Where(key => snapshot.FindWorkout(key) is null).ToList();
            if (missingWorkoutKeys.Count > 0)
            {
                issues.Add(new ValidationIssue("LM_ELIGIBLE_WORKOUT_KEY_MISSING", ValidationSeverity.Error,
                    $"Eligible workout keys not found in catalog: {string.Join(", ", missingWorkoutKeys)}.", "$.eligibleWorkoutKeys"));
            }
        }

        if (levelModifier.EligibleWorkouts is not null)
        {
            var missingExact = levelModifier.EligibleWorkouts.Where(r => snapshot.FindWorkout(r.Key, r.Version) is null).ToList();
            if (missingExact.Count > 0)
            {
                issues.Add(new ValidationIssue("LM_ELIGIBLE_WORKOUT_VERSION_MISSING", ValidationSeverity.Error,
                    $"Eligible workout references not found in catalog: {string.Join(", ", missingExact.Select(r => $"{r.Key} v{r.Version}"))}.", "$.eligibleWorkouts"));
            }
        }

        return new ValidationResult(issues);
    }
}
