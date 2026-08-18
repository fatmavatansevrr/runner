using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Core.Models;

/// <summary>
/// Phase 10K-FREQ.6D.4C.2 — narrow, additive-only exact-version capability metadata for an immutable
/// historical <see cref="WorkoutDefinition"/> that never declared a typed capability the newer
/// <see cref="WorkoutPrescriptionProfile"/> validator requires. Approved architecture: FREQ.6D.4C.1
/// (M3). Never mutates the referenced WorkoutDefinition; never overrides an explicitly-declared value
/// on it (see <see cref="Validation.WorkoutCapabilityResolver"/>); keyed on the exact
/// (WorkoutDefinitionRef.Key, WorkoutDefinitionRef.Version) pair only — no key-only, latest, highest,
/// family, phase, profile, or DoseCategory-based resolution exists anywhere in this type or its
/// consumers.
/// </summary>
public sealed record WorkoutDefinitionCapabilityOverlay
{
    public required CatalogDocumentMetadata Metadata { get; init; }
    public required VersionedCatalogReference WorkoutDefinitionRef { get; init; }
    public required IReadOnlyList<DistanceAccountingMode> AllowedDistanceAccountingModes { get; init; }
}
