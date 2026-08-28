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

    /// <summary>
    /// Phase 10K-FREQ.6D.10 — implements the already-approved FREQ.6C
    /// Intermediate×5D numeric authority (`PHASE_10K_FREQ_6C_INTERMEDIATE_5D_NUMERIC_DECISION_CLOSURE.md`
    /// §A). <see cref="GoldenFixtureStartingVolumeKm"/> is the FREQ.6C
    /// missing-readiness anchor itself (26.0, self-referential per that
    /// closure — not borrowed from the 4D golden fixture); <see cref="ResolvedPeakReference"/>
    /// is FREQ.6C's own 44.5km peak reference. FREQ.6C approved exactly two
    /// long-run share figures (28% selection, 36% hard cap) — no separate
    /// "preferred range" was calibrated for 5D, so the preferred
    /// minimum/maximum bounds below reuse those same two approved figures
    /// rather than inventing a third number: the preferred range collapses
    /// to exactly [28%, 36%], with the deterministic selection target sitting
    /// at its own lower edge. No new numeric constant appears anywhere in
    /// this record.
    /// </summary>
    public static VolumeSafetyPolicy FiveDayIntermediate { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 26.0d,
        ResolvedPeakReference: new(44.5d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.28d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.28d,
        LongRunHardCapShare: 0.36d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");

    /// <summary>
    /// Phase 10K-FREQ.6D.26 -- implements the already-approved FREQ.6D.23/6D.25
    /// Intermediate×6D numeric authority. Every value is byte-identical to
    /// <see cref="FiveDayIntermediate"/>: FREQ.6D.25 approved reusing 5D's
    /// exact starting-volume/peak-reference/long-run-share figures for 6D
    /// (the same real, undifferentiated evidence source spans both 5 and 6
    /// days), and FREQ.6D.25 separately approved PeakVolumeBand=[36,50]km
    /// (implemented as a catalog row, not a field here) via a new,
    /// materially-stronger cross-tier evidence finding -- not because a rule
    /// mandates equality with 5D. No new numeric constant appears anywhere
    /// in this record.
    /// </summary>
    public static VolumeSafetyPolicy SixDayIntermediate { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 26.0d,
        ResolvedPeakReference: new(44.5d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.28d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.28d,
        LongRunHardCapShare: 0.36d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");

    /// <summary>
    /// Phase 10K-FREQ.6D.26 -- centralizes the Intermediate LongHorizon
    /// GE/Runway daysPerWeek-to-policy dispatch that was previously
    /// duplicated as an ad-hoc `daysPerWeek == 5 ? FiveDayIntermediate :
    /// Default` (or `easySupportCount == 3`) ternary at four separate call
    /// sites. Fail-closed for any Intermediate frequency without an approved
    /// policy, rather than silently defaulting -- byte-identical selection
    /// for every existing caller (4D still resolves to <see cref="Default"/>,
    /// 5D to <see cref="FiveDayIntermediate"/>).
    /// </summary>
    public static VolumeSafetyPolicy ForIntermediateDaysPerWeek(int daysPerWeek) => daysPerWeek switch
    {
        4 => Default,
        5 => FiveDayIntermediate,
        6 => SixDayIntermediate,
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), daysPerWeek, "No approved Intermediate VolumeSafetyPolicy exists for this DaysPerWeek."),
    };

    /// <summary>
    /// Phase 10K-GEN.9 -- implements the already-approved GEN.7/GEN.8 Advanced
    /// numeric authority. Progression rates, taper factor, and rounding are
    /// reused verbatim (GEN.7 §9/§26/§27: confirmed Level-and-frequency-
    /// invariant across every existing policy). Long-run shares reuse the
    /// frequency-owned figures GEN.7 §6/§27 froze per frequency (identical to
    /// <see cref="ThreeDayIntermediate"/>'s own shares -- Advanced does not
    /// get a different long-run policy at 3D). <see cref="ResolvedPeakReference"/>
    /// is GEN.8's approved 40.0km (band midpoint, ProductDefaultWithEvidenceEnvelope).
    /// <see cref="GoldenFixtureStartingVolumeKm"/> is a growth-multiplier
    /// calibration constant this planner's <c>ResolvePeak</c> formula requires
    /// unconditionally for every non-3D-style policy; Advanced has no
    /// missing-readiness default to reuse for it (GEN.8 approved
    /// observed-only, positive-required readiness with no fallback number).
    /// An initial implementation reused the PeakVolumeBand minimum directly,
    /// but real dark verification (GEN.9's own dual-KEY LongHorizon lifecycle
    /// test) found this produces a genuine Runway-progression rounding edge
    /// case at the low starting values every existing policy avoids (every
    /// existing GoldenFixtureStartingVolumeKm sits meaningfully below its own
    /// band minimum, never at it). This implementation instead reuses
    /// <see cref="ThreeDayIntermediate"/>'s own already-proven
    /// GoldenFixtureStartingVolumeKm-to-ResolvedPeakReference ratio
    /// (12/22.5 = 0.5333, a ratio this exact Runway/GE numeric pipeline
    /// already exercises safely for Intermediate), applied to Advanced's own
    /// approved peak reference -- reusing an existing deterministic
    /// cross-axis relationship, not inventing a new number.
    /// </summary>
    public static VolumeSafetyPolicy Advanced3D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 21.5d,
        ResolvedPeakReference: new(40d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.38d,
        LongRunPreferredMaximumShare: 0.42d,
        LongRunSelectionShare: 0.40d,
        LongRunHardCapShare: 0.42d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");

    /// <summary>Phase 10K-GEN.9 -- see <see cref="Advanced3D"/>'s doc comment for the shared calibration rationale (here reusing <see cref="Default"/>'s own 24/38=0.6316 ratio, its 4D-owned figures, GEN.7 §6/§27). ResolvedPeakReference=45.0km (band [38,52] midpoint, GEN.8).</summary>
    public static VolumeSafetyPolicy Advanced4D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 28.5d,
        ResolvedPeakReference: new(45d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.30d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.33d,
        LongRunHardCapShare: 0.40d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");

    /// <summary>Phase 10K-GEN.9 -- see <see cref="Advanced3D"/>'s doc comment for the shared calibration rationale (here reusing <see cref="FiveDayIntermediate"/>'s own 26/44.5=0.5843 ratio, its 5D-owned figures, GEN.7 §6/§27). ResolvedPeakReference=50.0km (band [42,58] midpoint, GEN.8).</summary>
    public static VolumeSafetyPolicy Advanced5D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 29d,
        ResolvedPeakReference: new(50d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.28d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.28d,
        LongRunHardCapShare: 0.36d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");

    /// <summary>Phase 10K-GEN.9 -- see <see cref="Advanced3D"/>'s doc comment for the shared calibration rationale (here reusing <see cref="SixDayIntermediate"/>'s own 26/44.5=0.5843 ratio, its 6D-owned figures, GEN.7 §6/§27). ResolvedPeakReference=51.0km (band [42,60] midpoint, GEN.8).</summary>
    public static VolumeSafetyPolicy Advanced6D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 30d,
        ResolvedPeakReference: new(51d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.28d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.28d,
        LongRunHardCapShare: 0.36d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");

    /// <summary>Phase 10K-GEN.9 -- Advanced counterpart of <see cref="ForIntermediateDaysPerWeek"/>. Fail-closed for 7D (PRODUCT_NON_SUPPORT, GEN.7/GEN.8) and any other value.</summary>
    public static VolumeSafetyPolicy ForAdvancedDaysPerWeek(int daysPerWeek) => daysPerWeek switch
    {
        3 => Advanced3D,
        4 => Advanced4D,
        5 => Advanced5D,
        6 => Advanced6D,
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), daysPerWeek, "No approved Advanced VolumeSafetyPolicy exists for this DaysPerWeek."),
    };

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
