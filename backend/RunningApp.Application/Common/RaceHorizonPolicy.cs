using System;

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
    /// weeks — the only core length currently proven to produce a
    /// race-date-aligned standalone plan. Generation may proceed.
    /// </summary>
    ExactStandaloneCoreSupported,

    /// <summary>
    /// Horizon falls within the nominal standalone core range
    /// (<see cref="RaceHorizonPolicy.MinimumSupportedStandaloneWeeks"/>..<see cref="RaceHorizonPolicy.MaximumSupportedStandaloneWeeks"/>)
    /// but is not the exact proven-safe length — the current catalog phase
    /// allocator is not horizon-aware and always emits its fixed default
    /// (12-week) allocation regardless of this horizon, so generating here
    /// would silently misalign with RaceDate. Temporary safety constraint —
    /// see PHASE4G_2_EXACT_TWELVE_WEEK_ONLY_SAFETY_CONSTRAINT.md.
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
/// TEMPORARY SAFETY CONSTRAINT: horizon-aware 8-14-week core allocation is
/// not yet implemented. The catalog phase allocator
/// (<c>CatalogPhaseAllocationResolver.Resolve</c>) always emits its fixed
/// default ~12-week allocation regardless of the accepted cycle length —
/// verified live for both an 8-week horizon (plan overshoots the race by
/// ~4 weeks) and a 20-week horizon (plan falls ~8 weeks short). Until true
/// horizon-aware composition exists, only the exact 12-week horizon — the
/// one length proven to align with RaceDate — is generated; every other
/// horizon in the nominal 8-14 range fails closed, and horizons above 14
/// continue to fail closed as "composition required". This is not the final
/// product behavior — see
/// PHASE4G_1_LONG_HORIZON_FAIL_CLOSED_SAFETY_CONSTRAINT.md and
/// PHASE4G_2_EXACT_TWELVE_WEEK_ONLY_SAFETY_CONSTRAINT.md.
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
    /// The only standalone core length currently proven (live-verified) to
    /// produce a race-date-aligned plan. Every other horizon within
    /// [<see cref="MinimumSupportedStandaloneWeeks"/>, <see cref="MaximumSupportedStandaloneWeeks"/>]
    /// is nominally "in range" per the candidate's coreCycle bounds but is
    /// NOT yet safely generatable — see this class's own remarks.
    /// </summary>
    public const int ExactStandaloneCoreSupportedWeeks = 12;

    /// <summary>
    /// Whole weeks available between <paramref name="startDate"/> and
    /// <paramref name="raceDate"/>, rounded up — a partial trailing week
    /// still counts as a full week of available preparation time. Mirrors
    /// the day-count/7 ceiling convention already used elsewhere in this
    /// codebase for week-count math.
    /// </summary>
    public static int CalculateAvailableWeeks(DateOnly startDate, DateOnly raceDate) =>
        (int)Math.Ceiling((raceDate.DayNumber - startDate.DayNumber) / 7d);

    /// <summary>
    /// Classifies <paramref name="availableWeeks"/> against the currently
    /// implemented standalone core-generation capability. The single
    /// canonical decision — every layer (routing policy, PlanServices guard,
    /// defensive alignment validator) must derive its behavior from this,
    /// never a re-derived range check.
    /// </summary>
    public static RaceHorizonClassification Classify(int availableWeeks)
    {
        if (availableWeeks < MinimumSupportedStandaloneWeeks) return RaceHorizonClassification.BelowMinimum;
        if (availableWeeks > MaximumSupportedStandaloneWeeks) return RaceHorizonClassification.CompositionRequired;
        return availableWeeks == ExactStandaloneCoreSupportedWeeks
            ? RaceHorizonClassification.ExactStandaloneCoreSupported
            : RaceHorizonClassification.CoreLengthRecognizedButNotImplemented;
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
