using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;

internal sealed record TenKPreparationRunwayDarkOrchestrationRequest(
    PlanCatalogCandidateSummary Candidate,
    DateOnly StartDate,
    DateOnly RaceDate,
    DateOnly AsOfDate,
    IReadOnlyList<DayOfWeek> PreferredDays,
    DayOfWeek LongRunDayPreference,
    RuntimeConditionResolutionResult CoreEntryReadinessResult,
    IReadOnlyList<RuntimeConditionResolutionResult> ConditionResults,
    GeneratePreviewRequest PreviewRequest,
    ResolverInputSnapshot ResolverInput,
    PreparationRunwayQuantityUnit Unit,
    // Phase 4G.6B.1: optional authoritative horizon decision carried in from
    // a single upstream RaceHorizonPolicy.Decide call (public preview
    // routing). When supplied, OrchestrateAsync uses it directly instead of
    // computing its own -- see Stage 1 in TenKPreparationRunwayDarkOrchestrator.
    // Null preserves this record's original, pre-4G.6B.1 behavior (internal
    // computation) for every existing dark-orchestrator-level test/call site
    // that does not supply one.
    CoreHorizonDecision? HorizonDecision = null);

internal enum TenKPreparationRunwayOrchestrationStage
{
    Horizon,
    ReadinessProfile,
    AllocationPolicy,
    BlockAllocation,
    ProgressionLoading,
    WorkoutBinding,
    StructuralMaterialization,
    CoreGeneration,
    NumericMaterialization,
    CalendarComposition,
    PaceMaterialization,
    FinalInvariantValidation,
}

internal enum TenKPreparationRunwayOrchestrationFailureCode
{
    InvalidOrchestrationRequest,
    CandidateNotSupported,
    HorizonNotDirectRunway,
    ReadinessProfileUnavailable,
    AllocationPolicyUnavailable,
    BlockAllocationFailed,
    ProgressionLoadingFailed,
    WorkoutBindingFailed,
    StructuralMaterializationFailed,
    CoreGenerationFailed,
    NumericMaterializationFailed,
    CalendarCompositionFailed,
    PaceMaterializationFailed,
    FinalInvariantValidationFailed,
    OrchestrationInvariantViolation,
}

internal sealed record TenKPreparationRunwayOrchestrationTraceEntry(
    int Sequence,
    TenKPreparationRunwayOrchestrationStage Stage,
    string Subject,
    string SourceKey,
    int? SourceVersion,
    string Outcome,
    IReadOnlyDictionary<string, string> Details);

internal sealed record TenKPreparationRunwayLoadedProgression(
    PreparationRunwayBlockType BlockType,
    PlanCatalogReference Reference,
    PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType> Definition);

internal sealed record TenKPreparationRunwayResolvedBinding(
    PreparationRunwayBlockType BlockType,
    PreparationRunwayBlockWorkoutBinding<PreparationRunwayBlockType> Binding,
    string ProgressionId,
    int ProgressionVersion,
    IReadOnlyList<int> OrderedProgressionStepNumbers);

internal sealed record TenKPreparationRunwayFinalInvariantReport(
    int RequestedFullWeeks,
    int RunwayWeeks,
    int CoreWeeks,
    int TotalWeeks,
    int TotalSessions,
    bool AllocationExact,
    bool StructuralExact,
    bool NumericContinuity,
    bool CalendarContinuity,
    bool PaceContinuity,
    bool SegmentOrderValid,
    bool GlobalNumberingValid,
    bool SegmentLocalNumberingValid,
    bool PreferredDaysValid,
    bool LongRunDayValid,
    bool NoAlignmentRemainderSessions,
    bool NoForbiddenRunwayPace,
    bool ProvenanceComplete,
    bool IsValid,
    IReadOnlyList<string> Findings);

internal sealed record TenKPreparationRunwayDarkOrchestrationFailure(
    TenKPreparationRunwayOrchestrationStage Stage,
    TenKPreparationRunwayOrchestrationFailureCode Code,
    string OriginatingComponent,
    string? OriginalFailureCode,
    string Reason);

internal sealed record TenKPreparationRunwayDarkOrchestrationResult(
    bool IsSuccess,
    CoreHorizonDecision? HorizonDecision,
    PreparationRunwayAllocationProfile? Profile,
    PreparationRunwayAllocationEngineResult<PreparationRunwayBlockType>? AllocationResult,
    IReadOnlyList<TenKPreparationRunwayLoadedProgression>? LoadedProgressions,
    IReadOnlyList<TenKPreparationRunwayResolvedBinding>? Bindings,
    PreparationRunwayWeekMaterializationResult<PreparationRunwayBlockType>? StructuralRunway,
    DynamicCoreCalendarMaterializationResult? CoreResult,
    PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType>? NumericRunway,
    PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType>? CalendarComposition,
    PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType>? PacedRunway,
    TenKPreparationRunwayFinalInvariantReport? FinalInvariants,
    TenKPreparationRunwayDarkOrchestrationFailure? Failure,
    IReadOnlyList<TenKPreparationRunwayOrchestrationTraceEntry> Trace)
{
    public static TenKPreparationRunwayDarkOrchestrationResult Success(
        CoreHorizonDecision horizon,
        PreparationRunwayAllocationProfile profile,
        PreparationRunwayAllocationEngineResult<PreparationRunwayBlockType> allocation,
        IReadOnlyList<TenKPreparationRunwayLoadedProgression> progressions,
        IReadOnlyList<TenKPreparationRunwayResolvedBinding> bindings,
        PreparationRunwayWeekMaterializationResult<PreparationRunwayBlockType> structural,
        DynamicCoreCalendarMaterializationResult core,
        PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> numeric,
        PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> calendar,
        PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType> pace,
        TenKPreparationRunwayFinalInvariantReport invariants,
        IReadOnlyList<TenKPreparationRunwayOrchestrationTraceEntry> trace) =>
        new(true, horizon, profile, allocation, progressions, bindings, structural, core, numeric, calendar, pace, invariants, null, trace);

    public static TenKPreparationRunwayDarkOrchestrationResult Failed(
        TenKPreparationRunwayDarkOrchestrationFailure failure,
        IReadOnlyList<TenKPreparationRunwayOrchestrationTraceEntry> trace) =>
        new(false, null, null, null, null, null, null, null, null, null, null, null, failure, trace);
}

internal sealed record TenKPreparationRunwayCoreGenerationRequest(
    PlanCatalogCandidateSummary Candidate,
    DateOnly CoreStartDate,
    DateOnly RaceDate,
    DateOnly AsOfDate,
    IReadOnlyList<DayOfWeek> PreferredDays,
    DayOfWeek LongRunDayPreference,
    IReadOnlyList<RuntimeConditionResolutionResult> ConditionResults,
    GeneratePreviewRequest PreviewRequest,
    ResolverInputSnapshot ResolverInput);
