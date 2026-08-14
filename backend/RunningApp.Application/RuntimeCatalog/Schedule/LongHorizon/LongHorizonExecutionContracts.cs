namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6 — the final internal (dark, never-public) execution-level
/// contract, embedding (never duplicating) the Phase 4I.5 structural types.
/// Numeric and calendar fields are nullable: a segment whose numeric
/// execution was not performed by this phase (Preparation Runway, Core —
/// see this phase's own explicit non-implementation statement) carries null
/// <see cref="TotalVolumeKm"/>/<see cref="LongRunDistanceKm"/>/
/// <see cref="LongHorizonExecutedWorkoutSlot.PlannedDistanceKm"/> throughout,
/// never a fabricated or zero placeholder value.
/// </summary>
internal sealed record LongHorizonExecutedWorkoutSlot(
    LongHorizonStructuralWorkoutSlot Structural,
    string WorkoutKey,
    int WorkoutVersion,
    DayOfWeek? AssignedWeekday,
    DateOnly? AssignedDate,
    double? PlannedDistanceKm,
    bool IsLongRun,
    string Source);

internal sealed record LongHorizonExecutedWeek(
    LongHorizonStructuralWeek Structural,
    double? TotalVolumeKm,
    double? LongRunDistanceKm,
    string? NumericPolicyId,
    string? NumericPolicyVersion,
    DateOnly? WeekStartDate,
    IReadOnlyList<LongHorizonExecutedWorkoutSlot> OrderedSlots);

/// <summary>
/// Plan-level dark execution result. <see cref="GeNumericExecutionComplete"/>/
/// <see cref="RunwayNumericExecutionComplete"/>/<see cref="CoreNumericExecutionComplete"/>
/// make each segment's true completeness explicit and machine-checkable,
/// rather than requiring a consumer to infer it from scattered nulls.
/// </summary>
internal sealed record LongHorizonExecutedSchedule(
    LongHorizonGeneratedStructuralSkeleton Structural,
    DateOnly StartDate,
    bool GeNumericExecutionComplete,
    bool RunwayNumericExecutionComplete,
    bool CoreNumericExecutionComplete,
    IReadOnlyList<LongHorizonExecutedWeek> Weeks);
