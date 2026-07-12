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
/// Backend Integration Phase 4F.3 — derives a candidate's authoritative
/// phase (week-allocation) sequence from its already-loaded catalog data.
/// Never invents, hardcodes, rebalances, or fills a missing allocation —
/// every entry comes directly from <see cref="PlanCatalogCandidateSummary.PhaseAllocations"/>,
/// which is itself read verbatim from the master template's own
/// <c>phases[].preferredWeeks</c> declarations (see
/// <see cref="PlanCatalogBundleLoader"/>). Pure and dependency-free: no
/// database, clock, or additional catalog-file access — the candidate must
/// already be loaded.
/// </summary>
internal interface ICatalogPhaseAllocationResolver
{
    CatalogPhaseAllocation Resolve(PlanCatalogCandidateSummary candidate);
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
}
