using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DIST-GEN.20 — the EXPLICIT, dedicated core-horizon authority for the
/// dark <c>16K × Intermediate × 5D</c> cell, freezing DIST-GEN.19 §14/§61 and
/// DIST-GEN.19A §23's own finding that <see cref="PreferredCoreWeeks"/>/
/// <see cref="MaximumCoreWeeks"/> are <c>TARGET_ONLY</c> — reused directly,
/// unchanged, from 16K's own 4D horizon authority
/// (<see cref="TargetDistance16KHorizonPolicy"/>), since that class was never
/// frequency-parametrized to begin with and DIST-GEN.15/DIST-GEN.19 have now
/// each independently confirmed frequency does not alter this target's own
/// horizon bounds. <see cref="MinimumCoreWeeks"/> is likewise unchanged (10) —
/// parent-family structural authority, frequency-blind by construction.
///
/// Declared as its OWN distinct class, mirroring
/// <see cref="TargetDistance16KIntermediate3DHorizonPolicy"/>'s own established
/// pattern for target-owned horizon authority at a non-4D frequency, not
/// folded into <see cref="TargetDistance16KHorizonPolicy"/> itself, so that a
/// future governance change to any one frequency's own horizon bounds can
/// never silently affect another, and so this cell's own reason code
/// (<see cref="GetUnsupportedReasonCode"/>) stays distinct and traceable.
///
/// Consumed only by the dark 16K×Intermediate×5D target seam
/// (<see cref="Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(double?,RunningApp.Domain.Enums.GoalDistance,RunningApp.Domain.Enums.RunningBackground?,int?)"/>-resolved
/// call sites, via <see cref="ProjectedTargetAuthorityRegistry"/>); never
/// reachable for any canonical TEN_K/HALF_MARATHON request, and never for
/// 16K's own 3D or 4D cells, 15K, or 18K.
/// </summary>
public static class TargetDistance16KIntermediate5DHorizonPolicy
{
    /// <summary>
    /// DIST-GEN.19 §14 — the reused HALF_MARATHON catalog identity's own real
    /// phase-allocation minimum-week sum (2+3+3+2=10), a structural,
    /// frequency-blind fact. References the same parent-family constant every
    /// other HM-parented target's horizon policy already uses.
    /// </summary>
    public const int MinimumCoreWeeks = RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks;

    /// <summary>DIST-GEN.19 §14/§61 — <c>TARGET_ONLY</c>, reused unchanged from 16K's own 4D horizon authority (frequency-invariant).</summary>
    public const int PreferredCoreWeeks = TargetDistance16KHorizonPolicy.PreferredCoreWeeks;

    /// <summary>DIST-GEN.19 §14/§61 — <c>TARGET_ONLY</c>, reused unchanged from 16K's own 4D horizon authority (frequency-invariant).</summary>
    public const int MaximumCoreWeeks = TargetDistance16KHorizonPolicy.MaximumCoreWeeks;

    /// <summary>
    /// The single decision this policy produces — built from
    /// <see cref="RaceHorizonPolicy"/>'s own generic, explicit-bounds
    /// <c>Decide</c> overload (never the distance-blind parameterless one,
    /// and never any other cell's own bounds).
    /// </summary>
    public static CoreHorizonDecision Decide(DateOnly startDate, DateOnly raceDate) =>
        RaceHorizonPolicy.Decide(startDate, raceDate, MinimumCoreWeeks, PreferredCoreWeeks, MaximumCoreWeeks);

    /// <summary>Classifies a decision this policy itself produced. Never mixed with a decision built from different bounds.</summary>
    public static RaceHorizonClassification Classify(CoreHorizonDecision decision) => RaceHorizonPolicy.Classify(decision);

    /// <summary>Convenience: decide-then-classify in one call, for callers that don't need the raw decision.</summary>
    public static RaceHorizonClassification Classify(DateOnly startDate, DateOnly raceDate) => Classify(Decide(startDate, raceDate));

    /// <summary>Deterministic, traceable reason code for a rejected 16K×Intermediate×5D dark-cell horizon, distinct from TEN_K's, HALF_MARATHON's, and every other projected target/frequency's own reason codes (including 16K's own 3D/4D cells).</summary>
    public static string GetUnsupportedReasonCode(int availableWeeks) => $"DARK_16K_I5D_CORE_HORIZON_{availableWeeks}_NOT_SUPPORTED";
}
