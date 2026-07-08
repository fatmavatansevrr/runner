using PlanCatalog.Core.Catalog;

namespace PlanCatalog.Core.Validation;

/// <summary>
/// Source-integrity check enforcing schema-version-exclusive field shape for the three documents that
/// gained an exact-versioned successor field in the deterministic-graph migration (Part 2) — see
/// artifacts/audits/exact-workout-reference-migration.md and rule-pack-ownership-audit.md.
///
/// Rule, identical for all three: schemaVersion 1 documents must populate the legacy (key-only) field and
/// must NOT populate the new (exact-versioned) field; schemaVersion >= 2 documents must populate the new
/// field and must NOT populate the legacy field. A document that sets both, or neither, fails.
/// </summary>
public static class SchemaVersionShapeValidator
{
    public static ValidationResult Validate(CatalogSourceSnapshot snapshot)
    {
        var issues = new List<ValidationIssue>();

        foreach (var progression in snapshot.WorkoutProgressions)
        {
            foreach (var phase in progression.PhaseProgressions)
            {
                foreach (var stage in phase.Stages)
                {
                    CheckShape(
                        progression.Metadata.SchemaVersion,
                        legacyPresent: stage.WorkoutCandidateKeys is not null,
                        newPresent: stage.WorkoutCandidates is not null,
                        documentLabel: $"WORKOUT_PROGRESSION/{progression.Metadata.Key} v{progression.Metadata.Version}",
                        jsonPath: $"$.phaseProgressions[{phase.PhaseKey}].stages[{stage.StageKey}]",
                        legacyFieldName: "workoutCandidateKeys",
                        newFieldName: "workoutCandidates",
                        issues);
                }
            }
        }

        foreach (var levelModifier in snapshot.LevelModifiers)
        {
            CheckShape(
                levelModifier.Metadata.SchemaVersion,
                legacyPresent: levelModifier.EligibleWorkoutKeys is not null,
                newPresent: levelModifier.EligibleWorkouts is not null,
                documentLabel: $"LEVEL_MODIFIER/{levelModifier.Metadata.Key} v{levelModifier.Metadata.Version}",
                jsonPath: "$",
                legacyFieldName: "eligibleWorkoutKeys",
                newFieldName: "eligibleWorkouts",
                issues);
        }

        foreach (var template in snapshot.PlanTemplates)
        {
            CheckShape(
                template.Metadata.SchemaVersion,
                legacyPresent: template.RequiredRules is not null,
                newPresent: template.RequiredRuleKeys is not null,
                documentLabel: $"PLAN_TEMPLATE/{template.Metadata.Key} v{template.Metadata.Version}",
                jsonPath: "$",
                legacyFieldName: "requiredRules",
                newFieldName: "requiredRuleKeys",
                issues);
        }

        return new ValidationResult(issues);
    }

    private static void CheckShape(
        int schemaVersion,
        bool legacyPresent,
        bool newPresent,
        string documentLabel,
        string jsonPath,
        string legacyFieldName,
        string newFieldName,
        List<ValidationIssue> issues)
    {
        if (legacyPresent && newPresent)
        {
            issues.Add(new ValidationIssue("SCHEMA_SHAPE_BOTH_FORMS_PRESENT", ValidationSeverity.Error,
                $"{documentLabel} declares both legacy '{legacyFieldName}' and new '{newFieldName}' — exactly one is required.", jsonPath));
            return;
        }

        if (!legacyPresent && !newPresent)
        {
            issues.Add(new ValidationIssue("SCHEMA_SHAPE_NO_FORM_PRESENT", ValidationSeverity.Error,
                $"{documentLabel} declares neither legacy '{legacyFieldName}' nor new '{newFieldName}'.", jsonPath));
            return;
        }

        if (schemaVersion == 1 && !legacyPresent)
        {
            issues.Add(new ValidationIssue("SCHEMA_SHAPE_LEGACY_SCHEMA_REQUIRES_LEGACY_FIELD", ValidationSeverity.Error,
                $"{documentLabel} declares schemaVersion 1 but uses new field '{newFieldName}' instead of legacy '{legacyFieldName}'.", jsonPath));
        }
        else if (schemaVersion >= 2 && !newPresent)
        {
            issues.Add(new ValidationIssue("SCHEMA_SHAPE_NEW_SCHEMA_REQUIRES_NEW_FIELD", ValidationSeverity.Error,
                $"{documentLabel} declares schemaVersion {schemaVersion} but uses legacy field '{legacyFieldName}' instead of new '{newFieldName}'.", jsonPath));
        }
    }
}
