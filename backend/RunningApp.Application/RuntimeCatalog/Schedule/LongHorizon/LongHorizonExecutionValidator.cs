namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

internal sealed record LongHorizonExecutionValidationResult(bool IsValid, IReadOnlyList<string> Findings)
{
    public static LongHorizonExecutionValidationResult Valid() => new(true, Array.Empty<string>());
    public static LongHorizonExecutionValidationResult Invalid(IReadOnlyList<string> findings) => new(false, findings);
}

/// <summary>
/// Phase 4I.6 — the final fail-closed validator for a
/// <see cref="LongHorizonExecutedSchedule"/>. Never auto-corrects. Checks
/// every slot across every segment has a non-null workout key/version and a
/// non-null assigned date/weekday; checks GE numeric completeness (weekly
/// volume present, slot distances sum to the weekly total, exactly one long
/// run, long-run distance matches); and explicitly asserts Runway/Core
/// numeric fields remain null (this phase's own disclosed scope boundary,
/// not a defect -- see the phase document's non-implementation statement).
/// </summary>
internal static class LongHorizonExecutionValidator
{
    private const double ToleranceKm = 0.01;

    public static LongHorizonExecutionValidationResult Validate(LongHorizonExecutedSchedule schedule)
    {
        var findings = new List<string>();

        var structuralValidation = LongHorizonStructuralValidator.Validate(schedule.Structural);
        // Core's structural validator run was performed before workout binding (Phase 4I.5
        // behavior) and therefore still finds null Core workout references -- that specific
        // finding is expected and superseded by this validator's own binding checks below.
        var structuralFindingsExcludingCoreNullWorkout = structuralValidation.Findings
            .Where(f => !f.Contains("null workout reference", StringComparison.Ordinal))
            .ToList();
        findings.AddRange(structuralFindingsExcludingCoreNullWorkout);

        if (schedule.Weeks.Count != schedule.Structural.Weeks.Count)
            findings.Add("Executed week count does not match structural week count.");

        foreach (var week in schedule.Weeks)
        {
            foreach (var slot in week.OrderedSlots)
            {
                if (string.IsNullOrWhiteSpace(slot.WorkoutKey) || slot.WorkoutVersion <= 0)
                    findings.Add($"Global week {week.Structural.GlobalWeekNumber}: slot missing a resolved workout key/version.");
                if (slot.AssignedDate is null || slot.AssignedWeekday is null)
                    findings.Add($"Global week {week.Structural.GlobalWeekNumber}: slot missing an assigned date/weekday.");
            }

            var dates = week.OrderedSlots.Select(s => s.AssignedDate).Where(d => d is not null).Select(d => d!.Value).ToList();
            if (dates.Distinct().Count() != dates.Count)
                findings.Add($"Global week {week.Structural.GlobalWeekNumber}: duplicate assigned date within the week.");

            switch (week.Structural.Segment)
            {
                case LongHorizonSegmentType.LongHorizonGeneralEndurance:
                    ValidateGeWeek(week, findings);
                    break;
                case LongHorizonSegmentType.PreparationRunway:
                case LongHorizonSegmentType.Core:
                    if (week.TotalVolumeKm is not null || week.LongRunDistanceKm is not null)
                        findings.Add($"Global week {week.Structural.GlobalWeekNumber} ({week.Structural.Segment}): numeric fields must remain null in this phase (deferred, not executed).");
                    if (week.OrderedSlots.Any(s => s.PlannedDistanceKm is not null))
                        findings.Add($"Global week {week.Structural.GlobalWeekNumber} ({week.Structural.Segment}): slot distances must remain null in this phase (deferred, not executed).");
                    break;
            }
        }

        if (!schedule.GeNumericExecutionComplete)
            findings.Add("GeNumericExecutionComplete must be true -- this phase requires full GE numeric execution.");
        if (schedule.RunwayNumericExecutionComplete)
            findings.Add("RunwayNumericExecutionComplete must be false -- Runway numeric execution is not performed by this phase.");
        if (schedule.CoreNumericExecutionComplete)
            findings.Add("CoreNumericExecutionComplete must be false -- Core numeric execution is not performed by this phase.");

        return findings.Count == 0 ? LongHorizonExecutionValidationResult.Valid() : LongHorizonExecutionValidationResult.Invalid(findings);
    }

    private static void ValidateGeWeek(LongHorizonExecutedWeek week, List<string> findings)
    {
        if (week.TotalVolumeKm is not { } totalVolume || week.LongRunDistanceKm is not { } longRun)
        {
            findings.Add($"GE global week {week.Structural.GlobalWeekNumber}: missing weekly volume/long-run numeric values.");
            return;
        }

        var longRunSlots = week.OrderedSlots.Count(s => s.IsLongRun);
        if (longRunSlots != 1)
            findings.Add($"GE global week {week.Structural.GlobalWeekNumber}: expected exactly 1 long-run slot, found {longRunSlots}.");

        var longRunSlotDistance = week.OrderedSlots.SingleOrDefault(s => s.IsLongRun)?.PlannedDistanceKm;
        if (longRunSlotDistance is null || Math.Abs(longRunSlotDistance.Value - longRun) > ToleranceKm)
            findings.Add($"GE global week {week.Structural.GlobalWeekNumber}: long-run slot distance does not match the week's LongRunDistanceKm.");

        var sum = week.OrderedSlots.Sum(s => s.PlannedDistanceKm ?? 0);
        if (Math.Abs(sum - totalVolume) > ToleranceKm)
            findings.Add($"GE global week {week.Structural.GlobalWeekNumber}: slot distances ({sum}km) do not sum to the weekly total ({totalVolume}km).");

        if (week.OrderedSlots.Any(s => s.PlannedDistanceKm is null or <= 0))
            findings.Add($"GE global week {week.Structural.GlobalWeekNumber}: a slot has a null or non-positive planned distance.");
    }
}
