namespace RunningApp.Application.RuntimeCatalog;

/// <summary>
/// Configuration for <see cref="IPlanCatalogBundleLoader"/>. Bound from the
/// "PlanCatalog" configuration section (see appsettings.Development.json).
/// </summary>
public sealed class PlanCatalogOptions
{
    public const string SectionName = "PlanCatalog";

    /// <summary>
    /// Path to the Process A plan-catalog source directory (the "catalog/"
    /// folder inside the plan-catalog repository root, containing
    /// combinations/, templates/, layouts/, level-modifiers/,
    /// workout-progressions/, progression-modifiers/, rule-packs/,
    /// policies/, registries/, and workouts/ subfolders). May be relative
    /// to the API's working directory or absolute.
    /// </summary>
    public required string CatalogRootPath { get; set; }
}
