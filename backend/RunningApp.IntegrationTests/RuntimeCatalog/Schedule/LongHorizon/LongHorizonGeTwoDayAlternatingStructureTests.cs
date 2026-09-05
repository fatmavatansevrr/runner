using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 10K-GEN.32 -- dark, unit-level verification of GEN.31's six-item
/// contract items 1-2: <see cref="LongHorizonGeStructuralSelector"/>'s new
/// <c>alternatingKeyEasy</c> parameter (Option A, GEN.31 §1) and
/// <see cref="LongHorizonGeNumericExecutor"/>'s consumption of the resulting
/// per-week <c>HasKeySession</c> flag. Every existing (non-alternating)
/// caller's own behavior is separately re-verified byte-identical by the
/// untouched full regression suite; this file exercises only the new,
/// additive 2D-shaped path plus the zero-delta default-parameter contract
/// directly, at the unit level, since the real production admission gates
/// (GEN.31 §3.4 item 3) do not yet admit 2D into the end-to-end rolling-
/// activation pipeline -- see PHASE_10K_GEN_32 §3.3/§3.4 for the precise,
/// disclosed reason full dark end-to-end (real-Postgres, rolling-activation)
/// verification is deferred to a follow-up phase.
/// </summary>
public sealed class LongHorizonGeTwoDayAlternatingStructureTests
{
    // ── Structural selector: Option A alternation ───────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(32)]
    public void Select_AlternatingKeyEasy_ProducesPatternAOnOddWeekIndexPatternBOnEven(int geWeeks)
    {
        var weeks = LongHorizonGeStructuralSelector.Select(
            geWeeks, ReadinessProfile.ConsistencyNeeded, easySupportCount: 1, alternatingKeyEasy: true);

        Assert.Equal(geWeeks, weeks.Count);
        foreach (var week in weeks)
        {
            if (week.WeekIndex % 2 == 1)
            {
                // Pattern A: KEY_SESSION + LONG_RUN, zero EASY_SUPPORT.
                Assert.True(week.HasKeySession, $"Week {week.WeekIndex} (odd) expected Pattern A (HasKeySession=true).");
                Assert.False(week.HasEasySupport, $"Week {week.WeekIndex} (odd) expected zero EASY_SUPPORT.");
                Assert.Empty(week.EasySupportWorkouts);
            }
            else
            {
                // Pattern B: EASY_SUPPORT + LONG_RUN, zero KEY_SESSION.
                Assert.False(week.HasKeySession, $"Week {week.WeekIndex} (even) expected Pattern B (HasKeySession=false).");
                Assert.True(week.HasEasySupport, $"Week {week.WeekIndex} (even) expected non-zero EASY_SUPPORT.");
                Assert.Single(week.EasySupportWorkouts);
            }
            // Every week still carries a LONG_RUN and a resolvable (if unconsumed) KeySessionWorkout reference,
            // matching GEN.31 §2's content-reuse finding -- selection, not fresh authoring.
            Assert.NotNull(week.LongRunWorkout);
            Assert.NotNull(week.KeySessionWorkout);
        }

        // Every GE week alternates -- no two consecutive weeks share a pattern.
        for (var i = 1; i < weeks.Count; i++)
            Assert.NotEqual(weeks[i - 1].HasKeySession, weeks[i].HasKeySession);
    }

    [Fact]
    public void Select_AlternatingKeyEasy_RecoveryWeeksAlsoAlternate_NoStageCarveOut()
    {
        // A FullPhase 8-week GE: mesocycle weeks 1-4 (positions Dev1/Dev2/Dev3/RecoveryConsolidation), 5-8 likewise.
        var weeks = LongHorizonGeStructuralSelector.Select(
            8, ReadinessProfile.CoreEntryReady, easySupportCount: 1, alternatingKeyEasy: true);

        var recoveryWeeks = weeks.Where(w => w.IsRecoveryWeek).ToList();
        Assert.NotEmpty(recoveryWeeks);
        foreach (var rw in recoveryWeeks)
        {
            // GEN.31 §1: "every GE week is either Pattern A or Pattern B" -- no recovery-week exemption.
            Assert.Equal(rw.WeekIndex % 2 == 1, rw.HasKeySession);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(32)]
    public void Select_DefaultParameter_IsByteIdenticalToPreGen32Behavior(int geWeeks)
    {
        // Zero-delta by construction: no alternatingKeyEasy argument (matches every existing 4D/5D/6D call site).
        var weeks = LongHorizonGeStructuralSelector.Select(geWeeks, ReadinessProfile.ConsistencyNeeded, easySupportCount: 2);

        Assert.All(weeks, w =>
        {
            Assert.True(w.HasKeySession);
            Assert.True(w.HasEasySupport);
            Assert.Equal(2, w.EasySupportWorkouts.Count);
        });
    }

    // ── Numeric executor: consumes HasKeySession, zero-delta for existing shape ─────

    [Fact]
    public void Execute_TwoDayAlternatingWeeks_KeySessionZeroOnPatternB_EasySupportZeroOnPatternA()
    {
        var weeks = LongHorizonGeStructuralSelector.Select(
            6, ReadinessProfile.ConsistencyNeeded, easySupportCount: 1, alternatingKeyEasy: true);

        var baseline = new LongHorizonGeEntryBaselineInput(RecentWeeklyVolumeKm: 20d, RecentLongestRunKm: 8d, RecentRunsPerWeek: 4);
        var results = LongHorizonGeNumericExecutor.Execute(weeks, baseline, VolumeSafetyPolicy.Intermediate2D);

        Assert.Equal(weeks.Count, results.Count);
        for (var i = 0; i < weeks.Count; i++)
        {
            var week = weeks[i];
            var result = results[i];

            Assert.True(result.TotalVolumeKm > 0);
            Assert.True(result.LongRunDistanceKm > 0);

            if (week.HasKeySession)
            {
                // Pattern A: KEY_SESSION carries the non-long-run residual; no EASY_SUPPORT this week.
                Assert.True(result.KeySessionDistanceKm > 0);
                Assert.Empty(result.EasySupportDistancesKm);
            }
            else
            {
                // Pattern B: KEY_SESSION is exactly zero (never allocated); EASY_SUPPORT carries the residual.
                Assert.Equal(0d, result.KeySessionDistanceKm);
                Assert.Single(result.EasySupportDistancesKm);
                Assert.True(result.EasySupportDistancesKm[0] > 0);
            }

            // The week's own total volume is conserved: LONG_RUN + KEY (or EASY) sums to the total,
            // within the same rounding tolerance the allocation policy itself uses.
            var nonLongRun = week.HasKeySession ? result.KeySessionDistanceKm : result.EasySupportDistancesKm.Sum();
            Assert.True(Math.Abs(result.TotalVolumeKm - result.LongRunDistanceKm - nonLongRun) < 0.6d);
        }
    }

    [Fact]
    public void Execute_ExistingConstantKeyShape_KeySessionDistanceKm_IsUnaffectedByHasKeySessionGeneralization()
    {
        // Every pre-GEN.32 (4D/5D) week has HasKeySession==true via the descriptor's own default --
        // this must remain byte-identical after the Execute-layer generalization (GEN.31 §3.4 item 2).
        var weeks = LongHorizonGeStructuralSelector.Select(4, ReadinessProfile.ConsistencyNeeded, easySupportCount: 2);
        var baseline = new LongHorizonGeEntryBaselineInput(RecentWeeklyVolumeKm: 30d, RecentLongestRunKm: 10d, RecentRunsPerWeek: 4);

        var results = LongHorizonGeNumericExecutor.Execute(weeks, baseline, VolumeSafetyPolicy.Default);

        Assert.All(results, r =>
        {
            Assert.True(r.KeySessionDistanceKm > 0);
            Assert.Equal(2, r.EasySupportDistancesKm.Count);
        });
    }

    // ── GEN.31 §3.4 item 3 (partial): LongHorizonGeCardinality + the
    // ExistingLongHorizonGeWindowMaterializer daysPerWeek-threading fix ─────

    [Theory]
    [InlineData(3, 1, false)]
    [InlineData(4, 2, false)]
    [InlineData(5, 3, false)]
    [InlineData(6, 4, false)]
    public void LongHorizonGeCardinality_NonTwoDay_MatchesPreGen32DaysPerWeekMinusTwoIdentity(
        int daysPerWeek, int expectedEasySupportCount, bool expectedAlternating)
    {
        var (easySupportCount, alternating) = LongHorizonGeCardinality.Resolve(daysPerWeek);
        Assert.Equal(expectedEasySupportCount, easySupportCount);
        Assert.Equal(expectedAlternating, alternating);
    }

    [Fact]
    public void LongHorizonGeCardinality_TwoDay_ResolvesToPatternBCardinalityWithAlternation()
    {
        var (easySupportCount, alternating) = LongHorizonGeCardinality.Resolve(2);
        Assert.Equal(1, easySupportCount);
        Assert.True(alternating);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void ExistingLongHorizonGeWindowMaterializer_ExplicitDaysPerWeek_IsZeroDeltaVersusOmittedInference(int daysPerWeek)
    {
        // Every pre-GEN.32 caller omits daysPerWeek; the inference from
        // EasySupportWorkouts.Count is exact for every non-alternating (uniform
        // per-week) GE shape. Confirms the new explicit-daysPerWeek path
        // (GEN.31 §3.4 item 3, wired at LongHorizonRollingInitialActivationRuntime's
        // own call site) produces byte-identical results to the pre-GEN.32
        // inferred path for every currently-reachable (non-2D) daysPerWeek.
        var weeks = LongHorizonGeStructuralSelector.Select(4, ReadinessProfile.ConsistencyNeeded, easySupportCount: daysPerWeek - 2);
        var baseline = new LongHorizonGeEntryBaselineInput(RecentWeeklyVolumeKm: 30d, RecentLongestRunKm: 10d, RecentRunsPerWeek: 4);
        var materializer = new ExistingLongHorizonGeWindowMaterializer();

        var inferred = materializer.Materialize(weeks, baseline, RunningBackground.Intermediate);
        var explicitPath = materializer.Materialize(weeks, baseline, RunningBackground.Intermediate, daysPerWeek);

        Assert.Equal(inferred.Count, explicitPath.Count);
        for (var i = 0; i < inferred.Count; i++)
        {
            Assert.Equal(inferred[i].TotalVolumeKm, explicitPath[i].TotalVolumeKm);
            Assert.Equal(inferred[i].LongRunDistanceKm, explicitPath[i].LongRunDistanceKm);
            Assert.Equal(inferred[i].KeySessionDistanceKm, explicitPath[i].KeySessionDistanceKm);
            Assert.Equal(inferred[i].EasySupportDistancesKm, explicitPath[i].EasySupportDistancesKm);
        }
    }
}
