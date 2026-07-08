namespace PlanCatalog.Contracts.Enums;

/// <summary>
/// Describes how a session's total planned distance reconciles with its structural components (e.g. a
/// simple run's total IS the session distance; a repeats-based session's total is estimated/capped
/// around embedded repeat+recovery blocks). Distinct from <see cref="PrescriptionMode"/>, which describes
/// how the workout is prescribed — the two vocabularies must never overlap.
/// </summary>
/// <remarks>
/// CANONICAL_CONFIRMED — all three values are verbatim from Golden Fixture v3
/// (docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.plandocument.json
/// <c>workout.distanceAccountingMode</c>, observed across all 12 weeks).
/// </remarks>
public enum DistanceAccountingMode
{
    ExactSessionTotal,
    EstimatedSessionTotal,
    EmbeddedComponents
}
