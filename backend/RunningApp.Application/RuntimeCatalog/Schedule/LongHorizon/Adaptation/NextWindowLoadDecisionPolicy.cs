namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.1 -- pure NextWindowLoadDecisionPolicy (Rev3.1 §7). The 4-session
/// decision matrix below is an explicit PRODUCT DEFAULT calibrated for the
/// current 4-session pilot (Mon/Wed/Fri/Sun = Easy/Key/Easy/Long), not a
/// general formula and not a claimed scientific threshold -- see Rev3.1 §7/§12.
///
/// LoadDecision and SafetyReviewRequired are independent dimensions
/// (Rev3.1 §7's own corrected model -- Rev2 mistakenly folded
/// SafetyReviewRequired into the load-decision enum). SafetyReviewRequired
/// never overrides or participates in the LoadDecision computation below;
/// it is derived solely from <see cref="WindowExecutionSummary.HasSafetyFlag"/>.
///
/// Phase 10K-FREQ.6D.4D Split D: implements the already-`FREQ.6`-approved,
/// already-fidelity-verified (`FREQ.6D.1B` Track A, 24/24 rows) 5-session
/// severity table for a genuine 5-session structural week (Intermediate 5D:
/// 2×KEY + 2×EASY + 1×LONG). This is policy IMPLEMENTATION, not a new policy
/// decision -- the table was frozen by `FREQ.6 §6` and its fidelity against
/// this exact dispatch shape was independently re-verified by `FREQ.6D.1B`
/// before this phase ever began. Dispatch is keyed on
/// <see cref="WindowExecutionSummary.ExpectedSessionCount"/> so a real
/// 4-session (or 3-session) week's existing, unrelated behavior is preserved
/// byte-for-byte -- this class never applies the 5-session table merely
/// because <c>EffectiveCompletedCount</c> happens to be 3 or less.
/// </summary>
internal static class NextWindowLoadDecisionPolicy
{
    private const int FiveSessionStructuralWeekSize = 5;

    public static NextWindowAdaptationResult Evaluate(WindowExecutionSummary summary)
    {
        var loadDecision = DetermineLoadDecision(summary);
        return new NextWindowAdaptationResult(loadDecision, SafetyReviewRequired: summary.HasSafetyFlag);
    }

    private static NextWindowLoadDecision DetermineLoadDecision(WindowExecutionSummary summary) =>
        summary.ExpectedSessionCount == FiveSessionStructuralWeekSize
            ? DetermineFiveSessionLoadDecision(summary)
            : DetermineLegacyLoadDecision(summary);

    /// <summary>Severity-first (by EffectiveCompletedCount), then role
    /// importance -- the Rev3 fix for the Rev2 bug where role-first
    /// ordering let a worse-adherence window (0/4) outrank a
    /// better-adherence window (2/4). SupersededByAdaptationCount is
    /// deliberately not read here: it is informational only and must never
    /// influence this decision (Rev3.1 §6). Unchanged from before Split D --
    /// governs every real (3D/4D) window that is not a genuine 5-session
    /// structural week.</summary>
    private static NextWindowLoadDecision DetermineLegacyLoadDecision(WindowExecutionSummary summary)
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

    /// <summary>
    /// The `FREQ.6 §6` 24-row table's own dispatch shape, reproduced exactly
    /// as `FREQ.6D.1B §2` verified it (outer count-floor switch; role
    /// information read only inside the count=4 branch -- counts 0/1/2/3/5
    /// are role-blind by construction, not merely by coincidence of today's
    /// table values). Reuses the real, already-generalized
    /// <see cref="WindowExecutionSummary"/> aggregate KEY count pair
    /// (`Phase 10K-FREQ.4`) rather than introducing separate Key1/Key2
    /// fields -- FREQ.6 itself states both KEY lanes are severity-equivalent
    /// (every row's outcome is identical whichever KEY lane was the sole
    /// miss), so the aggregate count is sufficient and keeps Adaptation
    /// lane-blind by construction (`FREQ.6D.4D` architecture §21).
    /// </summary>
    private static NextWindowLoadDecision DetermineFiveSessionLoadDecision(WindowExecutionSummary summary)
    {
        ValidateFiveSessionSummary(summary);

        return summary.EffectiveCompletedCount switch
        {
            0 or 1 => NextWindowLoadDecision.Reduce,
            2 or 3 => NextWindowLoadDecision.Maintain,
            4 => OnlyEasyMissing(summary) ? NextWindowLoadDecision.ProgressAsPlanned : NextWindowLoadDecision.Maintain,
            5 => NextWindowLoadDecision.ProgressAsPlanned,
            _ => throw new AdaptationLineageInvalidException(
                $"EffectiveCompletedCount {summary.EffectiveCompletedCount} outside the valid 0-5 range for a 5-session structural week."),
        };
    }

    /// <summary>
    /// Mirrors `FREQ.6D.1B §7`'s `Validate5DVector` fail-closed precondition
    /// checks, adapted to the real aggregate-count `WindowExecutionSummary`
    /// shape: never normalize a structurally invalid summary, always throw.
    /// </summary>
    private static void ValidateFiveSessionSummary(WindowExecutionSummary summary)
    {
        var roleSum = summary.KeySessionExpectedCount + (summary.LongRunExpected ? 1 : 0) + summary.EasyExpectedCount;
        if (roleSum != summary.ExpectedSessionCount)
        {
            throw new AdaptationLineageInvalidException(
                $"5-session window role-expectation sum (KEY {summary.KeySessionExpectedCount} + LONG {(summary.LongRunExpected ? 1 : 0)} + EASY {summary.EasyExpectedCount} = {roleSum}) " +
                $"does not equal ExpectedSessionCount {summary.ExpectedSessionCount}.");
        }

        var roleCompletedSum = summary.KeySessionCompletedCount + (summary.LongRunCompleted ? 1 : 0) + summary.EasyCompletedCount;
        if (roleCompletedSum != summary.EffectiveCompletedCount)
        {
            throw new AdaptationLineageInvalidException(
                $"5-session window role-completed sum (KEY {summary.KeySessionCompletedCount} + LONG {(summary.LongRunCompleted ? 1 : 0)} + EASY {summary.EasyCompletedCount} = {roleCompletedSum}) " +
                $"does not equal EffectiveCompletedCount {summary.EffectiveCompletedCount}.");
        }

        if (summary.KeySessionCompletedCount < 0 || summary.KeySessionCompletedCount > summary.KeySessionExpectedCount)
        {
            throw new AdaptationLineageInvalidException(
                $"KeySessionCompletedCount {summary.KeySessionCompletedCount} outside valid [0,{summary.KeySessionExpectedCount}] range.");
        }

        if (summary.EasyCompletedCount < 0 || summary.EasyCompletedCount > summary.EasyExpectedCount)
        {
            throw new AdaptationLineageInvalidException(
                $"EasyCompletedCount {summary.EasyCompletedCount} outside valid [0,{summary.EasyExpectedCount}] range.");
        }

        if (summary.LongRunCompleted && !summary.LongRunExpected)
        {
            throw new AdaptationLineageInvalidException("LongRunCompleted is true but LongRunExpected is false.");
        }
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
