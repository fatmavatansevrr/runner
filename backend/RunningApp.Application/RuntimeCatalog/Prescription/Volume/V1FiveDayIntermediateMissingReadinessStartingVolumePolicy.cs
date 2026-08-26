namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Phase 10K-FREQ.6D.10 — implements the already-approved <c>FREQ.6C</c>
/// Intermediate×5D missing/explicit-zero starting-volume authority
/// (`PHASE_10K_FREQ_6C_INTERMEDIATE_5D_NUMERIC_DECISION_CLOSURE.md` §A):
/// missing-readiness 26.0km (Hal Higdon's real Week-1 5-day evidence
/// anchor), explicit-zero 19.5km (26.0 × 0.75, reusing 4D's own
/// missing:explicit-zero ratio applied to 5D's own anchor — not 4D's
/// absolute values). Mirrors <see cref="V1ThreeDayMissingReadinessStartingVolumePolicy"/>/
/// <see cref="V1BeginnerFourDayMissingReadinessStartingVolumePolicy"/>
/// exactly; no new numeric research, no new value invented here — every
/// constant below is a direct, unmodified transcription of FREQ.6C's own
/// closure table.
/// </summary>
internal static class V1FiveDayIntermediateMissingReadinessStartingVolumePolicy
{
    public const string PolicyKey = "V1_TEN_K_INTERMEDIATE_5D_STARTING_VOLUME_POLICY";
    public const int PolicyVersion = 1;
    public const double MissingWeeklyVolumeDefaultKm = 26.0d;
    public const double ExplicitZeroWeeklyVolumeDefaultKm = 19.5d;
    public const string Provenance =
        "PHASE_10K_FREQ_6C_INTERMEDIATE_5D_NUMERIC_DECISION_CLOSURE.md §A; explicit V1 product default for TEN_K/INTERMEDIATE/5D missing-zero readiness closure";

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
