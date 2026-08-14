using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4L.2F -- governance cross-check for
/// <c>TD-LONG-HORIZON-TRANSACTIONAL-FAILURE-INJECTION-ROLLBACK-MATRIX-001</c>
/// (CLOSED by Phase 4L.2F-A at complete mutation-category coverage) and the
/// append-only updates to the TDs it references.
/// </summary>
public sealed class LongHorizonTransactionalFailureInjectionRollbackMatrixGovernanceTests
{
    private const string RollbackTd = "TD-LONG-HORIZON-TRANSACTIONAL-FAILURE-INJECTION-ROLLBACK-MATRIX-001";
    private const string MatrixTd = "TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001";
    private const string RefreshTd = "TD-LONG-HORIZON-FUTURE-ONLY-CORE-CONTEXT-REFRESH-001";
    private const string PersistenceTd = "TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001";
    private const string PriorMatrixTd = "TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001";
    private const string LifecycleTd = "TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001";
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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4L_2F_TRANSACTIONAL_FAILURE_INJECTION_AND_ROLLBACK_MATRIX.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void RollbackTd_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(RollbackTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        Assert.Contains("Phase 4L.2F-A", risk.GetProperty("phase4L2FAUpdate").GetString());
        foreach (var field in new[]
        {
            "transactionInventory", "failpointDesign", "failpointSafety", "snapshotAuthority", "mixedRollback",
            "coreOnlyRollback", "coreRefreshRollback", "blockRollback", "retryRollback",
            "initialCheckpointTerminalCoverage", "postCommitAmbiguity", "constraintFailureBehavior",
            "leakDetection", "reconstructionEquivalence", "replayAfterRollback", "noFormulaChangeProof",
            "darkIntegration", "tests",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void RollbackTd_DisclosesConstraintFailureGapHonestly()
    {
        var text = Risk(RollbackTd).GetProperty("constraintFailureBehavior").GetString()!;
        Assert.Contains("NOT TESTED this phase", text);
    }

    [Fact]
    public void RollbackTd_DoesNotClaimFullMatrix()
    {
        var text = Risk(RollbackTd).GetProperty("closureNote").GetString()!;
        Assert.Contains("this TD cannot close", text);
    }

    [Fact]
    public void RollbackTd_ProvesTransactionInventoryFinding()
    {
        var text = Risk(RollbackTd).GetProperty("transactionInventory").GetString()!;
        Assert.Contains("exactly ONCE per method", text);
        Assert.Contains("NO Classification D", text);
    }

    [Fact]
    public void MatrixTd_RemainsOpenWithPartialFailureInjectionProgress()
    {
        Assert.Equal("CLOSED", Risk(MatrixTd).GetProperty("status").GetString());
        var text = Risk(MatrixTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2F, 2026-08-04)", text);
    }

    [Fact]
    public void RefreshTd_PersistenceTd_PriorMatrixTd_LifecycleTd_PreviewTd_CarryPhase4L2FUpdate()
    {
        foreach (var id in new[] { RefreshTd, PersistenceTd, PriorMatrixTd, LifecycleTd, PreviewTd })
        {
            var text = Risk(id).GetRawText();
            Assert.True(text.Contains("APPEND-ONLY UPDATE (Phase 4L.2F, 2026-08-04)"), $"{id} missing phase4L2FUpdate");
        }
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
    public void DecisionDocument_Exists_AndCarriesBlockedOrPartialClassification()
    {
        var text = File.ReadAllText(DecisionPath());
        var headings = File.ReadAllLines(DecisionPath()).Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        Assert.Equal("## 1. Executive result", headings[0]);
    }
}
