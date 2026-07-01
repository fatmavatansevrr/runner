using System;
using System.Collections.Generic;
using System.Linq;

namespace RunningApp.Application.Common;

public static class RunningDay
{
    public const string Monday    = "Monday";
    public const string Tuesday   = "Tuesday";
    public const string Wednesday = "Wednesday";
    public const string Thursday  = "Thursday";
    public const string Friday    = "Friday";
    public const string Saturday  = "Saturday";
    public const string Sunday    = "Sunday";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday
    };

    /// <summary>
    /// Herhangi bir formattan ("mon", "MON", "monday", "Mon", "Monday")
    /// standart tam isme dönüştürür. Tanınamayan değer için null döner.
    /// </summary>
    public static string? Normalize(string? day)
    {
        if (string.IsNullOrWhiteSpace(day)) return null;
        return day.Trim().ToLowerInvariant() switch
        {
            "monday"    or "mon" => Monday,
            "tuesday"   or "tue" => Tuesday,
            "wednesday" or "wed" => Wednesday,
            "thursday"  or "thu" => Thursday,
            "friday"    or "fri" => Friday,
            "saturday"  or "sat" => Saturday,
            "sunday"    or "sun" => Sunday,
            _ => null
        };
    }

    /// <summary>
    /// Virgülle ayrılmış gün listesini normalize eder.
    /// "mon,tue,sat" → "Monday,Tuesday,Saturday"
    /// </summary>
    public static string? NormalizeList(string? days)
    {
        if (string.IsNullOrWhiteSpace(days)) return null;
        var normalized = days
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize)
            .Where(d => d != null)
            .ToList();
        return normalized.Count > 0 ? string.Join(",", normalized) : null;
    }
}
