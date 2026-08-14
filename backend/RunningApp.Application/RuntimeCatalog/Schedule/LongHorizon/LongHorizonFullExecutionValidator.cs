using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6A — the final fail-closed validator for a fully numeric,
/// workout-bound, paced, and dated <see cref="LongHorizonExecutedSchedule"/>.
/// Checks per-segment completeness (every field present, no placeholder
/// zero) and the two cross-segment continuity boundaries (GE→Runway,
/// Runway→Core), reusing the existing Runway calendar/pace stages' own
/// continuity analyses rather than re-deriving them.
/// </summary>
internal static class LongHorizonFullExecutionValidator
{
    private const double ToleranceKm = 0.01;

    public static LongHorizonExecutionValidationResult Validate(
        LongHorizonExecutedSchedule schedule,
        LongHorizonGeExitState geExit,
        PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> numeric,
        PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> calendar,
        PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType> pace,
        Materialization.DynamicCoreCalendarMaterializationResult core)
    {
        var findings = new List<string>();

        if (!schedule.GeNumericExecutionComplete) findings.Add("GeNumericExecutionComplete must be true.");
        if (!schedule.RunwayNumericExecutionComplete) findings.Add("RunwayNumericExecutionComplete must be true.");
        if (!schedule.CoreNumericExecutionComplete) findings.Add("CoreNumericExecutionComplete must be true.");

        foreach (var week in schedule.Weeks)
        {
            if (week.TotalVolumeKm is null || week.LongRunDistanceKm is null)
                findings.Add($"Global week {week.Structural.GlobalWeekNumber} ({week.Structural.Segment}) is missing weekly volume/long-run.");
            if (week.OrderedSlots.Count != 4)
                findings.Add($"Global week {week.Structural.GlobalWeekNumber} does not have exactly 4 slots.");

            foreach (var slot in week.OrderedSlots)
            {
                if (string.IsNullOrWhiteSpace(slot.WorkoutKey) || slot.WorkoutVersion <= 0)
                    findings.Add($"Global week {week.Structural.GlobalWeekNumber}: slot missing a resolved workout key/version.");
                if (slot.AssignedDate is null || slot.AssignedWeekday is null)
                    findings.Add($"Global week {week.Structural.GlobalWeekNumber}: slot missing an assigned date/weekday.");
                if (slot.PlannedDistanceKm is null or <= 0)
                    findings.Add($"Global week {week.Structural.GlobalWeekNumber}: slot has a null or non-positive planned distance.");
            }

            var dates = week.OrderedSlots.Select(s => s.AssignedDate).Where(d => d is not null).Select(d => d!.Value).ToList();
            if (dates.Distinct().Count() != dates.Count)
                findings.Add($"Global week {week.Structural.GlobalWeekNumber}: duplicate assigned date within the week.");
        }

        // GE→Runway continuity.
        var firstRunwayWeek = schedule.Weeks.FirstOrDefault(w => w.Structural.Segment == LongHorizonSegmentType.PreparationRunway);
        if (firstRunwayWeek is null)
        {
            findings.Add("No Preparation Runway week found.");
        }
        else if (firstRunwayWeek.TotalVolumeKm is { } runwayWeek1Volume)
        {
            var maxAllowedIncrease = Math.Max(
                geExit.FinalWeeklyVolumeKm * 0.08, 2.5) + ToleranceKm;
            if (runwayWeek1Volume > geExit.FinalWeeklyVolumeKm + maxAllowedIncrease)
                findings.Add($"Runway Week 1 volume ({runwayWeek1Volume}km) exceeds the GE exit ({geExit.FinalWeeklyVolumeKm}km) by more than the approved progression caps.");
        }

        if (!numeric.IsSuccess) findings.Add("Runway numeric materialization did not succeed.");
        if (!calendar.IsSuccess) findings.Add("Runway calendar composition did not succeed.");
        if (calendar.ContinuityAnalysis is { IsValid: false } continuity)
            findings.Add($"Runway→Core calendar continuity invalid: {continuity.Provenance}");
        if (!pace.IsSuccess) findings.Add("Runway pace materialization did not succeed.");
        if (pace.ContinuityAnalysis is { IsValid: false } paceContinuity)
            findings.Add($"Runway→Core pace continuity invalid: {paceContinuity.Provenance}");

        // Runway→Core structural continuity: no duplicate transition, correct counts.
        var runwayWeeks = schedule.Weeks.Count(w => w.Structural.Segment == LongHorizonSegmentType.PreparationRunway);
        var coreWeeks = schedule.Weeks.Count(w => w.Structural.Segment == LongHorizonSegmentType.Core);
        if (runwayWeeks != 8) findings.Add($"Expected exactly 8 Runway weeks; found {runwayWeeks}.");
        if (coreWeeks != 12) findings.Add($"Expected exactly 12 Core weeks; found {coreWeeks}.");

        var firstCoreWeek = schedule.Weeks.FirstOrDefault(w => w.Structural.Segment == LongHorizonSegmentType.Core);
        if (firstCoreWeek is not null && firstCoreWeek.Structural.CorePhase != "FOUNDATION")
            findings.Add("Core Week 1 phase is not FOUNDATION.");

        if (!core.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid)
            findings.Add("Core FinalPrescribedPlan.ValidationResult reports invalid: " + string.Join("; ", core.PrescriptionResult.FinalPrescribedPlan.ValidationResult.Errors));

        return findings.Count == 0 ? LongHorizonExecutionValidationResult.Valid() : LongHorizonExecutionValidationResult.Invalid(findings);
    }
}
