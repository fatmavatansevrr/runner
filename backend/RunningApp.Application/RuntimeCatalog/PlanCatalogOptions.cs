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

    /// <summary>
    /// Phase 10K-FREQ.6D.4D Split E — the exact, pinned Process A release version whose published
    /// bundles (<c>plan-catalog/artifacts/appsel-plan-catalog/{this}/bundles/</c>) RunningApp reads
    /// for ProfileBacked execution-prescription consumption. Null/absent (the default, matching
    /// every deployment before this split) means no published-bundle lookup occurs at all — every
    /// candidate resolves purely Legacy, byte-identical to pre-Split-E behavior. Never "latest" —
    /// an explicit, deterministic identity only, matching this codebase's established exact-version
    /// discipline everywhere else.
    /// </summary>
    public string? PublishedBundleReleaseVersion { get; set; }
}
