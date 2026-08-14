namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

public enum AllocationOrderVerificationOutcome
{
    Pass,
    DecisionRequired,
    Invalid,
}

public sealed record AllocationOrderVerificationResult(
    int TargetWeeks,
    AllocationOrderVerificationOutcome Outcome,
    string ReasonCode,
    bool IsOrderIndependent = false,
    bool UsesApprovedPriority = false,
    bool IsExecutable = false);

/// <summary>One phase's approved, catalog-scoped allocation rule.</summary>
internal sealed record ApprovedPhaseAllocationRule(
    string PhaseKey,
    int MinimumWeeks,
    int PreferredWeeks,
    int MaximumWeeks,
    int? CompressionPriority,
    int? ExtensionPriority);

/// <summary>
/// Explicit governance input. Catalog priority numbers alone do not prove
/// approval; callers must name the governance source that approved them.
/// </summary>
internal sealed record ApprovedAllocationPriorityPolicy(
    bool IsApproved,
    string GovernanceSource,
    IReadOnlyList<ApprovedPhaseAllocationRule> Phases)
{
    internal static ApprovedAllocationPriorityPolicy FromCandidate(
        PlanCatalogCandidateSummary candidate,
        string governanceSource) =>
        new(
            IsApproved: true,
            GovernanceSource: governanceSource,
            Phases: candidate.PhaseAllocations.Select(p => new ApprovedPhaseAllocationRule(
                p.PhaseKey,
                p.MinimumWeeks,
                p.PreferredWeeks,
                p.MaximumWeeks,
                p.CompressionPriority,
                p.ExtensionPriority)).ToArray());
}

/// <summary>
/// Dark verifier that separates theoretical order-independence from
/// deterministic execution under an explicitly approved priority. It neither
/// selects nor approves priorities and has no live runtime consumer.
/// </summary>
internal static class AllocationOrderCorrectnessVerifier
{
    public static AllocationOrderVerificationResult Verify(PhaseAllocationResult allocation) =>
        Verify(allocation, priorityPolicy: null);

    public static AllocationOrderVerificationResult Verify(
        PhaseAllocationResult allocation,
        ApprovedAllocationPriorityPolicy? priorityPolicy)
    {
        if (!allocation.IsMathematicallyFeasible)
        {
            return Result(allocation, AllocationOrderVerificationOutcome.Pass,
                "NOT_APPLICABLE_TARGET_MATHEMATICALLY_INFEASIBLE", true, false, false);
        }

        var structuralError = ValidateAllocation(allocation);
        if (structuralError is not null)
        {
            return Result(allocation, AllocationOrderVerificationOutcome.Invalid,
                structuralError, false, false, false);
        }

        if (allocation.Mode == AllocationMode.Preferred)
        {
            return Result(allocation, AllocationOrderVerificationOutcome.Pass,
                "ORDER_INDEPENDENT_PREFERRED_ALLOCATION", true, false, true);
        }

        var sumMinimum = allocation.Phases.Sum(p => p.MinimumWeeks);
        var sumMaximum = allocation.Phases.Sum(p => p.MaximumWeeks);
        if (allocation.TargetWeeks == sumMinimum)
        {
            return Result(allocation, AllocationOrderVerificationOutcome.Pass,
                "ORDER_INDEPENDENT_COMPRESSION_HEADROOM_FULLY_EXHAUSTED", true, false, true);
        }

        if (allocation.TargetWeeks == sumMaximum)
        {
            return Result(allocation, AllocationOrderVerificationOutcome.Pass,
                "ORDER_INDEPENDENT_EXTENSION_HEADROOM_FULLY_EXHAUSTED", true, false, true);
        }

        if (priorityPolicy is null || !priorityPolicy.IsApproved)
        {
            return Result(allocation, AllocationOrderVerificationOutcome.DecisionRequired,
                "PRIORITY_REQUIRED: order-dependent allocation has no explicitly approved priority policy.",
                false, false, false);
        }

        var policyError = ValidatePolicy(allocation, priorityPolicy);
        if (policyError is not null)
        {
            return Result(allocation, AllocationOrderVerificationOutcome.Invalid,
                policyError, false, false, false);
        }

        var expected = AllocateByApprovedPriority(allocation, priorityPolicy);
        if (expected is null || expected.Values.Sum() != allocation.TargetWeeks)
        {
            return Result(allocation, AllocationOrderVerificationOutcome.Invalid,
                "INVALID_PRIORITY_CANNOT_PRODUCE_TARGET_TOTAL", false, true, false);
        }

        if (allocation.Phases.Any(p => expected[p.PhaseKey] != p.AllocatedWeeks))
        {
            return Result(allocation, AllocationOrderVerificationOutcome.Invalid,
                "INVALID_PRIORITY_ALLOCATION_MISMATCH", false, true, false);
        }

        return Result(allocation, AllocationOrderVerificationOutcome.Pass,
            $"ORDER_DEPENDENT_BUT_APPROVED_PRIORITY: {priorityPolicy.GovernanceSource}",
            false, true, true);
    }

    private static string? ValidateAllocation(PhaseAllocationResult allocation)
    {
        if (allocation.Phases.Count == 0 ||
            allocation.Phases.Select(p => p.PhaseKey).Distinct(StringComparer.Ordinal).Count() != allocation.Phases.Count)
        {
            return "BOUNDS_VIOLATION_DUPLICATE_OR_MISSING_PHASE";
        }

        if (allocation.Phases.Any(p => p.AllocatedWeeks < p.MinimumWeeks || p.AllocatedWeeks > p.MaximumWeeks) ||
            allocation.Phases.Sum(p => p.AllocatedWeeks) != allocation.TargetWeeks)
        {
            return "BOUNDS_VIOLATION";
        }

        return null;
    }

    private static string? ValidatePolicy(
        PhaseAllocationResult allocation,
        ApprovedAllocationPriorityPolicy policy)
    {
        var allocationKeys = allocation.Phases.Select(p => p.PhaseKey).ToHashSet(StringComparer.Ordinal);
        var policyKeys = policy.Phases.Select(p => p.PhaseKey).ToArray();
        if (policyKeys.Length != allocationKeys.Count ||
            policyKeys.Distinct(StringComparer.Ordinal).Count() != policyKeys.Length ||
            policyKeys.Any(key => !allocationKeys.Contains(key)))
        {
            return "INVALID_PRIORITY_UNKNOWN_DUPLICATE_OR_MISSING_PHASE";
        }

        var priorities = policy.Phases.Select(p => allocation.Mode == AllocationMode.Compression
            ? p.CompressionPriority
            : p.ExtensionPriority).ToArray();
        if (priorities.Any(p => p is null or <= 0) || priorities.Distinct().Count() != priorities.Length)
        {
            return "INVALID_PRIORITY_MISSING_NON_POSITIVE_OR_DUPLICATE_VALUE";
        }

        if (policy.Phases.Any(p => p.MinimumWeeks <= 0 || p.MinimumWeeks > p.PreferredWeeks ||
            p.PreferredWeeks > p.MaximumWeeks))
        {
            return "BOUNDS_VIOLATION_INVALID_POLICY_BOUNDS";
        }

        return null;
    }

    private static Dictionary<string, int>? AllocateByApprovedPriority(
        PhaseAllocationResult allocation,
        ApprovedAllocationPriorityPolicy policy)
    {
        var result = policy.Phases.ToDictionary(p => p.PhaseKey, p => p.PreferredWeeks, StringComparer.Ordinal);
        var remaining = Math.Abs(allocation.TargetWeeks - policy.Phases.Sum(p => p.PreferredWeeks));
        var ordered = allocation.Mode == AllocationMode.Compression
            ? policy.Phases.OrderBy(p => p.CompressionPriority).ToArray()
            : policy.Phases.OrderBy(p => p.ExtensionPriority).ToArray();

        while (remaining > 0)
        {
            var progressed = false;
            foreach (var phase in ordered)
            {
                if (remaining == 0) break;
                var canMove = allocation.Mode == AllocationMode.Compression
                    ? result[phase.PhaseKey] > phase.MinimumWeeks
                    : result[phase.PhaseKey] < phase.MaximumWeeks;
                if (!canMove) continue;
                result[phase.PhaseKey] += allocation.Mode == AllocationMode.Compression ? -1 : 1;
                remaining--;
                progressed = true;
            }

            if (!progressed) return null;
        }

        return result;
    }

    private static AllocationOrderVerificationResult Result(
        PhaseAllocationResult allocation,
        AllocationOrderVerificationOutcome outcome,
        string reason,
        bool orderIndependent,
        bool usesApprovedPriority,
        bool executable) =>
        new(allocation.TargetWeeks, outcome, reason, orderIndependent, usesApprovedPriority, executable);
}
