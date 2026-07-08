namespace PlanCatalog.Core.Ports;

/// <summary>Computes a SHA-256 content hash over canonical JSON, excluding the document's own ContentHash field.</summary>
public interface IContentHasher
{
    string ComputeHash(string canonicalJsonWithoutContentHash);
}
