using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4L.2B -- governance cross-check for
/// <c>TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001</c>
/// (kept OPEN by its own explicit governance instruction) and the
/// append-only updates to the TDs it references.
/// </summary>
public sealed class LongHorizonMixedCoreRefreshPostgresCompletionMatrixGovernanceTests
{
    private const string MatrixTd = "TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001";
    private const string PriorMatrixTd = "TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001";
    private const string PersistenceTd = "TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001";
    private const string PreviewTd = "TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001";
    private const string LifecycleTd = "TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001";
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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4L_2B_MIXED_WINDOW_CORE_REFRESH_FAILURE_INJECTION_AND_CONCURRENCY_COMPLETION_MATRIX.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void MatrixDecision_IsClosedByPhase4L2GAndRetainsEveryHistoricalField()
    {
        var risk = Risk(MatrixTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        Assert.Contains("Phase 4L.2G", risk.GetProperty("phase4L2GUpdate").GetString());
        foreach (var field in new[]
        {
            "exactScope", "geToRunwayRestart", "runwayToCoreRestart", "coreOnlyRestart", "futureCoreRefreshRestart",
            "failureInjectionCoverage", "commitAcknowledgementAmbiguity", "concurrencyMatrix", "idempotencyMatrix",
            "constraintReview", "coreContextPerPlanRegression", "jsonbFullFidelityRegression", "noRegenerationProof",
            "corruptionCompletion", "terminalCompletionRestart", "darkIntegration", "tests",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void MatrixDecision_DiscloseRealContinuationDefectNotFixed()
    {
        var text = Risk(MatrixTd).GetProperty("coreOnlyRestart").GetString()!;
        Assert.Contains("does not appear to advance the activation window", text);
        Assert.Contains("Phase 4L.2A's own continuation test only asserted prescription/target-lock identity reuse", text);
    }

    [Fact]
    public void MatrixDecision_MixedWindowAttemptedAndRemoved()
    {
        var text = Risk(MatrixTd).GetProperty("geToRunwayRestart").GetString()!;
        Assert.Contains("did not activate as expected", text);
        Assert.Contains("removed rather than left failing or fabricated as passing", text);
    }

    [Fact]
    public void MatrixDecision_NoProductionCodeChanged()
    {
        var text = Risk(MatrixTd).GetProperty("darkIntegration").GetString()!;
        Assert.Contains("no production code was modified this phase", text);
    }

    [Fact]
    public void MatrixDecision_ExistingSuiteStillPasses()
    {
        var text = Risk(MatrixTd).GetProperty("tests").GetString()!;
        Assert.Contains("46/46", text);
    }

    [Fact]
    public void MatrixDecision_NotBlockingAndNowMarkedComplete()
    {
        var risk = Risk(MatrixTd);
        Assert.False(risk.GetProperty("blocking").GetBoolean());
        Assert.Contains("COMPLETION_MATRIX_CLOSED", risk.GetProperty("classification").GetString());
    }

    [Fact]
    public void PriorMatrixTd_CarriesPhase4L2BUpdate()
    {
        var text = Risk(PriorMatrixTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2B, 2026-08-03)", text);
        Assert.Equal("CLOSED", Risk(PriorMatrixTd).GetProperty("status").GetString());
    }

    [Fact]
    public void PersistenceTd_CarriesPhase4L2BUpdate()
    {
        var text = Risk(PersistenceTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2B, 2026-08-03)", text);
    }

    [Fact]
    public void PreviewTd_CarriesPhase4L2BUpdate()
    {
        var text = Risk(PreviewTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2B, 2026-08-03)", text);
    }

    [Fact]
    public void LifecycleTd_CarriesPhase4L2BUpdate()
    {
        var text = Risk(LifecycleTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2B, 2026-08-03)", text);
    }

    [Fact]
    public void Redesign_RemainsOpenAndReferencesPhase4L2B()
    {
        var risk = Risk(RedesignTd);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        var text = risk.GetRawText();
        Assert.Contains("UPDATE (Phase 4L.2B):", text);
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
        Assert.Contains("LONG_HORIZON_POSTGRESQL_COMPLETION_MATRIX_REMAINS_BLOCKED", text);
    }
}
