using System;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.Common;

/// <summary>
/// Classification of a race request's available horizon (StartDate to
/// RaceDate) against the currently implemented standalone core-generation
/// capability. See <see cref="RaceHorizonPolicy.Classify"/>.
/// </summary>
public enum RaceHorizonClassification
{
    /// <summary>Horizon is shorter than the nominal minimum. Pre-existing, unchanged behavior — not addressed by this policy.</summary>
    BelowMinimum,

    /// <summary>
    /// Horizon is exactly <see cref="RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks"/>
    /// weeks — the preferred canonical core path. Generation may proceed.
    /// </summary>
    ExactStandaloneCoreSupported,

    /// <summary>Horizon is within the activated 8-14-week standalone-core range.</summary>
    StandaloneCoreSupported,

    /// <summary>
    /// Legacy compatibility value retained for old callers and tests. The
    /// canonical decision mapping no longer emits it: compressed and extended
    /// 8-14-week cores map to <see cref="StandaloneCoreSupported"/>.
    /// </summary>
    CoreLengthRecognizedButNotImplemented,

    /// <summary>
    /// Horizon exceeds the approved standalone maximum and requires
    /// not-yet-implemented preparation + race-core composition.
    /// </summary>
    CompositionRequired,
}

/// <summary>
/// Single source of truth for computing a race request's available training
/// horizon (whole weeks between StartDate and RaceDate) and for classifying
/// it against the currently implemented standalone core-generation
/// capability. Route selection, preview generation, skeleton building, and
/// calendar materialization must all derive any horizon-related decision
/// from this one calculation and this one classification — never a second,
/// independently-computed value (e.g. days-from-today instead of
/// days-from-StartDate, or a re-derived min/max range check) — so they can
/// never disagree.
///
/// Phase 4G.5L ownership: <see cref="CoreHorizonClassifier"/> performs the
/// only elapsed-day/full-week/remainder calculation. This policy consumes
/// that typed decision and maps modes to public behavior. Complete 8-14-week
/// standalone cores are supported; 12w0d retains its preferred canonical
/// path. Longer horizons remain preparation-runway composition required.
/// </summary>
public static class RaceHorizonPolicy
{
    /// <summary>
    /// Nominal standalone race-cycle minimum, mirroring the real candidate's
    /// master-template core-cycle bounds (plan-catalog/catalog/templates/
    /// ten-k-master.v6.json coreCycle.minimumWeeks). Horizons below this are
    /// a separate, pre-existing concern this policy does not classify.
    /// </summary>
    public const int MinimumSupportedStandaloneWeeks = 8;

    /// <summary>
    /// Nominal standalone race-cycle maximum, mirroring the real candidate's
    /// master-template core-cycle bounds (coreCycle.maximumWeeks). Horizons
    /// above this require preparation + race-core composition, which is not
    /// yet implemented.
    /// </summary>
    public const int MaximumSupportedStandaloneWeeks = 14;

    /// <summary>
    /// Preferred standalone core length. Exact 12w0d remains on the canonical
    /// preferred path rather than the compressed/extended dynamic path.
    /// </summary>
    public const int ExactStandaloneCoreSupportedWeeks = 12;

    /// <summary>
    /// HM.18 Blocker 1 — Half-Marathon's own real, frozen standalone core
    /// bounds, mirroring HALF_MARATHON_MASTER's coreCycle
    /// (minimumWeeks/defaultWeeks/maximumWeeks: 10/14/16 — byte-identical
    /// across v1/v2/v3, confirmed by direct read of
    /// plan-catalog/catalog/templates/half-marathon-master.v1/v2/v3.json).
    /// Additive only: introducing these constants does not change
    /// <see cref="MinimumSupportedStandaloneWeeks"/>/<see cref="ExactStandaloneCoreSupportedWeeks"/>/
    /// <see cref="MaximumSupportedStandaloneWeeks"/> (TEN_K's own bounds) at
    /// all, so every existing TEN_K caller of the parameterless
    /// <see cref="Decide(DateOnly,DateOnly)"/> overload is byte-identical,
    /// zero-delta, by construction.
    /// </summary>
    public const int HalfMarathonMinimumSupportedStandaloneWeeks = 10;

    /// <summary>See <see cref="HalfMarathonMinimumSupportedStandaloneWeeks"/>.</summary>
    public const int HalfMarathonMaximumSupportedStandaloneWeeks = 16;

    /// <summary>See <see cref="HalfMarathonMinimumSupportedStandaloneWeeks"/>.</summary>
    public const int HalfMarathonExactStandaloneCoreSupportedWeeks = 14;

    /// <summary>
    /// Complete weeks available between <paramref name="startDate"/> and
    /// <paramref name="raceDate"/>. The canonical decision retains any
    /// partial days separately and never rounds this value upward.
    /// </summary>
    public static int CalculateAvailableWeeks(DateOnly startDate, DateOnly raceDate) =>
        Decide(startDate, raceDate).AvailableFullWeeks;

    internal static CoreHorizonDecision Decide(DateOnly startDate, DateOnly raceDate) =>
        Decide(startDate, raceDate, MinimumSupportedStandaloneWeeks,
            ExactStandaloneCoreSupportedWeeks, MaximumSupportedStandaloneWeeks);

    /// <summary>
    /// HM.18 Blocker 1 — the single distance-aware entry point the early
    /// fail-closed gate in <c>PlanServices.GeneratePreviewAsync</c> now uses,
    /// replacing its previous unconditional call to the parameterless,
    /// TEN_K-hardcoded <see cref="Decide(DateOnly,DateOnly)"/>. For every
    /// <paramref name="distance"/> other than
    /// <see cref="GoalDistance.HalfMarathon"/> this delegates to the exact
    /// same parameterless overload — same bounds, same classification, same
    /// exceptions — so TEN_K (and every other non-HM distance) is byte-identical,
    /// zero-delta. Only <see cref="GoalDistance.HalfMarathon"/> now resolves
    /// its own real 10/14/16-week bounds instead of borrowing TEN_K's 8/12/14.
    /// </summary>
    internal static CoreHorizonDecision DecideForDistance(DateOnly startDate, DateOnly raceDate, GoalDistance distance) =>
        distance switch
        {
            GoalDistance.HalfMarathon => Decide(
                startDate, raceDate,
                HalfMarathonMinimumSupportedStandaloneWeeks,
                HalfMarathonExactStandaloneCoreSupportedWeeks,
                HalfMarathonMaximumSupportedStandaloneWeeks),
            _ => Decide(startDate, raceDate),
        };

    /// <summary>
    /// HM.18 Blocker 1 — advisory-only "if you started earlier, this exact
    /// race date would fit the standalone core" date. This is a forward
    /// projection from RaceDate by a whole number of weeks (never a reverse
    /// derivation of the elapsed-day/full-week split <see cref="CoreHorizonClassifier"/>
    /// owns), so it can never disagree with that classifier's own calculation.
    /// Never used to mutate a request's own StartDate — advisory metadata only.
    /// </summary>
    internal static DateOnly RecommendedStartDateForMaximumWeeks(DateOnly raceDate, int maximumSupportedStandaloneWeeks) =>
        raceDate.AddDays(-(maximumSupportedStandaloneWeeks * 7));

    internal static CoreHorizonDecision Decide(
        DateOnly startDate,
        DateOnly raceDate,
        int minimumCoreWeeks,
        int preferredCoreWeeks,
        int maximumCoreWeeks) =>
        CoreHorizonClassifier.Classify(new CoreHorizonContext(
            startDate, raceDate, minimumCoreWeeks, preferredCoreWeeks, maximumCoreWeeks));

    internal static RaceHorizonClassification Classify(CoreHorizonDecision decision) =>
        decision.Mode switch
        {
            CoreHorizonMode.Unsupported or CoreHorizonMode.ReadinessOnly => RaceHorizonClassification.BelowMinimum,
            CoreHorizonMode.CompressedCore or CoreHorizonMode.ExtendedCore => RaceHorizonClassification.StandaloneCoreSupported,
            CoreHorizonMode.PreferredCore => RaceHorizonClassification.ExactStandaloneCoreSupported,
            CoreHorizonMode.PreparationRunwayPlusCore => RaceHorizonClassification.CompositionRequired,
            CoreHorizonMode.InvalidInput => RaceHorizonClassification.BelowMinimum,
            _ => throw new ArgumentOutOfRangeException(nameof(decision)),
        };

    /// <summary>
    /// Classifies <paramref name="availableWeeks"/> against the currently
    /// implemented standalone core-generation capability. The single
    /// canonical decision — every layer (routing policy, PlanServices guard,
    /// defensive alignment validator) must derive its behavior from this,
    /// never a re-derived range check.
    /// </summary>
    public static RaceHorizonClassification Classify(int availableWeeks)
    {
        var anchor = new DateOnly(2000, 1, 1);
        return Classify(Decide(anchor, anchor.AddDays(checked(availableWeeks * 7))));
    }

    /// <summary>
    /// True when <paramref name="availableWeeks"/> falls within the nominal
    /// standalone race-cycle range (inclusive) — NOT the same as "safely
    /// generatable"; see <see cref="Classify"/> for the precise decision.
    /// </summary>
    public static bool IsWithinSupportedStandaloneRange(int availableWeeks) =>
        availableWeeks >= MinimumSupportedStandaloneWeeks && availableWeeks <= MaximumSupportedStandaloneWeeks;

    /// <summary>
    /// True when the horizon exceeds the approved standalone maximum and
    /// therefore requires not-yet-implemented preparation + race-core
    /// composition. Does not classify below-minimum horizons — that is a
    /// separate, pre-existing concern this policy does not change.
    /// </summary>
    public static bool RequiresLongHorizonComposition(int availableWeeks) =>
        availableWeeks > MaximumSupportedStandaloneWeeks;

    /// <summary>
    /// Deterministic, testable reason code for a
    /// <see cref="RaceHorizonClassification.CoreLengthRecognizedButNotImplemented"/>
    /// horizon — never hard-coded per request, always derived from the
    /// actual computed week count.
    /// </summary>
    public static string GetCoreHorizonUnsupportedReasonCode(int availableWeeks) =>
        $"CORE_HORIZON_{availableWeeks}_NOT_IMPLEMENTED";
}
