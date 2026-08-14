using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;

internal interface ITenKPreparationRunwayFinalInvariantValidator
{
    TenKPreparationRunwayFinalInvariantReport Validate(
        TenKPreparationRunwayDarkOrchestrationRequest request,
        int runwayWeeks,
        PreparationRunwayAllocationEngineResult<PreparationRunwayBlockType> allocation,
        IReadOnlyList<TenKPreparationRunwayLoadedProgression> progressions,
        IReadOnlyList<TenKPreparationRunwayResolvedBinding> bindings,
        PreparationRunwayWeekMaterializationResult<PreparationRunwayBlockType> structural,
        Materialization.DynamicCoreCalendarMaterializationResult core,
        PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> numeric,
        PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> calendar,
        PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType> pace);
}

internal sealed class TenKPreparationRunwayFinalInvariantValidator : ITenKPreparationRunwayFinalInvariantValidator
{
    public TenKPreparationRunwayFinalInvariantReport Validate(
        TenKPreparationRunwayDarkOrchestrationRequest request,
        int runwayWeeks,
        PreparationRunwayAllocationEngineResult<PreparationRunwayBlockType> allocation,
        IReadOnlyList<TenKPreparationRunwayLoadedProgression> progressions,
        IReadOnlyList<TenKPreparationRunwayResolvedBinding> bindings,
        PreparationRunwayWeekMaterializationResult<PreparationRunwayBlockType> structural,
        Materialization.DynamicCoreCalendarMaterializationResult core,
        PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> numeric,
        PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> calendar,
        PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType> pace)
    {
        var findings = new List<string>();
        var allocations = allocation.Allocations ?? [];
        var structuralWeeks = structural.Weeks ?? [];
        var numericWeeks = numeric.PrescribedWeeks ?? [];
        var combined = calendar.OrderedCombinedWeeks ?? [];
        var paced = pace.PacedRunwayWeeks ?? [];
        var corePlan = core.PrescriptionResult.FinalPrescribedPlan;

        var allocationExact = allocations.Sum(a => a.AllocatedWeeks) == runwayWeeks &&
                              allocations.Where(a => a.AllocatedWeeks > 0).All(a =>
                                  progressions.Count(p => p.BlockType == a.BlockKey) == 1 &&
                                  bindings.Count(b => b.BlockType == a.BlockKey) == 1);
        if (!allocationExact) findings.Add("ALLOCATION_OR_POSITIVE_BLOCK_CLOSURE_MISMATCH");

        var structuralExact = structuralWeeks.Count == runwayWeeks &&
                              structuralWeeks.All(w => w.OrderedWorkoutSlots.Count == 4 &&
                                  w.OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.KeySession) == 1 &&
                                  w.OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.EasySupport) == 2 &&
                                  w.OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.LongRun) == 1);
        if (!structuralExact) findings.Add("STRUCTURAL_WEEK_OR_ROLE_CARDINALITY_MISMATCH");

        var numericExact = numeric.IsSuccess && numeric.ContinuityAnalysis?.IsWithinTolerance == true &&
                           numericWeeks.Count == runwayWeeks && numericWeeks.All(w =>
                               Math.Abs(w.OrderedSlots.Sum(s => s.PlannedDistanceKm) - w.PlannedWeeklyVolumeKm) <= 0.001d &&
                               Math.Abs(w.OrderedSlots.Single(s => s.StructuralSlot.SlotRole == PreparationRunwaySlotRole.LongRun).PlannedDistanceKm - w.PlannedLongRunDistanceKm) <= 0.001d);
        if (!numericExact) findings.Add("NUMERIC_TOTAL_OR_CORE_BOUNDARY_MISMATCH");

        var coreWeeks = corePlan.Weeks.Count;
        var calendarExact = calendar.IsSuccess && calendar.ContinuityAnalysis?.IsValid == true &&
                            calendar.DatedRunwayWeeks?.Count == runwayWeeks && calendar.DatedCoreWeeks?.Count == 12 &&
                            combined.Count == runwayWeeks + 12;
        if (!calendarExact) findings.Add("CALENDAR_SEGMENT_COUNT_OR_CONTINUITY_MISMATCH");

        var paceExact = pace.IsSuccess && pace.ContinuityAnalysis?.IsValid == true && paced.Count == runwayWeeks &&
                        paced.SelectMany(w => w.StructuralOrderedSlots).All(s =>
                            s.PacePrescription.Kind == CatalogPacePrescriptionKind.EffortOnly &&
                            s.PacePrescription.Source != CatalogPaceSourceSelection.TargetGoalDerived &&
                            !s.PacePrescription.EffortLabel.Contains("GOAL_PACE", StringComparison.OrdinalIgnoreCase) &&
                            !s.PacePrescription.EffortLabel.Contains("THRESHOLD", StringComparison.OrdinalIgnoreCase) &&
                            !s.PacePrescription.EffortLabel.Contains("RACE_SPECIFIC", StringComparison.OrdinalIgnoreCase));
        if (!paceExact) findings.Add("PACE_CONTINUITY_OR_FORBIDDEN_RUNWAY_PACE_MISMATCH");

        var segmentOrder = structuralWeeks.LastOrDefault()?.BlockType == PreparationRunwayBlockType.PreSpecificTransition &&
                           calendar.DatedCoreWeeks?.OrderBy(w => w.SegmentLocalWeekNumber).FirstOrDefault()?.CoreWeek.PhaseKey == "FOUNDATION";
        if (!segmentOrder) findings.Add("TRANSITION_OR_FOUNDATION_BOUNDARY_MISMATCH");

        var globalNumbers = combined.Select(w => w.GlobalWeekNumber).SequenceEqual(Enumerable.Range(1, runwayWeeks + 12));
        if (!globalNumbers) findings.Add("GLOBAL_WEEK_NUMBERING_MISMATCH");
        var localNumbers = calendar.DatedRunwayWeeks?.Select(w => w.SegmentLocalWeekNumber).SequenceEqual(Enumerable.Range(1, runwayWeeks)) == true &&
                           calendar.DatedCoreWeeks?.Select(w => w.SegmentLocalWeekNumber).SequenceEqual(Enumerable.Range(1, 12)) == true;
        if (!localNumbers) findings.Add("SEGMENT_LOCAL_WEEK_NUMBERING_MISMATCH");

        var allRunwaySlots = calendar.DatedRunwayWeeks?.SelectMany(w => w.StructuralOrderedSlots).ToArray() ?? [];
        var preferred = allRunwaySlots.All(s => request.PreferredDays.Contains(s.SessionDayOfWeek));
        if (!preferred) findings.Add("PREFERRED_DAY_MISMATCH");
        var longRunDay = calendar.DatedRunwayWeeks?.All(w =>
            w.StructuralOrderedSlots.Single(s => s.PrescribedSlot.StructuralSlot.SlotRole == PreparationRunwaySlotRole.LongRun).SessionDayOfWeek == request.LongRunDayPreference) == true;
        if (!longRunDay) findings.Add("LONG_RUN_DAY_MISMATCH");

        var runwayStart = request.StartDate.AddDays(calendar.DateAuthority?.HorizonDecision.LeadingPartialDays ?? -1);
        var noRemainderSessions = allRunwaySlots.All(s => s.SessionDate >= runwayStart);
        if (!noRemainderSessions) findings.Add("ALIGNMENT_REMAINDER_CONTAINS_SESSION");

        var provenance = structuralWeeks.All(w =>
                             !string.IsNullOrWhiteSpace(w.Provenance.ProfileKey) &&
                             !string.IsNullOrWhiteSpace(w.Provenance.AllocationPolicyId) &&
                             w.OrderedWorkoutSlots.All(s => !string.IsNullOrWhiteSpace(s.SourceProgressionId) && !string.IsNullOrWhiteSpace(s.SourceMaterializationPolicyId))) &&
                         numericWeeks.All(w => !string.IsNullOrWhiteSpace(w.NumericTrace.TrajectoryRule) && w.OrderedSlots.All(s => !string.IsNullOrWhiteSpace(s.QuantityProvenance))) &&
                         allRunwaySlots.All(s => !string.IsNullOrWhiteSpace(s.CalendarProvenance.AssignmentRule)) &&
                         paced.SelectMany(w => w.StructuralOrderedSlots).All(s =>
                             !string.IsNullOrWhiteSpace(s.PaceProvenance.ResolverDecision) && !string.IsNullOrWhiteSpace(s.PaceProvenance.WorkoutRule));
        if (!provenance) findings.Add("PROVENANCE_INCOMPLETE");

        var totalWeeks = combined.Count;
        var totalSessions = paced.Sum(w => w.StructuralOrderedSlots.Count) + corePlan.Sessions.Count;
        var counts = coreWeeks == 12 && totalWeeks == runwayWeeks + 12 && totalSessions == (runwayWeeks + 12) * 4;
        if (!counts) findings.Add("TOTAL_WEEK_OR_SESSION_COUNT_MISMATCH");

        var valid = allocationExact && structuralExact && numericExact && calendarExact && paceExact &&
                    segmentOrder && globalNumbers && localNumbers && preferred && longRunDay &&
                    noRemainderSessions && provenance && counts;
        return new TenKPreparationRunwayFinalInvariantReport(
            runwayWeeks + 12, runwayWeeks, coreWeeks, totalWeeks, totalSessions,
            allocationExact, structuralExact, numericExact, calendarExact, paceExact,
            segmentOrder, globalNumbers, localNumbers, preferred, longRunDay,
            noRemainderSessions, paceExact, provenance, valid, findings);
    }
}
