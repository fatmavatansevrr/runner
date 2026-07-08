using PlanCatalog.Core.Metadata;
using PlanCatalog.Contracts.References;

namespace PlanCatalog.Core.Models;

/// <summary>
/// Root compatibility reference only — never carries workout progression, progression modifier,
/// peak-volume bands, workout lists, or week mapping directly. Those are resolved by dependency
/// closure at bundle-assembly time. See brief §7.11.
/// </summary>
public sealed record TemplateCombinationDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required VersionedCatalogReference MasterTemplate { get; init; }
    public required VersionedCatalogReference Layout { get; init; }
    public required VersionedCatalogReference LevelModifier { get; init; }
    public required VersionedCatalogReference RulePack { get; init; }
}
