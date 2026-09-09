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

public sealed class Hm2FullDarkVerticalSliceTests
{
    private readonly ITestOutputHelper _output;

    public Hm2FullDarkVerticalSliceTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

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
        Assert.Equal(56, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s =>
            Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        Assert.Equal(new[]
        {
            "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE",
            "BUILD_FASTER_THAN_HM_INTRO", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT",
            "RACE_SPECIFIC_HM_INTRODUCTION", "RACE_SPECIFIC_HM_DEVELOPMENT", "RACE_SPECIFIC_HM_DEVELOPMENT",
            "RACE_SPECIFIC_HM_REHEARSAL", "RACE_SPECIFIC_HM_REHEARSAL", "TAPER_HM_ACTIVATION", "TAPER_HM_ACTIVATION"
        }, prescribed.Weeks.Select(w => w.Sessions.Single(s => s.StructuralRole == "KEY_SESSION").ProgressionStageKey).ToArray());

        var hmPace = prescribed.Sessions.Where(s => s.WorkoutDefinitionKey == "HM_PACE").ToList();
        Assert.Equal(6, hmPace.Count);
        Assert.All(hmPace, s =>
        {
            Assert.Equal(CatalogPacePrescriptionKind.ExactPace, s.Prescription.PacePrescription.Kind);
            Assert.Equal(CatalogPaceSourceSelection.RecentRaceDerived, s.Prescription.PacePrescription.Source);
            Assert.Equal(Math.Round(6000d / GoalDistanceKm.Resolve(GoalDistance.HalfMarathon), 2, MidpointRounding.AwayFromZero),
                s.Prescription.PacePrescription.SecondsPerKilometer);
            Assert.NotEqual(Math.Round(5700d / GoalDistanceKm.Resolve(GoalDistance.HalfMarathon), 2, MidpointRounding.AwayFromZero),
                s.Prescription.PacePrescription.SecondsPerKilometer);
        });

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid);
        Assert.Equal(43d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d);
        });
        var taper = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taper.Length);
        Assert.Equal(30d, taper[0].PlannedWeeklyVolumeKm);
        Assert.Equal(18.5d, taper[1].PlannedWeeklyVolumeKm);

        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key = week.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
            var taperPosition = volumeWeek.IsTaperWeek ? Array.IndexOf(taper, volumeWeek) + 1 : 0;
            _output.WriteLine(
                $"W{week.WeekNumber:D2}|{week.PhaseKey}|{week.PlannedWeeklyVolumeKm:F1}|{longRunWeek.PlannedLongRunDistanceKm:F1}|{longRunWeek.LongRunShareOfWeeklyVolume:P2}|{key.ProgressionStageKey}|{key.WorkoutDefinitionKey} v{key.WorkoutDefinitionVersion}|T{taperPosition}|{volumeWeek.DecisionReason};{key.Prescription.PacePrescription.Source}");
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(14, materialized.Payload.PlannedWeekCount);
        Assert.All(materialized.Payload.Weeks, w =>
            Assert.Equal("HALF_MARATHON_WORKOUT_PROGRESSION", w.Provenance.ProgressionReferenceKey));
    }

    // Backend Integration Phase HM.5 — supersedes the pre-HM.5 assumption (once literally
    // named "NonPreferredCoreLengths_RemainFailClosedUntilExactPhaseAllocationsExist") that
    // every non-14W horizon was infeasible. HM.4 froze real phase/stage Min/Preferred/Max
    // headroom; HM.5 authored it into half-marathon-master.v1.json and
    // half-marathon-workout-progression.v1.json (in place -- both remain VALIDATED, not yet
    // PUBLISHED, matching the direct repository precedent of commit 1cc7a40's own in-place
    // HALF_MARATHON_MASTER edit). Every one of these seven horizons is now mathematically
    // feasible at the phase level (exactly reproducing HM.4's oracle table) AND fully
    // materializable end-to-end through the real, unmodified dark
    // DynamicCoreCalendarMaterializationOrchestrator pipeline -- the same pipeline this class's
    // own PreferredFourteenWeekCore_... test already exercises for 14W. HM remains dark (see
    // HalfMarathonIdentity_RemainsOutsideEveryPublicGate, unchanged): none of this activates
    // any public route.
    // 12W, 15W, and 16W are deliberately excluded from this theory -- see
    // TwelveAndFifteenWeekHorizons_HitGenuineLongRunShareNumericSafetyBlocker and
    // SixteenWeekHorizon_HitsGenuinePeakVolumeCeilingOvershootBlocker below, which prove and
    // document real, pre-existing volume/long-run-planner numeric-safety blockers for those
    // three horizons instead of silently asserting success.
    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })] // frozen, unchanged -- included for table completeness
    public async Task NonPreferredCoreLengths_NowFeasible_TraverseRealPipelineAndMaterializeValidDarkPreviews(
        int targetWeekCount, int[] expectedPhaseWeeks)
    {
        var candidate = await CandidateAsync();

        // Section 5/7: generic phase resolver proof -- exact HM.4 oracle table, real
        // unmodified CatalogPhaseAllocationResolver, no per-horizon hardcoded branch.
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);
        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, allocation.Phases.Select(p => p.PhaseKey));
        Assert.Equal(expectedPhaseWeeks, allocation.Phases.Select(p => p.AllocatedWeeks));
        Assert.Equal(2, allocation.Phases.Single(p => p.PhaseKey == "TAPER").AllocatedWeeks);
        Assert.True(allocation.Phases.Single(p => p.PhaseKey == "RACE_SPECIFIC").AllocatedWeeks <= 5);

        // Section 11: full dark dynamic-core materialization -- the real, unmodified pipeline
        // (identical wiring to the 14W golden test above).
        var result = await Pipeline().MaterializeAsync(Context(candidate, exactPaceEvidence: true, targetWeekCount));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(targetWeekCount, prescribed.Weeks.Count);
        Assert.Equal(targetWeekCount * 4, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count));
        Assert.Equal(expectedPhaseWeeks, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s =>
            Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        // Section 6/8: real stage allocator proof -- every KEY-lane week resolves to a real,
        // already-catalogued HM stage; RACE_SPECIFIC_HM_REHEARSAL (protected/fixed) always
        // appears exactly twice; TAPER_HM_ACTIVATION always appears exactly twice; no
        // unsupported/invented stage.
        var keyStages = prescribed.Weeks.Select(w => w.Sessions.Single(s => s.StructuralRole == "KEY_SESSION").ProgressionStageKey).ToArray();
        var knownStages = new HashSet<string>
        {
            "FOUNDATION_AEROBIC_BASE", "BUILD_FASTER_THAN_HM_INTRO", "BUILD_THRESHOLD_DEVELOPMENT",
            "RACE_SPECIFIC_HM_INTRODUCTION", "RACE_SPECIFIC_HM_DEVELOPMENT", "RACE_SPECIFIC_HM_DEVELOPMENT_CURRENT_FITNESS_FALLBACK",
            "RACE_SPECIFIC_HM_REHEARSAL", "RACE_SPECIFIC_HM_REHEARSAL_CURRENT_FITNESS_FALLBACK",
            "TAPER_HM_ACTIVATION", "TAPER_HM_ACTIVATION_EFFORT_FALLBACK",
        };
        Assert.All(keyStages, s => Assert.Contains(s, knownStages));
        Assert.Equal(2, keyStages.Count(s => s is "RACE_SPECIFIC_HM_REHEARSAL" or "RACE_SPECIFIC_HM_REHEARSAL_CURRENT_FITNESS_FALLBACK"));
        Assert.Equal(2, keyStages.Count(s => s is "TAPER_HM_ACTIVATION" or "TAPER_HM_ACTIVATION_EFFORT_FALLBACK"));

        // Section 12/13/14: volume/long-run/taper safety -- every horizon still obeys the
        // already-frozen HM VolumeSafetyPolicy fields (0.40 hard long-run-share cap, 19km
        // preferred absolute peak long-run ceiling, non-chained taper). No horizon is asserted
        // to reach the 43km reference peak -- shorter cores are explicitly NOT required to
        // chase it.
        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid);
        Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= 43d + 0.001d,
            $"targetWeekCount={targetWeekCount}: PeakVolumeKm={volume.WeeklyVolumePlan.PeakVolumeKm} exceeds the 43.0km reference ceiling.");
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });
        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).OrderBy(w => w.WeekNumber).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        Assert.True(taperWeeks[0].PlannedWeeklyVolumeKm >= taperWeeks[1].PlannedWeeklyVolumeKm,
            $"Taper must be non-chained/non-increasing for every horizon (week1={taperWeeks[0].PlannedWeeklyVolumeKm}, week2={taperWeeks[1].PlannedWeeklyVolumeKm}).");

        // Section 19: HM stays dark regardless of newly-feasible catalog headroom.
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
    }

    // Section 13/14 (HM.5.1 report) -- genuine, real numeric-safety blocker, NOT papered
    // over. Phase allocation for 12W/15W IS mathematically feasible (proven below), and the
    // stage schedule IS valid, but the volume/long-run planner's mechanical peak-interpolation
    // + long-run-share resolution produces a specific mid-cycle week whose planned long-run
    // distance exceeds the already-frozen 40% HardLongRunShareCap for these two horizons
    // specifically (12W week 10; 15W week 13) -- confirmed by the real, unmodified
    // CatalogFinalPrescribedPlanValidator's own fail-closed check. Per the governing
    // instruction ("if any shorter horizon requires exceeding approved progression merely to
    // reach the 43km reference: DO NOT modify growth policy here -- the correct result is a
    // lower reachable peak. If the current planner cannot represent that, classify the exact
    // numeric-mechanism blocker"), no VolumeSafetyPolicy field, growth-rate coefficient, or
    // long-run-share constant was touched to make this pass -- the planner's own mechanism is
    // the correct fail-closed behavior until a future phase revisits ResolvePeak's
    // interpolation for these two specific horizons.
    [Theory]
    [InlineData(12, "FINAL_WEEK_10_LONG_RUN_SHARE_EXCEEDS_CAP")]
    [InlineData(15, "FINAL_WEEK_13_LONG_RUN_SHARE_EXCEEDS_CAP")]
    public async Task TwelveAndFifteenWeekHorizons_HitGenuineLongRunShareNumericSafetyBlocker(int targetWeekCount, string expectedErrorCode)
    {
        var candidate = await CandidateAsync();

        // Phase allocation itself IS feasible (Layer A headroom works correctly for 12W/15W).
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);

        // But the real, unmodified volume/long-run planner's downstream final-plan validation
        // fails closed on a genuine long-run-share safety violation -- not an exception this
        // phase invented, and not one it silently worked around.
        var ex = await Assert.ThrowsAsync<DynamicCoreSessionPrescriptionFailedException>(() =>
            Pipeline().MaterializeAsync(Context(candidate, exactPaceEvidence: true, targetWeekCount)));
        var cause = Assert.IsType<CatalogFinalPrescribedPlanInvalidException>(ex.InnerException);
        Assert.Contains(expectedErrorCode, cause.Message);
    }

    // Section 14 (HM.5.1 report) -- genuine, real numeric-safety blocker for the LONGEST
    // horizon, NOT papered over. Phase allocation for 16W IS mathematically feasible (Foundation
    // extends to 4, Build extends to 5, both within HM.4's approved extension headroom), and the
    // stage schedule IS valid, but ResolvePeak's golden-fixture-calibrated interpolation
    // (`transitionAdjustedMultiplier = 1 + ((canonicalDefaultMultiplier - 1) * transitions /
    // GoldenFixtureNonTaperTransitions)`, CatalogVolumeAndLongRunPlanner.cs line ~326-328)
    // linearly extrapolates PAST the 14W golden calibration point once 16W's larger non-taper
    // transition count exceeds GoldenFixtureNonTaperTransitions, producing a resolved peak of
    // 46.5km -- ABOVE the frozen 43.0km ResolvedPeakReferenceKm ceiling, confirmed by direct
    // observation of the real, unmodified planner. Per the governing instruction ("Extension
    // should provide more progression room, not invent a higher HM peak target" / "DO NOT
    // modify growth policy here"), no VolumeSafetyPolicy field or ResolvePeak formula was
    // touched to force this under 43.0km -- the overshoot is reported precisely rather than
    // silently capped or hidden.
    [Fact]
    public async Task SixteenWeekHorizon_HitsGenuinePeakVolumeCeilingOvershootBlocker()
    {
        var candidate = await CandidateAsync();
        const int targetWeekCount = 16;

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);

        var result = await Pipeline().MaterializeAsync(Context(candidate, exactPaceEvidence: true, targetWeekCount));
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;

        // The real, unmodified planner currently resolves 46.5km here -- strictly greater than
        // the frozen 43.0km reference ceiling. Pinned as an exact value (not just "> 43") so
        // this test breaks loudly, not silently, the moment a future phase changes this number
        // in either direction.
        Assert.Equal(46.5d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm > 43d,
            "This assertion documents a genuine, unresolved numeric-mechanism blocker (see PHASE_HM_5_1 report section 14) -- it is expected to fail (i.e. the overshoot to disappear) only once a future phase revisits ResolvePeak's extrapolation for horizons beyond the 14W golden calibration point.");
    }

    // Section 5: boundary proof -- 9W remains below the canonical floor (sum of minimums),
    // 17W remains above the canonical ceiling (sum of maximums). Neither Runway nor
    // LongHorizon is activated by this assertion.
    [Theory]
    [InlineData(9)]
    [InlineData(17)]
    public async Task OutOfCanonicalRangeHorizons_RemainInfeasibleAfterHm5Headroom(int targetWeekCount)
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
    public async Task MissingIndependentExactPaceEvidence_HoldsForEveryNewlyFeasibleHorizon()
    {
        // 12W and 15W are excluded here -- see
        // NonPreferredCoreLengths_NowFeasible_TraverseRealPipelineAndMaterializeValidDarkPreviews,
        // which documents a genuine, pre-existing FINAL_WEEK_*_LONG_RUN_SHARE_EXCEEDS_CAP
        // numeric-safety blocker for those two horizons specifically (see PHASE_HM_5_1's own
        // report section 13/14) -- not something this test should silently paper over.
        foreach (var targetWeekCount in new[] { 10, 11, 13 })
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
        Assert.Equal(4, sessions.Count(s => s.FallbackProvenance is not null && s.WorkoutDefinitionKey == "THRESHOLD_TEMPO"));
        Assert.Equal(2, sessions.Count(s => s.FallbackProvenance is not null && s.WorkoutDefinitionKey == "EASY_STANDARD"));
        Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
    }

    [Fact]
    public async Task MissingWeeklyVolume_DoesNotBorrowTenKStartingDefault()
    {
        var context = Context(await CandidateAsync(), exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = null;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() =>
            Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    [Fact]
    public void HalfMarathonIdentity_RemainsOutsideEveryPublicGate()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
    }

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey,
            V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateVersion);
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
            DaysPerWeek = 4, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = exactPaceEvidence ? 5700 : null,
            PreferredDays = [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun], LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 25, RecentLongestRunKm = 10, RecentRunsPerWeek = 4,
            RecentRace = exactPaceEvidence
                ? new RecentRaceInput { Distance = GoalDistance.HalfMarathon, FinishTimeSeconds = 6000, RaceDate = start.AddDays(-21) }
                : null,
        };

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday],
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
                TargetFinishTimeSeconds = request.TargetFinishTimeSeconds, DaysPerWeek = 4, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(Options.Create(OptionsValue)),
        };
    }
}
