using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.8D — an immutable value-equivalent reference to one session
/// already dated by the real Preparation Runway/Core calendar composition.
/// It selects and carries authority; it never calculates a date.
/// </summary>
internal sealed record LongHorizonActivatedSessionCalendarProjection
{
    public required int GlobalWeekNumber { get; init; }
    public required LongHorizonStructuralSegmentType Segment { get; init; }
    public required int SessionOrdinal { get; init; }
    public required string SessionRole { get; init; }
    public required string WorkoutKey { get; init; }
    public required int WorkoutVersion { get; init; }
    public required DateOnly SessionDate { get; init; }
    public required DayOfWeek Weekday { get; init; }
    public required string PreferredDayProvenance { get; init; }
    public required string LongRunDayProvenance { get; init; }
    public required string CalendarCompositionIdentity { get; init; }
    public required string CalendarCompositionVersion { get; init; }
    public required string OriginalComposedSessionIdentity { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public PreparationRunwayPrescriptionId? RunwayPrescriptionId { get; init; }
    public Guid? RunwaySliceId { get; init; }
    public Guid? CoreTargetLockId { get; init; }
}

/// <summary>
/// The full immutable Runway calendar counterpart to the full immutable
/// numeric prescription. Continuation windows select exact references from
/// this value; they never recompose or redetermine dates.
/// </summary>
internal sealed record LongHorizonLockedRunwayCalendarProjection
{
    public required Guid ProjectionId { get; init; }
    public required PreparationRunwayPrescriptionId PrescriptionId { get; init; }
    public required int StartGlobalWeek { get; init; }
    public required int EndGlobalWeek { get; init; }
    public required IReadOnlyList<LongHorizonActivatedSessionCalendarProjection> Sessions { get; init; }
    public required string CalendarCompositionIdentity { get; init; }
    public required string CalendarCompositionVersion { get; init; }
    public bool Immutable { get; init; } = true;
}

internal sealed record LongHorizonActivatedCalendarProjectionResult
{
    public required Guid ProjectionId { get; init; }
    public required IReadOnlyList<LongHorizonActivatedSessionCalendarProjection> SelectedSessions { get; init; }
    public LongHorizonLockedRunwayCalendarProjection? FullRunwayProjection { get; init; }
    public required IReadOnlyList<string> ValidationStages { get; init; }
}

internal class LongHorizonSessionCalendarProjectionMismatchException : LongHorizonRollingContractException
{
    public LongHorizonSessionCalendarProjectionMismatchException(string message) : base("LONG_HORIZON_SESSION_CALENDAR_PROJECTION_MISMATCH", message) { }
}

internal sealed class LongHorizonDuplicateDatedSessionException : LongHorizonSessionCalendarProjectionMismatchException
{
    public LongHorizonDuplicateDatedSessionException(string message) : base(message) { }
}

internal sealed class LongHorizonMissingDatedSessionException : LongHorizonSessionCalendarProjectionMismatchException
{
    public LongHorizonMissingDatedSessionException(string message) : base(message) { }
}

internal sealed class LongHorizonCalendarIdentityMismatchException : LongHorizonSessionCalendarProjectionMismatchException
{
    public LongHorizonCalendarIdentityMismatchException(string message) : base(message) { }
}

internal sealed class LongHorizonActivatedCalendarAlignmentException : LongHorizonRollingContractException
{
    /// <summary>
    /// HM.18 Blocker 3 -- optional, more specific sub-reason alongside this
    /// exception's own fixed <see cref="LongHorizonRollingContractException.Code"/>.
    /// Null for every throw site except the two "not on a preferred day"/
    /// "not on the preferred long-run day" checks in
    /// <see cref="LongHorizonActivatedCalendarAlignmentValidator"/>, which set
    /// it to <c>PREFERRED_DAY_PLACEMENT_INFEASIBLE</c> -- the same reason code
    /// this phase adds to the reachable Standard-Core sibling condition
    /// (<see cref="RunningApp.Application.RuntimeCatalog.Schedule.Materialization.CatalogPreferredDayPlacementInfeasibleException"/>).
    /// This type remains dark/unwired (per its own class doc comment, "not
    /// thrown by any production/live request path") -- LongHorizon activation
    /// is explicitly out of this phase's scope; this is a consistency/paperwork
    /// change only, added so the two structurally-identical conditions share
    /// one vocabulary if/when LongHorizon is ever activated.
    /// </summary>
    public string? Reason { get; }

    public LongHorizonActivatedCalendarAlignmentException(string message, string? reason = null)
        : base("LONG_HORIZON_ACTIVATED_CALENDAR_ALIGNMENT_FAILED", message)
    {
        Reason = reason;
    }
}
