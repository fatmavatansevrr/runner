using System.Linq;

namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.5 — the first real implementation of
/// <see cref="IRuntimeConditionResolutionService"/>. Runs the four existing
/// concrete resolvers (<see cref="ITimeAdequacyResolver"/>,
/// <see cref="IPaceSourceResolver"/>, <see cref="ICoreEntryReadinessResolver"/>,
/// <see cref="IGoalFeasibilityResolver"/>) in the approved order and
/// automatically threads each result forward via
/// <see cref="RuntimeResolverContext.PriorResults"/>, so callers of
/// <see cref="GoalFeasibilityResolver"/> (or any future resolver needing
/// prior results) never have to hand-construct <c>PriorResults</c>
/// themselves for the normal pipeline case.
///
/// Orchestration only — this class is deliberately NOT referenced anywhere
/// in <c>PlanServices</c>/<c>PlaceholderPlanGenerationEngine</c>/live
/// generation. Registered in <c>Program.cs</c> as an internal service (per
/// this phase's own scope) with nothing consuming it from any live request
/// path. See PHASE4D_5_RUNTIME_CONDITION_RESOLVER_ORCHESTRATION_SERVICE.md
/// for the full rationale.
///
/// <see cref="RuntimeResolverContext"/> has no setters (init-only
/// properties) — each step's context is a fresh copy with an updated
/// <c>PriorResults</c> snapshot, never a mutation of the caller's original
/// context.
///
/// A resolver's configuration/context exceptions (e.g.
/// <see cref="TimeAdequacyResolver"/> throwing <see cref="InvalidOperationException"/>
/// for a missing <see cref="RuntimeResolverContext.CoreCycle"/>) are NEVER
/// caught or converted into a <see cref="RuntimeConditionResolutionStatus.NotEvaluated"/>
/// result here — they propagate to the caller unchanged, exactly as they
/// would from a direct, non-orchestrated call to that resolver.
/// </summary>
public sealed class RuntimeConditionResolutionService : IRuntimeConditionResolutionService
{
    private const string TimeAdequacyResolverKey = "TIME_ADEQUACY_RESOLVER";
    private const string PaceSourceResolverKey = "PACE_SOURCE_RESOLVER";
    private const string CoreEntryReadinessResolverKey = "CORE_ENTRY_READINESS_RESOLVER";
    private const string GoalFeasibilityResolverKey = "GOAL_FEASIBILITY_RESOLVER";

    private readonly ITimeAdequacyResolver _timeAdequacyResolver;
    private readonly IPaceSourceResolver _paceSourceResolver;
    private readonly ICoreEntryReadinessResolver _coreEntryReadinessResolver;
    private readonly IGoalFeasibilityResolver _goalFeasibilityResolver;

    public RuntimeConditionResolutionService(
        ITimeAdequacyResolver timeAdequacyResolver,
        IPaceSourceResolver paceSourceResolver,
        ICoreEntryReadinessResolver coreEntryReadinessResolver,
        IGoalFeasibilityResolver goalFeasibilityResolver)
    {
        _timeAdequacyResolver = timeAdequacyResolver;
        _paceSourceResolver = paceSourceResolver;
        _coreEntryReadinessResolver = coreEntryReadinessResolver;
        _goalFeasibilityResolver = goalFeasibilityResolver;
    }

    /// <summary>
    /// Runs all four resolvers in the approved order
    /// (TIME_ADEQUACY_IN → PACE_SOURCE_IN → CORE_ENTRY_READINESS_IN →
    /// GOAL_FEASIBILITY_IN) and returns the raw, ordered results — one per
    /// condition type, never duplicated. Does not swallow or short-circuit
    /// on a <see cref="RuntimeConditionResolutionStatus.NotEvaluated"/>
    /// result from an earlier resolver: every later resolver still runs,
    /// receiving that NotEvaluated result in its own
    /// <see cref="RuntimeResolverContext.PriorResults"/> exactly as it would
    /// see any other prior result — each resolver already has its own
    /// documented handling for a NotEvaluated dependency (see
    /// GoalFeasibilityResolver), so the orchestrator does not need to (and
    /// must not) invent a second short-circuit policy on top of that.
    /// </summary>
    public IReadOnlyList<RuntimeConditionResolutionResult> ResolveAllResults(RuntimeResolverContext initialContext)
    {
        var results = new List<RuntimeConditionResolutionResult>(4);

        var timeAdequacyResult = _timeAdequacyResolver.Resolve(initialContext);
        results.Add(timeAdequacyResult);

        var paceSourceResult = _paceSourceResolver.Resolve(WithPriorResults(initialContext, results));
        results.Add(paceSourceResult);

        var coreEntryReadinessResult = _coreEntryReadinessResolver.Resolve(WithPriorResults(initialContext, results));
        results.Add(coreEntryReadinessResult);

        var goalFeasibilityResult = _goalFeasibilityResolver.Resolve(WithPriorResults(initialContext, results));
        results.Add(goalFeasibilityResult);

        return results;
    }

    /// <summary>
    /// <see cref="IRuntimeConditionResolutionService"/> entry point — builds
    /// an internal <see cref="ResolverDecisionTrace"/> from
    /// <see cref="ResolveAllResults"/>. Application-layer only; not exposed
    /// on any public API DTO (unchanged from Phase 4C/4D.1.5's own
    /// documented deferral).
    /// </summary>
    public ResolverDecisionTrace ResolveAll(RuntimeResolverContext context)
    {
        var results = ResolveAllResults(context);
        var resolverKeys = new[] { TimeAdequacyResolverKey, PaceSourceResolverKey, CoreEntryReadinessResolverKey, GoalFeasibilityResolverKey };

        var steps = results
            .Select((result, index) => ResolverDecisionTraceStep.FromResult(index, resolverKeys[index], result))
            .ToList();

        return new ResolverDecisionTrace { Steps = steps };
    }

    /// <summary>
    /// Builds a fresh <see cref="RuntimeResolverContext"/> carrying the same
    /// <see cref="RuntimeResolverContext.InputSnapshot"/>/<see cref="RuntimeResolverContext.CoreCycle"/>/
    /// <see cref="RuntimeResolverContext.Config"/>/<see cref="RuntimeResolverContext.AsOfDate"/>
    /// as <paramref name="original"/>, with <see cref="RuntimeResolverContext.PriorResults"/>
    /// replaced by a snapshot copy of <paramref name="resultsSoFar"/>. Never
    /// mutates <paramref name="original"/> — <see cref="RuntimeResolverContext"/>
    /// has no setters, so mutation is not even possible; this method exists
    /// to make each step's context construction explicit and readable.
    /// </summary>
    private static RuntimeResolverContext WithPriorResults(RuntimeResolverContext original, IReadOnlyList<RuntimeConditionResolutionResult> resultsSoFar) =>
        new()
        {
            InputSnapshot = original.InputSnapshot,
            CoreCycle = original.CoreCycle,
            Config = original.Config,
            AsOfDate = original.AsOfDate,
            PriorResults = resultsSoFar.ToList(),
        };
}
