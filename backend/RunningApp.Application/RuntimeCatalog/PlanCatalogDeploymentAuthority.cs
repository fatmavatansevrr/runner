using System.Text.Json;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;

namespace RunningApp.Application.RuntimeCatalog;

public enum PlanCatalogRootSource
{
    ExplicitConfiguration,
    PackagedContentRoot,
    DevelopmentRepositoryFallback,
}

public sealed record PlanCatalogRootResolution(string CatalogRootPath, PlanCatalogRootSource Source);

/// <summary>
/// Stable deployment-time authority for the runtime catalog. Resolution never
/// uses the process current directory: relative configuration is rooted at the
/// application content root, then the packaged catalog is preferred, with a
/// repository fallback allowed only in Development.
/// </summary>
public static class PlanCatalogRootResolver
{
    public static PlanCatalogRootResolution Resolve(
        string? configuredPath,
        string contentRootPath,
        bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(contentRootPath))
            throw new InvalidOperationException("Application content root is required to resolve the plan catalog.");

        var contentRoot = Path.GetFullPath(contentRootPath);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var explicitPath = Path.IsPathRooted(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(Path.Combine(contentRoot, configuredPath));
            return new(explicitPath, PlanCatalogRootSource.ExplicitConfiguration);
        }

        var packaged = Path.GetFullPath(Path.Combine(contentRoot, "plan-catalog", "catalog"));
        if (Directory.Exists(packaged))
            return new(packaged, PlanCatalogRootSource.PackagedContentRoot);

        if (isDevelopment)
        {
            var repository = Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "plan-catalog", "catalog"));
            if (Directory.Exists(repository))
                return new(repository, PlanCatalogRootSource.DevelopmentRepositoryFallback);
        }

        throw new InvalidOperationException(
            $"Runtime plan catalog is missing. Configure PlanCatalog:CatalogRootPath or deploy 'plan-catalog/catalog' under the application content root '{contentRoot}'.");
    }
}

public sealed record PlanCatalogPackageInventory(int JsonFileCount, IReadOnlyList<string> RelativePaths);

/// <summary>Fail-fast structural, JSON, identity, and filename-case validation.</summary>
public static class PlanCatalogPackageValidator
{
    private static readonly string[] RequiredDirectories =
    [
        "combinations", "layouts", "level-modifiers", "long-horizon-progressions",
        "policies", "preparation-runway-progressions", "progression-modifiers",
        "registries", "rule-packs", "templates", "workout-progressions", "workouts",
    ];

    public static PlanCatalogPackageInventory Validate(string catalogRootPath)
    {
        if (!Directory.Exists(catalogRootPath))
            throw new InvalidOperationException("Runtime plan catalog root does not exist.");

        var actualDirectories = Directory.EnumerateDirectories(catalogRootPath)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
        var missingDirectories = RequiredDirectories.Where(required => !actualDirectories.Contains(required)).ToArray();
        if (missingDirectories.Length > 0)
            throw new InvalidOperationException(
                $"Runtime plan catalog is incomplete or has incorrect filename casing. Missing: {string.Join(", ", missingDirectories)}.");

        var files = Directory.EnumerateFiles(catalogRootPath, "*.json", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (files.Length == 0)
            throw new InvalidOperationException("Runtime plan catalog contains no JSON artifacts.");

        var relativePaths = new List<string>(files.Length);
        var pilotFound = false;
        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var relative = Path.GetRelativePath(catalogRootPath, file).Replace('\\', '/');
            relativePaths.Add(relative);
            if (document.RootElement.TryGetProperty("metadata", out var metadata) &&
                metadata.TryGetProperty("documentType", out var type) && type.GetString() == "TEMPLATE_COMBINATION" &&
                metadata.TryGetProperty("key", out var key) && key.GetString() == V1CatalogPilotIdentityPolicy.CandidateKey &&
                metadata.TryGetProperty("version", out var version) && version.GetInt32() == V1CatalogPilotIdentityPolicy.CandidateVersion)
            {
                pilotFound = true;
            }
        }

        // Canonical deployment packages must contain the active pilot. A
        // Process-A immutable release may intentionally omit a retired pilot;
        // its signed/checksummed release manifest is the retirement authority,
        // and existing frozen plans must remain readable under that release.
        var immutableReleaseManifestExists = File.Exists(Path.Combine(catalogRootPath, "release-manifest.json"));
        if (!pilotFound && !immutableReleaseManifestExists)
            throw new InvalidOperationException(
                $"Runtime plan catalog does not contain pilot candidate '{V1CatalogPilotIdentityPolicy.CandidateKey}' v{V1CatalogPilotIdentityPolicy.CandidateVersion}.");

        var caseDuplicates = relativePaths.GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (caseDuplicates.Length > 0)
            throw new InvalidOperationException(
                $"Runtime plan catalog has case-conflicting paths: {string.Join(", ", caseDuplicates)}.");

        return new(files.Length, relativePaths);
    }
}
