using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Catalog;

/// <summary>
/// Computes the exact, self-contained workout closure for a schemaVersion >= 2 (candidate)
/// progression+levelModifier pair — see Milestone E of
/// artifacts/audits/deterministic-graph-part2-migration.md. UNION, not intersection: every exact
/// WorkoutProgression.WorkoutCandidates reference union every exact LevelModifier.EligibleWorkouts
/// reference. Shared by <see cref="Validation.CandidatePublishGraphValidator"/> (layout coverage checking,
/// before assembly) and <c>CatalogBundleAssembler</c> (actual bundle content, at assembly) so both agree
/// on exactly the same closure.
/// </summary>
public static class WorkoutClosureResolver
{
    /// <summary>True once either side of the pair has adopted the exact (schemaVersion >= 2) shape.</summary>
    public static bool IsExactShape(WorkoutProgressionDefinition progression, LevelModifierDefinition levelModifier) =>
        progression.PhaseProgressions.SelectMany(p => p.Stages).Any(s => s.WorkoutCandidates is not null) ||
        levelModifier.EligibleWorkouts is not null;

    public static IReadOnlyList<VersionedCatalogReference> ComputeExactClosureRefs(
        WorkoutProgressionDefinition progression, LevelModifierDefinition levelModifier)
    {
        var candidateRefs = progression.PhaseProgressions
            .SelectMany(p => p.Stages)
            .SelectMany(s => s.WorkoutCandidates ?? Enumerable.Empty<VersionedCatalogReference>());
        var eligibleRefs = levelModifier.EligibleWorkouts ?? Enumerable.Empty<VersionedCatalogReference>();

        return candidateRefs.Concat(eligibleRefs)
            .Distinct()
            .OrderBy(r => r.Key, StringComparer.Ordinal)
            .ThenBy(r => r.Version)
            .ToList();
    }
}
