namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Phase HM-X1.3 -- freezes the two taper-week volume multipliers for
/// <c>HALF_MARATHON × INTERMEDIATE × 6D</c>'s reused 2-week Taper phase (the
/// same frequency-generic Foundation/Build/RaceSpecific/Taper=2 structure
/// every other HM cell uses, per HM.4/HM.6). Both values are applied
/// INDEPENDENTLY against the fixed pre-taper reference (this cell's frozen
/// <c>ResolvedPeakReferenceKm = 51.0</c>, HM-X1.2 §12) — never chained
/// week-to-week — via the same generic
/// <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/> non-chained
/// taper branch and <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>
/// mechanism 3D/4D/5D already use.
///
/// <b>Values are reused directly from <see cref="HalfMarathonIntermediate5DTaperVolumePolicy"/>,
/// not re-derived</b> — HM-X1.2 §22 re-confirmed the same four evidence
/// sources (Mujika &amp; Padilla meta-analyses, Marathon Handbook's HM taper
/// guide, best-running-tips.com, runtothefinish.com) are volume/proximity-based,
/// not frequency-scoped, and this cell's own peak (51.0km, unchanged from 5D
/// per HM-X1.2 §10/§12's own band-width-stability finding) sits below
/// best-running-tips.com's own named lighter-taper-exception threshold
/// (~56 km/week), so the standard reduction regime applies — no exception,
/// no new derivation. Classification: <c>EVIDENCE_INFORMED_PRODUCT_DEFAULT</c>
/// (reused with re-verified scope, not blind numeric copy).
///
/// <see cref="TaperWeek1VolumeMultiplier"/> = 0.70 (≈30% reduction).
/// <see cref="TaperWeek2VolumeMultiplier"/> = 0.43 (≈57% reduction, final/
/// deepest week). At the frozen 51.0km reference peak: 51.0 → 35.7 →
/// 21.93km — byte-identical numerically to Intermediate 5D's own taper
/// trace, since the peak did not move (HM-X1.2 §23 confirms this remains
/// feasible against the six-session floor: 2×3km KEY + 3×1.5km EASY = 10.5km,
/// leaving ~11.4km for LONG at Week 2, comfortably non-degenerate).
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonIntermediate6D"/>
/// consumes these values together with HM-X1.2's frozen peak/growth/long-run-
/// share authority. No public routing/gate is widened by that named policy's
/// existence. See PHASE_HM_X1_3_6D_INTERMEDIATE_ADVANCED_TEN_TO_SIXTEEN_WEEK_FULL_DARK_IMPLEMENTATION.md.
/// </summary>
internal static class HalfMarathonIntermediate6DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen HM-X1.2 pre-taper reference this cell's taper multipliers are computed against (not re-decided here) — unchanged from Intermediate 5D's own 51.0km.</summary>
    public const double ResolvedPeakReferenceKm = 51.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}
