namespace RunningApp.Domain.Enums;

/// <summary>
/// Canonical weekday token for the public request boundary (preferred_days,
/// long_run_day). Serializes via the API's global
/// <c>JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)</c>
/// (see <c>RunningApp.Api/Program.cs</c>) to exactly "mon".."sun" — no
/// bespoke converter needed. Any other spelling ("Monday", "MON", "1") fails
/// deserialization and surfaces as a 400, matching
/// <see cref="RunningBackgroundCanonicalJsonConverter"/>'s strictness at the
/// same public boundary.
/// </summary>
public enum Weekday
{
    Mon,
    Tue,
    Wed,
    Thu,
    Fri,
    Sat,
    Sun
}
