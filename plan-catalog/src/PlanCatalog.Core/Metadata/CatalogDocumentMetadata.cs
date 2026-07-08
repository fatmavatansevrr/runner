using PlanCatalog.Core.Enums;

namespace PlanCatalog.Core.Metadata;

/// <summary>
/// Authoring-time document metadata, including draft lifecycle state (<see cref="CatalogStatus"/>).
/// This is a Core (authoring) concept, not a published-boundary type — Process B only ever sees the
/// trimmed <c>VersionedCatalogReference</c>/<c>CatalogArtifactReference</c> shapes from Contracts.
/// </summary>
public sealed record CatalogDocumentMetadata
{
    public required string DocumentType { get; init; }
    public required int SchemaVersion { get; init; }

    public required string Key { get; init; }
    public required int Version { get; init; }

    public required CatalogStatus Status { get; init; }

    /// <summary>Computed at build/publish time from canonical JSON; null on draft sources.</summary>
    public string? ContentHash { get; init; }
}
