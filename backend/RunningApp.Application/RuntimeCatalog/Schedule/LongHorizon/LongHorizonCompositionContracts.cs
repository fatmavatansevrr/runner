namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.3 — dark, unwired typed contract for the 8-to-52-week horizon
/// classification/composition decision. Consumes an already-produced
/// <see cref="Horizon.CoreHorizonDecision"/> (never re-derives StartDate/
/// RaceDate arithmetic itself, matching the established
/// <c>PreparationRunwayDateAuthority</c> precedent) and adds the 15-20 vs
/// 21-52 vs 53+ distinction the existing day-accurate horizon classifier
/// itself does not make (its <see cref="Horizon.CoreHorizonMode.PreparationRunwayPlusCore"/>
/// mode is a single bucket for every horizon above the standalone-core
/// maximum). This type and its resolver are not called from any live
/// request path (see <c>LongHorizonCompositionResolver</c>'s own doc
/// comment) -- public preview/generation behavior for every horizon range
/// is completely unchanged by this phase.
/// </summary>
internal enum LongHorizonPath
{
    /// <summary>Below the standalone-core minimum, or an otherwise invalid input -- the pre-existing, unchanged concern <see cref="Horizon.CoreHorizonMode.Unsupported"/>/<see cref="Horizon.CoreHorizonMode.InvalidInput"/> already represent.</summary>
    InsufficientOrExistingUnsupportedPath,

    /// <summary>8-14 available full weeks. Core allocation is owned by the existing, unchanged 8-14-week authority (<c>CatalogPhaseAllocationResolver</c>) -- this type never recomputes per-phase counts, only the aggregate total.</summary>
    CoreOnly,

    /// <summary>15-20 available full weeks. Preparation Runway + 12-week Core, per Phase 4G.6A.1/4G.6A.2 (dark, unwired).</summary>
    PreparationRunwayAndCore,

    /// <summary>21-52 available full weeks. Long-Horizon General Endurance + 8-week Preparation Runway + 12-week Core, per Phase 4I.1/4I.2/4I.2A.</summary>
    LongHorizonGeneralEnduranceRunwayAndCore,

    /// <summary>53+ available full weeks. Rejected as a V1 product/system support boundary, not a physiological-safety claim (Phase 4G.6A.1/4I.1).</summary>
    UnsupportedAboveSupportedWindow,
}

/// <summary>Composition-policy eligibility for the 21-52 week Long-Horizon range specifically (Phase 4I.1's own typed shape).</summary>
internal enum LongHorizonEligibility
{
    /// <summary>Available weeks are outside 21-52 on the low side (below-8, 8-14, or 15-20) -- the Long-Horizon policy simply does not apply.</summary>
    NotApplicableBelowLongHorizonBoundary,

    /// <summary>21-52 available full weeks: the approved composition formula applies.</summary>
    SupportedLongHorizon,

    /// <summary>53+ available full weeks: rejected, per <see cref="LongHorizonPath.UnsupportedAboveSupportedWindow"/>.</summary>
    UnsupportedAboveAnnualWindow,
}

/// <summary>
/// Whether a policy-eligible Long-Horizon decision may currently produce a
/// real plan. Deliberately a separate axis from <see cref="LongHorizonEligibility"/>:
/// 21-52 is composition-ELIGIBLE (Phase 4I.1) but generation is NOT yet
/// ACTIVATED (no catalog, no materializer, no numeric execution -- Phase
/// 4I.3's own explicit non-scope). Never conflate this with the 53+
/// unsupported-horizon reason.
/// </summary>
internal enum LongHorizonGenerationActivationStatus
{
    /// <summary>This decision's <see cref="LongHorizonEligibility"/> is not <see cref="LongHorizonEligibility.SupportedLongHorizon"/> -- generation-activation status does not apply.</summary>
    NotApplicable,

    /// <summary>Composition-eligible (21-52 weeks), but runtime generation remains blocked pending catalog/materialization/numeric-execution/public-preview work -- see <c>TD-GENERAL-ENDURANCE-STAGED-PLAN-001</c> (still OPEN).</summary>
    EligibleButGenerationNotActivated,
}

/// <summary>1-32 GE-week duration classification, per Phase 4I.1 §Part 2/Phase 4I.2 §Part 1.</summary>
internal enum GeneralEnduranceDurationClassification
{
    /// <summary>This decision's horizon path does not include a Long-Horizon General Endurance segment at all.</summary>
    NotApplicable,

    /// <summary>1-3 GE weeks: orientation/continuity role only, never claimed as an independent physiological adaptation phase.</summary>
    ShortExtension,

    /// <summary>4-32 GE weeks: contains internal four-week-mesocycle structure (Phase 4I.2).</summary>
    FullPhase,
}

/// <summary>
/// The two currently supported GE/Preparation-Runway readiness profiles.
/// Profile affects segment CONTENT only, never segment DURATION (Phase
/// 4I.1 §13, Phase 4I.2 §7/§16, Phase 4I.2A §16) -- this resolver takes a
/// profile purely to preserve it verbatim for later content-selection
/// consumers; it never branches allocation on it.
/// </summary>
internal enum ReadinessProfile
{
    ConsistencyNeeded,
    CoreEntryReady,
}

/// <summary>
/// The single authoritative 8-to-52-week horizon classification/composition
/// decision. One instance fully describes which path applies, whether the
/// request is eligible, the exact segment durations (where applicable), the
/// GE duration classification, and the approved policy provenance that
/// produced the decision -- so no downstream consumer (catalog selection,
/// structural materialization, numeric execution, validation, preview,
/// confirmation, persistence) needs to, or may, independently recalculate
/// any of these values. None of those consumers are implemented by this
/// phase; this record is the contract they must eventually consume.
/// </summary>
internal sealed record LongHorizonCompositionDecision(
    int AvailableFullWeeks,
    LongHorizonPath HorizonPath,
    LongHorizonEligibility Eligibility,
    LongHorizonGenerationActivationStatus GenerationActivationStatus,
    int? GeneralEnduranceWeeks,
    int? PreparationRunwayWeeks,
    int? CoreWeeks,
    GeneralEnduranceDurationClassification GeneralEnduranceClassification,
    ReadinessProfile? ReadinessProfile,
    string? ReasonCode,
    string PolicyId,
    string PolicyVersion,
    string DeferredSafetyPolicyId,
    string RecoveryPolicyId,
    IReadOnlyList<string> Rules);
