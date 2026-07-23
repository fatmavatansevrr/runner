using RunningApp.Application.Common;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.5 — the strict, one-time string-to-typed-weekday
/// adapter used only at the internal calendar-assignment boundary. Deliberately
/// separate from the existing lenient <see cref="RunningDay"/>/<c>RunningDay.NormalizeList</c>
/// convention (used by the legacy SQL confirm-time snapshot and by
/// <c>PlanServices.ResolvePreferredDays</c>'s default-pattern fallback): those
/// silently drop unrecognized entries and silently substitute a default
/// weekday spread when parsing yields nothing — behavior this phase's own
/// scope constraints explicitly forbid ("do not silently coerce typos," "do
/// not fall back to a default weekday pattern"). This adapter instead rejects
/// outright on any unknown value or duplicate. Per-day tolerance (case,
/// "Mon"/"Monday") is still accepted via the same <see cref="RunningDay.Normalize"/>
/// single-value normalizer — only the "silently drop and continue" behavior
/// is replaced with "reject the whole input."
/// </summary>
internal static class CatalogPreferredDayAdapter
{
    /// <summary>
    /// Parses a comma-separated day-name string (e.g. "Mon,Wed,Fri,Sun") into
    /// an ordered (input-order-preserving) list of <see cref="DayOfWeek"/>
    /// values. Throws <see cref="CatalogPreferredDaysRequiredException"/> if
    /// blank/null, <see cref="CatalogCalendarRoleStructureInvalidException"/>
    /// if any entry fails to normalize (unknown value), or
    /// <see cref="CatalogPreferredDaysDuplicatedException"/> if the same
    /// weekday appears more than once.
    /// </summary>
    public static IReadOnlyList<DayOfWeek> ParsePreferredDays(string? preferredDaysCsv)
    {
        if (string.IsNullOrWhiteSpace(preferredDaysCsv))
        {
            throw new CatalogPreferredDaysRequiredException(
                "Race-plan catalog preview requires PreferredDays, but none was supplied.");
        }

        var rawEntries = preferredDaysCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var days = new List<DayOfWeek>(rawEntries.Length);

        foreach (var raw in rawEntries)
        {
            var normalized = RunningDay.Normalize(raw);
            if (normalized is null)
            {
                throw new CatalogCalendarRoleStructureInvalidException(
                    $"PreferredDays entry '{raw}' is not a recognized day name. This adapter never silently " +
                    "drops or coerces an unrecognized value.");
            }

            var day = ParseFullDayName(normalized);
            if (days.Contains(day))
            {
                throw new CatalogPreferredDaysDuplicatedException(
                    $"PreferredDays contains a duplicate weekday: '{normalized}'. Each preferred weekday must " +
                    "appear at most once.");
            }

            days.Add(day);
        }

        return days;
    }

    /// <summary>
    /// Parses a single day-name string (e.g. "Sun") into a <see cref="DayOfWeek"/>.
    /// Throws <see cref="CatalogLongRunDayRequiredException"/> if blank/null,
    /// or <see cref="CatalogCalendarRoleStructureInvalidException"/> if the
    /// value fails to normalize.
    /// </summary>
    public static DayOfWeek ParseLongRunDay(string? longRunDay)
    {
        if (string.IsNullOrWhiteSpace(longRunDay))
        {
            throw new CatalogLongRunDayRequiredException(
                "Race-plan catalog preview requires LongRunDayPreference, but none was supplied.");
        }

        var normalized = RunningDay.Normalize(longRunDay);
        if (normalized is null)
        {
            throw new CatalogCalendarRoleStructureInvalidException(
                $"LongRunDayPreference value '{longRunDay}' is not a recognized day name.");
        }

        return ParseFullDayName(normalized);
    }

    private static DayOfWeek ParseFullDayName(string fullName) => fullName switch
    {
        RunningDay.Monday => DayOfWeek.Monday,
        RunningDay.Tuesday => DayOfWeek.Tuesday,
        RunningDay.Wednesday => DayOfWeek.Wednesday,
        RunningDay.Thursday => DayOfWeek.Thursday,
        RunningDay.Friday => DayOfWeek.Friday,
        RunningDay.Saturday => DayOfWeek.Saturday,
        RunningDay.Sunday => DayOfWeek.Sunday,
        _ => throw new CatalogCalendarRoleStructureInvalidException($"Unreachable: normalized day name '{fullName}' has no DayOfWeek mapping."),
    };
}
