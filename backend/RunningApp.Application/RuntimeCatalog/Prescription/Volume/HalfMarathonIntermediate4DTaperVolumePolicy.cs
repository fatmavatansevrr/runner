namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Phase HM.1.4B — freezes the two taper-week volume multipliers for
/// <c>HALF_MARATHON × INTERMEDIATE × 4D × 14W</c>'s already-frozen 2-week
/// Taper phase (<c>HM.1</c>'s own Foundation 3 / Build 4 / RaceSpecific 5 /
/// Taper 2 split). Both values are applied INDEPENDENTLY against the fixed
/// pre-taper reference (this cell's already-frozen <c>ResolvedPeakReferenceKm
/// = 43.0</c>, <c>HM.1.4</c>/<c>HM.1.5</c>) — never chained week-to-week — via
/// <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/>'s new
/// non-chained taper branch and <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.
///
/// <c>HM.1.4A</c> (the immediate predecessor, audit-only) confirmed the prior
/// mechanism was <c>COMPOUNDED_MULTIPLIER</c>: chaining a single scalar twice
/// in sequence produced 43.0 → 23.0 → 12.0km, a 72.1% cumulative reduction,
/// far outside any external taper-reduction evidence envelope. This policy's
/// two values, applied non-chained, instead produce 43.0 → 30.0 → 18.5km
/// (30% then 57% reduction from the fixed 43.0km peak, each independently
/// computed) — consistent with real 2-week-taper sports-science practice.
///
/// <b>Classification: <c>EVIDENCE_INFORMED_PRODUCT_DEFAULT</c>, with
/// multi-source external convergence</b> (stronger than a single-source
/// product default; still not <c>EVIDENCE_BACKED</c> — no single pair of
/// numbers is uniquely proven optimal). Sources (recorded as previously
/// user-supplied external evidence per this engagement's established
/// pattern — not re-verified via new research this phase):
/// <list type="bullet">
/// <item>Mujika &amp; Padilla meta-analyses (2007, 2023): ~41-60% total volume
/// reduction over a ~2-week taper, intensity/frequency preserved.</item>
/// <item>Marathon Handbook's HM-specific 2-week taper guide: Week 1 ≈ 25-35%
/// reduction, Week 2 (race week) ≈ up to 60% reduction from peak.</item>
/// <item>best-running-tips.com: 40-60% standard reduction, with a
/// lighter-taper exception only for runners peaking above ~56km/week — the
/// frozen 36-50km/week band for this cell sits below that threshold, so the
/// standard range applies.</item>
/// <item>runtothefinish.com: taper week 2 up to 60% reduction from peak.</item>
/// </list>
///
/// <see cref="TaperWeek1VolumeMultiplier"/> = 0.70 (≈30% reduction, mid-range
/// of the 25-35% Week-1 envelope). <see cref="TaperWeek2VolumeMultiplier"/> =
/// 0.43 (≈57% reduction, within the 41-60%/up-to-60% Week-2 envelope,
/// consistent with race-week being the deeper cut). Both individually
/// verified against <see cref="CatalogVolumeAndLongRunPlanner.ResolveTaperDecision"/>'s
/// updated multi-week validation: the FINAL (deepest) taper week's reduction
/// must fall in the existing [41%,60%] envelope (0.57 does); every earlier
/// taper week's reduction must be strictly positive and strictly less than
/// the final week's (0.30 &lt; 0.57 does) — i.e. taper reductions must
/// deepen monotonically toward race week, never invert or match today's
/// single-step envelope in isolation.
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonIntermediate4D"/>
/// now consumes these values together with HM.1.5/HM.1.6's subsequently
/// frozen share authority. No public routing/gate is widened by that named
/// policy's existence.
/// </summary>
internal static class HalfMarathonIntermediate4DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The already-frozen pre-taper reference this cell's taper multipliers are computed against (HM.1.4/HM.1.5's own <c>ResolvedPeakReferenceKm</c>, not re-decided here).</summary>
    public const double ResolvedPeakReferenceKm = 43.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}

/// <summary>
/// Phase HM.8 — freezes the two taper-week volume multipliers for
/// <c>HALF_MARATHON × INTERMEDIATE × 3D</c>'s reused 2-week Taper phase
/// (the same frequency-generic Foundation/Build/RaceSpecific/Taper=2
/// structure 4D uses, per HM.4/HM.6). Both values are applied
/// INDEPENDENTLY against the fixed pre-taper reference (this cell's frozen
/// <c>ResolvedPeakReferenceKm = 34.0</c>, HM.7) — never chained week-to-week
/// — via the same generic <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/>
/// non-chained taper branch and <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>
/// mechanism 4D already uses.
///
/// <b>Values are reused directly from <see cref="HalfMarathonIntermediate4DTaperVolumePolicy"/>,
/// not re-derived</b> — HM.7 §12 re-read that policy's own four evidence
/// sources (Mujika &amp; Padilla meta-analyses, Marathon Handbook's HM taper
/// guide, best-running-tips.com, runtothefinish.com) directly and confirmed
/// none of them are frequency-scoped: they describe volume-reduction as a
/// function of total training load and race proximity, not running
/// frequency. 3D's own peak band (28–40 km/week, selected 34.0) sits well
/// below best-running-tips.com's own named lighter-taper-exception threshold
/// (~56 km/week), so the same standard reduction regime applies — no
/// exception, no new derivation. Classification: <c>EVIDENCE_INFORMED_PRODUCT_DEFAULT</c>
/// (reused with proven, re-verified scope, not blind numeric copy).
///
/// <see cref="TaperWeek1VolumeMultiplier"/> = 0.70 (≈30% reduction).
/// <see cref="TaperWeek2VolumeMultiplier"/> = 0.43 (≈57% reduction, final/
/// deepest week). At the frozen 34.0km reference peak: 34.0 → 23.8→24.0 →
/// 14.62→14.5km (rounded to the 0.5km grid).
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonIntermediate3D"/>
/// consumes these values together with HM.7's frozen peak/growth/long-run-
/// share authority. No public routing/gate is widened by that named
/// policy's existence. See PHASE_HM_8_INTERMEDIATE_3D_TEN_TO_SIXTEEN_WEEK_FULL_DARK_IMPLEMENTATION.md.
/// </summary>
internal static class HalfMarathonIntermediate3DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen HM.7 pre-taper reference this cell's taper multipliers are computed against (not re-decided here).</summary>
    public const double ResolvedPeakReferenceKm = 34.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}
