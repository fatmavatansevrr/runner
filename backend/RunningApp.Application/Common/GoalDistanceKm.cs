using RunningApp.Domain.Enums;

namespace RunningApp.Application.Common;

/// <summary>
/// Fixed family-representative distance for a <see cref="GoalDistance"/>,
/// in km. Shared by <c>PlanServices</c> (target-race distance) and
/// <c>CatalogPreviewGenerator</c> (recent-race distance mapping) so the two
/// call sites can never drift apart.
/// </summary>
public static class GoalDistanceKm
{
    public static double Resolve(GoalDistance distance) => distance switch
    {
        GoalDistance.FiveK => 5.0,
        GoalDistance.TenK => 10.0,
        GoalDistance.HalfMarathon => 21.0975,
        GoalDistance.Marathon => 42.195,
        _ => 5.0
    };
}
