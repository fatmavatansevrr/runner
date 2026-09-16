namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.2 — freezes DIST-GEN.1 §22's peak-volume band
/// ([34,46] km/week) for the dark 16K × INTERMEDIATE × 4D pilot. This exists
/// as a distinct, separately-encoded C# constant (never a catalog JSON row)
/// precisely because the pilot's catalog identity is the UNMODIFIED
/// <c>HALF_MARATHON__4D__INTERMEDIATE</c> combination — the real
/// <c>peak-volume-bands.v10.json</c> policy entry for
/// (distanceFamily=HALF_MARATHON, experience=INTERMEDIATE, runsPerWeek=4) is
/// HM's own real [36,50] band, per DIST-GEN.1 §20, and per this phase's own
/// "no catalog file changes" diff-safety constraint that band cannot be
/// edited or duplicated as a new catalog row. <see cref="CatalogPeakVolumeBand"/>
/// instances built from this policy carry the pilot's own frozen [34,46]
/// bounds directly, substituted in place of the catalog-loaded HM band only
/// for a request the <see cref="RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy"/>
/// gate has already approved — never for any canonical HALF_MARATHON or
/// TEN_K request, which always load the real catalog-declared band unchanged.
/// </summary>
internal static class TargetDistance16KPeakVolumeBandPolicy
{
    public const double MinimumKm = 34.0d;
    public const double MaximumKm = 46.0d;

    /// <summary>
    /// Builds the substitute band for the given candidate identity (always
    /// HALF_MARATHON/INTERMEDIATE/4 for this pilot). <paramref name="referenceKey"/>/
    /// <paramref name="referenceVersion"/> are carried through only for decision-trace
    /// provenance (they identify the real HM peak-volume-band catalog artifact
    /// this pilot's own request would otherwise have loaded) — never re-read
    /// from that artifact for the actual bounds, which are this policy's own
    /// frozen constants.
    /// </summary>
    public static CatalogPeakVolumeBand Build(string distanceFamily, string experience, int runsPerWeek, string referenceKey, int referenceVersion) =>
        new(distanceFamily, experience, runsPerWeek, MinimumKm, MaximumKm, referenceKey, referenceVersion);
}
