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
    /// not the long-run day; the remaining EASY_SUPPORT slots to the
    /// remaining preferred days in ascending order, keyed
    /// <c>EASY_SUPPORT_1</c>.."EASY_SUPPORT_&lt;<paramref name="daysPerWeek"/>-2&gt;".
    /// Requires exactly <paramref name="daysPerWeek"/> distinct preferred
    /// days including the long-run day. Phase 10K-FREQ.6D.14 generalized the
    /// prior hardcoded <c>==4</c> requirement/2-EASY output off
    /// <paramref name="daysPerWeek"/> (default 4, byte-identical for every
    /// pre-FREQ.6D.14 caller).
    /// </summary>
    public static IReadOnlyDictionary<string, DayOfWeek> AssignWeekdays(
        IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDay, int daysPerWeek = 4)
    {
        // Phase 10K-GEN.34 -- widened from "daysPerWeek must be at least 3"
        // to admit 2. A real 2D GE/Runway/Core week never has both a
        // KEY_SESSION and an EASY_SUPPORT slot in the same week (GEN.11 §1's
        // alternating Model B) -- its single non-long-run slot is
        // KEY_SESSION on a Pattern-A week and EASY_SUPPORT_1 on a Pattern-B
        // week, on the *same* physical weekday every week (the alternation is
        // in the role assigned to that weekday's session, not in which
        // weekday is used). This weekday-assignment dictionary is built once
        // per activation window and reused by MapActivatedWeek's per-week
        // RoleKeyFor lookup, so for 2D it must carry both role-key spellings
        // pointing at the same single remaining day. Byte-identical for
        // every daysPerWeek >= 3 caller (a real 3D+ week never needs both
        // "KEY_SESSION" and "EASY_SUPPORT_1" to resolve to the same day).
        if (daysPerWeek < 2)
            throw new InvalidOperationException("daysPerWeek must be at least 2 (one alternating KEY_SESSION/EASY_SUPPORT slot + LONG_RUN).");
        if (preferredDays is null || preferredDays.Count != daysPerWeek || preferredDays.Distinct().Count() != daysPerWeek)
            throw new InvalidOperationException($"Preferred days must contain exactly {daysPerWeek} distinct weekdays.");
        if (!preferredDays.Contains(longRunDay))
            throw new InvalidOperationException("Long-run day must be one of the preferred days.");

        var remaining = preferredDays.Where(d => d != longRunDay).OrderBy(d => (int)d).ToList();
        var assignments = new Dictionary<string, DayOfWeek>
        {
            ["LONG_RUN"] = longRunDay,
            ["KEY_SESSION"] = remaining[0],
        };
        if (daysPerWeek == 2)
        {
            assignments["EASY_SUPPORT_1"] = remaining[0];
        }
        else
        {
            for (var i = 1; i < remaining.Count; i++)
                assignments[$"EASY_SUPPORT_{i}"] = remaining[i];
        }
        return assignments;
    }

    public static DateOnly AssignedDate(DateOnly weekStartDate, DayOfWeek weekday)
    {
        var offset = ((int)weekday - (int)weekStartDate.DayOfWeek + 7) % 7;
        return weekStartDate.AddDays(offset);
    }
}
