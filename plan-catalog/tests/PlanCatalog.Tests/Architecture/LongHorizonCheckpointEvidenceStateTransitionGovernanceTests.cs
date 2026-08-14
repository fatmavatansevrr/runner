using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4K.3 — governance cross-check for
/// <c>TD-LONG-HORIZON-CHECKPOINT-EVIDENCE-STATE-TRANSITION-001</c>
/// (CLOSED, a policy-shape-and-formula decision, zero production code).
/// </summary>
public sealed class LongHorizonCheckpointEvidenceStateTransitionGovernanceTests
{
    private const string DecisionId = "TD-LONG-HORIZON-CHECKPOINT-EVIDENCE-STATE-TRANSITION-001";
    private const string PathADecisionId = "TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void DecisionRecordedAsClosed()
    {
        var risk = Risk(DecisionId);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
    }

    // 1, 2. checkpoint evidence uses actual history only; planned future values excluded.
    [Fact]
    public void EvidenceSnapshot_UsesActualHistoryOnly_ExcludesFutureValues()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("EVIDENCE SNAPSHOT", text);
        Assert.Contains("future planned values (null per Phase 4K.1) NEVER enter the snapshot", text);
    }

    // 3. weekly-volume freshness explicit.
    [Fact]
    public void WeeklyVolumeFreshness_IsExplicitFourWeekProductDefault()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("WEEKLY-VOLUME FRESHNESS", text);
        Assert.Contains("an explicit Appsel PRODUCT DEFAULT since no existing repository rule addresses weekly-volume evidence age", text);
    }

    // 4. long-run freshness explicit; distinct from the unrelated 30-day race-pace ladder.
    [Fact]
    public void LongRunFreshness_ReusesWindowDefault_NotTheRacePaceLadder()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("LONG-RUN FRESHNESS", text);
        Assert.Contains("not the unrelated 30-day race-pace ladder", text);
    }

    // 5. adherence freshness explicit.
    [Fact]
    public void AdherenceFreshness_MostRecentBlockOnly_NeverInheritedIndefinitely()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("ADHERENCE FRESHNESS", text);
        Assert.Contains("MUST NOT be inherited indefinitely from older blocks", text);
    }

    // 6. safety evidence behavior explicit (fail closed, never expires).
    [Fact]
    public void SafetyEvidence_NeverExpiresAutomatically_FailsClosed()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("An unresolved safety-critical signal NEVER expires automatically", text);
        Assert.Contains("fails closed", text);
    }

    // 7. terminal-window rule explicit.
    [Fact]
    public void TerminalWindowRule_RequiresCalendarEndAndAllSessionsTerminal()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("TERMINAL-WINDOW RULE", text);
        Assert.Contains("Completed, Missed, Skipped, or SoftMissed", text);
    }

    // 8. unresolved sessions do not silently count as completed or zero.
    [Fact]
    public void UnresolvedSessions_NeverSilentlyCountedAsZeroOrCompleted()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("unresolved sessions are NEVER silently counted as zero or Completed", text);
    }

    // 9. partial completion treatment explicit.
    [Fact]
    public void PartialCompletion_NoDistinctStatus_NoPercentageThresholdInvented()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("PARTIAL COMPLETION", text);
        Assert.Contains("no separate percentage-complete threshold is invented", text);
    }

    // 10. missed treatment explicit.
    [Fact]
    public void MissedTreatment_ContributesZeroDistance_LowersAdherence()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("MISSED AND NOT-TODAY TREATMENT", text);
        Assert.Contains("Missed contributes zero actual distance and lowers adherence", text);
    }

    // 11. NotToday treatment explicit.
    [Fact]
    public void NotTodayTreatment_IdenticalToUnexplainedMissed()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("NotToday-resolved sessions (default ResultingStatus=Missed) are treated IDENTICALLY to unexplained Missed sessions", text);
    }

    // 12, 13. adherence confidence rule deterministic; no unsupported percentage invented.
    [Fact]
    public void AdherenceConfidenceGate_IsDeterministic_NoNewPercentageInvented()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("no accepted numeric adherence threshold exists anywhere in the repository", text);
        Assert.Contains("no new percentage is invented", text);
        Assert.Contains("every non-recovery week of the activated window contains at least one Completed session with usable actual evidence", text);
    }

    // 14. Growth sufficiency explicit.
    [Fact]
    public void GrowthSufficiency_IsExplicit()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("EVIDENCE SUFFICIENCY", text);
        Assert.Contains("Growth requires: terminal window (6); validated weekly load AND validated long-run evidence available from the CURRENT window", text);
    }

    // 15. Maintenance sufficiency explicit.
    [Fact]
    public void MaintenanceSufficiency_IsExplicit()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("Maintenance requires: a validated weekly load available from the current OR a prior fresh window", text);
    }

    // 16. prior validated anchor behavior explicit.
    [Fact]
    public void PriorValidatedAnchor_UsedInFull_NeverBlendedWithPartialWindow()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("the prior block's own validated load is used in full as a Maintenance anchor (never a blend)", text);
    }

    // 17. Growth does not guarantee an increase.
    [Fact]
    public void Growth_DoesNotGuaranteeIncrease()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("GROWTH DOES NOT MEAN AUTOMATIC INCREASE", text);
        Assert.Contains("it does not mean every week increases, the first week must exceed the maintenance anchor, caps may be bypassed, or a new peak is guaranteed", text);
    }

    // 18. Maintenance does not create a new peak.
    [Fact]
    public void Maintenance_UsesUnchangedPhase4K2Rules_NoNewPeak()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("MaintenanceOnly always uses the Phase 4K.2 anchor/shape/Week-4-recovery rules unchanged", text);
    }

    // 19. safety-critical signals cannot produce Growth.
    [Fact]
    public void SafetyCriticalSignal_CannotProduceGrowth_HighestPriority()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("it can never produce GrowthEligible", text);
        Assert.Contains("evaluated with the highest priority in the transition table", text);
    }

    // 20. feasibility failure blocks activation.
    [Fact]
    public void FeasibilityFailure_BlocksActivation()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("(f) numericFeasible=false -> NumericActivationBlocked/NUMERIC_WINDOW_INFEASIBLE", text);
    }

    // 21, 22. state transition table exhaustive and mutually exclusive.
    [Fact]
    public void StateTransitionTable_IsExhaustiveAndMutuallyExclusive()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("STATE-TRANSITION TABLE -- eight deterministic, mutually exclusive cases evaluated in this priority order", text);
        Assert.Contains("(first match wins, guaranteeing exactly one output)", text);
    }

    // 23. every blocked state has one reason.
    [Fact]
    public void BlockedState_AlwaysCarriesExactlyOneReason()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("Blocked always carries exactly one authoritative reason, never a generic collapse of safety and missing-evidence causes into one code", text);
    }

    // 24. Maintenance carries decision provenance.
    [Fact]
    public void MaintenanceOnly_AlwaysCarriesNonFailureProvenanceReason()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("MaintenanceOnly always carries a non-failure decision-provenance reason explaining why Growth was not selected", text);
    }

    // 25. initial activation distinct from checkpoint activation.
    [Fact]
    public void InitialWindow_DistinctFromCheckpointMachinery()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("INITIAL WINDOW -- the plan's very first numeric window uses the existing, unmodified Phase 4I.6 onboarding-evidence baseline mechanism", text);
        Assert.Contains("NOT this checkpoint's freshness/terminal-window/adherence-confidence machinery", text);
    }

    // 26. GE logic does not prescribe Runway/Core.
    [Fact]
    public void GeLogic_NeverPrescribesRunwayOrCore()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("ordinary GE Growth/Maintenance logic NEVER prescribes Runway or Core weeks", text);
    }

    // 27. segment-boundary JIT handoff explicit.
    [Fact]
    public void SegmentBoundary_JitHandoffExplicit_UnavailableContextBlocksActivation()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("if that JIT context is unavailable at the boundary, activation becomes NumericActivationBlocked", text);
        Assert.Contains("explicitly deferred to Phase 4K.4", text);
    }

    // 28. both profiles use the approved transition policy.
    [Fact]
    public void BothProfiles_UseIdenticalTransitionPolicy()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("identical freshness, terminal-window, partial/missed/NotToday treatment, adherence-confidence rule, safety handling, and state-transition table for both CONSISTENCY_NEEDED and CORE_ENTRY_READY", text);
    }

    // 29. no production code changes.
    [Fact]
    public void ZeroProductionCodeChanges()
    {
        var risk = Risk(DecisionId);
        Assert.Contains("Zero production code was changed", risk.GetProperty("currentRuntimeImpact").GetString());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    // 30. Runway/Core exact timing remains deferred to Phase 4K.4.
    [Fact]
    public void RunwayCoreExactTiming_RemainsDeferredToPhase4K4()
    {
        var risk = Risk(DecisionId);
        var resolution = risk.GetProperty("requiredResolution").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains(resolution, r => r!.Contains("Phase 4K.4"));
    }

    // Reason taxonomy: exactly the nine approved reason codes are present.
    [Fact]
    public void ReasonTaxonomy_ContainsAllNineApprovedCodes()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("CHECKPOINT_WINDOW_NOT_COMPLETE", text);
        Assert.Contains("CHECKPOINT_EVIDENCE_STALE", text);
        Assert.Contains("VALIDATED_LOAD_UNAVAILABLE", text);
        Assert.Contains("VALIDATED_LONG_RUN_EVIDENCE_UNAVAILABLE", text);
        Assert.Contains("ADHERENCE_CONFIDENCE_INSUFFICIENT_FOR_GROWTH", text);
        Assert.Contains("MAINTENANCE_ANCHOR_UNAVAILABLE", text);
        Assert.Contains("NUMERIC_WINDOW_INFEASIBLE", text);
        Assert.Contains("SAFETY_REASSESSMENT_REQUIRED", text);
        Assert.Contains("EVIDENCE_CONFLICT_UNRESOLVED", text);
    }

    [Fact]
    public void NonClaims_RecordedVerbatim()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("checkpoint state is a product-planning decision, not a medical diagnosis", text);
        Assert.Contains("no universal adherence percentage is claimed (none was found, none was invented)", text);
    }

    [Fact]
    public void PathADecision_UpdatedToReflectApprovedCheckpointPolicy()
    {
        var text = Risk(PathADecisionId).GetRawText();
        Assert.Contains("Phase 4K.3", text);
        Assert.Equal("OPEN", Risk(PathADecisionId).GetProperty("status").GetString());
    }

    [Fact]
    public void DeferredStagedPlanBlocker_RemainsOpen()
    {
        Assert.Equal("OPEN", Risk(DeferredDependencyId).GetProperty("status").GetString());
    }

    [Fact]
    public void MarkdownProjection_ContainsTheDecisionRowWithClosedStatus()
    {
        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains($"`{DecisionId}`", markdown);
        var section = markdown.Substring(markdown.IndexOf(DecisionId, StringComparison.Ordinal));
        Assert.Contains("**CLOSED**", section);
    }

    [Fact]
    public void AggregateCountSentence_IsInternallyConsistent()
    {
        var json = File.ReadAllText(JsonPath());
        var match = System.Text.RegularExpressions.Regex.Match(
            json, @"(\d+)\s+risks are now recorded in total:\s*(\d+)\s+OPEN and\s*(\d+)\s+CLOSED");
        Assert.True(match.Success, "Aggregate sentence not found in JSON.");
        var total = int.Parse(match.Groups[1].Value);
        var open = int.Parse(match.Groups[2].Value);
        var closed = int.Parse(match.Groups[3].Value);
        Assert.Equal(total, open + closed);

        using var document = JsonDocument.Parse(json);
        var risks = document.RootElement.GetProperty("risks").EnumerateArray().ToList();
        Assert.Equal(total, risks.Count);
        Assert.Equal(open, risks.Count(r => r.GetProperty("status").GetString() == "OPEN"));
        Assert.Equal(closed, risks.Count(r => r.GetProperty("status").GetString() == "CLOSED"));
    }

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "artifacts", "audits")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Plan-catalog root not found.");
    }

    private static string JsonPath() => Path.Combine(Root(), "artifacts", "audits", "activation-readiness-risks.json");
    private static string MarkdownPath() => Path.Combine(Root(), "artifacts", "audits", "activation-readiness-risks.md");
}
