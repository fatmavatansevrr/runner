using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

internal enum GoalPaceOutcomeStatus
{
    /// <summary>Evaluated, satisfied -- GOAL_PACE_TEN_K scheduled directly, no fallback.</summary>
    Eligible,

    /// <summary>Evaluated, not satisfied -- routes to the fallback stage, and the fallback is itself confirmed reachable by the real scheduler.</summary>
    FallbackConfirmed,

    /// <summary>NotEvaluated -- routes to fallback via the mechanism TD-NOTEVALUATED-FALLBACK-001 already flagged as not yet product-approved.</summary>
    UncertainNotEvaluated,

    /// <summary>
    /// Neither Eligible nor a confirmed fallback was observed from the real
    /// scheduler for this outcome -- a genuine structural gap. Not one of
    /// the three states the task originally enumerated; added because the
    /// task explicitly allows extending the enum for this case, and every
    /// outcome this verifier checks must map to exactly one status with no
    /// silent "everything is fine" default.
    /// </summary>
    StructurallyUnreachable,
}

internal enum GoalPaceReachabilityOutcome
{
    /// <summary>Every registered value maps to Eligible or FallbackConfirmed -- no gaps, no open risk. Not achievable today (TD-NOTEVALUATED-FALLBACK-001 is still open).</summary>
    Pass,

    /// <summary>Every value resolves structurally, but at least one UncertainNotEvaluated case exists -- the expected, honest result today.</summary>
    PassWithOpenRisk,

    /// <summary>At least one registered value has no eligible or confirmed-fallback path at all.</summary>
    Fail,

    NotApplicable,
}

internal sealed record GoalPaceOutcomeCheck(string GoalFeasibilityValue, GoalPaceOutcomeStatus Status, string ReasonCode);

internal sealed record GoalPaceReachabilityVerificationResult(
    IReadOnlyList<GoalPaceOutcomeCheck> OutcomeChecks,
    GoalPaceReachabilityOutcome OverallOutcome,
    IReadOnlyList<string> Findings);

/// <summary>
/// Phase 4G.3B.3, verifier 5 of 9. Narrower than StageReachabilityVerifier:
/// it does not re-prove stage placement (already confirmed there) -- it
/// answers, for every value GOAL_FEASIBILITY_IN can actually take (per the
/// real runtime-condition-values registry, supplied by the caller) plus the
/// distinct NotEvaluated resolver status, whether GOAL_PACE_REHEARSAL's
/// eligibility/fallback decision is confirmed correct and complete,
/// including the exact TD-NOTEVALUATED-FALLBACK-001 gap.
///
/// Reuse, not duplication: this verifier calls the real
/// ProgressionStageAllocator.Allocate directly -- the same public entry
/// point StageReachabilityVerifier itself calls -- rather than
/// reimplementing any part of its private fallback-chain-walking or
/// eligibility algorithm. It deliberately does NOT route through the
/// sibling stage-reachability verifier's own public entry point: doing so
/// would make this file a second production-code call site of that method,
/// which would defeat that verifier's own test suite's "no production call
/// site" regression guard (that test conservatively treats any call
/// anywhere under RunningApp.Application, other than its own definition
/// file, as a live-reachability concern -- correctly so, since it cannot
/// distinguish "called by another dark verifier" from "called by something
/// that leads to a live path"). Calling the allocator directly avoids that
/// false positive while still reusing the actual decision-making code.
///
/// The only genuinely duplicated code is a small amount of unavoidable
/// context-object construction glue (mirroring
/// StageReachabilityVerifier.BuildSkeleton, which is private and not
/// exposed for reuse) -- plain field mapping, not eligibility or fallback
/// decision logic. The same set of ProgressionStageSchedulingException
/// subtypes StageReachabilityVerifier catches is caught here too, via the
/// shared abstract base type, so this verifier never throws for a
/// structural scheduling failure -- it reports StructurallyUnreachable
/// instead.
///
/// Pure given its inputs: no I/O, no live catalog loading, no readiness or
/// request-context dependency beyond what is passed in. Not called from
/// any live request path.
/// </summary>
internal static class GoalPaceReachabilityVerifier
{
    private const string GoalPaceStageKey = "GOAL_PACE_REHEARSAL";
    private const string RaceSpecificPhaseKey = "RACE_SPECIFIC";

    public static GoalPaceReachabilityVerificationResult Verify(
        PhaseAllocationResult allocation,
        CatalogWorkoutProgressionDefinition progression,
        IReadOnlyList<string> weeklySlotRoles,
        CatalogWorkoutProgressionStage goalPaceStage,
        IReadOnlySet<string> registeredGoalFeasibilityValues)
    {
        if (!allocation.IsMathematicallyFeasible)
        {
            return new([], GoalPaceReachabilityOutcome.NotApplicable,
                ["NOT_APPLICABLE_TARGET_MATHEMATICALLY_INFEASIBLE: no allocation exists for this target, so there is no schedule to check GOAL_PACE_REHEARSAL reachability against."]);
        }

        if (goalPaceStage.ProgressionStageKey != GoalPaceStageKey
            || goalPaceStage.Requires.Count != 1
            || goalPaceStage.Requires[0].ConditionType != GoalFeasibilityResolver.ConditionTypeValue
            || string.IsNullOrWhiteSpace(goalPaceStage.FallbackStageKey))
        {
            return new([], GoalPaceReachabilityOutcome.NotApplicable,
                [$"NOT_APPLICABLE_UNEXPECTED_STAGE_SHAPE: expected '{GoalPaceStageKey}' to declare exactly one Requires condition of type {GoalFeasibilityResolver.ConditionTypeValue} and a non-blank FallbackStageKey; the supplied stage definition does not match. This verifier's model does not apply to a differently-shaped stage."]);
        }

        var allowedValues = goalPaceStage.Requires[0].AllowedValues;
        var checks = new List<GoalPaceOutcomeCheck>();

        foreach (var value in registeredGoalFeasibilityValues.OrderBy(v => v, StringComparer.Ordinal))
        {
            var conditions = new[]
            {
                RuntimeConditionResolutionResult.Evaluated(
                    GoalFeasibilityResolver.ConditionTypeValue, value, "GOAL_PACE_REACHABILITY_VERIFIER_SYNTHETIC"),
            };
            checks.Add(CheckOutcome(allocation, progression, weeklySlotRoles, goalPaceStage, allowedValues.Contains(value), value, conditions));
        }

        var notEvaluatedConditions = new[]
        {
            RuntimeConditionResolutionResult.NotEvaluated(
                GoalFeasibilityResolver.ConditionTypeValue, "GOAL_PACE_REACHABILITY_VERIFIER_SYNTHETIC_NOT_EVALUATED"),
        };
        checks.Add(CheckNotEvaluated(allocation, progression, weeklySlotRoles, goalPaceStage, notEvaluatedConditions));

        var overall = checks.Any(c => c.Status == GoalPaceOutcomeStatus.StructurallyUnreachable)
            ? GoalPaceReachabilityOutcome.Fail
            : checks.Any(c => c.Status == GoalPaceOutcomeStatus.UncertainNotEvaluated)
                ? GoalPaceReachabilityOutcome.PassWithOpenRisk
                : GoalPaceReachabilityOutcome.Pass;

        return new(checks, overall, []);
    }

    private static GoalPaceOutcomeCheck CheckOutcome(
        PhaseAllocationResult allocation,
        CatalogWorkoutProgressionDefinition progression,
        IReadOnlyList<string> weeklySlotRoles,
        CatalogWorkoutProgressionStage goalPaceStage,
        bool expectedEligible,
        string value,
        IReadOnlyList<RuntimeConditionResolutionResult> conditions)
    {
        var exposure = ScheduleGoalPaceExposure(allocation, progression, weeklySlotRoles, goalPaceStage, conditions);

        if (exposure is null)
        {
            return new(value, GoalPaceOutcomeStatus.StructurallyUnreachable,
                $"GOAL_FEASIBILITY_IN={value}: the real scheduler could not produce a confirmed schedule placement for '{goalPaceStage.ProgressionStageKey}' (structural scheduling failure or no matching week).");
        }

        var (effectiveStageKey, usedFallback) = exposure.Value;

        if (expectedEligible)
        {
            if (usedFallback || effectiveStageKey != goalPaceStage.ProgressionStageKey)
            {
                return new(value, GoalPaceOutcomeStatus.StructurallyUnreachable,
                    $"GOAL_FEASIBILITY_IN={value}: value is within AllowedValues but the real scheduler routed to '{effectiveStageKey}' (usedFallback={usedFallback}) instead of scheduling '{goalPaceStage.ProgressionStageKey}' directly -- inconsistent with the stage definition.");
            }

            return new(value, GoalPaceOutcomeStatus.Eligible,
                $"GOAL_FEASIBILITY_IN={value}: within '{goalPaceStage.ProgressionStageKey}''s own AllowedValues; confirmed scheduled directly (no fallback) by the real ProgressionStageAllocator.");
        }

        if (!usedFallback || effectiveStageKey != goalPaceStage.FallbackStageKey)
        {
            return new(value, GoalPaceOutcomeStatus.StructurallyUnreachable,
                $"GOAL_FEASIBILITY_IN={value}: value is outside AllowedValues but the real scheduler did not confirm a fallback to '{goalPaceStage.FallbackStageKey}' (effective='{effectiveStageKey}', usedFallback={usedFallback}).");
        }

        return new(value, GoalPaceOutcomeStatus.FallbackConfirmed,
            $"GOAL_FEASIBILITY_IN={value}: outside '{goalPaceStage.ProgressionStageKey}''s AllowedValues; confirmed reachable via FallbackStageKey='{goalPaceStage.FallbackStageKey}' by the real ProgressionStageAllocator.");
    }

    private static GoalPaceOutcomeCheck CheckNotEvaluated(
        PhaseAllocationResult allocation,
        CatalogWorkoutProgressionDefinition progression,
        IReadOnlyList<string> weeklySlotRoles,
        CatalogWorkoutProgressionStage goalPaceStage,
        IReadOnlyList<RuntimeConditionResolutionResult> conditions)
    {
        var exposure = ScheduleGoalPaceExposure(allocation, progression, weeklySlotRoles, goalPaceStage, conditions);

        if (exposure is null)
        {
            return new("NOT_EVALUATED", GoalPaceOutcomeStatus.StructurallyUnreachable,
                "GOAL_FEASIBILITY_IN resolver status=NotEvaluated: the real scheduler could not produce a confirmed schedule placement for GOAL_PACE_REHEARSAL (structural scheduling failure or no matching week).");
        }

        var (effectiveStageKey, usedFallback) = exposure.Value;
        if (!usedFallback || effectiveStageKey != goalPaceStage.FallbackStageKey)
        {
            return new("NOT_EVALUATED", GoalPaceOutcomeStatus.StructurallyUnreachable,
                "GOAL_FEASIBILITY_IN resolver status=NotEvaluated: expected silent fallback routing (per ProgressionStageAllocator.IsStageEligible) did not occur as confirmed by the real scheduler -- investigate before trusting TD-NOTEVALUATED-FALLBACK-001's documented mechanism.");
        }

        return new("NOT_EVALUATED", GoalPaceOutcomeStatus.UncertainNotEvaluated,
            $"GOAL_FEASIBILITY_IN resolver status=NotEvaluated: ProgressionStageAllocator.IsStageEligible treats NotEvaluated identically to an unmet condition and silently routes '{goalPaceStage.ProgressionStageKey}' to fallback '{goalPaceStage.FallbackStageKey}' -- see TD-NOTEVALUATED-FALLBACK-001 (activation-readiness-risks.json), not yet product-approved.");
    }

    /// <summary>
    /// Calls the real ProgressionStageAllocator.Allocate directly (see class
    /// doc comment for why this does not go through
    /// StageReachabilityVerifier.Verify) and returns the effective stage key
    /// and fallback flag for the week whose requested stage matches
    /// <paramref name="goalPaceStage"/>. Returns null for any structural
    /// scheduling failure -- never throws.
    /// </summary>
    private static (string EffectiveStageKey, bool UsedFallback)? ScheduleGoalPaceExposure(
        PhaseAllocationResult allocation,
        CatalogWorkoutProgressionDefinition progression,
        IReadOnlyList<string> weeklySlotRoles,
        CatalogWorkoutProgressionStage goalPaceStage,
        IReadOnlyList<RuntimeConditionResolutionResult> conditions)
    {
        try
        {
            var context = new CatalogStageToWeekMaterializationContext
            {
                StartDate = new DateOnly(2000, 1, 3),
                AsOfDate = new DateOnly(2000, 1, 3),
                PlannedWeekCount = allocation.TargetWeeks,
                DaysPerWeek = weeklySlotRoles.Count,
                CanonicalDistanceFamily = progression.DistanceFamily,
                CandidateKey = "DARK_GOAL_PACE_REACHABILITY_VERIFICATION",
                CandidateVersion = progression.Version,
                DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
                SelectedStageSequence = allocation.Phases.Select(p => p.PhaseKey).ToList(),
                StageWeekAllocations = allocation.Phases.Select(p => new CatalogStageWeekAllocation(p.PhaseKey, p.AllocatedWeeks)).ToList(),
                RunLayout = new PlanCatalogReference("DARK_VERIFICATION_LAYOUT", 1),
                RunLayoutSlotRoles = weeklySlotRoles,
            };
            var skeleton = new CatalogStageToWeekMaterializer().Materialize(context).Skeleton;
            var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
            {
                CandidateKey = "DARK_GOAL_PACE_REACHABILITY_VERIFICATION",
                CandidateVersion = progression.Version,
                Progression = progression,
                Skeleton = skeleton,
                ConditionResults = conditions,
            });

            var week = schedule.Weeks.FirstOrDefault(w => w.PhaseKey == RaceSpecificPhaseKey
                && (w.RequestedProgressionStageKey ?? w.ProgressionStageKey) == goalPaceStage.ProgressionStageKey);

            return week is null ? null : (week.ProgressionStageKey, week.FallbackOrigin is not null);
        }
        catch (ProgressionStageSchedulingException)
        {
            return null;
        }
    }
}
