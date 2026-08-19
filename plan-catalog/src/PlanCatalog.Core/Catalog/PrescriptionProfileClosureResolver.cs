using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Catalog;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split B — computes the exact, distinct set of prescription-profile
/// references reachable across a workout progression's phases/lanes. Mirrors
/// <see cref="WorkoutClosureResolver"/>'s own union-and-dedupe pattern exactly (Milestone E
/// precedent), scoped to <see cref="WorkoutProgressionStageDefinition.PrescriptionProfileCandidates"/>
/// instead of <c>WorkoutCandidates</c>. Per FREQ.6D.4D §13: never scans all profiles or selects
/// by family/dose/key-only/latest/lane — this resolver only ever returns the exact references a
/// progression's own stages actually declare, deduplicated by exact (Key, Version) identity, in
/// deterministic (Key, Version) order so the resulting dependency list never depends on
/// dictionary/enumeration/lane-declaration order.
/// </summary>
public static class PrescriptionProfileClosureResolver
{
    public static IReadOnlyList<VersionedCatalogReference> ComputeExactClosureRefs(WorkoutProgressionDefinition progression) =>
        progression.PhaseProgressions
            .SelectMany(p => p.EffectiveLanes)
            .SelectMany(l => l.Stages)
            .SelectMany(s => s.PrescriptionProfileCandidates ?? Enumerable.Empty<VersionedCatalogReference>())
            .Distinct()
            .OrderBy(r => r.Key, StringComparer.Ordinal)
            .ThenBy(r => r.Version)
            .ToList();
}
