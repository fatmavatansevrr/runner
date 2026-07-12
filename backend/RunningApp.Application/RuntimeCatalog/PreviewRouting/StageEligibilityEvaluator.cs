using RunningApp.Application.RuntimeCatalog.Resolvers;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// A catalog workout-progression stage's <c>requires</c> clause, e.g.
/// <c>ten-k-workout-progression.v5.json</c>'s GOAL_PACE_REHEARSAL stage:
/// <c>{conditionType: "GOAL_FEASIBILITY_IN", allowedValues: ["REALISTIC", "CHALLENGING"]}</c>.
/// </summary>
public sealed record StageEligibilityRequirement(string ConditionType, IReadOnlyList<string> AllowedValues);

/// <summary>Which of the four possible outcomes <see cref="StageEligibilityEvaluator.Evaluate"/> produced.</summary>
public enum StageEligibilityOutcomeKind
{
    /// <summary>The condition was evaluated and its value is in AllowedValues — the primary stage is eligible.</summary>
    PrimaryStageEligible,

    /// <summary>The condition was evaluated, its value is NOT in AllowedValues, and a valid fallbackStageKey was supplied — the fallback stage is selected.</summary>
    FallbackStageSelected,

    /// <summary>The condition was evaluated, its value is NOT in AllowedValues, and no fallbackStageKey was supplied — no eligible stage.</summary>
    NoEligibleStage,

    /// <summary>The condition was NotEvaluated. No stage was selected — the caller must apply <see cref="Category"/>'s governance policy, NOT auto-select a fallback.</summary>
    BlockedByNotEvaluated,
}

/// <summary>
/// Result of evaluating one stage's eligibility requirement against one
/// resolver result. When <see cref="Kind"/> is <see cref="StageEligibilityOutcomeKind.BlockedByNotEvaluated"/>,
/// <see cref="Category"/> and <see cref="ReasonCode"/> are populated so the
/// caller can apply the correct governance policy (continue / explicit
/// domain error / explicit failure) — see
/// PHASE4E_1_CATALOG_PREVIEW_ROUTING_AND_IMMUTABLE_RESOLUTION_SNAPSHOT.md.
/// </summary>
public sealed class StageEligibilityOutcome
{
    public required StageEligibilityOutcomeKind Kind { get; init; }
    public string? SelectedStageKey { get; init; }
    public NotEvaluatedReasonCategory? Category { get; init; }
    public string? ReasonCode { get; init; }

    public static StageEligibilityOutcome PrimaryStageEligible(string stageKey) =>
        new() { Kind = StageEligibilityOutcomeKind.PrimaryStageEligible, SelectedStageKey = stageKey };

    public static StageEligibilityOutcome FallbackStageSelected(string fallbackStageKey) =>
        new() { Kind = StageEligibilityOutcomeKind.FallbackStageSelected, SelectedStageKey = fallbackStageKey };

    public static StageEligibilityOutcome NoEligibleStage() =>
        new() { Kind = StageEligibilityOutcomeKind.NoEligibleStage };

    public static StageEligibilityOutcome BlockedByNotEvaluated(NotEvaluatedReasonCategory category, string reasonCode) =>
        new() { Kind = StageEligibilityOutcomeKind.BlockedByNotEvaluated, Category = category, ReasonCode = reasonCode };
}

/// <summary>
/// Backend Integration Phase 4E.1 — decides whether a catalog stage's
/// primary workout, its <c>fallbackStageKey</c>, or neither applies, given a
/// resolver's result. Governance rule, enforced exactly: <c>fallbackStageKey</c>
/// may be selected ONLY when the condition was applicable, successfully
/// evaluated (Status=Evaluated), its eligibility requirement was not met,
/// AND a fallback stage key was actually supplied. A
/// <see cref="RuntimeConditionResolutionStatus.NotEvaluated"/> result NEVER
/// automatically triggers a fallback — it always produces
/// <see cref="StageEligibilityOutcomeKind.BlockedByNotEvaluated"/> instead,
/// carrying the classified <see cref="NotEvaluatedReasonCategory"/> for the
/// caller to act on.
///
/// Pure, stateless decision logic — not wired into any generation pipeline
/// (stage-to-week scheduling remains unimplemented, per every prior Phase 4D
/// resolver phase's own explicit boundary). Exists so this exact rule is
/// directly testable in isolation ahead of a future phase that implements
/// real stage selection.
/// </summary>
public static class StageEligibilityEvaluator
{
    public static StageEligibilityOutcome Evaluate(
        StageEligibilityRequirement requirement,
        RuntimeConditionResolutionResult conditionResult,
        string primaryStageKey,
        string? fallbackStageKey)
    {
        if (conditionResult.ConditionType != requirement.ConditionType)
        {
            throw new ArgumentException(
                $"Requirement conditionType '{requirement.ConditionType}' does not match result conditionType " +
                $"'{conditionResult.ConditionType}'.");
        }

        if (conditionResult.Status == RuntimeConditionResolutionStatus.NotEvaluated)
        {
            var category = NotEvaluatedReasonClassifier.Classify(conditionResult.ReasonCode);
            return StageEligibilityOutcome.BlockedByNotEvaluated(category, conditionResult.ReasonCode);
        }

        // Evaluated.
        if (requirement.AllowedValues.Contains(conditionResult.OutputValue))
        {
            return StageEligibilityOutcome.PrimaryStageEligible(primaryStageKey);
        }

        return fallbackStageKey is not null
            ? StageEligibilityOutcome.FallbackStageSelected(fallbackStageKey)
            : StageEligibilityOutcome.NoEligibleStage();
    }
}
