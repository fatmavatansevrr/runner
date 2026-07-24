using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase 4G.3B.4b -- test-only builder for
/// <see cref="SafetyVerificationContext"/>. No existing shared/reusable
/// fixture builder produces the full set of artifacts all nine canonical
/// verifiers need together (each verifier's own test file builds only the
/// subset it needs, as a private method on that test class -- see
/// WorkoutExposureVerifierTests.RealAsync, VolumeProgressionVerifierTests.RealVolumePlanAsync,
/// and GoalPaceReachabilityVerifierTests.LoadRealAsync, none of which are
/// exposed for reuse and none of which build everything this orchestrator's
/// context needs). This file is the smallest test-only builder that
/// combines them, invoking only already-existing real components in the
/// same sequence those three private methods already use -- it introduces
/// no new pipeline step and duplicates no verifier or production logic.
/// The orchestrator itself never calls any of this.
/// </summary>
internal static class SafetyVerificationContextFixtures
{
    public static readonly DateOnly StartDate = new(2026, 7, 20);
    private static readonly PlanCatalogReference RegistryRef = new("RUNTIME_CONDITION_VALUES_V1", 2);

    /// <summary>Builds a complete, real, mathematically-feasible <see cref="SafetyVerificationContext"/> for <paramref name="weeks"/> using the real TEN_K__4D__INTERMEDIATE v10 candidate and a single synthetic GOAL_FEASIBILITY_IN=<paramref name="goalFeasibilityValue"/> condition threaded consistently through stage scheduling and prescription context.</summary>
    public static async Task<SafetyVerificationContext> RealAsync(int weeks, string goalFeasibilityValue)
    {
        var root = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = root });

        var candidate = await new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance)
            .LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, weeks);
        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);

        var stageToWeekContext = new CatalogStageToWeekMaterializationContext
        {
            StartDate = StartDate, AsOfDate = StartDate, PlannedWeekCount = weeks, DaysPerWeek = candidate.SlotRoles.Count,
            CanonicalDistanceFamily = progression.DistanceFamily, CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            SelectedStageSequence = allocation.Phases.Select(p => p.PhaseKey).ToList(),
            StageWeekAllocations = allocation.Phases.Select(p => new CatalogStageWeekAllocation(p.PhaseKey, p.AllocatedWeeks)).ToList(),
            RunLayout = candidate.Layout, RunLayoutSlotRoles = candidate.SlotRoles,
        };
        var plainSkeleton = new CatalogStageToWeekMaterializer().Materialize(stageToWeekContext).Skeleton;

        var conditions = new List<RuntimeConditionResolutionResult>
        {
            RuntimeConditionResolutionResult.Evaluated(GoalFeasibilityResolver.ConditionTypeValue, goalFeasibilityValue, "SAFETY_VERIFICATION_ORCHESTRATOR_TEST_FIXTURE"),
        };
        var stageSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion, Progression = progression,
            Skeleton = plainSkeleton, ConditionResults = conditions,
        });

        var provenance = new CatalogCalendarMaterializationProvenance(candidate.CandidateKey, candidate.CandidateVersion, stageToWeekContext.AsOfDate, stageToWeekContext.StartDate,
            [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday], DayOfWeek.Sunday, CatalogCalendarDayMaterializerVersion.V1, plainSkeleton.SchemaVersion, new Dictionary<string, PlanCatalogReference>());
        var datedSkeleton = new CatalogWeekSkeletonCalendarMaterializer().Materialize(new CatalogCalendarAssignmentContext(stageToWeekContext.StartDate, GoalType.Race,
            provenance.PreferredDays, DayOfWeek.Sunday, plainSkeleton, CatalogCalendarAssignmentPolicy.RaceHardConstraint, provenance));

        var boundPlan = await new CatalogWorkoutBinder().BindAsync(new CatalogWorkoutBindingContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion, DatedSkeleton = datedSkeleton,
            StageSchedule = stageSchedule, Progression = progression, ReferencedWorkouts = candidate.ReferencedWorkouts,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
        });

        var raceDate = StartDate.AddDays(weeks * 7);
        var definitionLoader = new CatalogWorkoutDefinitionLoader(options);
        var definitions = new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal);
        foreach (var reference in candidate.ReferencedWorkouts)
        {
            definitions[reference.Key] = await definitionLoader.LoadAsync(reference);
        }

        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate, DaysPerWeek = candidate.DaysPerWeek,
            Unit = DistanceUnit.Km, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            PreferredDays = [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun], LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 24, RecentLongestRunKm = 9, RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = StartDate.AddDays(-21) },
        };
        var inputSnapshot = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            DaysPerWeek = candidate.DaysPerWeek, Level = RunningBackground.Intermediate,
        };
        var prescriptionConditions = new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "SAFETY_VERIFICATION_ORCHESTRATOR_TEST_FIXTURE"),
            RuntimeConditionResolutionResult.Evaluated(GoalFeasibilityResolver.ConditionTypeValue, goalFeasibilityValue, "SAFETY_VERIFICATION_ORCHESTRATOR_TEST_FIXTURE"),
        };
        var prescriptionContext = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            previewRequest, StartDate, candidate, inputSnapshot, prescriptionConditions, boundPlan, definitions));

        var peakBand = await new CatalogPeakVolumeBandLoader(options)
            .LoadAsync(candidate.PeakVolumeBandPolicy, candidate.CanonicalDistanceFamily, candidate.Level, candidate.DaysPerWeek);
        var volumeAndLongRunPlan = new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(candidate, boundPlan, prescriptionContext, peakBand));

        var goalPaceStage = progression.PhaseProgressions
            .Single(p => p.PhaseKey == "RACE_SPECIFIC").Stages
            .Single(s => s.ProgressionStageKey == "GOAL_PACE_REHEARSAL");
        var registrySnapshot = await new RuntimeConditionRegistryReader(options, NullLogger<RuntimeConditionRegistryReader>.Instance).LoadAsync(RegistryRef);
        var registeredValues = registrySnapshot.AllowedValuesByConditionType[GoalFeasibilityResolver.ConditionTypeValue].ToHashSet();

        return new SafetyVerificationContext(
            allocation, progression, candidate.SlotRoles, conditions, datedSkeleton, boundPlan,
            goalPaceStage, registeredValues, volumeAndLongRunPlan.WeeklyVolumePlan, VolumeSafetyPolicy.Default,
            volumeAndLongRunPlan.LongRunProgression, raceDate);
    }

    /// <summary>Root-mathematically-infeasible variant: reuses a real feasible 12-week context's artifacts (structurally valid on their own terms) but overrides only <see cref="PhaseAllocationResult.IsMathematicallyFeasible"/> to false, mirroring the exact pattern each individual verifier's own test suite already uses for its own "NotApplicable on infeasible allocation" case (e.g. WorkoutExposureVerifierTests.Verify_MathematicallyInfeasible_IsNotApplicable).</summary>
    public static async Task<SafetyVerificationContext> RealMathematicallyInfeasibleAsync()
    {
        var real = await RealAsync(12, "REALISTIC");
        return real with { Allocation = real.Allocation with { IsMathematicallyFeasible = false } };
    }
}
