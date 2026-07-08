namespace PlanCatalog.Contracts.Enums;

/// <summary>
/// Describes HOW a workout's headline dosage is prescribed. Distinct from <see cref="DistanceAccountingMode"/>,
/// which describes how a session's total distance reconciles with its components — the two vocabularies
/// must never overlap.
/// </summary>
/// <remarks>
/// <see cref="Distance"/> and <see cref="Mixed"/> are CANONICAL_CONFIRMED — verbatim from Golden Fixture v3
/// (docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.plandocument.json, every
/// <c>workout.prescriptionMode</c> value across all 12 weeks is one of these two).
/// <see cref="PaceBased"/>, <see cref="EffortBased"/>, and <see cref="HeartRateBased"/> remain
/// PLACEHOLDER_UNCONFIRMED legacy values: they do not appear anywhere in Golden Fixture v3 and have no
/// other canonical source, but are retained because <c>GOAL_PACE_TEN_K</c> (the one pilot workout with no
/// Golden Fixture v3 evidence at all) still declares <see cref="PaceBased"/> and removing it would force an
/// invented replacement value onto that workout, which this review does not do.
/// </remarks>
public enum PrescriptionMode
{
    /// <summary>CANONICAL_CONFIRMED — Golden Fixture v3.</summary>
    Distance,

    /// <summary>CANONICAL_CONFIRMED — Golden Fixture v3.</summary>
    Mixed,

    /// <summary>PLACEHOLDER_UNCONFIRMED legacy value — retained only because GOAL_PACE_TEN_K still uses it.</summary>
    PaceBased,

    /// <summary>PLACEHOLDER_UNCONFIRMED legacy value — no longer used by any confirmed pilot workout.</summary>
    EffortBased,

    /// <summary>PLACEHOLDER_UNCONFIRMED legacy value — no longer used by any confirmed pilot workout.</summary>
    HeartRateBased
}
