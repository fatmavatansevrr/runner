using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Progression;

/// <summary>
/// Backend Integration Phase 4F.6A — immutable input to <see cref="IProgressionStageAllocator"/>.
/// Carries the already-resolved phase/week skeleton (Phase 4F.2/4F.3), the already-loaded
/// workout-progression definition, and the already-resolved runtime-condition results (Phase
/// 4D.5) — the allocator never reloads the catalog and never re-evaluates a runtime
/// condition itself (Section 7).
/// </summary>
public sealed class ProgressionStageAllocationContext
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required CatalogWorkoutProgressionDefinition Progression { get; init; }
    public required GeneratedCatalogPlanSkeleton Skeleton { get; init; }
    public required IReadOnlyList<RuntimeConditionResolutionResult> ConditionResults { get; init; }
}

public interface IProgressionStageAllocator
{
    GeneratedCatalogStageSchedule Allocate(ProgressionStageAllocationContext context);
}

/// <summary>
/// Backend Integration Phase 4F.6A — deterministic fine-grained KEY_SESSION stage
/// scheduler. See PHASE4F_6A_FINE_GRAINED_KEY_SESSION_STAGE_SCHEDULER.md for the full
/// algorithm write-up; this class implements it exactly:
/// <list type="number">
/// <item>Structural validation (Section 8/9): unique, present stage keys; unique relative
/// order per phase; every progression phase matches a skeleton phase and vice versa.</item>
/// <item>Eligibility + fallback resolution (Section 7), never re-evaluating a runtime
/// condition — only consuming <see cref="RuntimeConditionResolutionResult"/> values already
/// computed upstream.</item>
/// <item>Duplicate-effective-stage merge for fallback targets (Section 12): safest
/// deterministic interpretation is max-of-minimums, min-of-maximums.</item>
/// <item>Phase capacity resolution (Sections 9/10/11): compress (reduce Compressible
/// stages, highest RelativeOrder first, down to a floor of 1 exposure — Protected stages
/// are never touched) or extend (grow Extendable stages, highest RelativeOrder first, up to
/// their own Maximum — FixedExposure stages are simply deprioritized (tried only once every
/// Extendable candidate's own headroom is exhausted), never entirely excluded; see
/// <see cref="ApplyExtension"/>'s own remarks for why this reading of the existing enum was
/// chosen over a hard on/off gate).</item>
/// <item>Contiguous week-block layout in ascending RelativeOrder (Section 8/9 Step 5/6).</item>
/// </list>
/// </summary>
public sealed class ProgressionStageAllocator : IProgressionStageAllocator
{
    private static readonly IReadOnlyList<string> KnownConditionTypes = new[]
    {
        TimeAdequacyResolver.ConditionTypeValue,
        PaceSourceResolver.ConditionTypeValue,
        CoreEntryReadinessResolver.ConditionTypeValue,
        GoalFeasibilityResolver.ConditionTypeValue,
    };

    public GeneratedCatalogStageSchedule Allocate(ProgressionStageAllocationContext context)
    {
        var progression = context.Progression;
        var skeleton = context.Skeleton;

        var skeletonPhaseGroups = skeleton.Weeks
            .GroupBy(w => w.StageKey)
            .ToDictionary(g => g.Key, g => g.OrderBy(w => w.WeekNumber).ToList());

        var progressionPhaseKeys = progression.PhaseProgressions.Select(p => p.PhaseKey).ToHashSet();
        var skeletonPhaseKeys = skeletonPhaseGroups.Keys.ToHashSet();

        foreach (var missing in progressionPhaseKeys.Except(skeletonPhaseKeys))
        {
            throw new ProgressionStagePhaseMismatchException(
                $"Workout progression '{progression.Key}' v{progression.Version} declares phase '{missing}', " +
                "which has no matching phase in the resolved phase/week skeleton.");
        }

        foreach (var missing in skeletonPhaseKeys.Except(progressionPhaseKeys))
        {
            throw new ProgressionStagePhaseMismatchException(
                $"Phase/week skeleton contains phase '{missing}', which has no matching phase in workout progression " +
                $"'{progression.Key}' v{progression.Version}.");
        }

        var weeks = new List<ScheduledProgressionWeek>();
        var traceSteps = new List<StageAllocationDecisionTraceStep>();

        // Backend Integration Phase 10K-FREQ.6D.4D Split A: AllocatePhase itself is reused
        // completely unmodified (its own allocation mathematics — compression, extension,
        // minimum/maximum exposures, contiguous-block layout — is untouched). What changes
        // here is orchestration/cardinality only: it is now invoked once PER LANE, each
        // invocation receiving that lane's own, independent Stages list against the SAME
        // shared phaseWeeks (coordinated phase timeline, independent lane prescriptive
        // content — see the architecture report §8/§22). Each lane's own allocation is
        // computed with zero shared mutable state between lanes — SAME_ALGORITHM does not
        // imply SAME_LANE_STAGE_BINDING.
        foreach (var phaseProgression in progression.PhaseProgressions.OrderBy(p => p.PhaseKey, StringComparer.Ordinal))
        {
            var phaseWeeks = skeletonPhaseGroups[phaseProgression.PhaseKey];
            var lanes = phaseProgression.EffectiveLanes;

            var duplicateLaneOrdinals = lanes.GroupBy(l => l.LaneOrdinal).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateLaneOrdinals.Count > 0)
            {
                throw new ProgressionStageDuplicateLaneOrdinalException(
                    $"Phase '{phaseProgression.PhaseKey}' declares LaneOrdinal {duplicateLaneOrdinals[0]} more than once — " +
                    "every lane within one phase's progression must have a unique LaneOrdinal.");
            }

            foreach (var lane in lanes.OrderBy(l => l.LaneOrdinal))
            {
                var laneView = new CatalogPhaseWorkoutProgression { PhaseKey = phaseProgression.PhaseKey, Stages = lane.Stages };
                AllocatePhase(laneView, phaseWeeks, context, weeks, traceSteps, lane.LaneOrdinal);
            }
        }

        return new GeneratedCatalogStageSchedule
        {
            CandidateKey = context.CandidateKey,
            CandidateVersion = context.CandidateVersion,
            ProgressionArtifactKey = progression.Key,
            ProgressionArtifactVersion = progression.Version,
            AllocatorVersion = ProgressionStageAllocatorVersion.V1,
            Weeks = weeks.OrderBy(w => w.WeekNumber).ThenBy(w => w.LaneOrdinal).ToList(),
            Trace = new StageAllocationDecisionTrace { Steps = traceSteps },
        };
    }

    private void AllocatePhase(
        CatalogPhaseWorkoutProgression phaseProgression,
        IReadOnlyList<GeneratedCatalogWeekSkeleton> phaseWeeks,
        ProgressionStageAllocationContext context,
        List<ScheduledProgressionWeek> weeksOut,
        List<StageAllocationDecisionTraceStep> traceOut,
        int laneOrdinal = 0)
    {
        var phaseKey = phaseProgression.PhaseKey;
        var stages = phaseProgression.Stages;

        ValidateStructure(phaseKey, stages);

        var fallbackTargetKeys = stages.Where(s => !string.IsNullOrEmpty(s.FallbackStageKey))
            .Select(s => s.FallbackStageKey!)
            .ToHashSet();

        var topLevelStages = stages.Where(s => !fallbackTargetKeys.Contains(s.ProgressionStageKey))
            .OrderBy(s => s.RelativeOrder)
            .ToList();

        var stagesByKey = stages.ToDictionary(s => s.ProgressionStageKey);

        // Resolve each top-level requested stage to its effective stage, merging duplicates
        // caused by two or more requested stages resolving to the same fallback target
        // (Section 12).
        var activeByEffectiveKey = new Dictionary<string, ActiveStage>();
        var orderedActiveKeys = new List<string>();

        foreach (var requested in topLevelStages)
        {
            var (outcome, effective, requestedKeyIfFallback) = ResolveEligibility(requested, stagesByKey, context.ConditionResults);

            if (outcome == ProgressionStageEligibilityOutcome.IneligibleWithoutFallback)
            {
                throw new ProgressionStageIneligibleWithoutFallbackException(
                    $"Phase '{phaseKey}' stage '{requested.ProgressionStageKey}' is ineligible under the current runtime-condition " +
                    "results and has no configured fallback stage.");
            }

            if (activeByEffectiveKey.TryGetValue(effective.ProgressionStageKey, out var existing))
            {
                // TRUE convergence: a second (or later) distinct requested stage independently
                // resolves to the same effective target as an earlier one. This is the actual
                // scenario Section 12 describes ("duplicate effective stages caused by
                // fallback"), and is a genuine competing-constraints situation — use the
                // conservative merge (max of minimums, so neither obligation's floor is
                // under-provisioned; min of maximums, so the effective stage never promises
                // more than the MORE restrictive of the two intents allows).
                var mergedMin = Math.Max(existing.MinimumExposures, requested.MinimumExposures);
                var mergedMax = Math.Min(existing.MaximumExposures, requested.MaximumExposures);

                if (mergedMin > mergedMax)
                {
                    throw new ProgressionStageFallbackBoundsUnreconcilableException(
                        $"Phase '{phaseKey}' effective stage '{effective.ProgressionStageKey}' is reached by more than one " +
                        "requested stage, and merging their exposure bounds (max of minimums, min of maximums) produced an " +
                        $"impossible range (minimum {mergedMin} > maximum {mergedMax}).");
                }

                existing.MinimumExposures = mergedMin;
                existing.MaximumExposures = mergedMax;
                existing.ContributingRequestedKeys.Add(requested.ProgressionStageKey);
                existing.ConditionOutcome = ProgressionStageEligibilityOutcome.IneligibleWithFallback;
            }
            else
            {
                var active = new ActiveStage
                {
                    EffectiveStage = effective,
                    MinimumExposures = effective.MinimumExposures,
                    MaximumExposures = effective.MaximumExposures,
                    ConditionOutcome = outcome,
                    ContributingRequestedKeys = new List<string> { requested.ProgressionStageKey },
                };

                if (requestedKeyIfFallback is not null)
                {
                    // Single substitution (the only shape that exists in the current v10
                    // catalog: GOAL_PACE_REHEARSAL -> CURRENT_FITNESS_SPECIFIC_REHEARSAL). Not a
                    // competing-constraints scenario — the fallback exists to preserve the
                    // ORIGINAL stage's training intent using a substitute, so the effective
                    // stage adopts whichever bound is MORE generous between its own declared
                    // range and the original's (max of minimums, max of maximums). This is the
                    // "safest deterministic interpretation" in the sense of never silently
                    // shrinking a fallback's capacity below what either contributing source
                    // already explicitly sanctioned — documented as a V1 technical scheduler
                    // rule, not scientific evidence. Contrast the TRUE-convergence branch above,
                    // which is a genuine multi-source conflict and stays conservative.
                    active.MinimumExposures = Math.Max(active.MinimumExposures, requested.MinimumExposures);
                    active.MaximumExposures = Math.Max(active.MaximumExposures, requested.MaximumExposures);
                }

                activeByEffectiveKey[effective.ProgressionStageKey] = active;
                orderedActiveKeys.Add(effective.ProgressionStageKey);
            }
        }

        var activeStages = orderedActiveKeys.Select(k => activeByEffectiveKey[k])
            .OrderBy(a => a.EffectiveStage.RelativeOrder)
            .ToList();

        var availableWeeks = phaseWeeks.Count;
        var totalMinimum = activeStages.Sum(a => a.MinimumExposures);

        var compressionAction = ProgressionPhaseCompressionAction.NotRequired;
        string tieBreakUsed = "NONE";

        if (totalMinimum > availableWeeks)
        {
            compressionAction = ProgressionPhaseCompressionAction.Reduced;
            tieBreakUsed = "COMPRESSION_RELATIVE_ORDER_DESC_THEN_STAGE_KEY_ORDINAL";
            ApplyCompression(phaseKey, activeStages, availableWeeks, ref totalMinimum);
        }
        else if (totalMinimum < availableWeeks)
        {
            tieBreakUsed = "EXTENSION_RELATIVE_ORDER_DESC_THEN_STAGE_KEY_ORDINAL";
            ApplyExtension(phaseKey, activeStages, availableWeeks, totalMinimum);
        }

        // Contiguous block layout: ascending RelativeOrder is authoritative (Section 8),
        // regardless of which tie-break was used to decide *how many* exposures each stage
        // received above.
        var weekIndex = 0;
        foreach (var active in activeStages)
        {
            var finalCount = active.FinalAllocatedExposures ?? active.MinimumExposures;
            for (var i = 0; i < finalCount; i++)
            {
                var week = phaseWeeks[weekIndex];
                var isFallback = active.ContributingRequestedKeys.Count > 1 || active.ContributingRequestedKeys[0] != active.EffectiveStage.ProgressionStageKey;

                var allocationReason = active.AllocationReasons.Count > i
                    ? active.AllocationReasons[i]
                    : "MINIMUM_EXPOSURE_ALLOCATION";

                weeksOut.Add(new ScheduledProgressionWeek
                {
                    WeekNumber = week.WeekNumber,
                    PhaseKey = phaseKey,
                    LaneOrdinal = laneOrdinal,
                    ProgressionStageKey = active.EffectiveStage.ProgressionStageKey,
                    RequestedProgressionStageKey = isFallback ? active.ContributingRequestedKeys[0] : null,
                    StageRelativeOrder = active.EffectiveStage.RelativeOrder,
                    ConditionOutcome = active.ConditionOutcome,
                    FallbackOrigin = isFallback ? active.ContributingRequestedKeys[0] : null,
                    AllocationReason = allocationReason,
                });

                traceOut.Add(new StageAllocationDecisionTraceStep
                {
                    PhaseKey = phaseKey,
                    WeekNumber = week.WeekNumber,
                    LaneOrdinal = laneOrdinal,
                    RequestedStageKey = isFallback ? active.ContributingRequestedKeys[0] : active.EffectiveStage.ProgressionStageKey,
                    EffectiveStageKey = active.EffectiveStage.ProgressionStageKey,
                    RelativeOrder = active.EffectiveStage.RelativeOrder,
                    AllocationKind = i < active.MinimumExposuresBeforeExtension
                        ? ProgressionStageAllocationKind.MinimumExposure
                        : ProgressionStageAllocationKind.ExtensionExposure,
                    CompressionAction = compressionAction,
                    ConditionOutcome = active.ConditionOutcome,
                    FallbackOrigin = isFallback ? active.ContributingRequestedKeys[0] : null,
                    TieBreakUsed = tieBreakUsed,
                    SourceArtifactKey = context.Progression.Key,
                    SourceArtifactVersion = context.Progression.Version,
                });

                weekIndex++;
            }
        }

        if (weekIndex != availableWeeks)
        {
            // Defensive — should be unreachable given the capacity checks above already
            // guarantee an exact fit before this point is reached.
            throw new ProgressionPhaseCapacityInsufficientException(
                $"Phase '{phaseKey}' allocation produced {weekIndex} assigned weeks but the skeleton has " +
                $"{availableWeeks} available weeks.");
        }
    }

    private static void ValidateStructure(string phaseKey, IReadOnlyList<CatalogWorkoutProgressionStage> stages)
    {
        foreach (var stage in stages)
        {
            if (string.IsNullOrWhiteSpace(stage.ProgressionStageKey))
            {
                throw new ProgressionStageDuplicateOrMissingKeyException(
                    $"Phase '{phaseKey}' contains a stage with a missing/blank ProgressionStageKey.");
            }
        }

        var duplicateKeys = stages.GroupBy(s => s.ProgressionStageKey).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateKeys.Count > 0)
        {
            throw new ProgressionStageDuplicateOrMissingKeyException(
                $"Phase '{phaseKey}' declares duplicate ProgressionStageKey value(s): {string.Join(", ", duplicateKeys)}.");
        }

        var duplicateOrders = stages.GroupBy(s => s.RelativeOrder).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateOrders.Count > 0)
        {
            throw new ProgressionStageDuplicateRelativeOrderException(
                $"Phase '{phaseKey}' declares more than one stage with RelativeOrder {string.Join(", ", duplicateOrders)}.");
        }
    }

    /// <summary>
    /// Resolves one top-level requested stage's eligibility, walking its fallback chain
    /// (Section 7) without ever re-evaluating a runtime condition — only consulting
    /// <paramref name="conditionResults"/>, which the caller already produced upstream.
    /// Returns the outcome as seen from the ORIGINAL requested stage's perspective, the
    /// effective stage that will actually be scheduled, and (if a fallback was taken) the
    /// original requested stage's own key for trace/merge purposes.
    /// </summary>
    private static (ProgressionStageEligibilityOutcome Outcome, CatalogWorkoutProgressionStage Effective, string? RequestedKeyIfFallback)
        ResolveEligibility(
            CatalogWorkoutProgressionStage requested,
            IReadOnlyDictionary<string, CatalogWorkoutProgressionStage> stagesByKey,
            IReadOnlyList<RuntimeConditionResolutionResult> conditionResults)
    {
        var visited = new HashSet<string> { requested.ProgressionStageKey };
        var current = requested;
        var tookFallback = false;

        while (true)
        {
            var isEligible = IsStageEligible(current, conditionResults);
            var isNotConditioned = current.Requires.Count == 0;

            if (isNotConditioned)
            {
                return (tookFallback ? ProgressionStageEligibilityOutcome.IneligibleWithFallback : ProgressionStageEligibilityOutcome.NotConditioned,
                    current, tookFallback ? requested.ProgressionStageKey : null);
            }

            if (isEligible)
            {
                return (tookFallback ? ProgressionStageEligibilityOutcome.IneligibleWithFallback : ProgressionStageEligibilityOutcome.Eligible,
                    current, tookFallback ? requested.ProgressionStageKey : null);
            }

            if (string.IsNullOrEmpty(current.FallbackStageKey))
            {
                return (ProgressionStageEligibilityOutcome.IneligibleWithoutFallback, current, null);
            }

            var matches = stagesByKey.Values.Where(s => s.ProgressionStageKey == current.FallbackStageKey).ToList();
            if (matches.Count == 0)
            {
                throw new ProgressionStageFallbackTargetNotFoundException(
                    $"Stage '{current.ProgressionStageKey}' declares fallbackStageKey '{current.FallbackStageKey}', which does not " +
                    "match any stage in the same phase's progression (outside the valid progression).");
            }

            if (matches.Count > 1)
            {
                throw new ProgressionStageAmbiguousFallbackException(
                    $"Stage '{current.ProgressionStageKey}' declares fallbackStageKey '{current.FallbackStageKey}', which matches " +
                    $"{matches.Count} stages in the same phase — ambiguous fallback target.");
            }

            var target = matches[0];
            if (!visited.Add(target.ProgressionStageKey))
            {
                throw new ProgressionStageFallbackCycleException(
                    $"Fallback chain starting at '{requested.ProgressionStageKey}' revisits stage '{target.ProgressionStageKey}' " +
                    "— a fallback cycle.");
            }

            tookFallback = true;
            current = target;
        }
    }

    private static bool IsStageEligible(CatalogWorkoutProgressionStage stage, IReadOnlyList<RuntimeConditionResolutionResult> conditionResults)
    {
        foreach (var condition in stage.Requires)
        {
            if (!KnownConditionTypes.Contains(condition.ConditionType))
            {
                throw new ProgressionStageUnknownConditionKeyException(
                    $"Stage '{stage.ProgressionStageKey}' requires unknown condition type '{condition.ConditionType}'.");
            }

            var result = conditionResults.FirstOrDefault(r => r.ConditionType == condition.ConditionType);
            if (result is null)
            {
                throw new ProgressionStageConditionResultMissingException(
                    $"Stage '{stage.ProgressionStageKey}' requires condition '{condition.ConditionType}', but no resolved result " +
                    "for that condition type was supplied to the scheduler.");
            }

            var satisfied = result.Status == RuntimeConditionResolutionStatus.Evaluated
                && result.OutputValue is not null
                && condition.AllowedValues.Contains(result.OutputValue);

            if (!satisfied)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Section 10: reduce Compressible active stages, highest RelativeOrder first then
    /// ProgressionStageKey ordinal, down to a floor of 1 exposure each (Protected stages are
    /// never touched; this floor is the literal reading of "reduce" against the only fields
    /// the catalog actually declares — no new "compressed minimum" field is invented).
    /// </summary>
    private static void ApplyCompression(string phaseKey, List<ActiveStage> activeStages, int availableWeeks, ref int totalMinimum)
    {
        var deficit = totalMinimum - availableWeeks;

        var candidates = activeStages
            .Where(a => a.EffectiveStage.CompressionBehavior == CatalogStageCompressionBehavior.Compressible)
            .OrderByDescending(a => a.EffectiveStage.RelativeOrder)
            .ThenBy(a => a.EffectiveStage.ProgressionStageKey, StringComparer.Ordinal)
            .ToList();

        var totalHeadroom = candidates.Sum(a => a.MinimumExposures - 1);
        if (totalHeadroom < deficit)
        {
            throw new ProgressionPhaseCapacityInsufficientException(
                $"Phase '{phaseKey}' requires {totalMinimum} minimum exposures across {activeStages.Count} active stage(s), " +
                $"but only has {availableWeeks} available week(s). Maximum permitted compression reduces this by " +
                $"{totalHeadroom}, which is insufficient (deficit {deficit}).");
        }

        foreach (var candidate in candidates)
        {
            if (deficit <= 0)
            {
                break;
            }

            var reducible = candidate.MinimumExposures - 1;
            var reduceBy = Math.Min(reducible, deficit);
            if (reduceBy <= 0)
            {
                continue;
            }

            candidate.MinimumExposures -= reduceBy;
            candidate.FinalAllocatedExposures = candidate.MinimumExposures;
            candidate.MinimumExposuresBeforeExtension = candidate.MinimumExposures;
            for (var i = 0; i < candidate.MinimumExposures; i++)
            {
                candidate.AllocationReasons.Add("COMPRESSION_REDUCED_ALLOCATION");
            }

            deficit -= reduceBy;
        }

        foreach (var stage in activeStages.Where(a => a.FinalAllocatedExposures is null))
        {
            stage.FinalAllocatedExposures = stage.MinimumExposures;
            stage.MinimumExposuresBeforeExtension = stage.MinimumExposures;
            for (var i = 0; i < stage.MinimumExposures; i++)
            {
                stage.AllocationReasons.Add("MINIMUM_EXPOSURE_ALLOCATION");
            }
        }

        totalMinimum = activeStages.Sum(a => a.FinalAllocatedExposures!.Value);
    }

    /// <summary>
    /// Section 11: distribute surplus weeks to Extendable active stages, highest
    /// RelativeOrder first then ProgressionStageKey ordinal, greedily filling each
    /// candidate's headroom (Maximum - Minimum) before moving to the next, up to their own
    /// Maximum.
    ///
    /// Section 11's own tie-break wording — "highest extension eligibility FIRST; then
    /// highest RelativeOrder; then ProgressionStageKey ordinal" — is a three-level SORT, not
    /// a binary filter (contrast Section 10's compression wording, "reduce ONLY stages whose
    /// current compression behavior PERMITS reduction," which IS an explicit hard gate — see
    /// <see cref="ApplyCompression"/>). Read that way, Extendable stages are simply tried
    /// before FixedExposure stages, not to the total exclusion of the latter: a FixedExposure
    /// stage may still absorb surplus, up to its own declared Maximum, once every Extendable
    /// candidate's own headroom is exhausted. This is the reading that makes the real,
    /// current v10 RACE_SPECIFIC phase's own exposure/extension-behavior values actually
    /// produce a valid 4-week default schedule (GOAL_PACE_REHEARSAL, though FixedExposure,
    /// still has its own Maximum=2 available once TEN_K_SPECIFIC_INTRO's smaller headroom is
    /// used up) — documented explicitly here as a V1 technical scheduler rule, not scientific
    /// evidence, per this phase's own instruction.
    /// </summary>
    private static void ApplyExtension(string phaseKey, List<ActiveStage> activeStages, int availableWeeks, int totalMinimum)
    {
        var surplus = availableWeeks - totalMinimum;

        foreach (var stage in activeStages)
        {
            stage.MinimumExposuresBeforeExtension = stage.MinimumExposures;
            stage.FinalAllocatedExposures = stage.MinimumExposures;
            for (var i = 0; i < stage.MinimumExposures; i++)
            {
                stage.AllocationReasons.Add("MINIMUM_EXPOSURE_ALLOCATION");
            }
        }

        var candidates = activeStages
            .OrderBy(a => a.EffectiveStage.ExtensionBehavior == CatalogStageExtensionBehavior.Extendable ? 0 : 1)
            .ThenByDescending(a => a.EffectiveStage.RelativeOrder)
            .ThenBy(a => a.EffectiveStage.ProgressionStageKey, StringComparer.Ordinal)
            .ToList();

        var totalHeadroom = candidates.Sum(a => a.MaximumExposures - a.MinimumExposures);
        if (totalHeadroom < surplus)
        {
            throw new ProgressionPhaseCapacityExceedsMaximumException(
                $"Phase '{phaseKey}' has {availableWeeks} available week(s), but its active stages' combined maximum " +
                $"exposures can absorb at most {totalMinimum + totalHeadroom} (minimum {totalMinimum} + extension headroom " +
                $"{totalHeadroom}). Excess of {surplus - totalHeadroom} week(s) beyond permitted maximum.");
        }

        foreach (var candidate in candidates)
        {
            if (surplus <= 0)
            {
                break;
            }

            var headroom = candidate.MaximumExposures - candidate.MinimumExposures;
            var growBy = Math.Min(headroom, surplus);
            if (growBy <= 0)
            {
                continue;
            }

            for (var i = 0; i < growBy; i++)
            {
                candidate.AllocationReasons.Add("EXTENSION_ALLOCATION");
            }

            candidate.FinalAllocatedExposures = candidate.MinimumExposures + growBy;
            surplus -= growBy;
        }
    }

    private sealed class ActiveStage
    {
        public required CatalogWorkoutProgressionStage EffectiveStage { get; init; }
        public int MinimumExposures { get; set; }
        public int MaximumExposures { get; set; }
        public int? FinalAllocatedExposures { get; set; }
        public int MinimumExposuresBeforeExtension { get; set; }
        public required ProgressionStageEligibilityOutcome ConditionOutcome { get; set; }
        public required List<string> ContributingRequestedKeys { get; init; }
        public List<string> AllocationReasons { get; } = new();
    }
}
