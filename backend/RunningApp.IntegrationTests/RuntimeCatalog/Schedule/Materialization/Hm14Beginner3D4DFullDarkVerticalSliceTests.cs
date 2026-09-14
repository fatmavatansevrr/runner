using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit.Abstractions;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase HM.14 — the same class of test harness as <see cref="Hm8Intermediate3DFullDarkVerticalSliceTests"/>/
/// <see cref="Hm11Intermediate5DFullDarkVerticalSliceTests"/>, exercising the real, unmodified
/// production pipeline against the two new dark-only HALF_MARATHON__3D__BEGINNER and
/// HALF_MARATHON__4D__BEGINNER candidates, implementing HM.13's frozen numeric authority
/// (PHASE_HM_13_BEGINNER_3D_4D_5D_NUMERIC_AND_LEVEL_AUTHORITY_CLOSURE.md). Both frequencies are
/// single-KEY-lane (no secondary lane, unlike 5D) -- structurally identical to Intermediate
/// 3D/4D, just at Beginner's own lower numeric authority and catalog "NEW" experience label.
/// HALF_MARATHON Beginner x5D is deliberately absent everywhere in this file except the explicit
/// ineligibility proofs -- HM.13 §6 froze it PRODUCT_INELIGIBLE.
/// </summary>
public sealed class Hm14Beginner3D4DFullDarkVerticalSliceTests
{
    private readonly ITestOutputHelper _output;

    public Hm14Beginner3D4DFullDarkVerticalSliceTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    // ============================================================================================
    // 3D golden 14W trace. Golden fixture: RecentWeeklyVolumeKm equals HalfMarathonBeginner3D's own
    // GoldenFixtureStartingVolumeKm (15.5), so the 14W (11-transition, exactly the calibration
    // point) resolved peak is exactly ResolvedPeakReference.Value = 27.0km.
    // ============================================================================================
    [Fact]
    public async Task PreferredFourteenWeekCore_ThreeDay_TraversesRealPipelineAndMaterializesValidDarkPreview()
    {
        var candidate = await CandidateAsync(3);
        Assert.Equal(new PlanCatalogCoreCycle(10, 14, 16), candidate.CoreCycle);
        var context = Context(candidate, daysPerWeek: 3, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        Assert.Equal(new[] { 3, 4, 5, 2 }, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.Equal(14, prescribed.Weeks.Count);
        Assert.Equal(42, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(3, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        // Max-one-true-hard-stimulus (beginner-progression-modifier.v1.json: allowSecondHardStimulus=false)
        // holds trivially here -- single-KEY-lane layout has structurally exactly one KEY per week.
        Assert.All(prescribed.Weeks, w => Assert.Single(w.Sessions.Where(s => s.StructuralRole == "KEY_SESSION")));

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid, string.Join("; ", volume.WeeklyVolumePlan.ValidationResult.Errors));
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid, string.Join("; ", volume.LongRunProgression.ValidationResult.Errors));
        Assert.Equal(27d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.46d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 16d + 0.001d);
        });
        var taper = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taper.Length);
        Assert.Equal(19.0d, taper[0].PlannedWeeklyVolumeKm);
        Assert.Equal(11.5d, taper[1].PlannedWeeklyVolumeKm);
        Assert.True(taper[0].PlannedWeeklyVolumeKm >= taper[1].PlannedWeeklyVolumeKm, "Taper must be non-chained/non-increasing.");
        Assert.NotEqual(Math.Round(taper[0].PlannedWeeklyVolumeKm * 0.43d, 1), taper[1].PlannedWeeklyVolumeKm);

        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key = week.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
            var easy = week.Sessions.Single(s => s.StructuralRole == "EASY_SUPPORT");
            var longRun = week.Sessions.Single(s => s.StructuralRole == "LONG_RUN");
            Assert.True(easy.Prescription.DistancePrescription.PlannedDistanceKm >= 0);
            Assert.True(key.Prescription.DistancePrescription.PlannedDistanceKm >= 0);
            _output.WriteLine(
                $"3D W{week.WeekNumber:D2}|{week.PhaseKey}|vol={volumeWeek.PlannedWeeklyVolumeKm:F1}|lr={longRunWeek.PlannedLongRunDistanceKm:F1}|share={longRunWeek.LongRunShareOfWeeklyVolume:P2}" +
                $"|key={key.ProgressionStageKey}|{key.WorkoutDefinitionKey} dist={key.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|easy_dist={easy.Prescription.DistancePrescription.PlannedDistanceKm:F1}|long_dist={longRun.Prescription.DistancePrescription.PlannedDistanceKm:F1}");
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(14, materialized.Payload.PlannedWeekCount);
    }

    // ============================================================================================
    // 4D golden 14W trace. RecentWeeklyVolumeKm = HalfMarathonBeginner4D's own
    // GoldenFixtureStartingVolumeKm (19.0) -> resolved peak = ResolvedPeakReference.Value = 33.0km.
    // ============================================================================================
    [Fact]
    public async Task PreferredFourteenWeekCore_FourDay_TraversesRealPipelineAndMaterializesValidDarkPreview()
    {
        var candidate = await CandidateAsync(4);
        Assert.Equal(new PlanCatalogCoreCycle(10, 14, 16), candidate.CoreCycle);
        var context = Context(candidate, daysPerWeek: 4, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        Assert.Equal(new[] { 3, 4, 5, 2 }, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.Equal(14, prescribed.Weeks.Count);
        Assert.Equal(56, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        Assert.All(prescribed.Weeks, w => Assert.Single(w.Sessions.Where(s => s.StructuralRole == "KEY_SESSION")));
        Assert.All(prescribed.Weeks, w => Assert.Equal(2, w.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT")));

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid, string.Join("; ", volume.WeeklyVolumePlan.ValidationResult.Errors));
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid, string.Join("; ", volume.LongRunProgression.ValidationResult.Errors));
        Assert.Equal(33d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 16d + 0.001d);
        });
        var taper = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taper.Length);
        Assert.Equal(23.0d, taper[0].PlannedWeeklyVolumeKm);
        Assert.Equal(14.0d, taper[1].PlannedWeeklyVolumeKm);
        Assert.True(taper[0].PlannedWeeklyVolumeKm >= taper[1].PlannedWeeklyVolumeKm, "Taper must be non-chained/non-increasing.");

        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key = week.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
            var longRun = week.Sessions.Single(s => s.StructuralRole == "LONG_RUN");
            _output.WriteLine(
                $"4D W{week.WeekNumber:D2}|{week.PhaseKey}|vol={volumeWeek.PlannedWeeklyVolumeKm:F1}|lr={longRunWeek.PlannedLongRunDistanceKm:F1}|share={longRunWeek.LongRunShareOfWeeklyVolume:P2}" +
                $"|key={key.ProgressionStageKey}|{key.WorkoutDefinitionKey} dist={key.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|long_dist={longRun.Prescription.DistancePrescription.PlannedDistanceKm:F1}");
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(14, materialized.Payload.PlannedWeekCount);
    }

    // ============================================================================================
    // 10-16W full dark materialization for both frequencies (frequency-generic phase allocation,
    // the same 7-row oracle table every other HM cell already proves).
    // ============================================================================================
    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(12, new[] { 2, 3, 5, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })]
    [InlineData(15, new[] { 4, 4, 5, 2 })]
    [InlineData(16, new[] { 4, 5, 5, 2 })]
    public async Task NonPreferredCoreLengths_ThreeDay_TraverseRealPipelineAndMaterializeValidDarkPreviews(
        int targetWeekCount, int[] expectedPhaseWeeks) =>
        await AssertNonPreferredHorizon(3, targetWeekCount, expectedPhaseWeeks, peak: 27d, lrHardCap: 0.46d);

    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(12, new[] { 2, 3, 5, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })]
    [InlineData(15, new[] { 4, 4, 5, 2 })]
    [InlineData(16, new[] { 4, 5, 5, 2 })]
    public async Task NonPreferredCoreLengths_FourDay_TraverseRealPipelineAndMaterializeValidDarkPreviews(
        int targetWeekCount, int[] expectedPhaseWeeks) =>
        await AssertNonPreferredHorizon(4, targetWeekCount, expectedPhaseWeeks, peak: 33d, lrHardCap: 0.40d);

    private async Task AssertNonPreferredHorizon(int daysPerWeek, int targetWeekCount, int[] expectedPhaseWeeks, double peak, double lrHardCap)
    {
        var candidate = await CandidateAsync(daysPerWeek);

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);
        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, allocation.Phases.Select(p => p.PhaseKey));
        Assert.Equal(expectedPhaseWeeks, allocation.Phases.Select(p => p.AllocatedWeeks));
        Assert.Equal(2, allocation.Phases.Single(p => p.PhaseKey == "TAPER").AllocatedWeeks);

        var result = await Pipeline().MaterializeAsync(Context(candidate, daysPerWeek, exactPaceEvidence: true, targetWeekCount));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(targetWeekCount, prescribed.Weeks.Count);
        Assert.Equal(targetWeekCount * daysPerWeek, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(daysPerWeek, w.Sessions.Count));
        Assert.Equal(expectedPhaseWeeks, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        // Max-one-true-hard-stimulus at every horizon: exactly one KEY_SESSION per week, always.
        Assert.All(prescribed.Weeks, w => Assert.Single(w.Sessions.Where(s => s.StructuralRole == "KEY_SESSION")));

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid);
        // Short horizons (10W-13W, fewer than 11 transitions) must never be forced to chase the peak.
        if (targetWeekCount < 14)
        {
            Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= peak + 0.001d,
                $"daysPerWeek={daysPerWeek} targetWeekCount={targetWeekCount}: PeakVolumeKm={volume.WeeklyVolumePlan.PeakVolumeKm} unexpectedly reached/exceeded the {peak}km reference despite fewer non-taper transitions than the calibration count.");
        }
        // Long horizons (15W/16W, more than 11 transitions) must never extrapolate past the peak
        // (HM.5.3 mechanism, ResolvedPeakReferenceIsSelectedCeiling=true).
        Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= peak + 0.001d,
            $"daysPerWeek={daysPerWeek} targetWeekCount={targetWeekCount}: PeakVolumeKm={volume.WeeklyVolumePlan.PeakVolumeKm} exceeds the {peak}km reference ceiling.");
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= lrHardCap + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 16d + 0.001d);
        });

        // Weekly growth safety -- Preferred=7%, Hard=8%, Absolute cap per-frequency (2.0km/3D, 2.5km/4D).
        var absoluteCap = daysPerWeek == 3 ? 2.0d : 2.5d;
        var orderedVolumeWeeks = volume.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        for (var i = 1; i < orderedVolumeWeeks.Length; i++)
        {
            var prev = orderedVolumeWeeks[i - 1];
            var cur = orderedVolumeWeeks[i];
            if (cur.IsTaperWeek || prev.IsTaperWeek) continue;
            var deltaKm = cur.PlannedWeeklyVolumeKm - prev.PlannedWeeklyVolumeKm;
            Assert.True(deltaKm <= absoluteCap + 0.001d,
                $"daysPerWeek={daysPerWeek} targetWeekCount={targetWeekCount}: W{prev.WeekNumber}->W{cur.WeekNumber} deltaKm={deltaKm:F2} exceeds the {absoluteCap}km absolute weekly-increase cap.");
        }

        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).OrderBy(w => w.WeekNumber).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        Assert.True(taperWeeks[0].PlannedWeeklyVolumeKm >= taperWeeks[1].PlannedWeeklyVolumeKm,
            $"Taper must be non-chained/non-increasing for every horizon (week1={taperWeeks[0].PlannedWeeklyVolumeKm}, week2={taperWeeks[1].PlannedWeeklyVolumeKm}).");

        _output.WriteLine(
            $"H{daysPerWeek}D-{targetWeekCount}|vol=[{string.Join(',', volume.WeeklyVolumePlan.Weeks.Select(w => w.PlannedWeeklyVolumeKm.ToString("F1")))}]" +
            $"|lr=[{string.Join(',', volume.LongRunProgression.Weeks.Select(w => w.PlannedLongRunDistanceKm.ToString("F1")))}]" +
            $"|peak={volume.WeeklyVolumePlan.PeakVolumeKm:F1}|taper={taperWeeks[0].PlannedWeeklyVolumeKm:F1}/{taperWeeks[1].PlannedWeeklyVolumeKm:F1}");

        // HM stays dark regardless of newly-feasible catalog headroom.
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Beginner, daysPerWeek));
    }

    // Boundary proof: 9W/17W remain infeasible for both frequencies (shared, frequency-generic
    // phase-allocation authority; no Beginner-specific horizon table).
    [Theory]
    [InlineData(3, 9)]
    [InlineData(3, 17)]
    [InlineData(4, 9)]
    [InlineData(4, 17)]
    public async Task OutOfCanonicalRangeHorizons_RemainInfeasible(int daysPerWeek, int targetWeekCount)
    {
        var candidate = await CandidateAsync(daysPerWeek);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);

        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Empty(allocation.Phases);
        Assert.Contains(targetWeekCount < 10 ? "TARGET_BELOW_SUM_OF_MINIMUMS" : "TARGET_ABOVE_SUM_OF_MAXIMUMS", allocation.ReasonCode);
        await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() =>
            Pipeline().MaterializeAsync(Context(candidate, daysPerWeek, exactPaceEvidence: true, targetWeekCount)));
    }

    // ============================================================================================
    // HM_PACE binds when independent evidence qualifies; falls back correctly (no fabricated
    // pace from desired finish time) when it doesn't -- proven for both frequencies.
    // ============================================================================================
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task ExactPaceEvidence_BindsHmPace_ForBeginner(int daysPerWeek)
    {
        var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(daysPerWeek), daysPerWeek, exactPaceEvidence: true));
        var sessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions;
        Assert.Contains(sessions, s => s.WorkoutDefinitionKey == "HM_PACE");
        Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task MissingIndependentExactPaceEvidence_FallsBackAndNeverFabricatesFromDesiredFinishTime(int daysPerWeek)
    {
        foreach (var targetWeekCount in new[] { 10, 11, 12, 13, 14, 15, 16 })
        {
            var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(daysPerWeek), daysPerWeek, exactPaceEvidence: false, targetWeekCount));
            var sessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions;

            Assert.DoesNotContain(sessions, s => s.WorkoutDefinitionKey == "HM_PACE");
            // No session outside the approved GOAL_PACE_TEN_K/HM_PACE workouts ever carries an
            // ExactPace prescription -- i.e. no desired-finish-time-derived pace is fabricated
            // for a fallback (THRESHOLD_TEMPO/EASY_STANDARD) session.
            Assert.DoesNotContain(sessions, s =>
                s.WorkoutDefinitionKey is not ("GOAL_PACE_TEN_K" or "HM_PACE") &&
                s.Prescription.PacePrescription.Kind == CatalogPacePrescriptionKind.ExactPace);
            Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
        }
    }

    // ============================================================================================
    // Calibration safety: missing/explicit-zero readiness never substitutes the calibration-only
    // starting volume -- fails closed identically to Intermediate 3D/4D/5D.
    // ============================================================================================
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task MissingWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume(int daysPerWeek)
    {
        var context = Context(await CandidateAsync(daysPerWeek), daysPerWeek, exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = null;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() => Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task ZeroWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume(int daysPerWeek)
    {
        var context = Context(await CandidateAsync(daysPerWeek), daysPerWeek, exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = 0;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() => Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    // ============================================================================================
    // The generic Level=="NEW" taper-floor override (CatalogSessionPrescriptionPlanner's own
    // useBeginnerThreeDayTaperMinima) activates correctly for HALF_MARATHON Beginner 3D's own
    // Taper weeks with zero new HM-specific code -- HM.13 §19's own finding, re-verified here.
    // ============================================================================================
    [Fact]
    public async Task BeginnerThreeDayTaperFloor_ActivatesGenericallyWithNoNewHmSpecificCode()
    {
        var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(3), 3, exactPaceEvidence: true));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));

        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var taperWeeks = prescribed.Weeks.Where(w => w.PhaseKey == "TAPER").OrderBy(w => w.WeekNumber).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        var week2Long = taperWeeks[1].Sessions.Single(s => s.StructuralRole == "LONG_RUN");
        var week2Volume = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == taperWeeks[1].WeekNumber);
        // Week 2 (deepest taper): 11.5km weekly volume, LR share 0.38 -> 4.37 -> rounds to 4.5km.
        // Below the NORMAL 5.0km LONG floor, but above the lowered Beginner-taper-specific 3.0km
        // floor -- proving the generic Level=="NEW" override actually fired (a normal-floor
        // violation would have thrown CatalogSessionPrescriptionInfeasibleException instead).
        Assert.Equal(11.5d, week2Volume.PlannedWeeklyVolumeKm);
        Assert.True(week2Long.Prescription.DistancePrescription.PlannedDistanceKm >= 3.0d);
        Assert.True(week2Long.Prescription.DistancePrescription.PlannedDistanceKm < 5.0d,
            "This assertion documents the exact floor-boundary case the generic Beginner taper-minima override exists for.");
    }

    // ============================================================================================
    // HALF_MARATHON Beginner x5D remains PRODUCT_INELIGIBLE -- typed via the identity policy's
    // own allow-list mechanism (IsSupportedLevelFrequency returns false; ResolveCandidate throws;
    // TryResolveCandidate returns null cleanly), never a generic/unhandled error. Intermediate x5D
    // remains available dark, unaffected.
    // ============================================================================================
    [Fact]
    public void HalfMarathonBeginnerFiveDay_RemainsProductIneligible()
    {
        Assert.Null(V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Beginner, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Beginner, 5));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Beginner, 5));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Beginner, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(5));

        // Intermediate x5D remains available dark, unaffected by Beginner x5D's ineligibility.
        Assert.NotNull(V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5));
    }

    [Fact]
    public void HalfMarathonBeginnerThreeAndFourDayIdentity_RemainOutsideEveryPublicGate()
    {
        foreach (var daysPerWeek in new[] { 3, 4 })
        {
            Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
                GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Beginner, daysPerWeek));
            Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(
                GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Beginner, daysPerWeek));
        }
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayBeginnerCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Beginner, 3));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonFourDayBeginnerCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Beginner, 4));
    }

    // ============================================================================================
    // Exact policy encoding proof for both new named policies.
    // ============================================================================================
    [Fact]
    public void HalfMarathonBeginner3D_ExactPolicyEncoding()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonBeginner3D, VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(3));
        var policy = VolumeSafetyPolicy.HalfMarathonBeginner3D;
        Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.0d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(15.5d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(27.0d, policy.ResolvedPeakReference.Value);
        Assert.Equal(11, policy.GoldenFixtureNonTaperTransitions);
        Assert.Equal(0.34d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.40d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.38d, policy.LongRunSelectionShare);
        Assert.Equal(0.46d, policy.LongRunHardCapShare);
        Assert.Equal(16.0d, policy.PreferredAbsolutePeakLongRunKm);
        Assert.Null(policy.StartingAbsoluteLongRunCapKm);
        Assert.Equal(new[] { 0.70d, 0.43d }, policy.TaperVolumeMultipliers);
        Assert.True(policy.ResolvedPeakReferenceIsSelectedCeiling);
    }

    [Fact]
    public void HalfMarathonBeginner4D_ExactPolicyEncoding()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonBeginner4D, VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(4));
        var policy = VolumeSafetyPolicy.HalfMarathonBeginner4D;
        Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(19.0d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(33.0d, policy.ResolvedPeakReference.Value);
        Assert.Equal(11, policy.GoldenFixtureNonTaperTransitions);
        Assert.Equal(0.30d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.33d, policy.LongRunSelectionShare);
        Assert.Equal(0.40d, policy.LongRunHardCapShare);
        Assert.Equal(16.0d, policy.PreferredAbsolutePeakLongRunKm);
        Assert.Null(policy.StartingAbsoluteLongRunCapKm);
        Assert.Equal(new[] { 0.70d, 0.43d }, policy.TaperVolumeMultipliers);
        Assert.True(policy.ResolvedPeakReferenceIsSelectedCeiling);
    }

    // ============================================================================================
    // Zero-delta regression: every existing Intermediate HM cell's policy identity/values are
    // byte-identical after this phase's additions.
    // ============================================================================================
    [Fact]
    public void IntermediateHalfMarathonCells_ZeroDelta_AfterBeginnerDispatchAddition()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate3D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(3));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate4D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(4));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate5D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(5));
        Assert.Equal(34.0d, VolumeSafetyPolicy.HalfMarathonIntermediate3D.ResolvedPeakReference.Value);
        Assert.Equal(43.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4D.ResolvedPeakReference.Value);
        Assert.Equal(51.0d, VolumeSafetyPolicy.HalfMarathonIntermediate5D.ResolvedPeakReference.Value);
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5));
    }

    // Zero-delta regression: live 10K Beginner 3D/4D policies/dispatch are unaffected by the new
    // ForHalfMarathonBeginnerDaysPerWeek dispatcher and BEGINNER_MODIFIER v2 catalog document.
    [Fact]
    public void TenKBeginnerThreeAndFourDay_ZeroDelta_AfterHalfMarathonBeginnerAddition()
    {
        Assert.Same(VolumeSafetyPolicy.ThreeDayBeginner, VolumeSafetyPolicy.ForBeginnerDaysPerWeek(3));
        Assert.Same(VolumeSafetyPolicy.BeginnerFourDay, VolumeSafetyPolicy.ForBeginnerDaysPerWeek(4));
        Assert.Equal(17d, VolumeSafetyPolicy.ThreeDayBeginner.ResolvedPeakReference.Value);
        Assert.Equal(21d, VolumeSafetyPolicy.BeginnerFourDay.ResolvedPeakReference.Value);
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 3));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 4));
        Assert.Equal((V1CatalogPilotIdentityPolicy.ThreeDayBeginnerCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(RunningBackground.Beginner, 3));
        Assert.Equal((V1CatalogPilotIdentityPolicy.BeginnerCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(RunningBackground.Beginner, 4));
    }

    // ============================================================================================
    // Adaptation smoke test for both frequencies (all-complete / EASY-missed / KEY-missed /
    // LONG-missed) -- generic mechanism, no HM/Beginner-specific adaptation code exists.
    // ============================================================================================
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task AdaptationSmoke_AllCombinationsProduceConservativeDecisions(int daysPerWeek)
    {
        var prescribed = (await Pipeline().MaterializeAsync(Context(await CandidateAsync(daysPerWeek), daysPerWeek, exactPaceEvidence: true)))
            .PrescriptionResult.FinalPrescribedPlan;
        var buildWeek = prescribed.Weeks.First(week => week.PhaseKey == "BUILD");
        var key = buildWeek.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
        var easy = buildWeek.Sessions.First(s => s.StructuralRole == "EASY_SUPPORT");
        var longRun = buildWeek.Sessions.Single(s => s.StructuralRole == "LONG_RUN");

        // Generic legacy N-session decision table (NextWindowLoadDecisionPolicy.DetermineLegacyLoadDecision,
        // a fixed table calibrated for the 4-session pilot, no HM/Beginner-specific adaptation
        // code exists anywhere): ProgressAsPlanned requires EffectiveCompletedCount>=4 UNLESS the
        // count==3 OnlyEasyMissing gate applies -- for a genuine 3-session structural week (3D),
        // full completion (count=3, nothing actually missing) does NOT satisfy OnlyEasyMissing
        // (it requires something to be missing), so all-complete resolves to Maintain, never
        // ProgressAsPlanned; a 4-session week's full completion (count=4) hits the >=4 arm
        // directly. Both are the same conservative, unmodified generic mechanism observed
        // honestly at their own respective session counts, not forced to match each other.
        Assert.Equal(daysPerWeek == 3 ? NextWindowLoadDecision.Maintain : NextWindowLoadDecision.ProgressAsPlanned,
            AdaptationDecision(buildWeek, missed: null));
        // A 3-session week's EASY-missed case (EffectiveCompletedCount=2) falls into the plain
        // count-floor Maintain branch (no count==3 OnlyEasyMissing gate is reachable at count=2);
        // a 4-session week's EASY-missed case (count=3: KEY+LONG both satisfied, one of the two
        // EASY_SUPPORT sessions missing) satisfies OnlyEasyMissing and reaches ProgressAsPlanned
        // via the count==3 gate.
        Assert.Equal(daysPerWeek == 3 ? NextWindowLoadDecision.Maintain : NextWindowLoadDecision.ProgressAsPlanned,
            AdaptationDecision(buildWeek, easy));
        Assert.Equal(NextWindowLoadDecision.Maintain, AdaptationDecision(buildWeek, key));
        Assert.Equal(NextWindowLoadDecision.Maintain, AdaptationDecision(buildWeek, longRun));
    }

    private static NextWindowLoadDecision AdaptationDecision(CatalogPrescribedWeek week, CatalogPrescribedSession? missed)
    {
        var evidence = week.Sessions.Select(session => new LogicalSessionEvidence(
            Guid.NewGuid(),
            session.StructuralRole switch
            {
                "KEY_SESSION" => PreparationRunwaySlotRole.KeySession,
                "EASY_SUPPORT" => PreparationRunwaySlotRole.EasySupport,
                "LONG_RUN" => PreparationRunwaySlotRole.LongRun,
                _ => throw new InvalidOperationException($"Unexpected role {session.StructuralRole}."),
            },
            ReferenceEquals(session, missed)
                ? LongHorizonRollingSessionOutcomeStatus.NotToday
                : LongHorizonRollingSessionOutcomeStatus.Completed,
            SessionPlanningStatus.Active,
            NotTodayReason: ReferenceEquals(session, missed) ? NotTodayReasonCode.ScheduleConflict : null)).ToArray();

        return NextWindowLoadDecisionPolicy.Evaluate(WindowExecutionSummaryBuilder.Build(evidence)).LoadDecision;
    }

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync(int daysPerWeek)
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        var (key, version) = daysPerWeek switch
        {
            3 => (V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayBeginnerCandidateVersion),
            4 => (V1CatalogPilotIdentityPolicy.HalfMarathonFourDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonFourDayBeginnerCandidateVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek)),
        };
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(key, version);
    }

    private static DynamicCoreCalendarMaterializationOrchestrator Pipeline() => new(
        new DynamicCoreSessionPrescriptionOrchestrator(
            new DynamicCoreVolumeAndLongRunOrchestrator(
                new DynamicCoreWorkoutBindingOrchestrator(
                    new DynamicCoreWeekSkeletonOrchestrator(
                        new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                        new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                    new CatalogWorkoutProgressionLoader(Options.Create(OptionsValue)),
                    new ProgressionStageAllocator(), new GeneratedCatalogStageScheduleValidator(),
                    new CatalogWeekSkeletonCalendarMaterializer(), new DatedGeneratedCatalogPlanSkeletonValidator(),
                    new CatalogWorkoutBinder(), new BoundCatalogPlanValidator()),
                new CatalogPrescriptionContextBuilder(), new CatalogVolumeAndLongRunPlanner()),
            new CatalogSessionPrescriptionPlanner(), new CatalogFinalPrescribedPlanFinalizer()));

    private static DynamicCoreCalendarMaterializationContext Context(
        PlanCatalogCandidateSummary candidate,
        int daysPerWeek,
        bool exactPaceEvidence,
        int targetWeekCount = 14)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(targetWeekCount * 7 - 1);
        var goalKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon);
        // Golden-fixture RecentWeeklyVolumeKm equals each policy's own GoldenFixtureStartingVolumeKm
        // (15.5 for 3D, 19.0 for 4D), so the 14W preferred-horizon test resolves cleanly to the
        // selected peak with no residual scaling.
        var recentWeeklyVolumeKm = daysPerWeek == 3 ? 15.5 : 19.0;
        var recentLongestRunKm = daysPerWeek == 3 ? 8d : 9d;
        var preferredDays = daysPerWeek == 3
            ? new[] { Weekday.Tue, Weekday.Thu, Weekday.Sun }
            : new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun };
        var preferredDaysOfWeek = daysPerWeek == 3
            ? new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Sunday }
            : new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };

        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.HalfMarathon, Level = RunningBackground.Beginner,
            DaysPerWeek = daysPerWeek, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = exactPaceEvidence ? 6900 : null,
            PreferredDays = preferredDays, LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = recentWeeklyVolumeKm, RecentLongestRunKm = recentLongestRunKm, RecentRunsPerWeek = daysPerWeek,
            RecentRace = exactPaceEvidence
                ? new RecentRaceInput { Distance = GoalDistance.HalfMarathon, FinishTimeSeconds = 7200, RaceDate = start.AddDays(-21) }
                : null,
        };

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = preferredDaysOfWeek,
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults = exactPaceEvidence
                ? [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
                   RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "WITHIN_REALISTIC_BAND")]
                : [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE"),
                   RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME")],
            PreviewRequest = request,
            ResolverInput = new ResolverInputSnapshot
            {
                RequestedTargetDistanceKm = goalKm, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = goalKm, StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = request.TargetFinishTimeSeconds, DaysPerWeek = daysPerWeek, Level = RunningBackground.Beginner,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(Options.Create(OptionsValue)),
        };
    }
}
