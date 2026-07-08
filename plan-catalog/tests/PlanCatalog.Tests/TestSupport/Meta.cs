using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Tests.TestSupport;

public static class Meta
{
    public static CatalogDocumentMetadata Of(string documentType, string key, int version = 1, CatalogStatus status = CatalogStatus.Draft) =>
        new()
        {
            DocumentType = documentType,
            SchemaVersion = 1,
            Key = key,
            Version = version,
            Status = status
        };
}
