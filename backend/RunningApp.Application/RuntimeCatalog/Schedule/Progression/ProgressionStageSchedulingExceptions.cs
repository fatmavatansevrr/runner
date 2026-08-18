namespace RunningApp.Application.RuntimeCatalog.Schedule.Progression;

/// <summary>
/// Backend Integration Phase 4F.6A — typed failures for the fine-grained progression-stage
/// scheduler. Each maps a distinct rejection listed in the phase's own specification
/// (Sections 7, 9, 10, 11, 12); none is caught/swallowed by the allocator itself — the dark
/// wiring caller (<see cref="PreviewRouting.CatalogPreviewGenerator"/>) maps all of them onto
/// the existing <see cref="Exceptions.PlanPreviewGenerationFailedException"/> taxonomy,
/// exactly as it already does for the Phase 4F.3/4F.5/4F.5.1 exception sets.
/// </summary>
public abstract class ProgressionStageSchedulingException : Exception
{
    protected ProgressionStageSchedulingException(string message) : base(message) { }
    protected ProgressionStageSchedulingException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>A stage's <c>Requires</c> names a condition type not among the four known runtime-condition types this pipeline resolves.</summary>
public sealed class ProgressionStageUnknownConditionKeyException : ProgressionStageSchedulingException
{
    public ProgressionStageUnknownConditionKeyException(string message) : base(message) { }
}

/// <summary>A stage's <c>Requires</c> names a recognized condition type, but no matching result exists in the already-resolved runtime-condition results supplied to the scheduler.</summary>
public sealed class ProgressionStageConditionResultMissingException : ProgressionStageSchedulingException
{
    public ProgressionStageConditionResultMissingException(string message) : base(message) { }
}

/// <summary>A stage is ineligible (its required condition(s) not satisfied) and has no configured fallback stage.</summary>
public sealed class ProgressionStageIneligibleWithoutFallbackException : ProgressionStageSchedulingException
{
    public ProgressionStageIneligibleWithoutFallbackException(string message) : base(message) { }
}

/// <summary>A stage's <c>FallbackStageKey</c> does not match any stage within the same phase's progression.</summary>
public sealed class ProgressionStageFallbackTargetNotFoundException : ProgressionStageSchedulingException
{
    public ProgressionStageFallbackTargetNotFoundException(string message) : base(message) { }
}

/// <summary>Following a stage's fallback chain revisits a stage already seen in the same chain (self-fallback counts as a 1-cycle).</summary>
public sealed class ProgressionStageFallbackCycleException : ProgressionStageSchedulingException
{
    public ProgressionStageFallbackCycleException(string message) : base(message) { }
}

/// <summary>A stage's <c>FallbackStageKey</c> matches more than one stage within the same phase (only reachable if duplicate-stage-key structural validation was bypassed — defensive).</summary>
public sealed class ProgressionStageAmbiguousFallbackException : ProgressionStageSchedulingException
{
    public ProgressionStageAmbiguousFallbackException(string message) : base(message) { }
}

/// <summary>Two stages within a phase share the same <c>RelativeOrder</c>.</summary>
public sealed class ProgressionStageDuplicateRelativeOrderException : ProgressionStageSchedulingException
{
    public ProgressionStageDuplicateRelativeOrderException(string message) : base(message) { }
}

/// <summary>Two stages within a phase share the same <c>ProgressionStageKey</c>, or a stage key is missing/blank.</summary>
public sealed class ProgressionStageDuplicateOrMissingKeyException : ProgressionStageSchedulingException
{
    public ProgressionStageDuplicateOrMissingKeyException(string message) : base(message) { }
}

/// <summary>The workout-progression document references a phase key not present in the resolved phase/week skeleton, or a skeleton phase has no matching progression entry.</summary>
public sealed class ProgressionStagePhaseMismatchException : ProgressionStageSchedulingException
{
    public ProgressionStagePhaseMismatchException(string message) : base(message) { }
}

/// <summary>Backend Integration Phase 10K-FREQ.6D.4D Split A: two lanes within one phase's progression declare the same LaneOrdinal.</summary>
public sealed class ProgressionStageDuplicateLaneOrdinalException : ProgressionStageSchedulingException
{
    public ProgressionStageDuplicateLaneOrdinalException(string message) : base(message) { }
}

/// <summary>
/// A phase's effective stages' combined minimum exposures exceed the phase's available
/// weeks, and the currently-accepted V1 compression behavior/values cannot close the gap
/// without reducing a Protected stage or a Compressible stage below its allowed compressed
/// floor (Section 10).
/// </summary>
public sealed class ProgressionPhaseCapacityInsufficientException : ProgressionStageSchedulingException
{
    public ProgressionPhaseCapacityInsufficientException(string message) : base(message) { }
}

/// <summary>
/// A phase has more available weeks than its effective stages' combined maximum exposures
/// can absorb, even after deprioritized FixedExposure stages' own headroom is included
/// (Section 11).
/// </summary>
public sealed class ProgressionPhaseCapacityExceedsMaximumException : ProgressionStageSchedulingException
{
    public ProgressionPhaseCapacityExceedsMaximumException(string message) : base(message) { }
}

/// <summary>
/// More than one requested stage resolved to the same effective fallback stage, and the
/// safest deterministic merge (max of minimums, min of maximums — Section 12) produced an
/// impossible range (merged minimum &gt; merged maximum).
/// </summary>
public sealed class ProgressionStageFallbackBoundsUnreconcilableException : ProgressionStageSchedulingException
{
    public ProgressionStageFallbackBoundsUnreconcilableException(string message) : base(message) { }
}

/// <summary>The generated <see cref="GeneratedCatalogStageSchedule"/> failed <see cref="IGeneratedCatalogStageScheduleValidator"/>'s own output validation (Section 14) — distinct from every allocation-time rejection above, which all fail before a schedule is ever produced.</summary>
public sealed class ProgressionStageScheduleInvalidException : ProgressionStageSchedulingException
{
    public ProgressionStageScheduleInvalidException(string message) : base(message) { }
}
