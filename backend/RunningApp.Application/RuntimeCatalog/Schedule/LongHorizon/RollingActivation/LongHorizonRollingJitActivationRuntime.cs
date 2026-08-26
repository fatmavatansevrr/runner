using System.Security.Cryptography;
using System.Text;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.8 — resolves and atomically activates the next rolling window
/// when it involves Preparation Runway and/or Core (GE-only windows remain
/// Phase 4K.7's scope). Dark, unwired, supplied-context based -- consumes
/// the Phase 4K.5/4K.8B typed contracts and the caller's own real,
/// unchanged pipeline outputs (Core generation, condition resolution, GE
/// materialization); never reimplements any of them.
/// </summary>
internal interface ILongHorizonRollingJitActivationRuntime
{
    Task<LongHorizonRollingJitActivationResult> ResolveAndActivateNextWindowAsync(
        LongHorizonRollingJitActivationRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class LongHorizonRollingJitActivationRuntime : ILongHorizonRollingJitActivationRuntime
{
    public Task<LongHorizonRollingJitActivationResult> ResolveAndActivateNextWindowAsync(
        LongHorizonRollingJitActivationRequest request,
        CancellationToken cancellationToken = default)
    {
        var stages = new List<string>();

        ValidateInput(request);
        stages.Add("InputValidation");
        LongHorizonStructuralRoadmapValidator.Validate(request.StructuralRoadmap);
        stages.Add("RoadmapValidation");

        var seed = BuildIdentitySeed(request);
        var decisionId = StableGuid(seed + "|decision");
        var contextVersion = request.PreviousContextVersion.Next(StableGuid(seed + "|context"));

        if (request.SafetyState == LongHorizonSafetyState.UnresolvedSafetyCritical)
        {
            stages.Add("SafetyCheck");
            return Task.FromResult(Blocked(request, contextVersion, stages, LongHorizonReasonCode.SafetyReassessmentRequired));
        }
        stages.Add("SafetyCheck");

        var windowStart = FirstUnstartedPendingWeek(request.LifecycleStates, request.StructuralRoadmap.TotalWeeks);
        if (windowStart is null)
        {
            stages.Add("WindowSelection");
            return Task.FromResult(Blocked(request, contextVersion, stages,
                LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitActivationBoundaryMissed)));
        }

        var cap = Math.Min(windowStart.Value + 3, request.StructuralRoadmap.TotalWeeks);
        var windowEnd = windowStart.Value;
        while (windowEnd + 1 <= cap
            && request.LifecycleStates.TryGetValue(windowEnd + 1, out var nextState)
            && nextState == LongHorizonNumericLifecycleState.NumericPending)
        {
            windowEnd++;
        }
        stages.Add("WindowSelection");

        if (!IsValidAvailability(request.CurrentAvailability, request.LongRunDay, request.DaysPerWeek))
        {
            stages.Add("AvailabilityValidation");
            return Task.FromResult(Blocked(request, contextVersion, stages,
                LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitAvailabilityInfeasible)));
        }
        stages.Add("AvailabilityValidation");

        if (HasUnresolvedPaceOrGoal(request.ConditionResults, out var paceGoalReason))
        {
            stages.Add("PaceGoalFeasibility");
            return Task.FromResult(Blocked(request, contextVersion, stages, paceGoalReason!.Value));
        }
        stages.Add("PaceGoalFeasibility");

        var startSegment = SegmentFor(request.StructuralRoadmap, windowStart.Value);
        var endSegment = SegmentFor(request.StructuralRoadmap, windowEnd);
        stages.Add("SegmentClassification");

        try
        {
            LongHorizonRollingJitActivationResult result;
            if (startSegment == LongHorizonStructuralSegmentType.GeneralEndurance && endSegment == LongHorizonStructuralSegmentType.PreparationRunway)
            {
                result = ActivateGeRunwayMixedWindow(request, windowStart.Value, windowEnd, decisionId, contextVersion, seed, stages);
            }
            else if (startSegment == LongHorizonStructuralSegmentType.PreparationRunway && endSegment == LongHorizonStructuralSegmentType.PreparationRunway)
            {
                result = ActivateRunwayOnlyWindow(request, windowStart.Value, windowEnd, decisionId, contextVersion, seed, stages);
            }
            else if (startSegment == LongHorizonStructuralSegmentType.PreparationRunway && endSegment == LongHorizonStructuralSegmentType.Core)
            {
                result = ActivateRunwayCoreMixedWindow(request, windowStart.Value, windowEnd, decisionId, contextVersion, seed, stages);
            }
            else if (startSegment == LongHorizonStructuralSegmentType.Core && endSegment == LongHorizonStructuralSegmentType.Core)
            {
                result = ActivateCoreOnlyWindow(request, windowStart.Value, windowEnd, contextVersion, stages);
            }
            else
            {
                stages.Add("UnsupportedSegmentCombination");
                return Task.FromResult(Blocked(request, contextVersion, stages,
                    LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitSegmentTransitionInfeasible)));
            }

            return Task.FromResult(result);
        }
        catch (LongHorizonRollingContractException)
        {
            // Any validator failure anywhere in a segment path is atomic: zero weeks activate.
            stages.Add("ContractValidationFailure");
            return Task.FromResult(Blocked(request, contextVersion, stages,
                LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitSegmentTransitionInfeasible)));
        }
    }

    // ── Part 10: GE + Runway mixed window ───────────────────────────────

    private static LongHorizonRollingJitActivationResult ActivateGeRunwayMixedWindow(
        LongHorizonRollingJitActivationRequest request, int windowStart, int windowEnd,
        Guid decisionId, LongHorizonContextVersion contextVersion, string seed, List<string> stages)
    {
        if (request.GeCheckpointDecision is null || request.GeActivatedWeeks is null)
        {
            throw new LongHorizonJitContextInvalidException(
                "A window starting in GeneralEndurance requires GeCheckpointDecision and GeActivatedWeeks (Phase 4K.7's own output).");
        }

        var geSegment = request.StructuralRoadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance);
        var geWeeks = request.GeActivatedWeeks
            .Where(w => w.GlobalWeekNumber >= windowStart && w.GlobalWeekNumber <= geSegment.EndGlobalWeek)
            .OrderBy(w => w.GlobalWeekNumber)
            .ToList();
        stages.Add("GePortionSelected");

        var (lockedTarget, prescription) = ResolveOrCreateRunwayPrescription(request, decisionId, seed);
        stages.Add("RunwayPrescriptionResolved");

        var slice = SliceForWindow(prescription, geSegment.EndGlobalWeek + 1, windowEnd);
        stages.Add("RunwayBoundedSliceCreated");

        var runwayWeeks = MapRunwayWeeks(slice, request.PlanStartDate, contextVersion);

        var newlyActivated = geWeeks.Concat(runwayWeeks).OrderBy(w => w.GlobalWeekNumber).ToList();
        var window = BuildWindow(request, windowStart, windowEnd, newlyActivated,
            [LongHorizonStructuralSegmentType.GeneralEndurance, LongHorizonStructuralSegmentType.PreparationRunway], decisionId, contextVersion, seed);
        stages.Add("WindowAssembled");

        return Success(LongHorizonRollingJitActivationOutcome.GeRunwayMixedWindowActivated, request,
            window, newlyActivated, contextVersion, stages, coreTargetLock: lockedTarget, runwayPrescription: prescription, runwaySlice: slice);
    }

    // ── Part 11: Runway-only window ─────────────────────────────────────

    private static LongHorizonRollingJitActivationResult ActivateRunwayOnlyWindow(
        LongHorizonRollingJitActivationRequest request, int windowStart, int windowEnd,
        Guid decisionId, LongHorizonContextVersion contextVersion, string seed, List<string> stages)
    {
        var (lockedTarget, prescription) = ResolveOrCreateRunwayPrescription(request, decisionId, seed);
        stages.Add("RunwayPrescriptionResolved");

        var slice = SliceForWindow(prescription, windowStart, windowEnd);
        stages.Add("RunwayBoundedSliceCreated");

        var runwayWeeks = MapRunwayWeeks(slice, request.PlanStartDate, contextVersion);
        var window = BuildWindow(request, windowStart, windowEnd, runwayWeeks,
            [LongHorizonStructuralSegmentType.PreparationRunway], decisionId, contextVersion, seed);
        stages.Add("WindowAssembled");

        return Success(LongHorizonRollingJitActivationOutcome.RunwayWindowActivated, request,
            window, runwayWeeks, contextVersion, stages, coreTargetLock: lockedTarget, runwayPrescription: prescription, runwaySlice: slice);
    }

    // ── Part 12: Runway + Core mixed window ─────────────────────────────

    private static LongHorizonRollingJitActivationResult ActivateRunwayCoreMixedWindow(
        LongHorizonRollingJitActivationRequest request, int windowStart, int windowEnd,
        Guid decisionId, LongHorizonContextVersion contextVersion, string seed, List<string> stages)
    {
        if (request.ExistingRunwayPrescription is null || request.ExistingLockedCoreTarget is null)
        {
            throw new PreparationRunwayFullPrescriptionInvalidException(
                "A Runway->Core mixed window requires an already-created, immutable full Runway prescription (Runway's own numeric materialization always completes before its final window).");
        }

        var (lockedTarget, prescription) = ResolveOrCreateRunwayPrescription(request, decisionId, seed);
        stages.Add("RunwayPrescriptionReused");

        var runwaySegment = request.StructuralRoadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway);
        var slice = SliceForWindow(prescription, windowStart, runwaySegment.EndGlobalWeek);
        stages.Add("RunwayBoundedSliceCreated");
        var runwayWeeks = MapRunwayWeeks(slice, request.PlanStartDate, contextVersion);

        if (request.AvailableCoreWeeks is null)
        {
            throw new LongHorizonJitContextInvalidException("A Runway->Core mixed window requires AvailableCoreWeeks.");
        }

        var coreWeeks = SelectCoreWeeks(request.AvailableCoreWeeks, runwaySegment.EndGlobalWeek + 1, windowEnd, request.PlanStartDate, contextVersion);
        stages.Add("CoreBoundedSelectionCreated");

        var newlyActivated = runwayWeeks.Concat(coreWeeks).OrderBy(w => w.GlobalWeekNumber).ToList();
        var window = BuildWindow(request, windowStart, windowEnd, newlyActivated,
            [LongHorizonStructuralSegmentType.PreparationRunway, LongHorizonStructuralSegmentType.Core], decisionId, contextVersion, seed);
        stages.Add("WindowAssembled");

        return Success(LongHorizonRollingJitActivationOutcome.RunwayCoreMixedWindowActivated, request,
            window, newlyActivated, contextVersion, stages, coreTargetLock: lockedTarget, runwayPrescription: prescription, runwaySlice: slice);
    }

    // ── Part 13: Core-only window ────────────────────────────────────────

    private static LongHorizonRollingJitActivationResult ActivateCoreOnlyWindow(
        LongHorizonRollingJitActivationRequest request, int windowStart, int windowEnd,
        LongHorizonContextVersion contextVersion, List<string> stages)
    {
        if (request.AvailableCoreWeeks is null)
        {
            throw new LongHorizonJitContextInvalidException("A Core-only window requires AvailableCoreWeeks.");
        }

        var runwaySegment = request.StructuralRoadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway);
        if (windowStart <= runwaySegment.EndGlobalWeek)
        {
            throw new PreparationRunwayMidRunwayRefreshViolationException(
                "A Core-only window must begin strictly after the Runway global range ends.");
        }

        var coreWeeks = SelectCoreWeeks(request.AvailableCoreWeeks, windowStart, windowEnd, request.PlanStartDate, contextVersion);
        stages.Add("CoreBoundedSelectionCreated");

        var window = BuildWindow(request, windowStart, windowEnd, coreWeeks,
            [LongHorizonStructuralSegmentType.Core], Guid.Empty, contextVersion, string.Empty, coreOnly: true);
        stages.Add("WindowAssembled");

        return Success(LongHorizonRollingJitActivationOutcome.CoreWindowActivated, request, window, coreWeeks, contextVersion, stages);
    }

    // ── shared helpers ───────────────────────────────────────────────────

    private static (LongHorizonLockedCoreWeekOneTarget Lock, ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType> Prescription)
        ResolveOrCreateRunwayPrescription(LongHorizonRollingJitActivationRequest request, Guid decisionId, string seed)
    {
        var runwaySegment = request.StructuralRoadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway);

        if (request.ExistingRunwayPrescription is { } existing && request.ExistingLockedCoreTarget is { } existingLock)
        {
            ImmutablePreparationRunwayPrescriptionValidator.Validate(existing);
            if (existing.StartGlobalWeek != runwaySegment.StartGlobalWeek || existing.EndGlobalWeek != runwaySegment.EndGlobalWeek)
            {
                throw new PreparationRunwayTargetLockScopeViolationException(
                    "Existing Runway prescription's range does not match the structural Runway range -- cannot reuse.");
            }

            return (existingLock, existing);
        }

        if (request.ResolvedCoreWeekOneTarget is null || request.RunwayStartingLoadEvidence is null || request.RunwayStructuralWeeks is null)
        {
            throw new LongHorizonJitContextInvalidException(
                "First Runway entry requires ResolvedCoreWeekOneTarget, RunwayStartingLoadEvidence, and RunwayStructuralWeeks.");
        }

        var lockedTarget = new LongHorizonLockedCoreWeekOneTarget
        {
            TargetWeeklyVolumeKm = request.ResolvedCoreWeekOneTarget.WeeklyVolumeKm,
            TargetLongRunKm = request.ResolvedCoreWeekOneTarget.LongRunDistanceKm,
            Source = LongHorizonEvidenceAuthorityCatalog.CoreWeekOneCurrentProductionSource,
            AuthorityStatus = LongHorizonEvidenceAuthorityStatus.LegacyCurrentProductionSource,
            ContextVersion = LongHorizonContextVersion.Initial(StableGuid(seed + "|target-lock")),
            LockedForActivatedRunwayWeekRange = (runwaySegment.StartGlobalWeek, runwaySegment.EndGlobalWeek),
            CreatedByDecisionId = decisionId,
        };
        LongHorizonCoreTargetLockValidator.Validate(lockedTarget);

        var policy = TenKPreparationRunwayNumericPolicyFactory.Build();
        var materializationRequest = new PreparationRunwayNumericMaterializationRequest<PreparationRunwayBlockType>(
            MapProfile(request.ReadinessProfile), request.RunwayStructuralWeeks, request.RunwayStartingLoadEvidence,
            request.ResolvedCoreWeekOneTarget, policy, PreparationRunwayQuantityUnit.Kilometers);

        var materializationResult = PreparationRunwayNumericMaterializer.Materialize(materializationRequest);
        if (!materializationResult.IsSuccess)
        {
            throw new PreparationRunwayFullPrescriptionInvalidException(
                $"Runway materialization failed: {materializationResult.FailureCode} {materializationResult.FailureReason}");
        }

        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(
            materializationResult,
            request.ValidatedLoad.WeeklyVolumeKm!.Value,
            request.ValidatedLoad.LongRunKm!.Value,
            lockedTarget,
            runwaySegment.StartGlobalWeek,
            request.ReadinessProfile,
            "TEN_K__4D__INTERMEDIATE v10 (unchanged)");

        return (lockedTarget, prescription);
    }

    private static BoundedPreparationRunwayPrescriptionSlice<PreparationRunwayBlockType> SliceForWindow(
        ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType> prescription, int windowStart, int windowEnd)
    {
        var startLocal = windowStart - prescription.StartGlobalWeek + 1;
        var endLocal = windowEnd - prescription.StartGlobalWeek + 1;
        return PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, startLocal, endLocal);
    }

    private static List<ActivatedNumericWeek> MapRunwayWeeks(
        BoundedPreparationRunwayPrescriptionSlice<PreparationRunwayBlockType> slice, DateOnly planStartDate, LongHorizonContextVersion contextVersion) =>
        slice.WeekReferences.Select(weekRef =>
        {
            var production = weekRef.ProductionWeek;
            var sessions = production.OrderedSlots.Select(s => new LongHorizonSessionPrescriptionReference
            {
                SessionOrdinal = s.StructuralSlot.SlotOrdinal,
                SessionRole = LongHorizonSessionRoleCodec.ToCanonicalToken(s.StructuralSlot.SlotRole),
                DistanceKm = s.PlannedDistanceKm,
                WorkoutKey = s.StructuralSlot.WorkoutId,
                WorkoutVersion = s.StructuralSlot.WorkoutVersion,
            }).ToList();

            var weekStart = LongHorizonCalendarAssigner.WeekStartDate(planStartDate, weekRef.GlobalPlanWeek);

            return new ActivatedNumericWeek
            {
                GlobalWeekNumber = weekRef.GlobalPlanWeek,
                SegmentType = LongHorizonStructuralSegmentType.PreparationRunway,
                LifecycleState = LongHorizonNumericLifecycleState.NumericActivated,
                TotalWeeklyVolumeKm = weekRef.WeeklyVolumeKm,
                LongRunKm = weekRef.LongRunKm,
                SessionPrescriptions = sessions,
                CalendarDates = (weekStart, weekStart.AddDays(6)),
                PaceIntensityContext = LongHorizonEvidenceAuthorityCatalog.PaceAndTargetTimeAuthority,
                EvidenceProvenance = weekRef.TargetLock.Source,
                NumericPolicyProvenance = $"PreparationRunwayNumericMaterializer (unchanged); prescription {weekRef.PrescriptionId.Value}",
                ContextVersion = contextVersion,
            };
        }).ToList();

    private static List<ActivatedNumericWeek> SelectCoreWeeks(
        IReadOnlyList<LongHorizonJitCoreCandidateWeek> available, int windowStart, int windowEnd,
        DateOnly planStartDate, LongHorizonContextVersion contextVersion)
    {
        var selected = available
            .Where(w => w.GlobalWeekNumber >= windowStart && w.GlobalWeekNumber <= windowEnd)
            .OrderBy(w => w.GlobalWeekNumber)
            .ToList();

        if (selected.Count != windowEnd - windowStart + 1)
        {
            throw new LongHorizonJitContextInvalidException(
                $"AvailableCoreWeeks does not contain a complete contiguous set for the selected window ({windowStart}-{windowEnd}).");
        }

        return selected.Select(candidate =>
        {
            var weekStart = LongHorizonCalendarAssigner.WeekStartDate(planStartDate, candidate.GlobalWeekNumber);
            return new ActivatedNumericWeek
            {
                GlobalWeekNumber = candidate.GlobalWeekNumber,
                SegmentType = LongHorizonStructuralSegmentType.Core,
                LifecycleState = LongHorizonNumericLifecycleState.NumericActivated,
                TotalWeeklyVolumeKm = candidate.WeeklyVolumeKm,
                LongRunKm = candidate.LongRunKm,
                SessionPrescriptions = candidate.SessionPrescriptions,
                CalendarDates = (weekStart, weekStart.AddDays(6)),
                PaceIntensityContext = LongHorizonEvidenceAuthorityCatalog.PaceAndTargetTimeAuthority,
                EvidenceProvenance = LongHorizonEvidenceAuthorityCatalog.CoreWeekOneCurrentProductionSource,
                NumericPolicyProvenance = $"Existing, unchanged Core generation pipeline (stage {candidate.Stage})",
                ContextVersion = contextVersion,
            };
        }).ToList();
    }

    private static RollingNumericActivationWindow BuildWindow(
        LongHorizonRollingJitActivationRequest request, int windowStart, int windowEnd,
        IReadOnlyList<ActivatedNumericWeek> weeks, IReadOnlyList<LongHorizonStructuralSegmentType> segments,
        Guid decisionId, LongHorizonContextVersion contextVersion, string seed, bool coreOnly = false)
    {
        var window = new RollingNumericActivationWindow
        {
            WindowId = coreOnly ? StableGuid($"core-only|{windowStart}-{windowEnd}|{contextVersion.VersionId}") : StableGuid(seed + "|window"),
            ContextVersion = contextVersion,
            StartGlobalWeek = windowStart,
            EndGlobalWeek = windowEnd,
            RequestedWindowSizeWeeks = 4,
            ActualWindowSizeWeeks = windowEnd - windowStart + 1,
            Weeks = weeks,
            SegmentsCovered = segments,
            ActivationSource = LongHorizonInitialActivationSource.RunwayJitActivation,
            CheckpointDecisionId = decisionId == Guid.Empty ? null : decisionId,
            Status = LongHorizonActivationWindowStatus.Activated,
            DecisionTimestamp = request.CheckpointDate.ToDateTime(TimeOnly.MinValue),
        };

        LongHorizonRollingActivationWindowValidator.Validate(window);
        LongHorizonRollingActivationWindowValidator.ValidateAtomicity(window.Status, window.Weeks);
        foreach (var week in weeks)
        {
            LongHorizonActivatedNumericWeekValidator.Validate(week);
        }

        return window;
    }

    private static LongHorizonRollingJitActivationResult Success(
        LongHorizonRollingJitActivationOutcome outcome, LongHorizonRollingJitActivationRequest request,
        RollingNumericActivationWindow window, IReadOnlyList<ActivatedNumericWeek> newlyActivated,
        LongHorizonContextVersion contextVersion, List<string> stages,
        LongHorizonLockedCoreWeekOneTarget? coreTargetLock = null,
        ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType>? runwayPrescription = null,
        BoundedPreparationRunwayPrescriptionSlice<PreparationRunwayBlockType>? runwaySlice = null)
    {
        var lifecycle = new Dictionary<int, LongHorizonNumericLifecycleState>(request.LifecycleStates);
        foreach (var week in newlyActivated)
        {
            lifecycle[week.GlobalWeekNumber] = LongHorizonNumericLifecycleState.NumericActivated;
        }

        return new LongHorizonRollingJitActivationResult
        {
            Outcome = outcome,
            LifecycleStates = lifecycle,
            ActivationWindow = window,
            NewlyActivatedWeeks = newlyActivated,
            CoreTargetLock = coreTargetLock,
            RunwayPrescription = runwayPrescription,
            RunwaySlice = runwaySlice,
            ContextVersion = contextVersion,
            AuthoritativeReason = null,
            ValidationStages = stages,
        };
    }

    private static LongHorizonRollingJitActivationResult Blocked(
        LongHorizonRollingJitActivationRequest request, LongHorizonContextVersion contextVersion,
        List<string> stages, LongHorizonReasonCode reason) => new()
    {
        Outcome = LongHorizonRollingJitActivationOutcome.JitWindowBlocked,
        LifecycleStates = request.LifecycleStates,
        ActivationWindow = null,
        NewlyActivatedWeeks = [],
        ContextVersion = contextVersion,
        AuthoritativeReason = reason,
        ValidationStages = stages,
    };

    // ── validation/utility ───────────────────────────────────────────────

    private static void ValidateInput(LongHorizonRollingJitActivationRequest request)
    {
        if (request.StructuralRoadmap.Profile != request.ReadinessProfile)
        {
            throw new LongHorizonJitContextInvalidException("ReadinessProfile must match the structural roadmap's own profile.");
        }
    }

    /// <summary>
    /// Phase 10K-FREQ.6D.13 — generalized from the prior hardcoded
    /// <c>== 4</c> literal: the required distinct-availability-day count is
    /// derived from the candidate's own <c>DaysPerWeek</c> (the resolved
    /// RunLayout cardinality), never a broad "4 or 5" widening. Every
    /// existing caller that omits the new field defaults to 4, preserving
    /// exact prior behavior for every historical/live 4D plan.
    /// </summary>
    private static bool IsValidAvailability(IReadOnlyList<DayOfWeek> availability, DayOfWeek longRunDay, int daysPerWeek) =>
        availability.Distinct().Count() == daysPerWeek && availability.Contains(longRunDay);

    private static bool HasUnresolvedPaceOrGoal(IReadOnlyList<RuntimeConditionResolutionResult> conditionResults, out LongHorizonReasonCode? reason)
    {
        foreach (var result in conditionResults)
        {
            if (result.Status == RuntimeConditionResolutionStatus.NotEvaluated
                && string.Equals(result.ConditionType, "PACE_SOURCE_IN", StringComparison.Ordinal))
            {
                reason = LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitPaceSourceUnresolved);
                return true;
            }

            if (result.Status == RuntimeConditionResolutionStatus.NotEvaluated
                && string.Equals(result.ConditionType, "GOAL_FEASIBILITY_IN", StringComparison.Ordinal))
            {
                reason = LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitGoalFeasibilityUnresolved);
                return true;
            }
        }

        reason = null;
        return false;
    }

    private static int? FirstUnstartedPendingWeek(IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> lifecycle, int totalWeeks)
    {
        for (var week = 1; week <= totalWeeks; week++)
        {
            if (lifecycle.TryGetValue(week, out var state) && state == LongHorizonNumericLifecycleState.NumericPending)
            {
                return week;
            }
        }

        return null;
    }

    private static LongHorizonStructuralSegmentType SegmentFor(LongHorizonStructuralRoadmap roadmap, int globalWeek) =>
        roadmap.Segments.Single(s => globalWeek >= s.StartGlobalWeek && globalWeek <= s.EndGlobalWeek).SegmentType;

    private static PreparationRunwayAllocationProfile MapProfile(ReadinessProfile profile) => profile switch
    {
        ReadinessProfile.ConsistencyNeeded => PreparationRunwayAllocationProfile.ConsistencyNeeded,
        ReadinessProfile.CoreEntryReady => PreparationRunwayAllocationProfile.CoreEntryReady,
        _ => throw new LongHorizonJitContextInvalidException($"Unsupported readiness profile {profile}."),
    };

    private static string BuildIdentitySeed(LongHorizonRollingJitActivationRequest request) => string.Join('|',
        request.StructuralRoadmap.TotalWeeks,
        request.StructuralRoadmap.Profile,
        request.CheckpointDate.ToString("yyyy-MM-dd"),
        request.PreviousActivatedWindow.EndGlobalWeek,
        request.ValidatedLoad.WeeklyVolumeKm,
        request.ValidatedLoad.LongRunKm,
        request.PreviousContextVersion.VersionId);

    private static Guid StableGuid(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));
}
