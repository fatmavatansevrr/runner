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
///
/// Phase 10K-FREQ.6D.4D Split B: stage/fallback/candidate validation is now scoped per-lane
/// (<see cref="PhaseWorkoutProgressionDefinition.EffectiveLanes"/>) rather than per-phase —
/// matching the RunningApp binder's own per-lane stage resolution exactly, so a lane-1 stage can
/// never accidentally fall back to (or collide with) a lane-0-only stage key. For every phase not
/// authored with <c>Lanes</c>, <c>EffectiveLanes</c> degenerates to one implicit lane wrapping
/// <c>Stages</c>, so this change is behaviorally inert for every existing legacy document.
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

            ValidateLaneOrdinals(phaseProgression, issues);

            foreach (var lane in phaseProgression.EffectiveLanes)
            {
                ValidateRelativeOrder(phaseProgression, lane, issues);
                ValidateLaneStages(phaseProgression, lane, owningPhase, snapshot, issues);
                DetectCircularFallback(phaseProgression, lane, issues);
            }
        }

        return new ValidationResult(issues);
    }

    private static void ValidateLaneOrdinals(PhaseWorkoutProgressionDefinition phaseProgression, List<ValidationIssue> issues)
    {
        if (phaseProgression.Lanes is not { Count: > 0 } lanes)
        {
            return;
        }

        var duplicateOrdinals = lanes.GroupBy(l => l.LaneOrdinal).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateOrdinals.Count > 0)
        {
            issues.Add(new ValidationIssue("WP_DUPLICATE_LANE_ORDINAL", ValidationSeverity.Error,
                $"Phase '{phaseProgression.PhaseKey}' declares more than one lane with the same LaneOrdinal: {string.Join(", ", duplicateOrdinals)}.",
                $"$.phaseProgressions[{phaseProgression.PhaseKey}].lanes"));
        }
    }

    private static void ValidateLaneStages(
        PhaseWorkoutProgressionDefinition phaseProgression,
        WorkoutProgressionLaneDefinition lane,
        PhaseDefinition? owningPhase,
        CatalogSourceSnapshot snapshot,
        List<ValidationIssue> issues)
    {
        // Fallback resolution and stage-key uniqueness are scoped per-lane — a stage in lane 1
        // must never fall back to a stage key that only exists in lane 0.
        var stageKeys = lane.Stages.Select(s => s.StageKey).ToHashSet(StringComparer.Ordinal);

        foreach (var stage in lane.Stages)
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

            ValidatePrescriptionProfileCandidates(phaseProgression, lane, stage, snapshot, issues);

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
                        $"Stage '{stage.StageKey}' fallback '{stage.FallbackStageKey}' does not exist in the same phase progression lane.",
                        $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}]"));
                }
            }
        }
    }

    /// <summary>
    /// Phase 10K-FREQ.6D.4D Split B: static (publish-time) validation for the new,
    /// additive <see cref="WorkoutProgressionStageDefinition.PrescriptionProfileCandidates"/>
    /// field. Absent/empty is legal (the stage stays Legacy, never ProfileBacked — §62 of the
    /// implementation prompt). Exactly one candidate is the only binder-resolvable shape; more
    /// than one is flagged here too (fail-fast) even though the RunningApp binder independently
    /// rejects it at bind time as defense-in-depth, mirroring the existing WorkoutCandidates
    /// ambiguity precedent. Reuses <see cref="PrescriptionProfileLaneDoseValidator"/> verbatim
    /// for the LaneOrdinal↔DoseCategory invariant — no second copy of that mapping.
    /// </summary>
    private static void ValidatePrescriptionProfileCandidates(
        PhaseWorkoutProgressionDefinition phaseProgression,
        WorkoutProgressionLaneDefinition lane,
        WorkoutProgressionStageDefinition stage,
        CatalogSourceSnapshot snapshot,
        List<ValidationIssue> issues)
    {
        if (stage.PrescriptionProfileCandidates is not { Count: > 0 } candidates)
        {
            return;
        }

        var duplicateRefs = candidates.GroupBy(r => (r.Key, r.Version)).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateRefs.Count > 0)
        {
            issues.Add(new ValidationIssue("WP_PRESCRIPTION_PROFILE_CANDIDATE_DUPLICATE", ValidationSeverity.Error,
                $"Stage '{stage.StageKey}' declares the same prescription-profile candidate more than once: " +
                $"{string.Join(", ", duplicateRefs.Select(r => $"{r.Key} v{r.Version}"))}.",
                $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}].prescriptionProfileCandidates"));
        }

        if (candidates.Count > 1)
        {
            issues.Add(new ValidationIssue("WP_PRESCRIPTION_PROFILE_CANDIDATE_AMBIGUOUS", ValidationSeverity.Error,
                $"Stage '{stage.StageKey}' declares {candidates.Count} prescription-profile candidates — " +
                "no multi-profile selection policy exists; a stage becomes ProfileBacked only with exactly one candidate.",
                $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}].prescriptionProfileCandidates"));
        }

        foreach (var candidateRef in candidates)
        {
            var profile = snapshot.FindPrescriptionProfile(candidateRef.Key, candidateRef.Version);
            if (profile is null)
            {
                issues.Add(new ValidationIssue("WP_PRESCRIPTION_PROFILE_CANDIDATE_MISSING", ValidationSeverity.Error,
                    $"Stage '{stage.StageKey}' references unknown exact prescription-profile version: {candidateRef.Key} v{candidateRef.Version}.",
                    $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}].prescriptionProfileCandidates"));
                continue;
            }

            var laneDoseResult = PrescriptionProfileLaneDoseValidator.Validate(lane.LaneOrdinal, profile);
            foreach (var issue in laneDoseResult.Issues)
            {
                issues.Add(issue with
                {
                    Message = $"Stage '{stage.StageKey}' lane {lane.LaneOrdinal}: {issue.Message}",
                    JsonPath = $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages[{stage.StageKey}].prescriptionProfileCandidates",
                });
            }
        }
    }

    private static void ValidateRelativeOrder(PhaseWorkoutProgressionDefinition phaseProgression, WorkoutProgressionLaneDefinition lane, List<ValidationIssue> issues)
    {
        var orders = lane.Stages.Select(s => s.RelativeOrder).OrderBy(x => x).ToList();
        var expected = Enumerable.Range(1, lane.Stages.Count).ToList();

        if (orders.Any(o => o <= 0) || orders.Distinct().Count() != orders.Count || !orders.SequenceEqual(expected))
        {
            issues.Add(new ValidationIssue("WP_RELATIVE_ORDER_NOT_CONTIGUOUS", ValidationSeverity.Error,
                $"Stage RelativeOrder values for phase '{phaseProgression.PhaseKey}' lane {lane.LaneOrdinal} must be unique, positive, and contiguous starting at 1.",
                $"$.phaseProgressions[{phaseProgression.PhaseKey}].stages"));
        }
    }

    private static void DetectCircularFallback(PhaseWorkoutProgressionDefinition phaseProgression, WorkoutProgressionLaneDefinition lane, List<ValidationIssue> issues)
    {
        var byKey = lane.Stages.ToDictionary(s => s.StageKey, StringComparer.Ordinal);

        foreach (var stage in lane.Stages)
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
