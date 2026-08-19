using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4G.6C — the pure, production-owned mapper from a
/// completed, successful <see cref="TenKPreparationRunwayDarkOrchestrationResult"/>
/// to a persistable <see cref="GeneratedCatalogPlanPayload"/> — the SAME
/// contract <see cref="RuntimeCatalog.PreviewRouting.CatalogPlanConfirmationService"/>
/// already knows how to validate and persist for the existing 8-14 week Core
/// path. This is the entire reason no new confirmation/persistence machinery
/// was written for the runway: the existing, deeply-tested transactional,
/// idempotent, concurrency-safe confirmation service is reused byte-for-byte
/// by producing the exact shape it already consumes.
///
/// Deterministic and side-effect-free: reads only from the supplied,
/// already-completed <paramref name="result"/> and <paramref name="candidate"/>
/// — never recalculates a distance, date, or pace, never touches the
/// database, never invokes any generation/allocation/materialization
/// component. Global week numbering comes directly from
/// <see cref="TenKPreparationRunwayDarkOrchestrationResult.CalendarComposition"/>'s
/// own <c>OrderedCombinedWeeks</c> — never recomputed.
///
/// Runway weeks carry the runway block's public name (e.g. "CONSISTENCY") as
/// both <see cref="GeneratedCatalogWeekPayload.StageKey"/> and
/// <see cref="GeneratedCatalogWeekProvenance.SourcePhaseKey"/> — the same
/// field <see cref="CatalogPlanConfirmationService"/> already reads to set
/// <c>TrainingWeek.WeekType</c>/<c>CatalogPhaseKey</c>, extended (see that
/// service's <c>MapWeekType</c>) to recognize the four runway block names and
/// map them to <see cref="TrainingWeekType.PreparationRunway"/> — never
/// mislabeled as Foundation/Build/Taper. Core weeks are mapped with the exact
/// same logic <see cref="Schedule.CatalogPublicPreviewMaterializer"/> already
/// uses for the standalone 8-14 week path (workout-type/pace/segment mapping
/// reused directly, never duplicated), only with globally-offset week
/// numbers instead of the standalone plan's own 1..12 local numbering.
/// </summary>
internal static class PreparationRunwayPersistablePlanMapper
{
    public const string MapperKey = "PREPARATION_RUNWAY_PERSISTABLE_PLAN_MAPPER";
    public const int MapperVersion = 1;

    public static GeneratedCatalogPlanPayload Map(
        TenKPreparationRunwayDarkOrchestrationResult result,
        PlanCatalogCandidateSummary candidate,
        DateOnly planStartDate,
        DateOnly asOfDate)
    {
        var composedWeeks = result.CalendarComposition!.OrderedCombinedWeeks!;
        var pacedRunwayByGlobalWeek = result.PacedRunway!.PacedRunwayWeeks!
            .ToDictionary(w => w.OriginalWeek.GlobalWeekNumber);
        var coreWeeksByLocalNumber = result.CoreResult!.PrescriptionResult.FinalPrescribedPlan.Weeks
            .ToDictionary(w => w.WeekNumber);

        var weeks = composedWeeks
            .OrderBy(w => w.GlobalWeekNumber)
            .Select(composed => composed.SegmentType == PreparationRunwaySegmentType.PreparationRunway
                ? MapRunwayWeek(planStartDate, composed.GlobalWeekNumber, pacedRunwayByGlobalWeek[composed.GlobalWeekNumber])
                : MapCoreWeek(planStartDate, composed.GlobalWeekNumber, coreWeeksByLocalNumber[composed.SegmentLocalWeekNumber]))
            .ToArray();

        var dependencyVersions = DependencyVersions(candidate);

        return new GeneratedCatalogPlanPayload
        {
            SchemaVersion = GeneratedCatalogPlanPayload.CurrentSchemaVersion,
            StartDate = planStartDate,
            EndDate = planStartDate.AddDays((weeks.Length * 7) - 1),
            PlannedWeekCount = weeks.Length,
            DaysPerWeek = candidate.DaysPerWeek,
            CanonicalDistanceFamily = candidate.CanonicalDistanceFamily,
            GoalType = GoalType.Race,
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            DependencyVersions = dependencyVersions,
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanProvenance
            {
                CandidateKey = candidate.CandidateKey,
                CandidateVersion = candidate.CandidateVersion,
                DependencyVersions = dependencyVersions,
                GenerationSource = GenerationSource.Catalog,
                AsOfDate = asOfDate,
                MaterializerVersion = $"{MapperKey} v{MapperVersion}",
            },
        };
    }

    private static GeneratedCatalogWeekPayload MapRunwayWeek(
        DateOnly planStartDate, int globalWeekNumber, PreparationRunwayPacedWeek<PreparationRunwayBlockType> pacedWeek)
    {
        var weekStart = planStartDate.AddDays((globalWeekNumber - 1) * 7);
        var blockType = pacedWeek.OriginalWeek.PrescribedWeek.StructuralWeek.BlockType;
        var blockName = PreparationRunwayPublicPreviewMapper.RunwayBlockPublicName(blockType);

        var sessions = pacedWeek.ChronologicalSlots
            .Select((slot, index) => MapRunwaySession(slot, index + 1, blockName))
            .ToArray();
        var plannedVolumeKm = sessions.Sum(s => s.TargetDistanceKm ?? 0);

        return new GeneratedCatalogWeekPayload
        {
            WeekNumber = globalWeekNumber,
            StartDate = weekStart,
            EndDate = weekStart.AddDays(6),
            StageKey = blockName,
            PlannedVolumeKm = plannedVolumeKm,
            Sessions = sessions,
            Provenance = new GeneratedCatalogWeekProvenance
            {
                StageKey = blockName,
                SourcePhaseKey = blockName,
                VolumeRuleKey = "PREPARATION_RUNWAY_NUMERIC_MATERIALIZATION_V1",
                ProgressionReferenceKey = blockName,
            },
        };
    }

    private static GeneratedCatalogTrainingDayPayload MapRunwaySession(
        PreparationRunwayPacedSlot<PreparationRunwayBlockType> slot, int order, string blockName)
    {
        var role = slot.OriginalSlot.PrescribedSlot.StructuralSlot.SlotRole;
        var roleName = role switch
        {
            PreparationRunwaySlotRole.LongRun => "LONG_RUN",
            PreparationRunwaySlotRole.KeySession => "KEY_SESSION",
            _ => "EASY_SUPPORT",
        };
        var workoutType = role switch
        {
            PreparationRunwaySlotRole.LongRun => GeneratedCatalogWorkoutType.LongRun,
            PreparationRunwaySlotRole.KeySession => GeneratedCatalogWorkoutType.Tempo,
            _ => GeneratedCatalogWorkoutType.Easy,
        };
        var structural = slot.OriginalSlot.PrescribedSlot.StructuralSlot;

        return new GeneratedCatalogTrainingDayPayload
        {
            Date = slot.OriginalSlot.SessionDate,
            SessionOrderInWeek = order,
            WorkoutType = workoutType,
            PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
            TargetDistanceKm = slot.OriginalSlot.PrescribedSlot.PlannedDistanceKm,
            TargetDurationMinutes = null,
            EstimatedDistanceKm = null,
            EstimatedDurationMinutes = null,
            PlannedIntensity = slot.PacePrescription.EffortLabel,
            PacePrescription = new GeneratedCatalogPacePrescription
            {
                PaceType = GeneratedCatalogPaceType.EffortOnly,
                EffortLabel = slot.PacePrescription.EffortLabel,
                DisplayText = "PREPARATION_RUNWAY_CONTROLLED_EFFORT_NO_NUMERIC_DERIVATION",
            },
            Segments = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
            Provenance = new GeneratedCatalogDayProvenance
            {
                SourceStageKey = blockName,
                SourceWorkoutKey = structural.WorkoutId,
                SourceWorkoutVersion = structural.WorkoutVersion,
                SourceProgressionStepKey = null,
                SourceLayoutSlotRole = roleName,
            },
        };
    }

    private static GeneratedCatalogWeekPayload MapCoreWeek(DateOnly planStartDate, int globalWeekNumber, CatalogPrescribedWeek coreWeek)
    {
        var weekStart = planStartDate.AddDays((globalWeekNumber - 1) * 7);
        var sessions = coreWeek.Sessions
            .OrderBy(s => s.Date)
            .Select((s, index) => MapCoreSession(s, index + 1))
            .ToArray();
        var stageKey = coreWeek.Sessions.FirstOrDefault(s => s.StructuralRole == "KEY_SESSION")?.ProgressionStageKey ?? coreWeek.PhaseKey;

        return new GeneratedCatalogWeekPayload
        {
            WeekNumber = globalWeekNumber,
            StartDate = weekStart,
            EndDate = weekStart.AddDays(6),
            StageKey = stageKey,
            PlannedVolumeKm = coreWeek.PlannedWeeklyVolumeKm,
            Sessions = sessions,
            Provenance = new GeneratedCatalogWeekProvenance
            {
                StageKey = stageKey,
                SourcePhaseKey = coreWeek.PhaseKey,
                VolumeRuleKey = coreWeek.AllocationTrace.PolicyKey + " v" + coreWeek.AllocationTrace.PolicyVersion,
                ProgressionReferenceKey = "TEN_K_WORKOUT_PROGRESSION_V1",
            },
        };
    }

    private static GeneratedCatalogTrainingDayPayload MapCoreSession(CatalogPrescribedSession session, int order) => new()
    {
        Date = session.Date,
        SessionOrderInWeek = order,
        WorkoutType = V1CatalogPublicWorkoutTypeMappingPolicy.Map(session),
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
        TargetDistanceKm = session.PlannedDistanceKm,
        TargetDurationMinutes = null,
        EstimatedDistanceKm = null,
        EstimatedDurationMinutes = CatalogPublicPreviewMaterializer.MapDuration(session.Prescription.DurationPrescription).EstimatedDurationMinutes,
        PlannedIntensity = session.Prescription.EffortGuidance,
        PacePrescription = CatalogPublicPreviewMaterializer.MapPace(session.Prescription.PacePrescription).Pace,
        Segments = session.Prescription.OrderedSegments
            .OrderBy(s => s.SequenceOrder)
            .Select(V1CatalogPublicSegmentMappingPolicy.Map)
            .ToArray(),
        Provenance = new GeneratedCatalogDayProvenance
        {
            SourceStageKey = session.ProgressionStageKey ?? session.PhaseKey,
            SourceWorkoutKey = session.WorkoutDefinitionKey,
            SourceWorkoutVersion = session.WorkoutDefinitionVersion,
            SourceProgressionStepKey = session.ProgressionStageKey,
            SourceLayoutSlotRole = session.StructuralRole,
            SourcePrescriptionProfileKey = session.PrescriptionSource.ExactProfileKeyOrNull(),
            SourcePrescriptionProfileVersion = session.PrescriptionSource.ExactProfileVersionOrNull(),
        },
    };

    private static IReadOnlyDictionary<string, PlanCatalogReference> DependencyVersions(PlanCatalogCandidateSummary candidate) =>
        new Dictionary<string, PlanCatalogReference>
        {
            ["masterTemplate"] = candidate.MasterTemplate,
            ["layout"] = candidate.Layout,
            ["levelModifier"] = candidate.LevelModifier,
            ["workoutProgression"] = candidate.WorkoutProgression,
            ["progressionModifier"] = candidate.ProgressionModifier,
            ["rulePack"] = candidate.RulePack,
            ["peakVolumeBandPolicy"] = candidate.PeakVolumeBandPolicy,
            ["runtimeConditionValueRegistry"] = candidate.RuntimeConditionValueRegistry,
        };
}
