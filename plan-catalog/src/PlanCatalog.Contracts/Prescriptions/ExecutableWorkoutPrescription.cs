using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;

namespace PlanCatalog.Contracts.Prescriptions;

public sealed record ExecutableWorkoutPrescription
{
    public required int ContractSchemaVersion { get; init; }
    public required CatalogArtifactReference SourceProfile { get; init; }
    public required CatalogArtifactReference SourceWorkout { get; init; }
    public required ExecutablePrescriptionDoseCategory DoseCategory { get; init; }
    public required DistanceAccountingMode DistanceAccountingMode { get; init; }
    public required IReadOnlyList<ExecutablePrescriptionComponent> Components { get; init; }
}

public sealed record ExecutablePrescriptionComponent
{
    public required int SequenceOrder { get; init; }
    public required WorkoutComponentType ComponentType { get; init; }
    public required ExecutablePrescriptionStructureMode StructureMode { get; init; }
    public required ExecutableWorkQuantity Work { get; init; }
    public int? RepetitionCount { get; init; }
    public ExecutableRecovery? Recovery { get; init; }
    public required ExecutableIntensityTarget Intensity { get; init; }
}

public sealed record ExecutableWorkQuantity
{
    public required ExecutableQuantityUnit Unit { get; init; }
    public required int Value { get; init; }
}

public sealed record ExecutableRecovery
{
    public required ExecutableQuantityUnit Unit { get; init; }
    public required int Value { get; init; }
    public required ExecutableRecoveryMode Mode { get; init; }
    public required ExecutableRecoveryPlacement Placement { get; init; }
    public required int RecoveryCount { get; init; }
}

public sealed record ExecutableIntensityTarget
{
    public required ExecutableIntensityMode Mode { get; init; }
    public required string DescriptorKey { get; init; }
}
