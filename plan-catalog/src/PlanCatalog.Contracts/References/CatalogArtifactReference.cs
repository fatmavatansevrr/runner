namespace PlanCatalog.Contracts.References;

public sealed record CatalogArtifactReference
{
    public required string DocumentType { get; init; }
    public required string Key { get; init; }
    public required int Version { get; init; }
    public required string ContentHash { get; init; }
}
