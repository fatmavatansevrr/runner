namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.5 — typed exception base for every rolling-activation contract
/// validator in this namespace, matching the existing
/// <c>CatalogSessionPrescriptionException</c>/<c>CatalogVolumeException</c>
/// convention (typed <see cref="Code"/> + <see cref="InvalidOperationException"/>).
/// Dark and unwired -- not thrown by any production/live request path.
/// </summary>
internal abstract class LongHorizonRollingContractException : InvalidOperationException
{
    public string Code { get; }

    protected LongHorizonRollingContractException(string code, string message) : base(message)
    {
        Code = code;
    }
}

internal sealed class LongHorizonIllegalLifecycleTransitionException : LongHorizonRollingContractException
{
    public LongHorizonIllegalLifecycleTransitionException(string message) : base("LONG_HORIZON_ILLEGAL_LIFECYCLE_TRANSITION", message) { }
}

internal sealed class LongHorizonStructuralRoadmapInvalidException : LongHorizonRollingContractException
{
    public LongHorizonStructuralRoadmapInvalidException(string message) : base("LONG_HORIZON_STRUCTURAL_ROADMAP_INVALID", message) { }
}

internal sealed class LongHorizonActivationWindowInvalidException : LongHorizonRollingContractException
{
    public LongHorizonActivationWindowInvalidException(string message) : base("LONG_HORIZON_ACTIVATION_WINDOW_INVALID", message) { }
}

internal sealed class LongHorizonMixedWindowAtomicityViolationException : LongHorizonRollingContractException
{
    public LongHorizonMixedWindowAtomicityViolationException(string message) : base("LONG_HORIZON_MIXED_WINDOW_ATOMICITY_VIOLATION", message) { }
}

internal sealed class LongHorizonNumericWeekInvalidException : LongHorizonRollingContractException
{
    public LongHorizonNumericWeekInvalidException(string message) : base("LONG_HORIZON_NUMERIC_WEEK_INVALID", message) { }
}

internal sealed class LongHorizonCheckpointDecisionInvalidException : LongHorizonRollingContractException
{
    public LongHorizonCheckpointDecisionInvalidException(string message) : base("LONG_HORIZON_CHECKPOINT_DECISION_INVALID", message) { }
}

internal sealed class LongHorizonEvidenceAuthorityDefaultingException : LongHorizonRollingContractException
{
    public LongHorizonEvidenceAuthorityDefaultingException(string message) : base("LONG_HORIZON_EVIDENCE_AUTHORITY_SILENT_DEFAULTING_REJECTED", message) { }
}

internal sealed class LongHorizonJitContextInvalidException : LongHorizonRollingContractException
{
    public LongHorizonJitContextInvalidException(string message) : base("LONG_HORIZON_JIT_CONTEXT_INVALID", message) { }
}

internal sealed class LongHorizonLockedTargetImmutabilityViolationException : LongHorizonRollingContractException
{
    public LongHorizonLockedTargetImmutabilityViolationException(string message) : base("LONG_HORIZON_LOCKED_TARGET_IMMUTABILITY_VIOLATION", message) { }
}
