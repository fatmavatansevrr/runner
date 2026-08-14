using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4I.6B.1 — governance cross-check for
/// <c>TD-LONG-HORIZON-NUMERIC-ACTIVATION-BOUNDARY-001</c> (CLOSED, a real
/// deterministic boundary was found) and
/// <c>TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001</c> (OPEN/DEFERRED, the
/// Path A placeholder).
/// </summary>
public sealed class LongHorizonNumericActivationBoundaryGovernanceTests
{
    private const string BoundaryDecisionId = "TD-LONG-HORIZON-NUMERIC-ACTIVATION-BOUNDARY-001";
    private const string PathADecisionId = "TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    [Fact]
    public void CanonicalJson_BoundaryDecisionRecordedAsClosed()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == BoundaryDecisionId);

        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        var rawText = risk.GetRawText();
        Assert.Contains("MaxNumericallySupportedTotalWeeks = 21", rawText);
    }

    [Fact]
    public void CanonicalJson_DoesNotClaimCompositionWindowChanged()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == BoundaryDecisionId);
        var rawText = risk.GetRawText();
        Assert.Contains("preserved and unchanged", rawText);
        Assert.DoesNotContain("PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW applies to 22", rawText);
    }

    [Fact]
    public void CanonicalJson_PathADecisionRecordedAsOpenDeferred()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == PathADecisionId);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        Assert.Equal("FUTURE_REDESIGN_DEFERRED", risk.GetProperty("classification").GetString());
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
    public void MarkdownProjection_ContainsBothRows()
    {
        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains($"`{BoundaryDecisionId}`", markdown);
        Assert.Contains($"`{PathADecisionId}`", markdown);
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
