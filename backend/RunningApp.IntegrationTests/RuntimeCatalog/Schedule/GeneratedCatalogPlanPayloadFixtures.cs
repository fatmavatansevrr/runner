using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Domain.Enums;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule;

/// <summary>
/// Backend Integration Phase 4F.1 — hand-built <see cref="GeneratedCatalogPlanPayload"/>
/// fixtures for contract/validator testing ONLY. These are NEVER produced by
/// any live code path (<see cref="RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogPreviewGenerator"/>
/// still never supplies a non-null payload) — using one of these fixtures in
/// a test proves the contract/validator behaves correctly, never that live
/// catalog schedule materialization exists. Scoped to the TEN_K/INTERMEDIATE/4D
/// pilot shape only (Phase 4F.1 Decision 12), but every field is populated
/// generically (no hardcoded TEN_K assumption inside the contract types
/// themselves — only this fixture's chosen values are pilot-specific).
/// </summary>
internal static class GeneratedCatalogPlanPayloadFixtures
{
    public const int DaysPerWeek = 4;

    /// <summary>A complete, fully valid 2-week TEN_K/INTERMEDIATE/4D fixture: 4 sessions/week, mixing distance- and duration-basis sessions, one session carrying optional segments.</summary>
    public static GeneratedCatalogPlanPayload ValidTwoWeekPlan(DateOnly? startDate = null)
    {
        var start = startDate ?? new DateOnly(2026, 8, 3); // a Monday, incidental — StartDate is never normalized
        return BuildPlan(start, weekCount: 2);
    }

    public static GeneratedCatalogPlanPayload ValidPlan(DateOnly startDate, int weekCount) =>
        BuildPlan(startDate, weekCount);

    private static GeneratedCatalogPlanPayload BuildPlan(DateOnly startDate, int weekCount)
    {
        var weeks = new List<GeneratedCatalogWeekPayload>();
        for (var weekNumber = 1; weekNumber <= weekCount; weekNumber++)
        {
            weeks.Add(BuildWeek(startDate, weekNumber));
        }

        var endDate = weeks[^1].EndDate;

        return new GeneratedCatalogPlanPayload
        {
            SchemaVersion = GeneratedCatalogPlanPayload.CurrentSchemaVersion,
            StartDate = startDate,
            EndDate = endDate,
            PlannedWeekCount = weekCount,
            DaysPerWeek = DaysPerWeek,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            DependencyVersions = DependencyVersions(),
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanProvenance
            {
                CandidateKey = "TEN_K__4D__INTERMEDIATE",
                CandidateVersion = 10,
                DependencyVersions = DependencyVersions(),
                GenerationSource = "CATALOG",
                AsOfDate = startDate,
                MaterializerVersion = null,
            },
        };
    }

    private static Dictionary<string, PlanCatalogReference> DependencyVersions() => new()
    {
        ["masterTemplate"] = new PlanCatalogReference("TEN_K_MASTER", 6),
        ["layout"] = new PlanCatalogReference("RUN_LAYOUT_4D", 2),
        ["levelModifier"] = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
        ["rulePack"] = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
    };

    private static GeneratedCatalogWeekPayload BuildWeek(DateOnly planStartDate, int weekNumber)
    {
        var weekStart = planStartDate.AddDays((weekNumber - 1) * 7);
        var weekEnd = weekStart.AddDays(6);

        var sessions = new List<GeneratedCatalogTrainingDayPayload>
        {
            BuildDistanceSession(weekStart, dayOffset: 0, order: 1, distanceKm: 6.0, workoutType: GeneratedCatalogWorkoutType.Easy),
            BuildDistanceSessionWithSegments(weekStart, dayOffset: 2, order: 2),
            BuildDurationSession(weekStart, dayOffset: 4, order: 3),
            BuildDistanceSession(weekStart, dayOffset: 6, order: 4, distanceKm: 10.0, workoutType: GeneratedCatalogWorkoutType.LongRun),
        };

        return new GeneratedCatalogWeekPayload
        {
            WeekNumber = weekNumber,
            StartDate = weekStart,
            EndDate = weekEnd,
            StageKey = "BUILD",
            PlannedVolumeKm = sessions.Sum(s => s.TargetDistanceKm ?? s.EstimatedDistanceKm ?? 0),
            Sessions = sessions,
            Provenance = new GeneratedCatalogWeekProvenance
            {
                StageKey = "BUILD",
                SourcePhaseKey = "BUILD",
                VolumeRuleKey = "BUILD_STANDARD",
                ProgressionReferenceKey = "TEN_K_INTERMEDIATE_PROGRESSION_V1",
            },
        };
    }

    private static GeneratedCatalogTrainingDayPayload BuildDistanceSession(
        DateOnly weekStart, int dayOffset, int order, double distanceKm, GeneratedCatalogWorkoutType workoutType) => new()
    {
        Date = weekStart.AddDays(dayOffset),
        SessionOrderInWeek = order,
        WorkoutType = workoutType,
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
        TargetDistanceKm = distanceKm,
        TargetDurationMinutes = null,
        EstimatedDistanceKm = null,
        EstimatedDurationMinutes = (int)(distanceKm * 6),
        PlannedIntensity = "z2",
        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "conversational" },
        Segments = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
        Provenance = new GeneratedCatalogDayProvenance
        {
            SourceStageKey = "BUILD",
            SourceWorkoutKey = "EASY_STANDARD",
            SourceWorkoutVersion = 4,
            SourceProgressionStepKey = "BUILD_WEEK",
            SourceLayoutSlotRole = "EASY_SUPPORT",
        },
    };

    private static GeneratedCatalogTrainingDayPayload BuildDistanceSessionWithSegments(DateOnly weekStart, int dayOffset, int order) => new()
    {
        Date = weekStart.AddDays(dayOffset),
        SessionOrderInWeek = order,
        WorkoutType = GeneratedCatalogWorkoutType.Interval,
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
        TargetDistanceKm = 8.0,
        TargetDurationMinutes = null,
        EstimatedDistanceKm = null,
        EstimatedDurationMinutes = 48,
        PlannedIntensity = "z4",
        PacePrescription = new GeneratedCatalogPacePrescription
        {
            PaceType = GeneratedCatalogPaceType.Target,
            TargetSecondsPerKm = 285,
        },
        Segments = new List<GeneratedCatalogWorkoutSegmentPayload>
        {
            new()
            {
                SegmentOrder = 1,
                SegmentType = GeneratedCatalogSegmentType.WarmUp,
                RepetitionCount = null,
                PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
                TargetDistanceKm = 2.0,
                TargetDurationSeconds = null,
                PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "easy" },
                Intensity = "z2",
            },
            new()
            {
                SegmentOrder = 2,
                SegmentType = GeneratedCatalogSegmentType.WorkInterval,
                RepetitionCount = 6,
                PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Duration,
                TargetDistanceKm = null,
                TargetDurationSeconds = 120,
                PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.Target, TargetSecondsPerKm = 255 },
                Intensity = "z4",
            },
            new()
            {
                SegmentOrder = 3,
                SegmentType = GeneratedCatalogSegmentType.CoolDown,
                RepetitionCount = null,
                PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
                TargetDistanceKm = 1.5,
                TargetDurationSeconds = null,
                PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "easy" },
                Intensity = "z2",
            },
        },
        Provenance = new GeneratedCatalogDayProvenance
        {
            SourceStageKey = "BUILD",
            SourceWorkoutKey = "GOAL_PACE_TEN_K",
            SourceWorkoutVersion = 2,
            SourceProgressionStepKey = "BUILD_WEEK",
            SourceLayoutSlotRole = "KEY_SESSION",
        },
    };

    private static GeneratedCatalogTrainingDayPayload BuildDurationSession(DateOnly weekStart, int dayOffset, int order) => new()
    {
        Date = weekStart.AddDays(dayOffset),
        SessionOrderInWeek = order,
        WorkoutType = GeneratedCatalogWorkoutType.Tempo,
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Duration,
        TargetDistanceKm = null,
        TargetDurationMinutes = 40,
        EstimatedDistanceKm = 7.0,
        EstimatedDurationMinutes = null,
        PlannedIntensity = "z3",
        PacePrescription = new GeneratedCatalogPacePrescription
        {
            PaceType = GeneratedCatalogPaceType.Range,
            MinSecondsPerKm = 300,
            MaxSecondsPerKm = 330,
            DisplayText = "5:00-5:30/km",
        },
        Segments = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
        Provenance = new GeneratedCatalogDayProvenance
        {
            SourceStageKey = "BUILD",
            SourceWorkoutKey = "THRESHOLD_TEMPO",
            SourceWorkoutVersion = 4,
            SourceProgressionStepKey = "BUILD_WEEK",
            SourceLayoutSlotRole = "KEY_SESSION",
        },
    };
}
