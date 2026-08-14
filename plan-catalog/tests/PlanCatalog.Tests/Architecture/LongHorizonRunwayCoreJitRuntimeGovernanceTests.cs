using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4K.8 — governance cross-check for
/// <c>TD-LONG-HORIZON-RUNWAY-CORE-JIT-RUNTIME-001</c> (CLOSED) and the
/// append-only updates to the two TDs it references.
/// </summary>
public sealed class LongHorizonRunwayCoreJitRuntimeGovernanceTests
{
    private const string RuntimeTd = "TD-LONG-HORIZON-RUNWAY-CORE-JIT-RUNTIME-001";
    private const string JitContextTd = "TD-LONG-HORIZON-RUNWAY-CORE-JIT-CONTEXT-001";
    private const string ContractsTd = "TD-LONG-HORIZON-RUNWAY-BOUNDED-PRESCRIPTION-CONTRACTS-001";
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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4K_8_RUNWAY_AND_CORE_JIT_NUMERIC_RUNTIME_AND_MIXED_SEGMENT_WINDOW_ACTIVATION.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void RuntimeDecision_IsClosedAndCarriesEveryRequiredField()
    {
        var risk = Risk(RuntimeTd);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        foreach (var field in new[]
        {
            "runtimeEntryPoint", "eligibility", "windowSelection", "jitEvidenceAuthority", "coreInputMapping",
            "coreTargetGeneration", "directionGuard", "targetLockCreationAndReuse", "fullRunwayMaterialization",
            "boundedRunwayExposure", "geRunwayBehavior", "runwayOnlyBehavior", "runwayCoreBehavior",
            "coreOnlyBehavior", "futureCoreRefresh", "calendarPaceBehavior", "atomicityAndLifecycle",
            "determinismAndVersioning", "boundedExecutionProof", "persistencePublicStatus", "tests",
        })
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty(field).GetString()), field);
    }

    [Fact]
    public void RuntimeDecision_OnboardingNeverRollingAuthority()
    {
        var text = Risk(RuntimeTd).GetRawText();
        Assert.Contains("onboarding evidence and planned GE exit are never read as rolling authority", text);
    }

    [Fact]
    public void RuntimeDecision_CoreGeneratorDisclosedAsCallerResponsibility()
    {
        var text = Risk(RuntimeTd).GetRawText();
        Assert.Contains("This runtime does not itself invoke TenKPreparationRunwayCoreGenerator", text);
        Assert.Contains("disclosed explicitly, not hidden", text);
    }

    [Fact]
    public void RuntimeDecision_DirectionGuardReusedVerbatim_NoDownwardInterpolation()
    {
        var text = Risk(RuntimeTd).GetRawText();
        Assert.Contains("Reuses Phase 4K.8B's PreparationRunwayDirectionGuard verbatim", text);
        Assert.Contains("No downward interpolation, target lift, or evidence lowering exists anywhere in this runtime", text);
    }

    [Fact]
    public void RuntimeDecision_FullMaterializerCalledExactlyOnce()
    {
        var text = Risk(RuntimeTd).GetRawText();
        Assert.Contains("is called exactly once, at first Runway entry", text);
        Assert.Contains("Later windows never call the materializer again", text);
    }

    [Fact]
    public void RuntimeDecision_AtomicityEnforcedBySharedTryCatch()
    {
        var text = Risk(RuntimeTd).GetRawText();
        Assert.Contains("wrapped by one shared try/catch", text);
        Assert.Contains("zero newly activated weeks and prior LifecycleStates entries fully preserved", text);
    }

    [Fact]
    public void RuntimeDecision_CalendarSimplificationDisclosed()
    {
        var text = Risk(RuntimeTd).GetRawText();
        Assert.Contains("an explicitly disclosed simplification, not a fabrication", text);
    }

    [Fact]
    public void RuntimeDecision_DeterminismReusesExistingStableGuidConvention()
    {
        var text = Risk(RuntimeTd).GetRawText();
        Assert.Contains("reusing the exact StableGuid convention already used by LongHorizonRollingCheckpointRuntime", text);
        Assert.Contains("no random GUID, no wall-clock read", text);
    }

    [Fact]
    public void RuntimeDecision_TestCountAndFullSuiteRecorded()
    {
        var text = Risk(RuntimeTd).GetRawText();
        Assert.Contains("41 new focused tests", text);
    }

    [Fact]
    public void RuntimeDecision_ZeroPublicPersistenceWiring()
    {
        var risk = Risk(RuntimeTd);
        Assert.Contains("Zero controller, DI registration, database entity/migration, or Flutter file touched", risk.GetRawText());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    [Fact]
    public void JitContextTd_CarriesPhase4K8Update()
    {
        var text = Risk(JitContextTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4K.8, 2026-08-03)", text);
        Assert.Equal("CLOSED", Risk(JitContextTd).GetProperty("status").GetString());
    }

    [Fact]
    public void ContractsTd_CarriesPhase4K8Update()
    {
        var text = Risk(ContractsTd).GetRawText();
        Assert.Contains("APPEND-ONLY UPDATE (Phase 4K.8, 2026-08-03)", text);
        Assert.Equal("CLOSED", Risk(ContractsTd).GetProperty("status").GetString());
    }

    [Fact]
    public void Redesign_RemainsOpenAndReferencesPhase4K8()
    {
        var risk = Risk(RedesignTd);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        var text = risk.GetRawText();
        Assert.Contains("UPDATE (Phase 4K.8):", text);
        Assert.Contains("Phase 4K.9", text);
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
        Assert.Contains("LONG_HORIZON_RUNWAY_AND_CORE_JIT_NUMERIC_RUNTIME_AND_MIXED_SEGMENT_WINDOW_ACTIVATION_COMPLETED_DARK", text);
        Assert.Contains("LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_API_AND_FLUTTER_REMAIN_UNCHANGED", text);
    }
}
