using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Phase 4F.9.1A — the single, centrally-owned definition of which request
/// identity (GoalType/GoalDistance/backend Level/DaysPerWeek) corresponds to
/// the TEN_K__4D__INTERMEDIATE catalog pilot candidate.
///
/// Evidence basis: NOT_AN_EVIDENCE_QUESTION. Decision status:
/// EXPLICIT_PRODUCT_DEFAULT. This is a technical/product mapping decision —
/// which backend request shape identifies the pilot candidate — not a
/// training-science evidence question, and is not subject to evidence-basis
/// review.
///
/// Prior to this phase, the same four-field comparison (GoalType.Race,
/// GoalDistance.TenK, RunningBackground.Intermediate, DaysPerWeek == 4)
/// was duplicated verbatim in two places:
/// <see cref="GenerationRouteDecision"/>'s (unregistered, dead-code)
/// <c>PilotGenerationRouteDecider</c> and in
/// <see cref="V1LiveCatalogPilotRoutingPolicy"/> (the one actually wired into
/// DI). This type is now the single owner of that mapping; both the
/// candidate key/version constants and the identity check live here.
///
/// Originally the backend's <c>RunningBackground</c> enum was three-valued
/// (NewToRunning/UsedToRun/RunningRegularly) with no exact "INTERMEDIATE"
/// member (Phase 2 finding: NotSupported, a different taxonomy axis), so
/// <c>RunningRegularly</c> was used as an informal stand-in for the
/// catalog's "INTERMEDIATE" level. Running Background V2 replaced the enum
/// with the four canonical values {Beginner, Intermediate, Advanced,
/// Experienced}; <see cref="RunningBackground.Intermediate"/> now
/// explicitly and exactly owns this pilot mapping (not a stand-in) — see
/// RUNNING_BACKGROUND_V2_FRONTEND_BACKEND_ALIGNMENT.md. The other three
/// values (Beginner/Advanced/Experienced) have no catalog mapping and are
/// not pilot-eligible; broadening pilot scope to them is explicitly out of
/// scope for that migration.
/// </summary>
public static class V1CatalogPilotIdentityPolicy
{
    public const string PolicyKey = "V1_CATALOG_PILOT_IDENTITY_POLICY";
    public const int PolicyVersion = 1;

    public const GoalType GoalType = Domain.Enums.GoalType.Race;
    public const GoalDistance GoalDistance = Domain.Enums.GoalDistance.TenK;
    public const RunningBackground Level = RunningBackground.Intermediate;
    public const int DaysPerWeek = 4;

    /// <summary>The catalog's own level label for <see cref="Level"/>.</summary>
    public const string CatalogLevel = "INTERMEDIATE";

    public const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    public const int CandidateVersion = 10;
    public const string ThreeDayCandidateKey = "TEN_K__3D__INTERMEDIATE";
    public const int ThreeDayCandidateVersion = 1;

    /// <summary>
    /// Phase 10K-FREQ.6D.4D Split 5B/5C/5D — the real, published Intermediate
    /// 5D Core candidate. Both real Taper-completeness blockers Split 5B/5C
    /// found (<c>CatalogPrescriptionContextValidator</c> and
    /// <c>CatalogFinalPrescribedPlanValidator</c>, both hardcoding the legacy
    /// <c>TAPER_SHARPEN</c>/<c>EASY_STANDARD</c> identity unconditionally)
    /// are now fixed (Split 5D) by partitioning each check along the
    /// existing Legacy/ProfileBacked classification — the real 5D dual-lane
    /// Taper is proven correct without any stage-name special-casing.
    /// Public activation was attempted a third time and reverted again: real
    /// E2E testing got past both Taper checks and found a third, genuinely
    /// independent blocker — <c>V1CatalogPublicWorkoutTypeMappingPolicy.Map</c>
    /// (in <c>CatalogPublicPreviewMaterializer.cs</c>) has no public
    /// workout-type mapping for <c>AEROBIC_STRENGTH_CONTROLLED_INTRO</c> (the
    /// real 5D FOUNDATION lane0 workout) — deciding which public workout-type
    /// category it belongs to is a real product decision, out of scope here.
    /// Kept here, not consumed anywhere yet.
    /// </summary>
    public const string FiveDayCandidateKey = "TEN_K__5D__INTERMEDIATE";
    public const int FiveDayCandidateVersion = 1;

    /// <summary>
    /// Phase 10K-FREQ.6D.26 -- the approved, dark-only Intermediate x6D Core
    /// candidate (FREQ.6D.23/6D.25). Kept here, not consumed by any public
    /// routing method (<see cref="IsSupportedIdentity"/>/<see cref="ResolveCandidate"/>/
    /// <see cref="IsSupportedPreparationRunwayIdentity"/> are deliberately
    /// left untouched -- the public 6D gate remains closed); only referenced
    /// by <see cref="IsSupportedPreparationRunwayCandidate"/>, an internal
    /// candidate-identity consistency check the dark Runway
    /// numeric/calendar/pace machinery already uses for 4D/5D.
    /// </summary>
    public const string SixDayCandidateKey = "TEN_K__6D__INTERMEDIATE";
    public const int SixDayCandidateVersion = 1;

    /// <summary>
    /// GEN.4E — Beginner 4D Core public activation. Per GEN.4A's frozen
    /// vocabulary decision, backend <see cref="RunningBackground.Beginner"/>
    /// is the exact canonical counterpart of the catalog's "NEW" experience
    /// label (see <c>BEGINNER_MODIFIER</c>); that translation is applied at
    /// candidate-load time, not here — this policy only ever deals in the
    /// backend enum.
    /// </summary>
    public const string BeginnerCandidateKey = "TEN_K__4D__BEGINNER";
    public const int BeginnerCandidateVersion = 1;

    /// <summary>
    /// Phase 10K-GEN.9 -- the approved, dark-only Advanced 3D/4D/5D/6D Core
    /// candidates (GEN.7/GEN.8 authority). Kept here, not consumed by any
    /// public routing method (<see cref="IsSupportedIdentity"/>/<see cref="ResolveCandidate"/>/
    /// <see cref="IsSupportedPreparationRunwayIdentity"/> are deliberately
    /// left untouched -- the public gate remains closed for every Advanced
    /// frequency); referenced only by dark/internal test and orchestration
    /// code that resolves a candidate identity directly, mirroring exactly
    /// how <see cref="SixDayCandidateKey"/> was introduced for Intermediate
    /// x6D during its own dark-implementation phase (FREQ.6D.26).
    /// </summary>
    public const string AdvancedThreeDayCandidateKey = "TEN_K__3D__ADVANCED";
    public const int AdvancedThreeDayCandidateVersion = 1;
    public const string AdvancedFourDayCandidateKey = "TEN_K__4D__ADVANCED";
    public const int AdvancedFourDayCandidateVersion = 1;
    public const string AdvancedFiveDayCandidateKey = "TEN_K__5D__ADVANCED";
    public const int AdvancedFiveDayCandidateVersion = 1;
    public const string AdvancedSixDayCandidateKey = "TEN_K__6D__ADVANCED";
    public const int AdvancedSixDayCandidateVersion = 1;

    /// <summary>
    /// The complete, explicit allow-list of (Level, DaysPerWeek) pairs the
    /// pilot recognizes. Deliberately enumerated rather than derived, so a
    /// future cell can never be admitted by accident — the two places above
    /// that resolve identity (<see cref="IsSupportedIdentity"/> and
    /// <see cref="ResolveCandidate"/>) both consult only this list.
    /// </summary>
    private static bool IsSupportedLevelFrequency(RunningBackground level, int daysPerWeek) =>
        (level, daysPerWeek) is
            (RunningBackground.Intermediate, 3) or
            (RunningBackground.Intermediate, 4) or
            (RunningBackground.Intermediate, 5) or
            (RunningBackground.Intermediate, 6) or
            (RunningBackground.Beginner, 4) or
            (RunningBackground.Advanced, 3) or
            (RunningBackground.Advanced, 4) or
            (RunningBackground.Advanced, 5) or
            (RunningBackground.Advanced, 6);
    // (Intermediate, 6): Phase 10K-FREQ.6D.27 public activation, implementing
    // the already-approved FREQ.6D.23/6D.25/6D.26 authority. Does not widen
    // to Beginner/Advanced x6D or Intermediate x7D -- see SixDayCandidateKey's
    // own doc comment for the full prior dark-implementation history.
    // (Advanced, 3/4/5/6): Phase 10K-GEN.10 public activation, implementing
    // the already-approved GEN.7/GEN.8 authority and the GEN.9 dark
    // implementation. Does not widen to Advanced x7D (PRODUCT_NON_SUPPORT,
    // GEN.7) or Advanced x2D (OUT_OF_V1, never designed) -- both remain
    // unreachable through this allow-list by construction.
    // (Intermediate, 5): fifth activation attempt (Phase 10K-FREQ.6D.4D.5G). The
    // execution-context propagation gap FREQ.6D.4D.5F found (CompressedCore/ExtendedCore
    // never threaded the published-bundle execution index into session prescription,
    // unlike the exact-12-week pipeline) is now fixed -- both dynamic-orchestration
    // context types carry it through to the same ExecutionPrescriptionIndex.ResolveExact
    // authority the 12-week path already used. See FiveDayCandidateKey's own doc
    // comment for the full prior four-revert history.

    /// <summary>
    /// Returns whether the given request identity matches the pilot
    /// candidate's supported combination. Pure identity comparison — no
    /// catalog files, resolvers, or database are consulted here.
    /// </summary>
    public static bool IsSupportedIdentity(
        GoalType goalType,
        GoalDistance goalDistance,
        RunningBackground level,
        int daysPerWeek) =>
        goalType == GoalType &&
        goalDistance == GoalDistance &&
        IsSupportedLevelFrequency(level, daysPerWeek);

    public static (string CandidateKey, int CandidateVersion) ResolveCandidate(RunningBackground level, int daysPerWeek) => (level, daysPerWeek) switch
    {
        (RunningBackground.Intermediate, 3) => (ThreeDayCandidateKey, ThreeDayCandidateVersion),
        (RunningBackground.Intermediate, 4) => (CandidateKey, CandidateVersion),
        (RunningBackground.Intermediate, 5) => (FiveDayCandidateKey, FiveDayCandidateVersion),
        (RunningBackground.Intermediate, 6) => (SixDayCandidateKey, SixDayCandidateVersion),
        (RunningBackground.Beginner, 4) => (BeginnerCandidateKey, BeginnerCandidateVersion),
        (RunningBackground.Advanced, 3) => (AdvancedThreeDayCandidateKey, AdvancedThreeDayCandidateVersion),
        (RunningBackground.Advanced, 4) => (AdvancedFourDayCandidateKey, AdvancedFourDayCandidateVersion),
        (RunningBackground.Advanced, 5) => (AdvancedFiveDayCandidateKey, AdvancedFiveDayCandidateVersion),
        (RunningBackground.Advanced, 6) => (AdvancedSixDayCandidateKey, AdvancedSixDayCandidateVersion),
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), "Only the activated Intermediate 3D/4D/5D/6D, Beginner 4D, and Advanced 3D/4D/5D/6D Core pilot identities are resolvable.")
    };

    /// <summary>
    /// Non-throwing counterpart of <see cref="ResolveCandidate"/> for call
    /// sites (e.g. route-decision logging) that must handle an unsupported
    /// combination without an exception, since they run for every request,
    /// not just already-confirmed pilot matches.
    /// </summary>
    public static (string CandidateKey, int CandidateVersion)? TryResolveCandidate(RunningBackground level, int daysPerWeek) =>
        IsSupportedLevelFrequency(level, daysPerWeek) ? ResolveCandidate(level, daysPerWeek) : null;

    /// <summary>
    /// Phase 10K-FREQ.6D.7 — the Preparation Runway's own, deliberately
    /// narrower allow-list. Runway is NOT simply "whatever Core supports":
    /// Core's identity set also includes Intermediate 3D and Beginner 4D,
    /// neither of which has an approved Runway product decision (see
    /// PHASE_10K_FREQ_6D_6_INTERMEDIATE_5D_RUNWAY_PRODUCT_DECISION.md).
    /// Widening Core's allow-list must never silently widen Runway
    /// eligibility, so Runway consults this separate, explicit list instead.
    /// </summary>
    private static bool IsSupportedPreparationRunwayLevelFrequency(RunningBackground level, int daysPerWeek) =>
        (level, daysPerWeek) is
            (RunningBackground.Intermediate, 4) or
            (RunningBackground.Intermediate, 5) or
            (RunningBackground.Intermediate, 6) or
            // Phase 10K-GEN.10 -- Advanced Runway is approved for all four
            // supported frequencies including 3D (GEN.7 §17: unlike
            // Intermediate x3D, Advanced x3D Runway/LongHorizon was approved).
            (RunningBackground.Advanced, 3) or
            (RunningBackground.Advanced, 4) or
            (RunningBackground.Advanced, 5) or
            (RunningBackground.Advanced, 6);

    public static bool IsSupportedPreparationRunwayIdentity(
        GoalType goalType,
        GoalDistance goalDistance,
        RunningBackground level,
        int daysPerWeek) =>
        goalType == GoalType &&
        goalDistance == GoalDistance &&
        IsSupportedPreparationRunwayLevelFrequency(level, daysPerWeek);

    /// <summary>
    /// Phase 10K-GEN.9 -- widened to admit the approved, dark-only Advanced
    /// 3D/4D/5D/6D candidates (GEN.7/GEN.8 authority), the same internal
    /// candidate-identity consistency check the dark Runway/LongHorizon
    /// machinery already used for Intermediate. This is not the public gate
    /// (<see cref="IsSupportedIdentity"/>/<see cref="IsSupportedPreparationRunwayIdentity"/>
    /// remain untouched) -- it only governs whether the shared dark
    /// Preparation Runway orchestrator recognizes an already-resolved
    /// candidate key/version as internally consistent.
    /// </summary>
    public static bool IsSupportedPreparationRunwayCandidate(string candidateKey, int candidateVersion) =>
        (candidateKey, candidateVersion) is
            (CandidateKey, CandidateVersion) or
            (FiveDayCandidateKey, FiveDayCandidateVersion) or
            (SixDayCandidateKey, SixDayCandidateVersion) or
            (AdvancedThreeDayCandidateKey, AdvancedThreeDayCandidateVersion) or
            (AdvancedFourDayCandidateKey, AdvancedFourDayCandidateVersion) or
            (AdvancedFiveDayCandidateKey, AdvancedFiveDayCandidateVersion) or
            (AdvancedSixDayCandidateKey, AdvancedSixDayCandidateVersion);
}
