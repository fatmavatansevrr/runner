using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.8C — tests for <see cref="LongHorizonRollingJitCompositionOrchestrator"/>.
/// Exercises the real, unchanged production authorities directly: the real
/// on-disk catalog (<c>plan-catalog/catalog</c>), the real four condition
/// resolvers, and the real <c>TenKPreparationRunwayDarkOrchestrator</c>
/// (via its own production factory). Dark and unwired: not called from any
/// production/live request path.
/// </summary>
public sealed class LongHorizonRollingJitCompositionOrchestratorTests
{
    internal static readonly DateOnly PlanStart = new(2026, 8, 3);
    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    private static readonly Lazy<Task<PlanCatalogCandidateSummary>> CandidateTask = new(LoadCandidateAsync);

    private static async Task<PlanCatalogCandidateSummary> LoadCandidateAsync()
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot });
        var loader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
    }

    private static LongHorizonStructuralRoadmap BuildRoadmap(int totalWeeks)
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
            Profile = ReadinessProfile.ConsistencyNeeded,
            StructuralStatus = "Confirmed",
        };
    }

    internal static Dictionary<int, LongHorizonNumericLifecycleState> Lifecycle(int totalWeeks, int completedThrough)
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

    internal static RollingNumericActivationWindow PriorWindow(int endGlobalWeek) => new()
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

    private static LongHorizonCheckpointEvidenceSnapshot EvidenceSnapshot(LongHorizonRollingJitCompositionRequestBuilderState state) => new()
    {
        CheckpointId = Guid.NewGuid(),
        CheckpointDate = state.CheckpointDate,
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
        Availability = state.Availability,
        SafetyState = LongHorizonSafetyState.Clear,
        CurrentSegment = LongHorizonStructuralSegmentType.GeneralEndurance,
        EvidenceSourceMetadata = LongHorizonEvidenceAuthorityCatalog.RunwayRollingWeeklyLoadAuthority,
    };

    private sealed record LongHorizonRollingJitCompositionRequestBuilderState(DateOnly CheckpointDate, IReadOnlyList<DayOfWeek> Availability);

    internal static async Task<LongHorizonRollingJitCompositionRequest> FirstRunwayEntryRequestAsync(
        int totalWeeks = 28, double weekly = 24, double longRun = 8)
    {
        var geWeeks = totalWeeks - 20;
        var roadmap = BuildRoadmap(totalWeeks);
        var checkpointDate = PlanStart.AddDays(geWeeks * 7);
        var availability = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var state = new LongHorizonRollingJitCompositionRequestBuilderState(checkpointDate, availability);

        return new LongHorizonRollingJitCompositionRequest
        {
            StructuralRoadmap = roadmap,
            LifecycleStates = Lifecycle(totalWeeks, geWeeks),
            PreviousActivatedWindow = PriorWindow(geWeeks),
            EvidenceSnapshot = EvidenceSnapshot(state),
            ValidatedLoad = ValidLoad(weekly, longRun),
            ExactCompletedFrequency = 4,
            PreviousContextVersion = LongHorizonContextVersion.Initial(),
            ReadinessProfile = ReadinessProfile.ConsistencyNeeded,
            CurrentAvailability = availability,
            PreferredDays = availability,
            LongRunDay = DayOfWeek.Sunday,
            CheckpointDate = checkpointDate,
            PlanStartDate = PlanStart,
            RaceDate = roadmap.RaceDate,
            TargetFinishTimeSeconds = 3480,
            TargetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
            RecentRace = null,
            SafetyState = LongHorizonSafetyState.Clear,
            CatalogRootPath = CatalogRoot,
            Candidate = await CandidateTask.Value,
        };
    }

    internal static LongHorizonRollingJitCompositionOrchestrator Orchestrator() => new();

    // ── Real authority proof (Part 19) ──────────────────────────────────

    [Fact]
    public async Task RealCoreGenerator_IsInvoked_AtFirstRunwayEntry()
    {
        var request = await FirstRunwayEntryRequestAsync();
        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded, result.Outcome);
        Assert.NotNull(result.RealCompositionResult);
        Assert.True(result.RealCompositionResult!.IsSuccess);
        Assert.Contains("TenKPreparationRunwayDarkOrchestrator", result.CoreGenerationProvenance);
    }

    [Fact]
    public async Task RealConditionResolutionService_IsInvoked()
    {
        var request = await FirstRunwayEntryRequestAsync();
        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(request);

        Assert.NotNull(result.ResolvedConditionResults);
        Assert.Contains(result.ResolvedConditionResults!, r => r.ConditionType == "PACE_SOURCE_IN");
        Assert.Contains(result.ResolvedConditionResults, r => r.ConditionType == "GOAL_FEASIBILITY_IN");
        Assert.Contains(result.ResolvedConditionResults, r => r.ConditionType == CoreEntryReadinessResolver.ConditionTypeValue);
    }

    [Fact]
    public async Task ExtractedCoreWeekOneTarget_ComesFromRealGeneratedResult()
    {
        var request = await FirstRunwayEntryRequestAsync();
        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(request);

        Assert.NotNull(result.ExtractedCoreWeekOneTarget);
        var realWeekOne = result.RealCompositionResult!.CoreResult!.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan
            .WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).First();
        Assert.Equal(realWeekOne.PlannedWeeklyVolumeKm, result.ExtractedCoreWeekOneTarget!.WeeklyVolumeKm);
    }

    [Fact]
    public void CallerCannotInjectCoreTarget_RequestHasNoSuchField()
    {
        var requestType = typeof(LongHorizonRollingJitCompositionRequest);
        Assert.DoesNotContain(requestType.GetProperties(), p => p.Name.Contains("CoreWeekOneTarget", StringComparison.Ordinal));
    }

    [Fact]
    public void CallerCannotInjectCoreWeeks_RequestHasNoSuchField()
    {
        var requestType = typeof(LongHorizonRollingJitCompositionRequest);
        Assert.DoesNotContain(requestType.GetProperties(), p => p.Name.Contains("AvailableCoreWeeks", StringComparison.Ordinal));
    }

    [Fact]
    public void CallerCannotInjectConditionResults_RequestHasNoSuchField()
    {
        var requestType = typeof(LongHorizonRollingJitCompositionRequest);
        Assert.DoesNotContain(requestType.GetProperties(), p => p.Name.Contains("ConditionResults", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BoundedCoreSelection_WeeksAreExactReferencesFromRealGeneratorOutput()
    {
        var request = await FirstRunwayEntryRequestAsync();
        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(request);

        Assert.NotNull(result.BoundedCoreSelection);
        var realWeeks = result.RealCompositionResult!.CoreResult!.PrescriptionResult.FinalPrescribedPlan.Weeks;
        foreach (var selected in result.BoundedCoreSelection!.SelectedWeeks)
        {
            var localWeek = selected.GlobalWeekNumber - request.StructuralRoadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.Core).StartGlobalWeek + 1;
            var real = realWeeks.Single(w => w.WeekNumber == localWeek);
            Assert.Equal(real.PlannedWeeklyVolumeKm, selected.WeeklyVolumeKm);
        }
    }

    [Fact]
    public async Task Phase4K8ActivationRuntime_IsInvokedOnlyAfterCompositionSucceeds()
    {
        var request = await FirstRunwayEntryRequestAsync();
        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(request);

        Assert.NotNull(result.ActivationResult);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, result.ActivationResult!.Outcome);
    }

    // ── End-to-end mixed windows ─────────────────────────────────────────

    [Fact]
    public async Task GeRunwayMixed_EndToEnd_Succeeds()
    {
        var totalWeeks = 28;
        var geWeeks = 8;
        var roadmap = BuildRoadmap(totalWeeks);
        var checkpointDate = PlanStart.AddDays((geWeeks - 2) * 7);
        var availability = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var request = new LongHorizonRollingJitCompositionRequest
        {
            StructuralRoadmap = roadmap,
            LifecycleStates = Lifecycle(totalWeeks, geWeeks - 2),
            PreviousActivatedWindow = PriorWindow(geWeeks - 2),
            EvidenceSnapshot = EvidenceSnapshot(new LongHorizonRollingJitCompositionRequestBuilderState(checkpointDate, availability)),
            ValidatedLoad = ValidLoad(24, 8),
            ExactCompletedFrequency = 4,
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
            PreviousContextVersion = LongHorizonContextVersion.Initial(),
            ReadinessProfile = ReadinessProfile.ConsistencyNeeded,
            CurrentAvailability = availability,
            PreferredDays = availability,
            LongRunDay = DayOfWeek.Sunday,
            CheckpointDate = checkpointDate,
            PlanStartDate = PlanStart,
            RaceDate = roadmap.RaceDate,
            TargetFinishTimeSeconds = 3480,
            TargetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
            SafetyState = LongHorizonSafetyState.Clear,
            CatalogRootPath = CatalogRoot,
            Candidate = await CandidateTask.Value,
        };

        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded, result.Outcome);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.GeRunwayMixedWindowActivated, result.ActivationResult!.Outcome);
        Assert.Equal(2, result.ActivationResult.NewlyActivatedWeeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance));
        Assert.Equal(2, result.ActivationResult.NewlyActivatedWeeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway));
    }

    private static ActivatedNumericWeek GeWeek(int globalWeek, double weeklyVolumeKm = 24, double longRunKm = 8)
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

    // ── Continuation (no Core regeneration) ─────────────────────────────

    [Fact]
    public async Task RunwayOnlyContinuation_DoesNotRegenerateCoreOrRunway()
    {
        var firstRequest = await FirstRunwayEntryRequestAsync();
        var firstResult = await Orchestrator().ComposeAndActivateNextWindowAsync(firstRequest);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, firstResult.ActivationResult!.Outcome);

        var continuation = firstRequest with
        {
            LifecycleStates = firstResult.ActivationResult.LifecycleStates,
            PreviousActivatedWindow = firstResult.ActivationResult.ActivationWindow!,
            ExistingLockedCoreTarget = firstResult.ActivationResult.CoreTargetLock,
            ExistingRunwayPrescription = firstResult.ActivationResult.RunwayPrescription,
            ExistingRunwayCalendarProjection = firstResult.FullRunwayCalendarProjection,
            GeCheckpointDecision = null,
            GeActivatedWeeks = null,
            CheckpointDate = firstRequest.CheckpointDate.AddDays(28),
        };

        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(continuation);

        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded, result.Outcome);
        Assert.Null(result.RealCompositionResult);
        Assert.Contains("no regeneration", result.CoreGenerationProvenance);
    }

    // ── Blocked scenarios ────────────────────────────────────────────────

    [Fact]
    public async Task SafetyUnresolved_BlocksBeforeConditionResolution()
    {
        var request = (await FirstRunwayEntryRequestAsync()) with { SafetyState = LongHorizonSafetyState.UnresolvedSafetyCritical };
        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionBlocked, result.Outcome);
        Assert.Equal(LongHorizonReasonCode.SafetyReassessmentRequired, result.AuthoritativeReason);
        Assert.Null(result.ResolvedConditionResults);
    }

    [Fact]
    public async Task InvalidatedLoad_Blocks()
    {
        var invalid = ValidLoad(24, 8) with { WeeklyVolumeKm = null, LongRunKm = null, ValidationStatus = LongHorizonValidationStatus.Unavailable };
        var request = (await FirstRunwayEntryRequestAsync()) with { ValidatedLoad = invalid };
        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(request);

        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionBlocked, result.Outcome);
        Assert.Equal(LongHorizonJitReasonCode.JitValidatedLoadUnavailable, result.AuthoritativeReason!.Value.JitReason);
    }

    [Fact]
    public async Task ExtremeEvidence_ProducesInternallyConsistentResult()
    {
        // Core's Week-1 target is itself derived from the SAME real, unchanged pipeline fed this
        // same evidence (Phase 4K.5A's approved design) -- so unlike Phase 4K.8's own synthetic
        // direction-guard tests (which fix the target independently of the starting evidence),
        // an "above target" scenario cannot be forced with a single scalar here. This test instead
        // proves the composition/blocked contract remains internally consistent under extreme,
        // real-pipeline-processed evidence, whichever real outcome results.
        var request = await FirstRunwayEntryRequestAsync(weekly: 40, longRun: 8);
        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(request);

        if (result.Outcome == LongHorizonRollingJitCompositionOutcome.CompositionBlocked)
        {
            Assert.NotNull(result.AuthoritativeReason);
            Assert.Null(result.ActivationResult);
            Assert.Null(result.RealCompositionResult);
        }
        else
        {
            Assert.NotNull(result.ActivationResult);
            Assert.NotEmpty(result.ActivationResult!.NewlyActivatedWeeks);
        }
    }

    [Fact]
    public async Task Blocked_NeverExposesRealCompositionResult()
    {
        var request = (await FirstRunwayEntryRequestAsync()) with { SafetyState = LongHorizonSafetyState.UnresolvedSafetyCritical };
        var result = await Orchestrator().ComposeAndActivateNextWindowAsync(request);
        Assert.Null(result.RealCompositionResult);
    }

    // ── Determinism ──────────────────────────────────────────────────────

    [Fact]
    public async Task Determinism_IdenticalInput_ProducesConsistentCoreTarget()
    {
        var request = await FirstRunwayEntryRequestAsync();
        var resultA = await Orchestrator().ComposeAndActivateNextWindowAsync(request);
        var resultB = await Orchestrator().ComposeAndActivateNextWindowAsync(request);

        Assert.Equal(resultA.ExtractedCoreWeekOneTarget!.WeeklyVolumeKm, resultB.ExtractedCoreWeekOneTarget!.WeeklyVolumeKm);
        Assert.Equal(resultA.ExtractedCoreWeekOneTarget.LongRunDistanceKm, resultB.ExtractedCoreWeekOneTarget!.LongRunDistanceKm);
    }

    [Fact]
    public async Task Determinism_ChangedEvidence_ChangesCoreTargetContext()
    {
        var requestA = await FirstRunwayEntryRequestAsync(weekly: 24, longRun: 8);
        var requestB = await FirstRunwayEntryRequestAsync(weekly: 22, longRun: 7.5);
        var resultA = await Orchestrator().ComposeAndActivateNextWindowAsync(requestA);
        var resultB = await Orchestrator().ComposeAndActivateNextWindowAsync(requestB);

        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded, resultA.Outcome);
        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded, resultB.Outcome);

        // Different starting evidence produces a different deterministic Runway prescription identity.
        Assert.NotEqual(resultA.ActivationResult!.RunwayPrescription!.PrescriptionId, resultB.ActivationResult!.RunwayPrescription!.PrescriptionId);
    }

    // ── Integration boundaries ───────────────────────────────────────────

    [Fact]
    public void NoPublicDiRegistration_OrchestratorTypeIsInternal()
    {
        Assert.False(typeof(LongHorizonRollingJitCompositionOrchestrator).IsPublic);
        Assert.False(typeof(ILongHorizonRollingJitCompositionOrchestrator).IsPublic);
    }

    [Fact]
    public void RollingActivationNamespace_ReferencesNoControllerOrDbContext()
    {
        var namespaceTypes = typeof(LongHorizonRollingJitCompositionOrchestrator).Assembly.GetTypes()
            .Where(t => t.Namespace == "RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation");
        Assert.DoesNotContain(namespaceTypes, t => t.Name.Contains("Controller", StringComparison.Ordinal) || t.Name.Contains("DbContext", StringComparison.Ordinal));
    }
}
