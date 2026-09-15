namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Phase HM-X1.3 -- freezes the two taper-week volume multipliers for
/// <c>HALF_MARATHON × ADVANCED × 6D</c>'s reused 2-week Taper phase. Values
/// reused directly from HM-X1.2 §22's own frozen decision -- 0.70/0.43
/// reused unchanged, per explicit instruction not to reinterpret or replace
/// them.
///
/// <b>Genuine disclosed evidence tension (HM-X1.2 §22, NOT resolved by this
/// implementation phase, only implemented as frozen):</b> this cell's own
/// selected peak (63.0km/week) sits even further past best-running-tips.com's
/// own named ~56km/week "lighter taper" exception threshold than Advanced
/// 5D's own 62.0km/week already did (HM.15 §19/HM.16's own disclosed
/// tension). The source's own guidance is qualitative ("lighter taper") with
/// no exact alternative percentage ever cited in this engagement's evidence
/// record, so no new number is invented here. 0.70/0.43 is frozen as the only
/// concrete value available -- potentially sub-optimal (a deeper taper
/// reduction is not itself unsafe) but not unsafe; this phase's own test
/// suite verifies this taper depth still produces a feasible, non-degenerate
/// six-session prescription (HM-X1.2 §23: Week 2 total 27.09km against a
/// 10.5km 2×KEY+3×EASY floor, leaving ~16.6km for LONG) rather than assuming
/// it does.
///
/// <see cref="TaperWeek1VolumeMultiplier"/> = 0.70 (≈30% reduction).
/// <see cref="TaperWeek2VolumeMultiplier"/> = 0.43 (≈57% reduction, final/
/// deepest week). At the frozen 63.0km reference peak: 63.0 → 44.1 →
/// 27.09km. Note: Taper-phase KEY2 reverts to EASY_EQUIVALENT (HM-X1.2 §3/§4,
/// mirroring HM.15 §7) -- Taper W2's own reduced volume is never asked to
/// carry two true-hard KEY sessions in the same week.
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonAdvanced6D"/> consumes
/// these values together with HM-X1.2's frozen peak/growth/long-run-share
/// authority. No public routing/gate is widened by that named policy's
/// existence. See PHASE_HM_X1_3_6D_INTERMEDIATE_ADVANCED_TEN_TO_SIXTEEN_WEEK_FULL_DARK_IMPLEMENTATION.md.
/// </summary>
internal static class HalfMarathonAdvanced6DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen HM-X1.2 pre-taper reference this cell's taper multipliers are computed against (not re-decided here).</summary>
    public const double ResolvedPeakReferenceKm = 63.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}
