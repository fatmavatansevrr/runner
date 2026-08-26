using System.Security.Cryptography;
using System.Text;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal enum LongHorizonRollingCheckpointRuntimeOutcome
{
    NextGeWindowActivated,
    NextGeWindowBlocked,
    GeCheckpointCompletedWithoutGeWindowBecauseRunwayBoundaryReached,
}

internal sealed record LongHorizonRollingCheckpointRequest
{
    public required LongHorizonStructuralRoadmap StructuralRoadmap { get; init; }
    public required LongHorizonGeneratedStructuralSkeleton StructuralSkeleton { get; init; }
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public required RollingNumericActivationWindow MostRecentlyActivatedWindow { get; init; }
    public required IReadOnlyList<LongHorizonTrainingDayEvidenceRow> TrainingDayEvidence { get; init; }
    public required DateOnly CheckpointDate { get; init; }
    public required IReadOnlyList<DayOfWeek> CurrentAvailability { get; init; }
    public required DayOfWeek LongRunDay { get; init; }
    public required LongHorizonSafetyState SafetyState { get; init; }
    public required ReadinessProfile ReadinessProfile { get; init; }
    public LongHorizonPriorValidatedAnchor? PriorValidatedAnchor { get; init; }
    public required LongHorizonContextVersion PreviousContextVersion { get; init; }
    public required GoalType GoalType { get; init; }
    public required GoalDistance GoalDistance { get; init; }
    public required RunningBackground Level { get; init; }
    public required int DaysPerWeek { get; init; }
}

internal sealed record LongHorizonRollingCheckpointResult
{
    public required LongHorizonRollingCheckpointRuntimeOutcome Outcome { get; init; }
    public required LongHorizonStructuralRoadmap StructuralRoadmap { get; init; }
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public required IReadOnlyList<ActivatedNumericWeek> NewlyActivatedWeeks { get; init; }
    public LongHorizonCheckpointEvidenceSnapshot? EvidenceSnapshot { get; init; }
    public ValidatedSustainableLoad? ValidatedLoad { get; init; }
    public LongHorizonCheckpointDecision? CheckpointDecision { get; init; }
    public RollingNumericActivationWindow? ActivationWindow { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public LongHorizonReasonCode? AuthoritativeReason { get; init; }
    public required IReadOnlyList<string> ValidationStages { get; init; }
}

internal interface ILongHorizonRollingCheckpointRuntime
{
    Task<LongHorizonRollingCheckpointResult> EvaluateAndActivateNextGeWindowAsync(
        LongHorizonRollingCheckpointRequest request,
        CancellationToken cancellationToken = default);
}

internal interface ILongHorizonGeMaintenanceWindowMaterializer
{
    IReadOnlyList<LongHorizonGeWeekNumericResult> Materialize(
        IReadOnlyList<LongHorizonGeWeekDescriptor> selectedWeeks,
        ValidatedSustainableLoad anchor);
}

internal sealed class LongHorizonGeMaintenanceWindowMaterializer : ILongHorizonGeMaintenanceWindowMaterializer
{
    public IReadOnlyList<LongHorizonGeWeekNumericResult> Materialize(
        IReadOnlyList<LongHorizonGeWeekDescriptor> selectedWeeks,
        ValidatedSustainableLoad anchor)
    {
        var weeklyAnchor = anchor.WeeklyVolumeKm!.Value;
        var longRunAnchor = anchor.LongRunKm!.Value;
        return selectedWeeks.Select(week =>
        {
            var total = weeklyAnchor;
            if (week.IsRecoveryWeek)
            {
                total = LongHorizonCheckpointEvidenceAggregator.Round(weeklyAnchor * LongHorizonGeNumericExecutor.RecoveryVolumeRatio);
                if (weeklyAnchor - total < LongHorizonGeNumericExecutor.MinimumRecoveryReductionKm)
                    total = LongHorizonCheckpointEvidenceAggregator.Round(weeklyAnchor - LongHorizonGeNumericExecutor.MinimumRecoveryReductionKm);
            }
            var selectedLongRun = LongHorizonCheckpointEvidenceAggregator.Round(total * VolumeSafetyPolicy.Default.LongRunSelectionShare);
            var hardCap = LongHorizonCheckpointEvidenceAggregator.Round(total * VolumeSafetyPolicy.Default.LongRunHardCapShare);
            var longRun = Math.Min(longRunAnchor, Math.Min(selectedLongRun, hardCap));
            var allocation = FourDaySessionDistanceAllocationPolicy.Allocate(total, longRun);
            return new LongHorizonGeWeekNumericResult(
                week.WeekIndex, total, longRun, allocation.KeySessionDistanceKm,
                allocation.EasySupportDistancesKm);
        }).ToList();
    }
}

internal sealed class LongHorizonRollingCheckpointRuntime : ILongHorizonRollingCheckpointRuntime
{
    private readonly ILongHorizonRollingGeWindowMaterializer _growthMaterializer;
    private readonly ILongHorizonGeMaintenanceWindowMaterializer _maintenanceMaterializer;

    public LongHorizonRollingCheckpointRuntime(
        ILongHorizonRollingGeWindowMaterializer? growthMaterializer = null,
        ILongHorizonGeMaintenanceWindowMaterializer? maintenanceMaterializer = null)
    {
        _growthMaterializer = growthMaterializer ?? new ExistingLongHorizonGeWindowMaterializer();
        _maintenanceMaterializer = maintenanceMaterializer ?? new LongHorizonGeMaintenanceWindowMaterializer();
    }

    public Task<LongHorizonRollingCheckpointResult> EvaluateAndActivateNextGeWindowAsync(
        LongHorizonRollingCheckpointRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stages = new List<string>();
        ValidateInput(request);
        stages.Add("InputEligibility");
        LongHorizonStructuralRoadmapValidator.Validate(request.StructuralRoadmap);
        stages.Add("StructuralRoadmap");

        var seed = IdentitySeed(request);
        var checkpointId = StableGuid(seed + "|snapshot");
        var decisionId = StableGuid(seed + "|decision");
        var nextVersion = request.PreviousContextVersion.Next(StableGuid(seed + "|context"));
        var nextStart = request.MostRecentlyActivatedWindow.EndGlobalWeek + 1;
        LongHorizonCheckpointAggregationResult aggregation;
        try
        {
            aggregation = LongHorizonCheckpointEvidenceAggregator.Aggregate(
                request.StructuralRoadmap, request.MostRecentlyActivatedWindow, request.TrainingDayEvidence,
                request.CheckpointDate, request.CurrentAvailability, request.SafetyState,
                request.PriorValidatedAnchor, nextVersion, checkpointId);
        }
        catch (LongHorizonCheckpointDecisionInvalidException exception)
        {
            var end = Math.Min(nextStart + 3, request.StructuralRoadmap.GeneralEnduranceWeeks);
            if (end < nextStart) end = nextStart;
            var decision = new LongHorizonCheckpointDecision
            {
                DecisionId = decisionId,
                EvidenceSnapshotId = checkpointId,
                Outcome = LongHorizonCheckpointOutcome.NumericActivationBlocked,
                AuthoritativeReason = LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved),
                ActivationWindowBoundary = (nextStart, end),
                SafetyPriorityApplied = false,
                PolicyProvenance = $"PHASE4K_7_EVIDENCE_CONFLICT:{exception.GetType().Name}:{exception.Message}",
            };
            var conflictLifecycle = new Dictionary<int, LongHorizonNumericLifecycleState>(request.LifecycleStates);
            for (var week = nextStart; week <= end && conflictLifecycle.ContainsKey(week); week++)
                if (conflictLifecycle[week] == LongHorizonNumericLifecycleState.NumericPending)
                    conflictLifecycle[week] = LongHorizonNumericLifecycleState.NumericActivationBlocked;
            return Task.FromResult(ValidateResult(new LongHorizonRollingCheckpointResult
            {
                Outcome = LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked,
                StructuralRoadmap = request.StructuralRoadmap,
                LifecycleStates = conflictLifecycle,
                NewlyActivatedWeeks = [],
                CheckpointDecision = decision,
                ContextVersion = nextVersion,
                AuthoritativeReason = decision.AuthoritativeReason,
                ValidationStages = stages.Append($"EvidenceConflict:{exception.GetType().Name}:{exception.Message}").Append("FinalResultValidation").ToList(),
            }));
        }
        stages.Add("EvidenceAggregation");
        stages.Add("SnapshotValidation");

        if (nextStart > request.StructuralRoadmap.GeneralEnduranceWeeks)
        {
            return Task.FromResult(ValidateResult(new LongHorizonRollingCheckpointResult
            {
                Outcome = LongHorizonRollingCheckpointRuntimeOutcome.GeCheckpointCompletedWithoutGeWindowBecauseRunwayBoundaryReached,
                StructuralRoadmap = request.StructuralRoadmap,
                LifecycleStates = new Dictionary<int, LongHorizonNumericLifecycleState>(request.LifecycleStates),
                NewlyActivatedWeeks = [],
                EvidenceSnapshot = aggregation.Snapshot,
                ValidatedLoad = aggregation.CurrentValidatedLoad,
                ContextVersion = nextVersion,
                ValidationStages = stages.Append("RunwayBoundaryHandoff").Append("FinalResultValidation").ToList(),
            }));
        }

        var nextEnd = Math.Min(nextStart + 3, request.StructuralRoadmap.GeneralEnduranceWeeks);
        var boundary = (nextStart, nextEnd);
        ValidatePendingBoundary(request, boundary);
        stages.Add("NextGeWindowSelection");
        var evaluation = LongHorizonCheckpointStateEvaluator.Evaluate(
            aggregation.Snapshot, aggregation.CurrentValidatedLoad, request.PriorValidatedAnchor, boundary, decisionId);
        stages.Add("StateTransition");

        if (evaluation.Decision.Outcome == LongHorizonCheckpointOutcome.NumericActivationBlocked)
        {
            return Task.FromResult(ValidateResult(Blocked(request, aggregation, evaluation.Decision, nextVersion, boundary, stages)));
        }

        var descriptors = LongHorizonGeStructuralSelector
            .Select(request.StructuralRoadmap.GeneralEnduranceWeeks, request.ReadinessProfile)
            .Skip(nextStart - 1).Take(nextEnd - nextStart + 1).ToList();
        IReadOnlyList<LongHorizonGeWeekNumericResult> numeric;
        try
        {
            numeric = evaluation.Decision.Outcome == LongHorizonCheckpointOutcome.GrowthEligible
                ? _growthMaterializer.Materialize(descriptors, new LongHorizonGeEntryBaselineInput(
                    evaluation.EffectiveLoad!.WeeklyVolumeKm, evaluation.EffectiveLoad.LongRunKm, null))
                : _maintenanceMaterializer.Materialize(descriptors, evaluation.EffectiveLoad!);
            if (numeric.Count != descriptors.Count)
                throw new LongHorizonCheckpointDecisionInvalidException("NUMERIC_WINDOW_INFEASIBLE: bounded materializer returned an incomplete window.");
            stages.Add(evaluation.Decision.Outcome == LongHorizonCheckpointOutcome.GrowthEligible ? "GrowthMaterialization" : "MaintenanceMaterialization");
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            var blockedDecision = evaluation.Decision with
            {
                Outcome = LongHorizonCheckpointOutcome.NumericActivationBlocked,
                AuthoritativeReason = LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.NumericWindowInfeasible),
                MaintenanceAnchorWeeklyVolumeKm = null,
            };
            return Task.FromResult(ValidateResult(Blocked(request, aggregation, blockedDecision, nextVersion, boundary,
                stages.Append($"NumericFailure:{exception.GetType().Name}:{exception.Message}").ToList())));
        }

        IReadOnlyDictionary<string, DayOfWeek> weekdays;
        try
        {
            weekdays = LongHorizonCalendarAssigner.AssignWeekdays(request.CurrentAvailability, request.LongRunDay);
        }
        catch (InvalidOperationException exception)
        {
            var blockedDecision = evaluation.Decision with
            {
                Outcome = LongHorizonCheckpointOutcome.NumericActivationBlocked,
                AuthoritativeReason = LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.NumericWindowInfeasible),
                MaintenanceAnchorWeeklyVolumeKm = null,
            };
            return Task.FromResult(ValidateResult(Blocked(request, aggregation, blockedDecision, nextVersion, boundary,
                stages.Append($"CalendarFailure:{exception.GetType().Name}:{exception.Message}").ToList())));
        }

        var activated = numeric.Select(value => MapWeek(
            request.StructuralSkeleton.Weeks[value.WeekIndex - 1], value, request, weekdays,
            aggregation.Snapshot.EvidenceSourceMetadata, evaluation.Decision, nextVersion)).ToList();
        foreach (var week in activated)
            LongHorizonActivatedNumericWeekValidator.Validate(week);
        stages.Add("ActivatedWeekValidation");

        var window = new RollingNumericActivationWindow
        {
            WindowId = StableGuid(seed + "|window"),
            ContextVersion = nextVersion,
            StartGlobalWeek = nextStart,
            EndGlobalWeek = nextEnd,
            RequestedWindowSizeWeeks = 4,
            ActualWindowSizeWeeks = nextEnd - nextStart + 1,
            Weeks = activated,
            SegmentsCovered = [LongHorizonStructuralSegmentType.GeneralEndurance],
            ActivationSource = LongHorizonInitialActivationSource.CheckpointRollingActivation,
            CheckpointDecisionId = decisionId,
            Status = LongHorizonActivationWindowStatus.Activated,
        };
        LongHorizonRollingActivationWindowValidator.ValidateAtomicity(window.Status, activated);
        LongHorizonRollingActivationWindowValidator.Validate(window);
        stages.Add("Atomicity");

        var lifecycle = new Dictionary<int, LongHorizonNumericLifecycleState>(request.LifecycleStates);
        foreach (var week in activated)
        {
            LongHorizonNumericLifecycleTransitionValidator.ValidateTransition(lifecycle[week.GlobalWeekNumber], LongHorizonNumericLifecycleState.NumericActivated);
            lifecycle[week.GlobalWeekNumber] = LongHorizonNumericLifecycleState.NumericActivated;
        }
        stages.Add("FinalResultValidation");
        return Task.FromResult(ValidateResult(new LongHorizonRollingCheckpointResult
        {
            Outcome = LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated,
            StructuralRoadmap = request.StructuralRoadmap,
            LifecycleStates = lifecycle,
            NewlyActivatedWeeks = activated,
            EvidenceSnapshot = aggregation.Snapshot,
            ValidatedLoad = evaluation.EffectiveLoad,
            CheckpointDecision = evaluation.Decision,
            ActivationWindow = window,
            ContextVersion = nextVersion,
            ValidationStages = stages,
        }));
    }

    private static LongHorizonRollingCheckpointResult Blocked(
        LongHorizonRollingCheckpointRequest request,
        LongHorizonCheckpointAggregationResult aggregation,
        LongHorizonCheckpointDecision decision,
        LongHorizonContextVersion version,
        (int StartGlobalWeek, int EndGlobalWeek) boundary,
        IReadOnlyList<string> stages)
    {
        LongHorizonCheckpointDecisionValidator.Validate(decision);
        var lifecycle = new Dictionary<int, LongHorizonNumericLifecycleState>(request.LifecycleStates);
        for (var week = boundary.StartGlobalWeek; week <= boundary.EndGlobalWeek; week++)
        {
            if (lifecycle[week] == LongHorizonNumericLifecycleState.NumericPending)
                lifecycle[week] = LongHorizonNumericLifecycleState.NumericActivationBlocked;
        }
        return new LongHorizonRollingCheckpointResult
        {
            Outcome = LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked,
            StructuralRoadmap = request.StructuralRoadmap,
            LifecycleStates = lifecycle,
            NewlyActivatedWeeks = [],
            EvidenceSnapshot = aggregation.Snapshot,
            ValidatedLoad = decision.ValidatedLoad,
            CheckpointDecision = decision,
            ContextVersion = version,
            AuthoritativeReason = decision.AuthoritativeReason,
            ValidationStages = stages.Append("FinalResultValidation").ToList(),
        };
    }

    private static ActivatedNumericWeek MapWeek(
        global::RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.LongHorizonStructuralWeek structural,
        LongHorizonGeWeekNumericResult numeric,
        LongHorizonRollingCheckpointRequest request,
        IReadOnlyDictionary<string, DayOfWeek> weekdays,
        LongHorizonEvidenceAuthorityRecord evidence,
        LongHorizonCheckpointDecision decision,
        LongHorizonContextVersion version)
    {
        var start = LongHorizonCalendarAssigner.WeekStartDate(
            request.MostRecentlyActivatedWindow.Weeks[^1].CalendarDates!.Value.End.AddDays(1),
            structural.GlobalWeekNumber - request.MostRecentlyActivatedWindow.EndGlobalWeek);
        var sessions = structural.OrderedWorkoutSlots.Select(slot =>
        {
            var role = slot.StructuralRole == "EASY_SUPPORT"
                ? (slot.StructuralSlotIndex <= 2 ? "EASY_SUPPORT_1" : "EASY_SUPPORT_2")
                : slot.StructuralRole;
            var distance = role switch
            {
                "KEY_SESSION" => numeric.KeySessionDistanceKm,
                "EASY_SUPPORT_1" => numeric.FirstEasySupportDistanceKm,
                "EASY_SUPPORT_2" => numeric.SecondEasySupportDistanceKm,
                "LONG_RUN" => numeric.LongRunDistanceKm,
                _ => throw new ArgumentOutOfRangeException(nameof(role)),
            };
            return new LongHorizonSessionPrescriptionReference
            {
                SessionRole = role,
                DistanceKm = distance,
                WorkoutKey = slot.WorkoutKey,
                WorkoutVersion = slot.WorkoutVersion,
                AssignedDate = LongHorizonCalendarAssigner.AssignedDate(start, weekdays[role]),
                Source = "Phase4K.7 existing GE catalog/numeric/calendar authorities",
            };
        }).ToList();
        return new ActivatedNumericWeek
        {
            GlobalWeekNumber = structural.GlobalWeekNumber,
            SegmentType = LongHorizonStructuralSegmentType.GeneralEndurance,
            LifecycleState = LongHorizonNumericLifecycleState.NumericActivated,
            TotalWeeklyVolumeKm = numeric.TotalVolumeKm,
            LongRunKm = numeric.LongRunDistanceKm,
            SessionPrescriptions = sessions,
            CalendarDates = (start, start.AddDays(6)),
            EvidenceProvenance = evidence,
            PaceIntensityContext = LongHorizonEvidenceAuthorityCatalog.PaceAndTargetTimeAuthority,
            NumericPolicyProvenance = decision.Outcome == LongHorizonCheckpointOutcome.GrowthEligible
                ? "LongHorizonGeNumericExecutor/GrowthEligible"
                : "ValidatedSustainableLoad/MaintenanceOnly+existing 0.85 recovery",
            ContextVersion = version,
            CheckpointDecisionId = decision.DecisionId,
        };
    }

    private static void ValidateInput(LongHorizonRollingCheckpointRequest request)
    {
        if (request.GoalType != GoalType.Race || request.GoalDistance != GoalDistance.TenK
            || request.Level != RunningBackground.Intermediate || request.DaysPerWeek != 4
            || request.StructuralRoadmap.TotalWeeks is < 21 or > 52
            || request.ReadinessProfile != request.StructuralRoadmap.Profile)
            throw new LongHorizonCheckpointDecisionInvalidException("Checkpoint runtime eligibility is Race/exact-10K/Intermediate/4D/21-52 only.");
    }

    private static void ValidatePendingBoundary(LongHorizonRollingCheckpointRequest request, (int StartGlobalWeek, int EndGlobalWeek) boundary)
    {
        for (var week = boundary.StartGlobalWeek; week <= boundary.EndGlobalWeek; week++)
            if (!request.LifecycleStates.TryGetValue(week, out var state) || state != LongHorizonNumericLifecycleState.NumericPending)
                throw new LongHorizonCheckpointDecisionInvalidException($"Next GE week {week} must be NumericPending.");
    }

    private static string IdentitySeed(LongHorizonRollingCheckpointRequest request) => string.Join('|',
        request.StructuralRoadmap.TotalWeeks, request.MostRecentlyActivatedWindow.WindowId,
        request.CheckpointDate.ToString("yyyy-MM-dd"), request.SafetyState,
        request.PreviousContextVersion.VersionId,
        string.Join(',', request.CurrentAvailability.OrderBy(day => day)), request.LongRunDay,
        request.PriorValidatedAnchor?.IsFreshForCurrentInvocation, request.PriorValidatedAnchor?.SourceContextSequence,
        request.PriorValidatedAnchor?.Load.WeeklyVolumeKm, request.PriorValidatedAnchor?.Load.LongRunKm,
        string.Join(';', request.TrainingDayEvidence.OrderBy(row => row.GlobalWeekNumber).ThenBy(row => row.TrainingDay.Id)
            .Select(row => $"{row.GlobalWeekNumber}:{row.TrainingDay.Id}:{row.TrainingDay.Status}:{row.TrainingDay.ActualDistanceKm}")));

    private static Guid StableGuid(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));

    private static LongHorizonRollingCheckpointResult ValidateResult(LongHorizonRollingCheckpointResult result)
    {
        LongHorizonRollingCheckpointResultValidator.Validate(result);
        return result;
    }
}

internal static class LongHorizonRollingCheckpointResultValidator
{
    public static void Validate(LongHorizonRollingCheckpointResult result)
    {
        if (result.ContextVersion.Sequence <= 1)
            throw new LongHorizonCheckpointDecisionInvalidException("A checkpoint result must advance the context version.");

        switch (result.Outcome)
        {
            case LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated:
                if (result.ActivationWindow is null || result.CheckpointDecision is null
                    || result.NewlyActivatedWeeks.Count is < 1 or > 4 || result.AuthoritativeReason is not null)
                    throw new LongHorizonCheckpointDecisionInvalidException("An activated checkpoint result must be atomic, bounded, and reason-free.");
                LongHorizonCheckpointDecisionValidator.Validate(result.CheckpointDecision);
                break;
            case LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked:
                if (result.ActivationWindow is not null || result.NewlyActivatedWeeks.Count != 0
                    || result.CheckpointDecision is null || result.AuthoritativeReason is null)
                    throw new LongHorizonCheckpointDecisionInvalidException("A blocked checkpoint result must activate zero weeks and carry one decision reason.");
                LongHorizonCheckpointDecisionValidator.Validate(result.CheckpointDecision);
                break;
            case LongHorizonRollingCheckpointRuntimeOutcome.GeCheckpointCompletedWithoutGeWindowBecauseRunwayBoundaryReached:
                if (result.ActivationWindow is not null || result.CheckpointDecision is not null
                    || result.NewlyActivatedWeeks.Count != 0 || result.AuthoritativeReason is not null)
                    throw new LongHorizonCheckpointDecisionInvalidException("A runway-boundary handoff must be non-error and contain no GE activation or decision.");
                break;
            default:
                throw new LongHorizonCheckpointDecisionInvalidException($"Unknown checkpoint runtime outcome {result.Outcome}.");
        }
    }
}
