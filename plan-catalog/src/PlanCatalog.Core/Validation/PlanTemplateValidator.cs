using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

public static class PlanTemplateValidator
{
    public static ValidationResult Validate(PlanTemplateDefinition template, CatalogSourceSnapshot snapshot)
    {
        var issues = new List<ValidationIssue>();

        if (template.Metadata.DocumentType != DocumentTypes.PlanTemplate)
        {
            issues.Add(new ValidationIssue("PT_DOCUMENT_TYPE_MISMATCH", ValidationSeverity.Error,
                $"Expected documentType '{DocumentTypes.PlanTemplate}' but found '{template.Metadata.DocumentType}'.", "$.metadata.documentType"));
        }

        var cycle = template.CoreCycle;
        if (!(cycle.MinimumWeeks <= cycle.DefaultWeeks && cycle.DefaultWeeks <= cycle.MaximumWeeks))
        {
            issues.Add(new ValidationIssue("PT_CORE_CYCLE_BOUNDS_INVALID", ValidationSeverity.Error,
                "CoreCycle must satisfy MinimumWeeks <= DefaultWeeks <= MaximumWeeks.", "$.coreCycle"));
        }

        var duplicatePhaseKeys = template.Phases.GroupBy(p => p.PhaseKey).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicatePhaseKeys.Count > 0)
        {
            issues.Add(new ValidationIssue("PT_DUPLICATE_PHASE_KEY", ValidationSeverity.Error,
                $"Duplicate phase keys: {string.Join(", ", duplicatePhaseKeys)}.", "$.phases"));
        }

        var taperPhases = template.Phases.Where(p => p.PhaseKey == PhaseKey.Taper).ToList();
        if (taperPhases.Count != 1)
        {
            issues.Add(new ValidationIssue("PT_TAPER_COUNT_INVALID", ValidationSeverity.Error,
                $"Exactly one TAPER phase is required; found {taperPhases.Count}.", "$.phases"));
        }
        else if (taperPhases[0].MinimumWeeks <= 0)
        {
            issues.Add(new ValidationIssue("PT_TAPER_MINIMUM_NOT_POSITIVE", ValidationSeverity.Error,
                "TAPER phase MinimumWeeks must be greater than zero.", "$.phases"));
        }

        var sumPreferred = template.Phases.Sum(p => p.PreferredWeeks);
        if (sumPreferred != cycle.DefaultWeeks)
        {
            issues.Add(new ValidationIssue("PT_PREFERRED_WEEKS_SUM_MISMATCH", ValidationSeverity.Error,
                $"sum(PreferredWeeks) = {sumPreferred} but DefaultWeeks = {cycle.DefaultWeeks}.", "$.phases"));
        }

        var sumMinimum = template.Phases.Sum(p => p.MinimumWeeks);
        if (sumMinimum > cycle.MinimumWeeks)
        {
            issues.Add(new ValidationIssue("PT_MINIMUM_WEEKS_SUM_EXCEEDS_CYCLE", ValidationSeverity.Error,
                $"sum(Phase.MinimumWeeks) = {sumMinimum} exceeds CoreCycle.MinimumWeeks = {cycle.MinimumWeeks}.", "$.phases"));
        }

        var sumMaximum = template.Phases.Sum(p => p.MaximumWeeks);
        if (sumMaximum < cycle.MaximumWeeks)
        {
            issues.Add(new ValidationIssue("PT_MAXIMUM_WEEKS_SUM_BELOW_CYCLE", ValidationSeverity.Error,
                $"sum(Phase.MaximumWeeks) = {sumMaximum} is below CoreCycle.MaximumWeeks = {cycle.MaximumWeeks}.", "$.phases"));
        }

        var progression = snapshot.FindWorkoutProgression(template.WorkoutProgression);
        if (progression is null)
        {
            issues.Add(new ValidationIssue("PT_WORKOUT_PROGRESSION_MISSING", ValidationSeverity.Error,
                $"Referenced WorkoutProgression '{template.WorkoutProgression.Key}' v{template.WorkoutProgression.Version} was not found.", "$.workoutProgression"));
        }
        else if (progression.DistanceFamily != template.DistanceFamily)
        {
            issues.Add(new ValidationIssue("PT_WORKOUT_PROGRESSION_DISTANCE_MISMATCH", ValidationSeverity.Error,
                $"WorkoutProgression distance family '{progression.DistanceFamily}' does not match template distance family '{template.DistanceFamily}'.", "$.workoutProgression"));
        }

        if (template.RequiredRules is not null)
        {
            foreach (var ruleRef in template.RequiredRules)
            {
                if (snapshot.FindRulePack(ruleRef) is null)
                {
                    issues.Add(new ValidationIssue("PT_REQUIRED_RULE_MISSING", ValidationSeverity.Error,
                        $"Required rule reference '{ruleRef.Key}' v{ruleRef.Version} was not found.", "$.requiredRules"));
                }
            }
        }

        if (template.RequiredRuleKeys is not null)
        {
            foreach (var ruleKey in template.RequiredRuleKeys)
            {
                if (!snapshot.RulePacks.Any(r => r.Metadata.Key == ruleKey))
                {
                    issues.Add(new ValidationIssue("PT_REQUIRED_RULE_KEY_UNKNOWN", ValidationSeverity.Error,
                        $"Required rule key '{ruleKey}' does not match any RulePack key present in the catalog (any version).", "$.requiredRuleKeys"));
                }
            }
        }

        return new ValidationResult(issues);
    }
}
