using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4L.2A -- governance cross-check for
/// <c>TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001</c>
/// (CLOSED) and the append-only updates to the TDs it references.
/// </summary>
public sealed class LongHorizonRunwayCorePostgresRestartRecoveryMatrixGovernanceTests
{
    private const string MatrixTd = "TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001";
    private const string PersistenceTd = "TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001";
    private const string LifecycleTd = "TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001";
    private const string PreviewTd = "TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001";
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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4L_2A_RUNWAY_CORE_POSTGRESQL_RESTART_RECOVERY_AND_TRANSACTION_BOUNDARY_MATRIX.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void MatrixDecision_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(MatrixTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[]
        {
            "postgreSqlTestAuthority", "restartCheckpointMatrix", "firstRunwayRestart", "runwayContinuationRestart",
            "runwayToCoreRestart", "coreOnlyRestart", "futureCoreRefreshRestart", "blockedRetryRestart",
            "transactionFailureInjection", "commitAcknowledgementAmbiguity", "concurrency", "noRegenerationProof",
            "immutableFingerprints", "corruptionBehavior", "databaseConstraints", "terminalCompletionRestart",
            "darkIntegration", "tests",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void MatrixDecision_UsesRealPostgresNeverInMemory()
    {
        var text = Risk(MatrixTd).GetProperty("postgreSqlTestAuthority").GetString()!;
        Assert.Contains("Zero EF InMemory usage", text);
        Assert.Contains("brand-new AppDbContext", text);
    }

    [Fact]
    public void MatrixDecision_TwoRealDefectsDisclosed()
    {
        var text = Risk(MatrixTd).GetProperty("closureNote").GetString()!;
        Assert.Contains("Two real defects were found and corrected", text);
        Assert.Contains("CalendarProjectionPayloadJson, TargetLockPayloadJson", text);
        Assert.Contains("collided across different plans", text);
    }

    [Fact]
    public void MatrixDecision_MixedWindowAndRefreshExplicitlyNotClosed()
    {
        var text = Risk(MatrixTd).GetProperty("closureNote").GetString()!;
        Assert.Contains("does NOT close", text);
        Assert.Contains("1+3/2+2/3+1", text);
        Assert.Contains("Core-only and future-Core-refresh restart", text);
    }

    [Fact]
    public void MatrixDecision_NoRegenerationProofExcludesRealCoreAndRunwayGenerators()
    {
        var text = Risk(MatrixTd).GetProperty("noRegenerationProof").GetString()!;
        Assert.Contains("RuntimeConditionResolutionService, TenKPreparationRunwayDarkOrchestrator, or PreparationRunwayNumericMaterializer", text);
        Assert.Contains("LongHorizonStructuralMaterializer.MaterializeAsync", text);
    }

    [Fact]
    public void MatrixDecision_ZeroLiveWiring()
    {
        var risk = Risk(MatrixTd);
        Assert.Contains("Internal only", risk.GetProperty("darkIntegration").GetString());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    [Fact]
    public void MatrixDecision_TestCountRecorded()
    {
        var text = Risk(MatrixTd).GetProperty("tests").GetString()!;
        Assert.Contains("16 new focused tests", text);
    }

    [Fact]
    public void MatrixTd_CarriesPhase4L2BUpdate()
    {
        var text = Risk(MatrixTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2B, 2026-08-03)", text);
        Assert.Equal("CLOSED", Risk(MatrixTd).GetProperty("status").GetString());
    }

    [Fact]
    public void PersistenceTd_CarriesPhase4L2AUpdate()
    {
        var text = Risk(PersistenceTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2A, 2026-08-03)", text);
        Assert.Equal("CLOSED", Risk(PersistenceTd).GetProperty("status").GetString());
    }

    [Fact]
    public void LifecycleTd_CarriesPhase4L2AUpdate()
    {
        var text = Risk(LifecycleTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2A, 2026-08-03)", text);
    }

    [Fact]
    public void PreviewTd_CarriesPhase4L2AUpdate()
    {
        var text = Risk(PreviewTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2A, 2026-08-03)", text);
    }

    [Fact]
    public void Redesign_RemainsOpenAndReferencesPhase4L2A()
    {
        var risk = Risk(RedesignTd);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        var text = risk.GetRawText();
        Assert.Contains("UPDATE (Phase 4L.2A):", text);
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
        Assert.Contains("LONG_HORIZON_RUNWAY_CORE_POSTGRESQL_RESTART_RECOVERY_AND_TRANSACTION_BOUNDARY_MATRIX_COMPLETED", text);
        Assert.Contains("LONG_HORIZON_PUBLIC_PREVIEW_CONFIRMATION_API_HOME_CALENDAR_AND_FLUTTER_REMAIN_UNWIRED", text);
    }
}
