using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.8 — tests for <see cref="LongHorizonRollingJitActivationRuntime"/>.
/// Runway numeric fixtures are built via the real, unchanged
/// <c>PreparationRunwayNumericMaterializer</c> through the existing
/// <c>PreparationRunwayNumericMaterializerTests</c> fixture helpers, never a
/// hand-built shortcut. Dark and unwired: not called from any production or
/// live request path.
/// </summary>
public sealed class LongHorizonRollingJitActivationRuntimeTests
{
    private static readonly DateOnly PlanStart = new(2026, 1, 5);

    private static LongHorizonStructuralRoadmap BuildRoadmap(int totalWeeks, ReadinessProfile profile = ReadinessProfile.ConsistencyNeeded)
    {
        var geWeeks = totalWeeks - 20;
        var segments = new List<LongHorizonStructuralSegment>
        {
            new() { SegmentType = LongHorizonStructuralSegmentType.GeneralEndurance, StartGlobalWeek = 1, EndGlobalWeek = geWeeks, Provenance = "Phase 4I.1" },
            new() { SegmentType = LongHorizonStructuralSegmentType.PreparationRunway, StartGlobalWeek = geWeeks + 1, EndGlobalWeek = geWeeks + 8, Provenance = "Phase 4G.6A.1" },
            new() { SegmentType = LongHorizonStructuralSegmentType.Core, StartGlobalWeek = geWeeks + 9, EndGlobalWeek = totalWeeks, Provenance = "Phase 4G.6A.1" },
        };

        return new LongHorizonStructuralRoadmap
        {
            TotalWeeks = totalWeeks,
            GeneralEnduranceWeeks = geWeeks,
            PreparationRunwayWeeks = 8,
            CoreWeeks = 12,
            Segments = segments,
            GlobalWeekNumbers = Enumerable.Range(1, totalWeeks).ToList(),
            RaceDate = PlanStart.AddDays(totalWeeks * 7),
            Profile = profile,
            StructuralStatus = "Confirmed",
        };
    }

    private static Dictionary<int, LongHorizonNumericLifecycleState> Lifecycle(int totalWeeks, int completedThrough)
    {
        var dict = new Dictionary<int, LongHorizonNumericLifecycleState>();
        for (var week = 1; week <= totalWeeks; week++)
        {
            dict[week] = week <= completedThrough ? LongHorizonNumericLifecycleState.Completed : LongHorizonNumericLifecycleState.NumericPending;
        }

        return dict;
    }

    private static ValidatedSustainableLoad ValidLoad(double weekly, double longRun) => new()
    {
        WeeklyVolumeKm = weekly,
        LongRunKm = longRun,
        EvidenceWindowStartWeek = 1,
        EvidenceWindowEndWeek = 3,
        CompletedEvidenceWeekNumbers = [1, 2, 3],
        ExcludedRecoveryWeekNumbers = [4],
        WeeklyLoadSource = LongHorizonEvidenceAuthorityCatalog.RunwayRollingWeeklyLoadAuthority,
        LongRunSource = LongHorizonEvidenceAuthorityCatalog.RunwayRollingLongRunAuthority,
        RoundingPolicy = "0.5km increment (Phase 4K.2)",
        LongRunCapPolicy = "LongRunHardCapShare 0.40 (Phase 4K.2)",
        ValidationStatus = LongHorizonValidationStatus.Valid,
    };

    private static IReadOnlyList<RuntimeConditionResolutionResult> ResolvedConditions() =>
    [
        RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "REALISTIC", "TEST_FIXTURE"),
        RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST_FIXTURE"),
    ];

    private static RollingNumericActivationWindow PriorWindow(int endGlobalWeek) => new()
    {
        WindowId = Guid.NewGuid(),
        ContextVersion = LongHorizonContextVersion.Initial(),
        StartGlobalWeek = Math.Max(1, endGlobalWeek - 3),
        EndGlobalWeek = endGlobalWeek,
        RequestedWindowSizeWeeks = 4,
        ActualWindowSizeWeeks = Math.Min(4, endGlobalWeek),
        Weeks = [],
        SegmentsCovered = [LongHorizonStructuralSegmentType.GeneralEndurance],
        ActivationSource = LongHorizonInitialActivationSource.CheckpointRollingActivation,
        Status = LongHorizonActivationWindowStatus.Blocked,
    };

    private static ActivatedNumericWeek GeWeek(int globalWeek, double weeklyVolumeKm = 30, double longRunKm = 10)
    {
        var support = (weeklyVolumeKm - longRunKm) / 2;
        return new ActivatedNumericWeek
        {
            GlobalWeekNumber = globalWeek,
            SegmentType = LongHorizonStructuralSegmentType.GeneralEndurance,
            LifecycleState = LongHorizonNumericLifecycleState.NumericActivated,
            TotalWeeklyVolumeKm = weeklyVolumeKm,
            LongRunKm = longRunKm,
            SessionPrescriptions =
            [
                new LongHorizonSessionPrescriptionReference { SessionRole = "LONG_RUN", DistanceKm = longRunKm },
                new LongHorizonSessionPrescriptionReference { SessionRole = "EASY_1", DistanceKm = support },
                new LongHorizonSessionPrescriptionReference { SessionRole = "EASY_2", DistanceKm = support },
            ],
            CalendarDates = (PlanStart.AddDays((globalWeek - 1) * 7), PlanStart.AddDays((globalWeek - 1) * 7 + 6)),
        };
    }

    private static LongHorizonJitCoreCandidateWeek CoreCandidate(int globalWeek, double weeklyVolumeKm = 24, double longRunKm = 8)
    {
        var support = (weeklyVolumeKm - longRunKm) / 2;
        return new LongHorizonJitCoreCandidateWeek
        {
            GlobalWeekNumber = globalWeek,
            WeeklyVolumeKm = weeklyVolumeKm,
            LongRunKm = longRunKm,
            SessionPrescriptions =
            [
                new LongHorizonSessionPrescriptionReference { SessionRole = "LONG_RUN", DistanceKm = longRunKm },
                new LongHorizonSessionPrescriptionReference { SessionRole = "KEY_SESSION", DistanceKm = support },
                new LongHorizonSessionPrescriptionReference { SessionRole = "EASY_SUPPORT", DistanceKm = support },
            ],
            Stage = "FOUNDATION_EASY_BASE",
        };
    }

    private static LongHorizonRollingJitActivationRequest BaseRequest(
        int totalWeeks, int completedThrough, ReadinessProfile profile = ReadinessProfile.ConsistencyNeeded) => new()
    {
        StructuralRoadmap = BuildRoadmap(totalWeeks, profile),
        LifecycleStates = Lifecycle(totalWeeks, completedThrough),
        PreviousActivatedWindow = PriorWindow(completedThrough),
        EvidenceSnapshot = null!, // not read by the runtime directly; validated upstream by Phase 4K.3 contracts.
        ValidatedLoad = ValidLoad(24, 8),
        PreviousContextVersion = LongHorizonContextVersion.Initial(),
        ReadinessProfile = profile,
        CurrentAvailability = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday],
        PreferredDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday],
        LongRunDay = DayOfWeek.Sunday,
        CheckpointDate = PlanStart.AddDays(completedThrough * 7),
        RaceDate = PlanStart.AddDays(totalWeeks * 7),
        SafetyState = LongHorizonSafetyState.Clear,
        ConditionResults = ResolvedConditions(),
        PlanStartDate = PlanStart,
    };

    // ── helpers to attach EvidenceSnapshot without relying on it (runtime doesn't need it) ──

    private static LongHorizonRollingJitActivationRequest WithEvidenceSnapshot(LongHorizonRollingJitActivationRequest request) => request with
    {
        EvidenceSnapshot = new LongHorizonCheckpointEvidenceSnapshot
        {
            CheckpointId = Guid.NewGuid(),
            CheckpointDate = request.CheckpointDate,
            ActivatedWindowStartWeek = 1,
            ActivatedWindowEndWeek = 4,
            WindowCalendarPeriodEnded = true,
            AllSessionsTerminal = true,
            ActualWeeklyVolumesKm = [24, 24, 24],
            CompletedLongRunsKm = [8, 8],
            CompletedRunsCount = 12,
            PlannedRunsCount = 12,
            AdherenceRatePercent = 100,
            MissedSessionCount = 0,
            Availability = request.CurrentAvailability,
            SafetyState = request.SafetyState,
            CurrentSegment = LongHorizonStructuralSegmentType.GeneralEndurance,
            EvidenceSourceMetadata = LongHorizonEvidenceAuthorityCatalog.RunwayRollingWeeklyLoadAuthority,
        },
    };

    private static LongHorizonRollingJitActivationRuntime Runtime() => new();

    // ── Authority and mapping ────────────────────────────────────────────

    [Fact]
    public void CoreInputAdapter_MapsValidatedWeeklyAndLongRun()
    {
        var mapping = LongHorizonJitCoreInputAdapter.Map(ValidLoad(24, 8), exactCompletedFrequency: 4);
        Assert.Equal(24, mapping.RecentWeeklyVolumeKm);
        Assert.Equal(8, mapping.RecentLongestRunKm);
        Assert.Equal(4, mapping.RecentRunsPerWeek);
    }

    [Fact]
    public void CoreInputAdapter_AbsentFrequency_RemainsNull()
    {
        var mapping = LongHorizonJitCoreInputAdapter.Map(ValidLoad(24, 8), exactCompletedFrequency: null);
        Assert.Null(mapping.RecentRunsPerWeek);
    }

    [Fact]
    public void CoreInputAdapter_UnavailableLoad_Throws()
    {
        var unavailable = ValidLoad(24, 8) with { WeeklyVolumeKm = null, LongRunKm = null, ValidationStatus = LongHorizonValidationStatus.Unavailable };
        Assert.Throws<LongHorizonJitContextInvalidException>(() => LongHorizonJitCoreInputAdapter.Map(unavailable, null));
    }

    [Fact]
    public void CoreInputAdapter_NeverSourcedFromOnboardingOrPlannedGeExit()
    {
        var mapping = LongHorizonJitCoreInputAdapter.Map(ValidLoad(24, 8), 4);
        Assert.NotEqual(LongHorizonEvidenceSource.OriginalOnboardingEvidence, mapping.WeeklyVolumeAuthority.Source);
        Assert.NotEqual(LongHorizonEvidenceSource.PlannedGeneralEnduranceExit, mapping.WeeklyVolumeAuthority.Source);
    }

    // ── Safety and window selection ─────────────────────────────────────

    [Fact]
    public async Task SafetyUnresolved_BlocksBeforeAllOtherFailures()
    {
        var request = BaseRequest(28, 8) with { SafetyState = LongHorizonSafetyState.UnresolvedSafetyCritical, CurrentAvailability = [DayOfWeek.Monday] };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitActivationOutcome.JitWindowBlocked, result.Outcome);
        Assert.Equal(LongHorizonReasonCode.SafetyReassessmentRequired, result.AuthoritativeReason);
    }

    [Fact]
    public async Task NoInvalidAvailability_BlocksWithJitAvailabilityInfeasible()
    {
        var request = BaseRequest(28, 8) with { CurrentAvailability = [DayOfWeek.Monday, DayOfWeek.Tuesday] };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitActivationOutcome.JitWindowBlocked, result.Outcome);
        Assert.Equal(LongHorizonJitReasonCode.JitAvailabilityInfeasible, result.AuthoritativeReason!.Value.JitReason);
    }

    [Fact]
    public async Task UnresolvedPace_Blocks()
    {
        var request = BaseRequest(28, 8) with
        {
            ConditionResults =
            [
                RuntimeConditionResolutionResult.NotEvaluated("PACE_SOURCE_IN", "TEST_FIXTURE"),
                RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST_FIXTURE"),
            ],
        };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonJitReasonCode.JitPaceSourceUnresolved, result.AuthoritativeReason!.Value.JitReason);
    }

    [Fact]
    public async Task UnresolvedGoalFeasibility_Blocks()
    {
        var request = BaseRequest(28, 8) with
        {
            ConditionResults =
            [
                RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "REALISTIC", "TEST_FIXTURE"),
                RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "TEST_FIXTURE"),
            ],
        };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonJitReasonCode.JitGoalFeasibilityUnresolved, result.AuthoritativeReason!.Value.JitReason);
    }

    [Fact]
    public async Task NoUnstartedPendingWeek_BlocksWithBoundaryMissed()
    {
        var request = BaseRequest(28, 28);
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonJitReasonCode.JitActivationBoundaryMissed, result.AuthoritativeReason!.Value.JitReason);
    }

    [Fact]
    public async Task GeOnlyWindow_IsOutOfScope_Blocks()
    {
        // 28-week roadmap: GE = 8 weeks. completedThrough=2 leaves a pure GE-only next window (3-6).
        var request = BaseRequest(28, 2);
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.JitWindowBlocked, result.Outcome);
        Assert.Equal(LongHorizonJitReasonCode.JitSegmentTransitionInfeasible, result.AuthoritativeReason!.Value.JitReason);
    }

    // ── Runway-only window (first entry) ────────────────────────────────

    private static LongHorizonRollingJitActivationRequest FirstRunwayEntryRequest(int totalWeeks = 28, int runwayWeeks = 8, double weekly = 24, double longRun = 8)
    {
        var geWeeks = totalWeeks - 20;
        var request = BaseRequest(totalWeeks, geWeeks); // GE fully completed, next window starts at Runway entry.
        return request with
        {
            ValidatedLoad = ValidLoad(weekly, longRun),
            ResolvedCoreWeekOneTarget = PreparationRunwayNumericMaterializerTests.Target(weekly, longRun),
            RunwayStartingLoadEvidence = PreparationRunwayNumericMaterializerTests.Evidence(
                Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization.PreparationRunwayLoadEvidenceState.Provided, weekly,
                Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization.PreparationRunwayLoadEvidenceState.Provided, longRun),
            RunwayStructuralWeeks = PreparationRunwayNumericMaterializerTests.StructuralWeeks(
                Application.RuntimeCatalog.Schedule.PreparationRunwayEngine.PreparationRunwayAllocationProfile.ConsistencyNeeded, runwayWeeks),
        };
    }

    [Fact]
    public async Task RunwayOnly_FirstEntry_ActivatesFourWeeks()
    {
        var request = FirstRunwayEntryRequest();
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, result.Outcome);
        Assert.Equal(4, result.NewlyActivatedWeeks.Count);
        Assert.All(result.NewlyActivatedWeeks, w => Assert.Equal(LongHorizonStructuralSegmentType.PreparationRunway, w.SegmentType));
        Assert.NotNull(result.CoreTargetLock);
        Assert.NotNull(result.RunwayPrescription);
        Assert.NotNull(result.RunwaySlice);
    }

    [Fact]
    public async Task RunwayOnly_TargetLockCoversCompleteRunwayRange()
    {
        var request = FirstRunwayEntryRequest();
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        var runwaySegment = request.StructuralRoadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway);
        Assert.Equal((runwaySegment.StartGlobalWeek, runwaySegment.EndGlobalWeek), result.CoreTargetLock!.LockedForActivatedRunwayWeekRange);
    }

    [Fact]
    public async Task RunwayOnly_NoDirectTargetAssignment_TargetComesFromResolvedCoreWeekOneTarget()
    {
        var request = FirstRunwayEntryRequest(weekly: 26, longRun: 9);
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(26, result.CoreTargetLock!.TargetWeeklyVolumeKm);
        Assert.Equal(9, result.CoreTargetLock.TargetLongRunKm);
    }

    [Fact]
    public async Task DirectionGuard_EqualEvidence_Activates()
    {
        var request = FirstRunwayEntryRequest(weekly: 24, longRun: 8);
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, result.Outcome);
    }

    [Fact]
    public async Task DirectionGuard_BelowTargetEvidence_Activates()
    {
        var request = FirstRunwayEntryRequest(weekly: 24, longRun: 8) with { ValidatedLoad = ValidLoad(18, 6) };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, result.Outcome);
    }

    [Fact]
    public async Task DirectionGuard_AboveTargetEvidence_Blocks_NoDownwardInterpolation()
    {
        var request = FirstRunwayEntryRequest(weekly: 24, longRun: 8) with
        {
            ValidatedLoad = ValidLoad(30, 8),
            RunwayStartingLoadEvidence = PreparationRunwayNumericMaterializerTests.Evidence(
                Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization.PreparationRunwayLoadEvidenceState.Provided, 30,
                Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization.PreparationRunwayLoadEvidenceState.Provided, 8),
        };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitActivationOutcome.JitWindowBlocked, result.Outcome);
        Assert.Equal(LongHorizonJitReasonCode.JitSegmentTransitionInfeasible, result.AuthoritativeReason!.Value.JitReason);
        Assert.Empty(result.NewlyActivatedWeeks);
    }

    [Fact]
    public async Task FullRunwayMaterializer_RunsOnceAtFirstEntry_PrescriptionDurationMatchesRunway()
    {
        var request = FirstRunwayEntryRequest();
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(8, result.RunwayPrescription!.FullRunwayDurationWeeks);
    }

    [Fact]
    public async Task GeRunwayMixed_LeavesNonMultipleOfFourRunwayRemainder_ForLaterPartialSlices()
    {
        // 21-week roadmap: GE=1 week only. First window is necessarily GE(1)+Runway(2-4) = 3 Runway weeks,
        // leaving 5 Runway weeks (non-multiple-of-4) -- proving partial slices arise naturally at both
        // segment boundaries, never via an artificial "partial final Runway-only" case (Runway is always
        // immediately followed by Core in this architecture, so a genuinely isolated Runway-only final
        // partial slice cannot occur -- see RunwayCoreMixed_PartialFinalSlice_ActivatesAtomically below).
        var request = FirstRunwayEntryRequest(totalWeeks: 21, runwayWeeks: 8) with
        {
            LifecycleStates = Lifecycle(21, 0),
            PreviousActivatedWindow = PriorWindow(0),
            GeCheckpointDecision = new LongHorizonCheckpointDecision
            {
                DecisionId = Guid.NewGuid(),
                EvidenceSnapshotId = Guid.NewGuid(),
                Outcome = LongHorizonCheckpointOutcome.GrowthEligible,
                ValidatedLoad = ValidLoad(24, 8),
                ActivationWindowBoundary = (1, 1),
                SafetyPriorityApplied = true,
                PolicyProvenance = "Phase 4K.6/4K.7 (test fixture)",
            },
            GeActivatedWeeks = [GeWeek(1)],
        };

        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitActivationOutcome.GeRunwayMixedWindowActivated, result.Outcome);
        Assert.Equal(1, result.NewlyActivatedWeeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance));
        Assert.Equal(3, result.NewlyActivatedWeeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway));
    }

    // ── Runway continuation slice (reuses existing prescription) ────────

    [Fact]
    public async Task RunwayOnly_Continuation_ReusesExistingPrescriptionAndLock_NoRegeneration()
    {
        var firstRequest = FirstRunwayEntryRequest();
        var firstResult = await Runtime().ResolveAndActivateNextWindowAsync(firstRequest);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, firstResult.Outcome);

        var continuation = BaseRequest(28, 12) with // GE(8) + first Runway window(4) = 12 completed
        {
            LifecycleStates = firstResult.LifecycleStates,
            PreviousActivatedWindow = firstResult.ActivationWindow!,
            ExistingLockedCoreTarget = firstResult.CoreTargetLock,
            ExistingRunwayPrescription = firstResult.RunwayPrescription,
        };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(continuation);

        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, result.Outcome);
        Assert.Equal(4, result.NewlyActivatedWeeks.Count);
        Assert.Equal(firstResult.RunwayPrescription!.PrescriptionId, result.RunwayPrescription!.PrescriptionId);
        Assert.Equal(firstResult.CoreTargetLock!.CreatedByDecisionId, result.CoreTargetLock!.CreatedByDecisionId);
    }

    [Fact]
    public async Task RunwayOnly_IncompatibleExistingPrescription_Blocks()
    {
        var firstRequest = FirstRunwayEntryRequest();
        var firstResult = await Runtime().ResolveAndActivateNextWindowAsync(firstRequest);

        // Build an incompatible prescription (different range) for a 36-week roadmap to force a mismatch.
        var otherRequest = FirstRunwayEntryRequest(totalWeeks: 36);
        var otherResult = await Runtime().ResolveAndActivateNextWindowAsync(otherRequest);

        var continuation = BaseRequest(28, 12) with
        {
            LifecycleStates = firstResult.LifecycleStates,
            PreviousActivatedWindow = firstResult.ActivationWindow!,
            ExistingLockedCoreTarget = otherResult.CoreTargetLock,
            ExistingRunwayPrescription = otherResult.RunwayPrescription,
        };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(continuation);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.JitWindowBlocked, result.Outcome);
    }

    // ── GE + Runway mixed window ─────────────────────────────────────────

    [Fact]
    public async Task GeRunwayMixed_ActivatesAtomically()
    {
        var geWeeks = 8; // 28-week roadmap
        var request = FirstRunwayEntryRequest() with
        {
            LifecycleStates = Lifecycle(28, geWeeks - 2), // 2 GE weeks left, then Runway starts
            PreviousActivatedWindow = PriorWindow(geWeeks - 2),
            GeCheckpointDecision = new LongHorizonCheckpointDecision
            {
                DecisionId = Guid.NewGuid(),
                EvidenceSnapshotId = Guid.NewGuid(),
                Outcome = LongHorizonCheckpointOutcome.GrowthEligible,
                ValidatedLoad = ValidLoad(24, 8),
                ActivationWindowBoundary = (geWeeks - 1, geWeeks),
                SafetyPriorityApplied = true,
                PolicyProvenance = "Phase 4K.7 (test fixture)",
            },
            GeActivatedWeeks = [GeWeek(geWeeks - 1), GeWeek(geWeeks)],
        };

        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitActivationOutcome.GeRunwayMixedWindowActivated, result.Outcome);
        Assert.Equal(4, result.NewlyActivatedWeeks.Count);
        Assert.Equal(2, result.NewlyActivatedWeeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance));
        Assert.Equal(2, result.NewlyActivatedWeeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway));
    }

    [Fact]
    public async Task GeRunwayMixed_MissingGeActivatedWeeks_Blocks()
    {
        var geWeeks = 8;
        var request = FirstRunwayEntryRequest() with
        {
            LifecycleStates = Lifecycle(28, geWeeks - 2),
            PreviousActivatedWindow = PriorWindow(geWeeks - 2),
            GeCheckpointDecision = null,
            GeActivatedWeeks = null,
        };

        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.JitWindowBlocked, result.Outcome);
        Assert.Empty(result.NewlyActivatedWeeks);
    }

    [Fact]
    public async Task GeRunwayMixed_RunwayFailure_BlocksFullWindow_ZeroGeWeeksActivate()
    {
        var geWeeks = 8;
        var request = FirstRunwayEntryRequest() with
        {
            LifecycleStates = Lifecycle(28, geWeeks - 2),
            PreviousActivatedWindow = PriorWindow(geWeeks - 2),
            ValidatedLoad = ValidLoad(30, 8), // above target -> Runway direction guard rejects
            GeCheckpointDecision = new LongHorizonCheckpointDecision
            {
                DecisionId = Guid.NewGuid(),
                EvidenceSnapshotId = Guid.NewGuid(),
                Outcome = LongHorizonCheckpointOutcome.GrowthEligible,
                ValidatedLoad = ValidLoad(30, 8),
                ActivationWindowBoundary = (geWeeks - 1, geWeeks),
                SafetyPriorityApplied = true,
                PolicyProvenance = "Phase 4K.7 (test fixture)",
            },
            GeActivatedWeeks = [GeWeek(geWeeks - 1), GeWeek(geWeeks)],
            RunwayStartingLoadEvidence = PreparationRunwayNumericMaterializerTests.Evidence(
                Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization.PreparationRunwayLoadEvidenceState.Provided, 30,
                Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization.PreparationRunwayLoadEvidenceState.Provided, 8),
        };

        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitActivationOutcome.JitWindowBlocked, result.Outcome);
        Assert.Empty(result.NewlyActivatedWeeks);
    }

    // ── Runway -> Core mixed window ──────────────────────────────────────

    [Fact]
    public async Task RunwayCoreMixed_PartialFinalSlice_ActivatesAtomically()
    {
        // 21-week roadmap: GE=1, Runway=2-9 (8 weeks), Core=10-21.
        // Window 1: GE(1) + Runway(2-4) = 3 Runway weeks used, 5 remain (2-9's tail: 5-9).
        var window1Request = FirstRunwayEntryRequest(totalWeeks: 21, runwayWeeks: 8) with
        {
            LifecycleStates = Lifecycle(21, 0),
            PreviousActivatedWindow = PriorWindow(0),
            GeCheckpointDecision = new LongHorizonCheckpointDecision
            {
                DecisionId = Guid.NewGuid(),
                EvidenceSnapshotId = Guid.NewGuid(),
                Outcome = LongHorizonCheckpointOutcome.GrowthEligible,
                ValidatedLoad = ValidLoad(24, 8),
                ActivationWindowBoundary = (1, 1),
                SafetyPriorityApplied = true,
                PolicyProvenance = "Phase 4K.6/4K.7 (test fixture)",
            },
            GeActivatedWeeks = [GeWeek(1)],
        };
        var window1 = await Runtime().ResolveAndActivateNextWindowAsync(window1Request);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.GeRunwayMixedWindowActivated, window1.Outcome);

        // Window 2: Runway(5-8) -- a full 4-week Runway-only window, leaving exactly week 9 remaining.
        var window2Request = BaseRequest(21, 0) with
        {
            LifecycleStates = window1.LifecycleStates,
            PreviousActivatedWindow = window1.ActivationWindow!,
            ExistingLockedCoreTarget = window1.CoreTargetLock,
            ExistingRunwayPrescription = window1.RunwayPrescription,
        };
        var window2 = await Runtime().ResolveAndActivateNextWindowAsync(window2Request);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, window2.Outcome);
        Assert.Equal(4, window2.NewlyActivatedWeeks.Count);

        // Window 3: Runway(9) + Core(10-12) -- the genuine Runway->Core mixed window.
        var window3Request = BaseRequest(21, 0) with
        {
            LifecycleStates = window2.LifecycleStates,
            PreviousActivatedWindow = window2.ActivationWindow!,
            ExistingLockedCoreTarget = window2.CoreTargetLock,
            ExistingRunwayPrescription = window2.RunwayPrescription,
            AvailableCoreWeeks = [CoreCandidate(10), CoreCandidate(11), CoreCandidate(12)],
        };
        var window3 = await Runtime().ResolveAndActivateNextWindowAsync(window3Request);

        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayCoreMixedWindowActivated, window3.Outcome);
        Assert.Equal(4, window3.NewlyActivatedWeeks.Count);
        Assert.Equal(1, window3.NewlyActivatedWeeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway));
        Assert.Equal(3, window3.NewlyActivatedWeeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.Core));
        Assert.Equal(window1.RunwayPrescription!.PrescriptionId, window3.RunwayPrescription!.PrescriptionId);
    }

    [Fact]
    public async Task RunwayCoreMixed_MissingExistingPrescription_Blocks()
    {
        var request21 = BaseRequest(21, 8) with
        {
            AvailableCoreWeeks = [CoreCandidate(10)],
        };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request21);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.JitWindowBlocked, result.Outcome);
    }

    // ── Core-only window ──────────────────────────────────────────────────

    [Fact]
    public async Task CoreOnly_ActivatesSelectedWeeks_FutureRemainPending()
    {
        var request = BaseRequest(21, 9) with // GE(1)+Runway(8)=9 completed; Core starts at week 10.
        {
            AvailableCoreWeeks = [CoreCandidate(10), CoreCandidate(11), CoreCandidate(12), CoreCandidate(13)],
        };

        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitActivationOutcome.CoreWindowActivated, result.Outcome);
        Assert.Equal(4, result.NewlyActivatedWeeks.Count);
        Assert.Equal(LongHorizonNumericLifecycleState.NumericPending, result.LifecycleStates[14]);
    }

    [Fact]
    public async Task CoreOnly_IncompleteAvailableWeeks_Blocks()
    {
        var request = BaseRequest(21, 9) with
        {
            AvailableCoreWeeks = [CoreCandidate(10)], // only 1 of 4 requested weeks available
        };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.JitWindowBlocked, result.Outcome);
    }

    [Fact]
    public async Task CoreOnly_ActivatedPriorCoreWeeksRemainUnchanged()
    {
        var lifecycle = Lifecycle(21, 9);
        lifecycle[10] = LongHorizonNumericLifecycleState.Completed; // simulate a prior Core week already completed
        var request = BaseRequest(21, 9) with
        {
            LifecycleStates = lifecycle,
            AvailableCoreWeeks = [CoreCandidate(11), CoreCandidate(12), CoreCandidate(13), CoreCandidate(14)],
        };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonNumericLifecycleState.Completed, result.LifecycleStates[10]);
    }

    // ── Atomicity and lifecycle ──────────────────────────────────────────

    [Fact]
    public async Task Blocked_PriorHistoryUnchanged()
    {
        var request = FirstRunwayEntryRequest() with { ValidatedLoad = ValidLoad(30, 8) };
        var before = new Dictionary<int, LongHorizonNumericLifecycleState>(request.LifecycleStates);
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        foreach (var (week, state) in before.Where(kv => kv.Value == LongHorizonNumericLifecycleState.Completed))
        {
            Assert.Equal(state, result.LifecycleStates[week]);
        }
    }

    [Fact]
    public async Task Blocked_HasExactlyOneReason()
    {
        var request = FirstRunwayEntryRequest() with { ValidatedLoad = ValidLoad(30, 8) };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.NotNull(result.AuthoritativeReason);
    }

    [Fact]
    public async Task Success_OnlySelectedPendingWeeksChange()
    {
        var request = FirstRunwayEntryRequest();
        var before = request.LifecycleStates;
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        foreach (var (week, state) in before)
        {
            if (result.NewlyActivatedWeeks.Any(w => w.GlobalWeekNumber == week))
            {
                continue;
            }

            Assert.Equal(state, result.LifecycleStates[week]);
        }
    }

    [Fact]
    public async Task InputRoadmap_IsNotMutated()
    {
        var request = FirstRunwayEntryRequest();
        var originalSegments = request.StructuralRoadmap.Segments;
        await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Same(originalSegments, request.StructuralRoadmap.Segments);
    }

    // ── Determinism ──────────────────────────────────────────────────────

    [Fact]
    public async Task Determinism_IdenticalFirstEntryInput_GivesIdenticalPrescriptionIdentity()
    {
        var request = FirstRunwayEntryRequest();
        var resultA = await Runtime().ResolveAndActivateNextWindowAsync(request);
        var resultB = await Runtime().ResolveAndActivateNextWindowAsync(request);

        Assert.Equal(resultA.RunwayPrescription!.PrescriptionId, resultB.RunwayPrescription!.PrescriptionId);
        Assert.Equal(resultA.CoreTargetLock!.CreatedByDecisionId != Guid.Empty, resultB.CoreTargetLock!.CreatedByDecisionId != Guid.Empty);
    }

    [Fact]
    public async Task Determinism_ChangedEvidenceBeforeRunwayEntry_ChangesTargetLockContext()
    {
        var requestA = FirstRunwayEntryRequest(weekly: 24, longRun: 8);
        var requestB = FirstRunwayEntryRequest(weekly: 20, longRun: 7);
        var resultA = await Runtime().ResolveAndActivateNextWindowAsync(requestA);
        var resultB = await Runtime().ResolveAndActivateNextWindowAsync(requestB);

        Assert.NotEqual(resultA.RunwayPrescription!.PrescriptionId, resultB.RunwayPrescription!.PrescriptionId);
    }

    [Fact]
    public async Task Determinism_NoRandomGuidOrClock_RepeatedCallsMatch()
    {
        var request = FirstRunwayEntryRequest();
        var resultA = await Runtime().ResolveAndActivateNextWindowAsync(request);
        await Task.Delay(5);
        var resultB = await Runtime().ResolveAndActivateNextWindowAsync(request);

        Assert.Equal(resultA.ActivationWindow!.WindowId, resultB.ActivationWindow!.WindowId);
    }

    // ── Bounded execution proof ──────────────────────────────────────────

    [Fact]
    public async Task BoundedExecution_OnlySelectedRunwayWeeksActivate_RestRemainPending()
    {
        var request = FirstRunwayEntryRequest();
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);

        var runwaySegment = request.StructuralRoadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway);
        for (var week = runwaySegment.StartGlobalWeek + 4; week <= runwaySegment.EndGlobalWeek; week++)
        {
            Assert.Equal(LongHorizonNumericLifecycleState.NumericPending, result.LifecycleStates[week]);
        }
    }

    [Fact]
    public async Task BoundedExecution_NoSecondWindowActivatesPerInvocation()
    {
        var request = FirstRunwayEntryRequest();
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(4, result.NewlyActivatedWeeks.Count);
    }

    [Fact]
    public void BoundedExecution_NoPublicDtoOrPersistenceTypeInNamespace()
    {
        var namespaceTypes = typeof(LongHorizonRollingJitActivationRuntime).Assembly.GetTypes()
            .Where(t => t.Namespace == "RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation");
        Assert.DoesNotContain(namespaceTypes, t => t.Name.Contains("Dto", StringComparison.Ordinal) || t.Name.Contains("Controller", StringComparison.Ordinal) || t.Name.Contains("DbContext", StringComparison.Ordinal));
    }

    [Fact]
    public void NoPublicDiRegistration_RuntimeTypeIsInternal()
    {
        Assert.False(typeof(LongHorizonRollingJitActivationRuntime).IsPublic);
        Assert.False(typeof(ILongHorizonRollingJitActivationRuntime).IsPublic);
    }

    // ── Profiles ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConsistencyNeeded_Profile_Works()
    {
        var request = FirstRunwayEntryRequest();
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, result.Outcome);
    }

    [Fact]
    public async Task CoreEntryReady_Profile_Works()
    {
        var request = FirstRunwayEntryRequest() with { ReadinessProfile = ReadinessProfile.CoreEntryReady, StructuralRoadmap = BuildRoadmap(28, ReadinessProfile.CoreEntryReady) };
        var result = await Runtime().ResolveAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, result.Outcome);
    }
}
