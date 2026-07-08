using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Ports;
using PlanCatalog.Infrastructure.Serialization;

namespace PlanCatalog.Infrastructure.Repositories;

/// <summary>Loads the editable authoring source tree under <c>catalog/</c> — see brief §14.1.</summary>
public sealed class FileSystemCatalogSourceRepository(string catalogRootDirectory) : ICatalogSourceRepository
{
    public CatalogSourceSnapshot LoadSnapshot() => new()
    {
        PlanTemplates = LoadAll<PlanTemplateDefinition>("templates"),
        RunLayouts = LoadAll<RunLayoutDefinition>("layouts"),
        LevelModifiers = LoadAll<LevelModifierDefinition>("level-modifiers"),
        WorkoutProgressions = LoadAll<WorkoutProgressionDefinition>("workout-progressions"),
        ProgressionModifiers = LoadAll<ProgressionModifierDefinition>("progression-modifiers"),
        Workouts = LoadAll<WorkoutDefinition>("workouts"),
        RuntimeConditionValueRegistries = LoadAll<RuntimeConditionValueRegistryDefinition>("registries"),
        PeakVolumeBandPolicies = LoadAll<PeakVolumeBandPolicy>("policies"),
        RulePacks = LoadAll<RulePackDefinition>("rule-packs"),
        Combinations = LoadAll<TemplateCombinationDefinition>("combinations")
    };

    private List<T> LoadAll<T>(string subFolder)
    {
        var directory = Path.Combine(catalogRootDirectory, subFolder);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var results = new List<T>();
        foreach (var file in Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.Ordinal))
        {
            var json = File.ReadAllText(file);
            var document = System.Text.Json.JsonSerializer.Deserialize<T>(json, CanonicalJsonOptions.Pretty)
                ?? throw new InvalidOperationException($"Failed to deserialize '{file}' as {typeof(T).Name}.");
            results.Add(document);
        }

        return results;
    }
}
