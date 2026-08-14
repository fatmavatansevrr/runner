using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4K.8C — governance cross-check for
/// <c>TD-LONG-HORIZON-JIT-REAL-CORE-CONDITION-CALENDAR-COMPOSITION-001</c> (CLOSED)
/// and the append-only updates to the three TDs it references.
/// </summary>
public sealed class LongHorizonJitRealCoreConditionCalendarCompositionGovernanceTests
{
    private const string CompositionTd = "TD-LONG-HORIZON-JIT-REAL-CORE-CONDITION-CALENDAR-COMPOSITION-001";
    private const string JitContextTd = "TD-LONG-HORIZON-RUNWAY-CORE-JIT-CONTEXT-001";
    private const string ContractsTd = "TD-LONG-HORIZON-RUNWAY-BOUNDED-PRESCRIPTION-CONTRACTS-001";
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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4K_8C_REAL_CORE_TARGET_RUNTIME_CONDITION_AND_SESSION_CALENDAR_COMPOSITION_ADAPTER.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void CompositionDecision_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(CompositionTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[]
        {
            "orchestratorEntryPoint", "conditionResolutionAuthority", "rollingCoreInputMapping",
            "coreGeneratorInvocation", "coreWeekOneTargetExtraction", "boundedCoreSelection",
            "runwayCalendarAuthority", "coreContextVersionAndRefresh", "phase4K8RuntimeIntegration",
            "atomicity", "versioning", "darkIntegration", "persistencePublicStatus", "tests",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void CompositionDecision_ConditionResolutionUsesRealProductionResolvers()
    {
        var text = Risk(CompositionTd).GetRawText();
        Assert.Contains("TimeAdequacyResolver/PaceSourceResolver/CoreEntryReadinessResolver/GoalFeasibilityResolver", text);
        Assert.Contains("real CoreCycle attached", text);
    }

    [Fact]
    public void CompositionDecision_CoreGeneratorIsRealUnmodifiedProductionFactory()
    {
        var text = Risk(CompositionTd).GetRawText();
        Assert.Contains("TenKPreparationRunwayDarkOrchestratorFactory.Create", text);
        Assert.Contains("the same real, unmodified production factory backend/RunningApp.Application already uses", text);
    }

    [Fact]
    public void CompositionDecision_CoreGenerationSkippedForPureContinuationWindows()
    {
        var text = Risk(CompositionTd).GetRawText();
        Assert.Contains("never rerun for a pure mid-Runway continuation window", text);
    }

    [Fact]
    public void CompositionDecision_Phase4K8AuthoritiesNeverDuplicated()
    {
        var text = Risk(CompositionTd).GetRawText();
        Assert.Contains("this orchestrator never duplicates window selection, mixed-window atomicity, lifecycle transitions, the direction guard, target-lock validation, or bounded Runway slice selection", text);
    }

    [Fact]
    public void CompositionDecision_CalendarScopeGapHonestlyDisclosed()
    {
        var text = Risk(CompositionTd).GetRawText();
        Assert.Contains("real per-session dates ARE computed and validated as part of composition", text);
        Assert.Contains("this phase is forbidden from redesigning", text);
    }

    [Fact]
    public void CompositionDecision_AtomicityEnforcedBySharedTryCatch()
    {
        var text = Risk(CompositionTd).GetRawText();
        Assert.Contains("One shared try/catch wraps the entire composition-plus-activation call", text);
        Assert.Contains("no exposed RealCompositionResult", text);
    }

    [Fact]
    public void CompositionDecision_TestCountRecorded()
    {
        var text = Risk(CompositionTd).GetRawText();
        Assert.Contains("18 new focused tests", text);
    }

    [Fact]
    public void CompositionDecision_ZeroPublicPersistenceWiring()
    {
        var risk = Risk(CompositionTd);
        Assert.Contains("Zero controller, DI registration, database entity/migration, or Flutter file touched", risk.GetRawText());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    [Fact]
    public void JitContextTd_CarriesPhase4K8CUpdate()
    {
        var text = Risk(JitContextTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4K.8C, 2026-08-03)", text);
        Assert.Equal("CLOSED", Risk(JitContextTd).GetProperty("status").GetString());
    }

    [Fact]
    public void ContractsTd_CarriesPhase4K8CUpdate()
    {
        var text = Risk(ContractsTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4K.8C, 2026-08-03)", text);
        Assert.Equal("CLOSED", Risk(ContractsTd).GetProperty("status").GetString());
    }

    [Fact]
    public void Redesign_RemainsOpenAndReferencesPhase4K8C()
    {
        var risk = Risk(RedesignTd);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        var text = risk.GetRawText();
        Assert.Contains("UPDATE (Phase 4K.8C):", text);
        Assert.Contains("Phase 4K.9", text);
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
        Assert.Contains("LONG_HORIZON_REAL_CORE_TARGET_RUNTIME_CONDITION_AND_SESSION_CALENDAR_COMPOSITION_COMPLETED_DARK", text);
        Assert.Contains("LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_API_AND_FLUTTER_REMAIN_UNCHANGED", text);
    }
}
