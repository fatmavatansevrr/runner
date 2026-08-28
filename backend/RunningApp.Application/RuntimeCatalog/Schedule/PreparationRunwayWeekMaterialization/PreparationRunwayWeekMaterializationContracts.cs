using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

/// <summary>
/// Structural roles supplied by the canonical four-day run layout. A key
/// session is the week's single defining session; the role does not imply a
/// hard or quality-only stimulus.
/// </summary>
internal enum PreparationRunwaySlotRole
{
    KeySession,
    EasySupport,
    LongRun,
}

internal enum PreparationRunwayWorkoutSlotSource
{
    Anchor,
    SupportPolicy,
}

internal sealed record PreparationRunwayCanonicalWeeklyLayout(
    PlanCatalogReference SourceLayout,
    IReadOnlyList<PreparationRunwaySlotRole> OrderedRoles);

/// <summary>
/// Phase 10K-FREQ.6D.7 — the single, shared definition of a structurally
/// valid Preparation Runway week shape: exactly one <see cref="PreparationRunwaySlotRole.KeySession"/>,
/// exactly one <see cref="PreparationRunwaySlotRole.LongRun"/>, and an
/// approved <see cref="PreparationRunwaySlotRole.EasySupport"/> count making
/// up the rest. Only the two FREQ.6D.6-approved shapes are valid -- 2 EASY
/// (Intermediate 4D: 1K+2E+1L) or 3 EASY (Intermediate 5D: 1K+3E+1L) -- not
/// an arbitrary width; a hypothetical future approved layout would extend
/// <see cref="ApprovedEasySupportCounts"/> explicitly, the same way this
/// phase added 3, rather than silently accepting any count. Every
/// structural/numeric/pace validator that previously hardcoded an exact
/// 4-slot count now consults this instead.
/// </summary>
internal static class PreparationRunwayWeeklyShape
{
    // Phase 10K-FREQ.6D.26 -- extends the approved EASY_SUPPORT count set
    // with 4 (Intermediate x6D Runway: 1 KEY + 4 EASY + 1 LONG, approved
    // FREQ.6D.23 §9), per this class's own documented convention of explicit
    // extension rather than silently accepting any count.
    // Phase 10K-GEN.9 -- extends further with 1 (Advanced x3D Runway: 1 KEY +
    // 1 EASY + 1 LONG, approved GEN.7 §26/§32) -- the same explicit-extension
    // convention, not a silent widening.
    private static readonly int[] ApprovedEasySupportCounts = [1, 2, 3, 4];

    public static bool IsValid(IReadOnlyList<PreparationRunwaySlotRole> roles) =>
        roles.Count(r => r == PreparationRunwaySlotRole.KeySession) == 1 &&
        roles.Count(r => r == PreparationRunwaySlotRole.LongRun) == 1 &&
        roles.Count(r => r == PreparationRunwaySlotRole.EasySupport) == roles.Count - 2 &&
        ApprovedEasySupportCounts.Contains(roles.Count(r => r == PreparationRunwaySlotRole.EasySupport));
}

/// <summary>
/// Enriches the existing binder output with the progression provenance the
/// pure binder deliberately does not own. A future orchestrator may build
/// this adapter, but this phase keeps that composition dark and direct-test
/// only.
/// </summary>
internal sealed record PreparationRunwayMaterializationBlockBinding<TKey>(
    TKey BlockKey,
    PreparationRunwayBlockWorkoutBinding<TKey> Binding,
    string ProgressionId,
    int ProgressionVersion,
    IReadOnlyList<int> OrderedProgressionStepNumbers) where TKey : notnull;

/// <summary>
/// Explicit block/step-to-anchor-role policy. Role selection is never
/// inferred from a workout family or a horizon length.
/// </summary>
internal sealed record PreparationRunwayBlockWeekRolePolicy<TKey>(
    TKey BlockKey,
    int CanonicalOrder,
    string PolicyId,
    int PolicyVersion,
    IReadOnlyDictionary<int, PreparationRunwaySlotRole> AnchorRoleByProgressionStep) where TKey : notnull;

/// <summary>
/// Versioned defaults used only for slots not occupied by the selected
/// anchor. Workout literals live in the policy factory, not in the engine.
/// </summary>
internal sealed record PreparationRunwaySupportWorkoutPolicy(
    string PolicyId,
    int PolicyVersion,
    PreparationRunwayWorkoutReference KeySessionDefault,
    PreparationRunwayWorkoutReference EasySupportDefault,
    PreparationRunwayWorkoutReference LongRunDefault);

internal sealed record PreparationRunwayWeekMaterializationRequest<TKey>(
    string ProfileKey,
    string CandidateKey,
    int CandidateVersion,
    string AllocationPolicyId,
    int AllocationPolicyVersion,
    PreparationRunwayCanonicalWeeklyLayout CanonicalWeeklyLayout,
    IReadOnlyList<PreparationRunwayBlockAllocationOutcome<TKey>> OrderedBlockAllocations,
    IReadOnlyList<PreparationRunwayMaterializationBlockBinding<TKey>> OrderedBlockBindings,
    IReadOnlyList<PreparationRunwayBlockWeekRolePolicy<TKey>> BlockRolePolicies,
    PreparationRunwaySupportWorkoutPolicy SupportWorkoutPolicy) where TKey : notnull;

internal sealed record PreparationRunwayMaterializedWorkoutSlot<TKey>(
    PreparationRunwaySlotRole SlotRole,
    int SlotOrdinal,
    int RoleOrdinal,
    string WorkoutId,
    int WorkoutVersion,
    TKey SourceBlockType,
    string SourceProgressionId,
    int SourceProgressionVersion,
    int SourceProgressionStep,
    PreparationRunwayWorkoutSlotSource SourceKind,
    string SourceMaterializationPolicyId,
    int SourceMaterializationPolicyVersion) where TKey : notnull;

internal sealed record PreparationRunwayMaterializedWeek<TKey>(
    int RunwayWeekNumber,
    TKey BlockType,
    int BlockWeekOrdinal,
    string ProgressionId,
    int ProgressionVersion,
    int ProgressionStepNumber,
    int CanonicalBlockOrder,
    IReadOnlyList<PreparationRunwayMaterializedWorkoutSlot<TKey>> OrderedWorkoutSlots,
    PreparationRunwayMaterializedWeekProvenance Provenance) where TKey : notnull;

internal sealed record PreparationRunwayMaterializedWeekProvenance(
    string ProfileKey,
    string CandidateKey,
    int CandidateVersion,
    string AllocationPolicyId,
    int AllocationPolicyVersion,
    string SupportWorkoutPolicyId,
    int SupportWorkoutPolicyVersion,
    PlanCatalogReference SourceLayout);

internal enum PreparationRunwayWeekMaterializationFailureCode
{
    InvalidMaterializationRequest,
    DuplicateBlockAllocation,
    MissingBlockBinding,
    BlockBindingMismatch,
    BindingCountMismatch,
    InvalidBlockOrder,
    UnsupportedBlockRolePolicy,
    AnchorWorkoutReferenceInvalid,
    AnchorRoleIncompatible,
    SupportWorkoutReferenceInvalid,
    WeekRoleCardinalityViolation,
    NonContiguousWeekNumber,
    NonContiguousBlockWeekOrdinal,
    MaterializationInvariantViolation,
}

/// <summary>Failure never carries a partial week skeleton.</summary>
internal sealed record PreparationRunwayWeekMaterializationResult<TKey>(
    bool IsSuccess,
    IReadOnlyList<PreparationRunwayMaterializedWeek<TKey>>? Weeks,
    int? TotalWeekCount,
    PreparationRunwayWeekMaterializationFailureCode? FailureCode,
    string? FailureReason,
    IReadOnlyList<string> Trace) where TKey : notnull
{
    public static PreparationRunwayWeekMaterializationResult<TKey> Success(
        IReadOnlyList<PreparationRunwayMaterializedWeek<TKey>> weeks,
        IReadOnlyList<string> trace) => new(true, weeks, weeks.Count, null, null, trace);

    public static PreparationRunwayWeekMaterializationResult<TKey> Failure(
        PreparationRunwayWeekMaterializationFailureCode code,
        string reason,
        IReadOnlyList<string> trace) => new(false, null, null, code, reason, trace);
}
