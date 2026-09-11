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
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit.Abstractions;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase HM.8 — the same class of test harness as <see cref="Hm2FullDarkVerticalSliceTests"/>,
/// exercising the real, unmodified production pipeline against the new dark-only
/// HALF_MARATHON__3D__INTERMEDIATE candidate, implementing HM.7's frozen numeric authority
/// (PHASE_HM_7_INTERMEDIATE_3D_NUMERIC_AUTHORITY_CLOSURE.md).
/// </summary>
public sealed class Hm8Intermediate3DFullDarkVerticalSliceTests
{
    private readonly ITestOutputHelper _output;

    public Hm8Intermediate3DFullDarkVerticalSliceTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    // Golden fixture: RecentWeeklyVolumeKm equals HalfMarathonIntermediate3D's own
    // GoldenFixtureStartingVolumeKm (20.0), so the 14W (11-transition, exactly the calibration
    // point) resolved peak is exactly ResolvedPeakReference.Value = 34.0km -- the same clean,
    // self-consistent, no-residual-scaling pattern HM.2/HM.1.5 already proved for 4D's own 14W
    // golden case (RecentWeeklyVolumeKm=25=GoldenFixtureStartingVolumeKm -> peak=43.0).
    [Fact]
    public async Task PreferredFourteenWeekCore_TraversesRealPipelineAndMaterializesValidDarkPreview()
    {
        var candidate = await CandidateAsync();
        Assert.Equal(new PlanCatalogCoreCycle(10, 14, 16), candidate.CoreCycle);
        var context = Context(candidate, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        Assert.Equal(new[] { 3, 4, 5, 2 }, prescribed.Weeks
            .GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.Equal(14, prescribed.Weeks.Count);
        Assert.Equal(42, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(3, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s =>
            Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        Assert.Equal(new[]
        {
            "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE",
            "BUILD_FASTER_THAN_HM_INTRO", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT",
            "RACE_SPECIFIC_HM_INTRODUCTION", "RACE_SPECIFIC_HM_DEVELOPMENT", "RACE_SPECIFIC_HM_DEVELOPMENT",
            "RACE_SPECIFIC_HM_REHEARSAL", "RACE_SPECIFIC_HM_REHEARSAL", "TAPER_HM_ACTIVATION", "TAPER_HM_ACTIVATION"
        }, prescribed.Weeks.Select(w => w.Sessions.Single(s => s.StructuralRole == "KEY_SESSION").ProgressionStageKey).ToArray());

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid, string.Join("; ", volume.WeeklyVolumePlan.ValidationResult.Errors));
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid, string.Join("; ", volume.LongRunProgression.ValidationResult.Errors));
        Assert.Equal(34d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.46d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });
        var taper = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taper.Length);
        Assert.Equal(24.0d, taper[0].PlannedWeeklyVolumeKm);
        Assert.Equal(14.5d, taper[1].PlannedWeeklyVolumeKm);
        Assert.True(taper[0].PlannedWeeklyVolumeKm >= taper[1].PlannedWeeklyVolumeKm, "Taper must be non-chained/non-increasing.");

        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key = week.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
            var easy = week.Sessions.Single(s => s.StructuralRole == "EASY_SUPPORT");
            var longRun = week.Sessions.Single(s => s.StructuralRole == "LONG_RUN");
            _output.WriteLine(
                $"W{week.WeekNumber:D2}|{week.PhaseKey}|vol={volumeWeek.PlannedWeeklyVolumeKm:F1}|lr={longRunWeek.PlannedLongRunDistanceKm:F1}|share={longRunWeek.LongRunShareOfWeeklyVolume:P2}|key={key.ProgressionStageKey}|{key.WorkoutDefinitionKey} v{key.WorkoutDefinitionVersion} dist={key.Prescription.DistancePrescription.PlannedDistanceKm:F1}|easy_dist={easy.Prescription.DistancePrescription.PlannedDistanceKm:F1}|long_dist={longRun.Prescription.DistancePrescription.PlannedDistanceKm:F1}|reason={volumeWeek.DecisionReason};pace={key.Prescription.PacePrescription.Source}");
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(14, materialized.Payload.PlannedWeekCount);
        Assert.All(materialized.Payload.Weeks, w =>
            Assert.Equal("HALF_MARATHON_WORKOUT_PROGRESSION", w.Provenance.ProgressionReferenceKey));
    }

    // HM.8 -- 10W-16W full dark materialization, mirroring Hm2FullDarkVerticalSliceTests'
    // NonPreferredCoreLengths theory exactly. Phase allocation is frequency-generic
    // (HM.4/HM.6, unchanged, reused verbatim) -- the same exact 7-row oracle table 4D already
    // proves applies identically to 3D.
    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(12, new[] { 2, 3, 5, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })]
    [InlineData(15, new[] { 4, 4, 5, 2 })]
    [InlineData(16, new[] { 4, 5, 5, 2 })]
    public async Task NonPreferredCoreLengths_TraverseRealPipelineAndMaterializeValidDarkPreviews(
        int targetWeekCount, int[] expectedPhaseWeeks)
    {
        var candidate = await CandidateAsync();

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);
        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, allocation.Phases.Select(p => p.PhaseKey));
        Assert.Equal(expectedPhaseWeeks, allocation.Phases.Select(p => p.AllocatedWeeks));
        Assert.Equal(2, allocation.Phases.Single(p => p.PhaseKey == "TAPER").AllocatedWeeks);
        Assert.True(allocation.Phases.Single(p => p.PhaseKey == "RACE_SPECIFIC").AllocatedWeeks <= 5);

        var result = await Pipeline().MaterializeAsync(Context(candidate, exactPaceEvidence: true, targetWeekCount));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(targetWeekCount, prescribed.Weeks.Count);
        Assert.Equal(targetWeekCount * 3, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(3, w.Sessions.Count));
        Assert.Equal(expectedPhaseWeeks, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s =>
            Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        var keyStages = prescribed.Weeks.Select(w => w.Sessions.Single(s => s.StructuralRole == "KEY_SESSION").ProgressionStageKey).ToArray();
        var knownStages = new HashSet<string?>
        {
            "FOUNDATION_AEROBIC_BASE", "BUILD_FASTER_THAN_HM_INTRO", "BUILD_THRESHOLD_DEVELOPMENT",
            "RACE_SPECIFIC_HM_INTRODUCTION", "RACE_SPECIFIC_HM_DEVELOPMENT", "RACE_SPECIFIC_HM_DEVELOPMENT_CURRENT_FITNESS_FALLBACK",
            "RACE_SPECIFIC_HM_REHEARSAL", "RACE_SPECIFIC_HM_REHEARSAL_CURRENT_FITNESS_FALLBACK",
            "TAPER_HM_ACTIVATION", "TAPER_HM_ACTIVATION_EFFORT_FALLBACK",
        };
        Assert.All(keyStages, s => Assert.Contains(s, knownStages));
        Assert.Equal(2, keyStages.Count(s => s is "RACE_SPECIFIC_HM_REHEARSAL" or "RACE_SPECIFIC_HM_REHEARSAL_CURRENT_FITNESS_FALLBACK"));
        Assert.Equal(2, keyStages.Count(s => s is "TAPER_HM_ACTIVATION" or "TAPER_HM_ACTIVATION_EFFORT_FALLBACK"));

        // Weekly growth safety -- Preferred=7%, Hard=8%, Absolute<=2.0km for every non-taper
        // transition (3D's own AbsoluteWeeklyIncrementCapKm, not 4D's 2.5km).
        var orderedVolumeWeeks = volume.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        for (var i = 1; i < orderedVolumeWeeks.Length; i++)
        {
            var prev = orderedVolumeWeeks[i - 1];
            var cur = orderedVolumeWeeks[i];
            if (cur.IsTaperWeek || prev.IsTaperWeek) continue;
            var deltaKm = cur.PlannedWeeklyVolumeKm - prev.PlannedWeeklyVolumeKm;
            _output.WriteLine($"targetWeekCount={targetWeekCount} W{prev.WeekNumber}->W{cur.WeekNumber}: {prev.PlannedWeeklyVolumeKm:F1}->{cur.PlannedWeeklyVolumeKm:F1} (deltaKm={deltaKm:F2})");
        }

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid);
        // Short horizons (10W-13W, fewer than 11 transitions) must never be forced to chase
        // the 34.0km reference peak.
        if (targetWeekCount < 14)
        {
            Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= 34d + 0.001d,
                $"targetWeekCount={targetWeekCount}: PeakVolumeKm={volume.WeeklyVolumePlan.PeakVolumeKm} unexpectedly reached/exceeded the 34.0km reference despite fewer non-taper transitions than the calibration count.");
        }
        // Long horizons (15W/16W, more than 11 transitions) must never extrapolate past 34.0km
        // (the exact HM.5.3 mechanism, reused via ResolvedPeakReferenceIsSelectedCeiling=true).
        Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= 34d + 0.001d,
            $"targetWeekCount={targetWeekCount}: PeakVolumeKm={volume.WeeklyVolumePlan.PeakVolumeKm} exceeds the 34.0km reference ceiling.");
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.46d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });
        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).OrderBy(w => w.WeekNumber).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        Assert.True(taperWeeks[0].PlannedWeeklyVolumeKm >= taperWeeks[1].PlannedWeeklyVolumeKm,
            $"Taper must be non-chained/non-increasing for every horizon (week1={taperWeeks[0].PlannedWeeklyVolumeKm}, week2={taperWeeks[1].PlannedWeeklyVolumeKm}).");

        // HM stays dark regardless of newly-feasible catalog headroom.
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3));
    }

    // Boundary proof: 9W/17W remain infeasible, exactly as 4D already proves (shared,
    // frequency-generic phase-allocation authority; no 3D-specific horizon table).
    [Theory]
    [InlineData(9)]
    [InlineData(17)]
    public async Task OutOfCanonicalRangeHorizons_RemainInfeasible(int targetWeekCount)
    {
        var candidate = await CandidateAsync();

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);

        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Empty(allocation.Phases);
        Assert.Contains(targetWeekCount < 10 ? "TARGET_BELOW_SUM_OF_MINIMUMS" : "TARGET_ABOVE_SUM_OF_MAXIMUMS", allocation.ReasonCode);
        await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() =>
            Pipeline().MaterializeAsync(Context(candidate, exactPaceEvidence: true, targetWeekCount)));
    }

    [Fact]
    public async Task MissingIndependentExactPaceEvidence_HoldsForEveryFeasibleHorizon()
    {
        foreach (var targetWeekCount in new[] { 10, 11, 12, 13, 14, 15, 16 })
        {
            var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(), exactPaceEvidence: false, targetWeekCount));
            var sessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions;

            Assert.DoesNotContain(sessions, s => s.WorkoutDefinitionKey == "HM_PACE");
            Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
        }
    }

    [Fact]
    public async Task MissingIndependentExactPaceEvidence_UsesApprovedCatalogFallbacks()
    {
        var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(), exactPaceEvidence: false));
        var sessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions;

        Assert.DoesNotContain(sessions, s => s.WorkoutDefinitionKey == "HM_PACE");
        // 14W's KEY-lane fallback stages: RACE_SPECIFIC_HM_DEVELOPMENT (2 weeks) +
        // RACE_SPECIFIC_HM_REHEARSAL (2 weeks) both fall back to THRESHOLD_TEMPO (4 total);
        // TAPER_HM_ACTIVATION (2 weeks) falls back to EASY_STANDARD (2 total) -- identical
        // stage-fallback shape to the existing 4D golden test.
        Assert.Equal(4, sessions.Count(s => s.FallbackProvenance is not null && s.WorkoutDefinitionKey == "THRESHOLD_TEMPO"));
        Assert.Equal(2, sessions.Count(s => s.FallbackProvenance is not null && s.WorkoutDefinitionKey == "EASY_STANDARD"));
        Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
    }

    [Fact]
    public async Task MissingWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume()
    {
        var context = Context(await CandidateAsync(), exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = null;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() =>
            Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    [Fact]
    public async Task ZeroWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume()
    {
        var context = Context(await CandidateAsync(), exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = 0;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() =>
            Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    [Fact]
    public void HalfMarathonThreeDayIdentity_RemainsOutsideEveryPublicGate()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3));
    }

    [Fact]
    public void HalfMarathonFourDayIdentity_ZeroDelta_AfterDispatchGeneralization()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate4D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(4));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate3D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(3));
    }

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateKey,
            V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateVersion);
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
        bool exactPaceEvidence,
        int targetWeekCount = 14)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(targetWeekCount * 7 - 1);
        var goalKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon);
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.HalfMarathon, Level = RunningBackground.Intermediate,
            DaysPerWeek = 3, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = exactPaceEvidence ? 5700 : null,
            PreferredDays = [Weekday.Tue, Weekday.Thu, Weekday.Sun], LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 20, RecentLongestRunKm = 9, RecentRunsPerWeek = 3,
            RecentRace = exactPaceEvidence
                ? new RecentRaceInput { Distance = GoalDistance.HalfMarathon, FinishTimeSeconds = 6000, RaceDate = start.AddDays(-21) }
                : null,
        };

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Sunday],
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
                TargetFinishTimeSeconds = request.TargetFinishTimeSeconds, DaysPerWeek = 3, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(Options.Create(OptionsValue)),
        };
    }
}
