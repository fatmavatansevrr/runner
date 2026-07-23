using System.Linq;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4G.3B.3 — deliberately narrow slice only: the
/// allocation-order-correctness question for a mechanical
/// <see cref="PhaseAllocationResult"/> (Phase 4G.3B.2). This is NOT the full
/// nine-verifier Safety Verification Pipeline referenced as deferred scope
/// in PHASE4G_3B_2_GENERIC_PHASE_ALLOCATOR.md -- <see cref="ICatalogPhaseAllocationResolver"/>-adjacent
/// concerns such as readiness eligibility (ReadinessEligibilityVerifier) and
/// the other eight verifiers remain unimplemented and out of scope for this
/// pass. Never called from any live request path -- same status as the
/// target-week-count overload on <see cref="ICatalogPhaseAllocationResolver"/>
/// itself.
/// </summary>
public enum AllocationOrderVerificationOutcome
{
    /// <summary>Priority order was never consulted, or the target's allocation is order-independent by construction -- no product/coaching decision is needed to trust this specific allocation's per-phase distribution.</summary>
    Pass,

    /// <summary>The target's specific per-phase distribution depends on <c>CompressionPriority</c>/<c>ExtensionPriority</c> values that are an unconfirmed catalog-authoring placeholder (AUD-008 / TD-ALLOCATION-PRIORITY-001) -- a product/coaching decision is required before this allocation can be trusted, not re-derived by this pipeline.</summary>
    DecisionRequired,
}

/// <summary>One target week count's allocation-order-correctness verification outcome.</summary>
public sealed record AllocationOrderVerificationResult(
    int TargetWeeks,
    AllocationOrderVerificationOutcome Outcome,
    string ReasonCode);

/// <summary>
/// Verifies only whether a <see cref="PhaseAllocationResult"/>'s specific
/// per-phase distribution can be trusted as correct, given that
/// <see cref="PlanCatalogPhaseAllocation.CompressionPriority"/>/<see cref="PlanCatalogPhaseAllocation.ExtensionPriority"/>
/// are an unconfirmed placeholder (see
/// plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.md AUD-008
/// and plan-catalog/artifacts/audits/activation-readiness-risks.json
/// TD-ALLOCATION-PRIORITY-001). Deliberately does not re-derive, correct, or
/// second-guess the priority values themselves -- per TD-ALLOCATION-PRIORITY-001's
/// own requiredResolution, that is a product/coaching decision outside this
/// pipeline's scope. Pure function of its input; no I/O.
/// </summary>
internal static class AllocationOrderCorrectnessVerifier
{
    public static AllocationOrderVerificationResult Verify(PhaseAllocationResult allocation)
    {
        if (!allocation.IsMathematicallyFeasible)
        {
            return new AllocationOrderVerificationResult(
                allocation.TargetWeeks,
                AllocationOrderVerificationOutcome.Pass,
                "NOT_APPLICABLE_TARGET_MATHEMATICALLY_INFEASIBLE: no allocation exists for this target, so there is no per-phase distribution to verify the order of.");
        }

        if (allocation.Mode == AllocationMode.Preferred)
        {
            return new AllocationOrderVerificationResult(
                allocation.TargetWeeks,
                AllocationOrderVerificationOutcome.Pass,
                "PASS_PREFERRED_ALLOCATION_UNAFFECTED_BY_PRIORITY_ORDER: targetWeeks equals the sum of every phase's PreferredWeeks, so no compression or extension adjustment occurs and CompressionPriority/ExtensionPriority are never consulted.");
        }

        var sumMinimum = allocation.Phases.Sum(p => p.MinimumWeeks);
        var sumMaximum = allocation.Phases.Sum(p => p.MaximumWeeks);

        if (allocation.TargetWeeks == sumMinimum)
        {
            return new AllocationOrderVerificationResult(
                allocation.TargetWeeks,
                AllocationOrderVerificationOutcome.Pass,
                $"PASS_COMPRESSION_HEADROOM_FULLY_EXHAUSTED: targetWeeks={allocation.TargetWeeks} equals the sum of every phase's MinimumWeeks, so the requested compression exactly consumes the candidate's total compressible headroom and every adjustable phase is forced to its own MinimumWeeks regardless of CompressionPriority order -- see PHASE4G_3B_2_GENERIC_PHASE_ALLOCATOR.md section 9.");
        }

        if (allocation.TargetWeeks == sumMaximum)
        {
            return new AllocationOrderVerificationResult(
                allocation.TargetWeeks,
                AllocationOrderVerificationOutcome.Pass,
                $"PASS_EXTENSION_HEADROOM_FULLY_EXHAUSTED: targetWeeks={allocation.TargetWeeks} equals the sum of every phase's MaximumWeeks, so the requested extension exactly consumes the candidate's total extendable headroom and every adjustable phase is forced to its own MaximumWeeks regardless of ExtensionPriority order -- mirrors the compression-headroom-exhausted case at the opposite boundary.");
        }

        return new AllocationOrderVerificationResult(
            allocation.TargetWeeks,
            AllocationOrderVerificationOutcome.DecisionRequired,
            $"DECISION_REQUIRED_UNCONFIRMED_ALLOCATION_ORDER (TD-ALLOCATION-PRIORITY-001): targetWeeks={allocation.TargetWeeks}'s specific per-phase distribution depends on CompressionPriority/ExtensionPriority values that AUD-008 classifies as an invented, unconfirmed placeholder with no canonical or coaching source. See plan-catalog/artifacts/audits/activation-readiness-risks.json entry TD-ALLOCATION-PRIORITY-001. Resolving the priority ordering is a product/coaching decision outside this pipeline's scope -- not something to re-derive here.");
    }
}
