using System.Text.Json;
using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Progression;

/// <summary>
/// Backend Integration Phase 4F.6A — read-only loader for a Process A WORKOUT_PROGRESSION
/// document's fine-grained stage content. Deliberately a new, small, single-purpose
/// interface (mirroring this repository's established pattern — see
/// <c>ICatalogPhaseAllocationResolver</c>/<c>ICatalogRunLayoutResolver</c>) rather than an
/// added method on <see cref="IPlanCatalogBundleLoader"/>: growing that interface would
/// require every existing implementer (including
/// <c>CatalogCandidateEligibilityGateTests.FakeBundleLoader</c>) to add a matching method,
/// for a capability only the stage scheduler needs. Reuses the exact same catalog-root
/// resolution and document-scan convention as <see cref="PlanCatalogBundleLoader"/> (scan
/// every *.json file under a subfolder, match by <c>metadata.documentType/key/version</c>,
/// never assume a filename convention).
/// </summary>
public interface ICatalogWorkoutProgressionLoader
{
    Task<CatalogWorkoutProgressionDefinition> LoadAsync(PlanCatalogReference reference, CancellationToken ct = default);
}

/// <inheritdoc cref="ICatalogWorkoutProgressionLoader"/>
public sealed class CatalogWorkoutProgressionLoader : ICatalogWorkoutProgressionLoader
{
    private readonly PlanCatalogOptions _options;

    public CatalogWorkoutProgressionLoader(IOptions<PlanCatalogOptions> options)
    {
        _options = options.Value;
    }

    public async Task<CatalogWorkoutProgressionDefinition> LoadAsync(PlanCatalogReference reference, CancellationToken ct = default)
    {
        var root = _options.CatalogRootPath;
        if (!Directory.Exists(root))
        {
            throw new PlanCatalogLoadException(
                $"Plan-catalog root directory was not found at '{root}' (configured via PlanCatalog:CatalogRootPath).");
        }

        using var document = await FindDocumentAsync(root, reference.Key, reference.Version, ct);

        var distanceFamily = RequireString(document.RootElement, "distanceFamily", reference);

        if (!document.RootElement.TryGetProperty("phaseProgressions", out var phaseProgressionsEl) || phaseProgressionsEl.ValueKind != JsonValueKind.Array)
        {
            throw new PlanCatalogLoadException(
                $"'phaseProgressions' is missing on workout-progressions/{reference.Key} v{reference.Version}.");
        }

        var phaseProgressions = phaseProgressionsEl.EnumerateArray()
            .Select(p => ReadPhaseProgression(p, reference))
            .ToList();

        return new CatalogWorkoutProgressionDefinition
        {
            Key = reference.Key,
            Version = reference.Version,
            DistanceFamily = distanceFamily,
            PhaseProgressions = phaseProgressions,
        };
    }

    private static CatalogPhaseWorkoutProgression ReadPhaseProgression(JsonElement phaseEl, PlanCatalogReference reference)
    {
        var phaseKey = RequireString(phaseEl, "phaseKey", reference);

        // Backend Integration Phase 10K-FREQ.6D.4D Split A: an optional "lanes" array is the
        // new, additive shape for a structural role with more than one slot per week (e.g.
        // Intermediate×5D's two KEY_SESSION lanes). When absent, the phase keeps today's bare
        // "stages" shape exactly as before — this loader change is purely additive.
        List<CatalogWorkoutProgressionLane>? lanes = null;
        if (phaseEl.TryGetProperty("lanes", out var lanesEl) && lanesEl.ValueKind == JsonValueKind.Array)
        {
            lanes = lanesEl.EnumerateArray().Select(l => ReadLane(l, phaseKey, reference)).ToList();
        }

        if (!phaseEl.TryGetProperty("stages", out var stagesEl) || stagesEl.ValueKind != JsonValueKind.Array)
        {
            if (lanes is null)
            {
                throw new PlanCatalogLoadException(
                    $"'stages' is missing on phaseProgressions[{phaseKey}] in workout-progressions/{reference.Key} v{reference.Version}.");
            }

            // A lane-authored phase may omit the legacy bare "stages" entirely — it carries
            // no independent meaning once Lanes is populated (see CatalogPhaseWorkoutProgression.EffectiveLanes).
            return new CatalogPhaseWorkoutProgression { PhaseKey = phaseKey, Stages = Array.Empty<CatalogWorkoutProgressionStage>(), Lanes = lanes };
        }

        var stages = stagesEl.EnumerateArray().Select(s => ReadStage(s, phaseKey, reference)).ToList();

        return new CatalogPhaseWorkoutProgression { PhaseKey = phaseKey, Stages = stages, Lanes = lanes };
    }

    private static CatalogWorkoutProgressionLane ReadLane(JsonElement laneEl, string phaseKey, PlanCatalogReference reference)
    {
        var laneOrdinal = RequireInt(laneEl, "laneOrdinal", reference);

        if (!laneEl.TryGetProperty("stages", out var stagesEl) || stagesEl.ValueKind != JsonValueKind.Array)
        {
            throw new PlanCatalogLoadException(
                $"'stages' is missing on phaseProgressions[{phaseKey}].lanes[{laneOrdinal}] in workout-progressions/{reference.Key} v{reference.Version}.");
        }

        var stages = stagesEl.EnumerateArray().Select(s => ReadStage(s, phaseKey, reference)).ToList();

        return new CatalogWorkoutProgressionLane { LaneOrdinal = laneOrdinal, Stages = stages };
    }

    private static CatalogWorkoutProgressionStage ReadStage(JsonElement stageEl, string phaseKey, PlanCatalogReference reference)
    {
        var stageKey = RequireString(stageEl, "stageKey", reference);
        var relativeOrder = RequireInt(stageEl, "relativeOrder", reference);
        var minimumExposures = RequireInt(stageEl, "minimumExposures", reference);
        var maximumExposures = RequireInt(stageEl, "maximumExposures", reference);
        var compressionBehavior = ParseCompressionBehavior(RequireString(stageEl, "compressionBehavior", reference), stageKey, reference);
        var extensionBehavior = ParseExtensionBehavior(RequireString(stageEl, "extensionBehavior", reference), stageKey, reference);

        var requires = stageEl.TryGetProperty("requires", out var requiresEl) && requiresEl.ValueKind == JsonValueKind.Array
            ? requiresEl.EnumerateArray().Select(r => ReadCondition(r, stageKey, reference)).ToList()
            : new List<CatalogRuntimeEligibilityCondition>();

        string? fallbackStageKey = stageEl.TryGetProperty("fallbackStageKey", out var fallbackEl) && fallbackEl.ValueKind == JsonValueKind.String
            ? fallbackEl.GetString()
            : null;

        // Backend Integration Phase 4F.6B: schemaVersion>=2 exact-reference shape only — the
        // current v10 artifact uses exactly this shape (see
        // artifacts/audits/exact-workout-reference-migration.md). The legacy schemaVersion 1
        // workoutCandidateKeys (bare, unversioned) shape is not read here; Phase 4F.6A never
        // needed workout identity at all, and no legacy-shape workout progression exists in
        // this pilot's dependency graph.
        var workoutCandidateReferences = stageEl.TryGetProperty("workoutCandidates", out var candidatesEl) && candidatesEl.ValueKind == JsonValueKind.Array
            ? candidatesEl.EnumerateArray()
                .Select(c => new PlanCatalogReference(
                    RequireString(c, "key", reference),
                    RequireInt(c, "version", reference)))
                .ToList()
            : new List<PlanCatalogReference>();

        // Backend Integration Phase 10K-FREQ.6D.4D Split B: optional "prescriptionProfileCandidates"
        // array — additive, sibling to "workoutCandidates". Absent stages remain Legacy.
        var prescriptionProfileCandidateKeys = stageEl.TryGetProperty("prescriptionProfileCandidates", out var profileCandidatesEl) && profileCandidatesEl.ValueKind == JsonValueKind.Array
            ? profileCandidatesEl.EnumerateArray()
                .Select(c => new PlanCatalogReference(
                    RequireString(c, "key", reference),
                    RequireInt(c, "version", reference)))
                .ToList()
            : new List<PlanCatalogReference>();

        return new CatalogWorkoutProgressionStage
        {
            ProgressionStageKey = stageKey,
            RelativeOrder = relativeOrder,
            MinimumExposures = minimumExposures,
            MaximumExposures = maximumExposures,
            CompressionBehavior = compressionBehavior,
            ExtensionBehavior = extensionBehavior,
            Requires = requires,
            FallbackStageKey = fallbackStageKey,
            WorkoutCandidateReferences = workoutCandidateReferences,
            PrescriptionProfileCandidateKeys = prescriptionProfileCandidateKeys,
        };
    }

    private static CatalogRuntimeEligibilityCondition ReadCondition(JsonElement conditionEl, string stageKey, PlanCatalogReference reference)
    {
        var conditionType = RequireString(conditionEl, "conditionType", reference);

        if (!conditionEl.TryGetProperty("allowedValues", out var allowedValuesEl) || allowedValuesEl.ValueKind != JsonValueKind.Array)
        {
            throw new PlanCatalogLoadException(
                $"'allowedValues' is missing on stage '{stageKey}' requires[{conditionType}] in workout-progressions/{reference.Key} v{reference.Version}.");
        }

        var allowedValues = allowedValuesEl.EnumerateArray()
            .Select(v => v.ValueKind == JsonValueKind.String ? v.GetString()! : throw new PlanCatalogLoadException(
                $"'allowedValues' entries must be strings on stage '{stageKey}' requires[{conditionType}] in workout-progressions/{reference.Key} v{reference.Version}."))
            .ToHashSet();

        return new CatalogRuntimeEligibilityCondition { ConditionType = conditionType, AllowedValues = allowedValues };
    }

    private static CatalogStageCompressionBehavior ParseCompressionBehavior(string value, string stageKey, PlanCatalogReference reference) => value switch
    {
        "COMPRESSIBLE" => CatalogStageCompressionBehavior.Compressible,
        "PROTECTED" => CatalogStageCompressionBehavior.Protected,
        _ => throw new PlanCatalogLoadException(
            $"Unrecognized compressionBehavior '{value}' on stage '{stageKey}' in workout-progressions/{reference.Key} v{reference.Version}."),
    };

    private static CatalogStageExtensionBehavior ParseExtensionBehavior(string value, string stageKey, PlanCatalogReference reference) => value switch
    {
        "EXTENDABLE" => CatalogStageExtensionBehavior.Extendable,
        "FIXED_EXPOSURE" => CatalogStageExtensionBehavior.FixedExposure,
        _ => throw new PlanCatalogLoadException(
            $"Unrecognized extensionBehavior '{value}' on stage '{stageKey}' in workout-progressions/{reference.Key} v{reference.Version}."),
    };

    private static async Task<JsonDocument> FindDocumentAsync(string catalogRoot, string expectedKey, int expectedVersion, CancellationToken ct)
    {
        const string subfolder = "workout-progressions";
        const string expectedDocumentType = "WORKOUT_PROGRESSION";
        return await CatalogArtifactFileResolver.LoadAsync(
            catalogRoot, subfolder, expectedDocumentType, expectedKey, expectedVersion, ct);
    }

    private static string RequireString(JsonElement element, string propertyName, PlanCatalogReference owner)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString()!;
        }

        throw new PlanCatalogLoadException(
            $"Required string field '{propertyName}' is missing on workout-progressions/{owner.Key} v{owner.Version} (or a nested element within it).");
    }

    private static int RequireInt(JsonElement element, string propertyName, PlanCatalogReference owner)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.GetInt32();
        }

        throw new PlanCatalogLoadException(
            $"Required integer field '{propertyName}' is missing on workout-progressions/{owner.Key} v{owner.Version} (or a nested element within it).");
    }
}
