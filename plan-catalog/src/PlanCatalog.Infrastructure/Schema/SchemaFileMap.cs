using PlanCatalog.Contracts;

namespace PlanCatalog.Infrastructure.Schema;

public static class SchemaFileMap
{
    public static readonly IReadOnlyDictionary<string, string> ByDocumentType = new Dictionary<string, string>
    {
        [DocumentTypes.PlanTemplate] = "plan-template.schema.json",
        [DocumentTypes.RunLayout] = "run-layout.schema.json",
        [DocumentTypes.LevelModifier] = "level-modifier.schema.json",
        [DocumentTypes.WorkoutProgression] = "workout-progression.schema.json",
        [DocumentTypes.ProgressionModifier] = "progression-modifier.schema.json",
        [DocumentTypes.WorkoutDefinition] = "workout-definition.schema.json",
        [DocumentTypes.RuntimeConditionValueRegistry] = "runtime-condition-value-registry.schema.json",
        [DocumentTypes.PeakVolumeBandPolicy] = "peak-volume-band-policy.schema.json",
        [DocumentTypes.RulePack] = "rule-pack.schema.json",
        [DocumentTypes.TemplateCombination] = "template-combination.schema.json",
        [DocumentTypes.PublishedTemplateBundle] = "published-template-bundle.schema.json"
    };
}
