namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.3 — one phase's resolved week-count
/// allocation, derived from catalog data (<see cref="PlanCatalogPhaseAllocation"/>).
/// Uses precise phase terminology throughout (<see cref="PhaseKey"/>,
/// <see cref="PhaseWeekCount"/>) per this phase's own terminology decision:
/// "phaseKey" is the week-allocation granularity, distinct from the
/// catalog's finer, nested "stageKey" workout-selection granularity, which
/// this resolver never reads.
/// </summary>
internal sealed record CatalogPhaseAllocationEntry(string PhaseKey, int PhaseWeekCount);

/// <summary>The complete, ordered, internally-validated phase allocation for one candidate.</summary>
internal sealed class CatalogPhaseAllocation
{
    public required IReadOnlyList<CatalogPhaseAllocationEntry> Entries { get; init; }

    /// <summary>Sum of every entry's <see cref="CatalogPhaseAllocationEntry.PhaseWeekCount"/>.</summary>
    public required int TotalWeeks { get; init; }
}

/// <summary>
/// Backend Integration Phase 4G.3B.2 — classifies a mechanical
/// <see cref="ICatalogPhaseAllocationResolver.Resolve(PlanCatalogCandidateSummary, int)"/>
/// result relative to the candidate's preferred (default) allocation.
/// </summary>
public enum AllocationMode
{
    /// <summary>Target is below the sum of preferred weeks — phases were reduced by <see cref="PlanCatalogPhaseAllocation.CompressionPriority"/> order, never below their own <see cref="PlanCatalogPhaseAllocation.MinimumWeeks"/>.</summary>
    Compression,

    /// <summary>Target equals the sum of preferred weeks exactly — no adjustment from the catalog's declared preferred allocation.</summary>
    Preferred,

    /// <summary>Target is above the sum of preferred weeks — phases were extended by <see cref="PlanCatalogPhaseAllocation.ExtensionPriority"/> order, never above their own <see cref="PlanCatalogPhaseAllocation.MaximumWeeks"/>.</summary>
    Extension,
}

/// <summary>One phase's mechanically-computed week allocation for a specific target week count, alongside its own catalog-declared bounds for context.</summary>
public sealed record AllocatedPhase(string PhaseKey, int AllocatedWeeks, int MinimumWeeks, int MaximumWeeks);

/// <summary>
/// Backend Integration Phase 4G.3B.2 — the mechanical result of computing a
/// phase allocation for an arbitrary target week count. Deliberately makes
/// no safety, readiness, or eligibility judgment: <see cref="IsMathematicallyFeasible"/>
/// answers only "does an allocation exist that respects every phase's own
/// catalog-declared [MinimumWeeks, MaximumWeeks] bound and sums exactly to
/// <see cref="TargetWeeks"/>?" — nothing about whether that allocation is
/// safe to generate, stage-reachable, or publicly supported. See
/// PHASE4G_3B_2_GENERIC_PHASE_ALLOCATOR.md: every feasible-but-non-12-week
/// result is MATHEMATICALLY_FEASIBLE_ONLY — NOT VERIFIED SAFE, NOT PUBLICLY
/// SUPPORTED.
/// </summary>
public sealed record PhaseAllocationResult(
    int TargetWeeks,
    int PreferredWeeks,
    int Delta,
    AllocationMode Mode,
    IReadOnlyList<AllocatedPhase> Phases,
    bool IsMathematicallyFeasible,
    string ReasonCode);

/// <summary>
/// Backend Integration Phase 4F.3 — derives a candidate's authoritative
/// phase (week-allocation) sequence from its already-loaded catalog data.
/// Never invents, hardcodes, rebalances, or fills a missing allocation —
/// every entry comes directly from <see cref="PlanCatalogCandidateSummary.PhaseAllocations"/>,
/// which is itself read verbatim from the master template's own
/// <c>phases[].preferredWeeks</c> declarations (see
/// <see cref="PlanCatalogBundleLoader"/>). Pure and dependency-free: no
/// database, clock, or additional catalog-file access — the candidate must
/// already be loaded.
///
/// Backend Integration Phase 4G.3B.2 adds
/// <see cref="Resolve(PlanCatalogCandidateSummary, int)"/> — a second, purely
/// mechanical overload that computes a phase allocation for an arbitrary
/// target week count using only catalog-authored constraint fields. It does
/// not replace, wrap, or change <see cref="Resolve(PlanCatalogCandidateSummary)"/>,
/// which remains exactly as it was (always the fixed preferred allocation).
/// Never called from any live request path — see
/// PHASE4G_3B_2_GENERIC_PHASE_ALLOCATOR.md section 11.
/// </summary>
internal interface ICatalogPhaseAllocationResolver
{
    CatalogPhaseAllocation Resolve(PlanCatalogCandidateSummary candidate);

    /// <summary>
    /// Mechanical-only: computes a feasible phase allocation for
    /// <paramref name="targetWeekCount"/> using only catalog-authored
    /// <c>MinimumWeeks</c>/<c>PreferredWeeks</c>/<c>MaximumWeeks</c>/
    /// <c>CompressionPriority</c>/<c>ExtensionPriority</c> fields. Pure
    /// function of its two parameters — no I/O, no readiness, no request
    /// context. Never throws for an out-of-range <paramref name="targetWeekCount"/>
    /// (returns <see cref="PhaseAllocationResult.IsMathematicallyFeasible"/> = false
    /// instead); still throws the existing structural exceptions
    /// (<see cref="CatalogPhaseAllocationSourceMissingException"/> etc.) for
    /// malformed candidate data, matching <see cref="Resolve(PlanCatalogCandidateSummary)"/>'s
    /// own convention.
    /// </summary>
    PhaseAllocationResult Resolve(PlanCatalogCandidateSummary candidate, int targetWeekCount);
}

/// <inheritdoc cref="ICatalogPhaseAllocationResolver"/>
internal sealed class CatalogPhaseAllocationResolver : ICatalogPhaseAllocationResolver
{
    public CatalogPhaseAllocation Resolve(PlanCatalogCandidateSummary candidate)
    {
        var source = candidate.PhaseAllocations;

        if (source.Count == 0)
        {
            throw new CatalogPhaseAllocationSourceMissingException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}'s loaded master template " +
                "declares no phases. PhaseAllocations must not be empty.");
        }

        var phaseKeys = source.Select(p => p.PhaseKey).ToList();

        if (phaseKeys.Any(string.IsNullOrWhiteSpace))
        {
            throw new CatalogPhaseAllocationInvalidException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' has a blank/malformed phase key. " +
                "This resolver enforces only non-blank keys -- it does not enforce a closed enum of accepted phase " +
                "keys, keeping it generic across distance families per the established Decision 12 pattern.");
        }

        if (phaseKeys.Distinct().Count() != phaseKeys.Count)
        {
            throw new CatalogPhaseAllocationInvalidException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' declares a duplicate phase key: " +
                string.Join(", ", phaseKeys) + ".");
        }

        var nonPositive = source.Where(p => p.PreferredWeeks <= 0).ToList();
        if (nonPositive.Count > 0)
        {
            throw new CatalogPhaseAllocationInvalidException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' has a non-positive " +
                $"preferredWeeks for: {string.Join(", ", nonPositive.Select(p => $"{p.PhaseKey}={p.PreferredWeeks}"))}. " +
                "Zero-length and negative phase allocations are rejected -- never silently redistributed.");
        }

        var entries = source.Select(p => new CatalogPhaseAllocationEntry(p.PhaseKey, p.PreferredWeeks)).ToList();

        return new CatalogPhaseAllocation
        {
            Entries = entries,
            TotalWeeks = entries.Sum(e => e.PhaseWeekCount),
        };
    }

    public PhaseAllocationResult Resolve(PlanCatalogCandidateSummary candidate, int targetWeekCount)
    {
        var source = candidate.PhaseAllocations;
        ValidateStructure(candidate, source);

        var sumMinimum = source.Sum(p => p.MinimumWeeks);
        var sumPreferred = source.Sum(p => p.PreferredWeeks);
        var sumMaximum = source.Sum(p => p.MaximumWeeks);
        var delta = targetWeekCount - sumPreferred;
        var mode = delta < 0 ? AllocationMode.Compression : delta == 0 ? AllocationMode.Preferred : AllocationMode.Extension;

        if (targetWeekCount < sumMinimum || targetWeekCount > sumMaximum)
        {
            var reasonCode = targetWeekCount < sumMinimum
                ? $"TARGET_BELOW_SUM_OF_MINIMUMS: targetWeekCount={targetWeekCount}, sumOfMinimums={sumMinimum}"
                : $"TARGET_ABOVE_SUM_OF_MAXIMUMS: targetWeekCount={targetWeekCount}, sumOfMaximums={sumMaximum}";
            return new PhaseAllocationResult(targetWeekCount, sumPreferred, delta, mode, Array.Empty<AllocatedPhase>(), false, reasonCode);
        }

        // Anchored at PreferredWeeks (never at MinimumWeeks) -- see
        // PHASE4G_3B_2_GENERIC_PHASE_ALLOCATOR.md section 3 for why this is
        // the algorithm actually implemented (it is the only interpretation
        // consistent with Delta/Mode being defined relative to
        // PreferredWeeks), and section 6 for the proof that it still
        // reproduces the Phase 4G.3A F2/B3/RS2/T1 result exactly at the
        // targetWeekCount=8 boundary.
        var allocated = source.ToDictionary(p => p.PhaseKey, p => p.PreferredWeeks);

        if (delta < 0)
        {
            DistributeByPriority(
                source.OrderBy(p => p.CompressionPriority).ToList(),
                allocated,
                remaining: -delta,
                canAdjust: p => allocated[p.PhaseKey] > p.MinimumWeeks,
                adjust: p => allocated[p.PhaseKey]--);
        }
        else if (delta > 0)
        {
            DistributeByPriority(
                source.OrderBy(p => p.ExtensionPriority).ToList(),
                allocated,
                remaining: delta,
                canAdjust: p => allocated[p.PhaseKey] < p.MaximumWeeks,
                adjust: p => allocated[p.PhaseKey]++);
        }

        var phases = source
            .Select(p => new AllocatedPhase(p.PhaseKey, allocated[p.PhaseKey], p.MinimumWeeks, p.MaximumWeeks))
            .ToList();

        return new PhaseAllocationResult(targetWeekCount, sumPreferred, delta, mode, phases, true, "MATHEMATICALLY_FEASIBLE");
    }

    /// <summary>
    /// Round-robin, one week at a time, in the given priority order — lower
    /// priority number moves first (confirmed against the real catalog's own
    /// data, see PHASE4G_3B_2_GENERIC_PHASE_ALLOCATOR.md section 4). Never
    /// moves a phase past the bound <paramref name="canAdjust"/> checks. The
    /// feasibility check already run in the caller guarantees this always
    /// fully consumes <paramref name="remaining"/>; the no-progress break is
    /// a defensive-only safety net, never expected to trigger for real
    /// catalog data.
    /// </summary>
    private static void DistributeByPriority(
        IReadOnlyList<PlanCatalogPhaseAllocation> orderedByPriority,
        Dictionary<string, int> allocated,
        int remaining,
        Func<PlanCatalogPhaseAllocation, bool> canAdjust,
        Action<PlanCatalogPhaseAllocation> adjust)
    {
        while (remaining > 0)
        {
            var progressedThisPass = false;
            foreach (var phase in orderedByPriority)
            {
                if (remaining == 0)
                {
                    break;
                }

                if (canAdjust(phase))
                {
                    adjust(phase);
                    remaining--;
                    progressedThisPass = true;
                }
            }

            if (!progressedThisPass)
            {
                break;
            }
        }
    }

    private static void ValidateStructure(PlanCatalogCandidateSummary candidate, IReadOnlyList<PlanCatalogPhaseAllocation> source)
    {
        if (source.Count == 0)
        {
            throw new CatalogPhaseAllocationSourceMissingException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}'s loaded master template " +
                "declares no phases. PhaseAllocations must not be empty.");
        }

        var phaseKeys = source.Select(p => p.PhaseKey).ToList();

        if (phaseKeys.Any(string.IsNullOrWhiteSpace))
        {
            throw new CatalogPhaseAllocationInvalidException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' has a blank/malformed phase key.");
        }

        if (phaseKeys.Distinct().Count() != phaseKeys.Count)
        {
            throw new CatalogPhaseAllocationInvalidException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' declares a duplicate phase key: " +
                string.Join(", ", phaseKeys) + ".");
        }

        var invalidBounds = source.Where(p => p.MinimumWeeks <= 0 || p.PreferredWeeks <= 0 || p.MaximumWeeks <= 0
            || p.MinimumWeeks > p.PreferredWeeks || p.PreferredWeeks > p.MaximumWeeks).ToList();
        if (invalidBounds.Count > 0)
        {
            throw new CatalogPhaseAllocationInvalidException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' has an invalid week-constraint " +
                $"ordering for: {string.Join(", ", invalidBounds.Select(p => $"{p.PhaseKey} [{p.MinimumWeeks},{p.PreferredWeeks},{p.MaximumWeeks}]"))}. " +
                "Every phase must satisfy 0 < MinimumWeeks <= PreferredWeeks <= MaximumWeeks.");
        }

        var invalidPriorities = source.Where(p => p.CompressionPriority <= 0 || p.ExtensionPriority <= 0).ToList();
        if (invalidPriorities.Count > 0)
        {
            throw new CatalogPhaseAllocationInvalidException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' has a non-positive " +
                $"compression/extension priority for: {string.Join(", ", invalidPriorities.Select(p => p.PhaseKey))}.");
        }
    }
}
