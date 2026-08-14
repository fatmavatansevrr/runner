using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4K.4 — governance cross-check for
/// <c>TD-LONG-HORIZON-RUNWAY-CORE-JIT-CONTEXT-001</c>
/// (CLOSED, a policy-shape decision grounded in real, existing pipeline
/// dependency tracing -- zero production code).
/// </summary>
public sealed class LongHorizonRunwayCoreJitContextGovernanceTests
{
    private const string DecisionId = "TD-LONG-HORIZON-RUNWAY-CORE-JIT-CONTEXT-001";
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

    // 1, 3. repository dependency findings recorded (real, not invented).
    [Fact]
    public void RepositoryDependencyFinding_ProvesRunwayRequiresCoreTarget()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("REPOSITORY DEPENDENCY FINDING", text);
        Assert.Contains("REQUIRES a Core Week-1 numeric and pace target as a required, validated, non-null input", text);
    }

    // 2. Runway JIT timing is exact (week-boundary terms).
    [Fact]
    public void RunwayJitTiming_IsExactWeekBoundary()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("the checkpoint that activates the first four-week rolling window (Phase 4K.1) containing any Runway week", text);
        Assert.Contains("EndWeek >= N+1 and StartWeek <= N+1", text);
    }

    // 4. atomic versus separate resolution explicit.
    [Fact]
    public void AtomicResolution_ExplicitlyApproved()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("ATOMIC RUNWAY+CORE RESOLUTION", text);
        Assert.Contains("resolved ATOMICALLY, at the SAME checkpoint", text);
    }

    // Core JIT timing: only in-window Core weeks activate; the rest refresh later.
    [Fact]
    public void CoreJitTiming_OnlyInWindowWeeksActivate_RestRefreshLater()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("CORE'S FULL SCHEDULE VS ACTIVATED CORE WEEKS", text);
        Assert.Contains("Core weeks beyond that window remain NumericPending", text);
        Assert.Contains("recomputed FRESH, via the same unchanged Core pipeline", text);
    }

    // Core Week-1 target locking (required invariant).
    [Fact]
    public void CoreWeekOneTarget_LockedForActivatedWeeks_VersionedForFutureWeeks()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("CORE WEEK-1 TARGET LOCKING", text);
        Assert.Contains("that specific target is LOCKED the moment it is used to activate any Runway numeric week", text);
        Assert.Contains("versioned future-only Core refresh with explicit compatibility validation", text);
    }

    // 5, 6. actual validated evidence outranks onboarding/planned GE exit.
    [Fact]
    public void ActualValidatedEvidence_OutranksPlannedGeExit()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("anchored to CURRENT VALIDATED ACTUAL evidence", text);
        Assert.Contains("not the last planned GE peak", text);
        Assert.Contains("planned GE exit never outranks actual validated evidence", text);
    }

    // 7. planned Runway exit does not prove Core capacity (independence confirmed).
    [Fact]
    public void CoreWeekOneTarget_IndependentFromRunwayEvidence()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("Core's Week-1 target is NOT derived from the same evidence used at Runway entry (confirmed independent, intentional", text);
    }

    // 8, 9. weekly-volume and long-run precedence deterministic.
    [Fact]
    public void EvidencePrecedence_IsDeterministic()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("EVIDENCE PRECEDENCE", text);
        Assert.Contains("otherwise blocked (JIT_VALIDATED_LOAD_UNAVAILABLE)", text);
        Assert.Contains("Long-run precedence mirrors this identically", text);
    }

    // 10, 11. target-time precedence and pace-source resolution reuse existing service.
    [Fact]
    public void TargetTimeAndPaceSource_ReuseExistingResolverService()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("reuses RuntimeConditionResolutionService, the existing target-time-source policy, and the existing recent-race freshness ladder verbatim and unmodified", text);
    }

    // 12, 13. no Core-target lifting, no GE ceiling.
    [Fact]
    public void NoCoreTargetLifting_NoGeCeiling()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("must never override lower actual capacity nor force Runway upward", text);
    }

    // 14, 16. mixed GE/Runway/Core window behavior explicit; GE policy never prescribes Runway/Core.
    [Fact]
    public void MixedWindow_BehaviorExplicit()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("MIXED-SEGMENT WINDOW POLICY", text);
        Assert.Contains("GE weeks use the Phase 4K.3 checkpoint Growth/Maintenance decision; Runway weeks use the approved JIT Runway context", text);
    }

    // 15. mixed-window atomicity explicit.
    [Fact]
    public void MixedWindow_AtomicityExplicit()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("ATOMICITY (Option A approved): the entire four-week mixed window succeeds or none of it activates", text);
    }

    // 17. Runway policy never rewrites GE history.
    [Fact]
    public void RunwayPolicy_NeverRewritesGeHistory()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("completed history dates never move; only unstarted future weeks may receive revised dates", text);
    }

    // 18. Core context never silently rewrites activated Runway weeks.
    [Fact]
    public void CoreRefresh_NeverInvalidatesActivatedRunwayWeeks()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("such recalculation NEVER invalidates already-activated Runway prescriptions", text);
    }

    // 19. race date remains authoritative.
    [Fact]
    public void RaceDate_RemainsStructurallyAuthoritative()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("the race date remains structurally authoritative and immutable (Phase 4K.1, reaffirmed)", text);
    }

    // 20. delayed activation behavior explicit.
    [Fact]
    public void DelayedActivation_ReusesExistingTimeAdequacyResolver()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("a delayed checkpoint does NOT shorten Runway/Core or shift the race date", text);
        Assert.Contains("resolved via the EXISTING, unmodified TimeAdequacyResolver/goal-feasibility taxonomy", text);
    }

    // 21, 22. missing JIT context blocks deterministically with one typed reason.
    [Fact]
    public void MissingJitContext_BlocksDeterministically_OneReason()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("deterministic NumericActivationBlocked with exactly one typed reason", text);
        Assert.Contains("explicitly FORBIDDEN fallbacks, reaffirmed verbatim: planned GE peak, stale original baseline, fabricated zero, generic product-average weekly volume, or GE maintenance rules applied to Runway/Core", text);
    }

    // 23. safety remains highest priority.
    [Fact]
    public void Safety_RemainsHighestPriority()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("SAFETY_REASSESSMENT_REQUIRED (Phase 4K.3) is reused directly, not duplicated, and retains the highest evaluation priority here too", text);
    }

    // 24, 25. activated numeric weeks immutable; future-only replacement is versioned.
    [Fact]
    public void ActivatedWeeks_Immutable_FutureReplacement_Versioned()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("already-activated numeric weeks, INCLUDING the Core Week-1 target locked into their interpolation math", text);
        Assert.Contains("never silently change", text);
        Assert.Contains("future-only recalculation produces a NEW decision identity/version rather than mutating the prior one in place", text);
    }

    // 26. both profiles use the approved JIT policy.
    [Fact]
    public void BothProfiles_UseIdenticalJitPolicy()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("identical JIT timing, evidence precedence, blocked conditions, and context immutability for both CONSISTENCY_NEEDED and CORE_ENTRY_READY", text);
    }

    // 27. exact implementation types remain deferred.
    [Fact]
    public void ImplementationTypes_RemainDeferredToPhase4K5()
    {
        var risk = Risk(DecisionId);
        var resolution = risk.GetProperty("requiredResolution").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains(resolution, r => r!.Contains("Phase 4K.5"));
        Assert.Contains("production types are deferred to Phase 4K.5", risk.GetRawText());
    }

    // 28. no production code changed.
    [Fact]
    public void ZeroProductionCodeChanges()
    {
        var risk = Risk(DecisionId);
        Assert.Contains("Zero production code was changed", risk.GetProperty("currentRuntimeImpact").GetString());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    // JIT reason taxonomy: ten approved codes present, additive to Phase 4K.3.
    [Fact]
    public void JitReasonTaxonomy_ContainsAllTenApprovedCodes()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("RUNWAY_JIT_CONTEXT_UNAVAILABLE", text);
        Assert.Contains("CORE_JIT_CONTEXT_UNAVAILABLE", text);
        Assert.Contains("JIT_VALIDATED_LOAD_UNAVAILABLE", text);
        Assert.Contains("JIT_VALIDATED_LONG_RUN_UNAVAILABLE", text);
        Assert.Contains("JIT_PACE_SOURCE_UNRESOLVED", text);
        Assert.Contains("JIT_GOAL_FEASIBILITY_UNRESOLVED", text);
        Assert.Contains("JIT_AVAILABILITY_INFEASIBLE", text);
        Assert.Contains("JIT_EVIDENCE_CONFLICT_UNRESOLVED", text);
        Assert.Contains("JIT_ACTIVATION_BOUNDARY_MISSED", text);
        Assert.Contains("JIT_SEGMENT_TRANSITION_INFEASIBLE", text);
    }

    [Fact]
    public void NonClaims_RecordedVerbatim()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("JIT resolution does not measure laboratory fitness", text);
        Assert.Contains("no medical or wearable requirement is introduced", text);
    }

    [Fact]
    public void PathADecision_UpdatedToReflectApprovedJitPolicy()
    {
        var text = Risk(PathADecisionId).GetRawText();
        Assert.Contains("Phase 4K.4", text);
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
