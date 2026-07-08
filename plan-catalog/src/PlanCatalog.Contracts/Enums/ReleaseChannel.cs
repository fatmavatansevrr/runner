namespace PlanCatalog.Contracts.Enums;

/// <summary>
/// Distribution channel for a published release — distinct from <c>CatalogStatus</c> (authoring
/// lifecycle) and deliberately not folded into it. Consumers use this to decide whether a release is
/// safe to resolve in production.
/// </summary>
public enum ReleaseChannel
{
    Pilot,
    Draft,
    Production
}
