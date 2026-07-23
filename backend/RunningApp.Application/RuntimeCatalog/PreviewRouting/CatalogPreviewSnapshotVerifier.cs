using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Resolvers;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.2 — verifies the integrity of a stored
/// <see cref="CatalogPreviewSnapshot"/> by recomputing its SHA-256 content
/// hash using the same deterministic mechanism used by
/// <see cref="CatalogPreviewSnapshotBuilder.Build"/> at preview-creation
/// time (both now delegate to the shared
/// <see cref="CatalogPreviewCanonicalHashSerializer"/>, Phase 4F.9.2) and
/// comparing it against <see cref="CatalogPreviewSnapshot.ContentHash"/>.
///
/// Pure static class — no constructor, no dependencies, no I/O.
///
/// If this method returns <c>false</c>, the snapshot has been corrupted or
/// tampered with after creation. Confirm must throw
/// <see cref="Exceptions.PlanPreviewIntegrityFailedException"/> immediately
/// and must not attempt to repair, normalize, or regenerate the snapshot.
/// </summary>
public static class CatalogPreviewSnapshotVerifier
{
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
        // Phase 4F.9.2: no supported verification algorithm exists for any
        // HashAlgorithmVersion other than the current one. No real (non-test)
        // snapshot was ever produced with a different version — this feature
        // has never been committed, published, or activated — so failing
        // closed here is safe and avoids permanently maintaining a second,
        // known-broken (order-dependent) hashing implementation. Callers
        // must not catch this and fall back to any other verification path.
        if (snapshot.HashAlgorithmVersion != CatalogPreviewCanonicalHashSerializer.CurrentHashAlgorithmVersion)
        {
            throw new PlanPreviewHashAlgorithmVersionUnsupportedException(
                $"Snapshot HashAlgorithmVersion={snapshot.HashAlgorithmVersion} is not supported; " +
                $"only version {CatalogPreviewCanonicalHashSerializer.CurrentHashAlgorithmVersion} can be verified.");
        }

        var recomputed = CatalogPreviewCanonicalHashSerializer.ComputeContentHash(
            snapshot.NormalizedInput,
            snapshot.AsOfDate,
            snapshot.CandidateKey,
            snapshot.CandidateVersion,
            snapshot.CandidateStatusAtGenerationTime,
            snapshot.ReferencedArtifacts,
            snapshot.GenerationSource,
            snapshot.RouteReason,
            snapshot.ResolverResults,
            snapshot.GeneratedPreviewPlanPayload,
            snapshot.CreatedAtUtc,
            snapshot.ExpiresAtUtc);

        return string.Equals(recomputed, snapshot.ContentHash, StringComparison.OrdinalIgnoreCase);
    }
}
