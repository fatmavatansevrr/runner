namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal abstract class CatalogSessionPrescriptionException : InvalidOperationException
{
    public string Code { get; }

    protected CatalogSessionPrescriptionException(string code, string message) : base(message)
    {
        Code = code;
    }
}

internal sealed class CatalogSessionPrescriptionInfeasibleException : CatalogSessionPrescriptionException
{
    public CatalogSessionPrescriptionInfeasibleException(string message) : base("CATALOG_SESSION_PRESCRIPTION_INFEASIBLE", message) { }
}

internal sealed class CatalogSessionPrescriptionInvalidException : CatalogSessionPrescriptionException
{
    public CatalogSessionPrescriptionInvalidException(string message) : base("CATALOG_SESSION_PRESCRIPTION_INVALID", message) { }
}

internal sealed class CatalogGoalPacePrescriptionUnsupportedException : CatalogSessionPrescriptionException
{
    public CatalogGoalPacePrescriptionUnsupportedException(string message) : base("CATALOG_GOAL_PACE_PRESCRIPTION_UNSUPPORTED", message) { }
}

internal sealed class CatalogTaperSharpenSchemaUnsupportedException : CatalogSessionPrescriptionException
{
    public CatalogTaperSharpenSchemaUnsupportedException(string message) : base("CATALOG_TAPER_SHARPEN_SCHEMA_UNSUPPORTED", message) { }
}

internal sealed class CatalogTaperSharpenComponentAmbiguousException : CatalogSessionPrescriptionException
{
    public CatalogTaperSharpenComponentAmbiguousException(string message) : base("CATALOG_TAPER_SHARPEN_COMPONENT_AMBIGUOUS", message) { }
}

internal sealed class CatalogTaperSharpenPrescriptionInfeasibleException : CatalogSessionPrescriptionException
{
    public CatalogTaperSharpenPrescriptionInfeasibleException(string message) : base("CATALOG_TAPER_SHARPEN_PRESCRIPTION_INFEASIBLE", message) { }
}

internal sealed class CatalogTaperSharpenDistanceAccountingException : CatalogSessionPrescriptionException
{
    public CatalogTaperSharpenDistanceAccountingException(string message) : base("CATALOG_TAPER_SHARPEN_DISTANCE_ACCOUNTING", message) { }
}

internal sealed class CatalogFinalPrescribedPlanInvalidException : CatalogSessionPrescriptionException
{
    public CatalogFinalPrescribedPlanInvalidException(string message) : base("CATALOG_FINAL_PRESCRIBED_PLAN_INVALID", message) { }
}

internal sealed class CatalogPendingPrescriptionStateException : CatalogSessionPrescriptionException
{
    public CatalogPendingPrescriptionStateException(string message) : base("CATALOG_PENDING_PRESCRIPTION_STATE", message) { }
}

/// <summary>Backend Integration Phase 10K-FREQ.6D.4D Split C: a bound session carries exactly one of PrescriptionProfileKey/PrescriptionProfileVersion set (partial lineage) — never a valid Legacy (both null) or ProfileBacked (both set) state.</summary>
internal sealed class CatalogSessionPrescriptionInvalidProfileLineageException : CatalogSessionPrescriptionException
{
    public CatalogSessionPrescriptionInvalidProfileLineageException(string message) : base("CATALOG_SESSION_PRESCRIPTION_INVALID_PROFILE_LINEAGE", message) { }
}

/// <summary>Backend Integration Phase 10K-FREQ.6D.4D Split C: a bound session is explicitly ProfileBacked but no execution-prescription index was supplied to resolve it against — never silently degraded to Legacy.</summary>
internal sealed class CatalogSessionPrescriptionMissingExecutionPrescriptionException : CatalogSessionPrescriptionException
{
    public CatalogSessionPrescriptionMissingExecutionPrescriptionException(string message) : base("CATALOG_SESSION_PRESCRIPTION_MISSING_EXECUTION_PRESCRIPTION", message) { }
}

/// <summary>Backend Integration Phase 10K-FREQ.6D.4D Split C: the resolved ExecutableWorkoutPrescription's SourceWorkout does not match the bound session's own WorkoutDefinitionKey/Version — the two independent workout-identity authorities (stage-level WorkoutCandidateReferences vs. the profile's own embedded WorkoutDefinitionRef) have diverged.</summary>
internal sealed class CatalogSessionPrescriptionProfileWorkoutMismatchException : CatalogSessionPrescriptionException
{
    public CatalogSessionPrescriptionProfileWorkoutMismatchException(string message) : base("CATALOG_SESSION_PRESCRIPTION_PROFILE_WORKOUT_MISMATCH", message) { }
}
