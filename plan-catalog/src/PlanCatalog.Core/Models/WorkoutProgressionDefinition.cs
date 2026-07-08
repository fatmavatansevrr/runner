using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Core.Models;

public sealed record WorkoutProgressionDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }
    public required DistanceFamily DistanceFamily { get; init; }

    public required IReadOnlyList<PhaseWorkoutProgressionDefinition> PhaseProgressions { get; init; }
}
