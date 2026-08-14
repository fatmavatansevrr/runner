namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

internal abstract class CatalogVolumePlanningException : InvalidOperationException
{
    public string Code { get; }

    protected CatalogVolumePlanningException(string code, string message) : base(message)
    {
        Code = code;
    }
}

internal sealed class CatalogVolumeRuleInconsistentException : CatalogVolumePlanningException
{
    public CatalogVolumeRuleInconsistentException(string message) : base("CATALOG_VOLUME_RULE_INCONSISTENT", message) { }
}

internal sealed class CatalogVolumeUnsupportedCycleLengthException : CatalogVolumePlanningException
{
    public CatalogVolumeUnsupportedCycleLengthException(int weeks)
        : base("CATALOG_VOLUME_UNSUPPORTED_CYCLE_LENGTH", $"Cycle length '{weeks}' is not supported by the current catalog numeric volume planner.") { }
}

internal sealed class CatalogVolumePlanInvalidException : CatalogVolumePlanningException
{
    public CatalogVolumePlanInvalidException(string message) : base("CATALOG_VOLUME_PLAN_INVALID", message) { }
}

internal sealed class CatalogVolumeInvalidReadinessInputException : CatalogVolumePlanningException
{
    public CatalogVolumeInvalidReadinessInputException(string message) : base("CATALOG_VOLUME_INVALID_READINESS_INPUT", message) { }
}

internal sealed class CatalogVolumeCanonicalRuleSourceMissingException : CatalogVolumePlanningException
{
    public CatalogVolumeCanonicalRuleSourceMissingException(string message) : base("CATALOG_VOLUME_CANONICAL_RULE_SOURCE_MISSING", message) { }
}

internal sealed class CatalogVolumeUnreachablePeakRuleException : CatalogVolumePlanningException
{
    public CatalogVolumeUnreachablePeakRuleException(string message) : base("CATALOG_VOLUME_UNREACHABLE_PEAK_RULE", message) { }
}

internal sealed class CatalogVolumeInvalidTaperRuleException : CatalogVolumePlanningException
{
    public CatalogVolumeInvalidTaperRuleException(string message) : base("CATALOG_VOLUME_INVALID_TAPER_RULE", message) { }
}

internal sealed class CatalogLongRunHardCapViolationException : CatalogVolumePlanningException
{
    public CatalogLongRunHardCapViolationException(string message) : base("CATALOG_LONG_RUN_HARD_CAP_VIOLATION", message) { }
}

internal sealed class CatalogVolumeInvalidGovernanceConfigurationException : CatalogVolumePlanningException
{
    public CatalogVolumeInvalidGovernanceConfigurationException(string message) : base("CATALOG_VOLUME_INVALID_GOVERNANCE_CONFIGURATION", message) { }
}

/// <summary>
/// Common base for every Level/Frequency-specific "projected taper volume
/// below the candidate's minimum full-layout volume" product-ineligibility
/// exception. <see cref="RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogPreviewGenerator"/>
/// catches this base type (not each concrete subtype) to translate to the
/// public <c>PlanProductIneligibleException</c> (HTTP 422) — every future
/// candidate cell's ineligibility exception is picked up automatically by
/// deriving from this type, with no corresponding catch-arm edit required.
/// </summary>
internal abstract class CatalogProductIneligibleException : CatalogVolumePlanningException
{
    protected CatalogProductIneligibleException(string code, string message) : base(code, message) { }
}

internal sealed class ThreeDayCoreProductIneligibleException : CatalogProductIneligibleException
{
    public const string Reason = "THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT";
    public ThreeDayCoreProductIneligibleException(double projectedTaperKm)
        : base(Reason, $"PRODUCT_INELIGIBLE: projected 3D taper volume {projectedTaperKm:0.##}km is below the 12km minimum full-layout volume.") { }
}

internal sealed class BeginnerFourDayCoreProductIneligibleException : CatalogProductIneligibleException
{
    public const string Reason = "BEGINNER_FOUR_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT";
    public BeginnerFourDayCoreProductIneligibleException(double projectedTaperKm)
        : base(Reason, $"PRODUCT_INELIGIBLE: projected Beginner 4D taper volume {projectedTaperKm:0.##}km is below the 9km minimum full-layout volume.") { }
}
