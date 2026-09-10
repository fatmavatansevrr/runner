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
    // HM.5.3 -- 12W, 15W, and 16W now join this theory. Before HM.5.3 these three horizons hit
    // real, genuine numeric-mechanism blockers (12W/15W: FINAL_WEEK_*_LONG_RUN_SHARE_EXCEEDS_CAP
    // from a long-run hard-cap rounding-to-nearest defect that could round the clamp ceiling UP
    // past the true 40% share; 16W: ResolvePeak's golden-fixture interpolation extrapolating past
    // its own calibration point, resolving 46.5km > the frozen 43.0km reference peak). Both
    // defects are fixed at their smallest shared seam in CatalogVolumeAndLongRunPlanner
    // (ResolvePeak's transition count is now capped at GoldenFixtureNonTaperTransitions before
    // driving the interpolation ratio; the long-run hard-cap ceiling now rounds DOWN, never
    // to-nearest, so it can never exceed the true percentage/absolute limit it represents) --
    // see PHASE_HM_5_3_VOLUME_AND_LONG_RUN_BOUNDARY_SEMANTIC_CLOSURE.md. No VolumeSafetyPolicy
    // field, phase/stage allocation, or taper multiplier was touched; 14W's golden numbers are
    // unchanged (still exactly 43/30/18.5, proven by the still-passing 14W golden test above).
    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(12, new[] { 2, 3, 5, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })] // frozen, unchanged -- included for table completeness
    [InlineData(15, new[] { 4, 4, 5, 2 })]
    [InlineData(16, new[] { 4, 5, 5, 2 })]
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

    // HM.5.3 -- 12W/15W previously threw FINAL_WEEK_10_LONG_RUN_SHARE_EXCEEDS_CAP (12W) /
    // FINAL_WEEK_13_LONG_RUN_SHARE_EXCEEDS_CAP (15W) from CatalogFinalPrescribedPlanValidator.
    // Root cause: CatalogVolumeAndLongRunPlanner.BuildLongRunPlan computed the long-run
    // hard-percentage-of-weekly-volume clamp CEILING via nearest-rounding
    // (Round(weeklyVolume * hardCapShare)), which can round a compliant raw ceiling UP past the
    // true 40% limit (e.g. weeklyVolume=41.0 -> raw ceiling 16.4km -> nearest-rounded to
    // 16.5km); the planner then clamped `selected` up to that inflated ceiling, which
    // CatalogFinalPrescribedPlanValidator's own unrounded 40%-of-weekly-volume check (with only
    // a 0.001km tolerance) correctly rejected. This is now fixed by having that ceiling round
    // DOWN (floor to the rounding grid) instead, so the planner's own clamp ceiling can never
    // exceed the true limit the final validator independently checks against -- restoring
    // planner/validator parity without touching the 30%/33%/36%/40% share authorities, the
    // 19km absolute ceiling, or any phase/stage allocation. Both horizons now materialize a
    // fully valid final prescribed plan end-to-end.
    [Theory]
    [InlineData(12, 10)]
    [InlineData(15, 13)]
    public async Task TwelveAndFifteenWeekHorizons_NoLongerHitLongRunShareNumericSafetyBlocker(int targetWeekCount, int previouslyOffendingWeekNumber)
    {
        var candidate = await CandidateAsync();

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);

        var result = await Pipeline().MaterializeAsync(Context(candidate, exactPaceEvidence: true, targetWeekCount));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid);

        // The specific previously-offending week is now compliant with the hard 40% share cap
        // (post-rounding -- see BuildLongRunPlan's RoundDown safety-ceiling comment), and every
        // other week is too.
        var offendingWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == previouslyOffendingWeekNumber);
        Assert.True(offendingWeek.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d,
            $"Week {previouslyOffendingWeekNumber} share={offendingWeek.LongRunShareOfWeeklyVolume:P4} still exceeds the 40% hard cap.");
        var offendingSession = prescribed.Weeks.Single(w => w.WeekNumber == previouslyOffendingWeekNumber)
            .Sessions.Single(s => s.StructuralRole == "LONG_RUN");
        var offendingWeeklyVolume = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == previouslyOffendingWeekNumber).PlannedWeeklyVolumeKm;
        Assert.True(offendingSession.PlannedDistanceKm <= offendingWeeklyVolume * 0.40d + 0.001d,
            $"Week {previouslyOffendingWeekNumber} bound session distance={offendingSession.PlannedDistanceKm} still exceeds the true 40% ceiling of {offendingWeeklyVolume} weekly km.");
        Assert.All(volume.LongRunProgression.Weeks, w => Assert.True(w.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d));
        Assert.All(volume.LongRunProgression.Weeks, w => Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d));
        Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= 43d + 0.001d);
    }

    // HM.5.3 -- 16W previously resolved a peak weekly volume of 46.5km, strictly greater than
    // the frozen 43.0km HALF_MARATHON x INTERMEDIATE x 4D selected/reference peak. Root cause:
    // ResolvePeak's golden-fixture-calibrated interpolation
    // (`transitionAdjustedMultiplier = 1 + ((canonicalDefaultMultiplier - 1) * transitions /
    // GoldenFixtureNonTaperTransitions)`) linearly EXTRAPOLATED past the 14W golden calibration
    // point once 16W's larger non-taper transition count (13) exceeded
    // GoldenFixtureNonTaperTransitions (11), treating "more Core weeks" as authority for "a
    // higher peak" -- which the frozen HM authority forbids (more weeks give more time to reach
    // the SAME selected peak, never a higher one). Fixed by capping the transition count fed
    // into the interpolation ratio at GoldenFixtureNonTaperTransitions, so the multiplier
    // saturates at the calibrated ratio for any horizon at or beyond the calibration point
    // instead of extrapolating past it. For this fixture (starting volume equals
    // GoldenFixtureStartingVolumeKm), the resolved peak is now exactly 43.0km -- not a new
    // number, the same already-frozen reference peak 14W already resolves to.
    [Fact]
    public async Task SixteenWeekHorizon_NoLongerOvershootsThePeakVolumeCeiling()
    {
        var candidate = await CandidateAsync();
        const int targetWeekCount = 16;

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);

        var result = await Pipeline().MaterializeAsync(Context(candidate, exactPaceEvidence: true, targetWeekCount));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid);
        Assert.Equal(43d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= 43d + 0.001d,
            "16W must not resolve a peak above the frozen 43.0km reference merely because it has more Core weeks than 14W.");
        Assert.All(volume.LongRunProgression.Weeks, w => Assert.True(w.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d));
        // Section 15's frozen ceiling-not-target invariant: even the longest horizon does not
        // need to reach 19km. At 43km/week, 40% is 17.2km, so ~17km remains a fully correct peak.
        Assert.All(volume.LongRunProgression.Weeks, w => Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d));
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
        // HM.5.3 -- 12W, 15W, and 16W now join this list: the FINAL_WEEK_*_LONG_RUN_SHARE_EXCEEDS_CAP
        // (12W/15W) and peak-volume-ceiling-overshoot (16W) blockers that previously excluded
        // them are fixed (see TwelveAndFifteenWeekHorizons_NoLongerHitLongRunShareNumericSafetyBlocker
        // and SixteenWeekHorizon_NoLongerOvershootsThePeakVolumeCeiling above).
        foreach (var targetWeekCount in new[] { 10, 11, 12, 13, 15, 16 })
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
