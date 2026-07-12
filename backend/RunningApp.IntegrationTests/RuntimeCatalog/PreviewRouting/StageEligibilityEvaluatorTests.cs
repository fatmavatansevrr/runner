using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — proves the exact fallbackStageKey
/// governance rule: NotEvaluated must NEVER automatically select a fallback
/// stage (it always blocks, carrying the classified reason category instead),
/// while a successfully EVALUATED condition whose value is outside the
/// allowed set MAY select its valid catalog fallback.
/// </summary>
public sealed class StageEligibilityEvaluatorTests
{
    private static readonly StageEligibilityRequirement Requirement =
        new("GOAL_FEASIBILITY_IN", new[] { "REALISTIC", "CHALLENGING" });

    [Fact]
    public void Evaluate_NotEvaluatedResult_NeverAutoSelectsFallback_EvenWhenFallbackKeyIsSupplied()
    {
        var result = RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "PACE_SOURCE_NOT_EVALUATED");

        var outcome = StageEligibilityEvaluator.Evaluate(Requirement, result, "primary-stage", "fallback-stage");

        Assert.Equal(StageEligibilityOutcomeKind.BlockedByNotEvaluated, outcome.Kind);
        Assert.Null(outcome.SelectedStageKey);
        Assert.Equal(NotEvaluatedReasonCategory.UpstreamShortCircuit, outcome.Category);
        Assert.Equal("PACE_SOURCE_NOT_EVALUATED", outcome.ReasonCode);
    }

    [Fact]
    public void Evaluate_NotEvaluatedResult_WithNoFallbackKeySupplied_StillBlocksRatherThanNoEligibleStage()
    {
        var result = RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "PACE_SOURCE_NOT_EVALUATED");

        var outcome = StageEligibilityEvaluator.Evaluate(Requirement, result, "primary-stage", fallbackStageKey: null);

        Assert.Equal(StageEligibilityOutcomeKind.BlockedByNotEvaluated, outcome.Kind);
    }

    [Fact]
    public void Evaluate_EvaluatedIneligibleValue_WithFallbackKeySupplied_SelectsFallbackStage()
    {
        var result = RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "AGGRESSIVE", "RATIO_ABOVE_CHALLENGING_MAX");

        var outcome = StageEligibilityEvaluator.Evaluate(Requirement, result, "primary-stage", "fallback-stage");

        Assert.Equal(StageEligibilityOutcomeKind.FallbackStageSelected, outcome.Kind);
        Assert.Equal("fallback-stage", outcome.SelectedStageKey);
    }

    [Fact]
    public void Evaluate_EvaluatedIneligibleValue_WithNoFallbackKeySupplied_ReturnsNoEligibleStage()
    {
        var result = RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "AGGRESSIVE", "RATIO_ABOVE_CHALLENGING_MAX");

        var outcome = StageEligibilityEvaluator.Evaluate(Requirement, result, "primary-stage", fallbackStageKey: null);

        Assert.Equal(StageEligibilityOutcomeKind.NoEligibleStage, outcome.Kind);
        Assert.Null(outcome.SelectedStageKey);
    }

    [Fact]
    public void Evaluate_EvaluatedEligibleValue_SelectsPrimaryStage_RegardlessOfFallbackKeyPresence()
    {
        var result = RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "RATIO_WITHIN_REALISTIC_MAX");

        var outcome = StageEligibilityEvaluator.Evaluate(Requirement, result, "primary-stage", "fallback-stage");

        Assert.Equal(StageEligibilityOutcomeKind.PrimaryStageEligible, outcome.Kind);
        Assert.Equal("primary-stage", outcome.SelectedStageKey);
    }
}
