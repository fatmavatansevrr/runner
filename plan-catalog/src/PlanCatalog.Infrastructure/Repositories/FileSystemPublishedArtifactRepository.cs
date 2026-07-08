using PlanCatalog.Contracts.Manifests;
using PlanCatalog.Core.Ports;
using PlanCatalog.Infrastructure.Serialization;

namespace PlanCatalog.Infrastructure.Repositories;

/// <summary>
/// Reads/writes the immutable, generated <c>artifacts/</c> tree. Writes are staged into a temporary
/// directory and atomically moved into place — a failed publish never leaves a partial release. See brief §16.2.
/// </summary>
public sealed class FileSystemPublishedArtifactRepository(string artifactsRootDirectory) : IPublishedArtifactRepository
{
    private const string ReleaseFamily = "appsel-plan-catalog";

    public bool ReleaseExists(string releaseVersion) => Directory.Exists(ReleaseDirectory(releaseVersion));

    public CatalogReleaseManifest ReadManifest(string releaseVersion)
    {
        var manifestPath = Path.Combine(ReleaseDirectory(releaseVersion), "release-manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"No release manifest found for release '{releaseVersion}'.", manifestPath);
        }

        var json = File.ReadAllText(manifestPath);
        return System.Text.Json.JsonSerializer.Deserialize<CatalogReleaseManifest>(json, CanonicalJsonOptions.Canonical)
            ?? throw new InvalidOperationException($"Failed to deserialize release manifest for '{releaseVersion}'.");
    }

    public void WriteRelease(string releaseVersion, CatalogReleaseManifest manifest, IReadOnlyDictionary<string, string> filesByRelativePath)
    {
        var finalDirectory = ReleaseDirectory(releaseVersion);
        if (Directory.Exists(finalDirectory))
        {
            throw new InvalidOperationException($"Release '{releaseVersion}' already exists and is immutable; publish a new version instead.");
        }

        var familyDirectory = Path.Combine(artifactsRootDirectory, ReleaseFamily);
        Directory.CreateDirectory(familyDirectory);

        var tempDirectory = Path.Combine(familyDirectory, $".tmp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            foreach (var (relativePath, content) in filesByRelativePath)
            {
                var fullPath = Path.Combine(tempDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, content);
            }

            Directory.Move(tempDirectory, finalDirectory);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    public IReadOnlyList<string> ListReleaseVersions()
    {
        var familyDirectory = Path.Combine(artifactsRootDirectory, ReleaseFamily);
        if (!Directory.Exists(familyDirectory))
        {
            return [];
        }

        return Directory.GetDirectories(familyDirectory)
            .Select(Path.GetFileName)
            .Where(name => name is not null && !name.StartsWith(".tmp-", StringComparison.Ordinal))
            .Select(name => name!)
            .ToList();
    }

    private string ReleaseDirectory(string releaseVersion) => Path.Combine(artifactsRootDirectory, ReleaseFamily, releaseVersion);
}
