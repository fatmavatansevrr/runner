using RunningApp.Domain.Enums;

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
    public int? TargetFinishTimeSeconds { get; init; }
    public int? DaysPerWeek { get; init; }
    public RunningBackground? Level { get; init; }

    // ── Phase 4B fitness-evidence fields (verbatim from GeneratePreviewRequest) ──
    public double? RecentLongestRunKm { get; init; }
    public double? RecentWeeklyVolumeKm { get; init; }
    public int? RecentRunsPerWeek { get; init; }
    public double? RecentRaceDistanceKm { get; init; }
    public int? RecentRaceFinishTimeSeconds { get; init; }
    public DateOnly? RecentRaceDate { get; init; }
}
