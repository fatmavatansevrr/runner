using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Progression;

public sealed class GeneratedCatalogStageScheduleValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<string> Errors { get; init; }

    public static GeneratedCatalogStageScheduleValidationResult Valid() => new() { IsValid = true, Errors = Array.Empty<string>() };
    public static GeneratedCatalogStageScheduleValidationResult Invalid(IReadOnlyList<string> errors) => new() { IsValid = false, Errors = errors };
}

public interface IGeneratedCatalogStageScheduleValidator
{
    GeneratedCatalogStageScheduleValidationResult Validate(GeneratedCatalogStageSchedule schedule, GeneratedCatalogPlanSkeleton skeleton);
}

/// <summary>
/// Backend Integration Phase 4F.6A — output validator for <see cref="GeneratedCatalogStageSchedule"/>
/// (Section 14). A pure, deterministic re-check of the allocator's own output against the
/// skeleton it was built from — never re-runs allocation, never re-evaluates a runtime
/// condition, never mutates its input.
/// </summary>
public sealed class GeneratedCatalogStageScheduleValidator : IGeneratedCatalogStageScheduleValidator
{
    public GeneratedCatalogStageScheduleValidationResult Validate(GeneratedCatalogStageSchedule schedule, GeneratedCatalogPlanSkeleton skeleton)
    {
        var errors = new List<string>();

        if (schedule.Weeks.Count != skeleton.Weeks.Count)
        {
            errors.Add($"Total week count mismatch: schedule has {schedule.Weeks.Count}, skeleton has {skeleton.Weeks.Count}.");
        }

        var weekNumberCounts = schedule.Weeks.GroupBy(w => w.WeekNumber).ToList();
        foreach (var group in weekNumberCounts.Where(g => g.Count() > 1))
        {
            errors.Add($"Week {group.Key} is assigned more than once ({group.Count()} times).");
        }

        var skeletonWeeksByNumber = skeleton.Weeks.ToDictionary(w => w.WeekNumber);
        foreach (var week in schedule.Weeks)
        {
            if (!skeletonWeeksByNumber.TryGetValue(week.WeekNumber, out var skeletonWeek))
            {
                errors.Add($"Week {week.WeekNumber} does not exist in the source skeleton.");
                continue;
            }

            if (skeletonWeek.StageKey != week.PhaseKey)
            {
                errors.Add(
                    $"Week {week.WeekNumber} is assigned to phase '{week.PhaseKey}', but the skeleton assigns this week to phase " +
                    $"'{skeletonWeek.StageKey}'.");
            }

            if (week.StructuralRole != ScheduledProgressionWeek.KeySessionStructuralRole)
            {
                errors.Add($"Week {week.WeekNumber} has structural role '{week.StructuralRole}' — only KEY_SESSION is represented by this scheduler.");
            }

            if (string.IsNullOrWhiteSpace(week.ProgressionStageKey))
            {
                errors.Add($"Week {week.WeekNumber} has no ProgressionStageKey.");
            }

            if (string.IsNullOrWhiteSpace(week.PhaseKey))
            {
                errors.Add($"Week {week.WeekNumber} has no PhaseKey.");
            }
        }

        if (string.IsNullOrWhiteSpace(schedule.ProgressionArtifactKey) || schedule.ProgressionArtifactVersion <= 0)
        {
            errors.Add("Schedule is missing progression artifact key/version provenance.");
        }

        // Ordering monotonicity within each phase: for a given phase, weeks must be
        // assignable to a contiguous, ascending-RelativeOrder stage sequence — i.e. once the
        // stage's RelativeOrder decreases going forward through the phase's own week order,
        // that is a violation (blocks must not alternate/regress).
        foreach (var phaseGroup in schedule.Weeks.GroupBy(w => w.PhaseKey))
        {
            var orderedByWeek = phaseGroup.OrderBy(w => w.WeekNumber).ToList();
            var lastOrder = int.MinValue;
            string? lastStageKey = null;
            var seenStageKeysInPhase = new HashSet<string>();

            foreach (var week in orderedByWeek)
            {
                if (week.StageRelativeOrder < lastOrder)
                {
                    errors.Add(
                        $"Phase '{phaseGroup.Key}' week {week.WeekNumber} has RelativeOrder {week.StageRelativeOrder}, which is lower than " +
                        $"a prior week's RelativeOrder {lastOrder} — ordering is not monotonic within the phase.");
                }

                if (week.ProgressionStageKey != lastStageKey)
                {
                    if (!seenStageKeysInPhase.Add(week.ProgressionStageKey))
                    {
                        errors.Add(
                            $"Phase '{phaseGroup.Key}' stage '{week.ProgressionStageKey}' occupies a non-contiguous block " +
                            $"(re-appears after another stage at week {week.WeekNumber}).");
                    }
                }

                lastOrder = week.StageRelativeOrder;
                lastStageKey = week.ProgressionStageKey;
            }
        }

        return errors.Count == 0 ? GeneratedCatalogStageScheduleValidationResult.Valid() : GeneratedCatalogStageScheduleValidationResult.Invalid(errors);
    }
}
