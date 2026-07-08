namespace PlanCatalog.Core.Ports;

/// <summary>Default ledger used when no retirement state is supplied — nothing is considered retired.</summary>
public sealed class NullRetirementLedger : IRetirementLedger
{
    public static readonly NullRetirementLedger Instance = new();

    public bool IsRetired(string documentType, string key, int version) => false;
}
