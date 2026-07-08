namespace PlanCatalog.Core.Audit;

/// <summary>One audited domain-content decision for a single field/section of a single catalog artifact.</summary>
public sealed record DomainContentDecision
{
    /// <summary>Stable identifier (e.g. "AUD-014"), preserved across reconciliation passes.</summary>
    public required string EntryId { get; init; }

    /// <summary>Reconciliation grouping: vocabulary, workout-progression, workout-definitions, progression-modifier, runtime-condition-registry, peak-volume-policy, phase-metadata, layout-metadata, technical-metadata.</summary>
    public required string Group { get; init; }

    public required string DocumentType { get; init; }
    public required string Key { get; init; }
    public required int Version { get; init; }
    public required string JsonPath { get; init; }
    public required string CurrentValue { get; init; }
    public required ContentDecisionStatus Classification { get; init; }
    public required string SourceFile { get; init; }
    public required string SourceSectionOrReason { get; init; }
    public required bool IsBlocking { get; init; }
    public string? RequiredDecision { get; init; }
    public required IReadOnlyList<string> AffectedValidators { get; init; }
    public required IReadOnlyList<string> AffectedBundlesOrReleases { get; init; }
    public required bool ProductionPublishAllowed { get; init; }
}
