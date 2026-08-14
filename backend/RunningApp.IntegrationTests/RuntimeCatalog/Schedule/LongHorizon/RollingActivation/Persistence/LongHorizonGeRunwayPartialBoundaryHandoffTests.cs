using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2D -- root-cause resolution proof for the GE-&gt;Runway partial-
/// boundary checkpoint handoff finding Phase 4L.2B/4L.2C surfaced (25/26/27-
/// week horizons failing with "Next GE week 5 must be NumericPending").
///
/// Root cause (proven, not the production defect the phase's own narrative
/// assumed): <see cref="LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync"/>
/// hardcoded RaceDate = StartDate + 21*7 days regardless of the plan's actual
/// TotalWeeks. For 25/26/27-week horizons this made the race appear 4-6 weeks
/// too soon, causing the real Core-generation/condition-resolution pipeline
/// (invoked for real inside LongHorizonRollingJitCompositionOrchestrator) to
/// reject the first GE-&gt;Runway mixed-window composition and block it. Since
/// LongHorizonRollingBlockPersistenceAdapter.PersistBlockAsync does not update
/// the plan's CurrentWindowStartWeek/EndWeek, the blocked weeks (5-8) were left
/// marked NumericActivationBlocked while the aggregate pointer stayed at the
/// prior window [1,4] -- so the SECOND checkpoint call recomputed nextStart=5
/// and found week 5 no longer Pending, producing the exact reported message.
/// This is a test-fixture defect, not a defect in
/// LongHorizonRollingCheckpointRuntime, LongHorizonRollingJitActivationRuntime,
/// or any other production component: a direct, isolated call to the checkpoint
/// runtime for the exact same partial-terminal-GE-window boundary (5,5)
/// succeeds correctly and returns NextGeWindowActivated with no GE-only
/// validator ever seeing a Runway week. Fixed by computing RaceDate from the
/// plan's own StructuralRoadmap.TotalWeeks instead of a hardcoded constant.
/// </summary>
public sealed class LongHorizonGeRunwayPartialBoundaryHandoffTests
{
    [Theory]
    [InlineData(25)]
    [InlineData(26)]
    [InlineData(27)]
    public async Task PartialTerminalGeWindow_HandsOffToRunwayAndPersistsAcrossRestart(int totalWeeks)
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(totalWeeks);
        var date1 = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        // Call 1: partial terminal GE remainder (week 5 only, since initial
        // activation already covers weeks 1-4) hands off atomically into a
        // real GE+Runway mixed window through the real production chain --
        // checkpoint runtime -> JIT composition orchestrator -> persistence.
        var call1 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, date1, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call1.Outcome);

        // Restart: brand-new connection, real reconstruction, no regeneration.
        using var db1 = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed1 = await new LongHorizonRollingStateRepository(db1).LoadRestartSnapshotAsync(planStateId);
        var window1 = reconstructed1!.DarkState.CurrentWindow;
        var geEnd = reconstructed1.DarkState.StructuralRoadmap.GeneralEnduranceWeeks;

        // Natural mixed shape (GE = TotalWeeks-20 weeks; initial activation
        // covers weeks 1-4): the terminal GE remainder is weeks 5..geEnd, and
        // the greedy 4-week window cap pulls in Runway weeks geEnd+1..8.
        Assert.Equal(5, window1.StartGlobalWeek);
        Assert.Equal(8, window1.EndGlobalWeek);
        Assert.Contains(LongHorizonStructuralSegmentType.GeneralEndurance, window1.SegmentsCovered);
        Assert.Contains(LongHorizonStructuralSegmentType.PreparationRunway, window1.SegmentsCovered);
        Assert.Contains(window1.Weeks, w => w.GlobalWeekNumber == geEnd && w.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance);
        Assert.Contains(window1.Weeks, w => w.GlobalWeekNumber == geEnd + 1 && w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway);
        // No GE week beyond the structural GE end was ever selected.
        Assert.All(window1.Weeks.Where(w => w.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance),
            w => Assert.True(w.GlobalWeekNumber <= geEnd));

        // Call 2: Runway continuation from the mixed window must advance
        // cleanly (no Runway week was ever pushed through a GE-only validator).
        var date2 = date1.AddDays(28);
        var call2 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, window1, date2, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call2.Outcome);

        using var db2 = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed2 = await new LongHorizonRollingStateRepository(db2).LoadRestartSnapshotAsync(planStateId);
        var window2 = reconstructed2!.DarkState.CurrentWindow;

        Assert.Equal(window1.EndGlobalWeek + 1, window2.StartGlobalWeek);
        Assert.DoesNotContain(LongHorizonStructuralSegmentType.GeneralEndurance, window2.SegmentsCovered);
        Assert.Contains(LongHorizonStructuralSegmentType.PreparationRunway, window2.SegmentsCovered);

        // No future/unselected week activated outside the two selected slices.
        var activatedWeeks = reconstructed2.DarkState.LifecycleStates
            .Where(kv => kv.Value == LongHorizonNumericLifecycleState.NumericActivated)
            .Select(kv => kv.Key).OrderBy(w => w).ToList();
        Assert.Equal(Enumerable.Range(1, window2.EndGlobalWeek), activatedWeeks);

        // No regeneration: same target lock/prescription identity across restarts.
        Assert.NotNull(reconstructed1.DarkState.RunwayPrescription);
        Assert.Equal(reconstructed1.DarkState.RunwayPrescription!.PrescriptionId, reconstructed2.DarkState.RunwayPrescription!.PrescriptionId);
    }

    /// <summary>
    /// Direct reproduction of the checkpoint runtime's own boundary behavior in
    /// isolation: a partial terminal GE window (here, just week 5) is a valid
    /// checkpoint window on its own and returns NextGeWindowActivated -- proving
    /// the GE checkpoint validator itself was never the defect.
    /// </summary>
    [Fact]
    public async Task CheckpointRuntime_AcceptsPartialTerminalGeWindowInIsolation()
    {
        var (planStateId, initialWindow, _, _) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);
        var state = snapshot!.DarkState;

        var checkpointRuntime = new LongHorizonRollingCheckpointRuntime();
        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(initialWindow, planStateId);
        var request = new LongHorizonRollingCheckpointRequest
        {
            StructuralRoadmap = state.StructuralRoadmap,
            StructuralSkeleton = state.StructuralSkeleton,
            LifecycleStates = state.LifecycleStates,
            MostRecentlyActivatedWindow = state.CurrentWindow,
            TrainingDayEvidence = evidenceRows,
            CheckpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29),
            CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            ReadinessProfile = state.StructuralRoadmap.Profile,
            PriorValidatedAnchor = LongHorizonCheckpointTestFixture.Prior(20, 8),
            PreviousContextVersion = state.ContextVersion,
            GoalType = RunningApp.Domain.Enums.GoalType.Race,
            GoalDistance = RunningApp.Domain.Enums.GoalDistance.TenK,
            Level = RunningApp.Domain.Enums.RunningBackground.Intermediate,
            DaysPerWeek = 4,
        };

        var result = await checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(request);

        Assert.Equal(LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated, result.Outcome);
        Assert.Equal(5, result.ActivationWindow!.StartGlobalWeek);
        Assert.Equal(5, result.ActivationWindow.EndGlobalWeek);
        Assert.Equal(5, state.StructuralRoadmap.GeneralEnduranceWeeks);
    }
}
