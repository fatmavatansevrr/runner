using System;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DERIVED-DIST.0 — the ONE generic Core-horizon authority for every
/// derived-eligible target in the open (TEN_K, HALF_MARATHON) interval.
///
/// <b>REUSED_CONVERGENT_PROJECTED_TARGET_AUTHORITY, not an invented number.</b>
/// 10/12/14 is the exact MinimumCoreWeeks/PreferredCoreWeeks/MaximumCoreWeeks
/// triple three INDEPENDENT real-evidence closures already froze and
/// re-confirmed for three different projected exact cells: 16K
/// (DIST-GEN.1/2/2A, real 10-mile evidence), 15K (DIST-GEN.5/6, independently
/// re-derived and found identical — "real, independently re-derived evidence
/// convergence", not template-copying), and 18K (DIST-GEN.9/10, same
/// independent re-derivation and convergence). <see cref="TargetDistance15KHorizonPolicy"/>,
/// <see cref="TargetDistance16KHorizonPolicy"/>, and <see cref="TargetDistance18KHorizonPolicy"/>
/// each still freeze this triple as that one target's own EXACT-TARGET
/// authority (DIST-GEN.12 §54's "value equality != authority equality" —
/// deliberately never collapsed into one shared constant for those three
/// phases' own governance purposes).
///
/// This prototype makes the DELIBERATE, DISCLOSED opposite decision for the
/// derived-distance path specifically: because the point of DERIVED-DIST.0 is
/// to test whether one GENERIC horizon authority can replace a per-distance
/// evidence-closure phase, this class reuses that same already-approved,
/// three-times-convergent triple as ONE shared, target-invariant authority
/// across the WHOLE open interval, rather than freezing a fourth (or
/// twelfth, or hundredth) near-identical constant. See
/// PHASE_DERIVED_DIST_0_...md §12/§17 for the full disclosure of this
/// decision and why it does not violate the "no invented training-science
/// numbers" rule: no new number is introduced; an existing, convergently-reconfirmed
/// number is applied generically instead of per-distance.
///
/// MinimumCoreWeeks is additionally PARENT-FAMILY STRUCTURAL authority in its
/// own right (not merely convergent with the three sibling cells): it is the
/// exact same <see cref="RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks"/>
/// constant those three cells' own horizon policies already reference — the
/// reused HALF_MARATHON catalog identity's own real phase-allocation
/// minimum-week sum (Foundation=2 + Build=3 + RaceSpecific=3 + Taper=2 = 10).
/// </summary>
public static class DerivedDistanceHorizonAuthority
{
    public const int MinimumCoreWeeks = RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks;
    public const int PreferredCoreWeeks = 12;
    public const int MaximumCoreWeeks = 14;

    /// <summary>
    /// PHASE DERIVED-DIST.4 §41 — TEN_K's OWN real, already-governed
    /// standalone-core horizon triple: <see cref="RaceHorizonPolicy.MinimumSupportedStandaloneWeeks"/>/
    /// <see cref="RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks"/>/
    /// <see cref="RaceHorizonPolicy.MaximumSupportedStandaloneWeeks"/> — the
    /// EXACT same triple canonical exact-TEN_K generation already uses via
    /// <see cref="RaceHorizonPolicy.DecideForDistance"/>'s own default
    /// (non-HalfMarathon) arm. Reused, not invented, for TEN_K-sourced
    /// derived cells (5K&lt;target&lt;TEN_K) per the governing prompt's own
    /// §18-19 instruction not to reuse HALF_MARATHON's horizon for a
    /// different parent family. Deliberately NOT the same triple as
    /// <see cref="MinimumCoreWeeks"/>/<see cref="PreferredCoreWeeks"/>/
    /// <see cref="MaximumCoreWeeks"/> above (8/12/14 vs 10/12/14) — the two
    /// intervals' own parents genuinely differ only in their minimum, which
    /// is exactly the kind of "value equality != authority equality" fact
    /// this engagement's culture requires preserving rather than collapsing.
    /// </summary>
    public const int TenKMinimumCoreWeeks = RaceHorizonPolicy.MinimumSupportedStandaloneWeeks;

    /// <summary>See <see cref="TenKMinimumCoreWeeks"/>.</summary>
    public const int TenKPreferredCoreWeeks = RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks;

    /// <summary>See <see cref="TenKMinimumCoreWeeks"/>.</summary>
    public const int TenKMaximumCoreWeeks = RaceHorizonPolicy.MaximumSupportedStandaloneWeeks;

    /// <summary>
    /// PHASE DERIVED-DIST.4 §41 — selects which already-governed horizon
    /// triple applies, by the resolved cell's own <c>ParentDistanceFamily</c>
    /// (never by the numeric target distance directly). Exhaustive over the
    /// two families <see cref="DerivedDistanceEligibilityPolicy.TryResolve"/>
    /// can ever return today; a future family with no arm here throws rather
    /// than silently defaulting to either existing triple.
    /// </summary>
    private static (int Minimum, int Preferred, int Maximum) TripleFor(GoalDistance parentDistanceFamily) => parentDistanceFamily switch
    {
        GoalDistance.HalfMarathon => (MinimumCoreWeeks, PreferredCoreWeeks, MaximumCoreWeeks),
        GoalDistance.TenK => (TenKMinimumCoreWeeks, TenKPreferredCoreWeeks, TenKMaximumCoreWeeks),
        _ => throw new ArgumentOutOfRangeException(nameof(parentDistanceFamily), parentDistanceFamily, "No derived-distance horizon triple is governed for this parent family."),
    };

    /// <summary>Byte-identical to pre-DERIVED-DIST.4 behavior: the HALF_MARATHON-sourced interval's own fixed triple.</summary>
    public static CoreHorizonDecision Decide(DateOnly startDate, DateOnly raceDate) =>
        RaceHorizonPolicy.Decide(startDate, raceDate, MinimumCoreWeeks, PreferredCoreWeeks, MaximumCoreWeeks);

    public static RaceHorizonClassification Classify(CoreHorizonDecision decision) => RaceHorizonPolicy.Classify(decision);

    public static RaceHorizonClassification Classify(DateOnly startDate, DateOnly raceDate) => Classify(Decide(startDate, raceDate));

    /// <summary>Deterministic, traceable reason code, distinct from every exact projected cell's own and from TEN_K's/HALF_MARATHON's own canonical codes.</summary>
    public static string GetUnsupportedReasonCode(int availableWeeks) => $"DERIVED_DISTANCE_CORE_HORIZON_{availableWeeks}_NOT_SUPPORTED";

    /// <summary>
    /// The complete horizon-authority surface for one resolved derived cell —
    /// deliberately the same SHAPE as <see cref="ProjectedTargetAuthority"/>'s
    /// own horizon-related members (<c>MinimumCoreWeeks</c>/<c>PreferredCoreWeeks</c>/
    /// <c>MaximumCoreWeeks</c>/<c>DecideHorizon</c>/<c>ClassifyHorizon</c>/
    /// <c>GetUnsupportedReasonCode</c>/<c>PilotLabel</c>), so every call site
    /// that already branches on a <see cref="ProjectedTargetAuthority"/> can
    /// add ONE parallel, structurally-identical fallback arm for a derived
    /// cell rather than a bespoke second shape.
    /// </summary>
    public sealed record ResolvedAuthority(
        DerivedDistanceEligibilityPolicy.DerivedDistanceCell Cell,
        string PilotLabel,
        int MinimumCoreWeeks,
        int PreferredCoreWeeks,
        int MaximumCoreWeeks,
        Func<DateOnly, DateOnly, CoreHorizonDecision> DecideHorizon,
        Func<CoreHorizonDecision, RaceHorizonClassification> ClassifyHorizon,
        Func<int, string> GetUnsupportedReasonCode);

    /// <summary>
    /// Resolves the generic derived authority for a request, or null when
    /// <see cref="DerivedDistanceEligibilityPolicy.TryResolve"/> itself
    /// returns null. Callers must check for an exact
    /// <see cref="ProjectedTargetAuthority"/> FIRST and only fall back to
    /// this method when that lookup is null — an already-registered exact
    /// projected cell is never shadowed by this generic authority.
    /// </summary>
    public static ResolvedAuthority? TryResolve(
        double? targetDistanceKmOverride,
        GoalDistance requestGoalDistance,
        RunningBackground? level,
        int? runsPerWeek)
    {
        var cell = DerivedDistanceEligibilityPolicy.TryResolve(targetDistanceKmOverride, requestGoalDistance, level, runsPerWeek);
        if (cell is null)
        {
            return null;
        }

        // PHASE DERIVED-DIST.4 §41 -- the triple (and the decide/classify
        // closures bound to it) is now selected by the resolved cell's own
        // ParentDistanceFamily instead of always being the fixed HALF_MARATHON
        // triple. For a HALF_MARATHON-sourced cell this resolves to exactly
        // MinimumCoreWeeks/PreferredCoreWeeks/MaximumCoreWeeks and
        // RaceHorizonPolicy.Decide invoked with those same three constants —
        // byte-identical to the pre-DERIVED-DIST.4 body for every existing
        // live request.
        var (minimum, preferred, maximum) = TripleFor(cell.ParentDistanceFamily);
        // PHASE DERIVED-DIST.4 §41 -- the label's "(source ...)" suffix must
        // keep using the same literal family string
        // NearestHigherCanonicalPlanResolver itself uses ("HALF_MARATHON"/
        // "TEN_K"), never the GoalDistance enum's own ToString ("HalfMarathon"),
        // so this string is byte-identical to pre-DERIVED-DIST.4 for the
        // HALF_MARATHON arm -- a regression caught by this phase's own full
        // regression run (§46), not a cosmetic choice.
        var sourceFamilyLabel = cell.ParentDistanceFamily switch
        {
            GoalDistance.HalfMarathon => "HALF_MARATHON",
            GoalDistance.TenK => "TEN_K",
            _ => cell.ParentDistanceFamily.ToString(),
        };
        return new ResolvedAuthority(
            cell,
            $"derived {cell.TargetDistanceKm:0.###}km (source {sourceFamilyLabel})",
            minimum,
            preferred,
            maximum,
            (startDate, raceDate) => RaceHorizonPolicy.Decide(startDate, raceDate, minimum, preferred, maximum),
            Classify,
            GetUnsupportedReasonCode);
    }
}
