using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4I.4 — governance cross-check for
/// <c>TD-LONG-HORIZON-GE-CATALOG-CAPACITY-001</c>, mirroring the pattern
/// established for TD-LONG-HORIZON-COMPOSITION-001/TD-LONG-HORIZON-GE-SAFETY-001/
/// TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001/TD-LONG-HORIZON-TYPED-RUNTIME-DECISION-001.
/// </summary>
public sealed class LongHorizonGeCatalogCapacityGovernanceTests
{
    private const string DecisionId = "TD-LONG-HORIZON-GE-CATALOG-CAPACITY-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    [Fact]
    public void CanonicalJson_RecordsTheDecisionAsClosed()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);

        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        var rawText = risk.GetRawText();
        Assert.Contains("LONG_HORIZON_GENERAL_ENDURANCE_CATALOG_AND_SCHEMA_CAPACITY_COMPLETED_FOR_1_TO_32_WEEKS", rawText);
        Assert.Contains(DeferredDependencyId, rawText);
    }

    [Fact]
    public void CanonicalJson_DoesNotClaimRuntimeActivation()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);
        Assert.Contains("None", risk.GetProperty("currentRuntimeImpact").GetString()!);
    }

    [Fact]
    public void DeferredStagedPlanBlocker_RemainsOpen()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var deferred = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DeferredDependencyId);
        Assert.Equal("OPEN", deferred.GetProperty("status").GetString());
    }

    [Fact]
    public void MarkdownProjection_ContainsTheDecisionRowAndClosedStatus()
    {
        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains($"`{DecisionId}`", markdown);
        var section = markdown.Substring(markdown.IndexOf(DecisionId, StringComparison.Ordinal));
        Assert.Contains("**CLOSED**", section);
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
