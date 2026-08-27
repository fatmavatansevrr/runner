namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Phase 10K-FREQ.6D.26 — implements the already-approved <c>FREQ.6D.25</c>
/// Intermediate×6D missing/explicit-zero starting-volume authority: the same
/// real, undifferentiated Hal Higdon evidence source FREQ.6C anchored 5D to
/// does not distinguish a 5-day from a 6-day execution of the same program,
/// so the identical values are reused verbatim (missing=26.0km,
/// explicit-zero=19.5km) rather than derived from a new source. Mirrors
/// <see cref="V1FiveDayIntermediateMissingReadinessStartingVolumePolicy"/>
/// exactly, with its own PolicyKey identity for decision-trace labeling. No
/// new numeric value here — every constant is a direct transcription of
/// FREQ.6D.25's own closure.
/// </summary>
internal static class V1SixDayIntermediateMissingReadinessStartingVolumePolicy
{
    public const string PolicyKey = "V1_TEN_K_INTERMEDIATE_6D_STARTING_VOLUME_POLICY";
    public const int PolicyVersion = 1;
    public const double MissingWeeklyVolumeDefaultKm = 26.0d;
    public const double ExplicitZeroWeeklyVolumeDefaultKm = 19.5d;
    public const string Provenance =
        "PHASE_10K_FREQ_6D_25_INTERMEDIATE_6D_PEAK_VOLUME_BAND_TIER_MATCHED_EVIDENCE_FINAL_DECISION.md; reuses FREQ.6C's 5D anchor verbatim -- same undifferentiated real source spans both 5 and 6 days";

    public static StartingVolumeDecision Resolve(NormalizedRunningReadiness readiness)
    {
        if (readiness.WeeklyVolume.State == PrescriptionInputState.NotProvided)
        {
            return new StartingVolumeDecision(
                null,
                readiness.WeeklyVolume.State,
                MissingWeeklyVolumeDefaultKm,
                WeeklyVolumeAnchorSource.LevelConservativeDefault,
                CatalogVolumeClamp.ConservativeClamp,
                CatalogEvidenceBasis.ProductPracticeInformed,
                CatalogDecisionStatus.ExplicitProductDefault,
                $"{PolicyKey} v{PolicyVersion}: missing recent weekly volume -> {MissingWeeklyVolumeDefaultKm}km. {Provenance}");
        }

        if (readiness.WeeklyVolume.State == PrescriptionInputState.Available && readiness.WeeklyVolume.Kilometers == 0)
        {
            return new StartingVolumeDecision(
                0,
                readiness.WeeklyVolume.State,
                ExplicitZeroWeeklyVolumeDefaultKm,
                WeeklyVolumeAnchorSource.NoRecentRunningDefault,
                CatalogVolumeClamp.ConservativeClamp,
                CatalogEvidenceBasis.ProductPracticeInformed,
                CatalogDecisionStatus.ExplicitProductDefault,
                $"{PolicyKey} v{PolicyVersion}: explicit zero recent weekly volume -> {ExplicitZeroWeeklyVolumeDefaultKm}km. {Provenance}");
        }

        throw new CatalogVolumeCanonicalRuleSourceMissingException(
            $"{PolicyKey} v{PolicyVersion} does not define a starting-volume rule for weekly-volume state '{readiness.WeeklyVolume.State}'.");
    }
}
