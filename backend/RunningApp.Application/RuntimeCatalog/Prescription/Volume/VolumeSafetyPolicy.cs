namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Backend Integration Phase 4G.3B.0 — typed, versioned contract for every
/// numeric safety constant that governs <see cref="CatalogVolumeAndLongRunPlanner"/>'s
/// weekly-volume and long-run planning. Pure extraction: every field's value
/// here is byte-identical to the private inline constant it replaces (see
/// PHASE4G_3B_0_VOLUME_SAFETY_POLICY_GOVERNANCE_NOTE.md for the exact prior
/// location and source classification of each field) — this record changes
/// no numeric behavior, only where the numbers live. No field represents a
/// constant that did not already exist in code.
/// </summary>
public sealed record VolumeSafetyPolicy(
    double PreferredMaxWeeklyIncreaseRatio,
    double HardMaxWeeklyIncreaseRatio,
    double AbsoluteWeeklyIncrementCapKm,
    double GoldenFixtureStartingVolumeKm,
    double GoldenFixtureResolvedPeakKm,
    int GoldenFixtureNonTaperTransitions,
    double TaperVolumeMultiplier,
    double LongRunPreferredMinimumShare,
    double LongRunPreferredMaximumShare,
    double LongRunSelectionShare,
    double LongRunHardCapShare,
    double RoundingIncrementKm,
    string RoundingRule)
{
    /// <summary>Stable identifier for this exact set of values — bump when any field's value changes, so a decision trace can always be traced back to the policy version that produced it.</summary>
    public const string PolicyVersion = "APPSEL_RACE_VOLUME_SAFETY_V1";

    /// <summary>
    /// The current, unchanged V1 values — identical to every value
    /// <see cref="CatalogVolumeAndLongRunPlanner"/> held as a private inline
    /// constant before Phase 4G.3B.0. See the governance note for per-field
    /// provenance.
    /// </summary>
    public static VolumeSafetyPolicy Default { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 24d,
        GoldenFixtureResolvedPeakKm: 38d,
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.30d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.33d,
        LongRunHardCapShare: 0.40d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");
}
