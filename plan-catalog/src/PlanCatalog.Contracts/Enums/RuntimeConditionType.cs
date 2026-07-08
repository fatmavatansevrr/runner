namespace PlanCatalog.Contracts.Enums;

/// <summary>
/// Runtime condition vocabulary — Process A only defines these types and their allowed values
/// (via the RuntimeConditionValueRegistry). Process A never evaluates them; see brief §7.5/§7.6.
/// </summary>
public enum RuntimeConditionType
{
    GoalFeasibilityIn,
    PlanModeIn,
    PaceSourceIn,
    TimeAdequacyIn,
    CoreEntryReadinessIn
}
