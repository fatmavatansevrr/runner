using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;

namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4C — read-only snapshot of a
/// RUNTIME_CONDITION_VALUE_REGISTRY document's allowed values, keyed by
/// conditionType. Used to validate that a (future) resolver's
/// <see cref="RuntimeConditionResolutionResult.OutputValue"/> is one of the
/// registry's actual allowed values, without hardcoding a second copy of
/// those values anywhere in backend code (avoids drift from the real
/// plan-catalog source file).
/// </summary>
public sealed class RuntimeConditionRegistrySnapshot
{
    public required string RegistryKey { get; init; }
    public required int RegistryVersion { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedValuesByConditionType { get; init; }

    /// <summary>True only if conditionType is known to this registry AND value is exactly one of its allowedValues.</summary>
    public bool IsValidValue(string conditionType, string value) =>
        AllowedValuesByConditionType.TryGetValue(conditionType, out var allowed) && allowed.Contains(value);

    /// <summary>
    /// Backend Integration Phase 4D.1.5 — validates a full resolver result
    /// against Status semantics, not just a bare string:
    /// <see cref="RuntimeConditionResolutionStatus.Evaluated"/> results must
    /// have a non-null <see cref="RuntimeConditionResolutionResult.OutputValue"/>
    /// that is registry-valid for the result's ConditionType.
    /// <see cref="RuntimeConditionResolutionStatus.NotEvaluated"/> results are
    /// NEVER looked up in the registry (there is nothing to look up — null
    /// OutputValue is not a registry-value question) and are always
    /// considered contract-valid here.
    /// </summary>
    public bool IsValid(RuntimeConditionResolutionResult result) =>
        result.Status switch
        {
            RuntimeConditionResolutionStatus.Evaluated => result.OutputValue is not null && IsValidValue(result.ConditionType, result.OutputValue),
            RuntimeConditionResolutionStatus.NotEvaluated => true,
            _ => false,
        };
}

/// <summary>
/// Read-only loader for a Process A RUNTIME_CONDITION_VALUE_REGISTRY document.
/// Uses the shared authoring/published-layout resolver. Published artifacts
/// must be manifest members and pass checksum and identity validation. Never
/// writes to, mutates, or creates any plan-catalog file.
/// </summary>
public interface IRuntimeConditionRegistryReader
{
    /// <summary>
    /// Loads and parses the named RUNTIME_CONDITION_VALUE_REGISTRY document.
    /// Throws <see cref="PlanCatalogLoadException"/> under the same conditions
    /// as <see cref="IPlanCatalogBundleLoader.LoadCandidateAsync"/> (missing
    /// catalog root, missing/invalid file, missing required field, or no
    /// document matching the requested key/version).
    /// </summary>
    Task<RuntimeConditionRegistrySnapshot> LoadAsync(PlanCatalogReference registryRef, CancellationToken ct = default);
}

/// <inheritdoc cref="IRuntimeConditionRegistryReader"/>
public sealed class RuntimeConditionRegistryReader : IRuntimeConditionRegistryReader
{
    private const string Subfolder = "registries";
    private const string ExpectedDocumentType = "RUNTIME_CONDITION_VALUE_REGISTRY";

    private readonly PlanCatalogOptions _options;
    private readonly ILogger<RuntimeConditionRegistryReader> _logger;

    public RuntimeConditionRegistryReader(IOptions<PlanCatalogOptions> options, ILogger<RuntimeConditionRegistryReader> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RuntimeConditionRegistrySnapshot> LoadAsync(PlanCatalogReference registryRef, CancellationToken ct = default)
    {
        var root = _options.CatalogRootPath;
        using var document = await CatalogArtifactFileResolver.LoadAsync(
            root, Subfolder, ExpectedDocumentType, registryRef.Key, registryRef.Version, ct);
        const string sourceDescription = "runtime-condition registry artifact";

        if (!document.RootElement.TryGetProperty("conditionValueSets", out var setsEl) || setsEl.ValueKind != JsonValueKind.Array)
        {
            throw new PlanCatalogLoadException($"The {sourceDescription} is missing the required 'conditionValueSets' array.");
        }

        var allowedValuesByConditionType = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var set in setsEl.EnumerateArray())
        {
            var conditionType = RequireString(set, "conditionType", sourceDescription);
            if (!set.TryGetProperty("allowedValues", out var valuesEl) || valuesEl.ValueKind != JsonValueKind.Array)
            {
                throw new PlanCatalogLoadException($"The {sourceDescription} conditionValueSets entry '{conditionType}' is missing 'allowedValues'.");
            }

            allowedValuesByConditionType[conditionType] = valuesEl.EnumerateArray()
                .Select(v => v.GetString() ?? throw new PlanCatalogLoadException($"The {sourceDescription} conditionValueSets entry '{conditionType}' has a non-string allowedValues element."))
                .ToList();
        }

        var snapshot = new RuntimeConditionRegistrySnapshot
        {
            RegistryKey = registryRef.Key,
            RegistryVersion = registryRef.Version,
            AllowedValuesByConditionType = allowedValuesByConditionType,
        };

        _logger.LogInformation(
            "RuntimeConditionRegistryReader: loaded {RegistryKey} v{RegistryVersion} with {ConditionCount} condition type(s) from '{CatalogRoot}'.",
            registryRef.Key, registryRef.Version, allowedValuesByConditionType.Count, root);

        return snapshot;
    }

    private static string RequireString(JsonElement element, string propertyName, string file)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString()!;
        }

        throw new PlanCatalogLoadException($"Required string field '{propertyName}' is missing on '{file}' (or a nested element within it).");
    }

    private static int RequireInt(JsonElement element, string propertyName, string file)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.GetInt32();
        }

        throw new PlanCatalogLoadException($"Required integer field '{propertyName}' is missing on '{file}' (or a nested element within it).");
    }
}
