namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;

/// <summary>
/// Backend Integration Phase 4G.6A.3B — production-owned, internal, generic
/// deterministic constrained-proportional allocation engine. Extracted from
/// Phase 4G.6A.3's test-project-only reference implementation, with its
/// implicit "effective minimum = max(declared minimum, 1)" heuristic
/// removed: this engine allocates exactly each eligible block's own
/// declared <see cref="PreparationRunwayBlockAllocationPolicy{TKey}.MinWeeks"/>,
/// nothing more, nothing less. Mandatory participation is expressed only by
/// a policy's own declared MinWeeks -- never inferred from PreferredWeight,
/// IsExpandable, profile membership, block name, or available capacity.
///
/// This class is dark: it is not registered in DI, not invoked by
/// <c>CatalogPreviewGenerator</c> or any other orchestrator, and produces no
/// workout, dated week, or persisted record. It knows nothing about TEN_K,
/// calendar dates, workout IDs, Race Core phases, or persistence -- see
/// <c>TenKPreparationRunwayAllocationPolicyFactory</c> (same folder) for the
/// one place that binds it to the real TEN_K__4D__INTERMEDIATE v10 policy.
/// </summary>
internal static class PreparationRunwayBlockAllocationEngine
{
    /// <summary>
    /// Runs the full allocation algorithm (validate policy, filter eligible
    /// blocks, allocate declared minima, validate maximum capacity,
    /// calculate remaining weeks, identify expandable unsaturated blocks,
    /// normalize weights, calculate ideal shares and floors, apply
    /// deterministic largest remainder, redistribute after caps, validate
    /// final invariants). Deterministic: repeated calls with the same input
    /// produce identical output, and reordering the input list never
    /// changes the result.
    /// </summary>
    public static PreparationRunwayAllocationEngineResult<TKey> Allocate<TKey>(
        int runwayWeeks,
        IReadOnlyList<PreparationRunwayBlockAllocationPolicy<TKey>> policies)
        where TKey : notnull
    {
        var trace = new List<string> { $"RunwayWeeks={runwayWeeks}, PolicyCount={policies.Count}" };

        // ── Step 1: validate inputs ──────────────────────────────────────
        if (runwayWeeks < 0)
            return Failure<TKey>(PreparationRunwayAllocationFailureCode.InvalidPolicy, "RunwayWeeks cannot be negative.", trace);

        var keys = policies.Select(p => p.BlockKey).ToArray();
        if (keys.Distinct().Count() != keys.Length)
            return Failure<TKey>(PreparationRunwayAllocationFailureCode.DuplicateBlockKey, "Block keys must be unique.", trace);

        foreach (var p in policies)
        {
            if (p.MinWeeks < 0 || p.MaxWeeks < p.MinWeeks || p.PreferredWeight < 0)
                return Failure<TKey>(PreparationRunwayAllocationFailureCode.InvalidPolicy,
                    $"Block '{p.BlockKey}' has invalid MinWeeks/MaxWeeks/PreferredWeight metadata.", trace);
            if (!p.IsExpandable && p.MaxWeeks != p.MinWeeks)
                return Failure<TKey>(PreparationRunwayAllocationFailureCode.InvalidPolicy,
                    $"Block '{p.BlockKey}' is non-expandable but declares MinWeeks != MaxWeeks; a non-expandable block cannot receive more than its fixed capacity.", trace);
        }

        // ── Step 2: filter eligible blocks ───────────────────────────────
        var eligible = policies.Where(p => p.IsEligible).ToArray();

        // ── Step 3: allocate declared minima (exactly -- no heuristic bump) ──
        var allocated = policies.ToDictionary(p => p.BlockKey, p => p.IsEligible ? p.MinWeeks : 0);
        trace.Add($"Eligible blocks: {string.Join(",", eligible.Select(p => p.BlockKey))}");

        var minSum = eligible.Sum(p => p.MinWeeks);
        if (minSum > runwayWeeks)
            return Failure<TKey>(PreparationRunwayAllocationFailureCode.MinimumCapacityExceedsRunway,
                $"Declared minima ({minSum}) exceed RunwayWeeks ({runwayWeeks}).", trace);

        // ── Step 4: validate total capacity ──────────────────────────────
        var maxSum = eligible.Sum(p => p.MaxWeeks);
        if (maxSum < runwayWeeks)
            return Failure<TKey>(PreparationRunwayAllocationFailureCode.MaximumCapacityBelowRunway,
                $"Combined maxima ({maxSum}) are below RunwayWeeks ({runwayWeeks}).", trace);

        // ── Step 5: remaining weeks ───────────────────────────────────────
        var remaining = runwayWeeks - minSum;
        trace.Add($"Declared minima allocated: {minSum}; RemainingWeeks={remaining}");

        // ── Steps 6-10: iterative normalize/floor/largest-remainder/redistribute ──
        var guard = policies.Count + 2; // bounded: each round either finishes or saturates >=1 block
        while (remaining > 0)
        {
            if (guard-- <= 0)
                return Failure<TKey>(PreparationRunwayAllocationFailureCode.AllocationInvariantViolation,
                    "Allocation loop exceeded its deterministic bound.", trace);

            // Step 6: identify expandable, unsaturated, positive-weight blocks.
            var open = eligible.Where(p => p.IsExpandable && p.PreferredWeight > 0 && allocated[p.BlockKey] < p.MaxWeeks).ToArray();
            if (open.Length == 0)
                return Failure<TKey>(PreparationRunwayAllocationFailureCode.NoExpandableCapacity,
                    $"{remaining} week(s) remain unallocated with no expandable capacity left.", trace);

            // Step 7: normalize weights over currently-open blocks.
            var totalWeight = open.Sum(p => p.PreferredWeight);
            var ideal = open.ToDictionary(p => p.BlockKey, p => remaining * (p.PreferredWeight / totalWeight));

            // Step 8: floor shares.
            var floorShare = open.ToDictionary(p => p.BlockKey, p => (int)Math.Floor(ideal[p.BlockKey]));
            var flooredTotal = floorShare.Values.Sum();
            var leftover = remaining - flooredTotal;

            // Step 9: largest-remainder distribution, tie-break: remainder desc, AllocationPriority desc, CanonicalOrder asc, key ordinal asc.
            var ordered = open
                .OrderByDescending(p => ideal[p.BlockKey] - floorShare[p.BlockKey])
                .ThenByDescending(p => p.AllocationPriority)
                .ThenBy(p => p.CanonicalOrder)
                .ThenBy(p => p.BlockKey.ToString(), StringComparer.Ordinal)
                .ToList();

            foreach (var p in ordered.Take(leftover))
                floorShare[p.BlockKey] += 1;

            // Step 10: apply, capping at MaxWeeks; overflow returns to the pool via recomputed `remaining`.
            foreach (var p in open)
            {
                var proposed = allocated[p.BlockKey] + floorShare[p.BlockKey];
                allocated[p.BlockKey] = Math.Min(proposed, p.MaxWeeks);
            }

            var newRemaining = runwayWeeks - allocated.Values.Sum();
            if (newRemaining == remaining)
                return Failure<TKey>(PreparationRunwayAllocationFailureCode.NoExpandableCapacity,
                    $"No progress could be made toward allocating the remaining {remaining} week(s).", trace);
            remaining = newRemaining;
            trace.Add($"Round complete; RemainingWeeks={remaining}");
        }

        // ── Step 11: final invariant validation ──────────────────────────
        var total = allocated.Values.Sum();
        if (total != runwayWeeks)
            return Failure<TKey>(PreparationRunwayAllocationFailureCode.AllocationInvariantViolation,
                $"Allocated total ({total}) does not equal RunwayWeeks ({runwayWeeks}).", trace);

        foreach (var p in policies)
        {
            var week = allocated[p.BlockKey];
            if (week < 0)
                return Failure<TKey>(PreparationRunwayAllocationFailureCode.AllocationInvariantViolation, $"Block '{p.BlockKey}' has a negative allocation.", trace);
            if (!p.IsEligible && week != 0)
                return Failure<TKey>(PreparationRunwayAllocationFailureCode.AllocationInvariantViolation, $"Ineligible block '{p.BlockKey}' received {week} week(s).", trace);
            if (p.IsEligible && (week < p.MinWeeks || week > p.MaxWeeks))
                return Failure<TKey>(PreparationRunwayAllocationFailureCode.AllocationInvariantViolation, $"Block '{p.BlockKey}' allocation {week} violates [{p.MinWeeks},{p.MaxWeeks}].", trace);
            if (p.IsEligible && !p.IsExpandable && week != p.MinWeeks)
                return Failure<TKey>(PreparationRunwayAllocationFailureCode.AllocationInvariantViolation, $"Fixed block '{p.BlockKey}' allocation {week} does not equal its fixed capacity {p.MinWeeks}.", trace);
        }

        var outcomes = policies
            .Select(p => new PreparationRunwayBlockAllocationOutcome<TKey>(p.BlockKey, allocated[p.BlockKey], p.CanonicalOrder))
            .OrderBy(o => o.CanonicalOrder)
            .ToArray();

        trace.Add($"Final allocation: {string.Join(",", outcomes.Select(o => $"{o.BlockKey}={o.AllocatedWeeks}"))}");
        return PreparationRunwayAllocationEngineResult<TKey>.Success(runwayWeeks, outcomes, trace);
    }

    private static PreparationRunwayAllocationEngineResult<TKey> Failure<TKey>(
        PreparationRunwayAllocationFailureCode code, string reason, List<string> trace) where TKey : notnull
    {
        trace.Add($"FAILED: {code} -- {reason}");
        return PreparationRunwayAllocationEngineResult<TKey>.Failure(code, reason, trace);
    }
}

/// <summary>
/// Typed policy input for one block. Generic over an arbitrary, stable
/// block-key type. Mandatory participation is expressed only by
/// <see cref="MinWeeks"/> -- the engine never infers a mandatory presence
/// from <see cref="PreferredWeight"/>, <see cref="IsExpandable"/>, or any
/// other field.
/// </summary>
internal sealed record PreparationRunwayBlockAllocationPolicy<TKey>(
    TKey BlockKey,
    bool IsEligible,
    int MinWeeks,
    int MaxWeeks,
    double PreferredWeight,
    int AllocationPriority,
    int CanonicalOrder,
    bool IsExpandable) where TKey : notnull;

/// <summary>One block's resolved allocation, retaining its CanonicalOrder for chronological-sequence consumers.</summary>
internal sealed record PreparationRunwayBlockAllocationOutcome<TKey>(
    TKey BlockKey,
    int AllocatedWeeks,
    int CanonicalOrder) where TKey : notnull;

internal enum PreparationRunwayAllocationFailureCode
{
    InvalidPolicy,
    DuplicateBlockKey,
    MinimumCapacityExceedsRunway,
    MaximumCapacityBelowRunway,
    NoExpandableCapacity,
    AllocationInvariantViolation,
}

/// <summary>Success/failure result. Failure never carries a partial allocation.</summary>
internal sealed record PreparationRunwayAllocationEngineResult<TKey>(
    bool IsSuccess,
    int RunwayWeeks,
    IReadOnlyList<PreparationRunwayBlockAllocationOutcome<TKey>>? Allocations,
    int? TotalAllocatedWeeks,
    PreparationRunwayAllocationFailureCode? FailureCode,
    string? FailureReason,
    IReadOnlyList<string> Trace) where TKey : notnull
{
    public static PreparationRunwayAllocationEngineResult<TKey> Success(int runwayWeeks, IReadOnlyList<PreparationRunwayBlockAllocationOutcome<TKey>> allocations, IReadOnlyList<string> trace) =>
        new(true, runwayWeeks, allocations, allocations.Sum(a => a.AllocatedWeeks), null, null, trace);

    public static PreparationRunwayAllocationEngineResult<TKey> Failure(PreparationRunwayAllocationFailureCode code, string reason, IReadOnlyList<string> trace) =>
        new(false, default, null, null, code, reason, trace);
}
