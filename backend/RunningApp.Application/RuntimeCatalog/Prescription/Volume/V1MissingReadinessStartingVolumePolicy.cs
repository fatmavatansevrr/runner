namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

internal static class V1MissingReadinessStartingVolumePolicy
{
    public const string PolicyKey = "V1_MISSING_READINESS_STARTING_VOLUME_POLICY";
    public const int PolicyVersion = 1;
    public const double MissingWeeklyVolumeDefaultKm = 16d;
    public const double ExplicitZeroWeeklyVolumeDefaultKm = 12d;
    public const string Provenance =
        "PHASE4F_7B2_MISSING_ZERO_READINESS_DECISION.md; explicit V1 product default for TEN_K/INTERMEDIATE/4D missing-zero readiness closure";

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
