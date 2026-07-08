using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Tests.TestSupport;

public sealed class CatalogSnapshotBuilder
{
    private readonly List<PlanTemplateDefinition> _planTemplates = new();
    private readonly List<RunLayoutDefinition> _runLayouts = new();
    private readonly List<LevelModifierDefinition> _levelModifiers = new();
    private readonly List<WorkoutProgressionDefinition> _workoutProgressions = new();
    private readonly List<ProgressionModifierDefinition> _progressionModifiers = new();
    private readonly List<WorkoutDefinition> _workouts = new();
    private readonly List<RuntimeConditionValueRegistryDefinition> _registries = new();
    private readonly List<PeakVolumeBandPolicy> _peakVolumeBandPolicies = new();
    private readonly List<RulePackDefinition> _rulePacks = new();
    private readonly List<TemplateCombinationDefinition> _combinations = new();

    public CatalogSnapshotBuilder With(PlanTemplateDefinition x) { _planTemplates.Add(x); return this; }
    public CatalogSnapshotBuilder With(RunLayoutDefinition x) { _runLayouts.Add(x); return this; }
    public CatalogSnapshotBuilder With(LevelModifierDefinition x) { _levelModifiers.Add(x); return this; }
    public CatalogSnapshotBuilder With(WorkoutProgressionDefinition x) { _workoutProgressions.Add(x); return this; }
    public CatalogSnapshotBuilder With(ProgressionModifierDefinition x) { _progressionModifiers.Add(x); return this; }
    public CatalogSnapshotBuilder With(WorkoutDefinition x) { _workouts.Add(x); return this; }
    public CatalogSnapshotBuilder With(RuntimeConditionValueRegistryDefinition x) { _registries.Add(x); return this; }
    public CatalogSnapshotBuilder With(PeakVolumeBandPolicy x) { _peakVolumeBandPolicies.Add(x); return this; }
    public CatalogSnapshotBuilder With(RulePackDefinition x) { _rulePacks.Add(x); return this; }
    public CatalogSnapshotBuilder With(TemplateCombinationDefinition x) { _combinations.Add(x); return this; }

    public CatalogSourceSnapshot Build() => new()
    {
        PlanTemplates = _planTemplates,
        RunLayouts = _runLayouts,
        LevelModifiers = _levelModifiers,
        WorkoutProgressions = _workoutProgressions,
        ProgressionModifiers = _progressionModifiers,
        Workouts = _workouts,
        RuntimeConditionValueRegistries = _registries,
        PeakVolumeBandPolicies = _peakVolumeBandPolicies,
        RulePacks = _rulePacks,
        Combinations = _combinations
    };
}
