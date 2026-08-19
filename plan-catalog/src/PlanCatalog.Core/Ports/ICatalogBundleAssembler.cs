using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;

namespace PlanCatalog.Core.Ports;

public interface ICatalogBundleAssembler
{
    /// <summary>
    /// Resolves the full dependency closure for a combination key/version and pins exact versions/hashes.
    /// A retired dependency (per <paramref name="retirementLedger"/>) always fails assembly of a NEW bundle.
    /// </summary>
    PublishedTemplateBundle Assemble(CatalogSourceSnapshot snapshot, string combinationKey, int combinationVersion, IRetirementLedger? retirementLedger = null);

    /// <summary>
    /// Phase 10K-FREQ.6D.4D Split E — projection-capability seam, exposed at the port level (Core cannot
    /// reference the Infrastructure-only <c>ExactPrescriptionProjectionDependency</c> type, so this overload
    /// takes the already-exact, already-resolved profile references directly). Callers supply exactly the
    /// distinct profile identities a combination's own progression declares — never a search, never "all
    /// profiles." An empty list is equivalent to the legacy overload (both produce a bundle whose
    /// <c>ExecutionPrescriptions</c> is null).
    /// </summary>
    PublishedTemplateBundle Assemble(CatalogSourceSnapshot snapshot, string combinationKey, int combinationVersion, IReadOnlyList<VersionedCatalogReference> exactPrescriptionProfileRefs, IRetirementLedger? retirementLedger = null);
}
