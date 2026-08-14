using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;

internal enum PreparationRunwaySegmentType
{
    PreparationRunway,
    RaceCore,
}

internal sealed record PreparationRunwayCalendarAuthority(
    CoreHorizonContext HorizonContext,
    CoreHorizonDecision HorizonDecision,
    PreparationRunwayDateAuthorityResult RunwayDateDecision,
    DateOnly RunwayStartDate,
    string SourcePolicyKey,
    int SourcePolicyVersion);

internal sealed record PreparationRunwayCalendarCompositionPolicy(
    string PolicyKey,
    int PolicyVersion,
    string CalendarMaterializerVersion,
    CatalogCalendarAssignmentPolicy AssignmentPolicy);

internal sealed record PreparationRunwayCalendarCompositionRequest<TKey>(
    PreparationRunwayCalendarAuthority DateAuthority,
    IReadOnlyList<DayOfWeek> PreferredDays,
    DayOfWeek? LongRunDayPreference,
    PreparationRunwayAllocationProfile Profile,
    string CandidateKey,
    int CandidateVersion,
    PreparationRunwayNumericMaterializationResult<TKey> NumericRunway,
    PreparationRunwayCoreWeekOneNumericTarget CoreWeekOneNumericTarget,
    GeneratedCatalogPlanSkeleton UndatedCoreSkeleton,
    PreparationRunwayCalendarCompositionPolicy Policy) where TKey : notnull;

internal sealed record PreparationRunwaySegmentProvenance(
    PreparationRunwaySegmentType SegmentType,
    int SegmentOrdinal,
    DateOnly SegmentStartDate,
    DateOnly SegmentEndDate,
    int SegmentWeekStart,
    int SegmentWeekEnd,
    string SourcePolicyKey,
    int SourcePolicyVersion,
    string CandidateKey,
    int CandidateVersion,
    string? ProfileKey);

internal sealed record PreparationRunwayDatedSlot<TKey>(
    PreparationRunwayPrescribedSlot<TKey> PrescribedSlot,
    DateOnly SessionDate,
    DayOfWeek SessionDayOfWeek,
    CatalogSessionCalendarProvenance CalendarProvenance) where TKey : notnull;

internal sealed record PreparationRunwayDatedWeek<TKey>(
    int GlobalWeekNumber,
    int SegmentLocalWeekNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    PreparationRunwayPrescribedWeek<TKey> PrescribedWeek,
    IReadOnlyList<PreparationRunwayDatedSlot<TKey>> StructuralOrderedSlots,
    IReadOnlyList<PreparationRunwayDatedSlot<TKey>> ChronologicalSlots,
    PreparationRunwaySegmentProvenance SegmentProvenance) where TKey : notnull;

internal sealed record PreparationRunwayDatedCoreWeek(
    int GlobalWeekNumber,
    int SegmentLocalWeekNumber,
    DatedGeneratedCatalogWeekSkeleton CoreWeek,
    PreparationRunwaySegmentProvenance SegmentProvenance);

internal sealed record PreparationRunwayComposedWeek<TKey>(
    int GlobalWeekNumber,
    int SegmentLocalWeekNumber,
    PreparationRunwaySegmentType SegmentType,
    DateOnly StartDate,
    DateOnly EndDate,
    PreparationRunwayDatedWeek<TKey>? RunwayWeek,
    PreparationRunwayDatedCoreWeek? CoreWeek) where TKey : notnull;

internal sealed record PreparationRunwayCalendarContinuityAnalysis(
    DateOnly AlignmentStartDate,
    int LeadingPartialDays,
    DateOnly RunwayStartDate,
    DateOnly FinalRunwayEndDate,
    DateOnly CoreStartDate,
    DateOnly FinalCoreEndDate,
    DateOnly RaceDateBoundary,
    bool RunwayCoreWindowsContiguous,
    bool CrossBoundarySpacingValid,
    bool NumericBoundaryPreserved,
    bool IsValid,
    string Provenance);

internal enum PreparationRunwayCalendarCompositionFailureCode
{
    InvalidCalendarCompositionRequest,
    InvalidDateAuthority,
    PreferredDaysInvalid,
    LongRunDayNotPreferred,
    RunwayWeekCountMismatch,
    CoreWeekCountMismatch,
    RunwayWindowInvalid,
    CoreStartDateMismatch,
    WorkoutDateOutsideWeek,
    DuplicateWorkoutDate,
    RoleDayAssignmentInvalid,
    RunwayCoreDateOverlap,
    RunwayCoreDateGap,
    LeadingPartialDayMismatch,
    SegmentOrderInvalid,
    GlobalWeekNumberInvalid,
    NumericPrescriptionChanged,
    RunwayCoreContinuityViolation,
    CalendarCompositionInvariantViolation,
}

internal sealed record PreparationRunwayCalendarCompositionResult<TKey>(
    bool IsSuccess,
    PreparationRunwayCalendarAuthority? DateAuthority,
    IReadOnlyList<PreparationRunwayDatedWeek<TKey>>? DatedRunwayWeeks,
    IReadOnlyList<PreparationRunwayDatedCoreWeek>? DatedCoreWeeks,
    IReadOnlyList<PreparationRunwayComposedWeek<TKey>>? OrderedCombinedWeeks,
    IReadOnlyList<PreparationRunwaySegmentProvenance>? Segments,
    PreparationRunwayCalendarContinuityAnalysis? ContinuityAnalysis,
    PreparationRunwayCalendarCompositionFailureCode? FailureCode,
    string? FailureReason,
    IReadOnlyList<string> Trace) where TKey : notnull
{
    public static PreparationRunwayCalendarCompositionResult<TKey> Success(
        PreparationRunwayCalendarAuthority authority,
        IReadOnlyList<PreparationRunwayDatedWeek<TKey>> runway,
        IReadOnlyList<PreparationRunwayDatedCoreWeek> core,
        IReadOnlyList<PreparationRunwayComposedWeek<TKey>> combined,
        IReadOnlyList<PreparationRunwaySegmentProvenance> segments,
        PreparationRunwayCalendarContinuityAnalysis continuity,
        IReadOnlyList<string> trace) =>
        new(true, authority, runway, core, combined, segments, continuity, null, null, trace);

    public static PreparationRunwayCalendarCompositionResult<TKey> Failure(
        PreparationRunwayCalendarCompositionFailureCode code,
        string reason,
        IReadOnlyList<string> trace) =>
        new(false, null, null, null, null, null, null, code, reason, trace);
}
