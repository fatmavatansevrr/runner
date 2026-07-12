using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RunningApp.Application.RuntimeCatalog.Resolvers;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.2 — verifies the integrity of a stored
/// <see cref="CatalogPreviewSnapshot"/> by recomputing its SHA-256 content
/// hash using the same deterministic mechanism used by
/// <see cref="CatalogPreviewSnapshotBuilder.Build"/> at preview-creation
/// time and comparing it against <see cref="CatalogPreviewSnapshot.ContentHash"/>.
///
/// Pure static class — no constructor, no dependencies, no I/O.
/// Reuses the exact same hashing approach (anonymous-object serialization
/// via <c>HashSerializerOptions</c> → SHA-256 → lowercase hex) established
/// in Phase 4E.1. No second hashing or canonicalization format is introduced.
///
/// If this method returns <c>false</c>, the snapshot has been corrupted or
/// tampered with after creation. Confirm must throw
/// <see cref="Exceptions.PlanPreviewIntegrityFailedException"/> immediately
/// and must not attempt to repair, normalize, or regenerate the snapshot.
/// </summary>
public static class CatalogPreviewSnapshotVerifier
{
    /// <summary>
    /// Same options as <c>CatalogPreviewSnapshotBuilder.HashSerializerOptions</c>:
    /// no indentation, default (PascalCase) property naming — the only policy
    /// that was in effect when the hash was originally computed.
    /// </summary>
    private static readonly JsonSerializerOptions HashSerializerOptions = new()
    {
        WriteIndented = false,
    };

    /// <summary>
    /// Returns <c>true</c> if the SHA-256 of the snapshot's canonical
    /// hashable content matches <see cref="CatalogPreviewSnapshot.ContentHash"/>;
    /// <c>false</c> otherwise.
    ///
    /// The hashable content is the same anonymous object used by
    /// <see cref="CatalogPreviewSnapshotBuilder.Build"/>: all fields except
    /// <c>ContentHash</c> itself (a hash cannot include itself),
    /// <c>DecisionTrace</c> (internal-only, excluded from the hash at
    /// generation time per the builder's own design), and
    /// <c>SelectedStageKeys</c>/<c>FallbackStagesUsed</c>/
    /// <c>GeneratedPreviewPlanPayload</c> (not included in the original
    /// hashable payload — the builder only hashes the inputs, not the
    /// Stage/Payload fields that were always empty/null in Phase 4E.1).
    /// </summary>
    /// <param name="snapshot">
    /// The deserialized snapshot. Must not be null. All required fields
    /// (<c>NormalizedInput</c>, <c>AsOfDate</c>, <c>CandidateKey</c>,
    /// <c>CandidateVersion</c>, <c>CandidateStatusAtGenerationTime</c>,
    /// <c>ReferencedArtifacts</c>, <c>GenerationSource</c>,
    /// <c>RouteReason</c>, <c>ResolverResults</c>,
    /// <c>CreatedAtUtc</c>, <c>ExpiresAtUtc</c>,
    /// <c>ContentHash</c>) must be non-null; the caller is responsible for
    /// validating schema completeness before calling this method.
    /// </param>
    public static bool Verify(CatalogPreviewSnapshot snapshot)
    {
        // Mirror the exact anonymous object from CatalogPreviewSnapshotBuilder.Build.
        // Any divergence here would make every stored hash unverifiable.
        var hashableContent = new
        {
            normalizedInput = snapshot.NormalizedInput,
            asOfDate = snapshot.AsOfDate,
            CandidateKey = snapshot.CandidateKey,
            CandidateVersion = snapshot.CandidateVersion,
            CandidateStatusAtGenerationTime = snapshot.CandidateStatusAtGenerationTime,
            referencedArtifacts = snapshot.ReferencedArtifacts,
            GenerationSource = snapshot.GenerationSource,
            routeReason = snapshot.RouteReason,
            resolverResults = snapshot.ResolverResults.Select(r => new
            {
                r.ConditionType,
                Status = r.Status.ToString(),
                r.OutputValue,
                r.ReasonCode,
                r.Metadata
            }),
            createdAtUtc = snapshot.CreatedAtUtc,
            expiresAtUtc = snapshot.ExpiresAtUtc,
        };

        var json = JsonSerializer.Serialize(hashableContent, HashSerializerOptions);
        var recomputed = ComputeSha256Hex(json);
        return string.Equals(recomputed, snapshot.ContentHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256Hex(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
