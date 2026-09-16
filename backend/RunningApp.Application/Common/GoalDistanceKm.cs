using RunningApp.Application.Exceptions;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.Common;

/// <summary>
/// Fixed family-representative distance for a <see cref="GoalDistance"/>,
/// in km. Shared by <c>PlanServices</c> (target-race distance) and
/// <c>CatalogPreviewGenerator</c> (recent-race distance mapping) so the two
/// call sites can never drift apart.
///
/// PHASE DIST-GEN.0.1 safety fix: <see cref="GoalDistance.Custom"/> (and any
/// future unrecognized <see cref="GoalDistance"/> value) previously fell
/// through to an unconditional <c>_ => 5.0</c> default, meaning a request
/// carrying <c>GoalDistance.Custom</c> silently resolved to a 5K distance
/// end-to-end (e.g. a user typing "16" in the onboarding free-text distance
/// field would silently receive a plan generated as if for a 5K). No arm of
/// this switch may ever again produce a canonical km value for a value it
/// does not have real evidence-backed authority for. The intended, primary
/// defense is the earlier fail-closed gate in
/// <see cref="RunningApp.Application.Validation.GeneratePreviewRequestValidator"/>,
/// which rejects <see cref="GoalDistance.Custom"/> before this method is ever
/// reached on the public path — this throw is the defense-in-depth backstop
/// for any other (including future/internal/test) caller that reaches this
/// method with an unresolvable distance.
/// </summary>
public static class GoalDistanceKm
{
    public static double Resolve(GoalDistance distance) => distance switch
    {
        GoalDistance.FiveK => 5.0,
        GoalDistance.TenK => 10.0,
        GoalDistance.HalfMarathon => 21.0975,
        GoalDistance.Marathon => 42.195,
        _ => throw new UnsupportedGoalDistanceException(
            $"GoalDistance.{distance} has no canonical family-representative distance and must never be " +
            "silently substituted (e.g. defaulted to 5.0km). This is a fail-closed safety guard " +
            "(PHASE DIST-GEN.0.1) -- the public generate-preview boundary should have already rejected " +
            "this request before reaching GoalDistanceKm.Resolve."),
    };

    /// <summary>
    /// Non-throwing counterpart for call sites that need to check
    /// resolvability without triggering the fail-closed exception (e.g.
    /// validators that want to produce their own typed error message).
    /// </summary>
    public static bool TryResolve(GoalDistance distance, out double km)
    {
        switch (distance)
        {
            case GoalDistance.FiveK: km = 5.0; return true;
            case GoalDistance.TenK: km = 10.0; return true;
            case GoalDistance.HalfMarathon: km = 21.0975; return true;
            case GoalDistance.Marathon: km = 42.195; return true;
            default: km = default; return false;
        }
    }
}
