using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DIST-GEN.10 — the EXPLICIT, dedicated 18K core-horizon authority,
/// freezing DIST-GEN.9 §12-14/§53's own evidence-derived triple
/// (MinimumCoreWeeks=10, PreferredCoreWeeks=12, MaximumCoreWeeks=14). Mirrors
/// <see cref="TargetDistance15KHorizonPolicy"/>'s and
/// <see cref="TargetDistance16KHorizonPolicy"/>'s own shape exactly, as a
/// DISTINCT, independently-declared class — deliberately NOT a shared
/// constant with either sibling policy, even though every numeric value here
/// is identical to both, per DIST-GEN.9's own governing instruction: this is
/// a real, disclosed evidence CONVERGENCE (all three independently reused the
/// reused, unmodified HALF_MARATHON catalog identity's own real
/// minimum-week-sum floor, and all three independently found no evidence to
/// prefer a different Preferred/Maximum), not a reason to collapse three
/// independently-authorized target cells into one literal.
///
/// Consumed only by the dark 18K target seam
/// (<see cref="Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(double?,RunningApp.Domain.Enums.GoalDistance,RunningApp.Domain.Enums.RunningBackground?,int?)"/>-resolved
/// call sites in <c>PlanServices.GeneratePreviewAsync</c> and
/// <c>CatalogPreviewGenerator</c>); never reachable for any canonical TEN_K,
/// HALF_MARATHON, or either of the other two dark targets (15K/16K).
/// </summary>
public static class TargetDistance18KHorizonPolicy
{
    /// <summary>DIST-GEN.9 §12 — the reused HALF_MARATHON catalog identity's own real phase-allocation minimum-week sum (2+3+3+2=10), a structural fact, not re-derived per target.</summary>
    public const int MinimumCoreWeeks = 10;

    /// <summary>DIST-GEN.9 §13 — evidence-informed product default, discovered convergence with 15K's/16K's own value.</summary>
    public const int PreferredCoreWeeks = 12;

    /// <summary>DIST-GEN.9 §14 — evidence-informed product default, discovered convergence with 15K's/16K's own value.</summary>
    public const int MaximumCoreWeeks = 14;

    /// <summary>
    /// The single decision this policy produces — built from
    /// <see cref="RaceHorizonPolicy"/>'s own generic, explicit-bounds
    /// <c>Decide</c> overload (never the distance-blind parameterless one,
    /// and never the TEN_K-fallback arm).
    /// </summary>
    public static CoreHorizonDecision Decide(DateOnly startDate, DateOnly raceDate) =>
        RaceHorizonPolicy.Decide(startDate, raceDate, MinimumCoreWeeks, PreferredCoreWeeks, MaximumCoreWeeks);

    /// <summary>Classifies a decision this policy itself produced. Never mixed with a decision built from different bounds.</summary>
    public static RaceHorizonClassification Classify(CoreHorizonDecision decision) => RaceHorizonPolicy.Classify(decision);

    /// <summary>Convenience: decide-then-classify in one call, for callers that don't need the raw decision.</summary>
    public static RaceHorizonClassification Classify(DateOnly startDate, DateOnly raceDate) => Classify(Decide(startDate, raceDate));

    /// <summary>Deterministic, traceable reason code for a rejected 18K dark-target horizon, distinct from TEN_K's, HALF_MARATHON's, the 15K target's, and the 16K target's own reason codes.</summary>
    public static string GetUnsupportedReasonCode(int availableWeeks) => $"DARK_18K_CORE_HORIZON_{availableWeeks}_NOT_SUPPORTED";
}
