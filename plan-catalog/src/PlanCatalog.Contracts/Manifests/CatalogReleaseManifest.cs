using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;

namespace PlanCatalog.Contracts.Manifests;

public sealed record CatalogReleaseManifest
{
    public required string ReleaseKey { get; init; }
    public required string ReleaseVersion { get; init; }

    /// <summary>
    /// Not <c>required</c>: releases published before this field existed must still deserialize and
    /// verify successfully (historical verification must never break). Defaults to <c>Draft</c> —
    /// i.e. "channel unknown, do not treat as production" — for any manifest predating this field.
    /// </summary>
    public ReleaseChannel Channel { get; init; } = ReleaseChannel.Draft;

    public required IReadOnlyList<CatalogArtifactReference> Artifacts { get; init; }
    public required IReadOnlyList<CatalogArtifactReference> Bundles { get; init; }

    /// <summary>Empty unless <c>Channel</c> is non-production and unconfirmed content was explicitly allowed.</summary>
    public IReadOnlyList<UnconfirmedContentWarning> UnconfirmedContentWarnings { get; init; } = [];

    public required string ManifestContentHash { get; init; }
}
