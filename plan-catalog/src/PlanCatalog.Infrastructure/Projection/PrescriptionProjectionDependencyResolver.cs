using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Infrastructure.Projection;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split B — the narrow, catalog-authoring-time glue disclosed as an
/// unknown (not yet named) by the FREQ.6D.4D architecture (§34): assembles the
/// <see cref="ExactPrescriptionProjectionDependency"/> list a real combination's own
/// <c>WorkoutProgressionDefinition</c> requires, for <see cref="Publishing.ICatalogBundleAssembler"/>'s
/// exact-dependency overload. Not a new projection route — the projector/assembler themselves
/// (FREQ.6D.3C) are reused verbatim; this only supplies their input for a profile-backed
/// progression, via <see cref="PrescriptionProfileClosureResolver"/>'s already-deterministic,
/// already-deduplicated closure.
/// </summary>
public static class PrescriptionProjectionDependencyResolver
{
    public static IReadOnlyList<ExactPrescriptionProjectionDependency> ResolveForProgression(WorkoutProgressionDefinition progression) =>
        PrescriptionProfileClosureResolver.ComputeExactClosureRefs(progression)
            .Select(r => new ExactPrescriptionProjectionDependency { Profile = r })
            .ToList();
}
