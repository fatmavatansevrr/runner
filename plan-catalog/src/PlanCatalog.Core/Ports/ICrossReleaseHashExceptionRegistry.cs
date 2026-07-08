namespace PlanCatalog.Core.Ports;

/// <summary>
/// Narrow, explicit registry of known, pre-existing cross-release content-hash mismatches for a given
/// (documentType, key, version) artifact identity — see
/// artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json and
/// artifacts/audits/cross-release-hash-consistency-audit.md. Consulted only by the publish-time
/// cross-release hash guard, so a documented historical defect never blocks a legitimate new publish,
/// while any NEW, unregistered mismatch still fails.
/// </summary>
public interface ICrossReleaseHashExceptionRegistry
{
    bool IsKnownException(string documentType, string key, int version, string releaseVersion, string observedContentHash);
}
