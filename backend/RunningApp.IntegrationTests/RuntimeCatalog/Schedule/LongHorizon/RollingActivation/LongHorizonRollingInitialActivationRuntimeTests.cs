using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal static class LongHorizonRollingInitialActivationTestFixture
{
    internal static readonly DateOnly StartDate = new(2026, 8, 3);
    internal static readonly IReadOnlyList<DayOfWeek> PreferredDays =
        [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday];

    internal static string RepoRoot() => RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    internal static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    internal static ICatalogWorkoutDefinitionLoader Loader() => new CatalogWorkoutDefinitionLoader(
        Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }));

    internal static LongHorizonCompositionDecision Decide(int totalWeeks, ReadinessProfile profile)
    {
        var raceDate = StartDate.AddDays(totalWeeks * 7);
        return LongHorizonCompositionResolver.Resolve(
            RaceHorizonPolicy.Decide(StartDate, raceDate), profile);
    }

    internal static LongHorizonRollingInitialActivationRequest Request(
        int totalWeeks,
        ReadinessProfile profile = ReadinessProfile.ConsistencyNeeded,
        double weeklyVolumeKm = 20,
        double? longestRunKm = 8,
        int? runsPerWeek = 3) => new()
    {
        CompositionDecision = Decide(totalWeeks, profile),
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        StartDate = StartDate,
        RaceDate = StartDate.AddDays(totalWeeks * 7),
        OnboardingBaseline = new LongHorizonGeEntryBaselineInput(weeklyVolumeKm, longestRunKm, runsPerWeek),
        PreferredDays = PreferredDays,
        LongRunDay = DayOfWeek.Sunday,
        CatalogRoot = CatalogRoot(),
        WorkoutLoader = Loader(),
    };

    internal static Task<LongHorizonRollingInitialActivationResult> ExecuteAsync(
        int totalWeeks,
        ReadinessProfile profile = ReadinessProfile.ConsistencyNeeded,
        double weeklyVolumeKm = 20,
        double? longestRunKm = 8,
        int? runsPerWeek = 3,
        ILongHorizonRollingGeWindowMaterializer? materializer = null) =>
        new LongHorizonRollingInitialActivationRuntime(materializer).BuildInitialActivationAsync(
            Request(totalWeeks, profile, weeklyVolumeKm, longestRunKm, runsPerWeek));
}

public sealed class LongHorizonRollingInitialActivationRuntimeTests
{
    [Theory]
    [InlineData(21, 1)]
    [InlineData(52, 32)]
    public async Task StructuralRoadmap_ReusesApprovedCompleteComposition(int totalWeeks, int expectedGeWeeks)
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(totalWeeks);

        Assert.Equal(LongHorizonRollingInitialActivationStatus.Approved, result.Status);
        Assert.Equal(totalWeeks, result.StructuralRoadmap!.TotalWeeks);
        Assert.Equal(expectedGeWeeks, result.StructuralRoadmap.GeneralEnduranceWeeks);
        Assert.Equal(8, result.StructuralRoadmap.PreparationRunwayWeeks);
        Assert.Equal(12, result.StructuralRoadmap.CoreWeeks);
        Assert.Equal(Enumerable.Range(1, totalWeeks), result.StructuralRoadmap.GlobalWeekNumbers);
        Assert.Equal(
            [LongHorizonStructuralSegmentType.GeneralEndurance, LongHorizonStructuralSegmentType.PreparationRunway, LongHorizonStructuralSegmentType.Core],
            result.StructuralRoadmap.Segments.Select(segment => segment.SegmentType));
    }

    [Fact]
    public async Task ActivatedWeeks_CarryExistingNumericBindingCalendarAndProvenance()
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(24);

        Assert.All(result.ActivatedNumericWeeks, week =>
        {
            Assert.Equal(LongHorizonNumericLifecycleState.NumericActivated, week.LifecycleState);
            Assert.True(week.TotalWeeklyVolumeKm > 0);
            Assert.True(week.LongRunKm > 0);
            Assert.NotNull(week.CalendarDates);
            Assert.NotNull(week.PaceIntensityContext);
            Assert.Equal(LongHorizonEvidenceSource.OriginalOnboardingEvidence, week.EvidenceProvenance!.Source);
            Assert.Equal(LongHorizonEvidenceAuthorityStatus.LegacyCurrentProductionSource, week.EvidenceProvenance.AuthorityStatus);
            Assert.NotNull(week.ContextVersion);
            Assert.Contains(LongHorizonGeNumericExecutor.PolicyId, week.NumericPolicyProvenance);
            Assert.Equal(4, week.SessionPrescriptions!.Count);
            Assert.Equal(week.TotalWeeklyVolumeKm!.Value, week.SessionPrescriptions.Sum(session => session.DistanceKm), 6);
            Assert.All(week.SessionPrescriptions, session =>
            {
                Assert.True(session.DistanceKm > 0);
                Assert.False(string.IsNullOrWhiteSpace(session.WorkoutKey));
                Assert.True(session.WorkoutVersion > 0);
                Assert.NotNull(session.AssignedDate);
                Assert.False(string.IsNullOrWhiteSpace(session.Source));
            });
        });
    }

    [Fact]
    public async Task PendingSuffix_PreservesStructureAndUsesNullNeverZeroForExecutableValues()
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(52);

        Assert.Equal(48, result.PendingNumericWeeks.Count);
        Assert.All(result.PendingNumericWeeks, week =>
        {
            Assert.Equal(LongHorizonNumericLifecycleState.NumericPending, week.LifecycleState);
            Assert.Null(week.TotalWeeklyVolumeKm);
            Assert.Null(week.LongRunKm);
            Assert.Null(week.SessionPrescriptions);
            Assert.Null(week.CalendarDates);
            LongHorizonActivatedNumericWeekValidator.Validate(week);
        });
        Assert.Contains(result.PendingNumericWeeks, week => week.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance);
        Assert.Equal(8, result.PendingNumericWeeks.Count(week => week.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway));
        Assert.Equal(12, result.PendingNumericWeeks.Count(week => week.SegmentType == LongHorizonStructuralSegmentType.Core));
        Assert.All(result.StructuralRoadmap!.Weeks, week => Assert.NotEmpty(week.StructuralWorkoutRoles));
    }

    [Fact]
    public async Task InitialContext_UsesOnboardingWithoutPretendingItIsCheckpointHistory()
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(24);
        var context = result.InitialActivationContext!;

        Assert.Equal(LongHorizonInitialActivationSource.InitialOnboardingActivation, context.ActivationSource);
        Assert.Equal(LongHorizonEvidenceSource.OriginalOnboardingEvidence, context.EvidenceSource.Source);
        Assert.NotEqual(LongHorizonEvidenceSource.CompletedTrainingHistory, context.EvidenceSource.Source);
        Assert.Equal(LongHorizonEvidenceAuthorityStatus.LegacyCurrentProductionSource, context.EvidenceSource.AuthorityStatus);
        Assert.True(context.SafetyValidationApplied);
        Assert.True(context.FeasibilityValidationApplied);
        Assert.Equal(1, context.ContextVersion.Sequence);
        Assert.Null(result.ActivationWindow!.CheckpointDecisionId);
    }

    [Theory]
    [InlineData(12, 4)]
    [InlineData(20, 8)]
    [InlineData(45, 16)]
    public async Task SupportedLowTypicalHighOnboardingBaselines_UseExistingPolicy(
        double weeklyVolumeKm, double longestRunKm)
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(
            24, weeklyVolumeKm: weeklyVolumeKm, longestRunKm: longestRunKm);

        Assert.Equal(LongHorizonRollingInitialActivationStatus.Approved, result.Status);
        Assert.Equal(weeklyVolumeKm, result.ActivatedNumericWeeks[0].TotalWeeklyVolumeKm);
    }

    [Fact]
    public async Task ReadinessProfiles_RetainCatalogContentDifferenceButShareNumericPolicy()
    {
        var consistency = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded);
        var ready = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(24, ReadinessProfile.CoreEntryReady);

        Assert.NotEqual(
            consistency.StructuralSkeleton!.Weeks.Take(4).SelectMany(w => w.OrderedWorkoutSlots).Select(s => (s.WorkoutKey, s.WorkoutVersion)),
            ready.StructuralSkeleton!.Weeks.Take(4).SelectMany(w => w.OrderedWorkoutSlots).Select(s => (s.WorkoutKey, s.WorkoutVersion)));
        Assert.Equal(
            consistency.ActivatedNumericWeeks.Select(w => (w.TotalWeeklyVolumeKm, w.LongRunKm)),
            ready.ActivatedNumericWeeks.Select(w => (w.TotalWeeklyVolumeKm, w.LongRunKm)));
        Assert.All(consistency.ActivatedNumericWeeks.Concat(ready.ActivatedNumericWeeks),
            week => Assert.Contains(LongHorizonGeNumericExecutor.PolicyId, week.NumericPolicyProvenance));
    }

    [Fact]
    public async Task IdenticalInputAndCatalog_ProducesDeterministicDomainOutput()
    {
        var first = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(24);
        var second = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(24);

        Assert.Equal(first.ContextVersion, second.ContextVersion);
        Assert.Equal(first.InitialActivationContext!.DecisionId, second.InitialActivationContext!.DecisionId);
        Assert.Equal(first.ActivationWindow!.WindowId, second.ActivationWindow!.WindowId);
        Assert.Equal(first.ActivationWindow.StartGlobalWeek, second.ActivationWindow.StartGlobalWeek);
        Assert.Equal(first.ActivationWindow.EndGlobalWeek, second.ActivationWindow.EndGlobalWeek);
        Assert.Equal(first.Provenance, second.Provenance);
        Assert.Equal(
            first.ActivatedNumericWeeks.Select(Projection),
            second.ActivatedNumericWeeks.Select(Projection));

        static string Projection(ActivatedNumericWeek week) => string.Join('|',
            week.GlobalWeekNumber,
            week.TotalWeeklyVolumeKm,
            week.LongRunKm,
            string.Join(';', week.SessionPrescriptions!.Select(s =>
                $"{s.SessionRole}:{s.DistanceKm}:{s.WorkoutKey}:{s.WorkoutVersion}:{s.AssignedDate:yyyy-MM-dd}")));
    }
}

public sealed class LongHorizonRollingInitialActivationPartialWindowTests
{
    [Theory]
    [InlineData(21, 1)]
    [InlineData(22, 2)]
    [InlineData(23, 3)]
    [InlineData(24, 4)]
    [InlineData(52, 4)]
    public async Task InitialWindow_ActivatesOnlyAvailableGePrefix(int totalWeeks, int expectedActualSize)
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(totalWeeks);
        var window = result.ActivationWindow!;

        Assert.Equal(1, window.StartGlobalWeek);
        Assert.Equal(expectedActualSize, window.EndGlobalWeek);
        Assert.Equal(4, window.RequestedWindowSizeWeeks);
        Assert.Equal(expectedActualSize, window.ActualWindowSizeWeeks);
        Assert.Equal(Enumerable.Range(1, expectedActualSize), window.Weeks.Select(w => w.GlobalWeekNumber));
        Assert.Equal([LongHorizonStructuralSegmentType.GeneralEndurance], window.SegmentsCovered);
        Assert.DoesNotContain(window.Weeks, week => week.SegmentType != LongHorizonStructuralSegmentType.GeneralEndurance);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(23)]
    public async Task OneToThreeWeekInitialWindows_DoNotInventRecoveryOrBorrowRunway(int totalWeeks)
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(totalWeeks);

        Assert.DoesNotContain(result.StructuralSkeleton!.Weeks.Take(totalWeeks - 20), week => week.IsRecoveryWeek == true);
        Assert.DoesNotContain(result.ActivatedNumericWeeks, week => week.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway);
        Assert.Equal(totalWeeks - 20, result.ActivatedNumericWeeks.Count);
    }

    [Fact]
    public async Task RealGeWeekFour_UsesExistingPointEightFiveRecoveryRule()
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(24);
        var priorPeak = result.ActivatedNumericWeeks[2].TotalWeeklyVolumeKm!.Value;
        var recovery = result.ActivatedNumericWeeks[3].TotalWeeklyVolumeKm!.Value;
        var expected = Math.Round(priorPeak * LongHorizonGeNumericExecutor.RecoveryVolumeRatio / 0.5,
            MidpointRounding.AwayFromZero) * 0.5;

        Assert.True(result.StructuralSkeleton!.Weeks[3].IsRecoveryWeek);
        Assert.Equal(expected, recovery, 6);
        Assert.True(recovery < priorPeak);
    }
}

public sealed class LongHorizonRollingInitialActivationNoFullUpfrontExecutionTests
{
    [Fact]
    public async Task FiftyTwoWeeks_BoundedMaterializerReceivesOnlyFirstFourGeWeeks()
    {
        var spy = new CapturingGeWindowMaterializer();
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(52, materializer: spy);

        Assert.Equal(LongHorizonRollingInitialActivationStatus.Approved, result.Status);
        Assert.Equal(1, spy.CallCount);
        Assert.Equal([1, 2, 3, 4], spy.ReceivedWeekIndexes);
        Assert.Equal(4, result.ActivatedNumericWeeks.Count);
        Assert.Equal(48, result.PendingNumericWeeks.Count);
    }

    [Fact]
    public async Task RuntimeCreatesNoRunwayCoreJitOrTargetLockOutput()
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(52);

        Assert.DoesNotContain(result.ActivatedNumericWeeks, week =>
            week.SegmentType is LongHorizonStructuralSegmentType.PreparationRunway or LongHorizonStructuralSegmentType.Core);
        Assert.Null(result.ActivationWindow!.JitContextDecisionId);
        Assert.Null(result.ActivationWindow.CheckpointDecisionId);
        Assert.DoesNotContain(result.Provenance, item => item.Contains("JIT", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Provenance, item => item.Contains("CORE_TARGET_LOCK", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class CapturingGeWindowMaterializer : ILongHorizonRollingGeWindowMaterializer
    {
        private readonly ExistingLongHorizonGeWindowMaterializer _inner = new();
        public int CallCount { get; private set; }
        public IReadOnlyList<int> ReceivedWeekIndexes { get; private set; } = [];

        public IReadOnlyList<LongHorizonGeWeekNumericResult> Materialize(
            IReadOnlyList<LongHorizonGeWeekDescriptor> selectedGeneralEnduranceWeeks,
            LongHorizonGeEntryBaselineInput onboardingBaseline)
        {
            CallCount++;
            ReceivedWeekIndexes = selectedGeneralEnduranceWeeks.Select(w => w.WeekIndex).ToList();
            return _inner.Materialize(selectedGeneralEnduranceWeeks, onboardingBaseline);
        }
    }
}

public sealed class LongHorizonRollingInitialActivationAtomicityValidatorTests
{
    [Fact]
    public async Task OneWeekFailure_BlocksWholeWindowAndReturnsNoPartialActivation()
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(
            24, materializer: new ThrowingGeWindowMaterializer());

        Assert.Equal(LongHorizonRollingInitialActivationStatus.Blocked, result.Status);
        Assert.Empty(result.ActivatedNumericWeeks);
        Assert.Equal(24, result.PendingNumericWeeks.Count);
        Assert.Equal(4, result.PendingNumericWeeks.Count(w => w.LifecycleState == LongHorizonNumericLifecycleState.NumericActivationBlocked));
        Assert.Equal(LongHorizonActivationWindowStatus.Blocked, result.ActivationWindow!.Status);
        Assert.Empty(result.ActivationWindow.Weeks);
        Assert.Equal(LongHorizonRollingInitialActivationFailureReason.GeneralEnduranceNumericInfeasibility, result.Failure!.Reason);
        Assert.Contains(nameof(InvalidOperationException), result.Failure.Message);
    }

    [Fact]
    public async Task MalformedMaterializerResult_BlocksAtomicallyWithTypedReason()
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(
            24, materializer: new EmptyGeWindowMaterializer());

        Assert.Equal(LongHorizonRollingInitialActivationStatus.Blocked, result.Status);
        Assert.Empty(result.ActivatedNumericWeeks);
        Assert.Equal("LONG_HORIZON_ROLLING_INITIAL_GE_RESULT_SHAPE_INVALID", result.Failure!.Code);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(53)]
    public async Task RuntimeDoesNotHandleHorizonsOutsideTwentyOneToFiftyTwo(int totalWeeks)
    {
        var result = await new LongHorizonRollingInitialActivationRuntime().BuildInitialActivationAsync(
            LongHorizonRollingInitialActivationTestFixture.Request(totalWeeks));

        Assert.Equal(LongHorizonRollingInitialActivationStatus.Blocked, result.Status);
        Assert.Equal(LongHorizonRollingInitialActivationFailureReason.InvalidStructuralHorizon, result.Failure!.Reason);
        Assert.Empty(result.ActivatedNumericWeeks);
    }

    [Fact]
    public async Task UnsupportedDistanceLevelFrequencyAndHabitRemainOutsideRuntime()
    {
        var runtime = new LongHorizonRollingInitialActivationRuntime();
        var valid = LongHorizonRollingInitialActivationTestFixture.Request(24);
        var requests = new[]
        {
            valid with { GoalDistance = GoalDistance.FiveK },
            valid with { Level = RunningBackground.Beginner },
            valid with { DaysPerWeek = 3 },
            valid with { GoalType = GoalType.Habit },
        };

        foreach (var request in requests)
        {
            var result = await runtime.BuildInitialActivationAsync(request);
            Assert.Equal(LongHorizonRollingInitialActivationStatus.Blocked, result.Status);
            Assert.Equal(LongHorizonRollingInitialActivationFailureReason.InvalidEligibility, result.Failure!.Reason);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidOnboardingWeeklyVolumeFailsTyped(double weeklyVolumeKm)
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(24, weeklyVolumeKm: weeklyVolumeKm);

        Assert.Equal(LongHorizonRollingInitialActivationStatus.Blocked, result.Status);
        Assert.Equal(LongHorizonRollingInitialActivationFailureReason.InvalidOnboardingEvidence, result.Failure!.Reason);
        Assert.Equal("LONG_HORIZON_ROLLING_INITIAL_ONBOARDING_EVIDENCE_INVALID", result.Failure.Code);
    }

    [Fact]
    public async Task SuccessfulProductionRuntimeInvokesAllValidatorsInRequiredOrder()
    {
        var result = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(24);

        Assert.Equal(Enum.GetValues<LongHorizonRollingInitialActivationValidationStage>(), result.Validation.CompletedStages);
        Assert.True(result.Validation.IsValid);
        LongHorizonRollingInitialActivationResultValidator.Validate(result);
    }

    private sealed class ThrowingGeWindowMaterializer : ILongHorizonRollingGeWindowMaterializer
    {
        public IReadOnlyList<LongHorizonGeWeekNumericResult> Materialize(
            IReadOnlyList<LongHorizonGeWeekDescriptor> selectedGeneralEnduranceWeeks,
            LongHorizonGeEntryBaselineInput onboardingBaseline) =>
            throw new InvalidOperationException("Injected week-3 numeric failure.");
    }

    private sealed class EmptyGeWindowMaterializer : ILongHorizonRollingGeWindowMaterializer
    {
        public IReadOnlyList<LongHorizonGeWeekNumericResult> Materialize(
            IReadOnlyList<LongHorizonGeWeekDescriptor> selectedGeneralEnduranceWeeks,
            LongHorizonGeEntryBaselineInput onboardingBaseline) => [];
    }
}
