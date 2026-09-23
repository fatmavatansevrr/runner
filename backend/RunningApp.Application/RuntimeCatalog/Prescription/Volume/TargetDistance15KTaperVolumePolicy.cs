namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.6 — freezes the two taper-week volume multipliers for the
/// dark 15K × INTERMEDIATE × 4D second target's reused 2-week Taper phase
/// (HALF_MARATHON's own frequency-generic Foundation/Build/RaceSpecific/Taper=2
/// structure, reused unchanged per DIST-GEN.5 §37/§39). Values are
/// byte-identical to <see cref="TargetDistance16KTaperVolumePolicy"/>'s own —
/// DIST-GEN.5 §38 re-examined the same general taper-percentage guidance
/// (explicitly covering "the 5K-15K range") and found it applies equally,
/// not merely non-contradicting. Both multipliers are applied INDEPENDENTLY
/// against the fixed pre-taper reference (40.0km) — never chained
/// week-to-week — via the same generic
/// <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/> non-chained
/// taper branch every other named 2-week-taper HM policy already uses.
///
/// PHASE DIST-GEN.13 — the two multipliers below are no longer independently
/// re-declared literals: they now REFERENCE
/// <see cref="HalfMarathonParentTaperAuthority"/>, the single explicit
/// implementation owner of HALF_MARATHON's own parent-family taper authority
/// (DIST-GEN.8 §7 <c>SHARED_PARENT_FAMILY_AUTHORITY</c>, DIST-GEN.12 §18/§53's
/// highest-priority normalization candidate). <b>No value changed</b> — both
/// were already byte-identical to that source, verified before wiring. This
/// class remains as this target's own named, traceable taper seam, and
/// continues to own <see cref="ResolvedPeakReferenceKm"/>, which is an
/// EXACT-TARGET field (DIST-GEN.12 §54) and is deliberately NOT normalized.
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K"/>
/// consumes these values. No public routing/gate is widened by that named
/// policy's existence.
/// </summary>
internal static class TargetDistance15KTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ~30% reduction. DIST-GEN.13: referenced from the HM-parent authority, value unchanged at 0.70.</summary>
    public const double TaperWeek1VolumeMultiplier = HalfMarathonParentTaperAuthority.TaperWeek1VolumeMultiplier;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ~57% reduction. DIST-GEN.13: referenced from the HM-parent authority, value unchanged at 0.43.</summary>
    public const double TaperWeek2VolumeMultiplier = HalfMarathonParentTaperAuthority.TaperWeek2VolumeMultiplier;

    /// <summary>EXACT-TARGET, deliberately NOT normalized (DIST-GEN.12 §54): the frozen DIST-GEN.5 §29 pre-taper reference this target's taper multipliers are computed against.</summary>
    public const double ResolvedPeakReferenceKm = 40.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>. DIST-GEN.13: the one shared HM-parent sequence instance, so provenance is provable by reference, not only by value.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = HalfMarathonParentTaperAuthority.OrderedMultipliers;
}
