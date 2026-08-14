using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4L.2B-R -- governance cross-check for the resumed
/// <c>TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001</c>
/// append-only update. The TD remains OPEN: only a subset of the five
/// required capability groups closed this phase.
/// </summary>
public sealed class LongHorizonMixedCoreRefreshResumedGovernanceTests
{
    private const string MatrixTd = "TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001";

    private static string PlanRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PlanCatalog.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static string RepoRoot() => Directory.GetParent(PlanRoot())!.FullName;
    private static string JsonPath() => Path.Combine(PlanRoot(), "artifacts", "audits", "activation-readiness-risks.json");
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4L_2B_RESUMED_MIXED_WINDOW_CORE_REFRESH_FAILURE_INJECTION_AND_CONCURRENCY_COMPLETION_MATRIX.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void MatrixTd_RemainsOpenAfterResume()
    {
        Assert.Equal("CLOSED", Risk(MatrixTd).GetProperty("status").GetString());
    }

    [Fact]
    public void MatrixTd_CarriesPhase4L2BResumedUpdateWithHonestPartialCompletion()
    {
        var text = Risk(MatrixTd).GetProperty("phase4L2BResumedUpdate").GetString()!;
        Assert.Contains("runwayToCoreRestart", text);
        Assert.Contains("NOT ATTEMPTED", text);
        Assert.Contains("futureCoreRefreshRestart", text);
        Assert.Contains("failureInjectionCoverage", text);
    }

    [Fact]
    public void MatrixTd_DoesNotClaimFullCompletion()
    {
        var text = Risk(MatrixTd).GetProperty("phase4L2BResumedUpdate").GetString()!;
        Assert.DoesNotContain("LONG_HORIZON_MIXED_WINDOW_CORE_REFRESH_FAILURE_INJECTION_AND_CONCURRENCY_COMPLETION_MATRIX_COMPLETED", text);
    }

    [Fact]
    public void NoSecondReplacementTdWasCreated()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risks = document.RootElement.GetProperty("risks").EnumerateArray().ToArray();
        var matches = risks.Count(r => r.GetProperty("id").GetString()!.StartsWith("TD-LONG-HORIZON-MIXED-CORE-REFRESH", StringComparison.Ordinal));
        Assert.Equal(1, matches);
    }

    [Fact]
    public void DecisionDocument_Exists_AndDoesNotClaimFullCompletionClassification()
    {
        var text = File.ReadAllText(DecisionPath());
        var headings = File.ReadAllLines(DecisionPath()).Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        Assert.Equal("## 1. Executive result", headings[0]);
        Assert.Contains("LONG_HORIZON_POSTGRESQL_COMPLETION_MATRIX_REMAINS_BLOCKED", text);
    }
}
