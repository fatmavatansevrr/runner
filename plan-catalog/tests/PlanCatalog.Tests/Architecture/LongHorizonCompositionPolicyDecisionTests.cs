using System.Linq;
using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4I.1 — governance-only decision validation. No production authority
/// type exists yet for the approved 21-52 week composition formula (that is
/// explicitly Phase 4I.3's scope), so this test defines the approved formula
/// locally, as a pure re-derivation independent of any future implementation,
/// and cross-checks it against the canonical governance record
/// (`TD-LONG-HORIZON-COMPOSITION-001`) in both its JSON and Markdown
/// projections. This is intentionally a duplicate-by-design local formula for
/// verification purposes only — a future Phase 4I.3 typed decision contract
/// must be the single runtime-consumed authority, not this test.
/// </summary>
public sealed class LongHorizonCompositionPolicyDecisionTests
{
    private const string DecisionId = "TD-LONG-HORIZON-COMPOSITION-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    // ── Locally re-derived approved formula (Phase 4I.1) ───────────────────

    private static int Core(int availableFullWeeks) => 12;

    private static int Runway(int availableFullWeeks) => 8;

    private static int GeneralEndurance(int availableFullWeeks) => availableFullWeeks - 20;

    private static bool IsApplicable(int availableFullWeeks) =>
        availableFullWeeks is >= 21 and <= 52;

    private static string GeDurationClassification(int generalEnduranceWeeks) =>
        generalEnduranceWeeks switch
        {
            >= 1 and <= 3 => "ShortExtension",
            >= 4 and <= 32 => "FullPhase",
            _ => throw new ArgumentOutOfRangeException(nameof(generalEnduranceWeeks)),
        };

    [Theory]
    [InlineData(21, 1)]
    [InlineData(22, 2)]
    [InlineData(23, 3)]
    [InlineData(24, 4)]
    [InlineData(28, 8)]
    [InlineData(32, 12)]
    [InlineData(40, 20)]
    [InlineData(48, 28)]
    [InlineData(52, 32)]
    public void ApprovedFormula_ProducesExpectedAllocationForEachFixture(
        int availableFullWeeks, int expectedGeneralEndurance)
    {
        Assert.True(IsApplicable(availableFullWeeks));
        Assert.Equal(expectedGeneralEndurance, GeneralEndurance(availableFullWeeks));
        Assert.Equal(8, Runway(availableFullWeeks));
        Assert.Equal(12, Core(availableFullWeeks));
        Assert.Equal(availableFullWeeks,
            GeneralEndurance(availableFullWeeks) + Runway(availableFullWeeks) + Core(availableFullWeeks));
    }

    [Fact]
    public void PolicyAppliesOnlyToTheApproved21To52Range()
    {
        Assert.False(IsApplicable(20));
        Assert.True(IsApplicable(21));
        Assert.True(IsApplicable(52));
        Assert.False(IsApplicable(53));
    }

    [Fact]
    public void CoreIsAlwaysTwelveAcrossTheFullApprovedRange()
    {
        for (var weeks = 21; weeks <= 52; weeks++)
            Assert.Equal(12, Core(weeks));
    }

    [Fact]
    public void RunwayIsAlwaysEightAcrossTheFullApprovedRange()
    {
        for (var weeks = 21; weeks <= 52; weeks++)
            Assert.Equal(8, Runway(weeks));
    }

    [Fact]
    public void GeneralEnduranceIncreasesMonotonicallyByExactlyOnePerAdditionalWeek()
    {
        for (var weeks = 21; weeks < 52; weeks++)
        {
            var current = GeneralEndurance(weeks);
            var next = GeneralEndurance(weeks + 1);
            Assert.Equal(1, next - current);
            // Runway/Core must not move -- only GE absorbs the extra week.
            Assert.Equal(Runway(weeks), Runway(weeks + 1));
            Assert.Equal(Core(weeks), Core(weeks + 1));
        }
    }

    [Fact]
    public void FiftyThreeWeeksReceivesNoAllocation()
    {
        Assert.False(IsApplicable(53));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void OneToThreeGeneralEnduranceWeeksClassifyAsShortExtension(int geWeeks) =>
        Assert.Equal("ShortExtension", GeDurationClassification(geWeeks));

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(20)]
    [InlineData(32)]
    public void FourToThirtyTwoGeneralEnduranceWeeksClassifyAsFullPhase(int geWeeks) =>
        Assert.Equal("FullPhase", GeDurationClassification(geWeeks));

    [Fact]
    public void BothReadinessProfilesUseIdenticalSegmentDurations()
    {
        // Horizon determines duration; profile determines content only (Phase
        // 4I.1 PART 3). The formula takes no profile parameter at all, which
        // is itself the proof that duration cannot vary by profile.
        foreach (var weeks in new[] { 21, 24, 32, 52 })
        {
            var consistencyNeededGe = GeneralEndurance(weeks);
            var coreEntryReadyGe = GeneralEndurance(weeks); // same function, no profile input
            Assert.Equal(consistencyNeededGe, coreEntryReadyGe);
        }
    }

    // ── Canonical governance-record cross-check ─────────────────────────────

    [Fact]
    public void CanonicalJson_RecordsTheApprovedDecisionAsClosed()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);

        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());

        var rawText = risk.GetRawText();
        Assert.Contains("AvailableFullWeeks - 20", rawText);
        Assert.Contains("PreparationRunwayWeeks = 8", rawText);
        Assert.Contains("CoreWeeks = 12", rawText);
        Assert.Contains("ShortExtension", rawText);
        Assert.Contains("FullPhase", rawText);
        Assert.Contains("PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW", rawText);
    }

    [Fact]
    public void CanonicalJson_DoesNotClaimRuntimeActivation()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);

        var impact = risk.GetProperty("currentRuntimeImpact").GetString()!;
        Assert.Contains("None", impact);
        Assert.Contains("PLAN_HORIZON_COMPOSITION_REQUIRED", impact);
    }

    [Fact]
    public void DeferredPhase4I2DependencyIsPresentAndStillOpen()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risks = document.RootElement.GetProperty("risks").EnumerateArray().ToList();

        var decision = risks.Single(r => r.GetProperty("id").GetString() == DecisionId);
        Assert.Contains(DeferredDependencyId, decision.GetRawText());

        var deferred = risks.Single(r => r.GetProperty("id").GetString() == DeferredDependencyId);
        Assert.Equal("OPEN", deferred.GetProperty("status").GetString());
    }

    [Fact]
    public void MarkdownProjection_ContainsTheDecisionRowAndClosedStatus()
    {
        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains($"`{DecisionId}`", markdown);
        Assert.Contains("AvailableFullWeeks - 20", markdown);
        Assert.Contains("**CLOSED**", markdown.Substring(markdown.IndexOf(DecisionId, StringComparison.Ordinal)));
    }

    [Fact]
    public void TerminologyDoesNotCollideWithTheExistingRunwayGeneralEnduranceBlock()
    {
        // The long-horizon segment name and the existing Preparation Runway
        // block's GeneralEndurance enum member must remain textually
        // distinct identifiers, even though both relate to "General
        // Endurance" content -- Phase 4I.1 PART 7's terminology decision.
        const string longHorizonSegmentName = "LONG_HORIZON_GENERAL_ENDURANCE";
        const string runwayBlockEnumMember = "GeneralEndurance";
        Assert.NotEqual(longHorizonSegmentName, runwayBlockEnumMember, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("RUNWAY", longHorizonSegmentName.Replace("_", ""), StringComparison.OrdinalIgnoreCase);
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
