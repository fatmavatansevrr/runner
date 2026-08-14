using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

/// <summary>
/// Dark TEN_K__4D__INTERMEDIATE v10 structural policy. It contains no
/// allocation weights, horizon branches, dates, or prescription values.
/// </summary>
internal static class TenKPreparationRunwayWeekMaterializationPolicyFactory
{
    public const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    public const int CandidateVersion = 10;
    public const string AllocationPolicyId = "TEN_K_PREPARATION_RUNWAY_ALLOCATION_POLICY";
    public const int AllocationPolicyVersion = 1;
    public const string SupportPolicyId = "TEN_K_PREPARATION_RUNWAY_SUPPORT_WORKOUT_POLICY";
    public const int SupportPolicyVersion = 1;
    public const string BlockRolePolicyId = "TEN_K_PREPARATION_RUNWAY_BLOCK_ROLE_POLICY";
    public const int BlockRolePolicyVersion = 1;

    public static PreparationRunwayCanonicalWeeklyLayout BuildLayout() => new(
        new PlanCatalogReference("RUN_LAYOUT_4D", 2),
        [
            PreparationRunwaySlotRole.KeySession,
            PreparationRunwaySlotRole.EasySupport,
            PreparationRunwaySlotRole.EasySupport,
            PreparationRunwaySlotRole.LongRun,
        ]);

    public static PreparationRunwaySupportWorkoutPolicy BuildSupportPolicy() => new(
        SupportPolicyId,
        SupportPolicyVersion,
        new PreparationRunwayWorkoutReference("EASY_STANDARD", 5),
        new PreparationRunwayWorkoutReference("EASY_STANDARD", 5),
        new PreparationRunwayWorkoutReference("LONG_RUN_STANDARD", 5));

    public static IReadOnlyList<PreparationRunwayBlockWeekRolePolicy<PreparationRunwayBlockType>> BuildBlockRolePolicies() =>
    [
        new(PreparationRunwayBlockType.Consistency, 1, BlockRolePolicyId, BlockRolePolicyVersion, new Dictionary<int, PreparationRunwaySlotRole>
        {
            [1] = PreparationRunwaySlotRole.KeySession,
            [2] = PreparationRunwaySlotRole.LongRun,
        }),
        new(PreparationRunwayBlockType.GeneralEndurance, 2, BlockRolePolicyId, BlockRolePolicyVersion, new Dictionary<int, PreparationRunwaySlotRole>
        {
            [1] = PreparationRunwaySlotRole.LongRun,
            [2] = PreparationRunwaySlotRole.LongRun,
            [3] = PreparationRunwaySlotRole.LongRun,
            [4] = PreparationRunwaySlotRole.LongRun,
            [5] = PreparationRunwaySlotRole.LongRun,
        }),
        new(PreparationRunwayBlockType.AerobicStrength, 3, BlockRolePolicyId, BlockRolePolicyVersion, new Dictionary<int, PreparationRunwaySlotRole>
        {
            [1] = PreparationRunwaySlotRole.KeySession,
            [2] = PreparationRunwaySlotRole.KeySession,
        }),
        new(PreparationRunwayBlockType.PreSpecificTransition, 4, BlockRolePolicyId, BlockRolePolicyVersion, new Dictionary<int, PreparationRunwaySlotRole>
        {
            [1] = PreparationRunwaySlotRole.KeySession,
        }),
    ];
}
