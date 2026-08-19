using RunningApp.Application.RuntimeCatalog;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Progression;

/// <summary>
/// Backend Integration Phase 4F.6A — a read-only backend-side parse of a Process A
/// WORKOUT_PROGRESSION document's fine-grained stage content. Distinct, on purpose, from
/// <see cref="Materialization.GeneratedCatalogWeekSkeleton.StageKey"/> (which is phase
/// granularity, e.g. "BUILD" — documented terminology debt, see
/// <see cref="Materialization.CatalogStageToWeekContextFactory"/>). This type's
/// <see cref="CatalogWorkoutProgressionStage.ProgressionStageKey"/> is the catalog's
/// finer, workout-selection-level stage identity (e.g. "TAPER_SHARPEN",
/// "GOAL_PACE_REHEARSAL") that no Phase 4F.1-4F.5.1 contract has ever carried — see
/// PHASE4F_6_STEP_C1_TAPER_SHARPEN_AND_RUNTIME_BOUNDARY_CLOSURE.md section 5.
///
/// Carries no workout identity (no <c>WorkoutCandidates</c>/<c>WorkoutCandidateKeys</c>
/// field) — binding a workout definition to this stage remains explicitly out of scope
/// for Phase 4F.6A (see D-C06/D-C09) and is Phase 4F.6B's responsibility.
/// </summary>
public sealed class CatalogWorkoutProgressionDefinition
{
    public required string Key { get; init; }
    public required int Version { get; init; }
    public required string DistanceFamily { get; init; }
    public required IReadOnlyList<CatalogPhaseWorkoutProgression> PhaseProgressions { get; init; }
}

/// <summary>
/// Backend Integration Phase 10K-FREQ.6D.4D Split A — a single catalog-authored progression
/// lane within one phase. <see cref="LaneOrdinal"/> is explicit and catalog-authored, never
/// derived from calendar/date order or dictionary/enumeration order (see
/// PHASE_10K_FREQ_6D_4D_DUAL_KEY_STAGE_PROFILE_PRODUCTION_INTEGRATION_ARCHITECTURE.md §6).
/// Reuses <see cref="CatalogWorkoutProgressionStage"/> verbatim, unmodified — a lane is
/// simply its own independent stage list, allocated via the existing, unmodified
/// <see cref="ProgressionStageAllocator"/> algorithm (invoked once per lane, never once for
/// all lanes combined).
/// </summary>
public sealed class CatalogWorkoutProgressionLane
{
    public required int LaneOrdinal { get; init; }
    public required IReadOnlyList<CatalogWorkoutProgressionStage> Stages { get; init; }
}

public sealed class CatalogPhaseWorkoutProgression
{
    public required string PhaseKey { get; init; }

    /// <summary>
    /// LEGACY_SINGLE_LANE_SHAPE: the bare, lane-less stage list. For a phase that also
    /// declares <see cref="Lanes"/>, this field carries no independent meaning and should be
    /// ignored — use <see cref="EffectiveLanes"/>. Kept required (rather than removed) so
    /// every existing single-lane catalog artifact and every existing test fixture that
    /// constructs this type without a lane concept continues to compile and behave
    /// byte-for-byte unchanged (Phase 10K-FREQ.6D.4D Split A, architecture §28: additive,
    /// degenerate-default legacy boundary).
    /// </summary>
    public required IReadOnlyList<CatalogWorkoutProgressionStage> Stages { get; init; }

    /// <summary>
    /// Backend Integration Phase 10K-FREQ.6D.4D Split A — catalog-authored lanes for a
    /// structural role with more than one slot per week (e.g. Intermediate×5D's two
    /// KEY_SESSION slots). Null/empty for every phase that has not been authored with
    /// multiple lanes — <see cref="EffectiveLanes"/> normalizes that case to a single
    /// implicit lane wrapping <see cref="Stages"/> at LaneOrdinal 0, so 3D/4D/Beginner×4D
    /// progression artifacts require zero change and behave identically to before this field
    /// existed.
    /// </summary>
    public IReadOnlyList<CatalogWorkoutProgressionLane>? Lanes { get; init; }

    /// <summary>
    /// The single canonical view every consumer (allocator, binder, dependency-closure
    /// computation) must use instead of reading <see cref="Stages"/>/<see cref="Lanes"/>
    /// directly — normalizes the legacy single-lane shape to one implicit lane at
    /// LaneOrdinal 0, so callers never need their own "is this lane-aware" branch.
    /// </summary>
    public IReadOnlyList<CatalogWorkoutProgressionLane> EffectiveLanes =>
        Lanes is { Count: > 0 } ? Lanes : new[] { new CatalogWorkoutProgressionLane { LaneOrdinal = 0, Stages = Stages } };
}

/// <summary>
/// One fine-grained progression stage, deliberately excluding workout identity (see this
/// file's own type-level doc comment). Mirrors the field set already established as
/// EXPLICIT_PRODUCT_DEFAULT/CANONICAL_CONFIRMED governance for the V1 pilot (Step C
/// AUD-500/AUD-501/AUD-502/AUD-507/AUD-508) — this type does not reopen or revalue any of
/// those decisions; it only makes them consumable by backend runtime code for the first
/// time.
/// </summary>
public sealed class CatalogWorkoutProgressionStage
{
    public required string ProgressionStageKey { get; init; }
    public required int RelativeOrder { get; init; }
    public required int MinimumExposures { get; init; }
    public required int MaximumExposures { get; init; }
    public required CatalogStageCompressionBehavior CompressionBehavior { get; init; }
    public required CatalogStageExtensionBehavior ExtensionBehavior { get; init; }
    public required IReadOnlyList<CatalogRuntimeEligibilityCondition> Requires { get; init; }
    public string? FallbackStageKey { get; init; }

    /// <summary>
    /// Backend Integration Phase 4F.6B — the stage's explicit, versioned workout-candidate
    /// reference(s) (schemaVersion&gt;=2 <c>workoutCandidates</c> shape only; the current v10
    /// artifact uses exactly this shape). Not populated/used by Phase 4F.6A (deliberately
    /// excluded there — see this type's own class-level doc comment); Phase 4F.6B is the
    /// first consumer. Optional (defaults empty) so every existing Phase 4F.6A test fixture
    /// that constructs a stage without this field continues to compile and behave unchanged.
    /// </summary>
    public IReadOnlyList<PlanCatalogReference> WorkoutCandidateReferences { get; init; } = Array.Empty<PlanCatalogReference>();

    /// <summary>
    /// Backend Integration Phase 10K-FREQ.6D.4D Split B — the stage's explicit, exact
    /// prescription-profile candidate reference(s), sibling to
    /// <see cref="WorkoutCandidateReferences"/>, same exact-pin discipline. Empty (the default)
    /// for every stage authored before this field existed — that stage remains Legacy, never
    /// ProfileBacked (see <see cref="Binding.CatalogWorkoutBinder"/>). Exactly one entry is the
    /// only binder-resolvable shape; more than one is rejected as ambiguous at bind time. Never
    /// filtered by DoseCategory here — that invariant is enforced catalog-side, at publish time,
    /// by PlanCatalog's <c>PrescriptionProfileLaneDoseValidator</c> against the lane this stage
    /// belongs to.
    /// </summary>
    public IReadOnlyList<PlanCatalogReference> PrescriptionProfileCandidateKeys { get; init; } = Array.Empty<PlanCatalogReference>();
}

public sealed class CatalogRuntimeEligibilityCondition
{
    public required string ConditionType { get; init; }
    public required IReadOnlySet<string> AllowedValues { get; init; }
}

/// <summary>Mirrors plan-catalog's <c>StageCompressionBehavior</c> enum (COMPRESSIBLE/PROTECTED) — backend has no project reference onto PlanCatalog.* assemblies (see PlanCatalogBundleLoader's own raw-JSON parsing precedent), so this is a deliberate, independent, string-parsed mirror, not a shared type.</summary>
public enum CatalogStageCompressionBehavior
{
    Compressible,
    Protected,
}

/// <summary>Mirrors plan-catalog's <c>StageExtensionBehavior</c> enum (EXTENDABLE/FIXED_EXPOSURE). See <see cref="CatalogStageCompressionBehavior"/>'s own doc comment for why this is an independent mirror.</summary>
public enum CatalogStageExtensionBehavior
{
    Extendable,
    FixedExposure,
}
