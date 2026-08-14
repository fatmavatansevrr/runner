namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal static class LongHorizonFinalLifecycleValidator
{
    public static void Validate(LongHorizonFullDarkLifecycleState state)
    {
        LongHorizonStructuralRoadmapValidator.Validate(state.StructuralRoadmap);
        if (state.LifecycleStates.Count != state.StructuralRoadmap.TotalWeeks
            || !state.LifecycleStates.Keys.OrderBy(x => x).SequenceEqual(Enumerable.Range(1, state.StructuralRoadmap.TotalWeeks)))
            throw new LongHorizonNumericWeekInvalidException("Final lifecycle must contain exactly the contiguous requested global weeks.");
        if (state.LifecycleStates.Values.Any(value => value is LongHorizonNumericLifecycleState.NumericPending
                or LongHorizonNumericLifecycleState.NumericActivationBlocked
                or LongHorizonNumericLifecycleState.StructurallyPlanned))
            throw new LongHorizonNumericWeekInvalidException("Final lifecycle cannot contain pending, blocked, or merely structural weeks.");
        if (state.ActivatedWeeks.Count != state.StructuralRoadmap.TotalWeeks)
            throw new LongHorizonNumericWeekInvalidException("Final lifecycle must retain one accumulated numeric week for every structural week.");

        var allDates = new HashSet<DateOnly>();
        foreach (var globalWeek in Enumerable.Range(1, state.StructuralRoadmap.TotalWeeks))
        {
            var week = state.ActivatedWeeks[globalWeek];
            LongHorizonActivatedNumericWeekValidator.Validate(week);
            if (week.GlobalWeekNumber != globalWeek)
                throw new LongHorizonNumericWeekInvalidException("Accumulated week identity does not match its global key.");
            if (week.SegmentType is LongHorizonStructuralSegmentType.PreparationRunway or LongHorizonStructuralSegmentType.Core)
            {
                if (week.SessionPrescriptions is null || week.SessionPrescriptions.Count != 4
                    || week.SessionPrescriptions.Any(session => session.AssignedDate is null))
                    throw new LongHorizonActivatedCalendarAlignmentException("Every final Runway/Core week requires four authoritative session dates.");
            }

            foreach (var date in week.SessionPrescriptions?.Select(s => s.AssignedDate).OfType<DateOnly>() ?? [])
                if (!allDates.Add(date))
                    throw new LongHorizonActivatedCalendarAlignmentException($"Duplicate executable date {date:yyyy-MM-dd} in accumulated lifecycle.");
        }

        if (state.ActivatedWeeks[state.StructuralRoadmap.TotalWeeks].SegmentType != LongHorizonStructuralSegmentType.Core)
            throw new LongHorizonNumericWeekInvalidException("The final structural week must be the final Core week.");
        if (state.RunwayPrescription is null || state.RunwayTargetLock is null || state.RunwayCalendarProjection is null)
            throw new LongHorizonNumericWeekInvalidException("A completed lifecycle must retain the immutable Runway prescription, target lock, and calendar projection.");
    }
}
