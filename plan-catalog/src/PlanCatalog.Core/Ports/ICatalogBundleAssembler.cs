using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Core.Catalog;

namespace PlanCatalog.Core.Ports;

public interface ICatalogBundleAssembler
{
    /// <summary>
    /// Resolves the full dependency closure for a combination key/version and pins exact versions/hashes.
    /// A retired dependency (per <paramref name="retirementLedger"/>) always fails assembly of a NEW bundle.
    /// </summary>
    PublishedTemplateBundle Assemble(CatalogSourceSnapshot snapshot, string combinationKey, int combinationVersion, IRetirementLedger? retirementLedger = null);
}
