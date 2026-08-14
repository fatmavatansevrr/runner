namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6 — a generic, numeric-independent weekday/date assignment
/// authority for GE and Preparation Runway weeks (Core reuses the real
/// existing calendar authority via <see cref="LongHorizonCoreWorkoutBindingExecutor"/>
/// instead, since it is already produced as a side effect of real workout
/// binding). Deliberately does not reuse <c>PreparationRunwayCalendarComposer</c>
/// (which is coupled to Runway's own numeric materialization result) or
/// <c>CatalogWeekSkeletonCalendarMaterializer</c> (which requires a
/// <see cref="Materialization.GeneratedCatalogPlanSkeleton"/>, not this
/// phase's joined structural type) -- this is new, minimal, pure date
/// arithmetic reusing the same day-assignment convention (long run to the
/// preferred long-run day; the remaining preferred days filled in order)
/// already established by those authorities.
/// </summary>
internal static class LongHorizonCalendarAssigner
{
    public const string AssignerId = "LONG_HORIZON_CALENDAR_ASSIGNER_V1";

    /// <summary>Week N's 7-day window starts at StartDate + (globalWeekNumber-1)*7 days -- the same convention <see cref="Materialization.CatalogStageToWeekMaterializer"/> already uses.</summary>
    public static DateOnly WeekStartDate(DateOnly planStartDate, int globalWeekNumber) =>
        planStartDate.AddDays((globalWeekNumber - 1) * 7);

    /// <summary>
    /// Assigns one weekday per structural role: LONG_RUN to
    /// <paramref name="longRunDay"/>; KEY_SESSION to the first remaining
    /// preferred day (ordered ascending by <see cref="DayOfWeek"/>) that is
    /// not the long-run day; the two EASY_SUPPORT slots to the two remaining
    /// preferred days in ascending order. Requires exactly 4 distinct
    /// preferred days including the long-run day (the existing 4-day race
    /// plan hard-constraint policy, unchanged).
    /// </summary>
    public static IReadOnlyDictionary<string, DayOfWeek> AssignWeekdays(
        IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDay)
    {
        if (preferredDays is null || preferredDays.Count != 4 || preferredDays.Distinct().Count() != 4)
            throw new InvalidOperationException("Preferred days must contain exactly 4 distinct weekdays.");
        if (!preferredDays.Contains(longRunDay))
            throw new InvalidOperationException("Long-run day must be one of the preferred days.");

        var remaining = preferredDays.Where(d => d != longRunDay).OrderBy(d => (int)d).ToList();
        return new Dictionary<string, DayOfWeek>
        {
            ["LONG_RUN"] = longRunDay,
            ["KEY_SESSION"] = remaining[0],
            ["EASY_SUPPORT_1"] = remaining[1],
            ["EASY_SUPPORT_2"] = remaining[2],
        };
    }

    public static DateOnly AssignedDate(DateOnly weekStartDate, DayOfWeek weekday)
    {
        var offset = ((int)weekday - (int)weekStartDate.DayOfWeek + 7) % 7;
        return weekStartDate.AddDays(offset);
    }
}
