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
    /// <summary>Phase 10K-GEN.9 -- Advanced x3D's own Runway shape (GEN.7 §26/§32: 1 KEY + 1 EASY + 1 LONG). Never reachable before GEN.9 since no prior Level had 3D LongHorizon/Runway support.</summary>
    /// <summary>
    /// Phase 10K-GEN.27 — 2D's own Model B repeating pattern (GEN.11 §1,
    /// GEN.26 Q1/Q2): Runway retains the identical A/B structure Core
    /// already uses, continuing the same global week ordinal (GEN.26 Q1 —
    /// this is a standalone 15-20wk Runway product with no preceding GE
    /// segment, so the materializer's own contiguous runway week number
    /// already *is* that ordinal). <c>SourceLayout</c> is a purely-internal
    /// provenance reference, never catalog-loaded, matching the existing
    /// 5D/6D internal-provenance convention.
    /// </summary>
    private static readonly PlanCatalogReference TwoDayModelBSourceLayout =
        new("PREPARATION_RUNWAY_LAYOUT_2D_MODEL_B_V1", 1);

    private static readonly IReadOnlyList<IReadOnlyList<PreparationRunwaySlotRole>> TwoDayModelBPattern =
    [
        [PreparationRunwaySlotRole.KeySession, PreparationRunwaySlotRole.LongRun],
        [PreparationRunwaySlotRole.EasySupport, PreparationRunwaySlotRole.LongRun],
    ];

    public static PreparationRunwayCanonicalWeeklyLayout BuildLayout(int daysPerWeek) => daysPerWeek switch
    {
        // Phase 10K-GEN.27 -- 2D Runway (Beginner + Intermediate): Pattern A
        // (odd global week) = KEY_SESSION + LONG_RUN, Pattern B (even global
        // week) = EASY_SUPPORT + LONG_RUN. OrderedRoles carries Pattern[0]
        // only for pre-GEN.27 consumers that read it directly for
        // provenance/logging -- the materializer itself always consults
        // WeeklyPatternRoles when present.
        2 => new(
            TwoDayModelBSourceLayout,
            TwoDayModelBPattern[0],
            TwoDayModelBPattern,
            PatternPeriodWeeks: 2),
        3 => new(
            new PlanCatalogReference("PREPARATION_RUNWAY_LAYOUT_3D_V1", 1),
            [
                PreparationRunwaySlotRole.KeySession,
                PreparationRunwaySlotRole.EasySupport,
                PreparationRunwaySlotRole.LongRun,
            ]),
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
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), daysPerWeek, "Only the approved Beginner/Intermediate 2D, Intermediate 4D/5D/6D, and Advanced 3D/4D/5D/6D Preparation Runway layouts are supported."),
    };

    public static PreparationRunwaySupportWorkoutPolicy BuildSupportPolicy() => new(
        SupportPolicyId,
        SupportPolicyVersion,
        new PreparationRunwayWorkoutReference("EASY_STANDARD", 5),
        new PreparationRunwayWorkoutReference("EASY_STANDARD", 5),
        new PreparationRunwayWorkoutReference("LONG_RUN_STANDARD", 5));

    /// <summary>
    /// Phase 10K-GEN.27 — parameterized by DaysPerWeek. For every existing
    /// frequency (3D/4D/5D/6D), returns byte-identical policies to the
    /// pre-GEN.27 parameterless overload (verified: literal dictionary
    /// values unchanged).
    ///
    /// Phase 10K-GEN.29 — the block-relative <c>AnchorRoleByProgressionStep</c>
    /// mapping below (which progression step anchors which structural role)
    /// was never actually frequency-dependent; it is purely a function of a
    /// block's own progression shape. GEN.27's <c>daysPerWeek == 2</c> throw
    /// stood only because, before GEN.28/GEN.29, there was no reconciliation
    /// for what happens when a block-local week's fixed anchor role (e.g.
    /// KeySession) is not present in a given week's *resolved* A/B pattern
    /// shape (a Pattern B week has no KEY_SESSION slot). GEN.28 §9 (Candidate
    /// C) and this phase's frozen governing decision close that gap with
    /// role-conditioned content selection inside
    /// <see cref="PreparationRunwayWeekMaterialization.PreparationRunwayWeekMaterializer"/>
    /// itself (redirecting to a catalog-declared EASY_SUPPORT-role
    /// alternate when the fixed anchor role is absent from the week's
    /// resolved shape) — not by varying this policy. This method therefore
    /// no longer branches on <c>daysPerWeek</c> at all: the same block-role
    /// policy set is correct for every frequency including 2D, and
    /// <paramref name="daysPerWeek"/> is retained only for call-site/
    /// provenance-signature compatibility.
    /// </summary>
    public static IReadOnlyList<PreparationRunwayBlockWeekRolePolicy<PreparationRunwayBlockType>> BuildBlockRolePolicies(int daysPerWeek) =>
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
