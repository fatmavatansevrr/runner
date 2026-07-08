using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Metadata;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Infrastructure.Publishing;

/// <summary>Stamps every document in a snapshot with its computed ContentHash and CatalogStatus.Published.</summary>
public static class CatalogStamper
{
    public static CatalogSourceSnapshot StampAsPublished(ICanonicalJsonSerializer serializer, IContentHasher hasher, CatalogSourceSnapshot snapshot)
    {
        CatalogDocumentMetadata Stamp<T>(T document, Func<T, CatalogDocumentMetadata> getMetadata) =>
            getMetadata(document) with { Status = CatalogStatus.Published, ContentHash = Hashing.CatalogDocumentHasher.ComputeContentHash(serializer, hasher, document) };

        return new CatalogSourceSnapshot
        {
            PlanTemplates = snapshot.PlanTemplates.Select(x => x with { Metadata = Stamp(x, d => d.Metadata) }).ToList(),
            RunLayouts = snapshot.RunLayouts.Select(x => x with { Metadata = Stamp(x, d => d.Metadata) }).ToList(),
            LevelModifiers = snapshot.LevelModifiers.Select(x => x with { Metadata = Stamp(x, d => d.Metadata) }).ToList(),
            WorkoutProgressions = snapshot.WorkoutProgressions.Select(x => x with { Metadata = Stamp(x, d => d.Metadata) }).ToList(),
            ProgressionModifiers = snapshot.ProgressionModifiers.Select(x => x with { Metadata = Stamp(x, d => d.Metadata) }).ToList(),
            Workouts = snapshot.Workouts.Select(x => x with { Metadata = Stamp(x, d => d.Metadata) }).ToList(),
            RuntimeConditionValueRegistries = snapshot.RuntimeConditionValueRegistries.Select(x => x with { Metadata = Stamp(x, d => d.Metadata) }).ToList(),
            PeakVolumeBandPolicies = snapshot.PeakVolumeBandPolicies.Select(x => x with { Metadata = Stamp(x, d => d.Metadata) }).ToList(),
            RulePacks = snapshot.RulePacks.Select(x => x with { Metadata = Stamp(x, d => d.Metadata) }).ToList(),
            Combinations = snapshot.Combinations.Select(x => x with { Metadata = Stamp(x, d => d.Metadata) }).ToList()
        };
    }
}
