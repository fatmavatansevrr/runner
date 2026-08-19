using System.Text.Json.Serialization;
using PlanCatalog.Contracts.Enums;

namespace PlanCatalog.Core.Models;

public sealed record PhaseWorkoutProgressionDefinition
{
    public required PhaseKey PhaseKey { get; init; }

    /// <summary>
    /// LEGACY_SINGLE_LANE_SHAPE: the bare, lane-less stage list. For a phase that also declares
    /// <see cref="Lanes"/>, this field carries no independent meaning — use
    /// <see cref="EffectiveLanes"/>. Kept required so every existing single-lane progression
    /// document continues to deserialize and validate byte-for-byte unchanged (Phase
    /// 10K-FREQ.6D.4D, additive/degenerate-default legacy boundary — mirrors the backend
    /// RunningApp-side <c>CatalogPhaseWorkoutProgression</c> shape exactly).
    /// </summary>
    public required IReadOnlyList<WorkoutProgressionStageDefinition> Stages { get; init; }

    /// <summary>
    /// Phase 10K-FREQ.6D.4D Split B — catalog-authored lanes for a structural role with more
    /// than one slot per week (e.g. Intermediate×5D's two KEY_SESSION lanes). Null/empty for
    /// every phase not authored with multiple lanes — <see cref="EffectiveLanes"/> normalizes
    /// that case to one implicit lane wrapping <see cref="Stages"/> at LaneOrdinal 0.
    /// </summary>
    public IReadOnlyList<WorkoutProgressionLaneDefinition>? Lanes { get; init; }

    /// <summary>
    /// The single canonical view every validator/resolver must use instead of reading
    /// <see cref="Stages"/>/<see cref="Lanes"/> directly. <c>[JsonIgnore]</c> is load-bearing:
    /// this is a computed, derived view, never a source-of-truth field — including it in
    /// canonical JSON/content-hash serialization would change every existing document's content
    /// hash the moment this property was added, which is exactly the historical-stability
    /// invariant this whole engagement treats as inviolable (see the identical, deliberate
    /// precedent already established for every other computed catalog-model property).
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<WorkoutProgressionLaneDefinition> EffectiveLanes =>
        Lanes is { Count: > 0 } ? Lanes : new[] { new WorkoutProgressionLaneDefinition { LaneOrdinal = 0, Stages = Stages } };
}

/// <summary>
/// Phase 10K-FREQ.6D.4D Split B — a single catalog-authored progression lane within one phase.
/// <see cref="LaneOrdinal"/> is explicit and catalog-authored, never derived. Mirrors the
/// already-implemented backend RunningApp-side <c>CatalogWorkoutProgressionLane</c> shape.
/// </summary>
public sealed record WorkoutProgressionLaneDefinition
{
    public required int LaneOrdinal { get; init; }
    public required IReadOnlyList<WorkoutProgressionStageDefinition> Stages { get; init; }
}
