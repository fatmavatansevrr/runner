using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
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
    /// PHASE DERIVED-DIST.2 §9/§51 — the PRODUCT V1 derived subset. Gate (A)
    /// of the two independent gates this policy now composes (see
    /// <see cref="TryResolve"/>'s own doc comment): Intermediate only, the
    /// resolver itself is not hardwired to this list structurally — widening
    /// to another level is a one-line addition here, never a new
    /// per-distance policy class. This gate alone is NECESSARY but never
    /// SUFFICIENT — see <see cref="IsPublicForHalfMarathonParent"/>, gate (B).
    /// </summary>
    private static readonly IReadOnlyList<RunningBackground> EligibleLevels = [RunningBackground.Intermediate];

    /// <summary>
    /// PHASE DERIVED-DIST.2 §9/§51 — the PRODUCT V1 derived subset's
    /// frequency arm. Gate (A), paired with <see cref="EligibleLevels"/>: the
    /// three frequencies canonical HALF_MARATHON Intermediate already has its
    /// own named <see cref="Prescription.Volume.VolumeSafetyPolicy"/> instance
    /// for (3D/4D/5D). No per-target frequency registry is introduced — this
    /// is the SAME generic list for every requested target distance in range.
    /// Deliberately excludes Intermediate 6D even though gate (B) (the real
    /// HM parent public matrix) admits it — proven by
    /// <c>DerivedPublicEligibilityProductSubsetStillRestrictsIntermediate6DTests</c>.
    /// </summary>
    private static readonly IReadOnlyList<int> EligibleRunsPerWeek = [3, 4, 5];

    /// <summary>
    /// PHASE DERIVED-DIST.4A §19/§20/§51 — the TEN_K-sourced derived interval
    /// (5K&lt;target&lt;TEN_K) is now structurally public, gated the SAME way
    /// the HALF_MARATHON-sourced interval already was from DERIVED-DIST.2
    /// onward: gate (A), <see cref="EligibleLevels"/>/<see cref="EligibleRunsPerWeek"/>
    /// (the shared V1 product subset), composed with gate (B),
    /// <see cref="IsPublicForParent"/> consulting canonical TEN_K's own real
    /// public Level×Frequency matrix. DERIVED-DIST.4 originally shipped this
    /// interval behind a dark/internal-only mutable static switch,
    /// <c>AllowTenKSourcedDerivationForInternalVerificationOnly</c>
    /// (default <c>false</c>), so the generalized generation pipeline could be
    /// proven end-to-end without widening any real public eligibility.
    /// DERIVED-DIST.4A's own TEN_K long-run safety audit (PHASE_DERIVED_DIST_4A...md
    /// §11) found TEN_K's existing share-based/growth-capped volume-safety
    /// authority (<see cref="Prescription.Volume.VolumeSafetyPolicy"/>)
    /// sufficient for this interval as-is — no new guardrail, no invented
    /// ceiling — so that switch has been REMOVED (not merely defaulted true):
    /// a mutable test-only global flag controlling production support is not
    /// an acceptable final design (governing-prompt §19). The two gates below
    /// are now the only thing standing between a TEN_K-sourced km and
    /// eligibility, exactly mirroring the HALF_MARATHON-sourced interval's own
    /// already-public composition.
    /// </summary>

    /// <summary>
    /// PHASE DERIVED-DIST.2 §10/§11/§50, generalized by PHASE DERIVED-DIST.4
    /// §41 — gate (B): structural coupling to the REAL canonical parent's
    /// public Level×Frequency eligibility source, fixing the accidental/
    /// hardcoded containment DERIVED-DIST.1 disclosed (this policy previously
    /// stayed within HM's own public matrix only because its own V1 scope
    /// happened to be a subset of it, never because it actually asked HM's
    /// own authority). <see cref="V1CatalogPilotIdentityPolicy.IsSupportedIdentity"/>
    /// is the single, centrally-owned definition of EVERY family's own public
    /// Level×Frequency allow-list (not only HALF_MARATHON's: it already has a
    /// genuinely distinct TEN_K arm — Intermediate 2D/3D/4D/5D/6D, Beginner
    /// 2D/3D/4D, Advanced 3D/4D/5D/6D — confirmed by direct read, zero lines
    /// of that file changed by DERIVED-DIST.4) — this method asks it
    /// directly, by reference, parameterized by whichever parent family
    /// <see cref="TryResolve"/> actually resolved, rather than duplicating
    /// any part of either matrix here or hardcoding a single family. A future
    /// change to either family's own public matrix (widening OR narrowing)
    /// therefore propagates to derived eligibility automatically and without
    /// a second edit — see <c>DerivedPublicEligibilityStructurallyConsultsHalfMarathonParentMatrixTests</c>
    /// for the pre-existing, still-valid non-gameable coupling proof for the
    /// HALF_MARATHON arm specifically.
    /// </summary>
    private static bool IsPublicForParent(GoalDistance parentFamily, RunningBackground level, int runsPerWeek) =>
        V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, parentFamily, level, runsPerWeek);

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
    /// PHASE DERIVED-DIST.4 §41 — maps a <see cref="NearestHigherCanonicalPlanResolver.CanonicalPlan"/>'s
    /// own <c>Family</c> string to the <see cref="GoalDistance"/> every
    /// caller of this policy must already carry for a request to be eligible
    /// through that source. Deliberately exhaustive over
    /// <see cref="NearestHigherCanonicalPlanResolver.ReadyCanonicalPlans"/>'s
    /// own two entries today (TEN_K, HALF_MARATHON) — FIVE_K is deliberately
    /// NOT a case here (and is not in <c>ReadyCanonicalPlans</c> either),
    /// because canonical FIVE_K has no governed generation authority for this
    /// mechanism to fall through to (PHASE_DERIVED_DIST_4...md §2); adding a
    /// FIVE_K arm here without first building that authority would be exactly
    /// the kind of silent, ungoverned widening this policy's whole design
    /// exists to prevent. A source family with no arm below resolves to null,
    /// never a guessed/default family.
    /// </summary>
    private static GoalDistance? ExpectedRequestGoalDistanceFor(string sourceFamily) => sourceFamily switch
    {
        "TEN_K" => GoalDistance.TenK,
        "HALF_MARATHON" => GoalDistance.HalfMarathon,
        _ => null,
    };

    /// <summary>
    /// Resolves a request as DERIVED-eligible, or null for any of: no
    /// target-distance override at all (every canonical/non-override
    /// request); no ready-higher canonical source at all (at/above
    /// HALF_MARATHON's own real distance — governing prompt §5/§40, fail
    /// closed, never a fallback to the nearest lower plan); a requested
    /// distance at or within tolerance of the resolved source's own real
    /// distance (exact-canonical bypass — that request must keep going
    /// through its own unmodified canonical generation path, never this
    /// derivation path); a request whose own GoalDistance does not already
    /// name the resolved source's structural family (PHASE DERIVED-DIST.4
    /// §41 generalization — previously hardcoded to require HALF_MARATHON
    /// specifically; now generic over whichever ready canonical family
    /// <see cref="NearestHigherCanonicalPlanResolver"/> actually resolves,
    /// via <see cref="ExpectedRequestGoalDistanceFor"/>, so a request whose
    /// own GoalDistance is TEN_K is eligible for the 5K&lt;target&lt;TEN_K
    /// interval exactly as HALF_MARATHON was already eligible for
    /// TEN_K&lt;target&lt;HALF_MARATHON — never inferred from the numeric
    /// distance alone, mirroring Dark16KPilotEligibilityPolicy's own
    /// requirement that the request's GoalDistance already names the
    /// structural parent family); an unsupported level or frequency
    /// (governing prompt §41 — never silently widened beyond what this
    /// mechanism actually exercises for either interval).
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

        // The ONE generic nearest-higher-canonical-plan source-selection call
        // (PHASE DERIVED-DIST.4 §41 -- this was previously only a defensive
        // re-confirmation performed AFTER two separate hardcoded endpoint
        // checks; it is now the PRIMARY source of both the ceiling (the
        // resolved source's own distance) and the family identity, so the
        // two concerns can never silently diverge from each other again).
        // Fails closed (null) for any km at/above HALF_MARATHON's own real
        // distance, with zero per-distance registration of any kind, exactly
        // as before.
        var source = NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(km);
        if (source is null)
        {
            return null;
        }

        // Exact-canonical bypass: strictly inside the open interval below
        // the resolved source's own real distance only. A request
        // numerically at (or within tolerance of) the resolved canonical
        // endpoint must never be treated as derived.
        if (km >= source.DistanceKm - ToleranceKm)
        {
            return null;
        }

        // PHASE DERIVED-DIST.4 §41 -- the FLOOR half of the open interval,
        // restored generically (a regression caught by this phase's own
        // full-regression run, §46/§9: the single-sided ceiling check above
        // is NOT sufficient on its own -- without this floor, km=10.0 with
        // GoalDistance=HalfMarathon would incorrectly resolve eligible,
        // because TryResolveNearestHigher(10.0) itself correctly answers
        // "HALF_MARATHON is the nearest plan strictly above 10.0" with no
        // concept of a lower bound; the floor is this policy's own concern,
        // not the resolver's). For a HALF_MARATHON-sourced cell the floor is
        // TEN_K's own real distance -- byte-identical to the pre-DERIVED-DIST.4
        // hardcoded "km <= tenKKm" check. For a TEN_K-sourced cell the floor
        // is FIVE_K's own real distance (reused GoalDistanceKm.Resolve
        // constant, never a new number) -- defense-in-depth alongside the
        // family-equality check below, which already independently rejects
        // any FIVE_K-flavored request via CanonicalDistanceFamilyResolver's
        // own band assignment (see that check's own doc comment).
        var floorKm = source.Family switch
        {
            "HALF_MARATHON" => GoalDistanceKm.Resolve(GoalDistance.TenK),
            "TEN_K" => GoalDistanceKm.Resolve(GoalDistance.FiveK),
            _ => 0d,
        };
        if (km <= floorKm + ToleranceKm)
        {
            return null;
        }

        // PHASE DERIVED-DIST.4 §41 generalization: the request's own
        // GoalDistance must already name the SAME family the resolver
        // independently picked for this numeric target -- never inferred
        // from the numeric distance alone. For km in (TEN_K, HALF_MARATHON)
        // this still requires exactly HALF_MARATHON (byte-identical to the
        // pre-DERIVED-DIST.4 hardcoded check, since source.Family can only
        // ever be "HALF_MARATHON" for that range). For km in (FIVE_K, TEN_K)
        // this now requires exactly TEN_K. A FIVE_K-flavored request (km at
        // or below FIVE_K's own real distance) can never pass this check: no
        // ready canonical plan exists at or below FIVE_K in
        // NearestHigherCanonicalPlanResolver.ReadyCanonicalPlans today, so
        // TryResolveNearestHigher would itself resolve TEN_K as the nearest
        // higher plan for such a km -- but CanonicalDistanceFamilyResolver
        // (the one place request.GoalDistance is actually assigned for a
        // real public request, see GeneratePreviewCommandMapper.cs) assigns
        // GoalDistance.FiveK for any km at or below 5.0, which will never
        // equal GoalDistance.TenK, so this check still correctly rejects it.
        // This is a structural, not incidental, guarantee -- see
        // PHASE_DERIVED_DIST_4...md §41/§9 for the full disclosure of why
        // this is deliberately NOT widened to admit FIVE_K.
        var expectedGoalDistance = ExpectedRequestGoalDistanceFor(source.Family);
        if (expectedGoalDistance is null || requestGoalDistance != expectedGoalDistance)
        {
            return null;
        }

        // PHASE DERIVED-DIST.4A §19/§20 -- the TEN_K-sourced arm is now
        // structurally public, on the same footing as the HALF_MARATHON-sourced
        // arm: no dark/internal-only switch gates it any longer. Gates (A) and
        // (B) below are the only remaining admission checks.

        // Gate (A) -- product V1 derived subset (§9/§51), SHARED unchanged
        // across both intervals: reusing the same already-approved,
        // deliberately conservative Intermediate x {3,4,5} subset for the
        // 5K-10K interval rather than inventing a second, wider or narrower
        // product decision for it (PHASE_DERIVED_DIST_4...md §41).
        if (level is not { } resolvedLevel || !EligibleLevels.Contains(resolvedLevel))
        {
            return null;
        }

        if (runsPerWeek is not { } resolvedRunsPerWeek || !EligibleRunsPerWeek.Contains(resolvedRunsPerWeek))
        {
            return null;
        }

        // Gate (B) -- the real canonical RESOLVED parent's own public
        // Level×Frequency eligibility (§10/§11/§50, generalized §41). BOTH
        // gates must pass; neither is sufficient alone (§51's product-subset
        // test proves (A) still restricts even where (B) alone would admit
        // more, e.g. Intermediate 6D, for either family).
        if (!IsPublicForParent(expectedGoalDistance.Value, resolvedLevel, resolvedRunsPerWeek))
        {
            return null;
        }

        return new DerivedDistanceCell(km, requestGoalDistance, resolvedLevel, resolvedRunsPerWeek);
    }
}
