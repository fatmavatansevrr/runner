using PlanCatalog.Core.Metadata;
using PlanCatalog.Contracts.References;

namespace PlanCatalog.Infrastructure.Publishing;

public static class CatalogArtifactReferences
{
    public static CatalogArtifactReference ToRef(CatalogDocumentMetadata metadata) => new()
    {
        DocumentType = metadata.DocumentType,
        Key = metadata.Key,
        Version = metadata.Version,
        ContentHash = metadata.ContentHash ?? throw new InvalidOperationException(
            $"'{metadata.DocumentType}/{metadata.Key}' v{metadata.Version} has no ContentHash.")
    };
}
