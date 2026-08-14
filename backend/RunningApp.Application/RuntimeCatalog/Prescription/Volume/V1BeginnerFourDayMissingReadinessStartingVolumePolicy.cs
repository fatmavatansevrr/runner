namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

internal static class V1BeginnerFourDayMissingReadinessStartingVolumePolicy
{
    public const string PolicyKey = "V1_TEN_K_BEGINNER_4D_STARTING_VOLUME_POLICY";
    public const int PolicyVersion = 1;
    public const double MissingWeeklyVolumeDefaultKm = 12d;
    public const double ExplicitZeroWeeklyVolumeDefaultKm = 9.5d;

    public static StartingVolumeDecision Resolve(NormalizedRunningReadiness readiness)
    {
        var missing = readiness.WeeklyVolume.State == PrescriptionInputState.NotProvided;
        var zero = readiness.WeeklyVolume.State == PrescriptionInputState.Available && readiness.WeeklyVolume.Kilometers == 0;
        if (!missing && !zero)
            throw new CatalogVolumeCanonicalRuleSourceMissingException($"{PolicyKey} does not govern state '{readiness.WeeklyVolume.State}'.");

        return new StartingVolumeDecision(
            zero ? 0 : null, readiness.WeeklyVolume.State,
            missing ? MissingWeeklyVolumeDefaultKm : ExplicitZeroWeeklyVolumeDefaultKm,
            missing ? WeeklyVolumeAnchorSource.LevelConservativeDefault : WeeklyVolumeAnchorSource.NoRecentRunningDefault,
            CatalogVolumeClamp.ConservativeClamp, CatalogEvidenceBasis.ProductPracticeInformed,
            CatalogDecisionStatus.ExplicitProductDefault,
            $"{PolicyKey} v{PolicyVersion}; GEN.4C.4 frozen PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE.");
    }
}
