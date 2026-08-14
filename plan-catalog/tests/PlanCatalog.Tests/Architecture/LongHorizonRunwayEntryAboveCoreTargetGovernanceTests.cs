using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4I.6B — governance cross-check for
/// <c>TD-LONG-HORIZON-RUNWAY-ENTRY-ABOVE-CORE-TARGET-001</c>. Remains OPEN
/// (structural decisions resolved; numeric magnitude explicitly unresolved,
/// not invented) -- these tests assert that status is accurately reflected.
/// </summary>
public sealed class LongHorizonRunwayEntryAboveCoreTargetGovernanceTests
{
    private const string DecisionId = "TD-LONG-HORIZON-RUNWAY-ENTRY-ABOVE-CORE-TARGET-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    [Fact]
    public void CanonicalJson_RecordedAsOpen()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);

        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        Assert.False(risk.TryGetProperty("closureNote", out _), "An OPEN entry must not carry a closureNote.");
    }

    [Fact]
    public void CanonicalJson_DoesNotInventANumericConvergenceValue()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);
        var rawText = risk.GetRawText();

        Assert.Contains("REJECTED", rawText);
        Assert.Contains("not invented", rawText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LONG_HORIZON_RUNWAY_ENTRY_ABOVE_CORE_TARGET_NUMERIC_TRANSITION_POLICY_APPROVED", rawText);
    }

    [Fact]
    public void CanonicalJson_RecordsRealEvidenceMagnitude()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);
        var rawText = risk.GetRawText();

        Assert.Contains("28 weeks", rawText);
        Assert.Contains("52 weeks", rawText);
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
    public void MarkdownProjection_ContainsTheDecisionRowWithOpenStatus()
    {
        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains($"`{DecisionId}`", markdown);
        var section = markdown.Substring(markdown.IndexOf(DecisionId, StringComparison.Ordinal));
        Assert.Contains("**OPEN**", section);
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
