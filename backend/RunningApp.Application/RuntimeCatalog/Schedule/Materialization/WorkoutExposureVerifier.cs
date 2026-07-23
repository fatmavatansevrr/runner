using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

internal enum WorkoutExposureOutcome { Pass, Fail, DecisionRequired, NotApplicable }

internal enum WorkoutExposureFindingCode
{
    AllocationNotMathematicallyFeasible, PlanWeekMissing, PlanWeekDuplicate, PlanWeekCountMismatch,
    UnknownWeeklyRole, WeeklyRoleExposureCountMismatch, DuplicateSlotIndex, InvalidSlotIndex,
    TotalWorkoutExposureCountMismatch, RestExposurePresent, KeySessionEffectiveStageMissing,
    KeySessionWorkoutMappingMissing, MultipleStageExposuresInOneKeySlot, FallbackCreatedDuplicateExposure,
    EasySupportWorkoutMappingMissing, LongRunWorkoutMappingMissing, WorkoutPhaseOwnershipViolation,
    TaperSharpenExposureMissing, TaperSharpenOutsideTaper, RaceSpecificExposureOutsidePhase,
    WorkoutStageMinimumExposureUnsatisfied, WorkoutStageMaximumExposureExceeded,
    WorkoutMappingUnresolved, ExactWorkoutExposureMatch,
}

internal sealed record WorkoutExposureFinding(WorkoutExposureFindingCode Code, string Message,
    int? WeekNumber = null, string? Role = null, string? StageKey = null, int? Expected = null, int? Actual = null);

internal sealed record WorkoutExposure(int SlotIndex, string Role, string? RequestedStageKey,
    string? EffectiveStageKey, string WorkoutKey, bool UsedFallback);

internal sealed record WeekWorkoutExposureResult(int WeekNumber, string PhaseKey, int ExpectedExposureCount,
    int ActualExposureCount, IReadOnlyDictionary<string, int> RoleCounts, IReadOnlyList<WorkoutExposure> Exposures,
    WorkoutExposureOutcome Outcome, IReadOnlyList<WorkoutExposureFinding> Findings);

internal sealed record WorkoutExposureVerificationResult(int TargetWeeks, int ExpectedTotalExposureCount,
    int ActualTotalExposureCount, IReadOnlyDictionary<string, int> ExpectedRoleCounts,
    IReadOnlyDictionary<string, int> ActualRoleCounts, IReadOnlyDictionary<string, int> WorkoutKeyExposureCounts,
    IReadOnlyList<WeekWorkoutExposureResult> WeekResults, WorkoutExposureOutcome Outcome,
    IReadOnlyList<WorkoutExposureFinding> Findings);

/// <summary>Dark structural exposure verifier over already-materialized and already-bound contracts. No mapping, scheduling, I/O or condition resolution occurs here.</summary>
internal static class WorkoutExposureVerifier
{
    public static WorkoutExposureVerificationResult Verify(PhaseAllocationResult allocation,
        DatedGeneratedCatalogPlanSkeleton skeleton, BoundCatalogPlan boundPlan,
        CatalogWorkoutProgressionDefinition progression, IReadOnlyList<string> expectedWeeklySlotRoles)
    {
        var expectedRoles = expectedWeeklySlotRoles.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
        var expectedTotal = allocation.TargetWeeks * expectedWeeklySlotRoles.Count;
        if (!allocation.IsMathematicallyFeasible)
            return Result(allocation.TargetWeeks, expectedTotal, 0, expectedRoles,
                new Dictionary<string, int>(), new Dictionary<string, int>(), [], WorkoutExposureOutcome.NotApplicable,
                [Finding(WorkoutExposureFindingCode.AllocationNotMathematicallyFeasible, "Allocation is not mathematically feasible.")]);

        var findings = new List<WorkoutExposureFinding>();
        if (skeleton.Weeks.Count != allocation.TargetWeeks)
            findings.Add(Finding(WorkoutExposureFindingCode.PlanWeekCountMismatch, "Generated week count does not equal target weeks.", expected: allocation.TargetWeeks, actual: skeleton.Weeks.Count));
        foreach (var duplicate in skeleton.Weeks.GroupBy(w => w.WeekNumber).Where(g => g.Count() > 1))
            findings.Add(Finding(WorkoutExposureFindingCode.PlanWeekDuplicate, "Duplicate plan week.", duplicate.Key));
        for (var n = 1; n <= allocation.TargetWeeks; n++)
            if (!skeleton.Weeks.Any(w => w.WeekNumber == n)) findings.Add(Finding(WorkoutExposureFindingCode.PlanWeekMissing, "Plan week is missing.", n));

        var phaseByStage = progression.PhaseProgressions.SelectMany(p => p.Stages.Select(s => (s.ProgressionStageKey, p.PhaseKey)))
            .GroupBy(x => x.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Select(x => x.PhaseKey).Distinct().ToList());
        var trace = boundPlan.Trace.Steps;
        var weekResults = new List<WeekWorkoutExposureResult>();

        foreach (var week in skeleton.Weeks.OrderBy(w => w.WeekNumber))
        {
            var weekFindings = new List<WorkoutExposureFinding>();
            var slots = week.SessionSlots;
            foreach (var duplicate in slots.GroupBy(s => s.SlotOrderInWeek).Where(g => g.Count() > 1))
                weekFindings.Add(Finding(WorkoutExposureFindingCode.DuplicateSlotIndex, "Duplicate slot index.", week.WeekNumber, actual: duplicate.Key));
            foreach (var slot in slots.Where(s => s.SlotOrderInWeek <= 0))
                weekFindings.Add(Finding(WorkoutExposureFindingCode.InvalidSlotIndex, "Slot index must be positive.", week.WeekNumber, slot.StructuralRole, actual: slot.SlotOrderInWeek));

            var actualCounts = slots.GroupBy(s => s.StructuralRole).ToDictionary(g => g.Key, g => g.Count());
            foreach (var role in actualCounts.Keys.Where(r => !expectedRoles.ContainsKey(r)))
            {
                var code = role.Contains("REST", StringComparison.OrdinalIgnoreCase) ? WorkoutExposureFindingCode.RestExposurePresent : WorkoutExposureFindingCode.UnknownWeeklyRole;
                weekFindings.Add(Finding(code, "Role is not part of the supplied layout contract.", week.WeekNumber, role));
            }
            foreach (var expected in expectedRoles)
            {
                var actual = actualCounts.GetValueOrDefault(expected.Key);
                if (actual != expected.Value)
                    weekFindings.Add(Finding(WorkoutExposureFindingCode.WeeklyRoleExposureCountMismatch,
                        "Weekly role exposure multiplicity differs from the layout.", week.WeekNumber, expected.Key, expected: expected.Value, actual: actual));
            }

            var boundWeek = boundPlan.Weeks.Where(w => w.WeekNumber == week.WeekNumber).ToList();
            var exposures = new List<WorkoutExposure>();
            if (boundWeek.Count != 1)
                weekFindings.Add(Finding(WorkoutExposureFindingCode.WorkoutMappingUnresolved, "Bound workout week is missing or duplicated.", week.WeekNumber));
            else
            {
                var remaining = boundWeek[0].Sessions.ToList();
                foreach (var slot in slots.OrderBy(s => s.SlotOrderInWeek))
                {
                    var matches = remaining.Where(s => s.StructuralRole == slot.StructuralRole && s.Date == slot.SessionDate).ToList();
                    if (matches.Count != 1)
                    {
                        weekFindings.Add(Finding(WorkoutExposureFindingCode.WorkoutMappingUnresolved,
                            "Slot does not map one-for-one to a bound workout session.", week.WeekNumber, slot.StructuralRole));
                        continue;
                    }
                    var session = matches[0]; remaining.Remove(session);
                    var stepMatches = trace.Where(t => t.WeekNumber == week.WeekNumber && t.Date == slot.SessionDate && t.StructuralRole == slot.StructuralRole).ToList();
                    var step = stepMatches.Count == 1 ? stepMatches[0] : null;
                    var requested = step?.RequestedStageKey;
                    var effective = session.ProgressionStageKey;
                    var usedFallback = step?.FallbackOrigin is not null || session.FallbackOrigin is not null;

                    if (slot.StructuralRole == "KEY_SESSION")
                    {
                        if (string.IsNullOrWhiteSpace(effective)) weekFindings.Add(Finding(WorkoutExposureFindingCode.KeySessionEffectiveStageMissing, "KEY_SESSION has no effective stage.", week.WeekNumber, slot.StructuralRole));
                        if (string.IsNullOrWhiteSpace(session.WorkoutDefinitionKey)) weekFindings.Add(Finding(WorkoutExposureFindingCode.KeySessionWorkoutMappingMissing, "KEY_SESSION has no resolved workout.", week.WeekNumber, slot.StructuralRole, effective));
                        if (stepMatches.Count > 1) weekFindings.Add(Finding(WorkoutExposureFindingCode.MultipleStageExposuresInOneKeySlot, "More than one binding trace maps to one key slot.", week.WeekNumber, slot.StructuralRole));
                    }
                    else if (slot.StructuralRole == "EASY_SUPPORT" && string.IsNullOrWhiteSpace(session.WorkoutDefinitionKey))
                        weekFindings.Add(Finding(WorkoutExposureFindingCode.EasySupportWorkoutMappingMissing, "EASY_SUPPORT mapping is missing.", week.WeekNumber, slot.StructuralRole));
                    else if (slot.StructuralRole == "LONG_RUN" && string.IsNullOrWhiteSpace(session.WorkoutDefinitionKey))
                        weekFindings.Add(Finding(WorkoutExposureFindingCode.LongRunWorkoutMappingMissing, "LONG_RUN mapping is missing.", week.WeekNumber, slot.StructuralRole));

                    if (effective is not null && phaseByStage.TryGetValue(effective, out var owners) && !owners.Contains(week.PhaseKey))
                    {
                        var code = owners.Contains("RACE_SPECIFIC") ? WorkoutExposureFindingCode.RaceSpecificExposureOutsidePhase
                            : effective == "TAPER_SHARPEN" ? WorkoutExposureFindingCode.TaperSharpenOutsideTaper
                            : WorkoutExposureFindingCode.WorkoutPhaseOwnershipViolation;
                        weekFindings.Add(Finding(code, "Effective stage exposure occurs outside its progression phase.", week.WeekNumber, slot.StructuralRole, effective));
                    }
                    exposures.Add(new(slot.SlotOrderInWeek, slot.StructuralRole, requested, effective, session.WorkoutDefinitionKey, usedFallback));
                }
                if (remaining.Count > 0)
                    weekFindings.Add(Finding(WorkoutExposureFindingCode.TotalWorkoutExposureCountMismatch, "Bound week contains sessions without skeleton slots.", week.WeekNumber, actual: remaining.Count));
            }

            var bad = weekFindings.Count > 0;
            weekResults.Add(new(week.WeekNumber, week.PhaseKey, expectedWeeklySlotRoles.Count, exposures.Count,
                actualCounts, exposures, bad ? WorkoutExposureOutcome.Fail : WorkoutExposureOutcome.Pass, weekFindings));
            findings.AddRange(weekFindings);
        }

        var allExposures = weekResults.SelectMany(w => w.Exposures).ToList();
        if (allExposures.Count != expectedTotal)
            findings.Add(Finding(WorkoutExposureFindingCode.TotalWorkoutExposureCountMismatch, "Total bound workout exposures differ from target weeks × layout slots.", expected: expectedTotal, actual: allExposures.Count));

        var requestedCounts = allExposures.Where(e => e.Role == "KEY_SESSION" && e.RequestedStageKey is not null).GroupBy(e => e.RequestedStageKey!).ToDictionary(g => g.Key, g => g.Count());
        var fallbackTargets = progression.PhaseProgressions.SelectMany(p => p.Stages).Where(s => s.FallbackStageKey is not null).Select(s => s.FallbackStageKey!).ToHashSet();
        foreach (var stage in progression.PhaseProgressions.SelectMany(p => p.Stages).Where(s => !fallbackTargets.Contains(s.ProgressionStageKey)))
        {
            var actual = requestedCounts.GetValueOrDefault(stage.ProgressionStageKey);
            var floor = stage.CompressionBehavior == CatalogStageCompressionBehavior.Compressible ? Math.Min(stage.MinimumExposures, 1) : stage.MinimumExposures;
            if (actual < floor) findings.Add(Finding(WorkoutExposureFindingCode.WorkoutStageMinimumExposureUnsatisfied, "Materialized key workouts drift below the scheduled stage floor.", stageKey: stage.ProgressionStageKey, expected: floor, actual: actual));
            if (actual > stage.MaximumExposures) findings.Add(Finding(WorkoutExposureFindingCode.WorkoutStageMaximumExposureExceeded, "Materialized key workouts exceed stage maximum.", stageKey: stage.ProgressionStageKey, expected: stage.MaximumExposures, actual: actual));
        }

        var taperExists = allocation.Phases.Any(p => p.PhaseKey == "TAPER");
        if (taperExists && !allExposures.Any(e => e.EffectiveStageKey == "TAPER_SHARPEN"))
            findings.Add(Finding(WorkoutExposureFindingCode.TaperSharpenExposureMissing, "TAPER phase exists but no TAPER_SHARPEN key exposure materialized."));

        if (findings.Count == 0) findings.Add(Finding(WorkoutExposureFindingCode.ExactWorkoutExposureMatch, "Every generated slot maps one-for-one to the expected bound workout exposure."));
        var outcome = findings.All(f => f.Code == WorkoutExposureFindingCode.ExactWorkoutExposureMatch) ? WorkoutExposureOutcome.Pass : WorkoutExposureOutcome.Fail;
        var actualRoles = allExposures.GroupBy(e => e.Role).ToDictionary(g => g.Key, g => g.Count());
        var workoutCounts = allExposures.GroupBy(e => e.WorkoutKey).ToDictionary(g => g.Key, g => g.Count());
        return Result(allocation.TargetWeeks, expectedTotal, allExposures.Count, expectedRoles, actualRoles, workoutCounts, weekResults, outcome, findings);
    }

    private static WorkoutExposureVerificationResult Result(int target, int expected, int actual,
        IReadOnlyDictionary<string, int> expectedRoles, IReadOnlyDictionary<string, int> actualRoles,
        IReadOnlyDictionary<string, int> workouts, IReadOnlyList<WeekWorkoutExposureResult> weeks,
        WorkoutExposureOutcome outcome, IReadOnlyList<WorkoutExposureFinding> findings) =>
        new(target, expected, actual, expectedRoles, actualRoles, workouts, weeks, outcome, findings);
    private static WorkoutExposureFinding Finding(WorkoutExposureFindingCode code, string message, int? week = null,
        string? role = null, string? stageKey = null, int? expected = null, int? actual = null) => new(code, message, week, role, stageKey, expected, actual);
}
