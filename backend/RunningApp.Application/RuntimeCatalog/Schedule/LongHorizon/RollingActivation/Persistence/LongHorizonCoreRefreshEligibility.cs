namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>Phase 4L.2E Part 2 -- typed eligibility outcomes for a future-only Core context refresh request.</summary>
internal enum LongHorizonCoreRefreshEligibilityOutcome
{
    Eligible,
    RejectedNotInCore,
    RejectedRunwayStillPending,
    RejectedNoFutureCoreWeeks,
    RejectedNoActiveCoreContext,
    RejectedSameOrEarlierAsOfDate,
    RejectedUnchangedEvidence,
    RejectedLifecycleBlocked,
    RejectedPlanTerminallyComplete,
}

/// <summary>Phase 4L.2E Part 2 -- result of evaluating refresh eligibility. Never selects or activates anything.</summary>
internal sealed record LongHorizonCoreRefreshEligibilityResult
{
    public required LongHorizonCoreRefreshEligibilityOutcome Outcome { get; init; }
    public int? FirstFuturePendingCoreGlobalWeek { get; init; }
    public string? RejectionDetail { get; init; }

    public bool IsEligible => Outcome == LongHorizonCoreRefreshEligibilityOutcome.Eligible;
}

/// <summary>
/// Phase 4L.2E Part 2 -- the explicit eligibility gate a future-only Core
/// context refresh must pass before real condition re-resolution or Core
/// regeneration may be invoked. Never selects, activates, or generates
/// anything itself -- a pure boolean/typed-rejection decision, reusing the
/// structural roadmap and per-week lifecycle as the sole authorities (never
/// a second boundary-derivation algorithm).
/// </summary>
internal static class LongHorizonCoreRefreshEligibility
{
    public static LongHorizonCoreRefreshEligibilityResult Evaluate(
        LongHorizonStructuralRoadmap roadmap,
        IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> lifecycleStates,
        LongHorizonActiveCoreContextSnapshot? activeCoreContext,
        DateOnly requestedAsOfDate,
        string requestedEvidenceFingerprint,
        bool lifecycleCurrentlyBlocked)
    {
        if (lifecycleCurrentlyBlocked)
        {
            return new LongHorizonCoreRefreshEligibilityResult
            {
                Outcome = LongHorizonCoreRefreshEligibilityOutcome.RejectedLifecycleBlocked,
                RejectionDetail = "The plan's current lifecycle status is Blocked; refresh is not evaluated until retry restores Pending.",
            };
        }

        var coreSegment = roadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.Core);
        var runwaySegment = roadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway);

        var anyRunwayPending = Enumerable.Range(runwaySegment.StartGlobalWeek, runwaySegment.EndGlobalWeek - runwaySegment.StartGlobalWeek + 1)
            .Any(week => lifecycleStates.TryGetValue(week, out var state) && state == LongHorizonNumericLifecycleState.NumericPending);
        if (anyRunwayPending)
        {
            return new LongHorizonCoreRefreshEligibilityResult
            {
                Outcome = LongHorizonCoreRefreshEligibilityOutcome.RejectedRunwayStillPending,
                RejectionDetail = "At least one Runway week remains NumericPending -- the lifecycle has not yet entered Core.",
            };
        }

        var anyCoreActivated = Enumerable.Range(coreSegment.StartGlobalWeek, coreSegment.EndGlobalWeek - coreSegment.StartGlobalWeek + 1)
            .Any(week => lifecycleStates.TryGetValue(week, out var state)
                && state is LongHorizonNumericLifecycleState.NumericActivated or LongHorizonNumericLifecycleState.Completed or LongHorizonNumericLifecycleState.Missed);
        if (!anyCoreActivated)
        {
            return new LongHorizonCoreRefreshEligibilityResult
            {
                Outcome = LongHorizonCoreRefreshEligibilityOutcome.RejectedNotInCore,
                RejectionDetail = "No Core week has been activated yet -- refresh requires at least one already-activated Core week under an existing context.",
            };
        }

        if (activeCoreContext is null)
        {
            return new LongHorizonCoreRefreshEligibilityResult
            {
                Outcome = LongHorizonCoreRefreshEligibilityOutcome.RejectedNoActiveCoreContext,
                RejectionDetail = "No Active LongHorizonCoreContextRecord exists for this plan.",
            };
        }

        var firstFuturePending = Enumerable.Range(coreSegment.StartGlobalWeek, coreSegment.EndGlobalWeek - coreSegment.StartGlobalWeek + 1)
            .Where(week => lifecycleStates.TryGetValue(week, out var state) && state == LongHorizonNumericLifecycleState.NumericPending)
            .Cast<int?>().FirstOrDefault();
        if (firstFuturePending is null)
        {
            return new LongHorizonCoreRefreshEligibilityResult
            {
                Outcome = LongHorizonCoreRefreshEligibilityOutcome.RejectedNoFutureCoreWeeks,
                RejectionDetail = "No NumericPending Core week remains -- the plan is at or past terminal Core completion.",
            };
        }

        if (requestedAsOfDate <= activeCoreContext.AsOfDate)
        {
            return new LongHorizonCoreRefreshEligibilityResult
            {
                Outcome = LongHorizonCoreRefreshEligibilityOutcome.RejectedSameOrEarlierAsOfDate,
                RejectionDetail = $"Requested AsOfDate {requestedAsOfDate} is not strictly later than the active context's AsOfDate {activeCoreContext.AsOfDate}.",
            };
        }

        if (activeCoreContext.EvidenceFingerprint is not null
            && string.Equals(requestedEvidenceFingerprint, activeCoreContext.EvidenceFingerprint, StringComparison.Ordinal))
        {
            return new LongHorizonCoreRefreshEligibilityResult
            {
                Outcome = LongHorizonCoreRefreshEligibilityOutcome.RejectedUnchangedEvidence,
                RejectionDetail = "The requested evidence fingerprint is identical to the active context's -- no material change to justify regeneration.",
            };
        }

        return new LongHorizonCoreRefreshEligibilityResult
        {
            Outcome = LongHorizonCoreRefreshEligibilityOutcome.Eligible,
            FirstFuturePendingCoreGlobalWeek = firstFuturePending,
        };
    }
}
