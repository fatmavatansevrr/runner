using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Core.Models;

/// <summary>
/// Versioned, published catalog artifact — the single source of truth for allowed
/// RuntimeEligibilityCondition values. Canonical key: RUNTIME_CONDITION_VALUES_V1. See brief §7.6.
/// </summary>
public sealed record RuntimeConditionValueRegistryDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required IReadOnlyList<RuntimeConditionValueSet> ConditionValueSets { get; init; }
}
