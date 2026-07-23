using RunningApp.Application.DTOs.Plan;

namespace RunningApp.Application.Validation;

/// <summary>
/// Public-boundary shape validation for <see cref="GenerateHabitPlanPreviewRequest"/>.
/// Throws <see cref="ArgumentException"/>, mapped to HTTP 400.
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
