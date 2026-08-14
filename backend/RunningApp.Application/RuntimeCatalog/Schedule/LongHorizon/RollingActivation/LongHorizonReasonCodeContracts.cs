namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>Phase 4K.3's nine approved checkpoint reason codes, verbatim.</summary>
internal enum LongHorizonCheckpointReasonCode
{
    CheckpointWindowNotComplete,
    CheckpointEvidenceStale,
    ValidatedLoadUnavailable,
    ValidatedLongRunEvidenceUnavailable,
    AdherenceConfidenceInsufficientForGrowth,
    MaintenanceAnchorUnavailable,
    NumericWindowInfeasible,
    SafetyReassessmentRequired,
    EvidenceConflictUnresolved,
}

/// <summary>
/// Phase 4K.4's ten approved JIT-specific reason codes. Deliberately does
/// NOT include a safety value -- SAFETY_REASSESSMENT_REQUIRED is reused
/// directly from <see cref="LongHorizonCheckpointReasonCode"/> via
/// <see cref="LongHorizonReasonCode.SafetyReassessmentRequired"/> (Phase
/// 4K.4 §21/Phase 4K.5 Part 11), never duplicated as a second enum value.
/// </summary>
internal enum LongHorizonJitReasonCode
{
    RunwayJitContextUnavailable,
    CoreJitContextUnavailable,
    JitValidatedLoadUnavailable,
    JitValidatedLongRunUnavailable,
    JitPaceSourceUnresolved,
    JitGoalFeasibilityUnresolved,
    JitAvailabilityInfeasible,
    JitEvidenceConflictUnresolved,
    JitActivationBoundaryMissed,
    JitSegmentTransitionInfeasible,
}

/// <summary>Which typed enum a <see cref="LongHorizonReasonCode"/> wraps.</summary>
internal enum LongHorizonReasonCodeCategory
{
    Checkpoint,
    Jit,
}

/// <summary>
/// Phase 4K.5 — the "shared top-level reason abstraction" Part 11 asks for:
/// a single typed value that can hold either a checkpoint reason or a JIT
/// reason, so SAFETY_REASSESSMENT_REQUIRED can be reused by JIT-blocked
/// results without a duplicate enum value, and without collapsing either
/// taxonomy into untyped strings. Two <see cref="LongHorizonReasonCode"/>
/// instances are equal (via record-struct equality) iff they wrap the same
/// category and the same underlying enum value.
/// </summary>
internal readonly record struct LongHorizonReasonCode
{
    public LongHorizonReasonCodeCategory Category { get; }
    public LongHorizonCheckpointReasonCode? CheckpointReason { get; }
    public LongHorizonJitReasonCode? JitReason { get; }

    private LongHorizonReasonCode(LongHorizonCheckpointReasonCode checkpointReason)
    {
        Category = LongHorizonReasonCodeCategory.Checkpoint;
        CheckpointReason = checkpointReason;
        JitReason = null;
    }

    private LongHorizonReasonCode(LongHorizonJitReasonCode jitReason)
    {
        Category = LongHorizonReasonCodeCategory.Jit;
        CheckpointReason = null;
        JitReason = jitReason;
    }

    public static LongHorizonReasonCode FromCheckpoint(LongHorizonCheckpointReasonCode reason) => new(reason);

    public static LongHorizonReasonCode FromJit(LongHorizonJitReasonCode reason) => new(reason);

    /// <summary>The single shared instance every safety-blocked checkpoint OR JIT result must reuse (Phase 4K.4 §21).</summary>
    public static LongHorizonReasonCode SafetyReassessmentRequired { get; } =
        FromCheckpoint(LongHorizonCheckpointReasonCode.SafetyReassessmentRequired);

    public string Code => Category switch
    {
        LongHorizonReasonCodeCategory.Checkpoint => CheckpointReason!.Value.ToString(),
        LongHorizonReasonCodeCategory.Jit => JitReason!.Value.ToString(),
        _ => throw new InvalidOperationException("Unreachable reason-code category."),
    };

    public override string ToString() => Code;
}
