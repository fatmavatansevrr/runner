using PlanCatalog.Contracts.Manifests;

namespace PlanCatalog.Core.Ports;

/// <summary>Reads/writes the immutable, generated <c>artifacts/</c> publish output tree.</summary>
public interface IPublishedArtifactRepository
{
    bool ReleaseExists(string releaseVersion);

    CatalogReleaseManifest ReadManifest(string releaseVersion);

    /// <summary>All release versions already published (immutable), regardless of superseded status.</summary>
    IReadOnlyList<string> ListReleaseVersions();

    /// <summary>Stages a release into a temporary directory and atomically moves it into place. Never leaves a partial release.</summary>
    void WriteRelease(string releaseVersion, CatalogReleaseManifest manifest, IReadOnlyDictionary<string, string> filesByRelativePath);
}
