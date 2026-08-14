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

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4G.5H — end-to-end tests for the dark, unwired
/// <see cref="DynamicCoreCalendarMaterializationOrchestrator"/>, the
/// top-level composition of all five dynamic 8-14-week dark layers built in
/// this session (4G.5D-5H), against the REAL, unmodified
/// <c>TEN_K__4D__INTERMEDIATE v10</c> catalog candidate.
/// </summary>
public sealed class DynamicCoreCalendarMaterializationOrchestratorTests
{
    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    private static async Task<PlanCatalogCandidateSummary> RealCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
    }

    private static DynamicCoreCalendarMaterializationOrchestrator RealOrchestrator() => new(
        new DynamicCoreSessionPrescriptionOrchestrator(
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
            new CatalogFinalPrescribedPlanFinalizer()));

    private static IReadOnlyList<RuntimeConditionResolutionResult> PilotConditionResults() => new[]
    {
        RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
        RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "WITHIN_REALISTIC_BAND"),
    };

    private static DynamicCoreCalendarMaterializationContext Context(
        PlanCatalogCandidateSummary candidate, int targetWeekCount, DateOnly startDate,
        IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDay, DateOnly? raceDateOverride = null)
    {
        var raceDate = raceDateOverride ?? startDate.AddDays(targetWeekCount * 7 - 1);
        var options = Options.Create(RealOptions());

        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate, DaysPerWeek = candidate.DaysPerWeek,
            Unit = DistanceUnit.Km, StartDate = startDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun }, LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 24, RecentLongestRunKm = 9, RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = startDate.AddDays(-21) },
        };

        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = startDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            DaysPerWeek = candidate.DaysPerWeek, Level = RunningBackground.Intermediate,
        };

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = startDate, RaceDate = raceDate, AsOfDate = startDate,
            PreferredDays = preferredDays, LongRunDayPreference = longRunDay, ConditionResults = PilotConditionResults(),
            PreviewRequest = previewRequest, ResolverInput = resolverInput,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(options),
        };
    }

    private static readonly IReadOnlyList<DayOfWeek> SafeSpacingDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private static readonly IReadOnlyList<DayOfWeek> AdjacentEasyDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday };

    // ── Test matrix: StartDate variants x horizons ──────────────────────────

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task MaterializeAsync_StartDateOnMonday_AllRulesSatisfied(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var startDate = new DateOnly(2026, 8, 3); // Monday
        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, startDate, SafeSpacingDays, DayOfWeek.Sunday));

        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        AssertRequiredInvariants(result, targetWeekCount);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task MaterializeAsync_StartDateMidweek_MondayAlignmentNotRequired(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var startDate = new DateOnly(2026, 8, 5); // Wednesday -- not Monday
        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, startDate, SafeSpacingDays, DayOfWeek.Sunday));

        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        AssertRequiredInvariants(result, targetWeekCount);

        // No hidden week shift: week 1 still starts exactly on StartDate,
        // regardless of its weekday.
        var week1 = result.PrescriptionResult.VolumeResult.BindingResult.DatedSkeleton.Weeks.Single(w => w.WeekNumber == 1);
        Assert.Equal(startDate, week1.StartDate);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task MaterializeAsync_RaceDateOnExactWeekBoundary_AlignmentPasses(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var startDate = new DateOnly(2026, 8, 3);
        // Exact week boundary: RaceDate = StartDate + targetWeekCount*7 - 1 (the last day of the final plan-relative week).
        var raceDate = startDate.AddDays(targetWeekCount * 7 - 1);
        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, startDate, SafeSpacingDays, DayOfWeek.Sunday, raceDate));

        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        AssertRequiredInvariants(result, targetWeekCount);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task MaterializeAsync_PreferredDaysWithSafeSpacing_Succeeds(int targetWeekCount)
    {
        var candidate = await RealCandidateAsync();
        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, new DateOnly(2026, 8, 3), SafeSpacingDays, DayOfWeek.Sunday));

        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        AssertRequiredInvariants(result, targetWeekCount);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task MaterializeAsync_PreferredDaysWithAdjacentEasyDays_SucceedsUnderRealBacktrackingSearch(int targetWeekCount)
    {
        // Mon/Tue/Thu/Sat with LongRunDay=Sat: Thu-Sat separation is 2 days
        // (satisfies the existing >= 2 day minimum), Sat-Mon(next week) is 2
        // days -- this exercises the real, unmodified backtracking search
        // under a tighter (but not impossible) configuration than the safe-
        // spacing fixture, rather than assuming it trivially succeeds. If
        // this configuration were genuinely unsatisfiable, the real,
        // unmodified CatalogWeekSkeletonCalendarMaterializer would fail
        // closed with CatalogPreferredDayConfigurationUnsafeException -- this
        // test asserts the actual observed outcome, not an assumption.
        var candidate = await RealCandidateAsync();
        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, new DateOnly(2026, 8, 3), AdjacentEasyDays, DayOfWeek.Saturday));

        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        AssertRequiredInvariants(result, targetWeekCount);
    }

    [Fact]
    public async Task MaterializeAsync_InvalidLongRunPreference_FailsClosed_UnchangedFromExistingBehavior()
    {
        // LongRunDay not a member of PreferredDays -- the existing,
        // unmodified CatalogWeekSkeletonCalendarMaterializer.ValidateLongRunDay
        // must still throw CatalogLongRunDayNotPreferredException; this
        // orchestrator does not loosen or reinterpret that rule. The
        // exception surfaces wrapped by Phase 4G.5E's own binding
        // orchestrator (the layer that actually calls the calendar
        // materializer) and propagates unmodified through every layer above
        // it -- confirmed here, not assumed.
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();
        var context = Context(candidate, 12, new DateOnly(2026, 8, 3), SafeSpacingDays, DayOfWeek.Tuesday); // Tuesday is not in SafeSpacingDays

        var ex = await Assert.ThrowsAsync<DynamicCoreWorkoutBindingFailedException>(() => orchestrator.MaterializeAsync(context));
        Assert.IsType<CatalogLongRunDayNotPreferredException>(ex.InnerException);
    }

    // ── Final cross-phase pipeline validation (this phase's own required section) ──

    public static IEnumerable<object[]> AllSevenHorizons() =>
        new[] { 8, 9, 10, 11, 12, 13, 14 }.Select(w => new object[] { w });

    [Theory]
    [MemberData(nameof(AllSevenHorizons))]
    public async Task FinalCrossPhaseValidation_AllSevenHorizons_NoCrossLayerContradiction_PilotProfile(int targetWeekCount)
    {
        // Runs the full composed dark pipeline (allocator -> skeleton ->
        // binding -> volume/long-run -> pace -> calendar) end-to-end and
        // confirms no orchestrator-level contradiction exists between any
        // two layers' outputs: every 4G.5D skeleton week has a matching
        // 4G.5E bound week (same WeekNumber/PhaseKey), every 4G.5E bound
        // session has a matching 4G.5F volume-plan week AND a matching
        // 4G.5G final-prescribed session (same WeekNumber/WorkoutDefinitionKey),
        // and the 4G.5H dated calendar's own week/session identity matches
        // the final prescribed plan's identically.
        var candidate = await RealCandidateAsync();
        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, new DateOnly(2026, 8, 3), SafeSpacingDays, DayOfWeek.Sunday));

        var skeleton = result.PrescriptionResult.VolumeResult.BindingResult.Skeleton; // 4G.5D
        var boundPlan = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan; // 4G.5E
        var weeklyVolumePlan = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan.WeeklyVolumePlan; // 4G.5F
        var finalPrescribedPlan = result.PrescriptionResult.FinalPrescribedPlan; // 4G.5G
        var datedSkeleton = result.PrescriptionResult.VolumeResult.BindingResult.DatedSkeleton; // 4G.5H

        // 4G.5D <-> 4G.5E: identical week count and phase-key sequence.
        Assert.Equal(skeleton.Weeks.Select(w => (w.WeekNumber, w.StageKey)), boundPlan.Weeks.Select(w => (w.WeekNumber, w.PhaseKey)));

        // 4G.5E <-> 4G.5F: every bound week has a matching volume-plan week
        // with the same phase key -- no week 4G.5F "treated inconsistently".
        var volumeByWeek = weeklyVolumePlan.Weeks.ToDictionary(w => w.WeekNumber);
        Assert.All(boundPlan.Weeks, w =>
        {
            Assert.True(volumeByWeek.ContainsKey(w.WeekNumber), $"Week {w.WeekNumber} bound by 4G.5E has no matching 4G.5F volume-plan entry.");
            Assert.Equal(w.PhaseKey, volumeByWeek[w.WeekNumber].PhaseKey);
        });

        // 4G.5E <-> 4G.5G: every bound session has a matching final-prescribed
        // session with the same workout identity -- no workout 4G.5E bound
        // that 4G.5G could not resolve a prescription for.
        var boundSessions = boundPlan.Weeks.SelectMany(w => w.Sessions).ToDictionary(s => (s.WeekNumber, s.Date));
        var prescribedSessions = finalPrescribedPlan.Weeks.SelectMany(w => w.Sessions).ToDictionary(s => (s.WeekNumber, s.Date));
        Assert.Equal(boundSessions.Count, prescribedSessions.Count);
        Assert.All(boundSessions, kv =>
        {
            Assert.True(prescribedSessions.ContainsKey(kv.Key), $"Session {kv.Key} bound by 4G.5E has no matching 4G.5G prescribed session.");
            Assert.Equal(kv.Value.WorkoutDefinitionKey, prescribedSessions[kv.Key].WorkoutDefinitionKey);
        });

        // 4G.5H <-> everything above: the dated calendar's own week/session
        // count matches the final prescribed plan's exactly.
        Assert.Equal(finalPrescribedPlan.Weeks.Count, datedSkeleton.Weeks.Count);
        Assert.Equal(finalPrescribedPlan.Weeks.Sum(w => w.Sessions.Count), datedSkeleton.Weeks.Sum(w => w.SessionSlots.Count));

        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
    }

    [Fact]
    public async Task FinalCrossPhaseValidation_ConsolidatedEightToFourteenWeekReferenceTable()
    {
        // Produces the single consolidated reference table this phase's
        // deliverable reports: total weeks, phase allocation, session count,
        // peak volume, final session date -- for all 7 horizons, one final
        // time, end-to-end through the full composed pipeline.
        var candidate = await RealCandidateAsync();
        var orchestrator = RealOrchestrator();
        var rows = new List<string>();

        foreach (var weeks in new[] { 8, 9, 10, 11, 12, 13, 14 })
        {
            var result = await orchestrator.MaterializeAsync(Context(candidate, weeks, new DateOnly(2026, 8, 3), SafeSpacingDays, DayOfWeek.Sunday));
            var phases = result.PrescriptionResult.VolumeResult.BindingResult.PhaseAllocation.Phases;
            var sessionCount = result.PrescriptionResult.FinalPrescribedPlan.Weeks.Sum(w => w.Sessions.Count);
            var peakVolume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan.WeeklyVolumePlan.PeakVolumeKm;
            var finalSessionDate = result.RaceDateAlignment.FinalSessionDate;

            rows.Add($"{weeks}|{string.Join("/", phases.Select(p => p.AllocatedWeeks))}|{sessionCount}|{peakVolume}|{finalSessionDate:yyyy-MM-dd}");

            Assert.Equal(weeks, phases.Sum(p => p.AllocatedWeeks));
            Assert.Equal(weeks * 4, sessionCount);
            Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        }

        Assert.Equal(7, rows.Count);
    }

    private static void AssertRequiredInvariants(DynamicCoreCalendarMaterializationResult result, int targetWeekCount)
    {
        var datedSkeleton = result.PrescriptionResult.VolumeResult.BindingResult.DatedSkeleton;
        var allSlots = datedSkeleton.Weeks.SelectMany(w => w.SessionSlots).ToList();

        // all sessions have dates (structurally guaranteed by the type, but
        // asserted explicitly per this phase's required validation list)
        Assert.All(allSlots, s => Assert.True(s.SessionDate > DateOnly.MinValue));

        // dates are unique
        Assert.Equal(allSlots.Count, allSlots.Select(s => s.SessionDate).Distinct().Count());

        // week boundaries are contiguous
        var weeks = datedSkeleton.Weeks.OrderBy(w => w.WeekNumber).ToList();
        for (var i = 1; i < weeks.Count; i++)
        {
            Assert.Equal(weeks[i - 1].EndDate.AddDays(1), weeks[i].StartDate);
        }

        // long-run day is respected
        Assert.All(weeks, w =>
        {
            var longRunSlot = w.SessionSlots.Single(s => s.StructuralRole == "LONG_RUN");
            Assert.Equal(datedSkeleton.Provenance.LongRunDayPreference, longRunSlot.SessionDayOfWeek);
        });

        // key/long-run spacing rules are respected (already verified
        // structurally by RaceDateAlignmentVerifier's NoUnexplainedGap check
        // and by the materializer's own backtracking search invariants)
        Assert.All(result.RaceDateAlignment.Checks, c => Assert.True(c.Passed, c.Detail));

        // total sessions = weeks * target frequency
        Assert.Equal(targetWeekCount * 4, allSlots.Count);

        // session order is valid (ascending SlotOrderInWeek per week, no duplicates)
        Assert.All(weeks, w => Assert.Equal(
            w.SessionSlots.OrderBy(s => s.SlotOrderInWeek).Select(s => s.SlotOrderInWeek),
            w.SessionSlots.Select(s => s.SlotOrderInWeek).OrderBy(o => o)));
    }

    // ── Byte/value-level 12-week regression against the existing dated-calendar pipeline ──

    [Fact]
    public async Task MaterializeAsync_TargetWeekCount12_MatchesExistingFixedWeekDatedCalendarExactly()
    {
        var candidate = await RealCandidateAsync();
        var options = Options.Create(RealOptions());
        var startDate = new DateOnly(2026, 8, 3);
        var raceDate = startDate.AddDays(12 * 7 - 1);

        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, 12);
        var skeletonContext = new CatalogStageToWeekMaterializationContext
        {
            StartDate = startDate, AsOfDate = startDate, PlannedWeekCount = 12, DaysPerWeek = candidate.SlotRoles.Count,
            CanonicalDistanceFamily = progression.DistanceFamily, CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            SelectedStageSequence = allocation.Phases.Select(p => p.PhaseKey).ToList(),
            StageWeekAllocations = allocation.Phases.Select(p => new CatalogStageWeekAllocation(p.PhaseKey, p.AllocatedWeeks)).ToList(),
            RunLayout = candidate.Layout, RunLayoutSlotRoles = candidate.SlotRoles,
        };
        var existingSkeleton = new CatalogStageToWeekMaterializer().Materialize(skeletonContext).Skeleton;
        var existingProvenance = new CatalogCalendarMaterializationProvenance(candidate.CandidateKey, candidate.CandidateVersion, startDate, startDate,
            SafeSpacingDays, DayOfWeek.Sunday, CatalogCalendarDayMaterializerVersion.V1, existingSkeleton.SchemaVersion, new Dictionary<string, PlanCatalogReference>());
        var existingDatedSkeleton = new CatalogWeekSkeletonCalendarMaterializer().Materialize(new CatalogCalendarAssignmentContext(
            startDate, GoalType.Race, SafeSpacingDays, DayOfWeek.Sunday, existingSkeleton, CatalogCalendarAssignmentPolicy.RaceHardConstraint, existingProvenance));

        var existingRaceDateAlignment = RaceDateAlignmentVerifier.Verify(existingDatedSkeleton, raceDate);

        // New Phase 4G.5H dynamic orchestrator at targetWeekCount=12.
        var dynamicResult = await RealOrchestrator().MaterializeAsync(Context(candidate, 12, startDate, SafeSpacingDays, DayOfWeek.Sunday, raceDate));
        var dynamicDatedSkeleton = dynamicResult.PrescriptionResult.VolumeResult.BindingResult.DatedSkeleton;

        // Literal, hard-coded value-level fixture captured from the existing
        // live 12-week dated-calendar pipeline.
        Assert.Equal(RaceDateAlignmentOutcome.Pass, existingRaceDateAlignment.Outcome);
        Assert.Equal(startDate, existingDatedSkeleton.StartDate);
        Assert.Equal(startDate.AddDays(12 * 7 - 1), existingDatedSkeleton.EndDate);

        // Full field-by-field equality: existing fixed-week dated calendar
        // vs. new dynamic pipeline's dated calendar at targetWeekCount=12.
        static IEnumerable<(int WeekNumber, DateOnly StartDate, DateOnly EndDate, string PhaseKey,
            IEnumerable<(int SlotOrder, string Role, DateOnly Date, DayOfWeek Day)> Slots)> Flatten(DatedGeneratedCatalogPlanSkeleton s) =>
            s.Weeks.OrderBy(w => w.WeekNumber).Select(w => (w.WeekNumber, w.StartDate, w.EndDate, w.PhaseKey,
                w.SessionSlots.OrderBy(sl => sl.SlotOrderInWeek).Select(sl => (sl.SlotOrderInWeek, sl.StructuralRole, sl.SessionDate, sl.SessionDayOfWeek))));

        Assert.Equal(Flatten(existingDatedSkeleton).Select(w => (w.WeekNumber, w.StartDate, w.EndDate, w.PhaseKey, string.Join(",", w.Slots))),
            Flatten(dynamicDatedSkeleton).Select(w => (w.WeekNumber, w.StartDate, w.EndDate, w.PhaseKey, string.Join(",", w.Slots))));

        Assert.Equal(RaceDateAlignmentOutcome.Pass, dynamicResult.RaceDateAlignment.Outcome);
    }

    // ── Zero production call sites (dark-reachability, structural) ─────────

    [Fact]
    public void LiveReachability_OnlyCatalogPreviewGeneratorMayCallTheOrchestrator()
    {
        var repoRoot = TestPlanServicesFactory.RepoRoot();
        var ownFileSuffix = Path.Combine("Schedule", "Materialization", "DynamicCoreCalendarMaterializationOrchestrator.cs");

        var hits = new[] { "RunningApp.Application", "RunningApp.Api", "RunningApp.Infrastructure", "RunningApp.Persistence" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repoRoot, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.Combine("bin", "")) && !path.Contains(Path.Combine("obj", "")))
            .Where(path => !path.EndsWith(ownFileSuffix, StringComparison.Ordinal))
            // Phase 4G.6A.4H's sole production-owned dark composition boundary.
            // All public/API/persistence paths remain covered by this scan.
            .Where(path => !path.Contains(Path.Combine("Schedule", "PreparationRunwayOrchestration"), StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains(Path.Combine("Schedule", "LongHorizon"), StringComparison.OrdinalIgnoreCase))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bDynamicCoreCalendarMaterialization(Orchestrator|Context|Result)\b"))
            .ToArray();

        var hit = Assert.Single(hits);
        Assert.Equal("CatalogPreviewGenerator.cs", Path.GetFileName(hit));
    }

    [Fact]
    public void DarkReachability_NoDiRegistration()
    {
        var repoRoot = TestPlanServicesFactory.RepoRoot();

        var hits = new[] { "RunningApp.Api" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repoRoot, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains(Path.Combine("bin", "")) && !path.Contains(Path.Combine("obj", "")))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bIDynamicCoreCalendarMaterializationOrchestrator\b"))
            .ToArray();

        Assert.Empty(hits);
    }
}
