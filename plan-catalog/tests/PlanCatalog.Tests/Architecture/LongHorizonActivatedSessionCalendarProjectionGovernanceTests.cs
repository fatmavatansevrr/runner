using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

public sealed class LongHorizonActivatedSessionCalendarProjectionGovernanceTests
{
    private const string DecisionId = "TD-LONG-HORIZON-ACTIVATED-SESSION-CALENDAR-PROJECTION-001";
    private const string CompositionId = "TD-LONG-HORIZON-JIT-REAL-CORE-CONDITION-CALENDAR-COMPOSITION-001";
    private const string RuntimeId = "TD-LONG-HORIZON-RUNWAY-CORE-JIT-RUNTIME-001";
    private const string RedesignId = "TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001";

    private static string PlanRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PlanCatalog.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static string RepoRoot() => Directory.GetParent(PlanRoot())!.FullName;
    private static string JsonPath() => Path.Combine(PlanRoot(), "artifacts", "audits", "activation-readiness-risks.json");
    private static string MarkdownPath() => Path.Combine(PlanRoot(), "artifacts", "audits", "activation-readiness-risks.md");
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4K_8D_REAL_SESSION_LEVEL_CALENDAR_PROJECTION_AND_ACTIVATED_NUMERIC_WEEK_ALIGNMENT.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray().Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void DecisionIsClosedAndCarriesAllRequiredFields()
    {
        var risk = Risk(DecisionId);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[] { "calendarAuthority", "structuralBoundaryAuthority", "sessionIdentityMapping", "runwayProjection",
                     "coreProjection", "mixedWindowContinuity", "activatedWeekAlignment", "validatorChain", "failureBehavior",
                     "atomicity", "determinism", "darkIntegration", "persistencePublicStatus", "tests" })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void ThreePriorRecordsContainAppendOnlyPhase4K8DUpdatesAndRedesignRemainsOpen()
    {
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4K.8D", Risk(CompositionId).GetProperty("phase4K8DUpdate").GetString());
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4K.8D", Risk(RuntimeId).GetProperty("phase4K8DUpdate").GetString());
        var redesign = Risk(RedesignId);
        Assert.Equal("OPEN", redesign.GetProperty("status").GetString());
        Assert.Contains("UPDATE (Phase 4K.8D)", redesign.GetRawText());
        Assert.Contains("Phase 4K.9", redesign.GetRawText());
        Assert.Contains("persistence", redesign.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Flutter", redesign.GetRawText());
    }

    [Fact]
    public void JsonMarkdownParityAndAggregateAreCurrentAndUnique()
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
    public void DecisionDocumentHasExactlyTwentyEightSectionsAndSuccessClassifications()
    {
        var headings = File.ReadAllLines(DecisionPath()).Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        var text = File.ReadAllText(DecisionPath());
        Assert.Equal(28, headings.Length);
        Assert.Equal("## 1. Executive result", headings[0]);
        Assert.Equal("## 28. Exact next phase", headings[^1]);
        foreach (var classification in new[]
        {
            "LONG_HORIZON_REAL_SESSION_LEVEL_CALENDAR_PROJECTION_AND_ACTIVATED_NUMERIC_WEEK_ALIGNMENT_COMPLETED_DARK",
            "LONG_HORIZON_SELECTED_RUNWAY_AND_CORE_ACTIVATED_WEEKS_NOW_EXPOSE_THE_EXACT_SESSION_DATES_PRODUCED_BY_THE_REAL_CALENDAR_COMPOSITION_AUTHORITIES",
            "LONG_HORIZON_WEEK_LEVEL_CALENDAR_BOUNDARIES_REMAIN_STRUCTURAL_ONLY_AND_NO_SECOND_SESSION_CALENDAR_ALGORITHM_IS_INTRODUCED",
            "LONG_HORIZON_MIXED_WINDOW_CALENDAR_ALIGNMENT_IS_ATOMIC_AND_FAILURE_LEAVES_ZERO_NEWLY_ACTIVATED_WEEKS",
            "LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_API_AND_FLUTTER_REMAIN_UNCHANGED",
        }) Assert.Contains(classification, text);
    }

    [Fact]
    public void AdapterSourceContainsNoSecondCalendarAuthorityOrPublicWiring()
    {
        var root = Path.Combine(RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "LongHorizon", "RollingActivation");
        var adapter = File.ReadAllText(Path.Combine(root, "LongHorizonRealCalendarProjectionAdapter.cs"));
        Assert.DoesNotContain("CalendarComposer", adapter);
        Assert.DoesNotContain("WeekStartDate(", adapter);
        Assert.DoesNotContain("Guid.NewGuid", adapter);
        Assert.Contains("SessionDate", adapter);
        Assert.Contains("AssignedDate = match.SessionDate", adapter);
    }

    [Fact]
    public void NoApiPersistenceDtoDiOrFlutterSourceReferencesProjectionContracts()
    {
        var roots = new[] { Path.Combine(RepoRoot(), "backend", "RunningApp.Api"), Path.Combine(RepoRoot(), "backend", "RunningApp.Persistence"),
            Path.Combine(RepoRoot(), "backend", "RunningApp.Infrastructure"), Path.Combine(RepoRoot(), "mobile", "lib") };
        var tokens = new[] { "LongHorizonActivatedSessionCalendarProjection", "LongHorizonRealCalendarProjectionAdapter", "LongHorizonActivatedCalendarAlignmentValidator" };
        var hits = roots.Where(Directory.Exists).SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(path => Path.GetExtension(path) is ".cs" or ".dart")
            .SelectMany(path => tokens.Where(token => File.ReadAllText(path).Contains(token, StringComparison.Ordinal)).Select(token => $"{path}:{token}"))
            .ToArray();
        Assert.Empty(hits);
    }
}
