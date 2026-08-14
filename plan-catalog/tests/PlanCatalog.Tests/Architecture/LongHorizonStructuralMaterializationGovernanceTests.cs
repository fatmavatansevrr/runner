using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4I.5 — governance cross-check for
/// <c>TD-LONG-HORIZON-STRUCTURAL-MATERIALIZATION-001</c>, mirroring the
/// pattern established for TD-LONG-HORIZON-COMPOSITION-001/
/// TD-LONG-HORIZON-GE-SAFETY-001/TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001/
/// TD-LONG-HORIZON-TYPED-RUNTIME-DECISION-001/TD-LONG-HORIZON-GE-CATALOG-CAPACITY-001.
/// </summary>
public sealed class LongHorizonStructuralMaterializationGovernanceTests
{
    private const string DecisionId = "TD-LONG-HORIZON-STRUCTURAL-MATERIALIZATION-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    [Fact]
    public void CanonicalJson_RecordsTheDecisionAsClosed()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);

        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        var rawText = risk.GetRawText();
        Assert.Contains("LONG_HORIZON_21_TO_52_WEEK_DARK_STRUCTURAL_MATERIALIZATION_COMPLETED", rawText);
        Assert.Contains(DeferredDependencyId, rawText);
    }

    [Fact]
    public void CanonicalJson_DoesNotClaimNumericOrPublicActivation()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);
        var runtimeImpact = risk.GetProperty("currentRuntimeImpact").GetString()!;
        Assert.Contains("None", runtimeImpact);
        Assert.DoesNotContain("PUBLIC_PREVIEW_ACTIVATED", risk.GetRawText());
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
