using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Core.Models;

/// <summary>
/// Versioned executable dose for exactly one WorkoutDefinition. WorkoutDefinition remains the
/// physiological identity and component-skeleton authority; this profile is the sole executable
/// quantity, recovery and intensity authority.
/// </summary>
public sealed record WorkoutPrescriptionProfile
{
    public required CatalogDocumentMetadata Metadata { get; init; }
    public required VersionedCatalogReference WorkoutDefinitionRef { get; init; }
    public required PrescriptionDoseCategory DoseCategory { get; init; }
    public required DistanceAccountingMode DistanceAccountingMode { get; init; }
    public required IReadOnlyList<PrescriptionProfileComponent> Components { get; init; }
}

public sealed record PrescriptionProfileComponent
{
    public required int SequenceOrder { get; init; }
    public required WorkoutComponentType ComponentType { get; init; }
    public required PrescriptionStructureMode StructureMode { get; init; }
    public PrescriptionWorkQuantity? WorkQuantity { get; init; }
    public PrescriptionRecoveryQuantity? RecoveryQuantity { get; init; }
    public required PrescriptionIntensityTarget IntensityTarget { get; init; }
}

public sealed record PrescriptionWorkQuantity
{
    public int? DurationSeconds { get; init; }
    public int? DistanceMeters { get; init; }
    public int? RepetitionCount { get; init; }
}

public sealed record PrescriptionRecoveryQuantity
{
    public int? DurationSeconds { get; init; }
    public int? DistanceMeters { get; init; }
    public required PrescriptionRecoveryMode Mode { get; init; }
}

public sealed record PrescriptionIntensityTarget
{
    public required PrescriptionIntensityMode Mode { get; init; }
    public string? PaceDescriptorKey { get; init; }
    public string? EffortDescriptorKey { get; init; }
    public string? HeartRateZoneKey { get; init; }
}
