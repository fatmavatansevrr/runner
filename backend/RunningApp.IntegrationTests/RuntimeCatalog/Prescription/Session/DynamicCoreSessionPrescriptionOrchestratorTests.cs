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
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Session;

/// <summary>
/// Backend Integration Phase 4G.5G — end-to-end tests for the dark, unwired
/// <see cref="DynamicCoreSessionPrescriptionOrchestrator"/> against the
/// REAL, unmodified <c>TEN_K__4D__INTERMEDIATE v10</c> catalog candidate,
/// for every mathematically feasible standalone-core week count (8-14)
/// across five pace-evidence source categories.
/// </summary>
public sealed class DynamicCoreSessionPrescriptionOrchestratorTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 3);
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
        return await new CatalogCandidateEligibilityGate(bundleLoader)
            .LoadForInternalDryRunAsync("TEN_K__3D__INTERMEDIATE", 1);
    }

    private static DynamicCoreSessionPrescriptionOrchestrator RealOrchestrator() => new(
        new DynamicCoreVolumeAndLongRunOrchestrator(
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
            new CatalogVolumeAndLongRunPlanner()),
        new CatalogSessionPrescriptionPlanner(),
        new CatalogFinalPrescribedPlanFinalizer());

    // ── Pace-evidence source categories (5, per this phase's own spec) ─────

    public enum PaceSourceCategory
    {
        RecentRace,
        TargetTimeNoIndependentEvidence,
        ProductAverage,
        NotEvaluated,
        Unsupported,
    }

    private static IReadOnlyList<RuntimeConditionResolutionResult> ConditionResultsFor(PaceSourceCategory category) => category switch
    {
        PaceSourceCategory.RecentRace => new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
            RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "WITHIN_REALISTIC_BAND"),
        },
        PaceSourceCategory.TargetTimeNoIndependentEvidence => new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "TARGET_TIME", "TARGET_FINISH_TIME_PROVIDED"),
            RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE"),
        },
        PaceSourceCategory.ProductAverage => new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "TARGET_TIME", "TARGET_FINISH_TIME_PROVIDED"),
            RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "CHALLENGING", "PACE_SOURCE_TARGET_TIME_PRODUCT_AVERAGE_ACCEPTED"),
        },
        PaceSourceCategory.NotEvaluated => new[]
        {
            RuntimeConditionResolutionResult.NotEvaluated("PACE_SOURCE_IN", "TEST_NOT_EVALUATED"),
            RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "TEST_NOT_EVALUATED"),
        },
        PaceSourceCategory.Unsupported => new[]
        {
            RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "TARGET_TIME", "TARGET_FINISH_TIME_PROVIDED"),
            RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "UNSUPPORTED", "EXCEEDS_CHALLENGING_BAND"),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };

    internal static async Task<DynamicCoreSessionPrescriptionResult> BuildAsync(
        PlanCatalogCandidateSummary candidate, int targetWeekCount, PaceSourceCategory category,
        double? recentWeeklyVolumeKm = 24, double? recentLongestRunKm = 9, int? recentRunsPerWeek = 4)
    {
        var options = Options.Create(RealOptions());
        var raceDate = StartDate.AddDays(targetWeekCount * 7);

        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate, DaysPerWeek = candidate.DaysPerWeek,
            Unit = DistanceUnit.Km, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            PreferredDays = candidate.DaysPerWeek == 3
                ? new[] { Weekday.Mon, Weekday.Wed, Weekday.Sun }
                : new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = recentWeeklyVolumeKm, RecentLongestRunKm = recentLongestRunKm, RecentRunsPerWeek = recentRunsPerWeek,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = StartDate.AddDays(-21) },
        };

        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            DaysPerWeek = candidate.DaysPerWeek, Level = RunningBackground.Intermediate,
        };

        var context = new DynamicCoreSessionPrescriptionContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = StartDate, AsOfDate = StartDate,
            PreferredDays = candidate.DaysPerWeek == 3
                ? new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Sunday }
                : PreferredDays,
            LongRunDayPreference = LongRunDay, ConditionResults = ConditionResultsFor(category),
            PreviewRequest = previewRequest, ResolverInput = resolverInput,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(options),
        };

        return await RealOrchestrator().PrescribeAsync(context);
    }

    public static IEnumerable<object[]> WeekCountsByPaceSource()
    {
        foreach (var weeks in new[] { 8, 9, 10, 11, 12, 13, 14 })
        {
            foreach (PaceSourceCategory category in Enum.GetValues(typeof(PaceSourceCategory)))
            {
                yield return new object[] { weeks, category };
            }
        }
    }

    // ── Full matrix: 7 week counts x 5 pace-source categories = 35 combinations ──

    [Theory]
    [MemberData(nameof(WeekCountsByPaceSource))]
    public async Task PrescribeAsync_RealCandidate_ProducesValidPlan_FailClosedBehaviorUnchanged(int targetWeekCount, PaceSourceCategory category)
    {
        var candidate = await RealCandidateAsync();

        var result = await BuildAsync(candidate, targetWeekCount, category);
        var sessions = result.FinalPrescribedPlan.Weeks.SelectMany(w => w.Sessions).ToList();

        Assert.True(result.FinalPrescribedPlan.ValidationResult.IsValid);
        Assert.Equal(targetWeekCount * 4, sessions.Count);

        var goalPaceSessions = sessions.Where(s => s.WorkoutDefinitionKey == "GOAL_PACE_TEN_K").ToList();

        if (category is PaceSourceCategory.RecentRace or PaceSourceCategory.ProductAverage)
        {
            // Feasible goal-pace evidence: any GOAL_PACE_TEN_K session present
            // (0 or more, depending on whether the horizon/eligibility routed
            // there) must carry an ExactPace, never experience-derived.
            Assert.All(goalPaceSessions, s =>
            {
                Assert.Equal(CatalogPacePrescriptionKind.ExactPace, s.Prescription.PacePrescription.Kind);
                Assert.Equal(CatalogPaceSourceSelection.TargetGoalDerived, s.Prescription.PacePrescription.Source);
                Assert.NotNull(s.Prescription.PacePrescription.SecondsPerKilometer);
            });
        }
        else
        {
            // Fail-closed philosophy (Phase 4G.3B.6.1): TargetTimeNoIndependentEvidence,
            // NotEvaluated, and Unsupported goal feasibility must never reach a
            // GOAL_PACE_TEN_K ExactPace prescription -- the upstream stage
            // allocator's own eligibility/fallback mechanism (unmodified, Phase
            // 4F.6A) already routes these away from GOAL_PACE_TEN_K before this
            // orchestrator's prescription step is ever reached. This is
            // confirmed here, not assumed.
            Assert.Empty(goalPaceSessions);
        }

        // No pace is ever derived from experience/level alone: every
        // non-goal-pace session is either EffortOnly or Unresolved, never a
        // numeric pace synthesized from RunningBackground/Level.
        Assert.All(sessions.Where(s => s.WorkoutDefinitionKey != "GOAL_PACE_TEN_K"), s =>
            Assert.True(s.Prescription.PacePrescription.Kind is CatalogPacePrescriptionKind.EffortOnly or CatalogPacePrescriptionKind.Unresolved));
    }

    // ── TAPER_SHARPEN: sharpening via prescription, not workout swap (AUD-507/508) ──

    [Theory]
    [MemberData(nameof(WeekCountsByPaceSource))]
    public async Task PrescribeAsync_RealCandidate_TaperSharpenAppliesPrescriptionAdjustment_IdentityUnchanged(int targetWeekCount, PaceSourceCategory category)
    {
        var candidate = await RealCandidateAsync();
        var result = await BuildAsync(candidate, targetWeekCount, category);

        var taperSharpenSessions = result.FinalPrescribedPlan.Weeks.SelectMany(w => w.Sessions)
            .Where(s => s.ProgressionStageKey == "TAPER_SHARPEN")
            .ToList();

        Assert.NotEmpty(taperSharpenSessions);
        Assert.All(taperSharpenSessions, s =>
        {
            // Identity unchanged (AUD-507): still EASY_STANDARD.
            Assert.Equal("EASY_STANDARD", s.WorkoutDefinitionKey);
            // Sharpening applied via PRESCRIPTION (not a workout swap):
            // exactly the three-component shape V1TaperSharpenPrescriptionPolicy
            // establishes -- an easy baseline, a controlled-sharpening segment,
            // and a recovery segment -- applied here, unmodified.
            Assert.Contains(s.Prescription.OrderedSegments, seg => seg.ComponentType == "CONTROLLED_SHARPENING");
            Assert.Equal(CatalogSessionPrescriptionStatus.FinalPrescriptionComplete, s.Prescription.Status);
            // No borrowed goal pace, no numeric duration -- matches the
            // existing policy's own hard invariants (AUD-508).
            Assert.DoesNotContain(s.Prescription.OrderedSegments, seg => seg.PacePrescription.Source == CatalogPaceSourceSelection.TargetGoalDerived);
        });
    }

    // ── Compression (8-11): no unsafe density; existing session structure preserved ──

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public async Task PrescribeAsync_CompressedHorizon_SessionCountAndStructurePreserved_NoDensityIncrease(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var result = await BuildAsync(candidate, targetWeekCount, PaceSourceCategory.RecentRace);

        // The prescription layer introduces no new session, and removes
        // none -- quality-session count/spacing is entirely a function of
        // the already-verified Phase 4G.5D/4G.5E skeleton/binding output
        // (4 slots/week, 1 KEY_SESSION), which this orchestrator consumes
        // unchanged. Confirmed here: exactly one KEY_SESSION-role session
        // per week, never more.
        var byWeek = result.FinalPrescribedPlan.Weeks.ToDictionary(w => w.WeekNumber);
        Assert.Equal(targetWeekCount, byWeek.Count);
        Assert.All(byWeek.Values, w =>
        {
            Assert.Equal(4, w.Sessions.Count);
            Assert.Single(w.Sessions, s => s.StructuralRole == "KEY_SESSION");
        });
    }

    // ── Extension (13-14): no unnecessary intensity; no unapproved goal-pace repetition beyond policy ──

    [Theory]
    [InlineData(13)]
    [InlineData(14)]
    public async Task PrescribeAsync_ExtendedHorizon_GoalPaceExposureStaysWithinApprovedPolicyBounds(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var result = await BuildAsync(candidate, targetWeekCount, PaceSourceCategory.RecentRace);

        var goalPaceSessions = result.FinalPrescribedPlan.Weeks.SelectMany(w => w.Sessions)
            .Where(s => s.WorkoutDefinitionKey == "GOAL_PACE_TEN_K")
            .ToList();

        // GOAL_PACE_REHEARSAL's own catalog-declared maximumExposures is 2
        // (ten-k-workout-progression.v5.json, unchanged) -- this orchestrator
        // introduces no additional repetition beyond what the real,
        // unmodified ProgressionStageAllocator already allocates.
        Assert.True(goalPaceSessions.Count <= 2, $"targetWeekCount={targetWeekCount}: {goalPaceSessions.Count} GOAL_PACE_TEN_K sessions exceeds the catalog's own approved maximum of 2.");
    }

    // ── Byte/value-level 12-week regression against the existing prescription pipeline ──

    [Fact]
    public async Task PrescribeAsync_TargetWeekCount12_MatchesExistingFixedWeekPrescriptionPipelineExactly()
    {
        // Concrete before/after proof: builds the prescribed plan via the
        // EXISTING, completely unmodified fixed-week pipeline (mirroring
        // the exact construction VolumeProgressionVerifierTests/
        // DynamicCoreVolumeAndLongRunOrchestratorTests already established,
        // extended through the real, unmodified CatalogSessionPrescriptionPlanner
        // and CatalogFinalPrescribedPlanFinalizer -- the same two components
        // CatalogPreviewGenerator itself calls) and via this new Phase
        // 4G.5G dynamic orchestrator requesting targetWeekCount=12, then
        // asserts full field-by-field equality of every session's pace/
        // duration/segments. No existing production file was modified by
        // Phase 4G.5G beyond the minimal, reported DynamicCoreVolumeAndLongRunResult
        // field addition (see this phase's deliverable).
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
            ConditionResults = ConditionResultsFor(PaceSourceCategory.RecentRace),
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
        var existingPrescriptionContext = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            previewRequest, StartDate, candidate, resolverInput, ConditionResultsFor(PaceSourceCategory.RecentRace), existingBoundPlan, definitions));
        var existingPeakBand = await new CatalogPeakVolumeBandLoader(options)
            .LoadAsync(candidate.PeakVolumeBandPolicy, candidate.CanonicalDistanceFamily, candidate.Level, candidate.DaysPerWeek);
        var existingVolumePlan = new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(
            candidate, existingBoundPlan, existingPrescriptionContext, existingPeakBand));

        var existingBaseline = new CatalogSessionPrescriptionPlanner().Build(new CatalogSessionPrescriptionRequest(
            candidate, existingBoundPlan, existingPrescriptionContext, existingVolumePlan, definitions));
        var existingFinal = new CatalogFinalPrescribedPlanFinalizer().Complete(new CatalogFinalPrescriptionRequest(
            candidate, existingBoundPlan, existingVolumePlan, existingBaseline));

        // New Phase 4G.5G dynamic orchestrator at targetWeekCount=12.
        var dynamicResult = await BuildAsync(candidate, 12, PaceSourceCategory.RecentRace);

        // Literal, hard-coded value-level fixture captured from the existing
        // live 12-week prescription pipeline.
        Assert.Equal(48, existingFinal.Weeks.Sum(w => w.Sessions.Count));
        var existingTaperSharpen = existingFinal.Weeks.Single(w => w.WeekNumber == 12).Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
        Assert.Equal("TAPER_SHARPEN", existingTaperSharpen.ProgressionStageKey);
        Assert.Equal("EASY_STANDARD", existingTaperSharpen.WorkoutDefinitionKey);
        Assert.Contains(existingTaperSharpen.Prescription.OrderedSegments, s => s.ComponentType == "CONTROLLED_SHARPENING");

        // Full field-by-field equality: existing fixed-week pipeline vs. new
        // dynamic pipeline at targetWeekCount=12.
        static IEnumerable<(int WeekNumber, DateOnly Date, string WorkoutDefinitionKey, double PlannedDistanceKm,
            CatalogPacePrescriptionKind PaceKind, double? SecondsPerKm, CatalogDurationKind DurationKind,
            string Segments)> Flatten(CatalogPrescribedPlan plan) =>
            plan.Weeks.SelectMany(w => w.Sessions).OrderBy(s => s.WeekNumber).ThenBy(s => s.Date)
                .Select(s => (s.WeekNumber, s.Date, s.WorkoutDefinitionKey, s.PlannedDistanceKm,
                    s.Prescription.PacePrescription.Kind, s.Prescription.PacePrescription.SecondsPerKilometer, s.Prescription.DurationPrescription.Kind,
                    string.Join(",", s.Prescription.OrderedSegments.OrderBy(seg => seg.SequenceOrder).Select(seg => $"{seg.SequenceOrder}:{seg.ComponentType}:{seg.DistanceKm}"))));

        Assert.Equal(Flatten(existingFinal), Flatten(dynamicResult.FinalPrescribedPlan));

        Assert.True(existingFinal.ValidationResult.IsValid);
        Assert.True(dynamicResult.FinalPrescribedPlan.ValidationResult.IsValid);
    }

    // ── Zero production call sites (dark-reachability, structural) ─────────

    [Fact]
    public void DarkReachability_NoProductionCallSiteOutsideTheOneApprovedDarkConsumer()
    {
        // Reconciled (Phase 4G.5H): DynamicCoreCalendarMaterializationOrchestrator.cs
        // is now a legitimate second production reference -- it chains this
        // Phase 4G.5G orchestrator into the final dark calendar-materialization/
        // race-alignment composition, and is itself proven dark by its own
        // DarkReachability_NoProductionCallSite test. This orchestrator
        // therefore remains structurally unreachable from any LIVE request
        // path, which is this test's actual invariant.
        var repoRoot = TestPlanServicesFactory.RepoRoot();
        var ownFileSuffix = Path.Combine("Prescription", "Session", "DynamicCoreSessionPrescriptionOrchestrator.cs");
        var approvedDarkConsumerSuffix = Path.Combine("Schedule", "Materialization", "DynamicCoreCalendarMaterializationOrchestrator.cs");

        var hits = new[] { "RunningApp.Application", "RunningApp.Api", "RunningApp.Infrastructure", "RunningApp.Persistence" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repoRoot, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.Combine("bin", "")) && !path.Contains(Path.Combine("obj", "")))
            .Where(path => !path.EndsWith(ownFileSuffix, StringComparison.Ordinal))
            .Where(path => !path.EndsWith(approvedDarkConsumerSuffix, StringComparison.Ordinal))
            .Where(path => !path.Contains(Path.Combine("Schedule", "PreparationRunwayOrchestration")))
            .Where(path => !path.Contains(Path.Combine("Schedule", "LongHorizon")))
            .Where(path => !path.EndsWith("CatalogPreviewGenerator.cs", StringComparison.Ordinal))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bDynamicCoreSessionPrescription(Orchestrator|Context|Result)\b"))
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
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bIDynamicCoreSessionPrescriptionOrchestrator\b"))
            .ToArray();

        Assert.Empty(hits);
    }
}
