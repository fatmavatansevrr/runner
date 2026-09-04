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

    /// <summary>
    /// Phase 10K-GEN.29 — <c>workoutCandidates</c> is, and has always been,
    /// schema-plural (GEN.28 §7's own finding); this reader previously
    /// hard-selected only the first element (<c>.First()</c>), silently
    /// ignoring any further entry. Now reads the full array: the primary
    /// candidate is the first entry carrying no <c>"role"</c> tag (or,
    /// absent one, the first entry outright — preserving byte-identical
    /// selection for every pre-GEN.29 progression document, all of which
    /// declare a single, untagged candidate); an optional second entry
    /// tagged <c>"role": "EASY_SUPPORT"</c> is captured separately as
    /// <see cref="PreparationRunwayBlockProgressionStep.PatternBEasySupportReference"/>
    /// — the explicit, role-conditioned Pattern-B content GEN.28 §9
    /// (Candidate C) approved for 2D's Consistency/PreSpecificTransition
    /// (mechanical reuse of the block's own existing EASY_STANDARD content)
    /// and, per the GEN.29 governing decision, AerobicStrength (EASY_STANDARD,
    /// no new content authored). No other role tag is recognized — an
    /// unexpected value fails closed rather than being silently ignored.
    /// </summary>
    private static PreparationRunwayBlockProgressionStep ReadStep(JsonElement stepEl, string owningKey, int owningVersion)
    {
        var stepOrder = RequireInt(stepEl, "stepOrder", owningKey, owningVersion);

        if (!stepEl.TryGetProperty("workoutCandidates", out var candidatesEl) || candidatesEl.ValueKind != JsonValueKind.Array || candidatesEl.GetArrayLength() == 0)
        {
            throw new PlanCatalogLoadException($"Step {stepOrder} has no workoutCandidates on preparation-runway-progressions/{owningKey} v{owningVersion}.");
        }

        var candidates = candidatesEl.EnumerateArray().ToArray();
        JsonElement? untaggedCandidate = null;
        foreach (var candidate in candidates)
        {
            if (!TryReadRole(candidate, out _))
            {
                untaggedCandidate = candidate;
                break;
            }
        }
        var primaryCandidate = untaggedCandidate ?? candidates[0];
        var workoutId = RequireString(primaryCandidate, "key", owningKey, owningVersion);
        var workoutVersion = RequireInt(primaryCandidate, "version", owningKey, owningVersion);

        PreparationRunwayWorkoutReference? patternBEasySupportReference = null;
        foreach (var candidate in candidates)
        {
            if (!TryReadRole(candidate, out var role))
                continue;

            if (role != "EASY_SUPPORT")
                throw new PlanCatalogLoadException(
                    $"Step {stepOrder} on preparation-runway-progressions/{owningKey} v{owningVersion} declares an unrecognized workoutCandidates role '{role}' (only 'EASY_SUPPORT' is supported).");

            patternBEasySupportReference = new PreparationRunwayWorkoutReference(
                RequireString(candidate, "key", owningKey, owningVersion),
                RequireInt(candidate, "version", owningKey, owningVersion));
        }

        return new PreparationRunwayBlockProgressionStep(stepOrder, workoutId, workoutVersion, patternBEasySupportReference);
    }

    private static bool TryReadRole(JsonElement candidateEl, out string role)
    {
        if (candidateEl.TryGetProperty("role", out var roleEl) && roleEl.ValueKind == JsonValueKind.String)
        {
            role = roleEl.GetString()!;
            return true;
        }

        role = string.Empty;
        return false;
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
