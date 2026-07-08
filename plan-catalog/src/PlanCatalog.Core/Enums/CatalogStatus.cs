namespace PlanCatalog.Core.Enums;

/// <summary>
/// Authoring lifecycle status of a catalog artifact. Not part of the Process A → Process B published
/// boundary — published references (VersionedCatalogReference/CatalogArtifactReference) never carry it.
/// Deliberately closed to four values — see brief §19.
/// </summary>
public enum CatalogStatus
{
    Draft,
    Validated,
    Published,
    Retired
}
