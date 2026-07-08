namespace PlanCatalog.Contracts.References;

public sealed record VersionedCatalogReference
{
    public required string DocumentType { get; init; }
    public required string Key { get; init; }
    public required int Version { get; init; }
}
