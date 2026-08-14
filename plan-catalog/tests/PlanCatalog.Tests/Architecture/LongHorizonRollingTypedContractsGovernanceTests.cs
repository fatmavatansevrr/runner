using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4K.5 — governance cross-check for
/// <c>TD-LONG-HORIZON-ROLLING-TYPED-CONTRACTS-001</c> (CLOSED) and
/// <c>TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001</c> (closed by Phase 4K.5A).
/// </summary>
public sealed class LongHorizonRollingTypedContractsGovernanceTests
{
    private const string ContractsDecisionId = "TD-LONG-HORIZON-ROLLING-TYPED-CONTRACTS-001";
    private const string CoreAuthorityDecisionId = "TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001";
    private const string PathADecisionId = "TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void ContractsDecision_RecordedAsClosed()
    {
        Assert.Equal("CLOSED", Risk(ContractsDecisionId).GetProperty("status").GetString());
    }

    [Fact]
    public void CoreAuthorityDecision_RecordedAsClosedAndNonBlocking()
    {
        var risk = Risk(CoreAuthorityDecisionId);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
        Assert.True(risk.TryGetProperty("closureNote", out var closure));
        Assert.Contains("Phase 4K.5A", closure.GetString());
    }

    [Fact]
    public void CoreAuthorityDecision_ResolvedByExistingPipelineEvidence_NotAssumption()
    {
        var text = Risk(CoreAuthorityDecisionId).GetRawText();
        Assert.Contains("OPTION A", text);
        Assert.Contains("ValidatedSustainableLoad", text);
        Assert.Contains("existing unchanged Core generator input boundary", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OriginalOnboardingEvidence", text);
        Assert.Contains("ProvenanceOnly", text);
    }

    [Fact]
    public void ContractsDecision_AllElevenTypedContractAreasPresent()
    {
        var text = Risk(ContractsDecisionId).GetRawText();
        Assert.Contains("(1) STRUCTURAL ROADMAP", text);
        Assert.Contains("(2) NUMERIC LIFECYCLE", text);
        Assert.Contains("(3) ACTIVATION WINDOW", text);
        Assert.Contains("(4) NUMERIC WEEK", text);
        Assert.Contains("(5) CHECKPOINT EVIDENCE SNAPSHOT", text);
        Assert.Contains("(6) VALIDATED LOAD", text);
        Assert.Contains("(7) CHECKPOINT DECISION", text);
        Assert.Contains("(8) CHECKPOINT REASON ENUM", text);
        Assert.Contains("(9) EVIDENCE AUTHORITY", text);
        Assert.Contains("(10) JIT CONTEXT", text);
        Assert.Contains("(11) JIT REASON ENUM", text);
        Assert.Contains("(12) CORE TARGET LOCK", text);
        Assert.Contains("(13) CONTEXT VERSIONING", text);
        Assert.Contains("(14) INITIAL ACTIVATION", text);
    }

    [Fact]
    public void ContractsDecision_EvidenceAuthorityGuard_RejectsSilentOnboardingDefaulting()
    {
        var text = Risk(ContractsDecisionId).GetRawText();
        Assert.Contains("THROWS LongHorizonEvidenceAuthorityDefaultingException if any caller attempts to mark OriginalOnboardingEvidence as Authoritative", text);
        Assert.Contains("Core Week-1's ROLLING authority", text);
    }

    [Fact]
    public void ContractsDecision_SafetyReasonSharedNotDuplicated()
    {
        var text = Risk(ContractsDecisionId).GetRawText();
        Assert.Contains("deliberately WITHOUT a duplicate safety value", text);
        Assert.Contains("LongHorizonReasonCode.SafetyReassessmentRequired as the single shared instance both checkpoint-Blocked and JIT-Blocked results reuse directly", text);
    }

    [Fact]
    public void ContractsDecision_LockedTargetImmutabilityEnforced()
    {
        var text = Risk(ContractsDecisionId).GetRawText();
        Assert.Contains("THROWS LongHorizonLockedTargetImmutabilityViolationException if a refreshed target's locked week range overlaps the prior target's already-locked range", text);
    }

    [Fact]
    public void ContractsDecision_ZeroProductionWiring()
    {
        var risk = Risk(ContractsDecisionId);
        var text = risk.GetRawText();
        Assert.Contains("Every new type is internal, unreferenced by any controller/DI registration/existing orchestrator", text);
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    [Fact]
    public void ContractsDecision_TestAndSuiteCountsRecorded()
    {
        var text = Risk(ContractsDecisionId).GetRawText();
        Assert.Contains("52 new backend tests", text);
    }

    [Fact]
    public void ContractsDecision_DoesNotImplementRollingGeneration()
    {
        var risk = Risk(ContractsDecisionId);
        var resolution = risk.GetProperty("requiredResolution").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains(resolution, r => r!.Contains("Rolling numeric generation, checkpoint aggregation from persisted TrainingDay rows, rolling runtime wiring, persistence, public preview activation, and Flutter: all OPEN"));
        Assert.Contains(resolution, r => r!.Contains(CoreAuthorityDecisionId));
    }

    [Fact]
    public void PathADecision_UpdatedButNotClosed()
    {
        var risk = Risk(PathADecisionId);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        var text = risk.GetRawText();
        Assert.Contains("Phase 4K.5", text);
        Assert.Contains("This does NOT close this redesign TD", text);
    }

    [Fact]
    public void DeferredStagedPlanBlocker_RemainsOpen()
    {
        Assert.Equal("OPEN", Risk(DeferredDependencyId).GetProperty("status").GetString());
    }

    [Fact]
    public void MarkdownProjection_ContainsBothDecisionRowsWithCorrectStatus()
    {
        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains($"`{ContractsDecisionId}`", markdown);
        Assert.Contains($"`{CoreAuthorityDecisionId}`", markdown);

        var contractsSection = markdown.Substring(markdown.IndexOf(ContractsDecisionId, StringComparison.Ordinal));
        Assert.Contains("**CLOSED**", contractsSection[..contractsSection.IndexOf(CoreAuthorityDecisionId, StringComparison.Ordinal)]);

        var authoritySection = markdown.Substring(markdown.IndexOf(CoreAuthorityDecisionId, StringComparison.Ordinal));
        Assert.Contains("**CLOSED**", authoritySection[..authoritySection.IndexOf("TD-AEROBIC-STRENGTH-SEMANTICS-001", StringComparison.Ordinal)]);
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
