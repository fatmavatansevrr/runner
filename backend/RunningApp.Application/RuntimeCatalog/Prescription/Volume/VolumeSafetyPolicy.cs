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
/// <param name="HardMaxWeeklyIncreaseRatio">
/// Documentation status clarified Phase 4G.3B.7/4G.3B.7.1 (TD-VOLUME-CAP-UNENFORCED-001,
/// now CLOSED): this field is currently informational/provenance-only. It is
/// threaded into <c>ReachablePeakDecision</c> (see <see cref="CatalogVolumeAndLongRunPlanner.ResolvePeak"/>)
/// purely for decision-trace purposes and is never read back by
/// <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/> to clamp or
/// reject an actual week-to-week transition. Safe today because Phase
/// 4G.3B.7's decision audit proved algebraically that the planner's
/// per-step interpolation ratio -- (GoldenFixtureResolvedPeakKm /
/// GoldenFixtureStartingVolumeKm - 1) / GoldenFixtureNonTaperTransitions --
/// is independent of both starting volume and target week count for
/// TEN_K_MASTER v6's current constants (the transition count cancels out of
/// the formula algebraically), and stays structurally below this value by
/// construction, not by coincidence of the specific inputs tested. Would
/// need re-verifying if GoldenFixtureStartingVolumeKm/GoldenFixtureResolvedPeakKm/
/// GoldenFixtureNonTaperTransitions change, or if a future candidate/catalog
/// revision introduces different starting-volume/peak-volume/core-cycle
/// constants for which this algebraic cancellation may no longer hold. See
/// PHASE4G_3B_7_VOLUME_CAP_ENFORCEMENT_DECISION_AUDIT.md for the full proof.
/// <see cref="VolumeProgressionVerifier"/> remains the sole place this bound
/// is actually verified today, independent of planner enforcement.
/// </param>
/// <param name="AbsoluteWeeklyIncrementCapKm">
/// Same informational/provenance-only status and same safety basis as
/// <see cref="HardMaxWeeklyIncreaseRatio"/> above (Phase 4G.3B.7/4G.3B.7.1,
/// TD-VOLUME-CAP-UNENFORCED-001, now CLOSED) -- never read back by
/// <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/>, structurally
/// unreachable for TEN_K_MASTER v6's current constants per the same proof.
/// See PHASE4G_3B_7_VOLUME_CAP_ENFORCEMENT_DECISION_AUDIT.md.
/// </param>
public enum ResolvedPeakReferenceProvenance
{
    GoldenFixtureDerived,
    ProductDefaultWithEvidenceEnvelope,
}

public sealed record ResolvedPeakReference(double Value, ResolvedPeakReferenceProvenance Provenance);

public sealed record VolumeSafetyPolicy(
    double PreferredMaxWeeklyIncreaseRatio,
    double HardMaxWeeklyIncreaseRatio,
    double AbsoluteWeeklyIncrementCapKm,
    double GoldenFixtureStartingVolumeKm,
    ResolvedPeakReference ResolvedPeakReference,
    int GoldenFixtureNonTaperTransitions,
    double TaperVolumeMultiplier,
    double LongRunPreferredMinimumShare,
    double LongRunPreferredMaximumShare,
    double LongRunSelectionShare,
    double LongRunHardCapShare,
    double RoundingIncrementKm,
    string RoundingRule)
{
    public double GoldenFixtureResolvedPeakKm => ResolvedPeakReference.Value;
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
        ResolvedPeakReference: new(38d, ResolvedPeakReferenceProvenance.GoldenFixtureDerived),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.30d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.33d,
        LongRunHardCapShare: 0.40d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");

    public static VolumeSafetyPolicy ThreeDayIntermediate { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.0d,
        GoldenFixtureStartingVolumeKm: 12d,
        ResolvedPeakReference: new(22.5d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.38d,
        LongRunPreferredMaximumShare: 0.42d,
        LongRunSelectionShare: 0.40d,
        LongRunHardCapShare: 0.42d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "3d_round_nearest_0.5km_then_reconcile_and_revalidate");

    public static VolumeSafetyPolicy BeginnerFourDay { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 12d,
        ResolvedPeakReference: new(21d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.30d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.33d,
        LongRunHardCapShare: 0.40d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");
}
