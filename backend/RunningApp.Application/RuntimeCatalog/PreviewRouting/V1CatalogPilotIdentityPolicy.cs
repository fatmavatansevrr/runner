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
    /// Phase 10K-FREQ.6D.4D Split 5B — the real, published Intermediate 5D
    /// Core candidate. <c>CatalogWeekSkeletonCalendarMaterializer</c> was
    /// generalized (Split 5B) to a multi-KEY_SESSION-per-week search
    /// enforcing the frozen KEY&lt;-&gt;KEY separation rule (Split 5A,
    /// <see cref="DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToKeySessionSeparationDays"/>
    /// = 2 calendar days) and proven end-to-end against the real dark 5D
    /// candidate for every supported horizon. Public activation was
    /// attempted and reverted again: real E2E testing found
    /// <c>CatalogPrescriptionContextValidator.Validate</c> hardcodes a
    /// requirement that some session carry
    /// <c>ProgressionStageKey == "TAPER_SHARPEN"</c> — the legacy 3D/4D/
    /// Beginner4D Taper stage-key naming (still real and correct for those
    /// candidates); the real 5D dual-lane progression's Taper stages are
    /// named <c>TAPER_PRIMARY_STAGE</c>/<c>TAPER_SECONDARY_STAGE</c>
    /// instead, so this check fails closed for every 5D request that
    /// includes a Taper phase (effectively all supported 8-14 week
    /// horizons) — a second, independent, calendar-unrelated blocker, out
    /// of this split's scope (STOP condition: prescription-context
    /// completeness logic must change, which is its own decision, not a
    /// calendar fix). Kept here, not consumed anywhere yet.
    /// </summary>
    public const string FiveDayCandidateKey = "TEN_K__5D__INTERMEDIATE";
    public const int FiveDayCandidateVersion = 1;

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
            (RunningBackground.Beginner, 4);
    // NOT (Intermediate, 5): reverted again (Split 5B). The calendar blocker
    // (Split E) is now genuinely fixed and proven dark -- see FiveDayCandidateKey's
    // own doc comment for the real, independent, calendar-unrelated blocker
    // (CatalogPrescriptionContextValidator's hardcoded TAPER_SHARPEN stage-key
    // check) found by real E2E testing when this was widened.

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
        (RunningBackground.Beginner, 4) => (BeginnerCandidateKey, BeginnerCandidateVersion),
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), "Only the activated Intermediate 3D/4D and Beginner 4D Core pilot identities are resolvable.")
    };

    /// <summary>
    /// Non-throwing counterpart of <see cref="ResolveCandidate"/> for call
    /// sites (e.g. route-decision logging) that must handle an unsupported
    /// combination without an exception, since they run for every request,
    /// not just already-confirmed pilot matches.
    /// </summary>
    public static (string CandidateKey, int CandidateVersion)? TryResolveCandidate(RunningBackground level, int daysPerWeek) =>
        IsSupportedLevelFrequency(level, daysPerWeek) ? ResolveCandidate(level, daysPerWeek) : null;
}
