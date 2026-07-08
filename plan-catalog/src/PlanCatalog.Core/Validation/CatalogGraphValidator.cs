using PlanCatalog.Contracts;
using PlanCatalog.Core.Catalog;

namespace PlanCatalog.Core.Validation;

/// <summary>
/// SOURCE-INTEGRITY layer (Milestone A1 of artifacts/audits/deterministic-graph-part2-migration.md).
/// Cross-catalog graph invariants that span multiple documents (uniqueness, experience collisions),
/// schema-version shape exclusivity, plus the aggregate run of every individual per-document validator.
/// Runs across ALL source artifacts, including historical and retired ones — a malformed retired artifact
/// must still fail this validation. Deliberately does NOT consult retirement eligibility (that is a
/// publish-graph concern handled by <see cref="CandidatePublishGraphValidator"/> for a specific selected,
/// non-retired root) — see deterministic-graph-prechange-assessment.md Finding 1 for why mixing the two
/// caused a retired root's retired dependency to block validation of the entire catalog.
/// </summary>
public static class CatalogGraphValidator
{
    public static ValidationResult Validate(CatalogSourceSnapshot snapshot)
    {
        var issues = new List<ValidationIssue>();

        issues.AddRange(SchemaVersionShapeValidator.Validate(snapshot).Issues);

        CheckDuplicateKeyVersion(DocumentTypes.PlanTemplate, snapshot.PlanTemplates.Select(x => x.Metadata), issues);
        CheckDuplicateKeyVersion(DocumentTypes.RunLayout, snapshot.RunLayouts.Select(x => x.Metadata), issues);
        CheckDuplicateKeyVersion(DocumentTypes.LevelModifier, snapshot.LevelModifiers.Select(x => x.Metadata), issues);
        CheckDuplicateKeyVersion(DocumentTypes.WorkoutProgression, snapshot.WorkoutProgressions.Select(x => x.Metadata), issues);
        CheckDuplicateKeyVersion(DocumentTypes.ProgressionModifier, snapshot.ProgressionModifiers.Select(x => x.Metadata), issues);
        CheckDuplicateKeyVersion(DocumentTypes.WorkoutDefinition, snapshot.Workouts.Select(x => x.Metadata), issues);
        CheckDuplicateKeyVersion(DocumentTypes.RuntimeConditionValueRegistry, snapshot.RuntimeConditionValueRegistries.Select(x => x.Metadata), issues);
        CheckDuplicateKeyVersion(DocumentTypes.PeakVolumeBandPolicy, snapshot.PeakVolumeBandPolicies.Select(x => x.Metadata), issues);
        CheckDuplicateKeyVersion(DocumentTypes.RulePack, snapshot.RulePacks.Select(x => x.Metadata), issues);
        CheckDuplicateKeyVersion(DocumentTypes.TemplateCombination, snapshot.Combinations.Select(x => x.Metadata), issues);

        // Grouped by distinct KEY (not by document/version) — multiple immutable versions of the SAME
        // LevelModifier/ProgressionModifier key legitimately share one Experience; this only flags two
        // DIFFERENT keys both claiming the same Experience, which is a genuine authoring conflict.
        var duplicateLevelModifierExperience = snapshot.LevelModifiers
            .GroupBy(x => x.Experience)
            .Where(g => g.Select(x => x.Metadata.Key).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateLevelModifierExperience.Count > 0)
        {
            issues.Add(new ValidationIssue("GRAPH_DUPLICATE_LEVEL_MODIFIER_EXPERIENCE", ValidationSeverity.Error,
                $"More than one distinct LevelModifier key declares experience: {string.Join(", ", duplicateLevelModifierExperience)}.", "$"));
        }

        var duplicateProgressionModifierExperience = snapshot.ProgressionModifiers
            .GroupBy(x => x.Experience)
            .Where(g => g.Select(x => x.Metadata.Key).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateProgressionModifierExperience.Count > 0)
        {
            issues.Add(new ValidationIssue("GRAPH_DUPLICATE_PROGRESSION_MODIFIER_EXPERIENCE", ValidationSeverity.Error,
                $"More than one distinct ProgressionModifier key declares experience: {string.Join(", ", duplicateProgressionModifierExperience)}.", "$"));
        }

        foreach (var template in snapshot.PlanTemplates)
        {
            issues.AddRange(PlanTemplateValidator.Validate(template, snapshot).Issues);
        }

        foreach (var layout in snapshot.RunLayouts)
        {
            issues.AddRange(RunLayoutValidator.Validate(layout).Issues);
        }

        foreach (var levelModifier in snapshot.LevelModifiers)
        {
            issues.AddRange(LevelModifierValidator.Validate(levelModifier, snapshot).Issues);
        }

        foreach (var progression in snapshot.WorkoutProgressions)
        {
            issues.AddRange(WorkoutProgressionValidator.Validate(progression, snapshot).Issues);
        }

        foreach (var progressionModifier in snapshot.ProgressionModifiers)
        {
            issues.AddRange(ProgressionModifierValidator.Validate(progressionModifier).Issues);
        }

        foreach (var workout in snapshot.Workouts)
        {
            issues.AddRange(WorkoutDefinitionValidator.Validate(workout).Issues);
        }

        foreach (var registry in snapshot.RuntimeConditionValueRegistries)
        {
            issues.AddRange(RuntimeConditionValueRegistryValidator.Validate(registry).Issues);
        }

        foreach (var policy in snapshot.PeakVolumeBandPolicies)
        {
            issues.AddRange(PeakVolumeBandPolicyValidator.Validate(policy).Issues);
        }

        foreach (var rulePack in snapshot.RulePacks)
        {
            issues.AddRange(RulePackValidator.Validate(rulePack, snapshot).Issues);
        }

        foreach (var combination in snapshot.Combinations)
        {
            issues.AddRange(TemplateCombinationValidator.Validate(combination, snapshot).Issues);
        }

        return new ValidationResult(issues);
    }

    private static void CheckDuplicateKeyVersion(
        string documentType,
        IEnumerable<Metadata.CatalogDocumentMetadata> metadataList,
        List<ValidationIssue> issues)
    {
        var duplicates = metadataList
            .GroupBy(m => (m.Key, m.Version))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var (key, version) in duplicates)
        {
            issues.Add(new ValidationIssue("GRAPH_DUPLICATE_KEY_VERSION", ValidationSeverity.Error,
                $"Duplicate ({documentType}, {key}, v{version}).", "$.metadata"));
        }
    }
}
