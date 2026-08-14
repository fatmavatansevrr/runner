using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6 — closes the Phase 4I.5 Core null-workout-reference gap by
/// invoking the EXISTING, UNCHANGED Core workout-resolution authority
/// (<see cref="CatalogWorkoutBinder"/>, fed by <see cref="ProgressionStageAllocator"/>
/// and <see cref="CatalogWeekSkeletonCalendarMaterializer"/>) directly,
/// exactly the same real production API sequence
/// <c>CatalogPreviewGenerator</c>'s own standard 8-14 week path uses, and the
/// same isolated-invocation pattern already proven safe and correct by the
/// pre-existing <c>CatalogWorkoutBinderTests.cs</c>. No stage-to-workout
/// mapping is reimplemented -- every workout key/version/source returned
/// here comes from the real catalog via the real binder.
///
/// The <c>GOAL_FEASIBILITY_IN</c> condition supplied to
/// <see cref="ProgressionStageAllocator"/> is a representative, explicitly
/// documented stand-in ("REALISTIC") rather than a live-resolved value --
/// this phase does not execute Core numeric/pace prescription (see this
/// phase's own explicit non-implementation statement), so no live
/// goal-feasibility evidence exists yet for the joined Long-Horizon
/// skeleton. This mirrors the exact representative-condition convention
/// <c>CatalogWorkoutBinderTests.cs</c> itself already establishes for
/// isolated binder invocation.
/// </summary>
internal static class LongHorizonCoreWorkoutBindingExecutor
{
    public const string RepresentativeConditionType = "GOAL_FEASIBILITY_IN";
    public const string RepresentativeConditionValue = "REALISTIC";
    public const string RepresentativeConditionReason = "LONG_HORIZON_DARK_REPRESENTATIVE_CONDITION_NOT_LIVE_RESOLVED";

    public static async Task<BoundCatalogPlan> BindAsync(
        DateOnly coreStartDate,
        IReadOnlyList<string> coreStageSequence,
        IReadOnlyList<CatalogStageWeekAllocation> coreStageAllocations,
        PlanCatalogReference coreRunLayout,
        IReadOnlyList<string> coreRunLayoutSlotRoles,
        string catalogRoot,
        ICatalogWorkoutDefinitionLoader workoutLoader,
        CancellationToken ct = default)
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = catalogRoot });
        var bundleLoader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        var candidate = await gate.LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);

        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);

        var skeletonContext = new CatalogStageToWeekMaterializationContext
        {
            StartDate = coreStartDate,
            AsOfDate = coreStartDate,
            PlannedWeekCount = coreStageAllocations.Sum(a => a.WeekCount),
            DaysPerWeek = coreRunLayoutSlotRoles.Count,
            CanonicalDistanceFamily = "TEN_K",
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            DependencyVersions = new Dictionary<string, PlanCatalogReference> { ["layout"] = coreRunLayout },
            SelectedStageSequence = coreStageSequence,
            StageWeekAllocations = coreStageAllocations,
            RunLayout = coreRunLayout,
            RunLayoutSlotRoles = coreRunLayoutSlotRoles,
        };
        var skeleton = new CatalogStageToWeekMaterializer().Materialize(skeletonContext).Skeleton;

        var preferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var longRunDay = DayOfWeek.Sunday;
        var calendarProvenance = new CatalogCalendarMaterializationProvenance(
            skeleton.CandidateKey, skeleton.CandidateVersion, coreStartDate, coreStartDate,
            preferredDays, longRunDay, CatalogCalendarDayMaterializerVersion.V1, skeleton.SchemaVersion, skeleton.DependencyVersions);
        var calendarContext = new CatalogCalendarAssignmentContext(
            coreStartDate, GoalType.Race, preferredDays, longRunDay, skeleton,
            CatalogCalendarAssignmentPolicy.RaceHardConstraint, calendarProvenance);
        var datedSkeleton = new CatalogWeekSkeletonCalendarMaterializer().Materialize(calendarContext);

        var representativeCondition = new[]
        {
            RuntimeConditionResolutionResult.Evaluated(
                RepresentativeConditionType, RepresentativeConditionValue, RepresentativeConditionReason),
        };
        var stageSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = representativeCondition,
        });

        var bindingContext = new CatalogWorkoutBindingContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            DatedSkeleton = datedSkeleton,
            StageSchedule = stageSchedule,
            Progression = progression,
            ReferencedWorkouts = candidate.ReferencedWorkouts,
            WorkoutDefinitionLoader = workoutLoader,
        };

        return await new CatalogWorkoutBinder().BindAsync(bindingContext, ct);
    }
}
