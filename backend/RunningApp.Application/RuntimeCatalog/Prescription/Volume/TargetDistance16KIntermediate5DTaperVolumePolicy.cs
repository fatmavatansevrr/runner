namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.20 — freezes the two taper-week volume multipliers for the
/// dark <c>16K × Intermediate × 5D</c> cell's reused 2-week Taper phase (the
/// same frequency-generic Foundation/Build/RaceSpecific/Taper=2 structure
/// 16K's own 3D/4D cells — and every other HM-parented cell — uses, per
/// DIST-GEN.19 §15/§23). Both multipliers are applied INDEPENDENTLY against
/// the fixed pre-taper reference (this cell's own frozen
/// <see cref="ResolvedPeakReferenceKm"/> = 51.0) — never chained week-to-week
/// — via the same generic
/// <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/> non-chained
/// taper branch every other named 2-week-taper HM-parented policy already
/// uses.
///
/// <b>The two multipliers below REFERENCE <see cref="HalfMarathonParentTaperAuthority"/>,
/// never a per-cell literal</b> — DIST-GEN.19 §27/§61 explicitly classified
/// taper duration/multipliers/mechanism as <c>PARENT_FAMILY_ONLY</c> authority
/// for this exact cell ("16K × Intermediate × 5D reuses the identical
/// HM-parent taper 16K's own 3D/4D cells already consume. No cell-specific
/// taper is created."), and DIST-GEN.19A §24 re-confirms this row unchanged.
/// This mirrors <see cref="TargetDistance16KIntermediate3DTaperVolumePolicy"/>'s
/// own DIST-GEN.16 wiring exactly.
///
/// At the frozen 51.0km reference peak: 51.0 × 0.70 = 35.7, rounded to the
/// 0.5km grid at runtime = 35.5km; 51.0 × 0.43 = 21.93, rounded = 22.0km
/// (DIST-GEN.19A §33's own verified feasibility trajectory).
///
/// <see cref="ResolvedPeakReferenceKm"/> stays EXACT-CELL and is deliberately
/// NOT normalized (DIST-GEN.19A §16's own <c>SelectedPeakReference</c> row):
/// 51.0 is this cell's own frozen exact-cell peak — genuinely different from
/// 16K's own 3D cell's 34.0 and 4D cell's 40.0, and numerically equal to (but
/// never referencing or falling back to) canonical HM I5D's own 51.0.
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonIntermediate5DTargetDistance16K"/>
/// consumes these values. No public routing/gate is widened by that named
/// policy's existence.
/// </summary>
internal static class TargetDistance16KIntermediate5DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ~30% reduction. Referenced from the HM-parent authority, value unchanged at 0.70.</summary>
    public const double TaperWeek1VolumeMultiplier = HalfMarathonParentTaperAuthority.TaperWeek1VolumeMultiplier;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ~57% reduction. Referenced from the HM-parent authority, value unchanged at 0.43.</summary>
    public const double TaperWeek2VolumeMultiplier = HalfMarathonParentTaperAuthority.TaperWeek2VolumeMultiplier;

    /// <summary>EXACT-CELL, deliberately NOT normalized: the frozen DIST-GEN.19A §16 pre-taper reference this cell's taper multipliers are computed against.</summary>
    public const double ResolvedPeakReferenceKm = 51.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>. The one shared HM-parent sequence instance, so provenance is provable by reference, not only by value.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = HalfMarathonParentTaperAuthority.OrderedMultipliers;
}
