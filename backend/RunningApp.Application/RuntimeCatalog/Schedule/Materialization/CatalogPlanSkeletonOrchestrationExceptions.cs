namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

// Backend Integration Phase 4F.3 — typed internal orchestration errors.
// No public HTTP endpoint invokes ICatalogPlanSkeletonOrchestrator as of
// this phase (Option A: internal orchestration service only, not called by
// live preview -- see PHASE4F_3_LIVE_CATALOG_STAGE_ALLOCATION_AND_SKELETON_INTEGRATION.md).
// None are registered in GlobalExceptionHandler. Never caught and downgraded
// into a default/partial skeleton -- Build() either returns a complete,
// validated CatalogPlanSkeletonOrchestrationResult or throws one of these.

/// <summary>Thrown when a candidate's loaded master template declares no phases at all. Error code equivalent: CATALOG_PHASE_ALLOCATION_SOURCE_MISSING.</summary>
internal sealed class CatalogPhaseAllocationSourceMissingException : Exception
{
    public CatalogPhaseAllocationSourceMissingException(string message) : base(message) { }
}

/// <summary>Thrown when the phase allocation itself is structurally invalid (blank/duplicate key, non-positive week count). Error code equivalent: CATALOG_PHASE_ALLOCATION_INVALID.</summary>
internal sealed class CatalogPhaseAllocationInvalidException : Exception
{
    public CatalogPhaseAllocationInvalidException(string message) : base(message) { }
}

/// <summary>Thrown when the phase allocation's total week count does not equal the authoritative planned-week-count (candidate.CoreCycle.DefaultWeeks). Never normalized, truncated, or padded. Error code equivalent: CATALOG_PHASE_ALLOCATION_TOTAL_MISMATCH.</summary>
internal sealed class CatalogPhaseAllocationTotalMismatchException : Exception
{
    public CatalogPhaseAllocationTotalMismatchException(string message) : base(message) { }
}

/// <summary>Thrown when the loaded candidate's actual MasterTemplate reference does not match the orchestration context's expected reference. Error code equivalent: CATALOG_MASTER_TEMPLATE_REFERENCE_MISMATCH.</summary>
internal sealed class CatalogMasterTemplateReferenceMismatchException : Exception
{
    public CatalogMasterTemplateReferenceMismatchException(string message) : base(message) { }
}

/// <summary>Thrown when the loaded candidate's actual Layout reference does not match the orchestration context's expected reference. Error code equivalent: CATALOG_RUN_LAYOUT_REFERENCE_MISMATCH.</summary>
internal sealed class CatalogRunLayoutReferenceMismatchException : Exception
{
    public CatalogRunLayoutReferenceMismatchException(string message) : base(message) { }
}

/// <summary>Thrown when the resolved run-layout slot sequence is structurally invalid (empty, blank role, wrong count, or an unsupported REST/OPTIONAL/RECOVERY role). Error code equivalent: CATALOG_RUN_LAYOUT_SLOT_INVALID.</summary>
internal sealed class CatalogRunLayoutSlotInvalidException : Exception
{
    public CatalogRunLayoutSlotInvalidException(string message) : base(message) { }
}

/// <summary>Thrown when the orchestration context itself is internally inconsistent (e.g. the loaded candidate's identity does not match the context's own expected candidate key/version). Error code equivalent: CATALOG_SKELETON_CONTEXT_INVALID.</summary>
internal sealed class CatalogSkeletonContextInvalidException : Exception
{
    public CatalogSkeletonContextInvalidException(string message) : base(message) { }
}

/// <summary>Generic typed catch-all wrapping a Phase 4F.2 materializer failure or a skeleton-validation failure encountered during orchestration. Never thrown for a condition covered by a more specific exception above. Error code equivalent: CATALOG_PLAN_SKELETON_ORCHESTRATION_FAILED.</summary>
internal sealed class CatalogPlanSkeletonOrchestrationFailedException : Exception
{
    public CatalogPlanSkeletonOrchestrationFailedException(string message) : base(message) { }
    public CatalogPlanSkeletonOrchestrationFailedException(string message, Exception innerException) : base(message, innerException) { }
}
