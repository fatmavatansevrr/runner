using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;

namespace RunningApp.Application.RuntimeCatalog;

/// <summary>
/// Read-only loader for a Process A plan-catalog TEMPLATE_COMBINATION candidate
/// and its immediate dependency graph (master template, layout, level modifier,
/// rule pack). This is the first Process B integration step that reads Process A
/// output — it never writes to, mutates, publishes, activates, retires, or
/// supersedes anything under the plan-catalog source tree, and it is not wired
/// into plan generation in this phase.
/// </summary>
public interface IPlanCatalogBundleLoader
{
    /// <summary>
    /// Loads and parses the named candidate. Throws
    /// <see cref="PlanCatalogLoadException"/> if the configured catalog root is
    /// missing, any required file is missing or contains invalid JSON, any
    /// required field is absent, or no TEMPLATE_COMBINATION document matches
    /// the requested (candidateKey, candidateVersion) exactly.
    /// </summary>
    Task<PlanCatalogCandidateSummary> LoadCandidateAsync(string candidateKey, int candidateVersion, CancellationToken ct = default);
}

/// <inheritdoc cref="IPlanCatalogBundleLoader"/>
public sealed class PlanCatalogBundleLoader : IPlanCatalogBundleLoader
{
    private readonly PlanCatalogOptions _options;
    private readonly ILogger<PlanCatalogBundleLoader> _logger;

    public PlanCatalogBundleLoader(IOptions<PlanCatalogOptions> options, ILogger<PlanCatalogBundleLoader> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PlanCatalogCandidateSummary> LoadCandidateAsync(string candidateKey, int candidateVersion, CancellationToken ct = default)
    {
        try
        {
            var root = _options.CatalogRootPath;
            if (!Directory.Exists(root))
            {
                throw new PlanCatalogLoadException(
                    $"Plan-catalog root directory was not found at '{root}' (configured via PlanCatalog:CatalogRootPath).");
            }

            using var combination = await FindDocumentAsync(root, "combinations", "TEMPLATE_COMBINATION", candidateKey, candidateVersion, ct);

            var candidateStatus = combination.RootElement.TryGetProperty("metadata", out var combinationMetadata)
                ? RequireString(combinationMetadata, "status", "combinations", new PlanCatalogReference(candidateKey, candidateVersion))
                : throw new PlanCatalogLoadException($"'metadata' is missing on combinations/{candidateKey} v{candidateVersion}.");

            var masterTemplateRef = ReadReference(combination, "masterTemplate");
            var layoutRef = ReadReference(combination, "layout");
            var levelModifierRef = ReadReference(combination, "levelModifier");
            var rulePackRef = ReadReference(combination, "rulePack");

            using var template = await FindDocumentAsync(root, "templates", "PLAN_TEMPLATE", masterTemplateRef.Key, masterTemplateRef.Version, ct);
            using var layout = await FindDocumentAsync(root, "layouts", "RUN_LAYOUT", layoutRef.Key, layoutRef.Version, ct);
            using var levelModifier = await FindDocumentAsync(root, "level-modifiers", "LEVEL_MODIFIER", levelModifierRef.Key, levelModifierRef.Version, ct);
            using var rulePack = await FindDocumentAsync(root, "rule-packs", "RULE_PACK", rulePackRef.Key, rulePackRef.Version, ct);

            var distanceFamily = RequireString(template, "distanceFamily", "templates", masterTemplateRef);
            var coreCycle = ReadCoreCycle(template, masterTemplateRef);
            var phaseKeys = template.RootElement.TryGetProperty("phases", out var phasesEl) && phasesEl.ValueKind == JsonValueKind.Array
                ? phasesEl.EnumerateArray().Select(p => RequireString(p, "phaseKey", "templates", masterTemplateRef)).ToList()
                : throw new PlanCatalogLoadException($"'phases' is missing on templates/{masterTemplateRef.Key} v{masterTemplateRef.Version}.");
            // Backend Integration Phase 4G.3B.1: preserve the complete schema-authored phase
            // constraint contract. Validation rejects malformed data; it never clamps or defaults.
            var phaseAllocations = ReadPhaseAllocations(phasesEl, masterTemplateRef);
            var workoutProgressionRef = ReadReference(template, "workoutProgression");

            var runsPerWeek = RequireInt(layout, "runsPerWeek", "layouts", layoutRef);
            var slotRoles = layout.RootElement.TryGetProperty("slots", out var slotsEl) && slotsEl.ValueKind == JsonValueKind.Array
                ? slotsEl.EnumerateArray().Select(s => RequireString(s, "role", "layouts", layoutRef)).ToList()
                : throw new PlanCatalogLoadException($"'slots' is missing on layouts/{layoutRef.Key} v{layoutRef.Version}.");

            var experience = RequireString(levelModifier, "experience", "level-modifiers", levelModifierRef);
            var progressionModifierRef = ReadReference(levelModifier, "progressionModifier");
            var referencedWorkouts = ReadEligibleWorkouts(levelModifier, levelModifierRef);

            var peakVolumeBandPolicyRef = ReadReference(rulePack, "peakVolumeBandPolicy");
            var runtimeConditionValueRegistryRef = ReadReference(rulePack, "runtimeConditionValueRegistry");

            var dependencyStatuses = new Dictionary<string, string>
            {
                ["masterTemplate"] = ReadStatus(template, "templates", masterTemplateRef),
                ["layout"] = ReadStatus(layout, "layouts", layoutRef),
                ["levelModifier"] = ReadStatus(levelModifier, "level-modifiers", levelModifierRef),
                ["rulePack"] = ReadStatus(rulePack, "rule-packs", rulePackRef),
            };

            var summary = new PlanCatalogCandidateSummary
            {
                CandidateKey = candidateKey,
                CandidateVersion = candidateVersion,
                CandidateStatus = candidateStatus,
                DependencyStatuses = dependencyStatuses,
                CanonicalDistanceFamily = distanceFamily,
                Level = experience,
                DaysPerWeek = runsPerWeek,
                CoreCycle = coreCycle,
                MasterTemplate = masterTemplateRef,
                Layout = layoutRef,
                LevelModifier = levelModifierRef,
                WorkoutProgression = workoutProgressionRef,
                ProgressionModifier = progressionModifierRef,
                RulePack = rulePackRef,
                PeakVolumeBandPolicy = peakVolumeBandPolicyRef,
                RuntimeConditionValueRegistry = runtimeConditionValueRegistryRef,
                ReferencedWorkouts = referencedWorkouts,
                PhaseKeys = phaseKeys,
                PhaseAllocations = phaseAllocations,
                SlotRoles = slotRoles,
            };

            _logger.LogInformation(
                "PlanCatalogBundleLoader: loaded {CandidateKey} v{CandidateVersion} " +
                "(distanceFamily={DistanceFamily}, level={Level}, daysPerWeek={DaysPerWeek}, " +
                "workoutCount={WorkoutCount}) from '{CatalogRoot}'.",
                candidateKey, candidateVersion, distanceFamily, experience, runsPerWeek, referencedWorkouts.Count, root);

            return summary;
        }
        catch (PlanCatalogLoadException ex)
        {
            _logger.LogWarning(ex, "PlanCatalogBundleLoader: failed to load {CandidateKey} v{CandidateVersion}.", candidateKey, candidateVersion);
            throw;
        }
    }

    private static string ReadStatus(JsonDocument document, string subfolder, PlanCatalogReference owner)
    {
        if (!document.RootElement.TryGetProperty("metadata", out var metadata))
        {
            throw new PlanCatalogLoadException($"'metadata' is missing on {subfolder}/{owner.Key} v{owner.Version}.");
        }

        return RequireString(metadata, "status", subfolder, owner);
    }

    private static PlanCatalogCoreCycle ReadCoreCycle(JsonDocument template, PlanCatalogReference owner)
    {
        if (!template.RootElement.TryGetProperty("coreCycle", out var coreCycleEl) || coreCycleEl.ValueKind != JsonValueKind.Object)
        {
            throw new PlanCatalogLoadException($"'coreCycle' is missing on templates/{owner.Key} v{owner.Version}.");
        }

        var minimumWeeks = RequireInt(coreCycleEl, "minimumWeeks", "templates", owner);
        var defaultWeeks = RequireInt(coreCycleEl, "defaultWeeks", "templates", owner);
        int? maximumWeeks = coreCycleEl.TryGetProperty("maximumWeeks", out var maxEl) && maxEl.ValueKind == JsonValueKind.Number
            ? maxEl.GetInt32()
            : null;

        return new PlanCatalogCoreCycle(minimumWeeks, defaultWeeks, maximumWeeks);
    }

    private static List<PlanCatalogPhaseAllocation> ReadPhaseAllocations(JsonElement phases, PlanCatalogReference owner)
    {
        var allocations = phases.EnumerateArray()
            .Select(phase => new PlanCatalogPhaseAllocation(
                RequireString(phase, "phaseKey", "templates", owner),
                RequireInt(phase, "minimumWeeks", "templates", owner),
                RequireInt(phase, "preferredWeeks", "templates", owner),
                RequireInt(phase, "maximumWeeks", "templates", owner),
                RequireInt(phase, "compressionPriority", "templates", owner),
                RequireInt(phase, "extensionPriority", "templates", owner),
                RequireBool(phase, "isCompressionProtected", "templates", owner)))
            .ToList();

        var requiredPhaseKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER",
        };
        var invalidIdentities = allocations.Where(p => !requiredPhaseKeys.Contains(p.PhaseKey)).Select(p => p.PhaseKey).ToList();
        if (invalidIdentities.Count > 0)
        {
            throw new PlanCatalogLoadException(
                $"templates/{owner.Key} v{owner.Version} declares invalid phase identity/identities: {string.Join(", ", invalidIdentities)}.");
        }

        var duplicates = allocations.GroupBy(p => p.PhaseKey, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
        {
            throw new PlanCatalogLoadException(
                $"templates/{owner.Key} v{owner.Version} declares duplicate phase definition(s): {string.Join(", ", duplicates)}.");
        }

        var missing = requiredPhaseKeys.Except(allocations.Select(p => p.PhaseKey), StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
        {
            throw new PlanCatalogLoadException(
                $"templates/{owner.Key} v{owner.Version} is missing required pilot phase definition(s): {string.Join(", ", missing)}.");
        }

        foreach (var phase in allocations)
        {
            if (phase.MinimumWeeks < 0 || phase.PreferredWeeks < 0 || phase.MaximumWeeks < 0)
            {
                throw new PlanCatalogLoadException(
                    $"templates/{owner.Key} v{owner.Version} phase '{phase.PhaseKey}' has negative week constraints.");
            }

            if (phase.MinimumWeeks > phase.PreferredWeeks || phase.PreferredWeeks > phase.MaximumWeeks)
            {
                throw new PlanCatalogLoadException(
                    $"templates/{owner.Key} v{owner.Version} phase '{phase.PhaseKey}' must satisfy minimumWeeks <= preferredWeeks <= maximumWeeks.");
            }

            if (phase.CompressionPriority < 1 || phase.ExtensionPriority < 1)
            {
                throw new PlanCatalogLoadException(
                    $"templates/{owner.Key} v{owner.Version} phase '{phase.PhaseKey}' allocation priorities must be positive integers.");
            }
        }

        return allocations;
    }

    private static List<PlanCatalogReference> ReadEligibleWorkouts(JsonDocument levelModifier, PlanCatalogReference owner)
    {
        if (levelModifier.RootElement.TryGetProperty("eligibleWorkouts", out var exactEl) && exactEl.ValueKind == JsonValueKind.Array)
        {
            return exactEl.EnumerateArray()
                .Select(w => new PlanCatalogReference(
                    RequireString(w, "key", "level-modifiers", owner),
                    RequireInt(w, "version", "level-modifiers", owner)))
                .ToList();
        }

        // Legacy schemaVersion 1 shape (eligibleWorkoutKeys, unversioned) carries no exact version
        // to resolve without additional highest-non-retired-version logic, which this minimal
        // read-only reader deliberately does not implement. Returning an empty list here (rather
        // than guessing a version) keeps the loader honest about what it actually resolved.
        return new List<PlanCatalogReference>();
    }

    private static async Task<JsonDocument> FindDocumentAsync(
        string catalogRoot, string subfolder, string expectedDocumentType, string expectedKey, int expectedVersion, CancellationToken ct)
    {
        return await CatalogArtifactFileResolver.LoadAsync(
            catalogRoot, subfolder, expectedDocumentType, expectedKey, expectedVersion, ct);
    }

    private static PlanCatalogReference ReadReference(JsonDocument document, string propertyName) =>
        ReadReference(document.RootElement, propertyName, document);

    private static PlanCatalogReference ReadReference(JsonElement parent, string propertyName, JsonDocument owner)
    {
        if (!parent.TryGetProperty(propertyName, out var refEl))
        {
            throw new PlanCatalogLoadException($"Required reference property '{propertyName}' is missing.");
        }

        var key = refEl.TryGetProperty("key", out var keyEl) && keyEl.ValueKind == JsonValueKind.String
            ? keyEl.GetString()!
            : throw new PlanCatalogLoadException($"Reference '{propertyName}' is missing a string 'key'.");

        var version = refEl.TryGetProperty("version", out var versionEl) && versionEl.ValueKind == JsonValueKind.Number
            ? versionEl.GetInt32()
            : throw new PlanCatalogLoadException($"Reference '{propertyName}' is missing an integer 'version'.");

        return new PlanCatalogReference(key, version);
    }

    private static string RequireString(JsonDocument document, string propertyName, string subfolder, PlanCatalogReference owner) =>
        RequireString(document.RootElement, propertyName, subfolder, owner);

    private static string RequireString(JsonElement element, string propertyName, string subfolder, PlanCatalogReference owner)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString()!;
        }

        throw new PlanCatalogLoadException(
            $"Required string field '{propertyName}' is missing on a {subfolder}/{owner.Key} v{owner.Version} document (or a nested element within it).");
    }

    private static int RequireInt(JsonDocument document, string propertyName, string subfolder, PlanCatalogReference owner) =>
        RequireInt(document.RootElement, propertyName, subfolder, owner);

    private static int RequireInt(JsonElement element, string propertyName, string subfolder, PlanCatalogReference owner)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.GetInt32();
        }

        throw new PlanCatalogLoadException(
            $"Required integer field '{propertyName}' is missing on a {subfolder}/{owner.Key} v{owner.Version} document (or a nested element within it).");
    }

    private static bool RequireBool(JsonElement element, string propertyName, string subfolder, PlanCatalogReference owner)
    {
        if (element.TryGetProperty(propertyName, out var value) &&
            (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False))
        {
            return value.GetBoolean();
        }

        throw new PlanCatalogLoadException(
            $"Required boolean field '{propertyName}' is missing on a {subfolder}/{owner.Key} v{owner.Version} document (or a nested element within it).");
    }
}
