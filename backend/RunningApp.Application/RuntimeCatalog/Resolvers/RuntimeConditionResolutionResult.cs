namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.1.5 — shared evaluation status for every
/// runtime-condition resolver result. Amended from Phase 4C/4D.1 (where only
/// TIME_ADEQUACY_IN's own ad hoc <c>TimeAdequacyResolutionOutcome</c> wrapper
/// had this concept) into the shared <see cref="RuntimeConditionResolutionResult"/>
/// itself, so every future resolver (PACE_SOURCE_IN, CORE_ENTRY_READINESS_IN,
/// GOAL_FEASIBILITY_IN, PLAN_MODE_IN) uses one consistent pattern instead of
/// each inventing its own missing-evidence behavior.
/// </summary>
public enum RuntimeConditionResolutionStatus
{
    /// <summary>A registry-valid <see cref="RuntimeConditionResolutionResult.OutputValue"/> was produced.</summary>
    Evaluated,

    /// <summary>
    /// The resolver could not produce a registry value because required
    /// OPTIONAL evidence/prerequisite context was missing (e.g. no recent
    /// race result for PACE_SOURCE_IN). <see cref="RuntimeConditionResolutionResult.OutputValue"/>
    /// is null. This is NOT a validation error and NOT a registry value —
    /// see PHASE4D_1_5_RESOLVER_CONTRACT_NOTEVALUATED_AMENDMENT.md for the
    /// full distinction from invalid request input and from registry values
    /// like GOAL_FEASIBILITY_IN.NOT_REQUESTED.
    /// </summary>
    NotEvaluated,
}

/// <summary>
/// Backend Integration Phase 4C (amended 4D.1.5) — generic output shape for a
/// single runtime-condition resolver decision. Construct only via
/// <see cref="Evaluated"/> or <see cref="NotEvaluated"/> — the private
/// constructor enforces the status/outputValue invariants described below;
/// there is no public object-initializer path that could produce an
/// inconsistent instance (e.g. Status=Evaluated with a null OutputValue).
///
/// IMPORTANT (Phase 4A.3 owner-approved V1 scope, unchanged by this
/// amendment): when <see cref="Status"/> is <see cref="RuntimeConditionResolutionStatus.Evaluated"/>,
/// <see cref="OutputValue"/> must always be one of the actual
/// runtime-condition-values registry's allowed values for
/// <see cref="ConditionType"/> (validate with
/// <see cref="RuntimeConditionRegistrySnapshot.IsValid"/>). Richer Appsel V1
/// product bands (e.g. CONSERVATIVE/STRETCH for goal feasibility, or a
/// pace-recency confidence ladder) are NEVER valid values for this property —
/// they belong in <see cref="Metadata"/> instead.
/// </summary>
public sealed class RuntimeConditionResolutionResult
{
    /// <summary>The condition type this result is for, e.g. "GOAL_FEASIBILITY_IN".</summary>
    public string ConditionType { get; }

    /// <summary>Whether this result carries a registry value (<see cref="RuntimeConditionResolutionStatus.Evaluated"/>) or not (<see cref="RuntimeConditionResolutionStatus.NotEvaluated"/>).</summary>
    public RuntimeConditionResolutionStatus Status { get; }

    /// <summary>
    /// The registry-simple output value. Non-null when <see cref="Status"/>
    /// is <see cref="RuntimeConditionResolutionStatus.Evaluated"/>; always
    /// null when <see cref="Status"/> is
    /// <see cref="RuntimeConditionResolutionStatus.NotEvaluated"/> — never a
    /// richer product-model value not present in the registry, and never a
    /// placeholder/sentinel string standing in for "not evaluated". This
    /// invariant is enforced by construction (private constructor; only
    /// <see cref="Evaluated"/>/<see cref="NotEvaluated"/> can create an
    /// instance), not just documented.
    /// </summary>
    public string? OutputValue { get; }

    /// <summary>
    /// Short machine-readable identifier for why this result was produced —
    /// either why this <see cref="OutputValue"/> was chosen (Evaluated) or
    /// why no value could be produced (NotEvaluated). Always required.
    /// </summary>
    public string ReasonCode { get; }

    /// <summary>The input evidence this result was computed from, if available.</summary>
    public ResolverInputSnapshot? InputSnapshot { get; }

    /// <summary>Non-fatal warnings surfaced during resolution (e.g. "recent race result is stale").</summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>True if a fallback/default path was taken due to missing or insufficient input evidence.</summary>
    public bool FallbackApplied { get; }

    /// <summary>
    /// Optional confidence label for this specific result (e.g. a pace-source
    /// recency confidence such as "HIGH"/"MODERATE"/"LOW"). Distinct from
    /// <see cref="OutputValue"/> — confidence is never itself a valid registry
    /// output value.
    /// </summary>
    public string? ConfidenceLabel { get; }

    /// <summary>
    /// Richer Appsel V1 product-model detail that has no registry home in V1
    /// (e.g. "aggressivenessBand" = "STRETCH", "paceEvidenceAgeDays" = "49",
    /// "paceEvidenceLayer" = "RECENT_RACE"). Free-form key/value strings —
    /// UX/explainability/decision-trace metadata only, never read as a
    /// runtime condition value by any stage-eligibility check. Allowed for
    /// both Evaluated and NotEvaluated results.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private RuntimeConditionResolutionResult(
        string conditionType,
        RuntimeConditionResolutionStatus status,
        string? outputValue,
        string reasonCode,
        ResolverInputSnapshot? inputSnapshot,
        IReadOnlyList<string>? warnings,
        bool fallbackApplied,
        string? confidenceLabel,
        IReadOnlyDictionary<string, string>? metadata)
    {
        ConditionType = conditionType;
        Status = status;
        OutputValue = outputValue;
        ReasonCode = reasonCode;
        InputSnapshot = inputSnapshot;
        Warnings = warnings ?? Array.Empty<string>();
        FallbackApplied = fallbackApplied;
        ConfidenceLabel = confidenceLabel;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Builds an Evaluated result. Throws <see cref="ArgumentException"/> if
    /// <paramref name="outputValue"/> is null/whitespace — Evaluated always
    /// requires a concrete registry-candidate value. This factory does NOT
    /// itself check registry membership (that requires an
    /// <see cref="RuntimeConditionRegistrySnapshot"/>, a separate read
    /// operation) — callers should validate with
    /// <see cref="RuntimeConditionRegistrySnapshot.IsValid"/> before trusting
    /// this result as final.
    /// </summary>
    public static RuntimeConditionResolutionResult Evaluated(
        string conditionType,
        string outputValue,
        string reasonCode,
        ResolverInputSnapshot? inputSnapshot = null,
        IReadOnlyList<string>? warnings = null,
        bool fallbackApplied = false,
        string? confidenceLabel = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(outputValue))
        {
            throw new ArgumentException("outputValue is required and cannot be null/whitespace for an Evaluated result.", nameof(outputValue));
        }

        return new RuntimeConditionResolutionResult(
            conditionType, RuntimeConditionResolutionStatus.Evaluated, outputValue, reasonCode,
            inputSnapshot, warnings, fallbackApplied, confidenceLabel, metadata);
    }

    /// <summary>
    /// Builds a NotEvaluated result. <see cref="OutputValue"/> is always
    /// null. Use only for missing OPTIONAL evidence/prerequisites — never for
    /// invalid request input (which should already have been rejected by
    /// frontend/backend validation before reaching a resolver) and never as a
    /// substitute for a real registry value like GOAL_FEASIBILITY_IN.NOT_REQUESTED.
    /// </summary>
    public static RuntimeConditionResolutionResult NotEvaluated(
        string conditionType,
        string reasonCode,
        ResolverInputSnapshot? inputSnapshot = null,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new RuntimeConditionResolutionResult(
            conditionType, RuntimeConditionResolutionStatus.NotEvaluated, null, reasonCode,
            inputSnapshot, warnings, fallbackApplied: false, confidenceLabel: null, metadata);
    }
}
