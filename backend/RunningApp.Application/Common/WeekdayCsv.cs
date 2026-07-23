using RunningApp.Domain.Enums;

namespace RunningApp.Application.Common;

/// <summary>
/// Bridges the public request boundary's typed <see cref="Weekday"/>
/// values back to the legacy comma-separated full-day-name string
/// (<see cref="RunningDay"/>) that <c>CatalogPreferredDayAdapter</c>,
/// <c>RunningDay.NormalizeList</c>, and the legacy SQL confirm path still
/// consume internally. Deliberately the only place this translation
/// happens — those internals are otherwise untouched.
/// </summary>
public static class WeekdayCsv
{
    public static string? ToCsv(IReadOnlyList<Weekday>? days)
    {
        if (days is null || days.Count == 0) return null;
        return string.Join(",", days.Select(ToFullName));
    }

    public static string? ToCsv(Weekday? day) => day is null ? null : ToFullName(day.Value);

    private static string ToFullName(Weekday day) => day switch
    {
        Weekday.Mon => RunningDay.Monday,
        Weekday.Tue => RunningDay.Tuesday,
        Weekday.Wed => RunningDay.Wednesday,
        Weekday.Thu => RunningDay.Thursday,
        Weekday.Fri => RunningDay.Friday,
        Weekday.Sat => RunningDay.Saturday,
        Weekday.Sun => RunningDay.Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(day), day, "Unreachable: all Weekday values are mapped."),
    };
}
