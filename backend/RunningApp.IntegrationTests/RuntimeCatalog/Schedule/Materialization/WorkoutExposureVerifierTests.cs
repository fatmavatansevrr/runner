using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

public sealed class WorkoutExposureVerifierTests
{
    [Theory]
    [InlineData(8, 32)] [InlineData(9, 36)] [InlineData(10, 40)] [InlineData(11, 44)]
    [InlineData(12, 48)] [InlineData(13, 52)] [InlineData(14, 56)]
    public async Task Verify_RealPrimaryEightThroughFourteen_Passes(int weeks, int total)
    {
        var x = await RealAsync(weeks, "REALISTIC"); var result = Verify(x);
        Assert.Equal(WorkoutExposureOutcome.Pass, result.Outcome); Assert.Equal(total, result.ActualTotalExposureCount);
        Assert.Equal(weeks, result.ActualRoleCounts["KEY_SESSION"]); Assert.Equal(weeks * 2, result.ActualRoleCounts["EASY_SUPPORT"]);
        Assert.Equal(weeks, result.ActualRoleCounts["LONG_RUN"]); Assert.DoesNotContain("REST", result.ActualRoleCounts.Keys);
    }

    [Theory]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)] [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public async Task Verify_RealFallbackEightThroughFourteen_PassesOneForOne(int weeks)
    {
        var x = await RealAsync(weeks, "UNSUPPORTED"); var result = Verify(x);
        Assert.Equal(WorkoutExposureOutcome.Pass, result.Outcome);
        var rs = result.WeekResults.Where(w => w.PhaseKey == "RACE_SPECIFIC").SelectMany(w => w.Exposures).Where(e => e.Role == "KEY_SESSION").ToList();
        Assert.Equal(x.Allocation.Phases.Single(p => p.PhaseKey == "RACE_SPECIFIC").AllocatedWeeks, rs.Count);
        Assert.Contains(rs, e => e.RequestedStageKey == "GOAL_PACE_REHEARSAL" && e.EffectiveStageKey == "CURRENT_FITNESS_SPECIFIC_REHEARSAL" && e.UsedFallback);
        Assert.DoesNotContain(rs, e => e.EffectiveStageKey == "GOAL_PACE_REHEARSAL");
    }

    [Fact]
    public async Task Verify_RealEightWeekExactCountsAndWorkoutKeys()
    {
        var result = Verify(await RealAsync(8, "REALISTIC"));
        Assert.Equal(32, result.ActualTotalExposureCount);
        // EASY_STANDARD includes 16 EASY_SUPPORT slots plus two Foundation
        // KEY_SESSION exposures and the TAPER_SHARPEN key exposure.
        Assert.Equal(19, result.WorkoutKeyExposureCounts["EASY_STANDARD"]);
        Assert.Equal(8, result.WorkoutKeyExposureCounts["LONG_RUN_STANDARD"]);
        Assert.Equal(3, result.WorkoutKeyExposureCounts["THRESHOLD_TEMPO"]);
        Assert.Equal(1, result.WorkoutKeyExposureCounts["FARTLEK"]);
        Assert.Equal(1, result.WorkoutKeyExposureCounts["GOAL_PACE_TEN_K"]);
        Assert.Contains(result.Findings, f => f.Code == WorkoutExposureFindingCode.ExactWorkoutExposureMatch);
    }

    [Fact]
    public async Task Verify_TaperSharpenIsEasyStandardKeyOnlyInTaper()
    {
        var result = Verify(await RealAsync(8, "REALISTIC"));
        var taper = Assert.Single(result.WeekResults, w => w.PhaseKey == "TAPER");
        var key = Assert.Single(taper.Exposures, e => e.Role == "KEY_SESSION");
        Assert.Equal("TAPER_SHARPEN", key.EffectiveStageKey); Assert.Equal("EASY_STANDARD", key.WorkoutKey);
        Assert.DoesNotContain(result.Findings, f => f.Code is WorkoutExposureFindingCode.TaperSharpenExposureMissing or WorkoutExposureFindingCode.TaperSharpenOutsideTaper);
    }

    [Theory]
    [InlineData("KEY_SESSION", 0)]
    [InlineData("KEY_SESSION", 2)]
    [InlineData("EASY_SUPPORT", 1)]
    [InlineData("EASY_SUPPORT", 3)]
    [InlineData("LONG_RUN", 0)]
    [InlineData("LONG_RUN", 2)]
    public async Task Verify_WeeklyRoleMultiplicityDrift_Fails(string role, int count)
    {
        var x = await RealAsync(8, "REALISTIC"); var week = x.Skeleton.Weeks[0];
        var retained = week.SessionSlots.Where(s => s.StructuralRole != role).ToList();
        var sample = week.SessionSlots.First(s => s.StructuralRole == role);
        for (var i = 0; i < count; i++) retained.Add(sample with { SlotOrderInWeek = 20 + i, SessionDate = sample.SessionDate.AddDays(i) });
        var changed = ReplaceWeek(x.Skeleton, week with { SessionSlots = retained });
        var result = WorkoutExposureVerifier.Verify(x.Allocation, changed, x.Bound, x.Progression, x.Roles);
        Assert.Equal(WorkoutExposureOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == WorkoutExposureFindingCode.WeeklyRoleExposureCountMismatch && f.Role == role);
    }

    [Fact]
    public async Task Verify_DuplicateSlotIndexUnknownRoleAndRest_Fail()
    {
        var x = await RealAsync(8, "REALISTIC"); var week = x.Skeleton.Weeks[0]; var first = week.SessionSlots[0];
        var slots = week.SessionSlots.Concat([
            first with { StructuralRole = "UNKNOWN", SessionDate = first.SessionDate.AddDays(1) },
            first with { StructuralRole = "REST", SessionDate = first.SessionDate.AddDays(2) }
        ]).ToList();
        slots[1] = slots[1] with { SlotOrderInWeek = first.SlotOrderInWeek };
        var result = WorkoutExposureVerifier.Verify(x.Allocation, ReplaceWeek(x.Skeleton, week with { SessionSlots = slots }), x.Bound, x.Progression, x.Roles);
        Assert.Contains(result.Findings, f => f.Code == WorkoutExposureFindingCode.DuplicateSlotIndex);
        Assert.Contains(result.Findings, f => f.Code == WorkoutExposureFindingCode.UnknownWeeklyRole);
        Assert.Contains(result.Findings, f => f.Code == WorkoutExposureFindingCode.RestExposurePresent);
    }

    [Fact]
    public async Task Verify_RaceSpecificExposureMovedIntoBuild_FailsOwnership()
    {
        var x = await RealAsync(8, "REALISTIC"); var build = x.Bound.Weeks.First(w => w.PhaseKey == "BUILD");
        var key = build.Sessions.First(s => s.StructuralRole == "KEY_SESSION");
        var mutated = CopyBound(x.Bound, build.WeekNumber, key, progressionStageKey: "GOAL_PACE_REHEARSAL", workoutKey: "GOAL_PACE_TEN_K");
        var result = WorkoutExposureVerifier.Verify(x.Allocation, x.Skeleton, mutated, x.Progression, x.Roles);
        Assert.Contains(result.Findings, f => f.Code == WorkoutExposureFindingCode.RaceSpecificExposureOutsidePhase);
    }

    [Fact]
    public async Task Verify_MissingTaperSharpenAndMissingMapping_Fail()
    {
        var x = await RealAsync(8, "REALISTIC"); var taper = x.Bound.Weeks.Single(w => w.PhaseKey == "TAPER");
        var key = taper.Sessions.First(s => s.StructuralRole == "KEY_SESSION");
        var mutated = CopyBound(x.Bound, taper.WeekNumber, key, progressionStageKey: null, workoutKey: "");
        var result = WorkoutExposureVerifier.Verify(x.Allocation, x.Skeleton, mutated, x.Progression, x.Roles);
        Assert.Contains(result.Findings, f => f.Code == WorkoutExposureFindingCode.KeySessionWorkoutMappingMissing);
        Assert.Contains(result.Findings, f => f.Code == WorkoutExposureFindingCode.TaperSharpenExposureMissing);
    }

    [Fact]
    public async Task Verify_MathematicallyInfeasible_IsNotApplicable()
    {
        var x = await RealAsync(8, "REALISTIC");
        var result = WorkoutExposureVerifier.Verify(x.Allocation with { IsMathematicallyFeasible = false }, x.Skeleton, x.Bound, x.Progression, x.Roles);
        Assert.Equal(WorkoutExposureOutcome.NotApplicable, result.Outcome);
    }

    [Fact]
    public async Task Verify_DeterministicIndependentAndDark()
    {
        var x = await RealAsync(8, "REALISTIC"); var a = Verify(x); var b = Verify(x);
        Assert.Equal(a.Outcome, b.Outcome); Assert.Equal(a.WorkoutKeyExposureCounts, b.WorkoutKeyExposureCounts);
        var source = File.ReadAllText(Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "Materialization", "WorkoutExposureVerifier.cs"));
        foreach (var forbidden in new[] { "AllocationOrderCorrectnessVerifier", "PhaseConstraintVerifier", "RaceSpecificCapacityVerifier", "StageReachabilityVerifier", "activation-readiness-risks", "TD-" }) Assert.DoesNotContain(forbidden, source);
        foreach (var root in new[] { "RunningApp.Application", "RunningApp.Api" }.Select(p => Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", p)))
        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.EndsWith("WorkoutExposureVerifier.cs"))) Assert.DoesNotContain("WorkoutExposureVerifier.Verify(", File.ReadAllText(file));
    }

    private static WorkoutExposureVerificationResult Verify(RealArtifacts x) => WorkoutExposureVerifier.Verify(x.Allocation, x.Skeleton, x.Bound, x.Progression, x.Roles);

    private static async Task<RealArtifacts> RealAsync(int weeks, string goal)
    {
        var root = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog"); var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = root });
        var candidate = await new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance).LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, weeks);
        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);
        var context = new CatalogStageToWeekMaterializationContext { StartDate = new DateOnly(2026, 7, 20), AsOfDate = new DateOnly(2026, 7, 20), PlannedWeekCount = weeks, DaysPerWeek = candidate.SlotRoles.Count,
            CanonicalDistanceFamily = progression.DistanceFamily, CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion, DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            SelectedStageSequence = allocation.Phases.Select(p => p.PhaseKey).ToList(), StageWeekAllocations = allocation.Phases.Select(p => new CatalogStageWeekAllocation(p.PhaseKey, p.AllocatedWeeks)).ToList(),
            RunLayout = candidate.Layout, RunLayoutSlotRoles = candidate.SlotRoles };
        var plain = new CatalogStageToWeekMaterializer().Materialize(context).Skeleton;
        var stageSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext { CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion, Progression = progression, Skeleton = plain,
            ConditionResults = [RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", goal, "TEST")] });
        var provenance = new CatalogCalendarMaterializationProvenance(candidate.CandidateKey, candidate.CandidateVersion, context.AsOfDate, context.StartDate,
            [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday], DayOfWeek.Sunday, CatalogCalendarDayMaterializerVersion.V1, plain.SchemaVersion, new Dictionary<string, PlanCatalogReference>());
        var dated = new CatalogWeekSkeletonCalendarMaterializer().Materialize(new CatalogCalendarAssignmentContext(context.StartDate, GoalType.Race,
            provenance.PreferredDays, DayOfWeek.Sunday, plain, CatalogCalendarAssignmentPolicy.RaceHardConstraint, provenance));
        var bound = await new CatalogWorkoutBinder().BindAsync(new CatalogWorkoutBindingContext { CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            DatedSkeleton = dated, StageSchedule = stageSchedule, Progression = progression, ReferencedWorkouts = candidate.ReferencedWorkouts, WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options) });
        return new(allocation, dated, bound, progression, candidate.SlotRoles);
    }

    private static DatedGeneratedCatalogPlanSkeleton ReplaceWeek(DatedGeneratedCatalogPlanSkeleton plan, DatedGeneratedCatalogWeekSkeleton week) => plan with { Weeks = plan.Weeks.Select(w => w.WeekNumber == week.WeekNumber ? week : w).ToList() };
    private static BoundCatalogPlan CopyBound(BoundCatalogPlan plan, int weekNumber, BoundCatalogSession target, string? progressionStageKey, string workoutKey) => new()
    { CandidateKey = plan.CandidateKey, CandidateVersion = plan.CandidateVersion, BinderVersion = plan.BinderVersion, Trace = plan.Trace,
      Weeks = plan.Weeks.Select(w => new BoundCatalogWeek { WeekNumber = w.WeekNumber, PhaseKey = w.PhaseKey, Sessions = w.Sessions.Select(s => w.WeekNumber == weekNumber && ReferenceEquals(s, target)
        ? new BoundCatalogSession { WeekNumber=s.WeekNumber, Date=s.Date, PhaseKey=s.PhaseKey, ProgressionStageKey=progressionStageKey, StructuralRole=s.StructuralRole, WorkoutDefinitionKey=workoutKey,
          WorkoutDefinitionVersion=s.WorkoutDefinitionVersion, BindingMode=s.BindingMode, BindingPolicyKey=s.BindingPolicyKey, BindingPolicyVersion=s.BindingPolicyVersion,
          SourceArtifactKey=s.SourceArtifactKey, SourceArtifactVersion=s.SourceArtifactVersion, ConditionOutcome=s.ConditionOutcome, FallbackOrigin=s.FallbackOrigin, BindingReason=s.BindingReason } : s).ToList() }).ToList() };
    private sealed record RealArtifacts(PhaseAllocationResult Allocation, DatedGeneratedCatalogPlanSkeleton Skeleton, BoundCatalogPlan Bound, CatalogWorkoutProgressionDefinition Progression, IReadOnlyList<string> Roles);
}
