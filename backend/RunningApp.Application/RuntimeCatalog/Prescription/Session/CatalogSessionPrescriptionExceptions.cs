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
