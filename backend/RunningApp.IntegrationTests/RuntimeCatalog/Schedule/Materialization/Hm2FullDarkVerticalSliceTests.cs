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

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(15)]
    [InlineData(16)]
    public async Task NonPreferredCoreLengths_RemainFailClosedUntilExactPhaseAllocationsExist(int targetWeekCount)
    {
        var candidate = await CandidateAsync();

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);

        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Empty(allocation.Phases);
        Assert.Contains(targetWeekCount < 14 ? "TARGET_BELOW_SUM_OF_MINIMUMS" : "TARGET_ABOVE_SUM_OF_MAXIMUMS", allocation.ReasonCode);
        await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() =>
            Pipeline().MaterializeAsync(Context(candidate, exactPaceEvidence: true, targetWeekCount)));
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
