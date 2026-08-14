using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4G.6B.2 — the pure, production-owned mapper from
/// a completed, successful <see cref="TenKPreparationRunwayDarkOrchestrationResult"/>
/// to the public <see cref="PreviewWeekDto"/>/<see cref="PreviewDayDto"/>
/// shape. Extracted from <see cref="CatalogPreviewGenerator"/> (which is its
/// only production caller) so that <c>RunningApp.IntegrationTests</c> can
/// exercise it directly against a real orchestrator result and prove
/// field-by-field equality with the public response, without duplicating the
/// mapping logic in a test-owned copy.
///
/// Deterministic and side-effect-free: reads only from the supplied,
/// already-completed <paramref name="result"/> — never recalculates a
/// distance, date, or pace, never touches the database, and never invokes
/// any generation/allocation/materialization component itself. Never leaks
/// internal trace/progression/policy identifiers into the mapped output.
/// </summary>
internal static class PreparationRunwayPublicPreviewMapper
{
    public static List<PreviewWeekDto> MapCombinedWeeks(TenKPreparationRunwayDarkOrchestrationResult result)
    {
        var composedWeeks = result.CalendarComposition!.OrderedCombinedWeeks!;
        var pacedRunwayByGlobalWeek = result.PacedRunway!.PacedRunwayWeeks!
            .ToDictionary(w => w.OriginalWeek.GlobalWeekNumber);
        var coreWeeksByLocalNumber = result.CoreResult!.PrescriptionResult.FinalPrescribedPlan.Weeks
            .ToDictionary(w => w.WeekNumber);

        var mapped = new List<PreviewWeekDto>();

        foreach (var composed in composedWeeks.OrderBy(w => w.GlobalWeekNumber))
        {
            if (composed.SegmentType == PreparationRunwaySegmentType.PreparationRunway)
            {
                var pacedWeek = pacedRunwayByGlobalWeek[composed.GlobalWeekNumber];
                var blockType = pacedWeek.OriginalWeek.PrescribedWeek.StructuralWeek.BlockType;

                var days = pacedWeek.ChronologicalSlots.Select((slot, index) => new PreviewDayDto
                {
                    SlotIndex = index + 1,
                    DayType = slot.OriginalSlot.PrescribedSlot.StructuralSlot.SlotRole switch
                    {
                        PreparationRunwaySlotRole.LongRun => TrainingDayType.LongRun,
                        PreparationRunwaySlotRole.KeySession => TrainingDayType.Tempo,
                        _ => TrainingDayType.Easy,
                    },
                    DistanceKm = slot.OriginalSlot.PrescribedSlot.PlannedDistanceKm,
                    DurationMin = 0,
                    Intensity = slot.PacePrescription.EffortLabel,
                    Date = slot.OriginalSlot.SessionDate.ToDateTime(TimeOnly.MinValue),
                }).ToList();

                mapped.Add(new PreviewWeekDto
                {
                    WeekNumber = composed.GlobalWeekNumber,
                    WeekType = TrainingWeekType.PreparationRunway,
                    RunwayBlock = RunwayBlockPublicName(blockType),
                    Days = days,
                });
            }
            else
            {
                var coreWeek = coreWeeksByLocalNumber[composed.SegmentLocalWeekNumber];
                var days = coreWeek.Sessions.OrderBy(s => s.Date).Select((s, index) => new PreviewDayDto
                {
                    SlotIndex = index + 1,
                    DayType = s.StructuralRole switch
                    {
                        "LONG_RUN" => TrainingDayType.LongRun,
                        "KEY_SESSION" => TrainingDayType.Tempo,
                        _ => TrainingDayType.Easy,
                    },
                    DistanceKm = s.PlannedDistanceKm,
                    DurationMin = (int)(s.PlannedDuration ?? s.EstimatedDuration ?? TimeSpan.Zero).TotalMinutes,
                    Intensity = s.Prescription.PacePrescription.EffortLabel,
                    Date = s.Date.ToDateTime(TimeOnly.MinValue),
                }).ToList();

                mapped.Add(new PreviewWeekDto
                {
                    WeekNumber = composed.GlobalWeekNumber,
                    WeekType = coreWeek.PhaseKey switch
                    {
                        "FOUNDATION" => TrainingWeekType.Base,
                        "BUILD" => TrainingWeekType.Build,
                        "RACE_SPECIFIC" => TrainingWeekType.Peak,
                        "TAPER" => TrainingWeekType.Taper,
                        _ => TrainingWeekType.Build,
                    },
                    RunwayBlock = null,
                    Days = days,
                });
            }
        }

        return mapped;
    }

    public static string RunwayBlockPublicName(PreparationRunwayBlockType blockType) => blockType switch
    {
        PreparationRunwayBlockType.Consistency => "CONSISTENCY",
        PreparationRunwayBlockType.GeneralEndurance => "GENERAL_ENDURANCE",
        PreparationRunwayBlockType.AerobicStrength => "AEROBIC_STRENGTH",
        PreparationRunwayBlockType.PreSpecificTransition => "PRE_SPECIFIC_TRANSITION",
        _ => blockType.ToString(),
    };
}
