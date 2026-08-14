using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4L.2 -- governance cross-check for
/// <c>TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001</c> (CLOSED)
/// and the append-only updates to the two TDs it references.
/// </summary>
public sealed class LongHorizonRollingPersistenceRestartSafetyGovernanceTests
{
    private const string PersistenceTd = "TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001";
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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4L_2_LONG_HORIZON_ROLLING_PERSISTENCE_AND_RESTART_SAFE_STATE_CONTRACT.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void PersistenceDecision_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(PersistenceTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[]
        {
            "durableStateClassification", "aggregateModel", "structuralWeekModel", "executableSessionOwnership",
            "checkpointPersistence", "runwayLockPrescriptionPersistence", "coreContextPersistence",
            "blockRetryPersistence", "persistenceVersioning", "transactionBoundaries", "concurrency", "idempotency",
            "reconstruction", "restartContinuation", "corruptionBehavior", "publicLeakageBoundary", "migration",
            "liveWiringStatus", "tests",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void PersistenceDecision_ExecutableSessionsAreNotTrainingDayRows()
    {
        var text = Risk(PersistenceTd).GetProperty("executableSessionOwnership").GetString()!;
        Assert.Contains("Deliberately NOT TrainingDay rows", text);
        Assert.Contains("fully isolated from every live read path", text);
    }

    [Fact]
    public void PersistenceDecision_RunwayCoreUseDecisionANotRegeneration()
    {
        var runway = Risk(PersistenceTd).GetProperty("runwayLockPrescriptionPersistence").GetString()!;
        Assert.Contains("Decision A (not B)", runway);
        Assert.Contains("never regenerated on restart", runway);
    }

    [Fact]
    public void PersistenceDecision_TestsRunAgainstRealPostgresNotInMemory()
    {
        var text = Risk(PersistenceTd).GetProperty("tests").GetString()!;
        Assert.Contains("real configured PostgreSQL database", text);
        Assert.Contains("30 new focused tests", text);
    }

    [Fact]
    public void PersistenceDecision_ConcurrencyUsesXminAndRejectsStaleWrites()
    {
        var text = Risk(PersistenceTd).GetProperty("concurrency").GetString()!;
        Assert.Contains("xmin", text);
        Assert.Contains("never silently swallowed or treated as success", text);
    }

    [Fact]
    public void PersistenceDecision_DirectBlockedToActivatedHasNoCodePath()
    {
        var text = Risk(PersistenceTd).GetProperty("blockRetryPersistence").GetString()!;
        Assert.Contains("no repository method performs that transition", text);
    }

    [Fact]
    public void PersistenceDecision_MigrationIsAdditiveOnly()
    {
        var text = Risk(PersistenceTd).GetProperty("migration").GetString()!;
        Assert.Contains("zero AddColumn/AlterColumn/DropColumn against any existing table", text);
    }

    [Fact]
    public void PersistenceDecision_JitCompositionPathScopeGapDisclosed()
    {
        var text = Risk(PersistenceTd).GetProperty("restartContinuation").GetString()!;
        Assert.Contains("was not exercised end-to-end through the continuation service", text);
    }

    [Fact]
    public void PersistenceDecision_ZeroLiveWiring()
    {
        var risk = Risk(PersistenceTd);
        Assert.Contains("Zero live wiring", risk.GetProperty("liveWiringStatus").GetString());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    [Fact]
    public void PreviewTd_CarriesPhase4L2Update()
    {
        var text = Risk(PreviewTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2, 2026-08-03)", text);
        Assert.Equal("CLOSED", Risk(PreviewTd).GetProperty("status").GetString());
    }

    [Fact]
    public void Redesign_RemainsOpenAndReferencesPhase4L2()
    {
        var risk = Risk(RedesignTd);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        var text = risk.GetRawText();
        Assert.Contains("UPDATE (Phase 4L.2):", text);
        Assert.Contains("closed for the dark subsystem", text);
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
        Assert.Contains("LONG_HORIZON_ROLLING_PERSISTENCE_AND_RESTART_SAFE_STATE_CONTRACT_COMPLETED_DARK", text);
        Assert.Contains("LONG_HORIZON_PUBLIC_PREVIEW_CONFIRMATION_API_HOME_CALENDAR_AND_FLUTTER_REMAIN_UNWIRED", text);
    }
}
