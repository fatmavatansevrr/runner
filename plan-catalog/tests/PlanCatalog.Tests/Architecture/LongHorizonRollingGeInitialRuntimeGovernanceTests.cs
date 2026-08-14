using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

public sealed class LongHorizonRollingGeInitialRuntimeGovernanceTests
{
    private const string RuntimeTd = "TD-LONG-HORIZON-ROLLING-GE-INITIAL-RUNTIME-001";
    private const string RedesignTd = "TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001";

    private static string PlanCatalogRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PlanCatalog.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static string RepoRoot() => Directory.GetParent(PlanCatalogRoot())!.FullName;
    private static string JsonPath() => Path.Combine(PlanCatalogRoot(), "artifacts", "audits", "activation-readiness-risks.json");
    private static string MarkdownPath() => Path.Combine(PlanCatalogRoot(), "artifacts", "audits", "activation-readiness-risks.md");
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4K_6_ROLLING_GENERAL_ENDURANCE_NUMERIC_RUNTIME_AND_INITIAL_WINDOW_IMPLEMENTATION.md");
    private static string RuntimePath() => Path.Combine(RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "LongHorizon", "RollingActivation", "LongHorizonRollingInitialActivationRuntime.cs");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(risk => risk.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void RuntimeTd_IsClosedAndContainsEveryRequiredGovernanceField()
    {
        var risk = Risk(RuntimeTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
        foreach (var field in new[]
        {
            "applicability", "runtimeEntryPoint", "structuralRoadmapImplementation", "initialWindowRule",
            "partialWindowBehavior", "onboardingAuthority", "geNumericAuthoritiesReused",
            "futureNumericPendingBehavior", "atomicity", "failureTaxonomy", "validatorInvocation",
            "darkIntegrationStatus", "publicActivationStatus", "persistenceStatus",
            "checkpointRuntimeStatus", "runwayCoreRuntimeStatus", "tests",
        })
        {
            Assert.True(risk.TryGetProperty(field, out var value), $"Missing governance field {field}.");
            Assert.False(string.IsNullOrWhiteSpace(value.GetString()));
        }
    }

    [Fact]
    public void RegistryJsonMarkdownAndAggregate_AreSemanticallyAligned()
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
        Assert.Contains(RuntimeTd, markdown);
    }

    [Fact]
    public void RedesignTd_RemainsOpenAndRecordsOnlyBoundedInitialProgress()
    {
        var risk = Risk(RedesignTd);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        Assert.Contains("Phase 4K.6", risk.GetRawText());
        Assert.Contains("checkpoint aggregation", risk.GetRawText());
        Assert.Contains("Runway/Core JIT", risk.GetRawText());
    }

    [Fact]
    public void DecisionDocument_HasExactlyTwentyThreeRequiredSectionsAndFinalClassifications()
    {
        var text = File.ReadAllText(DecisionPath());
        var headings = File.ReadAllLines(DecisionPath()).Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        Assert.Equal(23, headings.Length);
        Assert.Equal("## 1. Executive result", headings[0]);
        Assert.Equal("## 23. Exact next phase", headings[^1]);
        Assert.Contains("LONG_HORIZON_ROLLING_GENERAL_ENDURANCE_INITIAL_NUMERIC_RUNTIME_COMPLETED_DARK", text);
        Assert.Contains("LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_AND_CHECKPOINT_RECALCULATION_REMAIN_UNCHANGED", text);
    }

    [Fact]
    public void RuntimeSource_IsBoundedAndHasNoRunwayCoreJitOrTargetLockInvocation()
    {
        var text = File.ReadAllText(RuntimePath());
        Assert.Contains("ILongHorizonRollingGeWindowMaterializer", text);
        Assert.Contains("Take(actualWindowSize)", text);
        Assert.DoesNotContain("PreparationRunwayNumericMaterializer.Materialize", text);
        Assert.DoesNotContain("TenKPreparationRunwayCoreGenerator", text);
        Assert.DoesNotContain("LongHorizonJitContextDecision", text);
        Assert.DoesNotContain("LongHorizonLockedCoreWeekOneTarget", text);
    }

    [Fact]
    public void NoApiPersistenceInfrastructureOrFlutterSourceReferencesRuntime()
    {
        var roots = new[]
        {
            Path.Combine(RepoRoot(), "backend", "RunningApp.Api"),
            Path.Combine(RepoRoot(), "backend", "RunningApp.Persistence"),
            Path.Combine(RepoRoot(), "backend", "RunningApp.Infrastructure"),
            Path.Combine(RepoRoot(), "mobile", "lib"),
        };
        var hits = roots.Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(path => Path.GetExtension(path) is ".cs" or ".dart")
            .Where(path => File.ReadAllText(path).Contains("LongHorizonRollingInitialActivation", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(hits);
    }
}
