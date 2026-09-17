using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DIST-GEN.2 — the EXPLICIT, dedicated 16K core-horizon authority,
/// originally freezing DIST-GEN.1 §9/§10's own evidence-derived triple
/// (MinimumCoreWeeks=8, PreferredCoreWeeks=12, MaximumCoreWeeks=14).
/// PHASE DIST-GEN.2A corrected MinimumCoreWeeks to 10 (PreferredCoreWeeks=12
/// and MaximumCoreWeeks=14 re-confirmed unchanged) after DIST-GEN.2 found the
/// original 8 could never materialize against the reused HALF_MARATHON
/// catalog identity's own real phase-allocation minimum-week sum of 10; see
/// <see cref="MinimumCoreWeeks"/>'s own doc-comment for the full chronology.
/// 8-9W dark 16K requests are deferred to a future compressed-core track, not
/// silently unsupported-forever.
///
/// This is deliberately NOT <see cref="RaceHorizonPolicy.DecideForDistance"/>'s
/// own <c>_ =&gt; Decide(startDate, raceDate)</c> fallback arm (TEN_K's
/// borrowed 8/12/14 bounds, used today for every non-HalfMarathon distance
/// including a hypothetical future one) — reusing that fallback arm for 16K
/// would recreate exactly the "silent default" anti-pattern
/// PHASE_DIST_GEN_0_1 eliminated for GoalDistance.Custom. Instead, this class
/// calls <see cref="RaceHorizonPolicy"/>'s own explicit, generic, already-proven
/// 5-argument <c>Decide(startDate, raceDate, minimumCoreWeeks, preferredCoreWeeks,
/// maximumCoreWeeks)</c> overload directly, with its own named constants — the
/// exact same low-level engine every other named horizon (TEN_K's own 8/12/14,
/// HALF_MARATHON's own 10/14/16) is built from, but as a distinct, traceable
/// call site with its own reasoning (DIST-GEN.1 §9-10's real, on-point 10-mile
/// evidence), not a reuse of the distance-blind fallback arm. The numeric
/// coincidence with TEN_K's own triple is a discovered convergence from real
/// external evidence (DIST-GEN.1 §10), not a decision to treat TEN_K as this
/// pilot's horizon parent.
///
/// Consumed only by the dark 16K pilot seam (<see cref="Dark16KPilotEligibilityPolicy"/>-gated
/// call sites in <c>PlanServices.GeneratePreviewAsync</c>); never reachable
/// for any canonical TEN_K or HALF_MARATHON request.
/// </summary>
public static class TargetDistance16KHorizonPolicy
{
    /// <summary>
    /// DIST-GEN.1 §10 originally froze this at 8 from real, on-point
    /// 10-mile/16.09km evidence. DIST-GEN.2 discovered that value could never
    /// actually materialize against the reused, unmodified HALF_MARATHON
    /// catalog identity's own real phase-allocation minimum-week sum
    /// (FOUNDATION=2 + BUILD=3 + RACE_SPECIFIC=3 + TAPER=2 = 10), and closed
    /// honestly as blocked rather than force a value that could never
    /// materialize. DIST-GEN.2A is the governing correction: the standalone
    /// dark 16K Standard Core horizon authority is corrected to 10, mirroring
    /// this reused identity's real minimum; 8-9W are deferred to a future,
    /// separately-governed compressed-core track (see this repo's own
    /// HM-X2 precedent for HALF_MARATHON's own 7-9W deferral).
    /// </summary>
    public const int MinimumCoreWeeks = 10;

    /// <summary>DIST-GEN.1 §10 — preferred core length.</summary>
    public const int PreferredCoreWeeks = 12;

    /// <summary>DIST-GEN.1 §10 — real, on-point evidence-derived maximum.</summary>
    public const int MaximumCoreWeeks = 14;

    /// <summary>
    /// The single decision this policy produces — built from
    /// <see cref="RaceHorizonPolicy"/>'s own generic, explicit-bounds
    /// <c>Decide</c> overload (never the distance-blind parameterless one).
    /// </summary>
    public static CoreHorizonDecision Decide(DateOnly startDate, DateOnly raceDate) =>
        RaceHorizonPolicy.Decide(startDate, raceDate, MinimumCoreWeeks, PreferredCoreWeeks, MaximumCoreWeeks);

    /// <summary>Classifies a decision this policy itself produced. Never mixed with a decision built from different bounds.</summary>
    public static RaceHorizonClassification Classify(CoreHorizonDecision decision) => RaceHorizonPolicy.Classify(decision);

    /// <summary>Convenience: decide-then-classify in one call, for callers that don't need the raw decision.</summary>
    public static RaceHorizonClassification Classify(DateOnly startDate, DateOnly raceDate) => Classify(Decide(startDate, raceDate));

    /// <summary>Deterministic, traceable reason code for a rejected 16K dark-pilot horizon, distinct from both TEN_K's and HALF_MARATHON's own reason codes.</summary>
    public static string GetUnsupportedReasonCode(int availableWeeks) => $"DARK_16K_CORE_HORIZON_{availableWeeks}_NOT_SUPPORTED";
}
