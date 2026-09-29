namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.20 — freezes DIST-GEN.19A §14-15's peak-volume band
/// ([44,58] km/week) for the dark <c>16K × Intermediate × 5D</c> cell. This
/// exists as a distinct, separately-encoded C# constant (never a catalog JSON
/// row), for the identical reason every sibling projected-target band policy
/// in this file does: the reused catalog identity is the UNMODIFIED
/// <c>HALF_MARATHON__5D__INTERMEDIATE</c> combination, whose real
/// <c>peak-volume-bands.v10.json</c> policy entry for
/// (distanceFamily=HALF_MARATHON, experience=INTERMEDIATE, runsPerWeek=5) is
/// canonical HM's own real [44,58] band — the SAME numeric bounds this cell's
/// own exact-cell authority happens to adopt (DIST-GEN.19A §13's own central
/// "value equality ≠ authority equality" distinction: this is EXACT-CELL
/// authority for the tuple (16.0, HalfMarathon, Intermediate, 5), an
/// <c>EVIDENCE_INFORMED_PRODUCT_DEFAULT</c> that happens to numerically equal
/// canonical HM I5D's own value — NEVER a claim that 16K×I×5D falls back to or
/// references HM I5D's own catalog-declared or code-declared band). This
/// phase's own "no catalog file changes" diff-safety constraint forbids
/// editing or duplicating that catalog row regardless.
///
/// <see cref="CatalogPeakVolumeBand"/> instances built from this policy carry
/// this cell's own frozen [44,58] bounds directly, substituted in place of
/// the catalog-loaded HM band only for a request the
/// <see cref="RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy"/>
/// gate has already approved as the exact
/// <c>(16.0, HalfMarathon, Intermediate, 5)</c> cell — never for any canonical
/// HALF_MARATHON or TEN_K request, and never for 16K's own 3D/4D cells (whose
/// own independent [28,40]/[34,46] bands are owned by
/// <see cref="TargetDistance16KIntermediate3DPeakVolumeBandPolicy"/> and
/// <see cref="TargetDistance16KPeakVolumeBandPolicy"/>, completely untouched
/// by this class).
/// </summary>
internal static class TargetDistance16KIntermediate5DPeakVolumeBandPolicy
{
    public const double MinimumKm = 44.0d;
    public const double MaximumKm = 58.0d;

    /// <summary>
    /// Builds the substitute band for the given candidate identity (always
    /// HALF_MARATHON/INTERMEDIATE/5 for this cell). <paramref name="referenceKey"/>/
    /// <paramref name="referenceVersion"/> are carried through only for decision-trace
    /// provenance (they identify the real HM peak-volume-band catalog artifact
    /// this cell's own request would otherwise have loaded) — never re-read
    /// from that artifact for the actual bounds, which are this policy's own
    /// frozen constants.
    /// </summary>
    public static CatalogPeakVolumeBand Build(string distanceFamily, string experience, int runsPerWeek, string referenceKey, int referenceVersion) =>
        new(distanceFamily, experience, runsPerWeek, MinimumKm, MaximumKm, referenceKey, referenceVersion);
}
