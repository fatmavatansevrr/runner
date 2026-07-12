namespace RunningApp.Application.RuntimeCatalog;

/// <summary>
/// How well the current backend domain/DTO model (as of Backend Integration
/// Phase 2) can represent a given Process A plan-catalog vocabulary concept.
/// Purely descriptive — computing this classification never mutates
/// anything and never generates a schedule.
/// </summary>
public enum BackendRepresentationSupport
{
    /// <summary>An existing backend enum/field already has an exact, unambiguous match.</summary>
    NativeSupported,

    /// <summary>An existing free-form string field could hold the value as-is, but with no typed/validated representation.</summary>
    RepresentableAsStringOnly,

    /// <summary>No existing field carries this concept; a new additive field (and/or new small enum) is needed.</summary>
    RequiresNewField,

    /// <summary>An existing enum is the right category of thing, but needs a new member added to represent this exact value.</summary>
    RequiresEnumExtension,

    /// <summary>The concept has no structural home at all today (no field, no enum, no table) — needs a new table or a JSON metadata column.</summary>
    RequiresNewTableOrJson,

    /// <summary>No existing or planned-minimal representation covers this; the concept is absent from backend, including any loose/lossy proxy.</summary>
    NotSupported,

    /// <summary>Repository evidence was insufficient to classify this concept with confidence.</summary>
    UnknownFromCodeEvidence,
}

/// <summary>One classified catalog vocabulary concept, with the evidence-based reasoning behind its classification.</summary>
public sealed record CatalogConceptClassification(string Concept, BackendRepresentationSupport Support, string Reasoning);
