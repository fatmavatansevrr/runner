using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4K.8B — governance cross-check for
/// <c>TD-LONG-HORIZON-RUNWAY-BOUNDED-PRESCRIPTION-CONTRACTS-001</c> (CLOSED)
/// and the append-only updates to the two Phase 4K.8A authority TDs.
/// </summary>
public sealed class LongHorizonRunwayBoundedPrescriptionContractsGovernanceTests
{
    private const string ContractsTd = "TD-LONG-HORIZON-RUNWAY-BOUNDED-PRESCRIPTION-CONTRACTS-001";
    private const string DirectionTd = "TD-LONG-HORIZON-RUNWAY-DOWNWARD-CONSOLIDATION-AUTHORITY-001";
    private const string BoundedTd = "TD-LONG-HORIZON-BOUNDED-RUNWAY-MATERIALIZATION-001";
    private const string RedesignTd = "TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001";

    private static string PlanRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PlanCatalog.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static string RepoRoot() => Directory.GetParent(PlanRoot())!.FullName;
    private static string JsonPath() => Path.Combine(PlanRoot(), "artifacts", "audits", "activation-readiness-risks.json");
    private static string MarkdownPath() => Path.Combine(PlanRoot(), "artifacts", "audits", "activation-readiness-risks.md");
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4K_8B_PREPARATION_RUNWAY_DIRECTION_GUARD_AND_BOUNDED_PRESCRIPTION_CONTRACT_IMPLEMENTATION.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(risk => risk.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void ContractsDecision_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(ContractsTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[]
        {
            "directionRelationContract", "directionGuard", "fullPrescriptionContract", "weekReferenceContract",
            "targetLockScope", "boundedSliceContract", "exactEquivalenceValidator", "terminalStageInvariant",
            "refreshGuard", "internalPendingSafeguards", "deterministicIdentity", "validatorOrder",
            "integrationStatus", "phase4K8RuntimeStatus", "tests",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void ContractsDecision_DirectionGuardMapsToExistingJitReason_NoNewReason()
    {
        var text = Risk(ContractsTd).GetRawText();
        Assert.Contains("JIT_SEGMENT_TRANSITION_INFEASIBLE", text);
        Assert.Contains("never a new public reason", text);
    }

    [Fact]
    public void ContractsDecision_WeekReferenceReusesRealProductionObject_NeverRecomputes()
    {
        var text = Risk(ContractsTd).GetRawText();
        Assert.Contains("IS the original PreparationRunwayPrescribedWeek<TKey> instance", text);
        Assert.Contains("never recomputed or duplicated", text);
    }

    [Fact]
    public void ContractsDecision_DeterministicIdentityReusesExistingConvention_NoRandomGuid()
    {
        var text = Risk(ContractsTd).GetRawText();
        Assert.Contains("reuses the exact SHA-256 convention already used by LongHorizonRollingCheckpointRuntime", text);
        Assert.Contains("no random GUID, no wall clock", text);
    }

    [Fact]
    public void ContractsDecision_SliceFactoryStructurallyCannotInvokeMaterializer()
    {
        var text = Risk(ContractsTd).GetRawText();
        Assert.Contains("never invokes the numeric materializer (its signature structurally cannot", text);
    }

    [Fact]
    public void ContractsDecision_MidRunwayRefreshForbidden()
    {
        var text = Risk(ContractsTd).GetRawText();
        Assert.Contains("mid-Runway refresh forbidden", text);
        Assert.Contains("strictly later ContextVersion is required", text);
    }

    [Fact]
    public void ContractsDecision_ZeroFormulaChange_JitRuntimeStillAbsent()
    {
        var risk = Risk(ContractsTd);
        var text = risk.GetRawText();
        Assert.Contains("zero numeric-formula change", text);
        Assert.Contains("Still entirely absent", text);
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    [Fact]
    public void ContractsDecision_TestCountRecorded()
    {
        var text = Risk(ContractsTd).GetRawText();
        Assert.Contains("70 new production-contract tests", text);
    }

    [Fact]
    public void DirectionAuthority_CarriesPhase4K8BAddendum()
    {
        var text = Risk(DirectionTd).GetRawText();
        Assert.Contains("UPDATE (Phase 4K.8B, append-only)", text);
        Assert.Equal("CLOSED", Risk(DirectionTd).GetProperty("status").GetString());
    }

    [Fact]
    public void BoundedAuthority_CarriesPhase4K8BAddendum()
    {
        var text = Risk(BoundedTd).GetRawText();
        Assert.Contains("UPDATE (Phase 4K.8B, append-only)", text);
        Assert.Equal("CLOSED", Risk(BoundedTd).GetProperty("status").GetString());
    }

    [Fact]
    public void Redesign_RemainsOpenAndReferencesPhase4K8B()
    {
        var risk = Risk(RedesignTd);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        var text = risk.GetRawText();
        Assert.Contains("UPDATE (Phase 4K.8B)", text);
        Assert.Contains("JIT orchestration/runtime", text);
    }

    [Fact]
    public void RegistryAndMarkdown_AreUniqueAndSemanticallyAligned()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risks = document.RootElement.GetProperty("risks").EnumerateArray().ToArray();
        Assert.NotEmpty(risks);
        Assert.Equal(
            risks.Length,
            risks.Count(r => r.GetProperty("status").GetString() == "OPEN")
            + risks.Count(r => r.GetProperty("status").GetString() == "CLOSED"));
        Assert.Equal(risks.Length, risks.Select(r => r.GetProperty("id").GetString()).Distinct().Count());

        var markdown = File.ReadAllText(MarkdownPath());
        foreach (var risk in risks)
            Assert.Equal(1, markdown.Split('\n').Count(line => line.StartsWith($"| `{risk.GetProperty("id").GetString()}`", StringComparison.Ordinal)));
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

    [Fact]
    public void DecisionDocument_Exists_AndCarriesFinalClassifications()
    {
        var text = File.ReadAllText(DecisionPath());
        var headings = File.ReadAllLines(DecisionPath()).Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        Assert.Equal("## 1. Executive result", headings[0]);
        Assert.Contains("PREPARATION_RUNWAY_DIRECTION_GUARDS_AND_BOUNDED_PRESCRIPTION_CONTRACTS_COMPLETED_DARK", text);
        Assert.Contains("PHASE4K_8_JIT_RUNTIME_REMAINS_UNIMPLEMENTED", text);
    }
}
