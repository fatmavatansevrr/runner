using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4L.2C -- governance cross-check for
/// <c>TD-LONG-HORIZON-RUNWAY-CONTINUATION-WINDOW-ADVANCEMENT-001</c> (CLOSED)
/// and the append-only updates to the TDs it references.
/// </summary>
public sealed class LongHorizonRunwayContinuationWindowAdvancementGovernanceTests
{
    private const string AdvancementTd = "TD-LONG-HORIZON-RUNWAY-CONTINUATION-WINDOW-ADVANCEMENT-001";
    private const string MatrixTd = "TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001";
    private const string PriorMatrixTd = "TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001";
    private const string PersistenceTd = "TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001";
    private const string PreviewTd = "TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001";

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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4L_2C_RUNWAY_CONTINUATION_WINDOW_ADVANCEMENT_ROOT_CAUSE_RESOLUTION.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void AdvancementTd_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(AdvancementTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[]
        {
            "rootCause", "reproduction", "fix", "advancementProof", "advancementValidator",
            "geToRunwayReDiagnosis", "replayAndCorruption", "regressionScope", "darkIntegration", "tests",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void AdvancementTd_RootCauseNamesExactValueTupleDefect()
    {
        var text = Risk(AdvancementTd).GetProperty("rootCause").GetString()!;
        Assert.Contains("ValueTuple", text);
        Assert.Contains("PreparationRunwayTargetLockScopeViolationException", text);
    }

    [Fact]
    public void AdvancementTd_FixIsMinimalJsonOptionsChange()
    {
        var text = Risk(AdvancementTd).GetProperty("fix").GetString()!;
        Assert.Contains("IncludeFields", text);
        Assert.Contains("No numeric, calendar, direction, evidence, target-lock, or slice formula was changed", text);
    }

    [Fact]
    public void AdvancementTd_DoesNotClaimMixedWindowFixed()
    {
        var text = Risk(AdvancementTd).GetProperty("geToRunwayReDiagnosis").GetString()!;
        Assert.Contains("NOT resolved by this fix", text);
        Assert.Contains("IndependentDefectRemains", text);
    }

    [Fact]
    public void MatrixTd_RemainsOpenAfterAdvancementFix()
    {
        Assert.Equal("CLOSED", Risk(MatrixTd).GetProperty("status").GetString());
        var text = Risk(MatrixTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2C, 2026-08-04)", text);
    }

    [Fact]
    public void PriorMatrixTd_CarriesPhase4L2CUpdate()
    {
        var text = Risk(PriorMatrixTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2C, 2026-08-04)", text);
        Assert.Equal("CLOSED", Risk(PriorMatrixTd).GetProperty("status").GetString());
    }

    [Fact]
    public void PersistenceTd_CarriesPhase4L2CUpdate()
    {
        var text = Risk(PersistenceTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2C, 2026-08-04)", text);
    }

    [Fact]
    public void PreviewTd_CarriesPhase4L2CUpdate()
    {
        var text = Risk(PreviewTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2C, 2026-08-04)", text);
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
        Assert.Contains("LONG_HORIZON_RUNWAY_CONTINUATION_WINDOW_ADVANCEMENT_ROOT_CAUSE_RESOLVED", text);
    }
}
