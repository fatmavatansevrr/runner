using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RunningApp.Application.Exceptions;

namespace RunningApp.Application.RuntimeCatalog;

/// <summary>
/// Resolves a versioned catalog artifact from either the mutable authoring
/// source layout or an immutable published-release layout. A
/// <c>release-manifest.json</c> at the configured root is the explicit layout
/// marker; resolution never guesses by trying alternate filenames.
/// </summary>
internal static class CatalogArtifactFileResolver
{
    private const string ReleaseManifestFile = "release-manifest.json";
    private const string ChecksumsFile = "checksums.sha256";

    public static async Task<JsonDocument> LoadAsync(
        string catalogRoot,
        string subfolder,
        string documentType,
        string key,
        int version,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(catalogRoot) || !Directory.Exists(catalogRoot))
        {
            throw new PlanCatalogLoadException(
                $"Plan-catalog root directory was not found at '{catalogRoot}' (configured via PlanCatalog:CatalogRootPath).");
        }

        var manifestPath = Path.Combine(catalogRoot, ReleaseManifestFile);
        return File.Exists(manifestPath)
            ? await LoadPublishedAsync(catalogRoot, manifestPath, subfolder, documentType, key, version, ct)
            : await LoadAuthoringAsync(catalogRoot, subfolder, documentType, key, version, ct);
    }

    private static async Task<JsonDocument> LoadPublishedAsync(
        string root,
        string manifestPath,
        string subfolder,
        string documentType,
        string key,
        int version,
        CancellationToken ct)
    {
        using var manifest = await ParseAsync(manifestPath, ct);
        if (!manifest.RootElement.TryGetProperty("artifacts", out var artifacts) ||
            artifacts.ValueKind != JsonValueKind.Array)
        {
            throw new PlanCatalogLoadException($"Published catalog manifest '{manifestPath}' has no artifacts array.");
        }

        var listed = artifacts.EnumerateArray().Any(artifact =>
            ReadString(artifact, "documentType") == documentType &&
            ReadString(artifact, "key") == key &&
            ReadInt(artifact, "version") == version);
        if (!listed)
        {
            throw new PlanCatalogLoadException(
                $"Published catalog manifest does not contain {documentType} '{key}' v{version}.");
        }

        var relativePath = $"{subfolder}/{key}.v{version}.json";
        var path = Path.Combine(root, subfolder, $"{key}.v{version}.json");
        if (!File.Exists(path))
        {
            throw new PlanCatalogLoadException(
                $"Published catalog manifest lists {documentType} '{key}' v{version}, but '{relativePath}' is missing.");
        }

        await VerifyChecksumAsync(root, relativePath, path, ct);
        var document = await ParseAsync(path, ct);
        VerifyIdentity(document, path, documentType, key, version);
        return document;
    }

    private static async Task<JsonDocument> LoadAuthoringAsync(
        string root,
        string subfolder,
        string documentType,
        string key,
        int version,
        CancellationToken ct)
    {
        var directory = Path.Combine(root, subfolder);
        if (!Directory.Exists(directory))
        {
            throw new PlanCatalogLoadException($"Catalog subdirectory '{subfolder}' was not found under '{root}'.");
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            var document = await ParseAsync(path, ct);
            if (Matches(document, documentType, key, version))
            {
                return document;
            }

            document.Dispose();
        }

        throw new PlanCatalogLoadException(
            $"No {documentType} document matching '{key}' v{version} was found under '{directory}'.");
    }

    private static bool Matches(JsonDocument document, string documentType, string key, int version)
    {
        if (!document.RootElement.TryGetProperty("metadata", out var metadata))
        {
            return false;
        }

        return ReadString(metadata, "documentType") == documentType &&
               ReadString(metadata, "key") == key &&
               ReadInt(metadata, "version") == version;
    }

    private static void VerifyIdentity(JsonDocument document, string path, string documentType, string key, int version)
    {
        if (!Matches(document, documentType, key, version))
        {
            document.Dispose();
            throw new PlanCatalogLoadException(
                $"Published catalog artifact identity mismatch at '{path}'. Expected {documentType} '{key}' v{version}.");
        }
    }

    private static async Task VerifyChecksumAsync(string root, string relativePath, string artifactPath, CancellationToken ct)
    {
        var checksumsPath = Path.Combine(root, ChecksumsFile);
        if (!File.Exists(checksumsPath))
        {
            throw new PlanCatalogLoadException($"Published catalog root '{root}' is missing '{ChecksumsFile}'.");
        }

        var normalized = relativePath.Replace('\\', '/');
        var expected = (await File.ReadAllLinesAsync(checksumsPath, ct))
            .Select(line => line.Split("  ", 2, StringSplitOptions.None))
            .FirstOrDefault(parts => parts.Length == 2 && parts[1].Replace('\\', '/') == normalized)?[0];
        if (string.IsNullOrWhiteSpace(expected))
        {
            throw new PlanCatalogLoadException($"Published catalog checksum index does not contain '{normalized}'.");
        }

        var content = await File.ReadAllTextAsync(artifactPath, ct);
        var actual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
        {
            throw new PlanCatalogLoadException(
                $"Published catalog artifact '{normalized}' failed checksum verification.");
        }
    }

    private static async Task<JsonDocument> ParseAsync(string path, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            throw new PlanCatalogLoadException($"Catalog artifact '{path}' contains invalid JSON.", ex);
        }
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;
}
