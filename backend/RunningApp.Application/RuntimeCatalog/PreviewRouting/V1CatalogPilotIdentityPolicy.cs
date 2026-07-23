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
        level == Level &&
        daysPerWeek == DaysPerWeek;
}
