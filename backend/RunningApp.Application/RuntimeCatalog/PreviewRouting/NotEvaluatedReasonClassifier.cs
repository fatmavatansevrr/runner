namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — classifies a resolver's
/// <see cref="RunningApp.Application.RuntimeCatalog.Resolvers.RuntimeConditionResolutionStatus.NotEvaluated"/>
/// <c>reasonCode</c> into one of the governance-mandated categories, so
/// catalog-flow code can apply the correct policy per category instead of
/// treating every NotEvaluated result identically. NotEvaluated is never
/// equivalent to "successfully evaluated, eligibility not met" — a
/// NotEvaluated result must never automatically select a
/// <c>fallbackStageKey</c> (see <see cref="StageEligibilityEvaluator"/>).
/// </summary>
public enum NotEvaluatedReasonCategory
{
    /// <summary>The resolver is not required for the selected path (e.g. a non-race plan's TIME_ADEQUACY_IN). May continue.</summary>
    NotApplicable,

    /// <summary>A valid upstream resolver decision (itself NotEvaluated) is being propagated. Follow the upstream decision — do not independently re-decide.</summary>
    UpstreamShortCircuit,

    /// <summary>Optional evidence was not provided. May continue ONLY if the catalog explicitly supports generation without it (no reasonCode currently maps here — see remarks).</summary>
    OptionalInputNotProvided,

    /// <summary>Evidence required to proceed was not provided. Explicit domain error.</summary>
    RequiredInputNotProvided,

    /// <summary>The resolver recognizes the input combination but has no approved rule to classify it. Explicit error.</summary>
    Unsupported,

    /// <summary>A dependency result the resolver needed was absent from the pipeline entirely. Explicit error (indicates an orchestration/wiring defect, not a normal request state).</summary>
    DependencyUnresolved,

    /// <summary>An unrecognized or structurally invalid reasonCode reached the classifier. Explicit failure — never silently treated as any other category.</summary>
    TechnicalOrConfigurationFailure,
}

/// <summary>
/// Maps every reasonCode currently producible by
/// TimeAdequacyResolver/PaceSourceResolver/CoreEntryReadinessResolver/GoalFeasibilityResolver
/// (as of Phase 4D.5) to a <see cref="NotEvaluatedReasonCategory"/>. An
/// unrecognized reasonCode classifies as
/// <see cref="NotEvaluatedReasonCategory.TechnicalOrConfigurationFailure"/> —
/// fail loud on anything not explicitly accounted for, never guess.
///
/// Known limitation, recorded explicitly rather than silently glossed over:
/// <c>CoreEntryReadinessResolver</c>'s single reasonCode
/// "CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT" covers TWO
/// distinct upstream causes (a Habit-goal plan where core-entry readiness
/// genuinely does not apply, and an unknown/null GoalType where the resolver
/// conservatively declines to guess) — the resolver itself does not emit
/// separate reasonCodes for these two cases (see
/// PHASE4D_3_1_CORE_ENTRY_READINESS_THRESHOLD_DECISION_AND_RESOLVER_ACTIVATION.md).
/// This classifier maps the single shared reasonCode to
/// <see cref="NotEvaluatedReasonCategory.NotApplicable"/> (the more common,
/// primary-intent case) for both; distinguishing them precisely would
/// require a resolver-level change, out of scope for this phase.
/// </summary>
public static class NotEvaluatedReasonClassifier
{
    private static readonly IReadOnlyDictionary<string, NotEvaluatedReasonCategory> ReasonCodeToCategory =
        new Dictionary<string, NotEvaluatedReasonCategory>
        {
            // TimeAdequacyResolver (Phase 4D.1.5)
            ["NOT_APPLICABLE_NON_RACE_PLAN"] = NotEvaluatedReasonCategory.NotApplicable,
            ["MISSING_PLAN_TYPE_CONTEXT"] = NotEvaluatedReasonCategory.RequiredInputNotProvided,

            // CoreEntryReadinessResolver (Phase 4D.3.1) — see class doc "Known limitation".
            ["CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT"] = NotEvaluatedReasonCategory.NotApplicable,

            // GoalFeasibilityResolver (Phase 4D.4) — missing-dependency vs. NotEvaluated-dependency vs. unsupported.
            ["MISSING_CORE_ENTRY_READINESS_RESULT"] = NotEvaluatedReasonCategory.DependencyUnresolved,
            ["CORE_ENTRY_READINESS_NOT_EVALUATED"] = NotEvaluatedReasonCategory.UpstreamShortCircuit,
            ["MISSING_TIME_ADEQUACY_RESULT"] = NotEvaluatedReasonCategory.DependencyUnresolved,
            ["TIME_ADEQUACY_NOT_EVALUATED"] = NotEvaluatedReasonCategory.UpstreamShortCircuit,
            ["TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED"] = NotEvaluatedReasonCategory.Unsupported,
            ["MISSING_PACE_SOURCE_RESULT"] = NotEvaluatedReasonCategory.DependencyUnresolved,
            ["PACE_SOURCE_NOT_EVALUATED"] = NotEvaluatedReasonCategory.UpstreamShortCircuit,
            ["PACE_SOURCE_NONE_TARGET_TIME_REQUESTED"] = NotEvaluatedReasonCategory.Unsupported,
            ["PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE"] = NotEvaluatedReasonCategory.Unsupported,
            ["PACE_SOURCE_ESTIMATED_NO_APPROVED_METHOD"] = NotEvaluatedReasonCategory.Unsupported,
            ["UNKNOWN_PACE_SOURCE_OUTPUT_VALUE"] = NotEvaluatedReasonCategory.TechnicalOrConfigurationFailure,
        };

    public static NotEvaluatedReasonCategory Classify(string reasonCode) =>
        ReasonCodeToCategory.TryGetValue(reasonCode, out var category)
            ? category
            : NotEvaluatedReasonCategory.TechnicalOrConfigurationFailure;
}
