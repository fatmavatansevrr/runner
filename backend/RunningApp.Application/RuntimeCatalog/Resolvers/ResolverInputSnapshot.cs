using RunningApp.Domain.Enums;
using System.Text.Json.Serialization;

namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4C — contract-only input snapshot for a future
/// runtime-condition resolver implementation. Carries every piece of evidence
/// a resolver might need, gathered from existing request/preview data
/// (Phase 4B's fitness-evidence fields, the existing GeneratePreviewRequest
/// fields, and Phase 1's distance-family resolution) plus catalog identity.
/// All fields are nullable/optional: a snapshot may be built from a request
/// that omitted some or all optional evidence, and no resolver has been
/// implemented yet to require any of them.
///
/// This is a pure data carrier. Building one does not call any resolver,
/// does not read plan-catalog, and does not persist anything.
/// </summary>
public sealed class ResolverInputSnapshot
{
    // ── Distance / goal identity ─────────────────────────────────────────
    /// <summary>The user's exact requested target distance in km, if resolved (Phase 1 CanonicalDistanceFamilyResolver output).</summary>
    public double? RequestedTargetDistanceKm { get; init; }

    /// <summary>The internal catalog distance family the request was resolved to (e.g. "TEN_K"), if resolved.</summary>
    public string? CanonicalDistanceFamily { get; init; }

    public GoalType? GoalType { get; init; }
    public GoalDistance? GoalDistance { get; init; }

    /// <summary>Fixed family-representative distance (PlanServices.GetGoalDistanceInKm) — NOT the same as RequestedTargetDistanceKm.</summary>
    public double? GoalDistanceKm { get; init; }

    // ── Schedule identity ─────────────────────────────────────────────────
    public DateOnly? StartDate { get; init; }
    public DateOnly? RaceDate { get; init; }
    public string? RaceName { get; init; }
    public int? TargetFinishTimeSeconds { get; init; }

    /// <summary>
    /// How <see cref="TargetFinishTimeSeconds"/> was derived — required for
    /// <see cref="RunningApp.Application.RuntimeCatalog.Resolvers.GoalFeasibilityResolver"/>
    /// to distinguish a product-computed planning reference from a user's
    /// own performance claim. Null for Habit snapshots (no target time at
    /// all) and for any snapshot built before this field existed. See
    /// PHASE4D_4_1_PRODUCT_AVERAGE_TARGET_TIME_GOAL_FEASIBILITY_CLASSIFICATION.md.
    /// </summary>
    public TargetFinishTimeSource? TargetFinishTimeSource { get; init; }

    public int? DaysPerWeek { get; init; }
    public IReadOnlyList<Weekday>? PreferredDays { get; init; }
    public Weekday? LongRunDay { get; init; }
    public int? WeeklyAvailability { get; init; }
    public double? PreferredPace { get; init; }
    /// <summary>
    /// Running Background V2.1 — deliberately uses the historical-compat
    /// <see cref="RunningBackgroundJsonConverter"/> (NOT
    /// <c>RunningBackgroundCanonicalJsonConverter</c>, which owns the public
    /// request boundary) so an already-persisted preview snapshot written
    /// before the V2 migration — confirmed present in the real local dev
    /// database (166 of 221 stored <c>PlanPreviews</c> rows carry
    /// "running_regularly" in this exact field) — remains deserializable.
    /// This type is never part of a public request/response contract, so
    /// accepting legacy aliases here does not reopen the public HTTP
    /// boundary that Running Background V2.1 closed. See
    /// <see cref="RunningApp.Application.DTOs.Plan.GeneratePreviewRequest.Level"/>
    /// for why a property-level attribute is required here rather than
    /// relying on the type-level one alone.
    /// </summary>
    [JsonConverter(typeof(RunningBackgroundJsonConverter))]
    public RunningBackground? Level { get; init; }

    // ── Phase 4B fitness-evidence fields (verbatim from GeneratePreviewRequest) ──
    public double? RecentLongestRunKm { get; init; }
    public double? RecentWeeklyVolumeKm { get; init; }
    public int? RecentRunsPerWeek { get; init; }
    public double? RecentRaceDistanceKm { get; init; }
    public int? RecentRaceFinishTimeSeconds { get; init; }
    public DateOnly? RecentRaceDate { get; init; }
}
