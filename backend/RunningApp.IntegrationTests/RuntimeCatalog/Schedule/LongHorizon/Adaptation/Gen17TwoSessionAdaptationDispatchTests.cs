using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 10K-GEN.17 -- dark verification for the GEN.11 §9-frozen 2-session
/// Adaptation dispatch arm (2/2 Progress, 1/2 Maintain, 0/2 Reduce),
/// implemented in <see cref="NextWindowLoadDecisionPolicy"/>. No public HTTP,
/// no gate change -- this policy is reached only through the LongHorizon
/// rolling-activation pipeline, itself not publicly wired for 2D.
/// </summary>
public sealed class Gen17TwoSessionAdaptationDispatchTests
{
    private static WindowExecutionSummary Summary(int keyExpected, int keyCompleted, bool longExpected, bool longCompleted, int easyExpected, int easyCompleted, bool safetyFlag = false)
    {
        var expected = keyExpected + (longExpected ? 1 : 0) + easyExpected;
        var completed = keyCompleted + (longCompleted ? 1 : 0) + easyCompleted;
        return new WindowExecutionSummary(expected, completed, keyExpected, keyCompleted, longExpected, longCompleted, easyExpected, easyCompleted, 0, 0, safetyFlag);
    }

    [Fact]
    public void Evaluate_TwoOfTwoCompleted_ProgressAsPlanned()
    {
        // Pattern-A week: KEY_SESSION + LONG_RUN, both completed.
        var summary = Summary(keyExpected: 1, keyCompleted: 1, longExpected: true, longCompleted: true, easyExpected: 0, easyCompleted: 0);
        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, result.LoadDecision);
    }

    [Fact]
    public void Evaluate_OneOfTwoCompleted_KeyMissed_Maintain()
    {
        var summary = Summary(keyExpected: 1, keyCompleted: 0, longExpected: true, longCompleted: true, easyExpected: 0, easyCompleted: 0);
        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.Maintain, result.LoadDecision);
    }

    [Fact]
    public void Evaluate_OneOfTwoCompleted_LongMissed_Maintain()
    {
        // Pattern-B week: EASY_SUPPORT + LONG_RUN; long missed.
        var summary = Summary(keyExpected: 0, keyCompleted: 0, longExpected: true, longCompleted: false, easyExpected: 1, easyCompleted: 1);
        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.Maintain, result.LoadDecision);
    }

    [Fact]
    public void Evaluate_ZeroOfTwoCompleted_Reduce()
    {
        var summary = Summary(keyExpected: 1, keyCompleted: 0, longExpected: true, longCompleted: false, easyExpected: 0, easyCompleted: 0);
        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.Reduce, result.LoadDecision);
    }

    [Fact]
    public void Evaluate_SafetyFlag_IsIndependentOfLoadDecision()
    {
        var summary = Summary(keyExpected: 1, keyCompleted: 1, longExpected: true, longCompleted: true, easyExpected: 0, easyCompleted: 0, safetyFlag: true);
        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, result.LoadDecision);
        Assert.True(result.SafetyReviewRequired);
    }

    // ── Zero-delta: the existing 4/5/6-session arms are unaffected ──

    [Fact]
    public void Evaluate_LegacyFourSessionWeek_Unaffected()
    {
        var summary = Summary(keyExpected: 1, keyCompleted: 1, longExpected: true, longCompleted: true, easyExpected: 2, easyCompleted: 2);
        var result = NextWindowLoadDecisionPolicy.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, result.LoadDecision);
    }
}
