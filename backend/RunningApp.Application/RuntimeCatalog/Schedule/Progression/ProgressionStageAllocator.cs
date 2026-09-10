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
/// stages — by ascending catalog-declared CompressionPriority when declared [Backend
/// Integration Phase HM.5.2A], else highest RelativeOrder first — down to a floor of 1
/// exposure — Protected stages are never touched) or extend (grow Extendable stages, highest
/// RelativeOrder first, up to
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

                // Backend Integration Phase HM.5.2: PreferredExposures merges conservatively,
                // mirroring the min-of-maximums rule above — only defined when BOTH contributing
                // requested stages explicitly declare one (an undeclared side cannot contribute a
                // safe baseline), clamped into the merged [min,max] range. Not exercised by any
                // current real catalog data (no stage that reaches this true-convergence branch
                // today declares PreferredExposures on either side), so this is a documented,
                // defensible convention rather than an empirically-verified path.
                existing.PreferredExposures = CombineConservativePreferred(existing.PreferredExposures, requested.PreferredExposures, mergedMin, mergedMax);
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
                    PreferredExposures = effective.PreferredExposures,
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
                    active.PreferredExposures = CombineGenerousPreferred(active.PreferredExposures, requested.PreferredExposures);
                }

                activeByEffectiveKey[effective.ProgressionStageKey] = active;
                orderedActiveKeys.Add(effective.ProgressionStageKey);
            }
        }

        var activeStages = orderedActiveKeys.Select(k => activeByEffectiveKey[k])
            .OrderBy(a => a.EffectiveStage.RelativeOrder)
            .ToList();

        // Phase 10K-GEN.17: a lane's real capacity is the count of weeks that structurally
        // carry a slot for THIS lane, not phaseWeeks.Count. A week is eligible for lane N
        // iff it has more than N KEY_SESSION-role slots (0-based occurrence-ordinal), the
        // exact same "structural ordinal N binds to LaneOrdinal N" rule CatalogWorkoutBinder
        // already applies (see CatalogWorkoutBinder's own structuralOrdinalByRole comment) —
        // reusing the SAME per-week SessionSlots.StructuralRole data
        // CatalogStageToWeekMaterializer.ResolveWeekRoles already resolved (including 2D's
        // Model B pattern via GEN.12's WeeklyPatternRoles), not a second, independently
        // re-derived pattern-resolution mechanism. For every pre-GEN.12 frequency, every
        // lane's role is structurally present in 100% of a phase's weeks (GEN.13 §2 confirmed
        // this by direct inspection of every existing progression document), so
        // eligibleWeeks is always identical, in the same order, to phaseWeeks — this is
        // byte-identical to pre-GEN.17 behavior for every already-shipped candidate.
        //
        // A week with an EMPTY SessionSlots list (no structural data supplied at all -- e.g. a
        // synthetic skeleton built directly for an allocator-only unit test, never routed
        // through the real materializer) is a genuinely different case from a week with real,
        // non-empty SessionSlots that simply has zero KEY_SESSION-role entries (2D's real
        // Pattern-B week). The former carries no information to filter on and must remain
        // eligible (preserving the pre-GEN.17 "every phaseWeeks week is available" default for
        // every caller that never populates structural slot data); only the latter is a real,
        // structurally-absent lane.
        var eligibleWeeks = phaseWeeks
            .Where(w => w.SessionSlots.Count == 0
                || w.SessionSlots.Count(s => s.StructuralRole == ScheduledProgressionWeek.KeySessionStructuralRole) > laneOrdinal)
            .OrderBy(w => w.WeekNumber)
            .ToList();

        var availableWeeks = eligibleWeeks.Count;

        // Backend Integration Phase HM.5.2: the allocator's working baseline for the
        // exact-fit/compression/extension regimes (Section 3/4) is each stage's
        // PreferredExposures when the catalog declares one, else MinimumExposures — byte-identical
        // to pre-HM.5.2 behavior for every stage that leaves PreferredExposures null (every 10K
        // stage, every pre-HM.5.2 HM stage).
        var totalBaseline = activeStages.Sum(a => a.Baseline);

        var compressionAction = ProgressionPhaseCompressionAction.NotRequired;
        string tieBreakUsed = "NONE";

        if (totalBaseline > availableWeeks)
        {
            compressionAction = ProgressionPhaseCompressionAction.Reduced;
            tieBreakUsed = "COMPRESSION_RELATIVE_ORDER_DESC_THEN_STAGE_KEY_ORDINAL";
            ApplyCompression(phaseKey, activeStages, availableWeeks, ref totalBaseline);
        }
        else if (totalBaseline < availableWeeks)
        {
            tieBreakUsed = "EXTENSION_RELATIVE_ORDER_DESC_THEN_STAGE_KEY_ORDINAL";
            ApplyExtension(phaseKey, activeStages, availableWeeks, totalBaseline);
        }

        // Contiguous block layout: ascending RelativeOrder is authoritative (Section 8),
        // regardless of which tie-break was used to decide *how many* exposures each stage
        // received above.
        var weekIndex = 0;
        foreach (var active in activeStages)
        {
            // Backend Integration Phase HM.5.2: the exact-fit regime (Section 3, Regime B —
            // totalBaseline == availableWeeks) runs neither ApplyCompression nor ApplyExtension,
            // so FinalAllocatedExposures is still null here. It must fall back to each stage's
            // Baseline (Preferred when declared, else Minimum) — NOT the raw MinimumExposures
            // field — or a stage that declares PreferredExposures > MinimumExposures would be
            // silently under-allocated at its own preferred phase length. Byte-identical to the
            // pre-HM.5.2 fallback for every stage that leaves PreferredExposures null.
            var finalCount = active.FinalAllocatedExposures ?? active.Baseline;
            for (var i = 0; i < finalCount; i++)
            {
                var week = eligibleWeeks[weekIndex];
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
                    AllocationKind = i < active.BaselineExposuresBeforeExtension
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

            // Backend Integration Phase HM.5.2: a stage that opts into the new optional
            // PreferredExposures baseline must declare it within its own bounds. Unreachable
            // for every pre-HM.5.2 stage (PreferredExposures is null there).
            if (stage.PreferredExposures is { } preferred
                && (preferred < stage.MinimumExposures || preferred > stage.MaximumExposures))
            {
                throw new ProgressionStagePreferredExposuresOutOfBoundsException(
                    $"Phase '{phaseKey}' stage '{stage.ProgressionStageKey}' declares PreferredExposures {preferred}, " +
                    $"which is outside its own [MinimumExposures {stage.MinimumExposures}, MaximumExposures {stage.MaximumExposures}] range.");
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
    /// Section 10: reduce Compressible active stages down to each stage's own compression
    /// floor. Selection order: ascending catalog-declared
    /// <see cref="CatalogWorkoutProgressionStage.CompressionPriority"/> first (lower value
    /// removed earlier) when a stage declares one, then (for stages that leave it null, or as
    /// the tie-break among equal/undeclared priorities) descending RelativeOrder, then
    /// ProgressionStageKey ordinal — byte-identical to the pre-HM.5.2A order
    /// (RelativeOrder descending, then key ordinal) whenever no active Compressible stage in
    /// the phase declares CompressionPriority, which is true for every 10K stage and every
    /// pre-HM.5.2A HM stage (Backend Integration Phase HM.5.2A — see
    /// <see cref="CatalogWorkoutProgressionStage.CompressionPriority"/>'s own remarks for why
    /// this is a distinct authority from RelativeOrder's extension-fill and chronological-order
    /// meanings).
    ///
    /// Backend Integration Phase HM.5.2: the floor is no longer unconditionally hardcoded to 1.
    /// For a stage that leaves the new, optional PreferredExposures baseline unset (every
    /// pre-HM.5.2 stage), the baseline IS MinimumExposures, and — exactly as before this
    /// phase — the only floor the algorithm can express below an undifferentiated single field
    /// is the literal constant 1 (no separate "compressed minimum" existed). For a stage that
    /// DOES declare PreferredExposures, MinimumExposures becomes a genuine, catalog-declared
    /// hard floor distinct from the baseline being compressed — reducing below it would silently
    /// violate the very authority the new field exists to make representable. This is a strict
    /// generalization: every pre-HM.5.2 stage computes the identical floor (1) it always did.
    /// </summary>
    private static void ApplyCompression(string phaseKey, List<ActiveStage> activeStages, int availableWeeks, ref int totalBaseline)
    {
        var deficit = totalBaseline - availableWeeks;

        var candidates = activeStages
            .Where(a => a.EffectiveStage.CompressionBehavior == CatalogStageCompressionBehavior.Compressible)
            .OrderBy(a => a.EffectiveStage.CompressionPriority ?? int.MaxValue)
            .ThenByDescending(a => a.EffectiveStage.RelativeOrder)
            .ThenBy(a => a.EffectiveStage.ProgressionStageKey, StringComparer.Ordinal)
            .ToList();

        var totalHeadroom = candidates.Sum(a => a.Baseline - CompressionFloor(a));
        if (totalHeadroom < deficit)
        {
            throw new ProgressionPhaseCapacityInsufficientException(
                $"Phase '{phaseKey}' requires {totalBaseline} baseline exposures across {activeStages.Count} active stage(s), " +
                $"but only has {availableWeeks} available week(s). Maximum permitted compression reduces this by " +
                $"{totalHeadroom}, which is insufficient (deficit {deficit}).");
        }

        foreach (var candidate in candidates)
        {
            if (deficit <= 0)
            {
                break;
            }

            var reducible = candidate.Baseline - CompressionFloor(candidate);
            var reduceBy = Math.Min(reducible, deficit);
            if (reduceBy <= 0)
            {
                continue;
            }

            candidate.FinalAllocatedExposures = candidate.Baseline - reduceBy;
            candidate.BaselineExposuresBeforeExtension = candidate.FinalAllocatedExposures.Value;
            for (var i = 0; i < candidate.FinalAllocatedExposures; i++)
            {
                candidate.AllocationReasons.Add("COMPRESSION_REDUCED_ALLOCATION");
            }

            deficit -= reduceBy;
        }

        foreach (var stage in activeStages.Where(a => a.FinalAllocatedExposures is null))
        {
            stage.FinalAllocatedExposures = stage.Baseline;
            stage.BaselineExposuresBeforeExtension = stage.Baseline;
            for (var i = 0; i < stage.Baseline; i++)
            {
                stage.AllocationReasons.Add("MINIMUM_EXPOSURE_ALLOCATION");
            }
        }

        totalBaseline = activeStages.Sum(a => a.FinalAllocatedExposures!.Value);
    }

    /// <summary>See <see cref="ApplyCompression"/>'s own remarks — the compression floor is the
    /// stage's genuine MinimumExposures only when it also declares a distinct PreferredExposures
    /// baseline; otherwise it stays the historical hardcoded 1.</summary>
    private static int CompressionFloor(ActiveStage stage) => stage.PreferredExposures.HasValue ? stage.MinimumExposures : 1;

    /// <summary>
    /// Backend Integration Phase HM.5.2 — conservative merge for Section 12's TRUE-convergence
    /// branch: only defined (non-null) when BOTH contributing sides declare a PreferredExposures
    /// value; takes the smaller of the two, clamped into the already-merged [min,max] range.
    /// </summary>
    private static int? CombineConservativePreferred(int? existing, int? requested, int mergedMin, int mergedMax)
    {
        if (existing is null || requested is null)
        {
            return null;
        }

        return Math.Clamp(Math.Min(existing.Value, requested.Value), mergedMin, mergedMax);
    }

    /// <summary>
    /// Backend Integration Phase HM.5.2 — generous merge for Section 12's single-substitution
    /// branch (mirrors the existing max-of-minimums/max-of-maximums rule for that branch): the
    /// larger of the two declared values, or whichever one is declared if only one is.
    /// </summary>
    private static int? CombineGenerousPreferred(int? existing, int? requested)
    {
        if (existing is null)
        {
            return requested;
        }

        if (requested is null)
        {
            return existing;
        }

        return Math.Max(existing.Value, requested.Value);
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
    /// <summary>
    /// Backend Integration Phase HM.5.2: extension now grows each stage from its Baseline
    /// (PreferredExposures when declared, else MinimumExposures — byte-identical to before this
    /// phase for every stage that leaves PreferredExposures null) up toward MaximumExposures.
    /// This is what makes Regime B (Section 3 — totalBaseline == availableWeeks, zero surplus)
    /// resolve each stage at its own Preferred value: this method is not even invoked in that
    /// case (surplus is computed as 0 by the caller's branch condition), so the exact-fit
    /// fallback in <see cref="AllocatePhase"/> (Baseline) is what actually answers Regime B —
    /// documented here because the same Baseline concept both methods share is the crux of the
    /// whole fix (Section 7).
    /// </summary>
    private static void ApplyExtension(string phaseKey, List<ActiveStage> activeStages, int availableWeeks, int totalBaseline)
    {
        var surplus = availableWeeks - totalBaseline;

        foreach (var stage in activeStages)
        {
            stage.BaselineExposuresBeforeExtension = stage.Baseline;
            stage.FinalAllocatedExposures = stage.Baseline;
            for (var i = 0; i < stage.Baseline; i++)
            {
                stage.AllocationReasons.Add("MINIMUM_EXPOSURE_ALLOCATION");
            }
        }

        var candidates = activeStages
            .OrderBy(a => a.EffectiveStage.ExtensionBehavior == CatalogStageExtensionBehavior.Extendable ? 0 : 1)
            .ThenByDescending(a => a.EffectiveStage.RelativeOrder)
            .ThenBy(a => a.EffectiveStage.ProgressionStageKey, StringComparer.Ordinal)
            .ToList();

        var totalHeadroom = candidates.Sum(a => a.MaximumExposures - a.Baseline);
        if (totalHeadroom < surplus)
        {
            throw new ProgressionPhaseCapacityExceedsMaximumException(
                $"Phase '{phaseKey}' has {availableWeeks} available week(s), but its active stages' combined maximum " +
                $"exposures can absorb at most {totalBaseline + totalHeadroom} (baseline {totalBaseline} + extension headroom " +
                $"{totalHeadroom}). Excess of {surplus - totalHeadroom} week(s) beyond permitted maximum.");
        }

        foreach (var candidate in candidates)
        {
            if (surplus <= 0)
            {
                break;
            }

            var headroom = candidate.MaximumExposures - candidate.Baseline;
            var growBy = Math.Min(headroom, surplus);
            if (growBy <= 0)
            {
                continue;
            }

            for (var i = 0; i < growBy; i++)
            {
                candidate.AllocationReasons.Add("EXTENSION_ALLOCATION");
            }

            candidate.FinalAllocatedExposures = candidate.Baseline + growBy;
            surplus -= growBy;
        }
    }

    private sealed class ActiveStage
    {
        public required CatalogWorkoutProgressionStage EffectiveStage { get; init; }
        public int MinimumExposures { get; set; }
        public int MaximumExposures { get; set; }

        /// <summary>Backend Integration Phase HM.5.2 — see <see cref="CatalogWorkoutProgressionStage.PreferredExposures"/>. Carried on the active/merged copy so fallback-merge resolution (Section 12) can combine it independently of the effective stage's own catalog-declared value.</summary>
        public int? PreferredExposures { get; set; }

        /// <summary>Backend Integration Phase HM.5.2 — the allocator's working exposure baseline: PreferredExposures when declared, else MinimumExposures. Byte-identical to MinimumExposures for every stage that leaves PreferredExposures null.</summary>
        public int Baseline => PreferredExposures ?? MinimumExposures;

        public int? FinalAllocatedExposures { get; set; }
        public int BaselineExposuresBeforeExtension { get; set; }
        public required ProgressionStageEligibilityOutcome ConditionOutcome { get; set; }
        public required List<string> ContributingRequestedKeys { get; init; }
        public List<string> AllocationReasons { get; } = new();
    }
}
