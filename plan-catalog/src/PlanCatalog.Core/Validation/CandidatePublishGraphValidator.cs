using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Core.Validation;

/// <summary>
/// PUBLISH-GRAPH layer (Milestone A2 of artifacts/audits/deterministic-graph-part2-migration.md). Runs
/// only for a single, explicitly selected, non-retired combination and its exact dependency closure —
/// never across the full unfiltered catalog. Combines:
///   - retirement eligibility of the root and its full dependency closure (A2),
///   - RulePack/master semantic-requirement compatibility (C3),
///   - registry-pinned runtime-condition validation (D2),
///   - layout slot workout coverage (E1).
/// Source-integrity concerns (schema shape, structural invariants, existence-of-reference) are handled
/// separately by <see cref="CatalogGraphValidator"/>/<see cref="TemplateCombinationValidator"/> and are
/// assumed to already have passed before this runs.
/// </summary>
public static class CandidatePublishGraphValidator
{
    public static ValidationResult Validate(CatalogSourceSnapshot snapshot, TemplateCombinationDefinition combination, IRetirementLedger? retirementLedger = null)
    {
        var retirement = retirementLedger ?? NullRetirementLedger.Instance;
        var issues = new List<ValidationIssue>();

        if (retirement.IsRetired(combination.Metadata.DocumentType, combination.Metadata.Key, combination.Metadata.Version))
        {
            issues.Add(new ValidationIssue("RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE", ValidationSeverity.Error,
                $"Combination '{combination.Metadata.Key}' v{combination.Metadata.Version} is RETIRED and cannot be selected as a new-release publish root.", "$"));
            return new ValidationResult(issues);
        }

        var master = snapshot.FindPlanTemplate(combination.MasterTemplate);
        var layout = snapshot.FindRunLayout(combination.Layout);
        var levelModifier = snapshot.FindLevelModifier(combination.LevelModifier);
        var rulePack = snapshot.FindRulePack(combination.RulePack);

        // Existence is a source-integrity concern already caught elsewhere; nothing more to check
        // contextually if any required dependency is missing.
        if (master is null || layout is null || levelModifier is null || rulePack is null)
        {
            return new ValidationResult(issues);
        }

        var progression = snapshot.FindWorkoutProgression(master.WorkoutProgression);
        var progressionModifier = snapshot.FindProgressionModifier(levelModifier.ProgressionModifier);
        var registry = snapshot.FindRuntimeConditionValueRegistry(rulePack.RuntimeConditionValueRegistry);
        var peakPolicy = snapshot.FindPeakVolumeBandPolicy(rulePack.PeakVolumeBandPolicy);

        ValidateDependencyClosureRetirement(combination, master, layout, levelModifier, rulePack, progression, progressionModifier, registry, peakPolicy, retirement, issues);
        ValidateRulePackSatisfiesMasterRequirement(master, rulePack, issues);

        if (progression is not null && registry is not null)
        {
            ValidatePinnedRegistry(progression, registry, issues);
        }

        if (progression is not null && levelModifier is not null && layout is not null)
        {
            ValidateLayoutCoverage(snapshot, progression, levelModifier, layout, issues);
        }

        return new ValidationResult(issues);
    }

    private static void ValidateDependencyClosureRetirement(
        TemplateCombinationDefinition combination,
        PlanTemplateDefinition master, RunLayoutDefinition layout, LevelModifierDefinition levelModifier, RulePackDefinition rulePack,
        WorkoutProgressionDefinition? progression, ProgressionModifierDefinition? progressionModifier,
        RuntimeConditionValueRegistryDefinition? registry, PeakVolumeBandPolicy? peakPolicy,
        IRetirementLedger retirement, List<ValidationIssue> issues)
    {
        var dependencyMetadata = new[]
        {
            master.Metadata, layout.Metadata, levelModifier.Metadata, rulePack.Metadata,
            progression?.Metadata, progressionModifier?.Metadata, registry?.Metadata, peakPolicy?.Metadata
        };

        foreach (var metadata in dependencyMetadata)
        {
            if (metadata is not null && retirement.IsRetired(metadata.DocumentType, metadata.Key, metadata.Version))
            {
                issues.Add(new ValidationIssue("PUBLISH_DEPENDENCY_RETIRED", ValidationSeverity.Error,
                    $"Combination '{combination.Metadata.Key}' v{combination.Metadata.Version}: dependency '{metadata.DocumentType}/{metadata.Key}' v{metadata.Version} is RETIRED and cannot be used in a new publish graph.", "$"));
            }
        }
    }

    /// <summary>
    /// Decision C — combination.RulePack is the sole exact RulePack selection. The master's new
    /// (schemaVersion >= 2) RequiredRuleKeys expresses only a semantic key requirement, never a
    /// competing exact version. Legacy (schemaVersion 1) masters using RequiredRules are not
    /// retroactively cross-checked here — historical masters remain readable under their original rules.
    /// </summary>
    private static void ValidateRulePackSatisfiesMasterRequirement(PlanTemplateDefinition master, RulePackDefinition rulePack, List<ValidationIssue> issues)
    {
        if (master.RequiredRuleKeys is null)
        {
            return;
        }

        if (!master.RequiredRuleKeys.Contains(rulePack.Metadata.Key))
        {
            issues.Add(new ValidationIssue("COMBINATION_RULE_PACK_DOES_NOT_SATISFY_MASTER_REQUIREMENTS", ValidationSeverity.Error,
                $"Combination-selected RulePack key '{rulePack.Metadata.Key}' is not present in master template '{master.Metadata.Key}' v{master.Metadata.Version}'s RequiredRuleKeys.", "$.rulePack"));
        }
    }

    /// <summary>
    /// Decision D — Combination → exact RulePack → exact RuntimeConditionValueRegistry. Never
    /// FirstOrDefault. Re-validates every stage's Requires conditions against exactly this registry.
    /// </summary>
    private static void ValidatePinnedRegistry(WorkoutProgressionDefinition progression, RuntimeConditionValueRegistryDefinition registry, List<ValidationIssue> issues)
    {
        foreach (var phase in progression.PhaseProgressions)
        {
            foreach (var stage in phase.Stages)
            {
                foreach (var condition in stage.Requires)
                {
                    var valueSet = registry.ConditionValueSets.FirstOrDefault(v => v.ConditionType == condition.ConditionType);
                    if (valueSet is null)
                    {
                        issues.Add(new ValidationIssue("RUNTIME_CONDITION_VALUE_NOT_ALLOWED_BY_PINNED_REGISTRY", ValidationSeverity.Error,
                            $"Stage '{stage.StageKey}': condition type '{condition.ConditionType}' is not present in the pinned registry '{registry.Metadata.Key}' v{registry.Metadata.Version}.",
                            $"$.phaseProgressions[{phase.PhaseKey}].stages[{stage.StageKey}].requires"));
                        continue;
                    }

                    var invalidValues = condition.AllowedValues.Except(valueSet.AllowedValues, StringComparer.Ordinal).ToList();
                    if (invalidValues.Count > 0)
                    {
                        issues.Add(new ValidationIssue("RUNTIME_CONDITION_VALUE_NOT_ALLOWED_BY_PINNED_REGISTRY", ValidationSeverity.Error,
                            $"Stage '{stage.StageKey}': values not allowed by pinned registry '{registry.Metadata.Key}' v{registry.Metadata.Version} for '{condition.ConditionType}': {string.Join(", ", invalidValues)}.",
                            $"$.phaseProgressions[{phase.PhaseKey}].stages[{stage.StageKey}].requires"));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Milestone E1 — structural coverage only (no scheduling, no dates, no pace/dosage). For the
    /// candidate (exact) shape, checks every distinct layout SlotRole has at least one family-compatible
    /// workout reachable in the exact closure (Milestone E union). Legacy (non-exact) progressions are not
    /// checked here — this is a new, additive invariant for the candidate graph only.
    /// </summary>
    private static void ValidateLayoutCoverage(
        CatalogSourceSnapshot snapshot, WorkoutProgressionDefinition progression, LevelModifierDefinition levelModifier,
        RunLayoutDefinition layout, List<ValidationIssue> issues)
    {
        if (!WorkoutClosureResolver.IsExactShape(progression, levelModifier))
        {
            return;
        }

        var closureRefs = WorkoutClosureResolver.ComputeExactClosureRefs(progression, levelModifier);
        var closureWorkouts = new List<WorkoutDefinition>();
        foreach (var r in closureRefs)
        {
            var workout = snapshot.FindWorkout(r.Key, r.Version);
            if (workout is null)
            {
                issues.Add(new ValidationIssue("BUNDLE_MISSING_REFERENCED_WORKOUT", ValidationSeverity.Error,
                    $"Exact workout reference '{r.Key}' v{r.Version} could not be resolved for layout coverage checking.", "$"));
            }
            else
            {
                closureWorkouts.Add(workout);
            }
        }

        foreach (var role in layout.Slots.Select(s => s.Role).Distinct())
        {
            var compatibleFamilies = RoleCompatibleFamilies(role);
            if (!closureWorkouts.Any(w => compatibleFamilies.Contains(w.Family)))
            {
                issues.Add(new ValidationIssue("LAYOUT_SLOT_HAS_NO_ELIGIBLE_WORKOUT", ValidationSeverity.Error,
                    $"Layout '{layout.Metadata.Key}' has a '{role}' slot but the candidate workout closure contains no compatible workout family ({string.Join("/", compatibleFamilies)}).", "$.layout.slots"));
            }
        }
    }

    private static IReadOnlyList<WorkoutFamily> RoleCompatibleFamilies(SlotRole role) => role switch
    {
        SlotRole.LongRun => [WorkoutFamily.LongRun],
        SlotRole.EasySupport => [WorkoutFamily.Easy],
        SlotRole.KeySession => [WorkoutFamily.Quality, WorkoutFamily.Race],
        _ => []
    };
}
