using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule;

internal sealed record CatalogPublicPreviewMaterializationRequest(
    GeneratePreviewRequest PreviewRequest,
    PlanCatalogCandidateSummary Candidate,
    DateOnly PlanStartDate,
    DateOnly AsOfDate,
    CatalogVolumeAndLongRunPlan VolumePlan,
    CatalogPrescribedPlan FinalPrescribedPlan);

internal sealed record CatalogPublicMaterializationResult(
    GeneratedCatalogPlanPayload Payload,
    GeneratedCatalogPlanPayloadValidationResult ValidationResult);

internal sealed record CatalogPublicPaceMappingResult(GeneratedCatalogPacePrescription Pace);

internal sealed record CatalogPublicDurationMappingResult(
    int? TargetDurationMinutes,
    int? EstimatedDurationMinutes,
    string DurationStatus);

internal interface ICatalogPublicPreviewMaterializer
{
    CatalogPublicMaterializationResult Materialize(CatalogPublicPreviewMaterializationRequest request);
}

internal sealed class CatalogPublicPreviewMaterializer : ICatalogPublicPreviewMaterializer
{
    public const string MaterializerKey = "CATALOG_PUBLIC_PREVIEW_MATERIALIZER";
    public const int MaterializerVersion = 1;

    private readonly IGeneratedCatalogPlanPayloadValidator _validator;

    public CatalogPublicPreviewMaterializer() : this(new GeneratedCatalogPlanPayloadValidator()) { }

    public CatalogPublicPreviewMaterializer(IGeneratedCatalogPlanPayloadValidator validator)
    {
        _validator = validator;
    }

    public CatalogPublicMaterializationResult Materialize(CatalogPublicPreviewMaterializationRequest request)
    {
        if (!request.FinalPrescribedPlan.ValidationResult.IsValid)
        {
            throw new CatalogPublicPayloadInvalidException("Final prescribed plan must be valid before public preview materialization.");
        }
        if (request.FinalPrescribedPlan.Sessions.Any(s => s.Prescription.Status == CatalogSessionPrescriptionStatus.BaselinePrescribedSharpeningPending))
        {
            throw new CatalogPublicPayloadInvalidException("Final prescribed plan contains a pending prescription state.");
        }
        if (request.PreviewRequest.RecentWeeklyVolumeKm == 0 &&
            request.PreviewRequest.RaceDate is { } raceDate &&
            raceDate.DayNumber - request.AsOfDate.DayNumber <= 56)
        {
            throw new CatalogSessionPrescriptionInfeasibleException(
                "8-week explicit-zero weekly-volume catalog preview remains unsupported: current 4F.7C allocation minimums cannot be treated as safely satisfied by 4F.8.1 materialization.");
        }

        var weeks = request.FinalPrescribedPlan.Weeks
            .OrderBy(w => w.WeekNumber)
            .Select(w => MapWeek(request.PlanStartDate, w))
            .ToArray();
        var payload = new GeneratedCatalogPlanPayload
        {
            SchemaVersion = GeneratedCatalogPlanPayload.CurrentSchemaVersion,
            StartDate = request.PlanStartDate,
            EndDate = request.PlanStartDate.AddDays((weeks.Length * 7) - 1),
            PlannedWeekCount = weeks.Length,
            DaysPerWeek = request.Candidate.DaysPerWeek,
            CanonicalDistanceFamily = request.Candidate.CanonicalDistanceFamily,
            GoalType = request.PreviewRequest.GoalType,
            CandidateKey = request.Candidate.CandidateKey,
            CandidateVersion = request.Candidate.CandidateVersion,
            DependencyVersions = DependencyVersions(request.Candidate),
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanProvenance
            {
                CandidateKey = request.Candidate.CandidateKey,
                CandidateVersion = request.Candidate.CandidateVersion,
                DependencyVersions = DependencyVersions(request.Candidate),
                GenerationSource = GenerationSource.Catalog,
                AsOfDate = request.AsOfDate,
                MaterializerVersion = $"{MaterializerKey} v{MaterializerVersion}",
            }
        };

        ValidateAgainstInternalPlan(payload, request.FinalPrescribedPlan, request.VolumePlan);
        var validation = _validator.Validate(payload);
        if (!validation.IsValid)
        {
            throw new CatalogPublicPayloadInvalidException(string.Join(", ", validation.Errors));
        }

        return new CatalogPublicMaterializationResult(payload, validation);
    }

    private static GeneratedCatalogWeekPayload MapWeek(DateOnly planStartDate, CatalogPrescribedWeek week)
    {
        var weekStart = planStartDate.AddDays((week.WeekNumber - 1) * 7);
        var sessions = week.Sessions
            .OrderBy(s => s.Date)
            .ThenBy(s => RoleOrder(s.StructuralRole))
            .Select((s, index) => MapSession(s, index + 1))
            .ToArray();
        var stageKey = week.Sessions.FirstOrDefault(s => s.StructuralRole == "KEY_SESSION")?.ProgressionStageKey ?? week.PhaseKey;

        return new GeneratedCatalogWeekPayload
        {
            WeekNumber = week.WeekNumber,
            StartDate = weekStart,
            EndDate = weekStart.AddDays(6),
            StageKey = stageKey,
            PlannedVolumeKm = week.PlannedWeeklyVolumeKm,
            Sessions = sessions,
            Provenance = new GeneratedCatalogWeekProvenance
            {
                StageKey = stageKey,
                SourcePhaseKey = week.PhaseKey,
                VolumeRuleKey = week.AllocationTrace.PolicyKey + " v" + week.AllocationTrace.PolicyVersion,
                ProgressionReferenceKey = "TEN_K_WORKOUT_PROGRESSION_V1",
            }
        };
    }

    private static GeneratedCatalogTrainingDayPayload MapSession(CatalogPrescribedSession session, int order) => new()
    {
        Date = session.Date,
        SessionOrderInWeek = order,
        WorkoutType = V1CatalogPublicWorkoutTypeMappingPolicy.Map(session),
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
        TargetDistanceKm = session.PlannedDistanceKm,
        TargetDurationMinutes = null,
        EstimatedDistanceKm = null,
        EstimatedDurationMinutes = MapDuration(session.Prescription.DurationPrescription).EstimatedDurationMinutes,
        PlannedIntensity = session.Prescription.EffortGuidance,
        PacePrescription = MapPace(session.Prescription.PacePrescription).Pace,
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
        }
    };

    internal static CatalogPublicPaceMappingResult MapPace(CatalogPacePrescription pace)
    {
        var mapped = pace.Kind switch
        {
            CatalogPacePrescriptionKind.ExactPace when pace.SecondsPerKilometer is { } seconds => new GeneratedCatalogPacePrescription
            {
                PaceType = GeneratedCatalogPaceType.Target,
                TargetSecondsPerKm = ToWholeSeconds(seconds),
                EffortLabel = pace.EffortLabel,
                DisplayText = pace.Source.ToString(),
            },
            CatalogPacePrescriptionKind.PaceRange when pace.FasterBoundSecondsPerKilometer is { } fast && pace.SlowerBoundSecondsPerKilometer is { } slow => new GeneratedCatalogPacePrescription
            {
                PaceType = GeneratedCatalogPaceType.Range,
                MinSecondsPerKm = ToWholeSeconds(fast),
                MaxSecondsPerKm = ToWholeSeconds(slow),
                EffortLabel = pace.EffortLabel,
                DisplayText = pace.Source.ToString(),
            },
            CatalogPacePrescriptionKind.EffortOnly or CatalogPacePrescriptionKind.Unresolved => new GeneratedCatalogPacePrescription
            {
                PaceType = GeneratedCatalogPaceType.EffortOnly,
                EffortLabel = pace.EffortLabel,
                DisplayText = "NUMERIC_PACE_UNRESOLVED",
            },
            _ => throw new CatalogPublicPaceRepresentationUnsupportedException($"Unsupported pace representation '{pace.Kind}'.")
        };

        return new CatalogPublicPaceMappingResult(mapped);
    }

    internal static CatalogPublicDurationMappingResult MapDuration(CatalogDurationPrescription duration)
    {
        return duration.Kind switch
        {
            CatalogDurationKind.Prescribed or CatalogDurationKind.DerivedFromSegments => new CatalogPublicDurationMappingResult(
                ToWholeMinutes(duration.Duration), null, duration.Kind.ToString()),
            CatalogDurationKind.EstimatedFromPace => new CatalogPublicDurationMappingResult(
                null, ToWholeMinutes(duration.Duration), duration.Kind.ToString()),
            CatalogDurationKind.Unresolved => new CatalogPublicDurationMappingResult(null, null, duration.Kind.ToString()),
            _ => throw new CatalogPublicDurationMappingException($"Unsupported duration kind '{duration.Kind}'.")
        };
    }

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

    private static void ValidateAgainstInternalPlan(
        GeneratedCatalogPlanPayload payload,
        CatalogPrescribedPlan internalPlan,
        CatalogVolumeAndLongRunPlan volumePlan)
    {
        if (payload.Weeks.Count != internalPlan.Weeks.Count)
        {
            throw new CatalogPublicPayloadInvalidException("Public payload week count does not match final internal plan.");
        }

        foreach (var publicWeek in payload.Weeks)
        {
            var internalWeek = internalPlan.Weeks.Single(w => w.WeekNumber == publicWeek.WeekNumber);
            var volumeWeek = volumePlan.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == publicWeek.WeekNumber);
            var longRun = volumePlan.LongRunProgression.Weeks.Single(w => w.WeekNumber == publicWeek.WeekNumber);
            if (Math.Abs(publicWeek.PlannedVolumeKm - internalWeek.PlannedWeeklyVolumeKm) > 0.001 ||
                Math.Abs(publicWeek.PlannedVolumeKm - volumeWeek.PlannedWeeklyVolumeKm) > 0.001)
            {
                throw new CatalogPublicDistanceMismatchException($"Week {publicWeek.WeekNumber} public volume does not match internal volume.");
            }

            if (publicWeek.Sessions.Count != internalWeek.Sessions.Count)
            {
                throw new CatalogPublicPayloadInvalidException($"Week {publicWeek.WeekNumber} session count does not match internal plan.");
            }

            var publicTotal = publicWeek.Sessions.Sum(s => s.TargetDistanceKm ?? 0);
            if (Math.Abs(publicTotal - publicWeek.PlannedVolumeKm) > 0.001)
            {
                throw new CatalogPublicDistanceMismatchException($"Week {publicWeek.WeekNumber} public session distances do not reconcile.");
            }

            var publicLongRun = publicWeek.Sessions.Single(s => s.Provenance.SourceLayoutSlotRole == "LONG_RUN");
            if (Math.Abs((publicLongRun.TargetDistanceKm ?? 0) - longRun.PlannedLongRunDistanceKm) > 0.001)
            {
                throw new CatalogPublicDistanceMismatchException($"Week {publicWeek.WeekNumber} public long run does not match internal long run.");
            }

            foreach (var internalSession in internalWeek.Sessions)
            {
                var publicSession = publicWeek.Sessions.Single(s =>
                    s.Date == internalSession.Date &&
                    s.Provenance.SourceLayoutSlotRole == internalSession.StructuralRole &&
                    s.Provenance.SourceWorkoutKey == internalSession.WorkoutDefinitionKey);
                if (Math.Abs((publicSession.TargetDistanceKm ?? 0) - internalSession.PlannedDistanceKm) > 0.001)
                {
                    throw new CatalogPublicDistanceMismatchException($"Session {internalSession.WeekNumber}/{internalSession.Date} distance mismatch.");
                }
                var segmentTotal = publicSession.Segments.Sum(s => s.TargetDistanceKm ?? 0);
                if (Math.Abs(segmentTotal - internalSession.PlannedDistanceKm) > 0.001)
                {
                    throw new CatalogPublicDistanceMismatchException($"Session {internalSession.WeekNumber}/{internalSession.Date} segment distance mismatch.");
                }
            }
        }
    }

    private static int RoleOrder(string role) => role switch
    {
        "KEY_SESSION" => 0,
        "EASY_SUPPORT" => 1,
        "LONG_RUN" => 2,
        _ => 9
    };

    private static int ToWholeSeconds(double value) => (int)Math.Round(value, 0, MidpointRounding.AwayFromZero);

    private static int? ToWholeMinutes(TimeSpan? value) =>
        value is null ? null : Math.Max(1, (int)Math.Round(value.Value.TotalMinutes, 0, MidpointRounding.AwayFromZero));
}

internal static class V1CatalogPublicWorkoutTypeMappingPolicy
{
    public const string PolicyKey = "V1_CATALOG_PUBLIC_WORKOUT_TYPE_MAPPING_POLICY";
    public const int PolicyVersion = 1;

    public static GeneratedCatalogWorkoutType Map(CatalogPrescribedSession session) =>
        (session.WorkoutDefinitionKey, session.StructuralRole, session.ProgressionStageKey) switch
        {
            ("EASY_STANDARD", "EASY_SUPPORT", _) => GeneratedCatalogWorkoutType.Easy,
            ("EASY_STANDARD", "KEY_SESSION", "TAPER_SHARPEN") => GeneratedCatalogWorkoutType.Easy,
            ("EASY_STANDARD", "KEY_SESSION", _) => GeneratedCatalogWorkoutType.Easy,
            ("LONG_RUN_STANDARD", "LONG_RUN", _) => GeneratedCatalogWorkoutType.LongRun,
            ("FARTLEK", "KEY_SESSION", _) => GeneratedCatalogWorkoutType.Interval,
            ("THRESHOLD_TEMPO", "KEY_SESSION", _) => GeneratedCatalogWorkoutType.Tempo,
            ("GOAL_PACE_TEN_K", "KEY_SESSION", _) => GeneratedCatalogWorkoutType.Interval,
            // Phase 10K-FREQ.6D.4D.5E: real Intermediate×5D Foundation-Primary workout.
            // Key-only (no version branch) per 5E's confirmed key-level mapping-ownership
            // authority — every reachable version shares the same athlete-facing semantics.
            ("AEROBIC_STRENGTH_CONTROLLED_INTRO", "KEY_SESSION", _) => GeneratedCatalogWorkoutType.Interval,
            _ => throw new CatalogPublicWorkoutTypeUnsupportedException(
                $"No public workout type mapping for workout '{session.WorkoutDefinitionKey}', role '{session.StructuralRole}', stage '{session.ProgressionStageKey}'.")
        };
}

internal static class V1CatalogPublicSegmentMappingPolicy
{
    public const string PolicyKey = "V1_CATALOG_PUBLIC_SEGMENT_MAPPING_POLICY";
    public const int PolicyVersion = 1;

    public static GeneratedCatalogWorkoutSegmentPayload Map(CatalogPrescriptionSegment segment) => new()
    {
        SegmentOrder = segment.SequenceOrder,
        SegmentType = SegmentTypeFor(segment.ComponentType),
        RepetitionCount = null,
        PrescriptionBasis = segment.Duration is null
            ? GeneratedCatalogPrescriptionBasis.Distance
            : GeneratedCatalogPrescriptionBasis.Duration,
        TargetDistanceKm = segment.Duration is null ? segment.DistanceKm : null,
        TargetDurationSeconds = segment.Duration is null ? null : (int)Math.Round(segment.Duration.Value.TotalSeconds, 0, MidpointRounding.AwayFromZero),
        PacePrescription = CatalogPublicPreviewMaterializer.MapPace(segment.PacePrescription).Pace,
        Intensity = segment.IntensityDescriptor,
        RecoveryType = segment.ComponentType == "RECOVERY" || segment.ComponentType == "EASY_RECOVERY" ? "EASY" : null,
        DisplayText = segment.ComponentType,
    };

    private static GeneratedCatalogSegmentType SegmentTypeFor(string componentType) => componentType switch
    {
        "WARM_UP" => GeneratedCatalogSegmentType.WarmUp,
        "MAIN_SET" => GeneratedCatalogSegmentType.WorkInterval,
        "RECOVERY" => GeneratedCatalogSegmentType.Recovery,
        "COOL_DOWN" => GeneratedCatalogSegmentType.CoolDown,
        "SESSION_TOTAL" => GeneratedCatalogSegmentType.Steady,
        "EASY_BASELINE" => GeneratedCatalogSegmentType.Steady,
        "CONTROLLED_SHARPENING" => GeneratedCatalogSegmentType.WorkInterval,
        "EASY_RECOVERY" => GeneratedCatalogSegmentType.Recovery,
        _ => throw new CatalogPublicSegmentUnsupportedException($"No public segment mapping for '{componentType}'.")
    };
}
