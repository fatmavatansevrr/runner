using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;

namespace RunningApp.Application.Validation;

/// <summary>
/// Public-boundary shape validation for <see cref="GenerateHabitPlanPreviewRequest"/>.
/// Throws <see cref="ArgumentException"/>, mapped to HTTP 400, for structural
/// shape problems. PHASE DIST-GEN.0.1 also rejects an unsupported
/// <c>GoalDistance</c> (e.g. Custom) here with a typed
/// <see cref="UnsupportedGoalDistanceException"/> (HTTP 422) -- this is the
/// habit-plan path's own independent fail-closed gate; it does not rely
/// solely on the shared internal <c>GeneratePreviewRequestValidator</c>
/// downstream repeating the same check.
/// </summary>
public static class GenerateHabitPlanPreviewRequestValidator
{
    public static void Validate(GenerateHabitPlanPreviewRequest request)
    {
        if (request.DaysPerWeek < 1 || request.DaysPerWeek > 7)
        {
            throw new ArgumentException($"DaysPerWeek must be between 1 and 7, but was {request.DaysPerWeek}.");
        }

        if (request.StartDate == default)
        {
            throw new ArgumentException("StartDate is required.");
        }

        if (!GoalDistanceKm.TryResolve(request.GoalDistance, out _))
        {
            throw new UnsupportedGoalDistanceException(
                $"goal_distance '{request.GoalDistance}' is not a supported goal distance for plan generation. " +
                "Only five_k, ten_k, half_marathon, and marathon are currently supported. Reason: GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED.");
        }

        if (request.PreferredDays is null || request.PreferredDays.Count == 0)
        {
            throw new ArgumentException("PreferredDays is required and must not be empty.");
        }

        if (request.PreferredDays.Distinct().Count() != request.PreferredDays.Count)
        {
            throw new ArgumentException("PreferredDays must not contain duplicate weekdays.");
        }

        if (request.PreferredDays.Count != request.DaysPerWeek)
        {
            throw new ArgumentException(
                $"PreferredDays count ({request.PreferredDays.Count}) must equal DaysPerWeek ({request.DaysPerWeek}).");
        }

        if (request.LongRunDay is { } longRunDay && !request.PreferredDays.Contains(longRunDay))
        {
            throw new ArgumentException($"LongRunDay '{longRunDay}' must be a member of PreferredDays.");
        }
    }
}
