using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

public sealed class LongHorizonPostgresConstraintRollbackGovernanceTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string JsonPath = Path.Combine(RepoRoot, "plan-catalog", "artifacts", "audits", "activation-readiness-risks.json");
    private static readonly string MarkdownPath = Path.Combine(RepoRoot, "plan-catalog", "artifacts", "audits", "activation-readiness-risks.md");
    private static readonly string PhasePath = Path.Combine(RepoRoot, "PHASE4L_2F_A_POSTGRESQL_CONSTRAINT_EXCEPTION_AND_ROLLBACK_COMPLETION.md");

    [Fact]
    public void ConstraintRollbackTd_IsClosedAndParentFailureMatrixIsClosed()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(JsonPath));
        var risks = json.RootElement.GetProperty("risks").EnumerateArray().ToList();
        Assert.Equal("CLOSED", risks.Single(r => r.GetProperty("id").GetString() == "TD-LONG-HORIZON-POSTGRESQL-CONSTRAINT-EXCEPTION-ROLLBACK-001").GetProperty("status").GetString());
        Assert.Equal("CLOSED", risks.Single(r => r.GetProperty("id").GetString() == "TD-LONG-HORIZON-TRANSACTIONAL-FAILURE-INJECTION-ROLLBACK-MATRIX-001").GetProperty("status").GetString());
    }

    [Fact]
    public void RemainingPhase4L2BParent_StaysOpenForPhase4L2G()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(JsonPath));
        var parent = json.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == "TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001");
        Assert.Equal("CLOSED", parent.GetProperty("status").GetString());
    }

    [Fact]
    public void JsonAndMarkdownBothRecordTheNewTdAndAggregate()
    {
        var markdown = File.ReadAllText(MarkdownPath);
        Assert.Contains("TD-LONG-HORIZON-POSTGRESQL-CONSTRAINT-EXCEPTION-ROLLBACK-001", markdown);

        using var json = JsonDocument.Parse(File.ReadAllText(JsonPath));
        var risks = json.RootElement.GetProperty("risks").EnumerateArray().ToList();
        Assert.NotEmpty(risks);
        Assert.Equal(
            risks.Count,
            risks.Count(r => r.GetProperty("status").GetString() == "OPEN")
            + risks.Count(r => r.GetProperty("status").GetString() == "CLOSED"));
        Assert.Equal(risks.Count, risks.Select(r => r.GetProperty("id").GetString()).Distinct().Count());
    }

    [Fact]
    public void PhaseDocument_RecordsRealSqlStatesNoCheckConstraintAndNoMigration()
    {
        var document = File.ReadAllText(PhasePath);
        Assert.Contains("23505", document);
        Assert.Contains("23503", document);
        Assert.Contains("23502", document);
        Assert.Contains("no Long-Horizon check constraint", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No migration was added", document);
        Assert.Contains("Phase 4L.2G", document);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "plan-catalog")) && Directory.Exists(Path.Combine(current.FullName, "backend")))
                return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
