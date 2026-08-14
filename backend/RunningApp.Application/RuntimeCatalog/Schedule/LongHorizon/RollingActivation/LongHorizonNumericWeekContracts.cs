namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.5 Part 4 — a numeric prescription contract for one activated
/// (or pending) week. All executable numeric fields are nullable: a
/// NumericPending week carries all nulls (never a fabricated zero, Phase
/// 4K.1 §7); a NumericActivated week must have all of them populated (see
/// <see cref="LongHorizonActivatedNumericWeekValidator"/>). Individual
/// session-level prescription content is intentionally represented as
/// opaque role/definition-id references rather than the existing rich
/// session-prescription types (<c>FourDaySessionDistanceAllocation</c> etc.)
/// -- this contract governs the lifecycle/evidence shape only and must not
/// create a second, competing session-prescription authority; a future
/// implementation phase wires this to the existing types.
/// </summary>
internal sealed record ActivatedNumericWeek
{
    public required int GlobalWeekNumber { get; init; }
    public required LongHorizonStructuralSegmentType SegmentType { get; init; }
    public required LongHorizonNumericLifecycleState LifecycleState { get; init; }
    public double? TotalWeeklyVolumeKm { get; init; }
    public double? LongRunKm { get; init; }
    public IReadOnlyList<LongHorizonSessionPrescriptionReference>? SessionPrescriptions { get; init; }
    public (DateOnly Start, DateOnly End)? CalendarDates { get; init; }
    public LongHorizonEvidenceAuthorityRecord? PaceIntensityContext { get; init; }
    public LongHorizonEvidenceAuthorityRecord? EvidenceProvenance { get; init; }
    public string? NumericPolicyProvenance { get; init; }
    public LongHorizonContextVersion? ContextVersion { get; init; }
    public Guid? CheckpointDecisionId { get; init; }
}

/// <summary>Phase 4K.5 Part 4 — an opaque reference to an existing session prescription; not itself a prescription authority.</summary>
internal sealed record LongHorizonSessionPrescriptionReference
{
    /// <summary>Stable structural slot ordinal used for one-to-one calendar mapping.</summary>
    public int? SessionOrdinal { get; init; }
    public required string SessionRole { get; init; }
    public required double DistanceKm { get; init; }
    public string? WorkoutKey { get; init; }
    public int? WorkoutVersion { get; init; }
    public DateOnly? AssignedDate { get; init; }
    public string? Source { get; init; }
}

/// <summary>Phase 4K.5 Part 4 — validates the NumericPending-null / NumericActivated-complete distinction and the session-sum invariant.</summary>
internal static class LongHorizonActivatedNumericWeekValidator
{
    private const double DistanceToleranceKm = 0.05;

    public static void Validate(ActivatedNumericWeek week)
    {
        switch (week.LifecycleState)
        {
            case LongHorizonNumericLifecycleState.StructurallyPlanned:
            case LongHorizonNumericLifecycleState.NumericPending:
            case LongHorizonNumericLifecycleState.NumericActivationBlocked:
                ValidatePendingHasNoExecutableValues(week);
                break;

            case LongHorizonNumericLifecycleState.NumericActivated:
            case LongHorizonNumericLifecycleState.Completed:
            case LongHorizonNumericLifecycleState.Missed:
                ValidateActivatedHasCompleteExecutableValues(week);
                break;

            default:
                throw new LongHorizonNumericWeekInvalidException($"Unhandled lifecycle state {week.LifecycleState}.");
        }
    }

    private static void ValidatePendingHasNoExecutableValues(ActivatedNumericWeek week)
    {
        if (week.TotalWeeklyVolumeKm is not null || week.LongRunKm is not null
            || week.SessionPrescriptions is not null || week.CalendarDates is not null)
        {
            throw new LongHorizonNumericWeekInvalidException(
                $"Week {week.GlobalWeekNumber} is {week.LifecycleState} and must carry only null executable numeric fields " +
                "(never a fabricated zero, Phase 4K.1 §7).");
        }
    }

    private static void ValidateActivatedHasCompleteExecutableValues(ActivatedNumericWeek week)
    {
        if (week.TotalWeeklyVolumeKm is null || week.LongRunKm is null
            || week.SessionPrescriptions is null || week.CalendarDates is null)
        {
            throw new LongHorizonNumericWeekInvalidException(
                $"Week {week.GlobalWeekNumber} is {week.LifecycleState} and requires a complete numeric prescription.");
        }

        if (week.TotalWeeklyVolumeKm <= 0)
        {
            throw new LongHorizonNumericWeekInvalidException($"Week {week.GlobalWeekNumber}'s TotalWeeklyVolumeKm must be positive.");
        }

        foreach (var session in week.SessionPrescriptions)
        {
            if (session.DistanceKm <= 0)
            {
                throw new LongHorizonNumericWeekInvalidException(
                    $"Week {week.GlobalWeekNumber} session '{session.SessionRole}' must have a positive DistanceKm -- zero/negative sessions are not permitted.");
            }
        }

        var sessionSum = week.SessionPrescriptions.Sum(s => s.DistanceKm);
        if (Math.Abs(sessionSum - week.TotalWeeklyVolumeKm.Value) > DistanceToleranceKm)
        {
            throw new LongHorizonNumericWeekInvalidException(
                $"Week {week.GlobalWeekNumber}'s session distances ({sessionSum:0.##}km) must sum to the approved weekly total ({week.TotalWeeklyVolumeKm:0.##}km).");
        }
    }
}
