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

    /// <summary>
    /// Phase 10K-FREQ.6D.7: parameterized by the real candidate's DaysPerWeek.
    /// 4D is byte-for-byte unchanged. 5D materializes the FREQ.6D.6-approved
    /// Runway shape (1 KEY + 3 EASY + 1 LONG) under a purely-internal,
    /// non-catalog-loaded provenance reference -- <c>SourceLayout</c> is never
    /// dereferenced by the materializer (pure provenance metadata), and the
    /// Core structural authority <c>RUN_LAYOUT_5D</c> is untouched.
    /// </summary>
    public static PreparationRunwayCanonicalWeeklyLayout BuildLayout(int daysPerWeek) => daysPerWeek switch
    {
        4 => new(
            new PlanCatalogReference("RUN_LAYOUT_4D", 2),
            [
                PreparationRunwaySlotRole.KeySession,
                PreparationRunwaySlotRole.EasySupport,
                PreparationRunwaySlotRole.EasySupport,
                PreparationRunwaySlotRole.LongRun,
            ]),
        5 => new(
            new PlanCatalogReference("PREPARATION_RUNWAY_LAYOUT_5D_V1", 1),
            [
                PreparationRunwaySlotRole.KeySession,
                PreparationRunwaySlotRole.EasySupport,
                PreparationRunwaySlotRole.EasySupport,
                PreparationRunwaySlotRole.EasySupport,
                PreparationRunwaySlotRole.LongRun,
            ]),
        // Phase 10K-FREQ.6D.26 -- the approved Intermediate x6D Preparation
        // Runway shape (1 KEY + 4 EASY + 1 LONG, FREQ.6D.23 §9), same
        // internal-provenance-only reference pattern 5D already uses.
        6 => new(
            new PlanCatalogReference("PREPARATION_RUNWAY_LAYOUT_6D_V1", 1),
            [
                PreparationRunwaySlotRole.KeySession,
                PreparationRunwaySlotRole.EasySupport,
                PreparationRunwaySlotRole.EasySupport,
                PreparationRunwaySlotRole.EasySupport,
                PreparationRunwaySlotRole.EasySupport,
                PreparationRunwaySlotRole.LongRun,
            ]),
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), daysPerWeek, "Only the approved Intermediate 4D/5D/6D Preparation Runway layouts are supported."),
    };

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
