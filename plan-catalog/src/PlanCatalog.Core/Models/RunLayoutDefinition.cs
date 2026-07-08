using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Core.Models;

public sealed record RunLayoutDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }
    public required int RunsPerWeek { get; init; }
    public required IReadOnlyList<LayoutSlotDefinition> Slots { get; init; }
}
