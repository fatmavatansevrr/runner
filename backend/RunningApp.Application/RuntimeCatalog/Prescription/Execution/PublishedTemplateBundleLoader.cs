using PlanCatalog.Contracts.Bundles;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Execution;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split E — the single, deterministic runtime authority for locating the
/// published bundle corresponding to an exact combination key/version, closing the gap Split C
/// disclosed (<c>PlanCatalogBundleLoader</c> loads raw source catalog documents only; no
/// published-bundle file-discovery convention was ever wired into the live runtime path).
///
/// Reuses the REAL, already-existing Process A publish/release convention
/// (<c>FileSystemPublishedArtifactRepository</c>, <c>plan-catalog/artifacts/appsel-plan-catalog/
/// {releaseVersion}/bundles/{combinationKey}.v{combinationVersion}.json</c>) rather than inventing
/// an alternate side-channel. The release version is pinned by explicit configuration
/// (<see cref="PlanCatalogOptions.PublishedBundleReleaseVersion"/>) — never discovered by scanning
/// for the latest directory/highest filename version, per the exact-identity discovery
/// requirement. Returns <see langword="null"/> when no bundle file exists for the requested
/// combination (the overwhelmingly common case today — every 3D/4D/Beginner×4D combination has
/// no published bundle) — that is Legacy, not a failure. A combination whose bound plan actually
/// needs profile-backed execution but has no bundle available fails closed downstream, at the
/// existing Split-C <see cref="Session.CatalogSessionPrescriptionMissingExecutionPrescriptionException"/>
/// boundary — this loader itself never fails closed on absence, only on a bundle file that exists
/// but is malformed.
/// </summary>
public interface IPublishedTemplateBundleLoader
{
    /// <summary>
    /// Returns the published bundle for the exact <paramref name="combinationKey"/>/<paramref name="combinationVersion"/>,
    /// or <see langword="null"/> if no <see cref="PlanCatalogOptions.PublishedBundleReleaseVersion"/> is
    /// configured, or no bundle file exists for this exact combination identity in that release.
    /// Never resolves a different version of the same key ("wrong bundle version" — §23 — is simply
    /// treated as "no bundle for this exact identity," per the same exact-pin discipline every other
    /// catalog lookup in this codebase already follows).
    /// </summary>
    Task<PublishedTemplateBundle?> TryLoadAsync(string combinationKey, int combinationVersion, CancellationToken ct = default);
}

/// <inheritdoc cref="IPublishedTemplateBundleLoader"/>
public sealed class PublishedTemplateBundleLoader(PlanCatalogOptions options, string catalogRootPath) : IPublishedTemplateBundleLoader
{
    private const string ReleaseFamily = "appsel-plan-catalog";

    public async Task<PublishedTemplateBundle?> TryLoadAsync(string combinationKey, int combinationVersion, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.PublishedBundleReleaseVersion))
        {
            return null;
        }

        // "catalog" and "artifacts" are always siblings under the same plan-catalog root, both in
        // local/dev repository execution and in published application output — one deployment root
        // model, matching the exact resolution PlanCatalogRootResolver already computed for
        // CatalogRootPath (never a second, independently-resolved path).
        var planCatalogRoot = Directory.GetParent(catalogRootPath)?.FullName
            ?? throw new InvalidOperationException($"Could not resolve the plan-catalog root from catalog path '{catalogRootPath}'.");

        var bundlePath = Path.Combine(
            planCatalogRoot, "artifacts", ReleaseFamily, options.PublishedBundleReleaseVersion,
            "bundles", $"{combinationKey}.v{combinationVersion}.json");

        if (!File.Exists(bundlePath))
        {
            return null;
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(bundlePath, ct);
        }
        catch (IOException ex)
        {
            throw new PublishedTemplateBundleDiscoveryException(
                $"Published bundle file '{bundlePath}' exists but could not be read: {ex.Message}");
        }

        try
        {
            return PublishedTemplateBundleJsonReader.Read(json);
        }
        catch (Exception ex) when (ex is not PublishedTemplateBundleDiscoveryException)
        {
            throw new PublishedTemplateBundleDiscoveryException(
                $"Published bundle file '{bundlePath}' exists but is malformed: {ex.Message}");
        }
    }
}

/// <summary>Fail-closed: a published bundle file exists at its exact, deterministic location but could not be read or deserialized. Never silently skipped, never treated as "no bundle."</summary>
public sealed class PublishedTemplateBundleDiscoveryException(string message) : Exception(message);
