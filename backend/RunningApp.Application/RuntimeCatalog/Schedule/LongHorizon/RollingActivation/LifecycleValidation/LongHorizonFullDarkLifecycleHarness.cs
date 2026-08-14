using System.Security.Cryptography;
using System.Text;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.9 diagnostic-only orchestrator. It owns no numeric, evidence,
/// window-selection, Runway, Core, or calendar formula; it repeatedly invokes
/// the existing 4K.6, 4K.7, and 4K.8C production runtimes and accumulates their
/// immutable outputs for validation.
/// </summary>
internal sealed class LongHorizonFullDarkLifecycleHarness : ILongHorizonFullDarkLifecycleHarness
{
    private readonly ILongHorizonRollingInitialActivationRuntime _initialRuntime;
    private readonly ILongHorizonRollingCheckpointRuntime _checkpointRuntime;
    private readonly ILongHorizonRollingJitCompositionOrchestrator _jitComposition;
    private readonly ILongHorizonBlockedActivationRetryService _retryService;

    public LongHorizonFullDarkLifecycleHarness(
        ILongHorizonRollingInitialActivationRuntime? initialRuntime = null,
        ILongHorizonRollingCheckpointRuntime? checkpointRuntime = null,
        ILongHorizonRollingJitCompositionOrchestrator? jitComposition = null,
        ILongHorizonBlockedActivationRetryService? retryService = null)
    {
        _initialRuntime = initialRuntime ?? new LongHorizonRollingInitialActivationRuntime();
        _checkpointRuntime = checkpointRuntime ?? new LongHorizonRollingCheckpointRuntime();
        _jitComposition = jitComposition ?? new LongHorizonRollingJitCompositionOrchestrator();
        _retryService = retryService ?? new LongHorizonBlockedActivationRetryService();
    }

    public async Task<LongHorizonFullDarkLifecycleValidationResult> RunLifecycleAsync(
        LongHorizonLifecycleScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ValidateScenario(scenario);
        var validationStages = new List<string> { "ScenarioValidation" };
        var snapshots = new List<LongHorizonFullDarkLifecycleState>();
        var audit = new List<LongHorizonLifecycleAuditEvent>();

        var initial = await _initialRuntime.BuildInitialActivationAsync(new LongHorizonRollingInitialActivationRequest
        {
            CompositionDecision = LongHorizonCompositionResolver.Resolve(
                RaceHorizonPolicy.Decide(scenario.StartDate, scenario.RaceDate), scenario.ReadinessProfile),
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            StartDate = scenario.StartDate,
            RaceDate = scenario.RaceDate,
            OnboardingBaseline = scenario.InitialOnboardingEvidence,
            PreferredDays = scenario.PreferredDays,
            LongRunDay = scenario.LongRunDay,
            CatalogRoot = scenario.CatalogRootPath,
            WorkoutLoader = new CatalogWorkoutDefinitionLoader(
                Microsoft.Extensions.Options.Options.Create(new RunningApp.Application.RuntimeCatalog.PlanCatalogOptions
                {
                    CatalogRootPath = scenario.CatalogRootPath,
                })),
        }, cancellationToken);

        if (initial.Status != LongHorizonRollingInitialActivationStatus.Approved)
            return Failed($"Initial activation blocked: {initial.Failure?.Code}", snapshots, validationStages);

        audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.StructuralRoadmapCreated, null,
            initial.ContextVersion, initial.InitialActivationContext?.DecisionId, "Phase4K.6 structural materializer", "Created"));
        audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.InitialWindowActivated,
            Range(initial.ActivationWindow!), initial.ContextVersion, initial.InitialActivationContext?.DecisionId,
            "Phase4K.6 initial activation runtime", "Activated"));

        var state = new LongHorizonFullDarkLifecycleState
        {
            StructuralRoadmap = initial.StructuralRoadmap!,
            StructuralSkeleton = initial.StructuralSkeleton!,
            LifecycleStates = initial.ActivatedNumericWeeks.Concat(initial.PendingNumericWeeks)
                .ToDictionary(week => week.GlobalWeekNumber, week => week.LifecycleState),
            ActivatedWeeks = initial.ActivatedNumericWeeks.ToDictionary(week => week.GlobalWeekNumber),
            CurrentWindow = initial.ActivationWindow!,
            ContextVersion = initial.ContextVersion!,
            CheckpointDecisions = [],
            CoreContextIds = [],
            AuditEvents = audit.ToList(),
            ActivationInvocationCount = 1,
            FullRunwayMaterializationCount = 0,
            LastCheckpointDate = scenario.StartDate,
        };
        snapshots.Add(state);
        validationStages.Add("InitialActivation");

        var retryIndex = 0;
        var retryRecovered = false;
        // At least one week advances per successful activation. A block can add
        // at most one explicit restore plus one re-evaluation per supplied retry.
        var maximumIterations = scenario.TotalWeeks + (scenario.RetryEvidence.Count * 2) + 1;
        for (var iteration = 0; iteration < maximumIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evidence = scenario.EvidenceByActivationOrdinal.TryGetValue(state.ActivationInvocationCount, out var supplied)
                ? supplied
                : throw new LongHorizonCheckpointDecisionInvalidException(
                    $"Scenario lacks explicit evidence for activation ordinal {state.ActivationInvocationCount}.");
            var applied = ApplyWindowOutcomes(state, evidence, scenario);
            state = applied.State;
            snapshots.Add(state);

            if (!state.LifecycleStates.Values.Contains(LongHorizonNumericLifecycleState.NumericPending)
                && !state.LifecycleStates.Values.Contains(LongHorizonNumericLifecycleState.NumericActivationBlocked))
            {
                LongHorizonFinalLifecycleValidator.Validate(state);
                audit = state.AuditEvents.ToList();
                audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.LifecycleCompleted,
                    (1, scenario.TotalWeeks), state.ContextVersion, null,
                    "LongHorizonFinalLifecycleValidator", "CompletedSuccessfully"));
                state = state with { AuditEvents = audit };
                snapshots.Add(state);
                validationStages.Add("FinalLifecycleValidation");
                return new LongHorizonFullDarkLifecycleValidationResult
                {
                    Outcome = retryRecovered
                        ? LongHorizonFullDarkLifecycleOutcome.RetryRecoveredAndCompleted
                        : LongHorizonFullDarkLifecycleOutcome.CompletedSuccessfully,
                    FinalState = state,
                    StateSnapshots = snapshots,
                    ValidationStages = validationStages,
                };
            }

            var checkpointRequest = BuildCheckpointRequest(state, applied.EvidenceRows, evidence, scenario);
            var checkpoint = await _checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(checkpointRequest, cancellationToken);
            state = RecordCheckpoint(state, checkpoint, scenario);
            validationStages.Add("Phase4K.7CheckpointRuntime");

            if (checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked)
            {
                snapshots.Add(state);
                if (retryIndex >= scenario.RetryEvidence.Count)
                {
                    var expected = scenario.ExpectedBlockedActivationOrdinal == state.ActivationInvocationCount;
                    return new LongHorizonFullDarkLifecycleValidationResult
                    {
                        Outcome = expected ? LongHorizonFullDarkLifecycleOutcome.BlockedAsExpected : LongHorizonFullDarkLifecycleOutcome.ValidationFailed,
                        FinalState = state,
                        AuthoritativeReason = checkpoint.AuthoritativeReason,
                        InternalDiagnostic = expected ? null : "Unexpected checkpoint block.",
                        StateSnapshots = snapshots,
                        ValidationStages = validationStages,
                    };
                }

                var retryEvidence = scenario.RetryEvidence[retryIndex++];
                var blockedWeeks = checkpoint.LifecycleStates
                    .Where(pair => pair.Value == LongHorizonNumericLifecycleState.NumericActivationBlocked
                        && state.LifecycleStates.GetValueOrDefault(pair.Key) != LongHorizonNumericLifecycleState.NumericActivationBlocked)
                    .Select(pair => pair.Key).OrderBy(x => x).ToList();
                var priorDecisionId = checkpoint.CheckpointDecision!.DecisionId;
                var retry = _retryService.RestorePendingEligibility(new LongHorizonBlockedActivationRetryRequest
                {
                    LifecycleStates = checkpoint.LifecycleStates,
                    BlockedGlobalWeeks = blockedWeeks,
                    PreviousCheckpointDate = evidence.CheckpointDate,
                    RetryCheckpointDate = retryEvidence.CheckpointDate,
                    PreviousDecisionId = priorDecisionId,
                    PreviousEvidenceIdentity = EvidenceIdentity(evidence),
                    RetryEvidenceIdentity = EvidenceIdentity(retryEvidence),
                });
                audit = state.AuditEvents.ToList();
                audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.BlockRetryRequested,
                    blockedWeeks.Count == 0 ? null : (blockedWeeks[0], blockedWeeks[^1]), state.ContextVersion,
                    retry.RetryDecisionId, "Explicit Phase4K.9 retry transition", "Requested"));
                audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.BlockRestoredToPending,
                    (blockedWeeks[0], blockedWeeks[^1]), state.ContextVersion, retry.RetryDecisionId,
                    "LongHorizonBlockedActivationRetryService", "NumericPending"));
                state = state with
                {
                    LifecycleStates = retry.LifecycleStates,
                    AuditEvents = audit,
                    LastCheckpointDate = retryEvidence.CheckpointDate,
                };
                snapshots.Add(state);
                retryRecovered = true;

                var retryRows = BuildEvidenceRows(state.CurrentWindow, retryEvidence, scenario.ScenarioId);
                checkpoint = await _checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(
                    BuildCheckpointRequest(state, retryRows, retryEvidence, scenario), cancellationToken);
                state = RecordCheckpoint(state, checkpoint, scenario);
                validationStages.Add("ExplicitRetryReevaluation");
                if (checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked)
                {
                    snapshots.Add(state);
                    return new LongHorizonFullDarkLifecycleValidationResult
                    {
                        Outcome = LongHorizonFullDarkLifecycleOutcome.BlockedAsExpected,
                        FinalState = state,
                        AuthoritativeReason = checkpoint.AuthoritativeReason,
                        StateSnapshots = snapshots,
                        ValidationStages = validationStages,
                    };
                }
            }

            var geEnd = state.StructuralRoadmap.GeneralEnduranceWeeks;
            if (checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated
                && checkpoint.ActivationWindow!.EndGlobalWeek == geEnd
                && checkpoint.ActivationWindow.ActualWindowSizeWeeks < checkpoint.ActivationWindow.RequestedWindowSizeWeeks)
            {
                state = await ComposeJitAsync(state, applied.State.LifecycleStates, checkpoint, scenario, cancellationToken);
                validationStages.Add("Phase4K.8CGeRunwayMixedComposition");
            }
            else if (checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated)
            {
                state = AcceptGeActivation(state, checkpoint, scenario);
                validationStages.Add("Phase4K.7GeActivationAccepted");
            }
            else
            {
                state = await ComposeJitAsync(state, state.LifecycleStates, checkpoint, scenario, cancellationToken);
                validationStages.Add("Phase4K.8CComposition");
            }

            snapshots.Add(state);
        }

        return Failed($"Derived loop guard {maximumIterations} was exhausted without completion.", snapshots, validationStages);
    }

    private async Task<LongHorizonFullDarkLifecycleState> ComposeJitAsync(
        LongHorizonFullDarkLifecycleState state,
        IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> lifecycleForAtomicSelection,
        LongHorizonRollingCheckpointResult checkpoint,
        LongHorizonLifecycleScenario scenario,
        CancellationToken cancellationToken)
    {
        var effectiveValidatedLoad = checkpoint.ValidatedLoad
            ?? state.LatestValidatedLoad
            ?? scenario.InitialPriorValidatedAnchor?.Load;
        if (checkpoint.EvidenceSnapshot is null || effectiveValidatedLoad is null)
            throw new LongHorizonJitContextInvalidException("JIT composition requires the real checkpoint snapshot and a current or explicitly supplied fresh prior validated load.");

        var result = await _jitComposition.ComposeAndActivateNextWindowAsync(new LongHorizonRollingJitCompositionRequest
        {
            StructuralRoadmap = state.StructuralRoadmap,
            LifecycleStates = lifecycleForAtomicSelection,
            PreviousActivatedWindow = state.CurrentWindow,
            EvidenceSnapshot = checkpoint.EvidenceSnapshot,
            ValidatedLoad = effectiveValidatedLoad,
            ExactCompletedFrequency = checkpoint.EvidenceSnapshot.CompletedRunsCount == 0 ? null : 4,
            GeCheckpointDecision = checkpoint.CheckpointDecision,
            GeActivatedWeeks = checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated
                ? checkpoint.NewlyActivatedWeeks : null,
            PreviousContextVersion = checkpoint.ContextVersion,
            ReadinessProfile = scenario.ReadinessProfile,
            CurrentAvailability = checkpoint.EvidenceSnapshot.Availability,
            PreferredDays = scenario.PreferredDays,
            LongRunDay = scenario.LongRunDay,
            CheckpointDate = checkpoint.EvidenceSnapshot.CheckpointDate,
            PlanStartDate = scenario.StartDate,
            RaceDate = scenario.RaceDate,
            TargetFinishTimeSeconds = scenario.TargetFinishTimeSeconds,
            TargetFinishTimeSource = scenario.TargetFinishTimeSource,
            RecentRace = scenario.RecentRace,
            SafetyState = checkpoint.EvidenceSnapshot.SafetyState,
            ExistingLockedCoreTarget = state.RunwayTargetLock,
            ExistingRunwayPrescription = state.RunwayPrescription,
            ExistingRunwayCalendarProjection = state.RunwayCalendarProjection,
            CatalogRootPath = scenario.CatalogRootPath,
            Candidate = scenario.Candidate,
        }, cancellationToken);

        if (result.Outcome != LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded)
            throw new LongHorizonJitContextInvalidException(
                $"Canonical lifecycle JIT composition blocked: {result.AuthoritativeReason}; {result.InternalDiagnostic}");

        var activation = result.ActivationResult!;
        var activated = new Dictionary<int, ActivatedNumericWeek>(state.ActivatedWeeks);
        foreach (var week in activation.NewlyActivatedWeeks)
            activated[week.GlobalWeekNumber] = week;
        var audit = state.AuditEvents.ToList();
        if (state.RunwayPrescription is null && activation.RunwayPrescription is not null)
        {
            audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.RunwayTargetLocked,
                activation.CoreTargetLock!.LockedForActivatedRunwayWeekRange, activation.ContextVersion,
                activation.CoreTargetLock.CreatedByDecisionId, "Phase4K.8 target-lock authority", "Locked"));
            audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.RunwayPrescriptionCreated,
                (activation.RunwayPrescription.StartGlobalWeek, activation.RunwayPrescription.EndGlobalWeek), activation.ContextVersion,
                null, "Phase4K.8B immutable full prescription", "CreatedOnce"));
        }
        if (activation.RunwaySlice is not null)
            audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.RunwaySliceActivated,
                (activation.RunwaySlice.RequestedStartGlobalWeek, activation.RunwaySlice.RequestedEndGlobalWeek), activation.ContextVersion,
                null, "Phase4K.8 bounded slice", "Activated"));
        if (result.BoundedCoreSelection is not null)
            audit.Add(Event(scenario, state.CoreContextIds.Count == 0
                    ? LongHorizonLifecycleAuditEventType.CoreContextCreated
                    : LongHorizonLifecycleAuditEventType.CoreContextRefreshed,
                (result.BoundedCoreSelection.SelectedStartGlobalWeek, result.BoundedCoreSelection.SelectedEndGlobalWeek),
                result.BoundedCoreSelection.CoreContextVersion, result.BoundedCoreSelection.CoreContextId,
                "Phase4K.8C real Core generation", "FutureOnly"));
        audit.Add(Event(scenario,
            activation.Outcome == LongHorizonRollingJitActivationOutcome.CoreWindowActivated
                ? LongHorizonLifecycleAuditEventType.CoreWindowActivated
                : LongHorizonLifecycleAuditEventType.MixedWindowActivated,
            Range(activation.ActivationWindow!), activation.ContextVersion, activation.ActivationWindow!.CheckpointDecisionId,
            "Phase4K.8 activation runtime", activation.Outcome.ToString()));
        audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.CalendarProjectionAligned,
            Range(activation.ActivationWindow!), activation.ContextVersion, result.CalendarProjectionId,
            "Phase4K.8D real calendar projection", "Aligned"));

        return state with
        {
            LifecycleStates = new Dictionary<int, LongHorizonNumericLifecycleState>(activation.LifecycleStates),
            ActivatedWeeks = activated,
            CurrentWindow = activation.ActivationWindow!,
            ContextVersion = result.ContextVersion,
            LatestEvidenceSnapshot = checkpoint.EvidenceSnapshot,
            LatestValidatedLoad = effectiveValidatedLoad,
            RunwayTargetLock = activation.CoreTargetLock ?? state.RunwayTargetLock,
            RunwayPrescription = activation.RunwayPrescription ?? state.RunwayPrescription,
            RunwayCalendarProjection = result.FullRunwayCalendarProjection ?? state.RunwayCalendarProjection,
            CoreContextIds = result.BoundedCoreSelection is null
                ? state.CoreContextIds.ToList()
                : state.CoreContextIds.Append(result.BoundedCoreSelection.CoreContextId).ToList(),
            AuditEvents = audit,
            ActivationInvocationCount = state.ActivationInvocationCount + 1,
            FullRunwayMaterializationCount = state.FullRunwayMaterializationCount
                + (state.RunwayPrescription is null && activation.RunwayPrescription is not null ? 1 : 0),
            LastCheckpointDate = checkpoint.EvidenceSnapshot.CheckpointDate,
        };
    }

    private static LongHorizonFullDarkLifecycleState AcceptGeActivation(
        LongHorizonFullDarkLifecycleState state,
        LongHorizonRollingCheckpointResult checkpoint,
        LongHorizonLifecycleScenario scenario)
    {
        var activated = new Dictionary<int, ActivatedNumericWeek>(state.ActivatedWeeks);
        foreach (var week in checkpoint.NewlyActivatedWeeks)
            activated[week.GlobalWeekNumber] = week;
        var audit = state.AuditEvents.ToList();
        audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.GeWindowActivated,
            Range(checkpoint.ActivationWindow!), checkpoint.ContextVersion, checkpoint.CheckpointDecision?.DecisionId,
            "Phase4K.7 checkpoint runtime", checkpoint.CheckpointDecision!.Outcome.ToString()));
        return state with
        {
            LifecycleStates = new Dictionary<int, LongHorizonNumericLifecycleState>(checkpoint.LifecycleStates),
            ActivatedWeeks = activated,
            CurrentWindow = checkpoint.ActivationWindow!,
            ContextVersion = checkpoint.ContextVersion,
            LatestEvidenceSnapshot = checkpoint.EvidenceSnapshot,
            LatestValidatedLoad = checkpoint.ValidatedLoad,
            AuditEvents = audit,
            ActivationInvocationCount = state.ActivationInvocationCount + 1,
            LastCheckpointDate = checkpoint.EvidenceSnapshot!.CheckpointDate,
        };
    }

    private static LongHorizonFullDarkLifecycleState RecordCheckpoint(
        LongHorizonFullDarkLifecycleState state,
        LongHorizonRollingCheckpointResult checkpoint,
        LongHorizonLifecycleScenario scenario)
    {
        var audit = state.AuditEvents.ToList();
        if (checkpoint.EvidenceSnapshot is not null)
            audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.CheckpointSnapshotCreated,
                (checkpoint.EvidenceSnapshot.ActivatedWindowStartWeek, checkpoint.EvidenceSnapshot.ActivatedWindowEndWeek),
                checkpoint.ContextVersion, checkpoint.EvidenceSnapshot.CheckpointId,
                "Phase4K.7 TrainingDay evidence aggregator", "Created"));
        if (checkpoint.ValidatedLoad is not null)
            audit.Add(Event(scenario, LongHorizonLifecycleAuditEventType.ValidatedLoadCreated,
                (checkpoint.ValidatedLoad.EvidenceWindowStartWeek, checkpoint.ValidatedLoad.EvidenceWindowEndWeek),
                checkpoint.ContextVersion, checkpoint.CheckpointDecision?.DecisionId,
                "Phase4K.7 validated-load authority", "Created"));
        if (checkpoint.CheckpointDecision is not null)
        {
            var type = checkpoint.CheckpointDecision.Outcome switch
            {
                LongHorizonCheckpointOutcome.GrowthEligible => LongHorizonLifecycleAuditEventType.GrowthDecisionMade,
                LongHorizonCheckpointOutcome.MaintenanceOnly => LongHorizonLifecycleAuditEventType.MaintenanceDecisionMade,
                _ => LongHorizonLifecycleAuditEventType.WindowBlocked,
            };
            audit.Add(Event(scenario, type, checkpoint.CheckpointDecision.ActivationWindowBoundary,
                checkpoint.ContextVersion, checkpoint.CheckpointDecision.DecisionId,
                "Phase4K.7 checkpoint state evaluator", checkpoint.CheckpointDecision.Outcome.ToString(),
                checkpoint.CheckpointDecision.AuthoritativeReason));
        }
        return state with
        {
            ContextVersion = checkpoint.ContextVersion,
            LatestEvidenceSnapshot = checkpoint.EvidenceSnapshot ?? state.LatestEvidenceSnapshot,
            LatestValidatedLoad = checkpoint.ValidatedLoad ?? state.LatestValidatedLoad,
            CheckpointDecisions = checkpoint.CheckpointDecision is null
                ? state.CheckpointDecisions.ToList()
                : state.CheckpointDecisions.Append(checkpoint.CheckpointDecision).ToList(),
            AuditEvents = audit,
            LastCheckpointDate = checkpoint.EvidenceSnapshot?.CheckpointDate ?? state.LastCheckpointDate,
        };
    }

    private static (LongHorizonFullDarkLifecycleState State, IReadOnlyList<LongHorizonTrainingDayEvidenceRow> EvidenceRows)
        ApplyWindowOutcomes(LongHorizonFullDarkLifecycleState state, LongHorizonLifecycleWindowEvidence evidence,
            LongHorizonLifecycleScenario scenario)
    {
        if (evidence.ActivationOrdinal != state.ActivationInvocationCount)
            throw new LongHorizonCheckpointDecisionInvalidException("Evidence activation ordinal does not match the current window.");
        if (evidence.CheckpointDate <= state.LastCheckpointDate
            || evidence.CheckpointDate <= state.CurrentWindow.Weeks.Max(week => week.CalendarDates!.Value.End))
            throw new LongHorizonCheckpointDecisionInvalidException("Checkpoint date must be explicit, strictly increasing, and after the window period.");
        var rows = BuildEvidenceRows(state.CurrentWindow, evidence, scenario.ScenarioId);
        var lifecycle = new Dictionary<int, LongHorizonNumericLifecycleState>(state.LifecycleStates);
        var activated = new Dictionary<int, ActivatedNumericWeek>(state.ActivatedWeeks);
        foreach (var week in state.CurrentWindow.Weeks)
        {
            var weekRows = rows.Where(row => row.GlobalWeekNumber == week.GlobalWeekNumber).ToList();
            if (weekRows.Any(row => !LongHorizonCheckpointEvidenceAggregator.IsTerminal(row.TrainingDay.Status)))
                continue;
            var next = weekRows.Any(row => row.TrainingDay.Status == TrainingDayStatus.Completed)
                ? LongHorizonNumericLifecycleState.Completed
                : LongHorizonNumericLifecycleState.Missed;
            LongHorizonNumericLifecycleTransitionValidator.ValidateTransition(lifecycle[week.GlobalWeekNumber], next);
            lifecycle[week.GlobalWeekNumber] = next;
            activated[week.GlobalWeekNumber] = week with { LifecycleState = next };
        }
        return (state with { LifecycleStates = lifecycle, ActivatedWeeks = activated }, rows);
    }

    private static IReadOnlyList<LongHorizonTrainingDayEvidenceRow> BuildEvidenceRows(
        RollingNumericActivationWindow window, LongHorizonLifecycleWindowEvidence evidence, Guid scenarioId)
    {
        var sessions = window.Weeks.SelectMany(week => week.SessionPrescriptions!.Select(session => (week, session))).ToList();
        if (evidence.SessionOutcomes.Count < sessions.Count)
            throw new LongHorizonCheckpointDecisionInvalidException(
                $"Evidence row has {evidence.SessionOutcomes.Count} outcomes but window requires {sessions.Count} explicit outcomes.");
        return sessions.Select((pair, index) =>
        {
            var outcome = evidence.SessionOutcomes[index];
            var actual = outcome.Status == TrainingDayStatus.Completed
                ? outcome.ExplicitActualDistanceKm ?? pair.session.DistanceKm * outcome.ActualDistanceMultiplier
                : outcome.ExplicitActualDistanceKm;
            return new LongHorizonTrainingDayEvidenceRow(pair.week.GlobalWeekNumber, new TrainingDay
            {
                Id = StableGuid($"{scenarioId}|{evidence.ActivationOrdinal}|{pair.week.GlobalWeekNumber}|{index}"),
                Date = pair.session.AssignedDate!.Value.ToDateTime(TimeOnly.MinValue),
                DayType = IsLongRunRole(pair.session.SessionRole) ? TrainingDayType.LongRun : TrainingDayType.Easy,
                Status = outcome.Status,
                PlannedDistanceKm = pair.session.DistanceKm,
                ActualDistanceKm = actual,
                ActualDurationMin = outcome.Status == TrainingDayStatus.Completed ? 30 : null,
                IsLongRun = IsLongRunRole(pair.session.SessionRole),
                CompletedAt = outcome.Status == TrainingDayStatus.Completed
                    ? pair.session.AssignedDate.Value.ToDateTime(TimeOnly.MinValue).AddHours(1) : null,
            });
        }).ToList();
    }

    private static LongHorizonRollingCheckpointRequest BuildCheckpointRequest(
        LongHorizonFullDarkLifecycleState state,
        IReadOnlyList<LongHorizonTrainingDayEvidenceRow> rows,
        LongHorizonLifecycleWindowEvidence evidence,
        LongHorizonLifecycleScenario scenario) => new()
    {
        StructuralRoadmap = state.StructuralRoadmap,
        StructuralSkeleton = state.StructuralSkeleton,
        LifecycleStates = state.LifecycleStates,
        MostRecentlyActivatedWindow = state.CurrentWindow,
        TrainingDayEvidence = rows,
        CheckpointDate = evidence.CheckpointDate,
        CurrentAvailability = evidence.Availability,
        LongRunDay = scenario.LongRunDay,
        SafetyState = evidence.SafetyState,
        ReadinessProfile = scenario.ReadinessProfile,
        PriorValidatedAnchor = state.LatestValidatedLoad is null
            ? scenario.InitialPriorValidatedAnchor
            : ToPriorAnchor(state.LatestValidatedLoad, state.ContextVersion.Sequence),
        PreviousContextVersion = state.ContextVersion,
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
    };

    private static void ValidateScenario(LongHorizonLifecycleScenario scenario)
    {
        if (scenario.TotalWeeks is < 21 or > 52 || scenario.RaceDate != scenario.StartDate.AddDays(scenario.TotalWeeks * 7))
            throw new LongHorizonStructuralRoadmapInvalidException("Lifecycle scenario supports exact 21-52-week horizons only.");
        if (scenario.PreferredDays.Count != 4 || scenario.PreferredDays.Distinct().Count() != 4
            || !scenario.PreferredDays.Contains(scenario.LongRunDay))
            throw new LongHorizonCheckpointDecisionInvalidException("Scenario requires four distinct preferred days including LongRunDay.");
        if (scenario.EvidenceByActivationOrdinal.Count == 0)
            throw new LongHorizonCheckpointDecisionInvalidException("Every scenario requires explicit per-window evidence.");
    }

    private static LongHorizonPriorValidatedAnchor ToPriorAnchor(ValidatedSustainableLoad load, int contextSequence) => new(
        load with
        {
            WeeklyLoadSource = LongHorizonEvidenceAuthorityRecord.Create(
                LongHorizonEvidenceSource.PriorValidatedCheckpointLoad,
                LongHorizonEvidenceAuthorityStatus.Authoritative,
                "Prior completed checkpoint carried forward by the Phase 4K.9 harness."),
            LongRunSource = LongHorizonEvidenceAuthorityRecord.Create(
                LongHorizonEvidenceSource.PriorValidatedCheckpointLoad,
                LongHorizonEvidenceAuthorityStatus.Authoritative,
                "Prior completed checkpoint carried forward by the Phase 4K.9 harness."),
        },
        true,
        contextSequence);

    private static string EvidenceIdentity(LongHorizonLifecycleWindowEvidence evidence) => string.Join('|',
        evidence.ActivationOrdinal, evidence.CheckpointDate.ToString("yyyy-MM-dd"), evidence.SafetyState,
        string.Join(',', evidence.Availability),
        string.Join(';', evidence.SessionOutcomes.Select(x => $"{x.Status}:{x.ActualDistanceMultiplier}:{x.ExplicitActualDistanceKm}")));

    private static bool IsLongRunRole(string role) =>
        string.Equals(role.Replace("_", string.Empty, StringComparison.Ordinal), "LONGRUN", StringComparison.OrdinalIgnoreCase);

    private static LongHorizonLifecycleAuditEvent Event(
        LongHorizonLifecycleScenario scenario, LongHorizonLifecycleAuditEventType type, (int Start, int End)? range,
        LongHorizonContextVersion? version, Guid? decisionId, string authority, string result,
        LongHorizonReasonCode? reason = null) => new()
    {
        EventType = type,
        ScenarioId = scenario.ScenarioId,
        GlobalWindow = range,
        ContextVersion = version,
        DecisionId = decisionId,
        Authority = authority,
        Result = result,
        Reason = reason,
    };

    private static (int Start, int End) Range(RollingNumericActivationWindow window) =>
        (window.StartGlobalWeek, window.EndGlobalWeek);

    private static Guid StableGuid(string value) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));

    private static LongHorizonFullDarkLifecycleValidationResult Failed(
        string diagnostic, IReadOnlyList<LongHorizonFullDarkLifecycleState> snapshots,
        IReadOnlyList<string> stages) => new()
    {
        Outcome = LongHorizonFullDarkLifecycleOutcome.ValidationFailed,
        FinalState = snapshots.LastOrDefault(),
        InternalDiagnostic = diagnostic,
        StateSnapshots = snapshots,
        ValidationStages = stages,
    };
}
