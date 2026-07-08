using PlanCatalog.Core.Catalog;

namespace PlanCatalog.Core.Ports;

/// <summary>Loads the editable authoring source tree (<c>catalog/</c>) into memory. Never touched by the backend.</summary>
public interface ICatalogSourceRepository
{
    CatalogSourceSnapshot LoadSnapshot();
}
