using System;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.Validation;

/// <summary>
/// Public-boundary shape validation for <see cref="GeneratePreviewRequest"/>.
/// Runs before route decision so both the legacy SQL path and the catalog
/// path see identical 400s for structural request problems — catalog-only
/// typed exceptions (<c>Catalog*Exception</c>, mapped to 409/422 by
/// <c>GlobalExceptionHandler</c>) stay reserved for genuine catalog
/// feasibility failures, not request-shape problems.
///
/// Throws <see cref="ArgumentException"/>, which <c>GlobalExceptionHandler</c>
/// already maps to HTTP 400 — this codebase has no FluentValidation (or any
/// other validator) infrastructure, so this follows the file's own existing
/// inline-<c>ArgumentException</c> convention rather than introducing one.
/// </summary>
public static class GeneratePreviewRequestValidator
{
    public static void Validate(GeneratePreviewRequest request)
    {
        if (request.DaysPerWeek < 1 || request.DaysPerWeek > 7)
        {
            throw new ArgumentException($"DaysPerWeek must be between 1 and 7, but was {request.DaysPerWeek}.");
        }

        if (request.StartDate == default)
        {
            throw new ArgumentException("StartDate is required.");
        }

        var isRace = request.GoalType == GoalType.Race;

        // ── PreferredDays: required for both Race and Habit ─────────────────
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

        // ── LongRunDay: required for Race, optional for Habit; if present, ──
        // ── must belong to PreferredDays either way ──────────────────────────
        if (isRace && request.LongRunDay is null)
        {
            throw new ArgumentException("LongRunDay is required for race plans.");
        }

        if (request.LongRunDay is { } longRunDay && !request.PreferredDays.Contains(longRunDay))
        {
            throw new ArgumentException($"LongRunDay '{longRunDay}' must be a member of PreferredDays.");
        }

        // ── Race/Habit field consistency ─────────────────────────────────────
        // Race requires RaceDate/TargetFinishTimeSeconds(+positive); Habit
        // forbids every race-only field outright rather than silently
        // ignoring it — a Habit request carrying RaceDate/RaceName/target
        // time is contradictory input, not a benign no-op.
        if (isRace)
        {
            if (request.RaceDate is null)
            {
                throw new ArgumentException("RaceDate is required for race plans.");
            }

            if (request.TargetFinishTimeSeconds is null)
            {
                throw new ArgumentException("TargetFinishTimeSeconds is required for race plans.");
            }

            if (request.TargetFinishTimeSeconds <= 0)
            {
                throw new ArgumentException(
                    $"TargetFinishTimeSeconds must be positive for race plans, but was {request.TargetFinishTimeSeconds}.");
            }
        }
        else
        {
            if (request.RaceDate is not null)
            {
                throw new ArgumentException("RaceDate must not be provided for habit plans.");
            }

            if (request.TargetFinishTimeSeconds is not null)
            {
                throw new ArgumentException("TargetFinishTimeSeconds must not be provided for habit plans.");
            }

            if (request.RaceName is not null)
            {
                throw new ArgumentException("RaceName must not be provided for habit plans.");
            }
        }

        // ── Readiness fields: conservative shape checks only. A null field ──
        // ── is left untouched (not defaulted); an explicit 0 is preserved ───
        // ── and valid (0 = explicitly reported zero readiness, distinct ────
        // ── from null). Only negative values are rejected. ─────────────────
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
