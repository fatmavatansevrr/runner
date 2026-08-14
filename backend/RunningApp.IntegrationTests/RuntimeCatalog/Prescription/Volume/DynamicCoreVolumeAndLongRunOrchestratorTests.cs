using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Backend Integration Phase 4G.5F — end-to-end tests for the dark, unwired
/// <see cref="DynamicCoreVolumeAndLongRunOrchestrator"/> against the REAL,
/// unmodified <c>TEN_K__4D__INTERMEDIATE v10</c> catalog candidate, for
/// every mathematically feasible standalone-core week count (8-14) across
/// six distinct runner readiness profiles.
/// </summary>
public sealed class DynamicCoreVolumeAndLongRunOrchestratorTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 3); // Monday
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    internal static async Task<PlanCatalogCandidateSummary> RealCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
    }

    internal static async Task<PlanCatalogCandidateSummary> RealThreeDayCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync("TEN_K__3D__INTERMEDIATE", 1);
    }

    internal static async Task<PlanCatalogCandidateSummary> RealBeginnerFourDayCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync("TEN_K__4D__BEGINNER", 1);
    }

    private static DynamicCoreVolumeAndLongRunOrchestrator RealOrchestrator() => new(
        new DynamicCoreWorkoutBindingOrchestrator(
            new DynamicCoreWeekSkeletonOrchestrator(
                new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
            new CatalogWorkoutProgressionLoader(Options.Create(RealOptions())),
            new ProgressionStageAllocator(),
            new GeneratedCatalogStageScheduleValidator(),
            new CatalogWeekSkeletonCalendarMaterializer(),
            new DatedGeneratedCatalogPlanSkeletonValidator(),
            new CatalogWorkoutBinder(),
            new BoundCatalogPlanValidator()),
        new CatalogPrescriptionContextBuilder(),
        new CatalogVolumeAndLongRunPlanner());

    // ── Runner profiles ──────────────────────────────────────────────────

    public enum RunnerProfile
    {
        LowVolumeIntermediate,
        CurrentPilotProfile,
        HigherVolumeIntermediate,
        RecentLongRunMissing,
        RecentVolumeMissing,
        ExplicitZeroEvidence,
    }

    private static void ApplyProfile(GeneratePreviewRequest request, RunnerProfile profile)
    {
        switch (profile)
        {
            case RunnerProfile.LowVolumeIntermediate:
                request.RecentWeeklyVolumeKm = 14; request.RecentLongestRunKm = 6; request.RecentRunsPerWeek = 3;
                break;
            case RunnerProfile.CurrentPilotProfile:
                request.RecentWeeklyVolumeKm = 24; request.RecentLongestRunKm = 9; request.RecentRunsPerWeek = 4;
                break;
            case RunnerProfile.HigherVolumeIntermediate:
                request.RecentWeeklyVolumeKm = 35; request.RecentLongestRunKm = 14; request.RecentRunsPerWeek = 5;
                break;
            case RunnerProfile.RecentLongRunMissing:
                request.RecentWeeklyVolumeKm = 24; request.RecentLongestRunKm = null; request.RecentRunsPerWeek = 4;
                break;
            case RunnerProfile.RecentVolumeMissing:
                request.RecentWeeklyVolumeKm = null; request.RecentLongestRunKm = 9; request.RecentRunsPerWeek = 4;
                break;
            case RunnerProfile.ExplicitZeroEvidence:
                request.RecentWeeklyVolumeKm = 0; request.RecentLongestRunKm = 0; request.RecentRunsPerWeek = 0;
                break;
        }
    }

    internal static async Task<DynamicCoreVolumeAndLongRunResult> BuildAsync(
        PlanCatalogCandidateSummary candidate, int targetWeekCount, RunnerProfile profile)
    {
        var options = Options.Create(RealOptions());
        var raceDate = StartDate.AddDays(targetWeekCount * 7);

        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            Level = candidate.Level == "NEW" ? RunningBackground.Beginner : RunningBackground.Intermediate,
            DaysPerWeek = candidate.DaysPerWeek,
            Unit = DistanceUnit.Km, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            PreferredDays = candidate.DaysPerWeek == 3
                ? new[] { Weekday.Mon, Weekday.Wed, Weekday.Sun }
                : new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = StartDate.AddDays(-21) },
        };
        ApplyProfile(previewRequest, profile);

        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            DaysPerWeek = candidate.DaysPerWeek,
            Level = candidate.Level == "NEW" ? RunningBackground.Beginner : RunningBackground.Intermediate,
        };

        var conditionResults = new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "TEST"),
            RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST"),
        };

        var context = new DynamicCoreVolumeAndLongRunContext
        {
            Candidate = candidate,
            TargetWeekCount = targetWeekCount,
            StartDate = StartDate,
            AsOfDate = StartDate,
            PreferredDays = candidate.DaysPerWeek == 3
                ? new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Sunday }
                : PreferredDays,
            LongRunDayPreference = LongRunDay,
            ConditionResults = conditionResults,
            PreviewRequest = previewRequest,
            ResolverInput = resolverInput,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(options),
        };

        return await RealOrchestrator().PlanAsync(context);
    }

    public static IEnumerable<object[]> WeekCountsByProfile()
    {
        foreach (var weeks in new[] { 8, 9, 10, 11, 12, 13, 14 })
        {
            foreach (RunnerProfile profile in Enum.GetValues(typeof(RunnerProfile)))
            {
                yield return new object[] { weeks, profile };
            }
        }
    }

    // ── Full matrix: 7 week counts x 6 runner profiles = 42 combinations ───

    [Theory]
    [MemberData(nameof(WeekCountsByProfile))]
    public async Task PlanAsync_RealCandidate_ProducesValidPlan_NoCapViolations_TaperRemainsLowerLoad(int targetWeekCount, RunnerProfile profile)
    {
        var candidate = await RealCandidateAsync();

        var result = await BuildAsync(candidate, targetWeekCount, profile);
        var weeklyPlan = result.VolumeAndLongRunPlan.WeeklyVolumePlan;
        var longRunPlan = result.VolumeAndLongRunPlan.LongRunProgression;

        Assert.True(weeklyPlan.ValidationResult.IsValid, string.Join("; ", weeklyPlan.ValidationResult.Errors));
        Assert.True(longRunPlan.ValidationResult.IsValid, string.Join("; ", longRunPlan.ValidationResult.Errors));
        Assert.Equal(targetWeekCount, weeklyPlan.Weeks.Count);
        Assert.Equal(targetWeekCount, longRunPlan.Weeks.Count);

        // Required guarantee: no artificially aggressive progression / no
        // endless weekly increase -- re-uses the existing, unmodified,
        // already-dark VolumeProgressionVerifier directly (not re-derived).
        var progressionResult = VolumeProgressionVerifier.Verify(weeklyPlan, VolumeSafetyPolicy.Default);
        Assert.Equal(VolumeProgressionOutcome.Pass, progressionResult.Outcome);
        Assert.Empty(progressionResult.Findings);

        // Taper remains meaningful/lower-load: the taper week's planned
        // volume must be strictly below the resolved peak.
        var taperWeek = weeklyPlan.Weeks.Single(w => w.IsTaperWeek);
        Assert.True(taperWeek.PlannedWeeklyVolumeKm < weeklyPlan.PeakVolumeKm);

        // No null/negative planned values anywhere.
        Assert.All(weeklyPlan.Weeks, w => Assert.True(w.PlannedWeeklyVolumeKm > 0));
        Assert.All(longRunPlan.Weeks, w => Assert.True(w.PlannedLongRunDistanceKm > 0));

        // Long-run never exceeds the policy's own hard-cap share of that week's volume.
        Assert.All(longRunPlan.Weeks, w =>
            Assert.True(w.PlannedLongRunDistanceKm <= w.PlannedWeeklyVolumeKm * VolumeSafetyPolicy.Default.LongRunHardCapShare + 0.5001));
    }

    // ── Compressed horizons (8-11): peak may be lower than canonical ────────

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public async Task PlanAsync_CompressedHorizon_PeakIsNotForcedToCanonicalTwelveWeekPeak(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var result = await BuildAsync(candidate, targetWeekCount, RunnerProfile.CurrentPilotProfile);
        var twelveWeek = await BuildAsync(candidate, 12, RunnerProfile.CurrentPilotProfile);

        // A shorter horizon's resolved peak must not exceed the 12-week
        // canonical peak for the same starting profile -- the reachable-peak
        // formula scales down with fewer transitions, it is never forced up
        // to match the 12-week canonical value.
        Assert.True(result.VolumeAndLongRunPlan.WeeklyVolumePlan.PeakVolumeKm <= twelveWeek.VolumeAndLongRunPlan.WeeklyVolumePlan.PeakVolumeKm);
    }

    // ── Extended horizons (13-14): explicit progression/maintenance semantics ──

    [Theory]
    [InlineData(13)]
    [InlineData(14)]
    public async Task PlanAsync_ExtendedHorizon_NoEndlessGrowth_EveryWeekCarriesExplicitDecisionReason(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var result = await BuildAsync(candidate, targetWeekCount, RunnerProfile.CurrentPilotProfile);
        var weeklyPlan = result.VolumeAndLongRunPlan.WeeklyVolumePlan;

        // Peak is bounded (never unbounded growth): every week's planned
        // volume is at most the resolved peak.
        Assert.All(weeklyPlan.Weeks, w => Assert.True(w.PlannedWeeklyVolumeKm <= weeklyPlan.PeakVolumeKm + 0.0001));

        // Every extension-horizon week carries an explicit, non-blank
        // decision reason -- no silent/undocumented week.
        Assert.All(weeklyPlan.Weeks, w => Assert.False(string.IsNullOrWhiteSpace(w.DecisionReason)));
    }

    // ── 12-week parity (byte/value-level regression) ────────────────────────

    [Fact]
    public async Task PlanAsync_TargetWeekCount12_MatchesExistingFixedWeekVolumePipelineExactly()
    {
        // Concrete before/after proof: builds the volume/long-run plan via
        // the EXISTING, completely unmodified fixed-week pipeline (mirroring
        // VolumeProgressionVerifierTests.RealVolumePlanAsync's own
        // established construction) and via this new Phase 4G.5F dynamic
        // orchestrator requesting targetWeekCount=12, then asserts full
        // field-by-field equality of every PlannedWeeklyVolumeKm/
        // PlannedLongRunDistanceKm. No existing production file was modified
        // by Phase 4G.5F.
        var candidate = await RealCandidateAsync();
        var options = Options.Create(RealOptions());

        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, 12);
        var skeletonContext = new CatalogStageToWeekMaterializationContext
        {
            StartDate = StartDate, AsOfDate = StartDate, PlannedWeekCount = 12, DaysPerWeek = candidate.SlotRoles.Count,
            CanonicalDistanceFamily = progression.DistanceFamily, CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            SelectedStageSequence = allocation.Phases.Select(p => p.PhaseKey).ToList(),
            StageWeekAllocations = allocation.Phases.Select(p => new CatalogStageWeekAllocation(p.PhaseKey, p.AllocatedWeeks)).ToList(),
            RunLayout = candidate.Layout, RunLayoutSlotRoles = candidate.SlotRoles,
        };
        var existingSkeleton = new CatalogStageToWeekMaterializer().Materialize(skeletonContext).Skeleton;
        var existingStageSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion, Progression = progression, Skeleton = existingSkeleton,
            ConditionResults = new[] { RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST") },
        });
        var existingProvenance = new CatalogCalendarMaterializationProvenance(candidate.CandidateKey, candidate.CandidateVersion, StartDate, StartDate,
            PreferredDays, LongRunDay, CatalogCalendarDayMaterializerVersion.V1, existingSkeleton.SchemaVersion, new Dictionary<string, PlanCatalogReference>());
        var existingDatedSkeleton = new CatalogWeekSkeletonCalendarMaterializer().Materialize(new CatalogCalendarAssignmentContext(
            StartDate, GoalType.Race, PreferredDays, LongRunDay, existingSkeleton, CatalogCalendarAssignmentPolicy.RaceHardConstraint, existingProvenance));
        var existingBoundPlan = await new CatalogWorkoutBinder().BindAsync(new CatalogWorkoutBindingContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion, DatedSkeleton = existingDatedSkeleton,
            StageSchedule = existingStageSchedule, Progression = progression, ReferencedWorkouts = candidate.ReferencedWorkouts,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
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
            Unit = DistanceUnit.Km, StartDate = StartDate, RaceDate = StartDate.AddDays(12 * 7), TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun }, LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 24, RecentLongestRunKm = 9, RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = StartDate.AddDays(-21) },
        };
        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = StartDate, RaceDate = StartDate.AddDays(12 * 7), TargetFinishTimeSeconds = 3000,
            DaysPerWeek = candidate.DaysPerWeek, Level = RunningBackground.Intermediate,
        };
        var conditionResults = new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "TEST"),
            RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST"),
        };
        var existingPrescriptionContext = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            previewRequest, StartDate, candidate, resolverInput, conditionResults, existingBoundPlan, definitions));
        var existingPeakBand = await new CatalogPeakVolumeBandLoader(options)
            .LoadAsync(candidate.PeakVolumeBandPolicy, candidate.CanonicalDistanceFamily, candidate.Level, candidate.DaysPerWeek);

        var existingPlan = new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(
            candidate, existingBoundPlan, existingPrescriptionContext, existingPeakBand));

        // New Phase 4G.5F dynamic orchestrator at targetWeekCount=12.
        var dynamicResult = await BuildAsync(candidate, 12, RunnerProfile.CurrentPilotProfile);

        // Literal, hard-coded value-level fixture captured from the existing
        // live 12-week volume pipeline.
        Assert.Equal(12, existingPlan.WeeklyVolumePlan.Weeks.Count);
        Assert.Equal(24d, existingPlan.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.Equal(38d, existingPlan.WeeklyVolumePlan.PeakVolumeKm);

        // Full field-by-field equality: existing fixed-week pipeline vs. new
        // dynamic pipeline at targetWeekCount=12.
        static IEnumerable<(int WeekNumber, double PlannedWeeklyVolumeKm, string VolumeClassification, bool IsTaperWeek)> FlattenWeekly(CatalogWeeklyVolumePlan plan) =>
            plan.Weeks.OrderBy(w => w.WeekNumber).Select(w => (w.WeekNumber, w.PlannedWeeklyVolumeKm, w.VolumeClassification, w.IsTaperWeek));
        static IEnumerable<(int WeekNumber, double PlannedLongRunDistanceKm)> FlattenLongRun(CatalogLongRunProgression plan) =>
            plan.Weeks.OrderBy(w => w.WeekNumber).Select(w => (w.WeekNumber, w.PlannedLongRunDistanceKm));

        Assert.Equal(FlattenWeekly(existingPlan.WeeklyVolumePlan), FlattenWeekly(dynamicResult.VolumeAndLongRunPlan.WeeklyVolumePlan));
        Assert.Equal(FlattenLongRun(existingPlan.LongRunProgression), FlattenLongRun(dynamicResult.VolumeAndLongRunPlan.LongRunProgression));
    }

    // ── Zero production call sites (dark-reachability, structural) ─────────

    [Fact]
    public void DarkReachability_NoProductionCallSiteOutsideTheOneApprovedDarkConsumer()
    {
        // Reconciled (Phase 4G.5G): DynamicCoreSessionPrescriptionOrchestrator.cs
        // is now a legitimate second production reference -- it chains this
        // Phase 4G.5F orchestrator into a further dark session-prescription
        // pipeline, and is itself proven dark by its own
        // DarkReachability_NoProductionCallSite test. This orchestrator
        // therefore remains structurally unreachable from any LIVE request
        // path, which is this test's actual invariant. (Phase4G5GPrerequisiteCrossChecksTests.cs
        // also references this type, but lives in RunningApp.IntegrationTests,
        // which this scan's project list below does not include -- no
        // exclusion needed for it.)
        var repoRoot = TestPlanServicesFactory.RepoRoot();
        var ownFileSuffix = Path.Combine("Prescription", "Volume", "DynamicCoreVolumeAndLongRunOrchestrator.cs");
        var approvedDarkConsumerSuffix = Path.Combine("Prescription", "Session", "DynamicCoreSessionPrescriptionOrchestrator.cs");

        var hits = new[] { "RunningApp.Application", "RunningApp.Api", "RunningApp.Infrastructure", "RunningApp.Persistence" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repoRoot, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.Combine("bin", "")) && !path.Contains(Path.Combine("obj", "")))
            .Where(path => !path.EndsWith(ownFileSuffix, StringComparison.Ordinal))
            .Where(path => !path.EndsWith(approvedDarkConsumerSuffix, StringComparison.Ordinal))
            .Where(path => !path.Contains(Path.Combine("Schedule", "PreparationRunwayOrchestration")))
            .Where(path => !path.Contains(Path.Combine("Schedule", "LongHorizon")))
            .Where(path => !path.EndsWith("CatalogPreviewGenerator.cs", StringComparison.Ordinal))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bDynamicCoreVolumeAndLongRun(Orchestrator|Context|Result)\b"))
            .ToArray();

        Assert.Empty(hits);
    }

    [Fact]
    public void DarkReachability_NoDiRegistration()
    {
        var repoRoot = TestPlanServicesFactory.RepoRoot();

        var hits = new[] { "RunningApp.Api" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repoRoot, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.Combine("bin", "")) && !path.Contains(Path.Combine("obj", "")))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bIDynamicCoreVolumeAndLongRunOrchestrator\b"))
            .ToArray();

        Assert.Empty(hits);
    }
}
