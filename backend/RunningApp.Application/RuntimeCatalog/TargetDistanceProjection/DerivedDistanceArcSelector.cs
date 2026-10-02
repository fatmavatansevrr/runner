using System;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DERIVED-DIST.0 — the auditable arc-selection trace for one derived
/// plan (governing prompt §59/§60). Deliberately does NOT itself slice a
/// longer HM plan into a prefix: the actual body-length selection happens for
/// free, generically, through the already-existing, already-governed
/// generation seam this prototype reuses rather than reimplements —
/// <see cref="RaceHorizonPolicy"/>'s existing <c>Decide</c> engine (bounded by
/// <see cref="DerivedDistanceHorizonAuthority"/>'s own triple) resolves
/// <c>AvailableFullWeeks</c>, which becomes the DynamicCore pipeline's own
/// <c>TargetWeekCount</c>; the catalog's own generic, two-argument
/// mechanical phase-allocation resolver (candidate plus target week count —
/// Backend Integration Phase 4G.3B.2, pre-existing, unmodified) then
/// mechanically compresses/extends HALF_MARATHON's own
/// catalog-declared Foundation/Build/RaceSpecific/Taper allocation to fit
/// that week count, NEVER below any phase's own catalog-declared minimum —
/// which is exactly the "minimum meaningful full arc" invariant governing
/// prompt §14/§17/§18 require, already enforced by pre-existing, unmodified
/// catalog authority, not by anything this type computes. This type exists
/// so that fact is auditable and traceable per request, not so it can
/// recompute it.
/// </summary>
public static class DerivedDistanceArcSelector
{
    /// <summary>
    /// One derived plan's complete arc-selection trace (governing prompt
    /// §60/§71). <see cref="DistanceMilestoneWeek"/> is nullable because it is
    /// only knowable AFTER the real long-run progression has been generated
    /// (it is a fact about the realized long-run plan, never a precomputed
    /// input to body-length selection — governing prompt §13 forbids using it
    /// as the SOLE cutoff rule, and this prototype does not use it as any
    /// cutoff input at all: the cutoff is the horizon triple plus the
    /// catalog's own phase-allocation minimums, resolved before any long run
    /// is planned).
    /// </summary>
    public sealed record ArcSelectionTrace(
        double RequestedTargetDistanceKm,
        string SourceCanonicalFamily,
        double SourceCanonicalDistanceKm,
        int MinimumCoreWeeks,
        int PreferredCoreWeeks,
        int MaximumCoreWeeks,
        CoreHorizonMode HorizonMode,
        int AvailableFullWeeks,
        int TaperDurationWeeks,
        int ChosenBodyEndWeek,
        string ReasonForCutoff,
        int? DistanceMilestoneWeek = null);

    /// <summary>
    /// Builds the trace for one resolved derived cell and its already-computed
    /// <see cref="CoreHorizonDecision"/>. <paramref name="distanceMilestoneWeek"/>
    /// — the first pre-taper week whose realized long run reaches or exceeds
    /// the requested target, if any such week exists in the generated plan —
    /// is supplied by the caller AFTER real generation, purely for audit/trace
    /// purposes (governing prompt §16: an input to understanding the cutoff,
    /// never to computing it).
    /// </summary>
    /// <summary>
    /// PHASE DERIVED-DIST.4 §41 — audit-trace-only taper-week-count per
    /// resolved parent family. HALF_MARATHON's own real 2-week taper comes
    /// from the already-governed, already-shared
    /// <see cref="HalfMarathonParentTaperAuthority.TaperDurationWeeks"/> —
    /// unchanged. TEN_K's own real taper is genuinely different (that type's
    /// own doc comment explicitly names it: "a single taper week at a 0.53
    /// multiplier", and TEN_K is explicitly, deliberately NOT a consumer of
    /// <see cref="HalfMarathonParentTaperAuthority"/>) — this reads that fact
    /// directly off TEN_K's own real, already-governed base authority object
    /// (<see cref="VolumeSafetyPolicy.Default"/>'s own
    /// <see cref="VolumeSafetyPolicy.ResolvedTaperVolumeMultipliers"/> count)
    /// rather than re-declaring a second, independent "1" literal here. This
    /// field is audit/trace metadata only (see this method's own doc
    /// comment) — the REAL taper-week count real generation uses is always
    /// read from the catalog-bound plan's own materialized TAPER-phase weeks,
    /// never from this trace.
    /// </summary>
    private static int TaperDurationWeeksFor(GoalDistance parentDistanceFamily) => parentDistanceFamily switch
    {
        GoalDistance.HalfMarathon => HalfMarathonParentTaperAuthority.TaperDurationWeeks,
        GoalDistance.TenK => VolumeSafetyPolicy.Default.ResolvedTaperVolumeMultipliers.Count,
        _ => throw new ArgumentOutOfRangeException(nameof(parentDistanceFamily), parentDistanceFamily, "No derived-distance taper authority is governed for this parent family."),
    };

    public static ArcSelectionTrace BuildTrace(
        DerivedDistanceEligibilityPolicy.DerivedDistanceCell cell,
        NearestHigherCanonicalPlanResolver.CanonicalPlan source,
        CoreHorizonDecision decision,
        int? distanceMilestoneWeek = null)
    {
        // PHASE DERIVED-DIST.4 §41 -- both the taper-week count and the
        // Minimum/Preferred/MaximumCoreWeeks reported in this trace are now
        // selected by the resolved cell's own ParentDistanceFamily instead of
        // being unconditionally HALF_MARATHON's. For a HALF_MARATHON-sourced
        // cell this resolves to exactly HalfMarathonParentTaperAuthority's
        // 2 weeks and DerivedDistanceHorizonAuthority's 10/12/14 triple --
        // byte-identical to the pre-DERIVED-DIST.4 trace for every existing
        // live request.
        var taperWeeks = TaperDurationWeeksFor(cell.ParentDistanceFamily);
        var bodyEndWeek = decision.AvailableFullWeeks - taperWeeks;
        var minimumCoreWeeks = cell.ParentDistanceFamily == GoalDistance.TenK
            ? DerivedDistanceHorizonAuthority.TenKMinimumCoreWeeks
            : DerivedDistanceHorizonAuthority.MinimumCoreWeeks;
        var preferredCoreWeeks = cell.ParentDistanceFamily == GoalDistance.TenK
            ? DerivedDistanceHorizonAuthority.TenKPreferredCoreWeeks
            : DerivedDistanceHorizonAuthority.PreferredCoreWeeks;
        var maximumCoreWeeks = cell.ParentDistanceFamily == GoalDistance.TenK
            ? DerivedDistanceHorizonAuthority.TenKMaximumCoreWeeks
            : DerivedDistanceHorizonAuthority.MaximumCoreWeeks;

        return new ArcSelectionTrace(
            RequestedTargetDistanceKm: cell.TargetDistanceKm,
            SourceCanonicalFamily: source.Family,
            SourceCanonicalDistanceKm: source.DistanceKm,
            MinimumCoreWeeks: minimumCoreWeeks,
            PreferredCoreWeeks: preferredCoreWeeks,
            MaximumCoreWeeks: maximumCoreWeeks,
            HorizonMode: decision.Mode,
            AvailableFullWeeks: decision.AvailableFullWeeks,
            TaperDurationWeeks: taperWeeks,
            ChosenBodyEndWeek: bodyEndWeek,
            ReasonForCutoff:
                cell.ParentDistanceFamily == GoalDistance.TenK
                    ? "REUSED_TEN_K_OWN_HORIZON_TRIPLE_8_12_14 (DerivedDistanceHorizonAuthority.TenK*) bounds the " +
                      "RaceHorizonPolicy.Decide outcome; the catalog's own generic two-argument phase-allocation resolver (candidate, AvailableFullWeeks) " +
                      "then mechanically compresses/extends TEN_K's own catalog-declared Foundation/Build/" +
                      "RaceSpecific/Taper allocation to that exact week count, never below any phase's own catalog " +
                      "minimum -- this is never a 'first week long run >= target' cutoff."
                    : "REUSED_CONVERGENT_HORIZON_TRIPLE_10_12_14 (DerivedDistanceHorizonAuthority) bounds the " +
                      "RaceHorizonPolicy.Decide outcome; the catalog's own generic two-argument phase-allocation resolver (candidate, AvailableFullWeeks) " +
                      "then mechanically compresses/extends HALF_MARATHON's own catalog-declared Foundation/Build/" +
                      "RaceSpecific/Taper allocation to that exact week count, never below any phase's own catalog " +
                      "minimum -- this is never a 'first week long run >= target' cutoff.",
            DistanceMilestoneWeek: distanceMilestoneWeek);
    }
}
