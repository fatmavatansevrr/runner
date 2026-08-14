namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview;

/// <summary>
/// Phase 4L.1 Part 12 -- everything the mapper needs beyond the dark
/// lifecycle state itself. GoalType/GoalDistance are plain strings supplied
/// by the caller: no GoalType.LongHorizon/GoalDistance.LongHorizon enum
/// value exists yet (confirmed by direct search), so this input cannot
/// derive them from any typed enum -- disclosed, not hidden.
/// </summary>
internal sealed record LongHorizonPublicPreviewMapperInput
{
    public required Guid PreviewId { get; init; }
    public required string GoalType { get; init; }
    public required string GoalDistance { get; init; }
    public required LongHorizonFullDarkLifecycleState State { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EstimatedEndDate { get; init; }
    public required int DaysPerWeek { get; init; }
    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDay { get; init; }
    public required LongHorizonPublicProvenance ProvenanceSummary { get; init; }
}

/// <summary>
/// Phase 4L.1 Part 12 -- pure, deterministic, dark mapper. No runtime
/// invocation, no numeric/calendar computation (StructuralStartDate/EndDate
/// is arithmetic-only, never a materializer call), no persistence, no
/// mutation, no internal future-value exposure. Not exposed through any
/// endpoint by this phase.
/// </summary>
internal static class LongHorizonPublicPreviewMapper
{
    public static LongHorizonPlanPreviewContract Map(LongHorizonPublicPreviewMapperInput input)
    {
        var roadmap = input.State.StructuralRoadmap;
        var stageByWeek = roadmap.Weeks.ToDictionary(w => w.GlobalWeekNumber, w => w.Stage);

        var roadmapRows = roadmap.GlobalWeekNumbers
            .OrderBy(w => w)
            .Select(globalWeek => MapRoadmapRow(globalWeek, input, stageByWeek))
            .ToList();

        // The dark LifecycleStates map -- not CurrentWindow.Status, which only
        // reflects the most recent SUCCESSFUL activation and is left stale by
        // the harness when a later checkpoint attempt is blocked -- is the
        // authoritative source for both "what is currently executable" and
        // "is a next window currently blocked". Both can be true at once (a
        // prior window remains executable while the next one is blocked).
        var availableWeekNumbers = input.State.LifecycleStates
            .Where(kv => kv.Value == LongHorizonNumericLifecycleState.NumericActivated)
            .Select(kv => kv.Key).OrderBy(w => w).ToList();
        var executableWeeks = availableWeekNumbers
            .Select(w => MapExecutableWeek(input.State.ActivatedWeeks[w], stageByWeek, input.ProvenanceSummary))
            .ToList();

        // The harness never merges a terminal block's per-week
        // NumericActivationBlocked state back into state.LifecycleStates (it
        // is transient, local to the checkpoint result) -- the durable,
        // authoritative "are we currently blocked" signal is the audit trail:
        // blocked iff the most recent lifecycle-transition event is
        // WindowBlocked, i.e. no later successful activation superseded it.
        var lastTransition = input.State.AuditEvents.LastOrDefault(e => e.EventType is
            LongHorizonLifecycleAuditEventType.WindowBlocked or LongHorizonLifecycleAuditEventType.GeWindowActivated
            or LongHorizonLifecycleAuditEventType.MixedWindowActivated or LongHorizonLifecycleAuditEventType.CoreWindowActivated);
        var isBlocked = lastTransition?.EventType == LongHorizonLifecycleAuditEventType.WindowBlocked;
        var blockedState = isBlocked ? BuildBlockedState(input.State, lastTransition!.Reason) : null;

        int windowStart, windowEnd;
        if (availableWeekNumbers.Count > 0)
        {
            windowStart = availableWeekNumbers[0];
            windowEnd = availableWeekNumbers[^1];
        }
        else
        {
            windowStart = input.State.CurrentWindow.StartGlobalWeek;
            windowEnd = input.State.CurrentWindow.EndGlobalWeek;
        }

        var warnings = new List<string>();
        if (roadmapRows.Any(r => r.LifecycleStatus == LongHorizonPublicLifecycleStatus.Pending))
        {
            warnings.Add("long_horizon.warning.rolling_generation");
        }
        if (blockedState is not null)
        {
            warnings.Add("long_horizon.warning.next_window_blocked");
        }

        return new LongHorizonPlanPreviewContract
        {
            PreviewId = input.PreviewId,
            GoalType = input.GoalType,
            GoalDistance = input.GoalDistance,
            TotalWeeks = roadmap.TotalWeeks,
            StartDate = input.StartDate,
            EstimatedEndDate = input.EstimatedEndDate,
            RaceDate = roadmap.RaceDate,
            DaysPerWeek = input.DaysPerWeek,
            PreferredDays = input.PreferredDays,
            LongRunDay = input.LongRunDay,
            ReadinessProfile = roadmap.Profile.ToString(),
            CurrentWindowStartWeek = windowStart,
            CurrentWindowEndWeek = windowEnd,
            CurrentExecutableWeekCount = executableWeeks.Count,
            StructuralRoadmap = roadmapRows,
            CurrentExecutableWeeks = executableWeeks,
            PreviewReadiness = LongHorizonPreviewReadiness.ReadyForPublicPreview,
            ConfirmationReadiness = LongHorizonConfirmationReadiness.NotReadyForConfirmation,
            PublicWarnings = warnings,
            ProvenanceSummary = input.ProvenanceSummary,
            BlockedState = blockedState,
        };
    }

    private static LongHorizonStructuralRoadmapWeekContract MapRoadmapRow(
        int globalWeek,
        LongHorizonPublicPreviewMapperInput input,
        IReadOnlyDictionary<int, string> stageByWeek)
    {
        var segment = input.State.StructuralRoadmap.Segments.Single(s => globalWeek >= s.StartGlobalWeek && globalWeek <= s.EndGlobalWeek);
        var lifecycleState = input.State.LifecycleStates.TryGetValue(globalWeek, out var s)
            ? s
            : LongHorizonNumericLifecycleState.StructurallyPlanned;
        var publicStatus = MapLifecycleStatus(lifecycleState);
        var isExecutable = publicStatus is LongHorizonPublicLifecycleStatus.Available
            or LongHorizonPublicLifecycleStatus.Completed or LongHorizonPublicLifecycleStatus.Missed;

        var weekStart = input.StartDate.AddDays((globalWeek - 1) * 7);
        var weekEnd = weekStart.AddDays(6);

        return new LongHorizonStructuralRoadmapWeekContract
        {
            GlobalWeek = globalWeek,
            Phase = MapPhase(segment.SegmentType),
            Stage = stageByWeek.GetValueOrDefault(globalWeek),
            LifecycleStatus = publicStatus,
            IsExecutable = isExecutable,
            StructuralStartDate = weekStart,
            StructuralEndDate = weekEnd,
            NumericDetailsAvailable = isExecutable,
            PublicSummary = isExecutable
                ? "long_horizon.roadmap.executable_week"
                : publicStatus == LongHorizonPublicLifecycleStatus.Blocked
                    ? "long_horizon.roadmap.blocked_week"
                    : "long_horizon.roadmap.pending_week",
        };
    }

    private static LongHorizonExecutableWeekContract MapExecutableWeek(
        ActivatedNumericWeek week,
        IReadOnlyDictionary<int, string> stageByWeek,
        LongHorizonPublicProvenance provenance)
    {
        var dates = week.CalendarDates
            ?? throw new LongHorizonPublicPreviewContractInvalidException(
                $"Activated week {week.GlobalWeekNumber} has no CalendarDates -- cannot map to a public executable week.");
        var sessions = week.SessionPrescriptions
            ?? throw new LongHorizonPublicPreviewContractInvalidException(
                $"Activated week {week.GlobalWeekNumber} has no SessionPrescriptions -- cannot map to a public executable week.");

        return new LongHorizonExecutableWeekContract
        {
            GlobalWeek = week.GlobalWeekNumber,
            Phase = MapPhase(week.SegmentType),
            Stage = stageByWeek.GetValueOrDefault(week.GlobalWeekNumber, "Unknown"),
            WeekStartDate = dates.Start,
            WeekEndDate = dates.End,
            WeeklyVolumeKm = week.TotalWeeklyVolumeKm!.Value,
            LongRunVolumeKm = week.LongRunKm!.Value,
            LifecycleStatus = LongHorizonPublicLifecycleStatus.Available,
            Sessions = sessions.Select(MapSession).ToList(),
            PublicProvenanceSummary = provenance,
        };
    }

    private static LongHorizonExecutableSessionContract MapSession(LongHorizonSessionPrescriptionReference session)
    {
        var date = session.AssignedDate
            ?? throw new LongHorizonPublicPreviewContractInvalidException(
                $"Session '{session.SessionRole}' has no AssignedDate -- cannot map to a public executable session.");

        return new LongHorizonExecutableSessionContract
        {
            SessionDate = date,
            Weekday = date.DayOfWeek,
            SessionRole = session.SessionRole,
            DistanceKm = session.DistanceKm,
            IsLongRun = session.SessionRole.Contains("LONG", StringComparison.OrdinalIgnoreCase),
            ExecutableStatus = LongHorizonPublicLifecycleStatus.Available,
        };
    }

    private static LongHorizonBlockedStateContract BuildBlockedState(LongHorizonFullDarkLifecycleState state, LongHorizonReasonCode? reason)
    {
        var category = reason is null
            ? LongHorizonPublicBlockedReasonCategory.MoreTrainingDataNeeded
            : MapReasonToCategory(reason.Value);

        return new LongHorizonBlockedStateContract
        {
            ReasonCategory = category,
            RetryEligible = category != LongHorizonPublicBlockedReasonCategory.SafetyReviewRequired,
            NextActionKey = $"long_horizon.blocked.action.{category}",
            LastEvaluatedDate = state.LastCheckpointDate,
        };
    }

    internal static LongHorizonPublicBlockedReasonCategory MapReasonToCategory(LongHorizonReasonCode reason)
    {
        if (reason.Category == LongHorizonReasonCodeCategory.Checkpoint)
        {
            return reason.CheckpointReason!.Value switch
            {
                LongHorizonCheckpointReasonCode.CheckpointWindowNotComplete => LongHorizonPublicBlockedReasonCategory.CompleteCurrentWeek,
                LongHorizonCheckpointReasonCode.SafetyReassessmentRequired => LongHorizonPublicBlockedReasonCategory.SafetyReviewRequired,
                LongHorizonCheckpointReasonCode.NumericWindowInfeasible => LongHorizonPublicBlockedReasonCategory.PlanTransitionUnavailable,
                _ => LongHorizonPublicBlockedReasonCategory.MoreTrainingDataNeeded,
            };
        }

        return reason.JitReason!.Value switch
        {
            LongHorizonJitReasonCode.JitPaceSourceUnresolved => LongHorizonPublicBlockedReasonCategory.PaceInformationNeeded,
            LongHorizonJitReasonCode.JitGoalFeasibilityUnresolved => LongHorizonPublicBlockedReasonCategory.PaceInformationNeeded,
            LongHorizonJitReasonCode.JitAvailabilityInfeasible => LongHorizonPublicBlockedReasonCategory.UpdateAvailability,
            LongHorizonJitReasonCode.RunwayJitContextUnavailable => LongHorizonPublicBlockedReasonCategory.PlanTransitionUnavailable,
            LongHorizonJitReasonCode.CoreJitContextUnavailable => LongHorizonPublicBlockedReasonCategory.PlanTransitionUnavailable,
            LongHorizonJitReasonCode.JitActivationBoundaryMissed => LongHorizonPublicBlockedReasonCategory.PlanTransitionUnavailable,
            LongHorizonJitReasonCode.JitSegmentTransitionInfeasible => LongHorizonPublicBlockedReasonCategory.PlanTransitionUnavailable,
            _ => LongHorizonPublicBlockedReasonCategory.MoreTrainingDataNeeded,
        };
    }

    /// <summary>StructurallyPlanned collapses into Pending -- both mean "not yet executable" publicly.</summary>
    internal static LongHorizonPublicLifecycleStatus MapLifecycleStatus(LongHorizonNumericLifecycleState state) => state switch
    {
        LongHorizonNumericLifecycleState.StructurallyPlanned => LongHorizonPublicLifecycleStatus.Pending,
        LongHorizonNumericLifecycleState.NumericPending => LongHorizonPublicLifecycleStatus.Pending,
        LongHorizonNumericLifecycleState.NumericActivated => LongHorizonPublicLifecycleStatus.Available,
        LongHorizonNumericLifecycleState.NumericActivationBlocked => LongHorizonPublicLifecycleStatus.Blocked,
        LongHorizonNumericLifecycleState.Completed => LongHorizonPublicLifecycleStatus.Completed,
        LongHorizonNumericLifecycleState.Missed => LongHorizonPublicLifecycleStatus.Missed,
        _ => throw new LongHorizonPublicPreviewContractInvalidException($"Unhandled lifecycle state {state}."),
    };

    private static LongHorizonPublicPhase MapPhase(LongHorizonStructuralSegmentType segmentType) => segmentType switch
    {
        LongHorizonStructuralSegmentType.GeneralEndurance => LongHorizonPublicPhase.GeneralEndurance,
        LongHorizonStructuralSegmentType.PreparationRunway => LongHorizonPublicPhase.PreparationRunway,
        LongHorizonStructuralSegmentType.Core => LongHorizonPublicPhase.Core,
        _ => throw new LongHorizonPublicPreviewContractInvalidException($"Unhandled segment type {segmentType}."),
    };
}

internal sealed class LongHorizonPublicPreviewContractInvalidException(string message)
    : LongHorizonRollingContractException("LONG_HORIZON_PUBLIC_PREVIEW_CONTRACT_INVALID", message);
