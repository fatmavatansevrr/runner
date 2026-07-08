using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Core.Models;

/// <summary>The sole owner of peak-volume bands — see brief §7.9. Never copied into other artifacts.</summary>
public sealed record PeakVolumeBandPolicy
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required IReadOnlyList<PeakVolumeBandEntry> Entries { get; init; }
}
