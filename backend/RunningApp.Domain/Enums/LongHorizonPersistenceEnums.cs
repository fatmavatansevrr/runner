namespace RunningApp.Domain.Enums;

/// <summary>
/// Phase 4L.2 -- durable persistence enums for the dark Long-Horizon rolling
/// plan subsystem. Kept separate from the internal Application-layer
/// LongHorizon enums (which are `internal` and live in a higher layer
/// Domain must not depend on) -- persistence adapters translate between the
/// two explicitly.
/// </summary>
public enum LongHorizonPersistedSegmentType
{
    GeneralEndurance,
    PreparationRunway,
    Core,
}

public enum LongHorizonPersistedLifecycleState
{
    StructurallyPlanned,
    NumericPending,
    NumericActivated,
    NumericActivationBlocked,
    Completed,
    Missed,
}

public enum LongHorizonPersistedActivationOutcome
{
    Activated,
    Blocked,
}

public enum LongHorizonPersistedCheckpointDecision
{
    GrowthEligible,
    MaintenanceOnly,
    Blocked,
}

public enum LongHorizonPersistedBlockRetryEventType
{
    Block,
    RetryRequested,
    RetryRestored,
}

public enum LongHorizonPersistedCoreContextStatus
{
    Active,
    Superseded,
}

/// <summary>
/// Phase 4M.2 -- planning-status dimension for a rolling session, separate
/// from <see cref="LongHorizonRollingSessionOutcomeStatus"/> (Planned/
/// Completed/NotToday). A Superseded session was removed from the active
/// plan by adaptation, not missed by the user -- Superseded != NotToday.
/// Named/shaped after the existing <see cref="LongHorizonPersistedCoreContextStatus"/>
/// Active/Superseded convention already established in this file.
/// </summary>
public enum LongHorizonPersistedSessionPlanningStatus
{
    Active,
    Superseded,
}

/// <summary>
/// Phase 4M.2 -- persisted counterpart of the Phase 4M.1 Application-layer
/// (internal) <c>ScheduleRepairAction</c> enum. Domain must not depend on
/// Application-layer types (see LongHorizonActivationWindowRecord's own
/// note on this same boundary), so this is a deliberate, intentionally
/// parallel persistence-layer enum -- the persistence service maps between
/// the two explicitly, it never leaks the Application-layer enum into Domain.
/// </summary>
public enum LongHorizonPersistedAdaptationDecisionType
{
    Skip,
    RescheduleToEmptySlot,
    SubstituteFutureEasy,
}
