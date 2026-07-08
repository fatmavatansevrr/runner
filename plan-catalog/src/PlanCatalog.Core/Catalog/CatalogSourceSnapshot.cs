using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Core.Catalog;

/// <summary>An in-memory, already-loaded view of every authoring document in <c>catalog/</c>.</summary>
public sealed record CatalogSourceSnapshot
{
    public required IReadOnlyList<PlanTemplateDefinition> PlanTemplates { get; init; }
    public required IReadOnlyList<RunLayoutDefinition> RunLayouts { get; init; }
    public required IReadOnlyList<LevelModifierDefinition> LevelModifiers { get; init; }
    public required IReadOnlyList<WorkoutProgressionDefinition> WorkoutProgressions { get; init; }
    public required IReadOnlyList<ProgressionModifierDefinition> ProgressionModifiers { get; init; }
    public required IReadOnlyList<WorkoutDefinition> Workouts { get; init; }
    public required IReadOnlyList<RuntimeConditionValueRegistryDefinition> RuntimeConditionValueRegistries { get; init; }
    public required IReadOnlyList<PeakVolumeBandPolicy> PeakVolumeBandPolicies { get; init; }
    public required IReadOnlyList<RulePackDefinition> RulePacks { get; init; }
    public required IReadOnlyList<TemplateCombinationDefinition> Combinations { get; init; }

    public PlanTemplateDefinition? FindPlanTemplate(VersionedCatalogReference r) =>
        PlanTemplates.FirstOrDefault(x => x.Metadata.Key == r.Key && x.Metadata.Version == r.Version);

    public RunLayoutDefinition? FindRunLayout(VersionedCatalogReference r) =>
        RunLayouts.FirstOrDefault(x => x.Metadata.Key == r.Key && x.Metadata.Version == r.Version);

    public LevelModifierDefinition? FindLevelModifier(VersionedCatalogReference r) =>
        LevelModifiers.FirstOrDefault(x => x.Metadata.Key == r.Key && x.Metadata.Version == r.Version);

    public WorkoutProgressionDefinition? FindWorkoutProgression(VersionedCatalogReference r) =>
        WorkoutProgressions.FirstOrDefault(x => x.Metadata.Key == r.Key && x.Metadata.Version == r.Version);

    public ProgressionModifierDefinition? FindProgressionModifier(VersionedCatalogReference r) =>
        ProgressionModifiers.FirstOrDefault(x => x.Metadata.Key == r.Key && x.Metadata.Version == r.Version);

    /// <summary>
    /// Resolves a workout by key only (progression/level-modifier references are unversioned). When
    /// multiple versions of the same key coexist, deterministically resolves to the highest-versioned
    /// non-retired candidate — never an arbitrary/first-in-list match. See
    /// artifacts/audits/published-workout-immutability-remediation.md.
    /// </summary>
    /// <summary>
    /// LEGACY resolution only (schemaVersion 1 progression/level-modifier reading and historical
    /// verification). Auto-selects the highest non-retired version for a bare key — this is exactly the
    /// drift-prone behavior documented as a defect in
    /// artifacts/audits/deterministic-graph-prechange-assessment.md (Findings 5/6). Must never be used to
    /// assemble a new (schemaVersion >= 2) candidate graph — use <see cref="FindWorkout(string, int)"/> for
    /// that.
    /// </summary>
    public WorkoutDefinition? FindWorkout(string key, IRetirementLedger? retirementLedger = null)
    {
        var retirement = retirementLedger ?? NullRetirementLedger.Instance;
        return Workouts
            .Where(x => x.Metadata.Key == key && !retirement.IsRetired(x.Metadata.DocumentType, x.Metadata.Key, x.Metadata.Version))
            .OrderByDescending(x => x.Metadata.Version)
            .FirstOrDefault();
    }

    /// <summary>Exact lookup — no auto-selection. The only resolution path permitted for candidate (schemaVersion >= 2) graph assembly.</summary>
    public WorkoutDefinition? FindWorkout(string key, int version) =>
        Workouts.FirstOrDefault(x => x.Metadata.Key == key && x.Metadata.Version == version);

    /// <summary>Exact lookup that throws if the referenced workout does not exist in source.</summary>
    public WorkoutDefinition GetRequiredWorkout(string key, int version) =>
        FindWorkout(key, version) ?? throw new InvalidOperationException($"Workout '{key}' v{version} was not found in the source catalog.");

    public RuntimeConditionValueRegistryDefinition? FindRuntimeConditionValueRegistry(VersionedCatalogReference r) =>
        RuntimeConditionValueRegistries.FirstOrDefault(x => x.Metadata.Key == r.Key && x.Metadata.Version == r.Version);

    public PeakVolumeBandPolicy? FindPeakVolumeBandPolicy(VersionedCatalogReference r) =>
        PeakVolumeBandPolicies.FirstOrDefault(x => x.Metadata.Key == r.Key && x.Metadata.Version == r.Version);

    public RulePackDefinition? FindRulePack(VersionedCatalogReference r) =>
        RulePacks.FirstOrDefault(x => x.Metadata.Key == r.Key && x.Metadata.Version == r.Version);

    /// <summary>Looks up the CatalogStatus of any referenced document, regardless of its documentType.</summary>
    public Enums.CatalogStatus? FindStatus(VersionedCatalogReference r) => r.DocumentType switch
    {
        Contracts.DocumentTypes.PlanTemplate => FindPlanTemplate(r)?.Metadata.Status,
        Contracts.DocumentTypes.RunLayout => FindRunLayout(r)?.Metadata.Status,
        Contracts.DocumentTypes.LevelModifier => FindLevelModifier(r)?.Metadata.Status,
        Contracts.DocumentTypes.WorkoutProgression => FindWorkoutProgression(r)?.Metadata.Status,
        Contracts.DocumentTypes.ProgressionModifier => FindProgressionModifier(r)?.Metadata.Status,
        Contracts.DocumentTypes.WorkoutDefinition => Workouts.FirstOrDefault(x => x.Metadata.Key == r.Key && x.Metadata.Version == r.Version)?.Metadata.Status,
        Contracts.DocumentTypes.RuntimeConditionValueRegistry => FindRuntimeConditionValueRegistry(r)?.Metadata.Status,
        Contracts.DocumentTypes.PeakVolumeBandPolicy => FindPeakVolumeBandPolicy(r)?.Metadata.Status,
        Contracts.DocumentTypes.RulePack => FindRulePack(r)?.Metadata.Status,
        Contracts.DocumentTypes.TemplateCombination => Combinations.FirstOrDefault(x => x.Metadata.Key == r.Key && x.Metadata.Version == r.Version)?.Metadata.Status,
        _ => null
    };
}
