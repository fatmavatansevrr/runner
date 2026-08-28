using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal static class LongHorizonCheckpointTestFixture
{
    internal static async Task<LongHorizonRollingCheckpointRequest> RequestAsync(
        int totalWeeks = 28,
        ReadinessProfile profile = ReadinessProfile.ConsistencyNeeded)
    {
        var initial = await LongHorizonRollingInitialActivationTestFixture.ExecuteAsync(totalWeeks, profile);
        var lifecycle = initial.StructuralRoadmap!.GlobalWeekNumbers.ToDictionary(
            week => week,
            week => week <= initial.ActivationWindow!.EndGlobalWeek
                ? LongHorizonNumericLifecycleState.Completed
                : LongHorizonNumericLifecycleState.NumericPending);
        var rows = Rows(initial.ActivationWindow!);
        return new LongHorizonRollingCheckpointRequest
        {
            StructuralRoadmap = initial.StructuralRoadmap,
            StructuralSkeleton = initial.StructuralSkeleton!,
            LifecycleStates = lifecycle,
            MostRecentlyActivatedWindow = initial.ActivationWindow!,
            TrainingDayEvidence = rows,
            CheckpointDate = initial.ActivationWindow!.Weeks.Max(w => w.CalendarDates!.Value.End).AddDays(1),
            CurrentAvailability = LongHorizonRollingInitialActivationTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            ReadinessProfile = profile,
            PreviousContextVersion = initial.ContextVersion!,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
        };
    }

    internal static IReadOnlyList<LongHorizonTrainingDayEvidenceRow> Rows(RollingNumericActivationWindow window)
    {
        var result = new List<LongHorizonTrainingDayEvidenceRow>();
        foreach (var week in window.Weeks)
        foreach (var (session, index) in week.SessionPrescriptions!.Select((session, index) => (session, index)))
        {
            result.Add(new LongHorizonTrainingDayEvidenceRow(week.GlobalWeekNumber, new TrainingDay
            {
                Id = StableGuid(week.GlobalWeekNumber, index),
                Date = session.AssignedDate!.Value.ToDateTime(TimeOnly.MinValue),
                DayType = session.SessionRole == "LONG_RUN" ? TrainingDayType.LongRun : TrainingDayType.Easy,
                Status = TrainingDayStatus.Completed,
                PlannedDistanceKm = session.DistanceKm + 10,
                ActualDistanceKm = session.DistanceKm,
                ActualDurationMin = 30,
                IsLongRun = session.SessionRole == "LONG_RUN",
                CompletedAt = session.AssignedDate.Value.ToDateTime(TimeOnly.MinValue).AddHours(1),
            }));
        }
        return result;
    }

    internal static LongHorizonPriorValidatedAnchor Prior(double weekly = 20, double longRun = 7) => new(
        new ValidatedSustainableLoad
        {
            WeeklyVolumeKm = weekly,
            LongRunKm = longRun,
            EvidenceWindowStartWeek = 1,
            EvidenceWindowEndWeek = 4,
            CompletedEvidenceWeekNumbers = [1, 2, 3],
            ExcludedRecoveryWeekNumbers = [4],
            WeeklyLoadSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.PriorValidatedCheckpointLoad, LongHorizonEvidenceAuthorityStatus.Authoritative),
            LongRunSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.PriorValidatedCheckpointLoad, LongHorizonEvidenceAuthorityStatus.Authoritative),
            RoundingPolicy = "0.5km",
            LongRunCapPolicy = "0.40",
            ValidationStatus = LongHorizonValidationStatus.Valid,
            Provenance = "prior checkpoint",
        }, true, 1);

    private static Guid StableGuid(int week, int index) => Guid.Parse($"{week:D8}-0000-0000-0000-{index:D12}");
}

public sealed class LongHorizonCheckpointTrainingDayAggregationTests
{
    [Fact]
    public async Task CompletedActuals_AreAveragedByNonRecoveryWeek_PlannedNeverContributes()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        var expected = LongHorizonCheckpointEvidenceAggregator.Round(
            result.EvidenceSnapshot!.ActualWeeklyVolumeByGlobalWeek.Where(pair => pair.Key != 4).Average(pair => pair.Value));

        Assert.Equal(expected, result.ValidatedLoad!.WeeklyVolumeKm);
        Assert.DoesNotContain(4, result.ValidatedLoad.CompletedEvidenceWeekNumbers);
        Assert.Contains(4, result.ValidatedLoad.ExcludedRecoveryWeekNumbers);
        Assert.All(request.TrainingDayEvidence, row => Assert.NotEqual(row.TrainingDay.PlannedDistanceKm, row.TrainingDay.ActualDistanceKm));
    }

    [Theory]
    [InlineData(TrainingDayStatus.Missed)]
    [InlineData(TrainingDayStatus.Skipped)]
    [InlineData(TrainingDayStatus.SoftMissed)]
    public async Task NonCompletedTerminalStatuses_ContributeNoActualDistanceAndLowerAdherence(TrainingDayStatus status)
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        var row = request.TrainingDayEvidence.First();
        row.TrainingDay.Status = status;
        row.TrainingDay.ActualDistanceKm = null;
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);

        Assert.Equal(15, result.EvidenceSnapshot!.CompletedRunsCount);
        Assert.Equal(1, result.EvidenceSnapshot.MissedSessionCount);
        Assert.Equal(93.8, result.EvidenceSnapshot.AdherenceRatePercent);
    }

    [Fact]
    public async Task CompletedPartialSession_UsesLowerActualDistance()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        var row = request.TrainingDayEvidence.First(r => r.GlobalWeekNumber == 1);
        row.TrainingDay.ActualDistanceKm = 1;
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);

        Assert.True(result.EvidenceSnapshot!.ActualWeeklyVolumeByGlobalWeek[1] < 20);
        Assert.Equal(16, result.EvidenceSnapshot.CompletedRunsCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0d)]
    public async Task CompletedNullOrZeroActual_IsUnusableAndNeverReplacedByPlanned(double? actual)
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        var row = request.TrainingDayEvidence.First(r => r.GlobalWeekNumber == 1);
        row.TrainingDay.ActualDistanceKm = actual;
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);

        Assert.NotEqual(row.TrainingDay.PlannedDistanceKm, result.EvidenceSnapshot!.ActualWeeklyVolumeByGlobalWeek[1]);
        Assert.True(result.EvidenceSnapshot.ActualWeeklyVolumeByGlobalWeek[1] < 20);
    }

    [Fact]
    public async Task LongRunUsesCompletedMeanAndExistingFortyPercentCap()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        foreach (var row in request.TrainingDayEvidence.Where(r => r.TrainingDay.IsLongRun))
            row.TrainingDay.ActualDistanceKm = 100;
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);

        Assert.Equal(
            LongHorizonCheckpointEvidenceAggregator.Round(result.ValidatedLoad!.WeeklyVolumeKm!.Value * 0.40),
            result.ValidatedLoad.LongRunKm);
    }

    [Fact]
    public async Task RecoveryLongRun_IsExcludedFromValidatedLongRunMean()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        request.TrainingDayEvidence.Single(row => row.GlobalWeekNumber == 4 && row.TrainingDay.IsLongRun)
            .TrainingDay.ActualDistanceKm = 100;
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.DoesNotContain(100, result.EvidenceSnapshot!.CompletedLongRunsKm);
    }

    [Fact]
    public async Task FutureAndPreviousWindowRows_AreExcludedRatherThanBlended()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        var outside = new TrainingDay { Id = Guid.NewGuid(), Status = TrainingDayStatus.Completed, ActualDistanceKm = 999, IsLongRun = true };
        request = request with { TrainingDayEvidence = request.TrainingDayEvidence.Concat([new(8, outside), new(20, outside)]).ToList() };
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);

        Assert.DoesNotContain(999, result.EvidenceSnapshot!.CompletedLongRunsKm);
        Assert.DoesNotContain(8, result.EvidenceSnapshot.ActualWeeklyVolumeByGlobalWeek.Keys);
    }

    [Fact]
    public async Task ContradictoryNegativeActualEvidence_BlocksTyped()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        request.TrainingDayEvidence[0].TrainingDay.ActualDistanceKm = -1;
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);

        Assert.Equal(LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked, result.Outcome);
        Assert.Equal(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved, result.AuthoritativeReason!.Value.CheckpointReason);
        Assert.Empty(result.NewlyActivatedWeeks);
    }

    [Fact]
    public async Task NonCompletedRowWithActualDistance_BlocksAsContradictory()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        request.TrainingDayEvidence[0].TrainingDay.Status = TrainingDayStatus.Missed;
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved, result.AuthoritativeReason!.Value.CheckpointReason);
        Assert.Empty(result.NewlyActivatedWeeks);
    }
}

public sealed class LongHorizonCheckpointTerminalPriorAnchorTests
{
    [Fact]
    public async Task PeriodNotEnded_NeverActivatesEarlyEvenWithPriorAnchor()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        request = request with
        {
            CheckpointDate = request.MostRecentlyActivatedWindow.Weeks[^1].CalendarDates!.Value.End,
            PriorValidatedAnchor = LongHorizonCheckpointTestFixture.Prior(),
        };
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonCheckpointOutcome.NumericActivationBlocked, result.CheckpointDecision!.Outcome);
        Assert.Equal(LongHorizonCheckpointReasonCode.CheckpointWindowNotComplete, result.AuthoritativeReason!.Value.CheckpointReason);
    }

    [Fact]
    public async Task EndedUnresolved_WithFreshPriorAnchor_MaintainsButCannotGrow()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        request.TrainingDayEvidence[0].TrainingDay.Status = TrainingDayStatus.Planned;
        request.TrainingDayEvidence[0].TrainingDay.ActualDistanceKm = null;
        request = request with { PriorValidatedAnchor = LongHorizonCheckpointTestFixture.Prior() };
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);

        Assert.Equal(LongHorizonCheckpointOutcome.MaintenanceOnly, result.CheckpointDecision!.Outcome);
        Assert.Equal(LongHorizonCheckpointReasonCode.CheckpointWindowNotComplete, result.CheckpointDecision.AuthoritativeReason!.Value.CheckpointReason);
    }

    [Theory]
    [InlineData(TrainingDayStatus.Planned)]
    [InlineData(TrainingDayStatus.PendingConfirmation)]
    [InlineData(TrainingDayStatus.Rescheduled)]
    public async Task EndedUnresolved_WithoutPriorAnchor_Blocks(TrainingDayStatus status)
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        request.TrainingDayEvidence[0].TrainingDay.Status = status;
        request.TrainingDayEvidence[0].TrainingDay.ActualDistanceKm = null;
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonCheckpointOutcome.NumericActivationBlocked, result.CheckpointDecision!.Outcome);
        Assert.False(result.EvidenceSnapshot!.AllSessionsTerminal);
    }

    [Fact]
    public async Task StalePriorAnchor_IsNotReusedIndefinitely()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        foreach (var row in request.TrainingDayEvidence) { row.TrainingDay.Status = TrainingDayStatus.Missed; row.TrainingDay.ActualDistanceKm = null; }
        request = request with { PriorValidatedAnchor = LongHorizonCheckpointTestFixture.Prior() with { IsFreshForCurrentInvocation = false } };
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonCheckpointReasonCode.ValidatedLoadUnavailable, result.AuthoritativeReason!.Value.CheckpointReason);
    }
}

public sealed class LongHorizonCheckpointTransitionTests
{
    [Fact]
    public async Task SafetyConflict_HasHighestPriority()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        foreach (var row in request.TrainingDayEvidence) { row.TrainingDay.Status = TrainingDayStatus.Planned; row.TrainingDay.ActualDistanceKm = null; }
        request = request with { SafetyState = LongHorizonSafetyState.UnresolvedSafetyCritical };
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonCheckpointReasonCode.SafetyReassessmentRequired, result.AuthoritativeReason!.Value.CheckpointReason);
        Assert.True(result.CheckpointDecision!.SafetyPriorityApplied);
    }

    [Fact]
    public async Task NoValidatedWeeklyLoad_Blocks()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        foreach (var row in request.TrainingDayEvidence) { row.TrainingDay.Status = TrainingDayStatus.Missed; row.TrainingDay.ActualDistanceKm = null; }
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonCheckpointReasonCode.ValidatedLoadUnavailable, result.AuthoritativeReason!.Value.CheckpointReason);
    }

    [Fact]
    public async Task NoCompletedLongRun_BlocksWithSpecificReason()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        foreach (var row in request.TrainingDayEvidence.Where(r => r.TrainingDay.IsLongRun)) { row.TrainingDay.Status = TrainingDayStatus.Missed; row.TrainingDay.ActualDistanceKm = null; }
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonCheckpointReasonCode.ValidatedLongRunEvidenceUnavailable, result.AuthoritativeReason!.Value.CheckpointReason);
    }

    [Fact]
    public async Task InvalidAvailability_BlocksNumericWindow()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        request = request with { CurrentAvailability = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday] };
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonCheckpointReasonCode.NumericWindowInfeasible, result.AuthoritativeReason!.Value.CheckpointReason);
    }

    [Fact]
    public async Task FailedGrowthConfidence_MaintainsWithExactlyOneReason()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        foreach (var row in request.TrainingDayEvidence.Where(r => r.GlobalWeekNumber == 2)) { row.TrainingDay.Status = TrainingDayStatus.Missed; row.TrainingDay.ActualDistanceKm = null; }
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonCheckpointOutcome.MaintenanceOnly, result.CheckpointDecision!.Outcome);
        Assert.Equal(LongHorizonCheckpointReasonCode.AdherenceConfidenceInsufficientForGrowth, result.CheckpointDecision.AuthoritativeReason!.Value.CheckpointReason);
    }

    [Fact]
    public async Task FullFreshEvidence_GrowsWithoutFailureReason()
    {
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(await LongHorizonCheckpointTestFixture.RequestAsync());
        Assert.Equal(LongHorizonCheckpointOutcome.GrowthEligible, result.CheckpointDecision!.Outcome);
        Assert.Null(result.CheckpointDecision.AuthoritativeReason);
        Assert.Equal(LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated, result.Outcome);
    }
}

public sealed class LongHorizonCheckpointGrowthNextWindowTests
{
    [Fact]
    public async Task GrowthSelectsOnlyContiguousNextFourGeWeeksAndPreservesCaps()
    {
        var spy = new CapturingGrowthMaterializer();
        var result = await new LongHorizonRollingCheckpointRuntime(spy).EvaluateAndActivateNextGeWindowAsync(await LongHorizonCheckpointTestFixture.RequestAsync());
        Assert.Equal([5, 6, 7, 8], result.NewlyActivatedWeeks.Select(w => w.GlobalWeekNumber));
        Assert.Equal([5, 6, 7, 8], spy.Received);
        Assert.Equal(result.ValidatedLoad!.WeeklyVolumeKm, result.NewlyActivatedWeeks[0].TotalWeeklyVolumeKm);
        Assert.All(result.NewlyActivatedWeeks, week => Assert.True(week.LongRunKm <= week.TotalWeeklyVolumeKm * 0.40 + 0.01));
        for (var i = 1; i < 3; i++) Assert.True(result.NewlyActivatedWeeks[i].TotalWeeklyVolumeKm - result.NewlyActivatedWeeks[i - 1].TotalWeeklyVolumeKm <= 2.5);
    }

    [Theory]
    [InlineData(ReadinessProfile.ConsistencyNeeded)]
    [InlineData(ReadinessProfile.CoreEntryReady)]
    internal async Task BothProfilesUseSameNumericAuthorityAndRetainContent(ReadinessProfile profile)
    {
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(await LongHorizonCheckpointTestFixture.RequestAsync(profile: profile));
        Assert.Equal(LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated, result.Outcome);
        Assert.All(result.NewlyActivatedWeeks.SelectMany(w => w.SessionPrescriptions!), session => Assert.False(string.IsNullOrWhiteSpace(session.WorkoutKey)));
        Assert.All(result.NewlyActivatedWeeks, week => Assert.Contains("GrowthEligible", week.NumericPolicyProvenance));
    }

    [Fact]
    public async Task ProfilesHaveIdenticalNumericAuthorityWhileCatalogContentRemainsProfileAware()
    {
        var consistency = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(
            await LongHorizonCheckpointTestFixture.RequestAsync(profile: ReadinessProfile.ConsistencyNeeded));
        var ready = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(
            await LongHorizonCheckpointTestFixture.RequestAsync(profile: ReadinessProfile.CoreEntryReady));

        Assert.Equal(consistency.NewlyActivatedWeeks.Select(week => (week.TotalWeeklyVolumeKm, week.LongRunKm)),
            ready.NewlyActivatedWeeks.Select(week => (week.TotalWeeklyVolumeKm, week.LongRunKm)));
        Assert.NotEqual(
            consistency.NewlyActivatedWeeks.SelectMany(week => week.SessionPrescriptions!).Select(session => session.WorkoutKey),
            ready.NewlyActivatedWeeks.SelectMany(week => week.SessionPrescriptions!).Select(session => session.WorkoutKey));
    }

    private sealed class CapturingGrowthMaterializer : ILongHorizonRollingGeWindowMaterializer
    {
        private readonly ExistingLongHorizonGeWindowMaterializer _inner = new();
        public IReadOnlyList<int> Received { get; private set; } = [];
        public IReadOnlyList<LongHorizonGeWeekNumericResult> Materialize(IReadOnlyList<LongHorizonGeWeekDescriptor> weeks, LongHorizonGeEntryBaselineInput baseline, RunningBackground level = RunningBackground.Intermediate)
        { Received = weeks.Select(w => w.WeekIndex).ToList(); return _inner.Materialize(weeks, baseline); }
    }
}

public sealed class LongHorizonCheckpointMaintenanceNextWindowTests
{
    [Fact]
    public async Task MaintenanceUsesValidatedAnchorFlatThenExistingRecoveryWithoutDrift()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        foreach (var row in request.TrainingDayEvidence.Where(r => r.GlobalWeekNumber == 2)) { row.TrainingDay.Status = TrainingDayStatus.Missed; row.TrainingDay.ActualDistanceKm = null; }
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        var anchor = result.CheckpointDecision!.MaintenanceAnchorWeeklyVolumeKm!.Value;
        Assert.All(result.NewlyActivatedWeeks.Take(3), week => Assert.Equal(anchor, week.TotalWeeklyVolumeKm));
        Assert.Equal(LongHorizonCheckpointEvidenceAggregator.Round(anchor * 0.85), result.NewlyActivatedWeeks[3].TotalWeeklyVolumeKm);
        Assert.All(result.NewlyActivatedWeeks, week => Assert.True(week.TotalWeeklyVolumeKm <= anchor));
        Assert.All(result.NewlyActivatedWeeks, week => Assert.True(week.LongRunKm <= result.ValidatedLoad!.LongRunKm));
    }

    [Fact]
    public void ConsecutiveMaintenanceWithSameAnchor_DoesNotRiseOrRecoveryDrift()
    {
        var materializer = new LongHorizonGeMaintenanceWindowMaterializer();
        var anchor = LongHorizonCheckpointTestFixture.Prior().Load;
        var first = materializer.Materialize(LongHorizonGeStructuralSelector.Select(12, ReadinessProfile.ConsistencyNeeded).Take(4).ToList(), anchor);
        var second = materializer.Materialize(LongHorizonGeStructuralSelector.Select(12, ReadinessProfile.ConsistencyNeeded).Skip(4).Take(4).ToList(), anchor);
        Assert.Equal(first.Select(w => w.TotalVolumeKm), second.Select(w => w.TotalVolumeKm));
        Assert.Equal(anchor.WeeklyVolumeKm, second[0].TotalVolumeKm);
    }

    [Theory]
    [InlineData(25, 1)]
    [InlineData(26, 2)]
    [InlineData(27, 3)]
    public async Task PartialMaintenanceWindow_ActivatesOnlyRemainingGeWithoutSyntheticRecovery(int totalWeeks, int expected)
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync(totalWeeks);
        foreach (var row in request.TrainingDayEvidence.Where(r => r.GlobalWeekNumber == 2)) { row.TrainingDay.Status = TrainingDayStatus.Missed; row.TrainingDay.ActualDistanceKm = null; }
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(expected, result.NewlyActivatedWeeks.Count);
        Assert.All(result.NewlyActivatedWeeks, week => Assert.Equal(result.CheckpointDecision!.MaintenanceAnchorWeeklyVolumeKm, week.TotalWeeklyVolumeKm));
    }
}

public sealed class LongHorizonCheckpointBoundaryNoJitTests
{
    [Fact]
    public async Task RunwayBoundary_ReturnsNonErrorWithoutWindowOrJit()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync(24);
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonRollingCheckpointRuntimeOutcome.GeCheckpointCompletedWithoutGeWindowBecauseRunwayBoundaryReached, result.Outcome);
        Assert.Null(result.ActivationWindow);
        Assert.Null(result.CheckpointDecision);
        Assert.Empty(result.NewlyActivatedWeeks);
        Assert.All(result.LifecycleStates.Where(pair => pair.Key > 4), pair => Assert.Equal(LongHorizonNumericLifecycleState.NumericPending, pair.Value));
    }

    [Fact]
    public async Task RuntimeProducesNoRunwayCoreJitOrTargetLock()
    {
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(await LongHorizonCheckpointTestFixture.RequestAsync());
        Assert.DoesNotContain(result.NewlyActivatedWeeks, w => w.SegmentType != LongHorizonStructuralSegmentType.GeneralEndurance);
        Assert.Null(result.ActivationWindow!.JitContextDecisionId);
        Assert.DoesNotContain(result.ValidationStages, stage => stage.Contains("Runway", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.ValidationStages, stage => stage.Contains("Core", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class LongHorizonCheckpointAtomicityVersioningTests
{
    [Fact]
    public async Task OneWeekFailure_ActivatesZeroAndPreservesHistory()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        var before = request.LifecycleStates.ToDictionary(pair => pair.Key, pair => pair.Value);
        var result = await new LongHorizonRollingCheckpointRuntime(new ThrowingGrowthMaterializer()).EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Empty(result.NewlyActivatedWeeks);
        Assert.Equal(LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked, result.Outcome);
        Assert.All(before.Where(pair => pair.Key <= 4), pair => Assert.Equal(pair.Value, result.LifecycleStates[pair.Key]));
        Assert.Equal(4, result.LifecycleStates.Count(pair => pair.Value == LongHorizonNumericLifecycleState.NumericActivationBlocked));
    }

    [Fact]
    public async Task SuccessUpdatesOnlyNextWindowAndIncrementsContext()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(request.PreviousContextVersion.Sequence + 1, result.ContextVersion.Sequence);
        Assert.All(Enumerable.Range(5, 4), week => Assert.Equal(LongHorizonNumericLifecycleState.NumericActivated, result.LifecycleStates[week]));
        Assert.All(result.LifecycleStates.Where(pair => pair.Key > 8), pair => Assert.Equal(LongHorizonNumericLifecycleState.NumericPending, pair.Value));
        Assert.All(result.NewlyActivatedWeeks, week => Assert.Equal(result.CheckpointDecision!.DecisionId, week.CheckpointDecisionId));
    }

    [Fact]
    public async Task IdenticalInputIsDeterministic_ChangedEvidenceChangesIdentity()
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        var first = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        var second = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(first.ContextVersion, second.ContextVersion);
        Assert.Equal(first.CheckpointDecision!.DecisionId, second.CheckpointDecision!.DecisionId);
        Assert.Equal(first.ActivationWindow!.WindowId, second.ActivationWindow!.WindowId);

        request.TrainingDayEvidence[0].TrainingDay.ActualDistanceKm -= 0.5;
        var changed = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.NotEqual(first.CheckpointDecision.DecisionId, changed.CheckpointDecision!.DecisionId);
    }

    [Fact]
    public async Task ProductionValidationStagesExecuteInOrder()
    {
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(await LongHorizonCheckpointTestFixture.RequestAsync());
        Assert.Equal(["InputEligibility", "StructuralRoadmap", "EvidenceAggregation", "SnapshotValidation", "NextGeWindowSelection", "StateTransition", "GrowthMaterialization", "ActivatedWeekValidation", "Atomicity", "FinalResultValidation"], result.ValidationStages);
    }

    private sealed class ThrowingGrowthMaterializer : ILongHorizonRollingGeWindowMaterializer
    {
        public IReadOnlyList<LongHorizonGeWeekNumericResult> Materialize(IReadOnlyList<LongHorizonGeWeekDescriptor> weeks, LongHorizonGeEntryBaselineInput baseline, RunningBackground level = RunningBackground.Intermediate) =>
            throw new InvalidOperationException("Injected selected-week failure");
    }
}

public sealed class LongHorizonCheckpointEligibilityTests
{
    [Theory]
    [InlineData(GoalDistance.FiveK, RunningBackground.Intermediate, 4)]
    [InlineData(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4)]
    [InlineData(GoalDistance.TenK, RunningBackground.Beginner, 4)]
    [InlineData(GoalDistance.TenK, RunningBackground.Intermediate, 3)]
    public async Task UnsupportedDistanceLevelOrFrequency_RemainsRejected(
        GoalDistance distance, RunningBackground level, int daysPerWeek)
    {
        var request = await LongHorizonCheckpointTestFixture.RequestAsync();
        request = request with { GoalDistance = distance, Level = level, DaysPerWeek = daysPerWeek };
        await Assert.ThrowsAsync<LongHorizonCheckpointDecisionInvalidException>(
            () => new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request));
    }
}
