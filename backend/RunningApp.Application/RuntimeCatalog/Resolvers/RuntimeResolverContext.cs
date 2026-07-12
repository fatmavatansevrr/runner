namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.1.6 — the execution context a resolver
/// evaluates against, as distinct from <see cref="ResolverInputSnapshot"/>
/// (the user/request/onboarding evidence snapshot).
///
/// Boundary rule (why this type exists instead of growing
/// <see cref="ResolverInputSnapshot"/> further): <see cref="ResolverInputSnapshot"/>
/// holds only evidence that originates from the user/request — goal
/// identity, schedule dates, and Phase 4B fitness-evidence fields. Catalog
/// identity/config (e.g. <see cref="PlanCatalogCoreCycle"/>, read from a
/// PLAN_TEMPLATE document) and cross-resolver composition data (prior
/// resolver results) are a DIFFERENT kind of input — they come from Process A
/// catalog state or from earlier steps in the same resolution pass, never
/// from the user directly. Mixing the two into one snapshot type would make
/// it unclear, field by field, whether a given piece of data came from the
/// user or from the catalog/pipeline — <see cref="RuntimeResolverContext"/>
/// keeps that distinction explicit and inspectable.
///
/// This type replaces TimeAdequacyResolver's former
/// TIME_ADEQUACY-specific <c>Resolve(ResolverInputSnapshot, PlanCatalogCoreCycle)</c>
/// two-argument overload (Phase 4D.1/4D.1.5) with one shared shape every
/// resolver interface uses, so future resolvers (CORE_ENTRY_READINESS_IN
/// needing readiness config, GOAL_FEASIBILITY_IN needing prior resolver
/// outputs and pace-projection context) do not each invent their own
/// condition-specific overload.
/// </summary>
public sealed class RuntimeResolverContext
{
    /// <summary>The user/request/onboarding evidence snapshot. Always required.</summary>
    public required ResolverInputSnapshot InputSnapshot { get; init; }

    /// <summary>
    /// Core-cycle week boundaries from the candidate's PLAN_TEMPLATE (see
    /// <see cref="PlanCatalogCandidateSummary.CoreCycle"/>). Optional at the
    /// context-model level because not every resolver needs it (e.g.
    /// PACE_SOURCE_IN does not) — but a resolver that DOES need it (e.g.
    /// TIME_ADEQUACY_IN) treats its absence as a configuration/context error,
    /// not missing user evidence. See
    /// PHASE4D_1_6_RESOLVER_CONTEXT_CONTRACT_ALIGNMENT.md.
    /// </summary>
    public PlanCatalogCoreCycle? CoreCycle { get; init; }

    /// <summary>
    /// Results already produced by other resolvers earlier in the same
    /// resolution pass (e.g. GOAL_FEASIBILITY_IN may need
    /// CORE_ENTRY_READINESS_IN's result). Optional — empty/null for a
    /// resolver run in isolation, or for the first step of a composed pass.
    /// No resolver in this repository reads this field yet.
    /// </summary>
    public IReadOnlyList<RuntimeConditionResolutionResult>? PriorResults { get; init; }

    /// <summary>
    /// Free-form future policy/config slots (e.g. a future readiness
    /// threshold set), kept consistent with <see cref="RuntimeConditionResolutionResult.Metadata"/>'s
    /// free-form-dictionary style. No resolver in this repository reads this
    /// field yet — present only so a future resolver does not need another
    /// contract amendment just to receive a small piece of config.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Config { get; init; }

    /// <summary>
    /// Backend Integration Phase 4D.2 — the reference "as of" date a resolver
    /// should use for evidence-age calculations (e.g. how many days old a
    /// recent race result is). Added because the golden-fixture-v3 decision
    /// trace's own <c>PACE_CONVERSION</c> step computes <c>resultAgeDays</c>
    /// against a field named <c>confirmDate</c> — explicitly distinct from
    /// <c>planStartDate</c> in the same fixture's <c>INPUT_SNAPSHOT</c>
    /// (confirmDate=2026-07-30 vs. planStartDate=2026-08-03) — so evidence
    /// age is computed relative to when the plan is being generated/
    /// confirmed, not relative to the plan's future start date.
    ///
    /// Deliberately NOT populated from <see cref="DateTime.UtcNow"/>
    /// automatically inside any resolver — a resolver reading the system
    /// clock directly would make its output non-reproducible and hidden from
    /// its own inputs, unlike every other field a resolver consumes. If this
    /// is null, a resolver needing it must not invent a fallback date; it
    /// should report age/recency as not computable rather than guess.
    /// </summary>
    public DateOnly? AsOfDate { get; init; }
}
