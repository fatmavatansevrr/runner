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
    /// influence this decision (Rev3.1 §6).</summary>
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
        var keySatisfied = !summary.KeySessionExpected || summary.KeySessionCompleted;
        var longSatisfied = !summary.LongRunExpected || summary.LongRunCompleted;
        var easyMissing = summary.EasyCompletedCount < summary.EasyExpectedCount;
        return keySatisfied && longSatisfied && easyMissing;
    }
}
