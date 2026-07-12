namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

// Backend Integration Phase 4F.2 — typed materialization errors thrown only
// by ICatalogStageToWeekMaterializer. No public HTTP endpoint invokes the
// materializer as of this phase (see the phase's own documented "Integration
// boundary" — live CatalogPreviewGenerator is unchanged), so these are
// deliberately kept as plain internal/application exceptions, NOT registered
// in GlobalExceptionHandler. Never swallowed and converted into a partial
// skeleton -- Materialize() either returns a complete, valid
// GeneratedCatalogWeekSkeletonResult or throws one of these.

/// <summary>
/// Thrown when the supplied stage (phase) week allocation is structurally
/// invalid: a zero/negative week count for a stage, a duplicate stage key in
/// the allocation list, or the allocation's stage-key order does not exactly
/// match <see cref="CatalogStageToWeekMaterializationContext.SelectedStageSequence"/>.
/// Error code equivalent: CATALOG_STAGE_ALLOCATION_INVALID.
/// </summary>
public sealed class CatalogStageAllocationInvalidException : Exception
{
    public CatalogStageAllocationInvalidException(string message) : base(message) { }
}

/// <summary>
/// Thrown when <see cref="CatalogStageToWeekMaterializationContext.SelectedStageSequence"/>
/// itself is structurally invalid (empty, contains a duplicate stage key, or
/// contains an unknown/blank stage key). The materializer never invents,
/// reorders, or substitutes a stage sequence -- it only validates the one it
/// was given.
/// Error code equivalent: CATALOG_STAGE_SEQUENCE_INVALID.
/// </summary>
public sealed class CatalogStageSequenceInvalidException : Exception
{
    public CatalogStageSequenceInvalidException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the sum of the stage allocations' week counts does not equal
/// <see cref="CatalogStageToWeekMaterializationContext.PlannedWeekCount"/> --
/// either fewer or more allocated weeks than planned. The materializer never
/// silently rebalances, rounds, or redistributes the difference.
/// Error code equivalent: CATALOG_STAGE_WEEK_COUNT_MISMATCH.
/// </summary>
public sealed class CatalogStageWeekCountMismatchException : Exception
{
    public CatalogStageWeekCountMismatchException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the supplied run-layout slot-role sequence is structurally
/// invalid (empty, or its length does not match
/// <see cref="CatalogStageToWeekMaterializationContext.DaysPerWeek"/>).
/// Error code equivalent: CATALOG_RUN_LAYOUT_INVALID.
/// </summary>
public sealed class CatalogRunLayoutInvalidException : Exception
{
    public CatalogRunLayoutInvalidException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a generated week's structural session-slot count does not
/// equal <see cref="CatalogStageToWeekMaterializationContext.DaysPerWeek"/>.
/// In the current implementation this indicates an internal materializer
/// defect (the slot-count is derived directly from the validated run-layout
/// input) rather than a caller-input problem, but is kept as an explicit,
/// separately-named check per Phase 4F.2's own error taxonomy rather than
/// being folded into <see cref="CatalogRunLayoutInvalidException"/>.
/// Error code equivalent: CATALOG_SESSION_SLOT_COUNT_MISMATCH.
/// </summary>
public sealed class CatalogSessionSlotCountMismatchException : Exception
{
    public CatalogSessionSlotCountMismatchException(string message) : base(message) { }
}

/// <summary>
/// Generic typed catch-all for a materialization failure that does not fit
/// one of the more specific exceptions above (e.g. an internally
/// inconsistent context that structural validation did not anticipate).
/// Never thrown for a condition covered by a more specific exception type.
/// Error code equivalent: CATALOG_STAGE_TO_WEEK_MATERIALIZATION_FAILED.
/// </summary>
public sealed class CatalogStageToWeekMaterializationFailedException : Exception
{
    public CatalogStageToWeekMaterializationFailedException(string message) : base(message) { }
    public CatalogStageToWeekMaterializationFailedException(string message, Exception innerException) : base(message, innerException) { }
}
