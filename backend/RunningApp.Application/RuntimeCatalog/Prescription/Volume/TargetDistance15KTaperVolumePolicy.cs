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
/// Declared as a distinct class from <see cref="TargetDistance16KTaperVolumePolicy"/>
/// rather than a shared constant, per DIST-GEN.6's own instruction: the
/// numeric coincidence is a disclosed evidence convergence, not grounds to
/// erase the second target's own independent authority trail.
///
/// Dark-only: <see cref="VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K"/>
/// consumes these values. No public routing/gate is widened by that named
/// policy's existence.
/// </summary>
internal static class TargetDistance15KTaperVolumePolicy
{
    /// <summary>First (shallower) taper week's multiplier, applied against the fixed pre-taper reference. ~30% reduction.</summary>
    public const double TaperWeek1VolumeMultiplier = 0.70d;

    /// <summary>Second, race-week (deepest) taper week's multiplier, applied against the same fixed pre-taper reference — never chained from Week 1's own output. ~57% reduction.</summary>
    public const double TaperWeek2VolumeMultiplier = 0.43d;

    /// <summary>The frozen DIST-GEN.5 §29 pre-taper reference this target's taper multipliers are computed against.</summary>
    public const double ResolvedPeakReferenceKm = 40.0d;

    /// <summary>Ordered, non-chained taper multiplier sequence for direct use as <see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/>.</summary>
    public static readonly IReadOnlyList<double> OrderedMultipliers = [TaperWeek1VolumeMultiplier, TaperWeek2VolumeMultiplier];
}
