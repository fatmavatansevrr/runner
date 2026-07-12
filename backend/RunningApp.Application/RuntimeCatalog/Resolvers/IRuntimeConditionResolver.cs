namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4C (amended 4D.1.6) — base contract for a single
/// runtime-condition resolver. No production implementation is wired into
/// live generation in this phase — see
/// PHASE4C_RUNTIME_RESOLVER_CONTRACT_AND_DECISION_TRACE_SKELETON.md and
/// PHASE4D_1_6_RESOLVER_CONTEXT_CONTRACT_ALIGNMENT.md.
/// Implementations must never be registered in Program.cs or invoked from
/// PlanServices/PlaceholderPlanGenerationEngine until an explicit future
/// phase implements real resolver logic (Phase 4D.2+).
///
/// 4D.1.6 amendment: <see cref="Resolve"/> now takes a single
/// <see cref="RuntimeResolverContext"/> instead of a bare
/// <see cref="ResolverInputSnapshot"/> — the context carries the input
/// snapshot plus optional catalog/policy context (e.g.
/// <see cref="PlanCatalogCoreCycle"/>) and optional prior resolver results,
/// so a resolver that needs additional context (TIME_ADEQUACY_IN needs
/// CoreCycle; a future CORE_ENTRY_READINESS_IN may need readiness config; a
/// future GOAL_FEASIBILITY_IN may need prior resolver outputs) does not need
/// its own condition-specific method overload. This replaces
/// TimeAdequacyResolver's former two-argument
/// <c>Resolve(ResolverInputSnapshot, PlanCatalogCoreCycle)</c> shape.
/// </summary>
public interface IRuntimeConditionResolver
{
    /// <summary>The condition type this resolver produces, e.g. "GOAL_FEASIBILITY_IN". Must match a runtime-condition-values registry conditionType exactly.</summary>
    string ConditionType { get; }

    /// <summary>
    /// Resolves this condition type against the given context. Each
    /// implementation documents which optional <see cref="RuntimeResolverContext"/>
    /// fields it requires and how it treats their absence (a resolver
    /// context/configuration error vs. a
    /// <see cref="RuntimeConditionResolutionStatus.NotEvaluated"/> outcome —
    /// these are different things; see
    /// PHASE4D_1_6_RESOLVER_CONTEXT_CONTRACT_ALIGNMENT.md).
    /// </summary>
    RuntimeConditionResolutionResult Resolve(RuntimeResolverContext context);
}

/// <summary>Resolves GOAL_FEASIBILITY_IN. No production implementation exists in this phase.</summary>
public interface IGoalFeasibilityResolver : IRuntimeConditionResolver
{
}

/// <summary>Resolves PACE_SOURCE_IN. No production implementation exists in this phase.</summary>
public interface IPaceSourceResolver : IRuntimeConditionResolver
{
}

/// <summary>Resolves TIME_ADEQUACY_IN. No production implementation exists in this phase.</summary>
public interface ITimeAdequacyResolver : IRuntimeConditionResolver
{
}

/// <summary>Resolves CORE_ENTRY_READINESS_IN. No production implementation exists in this phase. Blocked on TD-REGISTRY-001 before real implementation.</summary>
public interface ICoreEntryReadinessResolver : IRuntimeConditionResolver
{
}

/// <summary>
/// Resolves PLAN_MODE_IN. Included because Phase 4A.3's owner-approved scope
/// explicitly requires PLAN_MODE_IN to be evaluated together with
/// TIME_ADEQUACY_IN and CORE_ENTRY_READINESS_IN for the sub-8-week /
/// readiness-gated-compressed composition (PLAN_MODE_IN.READINESS_ONLY) — see
/// PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md §B. No production
/// implementation exists in this phase.
/// </summary>
public interface IPlanModeResolver : IRuntimeConditionResolver
{
}

/// <summary>
/// Backend Integration Phase 4C — composite contract for running every
/// runtime-condition resolver against one input snapshot and assembling the
/// resulting decision trace. No production implementation exists in this
/// phase; a contract-only fake implementation exists in the test project only
/// (RunningApp.IntegrationTests), used solely to prove the contract shape,
/// never registered in Program.cs and never invoked from live generation.
/// </summary>
public interface IRuntimeConditionResolutionService
{
    ResolverDecisionTrace ResolveAll(RuntimeResolverContext context);
}
