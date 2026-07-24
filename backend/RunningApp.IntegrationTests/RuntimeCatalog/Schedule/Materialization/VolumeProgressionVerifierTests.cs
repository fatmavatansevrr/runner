using System.Reflection;
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
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

public sealed class VolumeProgressionVerifierTests
{
    private static readonly DateOnly AsOfDate = new(2026, 7, 20);

    [Fact]
    public async Task Verify_RealTwelveWeekPilotPlan_OutcomeIsPass()
    {
        var plan = await RealVolumePlanAsync(12);

        var result = VolumeProgressionVerifier.Verify(plan, VolumeSafetyPolicy.Default);

        Assert.Equal(VolumeProgressionOutcome.Pass, result.Outcome);
        Assert.Empty(result.Findings);
        Assert.Equal(11, result.TransitionChecks.Count);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Verify_RealNonPilotWeekCounts_ReportsActualOutcome_NotAssumedPass(int weeks)
    {
        // Investigated first via a temporary diagnostic dump (not committed)
        // before writing this assertion -- the real 8/9/10/11/13/14-week
        // curves all genuinely return Pass with zero violations; this is a
        // confirmed empirical finding, not an a-priori assumption. See this
        // phase's final report for the full per-transition numbers.
        var plan = await RealVolumePlanAsync(weeks);

        var result = VolumeProgressionVerifier.Verify(plan, VolumeSafetyPolicy.Default);

        Assert.Equal(weeks, plan.Weeks.Count);
        Assert.Equal(weeks - 1, result.TransitionChecks.Count);
        Assert.Equal(VolumeProgressionOutcome.Pass, result.Outcome);
        Assert.Empty(result.Findings);
        Assert.All(result.TransitionChecks, c => Assert.False(c.ViolatesRatio || c.ViolatesAbsoluteCapKm));
    }

    [Fact]
    public void Verify_SyntheticHardMaxRatioViolation_OutcomeIsFail_NamesExactTransition()
    {
        var plan = SyntheticPlan(
            Week(1, "FOUNDATION", 20, isTaper: false),
            Week(2, "FOUNDATION", 25, isTaper: false)); // (25-20)/20 = 0.25, far above HardMax=0.08

        var result = VolumeProgressionVerifier.Verify(plan, VolumeSafetyPolicy.Default);

        Assert.Equal(VolumeProgressionOutcome.Fail, result.Outcome);
        var check = Assert.Single(result.TransitionChecks);
        Assert.Equal(1, check.FromWeek);
        Assert.Equal(2, check.ToWeek);
        Assert.True(check.ViolatesRatio);
        Assert.Contains(result.Findings, f => f.Contains("WEEKLY_INCREASE_VIOLATION") && f.Contains("week 1->2"));
    }

    [Fact]
    public void Verify_SyntheticTaperMultiplierViolation_OutcomeIsFail_DistinctFromRatioViolation()
    {
        var plan = SyntheticPlan(
            Week(1, "BUILD", 30, isTaper: false),
            Week(2, "TAPER", 25, isTaper: true)); // expected 30*0.53=15.9, actual 25 -- far outside RoundingIncrementKm=0.5

        var result = VolumeProgressionVerifier.Verify(plan, VolumeSafetyPolicy.Default);

        Assert.Equal(VolumeProgressionOutcome.Fail, result.Outcome);
        var check = Assert.Single(result.TransitionChecks);
        Assert.True(check.IsTaperTransition);
        Assert.True(check.ViolatesRatio);
        Assert.False(check.ViolatesAbsoluteCapKm);
        Assert.Contains(result.Findings, f => f.Contains("TAPER_MULTIPLIER_VIOLATION") && f.Contains("week 1->2"));
        Assert.DoesNotContain(result.Findings, f => f.Contains("WEEKLY_INCREASE_VIOLATION"));
    }

    [Fact]
    public void Verify_MultipleSimultaneousViolations_AllCollected_NotJustFirst()
    {
        var plan = SyntheticPlan(
            Week(1, "FOUNDATION", 20, isTaper: false),
            Week(2, "FOUNDATION", 25, isTaper: false),   // violation 1: ratio
            Week(3, "BUILD", 25.4, isTaper: false),       // fine
            Week(4, "TAPER", 20, isTaper: true));          // violation 2: taper (expected 25.4*0.53=13.462)

        var result = VolumeProgressionVerifier.Verify(plan, VolumeSafetyPolicy.Default);

        Assert.Equal(VolumeProgressionOutcome.Fail, result.Outcome);
        Assert.Equal(3, result.TransitionChecks.Count);
        Assert.Equal(2, result.Findings.Count);
        Assert.Contains(result.Findings, f => f.Contains("week 1->2"));
        Assert.Contains(result.Findings, f => f.Contains("week 3->4"));
    }

    [Fact]
    public void Verify_FewerThanTwoWeeks_IsNotApplicable()
    {
        var plan = SyntheticPlan(Week(1, "FOUNDATION", 20, isTaper: false));

        var result = VolumeProgressionVerifier.Verify(plan, VolumeSafetyPolicy.Default);

        Assert.Equal(VolumeProgressionOutcome.NotApplicable, result.Outcome);
        Assert.Empty(result.TransitionChecks);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public async Task Verify_CalledTwiceWithIdenticalInput_ProducesIdenticalOutput()
    {
        var plan = await RealVolumePlanAsync(10);

        var first = VolumeProgressionVerifier.Verify(plan, VolumeSafetyPolicy.Default);
        var second = VolumeProgressionVerifier.Verify(plan, VolumeSafetyPolicy.Default);

        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.TransitionChecks, second.TransitionChecks);
        Assert.Equal(first.Findings, second.Findings);
    }

    [Fact]
    public void Verify_IsPureFunction_TwoParametersOnly()
    {
        var method = typeof(VolumeProgressionVerifier).GetMethod(nameof(VolumeProgressionVerifier.Verify), BindingFlags.Public | BindingFlags.Static)!;

        Assert.Equal(2, method.GetParameters().Length);
        Assert.Equal(typeof(CatalogWeeklyVolumePlan), method.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(VolumeSafetyPolicy), method.GetParameters()[1].ParameterType);
    }

    [Fact]
    public void VolumeProgressionVerifier_HasNoCallSiteInApplicationOrApiProductionCode()
    {
        var repoRoot = TestPlanServicesFactory.RepoRoot();
        foreach (var root in new[] { Path.Combine(repoRoot, "backend", "RunningApp.Application"), Path.Combine(repoRoot, "backend", "RunningApp.Api") })
        {
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    && !f.EndsWith($"{Path.DirectorySeparatorChar}VolumeProgressionVerifier.cs", StringComparison.OrdinalIgnoreCase));
            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                Assert.DoesNotContain("VolumeProgressionVerifier.Verify(", content, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void VolumeProgressionVerifier_DoesNotCallPlannerLoaderOrResolverLayer()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog",
            "Schedule", "Materialization", "VolumeProgressionVerifier.cs"));

        // Structural dependency checks -- actual instantiation/call syntax,
        // not prose. This file's own doc comment legitimately names
        // CatalogPeakVolumeBandLoader in prose (explaining what the real
        // planner threads through, as context for the AND/OR-combination
        // caveat) -- that is a documentation cross-reference, not a code
        // dependency, matching the same distinction already established
        // elsewhere in this session (e.g. TD-ID citations).
        Assert.DoesNotContain("new CatalogVolumeAndLongRunPlanner(", source);
        Assert.DoesNotContain("new CatalogPeakVolumeBandLoader(", source);
        Assert.DoesNotContain("new PlanCatalogBundleLoader(", source);
        Assert.DoesNotContain("RuntimeConditionResolutionResult", source);
    }

    // ── Real-pipeline construction (read-only: real candidate, real allocator, ──
    // ── real progression/binder/prescription-context/planner -- no live wiring) ──

    private static async Task<CatalogWeeklyVolumePlan> RealVolumePlanAsync(int weeks)
    {
        var root = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = root });

        var candidate = await new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance)
            .LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, weeks);
        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);

        var context = new CatalogStageToWeekMaterializationContext
        {
            StartDate = AsOfDate, AsOfDate = AsOfDate, PlannedWeekCount = weeks, DaysPerWeek = candidate.SlotRoles.Count,
            CanonicalDistanceFamily = progression.DistanceFamily, CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            SelectedStageSequence = allocation.Phases.Select(p => p.PhaseKey).ToList(),
            StageWeekAllocations = allocation.Phases.Select(p => new CatalogStageWeekAllocation(p.PhaseKey, p.AllocatedWeeks)).ToList(),
            RunLayout = candidate.Layout, RunLayoutSlotRoles = candidate.SlotRoles,
        };
        var plainSkeleton = new CatalogStageToWeekMaterializer().Materialize(context).Skeleton;
        var stageSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion, Progression = progression, Skeleton = plainSkeleton,
            ConditionResults = [RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST")],
        });
        var provenance = new CatalogCalendarMaterializationProvenance(candidate.CandidateKey, candidate.CandidateVersion, context.AsOfDate, context.StartDate,
            [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday], DayOfWeek.Sunday, CatalogCalendarDayMaterializerVersion.V1, plainSkeleton.SchemaVersion, new Dictionary<string, PlanCatalogReference>());
        var datedSkeleton = new CatalogWeekSkeletonCalendarMaterializer().Materialize(new CatalogCalendarAssignmentContext(context.StartDate, GoalType.Race,
            provenance.PreferredDays, DayOfWeek.Sunday, plainSkeleton, CatalogCalendarAssignmentPolicy.RaceHardConstraint, provenance));
        var boundPlan = await new CatalogWorkoutBinder().BindAsync(new CatalogWorkoutBindingContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion, DatedSkeleton = datedSkeleton, StageSchedule = stageSchedule,
            Progression = progression, ReferencedWorkouts = candidate.ReferencedWorkouts, WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
        });

        var definitionLoader = new CatalogWorkoutDefinitionLoader(options);
        var definitions = new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal);
        foreach (var reference in candidate.ReferencedWorkouts)
        {
            definitions[reference.Key] = await definitionLoader.LoadAsync(reference);
        }

        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate, DaysPerWeek = candidate.DaysPerWeek,
            Unit = DistanceUnit.Km, StartDate = AsOfDate, RaceDate = AsOfDate.AddDays(weeks * 7), TargetFinishTimeSeconds = 3000,
            PreferredDays = [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun], LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 24, RecentLongestRunKm = 9, RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = AsOfDate.AddDays(-21) },
        };
        var inputSnapshot = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = AsOfDate, RaceDate = AsOfDate.AddDays(weeks * 7), TargetFinishTimeSeconds = 3000,
            DaysPerWeek = candidate.DaysPerWeek, Level = RunningBackground.Intermediate,
        };

        var prescriptionContext = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            previewRequest, AsOfDate, candidate, inputSnapshot,
            [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "TEST"), RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST")],
            boundPlan, definitions));

        var peakBand = await new CatalogPeakVolumeBandLoader(options)
            .LoadAsync(candidate.PeakVolumeBandPolicy, candidate.CanonicalDistanceFamily, candidate.Level, candidate.DaysPerWeek);

        var volumeAndLongRunPlan = new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(candidate, boundPlan, prescriptionContext, peakBand));
        return volumeAndLongRunPlan.WeeklyVolumePlan;
    }

    private static CatalogWeeklyVolumePlan SyntheticPlan(params CatalogWeeklyVolumeWeek[] weeks) => new()
    {
        CandidateKey = "SYNTHETIC", CandidateVersion = 1, FirstWeekVolumeKm = weeks[0].PlannedWeeklyVolumeKm, PeakVolumeKm = weeks.Max(w => w.PlannedWeeklyVolumeKm),
        StartingVolumeDecision = SyntheticStartingVolumeDecision(weeks[0].PlannedWeeklyVolumeKm),
        ReachablePeakDecision = SyntheticPeakDecision(weeks.Max(w => w.PlannedWeeklyVolumeKm)),
        TaperVolumeDecision = new TaperVolumeDecision(VolumeSafetyPolicy.Default.TaperVolumeMultiplier, 1d - VolumeSafetyPolicy.Default.TaperVolumeMultiplier, "SYNTHETIC", CatalogEvidenceBasis.EvidenceInformed, CatalogDecisionStatus.ExplicitProductDefault, "SYNTHETIC"),
        CatalogBounds = new CatalogVolumeBounds(0, 1000, "SYNTHETIC", 1),
        Weeks = weeks,
        DecisionTrace = [],
        ValidationResult = new CatalogVolumeValidationResult(true, []),
    };

    private static StartingVolumeDecision SyntheticStartingVolumeDecision(double startingKm) => new(
        startingKm, PrescriptionInputState.Available, startingKm, WeeklyVolumeAnchorSource.RecentFourWeekAverage,
        CatalogVolumeClamp.None, CatalogEvidenceBasis.EvidenceInformed, CatalogDecisionStatus.CanonicalConfirmed, "SYNTHETIC");

    private static ReachablePeakDecision SyntheticPeakDecision(double peakKm) => new(
        peakKm, peakKm, peakKm, PeakBandClassification.WithinTypicalPeakBand,
        VolumeSafetyPolicy.Default.PreferredMaxWeeklyIncreaseRatio, VolumeSafetyPolicy.Default.HardMaxWeeklyIncreaseRatio, VolumeSafetyPolicy.Default.AbsoluteWeeklyIncrementCapKm,
        CatalogEvidenceBasis.EvidenceInformed, CatalogDecisionStatus.CanonicalConfirmed, "SYNTHETIC");

    private static CatalogWeeklyVolumeWeek Week(int weekNumber, string phaseKey, double volumeKm, bool isTaper) => new()
    {
        WeekNumber = weekNumber, PhaseKey = phaseKey, PlannedWeeklyVolumeKm = volumeKm, ChangeKm = 0,
        VolumeClassification = isTaper ? "TAPER" : "BUILDING", IsRecoveryOrDeloadWeek = false, IsTaperWeek = isTaper,
        AnchorSource = WeeklyVolumeAnchorSource.RecentFourWeekAverage, CatalogBounds = new CatalogVolumeBounds(0, 1000, "SYNTHETIC", 1),
        AppliedClamp = CatalogVolumeClamp.None, DecisionReason = "SYNTHETIC", SourceArtifactKey = "SYNTHETIC", SourceArtifactVersion = 1, Provenance = "SYNTHETIC",
    };
}
