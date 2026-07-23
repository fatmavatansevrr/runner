using RunningApp.Domain.Enums;

namespace RunningApp.Application.Common;

/// <summary>
/// The single backend source of truth for "Go with average" canonical
/// finish times, by goal distance. These values are validation/reference
/// data only — never a DTO/command default — and must stay numerically
/// identical to the Flutter client's <c>AverageFinishTimePolicy</c>
/// (mobile/lib/core/models/average_finish_time_policy.dart), enforced by
/// parity tests on both sides rather than a shared codegen artifact (none
/// exists in this repo). Do not invent new values here independently of
/// that Flutter table.
/// </summary>
public static class CanonicalTargetFinishTimePolicy
{
    public const int FiveKSeconds = 28 * 60;
    public const int TenKSeconds = 58 * 60;
    public const int HalfMarathonSeconds = (2 * 3600) + (5 * 60);
    public const int MarathonSeconds = (4 * 3600) + (21 * 60);

    /// <summary>
    /// Returns the canonical product-average seconds for <paramref name="distance"/>,
    /// or null if no canonical value exists for it (e.g. <see cref="GoalDistance.Custom"/>).
    /// </summary>
    public static int? GetCanonicalSeconds(GoalDistance distance) => distance switch
    {
        GoalDistance.FiveK => FiveKSeconds,
        GoalDistance.TenK => TenKSeconds,
        GoalDistance.HalfMarathon => HalfMarathonSeconds,
        GoalDistance.Marathon => MarathonSeconds,
        _ => null,
    };
}
