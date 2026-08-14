namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

internal static class V1ThreeDayMissingReadinessStartingVolumePolicy
{
    public const string PolicyKey = "V1_TEN_K_INTERMEDIATE_3D_STARTING_VOLUME_POLICY";
    public const int PolicyVersion = 1;
    public const double MissingWeeklyVolumeDefaultKm = 16d;
    public const double ExplicitZeroWeeklyVolumeDefaultKm = 12d;

    public static StartingVolumeDecision Resolve(NormalizedRunningReadiness readiness)
    {
        var missing = readiness.WeeklyVolume.State == PrescriptionInputState.NotProvided;
        var zero = readiness.WeeklyVolume.State == PrescriptionInputState.Available && readiness.WeeklyVolume.Kilometers == 0;
        if (!missing && !zero)
            throw new CatalogVolumeCanonicalRuleSourceMissingException($"{PolicyKey} does not govern state '{readiness.WeeklyVolume.State}'.");

        var selected = missing ? MissingWeeklyVolumeDefaultKm : ExplicitZeroWeeklyVolumeDefaultKm;
        return new StartingVolumeDecision(
            zero ? 0 : null, readiness.WeeklyVolume.State, selected,
            missing ? WeeklyVolumeAnchorSource.LevelConservativeDefault : WeeklyVolumeAnchorSource.NoRecentRunningDefault,
            CatalogVolumeClamp.ConservativeClamp, CatalogEvidenceBasis.ProductPracticeInformed,
            CatalogDecisionStatus.ExplicitProductDefault,
            $"{PolicyKey} v{PolicyVersion}; GEN.2B.2 evidence-constrained 3D product default; readiness safeguards remain authoritative.");
    }
}
