using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;

internal static class TenKPreparationRunwayProgressionPolicyFactory
{
    internal const string PolicyKey = "TEN_K_PREPARATION_RUNWAY_PROGRESSION_REFERENCE_POLICY";
    internal const int PolicyVersion = 1;

    public static IReadOnlyDictionary<PreparationRunwayBlockType, PlanCatalogReference> Build() =>
        new Dictionary<PreparationRunwayBlockType, PlanCatalogReference>
        {
            [PreparationRunwayBlockType.Consistency] = new("TEN_K_CONSISTENCY_PROGRESSION", 1),
            [PreparationRunwayBlockType.GeneralEndurance] = new("TEN_K_GENERAL_ENDURANCE_PROGRESSION", 1),
            [PreparationRunwayBlockType.AerobicStrength] = new("TEN_K_AEROBIC_STRENGTH_PROGRESSION", 1),
            [PreparationRunwayBlockType.PreSpecificTransition] = new("TEN_K_PRE_SPECIFIC_TRANSITION_PROGRESSION", 1),
        };

    public static string CatalogBlockName(PreparationRunwayBlockType block) => block switch
    {
        PreparationRunwayBlockType.Consistency => "CONSISTENCY",
        PreparationRunwayBlockType.GeneralEndurance => "GENERAL_ENDURANCE",
        PreparationRunwayBlockType.AerobicStrength => "AEROBIC_STRENGTH",
        PreparationRunwayBlockType.PreSpecificTransition => "PRE_SPECIFIC_TRANSITION",
        _ => throw new ArgumentOutOfRangeException(nameof(block)),
    };
}
