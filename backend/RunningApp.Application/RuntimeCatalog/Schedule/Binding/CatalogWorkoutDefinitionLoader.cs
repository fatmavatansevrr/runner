using System.Text.Json;
using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Binding;

/// <summary>Backend Integration Phase 4F.6B — a read-only parse of a Process A WORKOUT_DEFINITION document, just enough to validate a binding (family, eligible phases, allowed prescription modes, lifecycle status). Carries no components/complexity-tier detail — those remain Phase 4F.7 concerns.</summary>
public sealed class CatalogWorkoutDefinitionSummary
{
    public required string Key { get; init; }
    public required int Version { get; init; }
    public required string Status { get; init; }
    public required string Family { get; init; }
    public required IReadOnlyList<string> EligiblePhases { get; init; }
    public required IReadOnlyList<string> AllowedPrescriptionModes { get; init; }
    public required IReadOnlyList<string> AllowedDistanceAccountingModes { get; init; }
    public required IReadOnlyList<CatalogWorkoutComponentSummary> Components { get; init; }
}

public sealed record CatalogWorkoutComponentSummary(int SequenceOrder, string ComponentType, string IntensityDescriptor);

/// <summary>
/// Backend Integration Phase 4F.6B — read-only loader for a Process A WORKOUT_DEFINITION
/// document. New, small, single-purpose interface, mirroring
/// <see cref="Schedule.Progression.ICatalogWorkoutProgressionLoader"/>'s own established
/// pattern rather than growing <see cref="IPlanCatalogBundleLoader"/>.
/// </summary>
public interface ICatalogWorkoutDefinitionLoader
{
    Task<CatalogWorkoutDefinitionSummary> LoadAsync(PlanCatalogReference reference, CancellationToken ct = default);
}

/// <inheritdoc cref="ICatalogWorkoutDefinitionLoader"/>
public sealed class CatalogWorkoutDefinitionLoader : ICatalogWorkoutDefinitionLoader
{
    private readonly PlanCatalogOptions _options;

    public CatalogWorkoutDefinitionLoader(IOptions<PlanCatalogOptions> options)
    {
        _options = options.Value;
    }

    public async Task<CatalogWorkoutDefinitionSummary> LoadAsync(PlanCatalogReference reference, CancellationToken ct = default)
    {
        var root = _options.CatalogRootPath;
        if (!Directory.Exists(root))
        {
            throw new PlanCatalogLoadException(
                $"Plan-catalog root directory was not found at '{root}' (configured via PlanCatalog:CatalogRootPath).");
        }

        using var document = await FindDocumentAsync(root, reference.Key, reference.Version, ct);

        var status = document.RootElement.TryGetProperty("metadata", out var metadata)
            ? RequireString(metadata, "status", reference)
            : throw new PlanCatalogLoadException($"'metadata' is missing on workouts/{reference.Key} v{reference.Version}.");

        var family = RequireString(document.RootElement, "family", reference);

        var eligiblePhases = document.RootElement.TryGetProperty("eligiblePhases", out var phasesEl) && phasesEl.ValueKind == JsonValueKind.Array
            ? phasesEl.EnumerateArray().Select(p => p.GetString()!).ToList()
            : new List<string>();

        var allowedPrescriptionModes = document.RootElement.TryGetProperty("allowedPrescriptionModes", out var modesEl) && modesEl.ValueKind == JsonValueKind.Array
            ? modesEl.EnumerateArray().Select(p => p.GetString()!).ToList()
            : new List<string>();

        var allowedDistanceAccountingModes = document.RootElement.TryGetProperty("allowedDistanceAccountingModes", out var accountingEl) && accountingEl.ValueKind == JsonValueKind.Array
            ? accountingEl.EnumerateArray().Select(p => p.GetString()!).ToList()
            : new List<string>();

        var components = document.RootElement.TryGetProperty("components", out var componentsEl) && componentsEl.ValueKind == JsonValueKind.Array
            ? componentsEl.EnumerateArray()
                .Select(c => new CatalogWorkoutComponentSummary(
                    RequireInt(c, "sequenceOrder", reference),
                    RequireString(c, "componentType", reference),
                    RequireString(c, "intensityDescriptor", reference)))
                .ToList()
            : new List<CatalogWorkoutComponentSummary>();

        return new CatalogWorkoutDefinitionSummary
        {
            Key = reference.Key,
            Version = reference.Version,
            Status = status,
            Family = family,
            EligiblePhases = eligiblePhases,
            AllowedPrescriptionModes = allowedPrescriptionModes,
            AllowedDistanceAccountingModes = allowedDistanceAccountingModes,
            Components = components,
        };
    }

    private static async Task<JsonDocument> FindDocumentAsync(string catalogRoot, string expectedKey, int expectedVersion, CancellationToken ct)
    {
        const string subfolder = "workouts";
        const string expectedDocumentType = "WORKOUT_DEFINITION";

        var directory = Path.Combine(catalogRoot, subfolder);
        if (!Directory.Exists(directory))
        {
            throw new PlanCatalogLoadException($"Catalog subdirectory '{subfolder}' was not found under '{catalogRoot}'.");
        }

        var candidates = new List<(string File, string ActualKey, int ActualVersion)>();

        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            JsonDocument document;
            try
            {
                await using var stream = File.OpenRead(file);
                document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            }
            catch (JsonException ex)
            {
                throw new PlanCatalogLoadException($"'{file}' contains invalid JSON and could not be parsed.", ex);
            }

            if (!document.RootElement.TryGetProperty("metadata", out var metadata))
            {
                document.Dispose();
                throw new PlanCatalogLoadException($"'{file}' is missing the required 'metadata' object.");
            }

            var documentType = RequireString(metadata, "documentType", new PlanCatalogReference(expectedKey, expectedVersion));
            var key = RequireString(metadata, "key", new PlanCatalogReference(expectedKey, expectedVersion));
            var version = RequireInt(metadata, "version", new PlanCatalogReference(expectedKey, expectedVersion));

            if (documentType == expectedDocumentType && key == expectedKey && version == expectedVersion)
            {
                return document;
            }

            candidates.Add((file, key, version));
            document.Dispose();
        }

        throw new PlanCatalogLoadException(
            $"No {expectedDocumentType} document with key='{expectedKey}' version={expectedVersion} was found under " +
            $"'{directory}'. Found {candidates.Count} other document(s) in that folder " +
            $"({string.Join(", ", candidates.Select(c => $"{c.ActualKey} v{c.ActualVersion}"))}).");
    }

    private static string RequireString(JsonElement element, string propertyName, PlanCatalogReference owner)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString()!;
        }

        throw new PlanCatalogLoadException(
            $"Required string field '{propertyName}' is missing on workouts/{owner.Key} v{owner.Version} (or a nested element within it).");
    }

    private static int RequireInt(JsonElement element, string propertyName, PlanCatalogReference owner)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.GetInt32();
        }

        throw new PlanCatalogLoadException(
            $"Required integer field '{propertyName}' is missing on workouts/{owner.Key} v{owner.Version} (or a nested element within it).");
    }
}
