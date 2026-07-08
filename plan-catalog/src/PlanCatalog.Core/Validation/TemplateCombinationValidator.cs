using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

/// <summary>
/// SOURCE-INTEGRITY layer only — see Milestone A of artifacts/audits/deterministic-graph-part2-migration.md.
/// Explicitly resolves the Combination → LevelModifier → ProgressionModifier two-hop chain and the
/// combination-level peak/effective-workout-set invariants — see brief §12.10. Structural only: does NOT
/// consult retirement state (moved to <see cref="CandidatePublishGraphValidator"/>, which runs only for a
/// selected, publish-eligible root and its exact dependency closure) — a retired combination or a retired
/// combination's now-invalid dependency must not block source-integrity validation of the rest of the
/// catalog. See deterministic-graph-prechange-assessment.md Finding 1.
/// </summary>
public static class TemplateCombinationValidator
{
    public static ValidationResult Validate(TemplateCombinationDefinition combination, CatalogSourceSnapshot snapshot)
    {
        var issues = new List<ValidationIssue>();

        var master = snapshot.FindPlanTemplate(combination.MasterTemplate);
        if (master is null)
        {
            issues.Add(new ValidationIssue("TC_MASTER_TEMPLATE_MISSING", ValidationSeverity.Error,
                $"Referenced MasterTemplate '{combination.MasterTemplate.Key}' v{combination.MasterTemplate.Version} was not found.", "$.masterTemplate"));
        }

        var layout = snapshot.FindRunLayout(combination.Layout);
        if (layout is null)
        {
            issues.Add(new ValidationIssue("TC_LAYOUT_MISSING", ValidationSeverity.Error,
                $"Referenced Layout '{combination.Layout.Key}' v{combination.Layout.Version} was not found.", "$.layout"));
        }

        var levelModifier = snapshot.FindLevelModifier(combination.LevelModifier);
        if (levelModifier is null)
        {
            issues.Add(new ValidationIssue("TC_LEVEL_MODIFIER_MISSING", ValidationSeverity.Error,
                $"Referenced LevelModifier '{combination.LevelModifier.Key}' v{combination.LevelModifier.Version} was not found.", "$.levelModifier"));
        }

        var rulePack = snapshot.FindRulePack(combination.RulePack);
        if (rulePack is null)
        {
            issues.Add(new ValidationIssue("TC_RULE_PACK_MISSING", ValidationSeverity.Error,
                $"Referenced RulePack '{combination.RulePack.Key}' v{combination.RulePack.Version} was not found.", "$.rulePack"));
        }

        if (master is not null && layout is not null && !master.SupportedRunsPerWeek.Contains(layout.RunsPerWeek))
        {
            issues.Add(new ValidationIssue("TC_LAYOUT_RUNS_PER_WEEK_NOT_SUPPORTED", ValidationSeverity.Error,
                $"Layout RunsPerWeek {layout.RunsPerWeek} is not in master's SupportedRunsPerWeek.", "$.layout"));
        }

        var progression = master is not null ? snapshot.FindWorkoutProgression(master.WorkoutProgression) : null;
        if (master is not null && progression is null)
        {
            issues.Add(new ValidationIssue("TC_WORKOUT_PROGRESSION_MISSING", ValidationSeverity.Error,
                "Master template's WorkoutProgression reference could not be resolved.", "$.masterTemplate.workoutProgression"));
        }
        else if (progression is not null && master is not null && progression.DistanceFamily != master.DistanceFamily)
        {
            issues.Add(new ValidationIssue("TC_WORKOUT_PROGRESSION_DISTANCE_MISMATCH", ValidationSeverity.Error,
                "Workout progression distance family does not match master template distance family.", "$.masterTemplate.workoutProgression"));
        }

        // Explicit two-hop chain: Combination -> LevelModifier -> ProgressionModifier.
        var progressionModifier = levelModifier is not null ? snapshot.FindProgressionModifier(levelModifier.ProgressionModifier) : null;
        if (levelModifier is not null && progressionModifier is null)
        {
            issues.Add(new ValidationIssue("TC_PROGRESSION_MODIFIER_MISSING", ValidationSeverity.Error,
                $"LevelModifier '{levelModifier.Metadata.Key}' references a ProgressionModifier that could not be resolved; combination is invalid.", "$.levelModifier.progressionModifier"));
        }
        else if (progressionModifier is not null && levelModifier is not null && progressionModifier.Experience != levelModifier.Experience)
        {
            issues.Add(new ValidationIssue("TC_PROGRESSION_MODIFIER_EXPERIENCE_MISMATCH", ValidationSeverity.Error,
                "ProgressionModifier experience does not match LevelModifier experience.", "$.levelModifier.progressionModifier"));
        }

        if (layout is not null && progressionModifier is not null)
        {
            var keySessionCount = layout.Slots.Count(s => s.Role == SlotRole.KeySession);
            if (keySessionCount > progressionModifier.MaximumHardSessionsPerWeek)
            {
                issues.Add(new ValidationIssue("TC_KEY_SESSION_COUNT_EXCEEDS_CAP", ValidationSeverity.Error,
                    $"Layout KEY_SESSION count {keySessionCount} exceeds resolved ProgressionModifier.MaximumHardSessionsPerWeek {progressionModifier.MaximumHardSessionsPerWeek}.", "$.layout"));
            }
        }

        var peakPolicy = rulePack is not null ? snapshot.FindPeakVolumeBandPolicy(rulePack.PeakVolumeBandPolicy) : null;
        if (peakPolicy is not null && master is not null && layout is not null && levelModifier is not null)
        {
            var tupleExists = peakPolicy.Entries.Any(e =>
                e.DistanceFamily == master.DistanceFamily &&
                e.Experience == levelModifier.Experience &&
                e.RunsPerWeek == layout.RunsPerWeek);

            if (!tupleExists)
            {
                issues.Add(new ValidationIssue("TC_PEAK_TUPLE_MISSING", ValidationSeverity.Error,
                    $"No PeakVolumeBandPolicy entry for ({master.DistanceFamily}, {levelModifier.Experience}, {layout.RunsPerWeek}).", "$.rulePack.peakVolumeBandPolicy"));
            }
        }

        if (progression is not null && levelModifier is not null)
        {
            ValidateEffectiveWorkoutSet(progression, levelModifier, snapshot, issues);
        }

        if (combination.Metadata.Status == CatalogStatus.Published)
        {
            ValidatePublishedClosure(combination, master, layout, levelModifier, rulePack, progression, progressionModifier, peakPolicy, snapshot, issues);
        }

        return new ValidationResult(issues);
    }

    private static void ValidateEffectiveWorkoutSet(
        WorkoutProgressionDefinition progression,
        LevelModifierDefinition levelModifier,
        CatalogSourceSnapshot snapshot,
        List<ValidationIssue> issues)
    {
        bool IsUsableLegacy(string workoutKey) =>
            levelModifier.EligibleWorkoutKeys is not null &&
            levelModifier.EligibleWorkoutKeys.Contains(workoutKey) &&
            snapshot.FindWorkout(workoutKey) is not null;

        bool IsUsableExact(Contracts.References.VersionedCatalogReference candidate) =>
            levelModifier.EligibleWorkouts is not null &&
            levelModifier.EligibleWorkouts.Any(e => e.Key == candidate.Key && e.Version == candidate.Version) &&
            snapshot.FindWorkout(candidate.Key, candidate.Version) is not null;

        var anyReachable = false;

        foreach (var phaseProgression in progression.PhaseProgressions)
        {
            var byKey = phaseProgression.Stages.ToDictionary(s => s.StageKey, StringComparer.Ordinal);

            foreach (var stage in phaseProgression.Stages)
            {
                var reachable = StageHasReachableEffectiveCandidate(stage, byKey, IsUsableLegacy, IsUsableExact, new HashSet<string>(StringComparer.Ordinal));
                anyReachable |= reachable;

                if (reachable)
                {
                    continue;
                }

                issues.Add(new ValidationIssue("TC_STAGE_UNREACHABLE", ValidationSeverity.Error,
                    $"Stage '{stage.StageKey}' in phase '{phaseProgression.PhaseKey}' has no effective candidate and no valid fallback chain.",
                    $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}]"));
            }
        }

        if (!anyReachable)
        {
            issues.Add(new ValidationIssue("TC_EFFECTIVE_WORKOUT_SET_EMPTY", ValidationSeverity.Error,
                "The effective workout set (progression candidates ∩ level-modifier eligible workouts ∩ published workouts) is empty.", "$"));
        }
    }

    private static bool StageHasReachableEffectiveCandidate(
        WorkoutProgressionStageDefinition stage,
        IReadOnlyDictionary<string, WorkoutProgressionStageDefinition> byKey,
        Func<string, bool> isUsableLegacy,
        Func<Contracts.References.VersionedCatalogReference, bool> isUsableExact,
        HashSet<string> visited)
    {
        if (!visited.Add(stage.StageKey))
        {
            return false;
        }

        if (stage.WorkoutCandidateKeys is not null && stage.WorkoutCandidateKeys.Any(isUsableLegacy))
        {
            return true;
        }

        if (stage.WorkoutCandidates is not null && stage.WorkoutCandidates.Any(isUsableExact))
        {
            return true;
        }

        if (stage.FallbackStageKey is not null && byKey.TryGetValue(stage.FallbackStageKey, out var fallback))
        {
            return StageHasReachableEffectiveCandidate(fallback, byKey, isUsableLegacy, isUsableExact, visited);
        }

        return false;
    }

    private static void ValidatePublishedClosure(
        TemplateCombinationDefinition combination,
        PlanTemplateDefinition? master,
        RunLayoutDefinition? layout,
        LevelModifierDefinition? levelModifier,
        RulePackDefinition? rulePack,
        WorkoutProgressionDefinition? progression,
        ProgressionModifierDefinition? progressionModifier,
        PeakVolumeBandPolicy? peakPolicy,
        CatalogSourceSnapshot snapshot,
        List<ValidationIssue> issues)
    {
        var registry = rulePack is not null ? snapshot.FindRuntimeConditionValueRegistry(rulePack.RuntimeConditionValueRegistry) : null;

        var dependencies = new (string Label, CatalogStatus? Status)[]
        {
            ("masterTemplate", master?.Metadata.Status),
            ("layout", layout?.Metadata.Status),
            ("levelModifier", levelModifier?.Metadata.Status),
            ("rulePack", rulePack?.Metadata.Status),
            ("workoutProgression", progression?.Metadata.Status),
            ("progressionModifier", progressionModifier?.Metadata.Status),
            ("peakVolumeBandPolicy", peakPolicy?.Metadata.Status),
            ("runtimeConditionValueRegistry", registry?.Metadata.Status),
        };

        foreach (var (label, status) in dependencies)
        {
            if (status is not CatalogStatus.Published)
            {
                issues.Add(new ValidationIssue("TC_PUBLISHED_DEPENDENCY_NOT_PUBLISHED", ValidationSeverity.Error,
                    $"Combination '{combination.Metadata.Key}' is PUBLISHED but dependency '{label}' is '{status?.ToString() ?? "MISSING"}'.", $"$.{label}"));
            }
        }
    }
}
