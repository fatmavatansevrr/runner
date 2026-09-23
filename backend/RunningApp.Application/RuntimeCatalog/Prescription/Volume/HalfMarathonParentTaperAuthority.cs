namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.13 — the single, explicit implementation owner of the
/// <c>SHARED_PARENT_FAMILY_AUTHORITY</c> taper fields DIST-GEN.8 §7 already
/// closed and DIST-GEN.12 §18/§53 named the HIGHEST-priority normalization
/// candidate in the entire matrix.
///
/// <b>This component freezes no new number and creates no new authority.</b>
/// 0.70/0.43 over a 2-week taper, applied non-chained against a fixed
/// pre-taper anchor, has been the HALF_MARATHON parent family's own frozen
/// taper since HM.1.4B, and every HM-parented cell that has ever been closed
/// re-verified and reused it rather than re-deriving it. What DIST-GEN.13
/// changes is only WHERE that one already-decided conclusion is declared.
///
/// <b>Why the values live here and not per target.</b> DIST-GEN.8 §7 classified
/// taper duration/multipliers/mechanism as parent-family authority, and cited
/// canonical TEN_K's own genuinely DIFFERENT taper (a single taper week at a
/// 0.53 multiplier) as the proof that this is inherited from the HALF_MARATHON
/// parent rather than being a property of any distance or distance band.
/// <b>TEN_K is deliberately NOT a consumer of this component and must never
/// become one.</b>
///
/// <b>What this component deliberately does NOT own.</b> Each consuming cell's
/// own <c>ResolvedPeakReferenceKm</c> — the fixed pre-taper anchor the
/// multipliers are applied against — is an EXACT-TARGET field
/// (DIST-GEN.12 §54: <c>SelectedPeakReference</c>), genuinely different per
/// cell (43.0 canonical HM 4D, 51.0 HM 6D, 40.0 at each of 15K/16K/18K). It
/// stays declared by each cell's own taper policy class, is not normalized
/// here, and must never be moved into this component.
///
/// Consumers, named one by one (no "applies to anything HM-shaped" mechanism
/// exists here): <see cref="HalfMarathonIntermediate4DTaperVolumePolicy"/>,
/// <see cref="HalfMarathonIntermediate6DTaperVolumePolicy"/>,
/// <see cref="TargetDistance15KTaperVolumePolicy"/> and
/// <see cref="TargetDistance16KTaperVolumePolicy"/> (the latter two also being
/// the declared source 18K's own <see cref="VolumeSafetyPolicy"/> instance
/// reads, per DIST-GEN.10 §33). Each was verified byte-for-byte equal to the
/// values below before being wired here — see
/// PHASE_DIST_GEN_13_...md §16/§53.
/// </summary>
internal static class HalfMarathonParentTaperAuthority
{
    /// <summary>DIST-GEN.8 §7 <c>SHARED_PARENT_FAMILY_AUTHORITY</c> — HALF_MARATHON's own frequency-generic Foundation/Build/RaceSpecific/Taper topology carries exactly 2 taper weeks. TEN_K's own 1-week taper is a different parent family and is not governed here.</summary>
    public const int TaperDurationWeeks = 2;

    /// <summary>DIST-GEN.8 §7 <c>SHARED_PARENT_FAMILY_AUTHORITY</c> — first (shallower) taper week's multiplier, applied against the consuming cell's own fixed pre-taper reference. ≈30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>DIST-GEN.8 §7 <c>SHARED_PARENT_FAMILY_AUTHORITY</c> — second, race-week (deepest) taper week's multiplier, applied against the SAME fixed pre-taper reference, never chained from week 1's own output. ≈57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>. One shared instance, so provenance is provable by reference, not only by value.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}
