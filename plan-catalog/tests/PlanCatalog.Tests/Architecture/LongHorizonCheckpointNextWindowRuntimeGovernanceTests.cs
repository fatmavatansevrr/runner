using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

public sealed class LongHorizonCheckpointNextWindowRuntimeGovernanceTests
{
    private const string RuntimeTd = "TD-LONG-HORIZON-CHECKPOINT-NEXT-WINDOW-RUNTIME-001";
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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4K_7_CHECKPOINT_EVIDENCE_AGGREGATION_STATE_EVALUATION_AND_NEXT_WINDOW_ACTIVATION_RUNTIME.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(risk => risk.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void RuntimeTd_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(RuntimeTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[]
        {
            "runtimeEntryPoint", "trainingDayAggregation", "evidenceWindow", "terminalWindowImplementation",
            "validatedWeeklyLoadCalculation", "validatedLongRunCalculation", "adherenceConfidence", "safetyPriority",
            "transitionTableImplementation", "priorAnchorBehavior", "nextWindowSelection", "growthExecution",
            "maintenanceExecution", "recoveryBehavior", "boundaryBehavior", "atomicity", "contextVersioning",
            "darkIntegration", "persistenceStatus", "publicActivationStatus", "runwayCoreStatus", "tests",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void RegistryAndMarkdown_AreSemanticallyAlignedAndUnique()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risks = document.RootElement.GetProperty("risks").EnumerateArray().ToArray();
        Assert.NotEmpty(risks);
        Assert.Equal(
            risks.Length,
            risks.Count(risk => risk.GetProperty("status").GetString() == "OPEN")
            + risks.Count(risk => risk.GetProperty("status").GetString() == "CLOSED"));
        Assert.Equal(risks.Length, risks.Select(risk => risk.GetProperty("id").GetString()).Distinct().Count());
        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains(RuntimeTd, markdown);
    }

    [Fact]
    public void Redesign_RemainsOpenWithExplicitRemainingWork()
    {
        var risk = Risk(RedesignTd);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        Assert.Contains("Phase 4K.7", risk.GetRawText());
        Assert.Contains("Runway/Core JIT", risk.GetRawText());
        Assert.Contains("persistence", risk.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Flutter", risk.GetRawText());
    }

    [Fact]
    public void DecisionDocument_HasExactlyThirtyTwoRequiredSectionsAndClassifications()
    {
        var text = File.ReadAllText(DecisionPath());
        var headings = File.ReadAllLines(DecisionPath()).Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        Assert.Equal(32, headings.Length);
        Assert.Equal("## 1. Executive result", headings[0]);
        Assert.Equal("## 32. Exact next phase", headings[^1]);
        Assert.Contains("LONG_HORIZON_CHECKPOINT_EVIDENCE_AGGREGATION_STATE_EVALUATION_AND_NEXT_GE_WINDOW_RUNTIME_COMPLETED_DARK", text);
        Assert.Contains("LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_AND_RUNWAY_CORE_JIT_RUNTIME_REMAIN_UNCHANGED", text);
    }
}
