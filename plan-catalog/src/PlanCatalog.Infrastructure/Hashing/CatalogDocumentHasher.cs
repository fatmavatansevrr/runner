using System.Text.Json.Nodes;
using PlanCatalog.Core.Ports;
using PlanCatalog.Infrastructure.Serialization;

namespace PlanCatalog.Infrastructure.Hashing;

/// <summary>
/// Computes a document's content hash from its own canonical JSON, excluding its own hash field —
/// see brief §5 (ContentHash), §8.3 (BundleContentHash), §15 (ManifestContentHash).
/// </summary>
public static class CatalogDocumentHasher
{
    public static string ComputeContentHash<T>(ICanonicalJsonSerializer serializer, IContentHasher hasher, T document) =>
        ComputeHashExcludingField(serializer, hasher, document, "metadata", "contentHash");

    public static string ComputeHashExcludingField<T>(ICanonicalJsonSerializer serializer, IContentHasher hasher, T value, params string[] fieldPath)
    {
        var json = serializer.Serialize(value);
        var node = JsonNode.Parse(json)!.AsObject();

        JsonObject current = node;
        for (var i = 0; i < fieldPath.Length - 1; i++)
        {
            if (current.TryGetPropertyValue(fieldPath[i], out var next) && next is JsonObject nextObject)
            {
                current = nextObject;
            }
            else
            {
                current = null!;
                break;
            }
        }

        current?.Remove(fieldPath[^1]);

        var canonicalWithoutHash = node.ToJsonString(CanonicalJsonOptions.Canonical);
        return hasher.ComputeHash(canonicalWithoutHash);
    }
}
