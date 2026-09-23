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
    string RoundingRule,
    /// <summary>
    /// HM.1.4B — optional, ordered sequence of taper-week multipliers, one
    /// per taper week from first to last, each meant to be applied
    /// INDEPENDENTLY against the fixed pre-taper reference (never chained
    /// week-to-week — see <see cref="CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan"/>).
    /// Null for every existing 10K policy (all 12 named instances below):
    /// <see cref="ResolvedTaperVolumeMultipliers"/> then degrades to the
    /// single-element list [<see cref="TaperVolumeMultiplier"/>], reproducing
    /// today's exact 1-week-taper behavior with zero delta. Populate this
    /// only for a policy whose candidate shape has a genuine multi-week
    /// Taper phase. HALF_MARATHON Intermediate×4D is the first such named
    /// policy; its complete authority was frozen across HM.1.4–HM.1.6 and
    /// is represented by <see cref="HalfMarathonIntermediate4D"/> below.
    /// </summary>
    IReadOnlyList<double>? TaperVolumeMultipliers = null,
    /// <summary>
    /// HM.1.6 — optional absolute long-run ceiling in kilometers, expressing
    /// a PREFERRED ABSOLUTE PEAK, never a mandatory target (frozen semantic:
    /// HM.1.1's own <c>PreferredAbsolutePeakLongRunKm</c> decision, not
    /// re-decided here). Null for every existing 10K policy (all 12 named
    /// instances below) — the mechanism this field gates (see
    /// <see cref="CatalogVolumeAndLongRunPlanner.BuildLongRunPlan"/>'s
    /// RACE_SPECIFIC-phase share-progression branch) is entirely inert when
    /// this is null, reproducing today's exact flat-<see cref="LongRunSelectionShare"/>
    /// behavior with zero delta, regardless of whether a given candidate's
    /// own phase keys happen to include "RACE_SPECIFIC" (this field, not the
    /// phase key's mere presence, is what gates the mechanism). When set
    /// (e.g. HM's own frozen 19.0km, HM.1.1), <see cref="CatalogVolumeAndLongRunPlanner.BuildLongRunPlan"/>
    /// (a) lets a RACE_SPECIFIC-phase week's long-run share ramp linearly,
    /// position-in-phase-weighted, from <see cref="LongRunSelectionShare"/>
    /// toward <see cref="LongRunHardCapShare"/> (never past it — reuses the
    /// two already-approved share numbers, invents no new share figure), and
    /// (b) additionally clamps every week's effective hard-cap-in-kilometers
    /// down to this value when the share-derived hard cap would otherwise
    /// exceed it — so the resulting long run can rise above the ordinary
    /// selected-share target only as far as both the hard-cap SHARE and this
    /// absolute-KM ceiling jointly allow, never forcing either bound to be
    /// reached. See PHASE_HM_1_6_LONG_RUN_SHARE_PEAK_WEEK_MECHANISM_CLOSURE.md
    /// for the full mechanism rationale and zero-delta proof.
    /// </summary>
    double? PreferredAbsolutePeakLongRunKm = null,
    /// <summary>
    /// HM.5.3 — optional, opt-in semantic flag distinguishing two genuinely different meanings
    /// <see cref="ResolvedPeakReference"/> can carry, discovered while closing HM's 15W/16W
    /// peak-volume-overshoot blocker. <see cref="CatalogVolumeAndLongRunPlanner.ResolvePeak"/>'s
    /// interpolation formula (<c>transitionAdjustedMultiplier</c>) is calibrated at exactly
    /// <see cref="GoldenFixtureNonTaperTransitions"/> non-taper transitions; for every existing
    /// 10K policy (all false, the default — zero delta, confirmed by full regression), a
    /// horizon with MORE non-taper transitions than that calibration count is allowed to
    /// EXTRAPOLATE past <see cref="ResolvedPeakReference"/>'s own value — this is real, already
    /// shipped, already-tested 10K product behavior (e.g. <c>BeginnerFourDay</c>'s 13W/14W
    /// horizons intentionally resolve 22/23km, above its own 21km reference — see
    /// <c>Gen4DBeginnerFourDayCoreTests</c>). For <see cref="HalfMarathonIntermediate4D"/> only,
    /// the frozen HM.5.3 authority is the opposite: <see cref="ResolvedPeakReference"/> (43.0km)
    /// is a SELECTED PEAK CEILING for the whole 10–16W Core family, and a longer horizon must
    /// gain only more time to reach that same peak, never a higher one. Setting this true is
    /// the smallest typed seam that lets <c>ResolvePeak</c> express "the calibration ratio
    /// saturates at the calibration point instead of extrapolating past it" as an explicit,
    /// declarative per-policy choice rather than a distance-family branch inside the shared
    /// algorithm. Numeric planning never branches on this field for any value other than
    /// clamping the interpolation's own transition count — it introduces no new peak/share/taper
    /// number anywhere.
    /// </summary>
    bool ResolvedPeakReferenceIsSelectedCeiling = false)
{
    public double GoldenFixtureResolvedPeakKm => ResolvedPeakReference.Value;

    /// <summary>
    /// Optional first-Core-week absolute long-run ceiling. Null preserves every historical
    /// policy exactly. HM.11 is the first runtime consumer: its frozen 12km value is a cap,
    /// never a readiness default or substitute for missing athlete history.
    /// </summary>
    public double? StartingAbsoluteLongRunCapKm { get; init; }
    /// <summary>Stable identifier for this exact set of values — bump when any field's value changes, so a decision trace can always be traced back to the policy version that produced it.</summary>
    public const string PolicyVersion = "APPSEL_RACE_VOLUME_SAFETY_V1";

    /// <summary>
    /// HM.1.4B — the full, ordered, non-chained taper-week multiplier
    /// sequence this policy actually represents. Every existing 10K policy
    /// (<see cref="TaperVolumeMultipliers"/> is null) resolves to the
    /// single-element <c>[TaperVolumeMultiplier]</c> — byte-identical to the
    /// scalar every consumer already reads today.
    /// </summary>
    public IReadOnlyList<double> ResolvedTaperVolumeMultipliers => TaperVolumeMultipliers ?? [TaperVolumeMultiplier];

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

    /// <summary>HM.2 dark-only HALF_MARATHON×INTERMEDIATE×4D×14W authority. Taper multipliers are independently applied to the fixed anchor; 19km is a ceiling, never a target.</summary>
    public static VolumeSafetyPolicy HalfMarathonIntermediate4D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 25d,
        ResolvedPeakReference: new(43d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 11,
        TaperVolumeMultiplier: HalfMarathonIntermediate4DTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: 0.30d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.33d,
        LongRunHardCapShare: 0.40d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonIntermediate4DTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 19d,
        // HM.5.3 -- the only named policy where this is true. 43.0km is a frozen SELECTED PEAK
        // CEILING for the whole 10-16W Core family (HM.5.3's own governing authority), not a
        // calibration point 15W/16W may extrapolate past. Every 10K policy leaves this false
        // (default) and keeps its own already-shipped extrapolation-past-calibration behavior
        // for longer horizons unchanged (e.g. BeginnerFourDay's 13W/14W).
        ResolvedPeakReferenceIsSelectedCeiling: true);

    /// <summary>
    /// PHASE DIST-GEN.2 -- implements the already-frozen DIST-GEN.1 §47 complete
    /// policy table for the dark-only 16K x INTERMEDIATE x 4D pilot
    /// (TargetDistanceKm=16.0, ParentDistanceFamily=HALF_MARATHON). Every field's
    /// value and classification is consumed exactly as frozen, not re-derived
    /// here: PreferredMaxWeeklyIncreaseRatio/HardMaxWeeklyIncreaseRatio = 0.07/0.08
    /// (DIRECT_EXISTING_AUTHORITY, DIST-GEN.1 §24, identical to every named policy
    /// in this file); AbsoluteWeeklyIncrementCapKm = 2.5 (DIRECT_EXISTING_AUTHORITY,
    /// §25, both HM and TEN_K anchors agree exactly at 4D); GoldenFixtureStartingVolumeKm
    /// = 23.5 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, §26 -- <see cref="HalfMarathonIntermediate4D"/>'s
    /// own 25.0/43.0 = 0.5814 ratio applied to this pilot's own new peak,
    /// 40.0 x 0.5814 = 23.26, rounded to the 0.5km catalog increment = 23.5; NEVER
    /// a missing-readiness fallback -- the same fail-closed
    /// <see cref="CatalogVolumeAndLongRunPlanner.ResolveStartingVolume"/> guard every
    /// other named policy in this file already uses applies unchanged); ResolvedPeakReference
    /// = 40.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, §22-23, exact midpoint of the
    /// frozen [34,46] PeakVolumeBand -- see <see cref="TargetDistance16KPeakVolumeBandPolicy"/>
    /// for the band itself, encoded separately since <see cref="VolumeSafetyPolicy"/>
    /// carries only the resolved reference, not the band bounds, mirroring every
    /// other named policy here); GoldenFixtureNonTaperTransitions = 9 (this pilot's
    /// own uniquely-derived value at its 12W preferred horizon: 12W - 2W taper = 10
    /// non-taper weeks -&gt; 9 transitions, §38 -- distinct from both TEN_K's 10 and
    /// HM's 11); TaperVolumeMultiplier(s) = 0.70/0.43 (STRONG_EVIDENCE_DERIVED_DECISION,
    /// §31, <see cref="TargetDistance16KTaperVolumePolicy"/>); long-run share
    /// quadruple 0.30/0.36/0.33/0.40 (STRONG_EVIDENCE_DERIVED_DECISION, §27, both
    /// anchors agree exactly at 4D); PreferredAbsolutePeakLongRunKm = 15.0
    /// (STRONG_EVIDENCE_DERIVED_DECISION, §28, strictly below the 16.0km target
    /// distance and below HM's own 19.0km ceiling); ResolvedPeakReferenceIsSelectedCeiling
    /// = true (DIRECT_EXISTING_AUTHORITY, the same generic HM.5.3 mechanism every
    /// other named HM-parented policy in this file already uses, reused unmodified
    /// so 13W/14W saturate at 40.0km instead of extrapolating past it -- §39).
    /// StartingAbsoluteLongRunCapKm = N/A (DIRECT_EXISTING_AUTHORITY, §29, mirrors
    /// <see cref="HalfMarathonIntermediate4D"/>'s own null). Dark-only: no public
    /// routing/gate is widened by this named policy's existence -- reachable only
    /// through the <see cref="RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy"/>-gated
    /// internal seam, never through any public request DTO.
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonIntermediate4DTargetDistance16K { get; } = new(
        // DIST-GEN.13 -- SHARED_LEVEL_FREQUENCY_AUTHORITY, referenced not copied.
        // No value changed: 0.07/0.08/2.5 and 0.30/0.36/0.33/0.40 exactly as frozen.
        PreferredMaxWeeklyIncreaseRatio: IntermediateFourDayGrowthAuthority.PreferredWeeklyIncreaseRate,
        HardMaxWeeklyIncreaseRatio: IntermediateFourDayGrowthAuthority.HardWeeklyIncreaseRate,
        AbsoluteWeeklyIncrementCapKm: IntermediateFourDayGrowthAuthority.AbsoluteWeeklyIncreaseCapKm,
        // EXACT-TARGET below (DIST-GEN.12 §54) -- never normalized.
        GoldenFixtureStartingVolumeKm: 23.5d,
        ResolvedPeakReference: new(40.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 9,
        TaperVolumeMultiplier: TargetDistance16KTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: IntermediateFourDayGrowthAuthority.LongRunPreferredMinimumShare,
        LongRunPreferredMaximumShare: IntermediateFourDayGrowthAuthority.LongRunPreferredMaximumShare,
        LongRunSelectionShare: IntermediateFourDayGrowthAuthority.LongRunSelectionShare,
        LongRunHardCapShare: IntermediateFourDayGrowthAuthority.LongRunHardCapShare,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: TargetDistance16KTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 15.0d,
        // DIST-GEN.1 §23/§39 -- 40.0km is a frozen SELECTED PEAK CEILING for this
        // pilot's own 8-14W Core family, not a calibration point 13W/14W may
        // extrapolate past. Reuses HM.5.3's own generic mechanism unmodified.
        ResolvedPeakReferenceIsSelectedCeiling: true)
    {
        // DIST-GEN.13 -- frequency authority (null at 4D), referenced explicitly
        // rather than left implicit. Value unchanged: null, exactly as before.
        StartingAbsoluteLongRunCapKm = IntermediateFourDayGrowthAuthority.StartingAbsoluteLongRunCapKm,
    };

    /// <summary>
    /// PHASE DIST-GEN.6 -- implements the already-frozen DIST-GEN.5 §53 complete
    /// policy table for the dark-only, SECOND target-distance 15K x INTERMEDIATE
    /// x 4D projection (TargetDistanceKm=15.0, ParentDistanceFamily=HALF_MARATHON),
    /// reusing the identical HALF_MARATHON__4D__INTERMEDIATE catalog identity the
    /// 16K pilot above already reuses. Every field's value and classification is
    /// consumed exactly as frozen, not re-derived here. Every numeric field is
    /// byte-identical to <see cref="HalfMarathonIntermediate4DTargetDistance16K"/>'s
    /// own values -- DIST-GEN.5's own real, independently re-derived evidence
    /// convergence (§49/§57), NOT template-copying -- with exactly ONE
    /// deliberate, mechanically-forced exception:
    /// <see cref="PreferredAbsolutePeakLongRunKm"/> = 14.5 (NOT 15.0). DIST-GEN.5
    /// §35 -- 16K's own 15.0km ceiling cannot be reused for this 15.0km target
    /// without violating the binding "peak long run strictly below target
    /// distance" invariant; 14.5km is both real-evidence-supported (DIST-GEN.5
    /// §26/§35, 15K-specific sources citing 8-14.5km peak long runs) and
    /// satisfies the invariant with a real 0.5km margin.
    ///
    /// PreferredMaxWeeklyIncreaseRatio/HardMaxWeeklyIncreaseRatio = 0.07/0.08
    /// (DIRECT_EXISTING_AUTHORITY, DIST-GEN.5 §30); AbsoluteWeeklyIncrementCapKm
    /// = 2.5 (DIRECT_EXISTING_AUTHORITY, §31); GoldenFixtureStartingVolumeKm =
    /// 23.5 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, §32 -- the same 25.0/43.0=0.5814
    /// ratio applied to this target's own 40.0 peak, 40.0 x 0.5814 = 23.26,
    /// rounded to 23.5; never a missing-readiness fallback); ResolvedPeakReference
    /// = 40.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, §29, exact midpoint of the
    /// frozen [34,46] PeakVolumeBand -- see <see cref="TargetDistance15KPeakVolumeBandPolicy"/>);
    /// GoldenFixtureNonTaperTransitions = 9 (identical derivation to 16K's own,
    /// since the horizon triple and taper length are identical: 12 preferred
    /// weeks - 2 taper weeks - 1 = 9); TaperVolumeMultiplier(s) = 0.70/0.43
    /// (STRONG_EVIDENCE_DERIVED_DECISION, §38, <see cref="TargetDistance15KTaperVolumePolicy"/>);
    /// long-run share quadruple 0.30/0.36/0.33/0.40 (DIRECT_EXISTING_AUTHORITY,
    /// §33, byte-identical across every Intermediate 4D anchor);
    /// PreferredAbsolutePeakLongRunKm = 14.5 (STRONG_EVIDENCE_DERIVED_DECISION,
    /// §35, the one genuinely different field -- see above);
    /// ResolvedPeakReferenceIsSelectedCeiling = true (DIRECT_EXISTING_AUTHORITY,
    /// the same generic HM.5.3 mechanism every other named HM-parented policy in
    /// this file already uses, reused unmodified so 13W/14W saturate at 40.0km
    /// instead of extrapolating past it). StartingAbsoluteLongRunCapKm = N/A
    /// (DIRECT_EXISTING_AUTHORITY, §36, mirrors <see cref="HalfMarathonIntermediate4D"/>'s
    /// own null). Dark-only: no public routing/gate is widened by this named
    /// policy's existence -- reachable only through the
    /// <see cref="RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy"/>
    /// registry's dark-eligibility resolution, never through any public request DTO.
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonIntermediate4DTargetDistance15K { get; } = new(
        // DIST-GEN.13 -- SHARED_LEVEL_FREQUENCY_AUTHORITY, referenced not copied.
        // No value changed: 0.07/0.08/2.5 and 0.30/0.36/0.33/0.40 exactly as frozen.
        PreferredMaxWeeklyIncreaseRatio: IntermediateFourDayGrowthAuthority.PreferredWeeklyIncreaseRate,
        HardMaxWeeklyIncreaseRatio: IntermediateFourDayGrowthAuthority.HardWeeklyIncreaseRate,
        AbsoluteWeeklyIncrementCapKm: IntermediateFourDayGrowthAuthority.AbsoluteWeeklyIncreaseCapKm,
        // EXACT-TARGET below (DIST-GEN.12 §54) -- never normalized.
        GoldenFixtureStartingVolumeKm: 23.5d,
        ResolvedPeakReference: new(40.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 9,
        TaperVolumeMultiplier: TargetDistance15KTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: IntermediateFourDayGrowthAuthority.LongRunPreferredMinimumShare,
        LongRunPreferredMaximumShare: IntermediateFourDayGrowthAuthority.LongRunPreferredMaximumShare,
        LongRunSelectionShare: IntermediateFourDayGrowthAuthority.LongRunSelectionShare,
        LongRunHardCapShare: IntermediateFourDayGrowthAuthority.LongRunHardCapShare,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: TargetDistance15KTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 14.5d,
        // DIST-GEN.5 §29/§47 -- 40.0km is a frozen SELECTED PEAK CEILING for this
        // target's own 10-14W Core family, not a calibration point 13W/14W may
        // extrapolate past. Reuses HM.5.3's own generic mechanism unmodified.
        ResolvedPeakReferenceIsSelectedCeiling: true)
    {
        // DIST-GEN.13 -- frequency authority (null at 4D), referenced explicitly
        // rather than left implicit. Value unchanged: null, exactly as before.
        StartingAbsoluteLongRunCapKm = IntermediateFourDayGrowthAuthority.StartingAbsoluteLongRunCapKm,
    };

    /// <summary>
    /// PHASE DIST-GEN.10 -- implements the already-frozen DIST-GEN.9 §53 complete
    /// policy table for the dark-only, THIRD target-distance 18K x INTERMEDIATE
    /// x 4D projection (TargetDistanceKm=18.0, ParentDistanceFamily=HALF_MARATHON),
    /// reusing the identical HALF_MARATHON__4D__INTERMEDIATE catalog identity the
    /// 15K/16K targets above already reuse. Every field's value and
    /// classification is consumed exactly as frozen, not re-derived here. Every
    /// numeric field except <see cref="PreferredAbsolutePeakLongRunKm"/> is
    /// byte-identical to <see cref="HalfMarathonIntermediate4DTargetDistance15K"/>'s
    /// and <see cref="HalfMarathonIntermediate4DTargetDistance16K"/>'s own values
    /// -- DIST-GEN.9's own real, independently re-derived evidence convergence
    /// (§54/§55), NOT template-copying.
    ///
    /// PreferredMaxWeeklyIncreaseRatio/HardMaxWeeklyIncreaseRatio = 0.07/0.08 and
    /// AbsoluteWeeklyIncrementCapKm = 2.5 are SHARED level/frequency authority
    /// (DIRECT_EXISTING_AUTHORITY, DIST-GEN.9 §54 -- identical across every
    /// Intermediate 4D anchor including canonical TEN_K/HM, not re-derived for
    /// this target). GoldenFixtureStartingVolumeKm = 23.5 is a mechanical
    /// consequence of the SAME shared 0.5814 ratio methodology
    /// (<see cref="HalfMarathonIntermediate4D"/>'s own 25.0/43.0 ratio) applied
    /// to this target's own peak (40.0 x 0.5814 = 23.26, rounded to 23.5 --
    /// EVIDENCE_INFORMED_PRODUCT_DEFAULT, never a missing-readiness fallback).
    /// ResolvedPeakReference = 40.0 is this target's own SELECTED PEAK within
    /// the [34,46] band (<see cref="TargetDistance18KPeakVolumeBandPolicy"/>),
    /// discovered-identical to both siblings' own selection, not re-derived.
    /// GoldenFixtureNonTaperTransitions = 9 (identical derivation to 15K's/16K's
    /// own, since the horizon triple and taper length are identical: 12
    /// preferred weeks - 2 taper weeks - 1 = 9). Long-run share quadruple
    /// 0.30/0.36/0.33/0.40 is SHARED level/frequency authority (identical across
    /// every Intermediate 4D anchor). TaperVolumeMultiplier(s) = 0.70/0.43 are
    /// SHARED PARENT-FAMILY authority, inherited from HALF_MARATHON's own 2-week
    /// taper (never distance-banded -- TEN_K's own genuinely different 1-week/
    /// 0.53 taper proves this is parent-family, not target-specific) --
    /// deliberately reused DIRECTLY from <see cref="TargetDistance16KTaperVolumePolicy"/>'s
    /// own already-declared constants rather than mechanically creating a third,
    /// near-identical <c>TargetDistance18KTaperVolumePolicy</c> class: DIST-GEN.9
    /// explicitly classified taper as SHARED_PARENT_FAMILY_AUTHORITY, not
    /// target-specific, and this repo's own governing instruction warns against
    /// representing every field as if it were an independent target decision.
    /// See PHASE_DIST_GEN_10_...md §33/§37 for the full reasoning.
    ///
    /// <see cref="PreferredAbsolutePeakLongRunKm"/> = 16.0 (STRONG_EVIDENCE_DERIVED_DECISION,
    /// DIST-GEN.9 §30, THE primary target-specific decision this policy encodes
    /// -- deliberately different from both 15K's 14.5 and 16K's 15.0: strictly
    /// below this 18.0km target distance, and below HM's own 19.0km ceiling).
    /// ResolvedPeakReferenceIsSelectedCeiling = true (DIRECT_EXISTING_AUTHORITY,
    /// the same generic HM.5.3 mechanism every other named HM-parented policy in
    /// this file already uses, reused unmodified so 13W/14W saturate at 40.0km
    /// instead of extrapolating past it). StartingAbsoluteLongRunCapKm = N/A
    /// (SHARED authority, mirrors <see cref="HalfMarathonIntermediate4D"/>'s own
    /// null). Dark-only: no public routing/gate is widened by this named
    /// policy's existence -- reachable only through the
    /// <see cref="RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy"/>
    /// registry's dark-eligibility resolution, never through any public request DTO.
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonIntermediate4DTargetDistance18K { get; } = new(
        // DIST-GEN.13 -- SHARED_LEVEL_FREQUENCY_AUTHORITY, referenced not copied.
        // No value changed: 0.07/0.08/2.5 and 0.30/0.36/0.33/0.40 exactly as frozen.
        PreferredMaxWeeklyIncreaseRatio: IntermediateFourDayGrowthAuthority.PreferredWeeklyIncreaseRate,
        HardMaxWeeklyIncreaseRatio: IntermediateFourDayGrowthAuthority.HardWeeklyIncreaseRate,
        AbsoluteWeeklyIncrementCapKm: IntermediateFourDayGrowthAuthority.AbsoluteWeeklyIncreaseCapKm,
        // EXACT-TARGET below (DIST-GEN.12 §54) -- never normalized.
        GoldenFixtureStartingVolumeKm: 23.5d,
        ResolvedPeakReference: new(40.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 9,
        // DIST-GEN.10 reused 16K's own taper constants directly rather than
        // declaring a third near-duplicate class; DIST-GEN.13 leaves that
        // reference in place, and it now resolves transitively to the single
        // HalfMarathonParentTaperAuthority -- SHARED_PARENT_FAMILY_AUTHORITY,
        // value unchanged at 0.70/0.43.
        TaperVolumeMultiplier: TargetDistance16KTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: IntermediateFourDayGrowthAuthority.LongRunPreferredMinimumShare,
        LongRunPreferredMaximumShare: IntermediateFourDayGrowthAuthority.LongRunPreferredMaximumShare,
        LongRunSelectionShare: IntermediateFourDayGrowthAuthority.LongRunSelectionShare,
        LongRunHardCapShare: IntermediateFourDayGrowthAuthority.LongRunHardCapShare,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: TargetDistance16KTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 16.0d,
        // DIST-GEN.9 §55 -- 40.0km is a frozen SELECTED PEAK CEILING for this
        // target's own 10-14W Core family, not a calibration point 13W/14W may
        // extrapolate past. Reuses HM.5.3's own generic mechanism unmodified.
        ResolvedPeakReferenceIsSelectedCeiling: true)
    {
        // DIST-GEN.13 -- frequency authority (null at 4D), referenced explicitly
        // rather than left implicit. Value unchanged: null, exactly as before.
        StartingAbsoluteLongRunCapKm = IntermediateFourDayGrowthAuthority.StartingAbsoluteLongRunCapKm,
    };

    /// <summary>
    /// Phase HM.8 -- implements the already-frozen HM.7 numeric authority for
    /// HALF_MARATHON x INTERMEDIATE x 3D x 10-16W x DARK
    /// (PHASE_HM_7_INTERMEDIATE_3D_NUMERIC_AUTHORITY_CLOSURE.md's complete
    /// authority table). Every field's value and classification is consumed
    /// exactly as frozen, not re-derived here: PreferredMaxWeeklyIncreaseRatio
    /// /HardMaxWeeklyIncreaseRatio = 0.07/0.08 (DIRECT_EXISTING_AUTHORITY,
    /// universal across every named policy in this file);
    /// AbsoluteWeeklyIncrementCapKm = 2.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// reused from <see cref="ThreeDayIntermediate"/>'s own on-point 10K
    /// same-Level-same-Frequency precedent, NOT 4D's 2.5);
    /// GoldenFixtureStartingVolumeKm = 20.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// calibration-only -- derived by reusing <see cref="HalfMarathonIntermediate4D"/>'s
    /// own 25.0/43.0 = 0.5814 ratio against this policy's own 34.0 peak,
    /// 34.0 x 0.5814 = 19.77, rounded to the 0.5km catalog increment = 20.0;
    /// NEVER a missing-readiness fallback -- see <see cref="CatalogVolumeAndLongRunPlanner.ResolveStartingVolume"/>'s
    /// fail-closed guard for this exact policy, mirroring 4D's own);
    /// ResolvedPeakReference = 34.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, the
    /// exact midpoint of the frozen [28,40] PeakVolumeBand, replicating 4D's
    /// own observed midpoint-selection pattern -- not a new methodology);
    /// GoldenFixtureNonTaperTransitions = 11 (DIRECT_EXISTING_AUTHORITY,
    /// frequency-generic structural derivation from HM's already-frozen
    /// 3+4+5=12 non-taper-week Foundation/Build/RaceSpecific split, HM.4/HM.6);
    /// TaperVolumeMultiplier(s) = 0.70/0.43 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// reused directly from <see cref="HalfMarathonIntermediate4DTaperVolumePolicy"/>
    /// -- see <see cref="HalfMarathonIntermediate3DTaperVolumePolicy"/>'s own
    /// doc comment for the full frequency-independence re-verification);
    /// long-run share quadruple 0.34/0.40/0.38/0.46 (PRODUCT_DEFAULT, an
    /// explicit user product decision per HM.7 §9 -- a moderate, still-
    /// conservative uplift from 4D's 0.30/0.36/0.33/0.40, informed by but not
    /// copied from two independent real-world 3-day HM program sources and
    /// 10K's own <see cref="ThreeDayIntermediate"/> vs <see cref="Default"/>
    /// directional precedent); PreferredAbsolutePeakLongRunKm = 19.0
    /// (EVIDENCE_INFORMED_PRODUCT_DEFAULT, HM.1.1's own evidence base
    /// broadened by HM.7 §10 to HALF_MARATHON x INTERMEDIATE, frequency-
    /// independent -- a ceiling, never a target: HM.7 §5's own feasibility
    /// check confirms this cell's hard-cap-derived long run never exceeds
    /// ~18.5km even at the band's own upper bound, so the ceiling is never
    /// required to bind); ResolvedPeakReferenceIsSelectedCeiling = true
    /// (DIRECT_EXISTING_AUTHORITY, the same generic HM.5.3 mechanism 4D
    /// already uses, reused unmodified so 15W/16W saturate at 34.0km instead
    /// of extrapolating past it -- zero new code, per HM.7 §14).
    /// StartingAbsoluteLongRunCap = 12.0km (DIRECT_EXISTING_AUTHORITY, HM.7
    /// §11, frequency-independent) is documented authority only: no
    /// VolumeSafetyPolicy field or planner clamp implements it for ANY named
    /// policy today (including <see cref="HalfMarathonIntermediate4D"/>) --
    /// real user readiness evidence (recent weekly volume/longest run)
    /// remains the sole basis for a request's actual starting point, exactly
    /// as for 4D; this phase invents no new mechanism to enforce a cap 4D
    /// itself does not enforce.
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonIntermediate3D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.0d,
        GoldenFixtureStartingVolumeKm: 20.0d,
        ResolvedPeakReference: new(34.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 11,
        TaperVolumeMultiplier: HalfMarathonIntermediate3DTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: 0.34d,
        LongRunPreferredMaximumShare: 0.40d,
        LongRunSelectionShare: 0.38d,
        LongRunHardCapShare: 0.46d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonIntermediate3DTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 19.0d,
        // HM.7/HM.5.3 -- reused unmodified: 34.0km is a frozen SELECTED PEAK CEILING for the
        // whole 10-16W Core family, not a calibration point 15W/16W may extrapolate past.
        ResolvedPeakReferenceIsSelectedCeiling: true);

    /// <summary>
    /// Phase HM.11 -- implements the already-frozen HM.10 numeric authority for
    /// HALF_MARATHON x INTERMEDIATE x 5D x 10-16W x DARK
    /// (PHASE_HM_10_INTERMEDIATE_5D_NUMERIC_AUTHORITY_CLOSURE.md's complete
    /// authority table, §20). Every field's value and classification is
    /// consumed exactly as frozen, not re-derived here:
    /// PreferredMaxWeeklyIncreaseRatio/HardMaxWeeklyIncreaseRatio = 0.07/0.08
    /// (DIRECT_EXISTING_AUTHORITY, universal across every named policy in this
    /// file); AbsoluteWeeklyIncrementCapKm = 2.5 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// the direct on-point 10K <see cref="FiveDayIntermediate"/> precedent, NOT
    /// interpolated between 3D's 2.0 and 4D's 2.5); GoldenFixtureStartingVolumeKm
    /// = 29.5 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, calibration-only -- derived by
    /// reusing <see cref="HalfMarathonIntermediate4D"/>'s own 25.0/43.0 = 0.5814
    /// ratio against this policy's own 51.0 peak, 51.0 x 0.5814 = 29.65, rounded
    /// to the 0.5km catalog increment = 29.5; NEVER a missing-readiness fallback
    /// -- see <see cref="CatalogVolumeAndLongRunPlanner.ResolveStartingVolume"/>'s
    /// fail-closed guard for this exact policy, mirroring 4D's/3D's own);
    /// ResolvedPeakReference = 51.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, the exact
    /// midpoint of the frozen [44,58] PeakVolumeBand, a now-thrice-confirmed
    /// Appsel midpoint-selection convention); GoldenFixtureNonTaperTransitions =
    /// 11 (DIRECT_EXISTING_AUTHORITY, frequency-generic structural derivation
    /// from HM's already-frozen phase split, HM.4/HM.6); TaperVolumeMultiplier(s)
    /// = 0.70/0.43 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, reused directly from
    /// <see cref="HalfMarathonIntermediate4DTaperVolumePolicy"/> -- see
    /// <see cref="HalfMarathonIntermediate5DTaperVolumePolicy"/>'s own doc
    /// comment for the full frequency-independence re-verification); long-run
    /// share quadruple 0.28/0.36/0.28/0.36 (PRODUCT_DEFAULT, HM.10 §9 -- a
    /// conservative *decrease* from 4D's 0.30/0.36/0.33/0.40, derived by
    /// applying 10K's own real, observed 4D-to-5D directional deltas to HM's
    /// frozen 4D baseline, not copied from 10K's numbers even though they
    /// coincide); PreferredAbsolutePeakLongRunKm = 19.0 (DIRECT_EXISTING_AUTHORITY,
    /// HM.1.1's own evidence base, already broadened to HALF_MARATHON x
    /// INTERMEDIATE, frequency-independent -- a ceiling, never a target);
    /// ResolvedPeakReferenceIsSelectedCeiling = true (DIRECT_EXISTING_AUTHORITY,
    /// the same generic HM.5.3 mechanism 4D/3D already use, reused unmodified so
    /// 15W/16W saturate at 51.0km instead of extrapolating past it -- zero new
    /// code, per HM.10 §15). StartingAbsoluteLongRunCap = 12.0km
    /// (DIRECT_EXISTING_AUTHORITY, HM.10 §11, frequency-independent) is the
    /// opt-in first-Core-week hard ceiling; it never substitutes for missing
    /// recent-longest-run evidence.
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonIntermediate5D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 29.5d,
        ResolvedPeakReference: new(51.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 11,
        TaperVolumeMultiplier: HalfMarathonIntermediate5DTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: 0.28d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.28d,
        LongRunHardCapShare: 0.36d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonIntermediate5DTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 19.0d,
        // HM.10/HM.5.3 -- reused unmodified: 51.0km is a frozen SELECTED PEAK CEILING for the
        // whole 10-16W Core family, not a calibration point 15W/16W may extrapolate past.
        ResolvedPeakReferenceIsSelectedCeiling: true)
    {
        StartingAbsoluteLongRunCapKm = 12.0d,
    };

    /// <summary>
    /// Phase HM.14 -- implements the already-frozen HM.13 numeric authority for
    /// HALF_MARATHON x BEGINNER x 3D x 10-16W x DARK
    /// (PHASE_HM_13_BEGINNER_3D_4D_5D_NUMERIC_AND_LEVEL_AUTHORITY_CLOSURE.md §24's
    /// complete Beginner 3D authority table). Every field's value and
    /// classification is consumed exactly as frozen, not re-derived here:
    /// PreferredMaxWeeklyIncreaseRatio/HardMaxWeeklyIncreaseRatio = 0.07/0.08
    /// (DIRECT_EXISTING_AUTHORITY, universal across every named policy in this
    /// file); AbsoluteWeeklyIncrementCapKm = 2.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// direct on-point 10K <see cref="ThreeDayBeginner"/> same-level-same-frequency
    /// precedent, HM.13 §10); GoldenFixtureStartingVolumeKm = 15.5
    /// (EVIDENCE_INFORMED_PRODUCT_DEFAULT, HM.13 §11 -- <see cref="HalfMarathonIntermediate4D"/>'s
    /// own 25.0/43.0 = 0.5814 ratio extended across levels for the first time,
    /// applied to this policy's own 27.0 peak: 27.0 x 0.5814 = 15.70, rounded
    /// to the 0.5km catalog increment = 15.5; NEVER a missing-readiness
    /// fallback -- the same fail-closed guard <see cref="CatalogVolumeAndLongRunPlanner.ResolveStartingVolume"/>
    /// already applies to <see cref="HalfMarathonIntermediate3D"/>/<see cref="HalfMarathonIntermediate4D"/>/<see cref="HalfMarathonIntermediate5D"/> is
    /// mechanically extended to this policy too); ResolvedPeakReference = 27.0
    /// (EVIDENCE_INFORMED_PRODUCT_DEFAULT, the exact midpoint of the frozen
    /// [22,32] PeakVolumeBand, HM.13 §7/§8); GoldenFixtureNonTaperTransitions =
    /// 11 (DIRECT_EXISTING_AUTHORITY, the same frequency-generic structural
    /// derivation from HM's already-frozen 3+4+5=12 non-taper-week
    /// Foundation/Build/RaceSpecific split every other named HM policy in
    /// this file uses, re-verified as still correct for Beginner since the
    /// Foundation/Build/RaceSpecific/Taper split is carried by
    /// HALF_MARATHON_MASTER, which has no level field, HM.13 §15); TaperVolumeMultiplier(s)
    /// = 0.70/0.43 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, reused directly from
    /// <see cref="HalfMarathonBeginner3DTaperVolumePolicy"/>, HM.13 §16 --
    /// re-verified, and found strengthened, scope: Beginner's own lower peak
    /// sits further below best-running-tips.com's ~56km/week lighter-taper
    /// threshold than any Intermediate cell does); long-run share quadruple
    /// 0.34/0.40/0.38/0.46 (STRONG_EVIDENCE_DERIVED_DECISION, HM.13 §12 --
    /// reused verbatim from <see cref="HalfMarathonIntermediate3D"/>, the
    /// strongest evidentiary tier this phase reached, earned by 10K's own
    /// twice-independently-confirmed real precedent that Beginner's LR-share
    /// quadruple equals the same-frequency Intermediate quadruple exactly
    /// when the field is frequency-owned); PreferredAbsolutePeakLongRunKm =
    /// 16.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, HM.13 §13 -- HM.1.1's own
    /// already-accepted B.A.A. Level One ~9-10mi/14.5-16.1km evidence,
    /// Beginner-wide, frequency-independent within Beginner, a ceiling never
    /// a target -- HM.13 §22 confirms this ceiling never actually binds at
    /// this cell's own selected peak x selected share, 27.0x0.38=10.26km);
    /// ResolvedPeakReferenceIsSelectedCeiling = true (DIRECT_EXISTING_AUTHORITY,
    /// the same generic HM.5.3 mechanism every other named HM policy in this
    /// file already uses, reused unmodified so 15W/16W saturate at 27.0km
    /// instead of extrapolating past it -- zero new code, HM.13 §22).
    /// StartingAbsoluteLongRunCap = N/A (NOT_APPLICABLE, HM.13 §14, mirrors
    /// <see cref="HalfMarathonIntermediate3D"/>'s own null -- the historical
    /// 8km legacy candidate is explicitly not promoted).
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonBeginner3D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.0d,
        GoldenFixtureStartingVolumeKm: 15.5d,
        ResolvedPeakReference: new(27.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 11,
        TaperVolumeMultiplier: HalfMarathonBeginner3DTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: 0.34d,
        LongRunPreferredMaximumShare: 0.40d,
        LongRunSelectionShare: 0.38d,
        LongRunHardCapShare: 0.46d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonBeginner3DTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 16.0d,
        // HM.13/HM.14/HM.5.3 -- reused unmodified: 27.0km is a frozen SELECTED PEAK CEILING for
        // the whole 10-16W Core family, not a calibration point 15W/16W may extrapolate past.
        ResolvedPeakReferenceIsSelectedCeiling: true);

    /// <summary>
    /// Phase HM.14 -- implements the already-frozen HM.13 numeric authority for
    /// HALF_MARATHON x BEGINNER x 4D x 10-16W x DARK
    /// (PHASE_HM_13_BEGINNER_3D_4D_5D_NUMERIC_AND_LEVEL_AUTHORITY_CLOSURE.md §24's
    /// complete Beginner 4D authority table). Mirrors <see cref="HalfMarathonBeginner3D"/>'s
    /// own doc comment shape exactly, at 4D's own frozen values:
    /// AbsoluteWeeklyIncrementCapKm = 2.5 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// direct on-point 10K <see cref="BeginnerFourDay"/> precedent, HM.13 §10);
    /// GoldenFixtureStartingVolumeKm = 19.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// same 0.5814 cross-level ratio applied to this policy's own 33.0 peak:
    /// 33.0 x 0.5814 = 19.19, rounded to 19.0, HM.13 §11; NEVER a missing-readiness
    /// fallback, same mechanically-extended fail-closed guard); ResolvedPeakReference
    /// = 33.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, exact midpoint of the frozen
    /// [28,38] PeakVolumeBand, HM.13 §7/§8); GoldenFixtureNonTaperTransitions = 11
    /// (DIRECT_EXISTING_AUTHORITY, same frequency-generic derivation);
    /// TaperVolumeMultiplier(s) = 0.70/0.43 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// <see cref="HalfMarathonBeginner4DTaperVolumePolicy"/>, HM.13 §16); long-run
    /// share quadruple 0.30/0.36/0.33/0.40 (STRONG_EVIDENCE_DERIVED_DECISION,
    /// HM.13 §12 -- reused verbatim from <see cref="HalfMarathonIntermediate4D"/>);
    /// PreferredAbsolutePeakLongRunKm = 16.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// same Beginner-wide ceiling as 3D, HM.13 §13 -- never binds at this cell's
    /// own 33.0x0.33=10.89km); ResolvedPeakReferenceIsSelectedCeiling = true
    /// (DIRECT_EXISTING_AUTHORITY, same generic HM.5.3 mechanism). StartingAbsoluteLongRunCap
    /// = N/A (NOT_APPLICABLE, HM.13 §14, mirrors <see cref="HalfMarathonIntermediate4D"/>'s
    /// own null).
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonBeginner4D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 19.0d,
        ResolvedPeakReference: new(33.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 11,
        TaperVolumeMultiplier: HalfMarathonBeginner4DTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: 0.30d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.33d,
        LongRunHardCapShare: 0.40d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonBeginner4DTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 16.0d,
        // HM.13/HM.14/HM.5.3 -- reused unmodified: 33.0km is a frozen SELECTED PEAK CEILING for
        // the whole 10-16W Core family, not a calibration point 15W/16W may extrapolate past.
        ResolvedPeakReferenceIsSelectedCeiling: true);

    /// <summary>
    /// Phase HM.14 -- centralizes the HALF_MARATHON Beginner daysPerWeek-to-policy
    /// dispatch, mirroring <see cref="ForHalfMarathonIntermediateDaysPerWeek"/>'s
    /// own shape and 10K's own <see cref="ForBeginnerDaysPerWeek"/> sibling-method
    /// pattern. Fail-closed for any HALF_MARATHON Beginner frequency without an
    /// approved policy -- deliberately NO 5-arm: HM.13 §6 froze HALF_MARATHON
    /// Beginner x5D as PRODUCT_INELIGIBLE (real, on-point internal evidence: HM's
    /// own named rollout matrix + 10K's own real precedent), not a gap awaiting a
    /// future decision.
    /// </summary>
    public static VolumeSafetyPolicy ForHalfMarathonBeginnerDaysPerWeek(int daysPerWeek) => daysPerWeek switch
    {
        3 => HalfMarathonBeginner3D,
        4 => HalfMarathonBeginner4D,
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), daysPerWeek, "No approved HALF_MARATHON Beginner VolumeSafetyPolicy exists for this DaysPerWeek (5D is PRODUCT_INELIGIBLE, HM.13 §6)."),
    };

    /// <summary>
    /// Phase HM.8 -- centralizes the HALF_MARATHON Intermediate daysPerWeek-to-policy
    /// dispatch, mirroring 10K's own already-established <see cref="ForIntermediateDaysPerWeek"/>
    /// switch-based pattern (HM.6's own §19 finding: the pre-HM.8 dispatch was a
    /// single hardcoded exact-match <c>DaysPerWeek == 4</c> conditional in
    /// <see cref="CatalogVolumeAndLongRunPlanner"/>, not a typed dispatcher).
    /// Fail-closed for any HALF_MARATHON Intermediate frequency without an
    /// approved policy -- byte-identical selection for the existing 4D
    /// caller (still resolves to <see cref="HalfMarathonIntermediate4D"/>).
    /// Phase HM.11 -- widened again to admit 5D (<see cref="HalfMarathonIntermediate5D"/>).
    /// </summary>
    public static VolumeSafetyPolicy ForHalfMarathonIntermediateDaysPerWeek(int daysPerWeek) => daysPerWeek switch
    {
        3 => HalfMarathonIntermediate3D,
        4 => HalfMarathonIntermediate4D,
        5 => HalfMarathonIntermediate5D,
        6 => HalfMarathonIntermediate6D,
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), daysPerWeek, "No approved HALF_MARATHON Intermediate VolumeSafetyPolicy exists for this DaysPerWeek."),
    };

    /// <summary>
    /// Phase HM-X1.3 -- implements the already-frozen HM-X1.2 numeric authority for
    /// HALF_MARATHON x INTERMEDIATE x 6D x 10-16W x DARK
    /// (PHASE_HM_X1_2_..._STRUCTURAL_AND_NUMERIC_AUTHORITY_CLOSURE.md §34's complete
    /// authority table). Every field's value and classification is consumed exactly
    /// as frozen, not re-derived here. Every numeric field is byte-identical to
    /// <see cref="HalfMarathonIntermediate5D"/>'s own values -- HM-X1.2 §10/§12/§13/§14/
    /// §15/§16/§19/§20's own band-width-stability/zero-delta methodology, directly
    /// mirroring 10K's own real, approved <c>SixDayIntermediate</c> being byte-identical
    /// to <c>FiveDayIntermediate</c> on every volume field for the identical transition.
    /// PeakVolumeBandMin/Max = [44,58] (STRONG_EVIDENCE_DERIVED_DECISION, §10);
    /// SelectedPeakReference = 51.0 (unchanged midpoint, §12); PreferredMaxWeeklyIncreaseRatio/
    /// HardMaxWeeklyIncreaseRatio = 0.07/0.08 (DIRECT_EXISTING_AUTHORITY, §13);
    /// AbsoluteWeeklyIncrementCapKm = 2.5 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, §14);
    /// GoldenFixtureStartingVolumeKm = 29.5 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// ratio-reuse methodology, §15); GoldenFixtureNonTaperTransitions = 11
    /// (DIRECT_EXISTING_AUTHORITY, §26); long-run share quadruple 0.28/0.36/0.28/0.36
    /// (STRONG_EVIDENCE_DERIVED_DECISION, §16); PreferredAbsolutePeakLongRunKm = 19.0
    /// (DIRECT_EXISTING_AUTHORITY, §19); StartingAbsoluteLongRunCapKm = 12.0
    /// (DIRECT_EXISTING_AUTHORITY, §20, reused from <see cref="HalfMarathonIntermediate5D"/>);
    /// TaperVolumeMultiplier(s) = 0.70/0.43 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, §22,
    /// <see cref="HalfMarathonIntermediate6DTaperVolumePolicy"/>); ResolvedPeakReferenceIsSelectedCeiling
    /// = true (DIRECT_EXISTING_AUTHORITY, §12, HM.5.3's mechanism reused unmodified).
    ///
    /// <b>Structural:</b> KEY1 (RUN_LAYOUT_6D's first KEY_SESSION slot) is unchanged
    /// from Intermediate 5D's own primary progression content. KEY2 (the second
    /// KEY_SESSION slot) remains light-quality FARTLEK (never true-hard) in
    /// Build/RaceSpecific, EASY_EQUIVALENT in Foundation/Taper -- HM-X1.2 §3/§5/§29's
    /// frozen structural authority, implemented via the existing, unmodified
    /// half-marathon-workout-progression.v2.json (no new progression file, HM-X1.2 §7).
    /// The one added <c>EASY_SUPPORT</c> slot over 5D carries no phase-specific content
    /// of its own -- fixed-default-bound, generic across all phases (HM-X1.2 §3).
    /// Dark-only: no public routing/gate is widened by this named policy's existence.
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonIntermediate6D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 29.5d,
        ResolvedPeakReference: new(51.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 11,
        TaperVolumeMultiplier: HalfMarathonIntermediate6DTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: 0.28d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.28d,
        LongRunHardCapShare: 0.36d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonIntermediate6DTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 19.0d,
        // HM-X1.2/HM.5.3 -- reused unmodified: 51.0km is a frozen SELECTED PEAK CEILING for the
        // whole 10-16W Core family, not a calibration point 15W/16W may extrapolate past.
        ResolvedPeakReferenceIsSelectedCeiling: true)
    {
        StartingAbsoluteLongRunCapKm = 12.0d,
    };

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
        2 => Intermediate2D,
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

    /// <summary>
    /// Phase 10K-GEN.12 -- implements the already-approved GEN.11 2D numeric
    /// authority for Beginner. PeakVolumeBand=[16,22]km (band midpoint
    /// reference=19.0, ProductDefaultWithEvidenceEnvelope; catalog row).
    /// GoldenFixtureStartingVolumeKm reuses <see cref="BeginnerFourDay"/>'s
    /// own already-proven 12/21=0.5714 ratio applied to 19.0 (GEN.11's own
    /// methodology, mirroring GEN.9's Advanced-policy precedent) =
    /// 10.857, rounded to the 0.5km catalog increment = 11.0. Long-run
    /// shares (55% preferred/selection, 60% hard cap) are frequency-owned
    /// (GEN.11 §8, same on Pattern A/B, same at both Levels) -- not the
    /// Beginner-owned 30/36/33/40 shares <see cref="BeginnerFourDay"/> uses.
    /// GEN.11 explicitly decided missing/zero readiness is
    /// <see cref="TwoDayMissingOrZeroReadinessProductIneligibleException"/>
    /// for 2D at both levels -- <see cref="GoldenFixtureStartingVolumeKm"/>
    /// here is the growth-ratio calibration constant only, never a real
    /// request's actual starting point (mirroring the Advanced policies'
    /// own established rationale for why this field is still required even
    /// with no missing-readiness default).
    /// </summary>
    public static VolumeSafetyPolicy Beginner2D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.0d,
        GoldenFixtureStartingVolumeKm: 11.0d,
        ResolvedPeakReference: new(19.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.55d,
        LongRunPreferredMaximumShare: 0.60d,
        LongRunSelectionShare: 0.55d,
        LongRunHardCapShare: 0.60d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");

    /// <summary>
    /// Phase 10K-GEN.12 -- implements the already-approved GEN.11 2D numeric
    /// authority for Intermediate. PeakVolumeBand=[20,30]km (band midpoint
    /// reference=25.0, ProductDefaultWithEvidenceEnvelope; catalog row).
    /// GoldenFixtureStartingVolumeKm reuses <see cref="ThreeDayIntermediate"/>'s
    /// own already-proven 12/22.5=0.5333 ratio (the nearest existing
    /// lower-frequency Intermediate policy) applied to 25.0 = 13.33,
    /// rounded to the 0.5km catalog increment = 13.5. Long-run shares are
    /// the same frequency-owned 55%/60% figure as Beginner2D (GEN.11 §8:
    /// not Level-owned). Missing/zero readiness is
    /// <see cref="TwoDayMissingOrZeroReadinessProductIneligibleException"/>,
    /// same rationale as Beginner2D above.
    /// </summary>
    public static VolumeSafetyPolicy Intermediate2D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.0d,
        GoldenFixtureStartingVolumeKm: 13.5d,
        ResolvedPeakReference: new(25.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.55d,
        LongRunPreferredMaximumShare: 0.60d,
        LongRunSelectionShare: 0.55d,
        LongRunHardCapShare: 0.60d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate");

    /// <summary>
    /// Phase 10K-GEN.12 -- centralizes the Beginner daysPerWeek-to-policy
    /// dispatch (previously only 4D existed, with no dispatch function of
    /// its own). Fail-closed for any Beginner frequency without an approved
    /// policy -- byte-identical selection for every existing caller (4D
    /// still resolves to <see cref="BeginnerFourDay"/>). GEN.23 adds 3D.
    /// </summary>
    public static VolumeSafetyPolicy ForBeginnerDaysPerWeek(int daysPerWeek) => daysPerWeek switch
    {
        2 => Beginner2D,
        3 => ThreeDayBeginner,
        4 => BeginnerFourDay,
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), daysPerWeek, "No approved Beginner VolumeSafetyPolicy exists for this DaysPerWeek."),
    };

    /// <summary>
    /// Phase 10K-GEN.23 -- implements the frozen Option-1 authority approved
    /// on GEN.21's DOMAIN_DECISION_REQUIRED escalation (Phase K). Growth
    /// mechanics (7%/8%/2.0km, matching <see cref="ThreeDayIntermediate"/>'s
    /// own 3D-owned, Level-blind progression figures per GEN.2B.1/GEN.2B.3 --
    /// no Level effect established or introduced here) and normal-week
    /// long-run shares (38/42/40/42, same reasoning) are reused verbatim,
    /// unchanged from <see cref="ThreeDayIntermediate"/> -- required so the
    /// unchanged normal-week LONG floor (5.0km, <see cref="Session.V1ThreeDaySessionVolumeAllocationPolicy.MinimumLongKm"/>)
    /// remains satisfiable at Beginner's own (lower) starting volumes,
    /// verified this phase. <see cref="GoldenFixtureStartingVolumeKm"/>/<see cref="ResolvedPeakReference"/>
    /// are unused by this planner's 3D sequential-growth branch (mirrors
    /// <see cref="ThreeDayIntermediate"/>'s own established pattern) but are
    /// still populated for audit/decision-trace consistency: 12.0/9.5
    /// (GEN.4C.4's Beginner missing/explicit-zero starting-volume defaults,
    /// reused verbatim per GEN.5's own prior reuse for this exact candidate)
    /// and 17.0 (GEN.5A.2's frozen Beginner×3D PeakVolumeBand reference
    /// point). The TAPER week's long-run share is NOT this record's own
    /// field -- see <see cref="V1BeginnerThreeDayTaperLongRunSharePolicy"/>,
    /// a narrow, taper-only override this phase found is required (real
    /// arithmetic check, not assumed) so the new 3.0km taper-specific LONG
    /// floor is reachable without silently violating the unchanged 5.0km
    /// normal-week floor at low starting volumes -- the two floors need two
    /// different long-run share regimes, not one.
    /// </summary>
    public static VolumeSafetyPolicy ThreeDayBeginner { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.0d,
        GoldenFixtureStartingVolumeKm: 12d,
        ResolvedPeakReference: new(17d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 10,
        TaperVolumeMultiplier: 0.53d,
        LongRunPreferredMinimumShare: 0.38d,
        LongRunPreferredMaximumShare: 0.42d,
        LongRunSelectionShare: 0.40d,
        LongRunHardCapShare: 0.42d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "3d_round_nearest_0.5km_then_reconcile_and_revalidate");

    /// <summary>
    /// Phase HM.16 -- implements the already-frozen HM.15 numeric authority for
    /// HALF_MARATHON x ADVANCED x 3D x 10-16W x DARK
    /// (PHASE_HM_15_ADVANCED_3D_4D_5D_NUMERIC_AND_LEVEL_AUTHORITY_CLOSURE.md
    /// §28's complete Advanced 3D authority table). Every field's value and
    /// classification is consumed exactly as frozen, not re-derived here:
    /// PreferredMaxWeeklyIncreaseRatio/HardMaxWeeklyIncreaseRatio = 0.07/0.08
    /// (DIRECT_EXISTING_AUTHORITY, universal); AbsoluteWeeklyIncrementCapKm =
    /// 2.5 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, direct on-point 10K
    /// <see cref="Advanced3D"/> precedent -- a deliberate departure from HM's
    /// own Intermediate 3D value of 2.0, HM.15 §13); GoldenFixtureStartingVolumeKm
    /// = 24.5 (STRONG_EVIDENCE_DERIVED_DECISION, HM.15 §14 -- reuses
    /// <see cref="HalfMarathonIntermediate3D"/>'s own 20.0/34.0=0.5882 ratio
    /// applied to this policy's own 42.0 peak: 42.0 x 0.5882 = 24.71, rounded
    /// to the 0.5km catalog increment = 24.5; NEVER a missing-readiness
    /// fallback -- the same fail-closed guard <see cref="CatalogVolumeAndLongRunPlanner.ResolveStartingVolume"/>
    /// already applies to every other named HM policy is mechanically
    /// extended to this policy too); ResolvedPeakReference = 42.0
    /// (EVIDENCE_INFORMED_PRODUCT_DEFAULT, exact midpoint of the frozen
    /// [36,48] PeakVolumeBand, HM.15 §10/§11); GoldenFixtureNonTaperTransitions
    /// = 11 (DIRECT_EXISTING_AUTHORITY, the same frequency-generic structural
    /// derivation every other named HM policy uses); TaperVolumeMultiplier(s)
    /// = 0.70/0.43 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, reused from
    /// <see cref="HalfMarathonAdvanced3DTaperVolumePolicy"/>, HM.15 §19 --
    /// clean fit, no disclosed tension); long-run share quadruple
    /// 0.34/0.40/0.38/0.46 (STRONG_EVIDENCE_DERIVED_DECISION, HM.15 §15 --
    /// reused verbatim from <see cref="HalfMarathonIntermediate3D"/>, per
    /// HM.15's own strongest-evidenced field family, four-times-confirmed at
    /// 10K's own Advanced tier); PreferredAbsolutePeakLongRunKm = 19.0
    /// (DIRECT_EXISTING_AUTHORITY, HM.15 §16 -- the SAME ceiling as
    /// Intermediate, a deliberate user-escalated decision NOT to raise it for
    /// Advanced); ResolvedPeakReferenceIsSelectedCeiling = true
    /// (DIRECT_EXISTING_AUTHORITY, the same generic HM.5.3 mechanism every
    /// other named HM policy uses, reused unmodified so 15W/16W saturate at
    /// 42.0km instead of extrapolating past it). StartingAbsoluteLongRunCap =
    /// N/A (NOT_APPLICABLE, HM.15 §17, mirrors every other 3D/4D HM policy's
    /// own null). Structural: 3D has exactly ONE true-hard KEY (RUN_LAYOUT_3D's
    /// single KEY_SESSION slot, HM.15 §5) -- Advanced's up-to-2-hard capacity
    /// is genuine unused headroom at this frequency, not a quota; this record
    /// carries no field for a nonexistent KEY2.
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonAdvanced3D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 24.5d,
        ResolvedPeakReference: new(42.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 11,
        TaperVolumeMultiplier: HalfMarathonAdvanced3DTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: 0.34d,
        LongRunPreferredMaximumShare: 0.40d,
        LongRunSelectionShare: 0.38d,
        LongRunHardCapShare: 0.46d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonAdvanced3DTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 19.0d,
        // HM.15/HM.5.3 -- reused unmodified: 42.0km is a frozen SELECTED PEAK CEILING for the
        // whole 10-16W Core family, not a calibration point 15W/16W may extrapolate past.
        ResolvedPeakReferenceIsSelectedCeiling: true);

    /// <summary>
    /// Phase HM.16 -- implements the already-frozen HM.15 numeric authority for
    /// HALF_MARATHON x ADVANCED x 4D x 10-16W x DARK (HM.15 §28's complete
    /// Advanced 4D authority table). Mirrors <see cref="HalfMarathonAdvanced3D"/>'s
    /// own doc comment shape exactly, at 4D's own frozen values:
    /// AbsoluteWeeklyIncrementCapKm = 2.5 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// direct on-point 10K <see cref="Advanced4D"/> precedent, matches HM's
    /// own Intermediate 4D too); GoldenFixtureStartingVolumeKm = 31.0
    /// (STRONG_EVIDENCE_DERIVED_DECISION, reuses <see cref="HalfMarathonIntermediate4D"/>'s
    /// own 25.0/43.0=0.5814 ratio applied to this policy's own 53.0 peak:
    /// 53.0 x 0.5814 = 30.81, rounded to 31.0); ResolvedPeakReference = 53.0
    /// (EVIDENCE_INFORMED_PRODUCT_DEFAULT, exact midpoint of the frozen
    /// [46,60] PeakVolumeBand); GoldenFixtureNonTaperTransitions = 11
    /// (DIRECT_EXISTING_AUTHORITY); TaperVolumeMultiplier(s) = 0.70/0.43
    /// (EVIDENCE_INFORMED_PRODUCT_DEFAULT, <see cref="HalfMarathonAdvanced4DTaperVolumePolicy"/>,
    /// HM.15 §19 -- disclosed tension at this cell's own band ceiling, 60.0km,
    /// though not at its selected peak, 53.0km); long-run share quadruple
    /// 0.30/0.36/0.33/0.40 (STRONG_EVIDENCE_DERIVED_DECISION, reused verbatim
    /// from <see cref="HalfMarathonIntermediate4D"/>); PreferredAbsolutePeakLongRunKm
    /// = 19.0 (DIRECT_EXISTING_AUTHORITY, same HM-wide Advanced+Intermediate
    /// ceiling, HM.15 §16); ResolvedPeakReferenceIsSelectedCeiling = true
    /// (DIRECT_EXISTING_AUTHORITY). StartingAbsoluteLongRunCap = N/A
    /// (NOT_APPLICABLE, HM.15 §17). Structural: 4D has exactly ONE true-hard
    /// KEY (RUN_LAYOUT_4D v1's single KEY_SESSION slot, HM.15 §6) -- same
    /// headroom-not-quota reasoning as 3D.
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonAdvanced4D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 31.0d,
        ResolvedPeakReference: new(53.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 11,
        TaperVolumeMultiplier: HalfMarathonAdvanced4DTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: 0.30d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.33d,
        LongRunHardCapShare: 0.40d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonAdvanced4DTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 19.0d,
        // HM.15/HM.5.3 -- reused unmodified: 53.0km is a frozen SELECTED PEAK CEILING for the
        // whole 10-16W Core family, not a calibration point 15W/16W may extrapolate past.
        ResolvedPeakReferenceIsSelectedCeiling: true);

    /// <summary>
    /// Phase HM.16 -- implements the already-frozen HM.15 numeric authority for
    /// HALF_MARATHON x ADVANCED x 5D x 10-16W x DARK (HM.15 §28's complete
    /// Advanced 5D authority table). Mirrors <see cref="HalfMarathonAdvanced4D"/>'s
    /// own doc comment shape at 5D's own frozen values: AbsoluteWeeklyIncrementCapKm
    /// = 2.5 (EVIDENCE_INFORMED_PRODUCT_DEFAULT, direct on-point 10K
    /// <see cref="Advanced5D"/> precedent); GoldenFixtureStartingVolumeKm =
    /// 36.0 (STRONG_EVIDENCE_DERIVED_DECISION, reuses
    /// <see cref="HalfMarathonIntermediate5D"/>'s own 29.5/51.0=0.5784 ratio
    /// applied to this policy's own 62.0 peak: 62.0 x 0.5784 = 35.86, rounded
    /// to 36.0); ResolvedPeakReference = 62.0 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// exact midpoint of the frozen [54,70] PeakVolumeBand);
    /// GoldenFixtureNonTaperTransitions = 11 (DIRECT_EXISTING_AUTHORITY);
    /// TaperVolumeMultiplier(s) = 0.70/0.43 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// <see cref="HalfMarathonAdvanced5DTaperVolumePolicy"/>, HM.15 §19 --
    /// **genuine disclosed evidence tension**: this cell's own selected peak,
    /// 62.0km, itself exceeds best-running-tips.com's own named ~56km/week
    /// lighter-taper-exception threshold; no exact alternative percentage
    /// exists anywhere in this engagement's evidence base, so 0.70/0.43 is
    /// frozen as the only concrete value available, per HM.15's own explicit
    /// instruction not to invent or substitute a number -- see HM.16's own
    /// phase report §25 for the taper-feasibility verification this
    /// implementation phase ran against this specific tension); long-run
    /// share quadruple 0.28/0.36/0.28/0.36 (STRONG_EVIDENCE_DERIVED_DECISION,
    /// reused verbatim from <see cref="HalfMarathonIntermediate5D"/>);
    /// PreferredAbsolutePeakLongRunKm = 19.0 (DIRECT_EXISTING_AUTHORITY, HM.15
    /// §16); ResolvedPeakReferenceIsSelectedCeiling = true
    /// (DIRECT_EXISTING_AUTHORITY). StartingAbsoluteLongRunCap = N/A
    /// (NOT_APPLICABLE, HM.15 §17 -- unlike <see cref="HalfMarathonIntermediate5D"/>'s
    /// own 12.0km cap, HM.15 §17 found no comparable feasibility trigger for
    /// Advanced 5D: 36.0 x 0.28 ≈ 10.08→10.0km first-Core-week LR, comfortably
    /// below any floor-violation risk).
    ///
    /// <b>Structural: 5D is the one HM frequency where Advanced's native
    /// 2-hard-per-week capacity is genuinely EXERCISED, not unused headroom.</b>
    /// KEY2 (RUN_LAYOUT_5D's second KEY_SESSION slot) is a genuine second
    /// true-hard stimulus in Build/RaceSpecific via THRESHOLD_TEMPO (fallback
    /// EASY_STANDARD), reverting to EASY_EQUIVALENT (EASY_STANDARD) in
    /// Foundation/Taper -- a user-escalated, HM.15 §7-frozen product decision,
    /// implemented here via the new lane-1 stage content in
    /// half-marathon-workout-progression.v3.json (see
    /// CatalogPrescriptionContextBuilder/CatalogFinalPrescribedPlanValidator
    /// for the corresponding dual-lane taper-completeness widening). KEY2
    /// NEVER binds HM_PACE anywhere (HM.15 §7/§8, deliberate -- avoids
    /// duplicating KEY1's own race-pace-specific rehearsal). This
    /// VolumeSafetyPolicy record itself carries no KEY2-specific field: KEY2's
    /// workout-family selection is entirely a workout-progression/binding
    /// concern (see the new lane-1 progression stages), not a volume-safety
    /// concern -- the generic multi-KEY session-volume allocator already
    /// proven for <see cref="HalfMarathonIntermediate5D"/> (HM.10/HM.11)
    /// distributes this policy's own weekly volume across both KEY roles
    /// unchanged, since the allocator was never hardness-aware, only
    /// role-aware (HM.15 §24, HM.16's own §10 investigation re-verifies this
    /// holds true now that BOTH KEY roles are genuinely hard in Build/RaceSpecific).
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonAdvanced5D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 36.0d,
        ResolvedPeakReference: new(62.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 11,
        TaperVolumeMultiplier: HalfMarathonAdvanced5DTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: 0.28d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.28d,
        LongRunHardCapShare: 0.36d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonAdvanced5DTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 19.0d,
        // HM.15/HM.5.3 -- reused unmodified: 62.0km is a frozen SELECTED PEAK CEILING for the
        // whole 10-16W Core family, not a calibration point 15W/16W may extrapolate past.
        ResolvedPeakReferenceIsSelectedCeiling: true);

    /// <summary>
    /// Phase HM.16 -- centralizes the HALF_MARATHON Advanced daysPerWeek-to-policy
    /// dispatch, mirroring <see cref="ForHalfMarathonBeginnerDaysPerWeek"/>/
    /// <see cref="ForHalfMarathonIntermediateDaysPerWeek"/>'s own shape and
    /// 10K's own <see cref="ForAdvancedDaysPerWeek"/> sibling-method pattern.
    /// Unlike Beginner's own dispatcher (deliberately no 5-arm, 5D
    /// PRODUCT_INELIGIBLE), this one DOES admit 3/4/5 -- HM.15 §9 froze all
    /// three HALF_MARATHON Advanced frequencies as PRODUCT_ELIGIBLE, none
    /// deliberately excluded. Fail-closed for any other value.
    /// </summary>
    public static VolumeSafetyPolicy ForHalfMarathonAdvancedDaysPerWeek(int daysPerWeek) => daysPerWeek switch
    {
        3 => HalfMarathonAdvanced3D,
        4 => HalfMarathonAdvanced4D,
        5 => HalfMarathonAdvanced5D,
        6 => HalfMarathonAdvanced6D,
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), daysPerWeek, "No approved HALF_MARATHON Advanced VolumeSafetyPolicy exists for this DaysPerWeek."),
    };

    /// <summary>
    /// Phase HM-X1.3 -- implements the already-frozen HM-X1.2 numeric authority for
    /// HALF_MARATHON x ADVANCED x 6D x 10-16W x DARK
    /// (PHASE_HM_X1_2_..._STRUCTURAL_AND_NUMERIC_AUTHORITY_CLOSURE.md §35's complete
    /// authority table). Mirrors <see cref="HalfMarathonAdvanced5D"/>'s own doc comment
    /// shape at 6D's own frozen values: AbsoluteWeeklyIncrementCapKm = 2.5
    /// (EVIDENCE_INFORMED_PRODUCT_DEFAULT, §14); GoldenFixtureStartingVolumeKm = 36.5
    /// (EVIDENCE_INFORMED_PRODUCT_DEFAULT, §15 -- reuses Intermediate 6D's own
    /// 29.5/51.0=0.5784 ratio applied to this policy's own 63.0 peak:
    /// 63.0 x 0.5784 = 36.44, rounded to 36.5); ResolvedPeakReference = 63.0
    /// (STRONG_EVIDENCE_DERIVED_DECISION, exact midpoint of the newly-frozen [54,72]
    /// PeakVolumeBand, §11/§12 -- absolute-shift methodology, +2km upper-bound widening
    /// over Advanced 5D's own [54,70], mirroring 10K's own real Advanced 5D-to-6D delta);
    /// GoldenFixtureNonTaperTransitions = 11 (DIRECT_EXISTING_AUTHORITY, §27 -- HM's own
    /// 11-transition structural split, NOT 10K's own 10-transition Advanced convention);
    /// TaperVolumeMultiplier(s) = 0.70/0.43 (EVIDENCE_INFORMED_PRODUCT_DEFAULT,
    /// <see cref="HalfMarathonAdvanced6DTaperVolumePolicy"/>, §22 -- genuine disclosed
    /// evidence tension, deeper than 5D's own already-disclosed tension, not resolved
    /// by design); long-run share quadruple 0.28/0.36/0.28/0.36
    /// (STRONG_EVIDENCE_DERIVED_DECISION, §17, reused verbatim from
    /// <see cref="HalfMarathonIntermediate6D"/>); PreferredAbsolutePeakLongRunKm = 19.0
    /// (DIRECT_EXISTING_AUTHORITY, §19); ResolvedPeakReferenceIsSelectedCeiling = true
    /// (DIRECT_EXISTING_AUTHORITY, §12). StartingAbsoluteLongRunCap = N/A
    /// (NOT_APPLICABLE, §20 -- mirrors <see cref="HalfMarathonAdvanced5D"/>'s own null,
    /// no comparable feasibility trigger found).
    ///
    /// <b>Structural: 6D is another HM frequency where Advanced's native 2-hard-per-week
    /// capacity is genuinely EXERCISED, exactly as at 5D.</b> KEY2 (RUN_LAYOUT_6D's
    /// second KEY_SESSION slot) is a genuine second true-hard stimulus in
    /// Build/RaceSpecific via THRESHOLD_TEMPO (fallback EASY_STANDARD), reverting to
    /// EASY_EQUIVALENT (EASY_STANDARD) in Foundation/Taper -- HM-X1.2 §4/§5/§30's frozen
    /// structural authority, implemented via the existing, unmodified
    /// half-marathon-workout-progression.v3.json (no new progression file, HM-X1.2 §7).
    /// KEY2 NEVER binds HM_PACE anywhere (HM-X1.2 §6, re-affirming HM.15 §7/§8's rationale
    /// as frequency-independent). No third KEY_SESSION slot exists in RUN_LAYOUT_6D
    /// (HM-X1.1 §10) -- exactly 2 true-hard sessions/week in Build/RaceSpecific, never 3.
    /// This record carries no KEY2-specific field for the same reason
    /// <see cref="HalfMarathonAdvanced5D"/>'s own doc comment gives: KEY2's workout-family
    /// selection is entirely a workout-progression/binding concern, not a
    /// volume-safety concern. Dark-only: no public routing/gate is widened by this
    /// named policy's existence.
    /// </summary>
    public static VolumeSafetyPolicy HalfMarathonAdvanced6D { get; } = new(
        PreferredMaxWeeklyIncreaseRatio: 0.07d,
        HardMaxWeeklyIncreaseRatio: 0.08d,
        AbsoluteWeeklyIncrementCapKm: 2.5d,
        GoldenFixtureStartingVolumeKm: 36.5d,
        ResolvedPeakReference: new(63.0d, ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope),
        GoldenFixtureNonTaperTransitions: 11,
        TaperVolumeMultiplier: HalfMarathonAdvanced6DTaperVolumePolicy.TaperWeek2VolumeMultiplier,
        LongRunPreferredMinimumShare: 0.28d,
        LongRunPreferredMaximumShare: 0.36d,
        LongRunSelectionShare: 0.28d,
        LongRunHardCapShare: 0.36d,
        RoundingIncrementKm: 0.5d,
        RoundingRule: "round_nearest_0.5km_after_each_week_value_then_validate",
        TaperVolumeMultipliers: HalfMarathonAdvanced6DTaperVolumePolicy.OrderedMultipliers,
        PreferredAbsolutePeakLongRunKm: 19.0d,
        // HM-X1.2/HM.5.3 -- reused unmodified: 63.0km is a frozen SELECTED PEAK CEILING for the
        // whole 10-16W Core family, not a calibration point 15W/16W may extrapolate past.
        ResolvedPeakReferenceIsSelectedCeiling: true);

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
