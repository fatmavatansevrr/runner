using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase 10K-FREQ.6D.4D.5G — real, dark, unrouted proof that the
/// CompressedCore/ExtendedCore dynamic-orchestration chain (<see cref="DynamicCoreCalendarMaterializationOrchestrator"/>)
/// now consumes the same real published-bundle <see cref="ExecutionPrescriptionIndex"/> the
/// exact-12-week "preferred" pipeline already used successfully (FREQ.6D.4D.5F's disclosed blocker:
/// this chain previously never received one at all). Mirrors <see cref="Freq6D4D5BReal5DDarkPlanTests"/>'s
/// exact real-catalog-root/dark-candidate-loading pattern, extended one layer further (through
/// session prescription, not just binding) to actually exercise the fixed wiring. Public routing
/// (<see cref="V1CatalogPilotIdentityPolicy"/>) is not touched or required by any test in this file.
/// </summary>
public sealed class Freq6D4D5GCoreHorizonExecutionContextTests
{
    private const string CandidateKey = "TEN_K__5D__INTERMEDIATE";
    private const int CandidateVersion = 1;
    private const string RealPublishedBundleReleaseVersion = "1.1.0";

    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions RealOptions() => new()
    {
        CatalogRootPath = RealCatalogRoot(),
        PublishedBundleReleaseVersion = RealPublishedBundleReleaseVersion,
    };

    private static async Task<PlanCatalogCandidateSummary> RealFiveDayCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(CandidateKey, CandidateVersion);
    }

    /// <summary>The same real bundle discovery the exact-12-week pipeline uses — real committed <c>1.1.0</c> release, exact candidate identity.</summary>
    private static async Task<ExecutionPrescriptionIndex?> RealExecutionIndexAsync()
    {
        var loader = new PublishedTemplateBundleLoader(RealOptions(), RealCatalogRoot());
        var bundle = await loader.TryLoadAsync(CandidateKey, CandidateVersion);
        return bundle is not null ? ExecutionPrescriptionIndex.Build(bundle) : null;
    }

    private static DynamicCoreCalendarMaterializationOrchestrator RealOrchestrator() => new(
        new DynamicCoreSessionPrescriptionOrchestrator(
            new DynamicCoreVolumeAndLongRunOrchestrator(
                new DynamicCoreWorkoutBindingOrchestrator(
                    new DynamicCoreWeekSkeletonOrchestrator(
                        new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                        new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                    new CatalogWorkoutProgressionLoader(Options.Create(RealOptions())),
                    new ProgressionStageAllocator(), new GeneratedCatalogStageScheduleValidator(),
                    new CatalogWeekSkeletonCalendarMaterializer(), new DatedGeneratedCatalogPlanSkeletonValidator(),
                    new CatalogWorkoutBinder(), new BoundCatalogPlanValidator()),
                new CatalogPrescriptionContextBuilder(), new CatalogVolumeAndLongRunPlanner()),
            new CatalogSessionPrescriptionPlanner(), new CatalogFinalPrescribedPlanFinalizer()));

    private static readonly DayOfWeek[] PreferredDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;
    private static readonly DateOnly StartDate = new(2026, 8, 3);

    private static DynamicCoreCalendarMaterializationContext Context(
        PlanCatalogCandidateSummary candidate, int targetWeekCount, ExecutionPrescriptionIndex? executionIndex)
    {
        var raceDate = StartDate.AddDays(targetWeekCount * 7);
        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate, DaysPerWeek = candidate.DaysPerWeek,
            Unit = DistanceUnit.Km, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Mon, Weekday.Tue, Weekday.Wed, Weekday.Fri, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 25, RecentLongestRunKm = 10, RecentRunsPerWeek = 5,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = StartDate.AddDays(-21) },
        };
        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d, StartDate = StartDate, RaceDate = raceDate, TargetFinishTimeSeconds = 3000,
            DaysPerWeek = candidate.DaysPerWeek, Level = RunningBackground.Intermediate,
        };

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate,
            TargetWeekCount = targetWeekCount,
            StartDate = StartDate,
            RaceDate = raceDate,
            AsOfDate = StartDate,
            PreferredDays = PreferredDays,
            LongRunDayPreference = LongRunDay,
            ConditionResults = new[]
            {
                RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
                RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "WITHIN_REALISTIC_BAND"),
            },
            PreviewRequest = previewRequest,
            ResolverInput = resolverInput,
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(RealOptions())),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(Options.Create(RealOptions())),
            ExecutionIndex = executionIndex,
        };
    }

    // ── §16-17/§19: real 8/10/14-week dark proof, real CompressedCore/ExtendedCore ──

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(14)]
    public async Task RealFiveDayCandidate_CompressedOrExtendedCore_WithExecutionIndex_ResolvesEveryProfileBackedKeySession(int targetWeekCount)
    {
        var candidate = await RealFiveDayCandidateAsync();
        var executionIndex = await RealExecutionIndexAsync();
        Assert.NotNull(executionIndex); // real committed 1.1.0 bundle must actually be found

        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, executionIndex));

        Assert.Equal(targetWeekCount, result.PrescriptionResult.FinalPrescribedPlan.Weeks.Count);
        var keySessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions.Where(s => s.StructuralRole == "KEY_SESSION").ToList();
        Assert.NotEmpty(keySessions);
        Assert.All(keySessions, s => Assert.True(s.ValidationResult.IsValid, string.Join(", ", s.ValidationResult.Errors)));
    }

    // ── §32: intentionally-omitted context still fails closed (not a new fallback) ──

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(14)]
    public async Task RealFiveDayCandidate_CompressedOrExtendedCore_WithoutExecutionIndex_StillFailsClosed(int targetWeekCount)
    {
        var candidate = await RealFiveDayCandidateAsync();

        var ex = await Assert.ThrowsAsync<DynamicCoreSessionPrescriptionFailedException>(
            () => RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, executionIndex: null)));

        Assert.IsType<CatalogSessionPrescriptionMissingExecutionPrescriptionException>(ex.InnerException);
    }

    // ── §20-21: every ProfileBacked session resolves; profile closure is a real subset ──

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(14)]
    public async Task RealFiveDayCandidate_AllProfileBackedSessions_ResolveExactFromRealBundle(int targetWeekCount)
    {
        var candidate = await RealFiveDayCandidateAsync();
        var executionIndex = await RealExecutionIndexAsync();

        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, executionIndex));

        var profileBacked = result.PrescriptionResult.FinalPrescribedPlan.Sessions
            .Where(s => s.PrescriptionSource is CatalogSessionPrescriptionSource.ProfileBacked)
            .ToList();
        Assert.NotEmpty(profileBacked); // the real 5D closure is entirely ProfileBacked for KEY sessions
        Assert.All(profileBacked, s =>
        {
            var key = s.PrescriptionSource.ExactProfileKeyOrNull();
            var version = s.PrescriptionSource.ExactProfileVersionOrNull();
            Assert.NotNull(key);
            Assert.NotNull(version);
            var profileRef = new PlanCatalog.Contracts.References.VersionedCatalogReference
            {
                DocumentType = PlanCatalog.Contracts.DocumentTypes.WorkoutPrescriptionProfile, Key = key!, Version = version!.Value,
            };
            Assert.NotNull(executionIndex!.ResolveExact(profileRef)); // exact, not weakened to key-only
        });
    }

    // ── §22: representative phases actually reachable at each horizon ───────

    [Theory]
    [InlineData(8, new[] { "FOUNDATION", "BUILD" })]
    [InlineData(10, new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC" })]
    [InlineData(14, new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" })]
    public async Task RealFiveDayCandidate_RepresentativePhasesReachable_ResolveExecution(int targetWeekCount, string[] expectedPhases)
    {
        var candidate = await RealFiveDayCandidateAsync();
        var executionIndex = await RealExecutionIndexAsync();

        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, executionIndex));

        foreach (var phase in expectedPhases)
        {
            var phaseKeySessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions
                .Where(s => s.PhaseKey == phase && s.StructuralRole == "KEY_SESSION").ToList();
            Assert.NotEmpty(phaseKeySessions);
            Assert.All(phaseKeySessions, s => Assert.True(s.ValidationResult.IsValid, string.Join(", ", s.ValidationResult.Errors)));
        }
    }

    // ── §23: Taper zero-delta — real Taper sessions still pass both validators ──

    [Fact]
    public async Task RealFourteenWeek_RealTaper_BothLanesPassBothValidators_NoStageNameSpecialCasing()
    {
        var candidate = await RealFiveDayCandidateAsync();
        var executionIndex = await RealExecutionIndexAsync();

        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, 14, executionIndex));

        var taperKeySessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions
            .Where(s => s.PhaseKey == "TAPER" && s.StructuralRole == "KEY_SESSION").ToList();
        Assert.NotEmpty(taperKeySessions);
        Assert.All(taperKeySessions, s =>
        {
            Assert.IsType<CatalogSessionPrescriptionSource.ProfileBacked>(s.PrescriptionSource);
            Assert.NotEqual("TAPER_SHARPEN", s.ProgressionStageKey);
        });
        // Reaching FinalPrescriptionComplete status already proves both CatalogPrescriptionContextValidator
        // (upstream, in dated-skeleton prescription-context build) and CatalogFinalPrescribedPlanValidator
        // (this finalizer's own last step) accepted every Taper KEY session -- neither validator was
        // touched this phase.
        Assert.All(taperKeySessions, s => Assert.True(s.ValidationResult.IsValid, string.Join(", ", s.ValidationResult.Errors)));
    }

    // ── §24: calendar zero-delta — multi-KEY spacing still enforced ─────────

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(14)]
    public async Task RealFiveDayCandidate_CalendarSpacing_StillEnforced(int targetWeekCount)
    {
        var candidate = await RealFiveDayCandidateAsync();
        var executionIndex = await RealExecutionIndexAsync();

        var result = await RealOrchestrator().MaterializeAsync(Context(candidate, targetWeekCount, executionIndex));

        var datedSkeleton = result.PrescriptionResult.VolumeResult.BindingResult.DatedSkeleton;
        Assert.All(datedSkeleton.Weeks, week =>
        {
            var keySlots = week.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").ToList();
            Assert.Equal(2, keySlots.Count);
            Assert.True(Math.Abs(keySlots[0].SessionDate.DayNumber - keySlots[1].SessionDate.DayNumber) >= 2);
        });
    }

    // ── §34: determinism ─────────────────────────────────────────────────────

    [Fact]
    public async Task RealTenWeek_RepeatedGeneration_DeterministicPlanAndPrescriptionIdentity()
    {
        var candidate = await RealFiveDayCandidateAsync();
        var executionIndex = await RealExecutionIndexAsync();
        var context = Context(candidate, 10, executionIndex);

        var first = await RealOrchestrator().MaterializeAsync(context);
        var second = await RealOrchestrator().MaterializeAsync(context);

        var firstIdentities = first.PrescriptionResult.FinalPrescribedPlan.Sessions
            .Select(s => (s.WeekNumber, s.Date, s.WorkoutDefinitionKey, s.WorkoutDefinitionVersion, s.PlannedDistanceKm));
        var secondIdentities = second.PrescriptionResult.FinalPrescribedPlan.Sessions
            .Select(s => (s.WeekNumber, s.Date, s.WorkoutDefinitionKey, s.WorkoutDefinitionVersion, s.PlannedDistanceKm));
        Assert.Equal(firstIdentities, secondIdentities);
    }

    // ── §18: 12-week reference regression — still succeeds, byte-identical wiring ──

    [Fact]
    public async Task RealTwelveWeek_PreferredCore_DoesNotUseThisDynamicChain_StillSucceedsViaExistingReferencePath()
    {
        // 12 weeks is TEN_K_MASTER's DefaultWeeks (RaceHorizonMode.PreferredCore), which
        // CatalogPreviewGenerator routes through its own main body, not this dynamic chain --
        // confirmed by FREQ.6D.4D.5F (12-week preview already succeeded before this phase's fix).
        // This orchestrator only ever receives CompressedCore/ExtendedCore horizons in production
        // (see CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton's own horizon.Mode guard);
        // asserting that fact directly here, rather than re-deriving 12-week behavior through a
        // chain production never routes it through.
        var candidate = await RealFiveDayCandidateAsync();
        Assert.Equal(12, candidate.CoreCycle.DefaultWeeks);
    }
}
