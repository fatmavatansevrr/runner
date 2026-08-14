namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>Phase 4K.8D final alignment validator; it validates, never assigns.</summary>
internal static class LongHorizonActivatedCalendarAlignmentValidator
{
    public static void Validate(LongHorizonRollingJitActivationResult activation,
        LongHorizonActivatedCalendarProjectionResult projection,
        IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDay)
    {
        if (activation.ActivationWindow is null)
            throw new LongHorizonActivatedCalendarAlignmentException("Aligned activation requires an activation window.");

        var projectedDates = projection.SelectedSessions.Select(s => s.SessionDate).ToArray();
        if (projectedDates.Distinct().Count() != projectedDates.Length)
            throw new LongHorizonDuplicateDatedSessionException("Selected Runway/Core projection contains duplicate dates.");

        foreach (var week in activation.NewlyActivatedWeeks.Where(w => w.SegmentType is LongHorizonStructuralSegmentType.PreparationRunway or LongHorizonStructuralSegmentType.Core))
        {
            var sessions = week.SessionPrescriptions ?? throw new LongHorizonActivatedCalendarAlignmentException($"Week {week.GlobalWeekNumber} has no sessions.");
            var projected = projection.SelectedSessions.Where(s => s.GlobalWeekNumber == week.GlobalWeekNumber).OrderBy(s => s.SessionOrdinal).ToArray();
            if (sessions.Count != 4 || projected.Length != 4)
                throw new LongHorizonActivatedCalendarAlignmentException($"Week {week.GlobalWeekNumber} must map exactly four sessions.");
            if (week.CalendarDates is not { } boundary)
                throw new LongHorizonActivatedCalendarAlignmentException($"Week {week.GlobalWeekNumber} has no structural boundary.");

            foreach (var p in projected)
            {
                var numeric = sessions.SingleOrDefault(s => s.SessionOrdinal == p.SessionOrdinal)
                    ?? throw new LongHorizonMissingDatedSessionException($"Week {week.GlobalWeekNumber} session {p.SessionOrdinal} is missing.");
                if (numeric.AssignedDate != p.SessionDate || p.Weekday != p.SessionDate.DayOfWeek)
                    throw new LongHorizonActivatedCalendarAlignmentException($"Week {week.GlobalWeekNumber} session {p.SessionOrdinal} date/weekday differs from composition.");
                if (p.SessionDate < boundary.Start || p.SessionDate > boundary.End)
                    throw new LongHorizonActivatedCalendarAlignmentException($"Week {week.GlobalWeekNumber} session {p.SessionOrdinal} is outside its structural week.");
                if (!preferredDays.Contains(p.Weekday))
                    throw new LongHorizonActivatedCalendarAlignmentException($"Week {week.GlobalWeekNumber} session {p.SessionOrdinal} is not on a preferred day.");
                if (IsLongRun(p.SessionRole) && p.Weekday != longRunDay)
                    throw new LongHorizonActivatedCalendarAlignmentException($"Week {week.GlobalWeekNumber} long run is not on the preferred long-run day.");
                if (!string.Equals(numeric.SessionRole, p.SessionRole, StringComparison.Ordinal)
                    || !string.Equals(numeric.WorkoutKey, p.WorkoutKey, StringComparison.Ordinal)
                    || numeric.WorkoutVersion != p.WorkoutVersion
                    || p.Segment != week.SegmentType || p.ContextVersion != activation.ContextVersion)
                    throw new LongHorizonCalendarIdentityMismatchException($"Week {week.GlobalWeekNumber} session {p.SessionOrdinal} identity/context mismatch.");
            }
        }

        var orderedWeeks = activation.NewlyActivatedWeeks.OrderBy(w => w.GlobalWeekNumber).ToArray();
        for (var index = 1; index < orderedWeeks.Length; index++)
        {
            var previous = orderedWeeks[index - 1];
            var current = orderedWeeks[index];
            if (previous.GlobalWeekNumber + 1 != current.GlobalWeekNumber)
                throw new LongHorizonActivatedCalendarAlignmentException("Activated weeks must remain globally contiguous.");

            var previousLast = LastExecutableDate(previous, projection);
            var currentFirst = FirstExecutableDate(current, projection);
            if (previousLast >= currentFirst)
                throw new LongHorizonActivatedCalendarAlignmentException($"Calendar overlap at week boundary {previous.GlobalWeekNumber}->{current.GlobalWeekNumber}.");
        }
    }

    private static DateOnly LastExecutableDate(ActivatedNumericWeek week, LongHorizonActivatedCalendarProjectionResult projection) =>
        week.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance
            ? week.SessionPrescriptions?.Where(s => s.AssignedDate is not null).Max(s => s.AssignedDate) ?? week.CalendarDates!.Value.End
            : projection.SelectedSessions.Where(s => s.GlobalWeekNumber == week.GlobalWeekNumber).Max(s => s.SessionDate);

    private static DateOnly FirstExecutableDate(ActivatedNumericWeek week, LongHorizonActivatedCalendarProjectionResult projection) =>
        week.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance
            ? week.SessionPrescriptions?.Where(s => s.AssignedDate is not null).Min(s => s.AssignedDate) ?? week.CalendarDates!.Value.Start
            : projection.SelectedSessions.Where(s => s.GlobalWeekNumber == week.GlobalWeekNumber).Min(s => s.SessionDate);

    private static bool IsLongRun(string role) => LongHorizonSessionRoleCodec.IsLongRun(role);
}
