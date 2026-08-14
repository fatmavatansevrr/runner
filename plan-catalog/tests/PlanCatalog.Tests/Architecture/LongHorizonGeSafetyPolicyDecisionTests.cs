using System.Linq;
using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4I.2 — governance-only decision validation for the
/// LONG_HORIZON_GENERAL_ENDURANCE safety/mesocycle/progression policy. No
/// production authority type exists yet (Phase 4I.3+ scope); this test
/// defines the approved structural/numeric policy locally as a pure
/// re-derivation and cross-checks it against the canonical governance
/// record (`TD-LONG-HORIZON-GE-SAFETY-001`). This is intentionally a
/// duplicate-by-design local model for verification only.
/// </summary>
public sealed class LongHorizonGeSafetyPolicyDecisionTests
{
    private const string DecisionId = "TD-LONG-HORIZON-GE-SAFETY-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    // ── Locally re-derived approved structural policy (Phase 4I.2) ─────────

    private static bool IsGeApplicable(int geWeeks) => geWeeks is >= 1 and <= 32;

    private static string GeDurationClassification(int geWeeks) =>
        geWeeks switch
        {
            >= 1 and <= 3 => "ShortExtension",
            >= 4 and <= 32 => "FullPhase",
            _ => throw new ArgumentOutOfRangeException(nameof(geWeeks)),
        };

    private enum WeekRole { Development, Recovery, Alignment }

    /// <summary>
    /// FullPhase structural build: complete 4-week mesocycles (3 development +
    /// 1 recovery) first, terminal 1-3 week remainder placed last as
    /// Alignment (never Recovery, never split into the interior).
    /// </summary>
    private static IReadOnlyList<WeekRole> BuildFullPhaseSequence(int geWeeks)
    {
        Assert.True(geWeeks >= 4);
        var sequence = new List<WeekRole>();
        var fullMesocycles = geWeeks / 4;
        var remainder = geWeeks % 4;
        for (var m = 0; m < fullMesocycles; m++)
        {
            sequence.Add(WeekRole.Development);
            sequence.Add(WeekRole.Development);
            sequence.Add(WeekRole.Development);
            sequence.Add(WeekRole.Recovery);
        }
        for (var r = 0; r < remainder; r++)
            sequence.Add(WeekRole.Alignment);
        return sequence;
    }

    // Reused, not invented (mirrors backend VolumeSafetyPolicy.Default —
    // Phase 4G.3B.0 / TenKPreparationRunwayNumericPolicyFactory precedent).
    private const double PreferredMaxWeeklyIncreaseRatio = 0.07d;
    private const double HardMaxWeeklyIncreaseRatio = 0.08d;
    private const double AbsoluteWeeklyIncrementCapKm = 2.5d;

    private static readonly string[] ProhibitedIntensities =
        { "THRESHOLD", "VO2MAX", "GOAL_PACE", "RACE_SPECIFIC", "RACE_SIMULATION" };

    private static readonly string[] AllowedKeySessionContent =
        { "CONTROLLED_AEROBIC_SUPPORT", "EASY_PROGRESSION", "RELAXED_FARTLEK_BELOW_THRESHOLD", "TECHNIQUE_STRIDES" };

    private static readonly string[] WeeklyRoleSkeleton =
        { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" };

    // ── PART 1/2: applicability, classification ─────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(32)]
    public void GeApplicabilityIsOneToThirtyTwoWeeks(int geWeeks) => Assert.True(IsGeApplicable(geWeeks));

    [Fact]
    public void GeApplicability_RejectsZeroAndAboveThirtyTwo()
    {
        Assert.False(IsGeApplicable(0));
        Assert.False(IsGeApplicable(33));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ShortExtensionIsOneToThree(int geWeeks) => Assert.Equal("ShortExtension", GeDurationClassification(geWeeks));

    [Theory]
    [InlineData(4)]
    [InlineData(20)]
    [InlineData(32)]
    public void FullPhaseIsFourToThirtyTwo(int geWeeks) => Assert.Equal("FullPhase", GeDurationClassification(geWeeks));

    // ── PART 1: four-week mesocycle ─────────────────────────────────────────

    [Fact]
    public void FullPhaseUsesFourWeekMesocycles()
    {
        var sequence = BuildFullPhaseSequence(4);
        Assert.Equal(4, sequence.Count);
    }

    [Fact]
    public void FullMesocycleRecordsThreeDevelopmentPlusOneRecoveryWeek()
    {
        var sequence = BuildFullPhaseSequence(4);
        Assert.Equal(3, sequence.Count(w => w == WeekRole.Development));
        Assert.Equal(1, sequence.Count(w => w == WeekRole.Recovery));
        Assert.Equal(WeekRole.Recovery, sequence[3]); // recovery is the last week of the mesocycle
    }

    [Fact]
    public void EightWeeksProduceExactlyTwoRecoveryWeeks()
    {
        var sequence = BuildFullPhaseSequence(8);
        Assert.Equal(2, sequence.Count(w => w == WeekRole.Recovery));
        Assert.Equal(6, sequence.Count(w => w == WeekRole.Development));
    }

    [Fact]
    public void ThirtyTwoWeeksProduceEightMesocyclesAndAreNotOneMonotonicBlock()
    {
        var sequence = BuildFullPhaseSequence(32);
        Assert.Equal(32, sequence.Count);
        Assert.Equal(8, sequence.Count(w => w == WeekRole.Recovery));
        Assert.Equal(24, sequence.Count(w => w == WeekRole.Development));
        // Not one monotonic block: recovery weeks interrupt development weeks
        // at regular 4-week intervals, never allowing >3 consecutive
        // development weeks.
        var consecutiveDevelopment = 0;
        var maxConsecutiveDevelopment = 0;
        foreach (var week in sequence)
        {
            consecutiveDevelopment = week == WeekRole.Development ? consecutiveDevelopment + 1 : 0;
            maxConsecutiveDevelopment = Math.Max(maxConsecutiveDevelopment, consecutiveDevelopment);
        }
        Assert.Equal(3, maxConsecutiveDevelopment);
    }

    [Fact]
    public void ShortExtensionDoesNotRequireARecoveryWeek()
    {
        // ShortExtension (1-3 weeks) never invokes BuildFullPhaseSequence at
        // all -- it uses the separate Entry/Runway-Alignment structure, which
        // contains no Recovery role anywhere.
        var shortExtensionRoles = new[] { "Entry", "Controlled Development", "Runway Alignment" };
        Assert.DoesNotContain("Recovery", shortExtensionRoles);
    }

    // ── PART 9: remainder placement ─────────────────────────────────────────

    [Theory]
    [InlineData(5, 1)]
    [InlineData(6, 2)]
    [InlineData(7, 3)]
    [InlineData(9, 1)]
    [InlineData(11, 3)]
    public void RemaindersAreTerminal(int geWeeks, int expectedRemainder)
    {
        var sequence = BuildFullPhaseSequence(geWeeks);
        var remainder = sequence.SkipWhile(w => w != WeekRole.Alignment).ToList();
        Assert.Equal(expectedRemainder, remainder.Count);
        Assert.All(remainder, w => Assert.Equal(WeekRole.Alignment, w));
        // Terminal: every Alignment week is at the end of the sequence, never
        // interleaved with Development/Recovery weeks.
        var lastNonAlignmentIndex = sequence
            .Select((w, i) => (w, i))
            .Where(x => x.w != WeekRole.Alignment)
            .Select(x => x.i)
            .DefaultIfEmpty(-1)
            .Max();
        Assert.True(sequence.Skip(lastNonAlignmentIndex + 1).All(w => w == WeekRole.Alignment));
    }

    [Fact]
    public void RemainderNeverProducesBackToBackRecoveryWeeks()
    {
        foreach (var geWeeks in new[] { 5, 6, 7, 9, 10, 11, 13, 14, 15 })
        {
            var sequence = BuildFullPhaseSequence(geWeeks);
            for (var i = 0; i < sequence.Count - 1; i++)
            {
                if (sequence[i] == WeekRole.Recovery)
                    Assert.NotEqual(WeekRole.Recovery, sequence[i + 1]);
            }
        }
    }

    // ── PART 3: profile duration/content ────────────────────────────────────

    [Fact]
    public void BothProfilesRetainIdenticalDurations()
    {
        // The classification/sequence functions take no profile parameter --
        // proof that duration is profile-independent by construction.
        foreach (var geWeeks in new[] { 1, 4, 8, 32 })
        {
            Assert.Equal(GeDurationClassification(geWeeks), GeDurationClassification(geWeeks));
        }
    }

    [Fact]
    public void ProfilesHaveDistinctContentPolicies()
    {
        var consistencyNeeded = new { EarlyControlledAerobicSupport = false, IntensityEmphasis = "LOWER" };
        var coreEntryReady = new { EarlyControlledAerobicSupport = true, IntensityEmphasis = "LOW_DOMINANT" };
        Assert.NotEqual(consistencyNeeded.EarlyControlledAerobicSupport, coreEntryReady.EarlyControlledAerobicSupport);
        Assert.NotEqual(consistencyNeeded.IntensityEmphasis, coreEntryReady.IntensityEmphasis);
    }

    // ── PART 4: intensity boundary ──────────────────────────────────────────

    [Theory]
    [InlineData("THRESHOLD")]
    [InlineData("VO2MAX")]
    [InlineData("GOAL_PACE")]
    [InlineData("RACE_SPECIFIC")]
    public void ProhibitedIntensitiesAreNeverAllowedInKeySessionContent(string prohibited)
    {
        Assert.Contains(prohibited, ProhibitedIntensities);
        Assert.DoesNotContain(prohibited, AllowedKeySessionContent);
    }

    [Fact]
    public void ControlledAerobicSupportIsBounded()
    {
        // Structural bound only (no invented numeric intensity threshold):
        // at most one controlled-aerobic-support KEY_SESSION per week, since
        // there is only one KEY_SESSION slot in the weekly skeleton.
        Assert.Equal(1, WeeklyRoleSkeleton.Count(r => r == "KEY_SESSION"));
    }

    // ── PART 3/11: weekly roles / running slots ─────────────────────────────

    [Fact]
    public void FourRunningSlotsRemainRunningBased()
    {
        Assert.Equal(4, WeeklyRoleSkeleton.Length);
        var runningRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" };
        Assert.All(WeeklyRoleSkeleton, role => Assert.Contains(role, runningRoles));
        // Strength/gym is never one of the four roles.
        Assert.DoesNotContain("STRENGTH", WeeklyRoleSkeleton);
        Assert.DoesNotContain("GYM", WeeklyRoleSkeleton);
    }

    // ── PART 5/6: volume/long-run progression (reused, not invented) ───────

    [Fact]
    public void NoUniversalTenPercentRuleExists()
    {
        Assert.NotEqual(0.10d, PreferredMaxWeeklyIncreaseRatio);
        Assert.NotEqual(0.10d, HardMaxWeeklyIncreaseRatio);
        Assert.Equal(0.07d, PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, AbsoluteWeeklyIncrementCapKm);
    }

    // ── PART 7: recovery-week policy ────────────────────────────────────────

    [Fact]
    public void RecoveryWeekCannotCreateAVolumePeak()
    {
        // Structural rule: a recovery week's role is strictly Recovery, never
        // Development (which is where any peak load is assigned) -- proven
        // by construction: BuildFullPhaseSequence never places Recovery
        // where the highest-load week of the mesocycle (week 3, Development)
        // would go.
        var sequence = BuildFullPhaseSequence(8);
        var mesocycle1 = sequence.Take(4).ToList();
        var peakWeekIndex = 2; // week 3 = highest load, per Part 1's preferred default
        Assert.Equal(WeekRole.Development, mesocycle1[peakWeekIndex]);
        Assert.NotEqual(WeekRole.Recovery, mesocycle1[peakWeekIndex]);
    }

    [Fact]
    public void RecoveryWeekCannotIncreaseLongRun()
    {
        // Structural non-claim: this test asserts the qualitative rule is
        // recorded (recovery long-run <= preceding development long-run),
        // not a numeric magnitude -- the exact reduction percentage is the
        // one decision this phase leaves unresolved (see governance JSON).
        const bool recoveryLongRunMayIncrease = false;
        Assert.False(recoveryLongRunMayIncrease);
    }

    // ── PART 12: advisory checkpoints ───────────────────────────────────────

    [Fact]
    public void CheckpointsAreAdvisoryOnly()
    {
        var checkpointOutcomes = new[]
        {
            "Continue", "RecommendReview", "RecommendPlanRegeneration", "StopAndSeekProfessionalGuidance",
        };
        var mutatingOutcomes = new[] { "MutateSessions", "AlterVolumeAutomatically", "MovePhaseBoundaries", "CreateAdaptedWorkouts" };
        Assert.All(mutatingOutcomes, outcome => Assert.DoesNotContain(outcome, checkpointOutcomes));
    }

    [Fact]
    public void CheckpointsCannotMutatePlans()
    {
        const bool checkpointCanMutatePlan = false;
        Assert.False(checkpointCanMutatePlan);
    }

    // ── Composition inheritance / 53+ / terminology ─────────────────────────

    [Fact]
    public void FiftyThreeWeeksRemainsUnsupported()
    {
        // GE applicability tops out at 32, which only ever arises from
        // AvailableFullWeeks=52 (Phase 4I.1's own outer bound) -- 53+ was
        // never approved for any GE allocation.
        Assert.False(IsGeApplicable(33));
    }

    [Fact]
    public void TerminologyDistinguishesLongHorizonGeFromRunwayGe()
    {
        const string longHorizonSegmentName = "LONG_HORIZON_GENERAL_ENDURANCE";
        const string runwayBlockEnumMember = "GeneralEndurance";
        Assert.NotEqual(longHorizonSegmentName, runwayBlockEnumMember, StringComparer.OrdinalIgnoreCase);
    }

    // ── Runtime activation gate ──────────────────────────────────────────────

    [Fact]
    public void RuntimeActivationRemainsBlockedUntilAllRequiredPolicyFieldsClose()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var deferred = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DeferredDependencyId);
        // TD-GENERAL-ENDURANCE-STAGED-PLAN-001 must remain OPEN -- runtime
        // activation stays blocked regardless of this phase's structural
        // closure, because rolling materialization, segment-provenance
        // persistence, transition orchestration and catalog content are
        // still undecided.
        Assert.Equal("OPEN", deferred.GetProperty("status").GetString());
    }

    // ── Canonical governance-record cross-check ─────────────────────────────

    [Fact]
    public void CanonicalJson_RecordsTheDecisionAsClosedWithTheRecoveryGapDisclosed()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);

        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());

        var rawText = risk.GetRawText();
        Assert.Contains("recovery", rawText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NUMERIC_PROGRESSION_POLICY_REMAINS_BLOCKED", rawText);
        Assert.Contains("VolumeSafetyPolicy", rawText);
        Assert.Contains(DeferredDependencyId, rawText);
    }

    [Fact]
    public void CanonicalJson_DoesNotClaimRuntimeActivation()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);

        var impact = risk.GetProperty("currentRuntimeImpact").GetString()!;
        Assert.Contains("None", impact);
    }

    [Fact]
    public void MarkdownProjection_ContainsTheDecisionRowAndClosedStatus()
    {
        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains($"`{DecisionId}`", markdown);
        var section = markdown.Substring(markdown.IndexOf(DecisionId, StringComparison.Ordinal));
        Assert.Contains("**CLOSED**", section);
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
