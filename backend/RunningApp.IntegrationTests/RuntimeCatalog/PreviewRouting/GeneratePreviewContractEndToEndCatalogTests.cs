using System;
using System.Threading.Tasks;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// End-to-end mapping test for the generate-preview contract alignment,
/// exercised through the real catalog pilot path (10K/Intermediate/4-day —
/// the V1 pilot identity, and the task's own target race example shape):
/// a full typed request (StartDate, array PreferredDays, LongRunDay,
/// nested RecentRace) maps successfully through
/// <see cref="CatalogPreviewGenerator"/>'s skeleton + calendar-assignment
/// pipeline, and the request's own StartDate (not AsOfDate) drives the
/// calendar's actual dates -- see the Backend Integration contract
/// alignment change to <c>CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton</c>.
/// </summary>
public sealed class GeneratePreviewContractEndToEndCatalogTests
{
    private sealed class FixedResultEligibilityGate : ICatalogCandidateEligibilityGate
    {
        private readonly PlanCatalogCandidateSummary _result;
        public FixedResultEligibilityGate(PlanCatalogCandidateSummary result) => _result = result;

        public Task<PlanCatalogCandidateSummary> LoadForPublicPreviewAsync(string candidateKey, int candidateVersion, System.Threading.CancellationToken ct = default) =>
            Task.FromResult(_result);

        public Task<PlanCatalogCandidateSummary> LoadForInternalDryRunAsync(string candidateKey, int candidateVersion, System.Threading.CancellationToken ct = default) =>
            Task.FromResult(_result);
    }

    private static ICatalogPlanSkeletonOrchestrator RealSkeletonOrchestrator() => new CatalogPlanSkeletonOrchestrator(
        new CatalogPhaseAllocationResolver(),
        new CatalogRunLayoutResolver(),
        new CatalogStageToWeekContextFactory(),
        new CatalogStageToWeekMaterializer(),
        new GeneratedCatalogPlanSkeletonValidator());

    private static RuntimeConditionResolutionService RealOrchestration() =>
        new(new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());

    private static async Task<PlanCatalogCandidateSummary> LoadControlledPublishedCandidateAsync()
    {
        var real = await CatalogPlanSkeletonOrchestratorFixtures.LoadRealPilotCandidateAsync();
        return new PlanCatalogCandidateSummary
        {
            CandidateKey = real.CandidateKey,
            CandidateVersion = real.CandidateVersion,
            CandidateStatus = "PUBLISHED",
            CanonicalDistanceFamily = real.CanonicalDistanceFamily,
            Level = real.Level,
            DaysPerWeek = real.DaysPerWeek,
            CoreCycle = real.CoreCycle,
            MasterTemplate = real.MasterTemplate,
            Layout = real.Layout,
            LevelModifier = real.LevelModifier,
            WorkoutProgression = real.WorkoutProgression,
            ProgressionModifier = real.ProgressionModifier,
            RulePack = real.RulePack,
            PeakVolumeBandPolicy = real.PeakVolumeBandPolicy,
            RuntimeConditionValueRegistry = real.RuntimeConditionValueRegistry,
            DependencyStatuses = new System.Collections.Generic.Dictionary<string, string>
            {
                ["masterTemplate"] = "PUBLISHED", ["layout"] = "PUBLISHED", ["levelModifier"] = "PUBLISHED", ["rulePack"] = "PUBLISHED",
            },
            ReferencedWorkouts = real.ReferencedWorkouts,
            PhaseKeys = real.PhaseKeys,
            PhaseAllocations = real.PhaseAllocations,
            SlotRoles = real.SlotRoles,
        };
    }

    private static GeneratePreviewRequest FullTenKIntermediateRequest(DateOnly startDate, DateOnly raceDate) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = startDate,
        PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
        LongRunDay = Weekday.Sun,
        RaceName = "Local 10K",
        RaceDate = raceDate,
        TargetFinishTimeSeconds = 3600,
        RecentWeeklyVolumeKm = 20,
        RecentLongestRunKm = 8,
        RecentRunsPerWeek = 3,
        RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3510, RaceDate = new DateOnly(2026, 6, 1) },
    };

    [Fact]
    public async Task FullTenKIntermediate4DayRequest_MapsSuccessfully_ThroughCatalogPipeline()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator());
        // AsOfDate must be on/after RecentRace.RaceDate (2026-06-01) per the
        // PaceSourceResolver's own invariant -- a recent race can't be dated
        // in the future relative to the resolution reference date.
        var asOfDate = new DateOnly(2026, 6, 15);
        // StartDate deliberately NOT a Monday and deliberately different from
        // AsOfDate -- proves the request's own StartDate (not AsOfDate) drives
        // calendar assignment.
        var startDate = new DateOnly(2026, 7, 22); // a Wednesday
        var raceDate = startDate.AddDays(84);

        var snapshot = await generator.GenerateAsync(FullTenKIntermediateRequest(startDate, raceDate), asOfDate);

        Assert.NotNull(snapshot.GeneratedPreviewPlanPayload);
        Assert.NotEmpty(snapshot.GeneratedPreviewPlanPayload!.Weeks);
    }
}
