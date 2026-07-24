namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

internal enum ReadinessEligibilityOutcome
{
    /// <summary>No phase requires going below its own catalog minimum for this allocation.</summary>
    Pass,

    /// <summary>Some phase's allocation is below its own catalog minimum, and no product decision exists to justify it -- see TD-FOUNDATION-COMPRESSION-001.</summary>
    DecisionRequired,

    /// <summary>The allocation is not mathematically feasible.</summary>
    NotApplicable,
}

internal sealed record PhaseMinimumViolationCheck(
    string PhaseKey,
    int AllocatedWeeks,
    int CatalogMinimumWeeks,
    bool RequiresBelowMinimum);

internal sealed record ReadinessEligibilityVerificationResult(
    int TargetWeeks,
    IReadOnlyList<PhaseMinimumViolationCheck> PhaseChecks,
    ReadinessEligibilityOutcome Outcome,
    IReadOnlyList<string> Findings);

/// <summary>
/// Phase 4G.3B.3, verifier 6 of 9. Forward-looking governance check,
/// distinct from PhaseConstraintVerifier: PhaseConstraintVerifier proves the
/// allocator's OWN output is internally consistent (every phase already
/// stays within [MinimumWeeks, MaximumWeeks], by construction of the
/// current allocator). This verifier answers a different question: IF a
/// below-minimum allocation were ever produced -- which the current
/// allocator cannot produce today, but which is not structurally
/// impossible for a future candidate, catalog revision, or target week
/// count -- would that require a readiness decision that has not been
/// made? TD-FOUNDATION-COMPRESSION-001 (OPEN) documents exactly this
/// unresolved product question: whether Foundation (or any phase) may
/// compress below its catalog minimum only for sufficiently ready users,
/// and which CORE_ENTRY_READINESS_IN values would permit it.
///
/// Deliberately does not consume CORE_ENTRY_READINESS_IN or any runtime
/// condition -- it only inspects the already-computed AllocatedWeeks vs.
/// MinimumWeeks already carried on each AllocatedPhase in the supplied
/// PhaseAllocationResult. Pure function of its single parameter; no I/O,
/// no candidate/catalog loading. Never resolves TD-FOUNDATION-COMPRESSION-001
/// itself -- only detects the boundary condition and reports
/// DecisionRequired, citing it by ID. Not called from any live request path.
/// </summary>
internal static class ReadinessEligibilityVerifier
{
    public static ReadinessEligibilityVerificationResult Verify(PhaseAllocationResult allocation)
    {
        if (!allocation.IsMathematicallyFeasible)
        {
            return new(allocation.TargetWeeks, [], ReadinessEligibilityOutcome.NotApplicable,
                ["NOT_APPLICABLE_TARGET_MATHEMATICALLY_INFEASIBLE: no allocation exists for this target, so there is no phase-minimum boundary to check."]);
        }

        var phaseChecks = allocation.Phases
            .Select(p => new PhaseMinimumViolationCheck(p.PhaseKey, p.AllocatedWeeks, p.MinimumWeeks, p.AllocatedWeeks < p.MinimumWeeks))
            .ToList();

        var violations = phaseChecks.Where(c => c.RequiresBelowMinimum).ToList();
        if (violations.Count == 0)
        {
            return new(allocation.TargetWeeks, phaseChecks, ReadinessEligibilityOutcome.Pass, []);
        }

        var findings = violations
            .Select(v => $"DECISION_REQUIRED_BELOW_CATALOG_MINIMUM (TD-FOUNDATION-COMPRESSION-001): phase '{v.PhaseKey}' is allocated {v.AllocatedWeeks} week(s), below its own catalog MinimumWeeks={v.CatalogMinimumWeeks}. Resolving which CORE_ENTRY_READINESS_IN state(s) (READY/CAUTION/NOT_READY) would permit this is an open product/coaching decision outside this verifier's scope -- see plan-catalog/artifacts/audits/activation-readiness-risks.json entry TD-FOUNDATION-COMPRESSION-001. This verifier does not guess a default (neither permissive nor restrictive).")
            .ToList();

        return new(allocation.TargetWeeks, phaseChecks, ReadinessEligibilityOutcome.DecisionRequired, findings);
    }
}
