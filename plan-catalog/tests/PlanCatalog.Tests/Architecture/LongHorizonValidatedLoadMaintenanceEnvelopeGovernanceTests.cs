using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4K.2 — governance cross-check for
/// <c>TD-LONG-HORIZON-VALIDATED-LOAD-AND-MAINTENANCE-ENVELOPE-001</c>
/// (CLOSED, a policy-shape-and-formula decision, zero production code).
/// </summary>
public sealed class LongHorizonValidatedLoadMaintenanceEnvelopeGovernanceTests
{
    private const string DecisionId = "TD-LONG-HORIZON-VALIDATED-LOAD-AND-MAINTENANCE-ENVELOPE-001";
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

    // 1, 2. validated load uses actual completed evidence; planned-but-incomplete excluded.
    [Fact]
    public void ValidatedLoad_UsesActualCompletedEvidence_NotPlanned()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("TrainingDay.ActualDistanceKm", text);
        Assert.Contains("Status=Completed", text);
        Assert.Contains("never inferred from PlannedDistanceKm", text);
    }

    // 3, 4, 5. evidence window explicit; partial-completion and missed-session treatment explicit.
    [Fact]
    public void EvidenceWindow_PartialAndMissedTreatment_Explicit()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("most recent activated numeric window", text);
        Assert.Contains("Missed/Skipped/SoftMissed", text);
        Assert.Contains("never excluded from the denominator", text);
    }

    // 6. recovery-week treatment explicit.
    [Fact]
    public void RecoveryWeekTreatment_ExcludedFromAverage_NotAssumedAchieved()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("EXCLUDED from the average", text);
        Assert.Contains("NEVER automatically assumed achieved", text);
    }

    // 7. weekly-load formula deterministic (mean, rounded).
    [Fact]
    public void WeeklyLoadFormula_IsDeterministicMeanRounded()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("mean of ACTUAL summed weekly distance across the non-recovery weeks", text);
        Assert.Contains("0.5km increment", text);
    }

    // 8, 9. long-run evidence formula explicit; one anomalous run cannot define capacity alone.
    [Fact]
    public void LongRunFormula_BoundedByHardCapShare_NoSingleRunOverride()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("mean of completed LONG_RUN ActualDistanceKm", text);
        Assert.Contains("bounded by the EXISTING LongRunHardCapShare(0.40)", text);
        Assert.Contains("a single anomalously long completed run can never independently define the next block's long run", text);
    }

    // 10, 11. adherence role explicit; no unsupported percentage invented.
    [Fact]
    public void AdherenceRole_ReusesExistingTerm_NoThresholdInvented()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("AdherenceRatePercent", text);
        Assert.Contains("No exact adherence threshold is invented", text);
        Assert.Contains("Phase 4K.3", text);
    }

    // 12, 13. GrowthEligible conditions explicit; does not create a second progression formula.
    [Fact]
    public void GrowthEligible_ExplicitConditions_NoSecondProgressionFormula()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("GrowthEligible", text);
        Assert.Contains("no second progression formula was created", text);
        Assert.Contains("GrowthEligible does NOT mean", text);
    }

    // 14, 15. MaintenanceOnly conditions explicit; maintenance is not a universal fallback.
    [Fact]
    public void MaintenanceOnly_ExplicitConditions_NotUniversalFallback()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("MAINTENANCE ONLY", text);
        Assert.Contains("explicitly NOT a fallback for invalid inputs", text);
    }

    // 16, 17, 18. maintenance anchor explicit; no Taper multiplier; no recovery multiplier as anchor.
    [Fact]
    public void MaintenanceAnchor_IsValidatedLoadDirectly_NoTaperOrRecoveryMultiplier()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("MaintenanceAnchorWeeklyVolumeKm = ValidatedSustainableWeeklyVolumeKm directly", text);
        Assert.Contains("explicitly NOT a percentage of original baseline, planned peak, Core Week-1 target, GE recovery volume, or the 0.53 Taper multiplier", text);
    }

    // 19, 20. maintenance cannot create a new weekly or long-run peak.
    [Fact]
    public void Maintenance_CannotCreateNewWeeklyOrLongRunPeak()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("no maintenance week may produce a new planned weekly-volume peak", text);
        Assert.Contains("algebraically cannot produce a new peak either", text);
    }

    // 21. existing recovery week remains 0.85.
    [Fact]
    public void RecoveryWeek_Remains085Multiplier()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("EXISTING 0.85 recovery formula", text);
    }

    // 22, 23. consecutive maintenance windows covered; partial GE windows covered.
    [Fact]
    public void ConsecutiveMaintenanceAndPartialWindows_Covered()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("multiple MaintenanceOnly windows may occur back-to-back", text);
        Assert.Contains("anchor is REFRESHED each window", text);
        Assert.Contains("PARTIAL WINDOWS", text);
    }

    // 24. Runway weeks do not use GE maintenance policy.
    [Fact]
    public void RunwayWeeks_NeverUseGeMaintenancePolicy()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("Runway weeks are governed exclusively by the EXISTING, unmodified Runway numeric authority", text);
    }

    // 25. blocked conditions explicit with distinct reason families.
    [Fact]
    public void BlockedConditions_DistinctReasonFamilies()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("VALIDATED_LOAD_UNAVAILABLE", text);
        Assert.Contains("VALIDATED_LONG_RUN_EVIDENCE_UNAVAILABLE", text);
        Assert.Contains("NUMERIC_WINDOW_INFEASIBLE", text);
        Assert.Contains("SAFETY_REASSESSMENT_REQUIRED", text);
    }

    // 26. rounding explicit.
    [Fact]
    public void Rounding_ReusesExistingIncrement()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("0.5km increment", text);
    }

    // 27. both profiles use approved numeric policy.
    [Fact]
    public void BothProfiles_UseIdenticalFormula()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("identical validated-load formula", text);
    }

    // 28. maintenance distinct from recovery and Taper.
    [Fact]
    public void Maintenance_DistinctFromRecoveryAndTaper()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("maintenance is not Taper; maintenance is not GE recovery", text);
    }

    // 29. detailed checkpoint thresholds remain deferred to Phase 4K.3.
    [Fact]
    public void DetailedThresholds_DeferredToPhase4K3()
    {
        var risk = Risk(DecisionId);
        var resolution = risk.GetProperty("requiredResolution").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains(resolution, r => r!.Contains("Phase 4K.3"));
    }

    // 30. zero production code changes.
    [Fact]
    public void ZeroProductionCodeChanges()
    {
        var risk = Risk(DecisionId);
        Assert.Contains("Zero production code was changed", risk.GetProperty("currentRuntimeImpact").GetString());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    [Fact]
    public void PathADecision_UpdatedToReflectApprovedFormula()
    {
        var text = Risk(PathADecisionId).GetRawText();
        Assert.Contains("Phase 4K.2", text);
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
