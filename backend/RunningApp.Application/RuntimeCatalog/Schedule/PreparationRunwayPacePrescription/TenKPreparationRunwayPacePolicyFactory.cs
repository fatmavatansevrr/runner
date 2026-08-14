using RunningApp.Application.RuntimeCatalog.Prescription.Session;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;

internal static class TenKPreparationRunwayPacePolicyFactory
{
    internal const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    internal const int CandidateVersion = 10;
    internal const string PolicyKey = "TEN_K_PREPARATION_RUNWAY_PACE_POLICY";
    internal const int PolicyVersion = 1;

    public static PreparationRunwayPacePolicy Build() => new(
        PolicyKey,
        PolicyVersion,
        [
            Rule("EASY_STANDARD", 5, "EASY", "Existing Core planner EASY effort-only contract; no numeric easy-pace producer exists."),
            Rule("LONG_RUN_STANDARD", 5, "LONG_RUN_EASY_CONTROLLED", "Existing Core planner controlled easy long-run effort; no fast-finish or numeric offset is authorized."),
            Rule("AEROBIC_STRENGTH_CONTROLLED_INTRO", 1, "CONTROLLED_AEROBIC_POWER_INTRO", "Catalog-authored PREPARATION_RUNWAY main-set intensity; structural introductory dose, below threshold and race-specific work."),
            Rule("AEROBIC_STRENGTH_CONTROLLED_PROGRESSED", 1, "CONTROLLED_AEROBIC_POWER_PROGRESSED", "Catalog-authored PREPARATION_RUNWAY main-set intensity; progression is owned by workout structure, not a numeric pace escalation."),
        ],
        "Final PreSpecificTransition pace must be typed-compatible with authoritative Core Foundation Week 1 by structural role; no invented numeric tolerance.");

    private static PreparationRunwayWorkoutPaceRule Rule(string id, int version, string effort, string basis) => new(
        id, version, CatalogPacePrescriptionKind.EffortOnly,
        CatalogPaceSourceSelection.EffortOnly, effort, basis);
}
