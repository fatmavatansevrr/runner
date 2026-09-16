namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.2 — freezes the two taper-week volume multipliers for the
/// dark 16K × INTERMEDIATE × 4D pilot's reused 2-week Taper phase (HALF_MARATHON's
/// own frequency-generic Foundation/Build/RaceSpecific/Taper=2 structure,
/// reused unchanged per DIST-GEN.1 §30/§32). Values reused directly, unchanged,
/// from <see cref="HalfMarathonIntermediate4DTaperVolumePolicy"/> — DIST-GEN.1
/// §31 re-examined the same external evidence (Mujika &amp; Padilla meta-analyses,
/// Marathon Handbook's taper guide, best-running-tips.com, runtothefinish.com)
/// and found it independently corroborating, not merely non-contradictory:
/// this pilot's own frozen peak (40.0km) sits below best-running-tips.com's own
/// ~56km/week lighter-taper-exception threshold, so the standard reduction
/// regime applies. Both multipliers are applied INDEPENDENTLY against the
/// fixed pre-taper reference (40.0km) — never chained week-to-week — via the
/// same generic <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/>
/// non-chained taper branch every other named 2-week-taper HM policy already
/// uses.
///
/// At the frozen 40.0km reference peak: 40.0 -&gt; 28.0 -&gt; 17.2 -&gt; 17.0km
/// (rounded to the 0.5km grid).
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K"/>
/// consumes these values. No public routing/gate is widened by that named
/// policy's existence — see
/// PHASE_DIST_GEN_2_16K_INTERMEDIATE_4D_FULL_DARK_TARGET_DISTANCE_PROJECTION_IMPLEMENTATION.md.
/// </summary>
internal static class TargetDistance16KTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ~30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ~57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen DIST-GEN.1 §23 pre-taper reference this pilot's taper multipliers are computed against.</summary>
    public const double ResolvedPeakReferenceKm = 40.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}
