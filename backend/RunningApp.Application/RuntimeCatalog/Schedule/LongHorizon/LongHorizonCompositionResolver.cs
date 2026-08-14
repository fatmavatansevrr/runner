using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.3 — the single authoritative resolver converting an already-
/// produced <see cref="CoreHorizonDecision"/> into the richer
/// <see cref="LongHorizonCompositionDecision"/>, adding exactly the
/// 15-20 / 21-52 / 53+ distinction the existing classifier's single
/// <see cref="CoreHorizonMode.PreparationRunwayPlusCore"/> bucket does not
/// make. Implements, exactly once, the formulas approved by Phase 4I.1
/// (composition), Phase 4I.2 (structural policy provenance), and Phase
/// 4I.2A (recovery policy provenance) -- no numeric progression, recovery
/// calculation, catalog selection, or materialization is executed here.
///
/// <b>Not called from any live request path.</b> <c>PlanServices</c> and
/// <c>CatalogPreviewGenerator</c> continue to call
/// <c>RaceHorizonPolicy</c> exactly as before
/// this phase -- zero behavior change to any existing HTTP-facing path.
/// This mirrors the established precedent for every other dark,
/// not-yet-activated decision layer in this codebase
/// (<c>PreparationRunwayDateAuthority</c>, the Preparation Runway
/// allocator/contracts family): reachable today by tests only, until a
/// future phase explicitly wires it into a live call path.
/// </summary>
internal static class LongHorizonCompositionResolver
{
    internal const string PolicyId = "TD-LONG-HORIZON-COMPOSITION-001";
    internal const string PolicyVersion = "PHASE_4I_1_V1";
    internal const string DeferredSafetyPolicyId = "TD-LONG-HORIZON-GE-SAFETY-001";
    internal const string RecoveryPolicyId = "TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001";

    internal const string ReasonPlanHorizonExceedsSupportedWindow = "PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW";
    internal const string ReasonLongHorizonGenerationNotActivated = "LONG_HORIZON_GENERATION_NOT_ACTIVATED";

    /// <summary>Outer product-support boundary, per Phase 4G.6A.1/4I.1: 52 is the maximum supported full-week horizon; 53+ is rejected.</summary>
    internal const int MaximumSupportedFullWeeks = 52;

    /// <summary>Lower bound of the Long-Horizon range, per Phase 4I.1: <c>AvailableFullWeeks - 20</c> first becomes positive at 21.</summary>
    internal const int MinimumLongHorizonFullWeeks = 21;

    /// <summary>Fixed Preparation Runway duration for every 21-52 week Long-Horizon plan (Phase 4G.6A.1/4I.1).</summary>
    internal const int LongHorizonPreparationRunwayWeeks = 8;

    /// <summary>Fixed Core duration for every 21-52 week Long-Horizon plan (canonical candidate value, Phase 4I.1).</summary>
    internal const int LongHorizonCoreWeeks = 12;

    public static LongHorizonCompositionDecision Resolve(
        CoreHorizonDecision coreHorizon, ReadinessProfile? readinessProfile)
    {
        var availableFullWeeks = coreHorizon.AvailableFullWeeks;

        return coreHorizon.Mode switch
        {
            CoreHorizonMode.Unsupported or CoreHorizonMode.InvalidInput or CoreHorizonMode.ReadinessOnly =>
                BelowOrInvalid(availableFullWeeks, readinessProfile),

            CoreHorizonMode.CompressedCore or CoreHorizonMode.PreferredCore or CoreHorizonMode.ExtendedCore =>
                CoreOnly(availableFullWeeks, readinessProfile),

            // The existing classifier buckets every horizon above the
            // standalone-core maximum into one mode -- this resolver is the
            // one place that further distinguishes 15-20 / 21-52 / 53+,
            // using only the already-computed AvailableFullWeeks (never a
            // re-derivation of StartDate/RaceDate).
            CoreHorizonMode.PreparationRunwayPlusCore => availableFullWeeks switch
            {
                <= 20 => PreparationRunwayAndCore(availableFullWeeks, readinessProfile),
                <= MaximumSupportedFullWeeks => LongHorizon(availableFullWeeks, readinessProfile),
                _ => UnsupportedAboveWindow(availableFullWeeks, readinessProfile),
            },

            _ => throw new ArgumentOutOfRangeException(nameof(coreHorizon), coreHorizon.Mode, "Unrecognized CoreHorizonMode."),
        };
    }

    private static LongHorizonCompositionDecision BelowOrInvalid(int availableFullWeeks, ReadinessProfile? profile) => new(
        AvailableFullWeeks: availableFullWeeks,
        HorizonPath: LongHorizonPath.InsufficientOrExistingUnsupportedPath,
        Eligibility: LongHorizonEligibility.NotApplicableBelowLongHorizonBoundary,
        GenerationActivationStatus: LongHorizonGenerationActivationStatus.NotApplicable,
        GeneralEnduranceWeeks: null,
        PreparationRunwayWeeks: null,
        CoreWeeks: null,
        GeneralEnduranceClassification: GeneralEnduranceDurationClassification.NotApplicable,
        ReadinessProfile: profile,
        ReasonCode: null,
        PolicyId: PolicyId, PolicyVersion: PolicyVersion,
        DeferredSafetyPolicyId: DeferredSafetyPolicyId, RecoveryPolicyId: RecoveryPolicyId,
        Rules: ["Preserves the pre-existing, unchanged below-minimum/invalid-input decision -- not redefined by this phase."]);

    private static LongHorizonCompositionDecision CoreOnly(int availableFullWeeks, ReadinessProfile? profile) => new(
        AvailableFullWeeks: availableFullWeeks,
        HorizonPath: LongHorizonPath.CoreOnly,
        Eligibility: LongHorizonEligibility.NotApplicableBelowLongHorizonBoundary,
        GenerationActivationStatus: LongHorizonGenerationActivationStatus.NotApplicable,
        GeneralEnduranceWeeks: null,
        PreparationRunwayWeeks: null,
        // Aggregate total only -- per-phase (Foundation/Build/RaceSpecific/
        // Taper) counts remain owned exclusively by the existing, unchanged
        // CatalogPhaseAllocationResolver; this decision never recomputes them.
        CoreWeeks: availableFullWeeks,
        GeneralEnduranceClassification: GeneralEnduranceDurationClassification.NotApplicable,
        ReadinessProfile: profile,
        ReasonCode: null,
        PolicyId: PolicyId, PolicyVersion: PolicyVersion,
        DeferredSafetyPolicyId: DeferredSafetyPolicyId, RecoveryPolicyId: RecoveryPolicyId,
        Rules: ["8-14 available full weeks: Core-only, existing 8-14-week allocation authority unchanged."]);

    private static LongHorizonCompositionDecision PreparationRunwayAndCore(int availableFullWeeks, ReadinessProfile? profile) => new(
        AvailableFullWeeks: availableFullWeeks,
        HorizonPath: LongHorizonPath.PreparationRunwayAndCore,
        Eligibility: LongHorizonEligibility.NotApplicableBelowLongHorizonBoundary,
        GenerationActivationStatus: LongHorizonGenerationActivationStatus.NotApplicable,
        GeneralEnduranceWeeks: null,
        PreparationRunwayWeeks: availableFullWeeks - LongHorizonCoreWeeks,
        CoreWeeks: LongHorizonCoreWeeks,
        GeneralEnduranceClassification: GeneralEnduranceDurationClassification.NotApplicable,
        ReadinessProfile: profile,
        ReasonCode: null,
        PolicyId: PolicyId, PolicyVersion: PolicyVersion,
        DeferredSafetyPolicyId: DeferredSafetyPolicyId, RecoveryPolicyId: RecoveryPolicyId,
        Rules: ["15-20 available full weeks: RunwayWeeks = AvailableFullWeeks - 12 (Phase 4G.6A.1), CoreWeeks fixed at 12."]);

    private static LongHorizonCompositionDecision LongHorizon(int availableFullWeeks, ReadinessProfile? profile)
    {
        var generalEnduranceWeeks = availableFullWeeks - 20;
        return new(
            AvailableFullWeeks: availableFullWeeks,
            HorizonPath: LongHorizonPath.LongHorizonGeneralEnduranceRunwayAndCore,
            Eligibility: LongHorizonEligibility.SupportedLongHorizon,
            GenerationActivationStatus: LongHorizonGenerationActivationStatus.EligibleButGenerationNotActivated,
            GeneralEnduranceWeeks: generalEnduranceWeeks,
            PreparationRunwayWeeks: LongHorizonPreparationRunwayWeeks,
            CoreWeeks: LongHorizonCoreWeeks,
            GeneralEnduranceClassification: generalEnduranceWeeks <= 3
                ? GeneralEnduranceDurationClassification.ShortExtension
                : GeneralEnduranceDurationClassification.FullPhase,
            ReadinessProfile: profile,
            ReasonCode: ReasonLongHorizonGenerationNotActivated,
            PolicyId: PolicyId, PolicyVersion: PolicyVersion,
            DeferredSafetyPolicyId: DeferredSafetyPolicyId, RecoveryPolicyId: RecoveryPolicyId,
            Rules: [
                "21-52 available full weeks: GE = AvailableFullWeeks - 20, Runway fixed 8, Core fixed 12 (Phase 4I.1 TD-LONG-HORIZON-COMPOSITION-001).",
                "Composition-eligible; runtime generation not activated (TD-GENERAL-ENDURANCE-STAGED-PLAN-001 remains OPEN).",
            ]);
    }

    private static LongHorizonCompositionDecision UnsupportedAboveWindow(int availableFullWeeks, ReadinessProfile? profile) => new(
        AvailableFullWeeks: availableFullWeeks,
        HorizonPath: LongHorizonPath.UnsupportedAboveSupportedWindow,
        Eligibility: LongHorizonEligibility.UnsupportedAboveAnnualWindow,
        GenerationActivationStatus: LongHorizonGenerationActivationStatus.NotApplicable,
        GeneralEnduranceWeeks: null,
        PreparationRunwayWeeks: null,
        CoreWeeks: null,
        GeneralEnduranceClassification: GeneralEnduranceDurationClassification.NotApplicable,
        ReadinessProfile: profile,
        ReasonCode: ReasonPlanHorizonExceedsSupportedWindow,
        PolicyId: PolicyId, PolicyVersion: PolicyVersion,
        DeferredSafetyPolicyId: DeferredSafetyPolicyId, RecoveryPolicyId: RecoveryPolicyId,
        Rules: ["53+ available full weeks: V1 product/system support boundary, not a physiological-safety claim (Phase 4G.6A.1). No truncation, no fallback."]);
}
