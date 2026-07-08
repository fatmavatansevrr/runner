using PlanCatalog.Contracts.References;

namespace PlanCatalog.Contracts.Bundles;

public sealed record PublishedTemplateBundle
{
    public required string BundleKey { get; init; }
    public required int BundleVersion { get; init; }

    public required CatalogArtifactReference Combination { get; init; }
    public required CatalogArtifactReference MasterTemplate { get; init; }
    public required CatalogArtifactReference Layout { get; init; }
    public required CatalogArtifactReference LevelModifier { get; init; }
    public required CatalogArtifactReference WorkoutProgression { get; init; }
    public required CatalogArtifactReference ProgressionModifier { get; init; }
    public required CatalogArtifactReference RulePack { get; init; }
    public required CatalogArtifactReference RuntimeConditionValueRegistry { get; init; }
    public required CatalogArtifactReference PeakVolumeBandPolicy { get; init; }

    public required IReadOnlyList<CatalogArtifactReference> Workouts { get; init; }

    public required string BundleContentHash { get; init; }
}
