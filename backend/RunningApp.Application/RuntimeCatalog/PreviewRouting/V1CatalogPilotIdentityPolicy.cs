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
    /// Phase 10K-FREQ.6D.4D Split E — the real, published Intermediate 5D Core
    /// candidate. Deliberately NOT yet in <see cref="IsSupportedLevelFrequency"/>
    /// (see the comment there): <c>CatalogWeekSkeletonCalendarMaterializer</c>'s
    /// KEY_SESSION calendar-date assignment algorithm hardcodes exactly one
    /// KEY_SESSION slot per week; a 5D week has two (LaneOrdinal 0/1), which
    /// needs a real algorithm change plus an undecided product question
    /// (minimum separation between the two weekly KEY sessions) before this
    /// candidate can be safely reached from a live request. Kept here, not
    /// consumed anywhere yet, so the real key/version are recorded in one
    /// place for whoever picks this up next.
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
    // NOT (Intermediate, 5): reverted. CatalogWeekSkeletonCalendarMaterializer's
    // KEY_SESSION date-assignment algorithm hardcodes exactly one KEY_SESSION
    // slot per week (a scalar chosen date); a 5D week has two (LaneOrdinal 0/1),
    // which this algorithm would either reject (DaysPerWeek is not (3 or 4)) or,
    // if that guard were removed, silently collide onto the same calendar date.
    // Widening this requires a real algorithm change (multi-slot backtracking)
    // plus an undecided product question (minimum separation between the two
    // weekly KEY sessions) -- STOP-condition territory, not mechanical routing.

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
