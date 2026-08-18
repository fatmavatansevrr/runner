using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Core.Models;

public sealed record WorkoutDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required WorkoutFamily Family { get; init; }

    public int? ComplexityTier { get; init; }

    public required IReadOnlyList<PhaseKey> EligiblePhases { get; init; }

    public required IReadOnlyList<PrescriptionMode> AllowedPrescriptionModes { get; init; }

    /// <summary>
    /// Optional/omittable: absent means "not yet source-confirmed for this workout" rather than
    /// asserting a guessed value. See brief-review vocabulary separation of DistanceAccountingMode
    /// from PrescriptionMode (Golden Fixture v3).
    /// </summary>
    public IReadOnlyList<DistanceAccountingMode>? AllowedDistanceAccountingModes { get; init; }

    /// <summary>
    /// Phase 10K-FREQ.6D.4C.5: governs ONLY <see cref="Catalog.CatalogSourceSnapshot.FindWorkout(string, PlanCatalog.Core.Ports.IRetirementLedger?)"/>'s
    /// implicit bare-key candidate set — never exact (key, version) lookup, never combination
    /// activation, never publisher eligibility, never phase eligibility. Nullable (rather than a
    /// non-nullable bool defaulting to true) specifically so historical documents that omit this field
    /// serialize/hash byte-identically to before this field existed — <c>CanonicalJsonOptions</c>
    /// omits null on write, so absence round-trips as absence, not as a materialized "true" that would
    /// perturb every pre-existing content hash. Absent/null means eligible (true) - identical to every
    /// artifact's real behavior before this field existed. Set explicitly <c>false</c> only for a
    /// version that narrowly extends an existing key's eligibility (e.g. adds a phase) without being
    /// intended to become that key's new legacy default — see FREQ.6D.4C.4's architecture decision.
    /// </summary>
    public bool? EligibleForLegacyDefaultResolution { get; init; }

    public IReadOnlyList<WorkoutComponentDefinition>? Components { get; init; }
}
