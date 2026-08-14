namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2F Part 2 -- the authoritative operations this failure-injection
/// seam can target. Every value here maps to a real repository method that
/// commits via exactly one EF SaveChangesAsync call (confirmed by direct
/// inspection: no Long-Horizon persistence method calls SaveChangesAsync
/// more than once, and none use an explicit BeginTransaction -- EF's own
/// Unit-of-Work pattern already wraps every staged entity change into one
/// atomic transaction at that single call).
/// </summary>
internal enum LongHorizonPersistenceOperation
{
    InitialPersistence,
    MixedRunwayCoreActivation,
    CoreOnlyActivation,
    FutureCoreRefresh,
    BlockPersistence,
    RetryPersistence,
}

/// <summary>
/// Phase 4L.2F Part 2 -- injection stages. Because every operation above
/// commits via one SaveChangesAsync call, every stage before
/// <see cref="BeforeCommit"/> shares the exact same underlying atomicity
/// guarantee (nothing is sent to PostgreSQL until that one call) -- each
/// stage value marks a distinct point in the C# staging code (immediately
/// after the corresponding entity is Add()'ed/updated), not a distinct
/// database round-trip. This is documented explicitly, not hidden.
/// </summary>
internal enum LongHorizonPersistenceFailpoint
{
    AfterVersionValidation,
    AfterContextInsert,
    AfterActivationWindowInsert,
    AfterWeekUpdates,
    AfterSessionInserts,
    BeforeCommit,
    AfterCommitBeforeAcknowledgement,
}

/// <summary>
/// Phase 4L.2F Part 3 -- test infrastructure only. Never mapped to public
/// API output; production code must never catch and convert this into a
/// normal business outcome inside the transaction it interrupts.
/// </summary>
internal sealed class LongHorizonInjectedPersistenceFailureException(
    LongHorizonPersistenceOperation operation, LongHorizonPersistenceFailpoint stage, Guid? planId = null)
    : Exception($"Injected test failure: {operation} @ {stage}" + (planId is { } id ? $" (plan {id})" : ""))
{
    public LongHorizonPersistenceOperation Operation { get; } = operation;
    public LongHorizonPersistenceFailpoint Stage { get; } = stage;
    public Guid? PlanId { get; } = planId;
}

/// <summary>
/// Phase 4L.2F Part 2 -- the shared failure-injection seam. Inert by
/// default (see <see cref="NoOpLongHorizonPersistenceFailureInjector"/>),
/// internal only, never remotely configurable, never randomized.
/// </summary>
internal interface ILongHorizonPersistenceFailureInjector
{
    void MaybeThrow(LongHorizonPersistenceOperation operation, LongHorizonPersistenceFailpoint stage, Guid? planId = null);
}

/// <summary>The production default: never throws, zero behavioral effect.</summary>
internal sealed class NoOpLongHorizonPersistenceFailureInjector : ILongHorizonPersistenceFailureInjector
{
    public static readonly NoOpLongHorizonPersistenceFailureInjector Instance = new();
    public void MaybeThrow(LongHorizonPersistenceOperation operation, LongHorizonPersistenceFailpoint stage, Guid? planId = null) { }
}

/// <summary>
/// Phase 4L.2F -- test-only configurable injector. Throws exactly once for
/// the exact configured (Operation, Stage) pair, then disarms itself (so a
/// disabled/replayed operation proceeds normally without reconfiguring a
/// fresh instance). Never used by any production DI registration.
/// </summary>
internal sealed class LongHorizonTestPersistenceFailureInjector : ILongHorizonPersistenceFailureInjector
{
    private readonly LongHorizonPersistenceOperation _armedOperation;
    private readonly LongHorizonPersistenceFailpoint _armedStage;
    private bool _fired;

    public LongHorizonTestPersistenceFailureInjector(LongHorizonPersistenceOperation operation, LongHorizonPersistenceFailpoint stage)
    {
        _armedOperation = operation;
        _armedStage = stage;
    }

    public void MaybeThrow(LongHorizonPersistenceOperation operation, LongHorizonPersistenceFailpoint stage, Guid? planId = null)
    {
        if (_fired || operation != _armedOperation || stage != _armedStage) return;
        _fired = true;
        throw new LongHorizonInjectedPersistenceFailureException(operation, stage, planId);
    }
}
