using System;
using System.Collections.Generic;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — proves (a) every currently-producible
/// resolver reasonCode classifies into its documented category (an unknown
/// code fails loud as TechnicalOrConfigurationFailure, never guessed), and
/// (b) CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy applies the
/// correct typed-exception policy per category — proving "required
/// unresolved resolver state returns an explicit error" and
/// "technical/configuration failure returns an explicit failure" even though
/// neither reasonCode is reachable end-to-end through the TEN_K/Race-only
/// pilot route today (see ApplyNotEvaluatedGovernancePolicy's own doc
/// comment on OptionalInputNotProvided for why this direct-call seam
/// exists).
/// </summary>
public sealed class NotEvaluatedReasonClassifierTests
{
    [Theory]
    [InlineData("NOT_APPLICABLE_NON_RACE_PLAN", NotEvaluatedReasonCategory.NotApplicable)]
    [InlineData("MISSING_PLAN_TYPE_CONTEXT", NotEvaluatedReasonCategory.RequiredInputNotProvided)]
    [InlineData("CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT", NotEvaluatedReasonCategory.NotApplicable)]
    [InlineData("MISSING_CORE_ENTRY_READINESS_RESULT", NotEvaluatedReasonCategory.DependencyUnresolved)]
    [InlineData("CORE_ENTRY_READINESS_NOT_EVALUATED", NotEvaluatedReasonCategory.UpstreamShortCircuit)]
    [InlineData("MISSING_TIME_ADEQUACY_RESULT", NotEvaluatedReasonCategory.DependencyUnresolved)]
    [InlineData("TIME_ADEQUACY_NOT_EVALUATED", NotEvaluatedReasonCategory.UpstreamShortCircuit)]
    [InlineData("TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED", NotEvaluatedReasonCategory.Unsupported)]
    [InlineData("MISSING_PACE_SOURCE_RESULT", NotEvaluatedReasonCategory.DependencyUnresolved)]
    [InlineData("PACE_SOURCE_NOT_EVALUATED", NotEvaluatedReasonCategory.UpstreamShortCircuit)]
    [InlineData("PACE_SOURCE_NONE_TARGET_TIME_REQUESTED", NotEvaluatedReasonCategory.Unsupported)]
    [InlineData("PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE", NotEvaluatedReasonCategory.Unsupported)]
    [InlineData("PACE_SOURCE_ESTIMATED_NO_APPROVED_METHOD", NotEvaluatedReasonCategory.Unsupported)]
    [InlineData("UNKNOWN_PACE_SOURCE_OUTPUT_VALUE", NotEvaluatedReasonCategory.TechnicalOrConfigurationFailure)]
    public void Classify_KnownReasonCode_ReturnsDocumentedCategory(string reasonCode, NotEvaluatedReasonCategory expected)
    {
        Assert.Equal(expected, NotEvaluatedReasonClassifier.Classify(reasonCode));
    }

    [Fact]
    public void Classify_UnknownReasonCode_FailsLoudAsTechnicalOrConfigurationFailure()
    {
        Assert.Equal(NotEvaluatedReasonCategory.TechnicalOrConfigurationFailure, NotEvaluatedReasonClassifier.Classify("SOME_FUTURE_UNMAPPED_CODE"));
    }

    private static List<RuntimeConditionResolutionResult> SingleResult(RuntimeConditionResolutionResult result) => new() { result };

    [Fact]
    public void ApplyNotEvaluatedGovernancePolicy_RequiredInputNotProvided_ThrowsRuntimeConditionRequiredInputMissing()
    {
        var results = SingleResult(RuntimeConditionResolutionResult.NotEvaluated("TIME_ADEQUACY_IN", "MISSING_PLAN_TYPE_CONTEXT"));

        Assert.Throws<RuntimeConditionRequiredInputMissingException>(() =>
            CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy(results));
    }

    [Fact]
    public void ApplyNotEvaluatedGovernancePolicy_DependencyUnresolved_ThrowsRuntimeConditionDependencyUnresolved()
    {
        var results = SingleResult(RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "MISSING_TIME_ADEQUACY_RESULT"));

        Assert.Throws<RuntimeConditionDependencyUnresolvedException>(() =>
            CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy(results));
    }

    [Fact]
    public void ApplyNotEvaluatedGovernancePolicy_Unsupported_ThrowsRuntimeConditionUnsupported()
    {
        var results = SingleResult(RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "PACE_SOURCE_NONE_TARGET_TIME_REQUESTED"));

        Assert.Throws<RuntimeConditionUnsupportedException>(() =>
            CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy(results));
    }

    [Fact]
    public void ApplyNotEvaluatedGovernancePolicy_UnknownReasonCode_TechnicalOrConfigurationFailure_ThrowsPlanPreviewGenerationFailed()
    {
        var results = SingleResult(RuntimeConditionResolutionResult.NotEvaluated("PACE_SOURCE_IN", "UNKNOWN_PACE_SOURCE_OUTPUT_VALUE"));

        Assert.Throws<PlanPreviewGenerationFailedException>(() =>
            CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy(results));
    }

    [Fact]
    public void ApplyNotEvaluatedGovernancePolicy_NotApplicableOrUpstreamShortCircuit_DoesNotThrow()
    {
        var results = new List<RuntimeConditionResolutionResult>
        {
            RuntimeConditionResolutionResult.NotEvaluated("TIME_ADEQUACY_IN", "NOT_APPLICABLE_NON_RACE_PLAN"),
            RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "TIME_ADEQUACY_NOT_EVALUATED"),
        };

        var exception = Record.Exception(() => CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy(results));

        Assert.Null(exception);
    }

    [Fact]
    public void ApplyNotEvaluatedGovernancePolicy_EvaluatedResults_DoesNotThrow()
    {
        var results = SingleResult(RuntimeConditionResolutionResult.Evaluated("TIME_ADEQUACY_IN", "ADEQUATE", "MEETS_DEFAULT_CORE_DURATION"));

        var exception = Record.Exception(() => CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy(results));

        Assert.Null(exception);
    }
}
