using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

/// <summary>
/// Phase 4K.8B — typed exception family for the Preparation Runway
/// direction-guard/bounded-prescription contracts, matching the existing
/// <see cref="LongHorizonRollingContractException"/> convention (typed
/// <c>Code</c> + <see cref="InvalidOperationException"/>). Dark and unwired.
/// </summary>
internal sealed class PreparationRunwayDirectionUnsupportedException : LongHorizonRollingContractException
{
    public PreparationRunwayDirectionUnsupportedException(string message) : base("PREPARATION_RUNWAY_DIRECTION_UNSUPPORTED", message) { }
}

internal sealed class PreparationRunwayFullPrescriptionInvalidException : LongHorizonRollingContractException
{
    public PreparationRunwayFullPrescriptionInvalidException(string message) : base("PREPARATION_RUNWAY_FULL_PRESCRIPTION_INVALID", message) { }
}

internal sealed class PreparationRunwayTargetLockScopeViolationException : LongHorizonRollingContractException
{
    public PreparationRunwayTargetLockScopeViolationException(string message) : base("PREPARATION_RUNWAY_TARGET_LOCK_SCOPE_VIOLATION", message) { }
}

internal sealed class PreparationRunwayMidRunwayRefreshViolationException : LongHorizonRollingContractException
{
    public PreparationRunwayMidRunwayRefreshViolationException(string message) : base("PREPARATION_RUNWAY_MID_RUNWAY_REFRESH_VIOLATION", message) { }
}

internal sealed class PreparationRunwayBoundedSliceInvalidException : LongHorizonRollingContractException
{
    public PreparationRunwayBoundedSliceInvalidException(string message) : base("PREPARATION_RUNWAY_BOUNDED_SLICE_INVALID", message) { }
}

internal sealed class PreparationRunwaySliceEquivalenceViolationException : LongHorizonRollingContractException
{
    public PreparationRunwaySliceEquivalenceViolationException(string message) : base("PREPARATION_RUNWAY_SLICE_EQUIVALENCE_VIOLATION", message) { }
}

internal sealed class PreparationRunwayTerminalStageViolationException : LongHorizonRollingContractException
{
    public PreparationRunwayTerminalStageViolationException(string message) : base("PREPARATION_RUNWAY_TERMINAL_STAGE_VIOLATION", message) { }
}
