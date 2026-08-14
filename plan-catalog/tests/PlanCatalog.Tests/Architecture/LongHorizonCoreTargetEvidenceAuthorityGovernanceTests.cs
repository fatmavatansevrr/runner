using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

public sealed class LongHorizonCoreTargetEvidenceAuthorityGovernanceTests
{
    private const string DecisionId = "TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001";
    private const string RedesignId = "TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001";

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
    private static string DecisionPath() => Path.Combine(RepoRoot(), "PHASE4K_5A_CORE_WEEK_ONE_ROLLING_JIT_EVIDENCE_AUTHORITY_RESOLUTION.md");

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    [Fact]
    public void AuthorityDecision_IsClosedNonBlockingOptionA()
    {
        var risk = Risk(DecisionId);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        Assert.False(risk.GetProperty("blocking").GetBoolean());
        Assert.Contains("OPTION_A", risk.GetProperty("classification").GetString());
        Assert.Contains("OPTION A", risk.GetProperty("closureNote").GetString());
    }

    [Fact]
    public void DecisionDocument_HasExactlyTwentyEightRequiredSections()
    {
        var headings = File.ReadAllLines(DecisionPath()).Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();
        Assert.Equal(28, headings.Length);
        Assert.Equal("## 1. Executive result", headings[0]);
        Assert.Equal("## 28. Exact next phase", headings[^1]);
    }

    [Fact]
    public void CoreTargetSemantics_AreExplicitlyPrescriptionNotAchievedCapacity()
    {
        var text = File.ReadAllText(DecisionPath());
        Assert.Contains("planned starting prescription", text);
        Assert.Contains("or proof of achieved capacity", text);
        Assert.Contains("Runway converges toward or consolidates at this prescription", text);
    }

    [Fact]
    public void AllAuthorityCandidates_AreCompared()
    {
        var text = File.ReadAllText(DecisionPath());
        Assert.Contains("Original onboarding", text);
        Assert.Contains("Validated load directly equals target", text);
        Assert.Contains("Validated load through existing generator", text);
        Assert.Contains("Planned GE/Runway value", text);
        Assert.Contains("Bounded hybrid", text);
    }

    [Fact]
    public void CurrentProductionAndRollingAuthorities_AreNotConflated()
    {
        var closure = Risk(DecisionId).GetProperty("closureNote").GetString()!;
        Assert.Contains("LegacyCurrentProductionSource", closure);
        Assert.Contains("ProvenanceOnly", closure);
        Assert.Contains("ValidatedSustainableWeeklyVolumeKm", closure);
        Assert.DoesNotContain("onboarding fallback is approved", closure, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WeeklyLongRunAndFrequencyPrecedence_AreDeterministic()
    {
        var text = File.ReadAllText(DecisionPath());
        Assert.Contains("fresh current `ValidatedSustainableWeeklyVolumeKm`", text);
        Assert.Contains("prior still-valid validated checkpoint", text);
        Assert.Contains("JIT_VALIDATED_LONG_RUN_UNAVAILABLE", text);
        Assert.Contains("Completed history is the only permitted rolling source", text);
        Assert.Contains("no fractional-to-integer rounding policy", text);
    }

    [Fact]
    public void PlannedGeExit_IsNeverAuthoritativeAndOnboardingIsNotFallback()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("Planned GE exit is ProvenanceOnly", text);
        Assert.Contains("it is not a rolling fallback", text);
    }

    [Fact]
    public void MissingLongRunRuleAndExistingReasons_ArePreserved()
    {
        var text = File.ReadAllText(DecisionPath());
        Assert.Contains("Missing required long-run evidence blocks", text);
        Assert.Contains("JIT_EVIDENCE_CONFLICT_UNRESOLVED", text);
        Assert.Contains("JIT_SEGMENT_TRANSITION_INFEASIBLE", text);
        Assert.Contains("SAFETY_REASSESSMENT_REQUIRED", text);
        Assert.Contains("No new reason code is needed", text);
    }

    [Fact]
    public void NoNewFormulaPercentageOrBlendIsApproved()
    {
        var text = File.ReadAllText(DecisionPath());
        Assert.Contains("No progression percentage, convergence multiplier, uplift, reduction, blend weight", text);
        Assert.Contains("existing Core generator", text);
        Assert.Contains("remain unchanged", text);
        Assert.DoesNotContain("new convergence percentage", Risk(DecisionId).GetProperty("closureNote").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LockAndFutureOnlyVersioningRemainAuthoritative()
    {
        var text = File.ReadAllText(DecisionPath());
        Assert.Contains("remains immutable for that activated range", text);
        Assert.Contains("non-overlapping, not-yet-activated weeks", text);
        Assert.Contains("incompatibility blocks", text);
    }

    [Fact]
    public void PaceTargetTimeAndProfilesRemainIndependentOfLoadAuthority()
    {
        var text = File.ReadAllText(DecisionPath());
        Assert.Contains("RuntimeConditionResolutionService", text);
        Assert.Contains("product-average", text);
        Assert.Contains("recent-race", text);
        Assert.Contains("CONSISTENCY_NEEDED", text);
        Assert.Contains("CORE_ENTRY_READY", text);
        Assert.Contains("identical evidence precedence", text);
    }

    [Fact]
    public void RedesignTdRemainsOpenForRuntimeImplementation()
    {
        var risk = Risk(RedesignId);
        Assert.Equal("OPEN", risk.GetProperty("status").GetString());
        Assert.Contains("Phase 4K.5A", risk.GetRawText());
        Assert.Contains("rolling numeric runtime", risk.GetRawText());
    }

    [Fact]
    public void AggregateAndMarkdownProjectionMatchClosure()
    {
        var json = File.ReadAllText(JsonPath());
        var markdown = File.ReadAllText(MarkdownPath());
        var row = markdown.Split('\n').Single(line => line.StartsWith($"| `{DecisionId}`", StringComparison.Ordinal));
        Assert.EndsWith("**CLOSED** |", row.TrimEnd('\r'));
    }

    [Fact]
    public void NoRuntimeApiPersistenceOrFlutterWiringReferencesRollingAuthority()
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
            .Where(path => File.ReadAllText(path).Contains("CoreWeekOneRollingAuthority", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(hits);
    }
}
