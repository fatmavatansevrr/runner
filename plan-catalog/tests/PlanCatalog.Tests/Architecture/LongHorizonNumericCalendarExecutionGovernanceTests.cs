using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4I.6 — governance cross-check for
/// <c>TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001</c>, mirroring the
/// established pattern for the prior Long-Horizon TDs. Unlike its
/// predecessors, this entry is recorded OPEN (a deliberate, honestly
/// disclosed partial closure) -- these tests assert that status is
/// accurately reflected, not that the entry is closed.
/// </summary>
public sealed class LongHorizonNumericCalendarExecutionGovernanceTests
{
    private const string DecisionId = "TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    [Fact]
    public void CanonicalJson_RecordsTheDecisionAsOpen()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);

        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        Assert.False(risk.TryGetProperty("closureNote", out _), "An OPEN entry must not carry a closureNote.");
    }

    [Fact]
    public void CanonicalJson_DoesNotClaimFullRunwayOrCoreNumericExecution()
    {
        // Updated Phase 4I.6A: Runway/Core numeric execution is now real for
        // short-GE horizons, but the entry must still disclose the remaining
        // existing-pipeline gap for longer GE horizons -- it must not claim
        // unconditional/complete execution.
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);
        var rawText = risk.GetRawText();
        Assert.Contains("existing-pipeline compatibility gap", rawText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RUNWAY_NUMERIC_EXECUTION_COMPLETE", rawText);
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
    public void MarkdownProjection_ContainsTheDecisionRowAndOpenStatus()
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
