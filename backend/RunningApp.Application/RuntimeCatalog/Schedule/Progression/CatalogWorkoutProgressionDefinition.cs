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

public sealed class CatalogPhaseWorkoutProgression
{
    public required string PhaseKey { get; init; }
    public required IReadOnlyList<CatalogWorkoutProgressionStage> Stages { get; init; }
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
