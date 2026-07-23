using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.Commands.Plan;

/// <summary>
/// The validated, typed internal representation of a generate-preview
/// request — the boundary type between the public transport DTO
/// (<see cref="GeneratePreviewRequest"/>, all conditionally-required fields
/// nullable) and generation logic. Produced only by
/// <see cref="GeneratePreviewCommandMapper.ToCommand"/>, which assumes
/// <see cref="RunningApp.Application.Validation.GeneratePreviewRequestValidator.Validate"/>
/// has already passed — that is the one place a null-forgiving operator on
/// a conditionally-required field is allowed in this codebase; every other
/// call site should be able to trust this type's own non-nullability
/// instead of re-deriving it.
///
/// Race-only and Habit-only fields live on <see cref="RacePlanPreviewCommand"/>/
/// <see cref="HabitPlanPreviewCommand"/> respectively, not on this shared
/// base — a Habit command has no RaceDate/TargetFinishTimeSeconds field to
/// be accidentally read at all, not merely a null one.
/// </summary>
public abstract record PlanPreviewCommand
{
    public required GoalType GoalType { get; init; }
    public required GoalDistance GoalDistance { get; init; }
    public required RunningBackground Level { get; init; }
    public required int DaysPerWeek { get; init; }
    public required DistanceUnit Unit { get; init; }
    public required DateOnly StartDate { get; init; }
    public required IReadOnlyList<Weekday> PreferredDays { get; init; }

    public double? RecentWeeklyVolumeKm { get; init; }
    public double? RecentLongestRunKm { get; init; }
    public int? RecentRunsPerWeek { get; init; }
    public RecentRaceInput? RecentRace { get; init; }
}

/// <summary>
/// A validated Race generate-preview request. Every field a race plan
/// genuinely needs is non-nullable and required — there is no code path
/// that can construct one with a missing race date, long-run day, or
/// target time.
/// </summary>
public sealed record RacePlanPreviewCommand : PlanPreviewCommand
{
    public required DateOnly RaceDate { get; init; }
    public required Weekday LongRunDay { get; init; }

    /// <summary>Always positive — enforced by the validator before this command exists.</summary>
    public required int TargetFinishTimeSeconds { get; init; }

    /// <summary>
    /// Always set for a race command — required, never inferred from the
    /// numeric value or from recent-race presence. See
    /// PHASE4D_4_1_PRODUCT_AVERAGE_TARGET_TIME_GOAL_FEASIBILITY_CLASSIFICATION.md.
    /// </summary>
    public required TargetFinishTimeSource TargetFinishTimeSource { get; init; }

    public string? RaceName { get; init; }
}

/// <summary>
/// A validated Habit generate-preview request. Has no RaceDate, RaceName,
/// or TargetFinishTimeSeconds property at all — a habit-generation code
/// path can't accidentally read a meaningless race field because there is
/// none to read.
/// </summary>
public sealed record HabitPlanPreviewCommand : PlanPreviewCommand
{
    public Weekday? LongRunDay { get; init; }
}
