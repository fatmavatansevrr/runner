using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6 — the single dark execution orchestrator. Consumes the
/// already-validated Phase 4I.5 structural skeleton and produces a
/// <see cref="LongHorizonExecutedSchedule"/>: every GE/Runway/Core slot
/// carries a real, non-null workout key/version, every week/slot carries a
/// real calendar date, and every GE week additionally carries a complete
/// numeric prescription (weekly volume, long-run distance, per-slot
/// distance). Preparation Runway and Core numeric prescription (volume/pace)
/// is explicitly NOT executed by this phase -- see the phase document's own
/// non-implementation statement. Never persists, never exposes a public DTO,
/// not called from any live request path.
/// </summary>
internal static class LongHorizonDarkExecutionOrchestrator
{
    public static async Task<LongHorizonExecutedSchedule> ExecuteAsync(
        LongHorizonCompositionDecision decision,
        DateOnly startDate,
        LongHorizonGeEntryBaselineInput geBaseline,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek longRunDay,
        string catalogRoot,
        Binding.ICatalogWorkoutDefinitionLoader workoutLoader,
        CancellationToken ct = default)
    {
        var structural = await LongHorizonStructuralMaterializer.MaterializeAsync(decision, catalogRoot, workoutLoader, ct);

        var geWeeks = decision.GeneralEnduranceWeeks!.Value;
        var profile = decision.ReadinessProfile!.Value;
        var geDescriptors = LongHorizonGeStructuralSelector.Select(geWeeks, profile);
        var geNumeric = LongHorizonGeNumericExecutor.Execute(geDescriptors, geBaseline);
        var geNumericByWeekIndex = geNumeric.ToDictionary(r => r.WeekIndex);

        var weekdayAssignments = LongHorizonCalendarAssigner.AssignWeekdays(preferredDays, longRunDay);

        var coreStartDate = LongHorizonCalendarAssigner.WeekStartDate(startDate, geWeeks + 8 + 1);
        var coreBinding = await LongHorizonCoreWorkoutBindingExecutor.BindAsync(
            coreStartDate,
            new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" },
            new CatalogStageWeekAllocation[]
            {
                new("FOUNDATION", 3), new("BUILD", 4), new("RACE_SPECIFIC", 4), new("TAPER", 1),
            },
            new PlanCatalogReference("RUN_LAYOUT_4D", 2),
            new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" },
            catalogRoot, workoutLoader, ct);
        var coreSessionsByLocalWeek = coreBinding.Weeks.ToDictionary(w => w.WeekNumber);

        var executedWeeks = new List<LongHorizonExecutedWeek>(structural.Weeks.Count);
        foreach (var week in structural.Weeks)
        {
            var weekStartDate = LongHorizonCalendarAssigner.WeekStartDate(startDate, week.GlobalWeekNumber);

            executedWeeks.Add(week.Segment switch
            {
                LongHorizonSegmentType.LongHorizonGeneralEndurance =>
                    BuildGeExecutedWeek(week, weekStartDate, geNumericByWeekIndex[week.LocalSegmentWeekNumber], weekdayAssignments),
                LongHorizonSegmentType.PreparationRunway =>
                    BuildRunwayExecutedWeek(week, weekStartDate, weekdayAssignments),
                LongHorizonSegmentType.Core =>
                    BuildCoreExecutedWeek(week, coreSessionsByLocalWeek[week.LocalSegmentWeekNumber],
                        LongHorizonCalendarAssigner.WeekStartDate(coreStartDate, week.LocalSegmentWeekNumber)),
                _ => throw new ArgumentOutOfRangeException(),
            });
        }

        var executed = new LongHorizonExecutedSchedule(
            Structural: structural,
            StartDate: startDate,
            GeNumericExecutionComplete: true,
            RunwayNumericExecutionComplete: false,
            CoreNumericExecutionComplete: false,
            Weeks: executedWeeks);

        var validation = LongHorizonExecutionValidator.Validate(executed);
        if (!validation.IsValid)
            throw new InvalidOperationException(
                "LongHorizonDarkExecutionOrchestrator produced an invalid executed schedule: " + string.Join("; ", validation.Findings));

        return executed;
    }

    private static LongHorizonExecutedWeek BuildGeExecutedWeek(
        LongHorizonStructuralWeek week, DateOnly weekStartDate, LongHorizonGeWeekNumericResult numeric,
        IReadOnlyDictionary<string, DayOfWeek> weekdayAssignments)
    {
        var slots = new List<LongHorizonExecutedWorkoutSlot>(4);
        foreach (var slot in week.OrderedWorkoutSlots)
        {
            var roleKey = RoleKeyFor(slot.StructuralRole, slot.StructuralSlotIndex);
            var weekday = weekdayAssignments[roleKey];
            var distance = slot.StructuralRole switch
            {
                "KEY_SESSION" => numeric.KeySessionDistanceKm,
                "LONG_RUN" => numeric.LongRunDistanceKm,
                "EASY_SUPPORT" when roleKey == "EASY_SUPPORT_1" => numeric.FirstEasySupportDistanceKm,
                "EASY_SUPPORT" => numeric.SecondEasySupportDistanceKm,
                _ => throw new ArgumentOutOfRangeException(),
            };
            slots.Add(new LongHorizonExecutedWorkoutSlot(
                slot, slot.WorkoutKey!, slot.WorkoutVersion!.Value, weekday,
                LongHorizonCalendarAssigner.AssignedDate(weekStartDate, weekday), distance,
                slot.StructuralRole == "LONG_RUN", "LongHorizonGeNumericExecutor"));
        }

        return new LongHorizonExecutedWeek(
            week, numeric.TotalVolumeKm, numeric.LongRunDistanceKm,
            LongHorizonGeNumericExecutor.PolicyId, LongHorizonGeNumericExecutor.PolicyVersion,
            weekStartDate, slots);
    }

    private static LongHorizonExecutedWeek BuildRunwayExecutedWeek(
        LongHorizonStructuralWeek week, DateOnly weekStartDate, IReadOnlyDictionary<string, DayOfWeek> weekdayAssignments)
    {
        var slots = new List<LongHorizonExecutedWorkoutSlot>(4);
        foreach (var slot in week.OrderedWorkoutSlots)
        {
            var roleKey = RoleKeyFor(slot.StructuralRole, slot.StructuralSlotIndex);
            var weekday = weekdayAssignments[roleKey];
            slots.Add(new LongHorizonExecutedWorkoutSlot(
                slot, slot.WorkoutKey!, slot.WorkoutVersion!.Value, weekday,
                LongHorizonCalendarAssigner.AssignedDate(weekStartDate, weekday), null,
                slot.StructuralRole == "LONG_RUN", "LongHorizonStructuralMaterializer(4I.5 reuse)"));
        }

        return new LongHorizonExecutedWeek(week, null, null, null, null, weekStartDate, slots);
    }

    private static LongHorizonExecutedWeek BuildCoreExecutedWeek(LongHorizonStructuralWeek week, BoundCatalogWeek bound, DateOnly weekStartDate)
    {
        var sessionsByRoleOccurrence = new Dictionary<string, int>();
        var slots = new List<LongHorizonExecutedWorkoutSlot>(week.OrderedWorkoutSlots.Count);
        foreach (var slot in week.OrderedWorkoutSlots)
        {
            sessionsByRoleOccurrence.TryGetValue(slot.StructuralRole, out var occurrence);
            var matches = bound.Sessions.Where(s => s.StructuralRole == slot.StructuralRole).ToList();
            var session = matches[occurrence];
            sessionsByRoleOccurrence[slot.StructuralRole] = occurrence + 1;

            slots.Add(new LongHorizonExecutedWorkoutSlot(
                slot, session.WorkoutDefinitionKey, session.WorkoutDefinitionVersion,
                session.Date.DayOfWeek, session.Date, null, slot.StructuralRole == "LONG_RUN",
                "LongHorizonCoreWorkoutBindingExecutor"));
        }

        return new LongHorizonExecutedWeek(week, null, null, null, null, weekStartDate, slots);
    }

    private static string RoleKeyFor(string structuralRole, int structuralSlotIndex) => structuralRole switch
    {
        "KEY_SESSION" => "KEY_SESSION",
        "LONG_RUN" => "LONG_RUN",
        "EASY_SUPPORT" => structuralSlotIndex <= 2 ? "EASY_SUPPORT_1" : "EASY_SUPPORT_2",
        _ => throw new ArgumentOutOfRangeException(nameof(structuralRole)),
    };
}
