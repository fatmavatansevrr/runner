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
/// PHASE FIVE-K.1 -- the same class of real-pipeline test harness as
/// <see cref="Hm8Intermediate3DFullDarkVerticalSliceTests"/>/<see cref="Hm2FullDarkVerticalSliceTests"/>,
/// exercising the real, unmodified production pipeline against the new, PUBLICLY-ACTIVATED
/// FIVE_K__4D__INTERMEDIATE / FIVE_K__5D__INTERMEDIATE candidates, implementing FIVE-K.0B's
/// frozen restricted V1 matrix authority
/// (PHASE_FIVE_K_0B_CANONICAL_FIVE_K_PRODUCT_DEFAULT_RESOLUTION_FOR_RESTRICTED_V1_MATRIX.md).
/// Unlike the HALF_MARATHON dark-vertical-slice tests this mirrors, FIVE_K Intermediate 4D/5D
/// are public from first activation (FIVE-K.0B §6/§8) -- this suite also asserts the public
/// identity gate directly, not merely the internal generation pipeline.
/// </summary>
public sealed class FiveK1Intermediate4D5DPublicActivationTests
{
    private readonly ITestOutputHelper _output;

    public FiveK1Intermediate4D5DPublicActivationTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    [Fact]
    public void PublicIdentityGate_AdmitsExactlyIntermediate4D5D_AndNothingElse()
    {
        // FIVE-K.0B §8/§36 -- the only two publicly-approved FIVE_K cells.
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.FiveK, RunningBackground.Intermediate, 4));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.FiveK, RunningBackground.Intermediate, 5));
        Assert.Equal((V1CatalogPilotIdentityPolicy.FiveKFourDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.FiveK, RunningBackground.Intermediate, 4));
        Assert.Equal((V1CatalogPilotIdentityPolicy.FiveKFiveDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.FiveK, RunningBackground.Intermediate, 5));

        // FIVE-K.0B §37-39 -- every other FIVE_K cell remains deferred/out of V1 scope.
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.FiveK, RunningBackground.Beginner, 4));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.FiveK, RunningBackground.Advanced, 4));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.FiveK, RunningBackground.Intermediate, 2));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.FiveK, RunningBackground.Intermediate, 3));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.FiveK, RunningBackground.Intermediate, 6));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.FiveK, RunningBackground.Experienced, 4));

        // Preparation Runway / LongHorizon are never widened for FIVE_K -- Core only.
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(GoalType.Race, GoalDistance.FiveK, RunningBackground.Intermediate, 4));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(GoalType.Race, GoalDistance.FiveK, RunningBackground.Intermediate, 5));
    }

    [Fact]
    public void VolumeSafetyPolicyDispatch_ResolvesTheFrozenFiveKAuthority_SeparateFromTenKAndHm()
    {
        var fourDay = VolumeSafetyPolicy.ForFiveKIntermediateDaysPerWeek(4);
        var fiveDay = VolumeSafetyPolicy.ForFiveKIntermediateDaysPerWeek(5);
        Assert.Same(VolumeSafetyPolicy.FiveKIntermediate4D, fourDay);
        Assert.Same(VolumeSafetyPolicy.FiveKIntermediate5D, fiveDay);

        // FIVE-K.0B §23/§25 -- band 35.0-44.0 shared; SelectedPeakReference/
        // GoldenFixtureStartingVolumeKm differ by frequency.
        Assert.Equal(38.0d, fourDay.ResolvedPeakReference.Value);
        Assert.Equal(25.0d, fourDay.GoldenFixtureStartingVolumeKm);
        Assert.Equal(42.0d, fiveDay.ResolvedPeakReference.Value);
        Assert.Equal(28.0d, fiveDay.GoldenFixtureStartingVolumeKm);

        // FIVE-K.0B §28 -- long-run share quadruple, identical across both frequencies.
        foreach (var policy in new[] { fourDay, fiveDay })
        {
            Assert.Equal(0.25d, policy.LongRunPreferredMinimumShare);
            Assert.Equal(0.30d, policy.LongRunPreferredMaximumShare);
            Assert.Equal(0.28d, policy.LongRunSelectionShare);
            Assert.Equal(0.33d, policy.LongRunHardCapShare);
            // FIVE-K.0B §29/§30 -- no FIVE_K-specific absolute LR ceiling or starting cap.
            Assert.Null(policy.PreferredAbsolutePeakLongRunKm);
            Assert.Null(policy.StartingAbsoluteLongRunCapKm);
            // FIVE-K.0B §34 -- taper: single, non-chained, FIVE_K-own 0.55 -- distinct from
            // TEN_K's 0.53 and HM's 0.70/0.43.
            Assert.Equal(0.55d, policy.TaperVolumeMultiplier);
            Assert.NotEqual(0.53d, policy.TaperVolumeMultiplier);
            Assert.Null(policy.TaperVolumeMultipliers);
            // FIVE-K.0B §26 -- shared growth mechanism, unchanged values.
            Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
            Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
            Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        }

        // Disclosed coincidence (FIVE-K.0B §25): 4D's 25.0 numerically matches HM's own
        // GoldenFixtureStartingVolumeKm, but is NOT the same authority object/reference.
        Assert.NotSame(VolumeSafetyPolicy.HalfMarathonIntermediate4D, fourDay);
        Assert.Equal(VolumeSafetyPolicy.HalfMarathonIntermediate4D.GoldenFixtureStartingVolumeKm, fourDay.GoldenFixtureStartingVolumeKm);
        Assert.NotEqual(VolumeSafetyPolicy.HalfMarathonIntermediate4D.ResolvedPeakReference.Value, fourDay.ResolvedPeakReference.Value);
    }

    [Theory]
    [InlineData(8, new[] { 1, 3, 3, 1 })]
    [InlineData(9, new[] { 1, 4, 3, 1 })]
    [InlineData(10, new[] { 2, 4, 3, 1 })]
    [InlineData(11, new[] { 3, 4, 3, 1 })]
    [InlineData(12, new[] { 4, 4, 3, 1 })]
    [InlineData(13, new[] { 5, 4, 3, 1 })]
    public async Task PhaseAllocation_MatchesFrozenFiveKB13Table_AcrossEveryHorizon_FourDay(
        int targetWeekCount, int[] expectedPhaseWeeks)
    {
        var candidate = await CandidateAsync(fourDay: true);
        Assert.Equal(new PlanCatalogCoreCycle(8, 10, 13), candidate.CoreCycle);

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);
        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, allocation.Phases.Select(p => p.PhaseKey));
        Assert.Equal(expectedPhaseWeeks, allocation.Phases.Select(p => p.AllocatedWeeks));
        Assert.Equal(targetWeekCount, allocation.Phases.Sum(p => p.AllocatedWeeks));
        // Taper never compressed/extended (FIVE-K.0B §14/§15).
        Assert.Equal(1, allocation.Phases.Single(p => p.PhaseKey == "TAPER").AllocatedWeeks);
        // RaceSpecific floor (3) is never actually reached-below in the 8-13W supported range.
        Assert.Equal(3, allocation.Phases.Single(p => p.PhaseKey == "RACE_SPECIFIC").AllocatedWeeks);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(14)]
    public async Task OutOfCanonicalRangeHorizons_RemainInfeasible(int targetWeekCount)
    {
        var candidate = await CandidateAsync(fourDay: true);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Empty(allocation.Phases);
        Assert.Contains(targetWeekCount < 8 ? "TARGET_BELOW_SUM_OF_MINIMUMS" : "TARGET_ABOVE_SUM_OF_MAXIMUMS", allocation.ReasonCode);
    }

    [Fact]
    public async Task PreferredTenWeekCore_FourDay_TraversesRealPipeline_ProducesFrozenStageSequenceAndPeakVolume()
    {
        var candidate = await CandidateAsync(fourDay: true);
        var context = Context(candidate, fourDay: true, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(10, prescribed.Weeks.Count);
        Assert.Equal(new[] { 2, 4, 3, 1 }, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.Equal(40, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        // FIVE-K.0B §16-17 frozen stage sequence, read directly from the governance document.
        Assert.Equal(new[]
        {
            "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE",
            "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_VO2_INTERVAL_DEVELOPMENT", "BUILD_VO2_INTERVAL_DEVELOPMENT",
            "RACE_SPECIFIC_VO2_CONTINUATION", "RACE_SPECIFIC_REHEARSAL", "RACE_SPECIFIC_REHEARSAL",
            "TAPER_FIVE_K_ACTIVATION"
        }, prescribed.Weeks.Select(w => w.Sessions.Single(s => s.StructuralRole == "KEY_SESSION").ProgressionStageKey).ToArray());

        // Exact FIVE_K identity, never TEN_K/HM-derived or legacy habit-FiveK.
        Assert.All(prescribed.Sessions, s => Assert.DoesNotContain("TEN_K", s.WorkoutDefinitionKey ?? string.Empty));

        // Golden fixture: RecentWeeklyVolumeKm == GoldenFixtureStartingVolumeKm (25.0) and the
        // preferred 10W horizon sits exactly at the calibration transition count (8), so the
        // resolved peak is exactly ResolvedPeakReference.Value = 38.0km, the same clean,
        // no-residual-scaling pattern this engagement's other golden fixtures already use.
        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid, string.Join("; ", volume.WeeklyVolumePlan.ValidationResult.Errors));
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid, string.Join("; ", volume.LongRunProgression.ValidationResult.Errors));
        Assert.Equal(38.0d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.33d + 0.001d);
        });

        var taper = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Single(taper);
        Assert.True(taper[0].PlannedWeeklyVolumeKm < volume.WeeklyVolumePlan.PeakVolumeKm, "Taper volume must drop below peak.");

        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key = week.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
            _output.WriteLine(
                $"W{week.WeekNumber:D2}|{week.PhaseKey}|vol={volumeWeek.PlannedWeeklyVolumeKm:F2}|lr={longRunWeek.PlannedLongRunDistanceKm:F2}|share={longRunWeek.LongRunShareOfWeeklyVolume:P2}|key={key.ProgressionStageKey}|{key.WorkoutDefinitionKey} v{key.WorkoutDefinitionVersion} dist={key.Prescription.DistancePrescription.PlannedDistanceKm:F2}");
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid, string.Join("; ", materialized.ValidationResult.Errors));
        Assert.Equal(10, materialized.Payload.PlannedWeekCount);
        Assert.All(materialized.Payload.Weeks, w => Assert.Equal("FIVE_K_WORKOUT_PROGRESSION_V1", w.Provenance.ProgressionReferenceKey));
    }

    [Fact]
    public async Task PreferredTenWeekCore_FiveDay_NoKey2Invariant_FifthSessionIsPlainEasy()
    {
        var candidate = await CandidateAsync(fourDay: false);
        var context = Context(candidate, fourDay: false, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(10, prescribed.Weeks.Count);
        Assert.Equal(50, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(5, w.Sessions.Count));

        // FIVE-K.0B §19's binding, discriminating no-KEY2 invariant: exactly ONE KEY_SESSION
        // per week (never two), and exactly THREE EASY_SUPPORT (never two, which would be the
        // dual-KEY HALF_MARATHON/TEN_K Intermediate x5D shape) plus one LONG_RUN. A future
        // shared adapter that silently injected a second quality day at 5D would fail this
        // assertion immediately.
        Assert.All(prescribed.Weeks, w =>
        {
            Assert.Equal(1, w.Sessions.Count(s => s.StructuralRole == "KEY_SESSION"));
            Assert.Equal(3, w.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT"));
            Assert.Equal(1, w.Sessions.Count(s => s.StructuralRole == "LONG_RUN"));
        });
        // Zero KEY2 lane ordinal anywhere (HALF_MARATHON's own dual-KEY 5D/6D candidates use
        // LaneOrdinal 1 for the second KEY; FIVE_K's single-KEY layout never produces one).
        Assert.DoesNotContain(prescribed.Sessions, s => s.StructuralRole == "KEY_SESSION" && (s.LaneOrdinal ?? 0) != 0);

        // Golden fixture: RecentWeeklyVolumeKm == 5D's own GoldenFixtureStartingVolumeKm (28.0)
        // -> resolved peak exactly 42.0km at the calibration-point 10W horizon.
        Assert.Equal(42.0d, volume.WeeklyVolumePlan.PeakVolumeKm);
    }

    [Fact]
    public async Task MissingIndependentExactPaceEvidence_UsesApprovedCatalogFallbacks_FourDay()
    {
        var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(fourDay: true), fourDay: true, exactPaceEvidence: false));
        var sessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions;

        Assert.DoesNotContain(sessions, s => s.WorkoutDefinitionKey == "GOAL_PACE_FIVE_K");
        // RACE_SPECIFIC_REHEARSAL (2 weeks) falls back to THRESHOLD_TEMPO;
        // TAPER_FIVE_K_ACTIVATION (1 week) falls back to EASY_STANDARD.
        Assert.Equal(2, sessions.Count(s => s.FallbackProvenance is not null && s.WorkoutDefinitionKey == "THRESHOLD_TEMPO"));
        Assert.Equal(1, sessions.Count(s => s.FallbackProvenance is not null && s.WorkoutDefinitionKey == "EASY_STANDARD"));
        Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
    }

    [Fact]
    public async Task ExactGoalPace_BindsToFiveKDistance_NeverTenKOrDerived()
    {
        var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(fourDay: true), fourDay: true, exactPaceEvidence: true));
        var rehearsal = result.PrescriptionResult.FinalPrescribedPlan.Sessions
            .Where(s => s.WorkoutDefinitionKey == "GOAL_PACE_FIVE_K").ToArray();
        Assert.NotEmpty(rehearsal);
        Assert.All(rehearsal, s =>
        {
            Assert.Equal(CatalogPacePrescriptionKind.ExactPace, s.Prescription.PacePrescription.Kind);
            Assert.Equal(CatalogPaceSourceSelection.TargetGoalDerived, s.Prescription.PacePrescription.Source);
            // TargetFinishTimeSeconds (1200 = 20:00) / GoalDistanceKm (5.0) = 240.0 sec/km.
            Assert.Equal(240.0d, s.Prescription.PacePrescription.SecondsPerKilometer);
        });
    }

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync(bool fourDay)
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            fourDay ? V1CatalogPilotIdentityPolicy.FiveKFourDayIntermediateCandidateKey : V1CatalogPilotIdentityPolicy.FiveKFiveDayIntermediateCandidateKey,
            fourDay ? V1CatalogPilotIdentityPolicy.FiveKFourDayIntermediateCandidateVersion : V1CatalogPilotIdentityPolicy.FiveKFiveDayIntermediateCandidateVersion);
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
        bool fourDay,
        bool exactPaceEvidence,
        int targetWeekCount = 10)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(targetWeekCount * 7 - 1);
        var goalKm = GoalDistanceKm.Resolve(GoalDistance.FiveK);
        var daysPerWeek = fourDay ? 4 : 5;
        var preferredDays = fourDay
            ? new[] { Weekday.Tue, Weekday.Thu, Weekday.Sat, Weekday.Sun }
            : new[] { Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Sat, Weekday.Sun };
        var preferredDowList = fourDay
            ? new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday }
            : new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        var startingVolume = fourDay ? 25d : 28d;
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.FiveK, Level = RunningBackground.Intermediate,
            DaysPerWeek = daysPerWeek, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = exactPaceEvidence ? 1200 : null,
            PreferredDays = preferredDays, LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = startingVolume, RecentLongestRunKm = 8, RecentRunsPerWeek = daysPerWeek,
            RecentRace = null,
        };

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = preferredDowList,
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults = exactPaceEvidence
                ? [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "TARGET_TIME", "TARGET_TIME_PROVIDED"),
                   RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "WITHIN_REALISTIC_BAND")]
                : [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE"),
                   RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME")],
            PreviewRequest = request,
            ResolverInput = new ResolverInputSnapshot
            {
                RequestedTargetDistanceKm = goalKm, CanonicalDistanceFamily = "FIVE_K", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.FiveK, GoalDistanceKm = goalKm, StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = request.TargetFinishTimeSeconds, DaysPerWeek = daysPerWeek, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(Options.Create(OptionsValue)),
        };
    }
}
