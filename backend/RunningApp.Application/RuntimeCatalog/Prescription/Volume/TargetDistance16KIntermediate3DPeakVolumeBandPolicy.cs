namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.16 — freezes DIST-GEN.15B §4/§7's peak-volume band
/// ([28,40] km/week) for the dark <c>16K × Intermediate × 3D</c> cell — the
/// FIRST projected-distance × non-4D-frequency cell. This exists as a
/// distinct, separately-encoded C# constant (never a catalog JSON row), for
/// the identical reason every sibling projected-target band policy in this
/// file does: the reused catalog identity is the UNMODIFIED
/// <c>HALF_MARATHON__3D__INTERMEDIATE</c> combination, whose real
/// <c>peak-volume-bands.v10.json</c> policy entry for
/// (distanceFamily=HALF_MARATHON, experience=INTERMEDIATE, runsPerWeek=3) is
/// canonical HM's own real [28,40] band — the SAME numeric bounds this cell's
/// own exact-cell authority happens to adopt (DIST-GEN.15B §3's own central
/// "value equality ≠ authority equality" distinction: this is EXACT-CELL
/// authority for the tuple (16.0, HalfMarathon, Intermediate, 3), an
/// <c>EVIDENCE_INFORMED_PRODUCT_DEFAULT</c> that happens to numerically equal
/// canonical HM I3D's own value — NEVER a claim that 16K×I×3D falls back to
/// or references HM I3D's own catalog-declared or code-declared band). This
/// phase's own "no catalog file changes" diff-safety constraint forbids
/// editing or duplicating that catalog row regardless.
///
/// <see cref="CatalogPeakVolumeBand"/> instances built from this policy carry
/// this cell's own frozen [28,40] bounds directly, substituted in place of
/// the catalog-loaded HM band only for a request the
/// <see cref="RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy"/>
/// gate has already approved as the exact
/// <c>(16.0, HalfMarathon, Intermediate, 3)</c> cell — never for any canonical
/// HALF_MARATHON or TEN_K request, and never for 16K's own 4D cell (whose own
/// independent [34,46] band is <see cref="TargetDistance16KPeakVolumeBandPolicy"/>,
/// completely untouched by this class).
/// </summary>
internal static class TargetDistance16KIntermediate3DPeakVolumeBandPolicy
{
    public const double MinimumKm = 28.0d;
    public const double MaximumKm = 40.0d;

    /// <summary>
    /// Builds the substitute band for the given candidate identity (always
    /// HALF_MARATHON/INTERMEDIATE/3 for this cell). <paramref name="referenceKey"/>/
    /// <paramref name="referenceVersion"/> are carried through only for decision-trace
    /// provenance (they identify the real HM peak-volume-band catalog artifact
    /// this cell's own request would otherwise have loaded) — never re-read
    /// from that artifact for the actual bounds, which are this policy's own
    /// frozen constants.
    /// </summary>
    public static CatalogPeakVolumeBand Build(string distanceFamily, string experience, int runsPerWeek, string referenceKey, int referenceVersion) =>
        new(distanceFamily, experience, runsPerWeek, MinimumKm, MaximumKm, referenceKey, referenceVersion);
}
