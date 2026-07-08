using PlanCatalog.Core.Metadata;
using PlanCatalog.Contracts.References;

namespace PlanCatalog.Core.Models;

public sealed record RulePackDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required VersionedCatalogReference RuntimeConditionValueRegistry { get; init; }
    public required VersionedCatalogReference PeakVolumeBandPolicy { get; init; }

    public required IReadOnlyList<VersionedCatalogReference> Policies { get; init; }
    public required IReadOnlyList<VersionedCatalogReference> Rules { get; init; }
}
