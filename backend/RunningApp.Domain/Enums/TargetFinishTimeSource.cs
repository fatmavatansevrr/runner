namespace RunningApp.Domain.Enums;

/// <summary>
/// Distinguishes how a Race request's <c>target_finish_time_seconds</c> was
/// derived — required so the runtime feasibility pipeline never treats a
/// product-computed planning reference as demonstrated athlete capability.
/// See PHASE4D_4_1_PRODUCT_AVERAGE_TARGET_TIME_GOAL_FEASIBILITY_CLASSIFICATION.md.
/// Serializes via the API's global <c>JsonStringEnumConverter(SnakeCaseLower)</c>
/// to "product_average"/"user_defined" — no bespoke converter needed (same
/// pattern as <see cref="Weekday"/>).
/// </summary>
public enum TargetFinishTimeSource
{
    /// <summary>
    /// The value is the canonical product-average finish time for the
    /// selected goal distance (see CanonicalTargetFinishTimePolicy) —
    /// a planning reference, not evidence of the runner's actual pace.
    /// </summary>
    ProductAverage,

    /// <summary>The value was entered directly by the user as their own goal.</summary>
    UserDefined
}
