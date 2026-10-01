using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.Common;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DERIVED-DIST.0 — dark/internal-prototype-only eligibility gate for
/// the NEAREST-HIGHER-CANONICAL-PLAN derivation mechanism (governing prompt
/// §4/§38-41). Genuinely distinct in kind from <see cref="Dark16KPilotEligibilityPolicy"/>:
/// that gate is an exact-cell REGISTRY (one explicit record per approved
/// distance/level/frequency quadruple); this gate is a CONTINUOUS-RANGE
/// check (any exact decimal target strictly between TEN_K and HALF_MARATHON,
/// for the level/frequency combinations this prototype actually exercises) —
/// the whole architectural point being that NO per-distance registration is
/// required here (§34).
///
/// Consulted only AFTER <see cref="Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(double?,GoalDistance,RunningBackground?,int?)"/>
/// / <see cref="ProjectedTargetAuthorityRegistry.TryResolve"/> have already
/// returned null at every call site that widens for this phase — an
/// already-registered exact projected cell (15K/16K/18K) is NEVER shadowed or
/// double-dispatched by this policy; it keeps going through its own,
/// completely unmodified DIST-GEN path. This also means the existing
/// projected cells are never themselves treated as derivation SOURCES here
/// (governing prompt §6) — this gate's own source is always resolved
/// through <see cref="NearestHigherCanonicalPlanResolver"/>, which only
/// knows about READY CANONICAL plans (TEN_K, HALF_MARATHON), never about any
/// projected exact cell.
///
/// Never reachable from any public request DTO — exactly the same
/// "production-owned but not publicly reachable" seam
/// <see cref="Dark16KPilotEligibilityPolicy"/> itself documents: no public
/// wire contract carries an arbitrary target-distance field, so
/// <see cref="RunningApp.Application.DTOs.Plan.GeneratePreviewRequest.TargetDistanceKmOverride"/>
/// is always null for every real HTTP-originated request. This prototype
/// adds no new public DTO field and widens no public validator — it is
/// reachable only by a test harness (or a future, separately-governed
/// production caller) constructing <see cref="RunningApp.Application.DTOs.Plan.GeneratePreviewRequest"/>
/// directly.
/// </summary>
public static class DerivedDistanceEligibilityPolicy
{
    private const double ToleranceKm = 0.001;

    /// <summary>
    /// V1 prototype scope (governing prompt §41): Intermediate only. The
    /// resolver itself is not hardwired to this list structurally (see
    /// <see cref="TryResolve"/>'s own shape) — widening to another level is a
    /// one-line addition here, never a new per-distance policy class.
    /// </summary>
    private static readonly IReadOnlyList<RunningBackground> EligibleLevels = [RunningBackground.Intermediate];

    /// <summary>
    /// V1 prototype scope (governing prompt §33/§42): the three frequencies
    /// canonical HALF_MARATHON Intermediate already has its own named
    /// <see cref="Prescription.Volume.VolumeSafetyPolicy"/> instance for
    /// (3D/4D/5D). No per-target frequency registry is introduced — this is
    /// the SAME generic list for every requested target distance in range.
    /// </summary>
    private static readonly IReadOnlyList<int> EligibleRunsPerWeek = [3, 4, 5];

    /// <summary>
    /// One resolved derived-eligible request shape — deliberately NOT the
    /// same type as <see cref="Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell"/>:
    /// that type represents one row of an exact-match REGISTRY; this type
    /// represents one point inside a CONTINUOUS eligible range. The two are
    /// never interchangeable and never compared to each other.
    /// </summary>
    public sealed record DerivedDistanceCell(double TargetDistanceKm, GoalDistance ParentDistanceFamily, RunningBackground Level, int RunsPerWeek);

    /// <summary>Convenience boolean form of <see cref="TryResolve"/>.</summary>
    public static bool IsEligible(double? targetDistanceKmOverride, GoalDistance requestGoalDistance, RunningBackground? level, int? runsPerWeek) =>
        TryResolve(targetDistanceKmOverride, requestGoalDistance, level, runsPerWeek) is not null;

    /// <summary>
    /// Resolves a request as DERIVED-eligible, or null for any of: no
    /// target-distance override at all (every canonical/non-override
    /// request); a requested distance at or below TEN_K's own real distance,
    /// or at or above HALF_MARATHON's own real distance (exact-canonical
    /// bypass, governing prompt §5/§40 — those requests must keep going
    /// through their own unmodified canonical generation path, never this
    /// derivation path); a resolved nearest-higher source that is not
    /// HALF_MARATHON (structurally impossible for V1's own (10, HALF_MARATHON)
    /// scope, defensive only); an unsupported level or frequency (governing
    /// prompt §41 — never silently widened beyond what this prototype
    /// actually exercises).
    /// </summary>
    public static DerivedDistanceCell? TryResolve(
        double? targetDistanceKmOverride,
        GoalDistance requestGoalDistance,
        RunningBackground? level,
        int? runsPerWeek)
    {
        if (targetDistanceKmOverride is not { } km)
        {
            return null;
        }

        // Exact-canonical bypass (§5/§40): strictly inside the open interval
        // only. A request numerically at (or within tolerance of) either
        // canonical endpoint must never be treated as derived.
        var tenKKm = GoalDistanceKm.Resolve(GoalDistance.TenK);
        var halfMarathonKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon);
        if (km <= tenKKm + ToleranceKm || km >= halfMarathonKm - ToleranceKm)
        {
            return null;
        }

        // Governing prompt §4: V1 prototype scope is strictly the 10K->HM
        // interval. The request's own GoalDistance must already be
        // HALF_MARATHON (the one family this interval's nearest-higher
        // source resolves to) -- never inferred from the numeric distance
        // alone, mirroring Dark16KPilotEligibilityPolicy's own requirement
        // that the request's GoalDistance already names the structural
        // parent family.
        if (requestGoalDistance != GoalDistance.HalfMarathon)
        {
            return null;
        }

        // Defensive re-confirmation through the generic nearest-higher
        // resolver (never bypassed, even though the two endpoint checks
        // above already make the answer structurally HALF_MARATHON for
        // every value that reaches this line) -- keeps this policy and
        // NearestHigherCanonicalPlanResolver from ever silently diverging.
        var source = NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(km);
        if (source is null || source.Family != "HALF_MARATHON")
        {
            return null;
        }

        if (level is not { } resolvedLevel || !EligibleLevels.Contains(resolvedLevel))
        {
            return null;
        }

        if (runsPerWeek is not { } resolvedRunsPerWeek || !EligibleRunsPerWeek.Contains(resolvedRunsPerWeek))
        {
            return null;
        }

        return new DerivedDistanceCell(km, requestGoalDistance, resolvedLevel, resolvedRunsPerWeek);
    }
}
