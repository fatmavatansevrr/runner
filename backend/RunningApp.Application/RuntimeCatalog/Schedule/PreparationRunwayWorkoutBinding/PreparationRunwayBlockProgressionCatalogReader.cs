using System.Text.Json;
using RunningApp.Application.Exceptions;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;

/// <summary>
/// Backend Integration Phase 4G.6A.4B — read-only, dark loader for a
/// PREPARATION_RUNWAY_BLOCK_PROGRESSION catalog document (Phase 4G.6A.4A),
/// mirroring the exact catalog-root resolution and document-scan convention
/// already established by <see cref="Schedule.Progression.CatalogWorkoutProgressionLoader"/>
/// and <see cref="Schedule.Binding.CatalogWorkoutDefinitionLoader"/> (reuse
/// of <see cref="CatalogArtifactFileResolver"/>, never a parallel loading
/// architecture). Deliberately NOT DI-registered and takes a plain
/// <c>catalogRoot</c> string rather than <c>IOptions&lt;PlanCatalogOptions&gt;</c>,
/// so it cannot be constructor-injected by accident -- it remains just as
/// dark as the sibling Preparation Runway allocation engine (Phase 4G.6A.3B)
/// until a future phase makes an explicit, separate decision to wire it.
/// Produces a generic-<c>string</c>-keyed <see cref="PreparationRunwayBlockProgressionDefinition{TKey}"/>
/// (the catalog document's own <c>blockType</c> field is a string); callers
/// needing a strongly-typed block key (e.g. <c>PreparationRunwayBlockType</c>)
/// convert it themselves -- this reader has no dependency on that enum.
/// </summary>
internal static class PreparationRunwayBlockProgressionCatalogReader
{
    private const string Subfolder = "preparation-runway-progressions";
    private const string DocumentType = "PREPARATION_RUNWAY_BLOCK_PROGRESSION";

    public static async Task<PreparationRunwayBlockProgressionDefinition<string>> LoadAsync(
        string catalogRoot, string key, int version, CancellationToken ct = default)
    {
        using var document = await CatalogArtifactFileResolver.LoadAsync(catalogRoot, Subfolder, DocumentType, key, version, ct);

        var blockType = RequireString(document.RootElement, "blockType", key, version);

        if (!document.RootElement.TryGetProperty("steps", out var stepsEl) || stepsEl.ValueKind != JsonValueKind.Array)
        {
            throw new PlanCatalogLoadException($"'steps' is missing on preparation-runway-progressions/{key} v{version}.");
        }

        var steps = stepsEl.EnumerateArray().Select(s => ReadStep(s, key, version)).ToList();

        return new PreparationRunwayBlockProgressionDefinition<string>(key, version, blockType, steps);
    }

    private static PreparationRunwayBlockProgressionStep ReadStep(JsonElement stepEl, string owningKey, int owningVersion)
    {
        var stepOrder = RequireInt(stepEl, "stepOrder", owningKey, owningVersion);

        if (!stepEl.TryGetProperty("workoutCandidates", out var candidatesEl) || candidatesEl.ValueKind != JsonValueKind.Array || candidatesEl.GetArrayLength() == 0)
        {
            throw new PlanCatalogLoadException($"Step {stepOrder} has no workoutCandidates on preparation-runway-progressions/{owningKey} v{owningVersion}.");
        }

        var firstCandidate = candidatesEl.EnumerateArray().First();
        var workoutId = RequireString(firstCandidate, "key", owningKey, owningVersion);
        var workoutVersion = RequireInt(firstCandidate, "version", owningKey, owningVersion);

        return new PreparationRunwayBlockProgressionStep(stepOrder, workoutId, workoutVersion);
    }

    private static string RequireString(JsonElement element, string propertyName, string owningKey, int owningVersion)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString()!;
        }

        throw new PlanCatalogLoadException(
            $"Required string field '{propertyName}' is missing on preparation-runway-progressions/{owningKey} v{owningVersion} (or a nested element within it).");
    }

    private static int RequireInt(JsonElement element, string propertyName, string owningKey, int owningVersion)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.GetInt32();
        }

        throw new PlanCatalogLoadException(
            $"Required integer field '{propertyName}' is missing on preparation-runway-progressions/{owningKey} v{owningVersion} (or a nested element within it).");
    }
}
