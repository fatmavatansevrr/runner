using RunningApp.Application.RuntimeCatalog.Schedule.Progression;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Binding;

/// <summary>Backend Integration Phase 4F.6B version tag for <see cref="BoundCatalogPlan"/>, independent of every other phase's own schema version.</summary>
internal static class CatalogWorkoutBinderVersion
{
    public const string V1 = "CATALOG_WORKOUT_BINDER_V1";
}

/// <summary>
/// Backend Integration Phase 4F.6B — the internal, dark binder's top-level output: every
/// dated structural run slot bound to one exact, versioned workout definition. Assigns
/// workout IDENTITY only — no prescription field (pace/distance/duration/volume/
/// repetitions/recovery/segments/public workout type) exists anywhere on this type or its
/// children. Never exposed on any public DTO, never persisted, never hashed.
/// </summary>
internal sealed class BoundCatalogPlan
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required string BinderVersion { get; init; }
    public required IReadOnlyList<BoundCatalogWeek> Weeks { get; init; }
    public required WorkoutBindingDecisionTrace Trace { get; init; }
}

internal sealed class BoundCatalogWeek
{
    public required int WeekNumber { get; init; }
    public required string PhaseKey { get; init; }
    public required IReadOnlyList<BoundCatalogSession> Sessions { get; init; }
}

/// <summary>One structural run slot, now bound to an exact workout definition.</summary>
internal sealed class BoundCatalogSession
{
    public required int WeekNumber { get; init; }
    public required DateOnly Date { get; init; }
    public required string PhaseKey { get; init; }

    /// <summary>Null for FixedDefault (EASY_SUPPORT/LONG_RUN) roles — only StageControlled (KEY_SESSION) sessions carry the fine-grained progression stage that produced them.</summary>
    public string? ProgressionStageKey { get; init; }

    /// <summary>
    /// Backend Integration Phase 10K-FREQ.6D.4D Split A — the catalog-authored lane identity
    /// this session was bound from (see Progression.CatalogWorkoutProgressionLane.LaneOrdinal),
    /// bound to the structural ordinal of this session's own slot among same-role slots in its
    /// week (see CatalogWorkoutBinder). Null for FixedDefault roles and for any StageControlled
    /// role with only one structural slot per week is still populated (always 0) — null is
    /// reserved for roles this binder does not resolve a progression stage for at all, mirroring
    /// <see cref="ProgressionStageKey"/>'s own null convention exactly. This is the single,
    /// canonical source of "which KEY lane" identity — CatalogSessionPrescriptionPlanner must
    /// read this field rather than recompute its own ordinal (Split-A closes exactly that
    /// pre-existing divergence risk).
    /// </summary>
    public int? LaneOrdinal { get; init; }

    public required string StructuralRole { get; init; }
    public required string WorkoutDefinitionKey { get; init; }
    public required int WorkoutDefinitionVersion { get; init; }
    public required CatalogWorkoutBindingMode BindingMode { get; init; }
    public required string BindingPolicyKey { get; init; }
    public required int BindingPolicyVersion { get; init; }
    public required string SourceArtifactKey { get; init; }
    public required int SourceArtifactVersion { get; init; }
    public required ProgressionStageEligibilityOutcome? ConditionOutcome { get; init; }
    public string? FallbackOrigin { get; init; }
    public required string BindingReason { get; init; }
}

internal sealed class WorkoutBindingDecisionTrace
{
    public required IReadOnlyList<WorkoutBindingDecisionTraceStep> Steps { get; init; }
}

internal sealed class WorkoutBindingDecisionTraceStep
{
    public required int WeekNumber { get; init; }
    public required DateOnly Date { get; init; }
    public required string StructuralRole { get; init; }
    public required string PhaseKey { get; init; }
    public string? ProgressionStageKey { get; init; }
    public string? RequestedStageKey { get; init; }
    public string? EffectiveStageKey { get; init; }
    public required CatalogWorkoutBindingMode BindingMode { get; init; }
    public required string ConfiguredDefaultOrStageCandidate { get; init; }
    public required string ResolvedWorkoutKey { get; init; }
    public required int ResolvedWorkoutVersion { get; init; }
    public ProgressionStageEligibilityOutcome? ConditionOutcome { get; init; }
    public string? FallbackOrigin { get; init; }
    public required string PolicyKey { get; init; }
    public required int PolicyVersion { get; init; }
    public required string SourceArtifactKey { get; init; }
    public required int SourceArtifactVersion { get; init; }
    public required string ValidationResult { get; init; }
}
