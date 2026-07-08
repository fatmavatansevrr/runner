namespace PlanCatalog.Core.Ports;

/// <summary>
/// Read-only view of the retirement ledger. RETIRED artifacts may never enter a newly assembled
/// bundle, but historical bundles/releases that already reference them must continue to verify.
/// </summary>
public interface IRetirementLedger
{
    bool IsRetired(string documentType, string key, int version);
}
