using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

/// <summary>
/// Source-integrity (local structural) validation only — see Milestone A/D of
/// artifacts/audits/deterministic-graph-part2-migration.md. Validates stage identity, ordering,
/// duplicates, fallback structure, candidate-workout existence, and phase/family eligibility — all of
/// which are intrinsic to the progression document itself and do not depend on which combination/RulePack
/// is using it. Does NOT validate <c>Requires</c> condition values against any runtime-condition-value
/// registry — that requires knowing which exact registry the combination's RulePack pins, and is handled
/// by <see cref="CandidatePublishGraphValidator"/> for a specific selected combination. This validator
/// therefore no longer reads <c>RuntimeConditionValueRegistries.FirstOrDefault()</c> at all.
/// </summary>
public static class WorkoutProgressionValidator
{
    public static ValidationResult Validate(WorkoutProgressionDefinition progression, CatalogSourceSnapshot snapshot)
    {
        var issues = new List<ValidationIssue>();

        var owningMasters = snapshot.PlanTemplates
            .Where(t => t.WorkoutProgression.Key == progression.Metadata.Key && t.WorkoutProgression.Version == progression.Metadata.Version)
            .ToList();

        foreach (var phaseProgression in progression.PhaseProgressions)
        {
            var owningPhase = owningMasters
                .SelectMany(m => m.Phases)
                .FirstOrDefault(p => p.PhaseKey == phaseProgression.PhaseKey);

            ValidateRelativeOrder(phaseProgression, issues);

            var stageKeys = phaseProgression.Stages.Select(s => s.StageKey).ToHashSet(StringComparer.Ordinal);

            foreach (var stage in phaseProgression.Stages)
            {
                if (stage.MinimumExposures < 0 || stage.MinimumExposures > stage.MaximumExposures)
                {
                    issues.Add(new ValidationIssue("WP_EXPOSURE_BOUNDS_INVALID", ValidationSeverity.Error,
                        $"Stage '{stage.StageKey}': 0 <= MinimumExposures <= MaximumExposures violated ({stage.MinimumExposures}..{stage.MaximumExposures}).",
                        $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}]"));
                }

                if (stage.WorkoutCandidateKeys is not null)
                {
                    var missingCandidates = stage.WorkoutCandidateKeys.Where(k => snapshot.FindWorkout(k) is null).ToList();
                    if (missingCandidates.Count > 0)
                    {
                        issues.Add(new ValidationIssue("WP_CANDIDATE_WORKOUT_MISSING", ValidationSeverity.Error,
                            $"Stage '{stage.StageKey}' references unknown workout keys: {string.Join(", ", missingCandidates)}.",
                            $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}].workoutCandidateKeys"));
                    }

                    if (owningPhase is not null)
                    {
                        foreach (var candidateKey in stage.WorkoutCandidateKeys)
                        {
                            var workout = snapshot.FindWorkout(candidateKey);
                            if (workout is not null && !owningPhase.EligibleWorkoutFamilies.Contains(workout.Family))
                            {
                                issues.Add(new ValidationIssue("WP_CANDIDATE_FAMILY_NOT_ELIGIBLE_FOR_PHASE", ValidationSeverity.Error,
                                    $"Workout '{candidateKey}' family '{workout.Family}' is not eligible for phase '{phaseProgression.PhaseKey}'.",
                                    $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}]"));
                            }
                        }
                    }
                }

                if (stage.WorkoutCandidates is not null)
                {
                    var missingExact = stage.WorkoutCandidates.Where(r => snapshot.FindWorkout(r.Key, r.Version) is null).ToList();
                    if (missingExact.Count > 0)
                    {
                        issues.Add(new ValidationIssue("WP_CANDIDATE_WORKOUT_VERSION_MISSING", ValidationSeverity.Error,
                            $"Stage '{stage.StageKey}' references unknown exact workout versions: {string.Join(", ", missingExact.Select(r => $"{r.Key} v{r.Version}"))}.",
                            $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}].workoutCandidates"));
                    }

                    if (owningPhase is not null)
                    {
                        foreach (var candidateRef in stage.WorkoutCandidates)
                        {
                            var workout = snapshot.FindWorkout(candidateRef.Key, candidateRef.Version);
                            if (workout is not null && !owningPhase.EligibleWorkoutFamilies.Contains(workout.Family))
                            {
                                issues.Add(new ValidationIssue("WP_CANDIDATE_FAMILY_NOT_ELIGIBLE_FOR_PHASE", ValidationSeverity.Error,
                                    $"Workout '{candidateRef.Key}' v{candidateRef.Version} family '{workout.Family}' is not eligible for phase '{phaseProgression.PhaseKey}'.",
                                    $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}]"));
                            }
                        }
                    }
                }

                foreach (var condition in stage.Requires)
                {
                    if (condition.AllowedValues.Count == 0)
                    {
                        issues.Add(new ValidationIssue("WP_CONDITION_ALLOWED_VALUES_EMPTY", ValidationSeverity.Error,
                            $"Stage '{stage.StageKey}' condition '{condition.ConditionType}' declares zero AllowedValues.",
                            $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}].requires"));
                    }
                }

                if (stage.FallbackStageKey is not null)
                {
                    if (stage.FallbackStageKey == stage.StageKey)
                    {
                        issues.Add(new ValidationIssue("WP_SELF_FALLBACK", ValidationSeverity.Error,
                            $"Stage '{stage.StageKey}' cannot fall back to itself.",
                            $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}]"));
                    }
                    else if (!stageKeys.Contains(stage.FallbackStageKey))
                    {
                        issues.Add(new ValidationIssue("WP_FALLBACK_STAGE_MISSING", ValidationSeverity.Error,
                            $"Stage '{stage.StageKey}' fallback '{stage.FallbackStageKey}' does not exist in the same phase progression.",
                            $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}]"));
                    }
                }
            }

            DetectCircularFallback(phaseProgression, issues);
        }

        return new ValidationResult(issues);
    }

    private static void ValidateRelativeOrder(PhaseWorkoutProgressionDefinition phaseProgression, List<ValidationIssue> issues)
    {
        var orders = phaseProgression.Stages.Select(s => s.RelativeOrder).OrderBy(x => x).ToList();
        var expected = Enumerable.Range(1, phaseProgression.Stages.Count).ToList();

        if (orders.Any(o => o <= 0) || orders.Distinct().Count() != orders.Count || !orders.SequenceEqual(expected))
        {
            issues.Add(new ValidationIssue("WP_RELATIVE_ORDER_NOT_CONTIGUOUS", ValidationSeverity.Error,
                $"Stage RelativeOrder values for phase '{phaseProgression.PhaseKey}' must be unique, positive, and contiguous starting at 1.",
                $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages"));
        }
    }

    private static void DetectCircularFallback(PhaseWorkoutProgressionDefinition phaseProgression, List<ValidationIssue> issues)
    {
        var byKey = phaseProgression.Stages.ToDictionary(s => s.StageKey, StringComparer.Ordinal);

        foreach (var stage in phaseProgression.Stages)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal) { stage.StageKey };
            var current = stage.FallbackStageKey;

            while (current is not null)
            {
                if (!visited.Add(current))
                {
                    issues.Add(new ValidationIssue("WP_CIRCULAR_FALLBACK", ValidationSeverity.Error,
                        $"Circular fallback chain detected starting at stage '{stage.StageKey}'.",
                        $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}]"));
                    break;
                }

                current = byKey.TryGetValue(current, out var next) ? next.FallbackStageKey : null;
            }
        }
    }
}
