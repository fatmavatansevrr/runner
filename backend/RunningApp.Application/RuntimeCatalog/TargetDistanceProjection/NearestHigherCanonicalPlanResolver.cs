using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.Common;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DERIVED-DIST.0 — the one generic resolution boundary answering
/// "given a requested target race distance, which READY CANONICAL plan is
/// the nearest one strictly above it?" (governing prompt §7/§9). This is
/// deliberately NOT a classification of which catalog family would
/// structurally serve a distance (that is <see cref="CanonicalDistanceFamilyResolver"/>'s
/// job) and NOT an eligibility/approval gate (that is
/// <see cref="DerivedDistanceEligibilityPolicy"/>'s job) — this type answers
/// only the nearest-higher SOURCE-SELECTION question, over an explicit,
/// small list of READY CANONICAL plans.
///
/// "Ready canonical" here means: a real, already-governed, non-projected
/// canonical race-distance family this codebase generates today
/// (TEN_K, HALF_MARATHON) — never one of the existing DIST-GEN projected
/// exact cells (15K/16K/18K), which the governing prompt's §6 explicitly
/// forbids using as a derived-distance SOURCE (they remain comparison/oracle
/// cells only). Deliberately data-driven (§57): adding a future ready
/// canonical plan (e.g. Marathon) to <see cref="ReadyCanonicalPlans"/> would
/// naturally change nearest-higher selection for distances above today's
/// HALF_MARATHON ceiling without rewriting any if/else chain — no such
/// addition is made by this phase.
/// </summary>
public static class NearestHigherCanonicalPlanResolver
{
    /// <summary>One ready canonical plan's structural family and real race distance.</summary>
    public sealed record CanonicalPlan(string Family, double DistanceKm);

    /// <summary>
    /// The complete, explicit set of READY CANONICAL plans this resolver may
    /// select from today. Order is not significant — selection is always by
    /// smallest DistanceKm strictly greater than the request, never list
    /// order. Values are read from the same shared <see cref="GoalDistanceKm"/>
    /// table every canonical request already uses — never a new or
    /// re-declared distance constant.
    /// </summary>
    public static readonly IReadOnlyList<CanonicalPlan> ReadyCanonicalPlans =
    [
        new("TEN_K", GoalDistanceKm.Resolve(GoalDistance.TenK)),
        new("HALF_MARATHON", GoalDistanceKm.Resolve(GoalDistance.HalfMarathon)),
    ];

    /// <summary>
    /// The nearest-higher ready canonical plan for <paramref name="requestedTargetDistanceKm"/>,
    /// or null if no ready canonical plan exists strictly above it (fail
    /// closed per governing prompt §8 — never a fallback to the nearest
    /// lower plan, nearest absolute distance, or an invented source).
    /// Exact-equality to a canonical plan's own distance never selects that
    /// plan as its own "higher" source (strictly greater than, never
    /// greater-or-equal) — an exact canonical request bypasses this resolver
    /// entirely (see <see cref="DerivedDistanceEligibilityPolicy"/>).
    /// </summary>
    public static CanonicalPlan? TryResolveNearestHigher(double requestedTargetDistanceKm) =>
        ReadyCanonicalPlans
            .Where(p => p.DistanceKm > requestedTargetDistanceKm)
            .OrderBy(p => p.DistanceKm)
            .FirstOrDefault();
}
