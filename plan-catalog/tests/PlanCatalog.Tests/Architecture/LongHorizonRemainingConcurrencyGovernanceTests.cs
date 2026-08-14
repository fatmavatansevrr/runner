using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

public sealed class LongHorizonRemainingConcurrencyGovernanceTests
{
    private const string NewTd = "TD-LONG-HORIZON-REMAINING-CONCURRENCY-IDEMPOTENCY-COMMIT-AMBIGUITY-001";
    private static readonly string[] UpdatedIds =
    [
        "TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001",
        "TD-LONG-HORIZON-TRANSACTIONAL-FAILURE-INJECTION-ROLLBACK-MATRIX-001",
        "TD-LONG-HORIZON-POSTGRESQL-CONSTRAINT-EXCEPTION-ROLLBACK-001",
        "TD-LONG-HORIZON-FUTURE-ONLY-CORE-CONTEXT-REFRESH-001",
        "TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001",
        "TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001",
        "TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001",
        "TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001",
    ];

    private static string PlanRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PlanCatalog.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static JsonElement[] Risks()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(PlanRoot(), "artifacts", "audits", "activation-readiness-risks.json")));
        return doc.RootElement.GetProperty("risks").EnumerateArray().Select(x => x.Clone()).ToArray();
    }

    [Fact]
    public void NewTd_IsClosedAndCarriesEveryRequiredEvidenceField()
    {
        var risk = Risks().Single(x => x.GetProperty("id").GetString() == NewTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[] { "concurrencyAuthorityInventory", "raceHarness", "mixedActivation", "coreOnlyActivation",
            "coreRefresh", "block", "retry", "activationRetryRace", "blockActivationRace", "checkpointRefreshRace",
            "terminalRace", "exactReplay", "nextOperationDistinction", "idempotencyAudit", "lookupPrecision",
            "commitAmbiguity", "staleContextBehavior", "crossPlanIsolation", "constraintXminInteraction",
            "finalAcceptance", "darkIntegration", "tests" })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void EveryRequiredParent_HasAppendOnlyPhase4L2GUpdate()
    {
        var risks = Risks();
        foreach (var id in UpdatedIds)
        {
            var risk = risks.Single(x => x.GetProperty("id").GetString() == id);
            Assert.Contains("APPEND-ONLY UPDATE (Phase 4L.2G", risk.GetProperty("phase4L2GUpdate").GetString());
        }
    }

    [Fact]
    public void PhaseDocument_HasAllThirtyFiveOrderedSections()
    {
        var path = Path.Combine(Directory.GetParent(PlanRoot())!.FullName,
            "PHASE4L_2G_REMAINING_CONCURRENCY_IDEMPOTENCY_AND_COMMIT_AMBIGUITY_MATRIX.md");
        var headings = File.ReadAllLines(path).Where(x => x.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        Assert.Equal(35, headings.Length);
        for (var i = 1; i <= 35; i++) Assert.StartsWith($"## {i}.", headings[i - 1]);
    }
}
