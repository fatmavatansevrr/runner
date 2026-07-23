using System.Collections.Generic;
using System.Linq;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4G.3B.3 -- standalone, narrow slice only: formalizes
/// the phase-bound guarantee already proven for the generic allocator
/// (targetWeeks between 8 and 14) into a reusable, independent verifier
/// component. This does not implement the other eight safety verifiers named
/// in prior planning, nor any orchestrating pipeline that composes them --
/// each remains standalone until an explicit future phase wires them
/// together. Not called from any live request path.
/// </summary>
public enum PhaseConstraintVerificationOutcome
{
    Pass,
    Fail,
    NotApplicable,
}

/// <summary>One target week count's structural phase-bound verification outcome.</summary>
public sealed record PhaseConstraintVerificationResult(
    int TargetWeeks,
    PhaseConstraintVerificationOutcome Outcome,
    IReadOnlyList<string> Findings);

/// <summary>
/// Verifies only the structural bound invariants of a
/// <see cref="PhaseAllocationResult"/>: every phase's AllocatedWeeks stays
/// within its own MinimumWeeks/MaximumWeeks and above zero, and the sum of
/// every phase's AllocatedWeeks equals TargetWeeks. Pure function of its
/// input; no I/O, no candidate/catalog/readiness dependency.
/// </summary>
internal static class PhaseConstraintVerifier
{
    public static PhaseConstraintVerificationResult Verify(PhaseAllocationResult allocation)
    {
        if (!allocation.IsMathematicallyFeasible)
        {
            return new PhaseConstraintVerificationResult(
                allocation.TargetWeeks,
                PhaseConstraintVerificationOutcome.NotApplicable,
                new[] { "NOT_APPLICABLE_TARGET_MATHEMATICALLY_INFEASIBLE: no allocation exists for this target, so there are no phase bounds to check." });
        }

        var findings = new List<string>();

        foreach (var phase in allocation.Phases)
        {
            if (phase.AllocatedWeeks <= 0)
            {
                findings.Add($"PHASE_ALLOCATED_ZERO_OR_NEGATIVE: phase '{phase.PhaseKey}' has AllocatedWeeks={phase.AllocatedWeeks}, which must be > 0.");
            }

            if (phase.AllocatedWeeks < phase.MinimumWeeks)
            {
                findings.Add($"PHASE_BELOW_MINIMUM: phase '{phase.PhaseKey}' has AllocatedWeeks={phase.AllocatedWeeks}, below its own MinimumWeeks={phase.MinimumWeeks}.");
            }

            if (phase.AllocatedWeeks > phase.MaximumWeeks)
            {
                findings.Add($"PHASE_ABOVE_MAXIMUM: phase '{phase.PhaseKey}' has AllocatedWeeks={phase.AllocatedWeeks}, above its own MaximumWeeks={phase.MaximumWeeks}.");
            }
        }

        var sumAllocated = allocation.Phases.Sum(p => p.AllocatedWeeks);
        if (sumAllocated != allocation.TargetWeeks)
        {
            findings.Add($"AGGREGATE_SUM_MISMATCH: sum of every phase's AllocatedWeeks is {sumAllocated}, which does not equal TargetWeeks={allocation.TargetWeeks}.");
        }

        return new PhaseConstraintVerificationResult(
            allocation.TargetWeeks,
            findings.Count == 0 ? PhaseConstraintVerificationOutcome.Pass : PhaseConstraintVerificationOutcome.Fail,
            findings);
    }
}
