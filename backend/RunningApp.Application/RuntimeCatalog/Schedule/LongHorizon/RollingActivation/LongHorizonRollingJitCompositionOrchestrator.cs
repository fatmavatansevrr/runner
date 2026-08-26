using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using System.Security.Cryptography;
using System.Text;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.8C — the dark composition orchestrator that closes Phase 4K.8's
/// disclosed gap: instead of requiring a caller-supplied Core Week-1
/// target/available Core weeks/resolved condition results, this type
/// produces all three for real, then invokes the unchanged Phase 4K.8
/// <see cref="ILongHorizonRollingJitActivationRuntime"/>. It never
/// duplicates window selection, mixed-window atomicity, lifecycle
/// transitions, the direction guard, target-lock validation, or bounded
/// Runway slice selection -- all of those remain Phase 4K.8's own
/// authority.
/// </summary>
internal interface ILongHorizonRollingJitCompositionOrchestrator
{
    Task<LongHorizonRollingJitCompositionResult> ComposeAndActivateNextWindowAsync(
        LongHorizonRollingJitCompositionRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class LongHorizonRollingJitCompositionOrchestrator : ILongHorizonRollingJitCompositionOrchestrator
{
    private readonly ILongHorizonRollingJitActivationRuntime _activationRuntime;

    public LongHorizonRollingJitCompositionOrchestrator(ILongHorizonRollingJitActivationRuntime? activationRuntime = null)
    {
        _activationRuntime = activationRuntime ?? new LongHorizonRollingJitActivationRuntime();
    }

    public async Task<LongHorizonRollingJitCompositionResult> ComposeAndActivateNextWindowAsync(
        LongHorizonRollingJitCompositionRequest request,
        CancellationToken cancellationToken = default)
    {
        var stages = new List<string>();

        try
        {
            ValidateInput(request);
            stages.Add("CompositionInputValidation");

            if (request.SafetyState == LongHorizonSafetyState.UnresolvedSafetyCritical)
            {
                stages.Add("SafetyPrecheck");
                return Blocked(stages, LongHorizonReasonCode.SafetyReassessmentRequired);
            }
            stages.Add("SafetyPrecheck");

            if (request.ValidatedLoad.ValidationStatus != LongHorizonValidationStatus.Valid)
            {
                stages.Add("ValidatedLoadAuthority");
                return Blocked(stages, LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitValidatedLoadUnavailable));
            }
            stages.Add("ValidatedLoadAuthority");

            var runwaySegment = request.StructuralRoadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway);
            var coreSegment = request.StructuralRoadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.Core);
            var runwayStartDate = request.PlanStartDate.AddDays((runwaySegment.StartGlobalWeek - 1) * 7);

            var (previewRequest, resolverInput) = LongHorizonRollingCoreGenerationInputAdapter.Build(
                request.ValidatedLoad, request.ExactCompletedFrequency, runwayStartDate, request.RaceDate,
                request.TargetFinishTimeSeconds, request.TargetFinishTimeSource, request.RecentRace,
                request.PreferredDays, request.LongRunDay);
            stages.Add("RollingCoreInputMapping");

            // Part 2: real, unmodified condition resolution -- the same four
            // stateless resolvers RunningApp.Api registers for production DI.
            var resolutionService = new RuntimeConditionResolutionService(
                new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());
            var resolverContext = new RuntimeResolverContext
            {
                InputSnapshot = resolverInput,
                AsOfDate = request.CheckpointDate,
                CoreCycle = request.Candidate.CoreCycle,
            };
            var conditionResults = resolutionService.ResolveAllResults(resolverContext);
            stages.Add("ConditionResolution");

            if (HasUnresolvedPaceOrGoal(conditionResults, out var conditionReason))
            {
                stages.Add("ConditionResolutionCheck");
                return Blocked(stages, conditionReason!.Value, conditionResults);
            }
            stages.Add("ConditionResolutionCheck");

            var coreEntryReadiness = conditionResults.SingleOrDefault(r => r.ConditionType == CoreEntryReadinessResolver.ConditionTypeValue)
                ?? throw new LongHorizonJitContextInvalidException("CoreEntryReadinessResolver did not produce a result.");

            // A Core-touching window is needed at first Runway entry, or
            // whenever the next 4-week cap could reach Core -- a narrow,
            // one-line boundary lookup, never the actual window-selection
            // decision itself (that remains Phase 4K.8's own authority).
            var firstPendingWeek = FirstUnstartedPendingWeek(request.LifecycleStates, request.StructuralRoadmap.TotalWeeks);
            var needsRunwayEntry = request.ExistingRunwayPrescription is null;
            var mayReachCore = firstPendingWeek is not null && firstPendingWeek.Value + 3 > runwaySegment.EndGlobalWeek;
            var needsCoreGeneration = needsRunwayEntry || mayReachCore;

            TenKPreparationRunwayDarkOrchestrationResult? realComposition = null;
            PreparationRunwayCoreWeekOneNumericTarget? extractedTarget = null;
            LongHorizonBoundedCorePrescriptionSelection? boundedCoreSelection = null;
            IReadOnlyList<PreparationRunwayWeekMaterialization.PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>>? runwayStructuralWeeks = null;
            PreparationRunwayNumericMaterialization.PreparationRunwayStartingLoadEvidence? runwayStartingEvidence = null;

            if (needsCoreGeneration)
            {
                var orchestrator = TenKPreparationRunwayDarkOrchestratorFactory.Create(new PlanCatalogOptions { CatalogRootPath = request.CatalogRootPath });
                var compositionRequest = new TenKPreparationRunwayDarkOrchestrationRequest(
                    request.Candidate, runwayStartDate, request.RaceDate, request.CheckpointDate,
                    request.PreferredDays, request.LongRunDay, coreEntryReadiness, conditionResults,
                    previewRequest, resolverInput, PreparationRunwayQuantityUnit.Kilometers);
                stages.Add("RealCoreRunwayCompositionRequestBuilt");

                realComposition = await orchestrator.OrchestrateAsync(compositionRequest, cancellationToken);
                stages.Add("RealCoreGeneratorInvocation");

                if (!realComposition.IsSuccess)
                {
                    stages.Add("RealCompositionFailed");
                    return Blocked(stages, MapCompositionFailure(realComposition), conditionResults, realComposition);
                }

                extractedTarget = PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior(
                    realComposition.CoreResult!.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan,
                    realComposition.CoreResult!.PrescriptionResult.FinalPrescribedPlan);
                stages.Add("CoreWeekOneTargetExtraction");

                if (needsRunwayEntry)
                {
                    runwayStructuralWeeks = realComposition.StructuralRunway!.Weeks;
                    runwayStartingEvidence = BuildStartingEvidence(request.ValidatedLoad, request.ExactCompletedFrequency);
                }

                boundedCoreSelection = BuildBoundedCoreSelection(realComposition, coreSegment, request.PreviousContextVersion);
                stages.Add("CoreBoundedSelection");
            }

            var activationRequest = new LongHorizonRollingJitActivationRequest
            {
                StructuralRoadmap = request.StructuralRoadmap,
                LifecycleStates = request.LifecycleStates,
                PreviousActivatedWindow = request.PreviousActivatedWindow,
                EvidenceSnapshot = request.EvidenceSnapshot,
                ValidatedLoad = request.ValidatedLoad,
                GeCheckpointDecision = request.GeCheckpointDecision,
                GeActivatedWeeks = request.GeActivatedWeeks,
                PreviousContextVersion = request.PreviousContextVersion,
                ReadinessProfile = request.ReadinessProfile,
                CurrentAvailability = request.CurrentAvailability,
                PreferredDays = request.PreferredDays,
                LongRunDay = request.LongRunDay,
                CheckpointDate = request.CheckpointDate,
                RaceDate = request.RaceDate,
                SafetyState = request.SafetyState,
                ConditionResults = conditionResults,
                ResolvedCoreWeekOneTarget = extractedTarget,
                RunwayStartingLoadEvidence = runwayStartingEvidence,
                RunwayStructuralWeeks = runwayStructuralWeeks,
                ExistingLockedCoreTarget = request.ExistingLockedCoreTarget,
                ExistingRunwayPrescription = request.ExistingRunwayPrescription,
                AvailableCoreWeeks = boundedCoreSelection?.SelectedWeeks,
                PlanStartDate = request.PlanStartDate,
            };
            stages.Add("Phase4K8RuntimeRequestComposed");

            var activationResult = await _activationRuntime.ResolveAndActivateNextWindowAsync(activationRequest, cancellationToken);
            stages.Add("Phase4K8RuntimeInvocation");

            if (activationResult.Outcome == LongHorizonRollingJitActivationOutcome.JitWindowBlocked)
            {
                stages.Add("Phase4K8RuntimeBlocked");
                return Blocked(stages, activationResult.AuthoritativeReason!.Value, conditionResults, realComposition);
            }

            var calendarProjection = LongHorizonRealCalendarProjectionAdapter.MapSelectedWindow(
                realComposition, activationResult, request.StructuralRoadmap,
                request.PreferredDays, request.LongRunDay, request.ExistingRunwayCalendarProjection);
            stages.AddRange(calendarProjection.ValidationStages);

            var alignedActivation = LongHorizonRealCalendarProjectionAdapter.AlignActivationResult(
                activationResult, calendarProjection, request.PreferredDays, request.LongRunDay);
            stages.Add("PerWeekCalendarAlignment");
            stages.Add("MixedBoundaryContinuity");
            stages.Add("ActivatedNumericWeekValidation");
            stages.Add("FinalActivationResultValidation");

            return new LongHorizonRollingJitCompositionResult
            {
                Outcome = LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded,
                ActivationResult = alignedActivation,
                ResolvedConditionResults = conditionResults,
                CoreGenerationProvenance = needsCoreGeneration
                    ? "TenKPreparationRunwayDarkOrchestrator (real, unchanged production factory)"
                    : "Reused existing Runway prescription/Core target lock -- no regeneration (Runway continuation window)",
                ExtractedCoreWeekOneTarget = extractedTarget,
                BoundedCoreSelection = boundedCoreSelection,
                RealCompositionResult = realComposition,
                ActivatedSessionCalendarProjection = calendarProjection.SelectedSessions,
                FullRunwayCalendarProjection = calendarProjection.FullRunwayProjection,
                CalendarProjectionId = calendarProjection.ProjectionId,
                ContextVersion = alignedActivation.ContextVersion,
                AuthoritativeReason = null,
                InternalDiagnostic = null,
                ValidationStages = stages,
            };
        }
        catch (LongHorizonRollingContractException exception)
        {
            stages.Add("CompositionContractValidationFailure");
            return Blocked(stages, MapAlignmentFailure(exception), diagnostic: $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static LongHorizonBoundedCorePrescriptionSelection BuildBoundedCoreSelection(
        TenKPreparationRunwayDarkOrchestrationResult composition, LongHorizonStructuralSegment coreSegment, LongHorizonContextVersion previousVersion)
    {
        var finalPlan = composition.CoreResult!.PrescriptionResult.FinalPrescribedPlan;
        var weeks = finalPlan.Weeks.Select(week =>
        {
            var datedCoreWeek = composition.CalendarComposition!.DatedCoreWeeks!
                .Single(w => w.SegmentLocalWeekNumber == week.WeekNumber).CoreWeek;
            var remainingByRole = week.Sessions.OrderBy(s => s.Date).ThenBy(s => s.StructuralRole)
                .GroupBy(s => s.StructuralRole, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => new Queue<RunningApp.Application.RuntimeCatalog.Prescription.Session.CatalogPrescribedSession>(g), StringComparer.Ordinal);
            var sessions = new List<LongHorizonSessionPrescriptionReference>();
            foreach (var datedSlot in datedCoreWeek.SessionSlots.OrderBy(slot => slot.SlotOrderInWeek))
            {
                if (!remainingByRole.TryGetValue(datedSlot.StructuralRole, out var candidates) || candidates.Count == 0)
                    throw new LongHorizonJitContextInvalidException($"Core week {week.WeekNumber} role {datedSlot.StructuralRole} has no prescribed-session identity.");
                var prescribed = candidates.Dequeue();
                sessions.Add(new LongHorizonSessionPrescriptionReference
                {
                    SessionOrdinal = datedSlot.SlotOrderInWeek,
                    SessionRole = prescribed.StructuralRole,
                    DistanceKm = prescribed.PlannedDistanceKm,
                    WorkoutKey = prescribed.WorkoutDefinitionKey,
                    WorkoutVersion = prescribed.WorkoutDefinitionVersion,
                    AssignedDate = datedSlot.SessionDate,
                    Source = "CalendarComposition.DatedCoreWeeks + FinalPrescribedPlan role-occurrence identity",
                });
            }
            var longRunKm = week.Sessions.Count == 0 ? 0 : week.Sessions.Max(s => s.PlannedDistanceKm);

            return new LongHorizonJitCoreCandidateWeek
            {
                GlobalWeekNumber = coreSegment.StartGlobalWeek + week.WeekNumber - 1,
                WeeklyVolumeKm = week.PlannedWeeklyVolumeKm,
                LongRunKm = longRunKm,
                SessionPrescriptions = sessions,
                Stage = week.PhaseKey,
            };
        }).ToList();

        return new LongHorizonBoundedCorePrescriptionSelection
        {
            CoreContextId = StableGuid(string.Join('|', previousVersion.VersionId, coreSegment.StartGlobalWeek,
                string.Join(';', weeks.Select(w => $"{w.GlobalWeekNumber}:{w.WeeklyVolumeKm}:{w.LongRunKm}")))),
            CoreContextVersion = previousVersion.Next(),
            FullCoreResultProvenance = "TenKPreparationRunwayDarkOrchestrator.CoreResult.PrescriptionResult.FinalPrescribedPlan (real, unchanged)",
            SelectedStartGlobalWeek = weeks.Count == 0 ? coreSegment.StartGlobalWeek : weeks[0].GlobalWeekNumber,
            SelectedEndGlobalWeek = weeks.Count == 0 ? coreSegment.StartGlobalWeek : weeks[^1].GlobalWeekNumber,
            SelectedWeeks = weeks,
        };
    }

    private static PreparationRunwayNumericMaterialization.PreparationRunwayStartingLoadEvidence BuildStartingEvidence(
        ValidatedSustainableLoad validatedLoad, int? exactCompletedFrequency) => new(
        PreparationRunwayNumericMaterialization.PreparationRunwayLoadEvidenceState.Provided, validatedLoad.WeeklyVolumeKm,
        PreparationRunwayNumericMaterialization.PreparationRunwayLoadEvidenceState.Provided, validatedLoad.LongRunKm,
        exactCompletedFrequency, "Phase 4K.8C rolling Core input adapter -- ValidatedSustainableLoad (completed training history)");

    private static LongHorizonReasonCode MapCompositionFailure(TenKPreparationRunwayDarkOrchestrationResult composition)
    {
        var stage = composition.Failure!.Stage;
        return stage switch
        {
            TenKPreparationRunwayOrchestrationStage.CoreGeneration or TenKPreparationRunwayOrchestrationStage.AllocationPolicy
                => LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.CoreJitContextUnavailable),
            TenKPreparationRunwayOrchestrationStage.NumericMaterialization or TenKPreparationRunwayOrchestrationStage.StructuralMaterialization
                or TenKPreparationRunwayOrchestrationStage.BlockAllocation or TenKPreparationRunwayOrchestrationStage.ProgressionLoading
                or TenKPreparationRunwayOrchestrationStage.WorkoutBinding
                => LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.RunwayJitContextUnavailable),
            TenKPreparationRunwayOrchestrationStage.CalendarComposition
                => LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitAvailabilityInfeasible),
            _ => LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitEvidenceConflictUnresolved),
        };
    }

    private static LongHorizonReasonCode MapAlignmentFailure(LongHorizonRollingContractException exception) => exception switch
    {
        LongHorizonSessionCalendarProjectionMismatchException or LongHorizonCalendarIdentityMismatchException
            => LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitEvidenceConflictUnresolved),
        LongHorizonActivatedCalendarAlignmentException when exception.Message.Contains("preferred", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("long run", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("structural week", StringComparison.OrdinalIgnoreCase)
            => LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitAvailabilityInfeasible),
        _ => LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitSegmentTransitionInfeasible),
    };

    private static bool HasUnresolvedPaceOrGoal(IReadOnlyList<RuntimeConditionResolutionResult> conditionResults, out LongHorizonReasonCode? reason)
    {
        foreach (var result in conditionResults)
        {
            if (result.Status == RuntimeConditionResolutionStatus.NotEvaluated && string.Equals(result.ConditionType, "PACE_SOURCE_IN", StringComparison.Ordinal))
            {
                reason = LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitPaceSourceUnresolved);
                return true;
            }

            if (result.Status == RuntimeConditionResolutionStatus.NotEvaluated && string.Equals(result.ConditionType, "GOAL_FEASIBILITY_IN", StringComparison.Ordinal))
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

    private static void ValidateInput(LongHorizonRollingJitCompositionRequest request)
    {
        if (request.StructuralRoadmap.Profile != request.ReadinessProfile)
        {
            throw new LongHorizonJitContextInvalidException("ReadinessProfile must match the structural roadmap's own profile.");
        }
    }

    private static LongHorizonRollingJitCompositionResult Blocked(
        List<string> stages, LongHorizonReasonCode reason,
        IReadOnlyList<RuntimeConditionResolutionResult>? conditionResults = null,
        TenKPreparationRunwayDarkOrchestrationResult? realComposition = null,
        string? diagnostic = null) => new()
    {
        Outcome = LongHorizonRollingJitCompositionOutcome.CompositionBlocked,
        ActivationResult = null,
        ResolvedConditionResults = conditionResults,
        RealCompositionResult = null, // never expose a partial/failed real composition result externally.
        ContextVersion = LongHorizonContextVersion.Initial(),
        AuthoritativeReason = reason,
        InternalDiagnostic = diagnostic,
        ValidationStages = stages,
    };

    private static Guid StableGuid(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));
}
