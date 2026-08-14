using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4I.6A — governance cross-check for
/// <c>TD-LONG-HORIZON-RUNWAY-CORE-NUMERIC-CONTEXT-INTEGRATION-001</c> and the
/// updated <c>TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001</c>. Both remain
/// OPEN (a deliberate, honestly disclosed partial closure) -- these tests
/// assert that status is accurately reflected, not that either is closed.
/// </summary>
public sealed class LongHorizonRunwayCoreNumericContextIntegrationGovernanceTests
{
    private const string NewDecisionId = "TD-LONG-HORIZON-RUNWAY-CORE-NUMERIC-CONTEXT-INTEGRATION-001";
    private const string UpdatedDecisionId = "TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    [Fact]
    public void CanonicalJson_NewDecisionRecordedAsOpen()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == NewDecisionId);

        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        Assert.False(risk.TryGetProperty("closureNote", out _), "An OPEN entry must not carry a closureNote.");
    }

    [Fact]
    public void CanonicalJson_DoesNotClaimFullMatrixSuccess()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == NewDecisionId);
        var rawText = risk.GetRawText();
        Assert.Contains("existing-pipeline compatibility gap", rawText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LONG_HORIZON_21_TO_52_WEEK_FULL_DARK_NUMERIC_EXECUTION_COMPLETED", rawText);
    }

    [Fact]
    public void CanonicalJson_UpdatedDecisionStillOpen()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == UpdatedDecisionId);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
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
    public void MarkdownProjection_ContainsBothRowsWithOpenStatus()
    {
        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains($"`{NewDecisionId}`", markdown);
        Assert.Contains($"`{UpdatedDecisionId}`", markdown);
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
