using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Metadata;
using PlanCatalog.Contracts.References;

namespace PlanCatalog.Core.Models;

public sealed record PlanTemplateDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required DistanceFamily DistanceFamily { get; init; }
    public required CoreCycleDefinition CoreCycle { get; init; }
    public required IReadOnlyList<int> SupportedRunsPerWeek { get; init; }
    public required IReadOnlyList<PhaseDefinition> Phases { get; init; }

    /// <summary>Distance-specific, phase-relative progression artifact — see brief §7.4.</summary>
    public required VersionedCatalogReference WorkoutProgression { get; init; }

    /// <summary>
    /// Legacy (schemaVersion 1) exact-version RulePack selection. Null for schemaVersion >= 2. Superseded
    /// because it competed with <see cref="TemplateCombinationDefinition.RulePack"/> as a second, exact,
    /// uncoordinated RulePack-version owner (see artifacts/audits/rule-pack-ownership-audit.md).
    /// </summary>
    public IReadOnlyList<VersionedCatalogReference>? RequiredRules { get; init; }

    /// <summary>
    /// Semantic (schemaVersion >= 2) RulePack key requirement — "the combination-selected RulePack key
    /// must satisfy this master requirement." Never selects an exact RulePack version; the combination's
    /// own <see cref="TemplateCombinationDefinition.RulePack"/> remains the sole exact selection. Null for
    /// schemaVersion 1.
    /// </summary>
    public IReadOnlyList<string>? RequiredRuleKeys { get; init; }
}
