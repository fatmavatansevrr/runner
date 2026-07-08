using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Core.Models;

public sealed record WorkoutDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required WorkoutFamily Family { get; init; }
    public required int ComplexityTier { get; init; }

    public required IReadOnlyList<PhaseKey> EligiblePhases { get; init; }

    public required IReadOnlyList<PrescriptionMode> AllowedPrescriptionModes { get; init; }

    /// <summary>
    /// Optional/omittable: absent means "not yet source-confirmed for this workout" rather than
    /// asserting a guessed value. See brief-review vocabulary separation of DistanceAccountingMode
    /// from PrescriptionMode (Golden Fixture v3).
    /// </summary>
    public IReadOnlyList<DistanceAccountingMode>? AllowedDistanceAccountingModes { get; init; }

    public required IReadOnlyList<WorkoutComponentDefinition> Components { get; init; }
}
