using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

/// <summary>Phase 4K.8B Part 2 — the three possible relations between a starting value and its Core Week-1 target.</summary>
internal enum PreparationRunwayDirectionRelation
{
    BelowTarget,
    EqualTarget,
    AboveTarget,
}

/// <summary>
/// Phase 4K.8B Part 2 — one deterministic comparer, reused everywhere a
/// direction relation is needed, instead of ad hoc comparisons scattered
/// across validators. Compares already-normalized values using the
/// repository's existing rounding granularity (0.5km, matching
/// <c>PreparationRunwayNumericPolicy.RoundingIncrementKm</c>) -- no new
/// tolerance is introduced.
/// </summary>
internal static class PreparationRunwayDirectionComparer
{
    public const double DefaultRoundingIncrementKm = 0.5;

    public static PreparationRunwayDirectionRelation Compare(double starting, double target, double roundingIncrementKm = DefaultRoundingIncrementKm)
    {
        var roundedStarting = Round(starting, roundingIncrementKm);
        var roundedTarget = Round(target, roundingIncrementKm);

        if (roundedStarting < roundedTarget) return PreparationRunwayDirectionRelation.BelowTarget;
        if (roundedStarting > roundedTarget) return PreparationRunwayDirectionRelation.AboveTarget;
        return PreparationRunwayDirectionRelation.EqualTarget;
    }

    private static double Round(double value, double increment) =>
        Math.Round(value / increment, MidpointRounding.AwayFromZero) * increment;
}

/// <summary>
/// Phase 4K.8A's approved direction support matrix (Phase 4K.8B Part 3):
/// weekly and long-run BelowTarget/EqualTarget are conditionally supported
/// (existing downstream validators still govern actual feasibility);
/// AboveTarget is unsupported for both, mapping to the existing
/// JIT_SEGMENT_TRANSITION_INFEASIBLE reason -- never a new public reason.
/// </summary>
internal sealed record PreparationRunwayDirectionPolicy
{
    public required PreparationRunwayDirectionRelation WeeklyDirection { get; init; }
    public required PreparationRunwayDirectionRelation LongRunDirection { get; init; }
    public required bool WeeklyDirectionSupported { get; init; }
    public required bool LongRunDirectionSupported { get; init; }
    public required bool OverallSupported { get; init; }
    public LongHorizonReasonCode? FailureReason { get; init; }
    public required string PolicyProvenance { get; init; }
}

/// <summary>
/// Phase 4K.8B Part 4 — evaluates the approved direction policy for a
/// candidate Runway start against a locked Core Week-1 target. Weekly and
/// long-run directions are evaluated independently (Phase 4K.8A §11: weekly
/// equality does not erase an independent long-run conflict).
/// </summary>
internal static class PreparationRunwayDirectionGuard
{
    public static PreparationRunwayDirectionPolicy Evaluate(
        double startingWeeklyVolumeKm,
        double coreWeekOneTargetWeeklyVolumeKm,
        double startingLongRunKm,
        double coreWeekOneTargetLongRunKm,
        double roundingIncrementKm = PreparationRunwayDirectionComparer.DefaultRoundingIncrementKm)
    {
        var weeklyDirection = PreparationRunwayDirectionComparer.Compare(startingWeeklyVolumeKm, coreWeekOneTargetWeeklyVolumeKm, roundingIncrementKm);
        var longRunDirection = PreparationRunwayDirectionComparer.Compare(startingLongRunKm, coreWeekOneTargetLongRunKm, roundingIncrementKm);

        var weeklySupported = weeklyDirection != PreparationRunwayDirectionRelation.AboveTarget;
        var longRunSupported = longRunDirection != PreparationRunwayDirectionRelation.AboveTarget;
        var overallSupported = weeklySupported && longRunSupported;

        return new PreparationRunwayDirectionPolicy
        {
            WeeklyDirection = weeklyDirection,
            LongRunDirection = longRunDirection,
            WeeklyDirectionSupported = weeklySupported,
            LongRunDirectionSupported = longRunSupported,
            OverallSupported = overallSupported,
            FailureReason = overallSupported ? null : LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitSegmentTransitionInfeasible),
            PolicyProvenance = "Phase 4K.8A -- weekly/long-run BelowTarget/EqualTarget conditionally supported, AboveTarget unsupported for both.",
        };
    }
}
