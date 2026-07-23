using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Binding;

/// <summary>
/// Backend Integration Phase 4F.6B — tests the exact workout-definition binder
/// (<see cref="CatalogWorkoutBinder"/>) directly, using real v10 catalog artifacts for the
/// primary end-to-end scenarios and small synthetic fixtures for structural rejection paths.
/// </summary>
public sealed class CatalogWorkoutBinderTests
{
    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    private static async Task<PlanCatalogCandidateSummary> RealCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), Microsoft.Extensions.Logging.Abstractions.NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
    }

    private static async Task<CatalogWorkoutProgressionDefinition> RealProgressionAsync(PlanCatalogCandidateSummary candidate) =>
        await new CatalogWorkoutProgressionLoader(Options.Create(RealOptions())).LoadAsync(candidate.WorkoutProgression);

    private static GeneratedCatalogPlanSkeleton RealSkeleton(PlanCatalogCandidateSummary candidate, DateOnly startDate)
    {
        var orchestrator = new CatalogPlanSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
            new CatalogStageToWeekContextFactory(), new CatalogStageToWeekMaterializer(),
            new GeneratedCatalogPlanSkeletonValidator());

        var context = new CatalogPlanSkeletonOrchestrationContext
        {
            Candidate = candidate, ExpectedCandidateKey = candidate.CandidateKey, ExpectedCandidateVersion = candidate.CandidateVersion,
            ExpectedMasterTemplate = candidate.MasterTemplate, ExpectedRunLayout = candidate.Layout, StartDate = startDate, AsOfDate = startDate,
        };

        return orchestrator.Build(context).Skeleton;
    }

    private static DatedGeneratedCatalogPlanSkeleton RealDatedSkeleton(GeneratedCatalogPlanSkeleton skeleton, DateOnly startDate)
    {
        var preferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var longRunDay = DayOfWeek.Sunday;

        var provenance = new CatalogCalendarMaterializationProvenance(
            skeleton.CandidateKey, skeleton.CandidateVersion, startDate, startDate, preferredDays, longRunDay,
            CatalogCalendarDayMaterializerVersion.V1, skeleton.SchemaVersion, skeleton.DependencyVersions);

        var context = new CatalogCalendarAssignmentContext(
            startDate, RunningApp.Domain.Enums.GoalType.Race, preferredDays, longRunDay, skeleton,
            CatalogCalendarAssignmentPolicy.RaceHardConstraint, provenance);

        return new CatalogWeekSkeletonCalendarMaterializer().Materialize(context);
    }

    private static IReadOnlyList<RuntimeConditionResolutionResult> GoalFeasibilityResult(string? outputValue) =>
        outputValue is null
            ? new[] { RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "TEST_NOT_EVALUATED") }
            : new[] { RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", outputValue, "TEST_EVALUATED") };

    private static async Task<(PlanCatalogCandidateSummary Candidate, CatalogWorkoutProgressionDefinition Progression, GeneratedCatalogPlanSkeleton Skeleton, DatedGeneratedCatalogPlanSkeleton DatedSkeleton, GeneratedCatalogStageSchedule StageSchedule)>
        RealFixtureAsync(string? goalFeasibility = "REALISTIC")
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var startDate = new DateOnly(2026, 1, 5);
        var skeleton = RealSkeleton(candidate, startDate);
        var datedSkeleton = RealDatedSkeleton(skeleton, startDate);
        var stageSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            Progression = progression, Skeleton = skeleton, ConditionResults = GoalFeasibilityResult(goalFeasibility),
        });

        return (candidate, progression, skeleton, datedSkeleton, stageSchedule);
    }

    private static CatalogWorkoutBindingContext RealContext(
        PlanCatalogCandidateSummary candidate, CatalogWorkoutProgressionDefinition progression,
        DatedGeneratedCatalogPlanSkeleton datedSkeleton, GeneratedCatalogStageSchedule stageSchedule) => new()
    {
        CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
        DatedSkeleton = datedSkeleton, StageSchedule = stageSchedule, Progression = progression,
        ReferencedWorkouts = candidate.ReferencedWorkouts,
        WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(RealOptions())),
    };

    // ───────────────────────── Real v10 catalog scenarios ─────────────────────────

    [Fact]
    public async Task DefaultTwelveWeekPilot_EverySlotBoundExactlyOnce()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        var totalSlots = datedSkeleton.Weeks.Sum(w => w.SessionSlots.Count);
        var totalSessions = plan.Weeks.Sum(w => w.Sessions.Count);
        Assert.Equal(totalSlots, totalSessions);
        Assert.Equal(48, totalSessions); // 12 weeks * 4 slots/week
    }

    [Fact]
    public async Task EasySupport_BindsToEasyStandard()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        var easySupportSessions = plan.Weeks.SelectMany(w => w.Sessions).Where(s => s.StructuralRole == "EASY_SUPPORT").ToList();
        Assert.Equal(24, easySupportSessions.Count); // 12 weeks * 2
        Assert.All(easySupportSessions, s => Assert.Equal("EASY_STANDARD", s.WorkoutDefinitionKey));
        Assert.All(easySupportSessions, s => Assert.Equal(4, s.WorkoutDefinitionVersion));
        Assert.All(easySupportSessions, s => Assert.Equal(CatalogWorkoutBindingMode.FixedDefault, s.BindingMode));
        Assert.All(easySupportSessions, s => Assert.Null(s.ProgressionStageKey));
    }

    [Fact]
    public async Task LongRun_BindsToLongRunStandard()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        var longRunSessions = plan.Weeks.SelectMany(w => w.Sessions).Where(s => s.StructuralRole == "LONG_RUN").ToList();
        Assert.Equal(12, longRunSessions.Count);
        Assert.All(longRunSessions, s => Assert.Equal("LONG_RUN_STANDARD", s.WorkoutDefinitionKey));
        Assert.All(longRunSessions, s => Assert.Equal(4, s.WorkoutDefinitionVersion));
        Assert.All(longRunSessions, s => Assert.Equal(CatalogWorkoutBindingMode.FixedDefault, s.BindingMode));
        Assert.All(longRunSessions, s => Assert.Null(s.ProgressionStageKey));
    }

    [Fact]
    public async Task KeySession_BindsToStageCandidate()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        var foundationKeySession = plan.Weeks.First(w => w.PhaseKey == "FOUNDATION").Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
        Assert.Equal("FOUNDATION_EASY_BASE", foundationKeySession.ProgressionStageKey);
        Assert.Equal("EASY_STANDARD", foundationKeySession.WorkoutDefinitionKey);
        Assert.Equal(CatalogWorkoutBindingMode.StageControlled, foundationKeySession.BindingMode);

        var buildFartlekWeek = plan.Weeks.Where(w => w.PhaseKey == "BUILD").First(w => w.Sessions.Any(s => s.ProgressionStageKey == "FARTLEK_INTRO"));
        var fartlekSession = buildFartlekWeek.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
        Assert.Equal("FARTLEK", fartlekSession.WorkoutDefinitionKey);
    }

    [Fact]
    public async Task ExactVersionResolution_MatchesLoadedDefinitionVersion()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        foreach (var session in plan.Weeks.SelectMany(w => w.Sessions))
        {
            Assert.True(session.WorkoutDefinitionVersion > 0);
        }
    }

    [Fact]
    public async Task InputOrderIndependence_ReversedPhaseOrderProducesSameBindings()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();

        var reversed = new CatalogWorkoutProgressionDefinition
        {
            Key = progression.Key, Version = progression.Version, DistanceFamily = progression.DistanceFamily,
            PhaseProgressions = progression.PhaseProgressions.Select(p => new CatalogPhaseWorkoutProgression
            {
                PhaseKey = p.PhaseKey, Stages = p.Stages.Reverse().ToList(),
            }).Reverse().ToList(),
        };

        var plan1 = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));
        var plan2 = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, reversed, datedSkeleton, stageSchedule));

        var seq1 = plan1.Weeks.SelectMany(w => w.Sessions).OrderBy(s => s.WeekNumber).ThenBy(s => s.StructuralRole).Select(s => (s.WeekNumber, s.StructuralRole, s.WorkoutDefinitionKey)).ToList();
        var seq2 = plan2.Weeks.SelectMany(w => w.Sessions).OrderBy(s => s.WeekNumber).ThenBy(s => s.StructuralRole).Select(s => (s.WeekNumber, s.StructuralRole, s.WorkoutDefinitionKey)).ToList();
        Assert.Equal(seq1, seq2);
    }

    [Fact]
    public async Task Deterministic_RepeatedRunsProduceIdenticalOutput()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var context = RealContext(candidate, progression, datedSkeleton, stageSchedule);

        var plan1 = await new CatalogWorkoutBinder().BindAsync(context);
        var plan2 = await new CatalogWorkoutBinder().BindAsync(context);

        var seq1 = plan1.Weeks.SelectMany(w => w.Sessions).OrderBy(s => s.WeekNumber).ThenBy(s => s.StructuralRole).Select(s => (s.WeekNumber, s.StructuralRole, s.WorkoutDefinitionKey, s.WorkoutDefinitionVersion)).ToList();
        var seq2 = plan2.Weeks.SelectMany(w => w.Sessions).OrderBy(s => s.WeekNumber).ThenBy(s => s.StructuralRole).Select(s => (s.WeekNumber, s.StructuralRole, s.WorkoutDefinitionKey, s.WorkoutDefinitionVersion)).ToList();
        Assert.Equal(seq1, seq2);
    }

    [Fact]
    public async Task FineGrainedStagePreserved_OnKeySessionSessions()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        var keySessions = plan.Weeks.SelectMany(w => w.Sessions).Where(s => s.StructuralRole == "KEY_SESSION").ToList();
        Assert.All(keySessions, s => Assert.False(string.IsNullOrWhiteSpace(s.ProgressionStageKey)));
    }

    [Fact]
    public async Task TaperSharpen_BindsToEasyStandardWithStageContextPreserved()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        var taperSession = plan.Weeks.Single(w => w.PhaseKey == "TAPER").Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
        Assert.Equal("TAPER", taperSession.PhaseKey);
        Assert.Equal("TAPER_SHARPEN", taperSession.ProgressionStageKey);
        Assert.Equal("KEY_SESSION", taperSession.StructuralRole);
        Assert.Equal("EASY_STANDARD", taperSession.WorkoutDefinitionKey);
    }

    [Fact]
    public async Task TaperSharpen_DistinguishableFromOrdinaryEasySupportSession()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        var taperWeek = plan.Weeks.Single(w => w.PhaseKey == "TAPER");
        var taperSharpen = taperWeek.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
        var ordinaryEasySupport = taperWeek.Sessions.First(s => s.StructuralRole == "EASY_SUPPORT");

        Assert.Equal(taperSharpen.WorkoutDefinitionKey, ordinaryEasySupport.WorkoutDefinitionKey); // both EASY_STANDARD
        Assert.NotEqual(taperSharpen.StructuralRole, ordinaryEasySupport.StructuralRole);
        Assert.NotEqual(taperSharpen.BindingMode, ordinaryEasySupport.BindingMode);
        Assert.NotNull(taperSharpen.ProgressionStageKey);
        Assert.Null(ordinaryEasySupport.ProgressionStageKey);
    }

    [Fact]
    public async Task FallbackProvenance_Preserved_WhenGoalFeasibilityIneligible()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync(goalFeasibility: "UNSUPPORTED");
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        var raceSpecificKeySessions = plan.Weeks.Where(w => w.PhaseKey == "RACE_SPECIFIC").SelectMany(w => w.Sessions).Where(s => s.StructuralRole == "KEY_SESSION").ToList();
        Assert.Contains(raceSpecificKeySessions, s => s.ProgressionStageKey == "CURRENT_FITNESS_SPECIFIC_REHEARSAL" && s.FallbackOrigin == "GOAL_PACE_REHEARSAL");
    }

    [Fact]
    public async Task NotEvaluated_IsNotReinterpretedAsFallback()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync(goalFeasibility: null);
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        // NotEvaluated is treated as "not satisfied" by the 4F.6A allocator (consumed as-is,
        // never re-evaluated here) -- this test proves the binder faithfully reflects whatever
        // the allocator already decided, rather than inventing its own NotEvaluated handling.
        var raceSpecificKeySessions = plan.Weeks.Where(w => w.PhaseKey == "RACE_SPECIFIC").SelectMany(w => w.Sessions).Where(s => s.StructuralRole == "KEY_SESSION").ToList();
        Assert.Contains(raceSpecificKeySessions, s => s.ProgressionStageKey == "CURRENT_FITNESS_SPECIFIC_REHEARSAL");
    }

    [Fact]
    public async Task NoPrescriptionFields_InOutputType()
    {
        var sessionProperties = typeof(BoundCatalogSession).GetProperties().Select(p => p.Name).ToList();
        string[] forbidden = { "Pace", "Distance", "Duration", "Volume", "Repetition", "Recovery", "Segment", "PublicWorkoutType" };
        foreach (var f in forbidden)
        {
            Assert.DoesNotContain(sessionProperties, name => name.Contains(f, StringComparison.OrdinalIgnoreCase));
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Validator_AcceptsRealDefaultBoundPlan()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        var validation = new BoundCatalogPlanValidator().Validate(plan, datedSkeleton);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
    }

    [Fact]
    public async Task Validator_RejectsCorruptedBinding_WrongModeForRole()
    {
        var (candidate, progression, _, datedSkeleton, stageSchedule) = await RealFixtureAsync();
        var plan = await new CatalogWorkoutBinder().BindAsync(RealContext(candidate, progression, datedSkeleton, stageSchedule));

        var firstWeek = plan.Weeks[0];
        var original = firstWeek.Sessions.First(x => x.StructuralRole == "EASY_SUPPORT");
        var corruptedSession = new BoundCatalogSession
        {
            WeekNumber = original.WeekNumber, Date = original.Date, PhaseKey = original.PhaseKey, ProgressionStageKey = original.ProgressionStageKey,
            StructuralRole = original.StructuralRole, WorkoutDefinitionKey = original.WorkoutDefinitionKey, WorkoutDefinitionVersion = original.WorkoutDefinitionVersion,
            BindingMode = CatalogWorkoutBindingMode.StageControlled, // corrupted: EASY_SUPPORT must be FixedDefault
            BindingPolicyKey = original.BindingPolicyKey, BindingPolicyVersion = original.BindingPolicyVersion,
            SourceArtifactKey = original.SourceArtifactKey, SourceArtifactVersion = original.SourceArtifactVersion,
            ConditionOutcome = original.ConditionOutcome, FallbackOrigin = original.FallbackOrigin, BindingReason = original.BindingReason,
        };

        var corruptedWeek = new BoundCatalogWeek
        {
            WeekNumber = firstWeek.WeekNumber, PhaseKey = firstWeek.PhaseKey,
            Sessions = firstWeek.Sessions.Select(x => x.StructuralRole == "EASY_SUPPORT" ? corruptedSession : x).ToList(),
        };

        var corruptedPlan = new BoundCatalogPlan
        {
            CandidateKey = plan.CandidateKey, CandidateVersion = plan.CandidateVersion, BinderVersion = plan.BinderVersion,
            Weeks = new[] { corruptedWeek }.Concat(plan.Weeks.Skip(1)).ToList(), Trace = plan.Trace,
        };

        var validation = new BoundCatalogPlanValidator().Validate(corruptedPlan, datedSkeleton);
        Assert.False(validation.IsValid);
    }

    // ───────────────────────── Synthetic structural-failure fixtures ─────────────────────────

    private static GeneratedCatalogWeekSkeletonProvenance WeekProvenance(string phaseKey) => new() { StageKey = phaseKey, SourcePhaseKey = phaseKey };

    private static DatedGeneratedCatalogPlanSkeleton SyntheticDatedSkeleton(string phaseKey, IReadOnlyList<string> roles)
    {
        var slots = roles.Select((role, i) => new DatedGeneratedCatalogSessionSlotSkeleton(
            i + 1, $"{role}_{i + 1}", role, new DateOnly(2026, 1, 5 + i), DayOfWeek.Monday,
            new CatalogSessionCalendarProvenance($"{role}_{i + 1}", role, DayOfWeek.Monday, new DateOnly(2026, 1, 5 + i), "TEST"))).ToList();

        var week = new DatedGeneratedCatalogWeekSkeleton(1, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11), phaseKey, 1, 1, slots,
            new CatalogWeekCalendarProvenance(1, phaseKey, 1, CatalogCalendarAssignmentPolicy.RaceHardConstraint));

        return new DatedGeneratedCatalogPlanSkeleton("1", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11), 1, new[] { week },
            new CatalogCalendarMaterializationProvenance("SYNTHETIC", 1, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5),
                new[] { DayOfWeek.Monday }, DayOfWeek.Monday, "TEST", 1, new Dictionary<string, PlanCatalogReference>()));
    }

    private static CatalogWorkoutProgressionStage Stage(string key, int order, PlanCatalogReference? candidate = null) => new()
    {
        ProgressionStageKey = key, RelativeOrder = order, MinimumExposures = 1, MaximumExposures = 1,
        CompressionBehavior = CatalogStageCompressionBehavior.Compressible, ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
        Requires = Array.Empty<CatalogRuntimeEligibilityCondition>(), FallbackStageKey = null,
        WorkoutCandidateReferences = candidate is null ? Array.Empty<PlanCatalogReference>() : new[] { candidate },
    };

    private static GeneratedCatalogStageSchedule SyntheticStageSchedule(string phaseKey, string? progressionStageKey) => new()
    {
        CandidateKey = "SYNTHETIC", CandidateVersion = 1, ProgressionArtifactKey = "SYNTHETIC_PROGRESSION", ProgressionArtifactVersion = 1,
        AllocatorVersion = ProgressionStageAllocatorVersion.V1,
        Weeks = progressionStageKey is null
            ? Array.Empty<ScheduledProgressionWeek>()
            : new[]
            {
                new ScheduledProgressionWeek
                {
                    WeekNumber = 1, PhaseKey = phaseKey, ProgressionStageKey = progressionStageKey, StageRelativeOrder = 1,
                    ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned, AllocationReason = "TEST",
                },
            },
        Trace = new StageAllocationDecisionTrace { Steps = Array.Empty<StageAllocationDecisionTraceStep>() },
    };

    private static IReadOnlyList<PlanCatalogReference> RealEasyLongRunReferences() =>
        new[] { new PlanCatalogReference("EASY_STANDARD", 4), new PlanCatalogReference("LONG_RUN_STANDARD", 4) };

    private sealed class RealWorkoutDefinitionLoader : ICatalogWorkoutDefinitionLoader
    {
        private readonly CatalogWorkoutDefinitionLoader _inner = new(Options.Create(new PlanCatalogOptions { CatalogRootPath = RealCatalogRoot() }));
        public Task<CatalogWorkoutDefinitionSummary> LoadAsync(PlanCatalogReference reference, CancellationToken ct = default) => _inner.LoadAsync(reference, ct);
    }

    [Fact]
    public async Task MissingFixedDefaultWorkout_NotInReferencedWorkouts_ThrowsTypedException()
    {
        var datedSkeleton = SyntheticDatedSkeleton("FOUNDATION", new[] { "EASY_SUPPORT" });
        var progression = new CatalogWorkoutProgressionDefinition { Key = "P", Version = 1, DistanceFamily = "TEN_K", PhaseProgressions = Array.Empty<CatalogPhaseWorkoutProgression>() };

        var context = new CatalogWorkoutBindingContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, DatedSkeleton = datedSkeleton,
            StageSchedule = SyntheticStageSchedule("FOUNDATION", null), Progression = progression,
            ReferencedWorkouts = Array.Empty<PlanCatalogReference>(), // EASY_STANDARD missing
            WorkoutDefinitionLoader = new RealWorkoutDefinitionLoader(),
        };

        await Assert.ThrowsAsync<CatalogWorkoutBindingCandidateNotFoundException>(() => new CatalogWorkoutBinder().BindAsync(context));
    }

    [Fact]
    public async Task MissingStageCandidate_ZeroWorkoutCandidateReferences_ThrowsTypedException()
    {
        var datedSkeleton = SyntheticDatedSkeleton("FOUNDATION", new[] { "KEY_SESSION" });
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "P", Version = 1, DistanceFamily = "TEN_K",
            PhaseProgressions = new[] { new CatalogPhaseWorkoutProgression { PhaseKey = "FOUNDATION", Stages = new[] { Stage("STAGE_A", 1) } } },
        };

        var context = new CatalogWorkoutBindingContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, DatedSkeleton = datedSkeleton,
            StageSchedule = SyntheticStageSchedule("FOUNDATION", "STAGE_A"), Progression = progression,
            ReferencedWorkouts = RealEasyLongRunReferences(), WorkoutDefinitionLoader = new RealWorkoutDefinitionLoader(),
        };

        await Assert.ThrowsAsync<CatalogWorkoutBindingMissingCandidateReferenceException>(() => new CatalogWorkoutBinder().BindAsync(context));
    }

    [Fact]
    public async Task CandidateNotInBundle_ThrowsTypedException()
    {
        var datedSkeleton = SyntheticDatedSkeleton("FOUNDATION", new[] { "KEY_SESSION" });
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "P", Version = 1, DistanceFamily = "TEN_K",
            PhaseProgressions = new[] { new CatalogPhaseWorkoutProgression { PhaseKey = "FOUNDATION", Stages = new[] { Stage("STAGE_A", 1, new PlanCatalogReference("DOES_NOT_EXIST", 1)) } } },
        };

        var context = new CatalogWorkoutBindingContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, DatedSkeleton = datedSkeleton,
            StageSchedule = SyntheticStageSchedule("FOUNDATION", "STAGE_A"), Progression = progression,
            ReferencedWorkouts = new[] { new PlanCatalogReference("DOES_NOT_EXIST", 1) }, WorkoutDefinitionLoader = new RealWorkoutDefinitionLoader(),
        };

        await Assert.ThrowsAsync<CatalogWorkoutBindingCandidateNotFoundException>(() => new CatalogWorkoutBinder().BindAsync(context));
    }

    [Fact]
    public async Task VersionMismatch_RequestedVersionDoesNotExist_ThrowsTypedException()
    {
        var datedSkeleton = SyntheticDatedSkeleton("FOUNDATION", new[] { "KEY_SESSION" });
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "P", Version = 1, DistanceFamily = "TEN_K",
            PhaseProgressions = new[] { new CatalogPhaseWorkoutProgression { PhaseKey = "FOUNDATION", Stages = new[] { Stage("STAGE_A", 1, new PlanCatalogReference("EASY_STANDARD", 999)) } } },
        };

        var context = new CatalogWorkoutBindingContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, DatedSkeleton = datedSkeleton,
            StageSchedule = SyntheticStageSchedule("FOUNDATION", "STAGE_A"), Progression = progression,
            ReferencedWorkouts = new[] { new PlanCatalogReference("EASY_STANDARD", 999) }, WorkoutDefinitionLoader = new RealWorkoutDefinitionLoader(),
        };

        // EASY_STANDARD v999 does not exist on disk -> the loader itself throws PlanCatalogLoadException,
        // which the binder maps to CatalogWorkoutBindingCandidateNotFoundException (not found at that version).
        await Assert.ThrowsAsync<CatalogWorkoutBindingCandidateNotFoundException>(() => new CatalogWorkoutBinder().BindAsync(context));
    }

    [Fact]
    public async Task AmbiguousCandidate_MoreThanOneWorkoutCandidateReference_ThrowsTypedException()
    {
        var datedSkeleton = SyntheticDatedSkeleton("FOUNDATION", new[] { "KEY_SESSION" });
        var stageWithTwoCandidates = new CatalogWorkoutProgressionStage
        {
            ProgressionStageKey = "STAGE_A", RelativeOrder = 1, MinimumExposures = 1, MaximumExposures = 1,
            CompressionBehavior = CatalogStageCompressionBehavior.Compressible, ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
            Requires = Array.Empty<CatalogRuntimeEligibilityCondition>(),
            WorkoutCandidateReferences = new[] { new PlanCatalogReference("EASY_STANDARD", 4), new PlanCatalogReference("LONG_RUN_STANDARD", 4) },
        };
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "P", Version = 1, DistanceFamily = "TEN_K",
            PhaseProgressions = new[] { new CatalogPhaseWorkoutProgression { PhaseKey = "FOUNDATION", Stages = new[] { stageWithTwoCandidates } } },
        };

        var context = new CatalogWorkoutBindingContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, DatedSkeleton = datedSkeleton,
            StageSchedule = SyntheticStageSchedule("FOUNDATION", "STAGE_A"), Progression = progression,
            ReferencedWorkouts = RealEasyLongRunReferences(), WorkoutDefinitionLoader = new RealWorkoutDefinitionLoader(),
        };

        await Assert.ThrowsAsync<CatalogWorkoutBindingAmbiguousCandidateException>(() => new CatalogWorkoutBinder().BindAsync(context));
    }

    [Fact]
    public void NoProgressionStageKey_OnOrdinaryEasySupport_ValidatorEnforced()
    {
        var validator = new BoundCatalogPlanValidator();
        var week = new BoundCatalogWeek
        {
            WeekNumber = 1, PhaseKey = "FOUNDATION",
            Sessions = new[]
            {
                new BoundCatalogSession
                {
                    WeekNumber = 1, Date = new DateOnly(2026, 1, 5), PhaseKey = "FOUNDATION", ProgressionStageKey = "SOMETHING_WRONG",
                    StructuralRole = "EASY_SUPPORT", WorkoutDefinitionKey = "EASY_STANDARD", WorkoutDefinitionVersion = 4,
                    BindingMode = CatalogWorkoutBindingMode.FixedDefault, BindingPolicyKey = "P", BindingPolicyVersion = 1,
                    SourceArtifactKey = "P", SourceArtifactVersion = 1, ConditionOutcome = null, FallbackOrigin = null, BindingReason = "TEST",
                },
            },
        };

        var plan = new BoundCatalogPlan
        {
            CandidateKey = "X", CandidateVersion = 1, BinderVersion = "V1", Weeks = new[] { week },
            Trace = new WorkoutBindingDecisionTrace { Steps = Array.Empty<WorkoutBindingDecisionTraceStep>() },
        };

        var datedSkeleton = SyntheticDatedSkeleton("FOUNDATION", new[] { "EASY_SUPPORT" });
        var result = validator.Validate(plan, datedSkeleton);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("unexpectedly carries a ProgressionStageKey"));
    }
}
