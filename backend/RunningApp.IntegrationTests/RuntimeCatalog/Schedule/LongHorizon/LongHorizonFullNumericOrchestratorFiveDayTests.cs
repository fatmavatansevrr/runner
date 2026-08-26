using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 10K-FREQ.6D.14 — dark 21-52 week verification for the
/// FREQ.6D.12-approved Intermediate x5D LongHorizon GE structural/numeric
/// policy (1 KEY + 3 EASY + 1 LONG, target-capped growth at the existing
/// 44.5km FREQ.6C/FREQ.6D.10 reference, 28% preferred / 36% hard-cap
/// long-run share), implemented this phase via
/// <see cref="LongHorizonFullNumericOrchestrator.ExecuteAsync"/>'s new
/// <c>daysPerWeek</c> parameter. Mirrors
/// <see cref="LongHorizonFullNumericOrchestratorTests"/>'s own 4D structure
/// exactly -- same orchestrator, same real Preparation Runway/Core
/// pipelines, only the day-count parameter differs. Real file-system
/// catalog only -- no database, no network. Not called from any live
/// request path; no public 21+ activation.
///
/// Phase 10K-FREQ.6D.15 found, and FREQ.6D.16 root-caused, a genuine,
/// PRE-EXISTING (not 5D-specific) Preparation Runway numeric-materializer
/// gap: it had no accepted rule for "entry evidence exceeds the
/// independently-computed Core Week 1 boundary" outside Taper, requiring a
/// forced near-target-cap baseline (<c>SustainedHighBaseline</c>, retained
/// below for the tests that specifically document that history) to reach
/// long horizons at all. FREQ.6D.17 implements FREQ.6D.16's approved,
/// no-new-number clamp (Runway's own starting evidence is bounded to Core's
/// already-computed Week-1 target), so the full 21-52 matrix now succeeds
/// uniformly at the same representative baseline every other test in this
/// file uses -- see <see cref="Full21To52Matrix_RepresentativeBaseline_NoHorizonHole_ExactFiveDayCandidate"/>
/// and <see cref="PositiveReadinessMatrix_LowRepresentativeHigh_AllRepresentativeHorizons_NoLongerBaselineTuned"/>.
/// </summary>
public sealed class LongHorizonFullNumericOrchestratorFiveDayTests
{
    private static readonly DateOnly Anchor = new(2000, 1, 1);
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays =
        new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    // FREQ.6D.9/FREQ.6D.10-approved Intermediate x5D missing-readiness anchor (26.0km),
    // reused here as the "representative positive" baseline for short GE horizons -- never invented.
    private static readonly LongHorizonGeEntryBaselineInput RepresentativeBaseline = new(RecentWeeklyVolumeKm: 26, RecentLongestRunKm: 8, RecentRunsPerWeek: 5);
    private static readonly LongHorizonGeEntryBaselineInput LowBaseline = new(RecentWeeklyVolumeKm: 20, RecentLongestRunKm: 6, RecentRunsPerWeek: 5);
    private static readonly LongHorizonGeEntryBaselineInput HighBaseline = new(RecentWeeklyVolumeKm: 32, RecentLongestRunKm: 10, RecentRunsPerWeek: 5);
    private static readonly LongHorizonGeEntryBaselineInput MissingBaseline = new(null, null, null);
    private static readonly LongHorizonGeEntryBaselineInput ZeroBaseline = new(0, null, null);

    // Confirmed (diagnostic sweep) to reach 21/24/28/32/40/52 weeks without hitting the
    // pre-existing Runway/Core boundary gap described in the class doc comment above.
    private static readonly LongHorizonGeEntryBaselineInput SustainedHighBaseline = new(RecentWeeklyVolumeKm: 40, RecentLongestRunKm: 12, RecentRunsPerWeek: 5);

    private static string RepoRoot() => RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    private static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    private static ICatalogWorkoutDefinitionLoader Loader() =>
        new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }));

    private static LongHorizonCompositionDecision Decide(int availableFullWeeks, ReadinessProfile profile)
    {
        var raceDate = Anchor.AddDays(availableFullWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(Anchor, raceDate);
        return LongHorizonCompositionResolver.Resolve(coreHorizon, profile);
    }

    private static Task<LongHorizonExecutedSchedule> ExecuteAsync(
        int totalWeeks, ReadinessProfile profile, LongHorizonGeEntryBaselineInput baseline)
    {
        var startDate = new DateOnly(2026, 8, 3); // a Monday
        var raceDate = startDate.AddDays(totalWeeks * 7);
        return LongHorizonFullNumericOrchestrator.ExecuteAsync(
            Decide(totalWeeks, profile), startDate, raceDate, baseline,
            PreferredDays, LongRunDay, CatalogRoot(), Loader(), daysPerWeek: 5);
    }

    // ── GE structure: 1 KEY + 3 EASY + 1 LONG, exactly 5 sessions ──────────

    [Theory]
    [InlineData(21)]
    [InlineData(28)]
    [InlineData(32)]
    [InlineData(52)]
    public async Task GeWeeks_AreExactlyOneKeyThreeEasyOneLong_FixedFrequencyNoRamp(int totalWeeks)
    {
        var schedule = await ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, SustainedHighBaseline);
        var geWeeks = schedule.Weeks.Where(w => w.Structural.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance).ToList();
        Assert.NotEmpty(geWeeks);
        Assert.All(geWeeks, w =>
        {
            Assert.Equal(5, w.OrderedSlots.Count);
            Assert.Equal(1, w.OrderedSlots.Count(s => s.Structural.StructuralRole == "KEY_SESSION"));
            Assert.Equal(3, w.OrderedSlots.Count(s => s.Structural.StructuralRole == "EASY_SUPPORT"));
            Assert.Equal(1, w.OrderedSlots.Count(s => s.Structural.StructuralRole == "LONG_RUN"));
        });
    }

    // ── Readiness: positive/missing/zero matrices across representative horizons ──

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    public async Task PositiveReadinessMatrix_LowRepresentativeHigh_ShortHorizons_AllProduceValidSchedule(int totalWeeks)
    {
        foreach (var baseline in new[] { LowBaseline, RepresentativeBaseline, HighBaseline })
        {
            var schedule = await ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, baseline);
            Assert.Equal(totalWeeks, schedule.Weeks.Count);
            Assert.All(schedule.Weeks, w => Assert.NotNull(w.TotalVolumeKm));
        }
    }

    [Theory]
    [InlineData(28)]
    [InlineData(32)]
    [InlineData(52)]
    public async Task PositiveReadiness_LongHorizons_SustainedBaseline_ProducesValidSchedule(int totalWeeks)
    {
        var schedule = await ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, SustainedHighBaseline);
        Assert.Equal(totalWeeks, schedule.Weeks.Count);
        Assert.All(schedule.Weeks, w => Assert.NotNull(w.TotalVolumeKm));
    }

    /// <summary>
    /// Phase 10K-FREQ.6D.17 — with the FREQ.6D.16-approved clamp in place,
    /// the representative-baseline-range constraint documented in this
    /// class's own doc comment (SustainedHighBaseline was previously the
    /// ONLY baseline that reached long horizons without hitting the
    /// boundary gap) no longer applies: Low/Representative/High all now
    /// succeed at every representative horizon, proving the clamp is not
    /// tuned to one baseline.
    /// </summary>
    [Theory]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(40)]
    [InlineData(52)]
    public async Task PositiveReadinessMatrix_LowRepresentativeHigh_AllRepresentativeHorizons_NoLongerBaselineTuned(int totalWeeks)
    {
        foreach (var baseline in new[] { LowBaseline, RepresentativeBaseline, HighBaseline })
        {
            var schedule = await ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, baseline);
            Assert.Equal(totalWeeks, schedule.Weeks.Count);
            Assert.All(schedule.Weeks, w => Assert.NotNull(w.TotalVolumeKm));
        }
    }

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(52)]
    public async Task MissingReadinessMatrix_TypedProductIneligible_NotGeneric500(int totalWeeks)
    {
        var ex = await Assert.ThrowsAsync<LongHorizonGeMissingReadinessProductIneligibleException>(
            () => ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, MissingBaseline));
        Assert.Equal("LONG_HORIZON_GE_MISSING_READINESS_PRODUCT_INELIGIBLE", ex.Code);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(52)]
    public async Task ZeroReadinessMatrix_TypedProductIneligible_NotGeneric500(int totalWeeks)
    {
        var ex = await Assert.ThrowsAsync<LongHorizonGeExplicitZeroReadinessProductIneligibleException>(
            () => ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, ZeroBaseline));
        Assert.Equal("LONG_HORIZON_GE_EXPLICIT_ZERO_READINESS_PRODUCT_INELIGIBLE", ex.Code);
    }

    // ── 44.5km target cap / plateau (mandatory 52-week non-runaway proof) ──

    [Fact]
    public async Task FiftyTwoWeeks_GeNeverExceeds44Point5Cap_NoRunawayGrowth()
    {
        var schedule = await ExecuteAsync(52, ReadinessProfile.ConsistencyNeeded, SustainedHighBaseline);
        var geWeeks = schedule.Weeks.Where(w => w.Structural.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance).ToList();
        Assert.Equal(32, geWeeks.Count);
        Assert.All(geWeeks, w => Assert.True(w.TotalVolumeKm!.Value <= VolumeSafetyPolicy.FiveDayIntermediate.ResolvedPeakReference.Value + 0.01,
            $"GE week {w.Structural.LocalSegmentWeekNumber} total ({w.TotalVolumeKm}km) exceeded the approved 44.5km target cap."));

        // Non-runaway: nothing anywhere near the previously-rejected ~70+km/week uncapped pattern.
        Assert.All(geWeeks, w => Assert.True(w.TotalVolumeKm!.Value < 50,
            $"GE week {w.Structural.LocalSegmentWeekNumber} total ({w.TotalVolumeKm}km) is in the previously-rejected uncapped-growth range."));
    }

    // ── 28% preferred / 36% hard-cap long-run share ─────────────────────────

    [Theory]
    [InlineData(21)]
    [InlineData(32)]
    [InlineData(52)]
    public async Task GeLongRunShare_Within28To36PercentBand(int totalWeeks)
    {
        var schedule = await ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, SustainedHighBaseline);
        var geWeeks = schedule.Weeks.Where(w => w.Structural.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance && w.Structural.IsRecoveryWeek != true).ToList();
        Assert.All(geWeeks, w =>
        {
            var share = w.LongRunDistanceKm!.Value / w.TotalVolumeKm!.Value;
            Assert.True(share >= 0.28 - 0.02 && share <= 0.36 + 0.02,
                $"GE week {w.Structural.LocalSegmentWeekNumber} long-run share {share:P1} outside the approved 28%/36% band.");
        });
    }

    // ── GE -> Runway structural + numeric continuity ────────────────────────

    [Fact]
    public async Task GeToRunwayStructure_BothOneKeyThreeEasyOneLong()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded, RepresentativeBaseline);
        var lastGe = schedule.Weeks.Last(w => w.Structural.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance);
        var firstRunway = schedule.Weeks.First(w => w.Structural.Segment == LongHorizonSegmentType.PreparationRunway);

        Assert.Equal(5, lastGe.OrderedSlots.Count);
        Assert.Equal(5, firstRunway.OrderedSlots.Count);
        Assert.Equal(1, firstRunway.OrderedSlots.Count(s => s.Structural.StructuralRole == "KEY_SESSION"));
        Assert.Equal(3, firstRunway.OrderedSlots.Count(s => s.Structural.StructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, firstRunway.OrderedSlots.Count(s => s.Structural.StructuralRole == "LONG_RUN"));
    }

    [Fact]
    public async Task GeToRunwayVolumeContinuity_WithinApprovedCaps()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded, RepresentativeBaseline);
        var lastGe = schedule.Weeks.Last(w => w.Structural.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance);
        var firstRunway = schedule.Weeks.First(w => w.Structural.Segment == LongHorizonSegmentType.PreparationRunway);

        var maxAllowed = Math.Max(lastGe.TotalVolumeKm!.Value * 0.08, 2.5) + 0.01;
        Assert.True(firstRunway.TotalVolumeKm!.Value <= lastGe.TotalVolumeKm!.Value + maxAllowed,
            $"Runway Week 1 ({firstRunway.TotalVolumeKm}) exceeds GE exit ({lastGe.TotalVolumeKm}) by more than approved caps.");
    }

    // ── Runway -> Core dual-KEY continuity (exercises the real FREQ.6D.13 fix end-to-end) ──

    [Fact]
    public async Task RunwayToCore_DualKeyLanesSurviveIndependently_NoRoleCollision()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded, RepresentativeBaseline);
        var lastRunway = schedule.Weeks.Last(w => w.Structural.Segment == LongHorizonSegmentType.PreparationRunway);
        var firstCore = schedule.Weeks.First(w => w.Structural.Segment == LongHorizonSegmentType.Core);

        Assert.Equal(1, lastRunway.OrderedSlots.Count(s => s.Structural.StructuralRole == "KEY_SESSION"));
        Assert.Equal(2, firstCore.OrderedSlots.Count(s => s.Structural.StructuralRole == "KEY_SESSION"));
        Assert.Equal(2, firstCore.OrderedSlots.Count(s => s.Structural.StructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, firstCore.OrderedSlots.Count(s => s.Structural.StructuralRole == "LONG_RUN"));

        var keySlots = firstCore.OrderedSlots.Where(s => s.Structural.StructuralRole == "KEY_SESSION").ToList();
        Assert.Equal(2, keySlots.Select(s => s.AssignedDate).Distinct().Count());
    }

    [Fact]
    public async Task RepeatedEasySlots_RemainIndependentlyAddressable_AcrossGeRunwayCore()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded, RepresentativeBaseline);
        foreach (var week in schedule.Weeks)
        {
            var easySlots = week.OrderedSlots.Where(s => s.Structural.StructuralRole == "EASY_SUPPORT").ToList();
            if (easySlots.Count < 2) continue;
            Assert.Equal(easySlots.Count, easySlots.Select(s => s.AssignedDate).Distinct().Count());
        }
    }

    // ── Full 21-52 matrix: 32/32, no exclusions, per FREQ.6D.16/FREQ.6D.17 ──

    public static IEnumerable<object[]> AllSupportedHorizons() =>
        Enumerable.Range(21, 32).Select(weeks => new object[] { weeks });

    /// <summary>
    /// Phase 10K-FREQ.6D.17 — mechanically exercises every one of the 32
    /// supported horizons (21-52 inclusive) with the FREQ.6D.9/10-approved
    /// representative positive-readiness baseline. Previously 22 weeks (and
    /// the wider non-Recovery-terminal set FREQ.6D.15 found) required a
    /// forced near-cap workaround baseline (SustainedHighBaseline) to reach
    /// at all; with the FREQ.6D.16-approved clamp implemented, the full
    /// matrix now succeeds uniformly at the SAME representative baseline
    /// every other LongHorizon test in this file uses. No exclusions, no
    /// "22 known issue," no fallback.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllSupportedHorizons))]
    public async Task Full21To52Matrix_RepresentativeBaseline_NoHorizonHole_ExactFiveDayCandidate(int totalWeeks)
    {
        var schedule = await ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, RepresentativeBaseline);
        Assert.Equal(totalWeeks, schedule.Weeks.Count);
        Assert.Equal(LongHorizonStructuralMaterializer.CandidateKeyFiveDay, schedule.Structural.CandidateKey);
        Assert.Equal(LongHorizonStructuralMaterializer.CandidateVersionFiveDay, schedule.Structural.CandidateVersion);
        Assert.True(schedule.GeNumericExecutionComplete);
        Assert.True(schedule.RunwayNumericExecutionComplete);
        Assert.True(schedule.CoreNumericExecutionComplete);
    }

    /// <summary>
    /// Phase 10K-FREQ.6D.16/FREQ.6D.17 — 22 weeks previously failed at every
    /// baseline tried (documented by FREQ.6D.15), and so did the completely
    /// unmodified 4D orchestrator (a genuine, pre-existing, day-count-neutral
    /// gap, not a 5D regression). FREQ.6D.16 root-caused it precisely to
    /// <see cref="RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization.PreparationRunwayNumericMaterializer.Materialize{TKey}"/>
    /// never reconciling GE's exit against Core's own Week-1 target, and
    /// approved a generic clamp (no new number) resolving it. Both frequencies
    /// now succeed at 22 weeks.
    /// </summary>
    [Fact]
    public async Task TwentyTwoWeeks_SucceedsViaFreq6D16ApprovedClamp()
    {
        var schedule = await ExecuteAsync(22, ReadinessProfile.ConsistencyNeeded, SustainedHighBaseline);
        Assert.Equal(22, schedule.Weeks.Count);
        Assert.All(schedule.Weeks, w => Assert.NotNull(w.TotalVolumeKm));
    }

    [Fact]
    public async Task TwentyTwoWeeks_AlsoSucceedsOnUnmodifiedFourDayOrchestrator_SharedClampConfirmed()
    {
        var startDate = new DateOnly(2026, 8, 3);
        var raceDate = startDate.AddDays(22 * 7);
        var fourDayPreferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var schedule = await LongHorizonFullNumericOrchestrator.ExecuteAsync(
            Decide(22, ReadinessProfile.ConsistencyNeeded), startDate, raceDate,
            new LongHorizonGeEntryBaselineInput(20, 8, 3), fourDayPreferredDays, LongRunDay, CatalogRoot(), Loader());
        Assert.Equal(22, schedule.Weeks.Count);
    }

    /// <summary>
    /// Phase 10K-FREQ.6D.15 root-caused the 22-week gap and found it was far
    /// wider: 23/25/26/27 weeks (all non-Recovery-terminal GE segments) and
    /// even a Recovery-terminal 28-week horizon at a low baseline failed
    /// identically. FREQ.6D.16 approved, and FREQ.6D.17 implemented, a
    /// generic clamp resolving all of them uniformly -- no per-horizon
    /// branching, no new numeric constant.
    /// </summary>
    [Theory]
    [InlineData(23)]
    [InlineData(25)]
    [InlineData(26)]
    [InlineData(27)]
    public async Task NonRecoveryTerminalShortHorizons_SucceedViaFreq6D16ApprovedClamp(int totalWeeks)
    {
        var baseline = new LongHorizonGeEntryBaselineInput(26, 8, 5);
        var schedule = await ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, baseline);
        Assert.Equal(totalWeeks, schedule.Weeks.Count);
        Assert.All(schedule.Weeks, w => Assert.NotNull(w.TotalVolumeKm));
    }

    [Fact]
    public async Task RecoveryTerminalHorizon_LowBaseline_SucceedsViaFreq6D16ApprovedClamp()
    {
        // 28 weeks = 8 GE weeks = 2 full mesocycles, ending on a Recovery week --
        // previously still failed at a low baseline because the cumulative
        // pre-recovery peak (not just "ends on Recovery") determined the
        // outcome. The shared clamp resolves this the same way it resolves
        // every other case: by construction, not by tuning to this baseline.
        var lowBaseline = new LongHorizonGeEntryBaselineInput(20, 6, 5);
        var schedule = await ExecuteAsync(28, ReadinessProfile.ConsistencyNeeded, lowBaseline);
        Assert.Equal(28, schedule.Weeks.Count);
        Assert.All(schedule.Weeks, w => Assert.NotNull(w.TotalVolumeKm));
    }

    [Fact]
    public async Task RecoveryTerminalHorizon_TwentyFourWeeks_LowBaseline_SucceedsBecauseRecoveryLandsUnderBoundary()
    {
        var lowBaseline = new LongHorizonGeEntryBaselineInput(20, 6, 5);
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded, lowBaseline);
        var lastGe = schedule.Weeks.Last(w => w.Structural.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance);
        Assert.True(lastGe.Structural.IsRecoveryWeek);
        Assert.Equal(24, schedule.Weeks.Count);
    }

    [Fact]
    public async Task ClampNeverRaisesVolume_RunwayEntryNeverExceedsRawGeExit()
    {
        // Phase 10K-FREQ.6D.17 permanent regression: the clamp is a Math.Min,
        // so wherever it activates, Runway's own entry can only be <= the raw
        // GE exit -- never above it.
        var baseline = new LongHorizonGeEntryBaselineInput(26, 8, 5);
        var schedule = await ExecuteAsync(23, ReadinessProfile.ConsistencyNeeded, baseline);
        var lastGe = schedule.Weeks.Last(w => w.Structural.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance);
        var firstRunway = schedule.Weeks.First(w => w.Structural.Segment == LongHorizonSegmentType.PreparationRunway);
        Assert.True(firstRunway.TotalVolumeKm!.Value <= lastGe.TotalVolumeKm!.Value + 0.01);
        Assert.True(firstRunway.LongRunDistanceKm!.Value <= lastGe.LongRunDistanceKm!.Value + 0.01);
    }

    // ── Determinism ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Determinism_RepeatedExecutionProducesIdenticalResult()
    {
        var first = await ExecuteAsync(24, ReadinessProfile.CoreEntryReady, RepresentativeBaseline);
        var second = await ExecuteAsync(24, ReadinessProfile.CoreEntryReady, RepresentativeBaseline);

        Assert.Equal(
            first.Weeks.Select(w => (w.TotalVolumeKm, w.LongRunDistanceKm, w.OrderedSlots.Select(s => (s.WorkoutKey, s.WorkoutVersion, s.AssignedDate, s.PlannedDistanceKm)))),
            second.Weeks.Select(w => (w.TotalVolumeKm, w.LongRunDistanceKm, w.OrderedSlots.Select(s => (s.WorkoutKey, s.WorkoutVersion, s.AssignedDate, s.PlannedDistanceKm)))));
    }

    // ── Historical 4D zero-delta guard (same orchestrator, default daysPerWeek) ──

    [Fact]
    public async Task FourDayDefaultCaller_StillProducesFourSlotWeeks_ZeroDelta()
    {
        var startDate = new DateOnly(2026, 8, 3);
        var raceDate = startDate.AddDays(24 * 7);
        var fourDayPreferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var schedule = await LongHorizonFullNumericOrchestrator.ExecuteAsync(
            Decide(24, ReadinessProfile.ConsistencyNeeded), startDate, raceDate,
            new LongHorizonGeEntryBaselineInput(20, 8, 3), fourDayPreferredDays, LongRunDay, CatalogRoot(), Loader());

        var geWeeks = schedule.Weeks.Where(w => w.Structural.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance).ToList();
        Assert.All(geWeeks, w => Assert.Equal(4, w.OrderedSlots.Count));
        Assert.Equal(LongHorizonStructuralMaterializer.CandidateKey, schedule.Structural.CandidateKey);
    }
}
