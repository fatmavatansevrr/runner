namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.16 — freezes the two taper-week volume multipliers for the
/// dark <c>16K × Intermediate × 3D</c> cell's reused 2-week Taper phase (the
/// same frequency-generic Foundation/Build/RaceSpecific/Taper=2 structure
/// 16K's own 4D cell — and every other HM-parented cell — uses, per
/// DIST-GEN.15 §14/§34). Both multipliers are applied INDEPENDENTLY against
/// the fixed pre-taper reference (this cell's own frozen
/// <see cref="ResolvedPeakReferenceKm"/> = 34.0) — never chained week-to-week
/// — via the same generic
/// <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/> non-chained
/// taper branch every other named 2-week-taper HM-parented policy already
/// uses.
///
/// <b>The two multipliers below REFERENCE <see cref="HalfMarathonParentTaperAuthority"/>,
/// never a per-cell literal</b> — DIST-GEN.15 §34 explicitly classified taper
/// duration/multipliers/mechanism as <c>PARENT_FAMILY_ONLY</c> authority for
/// this exact cell ("16K × Intermediate × 3D consumes the identical HM-parent
/// taper authority 16K's own 4D cell already consumes. No cell-specific taper
/// is created."), and DIST-GEN.15B §7 re-confirms this row unchanged. This
/// mirrors <see cref="TargetDistance16KTaperVolumePolicy"/>'s own DIST-GEN.13
/// wiring exactly, giving this new cell the same provable-by-reference
/// provenance from day one, rather than the un-migrated per-class literal
/// canonical <c>HalfMarathonIntermediate3DTaperVolumePolicy</c> still carries
/// (that class predates DIST-GEN.13's own normalization and was correctly out
/// of that phase's scope — see DIST-GEN.13 §8/§16 — but the established
/// current best practice for every projected target is to reference the
/// shared parent-family source directly).
///
/// At the frozen 34.0km reference peak: 34.0 → 23.8 → 14.62km, rounded to the
/// 0.5km grid at runtime — 34.0 → 24.0 → 14.5km (DIST-GEN.15B §6's own
/// verified feasibility trajectory).
///
/// <see cref="ResolvedPeakReferenceKm"/> stays EXACT-CELL and is deliberately
/// NOT normalized (DIST-GEN.15B §7's own <c>SelectedPeakReference</c> row):
/// 34.0 is this cell's own frozen exact-cell peak — genuinely different from
/// 16K's own 4D cell's 40.0, and numerically equal to (but never referencing
/// or falling back to) canonical HM I3D's own 34.0.
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonIntermediate3DTargetDistance16K"/>
/// consumes these values. No public routing/gate is widened by that named
/// policy's existence.
/// </summary>
internal static class TargetDistance16KIntermediate3DTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ~30% reduction. Referenced from the HM-parent authority, value unchanged at 0.70.</summary>
    public const double TaperWeek1VolumeMultiplier = HalfMarathonParentTaperAuthority.TaperWeek1VolumeMultiplier;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ~57% reduction. Referenced from the HM-parent authority, value unchanged at 0.43.</summary>
    public const double TaperWeek2VolumeMultiplier = HalfMarathonParentTaperAuthority.TaperWeek2VolumeMultiplier;

    /// <summary>EXACT-CELL, deliberately NOT normalized: the frozen DIST-GEN.15B §4/§7 pre-taper reference this cell's taper multipliers are computed against.</summary>
    public const double ResolvedPeakReferenceKm = 34.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>. The one shared HM-parent sequence instance, so provenance is provable by reference, not only by value.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = HalfMarathonParentTaperAuthority.OrderedMultipliers;
}
