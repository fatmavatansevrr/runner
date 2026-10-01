using System;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;

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
    public static ArcSelectionTrace BuildTrace(
        DerivedDistanceEligibilityPolicy.DerivedDistanceCell cell,
        NearestHigherCanonicalPlanResolver.CanonicalPlan source,
        CoreHorizonDecision decision,
        int? distanceMilestoneWeek = null)
    {
        var taperWeeks = HalfMarathonParentTaperAuthority.TaperDurationWeeks;
        var bodyEndWeek = decision.AvailableFullWeeks - taperWeeks;

        return new ArcSelectionTrace(
            RequestedTargetDistanceKm: cell.TargetDistanceKm,
            SourceCanonicalFamily: source.Family,
            SourceCanonicalDistanceKm: source.DistanceKm,
            MinimumCoreWeeks: DerivedDistanceHorizonAuthority.MinimumCoreWeeks,
            PreferredCoreWeeks: DerivedDistanceHorizonAuthority.PreferredCoreWeeks,
            MaximumCoreWeeks: DerivedDistanceHorizonAuthority.MaximumCoreWeeks,
            HorizonMode: decision.Mode,
            AvailableFullWeeks: decision.AvailableFullWeeks,
            TaperDurationWeeks: taperWeeks,
            ChosenBodyEndWeek: bodyEndWeek,
            ReasonForCutoff:
                "REUSED_CONVERGENT_HORIZON_TRIPLE_10_12_14 (DerivedDistanceHorizonAuthority) bounds the " +
                "RaceHorizonPolicy.Decide outcome; the catalog's own generic two-argument phase-allocation resolver (candidate, AvailableFullWeeks) " +
                "then mechanically compresses/extends HALF_MARATHON's own catalog-declared Foundation/Build/" +
                "RaceSpecific/Taper allocation to that exact week count, never below any phase's own catalog " +
                "minimum -- this is never a 'first week long run >= target' cutoff.",
            DistanceMilestoneWeek: distanceMilestoneWeek);
    }
}
