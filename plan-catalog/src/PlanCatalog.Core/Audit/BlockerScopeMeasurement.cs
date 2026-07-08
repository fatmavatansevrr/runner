namespace PlanCatalog.Core.Audit;

/// <summary>
/// Milestone H — decision-level vs artifact-level blocker measurement, kept as two explicitly separate
/// units (never compared to each other) across four scopes: total-catalog, eligible-release-union,
/// candidate-root-closure, and historical-only. See
/// artifacts/audits/deterministic-graph-part2-migration.md Milestone H and
/// artifacts/audits/ten-k-pilot-domain-review-summary.md Finding 3 (the flat-number conflation this
/// measurement exists to prevent from recurring).
///
/// "Decision-level" = count of individual <see cref="DomainContentDecision"/> entries whose
/// Classification is PLACEHOLDER_UNCONFIRMED. "Artifact-level" = count of DISTINCT
/// (documentType, key, version) identities that have at least one such blocking decision. An artifact
/// with 5 blocking fields contributes 5 to the decision-level count but only 1 to the artifact-level
/// count — the two numbers measure different things and must never be compared to each other directly.
/// </summary>
public static class BlockerScopeMeasurement
{
    public readonly record struct ArtifactIdentity(string DocumentType, string Key, int Version);

    private static IEnumerable<DomainContentDecision> BlockingEntries() =>
        PilotDomainContentAudit.Entries.Where(e => e.Classification == ContentDecisionStatus.PlaceholderUnconfirmed);

    /// <summary>Every blocking decision across the entire audit list, regardless of reachability from any current bundle.</summary>
    public static int TotalCatalogPlaceholderDecisionCount() => BlockingEntries().Count();

    /// <summary>Distinct (documentType,key,version) identities carrying at least one blocking decision, across the entire audit list.</summary>
    public static int TotalCatalogBlockingArtifactCount() =>
        BlockingEntries().Select(e => new ArtifactIdentity(e.DocumentType, e.Key, e.Version)).Distinct().Count();

    /// <summary>Blocking decisions whose (documentType,key,version) identity is present in the supplied scope (e.g. an eligible-release union or a single candidate root's closure).</summary>
    public static int ScopedDecisionCount(IEnumerable<ArtifactIdentity> scope)
    {
        var scopeSet = scope.ToHashSet();
        return BlockingEntries().Count(e => scopeSet.Contains(new ArtifactIdentity(e.DocumentType, e.Key, e.Version)));
    }

    /// <summary>Distinct blocking artifact identities present in the supplied scope.</summary>
    public static int ScopedArtifactCount(IEnumerable<ArtifactIdentity> scope)
    {
        var scopeSet = scope.ToHashSet();
        return BlockingEntries()
            .Select(e => new ArtifactIdentity(e.DocumentType, e.Key, e.Version))
            .Where(scopeSet.Contains)
            .Distinct()
            .Count();
    }

    /// <summary>Blocking decisions whose identity is NOT reachable from the supplied eligible-release-union scope.</summary>
    public static int HistoricalOnlyDecisionCount(IEnumerable<ArtifactIdentity> eligibleReleaseUnionScope)
    {
        var scopeSet = eligibleReleaseUnionScope.ToHashSet();
        return BlockingEntries().Count(e => !scopeSet.Contains(new ArtifactIdentity(e.DocumentType, e.Key, e.Version)));
    }

    /// <summary>Distinct blocking artifact identities NOT reachable from the supplied eligible-release-union scope.</summary>
    public static int HistoricalOnlyArtifactCount(IEnumerable<ArtifactIdentity> eligibleReleaseUnionScope)
    {
        var scopeSet = eligibleReleaseUnionScope.ToHashSet();
        return BlockingEntries()
            .Select(e => new ArtifactIdentity(e.DocumentType, e.Key, e.Version))
            .Where(id => !scopeSet.Contains(id))
            .Distinct()
            .Count();
    }
}
