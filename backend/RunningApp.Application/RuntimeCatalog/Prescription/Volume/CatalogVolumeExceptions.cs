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

/// <summary>
/// Phase 10K-GEN.23 -- implements GEN.21's frozen Option-1 authority: a
/// distinct, LOWER (8.5km) taper-specific floor for Beginner×3D, separate
/// from the unchanged 12.0km Intermediate×3D/normal-week floor
/// (<see cref="ThreeDayCoreProductIneligibleException"/>, still used
/// unmodified for Intermediate). See <see cref="V1BeginnerThreeDayVolumeEligibilityPolicy"/>.
/// </summary>
internal sealed class BeginnerThreeDayCoreProductIneligibleException : CatalogProductIneligibleException
{
    public const string Reason = "BEGINNER_THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT";
    public BeginnerThreeDayCoreProductIneligibleException(double projectedTaperKm)
        : base(Reason, $"PRODUCT_INELIGIBLE: projected Beginner 3D taper volume {projectedTaperKm:0.##}km is below the 8.5km minimum full-layout taper volume.") { }
}

/// <summary>
/// Phase 10K-GEN.9 -- GEN.8's frozen Advanced readiness authority: both
/// missing and explicit-zero recent weekly volume are PRODUCT_INELIGIBLE for
/// every Advanced frequency/horizon (a direct, non-inventive extension of the
/// already-approved zero-readiness rule to the identical "no positive
/// evidence" case) -- no starting-volume default is ever resolved for
/// Advanced, unlike Beginner/Intermediate.
/// </summary>
internal sealed class AdvancedMissingOrZeroReadinessProductIneligibleException : CatalogProductIneligibleException
{
    public const string Reason = "ADVANCED_MISSING_OR_ZERO_READINESS_NOT_ELIGIBLE";
    public AdvancedMissingOrZeroReadinessProductIneligibleException()
        : base(Reason, "PRODUCT_INELIGIBLE: Advanced requires positive observed recent weekly volume evidence; missing and explicit-zero readiness are not eligible for this product (GEN.8).") { }
}

/// <summary>
/// Phase 10K-GEN.12 -- GEN.11's frozen 2D readiness authority: both missing
/// and explicit-zero recent weekly volume are PRODUCT_INELIGIBLE for 2D at
/// BOTH Beginner and Intermediate (frequency-owned, not Level-specific) --
/// no starting-volume default is ever resolved for 2D, matching Advanced's
/// own pattern rather than the other Beginner/Intermediate policies'
/// defaults, because 2D concentrates weekly volume into only two, larger
/// sessions and no genuine zero-running 2D race-plan start was found in
/// GEN.11's evidence review.
/// </summary>
internal sealed class TwoDayMissingOrZeroReadinessProductIneligibleException : CatalogProductIneligibleException
{
    public const string Reason = "TWO_DAY_MISSING_OR_ZERO_READINESS_NOT_ELIGIBLE";
    public TwoDayMissingOrZeroReadinessProductIneligibleException()
        : base(Reason, "PRODUCT_INELIGIBLE: 2D requires positive observed recent weekly volume evidence; missing and explicit-zero readiness are not eligible for this product (GEN.11).") { }
}
