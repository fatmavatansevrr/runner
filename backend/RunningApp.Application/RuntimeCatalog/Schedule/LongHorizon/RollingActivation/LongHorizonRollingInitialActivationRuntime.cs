using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal interface ILongHorizonRollingInitialActivationRuntime
{
    Task<LongHorizonRollingInitialActivationResult> BuildInitialActivationAsync(
        LongHorizonRollingInitialActivationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The only numeric dependency available to the Phase 4K.6 runtime. Its
/// input is already bounded to the selected one-to-four GE descriptors, so
/// neither future GE weeks nor Runway/Core numeric components are reachable.
/// </summary>
internal interface ILongHorizonRollingGeWindowMaterializer
{
    IReadOnlyList<LongHorizonGeWeekNumericResult> Materialize(
        IReadOnlyList<LongHorizonGeWeekDescriptor> selectedGeneralEnduranceWeeks,
        LongHorizonGeEntryBaselineInput onboardingBaseline);
}

internal sealed class ExistingLongHorizonGeWindowMaterializer : ILongHorizonRollingGeWindowMaterializer
{
    /// <summary>
    /// Phase 10K-FREQ.6D.15 -- derives the FREQ.6D.14-approved 5D GE policy
    /// (VolumeSafetyPolicy.FiveDayIntermediate, target-capped growth) off
    /// the selected descriptors' own resolved EasySupportWorkouts.Count
    /// (3 for 5D, already selected by the daysPerWeek-aware
    /// LongHorizonGeStructuralSelector.Select this runtime now calls)
    /// rather than adding a new interface parameter or a DaysPerWeek
    /// special case here. Byte-identical for every 4D caller (Count == 2).
    /// </summary>
    public IReadOnlyList<LongHorizonGeWeekNumericResult> Materialize(
        IReadOnlyList<LongHorizonGeWeekDescriptor> selectedGeneralEnduranceWeeks,
        LongHorizonGeEntryBaselineInput onboardingBaseline)
    {
        // Phase 10K-FREQ.6D.26 -- generalized off the resolved descriptor's own
        // easySupportCount (DaysPerWeek = easySupportCount + 2) into
        // VolumeSafetyPolicy.ForIntermediateDaysPerWeek; applyTargetCap
        // extended to every frequency whose approved numeric authority uses
        // target-capped growth (5D and 6D today), not merely 5D.
        var resolvedDaysPerWeek = (selectedGeneralEnduranceWeeks.Count > 0 ? selectedGeneralEnduranceWeeks[0].EasySupportWorkouts.Count : 2) + 2;
        var policy = Prescription.Volume.VolumeSafetyPolicy.ForIntermediateDaysPerWeek(resolvedDaysPerWeek);
        return LongHorizonGeNumericExecutor.Execute(selectedGeneralEnduranceWeeks, onboardingBaseline, policy, applyTargetCap: resolvedDaysPerWeek is 5 or 6);
    }
}

/// <summary>
/// Phase 4K.6 dark internal runtime. It builds the complete structural
/// roadmap but numerically executes only GE weeks 1..min(4, GE weeks).
/// It has no preview, persistence, Runway numeric, Core generator, JIT,
/// target-lock, controller, or public-DI dependency.
/// </summary>
internal sealed class LongHorizonRollingInitialActivationRuntime : ILongHorizonRollingInitialActivationRuntime
{
    private const int RequestedWindowSizeWeeks = 4;
    private readonly ILongHorizonRollingGeWindowMaterializer _geWindowMaterializer;

    public LongHorizonRollingInitialActivationRuntime(ILongHorizonRollingGeWindowMaterializer? geWindowMaterializer = null)
    {
        _geWindowMaterializer = geWindowMaterializer ?? new ExistingLongHorizonGeWindowMaterializer();
    }

    public async Task<LongHorizonRollingInitialActivationResult> BuildInitialActivationAsync(
        LongHorizonRollingInitialActivationRequest request,
        CancellationToken cancellationToken = default)
    {
        var stages = new List<LongHorizonRollingInitialActivationValidationStage>();
        try
        {
            LongHorizonRollingInitialActivationInputValidator.Validate(request);
            stages.Add(LongHorizonRollingInitialActivationValidationStage.InputAndEligibility);
        }
        catch (LongHorizonRollingInitialActivationException exception)
        {
            return BlockedBeforeRoadmap(exception, stages);
        }

        LongHorizonGeneratedStructuralSkeleton skeleton;
        try
        {
            skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
                request.CompositionDecision,
                request.CatalogRoot,
                request.WorkoutLoader,
                cancellationToken,
                request.DaysPerWeek);
        }
        catch (Exception exception) when (exception is PlanCatalogLoadException or JsonException or InvalidOperationException or IOException)
        {
            return BlockedBeforeRoadmap(new LongHorizonRollingInitialActivationException(
                LongHorizonRollingInitialActivationFailureReason.CatalogResolutionFailure,
                "LONG_HORIZON_ROLLING_INITIAL_STRUCTURAL_CATALOG_RESOLUTION_FAILED",
                "The existing structural/catalog authority could not materialize the roadmap.",
                exception), stages, exception);
        }

        var roadmap = BuildRoadmap(skeleton, request.RaceDate);
        try
        {
            LongHorizonStructuralRoadmapValidator.Validate(roadmap);
            stages.Add(LongHorizonRollingInitialActivationValidationStage.StructuralRoadmap);
        }
        catch (LongHorizonRollingContractException exception)
        {
            return BlockedWithRoadmap(request, skeleton, roadmap, null, null, exception, stages);
        }

        var identitySeed = BuildIdentitySeed(request, skeleton);
        var decisionId = DeterministicGuid($"{identitySeed}|decision");
        var contextVersion = LongHorizonContextVersion.Initial(DeterministicGuid($"{identitySeed}|context"));
        var onboardingEvidence = LongHorizonEvidenceAuthorityRecord.Create(
            LongHorizonEvidenceSource.OriginalOnboardingEvidence,
            LongHorizonEvidenceAuthorityStatus.LegacyCurrentProductionSource,
            "Phase 4K.6 initial-window-only exception: onboarding evidence is permitted before any completed-history checkpoint exists; it is not checkpoint-validated evidence.");
        var context = new LongHorizonInitialActivationContext
        {
            DecisionId = decisionId,
            ContextVersion = contextVersion,
            ActivationSource = LongHorizonInitialActivationSource.InitialOnboardingActivation,
            EvidenceSource = onboardingEvidence,
            SafetyValidationApplied = true,
            FeasibilityValidationApplied = true,
        };

        try
        {
            LongHorizonInitialActivationValidator.Validate(context);
            stages.Add(LongHorizonRollingInitialActivationValidationStage.InitialActivationContext);
        }
        catch (LongHorizonRollingContractException exception)
        {
            return BlockedWithRoadmap(request, skeleton, roadmap, context, null, exception, stages);
        }

        var actualWindowSize = Math.Min(RequestedWindowSizeWeeks, skeleton.GeneralEnduranceWeeks);
        try
        {
            LongHorizonRollingActivationWindowValidator.ValidateInitialSelection(
                skeleton.GeneralEnduranceWeeks,
                1,
                actualWindowSize,
                RequestedWindowSizeWeeks,
                actualWindowSize,
                [LongHorizonStructuralSegmentType.GeneralEndurance],
                LongHorizonInitialActivationSource.InitialOnboardingActivation,
                contextVersion);
            stages.Add(LongHorizonRollingInitialActivationValidationStage.ActivationWindow);
        }
        catch (LongHorizonRollingContractException exception)
        {
            return BlockedWithRoadmap(request, skeleton, roadmap, context, null, exception, stages);
        }

        IReadOnlyList<LongHorizonGeWeekNumericResult> numericResults;
        // Phase 10K-FREQ.6D.26 -- generalized from a DaysPerWeek==5-only binary
        // ternary to the structural identity every GE/Runway week already
        // obeys (exactly 1 KEY + 1 LONG, the remainder EASY): easySupportCount
        // = DaysPerWeek - 2. Byte-identical for every existing caller.
        var selectedDescriptors = LongHorizonGeStructuralSelector
            .Select(skeleton.GeneralEnduranceWeeks, skeleton.ReadinessProfile, request.DaysPerWeek - 2)
            .Take(actualWindowSize)
            .ToList();
        try
        {
            numericResults = _geWindowMaterializer.Materialize(selectedDescriptors, request.OnboardingBaseline);
            if (numericResults.Count != actualWindowSize
                || !numericResults.Select(w => w.WeekIndex).SequenceEqual(Enumerable.Range(1, actualWindowSize)))
            {
                throw new LongHorizonRollingInitialActivationException(
                    LongHorizonRollingInitialActivationFailureReason.GeneralEnduranceNumericInfeasibility,
                    "LONG_HORIZON_ROLLING_INITIAL_GE_RESULT_SHAPE_INVALID",
                    "The bounded GE materializer must return exactly the selected contiguous initial weeks.");
            }
            stages.Add(LongHorizonRollingInitialActivationValidationStage.GeneralEnduranceNumericMaterialization);
        }
        catch (CatalogSessionPrescriptionInfeasibleException exception)
        {
            return BlockedWithRoadmap(request, skeleton, roadmap, context, null,
                new LongHorizonRollingInitialActivationException(
                    LongHorizonRollingInitialActivationFailureReason.SessionAllocationInfeasibility,
                    "LONG_HORIZON_ROLLING_INITIAL_SESSION_ALLOCATION_INFEASIBLE",
                    "The existing four-day session allocation authority rejected the selected GE window.", exception), stages, exception);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            var typed = exception as LongHorizonRollingInitialActivationException
                ?? new LongHorizonRollingInitialActivationException(
                    LongHorizonRollingInitialActivationFailureReason.GeneralEnduranceNumericInfeasibility,
                    "LONG_HORIZON_ROLLING_INITIAL_GE_NUMERIC_INFEASIBLE",
                    "The existing GE numeric authority rejected the selected initial window.", exception);
            return BlockedWithRoadmap(request, skeleton, roadmap, context, null, typed, stages, exception);
        }

        IReadOnlyDictionary<string, DayOfWeek> weekdays;
        try
        {
            weekdays = LongHorizonCalendarAssigner.AssignWeekdays(request.PreferredDays, request.LongRunDay, request.DaysPerWeek);
        }
        catch (InvalidOperationException exception)
        {
            return BlockedWithRoadmap(request, skeleton, roadmap, context, null,
                new LongHorizonRollingInitialActivationException(
                    LongHorizonRollingInitialActivationFailureReason.CalendarAssignmentFailure,
                    "LONG_HORIZON_ROLLING_INITIAL_CALENDAR_ASSIGNMENT_FAILED",
                    "The existing Long-Horizon calendar authority rejected the selected initial window.", exception), stages, exception);
        }

        IReadOnlyList<ActivatedNumericWeek> activated;
        try
        {
            activated = numericResults.Select(numeric => MapActivatedWeek(
                skeleton.Weeks[numeric.WeekIndex - 1], numeric, request, contextVersion,
                onboardingEvidence, weekdays)).ToList();
            foreach (var week in activated)
            {
                LongHorizonActivatedNumericWeekValidator.Validate(week);
            }
            stages.Add(LongHorizonRollingInitialActivationValidationStage.ActivatedNumericWeeks);
        }
        catch (LongHorizonRollingContractException exception)
        {
            return BlockedWithRoadmap(request, skeleton, roadmap, context, null,
                new LongHorizonRollingInitialActivationException(
                    LongHorizonRollingInitialActivationFailureReason.ActivatedWeekContractFailure,
                    "LONG_HORIZON_ROLLING_INITIAL_ACTIVATED_WEEK_INVALID",
                    "An activated GE week failed its existing typed numeric-week contract.", exception), stages, exception);
        }

        var window = new RollingNumericActivationWindow
        {
            WindowId = DeterministicGuid($"{identitySeed}|window"),
            ContextVersion = contextVersion,
            StartGlobalWeek = 1,
            EndGlobalWeek = actualWindowSize,
            RequestedWindowSizeWeeks = RequestedWindowSizeWeeks,
            ActualWindowSizeWeeks = actualWindowSize,
            Weeks = activated,
            SegmentsCovered = [LongHorizonStructuralSegmentType.GeneralEndurance],
            ActivationSource = LongHorizonInitialActivationSource.InitialOnboardingActivation,
            CheckpointDecisionId = null,
            JitContextDecisionId = null,
            Status = LongHorizonActivationWindowStatus.Activated,
            DecisionTimestamp = null,
        };

        try
        {
            LongHorizonRollingActivationWindowValidator.ValidateAtomicity(window.Status, activated);
            LongHorizonRollingActivationWindowValidator.Validate(window);
            stages.Add(LongHorizonRollingInitialActivationValidationStage.Atomicity);
        }
        catch (LongHorizonRollingContractException exception)
        {
            return BlockedWithRoadmap(request, skeleton, roadmap, context, window,
                new LongHorizonRollingInitialActivationException(
                    LongHorizonRollingInitialActivationFailureReason.ActivationWindowAtomicityFailure,
                    "LONG_HORIZON_ROLLING_INITIAL_ATOMICITY_FAILED",
                    "The initial activation window failed all-or-nothing validation.", exception), stages, exception);
        }

        var pending = BuildPendingWeeks(skeleton, actualWindowSize);
        roadmap = roadmap with
        {
            Weeks = roadmap.Weeks.Select(w => w.GlobalWeekNumber <= actualWindowSize
                ? w with { NumericLifecycleState = LongHorizonNumericLifecycleState.NumericActivated }
                : w).ToList(),
        };
        var approved = new LongHorizonRollingInitialActivationResult
        {
            Status = LongHorizonRollingInitialActivationStatus.Approved,
            StructuralRoadmap = roadmap,
            StructuralSkeleton = skeleton,
            InitialActivationContext = context,
            ActivationWindow = window,
            ActivatedNumericWeeks = activated,
            PendingNumericWeeks = pending,
            ContextVersion = contextVersion,
            Validation = new LongHorizonRollingInitialActivationValidationResult
            {
                IsValid = true,
                CompletedStages = stages.Append(LongHorizonRollingInitialActivationValidationStage.FinalResult).ToList(),
            },
            Provenance = BuildProvenance(),
        };

        try
        {
            LongHorizonRollingInitialActivationResultValidator.Validate(approved);
            return approved;
        }
        catch (LongHorizonRollingContractException exception)
        {
            return BlockedWithRoadmap(request, skeleton, roadmap, context, window,
                new LongHorizonRollingInitialActivationException(
                    LongHorizonRollingInitialActivationFailureReason.FinalResultValidationFailure,
                    "LONG_HORIZON_ROLLING_INITIAL_RESULT_VALIDATION_FAILED",
                    "The final dark rolling initial-activation result failed validation.", exception), stages, exception);
        }
    }

    private static LongHorizonStructuralRoadmap BuildRoadmap(LongHorizonGeneratedStructuralSkeleton skeleton, DateOnly raceDate)
    {
        var geEnd = skeleton.GeneralEnduranceWeeks;
        var runwayEnd = geEnd + skeleton.PreparationRunwayWeeks;
        return new LongHorizonStructuralRoadmap
        {
            TotalWeeks = skeleton.TotalWeeks,
            GeneralEnduranceWeeks = skeleton.GeneralEnduranceWeeks,
            PreparationRunwayWeeks = skeleton.PreparationRunwayWeeks,
            CoreWeeks = skeleton.CoreWeeks,
            Segments =
            [
                new LongHorizonStructuralSegment { SegmentType = LongHorizonStructuralSegmentType.GeneralEndurance, StartGlobalWeek = 1, EndGlobalWeek = geEnd, Provenance = skeleton.CompositionPolicyId },
                new LongHorizonStructuralSegment { SegmentType = LongHorizonStructuralSegmentType.PreparationRunway, StartGlobalWeek = geEnd + 1, EndGlobalWeek = runwayEnd, Provenance = skeleton.MaterializerId },
                new LongHorizonStructuralSegment { SegmentType = LongHorizonStructuralSegmentType.Core, StartGlobalWeek = runwayEnd + 1, EndGlobalWeek = skeleton.TotalWeeks, Provenance = skeleton.MaterializerId },
            ],
            GlobalWeekNumbers = skeleton.Weeks.Select(w => w.GlobalWeekNumber).ToList(),
            Weeks = skeleton.Weeks.Select(MapStructuralWeek).ToList(),
            RaceDate = raceDate,
            Profile = skeleton.ReadinessProfile,
            StructuralStatus = "ConfirmedDarkRollingRoadmap",
        };
    }

    private static LongHorizonStructuralWeek MapStructuralWeek(
        Schedule.LongHorizon.LongHorizonStructuralWeek week) => new()
    {
        GlobalWeekNumber = week.GlobalWeekNumber,
        SegmentType = MapSegment(week.Segment),
        Stage = week.GeStageFamily?.ToString() ?? week.RunwayBlock ?? week.CorePhase ?? week.WeekType,
        StructuralWorkoutRoles = week.OrderedWorkoutSlots.Select(s => s.StructuralRole).ToList(),
        CalendarRange = null,
        NumericLifecycleState = LongHorizonNumericLifecycleState.NumericPending,
    };

    private static ActivatedNumericWeek MapActivatedWeek(
        Schedule.LongHorizon.LongHorizonStructuralWeek structural,
        LongHorizonGeWeekNumericResult numeric,
        LongHorizonRollingInitialActivationRequest request,
        LongHorizonContextVersion contextVersion,
        LongHorizonEvidenceAuthorityRecord onboardingEvidence,
        IReadOnlyDictionary<string, DayOfWeek> weekdays)
    {
        var weekStart = LongHorizonCalendarAssigner.WeekStartDate(request.StartDate, structural.GlobalWeekNumber);
        var easyOccurrence = 0;
        var sessions = structural.OrderedWorkoutSlots.Select(slot =>
        {
            if (slot.StructuralRole == "EASY_SUPPORT") easyOccurrence++;
            var roleKey = RoleKeyFor(slot.StructuralRole, easyOccurrence);
            var distance = slot.StructuralRole switch
            {
                "KEY_SESSION" => numeric.KeySessionDistanceKm,
                "EASY_SUPPORT" => numeric.EasySupportDistancesKm[easyOccurrence - 1],
                "LONG_RUN" => numeric.LongRunDistanceKm,
                _ => throw new InvalidOperationException($"Unsupported GE structural role '{slot.StructuralRole}'."),
            };
            var weekday = weekdays[roleKey];
            return new LongHorizonSessionPrescriptionReference
            {
                SessionRole = roleKey,
                DistanceKm = distance,
                WorkoutKey = slot.WorkoutKey,
                WorkoutVersion = slot.WorkoutVersion,
                AssignedDate = LongHorizonCalendarAssigner.AssignedDate(weekStart, weekday),
                Source = "LongHorizonStructuralMaterializer+LongHorizonGeNumericExecutor+LongHorizonCalendarAssigner",
                // Phase 10K-FREQ.6D.15 -- GE never carries more than one KEY session in any
                // approved shape (4D or 5D), so LaneOrdinal stays null (matching the same
                // "no lane ambiguity for GE" invariant FREQ.6D.14 established); SlotOrdinal
                // is the slot's own week-wide positional index (0-based, matching the
                // FREQ.6D.13 Core convention), populated for every role. GE sessions are
                // always Legacy (never ProfileBacked), so ProfileKey/Version stay null --
                // matching the existing both-null-or-both-present invariant.
                LaneOrdinal = null,
                SlotOrdinal = slot.StructuralSlotIndex - 1,
                ProgressionStageKey = structural.GeStageFamily?.ToString() ?? structural.WeekType,
                ProfileKey = null,
                ProfileVersion = null,
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
            CalendarDates = (weekStart, weekStart.AddDays(6)),
            PaceIntensityContext = request.PaceIntensityContext ?? LongHorizonEvidenceAuthorityRecord.Create(
                LongHorizonEvidenceSource.OriginalOnboardingEvidence,
                LongHorizonEvidenceAuthorityStatus.ProvenanceOnly,
                "The existing GE numeric executor has no pace formula dependency; onboarding pace context is retained as provenance only."),
            EvidenceProvenance = onboardingEvidence,
            NumericPolicyProvenance = $"{LongHorizonGeNumericExecutor.PolicyId}/{LongHorizonGeNumericExecutor.PolicyVersion};{LongHorizonGeNumericExecutor.RecoveryPolicyId}",
            ContextVersion = contextVersion,
        };
    }

    private static IReadOnlyList<ActivatedNumericWeek> BuildPendingWeeks(
        LongHorizonGeneratedStructuralSkeleton skeleton,
        int activatedCount)
    {
        return skeleton.Weeks.Skip(activatedCount).Select(week => new ActivatedNumericWeek
        {
            GlobalWeekNumber = week.GlobalWeekNumber,
            SegmentType = MapSegment(week.Segment),
            LifecycleState = LongHorizonNumericLifecycleState.NumericPending,
        }).ToList();
    }

    private static IReadOnlyList<ActivatedNumericWeek> BuildAllNonExecutableWeeks(
        LongHorizonGeneratedStructuralSkeleton skeleton,
        int selectedWindowSize)
    {
        return skeleton.Weeks.Select(week => new ActivatedNumericWeek
        {
            GlobalWeekNumber = week.GlobalWeekNumber,
            SegmentType = MapSegment(week.Segment),
            LifecycleState = week.GlobalWeekNumber <= selectedWindowSize
                ? LongHorizonNumericLifecycleState.NumericActivationBlocked
                : LongHorizonNumericLifecycleState.NumericPending,
        }).ToList();
    }

    private static LongHorizonRollingInitialActivationResult BlockedBeforeRoadmap(
        LongHorizonRollingInitialActivationException exception,
        IReadOnlyList<LongHorizonRollingInitialActivationValidationStage> stages,
        Exception? original = null)
    {
        var result = new LongHorizonRollingInitialActivationResult
        {
            Status = LongHorizonRollingInitialActivationStatus.Blocked,
            ActivatedNumericWeeks = [],
            PendingNumericWeeks = [],
            Validation = new LongHorizonRollingInitialActivationValidationResult
            {
                IsValid = false,
                CompletedStages = stages.Append(LongHorizonRollingInitialActivationValidationStage.FinalResult).ToList(),
            },
            Provenance = BuildProvenance(),
            Failure = BuildFailure(exception, original),
        };
        LongHorizonRollingInitialActivationResultValidator.Validate(result);
        return result;
    }

    private static LongHorizonRollingInitialActivationResult BlockedWithRoadmap(
        LongHorizonRollingInitialActivationRequest request,
        LongHorizonGeneratedStructuralSkeleton skeleton,
        LongHorizonStructuralRoadmap roadmap,
        LongHorizonInitialActivationContext? context,
        RollingNumericActivationWindow? attemptedWindow,
        LongHorizonRollingContractException exception,
        IReadOnlyList<LongHorizonRollingInitialActivationValidationStage> stages,
        Exception? original = null)
    {
        var selectedWindowSize = Math.Min(RequestedWindowSizeWeeks, skeleton.GeneralEnduranceWeeks);
        var blockedWindow = context is null ? null : new RollingNumericActivationWindow
        {
            WindowId = attemptedWindow?.WindowId ?? DeterministicGuid($"{BuildIdentitySeed(request, skeleton)}|window"),
            ContextVersion = context.ContextVersion,
            StartGlobalWeek = 1,
            EndGlobalWeek = selectedWindowSize,
            RequestedWindowSizeWeeks = RequestedWindowSizeWeeks,
            ActualWindowSizeWeeks = selectedWindowSize,
            Weeks = [],
            SegmentsCovered = [LongHorizonStructuralSegmentType.GeneralEndurance],
            ActivationSource = LongHorizonInitialActivationSource.InitialOnboardingActivation,
            Status = LongHorizonActivationWindowStatus.Blocked,
        };
        var pending = BuildAllNonExecutableWeeks(skeleton, selectedWindowSize);

        var result = new LongHorizonRollingInitialActivationResult
        {
            Status = LongHorizonRollingInitialActivationStatus.Blocked,
            StructuralRoadmap = roadmap with
            {
                Weeks = roadmap.Weeks.Select(w => w.GlobalWeekNumber <= selectedWindowSize
                    ? w with { NumericLifecycleState = LongHorizonNumericLifecycleState.NumericActivationBlocked }
                    : w).ToList(),
            },
            StructuralSkeleton = skeleton,
            InitialActivationContext = context,
            ActivationWindow = blockedWindow,
            ActivatedNumericWeeks = [],
            PendingNumericWeeks = pending,
            ContextVersion = context?.ContextVersion,
            Validation = new LongHorizonRollingInitialActivationValidationResult
            {
                IsValid = false,
                CompletedStages = stages.Append(LongHorizonRollingInitialActivationValidationStage.FinalResult).ToList(),
            },
            Provenance = BuildProvenance(),
            Failure = exception is LongHorizonRollingInitialActivationException typed
                ? BuildFailure(typed, original)
                : new LongHorizonRollingInitialActivationFailure
                {
                    Reason = LongHorizonRollingInitialActivationFailureReason.SafetyOrFeasibilityFailure,
                    Code = exception.Code,
                    Message = exception.Message,
                    OriginalExceptionType = (original ?? exception).GetType().Name,
                },
        };
        LongHorizonRollingInitialActivationResultValidator.Validate(result);
        return result;
    }

    private static LongHorizonRollingInitialActivationFailure BuildFailure(
        LongHorizonRollingInitialActivationException exception,
        Exception? original) => new()
    {
        Reason = exception.Reason,
        Code = exception.Code,
        Message = exception.Message,
        OriginalExceptionType = (original ?? exception).GetType().Name,
    };

    private static IReadOnlyList<string> BuildProvenance() =>
    [
        LongHorizonCompositionResolver.PolicyId,
        LongHorizonStructuralMaterializer.MaterializerId,
        LongHorizonGeNumericExecutor.PolicyId,
        LongHorizonGeNumericExecutor.RecoveryPolicyId,
        LongHorizonCalendarAssigner.AssignerId,
        "PHASE4K_6_INITIAL_ONBOARDING_ACTIVATION_V1",
    ];

    private static LongHorizonStructuralSegmentType MapSegment(LongHorizonSegmentType segment) => segment switch
    {
        LongHorizonSegmentType.LongHorizonGeneralEndurance => LongHorizonStructuralSegmentType.GeneralEndurance,
        LongHorizonSegmentType.PreparationRunway => LongHorizonStructuralSegmentType.PreparationRunway,
        LongHorizonSegmentType.Core => LongHorizonStructuralSegmentType.Core,
        _ => throw new ArgumentOutOfRangeException(nameof(segment)),
    };

    /// <summary>
    /// Phase 10K-FREQ.6D.15 -- generalized off the week's own EASY_SUPPORT
    /// occurrence ordinal (1-based rank among EASY_SUPPORT slots only, not
    /// the raw structural slot index) instead of a hardcoded
    /// <c>&lt;=2 ? "_1" : "_2"</c> literal. Byte-identical for every 4D week
    /// (exactly 2 EASY_SUPPORT slots, occurrence 1 and 2).
    /// </summary>
    private static string RoleKeyFor(string structuralRole, int easySupportOccurrence) => structuralRole switch
    {
        "KEY_SESSION" => "KEY_SESSION",
        "LONG_RUN" => "LONG_RUN",
        "EASY_SUPPORT" => $"EASY_SUPPORT_{easySupportOccurrence}",
        _ => throw new ArgumentOutOfRangeException(nameof(structuralRole)),
    };

    private static string BuildIdentitySeed(
        LongHorizonRollingInitialActivationRequest request,
        LongHorizonGeneratedStructuralSkeleton skeleton) => string.Join('|',
        skeleton.CandidateKey,
        skeleton.CandidateVersion,
        skeleton.MaterializerVersion,
        skeleton.TotalWeeks,
        skeleton.ReadinessProfile,
        request.StartDate.ToString("yyyy-MM-dd"),
        request.RaceDate.ToString("yyyy-MM-dd"),
        request.OnboardingBaseline.RecentWeeklyVolumeKm,
        request.OnboardingBaseline.RecentLongestRunKm,
        request.OnboardingBaseline.RecentRunsPerWeek,
        string.Join(',', request.PreferredDays),
        request.LongRunDay);

    private static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }
}
