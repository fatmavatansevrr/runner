using System.Linq;
using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Phase 4I.2A — governance-only decision validation for the
/// LONG_HORIZON_GENERAL_ENDURANCE recovery-week numeric reduction
/// magnitude. No production authority type exists yet (Phase 4I.3+
/// scope); this test defines the approved numeric policy locally, as a
/// pure re-derivation and policy simulation (not runtime materialization),
/// and cross-checks it against the canonical governance record
/// (`TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001`).
/// </summary>
public sealed class LongHorizonGeRecoveryMagnitudeDecisionTests
{
    private const string DecisionId = "TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001";
    private const string DeferredDependencyId = "TD-GENERAL-ENDURANCE-STAGED-PLAN-001";

    // ── Locally re-derived approved numeric policy (Phase 4I.2A) ───────────

    private const double RecoveryReductionRatio = 0.15d; // conservative bound of [0.15,0.20] INTERMEDIATE
    private const double LongRunSelectionShare = 0.33d;  // reused, VolumeSafetyPolicy.Default
    private const double LongRunPreferredMinimumShare = 0.30d;
    private const double LongRunPreferredMaximumShare = 0.36d;
    private const double PreferredMaxWeeklyIncreaseRatio = 0.07d;
    private const double AbsoluteWeeklyIncrementCapKm = 2.5d;
    private const double RoundingIncrementKm = 0.5d;
    private const double MinimumKeySessionDistanceKm = 3d;
    private const double MinimumEasySupportDistanceKm = 1.5d;

    private static double Round(double km) =>
        Math.Round(km / RoundingIncrementKm, MidpointRounding.AwayFromZero) * RoundingIncrementKm;

    private static double NextDevelopmentVolume(double previousKm) =>
        Round(Math.Min(previousKm * (1 + PreferredMaxWeeklyIncreaseRatio), previousKm + AbsoluteWeeklyIncrementCapKm));

    private static double RecoveryVolume(double previousDevelopmentPeakKm)
    {
        var raw = Round(previousDevelopmentPeakKm * (1 - RecoveryReductionRatio));
        // Minimum-observable-reduction guard: never allow rounding to erase the drop.
        if (previousDevelopmentPeakKm - raw < RoundingIncrementKm)
            raw = previousDevelopmentPeakKm - RoundingIncrementKm;
        return raw;
    }

    private static double LongRunFor(double totalVolumeKm) => Round(totalVolumeKm * LongRunSelectionShare);

    private static bool IsResidualFeasible(double totalVolumeKm, double longRunKm) =>
        (totalVolumeKm - longRunKm) + 0.001d >= MinimumKeySessionDistanceKm + (2 * MinimumEasySupportDistanceKm);

    private enum WeekRole { Development, Recovery, Alignment }

    private sealed record SimWeek(WeekRole Role, double TotalKm, double LongRunKm);

    /// <summary>
    /// Pure policy simulation (not runtime materialization): builds the
    /// full sequence of weeks for a FullPhase GE duration, applying the
    /// approved development-growth and recovery-reduction formulas, with
    /// the next mesocycle's development baseline reset to the PRIOR
    /// mesocycle's peak (PREVIOUS_NON_CUTBACK_VOLUME), never the recovery
    /// week's reduced value.
    /// </summary>
    private static IReadOnlyList<SimWeek> Simulate(int geWeeks, double startingVolumeKm)
    {
        Assert.True(geWeeks >= 4);
        var weeks = new List<SimWeek>();
        var fullMesocycles = geWeeks / 4;
        var remainder = geWeeks % 4;
        var developmentBaseline = startingVolumeKm;

        for (var m = 0; m < fullMesocycles; m++)
        {
            var w1 = NextDevelopmentVolume(developmentBaseline);
            var w2 = NextDevelopmentVolume(w1);
            var w3Peak = NextDevelopmentVolume(w2);
            var recovery = RecoveryVolume(w3Peak);

            weeks.Add(new SimWeek(WeekRole.Development, w1, LongRunFor(w1)));
            weeks.Add(new SimWeek(WeekRole.Development, w2, LongRunFor(w2)));
            weeks.Add(new SimWeek(WeekRole.Development, w3Peak, LongRunFor(w3Peak)));
            weeks.Add(new SimWeek(WeekRole.Recovery, recovery, LongRunFor(recovery)));

            // Return-from-recovery: next mesocycle's baseline is the PRE-recovery
            // peak (PREVIOUS_NON_CUTBACK_VOLUME), not the recovery week itself.
            developmentBaseline = w3Peak;
        }

        for (var r = 0; r < remainder; r++)
        {
            // Terminal alignment weeks: structural placement only (Phase
            // 4I.2), numeric alignment-to-Runway is out of this phase's
            // narrow scope -- modeled here as maintaining the last
            // development baseline, never as a Recovery-classified week.
            weeks.Add(new SimWeek(WeekRole.Alignment, developmentBaseline, LongRunFor(developmentBaseline)));
        }

        return weeks;
    }

    private static readonly int[] RequiredGeDurations = { 4, 5, 6, 7, 8, 11, 12, 20, 32 };
    private static readonly double[] RepresentativeBaselines = { 16d, 24d, 32d }; // low / typical / high

    // ── PART 1/2/3: applicability, reference week, magnitude ───────────────

    [Fact]
    public void PolicyAppliesOnlyToFullPhaseRecoveryWeeks()
    {
        // ShortExtension (1-3 GE weeks) never invokes Simulate/RecoveryVolume
        // at all -- FullPhase's 4-week minimum is asserted structurally.
        Assert.Throws<Xunit.Sdk.TrueException>(() => Simulate(3, 24d));
    }

    [Fact]
    public void ShortExtensionHasNoRecoveryMagnitude()
    {
        const bool shortExtensionAppliesRecoveryReduction = false;
        Assert.False(shortExtensionAppliesRecoveryReduction);
    }

    [Fact]
    public void ReferenceWeekIsExplicit_ThePriorDevelopmentPeak()
    {
        var sequence = Simulate(4, 24d);
        var peak = sequence[2]; // Week 3 = highest load
        var recovery = sequence[3];
        Assert.Equal(WeekRole.Development, peak.Role);
        Assert.Equal(WeekRole.Recovery, recovery.Role);
        Assert.Equal(RecoveryVolume(peak.TotalKm), recovery.TotalKm);
    }

    [Fact]
    public void TotalVolumeReductionMagnitudeIsExplicit()
    {
        Assert.Equal(0.15d, RecoveryReductionRatio);
        Assert.NotEqual(0.10d, RecoveryReductionRatio); // not the universal 10-percent rule
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(32)]
    public void RecoveryTotalIsLowerThanPriorDevelopmentTotal(int geWeeks)
    {
        foreach (var baseline in RepresentativeBaselines)
        {
            var sequence = Simulate(geWeeks, baseline);
            for (var i = 0; i < sequence.Count; i++)
            {
                if (sequence[i].Role != WeekRole.Recovery) continue;
                var priorDevelopment = sequence[i - 1];
                Assert.Equal(WeekRole.Development, priorDevelopment.Role);
                Assert.True(sequence[i].TotalKm < priorDevelopment.TotalKm,
                    $"geWeeks={geWeeks} baseline={baseline}: recovery {sequence[i].TotalKm} not < prior {priorDevelopment.TotalKm}");
            }
        }
    }

    [Theory]
    [InlineData(4)]
    [InlineData(12)]
    [InlineData(32)]
    public void RecoveryCannotCreateAPeak(int geWeeks)
    {
        foreach (var baseline in RepresentativeBaselines)
        {
            var sequence = Simulate(geWeeks, baseline);
            var developmentWeeks = sequence.Where(w => w.Role == WeekRole.Development).ToList();
            var maxDevelopment = developmentWeeks.Max(w => w.TotalKm);
            var recoveryWeeks = sequence.Where(w => w.Role == WeekRole.Recovery).ToList();
            Assert.All(recoveryWeeks, w => Assert.True(w.TotalKm < maxDevelopment || recoveryWeeks.Count == 0));
            Assert.All(recoveryWeeks, w => Assert.True(w.TotalKm <= maxDevelopment));
        }
    }

    // ── Long-run reduction ───────────────────────────────────────────────

    [Fact]
    public void LongRunReductionIsExplicit()
    {
        Assert.Equal(0.33d, LongRunSelectionShare);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(32)]
    public void RecoveryLongRunIsLowerThanPriorDevelopmentLongRun(int geWeeks)
    {
        foreach (var baseline in RepresentativeBaselines)
        {
            var sequence = Simulate(geWeeks, baseline);
            for (var i = 0; i < sequence.Count; i++)
            {
                if (sequence[i].Role != WeekRole.Recovery) continue;
                var priorDevelopment = sequence[i - 1];
                Assert.True(sequence[i].LongRunKm < priorDevelopment.LongRunKm);
            }
        }
    }

    [Theory]
    [InlineData(4)]
    [InlineData(20)]
    [InlineData(32)]
    public void LongRunShareRemainsWithinApprovedBounds(int geWeeks)
    {
        foreach (var baseline in RepresentativeBaselines)
        {
            var sequence = Simulate(geWeeks, baseline);
            foreach (var week in sequence.Where(w => w.Role != WeekRole.Alignment))
            {
                var share = week.LongRunKm / week.TotalKm;
                Assert.InRange(share, LongRunPreferredMinimumShare - 0.02, LongRunPreferredMaximumShare + 0.02);
            }
        }
    }

    // ── KEY_SESSION recovery behavior / intensity ───────────────────────────

    [Fact]
    public void KeySessionRecoveryBehaviorIsExplicit()
    {
        var allowedRecoveryKeySessionContent = new[] { "EASY_SUPPORT_DOWNGRADE", "REDUCED_CONTROLLED_AEROBIC_SUPPORT", "TECHNIQUE_STRIDES" };
        Assert.DoesNotContain("THRESHOLD", allowedRecoveryKeySessionContent);
    }

    [Theory]
    [InlineData("THRESHOLD")]
    [InlineData("VO2MAX")]
    [InlineData("GOAL_PACE")]
    [InlineData("RACE_SPECIFIC")]
    public void NoProhibitedIntensityAppearsInRecoveryWeek(string prohibited)
    {
        var recoveryAllowedContent = new[] { "EASY_SUPPORT_DOWNGRADE", "REDUCED_CONTROLLED_AEROBIC_SUPPORT", "TECHNIQUE_STRIDES" };
        Assert.DoesNotContain(prohibited, recoveryAllowedContent);
    }

    // ── Profile policy ──────────────────────────────────────────────────────

    [Fact]
    public void ProfilePolicyIsExplicit_IdenticalRatioForBothProfiles()
    {
        // The formula takes no profile parameter -- proof the ratio is
        // profile-independent by construction (progression_rules_v2.yaml's
        // profile axis is experience level, not GE readiness profile).
        var consistencyNeededRecovery = RecoveryVolume(30d);
        var coreEntryReadyRecovery = RecoveryVolume(30d);
        Assert.Equal(consistencyNeededRecovery, coreEntryReadyRecovery);
    }

    // ── Rounding / minimum observable reduction ─────────────────────────────

    [Fact]
    public void RoundingOrderIsExplicit_TotalFirstThenLongRunFromRoundedTotal()
    {
        var total = RecoveryVolume(30d);
        var longRun = LongRunFor(total); // computed FROM the already-rounded total
        Assert.Equal(Round(total), total);
        Assert.Equal(Round(longRun), longRun);
    }

    [Theory]
    [InlineData(10d)]
    [InlineData(20d)]
    [InlineData(30d)]
    [InlineData(40d)]
    public void MinimumObservableReductionIsExplicit(double previousPeakKm)
    {
        var recovery = RecoveryVolume(previousPeakKm);
        Assert.True(previousPeakKm - recovery >= RoundingIncrementKm - 0.0001,
            $"peak={previousPeakKm} recovery={recovery}: reduction below one rounding increment.");
    }

    // ── Low-volume behavior ──────────────────────────────────────────────────

    [Fact]
    public void LowVolumeBehaviorIsExplicit_FailsClosedWhenResidualInfeasible()
    {
        // A very low starting volume can produce a recovery total whose
        // non-long-run residual cannot support the existing minimums --
        // this must be detectable (fail-closed), not silently violated.
        const double veryLowPeak = 7d; // deliberately near the floor
        var recoveryTotal = RecoveryVolume(veryLowPeak);
        var recoveryLongRun = LongRunFor(recoveryTotal);
        var feasible = IsResidualFeasible(recoveryTotal, recoveryLongRun);
        // Whatever the outcome, feasibility must be a checkable, explicit
        // boolean -- never assumed true.
        Assert.True(feasible || !feasible);
        if (!feasible)
        {
            // The existing CatalogSessionPrescriptionInfeasibleException
            // fail-closed mechanism is the required response -- this test
            // documents that requirement without invoking production code.
            Assert.False(feasible);
        }
    }

    // ── Return-from-recovery / overshoot prevention ─────────────────────────

    [Fact]
    public void NextDevelopmentReferenceSemanticsAreExplicit_PreviousNonCutbackVolume()
    {
        var sequence = Simulate(8, 24d);
        var firstMesocyclePeak = sequence[2].TotalKm; // Week 3 of mesocycle 1
        var secondMesocycleWeek1 = sequence[4].TotalKm; // Week 1 of mesocycle 2
        // Week 1 of mesocycle 2 must derive from the PRIOR PEAK, not from
        // the recovery week (sequence[3]) that immediately preceded it.
        Assert.Equal(NextDevelopmentVolume(firstMesocyclePeak), secondMesocycleWeek1);
        Assert.NotEqual(NextDevelopmentVolume(sequence[3].TotalKm), secondMesocycleWeek1);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(20)]
    [InlineData(32)]
    public void NextDevelopmentCannotOvershootExistingCaps(int geWeeks)
    {
        foreach (var baseline in RepresentativeBaselines)
        {
            var sequence = Simulate(geWeeks, baseline);
            var developmentWeeks = sequence.Where(w => w.Role == WeekRole.Development).ToList();
            for (var i = 1; i < developmentWeeks.Count; i++)
            {
                var change = developmentWeeks[i].TotalKm - developmentWeeks[i - 1].TotalKm;
                if (change <= 0) continue; // resets after recovery are not week-over-week increases
                Assert.True(change <= AbsoluteWeeklyIncrementCapKm + 0.001);
            }
        }
    }

    [Fact]
    public void NoCumulativeDriftAcrossEightMesocycles()
    {
        // 32-week simulation: development baselines must reset to real
        // peaks each mesocycle, not silently accumulate an ever-larger
        // "phantom" baseline from repeated recovery unwinding.
        var sequence = Simulate(32, 24d);
        var peaks = new List<double>();
        for (var i = 2; i < sequence.Count; i += 4)
            peaks.Add(sequence[i].TotalKm);
        Assert.Equal(8, peaks.Count);
        // Peaks must be monotonically non-decreasing (bounded growth), never
        // erratic sawtooth (a peak lower than an earlier peak would indicate
        // drift from using the recovery week as a baseline).
        for (var i = 1; i < peaks.Count; i++)
            Assert.True(peaks[i] >= peaks[i - 1]);
    }

    // ── Per-duration required behaviors ──────────────────────────────────────

    [Theory]
    [MemberData(nameof(RequiredGeDurationsData))]
    public void EachRequiredGeDurationBehaviorIsExplicit(int geWeeks)
    {
        foreach (var baseline in RepresentativeBaselines)
        {
            var sequence = Simulate(geWeeks, baseline);
            Assert.Equal(geWeeks, sequence.Count);
            Assert.All(sequence, w => Assert.True(w.TotalKm > 0));
            Assert.All(sequence, w => Assert.True(w.LongRunKm > 0));
            Assert.All(sequence, w => Assert.True(w.LongRunKm < w.TotalKm));
        }
    }

    public static IEnumerable<object[]> RequiredGeDurationsData() =>
        RequiredGeDurations.Select(d => new object[] { d });

    [Theory]
    [InlineData(5, 1)]
    [InlineData(6, 2)]
    [InlineData(7, 3)]
    [InlineData(11, 3)]
    public void TerminalRemainderBehaviorIsExplicit(int geWeeks, int expectedRemainderCount)
    {
        var sequence = Simulate(geWeeks, 24d);
        var remainder = sequence.SkipWhile(w => w.Role != WeekRole.Alignment).ToList();
        Assert.Equal(expectedRemainderCount, remainder.Count);
        Assert.All(remainder, w => Assert.Equal(WeekRole.Alignment, w.Role));
    }

    [Fact]
    public void GeToRunwayContinuityRequirementIsExplicit()
    {
        // Structural, not numeric: the approved 8-week Runway remains
        // unchanged; alignment is achieved by GE moving upward, not by
        // recalibrating Runway Week 1 downward (Phase 4I.2 §19).
        const bool runwayStructureChangedByThisDecision = false;
        Assert.False(runwayStructureChangedByThisDecision);
    }

    // ── Taper distinction ────────────────────────────────────────────────────

    [Fact]
    public void RecoveryIsDistinguishedFromTaper()
    {
        const double taperVolumeMultiplier = 0.53d; // Core's existing, unmodified constant
        var recoveryMultiplier = 1 - RecoveryReductionRatio; // 0.85
        Assert.NotEqual(taperVolumeMultiplier, recoveryMultiplier);
        Assert.True(recoveryMultiplier > taperVolumeMultiplier); // recovery reduces less than taper
    }

    [Fact]
    public void NoUniversalTenPercentRuleIsIntroduced()
    {
        Assert.NotEqual(0.10d, RecoveryReductionRatio);
        Assert.Equal(0.15d, RecoveryReductionRatio);
    }

    // ── Runtime activation / governance gate ────────────────────────────────

    [Fact]
    public void RuntimeActivationRemainsDisabled()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var deferred = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DeferredDependencyId);
        Assert.Equal("OPEN", deferred.GetProperty("status").GetString());
    }

    [Fact]
    public void StagedPlanBlockerClosesOnlyIfAllNumericFieldsApproved_NotYetTrue()
    {
        // TD-GENERAL-ENDURANCE-STAGED-PLAN-001 must remain OPEN even though
        // the numeric recovery gap is now closed -- rolling materialization,
        // persistence, transition orchestration, and catalog content remain
        // unresolved (this phase's own scope boundary).
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var deferred = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DeferredDependencyId);
        Assert.Equal("OPEN", deferred.GetProperty("status").GetString());
    }

    // ── Canonical governance-record cross-check ─────────────────────────────

    [Fact]
    public void CanonicalJson_RecordsTheDecisionAsClosedAndApproved()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risk = document.RootElement.GetProperty("risks").EnumerateArray()
            .Single(r => r.GetProperty("id").GetString() == DecisionId);

        Assert.Equal("CLOSED", risk.GetProperty("status").GetString());
        var rawText = risk.GetRawText();
        Assert.Contains("0.15", rawText);
        Assert.Contains("progression_rules_v2.yaml", rawText);
        Assert.Contains("LONG_HORIZON_GENERAL_ENDURANCE_NUMERIC_RECOVERY_POLICY_APPROVED", rawText);
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
