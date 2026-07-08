namespace PlanCatalog.Core.Audit;

/// <summary>
/// Classifies the provenance of a single domain-content value (a number, enum-vocabulary choice, or
/// structural decision) independently of <see cref="Enums.CatalogStatus"/>. CatalogStatus tracks
/// authoring/publish lifecycle; this tracks whether the *value itself* is a confirmed product decision.
/// Applied at field/JsonPath granularity — a document is never "canonical" just because one field is.
/// </summary>
public enum ContentDecisionStatus
{
    /// <summary>Value is mandated verbatim by the canonical brief or an explicitly provided source.</summary>
    CanonicalConfirmed,

    /// <summary>Value is a reasonable, explicitly-approved product default, not yet independently sourced.</summary>
    ExplicitProductDefault,

    /// <summary>Value was authored/invented during implementation with no traceable canonical source.</summary>
    PlaceholderUnconfirmed,

    /// <summary>Value is purely structural/mechanical (ordering, identifiers) and carries no domain-content decision.</summary>
    TechnicalOnly
}
