using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split D — implementation closure of the already-`FREQ.6`-approved,
/// already-fidelity-verified (`FREQ.6D.1B` Track A, 24/24) 5-session Adaptation severity
/// table against the real <see cref="NextWindowLoadDecisionPolicy"/>. All 24 reachable
/// states are reproduced verbatim from `FREQ.6 §6`'s frozen table (via `FREQ.6D.1B §3`'s
/// full reproduction), using the real, already-generalized aggregate `KeySessionExpectedCount`/
/// `KeySessionCompletedCount` pair (`Phase 10K-FREQ.4`) rather than separate Key1/Key2 fields —
/// both KEY lanes are severity-equivalent (`FREQ.6 §5`), so every row's outcome is identical
/// whichever lane was the sole miss, and the aggregate representation keeps Adaptation
/// lane-blind by construction.
/// </summary>
public sealed class Freq6D4DSplitDFiveSessionAdaptationSeverityTests
{
    private static WindowExecutionSummary FiveSessionSummary(
        int effectiveCompleted, bool key1Completed, bool key2Completed, bool longCompleted, int easyCompleted, bool safetyFlag = false) =>
        new(
            ExpectedSessionCount: 5,
            EffectiveCompletedCount: effectiveCompleted,
            KeySessionExpectedCount: 2,
            KeySessionCompletedCount: (key1Completed ? 1 : 0) + (key2Completed ? 1 : 0),
            LongRunExpected: true,
            LongRunCompleted: longCompleted,
            EasyExpectedCount: 2,
            EasyCompletedCount: easyCompleted,
            UnrecoveredNotTodayCount: 5 - effectiveCompleted,
            SupersededByAdaptationCount: 0,
            HasSafetyFlag: safetyFlag);

    // ── FREQ.6 §6's real, frozen 24-row table — reproduced verbatim, all 24 states ──

    [Theory]
    [InlineData(1, 0, false, false, false, 0, NextWindowLoadDecision.Reduce)]
    [InlineData(2, 1, false, false, false, 1, NextWindowLoadDecision.Reduce)]
    [InlineData(3, 2, false, false, false, 2, NextWindowLoadDecision.Maintain)]
    [InlineData(4, 1, false, false, true, 0, NextWindowLoadDecision.Reduce)]
    [InlineData(5, 2, false, false, true, 1, NextWindowLoadDecision.Maintain)]
    [InlineData(6, 3, false, false, true, 2, NextWindowLoadDecision.Maintain)]
    [InlineData(7, 1, false, true, false, 0, NextWindowLoadDecision.Reduce)]
    [InlineData(8, 2, false, true, false, 1, NextWindowLoadDecision.Maintain)]
    [InlineData(9, 3, false, true, false, 2, NextWindowLoadDecision.Maintain)]
    [InlineData(10, 2, false, true, true, 0, NextWindowLoadDecision.Maintain)]
    [InlineData(11, 3, false, true, true, 1, NextWindowLoadDecision.Maintain)]
    [InlineData(12, 4, false, true, true, 2, NextWindowLoadDecision.Maintain)] // sole miss KEY1
    [InlineData(13, 1, true, false, false, 0, NextWindowLoadDecision.Reduce)]
    [InlineData(14, 2, true, false, false, 1, NextWindowLoadDecision.Maintain)]
    [InlineData(15, 3, true, false, false, 2, NextWindowLoadDecision.Maintain)]
    [InlineData(16, 2, true, false, true, 0, NextWindowLoadDecision.Maintain)]
    [InlineData(17, 3, true, false, true, 1, NextWindowLoadDecision.Maintain)]
    [InlineData(18, 4, true, false, true, 2, NextWindowLoadDecision.Maintain)] // sole miss KEY2
    [InlineData(19, 2, true, true, false, 0, NextWindowLoadDecision.Maintain)]
    [InlineData(20, 3, true, true, false, 1, NextWindowLoadDecision.Maintain)]
    [InlineData(21, 4, true, true, false, 2, NextWindowLoadDecision.Maintain)] // sole miss LONG
    [InlineData(22, 3, true, true, true, 0, NextWindowLoadDecision.Maintain)]
    [InlineData(23, 4, true, true, true, 1, NextWindowLoadDecision.ProgressAsPlanned)] // sole miss EASY
    [InlineData(24, 5, true, true, true, 2, NextWindowLoadDecision.ProgressAsPlanned)]
    internal void Row_MatchesFrozenFreq6SeverityTable(
        int rowNumber, int effectiveCompleted, bool key1, bool key2, bool longCompleted, int easyCompleted, NextWindowLoadDecision expected)
    {
        var summary = FiveSessionSummary(effectiveCompleted, key1, key2, longCompleted, easyCompleted);

        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);

        Assert.True(expected == result.LoadDecision, $"Row {rowNumber}: expected {expected}, got {result.LoadDecision}.");
    }

    [Fact]
    public void All24Rows_ExactlyTwentyFourInlineDataCases()
    {
        var method = typeof(Freq6D4DSplitDFiveSessionAdaptationSeverityTests).GetMethod(
            nameof(Row_MatchesFrozenFreq6SeverityTable),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var inlineDataCount = method.GetCustomAttributes(typeof(InlineDataAttribute), false).Length;
        Assert.Equal(24, inlineDataCount);
    }

    // ── Explicit, individually named count-4 role-gate tests (§26 of the phase prompt) ──

    [Fact]
    public void CountFour_SoleMissKeyLane0_Maintain() =>
        Assert.Equal(NextWindowLoadDecision.Maintain, NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(4, false, true, true, 2)).LoadDecision);

    [Fact]
    public void CountFour_SoleMissKeyLane1_Maintain() =>
        Assert.Equal(NextWindowLoadDecision.Maintain, NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(4, true, false, true, 2)).LoadDecision);

    [Fact]
    public void CountFour_SoleMissLong_Maintain() =>
        Assert.Equal(NextWindowLoadDecision.Maintain, NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(4, true, true, false, 2)).LoadDecision);

    [Fact]
    public void CountFour_SoleMissEasySlotA_Progress() =>
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(4, true, true, true, 1)).LoadDecision);

    // Both EASY slots are symmetric (FREQ.6 §5: "EASY1 and EASY2 are symmetric") — the aggregate
    // EasyCompletedCount=1 represents either slot missing identically; there is no second,
    // slot-distinguishing test to write without inventing per-slot identity the real policy
    // deliberately does not carry.
    [Fact]
    public void CountFour_SoleMissEasySlotB_Progress_SameAggregateAsSlotA() =>
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(4, true, true, true, 1)).LoadDecision);

    // ── KEY lane severity equivalence (§28) ──

    [Fact]
    public void KeyLane0AndLane1_AreSeverityEquivalent_BothProduceMaintainAtCountFour()
    {
        var missLane0 = NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(4, false, true, true, 2)).LoadDecision;
        var missLane1 = NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(4, true, false, true, 2)).LoadDecision;

        Assert.Equal(missLane0, missLane1);
        Assert.Equal(NextWindowLoadDecision.Maintain, missLane0);
    }

    // ── 0-3/5 explicit results (§41.B of the phase prompt) ──

    [Fact]
    public void ZeroOfFive_Reduce() =>
        Assert.Equal(NextWindowLoadDecision.Reduce, NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(0, false, false, false, 0)).LoadDecision);

    [Fact]
    public void OneOfFive_Reduce() =>
        Assert.Equal(NextWindowLoadDecision.Reduce, NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(1, false, false, false, 1)).LoadDecision);

    [Fact]
    public void TwoOfFive_Maintain() =>
        Assert.Equal(NextWindowLoadDecision.Maintain, NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(2, false, false, false, 2)).LoadDecision);

    [Fact]
    public void ThreeOfFive_Maintain() =>
        Assert.Equal(NextWindowLoadDecision.Maintain, NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(3, false, false, true, 2)).LoadDecision);

    [Fact]
    public void FiveOfFive_Progress() =>
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, NextWindowLoadDecisionPolicy.Evaluate(FiveSessionSummary(5, true, true, true, 2)).LoadDecision);

    // ── Existing 4-session (legacy) behavior must remain byte-for-byte unchanged (§23) ──

    private static WindowExecutionSummary FourSessionSummary(int effectiveCompleted, bool keyCompleted, bool longCompleted, int easyCompleted) =>
        new(
            ExpectedSessionCount: 4,
            EffectiveCompletedCount: effectiveCompleted,
            KeySessionExpectedCount: 1,
            KeySessionCompletedCount: keyCompleted ? 1 : 0,
            LongRunExpected: true,
            LongRunCompleted: longCompleted,
            EasyExpectedCount: 2,
            EasyCompletedCount: easyCompleted,
            UnrecoveredNotTodayCount: 4 - effectiveCompleted,
            SupersededByAdaptationCount: 0,
            HasSafetyFlag: false);

    [Fact]
    public void FourSessionWeek_FullyCompleted_ProgressAsPlanned_NotMisreadAsFiveSessionCountFour() =>
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(4, true, true, 2)).LoadDecision);

    [Fact]
    public void FourSessionWeek_3Of4OnlyEasyMissing_ProgressAsPlanned_UnchangedFromBeforeSplitD() =>
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(3, true, true, 1)).LoadDecision);

    [Fact]
    public void FourSessionWeek_3Of4KeyMissing_Maintain_UnchangedFromBeforeSplitD() =>
        Assert.Equal(NextWindowLoadDecision.Maintain, NextWindowLoadDecisionPolicy.Evaluate(FourSessionSummary(3, false, true, 2)).LoadDecision);

    // ── Fail-closed invalid-state behavior (§31) ──

    [Fact]
    public void InvalidState_RoleExpectedSumMismatch_ThrowsTypedException()
    {
        var invalid = new WindowExecutionSummary(
            ExpectedSessionCount: 5, EffectiveCompletedCount: 3,
            KeySessionExpectedCount: 1, KeySessionCompletedCount: 1, // should be 2 for a real 5-session week
            LongRunExpected: true, LongRunCompleted: true,
            EasyExpectedCount: 2, EasyCompletedCount: 1,
            UnrecoveredNotTodayCount: 2, SupersededByAdaptationCount: 0, HasSafetyFlag: false);

        Assert.Throws<AdaptationLineageInvalidException>(() => NextWindowLoadDecisionPolicy.Evaluate(invalid));
    }

    [Fact]
    public void InvalidState_RoleCompletedSumMismatchesEffectiveCompletedCount_ThrowsTypedException()
    {
        var invalid = new WindowExecutionSummary(
            ExpectedSessionCount: 5, EffectiveCompletedCount: 4, // claims 4 completed
            KeySessionExpectedCount: 2, KeySessionCompletedCount: 2,
            LongRunExpected: true, LongRunCompleted: true,
            EasyExpectedCount: 2, EasyCompletedCount: 2, // but role sum is 2+1+2=5, not 4
            UnrecoveredNotTodayCount: 1, SupersededByAdaptationCount: 0, HasSafetyFlag: false);

        Assert.Throws<AdaptationLineageInvalidException>(() => NextWindowLoadDecisionPolicy.Evaluate(invalid));
    }

    [Fact]
    public void InvalidState_KeyCompletedExceedsExpected_ThrowsTypedException()
    {
        var invalid = new WindowExecutionSummary(
            ExpectedSessionCount: 5, EffectiveCompletedCount: 5,
            KeySessionExpectedCount: 1, KeySessionCompletedCount: 2, // impossible: completed > expected
            LongRunExpected: true, LongRunCompleted: true,
            EasyExpectedCount: 2, EasyCompletedCount: 2,
            UnrecoveredNotTodayCount: 0, SupersededByAdaptationCount: 0, HasSafetyFlag: false);

        Assert.Throws<AdaptationLineageInvalidException>(() => NextWindowLoadDecisionPolicy.Evaluate(invalid));
    }

    [Fact]
    public void InvalidState_LongCompletedButNotExpected_ThrowsTypedException()
    {
        var invalid = new WindowExecutionSummary(
            ExpectedSessionCount: 5, EffectiveCompletedCount: 5,
            KeySessionExpectedCount: 2, KeySessionCompletedCount: 2,
            LongRunExpected: false, LongRunCompleted: true, // impossible combination
            EasyExpectedCount: 2, EasyCompletedCount: 2,
            UnrecoveredNotTodayCount: 0, SupersededByAdaptationCount: 0, HasSafetyFlag: false);

        Assert.Throws<AdaptationLineageInvalidException>(() => NextWindowLoadDecisionPolicy.Evaluate(invalid));
    }

    // ── SafetyReviewRequired remains independent of the 5-session LoadDecision ──

    [Fact]
    public void SafetyReviewRequired_IndependentOfFiveSessionLoadDecision()
    {
        var summary = FiveSessionSummary(4, true, true, true, 1, safetyFlag: true);
        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, result.LoadDecision);
        Assert.True(result.SafetyReviewRequired);
    }
}
