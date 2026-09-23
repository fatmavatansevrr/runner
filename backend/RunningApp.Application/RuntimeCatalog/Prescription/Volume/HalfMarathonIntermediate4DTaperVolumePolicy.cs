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
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction. DIST-GEN.13: referenced from the HM-parent authority, value unchanged at 0.70.</summary>
    public const double TaperWeek1VolumeMultiplier = HalfMarathonParentTaperAuthority.TaperWeek1VolumeMultiplier;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction. DIST-GEN.13: referenced from the HM-parent authority, value unchanged at 0.43.</summary>
    public const double TaperWeek2VolumeMultiplier = HalfMarathonParentTaperAuthority.TaperWeek2VolumeMultiplier;

    /// <summary>EXACT-CELL, deliberately NOT normalized: the already-frozen pre-taper reference this cell's taper multipliers are computed against (HM.1.4/HM.1.5's own <c>ResolvedPeakReferenceKm</c>, not re-decided here, and genuinely different from every projected target's own 40.0).</summary>
    public const double ResolvedPeakReferenceKm = 43.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>. DIST-GEN.13: the one shared HM-parent sequence instance.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = HalfMarathonParentTaperAuthority.OrderedMultipliers;
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

/// <summary>
/// Phase HM.11 -- freezes the two taper-week volume multipliers for
/// <c>HALF_MARATHON × INTERMEDIATE × 5D</c>'s reused 2-week Taper phase (the
/// same frequency-generic Foundation/Build/RaceSpecific/Taper=2 structure
/// 4D/3D use, per HM.4/HM.6). Both values are applied INDEPENDENTLY against
/// the fixed pre-taper reference (this cell's frozen
/// <c>ResolvedPeakReferenceKm = 51.0</c>, HM.10) — never chained week-to-week
/// — via the same generic <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/>
/// non-chained taper branch and <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>
/// mechanism 4D/3D already use.
///
/// <b>Values are reused directly from <see cref="HalfMarathonIntermediate4DTaperVolumePolicy"/>/
/// <see cref="HalfMarathonIntermediate3DTaperVolumePolicy"/>, not re-derived</b>
/// — HM.10 §12 re-confirmed the same four evidence sources (Mujika &amp;
/// Padilla meta-analyses, Marathon Handbook's HM taper guide,
/// best-running-tips.com, runtothefinish.com) are volume/proximity-based,
/// not frequency-scoped, and this cell's own peak (51.0km) sits below
/// best-running-tips.com's own named lighter-taper-exception threshold
/// (~56 km/week), so the standard reduction regime applies — no exception,
/// no new derivation. Classification: <c>EVIDENCE_INFORMED_PRODUCT_DEFAULT</c>
/// (reused with re-verified scope, not blind numeric copy).
///
/// <see cref="TaperWeek1VolumeMultiplier"/> = 0.70 (≈30% reduction).
/// <see cref="TaperWeek2VolumeMultiplier"/> = 0.43 (≈57% reduction, final/
/// deepest week). At the frozen 51.0km reference peak: 51.0 → 35.7→35.5 →
/// 21.93→22.0km (rounded to the 0.5km grid).
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonIntermediate5D"/>
/// consumes these values together with HM.10's frozen peak/growth/long-run-
/// share authority. No public routing/gate is widened by that named policy's
/// existence. See PHASE_HM_11_INTERMEDIATE_5D_TEN_TO_SIXTEEN_WEEK_FULL_DARK_IMPLEMENTATION.md.
/// </summary>
internal static class HalfMarathonIntermediate5DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen HM.10 pre-taper reference this cell's taper multipliers are computed against (not re-decided here).</summary>
    public const double ResolvedPeakReferenceKm = 51.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}

/// <summary>
/// Phase HM.14 -- freezes the two taper-week volume multipliers for
/// <c>HALF_MARATHON × BEGINNER × 3D</c>'s reused 2-week Taper phase (the same
/// frequency-generic Foundation/Build/RaceSpecific/Taper=2 structure every
/// other HM cell uses). Values reused directly from
/// <see cref="HalfMarathonIntermediate3DTaperVolumePolicy"/> -- HM.13 §16
/// re-confirmed the same four evidence sources (Mujika &amp; Padilla
/// meta-analyses, Marathon Handbook's HM taper guide, best-running-tips.com,
/// runtothefinish.com) are volume/proximity-based, not level-scoped, and this
/// cell's own peak (27.0km) sits further below best-running-tips.com's own
/// named lighter-taper-exception threshold (~56 km/week) than any
/// Intermediate cell does -- the standard regime applies at least as cleanly.
/// Classification: <c>EVIDENCE_INFORMED_PRODUCT_DEFAULT</c> (reused with
/// re-verified, and in this instance strengthened, scope).
///
/// <see cref="TaperWeek1VolumeMultiplier"/> = 0.70 (≈30% reduction).
/// <see cref="TaperWeek2VolumeMultiplier"/> = 0.43 (≈57% reduction, final/
/// deepest week). At the frozen 27.0km reference peak: 27.0 → 18.9→19.0 →
/// 11.61→11.5km (rounded to the 0.5km grid).
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonBeginner3D"/> consumes
/// these values together with HM.13's frozen peak/growth/long-run-share
/// authority. No public routing/gate is widened by that named policy's
/// existence.
/// </summary>
internal static class HalfMarathonBeginner3DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen HM.13 pre-taper reference this cell's taper multipliers are computed against (not re-decided here).</summary>
    public const double ResolvedPeakReferenceKm = 27.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}

/// <summary>
/// Phase HM.14 -- freezes the two taper-week volume multipliers for
/// <c>HALF_MARATHON × BEGINNER × 4D</c>'s reused 2-week Taper phase. See
/// <see cref="HalfMarathonBeginner3DTaperVolumePolicy"/>'s own doc comment for
/// the shared evidence re-verification (HM.13 §16). At the frozen 33.0km
/// reference peak: 33.0 → 23.1→23.0 → 14.19→14.0km (rounded to the 0.5km grid).
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonBeginner4D"/> consumes
/// these values together with HM.13's frozen peak/growth/long-run-share
/// authority. No public routing/gate is widened by that named policy's
/// existence.
/// </summary>
internal static class HalfMarathonBeginner4DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen HM.13 pre-taper reference this cell's taper multipliers are computed against (not re-decided here).</summary>
    public const double ResolvedPeakReferenceKm = 33.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}

/// <summary>
/// Phase HM.16 -- freezes the two taper-week volume multipliers for
/// <c>HALF_MARATHON × ADVANCED × 3D</c>'s reused 2-week Taper phase (the same
/// frequency-generic Foundation/Build/RaceSpecific/Taper=2 structure every
/// other HM cell uses). Values reused directly from HM.15 §19's own frozen
/// decision -- 0.70/0.43 reused unchanged. HM.15 §19 re-examined the same
/// four evidence sources (Mujika &amp; Padilla, Marathon Handbook, best-running-tips.com,
/// runtothefinish.com) against Advanced 3D's own frozen peak (42.0km):
/// comfortably below best-running-tips.com's own named ~56km/week
/// lighter-taper-exception threshold, so the standard regime applies cleanly,
/// same reasoning as every Intermediate/Beginner cell. Classification:
/// <c>EVIDENCE_INFORMED_PRODUCT_DEFAULT</c> (clean fit, no disclosed tension --
/// contrast <see cref="HalfMarathonAdvanced5DTaperVolumePolicy"/> below).
///
/// <see cref="TaperWeek1VolumeMultiplier"/> = 0.70 (≈30% reduction).
/// <see cref="TaperWeek2VolumeMultiplier"/> = 0.43 (≈57% reduction, final/
/// deepest week). At the frozen 42.0km reference peak: 42.0 → 29.4→29.5 →
/// 18.06→18.0km (rounded to the 0.5km grid).
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonAdvanced3D"/> consumes
/// these values together with HM.15's frozen peak/growth/long-run-share
/// authority. No public routing/gate is widened by that named policy's
/// existence.
/// </summary>
internal static class HalfMarathonAdvanced3DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen HM.15 pre-taper reference this cell's taper multipliers are computed against (not re-decided here).</summary>
    public const double ResolvedPeakReferenceKm = 42.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}

/// <summary>
/// Phase HM.16 -- freezes the two taper-week volume multipliers for
/// <c>HALF_MARATHON × ADVANCED × 4D</c>'s reused 2-week Taper phase. Values
/// reused directly from HM.15 §19's own frozen decision -- 0.70/0.43 reused
/// unchanged. HM.15 §19 disclosed a real, non-blocking evidence tension for
/// this cell: its own PeakVolumeBand ceiling (60.0km) exceeds
/// best-running-tips.com's own named ~56km/week lighter-taper-exception
/// threshold, though this cell's own SELECTED peak (53.0km) does not. No
/// exact alternative percentage exists anywhere in this engagement's evidence
/// base for the "lighter taper" exception, so 0.70/0.43 is retained as the
/// only concrete number available -- not because the evidence cleanly
/// supports it at this cell's own band ceiling. Classification:
/// <c>EVIDENCE_INFORMED_PRODUCT_DEFAULT</c>, with a disclosed tension at the
/// band ceiling (not at the selected peak itself).
///
/// <see cref="TaperWeek1VolumeMultiplier"/> = 0.70 (≈30% reduction).
/// <see cref="TaperWeek2VolumeMultiplier"/> = 0.43 (≈57% reduction, final/
/// deepest week). At the frozen 53.0km reference peak: 53.0 → 37.1→37.0 →
/// 22.79→23.0km (rounded to the 0.5km grid).
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonAdvanced4D"/> consumes
/// these values together with HM.15's frozen peak/growth/long-run-share
/// authority. No public routing/gate is widened by that named policy's
/// existence.
/// </summary>
internal static class HalfMarathonAdvanced4DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen HM.15 pre-taper reference this cell's taper multipliers are computed against (not re-decided here).</summary>
    public const double ResolvedPeakReferenceKm = 53.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}

/// <summary>
/// Phase HM.16 -- freezes the two taper-week volume multipliers for
/// <c>HALF_MARATHON × ADVANCED × 5D</c>'s reused 2-week Taper phase. Values
/// reused directly from HM.15 §19's own frozen decision -- 0.70/0.43 reused
/// unchanged, per explicit instruction not to reinterpret or replace them.
///
/// <b>Genuine disclosed evidence tension (HM.15 §19/§30 item 2, NOT resolved
/// by this implementation phase, only implemented as frozen):</b> this cell's
/// own selected peak (62.0km/week) itself EXCEEDS best-running-tips.com's own
/// named ~56km/week "lighter taper" exception threshold -- the first HM cell
/// where the evidence source's own stated exception condition is genuinely
/// triggered by the frozen selected-peak value itself, not merely an edge of
/// its band. The source's own guidance is qualitative ("lighter taper") with
/// no exact alternative percentage ever cited in this engagement's evidence
/// record, so no new number is invented here. 0.70/0.43 is frozen as the only
/// concrete value available -- potentially sub-optimal (a deeper taper
/// reduction is not itself unsafe) but not unsafe; HM.16's own test suite
/// verifies this taper depth still produces a feasible, non-degenerate
/// prescription (both KEY roles, including the genuinely-hard KEY2 which
/// reverts to EASY_EQUIVALENT in Taper anyway per HM.15 §7, still receive
/// real dose at Taper W2 volume) rather than assuming it does.
///
/// <see cref="TaperWeek1VolumeMultiplier"/> = 0.70 (≈30% reduction).
/// <see cref="TaperWeek2VolumeMultiplier"/> = 0.43 (≈57% reduction, final/
/// deepest week). At the frozen 62.0km reference peak: 62.0 → 43.4→43.5 →
/// 26.66→26.5km (rounded to the 0.5km grid). Note: Taper-phase KEY2 reverts to
/// EASY_EQUIVALENT (HM.15 §7) -- Taper W2's own reduced volume is never asked
/// to carry two true-hard KEY sessions in the same week.
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonAdvanced5D"/> consumes
/// these values together with HM.15's frozen peak/growth/long-run-share
/// authority. No public routing/gate is widened by that named policy's
/// existence.
/// </summary>
internal static class HalfMarathonAdvanced5DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen HM.15 pre-taper reference this cell's taper multipliers are computed against (not re-decided here).</summary>
    public const double ResolvedPeakReferenceKm = 62.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}
