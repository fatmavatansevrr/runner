namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase 4G.3B.5 — the final composition layer before any future phase can
/// decide whether to actually enable a new race-plan horizon. This registry
/// does NOT make that activation decision itself; it only formalizes, as
/// data, what each target week count's CURRENT mechanical verification
/// status is, derived exclusively from already-produced
/// <see cref="SafetyVerificationPipelineResult"/> values (Phase 4G.3B.4b)
/// and, separately, <see cref="AllocationOrderVerificationResult"/> values
/// (Phase 4G.3B.3) -- see <see cref="RaceCoreSupportRegistryBuilder"/> for
/// the AllocationOrderCorrectnessVerifier inclusion decision.
///
/// <see cref="Supported"/> exists in <see cref="RaceCoreSupportStatus"/> only
/// as a placeholder for a future, explicitly separate activation step that
/// this phase does not build and that <see cref="RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification"/>
/// can NEVER produce -- see that method's own doc comment for why, and
/// PHASE4G_3B_5_SUPPORT_REGISTRY.md for the full design record including
/// why the already-live 12-week horizon still shows as
/// <see cref="MechanicallyPassed"/> here, not <see cref="Supported"/>.
///
/// Not called from any live request path. Not registered in DI. Performs no
/// governance-file I/O (no File/Path/Stream/JsonDocument/JsonSerializer/
/// IConfiguration access anywhere in this file) -- any TD citation visible
/// in <see cref="RaceCoreSupportEntry.BlockingFindings"/> is propagated
/// unchanged from an already-produced verifier finding string, never read
/// from a governance file by this registry itself.
/// </summary>
internal enum RaceCoreSupportStatus
{
    /// <summary>Default. No orchestrator result was supplied for this week count -- no verification run recorded.</summary>
    NotYetEvaluated,

    /// <summary>The orchestrator's own root allocation was mathematically infeasible for this target (OverallOutcome == NotApplicable).</summary>
    StructurallyInfeasible,

    /// <summary>The orchestrator reports Fail: at least one of the nine canonical verifiers normalized to Fail.</summary>
    Fail,

    /// <summary>
    /// The orchestrator reports DecisionRequired, OR the orchestrator
    /// reports Pass but the independently-consumed
    /// AllocationOrderCorrectnessVerifier result for this week count is
    /// DecisionRequired. This can NEVER become <see cref="Supported"/>
    /// without an explicit, separate step entirely outside this registry's
    /// own mechanical build logic -- there is no code path in
    /// <see cref="RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification"/>
    /// that upgrades a DecisionRequired result to anything else.
    /// </summary>
    DecisionRequired,

    /// <summary>
    /// The orchestrator reports Pass, AND (if AllocationOrderCorrectnessVerifier
    /// is included for this week count) its result is also Pass. This is
    /// NOT the same as "publicly supported" -- it means only that every
    /// mechanical safety/order check this registry knows how to run
    /// currently passes for the supplied context. It carries no product,
    /// governance, or activation approval.
    /// </summary>
    MechanicallyPassed,

    /// <summary>
    /// An explicit, separate activation decision has been recorded for this
    /// week count. NEVER produced by
    /// <see cref="RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification"/> --
    /// this value exists only so a future, explicitly-authorized phase has
    /// somewhere typed to record that decision once it is actually made
    /// (e.g. after resolving the relevant open TDs and completing a real
    /// product/governance approval). Setting this value is out of scope for
    /// Phase 4G.3B.5.
    /// </summary>
    Supported,
}

internal sealed record RaceCoreSupportEntry(
    int WeekCount,
    RaceCoreSupportStatus Status,
    SafetyVerificationPipelineResult? OrchestratorResult,
    string? AllocationOrderCorrectnessOutcome,
    string StatusReasonCode,
    IReadOnlyList<string> BlockingFindings);

internal sealed record RaceCoreSupportRegistry(
    string CandidateKey,
    IReadOnlyList<RaceCoreSupportEntry> Entries);

/// <summary>
/// Phase 4G.3B.5. See <see cref="RaceCoreSupportRegistry"/>'s own doc
/// comment for the full design context.
///
/// AllocationOrderCorrectnessVerifier inclusion decision (Question 3):
/// INCLUDED, as a second, independent gate alongside
/// SafetyVerificationOrchestrator's nine-verifier composition. Reasoning:
/// PHASE4G_3B_3_SAFETY_VERIFICATION_PIPELINE_PLANNING.md's own framing
/// decision (written when AllocationOrderCorrectnessVerifier was excluded
/// from the orchestrator) explicitly states "support activation will
/// eventually need to consider both [allocation-order correctness and the
/// canonical nine-verifier pipeline], but that belongs to the later
/// support-registry/activation layer" -- this registry IS that layer.
/// Excluding it here would leave every non-exhausted-headroom week count
/// (9, 10, 11, 13 for the real TEN_K__4D__INTERMEDIATE candidate) showing
/// MechanicallyPassed despite TD-ALLOCATION-PRIORITY-001 remaining
/// genuinely open and directly relevant to whether that specific
/// allocation's per-phase distribution can be trusted -- a materially
/// misleading signal for anyone consulting this registry before any future
/// activation consideration, and in tension with this phase's own
/// "never silently upgrade" acceptance criterion.
/// </summary>
internal static class RaceCoreSupportRegistryBuilder
{
    /// <summary>
    /// Purely mechanical: reports what the supplied verifier results say,
    /// nothing more. This method can NEVER return
    /// <see cref="RaceCoreSupportStatus.Supported"/> for any entry, under
    /// any input -- there is no branch, default, or fallback anywhere in
    /// this method (or the private helpers it calls) that produces that
    /// value; it is not a reachable case of any switch expression this
    /// method evaluates. The already-publicly-live 12-week horizon is
    /// deliberately reported as <see cref="RaceCoreSupportStatus.MechanicallyPassed"/>
    /// here, not <see cref="RaceCoreSupportStatus.Supported"/> -- this
    /// registry has no independent knowledge of what <c>RaceHorizonPolicy</c>
    /// actually enables today (it never reads that type or any live
    /// routing/policy state), so it cannot honestly claim more than "the
    /// mechanical checks it knows how to run currently pass."
    /// </summary>
    public static RaceCoreSupportRegistry BuildFromMechanicalVerification(
        string candidateKey,
        IReadOnlyDictionary<int, SafetyVerificationPipelineResult> orchestratorResultsByWeekCount,
        IReadOnlyDictionary<int, AllocationOrderVerificationResult>? allocationOrderResultsByWeekCount)
    {
        var weekCounts = orchestratorResultsByWeekCount.Keys
            .Union(allocationOrderResultsByWeekCount?.Keys ?? Enumerable.Empty<int>())
            .OrderBy(w => w)
            .ToList();

        var entries = weekCounts
            .Select(week => BuildEntry(week, orchestratorResultsByWeekCount, allocationOrderResultsByWeekCount))
            .ToList();

        return new RaceCoreSupportRegistry(candidateKey, entries);
    }

    private static RaceCoreSupportEntry BuildEntry(
        int week,
        IReadOnlyDictionary<int, SafetyVerificationPipelineResult> orchestratorResultsByWeekCount,
        IReadOnlyDictionary<int, AllocationOrderVerificationResult>? allocationOrderResultsByWeekCount)
    {
        if (!orchestratorResultsByWeekCount.TryGetValue(week, out var orchestratorResult))
        {
            return new RaceCoreSupportEntry(week, RaceCoreSupportStatus.NotYetEvaluated, null, null,
                "NOT_YET_EVALUATED_NO_ORCHESTRATOR_RESULT", Array.Empty<string>());
        }

        AllocationOrderVerificationResult? allocationOrderResult = null;
        var hasAllocationOrder = allocationOrderResultsByWeekCount is not null
            && allocationOrderResultsByWeekCount.TryGetValue(week, out allocationOrderResult);
        var allocationOrderOutcomeText = hasAllocationOrder ? allocationOrderResult!.Outcome.ToString() : null;

        switch (orchestratorResult.OverallOutcome)
        {
            case SafetyVerificationOverallOutcome.NotApplicable:
                return new RaceCoreSupportEntry(week, RaceCoreSupportStatus.StructurallyInfeasible, orchestratorResult,
                    allocationOrderOutcomeText, "STRUCTURALLY_INFEASIBLE_ALLOCATION",
                    orchestratorResult.PhaseConstraint.Findings.ToList());

            case SafetyVerificationOverallOutcome.Fail:
            {
                var firstFailing = orchestratorResult.OrderedSummaries.First(s => s.NormalizedOutcome == SafetyVerificationOverallOutcome.Fail);
                var blocking = orchestratorResult.OrderedSummaries
                    .Where(s => s.NormalizedOutcome == SafetyVerificationOverallOutcome.Fail)
                    .SelectMany(s => s.Findings)
                    .Select(f => $"{f.SourceVerifier}:{f.Code}: {f.Message}")
                    .ToList();
                return new RaceCoreSupportEntry(week, RaceCoreSupportStatus.Fail, orchestratorResult,
                    allocationOrderOutcomeText, $"ORCHESTRATOR_FAIL_AT_{firstFailing.Verifier}", blocking);
            }

            case SafetyVerificationOverallOutcome.DecisionRequired:
            {
                var firstDecisionRequired = orchestratorResult.OrderedSummaries.First(s => s.NormalizedOutcome == SafetyVerificationOverallOutcome.DecisionRequired);
                var blocking = orchestratorResult.OrderedSummaries
                    .Where(s => s.NormalizedOutcome == SafetyVerificationOverallOutcome.DecisionRequired)
                    .SelectMany(s => s.Findings)
                    .Select(f => $"{f.SourceVerifier}:{f.Code}: {f.Message}")
                    .ToList();
                return new RaceCoreSupportEntry(week, RaceCoreSupportStatus.DecisionRequired, orchestratorResult,
                    allocationOrderOutcomeText, $"ORCHESTRATOR_DECISION_REQUIRED_AT_{firstDecisionRequired.Verifier}", blocking);
            }

            case SafetyVerificationOverallOutcome.Pass:
                return BuildFromPassingOrchestrator(week, orchestratorResult, hasAllocationOrder,
                    allocationOrderResultsByWeekCount, allocationOrderOutcomeText);

            default:
                throw new NotSupportedException($"Unrecognized {nameof(SafetyVerificationOverallOutcome)} value: {orchestratorResult.OverallOutcome}.");
        }
    }

    private static RaceCoreSupportEntry BuildFromPassingOrchestrator(
        int week, SafetyVerificationPipelineResult orchestratorResult, bool hasAllocationOrder,
        IReadOnlyDictionary<int, AllocationOrderVerificationResult>? allocationOrderResultsByWeekCount,
        string? allocationOrderOutcomeText)
    {
        if (hasAllocationOrder)
        {
            var allocationOrderResult = allocationOrderResultsByWeekCount![week];
            if (allocationOrderResult.Outcome == AllocationOrderVerificationOutcome.DecisionRequired)
            {
                return new RaceCoreSupportEntry(week, RaceCoreSupportStatus.DecisionRequired, orchestratorResult,
                    allocationOrderOutcomeText, "ALLOCATION_ORDER_DECISION_REQUIRED",
                    new[] { allocationOrderResult.ReasonCode });
            }

            return new RaceCoreSupportEntry(week, RaceCoreSupportStatus.MechanicallyPassed, orchestratorResult,
                allocationOrderOutcomeText, "MECHANICALLY_PASSED_ORCHESTRATOR_AND_ALLOCATION_ORDER_PASS", Array.Empty<string>());
        }

        // Not held to the allocation-order gate for this week -- either the
        // gate is fully excluded from this build call (dictionary null) or
        // no result was supplied for this specific week count within an
        // otherwise-included dictionary. Both are reported as "not
        // evaluated for this week," never silently treated as Pass.
        var reasonCode = allocationOrderResultsByWeekCount is null
            ? "MECHANICALLY_PASSED_ORCHESTRATOR_PASS_ALLOCATION_ORDER_EXCLUDED"
            : "MECHANICALLY_PASSED_ORCHESTRATOR_PASS_ALLOCATION_ORDER_NOT_EVALUATED_FOR_WEEK";
        return new RaceCoreSupportEntry(week, RaceCoreSupportStatus.MechanicallyPassed, orchestratorResult,
            allocationOrderOutcomeText, reasonCode, Array.Empty<string>());
    }
}
