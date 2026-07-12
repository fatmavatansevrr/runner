namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4C (amended 4D.1.5) — one step in a future
/// decision trace, mirroring the shape plan-catalog's own golden-fixture-v3
/// decision trace already uses (step name, facts/inputs, result, ordering).
/// Contract-only: no code wires a "real" trace into any public response yet.
///
/// Amended in 4D.1.5 to carry <see cref="Status"/> and a nullable
/// <see cref="OutputValue"/>, mirroring <see cref="RuntimeConditionResolutionResult"/>
/// exactly — a trace step can represent either an Evaluated or a
/// NotEvaluated resolver outcome. Use <see cref="FromResult"/> to build a
/// step directly from a resolver's <see cref="RuntimeConditionResolutionResult"/>
/// so the two can never drift out of sync.
/// </summary>
public sealed class ResolverDecisionTraceStep
{
    /// <summary>Ordering index within the trace (0-based), mirroring the golden fixture's ordered "steps" array.</summary>
    public required int StepIndex { get; init; }

    /// <summary>Identifies which resolver produced this step, e.g. "GOAL_FEASIBILITY_RESOLVER".</summary>
    public required string ResolverKey { get; init; }

    /// <summary>The condition type resolved by this step, e.g. "GOAL_FEASIBILITY_IN".</summary>
    public required string ConditionType { get; init; }

    public ResolverInputSnapshot? InputSnapshot { get; init; }

    /// <summary>Whether this step is Evaluated (has an <see cref="OutputValue"/>) or NotEvaluated (does not).</summary>
    public required RuntimeConditionResolutionStatus Status { get; init; }

    /// <summary>
    /// Registry-simple output value for this step (same constraint as
    /// <see cref="RuntimeConditionResolutionResult.OutputValue"/>) — non-null
    /// only when <see cref="Status"/> is
    /// <see cref="RuntimeConditionResolutionStatus.Evaluated"/>.
    /// </summary>
    public string? OutputValue { get; init; }

    public required string ReasonCode { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public bool FallbackApplied { get; init; }

    /// <summary>Builds a trace step directly from a resolver's result, keeping Status/OutputValue in sync by construction.</summary>
    public static ResolverDecisionTraceStep FromResult(int stepIndex, string resolverKey, RuntimeConditionResolutionResult result) =>
        new()
        {
            StepIndex = stepIndex,
            ResolverKey = resolverKey,
            ConditionType = result.ConditionType,
            InputSnapshot = result.InputSnapshot,
            Status = result.Status,
            OutputValue = result.OutputValue,
            ReasonCode = result.ReasonCode,
            Metadata = result.Metadata,
            Warnings = result.Warnings,
            FallbackApplied = result.FallbackApplied,
        };
}

/// <summary>
/// Backend Integration Phase 4C — an ordered collection of resolver decision
/// steps for a single preview/generation attempt. Application-layer only:
/// not exposed on any public API DTO in this phase. See
/// PHASE4C_RUNTIME_RESOLVER_CONTRACT_AND_DECISION_TRACE_SKELETON.md for the
/// documented future exposure point (a detail/debug response, not the broad
/// preview/home payload — matching Phase 3's DTO-exposure deferral rationale).
/// </summary>
public sealed class ResolverDecisionTrace
{
    public IReadOnlyList<ResolverDecisionTraceStep> Steps { get; init; } = Array.Empty<ResolverDecisionTraceStep>();
}
