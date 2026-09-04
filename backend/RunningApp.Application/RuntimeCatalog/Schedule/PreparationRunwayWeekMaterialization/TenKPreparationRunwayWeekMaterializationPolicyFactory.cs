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
    /// 2D (<c>daysPerWeek == 2</c>) deliberately throws rather than returning
    /// a policy — this phase attempted, then empirically disproved via a
    /// real dark-verification test against the real block-progression
    /// catalog (<c>Gen27TwoDayPreparationRunwayDarkVerificationTests</c>),
    /// the hypothesis that every progression step's anchor could uniformly
    /// map to <see cref="PreparationRunwaySlotRole.LongRun"/> (reasoning:
    /// 2D's Pattern B weeks have no KEY_SESSION slot to anchor onto, and
    /// LONG_RUN exists in every week regardless of pattern letter). The real
    /// catalog content disproves this: <c>TEN_K_CONSISTENCY_PROGRESSION</c>
    /// step 1's anchor and <c>TEN_K_AEROBIC_STRENGTH_PROGRESSION</c>'s
    /// anchors are real EASY/QUALITY-family workouts (e.g. the literally
    /// Runway-owned-controlled-intensity <c>AEROBIC_STRENGTH_CONTROLLED_INTRO</c>)
    /// authored to occupy the KEY_SESSION role specifically — forcing them
    /// onto LONG_RUN fails <c>PreparationRunwayWeekMaterializer</c>'s own
    /// family-compatibility check (LONG_RUN role requires LONG_RUN family).
    /// Redirecting that same content to the KEY_SESSION slot instead does
    /// not resolve it either: those blocks' anchor-bearing progression steps
    /// have no guarantee of landing on a Pattern A (global-odd) week, and a
    /// Pattern B week has no KEY_SESSION slot at all to place them in. This
    /// is a genuine, now empirically-confirmed architecture question this
    /// phase has no standing to invent unilaterally (does the block/calendar
    /// allocation need to be constrained so quality-anchor-bearing weeks
    /// always land on Pattern A; does the family-compatibility rule need a
    /// product-approved exception; or does 2D need its own block-role model
    /// entirely) — matching the exact STOP discipline <see cref="GEN_19_STOP_PRECEDENT"/>
    /// documents. Left as a hard, documented throw rather than a
    /// plausible-looking-but-wrong silent default.
    /// </summary>
    private const string GEN_19_STOP_PRECEDENT =
        "See PHASE_10K_GEN_19_2D_PREPARATION_RUNWAY_LONGHORIZON_ARCHITECTURE_GAP_CONFIRMATION.md §2 and " +
        "PHASE_10K_GEN_27_TWO_D_PREPARATION_RUNWAY_REPEATING_PATTERN_IMPLEMENTATION.md for the full disclosure.";

    public static IReadOnlyList<PreparationRunwayBlockWeekRolePolicy<PreparationRunwayBlockType>> BuildBlockRolePolicies(int daysPerWeek) =>
        daysPerWeek == 2
            ? throw new NotSupportedException(
                "2D Preparation Runway block-role anchor policy is not yet defined: forcing every progression " +
                "step's anchor onto LONG_RUN was attempted and empirically disproved (real QUALITY/EASY-family " +
                "block-progression anchor content is incompatible with the LONG_RUN role, and re-targeting it to " +
                "KEY_SESSION does not resolve it either, since KEY_SESSION does not exist on every week under " +
                "2D's own frozen A/B pattern). " + GEN_19_STOP_PRECEDENT)
            : [
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
