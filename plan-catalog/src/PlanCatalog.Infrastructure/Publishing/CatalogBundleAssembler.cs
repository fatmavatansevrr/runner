using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Core.Metadata;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Infrastructure.Publishing;

/// <summary>
/// Resolves the full dependency closure for a combination and pins every dependency by exact
/// version and content hash — see brief §8.4. Must run against an already-hashed (stamped) snapshot.
/// </summary>
public sealed class CatalogBundleAssembler(ICanonicalJsonSerializer serializer, IContentHasher hasher) : ICatalogBundleAssembler
{
    public PublishedTemplateBundle Assemble(CatalogSourceSnapshot snapshot, string combinationKey, int combinationVersion, IRetirementLedger? retirementLedger = null)
    {
        var retirement = retirementLedger ?? NullRetirementLedger.Instance;

        var combination = snapshot.Combinations.FirstOrDefault(c => c.Metadata.Key == combinationKey && c.Metadata.Version == combinationVersion)
            ?? throw new InvalidOperationException($"Combination '{combinationKey}' v{combinationVersion} was not found.");

        if (retirement.IsRetired(combination.Metadata.DocumentType, combination.Metadata.Key, combination.Metadata.Version))
        {
            throw new InvalidOperationException(
                $"RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE: Combination '{combinationKey}' v{combinationVersion} is RETIRED and cannot be assembled into a new bundle. " +
                "It remains available for historical release verification only.");
        }

        var master = snapshot.FindPlanTemplate(combination.MasterTemplate)
            ?? throw new InvalidOperationException($"MasterTemplate for combination '{combinationKey}' could not be resolved.");
        var layout = snapshot.FindRunLayout(combination.Layout)
            ?? throw new InvalidOperationException($"Layout for combination '{combinationKey}' could not be resolved.");
        var levelModifier = snapshot.FindLevelModifier(combination.LevelModifier)
            ?? throw new InvalidOperationException($"LevelModifier for combination '{combinationKey}' could not be resolved.");
        var rulePack = snapshot.FindRulePack(combination.RulePack)
            ?? throw new InvalidOperationException($"RulePack for combination '{combinationKey}' could not be resolved.");
        var progression = snapshot.FindWorkoutProgression(master.WorkoutProgression)
            ?? throw new InvalidOperationException($"WorkoutProgression for combination '{combinationKey}' could not be resolved.");
        var progressionModifier = snapshot.FindProgressionModifier(levelModifier.ProgressionModifier)
            ?? throw new InvalidOperationException($"ProgressionModifier for combination '{combinationKey}' could not be resolved.");
        var registry = snapshot.FindRuntimeConditionValueRegistry(rulePack.RuntimeConditionValueRegistry)
            ?? throw new InvalidOperationException($"RuntimeConditionValueRegistry for combination '{combinationKey}' could not be resolved.");
        var peakPolicy = snapshot.FindPeakVolumeBandPolicy(rulePack.PeakVolumeBandPolicy)
            ?? throw new InvalidOperationException($"PeakVolumeBandPolicy for combination '{combinationKey}' could not be resolved.");

        var dependencyMetadata = new[]
        {
            master.Metadata, layout.Metadata, levelModifier.Metadata, rulePack.Metadata,
            progression.Metadata, progressionModifier.Metadata, registry.Metadata, peakPolicy.Metadata
        };

        var retiredDependency = dependencyMetadata.FirstOrDefault(m => retirement.IsRetired(m.DocumentType, m.Key, m.Version));
        if (retiredDependency is not null)
        {
            throw new InvalidOperationException(
                $"Cannot assemble a new bundle for '{combinationKey}': dependency '{retiredDependency.DocumentType}/{retiredDependency.Key}' v{retiredDependency.Version} is RETIRED.");
        }

        // Milestone B/E of artifacts/audits/deterministic-graph-part2-migration.md: schemaVersion 1
        // (legacy) documents keep their exact original behavior (intersection, auto-selected highest
        // non-retired version, via CatalogSourceSnapshot.FindWorkout(key)) — untouched, so historical
        // combinations v1-v3 keep resolving byte-identically. schemaVersion >= 2 (candidate) documents use
        // an exact, self-contained UNION closure (progression candidates ∪ level-modifier eligible
        // workouts) resolved only through exact key+version lookups — never FindWorkout(key).
        var progressionIsExact = progression.PhaseProgressions.SelectMany(p => p.Stages).Any(s => s.WorkoutCandidates is not null);
        var levelModifierIsExact = levelModifier.EligibleWorkouts is not null;

        if (progressionIsExact != levelModifierIsExact)
        {
            throw new InvalidOperationException(
                $"Combination '{combinationKey}': WorkoutProgression '{progression.Metadata.Key}' v{progression.Metadata.Version} and LevelModifier '{levelModifier.Metadata.Key}' v{levelModifier.Metadata.Version} use inconsistent workout-reference shapes (one exact, one legacy).");
        }

        List<CatalogDocumentMetadata> effectiveWorkouts;
        if (progressionIsExact)
        {
            var unionRefs = WorkoutClosureResolver.ComputeExactClosureRefs(progression, levelModifier);

            effectiveWorkouts = unionRefs
                .Select(r => snapshot.FindWorkout(r.Key, r.Version)
                    ?? throw new InvalidOperationException(
                        $"WORKOUT_REFERENCE_VERSION_NOT_FOUND: '{r.Key}' v{r.Version} referenced by combination '{combinationKey}' was not found in the source catalog."))
                .Select(w => w.Metadata)
                .ToList();
        }
        else
        {
            var candidateKeys = progression.PhaseProgressions
                .SelectMany(p => p.Stages)
                .SelectMany(s => s.WorkoutCandidateKeys ?? Enumerable.Empty<string>())
                .Distinct();

            effectiveWorkouts = candidateKeys
                .Where(k => levelModifier.EligibleWorkoutKeys is not null && levelModifier.EligibleWorkoutKeys.Contains(k))
                .Select(key => snapshot.FindWorkout(key, retirement))
                .Where(w => w is not null)
                .Select(w => w!.Metadata)
                .OrderBy(m => m.Key, StringComparer.Ordinal)
                .ToList();
        }

        var bundle = new PublishedTemplateBundle
        {
            BundleKey = combination.Metadata.Key,
            BundleVersion = combination.Metadata.Version,
            Combination = ToRef(combination.Metadata),
            MasterTemplate = ToRef(master.Metadata),
            Layout = ToRef(layout.Metadata),
            LevelModifier = ToRef(levelModifier.Metadata),
            WorkoutProgression = ToRef(progression.Metadata),
            ProgressionModifier = ToRef(progressionModifier.Metadata),
            RulePack = ToRef(rulePack.Metadata),
            RuntimeConditionValueRegistry = ToRef(registry.Metadata),
            PeakVolumeBandPolicy = ToRef(peakPolicy.Metadata),
            Workouts = effectiveWorkouts.Select(ToRef).ToList(),
            BundleContentHash = string.Empty
        };

        var hash = Hashing.CatalogDocumentHasher.ComputeHashExcludingField(serializer, hasher, bundle, "bundleContentHash");
        return bundle with { BundleContentHash = hash };
    }

    private static CatalogArtifactReference ToRef(CatalogDocumentMetadata metadata) => CatalogArtifactReferences.ToRef(metadata);
}
