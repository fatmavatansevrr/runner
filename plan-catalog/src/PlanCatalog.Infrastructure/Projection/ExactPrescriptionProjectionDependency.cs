using PlanCatalog.Contracts.References;

namespace PlanCatalog.Infrastructure.Projection;

/// <summary>An exact, already-selected profile dependency; it contains no selection criteria.</summary>
public sealed record ExactPrescriptionProjectionDependency
{
    public required VersionedCatalogReference Profile { get; init; }
}
