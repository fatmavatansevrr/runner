namespace RunningApp.Application.RuntimeCatalog.Schedule.Progression;

/// <summary>Backend Integration Phase 4F.6A version tag for <see cref="GeneratedCatalogStageSchedule"/>, independent of every other phase's own schema version (same pattern as <see cref="Materialization.GeneratedCatalogPlanSkeleton.CurrentSchemaVersion"/>/<see cref="Materialization.CatalogCalendarDayMaterializerVersion"/>).</summary>
public static class ProgressionStageAllocatorVersion
{
    public const string V1 = "PROGRESSION_STAGE_ALLOCATOR_V1";
}

/// <summary>
/// Backend Integration Phase 4F.6A — eligibility classification for a requested progression
/// stage, evaluated against already-resolved runtime-condition results (never re-evaluated
/// here; see <see cref="ProgressionStageAllocator"/>'s own doc comment).
/// </summary>
public enum ProgressionStageEligibilityOutcome
{
    /// <summary>Stage has no <c>Requires</c> entries — always eligible, no condition to check.</summary>
    NotConditioned,

    /// <summary>Every required condition's resolved result matched an allowed value.</summary>
    Eligible,

    /// <summary>At least one required condition was not satisfied, and a valid fallback stage was resolved.</summary>
    IneligibleWithFallback,

    /// <summary>At least one required condition was not satisfied, and no fallback stage is configured.</summary>
    IneligibleWithoutFallback,
}

/// <summary>Which allocation step assigned a given exposure unit — trace-only, distinguishes the minimum-guarantee pass from the extension pass.</summary>
public enum ProgressionStageAllocationKind
{
    MinimumExposure,
    ExtensionExposure,
}

/// <summary>Whether a phase's effective stage set required a compression reduction below combined minimum exposures to fit available weeks.</summary>
public enum ProgressionPhaseCompressionAction
{
    NotRequired,
    Reduced,
}

/// <summary>
/// Backend Integration Phase 4F.6A — the internal, dark scheduler's top-level output: one
/// fine-grained progression-stage assignment per eligible plan week, for the
/// progression-controlled KEY_SESSION only. Never exposed on any public DTO, never
/// persisted, never hashed into <see cref="PreviewRouting.CatalogPreviewSnapshot"/>. Carries
/// no workout identity (see <see cref="CatalogWorkoutProgressionDefinition"/>'s own doc
/// comment) — binding EASY_SUPPORT/LONG_RUN/KEY_SESSION workout definitions remains Phase
/// 4F.6B's responsibility (D-C06/D-C09, not reopened here).
/// </summary>
public sealed class GeneratedCatalogStageSchedule
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required string ProgressionArtifactKey { get; init; }
    public required int ProgressionArtifactVersion { get; init; }
    public required string AllocatorVersion { get; init; }

    public required IReadOnlyList<ScheduledProgressionWeek> Weeks { get; init; }

    public required StageAllocationDecisionTrace Trace { get; init; }
}

/// <summary>
/// One plan-relative week's progression-stage assignment. <see cref="StructuralRole"/> is
/// always the literal string "KEY_SESSION" (D-C06: WorkoutProgressionStageDefinition governs
/// KEY_SESSION progression intent only — not reopened here, and not derived from anything
/// else on this type).
/// </summary>
public sealed class ScheduledProgressionWeek
{
    public required int WeekNumber { get; init; }
    public required string PhaseKey { get; init; }

    /// <summary>The EFFECTIVE fine-grained stage assigned to this week (post-fallback-resolution if applicable) — never the phase-granularity key. See PHASE4F_6_STEP_C1_TAPER_SHARPEN_AND_RUNTIME_BOUNDARY_CLOSURE.md section 5 for why this field must exist at all.</summary>
    public required string ProgressionStageKey { get; init; }

    /// <summary>Non-null only when a fallback was applied for this week's stage-eligibility resolution; the originally-requested (ineligible) stage key. Null when <see cref="ProgressionStageKey"/> was directly eligible or unconditioned.</summary>
    public string? RequestedProgressionStageKey { get; init; }

    public required int StageRelativeOrder { get; init; }

    public const string KeySessionStructuralRole = "KEY_SESSION";
    public string StructuralRole => KeySessionStructuralRole;

    /// <summary>
    /// Backend Integration Phase 10K-FREQ.6D.4D Split A — the catalog-authored lane this
    /// week's stage allocation belongs to (see <see cref="Progression.CatalogWorkoutProgressionLane.LaneOrdinal"/>).
    /// Defaults to 0 so every existing single-lane allocator invocation/test fixture
    /// continues to compile and behave unchanged. Together with <see cref="WeekNumber"/> this
    /// is the canonical, unique key for one lane's stage assignment in one week — never
    /// <see cref="WeekNumber"/> alone (see <see cref="CatalogWorkoutBinder"/>'s own doc
    /// comment for why a WeekNumber-only key was the pre-Split-A defect).
    /// </summary>
    public int LaneOrdinal { get; init; }

    public required ProgressionStageEligibilityOutcome ConditionOutcome { get; init; }

    /// <summary>Non-null only when this week's effective stage was reached via a fallback chain — same value as <see cref="RequestedProgressionStageKey"/>, kept as a distinct named field to mirror the task's own required-field list literally.</summary>
    public string? FallbackOrigin { get; init; }

    /// <summary>Short machine-readable reason for this week's assignment, e.g. "MINIMUM_EXPOSURE_ALLOCATION", "EXTENSION_ALLOCATION", "COMPRESSION_REDUCED_ALLOCATION".</summary>
    public required string AllocationReason { get; init; }
}

/// <summary>
/// Backend Integration Phase 4F.6A — the full, explainable decision trace for one
/// scheduling run. Deterministic and testable (Section 15) — never exposed publicly (no
/// existing internal trace wrapper covers stage-scheduling content, so this stays a
/// dedicated internal type per the task's own instruction).
/// </summary>
public sealed class StageAllocationDecisionTrace
{
    public required IReadOnlyList<StageAllocationDecisionTraceStep> Steps { get; init; }
}

public sealed class StageAllocationDecisionTraceStep
{
    public required string PhaseKey { get; init; }
    public required int WeekNumber { get; init; }

    /// <summary>Backend Integration Phase 10K-FREQ.6D.4D Split A — see <see cref="ScheduledProgressionWeek.LaneOrdinal"/>. Defaults to 0.</summary>
    public int LaneOrdinal { get; init; }
    public required string RequestedStageKey { get; init; }
    public required string EffectiveStageKey { get; init; }
    public required int RelativeOrder { get; init; }
    public required ProgressionStageAllocationKind AllocationKind { get; init; }
    public required ProgressionPhaseCompressionAction CompressionAction { get; init; }
    public required ProgressionStageEligibilityOutcome ConditionOutcome { get; init; }
    public string? FallbackOrigin { get; init; }
    public required string TieBreakUsed { get; init; }
    public required string SourceArtifactKey { get; init; }
    public required int SourceArtifactVersion { get; init; }
}
