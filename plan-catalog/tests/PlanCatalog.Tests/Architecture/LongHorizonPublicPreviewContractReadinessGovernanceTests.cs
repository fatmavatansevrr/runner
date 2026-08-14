using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4L.1 -- governance cross-check for
/// <c>TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001</c> (CLOSED)
/// and the append-only updates to the two TDs it references.
/// </summary>
public sealed class LongHorizonPublicPreviewContractReadinessGovernanceTests
{
    private const string ContractTd = "TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001";
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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4L_1_LONG_HORIZON_PUBLIC_PREVIEW_CONTRACT_AND_DARK_TO_PUBLIC_READINESS_REVIEW.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void ContractDecision_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(ContractTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[]
        {
            "publicPreviewSemantics", "structuralRoadmapContract", "executableWeekContract", "pendingWeekContract",
            "blockedWeekContract", "publicLifecycleMapping", "previewReadiness", "confirmationReadiness",
            "persistenceReadiness", "restartResumeReadiness", "apiReadiness", "flutterReadiness", "leakageGuard",
            "privacyReview", "payloadSizeReview", "darkMapper", "validator", "liveWiringStatus", "tests",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void ContractDecision_ConfirmationReadinessIsNotReadyWithEvidence()
    {
        var text = Risk(ContractTd).GetProperty("confirmationReadiness").GetString()!;
        Assert.Contains("NotReadyForConfirmation", text);
        Assert.Contains("CatalogPreviewNotPersistableException", text);
    }

    [Fact]
    public void ContractDecision_PersistenceReadinessRequiresNewSchema()
    {
        var text = Risk(ContractTd).GetProperty("persistenceReadiness").GetString()!;
        Assert.Contains("Blocked/requires new persistence", text);
        Assert.Contains("no Pending/Blocked/Available lifecycle status field", text);
    }

    [Fact]
    public void ContractDecision_ApiReadinessRecommendsDedicatedDiscriminator()
    {
        var text = Risk(ContractTd).GetProperty("apiReadiness").GetString()!;
        Assert.Contains("dedicated Long-Horizon preview response discriminator", text);
        Assert.Contains("non-nullable", text);
    }

    [Fact]
    public void ContractDecision_PendingWeekLeakageIsStructurallyImpossible()
    {
        var text = Risk(ContractTd).GetProperty("pendingWeekContract").GetString()!;
        Assert.Contains("structurally impossible", text);
    }

    [Fact]
    public void ContractDecision_LiveWiringStatusIsZero()
    {
        var risk = Risk(ContractTd);
        Assert.Contains("Zero live wiring exists or was added", risk.GetProperty("liveWiringStatus").GetString());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    [Fact]
    public void ContractDecision_TestCountRecorded()
    {
        var text = Risk(ContractTd).GetProperty("tests").GetString()!;
        Assert.Contains("37 new focused tests", text);
    }

    [Fact]
    public void ContractDecision_TwoRealBugsDisclosedInClosureNote()
    {
        var text = Risk(ContractTd).GetProperty("closureNote").GetString()!;
        Assert.Contains("Two genuine bugs were found and fixed", text);
        Assert.Contains("CurrentWindow.Status", text);
    }

    [Fact]
    public void LifecycleTd_CarriesPhase4L1Update()
    {
        var text = Risk(LifecycleTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.1, 2026-08-03)", text);
        Assert.Equal("CLOSED", Risk(LifecycleTd).GetProperty("status").GetString());
    }

    [Fact]
    public void Redesign_RemainsOpenAndReferencesPhase4L1()
    {
        var risk = Risk(RedesignTd);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        var text = risk.GetRawText();
        Assert.Contains("UPDATE (Phase 4L.1):", text);
        Assert.Contains("still Blocked/NotReady", text);
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
        Assert.Contains("LONG_HORIZON_PUBLIC_PREVIEW_CONTRACT_AND_DARK_TO_PUBLIC_READINESS_REVIEW_COMPLETED", text);
        Assert.Contains("LONG_HORIZON_LIVE_PUBLIC_ACTIVATION_REMAINS_UNWIRED", text);
    }
}
