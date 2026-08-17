namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.1 -- pure NextWindowLoadDecisionPolicy (Rev3.1 §7). The decision
/// matrix below is an explicit PRODUCT DEFAULT calibrated for the current
/// 4-session pilot (Mon/Wed/Fri/Sun = Easy/Key/Easy/Long), not a general
/// formula and not a claimed scientific threshold -- see Rev3.1 §7/§12.
///
/// LoadDecision and SafetyReviewRequired are independent dimensions
/// (Rev3.1 §7's own corrected model -- Rev2 mistakenly folded
/// SafetyReviewRequired into the load-decision enum). SafetyReviewRequired
/// never overrides or participates in the LoadDecision computation below;
/// it is derived solely from <see cref="WindowExecutionSummary.HasSafetyFlag"/>.
/// </summary>
internal static class NextWindowLoadDecisionPolicy
{
    public static NextWindowAdaptationResult Evaluate(WindowExecutionSummary summary)
    {
        var loadDecision = DetermineLoadDecision(summary);
        return new NextWindowAdaptationResult(loadDecision, SafetyReviewRequired: summary.HasSafetyFlag);
    }

    /// <summary>Severity-first (by EffectiveCompletedCount), then role
    /// importance -- the Rev3 fix for the Rev2 bug where role-first
    /// ordering let a worse-adherence window (0/4) outrank a
    /// better-adherence window (2/4). SupersededByAdaptationCount is
    /// deliberately not read here: it is informational only and must never
    /// influence this decision (Rev3.1 §6).
    ///
    /// Phase 10K-FREQ.4, deliberately NOT changed here (flagged, not
    /// silently resolved): these raw EffectiveCompletedCount thresholds are
    /// still hardcoded to a 4-total-session week (this class's own §7 doc
    /// comment above says so explicitly). For a hypothetical 5-session week
    /// (e.g. Intermediate 5D, 2 KEY + 2 EASY + 1 LONG), completing 4 of 5
    /// falls into the "&gt;= 4" branch and is misclassified identically to a
    /// fully-complete 4-session week. This is a GENUINELY NEW sub-case, not
    /// something Rev5's multi-week aggregation (WeeklyWindowPartitioner +
    /// WeeklyLoadDecisionAggregator, Phase 4M.5C) already solved --
    /// confirmed by direct read of WeeklyLoadDecisionAggregation.cs's own
    /// doc comment: "WindowExecutionSummaryBuilder and
    /// NextWindowLoadDecisionPolicy remain completely unchanged... they are
    /// simply invoked once per resulting bucket [week]." Rev5 operates
    /// strictly ABOVE this single-week function, across a variable number
    /// of WEEKS; it never addresses variability in session COUNT WITHIN one
    /// week. Making these thresholds ratio/role-aware for non-4-session
    /// weeks is a real product-decision question (what does "Reduce" mean
    /// at 5 sessions?), not a mechanism fix -- left for a future decision
    /// phase, per this phase's explicit instruction not to touch it.</summary>
    private static NextWindowLoadDecision DetermineLoadDecision(WindowExecutionSummary summary)
    {
        return summary.EffectiveCompletedCount switch
        {
            0 or 1 => NextWindowLoadDecision.Reduce,
            2 => NextWindowLoadDecision.Maintain,
            3 => OnlyEasyMissing(summary) ? NextWindowLoadDecision.ProgressAsPlanned : NextWindowLoadDecision.Maintain,
            >= 4 => NextWindowLoadDecision.ProgressAsPlanned,
            _ => NextWindowLoadDecision.Reduce,
        };
    }

    private static bool OnlyEasyMissing(WindowExecutionSummary summary)
    {
        // Phase 10K-FREQ.4: reads the count pair directly (rather than the
        // back-compat KeySessionExpected/KeySessionCompleted booleans) so a
        // 5-session week (e.g. a hypothetical Intermediate 5D layout)
        // missing exactly one of two KEY sessions is correctly NOT
        // classified as "only Easy missing" -- behaviorally identical to
        // the pre-FREQ.4 boolean check for KeySessionExpectedCount <= 1
        // (verified by regression), but now correct for N > 1.
        var keySatisfied = summary.KeySessionCompletedCount == summary.KeySessionExpectedCount;
        var longSatisfied = !summary.LongRunExpected || summary.LongRunCompleted;
        var easyMissing = summary.EasyCompletedCount < summary.EasyExpectedCount;
        return keySatisfied && longSatisfied && easyMissing;
    }
}
