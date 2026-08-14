using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

public sealed class LongHorizonRunwayDirectionAndBoundedMaterializationGovernanceTests
{
    private const string DirectionTd = "TD-LONG-HORIZON-RUNWAY-DOWNWARD-CONSOLIDATION-AUTHORITY-001";
    private const string BoundedTd = "TD-LONG-HORIZON-BOUNDED-RUNWAY-MATERIALIZATION-001";
    private const string JitTd = "TD-LONG-HORIZON-RUNWAY-CORE-JIT-CONTEXT-001";
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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4K_8A_PREPARATION_RUNWAY_DOWNWARD_CONSOLIDATION_AND_BOUNDED_ROLLING_MATERIALIZATION_AUTHORITY_RESOLUTION.md");
    private static string MaterializerPath() => Path.Combine(RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunwayNumericMaterialization", "PreparationRunwayNumericMaterializer.cs");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(risk => risk.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void DirectionAuthority_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(DirectionTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[]
        {
            "weeklyDirectionRules", "longRunDirectionRules", "supportedRelations", "unsupportedRelations",
            "semanticRationale", "existingValidatorTreatment", "failureBehavior", "formulasPreserved",
            "nonClaims", "implementationImpact",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);

        var text = risk.GetRawText();
        Assert.Contains("Start above target: unsupported, fail closed", text);
        Assert.Contains("JIT_SEGMENT_TRANSITION_INFEASIBLE", text);
        Assert.Contains("No interpolation", text);
        Assert.DoesNotContain("downward percentage", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BoundedAuthority_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(BoundedTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[]
        {
            "selectedMaterializationStrategy", "fullBlockAuthority", "boundedExposure", "interpolationCoordinates",
            "localGlobalWeekMapping", "preSpecificTransitionBehavior", "targetLockScope", "futureEvidenceRefresh",
            "mixedWindowImplications", "internalComputationSafeguards", "implementationImpact",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);

        var text = risk.GetRawText();
        Assert.Contains("Option A", text);
        Assert.Contains("complete 3-8-week materializer remains the only numeric authority", text);
        Assert.Contains("Slice-local progress never resets", text);
        Assert.Contains("no TrainingDay", text);
        Assert.Contains("checkpoint evidence", text);
    }

    [Fact]
    public void PriorJitClaim_IsExplicitlyCorrectedWhileUnrelatedAuthoritiesRemain()
    {
        var risk = Risk(JitTd);
        var correction = risk.GetProperty("phase4K8ACorrection").GetString()!;
        Assert.Contains("superseded by direct production evidence", correction);
        Assert.Contains("rejects starting weekly volume above Core Week 1", correction);
        Assert.Contains("rejects negative weekly change", correction);
        Assert.Contains("All unrelated Phase 4K.4 decisions remain valid", correction);
        Assert.Contains("atomic Runway/Core-target timing", correction);
        Assert.Contains("mixed-window all-or-nothing", correction);
        Assert.Contains("target immutability", correction);
        Assert.Contains("safety priority", correction);
        Assert.Contains("pace/calendar authorities", correction);
        Assert.Contains("JIT reason taxonomy", correction);
    }

    [Fact]
    public void Redesign_RemainsOpenAndNamesOutstandingBoundaries()
    {
        var risk = Risk(RedesignTd);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        var text = risk.GetRawText();
        Assert.Contains("UPDATE (Phase 4K.8A)", text);
        Assert.Contains("Phase 4K.8B", text);
        Assert.Contains("JIT runtime remains unimplemented", text);
        Assert.Contains("persistence", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public/API", text);
        Assert.Contains("Flutter", text);
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
    public void DecisionDocument_HasExactlyThirtyOneSectionsAndRequiredClassifications()
    {
        var text = File.ReadAllText(DecisionPath());
        var headings = File.ReadAllLines(DecisionPath()).Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        Assert.Equal(31, headings.Length);
        Assert.Equal("## 1. Executive result", headings[0]);
        Assert.Equal("## 31. Exact next phase", headings[^1]);
        Assert.Contains("PREPARATION_RUNWAY_DIRECTION_AND_BOUNDED_ROLLING_MATERIALIZATION_AUTHORITIES_APPROVED", text);
        Assert.Contains("PREPARATION_RUNWAY_SUPPORTED_DIRECTION_RELATIONS_ARE_EXPLICIT_AND_UNSUPPORTED_DOWNWARD_CASES_FAIL_CLOSED_WITHOUT_NEW_FORMULA", text);
        Assert.Contains("PREPARATION_RUNWAY_FULL_BLOCK_REMAINS_THE_SINGLE_NUMERIC_AUTHORITY_WITH_BOUNDED_ROLLING_EXPOSURE_PRESERVING_EXACT_WEEK_VALUES_TARGET_LOCK_AND_TERMINAL_STAGE", text);
        Assert.Contains("PHASE4K_8_RUNTIME_REMAINS_UNIMPLEMENTED_PENDING_THE_APPROVED_AUTHORITY_CONTRACT_IMPLEMENTATION", text);
    }

    [Fact]
    public void ProductionSource_StillCarriesExistingFailClosedAndFullBlockGuards()
    {
        var text = File.ReadAllText(MaterializerPath());
        Assert.Contains("startingWeekly - targetWeekly > policy.ContinuityToleranceKm", text);
        Assert.Contains("weeklyChange < -policy.ContinuityToleranceKm", text);
        Assert.Contains("MaterializedWeeks.Count is < 3 or > 8", text);
        Assert.Contains("\"PreSpecificTransition\"", text);
        Assert.Contains("Enumerable.Range(1, ordered.Length)", text);
    }

    [Fact]
    public void DecisionIntroducesNoFormulaFallbackTargetLiftOrRuntimeWiring()
    {
        var text = File.ReadAllText(DecisionPath());
        Assert.Contains("no hidden target lift", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("onboarding fallback", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("planned GE-exit provenance-only", text);
        Assert.Contains("No production contract, algorithm, runtime wiring", text);
        Assert.Contains("No future Runway output is persisted or publicly exposed", text);
        Assert.DoesNotContain("new downward percentage", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Phase4K8A_AddsNoProductionApiPersistenceOrFlutterWiring()
    {
        var roots = new[]
        {
            Path.Combine(RepoRoot(), "backend", "RunningApp.Application"),
            Path.Combine(RepoRoot(), "backend", "RunningApp.Api"),
            Path.Combine(RepoRoot(), "backend", "RunningApp.Persistence"),
            Path.Combine(RepoRoot(), "backend", "RunningApp.Infrastructure"),
            Path.Combine(RepoRoot(), "mobile", "lib"),
        };
        // PreparationRunwayDirectionPolicy and ComputedInternalPending were
        // deliberately EXPECTED next-phase implementation targets when this
        // test was written for Phase 4K.8A's own zero-code boundary -- Phase
        // 4K.8B has since implemented them exactly as directed (dark,
        // unwired, RollingActivation/PreparationRunway only), so their
        // presence is no longer a violation; removing them from this list
        // keeps the test meaningful for what it can still prove (no stray
        // phase tag, no invented public reason code).
        var forbidden = new[]
        {
            "Phase4K8A",
            "RUNWAY_DOWNWARD_CONSOLIDATION_UNSUPPORTED",
            "RUNWAY_BOUNDED_MATERIALIZATION_UNAVAILABLE",
        };
        var hits = roots.Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(path => Path.GetExtension(path) is ".cs" or ".dart")
            .SelectMany(path => forbidden.Where(token => File.ReadAllText(path).Contains(token, StringComparison.Ordinal))
                .Select(token => $"{path}: {token}"))
            .ToArray();
        Assert.Empty(hits);
    }
}
