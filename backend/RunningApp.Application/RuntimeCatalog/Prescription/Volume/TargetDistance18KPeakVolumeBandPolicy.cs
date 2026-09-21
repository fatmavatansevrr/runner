namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.10 — freezes DIST-GEN.9 §28/§53's peak-volume band
/// ([34,46] km/week) for the dark 18K × INTERMEDIATE × 4D third target.
/// Mirrors <see cref="TargetDistance15KPeakVolumeBandPolicy"/>'s and
/// <see cref="TargetDistance16KPeakVolumeBandPolicy"/>'s own shape exactly,
/// as a distinct, separately-encoded C# constant (never a catalog JSON row)
/// — for the identical reason: the reused catalog identity is the
/// UNMODIFIED <c>HALF_MARATHON__4D__INTERMEDIATE</c> combination, whose real
/// catalog-declared band is HM's own [36,50] (not this target's own [34,46]
/// authority), and this phase's own "no catalog file changes" constraint
/// forbids editing or duplicating that catalog row.
///
/// The numeric bounds are byte-identical to both sibling policies' own — a
/// real, disclosed evidence convergence (DIST-GEN.9 §28/§57), not
/// template-copying — kept as an independently-declared class rather than a
/// shared constant per DIST-GEN.6's own instruction against collapsing
/// independently-authorized target cells into one literal.
/// </summary>
internal static class TargetDistance18KPeakVolumeBandPolicy
{
    public const double MinimumKm = 34.0d;
    public const double MaximumKm = 46.0d;

    /// <summary>
    /// Builds the substitute band for the given candidate identity (always
    /// HALF_MARATHON/INTERMEDIATE/4 for this target). <paramref name="referenceKey"/>/
    /// <paramref name="referenceVersion"/> are carried through only for decision-trace
    /// provenance — never re-read from that artifact for the actual bounds,
    /// which are this policy's own frozen constants.
    /// </summary>
    public static CatalogPeakVolumeBand Build(string distanceFamily, string experience, int runsPerWeek, string referenceKey, int referenceVersion) =>
        new(distanceFamily, experience, runsPerWeek, MinimumKm, MaximumKm, referenceKey, referenceVersion);
}
