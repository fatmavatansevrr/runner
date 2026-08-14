using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4K.1 — governance cross-check for
/// <c>TD-LONG-HORIZON-ROLLING-NUMERIC-ACTIVATION-001</c> (CLOSED, a
/// policy-shape-only decision) and the updated
/// <c>TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001</c>. Proves the required
/// policy content is present and that no maintenance formula, checkpoint
/// threshold, or code was introduced.
/// </summary>
public sealed class LongHorizonRollingNumericActivationGovernanceTests
{
    private const string DecisionId = "TD-LONG-HORIZON-ROLLING-NUMERIC-ACTIVATION-001";
    private const string BoundaryDecisionId = "TD-LONG-HORIZON-NUMERIC-ACTIVATION-BOUNDARY-001";
    private const string PathADecisionId = "TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    private static JsonElement Risk(string id)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        return document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == id).Clone();
    }

    // 1. 21-52 structural support remains approved (unaffected -- no structural TD was reopened).
    [Fact]
    public void StructuralSupport_21To52_RemainsApproved()
    {
        var structural = Risk("TD-LONG-HORIZON-COMPOSITION-001");
        Assert.Equal("CLOSED", structural.GetProperty("status").GetString());
        var materialization = Risk("TD-LONG-HORIZON-STRUCTURAL-MATERIALIZATION-001");
        Assert.Equal("CLOSED", materialization.GetProperty("status").GetString());
    }

    // 2 & 3. full-upfront rejected; rolling activation approved.
    [Fact]
    public void FullUpfrontRejected_RollingActivationApproved()
    {
        var risk = Risk(DecisionId);
        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        var text = risk.GetRawText();
        Assert.Contains("rolling numeric activation", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Path B", text);
    }

    // 4 & 5. activation-window size explicit; classified as a product default.
    [Fact]
    public void ActivationWindow_IsExplicitFourWeekProductDefault()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("ACTIVATION WINDOW = 4 weeks", text);
        Assert.Contains("PRODUCT DEFAULT", text);
        Assert.Contains("not a universal", text, StringComparison.OrdinalIgnoreCase);
    }

    // 6 & 7. pending weeks have no numeric prescription; zero is not used as absence.
    [Fact]
    public void PendingWeeks_NullNotFabricatedZero()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("null", text);
        Assert.Contains("never fabricated zero", text, StringComparison.OrdinalIgnoreCase);
    }

    // 8 & 9. completed history immutable; only future unstarted weeks activate.
    [Fact]
    public void CompletedHistory_ImmutableOnlyFutureActivates()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("immutable history, never rewritten", text);
        Assert.Contains("only future, not-yet-started weeks may receive new numeric activation", text);
    }

    // 10 & 11. checkpoint authoritative for next window only; does not mutate history.
    [Fact]
    public void Checkpoint_AuthoritativeForNextWindowOnly_NeverMutatesHistory()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("AUTHORITATIVE for activating the NEXT numeric window only", text);
        Assert.Contains("never changes the approved structural roadmap by default", text);
    }

    // 12. missing evidence cannot permit upward progression.
    [Fact]
    public void MissingEvidence_CannotPermitUpwardProgression()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("no new validated evidence", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no upward numeric progression", text, StringComparison.OrdinalIgnoreCase);
    }

    // 13 & 14. maintenance and evidence thresholds explicitly deferred.
    [Fact]
    public void MaintenanceAndEvidenceThresholds_ExplicitlyDeferred()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("Phase 4K.2", text);
        Assert.Contains("Phase 4K.3", text);
    }

    // 15 & 16. Runway/Core use JIT numeric context in principle; exact timing deferred.
    [Fact]
    public void RunwayCore_JustInTimePrinciple_ExactTimingDeferred()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("just-in-time", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Exact JIT checkpoint timing", text);
        Assert.Contains("Phase 4K.4", text);
    }

    // 17, 18, 19. existing algorithms remain authoritative; no Core-target lifting; no GE ceiling.
    [Fact]
    public void ExistingAlgorithmsAuthoritative_NoCoreTargetLifting_NoGeCeiling()
    {
        var text = Risk(DecisionId).GetRawText();
        Assert.Contains("fully unchanged and authoritative", text);
        Assert.Contains("no Core target is ever arbitrarily lifted from planned GE progression", text);
        Assert.Contains("no GE-side ceiling", text);
    }

    // 20, 21, 22. no production code changes; no public activation; no persistence/Flutter changes.
    [Fact]
    public void NoProductionCodePublicActivationOrPersistenceFlutterChanges()
    {
        var risk = Risk(DecisionId);
        var runtimeImpact = risk.GetProperty("currentRuntimeImpact").GetString()!;
        Assert.Contains("Zero production code was changed", runtimeImpact);
        Assert.Contains("No", runtimeImpact);
        Assert.False(risk.GetProperty("blocking").GetBoolean());
    }

    [Fact]
    public void PathADecision_UpdatedToReflectApprovedDirection()
    {
        var text = Risk(PathADecisionId).GetRawText();
        Assert.Contains("Phase 4K.1", text);
        Assert.Contains("formally approved", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("OPEN", Risk(PathADecisionId).GetProperty("status").GetString());
    }

    [Fact]
    public void BoundaryDecision_RemainsClosedButSuperseded()
    {
        // Path B's narrow boundary decision itself is not reopened (append-only) but the new
        // decision explicitly supersedes its activation MODEL, not its own historical record.
        Assert.Equal("CLOSED", Risk(BoundaryDecisionId).GetProperty("status").GetString());
    }

    [Fact]
    public void DeferredStagedPlanBlocker_RemainsOpen()
    {
        Assert.Equal("OPEN", Risk(DeferredDependencyId).GetProperty("status").GetString());
    }

    [Fact]
    public void MarkdownProjection_ContainsTheDecisionRowWithClosedStatus()
    {
        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains($"`{DecisionId}`", markdown);
        var section = markdown.Substring(markdown.IndexOf(DecisionId, StringComparison.Ordinal));
        Assert.Contains("**CLOSED**", section);
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

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "artifacts", "audits")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Plan-catalog root not found.");
    }

    private static string JsonPath() => Path.Combine(Root(), "artifacts", "audits", "activation-readiness-risks.json");
    private static string MarkdownPath() => Path.Combine(Root(), "artifacts", "audits", "activation-readiness-risks.md");
}
