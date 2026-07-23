using RunningApp.Domain.Enums;
using System.Text.Json.Serialization;

namespace RunningApp.Application.DTOs.Plan;

/// <summary>
/// Public transport DTO for <c>POST /api/v1/plans/generate-preview/habit</c>.
/// Contains only habit and shared fields — no race/target-time/recent-race/
/// custom properties exist on this type at all. `PlaceholderPlanGenerationEngine`
/// (the only generator habit requests ever reach) selects templates on
/// GoalDistance/Level/DaysPerWeek alone and never reads readiness fields
/// today; they are omitted here rather than carried over "because race has
/// them" (confirmed during the contract audit — see plan doc).
/// </summary>
public sealed class GenerateHabitPlanPreviewRequest
{
    public required GoalDistance GoalDistance { get; set; }

    /// <summary>See <see cref="GeneratePreviewRequest.Level"/> for why the property-level converter is required.</summary>
    [JsonConverter(typeof(RunningBackgroundCanonicalJsonConverter))]
    public required RunningBackground Level { get; set; }

    public required int DaysPerWeek { get; set; }
    public required DistanceUnit Unit { get; set; }
    public required DateOnly StartDate { get; set; }

    /// <summary>Canonical weekday tokens (mon..sun), distinct, count must equal <see cref="DaysPerWeek"/>.</summary>
    public required IReadOnlyList<Weekday> PreferredDays { get; set; }

    /// <summary>Optional for Habit. If provided, must be a member of <see cref="PreferredDays"/>.</summary>
    public Weekday? LongRunDay { get; set; }
}
