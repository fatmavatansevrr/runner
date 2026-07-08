namespace PlanCatalog.Contracts;

/// <summary>Stable, UPPER_SNAKE_CASE documentType discriminators. Never a CLR or assembly-qualified name.</summary>
public static class DocumentTypes
{
    public const string PlanTemplate = "PLAN_TEMPLATE";
    public const string RunLayout = "RUN_LAYOUT";
    public const string LevelModifier = "LEVEL_MODIFIER";
    public const string WorkoutProgression = "WORKOUT_PROGRESSION";
    public const string ProgressionModifier = "PROGRESSION_MODIFIER";
    public const string WorkoutDefinition = "WORKOUT_DEFINITION";
    public const string RuntimeConditionValueRegistry = "RUNTIME_CONDITION_VALUE_REGISTRY";
    public const string PeakVolumeBandPolicy = "PEAK_VOLUME_BAND_POLICY";
    public const string RulePack = "RULE_PACK";
    public const string TemplateCombination = "TEMPLATE_COMBINATION";
    public const string PublishedTemplateBundle = "PUBLISHED_TEMPLATE_BUNDLE";
    public const string CatalogReleaseManifest = "CATALOG_RELEASE_MANIFEST";
}
