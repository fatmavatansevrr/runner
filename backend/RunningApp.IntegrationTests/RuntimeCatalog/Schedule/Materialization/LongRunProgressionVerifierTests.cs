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

public sealed class LongRunProgressionVerifierTests
{
    private static readonly DateOnly AsOfDate = new(2026, 7, 20);

    [Fact]
    public async Task Verify_RealTwelveWeekPilotPlan_ReportsOutcomeAndFullPerWeekShares()
    {
        var plan = await RealPlanAsync(12);

        var result = LongRunProgressionVerifier.Verify(plan.WeeklyVolumePlan, plan.LongRunProgression, VolumeSafetyPolicy.Default);

        Assert.Equal(12, result.WeekChecks.Count);
        Assert.Equal(LongRunProgressionOutcome.Pass, result.Outcome);
        Assert.Empty(result.Findings);
        Assert.All(result.WeekChecks, c => Assert.False(c.ExceedsWeeklyVolume));
        Assert.All(result.WeekChecks, c => Assert.False(c.ViolatesHardCapShare));
        // Full per-week ActualShare assertions -- not just the aggregate outcome.
        Assert.All(result.WeekChecks.Where(c => !c.IsTaperWeek), c => Assert.InRange(c.ActualShare, 0.32, 0.34));
        var taper = Assert.Single(result.WeekChecks, c => c.IsTaperWeek);
        Assert.Equal(12, taper.WeekNumber);
        Assert.InRange(taper.ActualShare, 0.32, 0.34);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Verify_RealNonPilotWeekCounts_ReportsActualOutcomeAndZeroViolations(int weeks)
    {
        // Investigated first (real numbers confirmed via the real pipeline
        // before writing these assertions) -- see this phase's final report
        // for the full per-week share table across all 7 targets.
        var plan = await RealPlanAsync(weeks);

        var result = LongRunProgressionVerifier.Verify(plan.WeeklyVolumePlan, plan.LongRunProgression, VolumeSafetyPolicy.Default);

        Assert.Equal(weeks, result.WeekChecks.Count);
        Assert.Equal(LongRunProgressionOutcome.Pass, result.Outcome);
        Assert.Empty(result.Findings);
        Assert.All(result.WeekChecks, c => Assert.False(c.ExceedsWeeklyVolume || c.ViolatesHardCapShare));
    }

    [Fact]
    public void Verify_SyntheticLongRunExceedsWeeklyVolume_OutcomeIsFail_NamesExactWeekAndBothValues()
    {
        var volumePlan = SyntheticVolumePlan(Week(1, 20, isTaper: false));
        var longRunPlan = SyntheticLongRunPlan(LongRunWeek(1, totalVolumeKm: 20, longRunKm: 25)); // 25 > 20

        var result = LongRunProgressionVerifier.Verify(volumePlan, longRunPlan, VolumeSafetyPolicy.Default);

        Assert.Equal(LongRunProgressionOutcome.Fail, result.Outcome);
        var check = Assert.Single(result.WeekChecks);
        Assert.True(check.ExceedsWeeklyVolume);
        Assert.Contains(result.Findings, f => f.Contains("EXCEEDS_WEEKLY_VOLUME") && f.Contains("week 1") && f.Contains("25km") && f.Contains("20km"));
    }

    [Fact]
    public void Verify_SyntheticHardCapShareViolation_OutcomeIsFail_DistinctFromExceedsWeeklyVolumeCase()
    {
        // 9km / 20km = 0.45, above HardCapShare=0.40, but still <= TotalVolumeKm.
        var volumePlan = SyntheticVolumePlan(Week(1, 20, isTaper: false));
        var longRunPlan = SyntheticLongRunPlan(LongRunWeek(1, totalVolumeKm: 20, longRunKm: 9));

        var result = LongRunProgressionVerifier.Verify(volumePlan, longRunPlan, VolumeSafetyPolicy.Default);

        Assert.Equal(LongRunProgressionOutcome.Fail, result.Outcome);
        var check = Assert.Single(result.WeekChecks);
        Assert.False(check.ExceedsWeeklyVolume);
        Assert.True(check.ViolatesHardCapShare);
        Assert.Contains(result.Findings, f => f.Contains("HARD_CAP_SHARE_VIOLATION") && f.Contains("week 1"));
        Assert.DoesNotContain(result.Findings, f => f.Contains("EXCEEDS_WEEKLY_VOLUME"));
    }

    [Fact]
    public void Verify_PreferredShareBandDeviationWithoutHardCapViolation_IsReported_ButDoesNotFail()
    {
        // 6.4km / 20km = 0.32, below PreferredMinimumShare=0.30? No -- 0.32 is
        // within [0.30, 0.36]. Use 5.8km/20km=0.29, below preferred minimum
        // but still within hard cap (0.40) -- soft finding only.
        var volumePlan = SyntheticVolumePlan(Week(1, 20, isTaper: false));
        var longRunPlan = SyntheticLongRunPlan(LongRunWeek(1, totalVolumeKm: 20, longRunKm: 5.8));

        var result = LongRunProgressionVerifier.Verify(volumePlan, longRunPlan, VolumeSafetyPolicy.Default);

        Assert.Equal(LongRunProgressionOutcome.Pass, result.Outcome);
        var check = Assert.Single(result.WeekChecks);
        Assert.True(check.ViolatesPreferredShareBand);
        Assert.False(check.ViolatesHardCapShare);
        Assert.Contains(result.Findings, f => f.Contains("PREFERRED_SHARE_BAND_DEVIATION_NONFAILING") && f.Contains("week 1"));
    }

    [Fact]
    public void Verify_MultipleSimultaneousViolations_AllCollected_NotJustFirst()
    {
        var volumePlan = SyntheticVolumePlan(Week(1, 20, isTaper: false), Week(2, 20, isTaper: false));
        var longRunPlan = SyntheticLongRunPlan(
            LongRunWeek(1, totalVolumeKm: 20, longRunKm: 25),  // exceeds weekly volume
            LongRunWeek(2, totalVolumeKm: 20, longRunKm: 9));   // hard cap share violation

        var result = LongRunProgressionVerifier.Verify(volumePlan, longRunPlan, VolumeSafetyPolicy.Default);

        Assert.Equal(LongRunProgressionOutcome.Fail, result.Outcome);
        Assert.Equal(2, result.WeekChecks.Count);
        // Week 1's LongRunKm (25) exceeding its TotalVolumeKm (20) necessarily
        // also makes ActualShare (1.25) exceed HardCapShare -- both findings
        // are correctly reported for week 1, plus week 2's separate hard-cap
        // violation: 3 findings total, none of them just "the first one".
        Assert.Equal(3, result.Findings.Count);
        Assert.Contains(result.Findings, f => f.Contains("week 1") && f.Contains("EXCEEDS_WEEKLY_VOLUME"));
        Assert.Contains(result.Findings, f => f.Contains("week 1") && f.Contains("HARD_CAP_SHARE_VIOLATION"));
        Assert.Contains(result.Findings, f => f.Contains("week 2") && f.Contains("HARD_CAP_SHARE_VIOLATION"));
    }

    [Fact]
    public void Verify_EmptyLongRunPlan_IsNotApplicable()
    {
        var volumePlan = SyntheticVolumePlan();
        var longRunPlan = SyntheticLongRunPlan();

        var result = LongRunProgressionVerifier.Verify(volumePlan, longRunPlan, VolumeSafetyPolicy.Default);

        Assert.Equal(LongRunProgressionOutcome.NotApplicable, result.Outcome);
        Assert.Empty(result.WeekChecks);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public async Task Verify_CalledTwiceWithIdenticalInput_ProducesIdenticalOutput()
    {
        var plan = await RealPlanAsync(10);

        var first = LongRunProgressionVerifier.Verify(plan.WeeklyVolumePlan, plan.LongRunProgression, VolumeSafetyPolicy.Default);
        var second = LongRunProgressionVerifier.Verify(plan.WeeklyVolumePlan, plan.LongRunProgression, VolumeSafetyPolicy.Default);

        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.WeekChecks, second.WeekChecks);
        Assert.Equal(first.Findings, second.Findings);
    }

    [Fact]
    public void Verify_IsPureFunction_ThreeParametersOnly()
    {
        var method = typeof(LongRunProgressionVerifier).GetMethod(nameof(LongRunProgressionVerifier.Verify), BindingFlags.Public | BindingFlags.Static)!;

        Assert.Equal(3, method.GetParameters().Length);
        Assert.Equal(typeof(CatalogWeeklyVolumePlan), method.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(CatalogLongRunProgression), method.GetParameters()[1].ParameterType);
        Assert.Equal(typeof(VolumeSafetyPolicy), method.GetParameters()[2].ParameterType);
    }

    [Fact]
    public void LongRunProgressionVerifier_HasNoCallSiteInApplicationOrApiProductionCode()
    {
        DarkReachabilityAssertions.AssertVerifierIsReachableOnlyFromDarkOrchestrator(nameof(LongRunProgressionVerifier));
    }

    [Fact]
    public void LongRunProgressionVerifier_DoesNotCallPlannerLoaderOrResolverLayer()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog",
            "Schedule", "Materialization", "LongRunProgressionVerifier.cs"));

        Assert.DoesNotContain("new CatalogVolumeAndLongRunPlanner(", source);
        Assert.DoesNotContain("new CatalogPeakVolumeBandLoader(", source);
        Assert.DoesNotContain("new PlanCatalogBundleLoader(", source);
        Assert.DoesNotContain("RuntimeConditionResolutionResult", source);
    }

    // ── Real-pipeline construction (read-only: real candidate, real allocator, ──
    // ── real progression/binder/prescription-context/planner -- no live wiring) ──

    private static async Task<CatalogVolumeAndLongRunPlan> RealPlanAsync(int weeks)
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

        return new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(candidate, boundPlan, prescriptionContext, peakBand));
    }

    private static CatalogWeeklyVolumePlan SyntheticVolumePlan(params CatalogWeeklyVolumeWeek[] weeks) => new()
    {
        CandidateKey = "SYNTHETIC", CandidateVersion = 1,
        FirstWeekVolumeKm = weeks.Length > 0 ? weeks[0].PlannedWeeklyVolumeKm : 0,
        PeakVolumeKm = weeks.Length > 0 ? weeks.Max(w => w.PlannedWeeklyVolumeKm) : 0,
        StartingVolumeDecision = new StartingVolumeDecision(20, PrescriptionInputState.Available, 20, WeeklyVolumeAnchorSource.RecentFourWeekAverage, CatalogVolumeClamp.None, CatalogEvidenceBasis.EvidenceInformed, CatalogDecisionStatus.CanonicalConfirmed, "SYNTHETIC"),
        ReachablePeakDecision = new ReachablePeakDecision(20, 20, 20, PeakBandClassification.WithinTypicalPeakBand, VolumeSafetyPolicy.Default.PreferredMaxWeeklyIncreaseRatio, VolumeSafetyPolicy.Default.HardMaxWeeklyIncreaseRatio, VolumeSafetyPolicy.Default.AbsoluteWeeklyIncrementCapKm, CatalogEvidenceBasis.EvidenceInformed, CatalogDecisionStatus.CanonicalConfirmed, "SYNTHETIC"),
        TaperVolumeDecision = new TaperVolumeDecision(VolumeSafetyPolicy.Default.TaperVolumeMultiplier, 1d - VolumeSafetyPolicy.Default.TaperVolumeMultiplier, "SYNTHETIC", CatalogEvidenceBasis.EvidenceInformed, CatalogDecisionStatus.ExplicitProductDefault, "SYNTHETIC"),
        CatalogBounds = new CatalogVolumeBounds(0, 1000, "SYNTHETIC", 1),
        Weeks = weeks,
        DecisionTrace = [],
        ValidationResult = new CatalogVolumeValidationResult(true, []),
    };

    private static CatalogWeeklyVolumeWeek Week(int weekNumber, double volumeKm, bool isTaper) => new()
    {
        WeekNumber = weekNumber, PhaseKey = isTaper ? "TAPER" : "BUILD", PlannedWeeklyVolumeKm = volumeKm, ChangeKm = 0,
        VolumeClassification = isTaper ? "TAPER" : "BUILDING", IsRecoveryOrDeloadWeek = false, IsTaperWeek = isTaper,
        AnchorSource = WeeklyVolumeAnchorSource.RecentFourWeekAverage, CatalogBounds = new CatalogVolumeBounds(0, 1000, "SYNTHETIC", 1),
        AppliedClamp = CatalogVolumeClamp.None, DecisionReason = "SYNTHETIC", SourceArtifactKey = "SYNTHETIC", SourceArtifactVersion = 1, Provenance = "SYNTHETIC",
    };

    private static CatalogLongRunProgression SyntheticLongRunPlan(params CatalogLongRunWeek[] weeks) => new()
    {
        CandidateKey = "SYNTHETIC", CandidateVersion = 1, Weeks = weeks, DecisionTrace = [],
        ValidationResult = new CatalogVolumeValidationResult(true, []),
        WeeklyShareDecision = new LongRunWeeklyShareDecision(
            VolumeSafetyPolicy.Default.LongRunPreferredMinimumShare, VolumeSafetyPolicy.Default.LongRunPreferredMaximumShare,
            VolumeSafetyPolicy.Default.LongRunSelectionShare, VolumeSafetyPolicy.Default.LongRunHardCapShare,
            CatalogEvidenceBasis.ProductPracticeInformed, CatalogDecisionStatus.ExplicitProductDefault, "SYNTHETIC"),
    };

    private static CatalogLongRunWeek LongRunWeek(int weekNumber, double totalVolumeKm, double longRunKm) => new()
    {
        WeekNumber = weekNumber, PhaseKey = "BUILD", PlannedLongRunDistanceKm = longRunKm, PlannedWeeklyVolumeKm = totalVolumeKm,
        LongRunShareOfWeeklyVolume = totalVolumeKm == 0 ? 0 : longRunKm / totalVolumeKm,
        LongRunAnchorSource = LongRunAnchorSource.WeeklyVolumeDerived, RecentLongestRunState = PrescriptionInputState.Available,
        CompatibilityClamp = CatalogVolumeClamp.None, CatalogBounds = new CatalogVolumeBounds(0, 1000, "SYNTHETIC", 1),
        ChangeFromPreviousWeekKm = 0, DecisionReason = "SYNTHETIC", SourceArtifactKey = "SYNTHETIC", SourceArtifactVersion = 1, Provenance = "SYNTHETIC",
    };
}
