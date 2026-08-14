namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.1 -- deterministic pure candidate selection (Rev3.1 §3
/// CandidateSelectionPolicy). Never queries a database or calendar and
/// never generates candidate dates; it only orders and picks from a list
/// the caller already resolved. No randomness, no scoring, no "best slot"
/// heuristic -- earliest date wins, and among candidates sharing a date,
/// the caller's own input order is the tie-breaker (list order is already
/// a stable, existing ordering the caller controls; no new domain
/// significance is invented for it).
/// </summary>
internal static class CandidateSelectionPolicy
{
    /// <summary>
    /// Returns the earliest candidate (by <see cref="ScheduleRepairCandidate.Date"/>)
    /// for which <see cref="ScheduleRepairCandidate.IsSafetyValid"/> is true,
    /// or null if none is valid. Candidates are evaluated in ascending date
    /// order; an earlier invalid candidate is skipped, never disqualifying
    /// the search.
    /// </summary>
    public static ScheduleRepairCandidate? SelectEarliestValid(IReadOnlyList<ScheduleRepairCandidate> candidates) =>
        // LINQ OrderBy is a stable sort, so candidates sharing the same date
        // keep their original relative input order -- that is the documented
        // tie-breaker, not an invented domain-significant ranking.
        candidates.OrderBy(c => c.Date).FirstOrDefault(c => c.IsSafetyValid);
}
