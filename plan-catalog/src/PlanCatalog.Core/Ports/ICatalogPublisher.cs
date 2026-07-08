using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.Manifests;

namespace PlanCatalog.Core.Ports;

public interface ICatalogPublisher
{
    /// <summary>
    /// Runs the full DRAFT→VALIDATED→PUBLISHED atomic publish workflow for a release version.
    /// Production channel never accepts PLACEHOLDER_UNCONFIRMED domain content, regardless of
    /// <paramref name="allowUnconfirmedContent"/>; a non-production channel requires the flag explicitly.
    /// </summary>
    CatalogReleaseManifest Publish(string releaseVersion, ReleaseChannel channel, bool allowUnconfirmedContent = false);
}
