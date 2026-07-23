using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.Validation;

/// <summary>
/// Public-boundary shape validation for <see cref="GenerateRacePlanPreviewRequest"/>.
/// Throws <see cref="ArgumentException"/>, mapped to HTTP 400 by
/// <c>GlobalExceptionHandler</c> — same convention as the rest of this
/// codebase's request validators. Catalog feasibility (e.g. whether a
/// target time is achievable) is never decided here — that stays the
/// runtime-condition resolver pipeline's job (typed HTTP 422).
/// </summary>
public static class GenerateRacePlanPreviewRequestValidator
{
    public static void Validate(GenerateRacePlanPreviewRequest request)
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

        if (!request.PreferredDays.Contains(request.LongRunDay))
        {
            throw new ArgumentException($"LongRunDay '{request.LongRunDay}' must be a member of PreferredDays.");
        }

        if (request.RaceDate == default)
        {
            throw new ArgumentException("RaceDate is required.");
        }

        if (request.TargetFinishTimeSeconds <= 0)
        {
            throw new ArgumentException(
                $"TargetFinishTimeSeconds must be positive, but was {request.TargetFinishTimeSeconds}.");
        }

        // ── target_finish_time_source / target_finish_time_seconds consistency ──
        if (request.TargetFinishTimeSource == TargetFinishTimeSource.ProductAverage)
        {
            var canonicalSeconds = CanonicalTargetFinishTimePolicy.GetCanonicalSeconds(request.GoalDistance);
            if (canonicalSeconds is null)
            {
                throw new ArgumentException(
                    $"target_finish_time_source=product_average has no canonical value for goal_distance '{request.GoalDistance}'.");
            }

            if (request.TargetFinishTimeSeconds != canonicalSeconds.Value)
            {
                throw new ArgumentException(
                    $"target_finish_time_source=product_average requires target_finish_time_seconds to equal the " +
                    $"canonical average for '{request.GoalDistance}' ({canonicalSeconds.Value}), but was {request.TargetFinishTimeSeconds}.");
            }
        }

        // ── Readiness: conservative shape checks only. A null field is left ──
        // ── untouched (not defaulted); an explicit 0 is preserved and valid ──
        // ── (0 = explicitly reported zero readiness, distinct from null). ────
        // ── Only negative values are rejected. ────────────────────────────────
        if (request.RecentLongestRunKm is < 0)
        {
            throw new ArgumentException($"RecentLongestRunKm must be non-negative if provided, but was {request.RecentLongestRunKm}.");
        }
        if (request.RecentWeeklyVolumeKm is < 0)
        {
            throw new ArgumentException($"RecentWeeklyVolumeKm must be non-negative if provided, but was {request.RecentWeeklyVolumeKm}.");
        }
        if (request.RecentRunsPerWeek is < 0)
        {
            throw new ArgumentException($"RecentRunsPerWeek must be non-negative if provided, but was {request.RecentRunsPerWeek}.");
        }

        if (request.RecentRace is { } recentRace)
        {
            if (recentRace.FinishTimeSeconds <= 0)
            {
                throw new ArgumentException(
                    $"RecentRace.FinishTimeSeconds must be positive, but was {recentRace.FinishTimeSeconds}.");
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (recentRace.RaceDate > today)
            {
                throw new ArgumentException(
                    $"RecentRace.RaceDate ({recentRace.RaceDate}) must not be in the future (today is {today}).");
            }
        }
    }
}
