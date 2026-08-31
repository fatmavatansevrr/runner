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

        // Backend Integration Phase 10K-FREQ.6D.4D Split A: every declared lane independently
        // covers the full skeleton (coordinated shared phase timeline, §8/§22) — checked per
        // lane rather than against the raw total, which degenerates to the original
        // single-lane check when only LaneOrdinal 0 exists.
        //
        // Phase 10K-GEN.17: "the full skeleton" for a lane means the skeleton weeks that
        // structurally carry a slot for THAT lane, not skeleton.Weeks.Count unconditionally —
        // the same "structural ordinal N binds to LaneOrdinal N" rule ProgressionStageAllocator
        // and CatalogWorkoutBinder both already apply (a week's Nth-occurring KEY_SESSION slot
        // is lane N). For every pre-GEN.12 frequency every lane's role is present in 100% of a
        // phase's weeks (GEN.13 §2), so eligibleWeekCount is always identical to
        // skeleton.Weeks.Count — byte-identical to pre-GEN.17 behavior for every already-shipped
        // candidate.
        // A week with an empty SessionSlots list carries no structural information to filter
        // on (e.g. a synthetic skeleton built directly for an allocator/validator unit test) --
        // it remains eligible, matching ProgressionStageAllocator's own identical fallback.
        foreach (var laneGroup in schedule.Weeks.GroupBy(w => w.LaneOrdinal))
        {
            var laneOrdinal = laneGroup.Key;
            var eligibleWeekCount = skeleton.Weeks.Count(w =>
                w.SessionSlots.Count == 0
                || w.SessionSlots.Count(s => s.StructuralRole == ScheduledProgressionWeek.KeySessionStructuralRole) > laneOrdinal);

            if (laneGroup.Count() != eligibleWeekCount)
            {
                errors.Add($"Lane {laneOrdinal}: total week count mismatch: schedule has {laneGroup.Count()}, eligible skeleton weeks has {eligibleWeekCount}.");
            }
        }

        // Backend Integration Phase 10K-FREQ.6D.4D Split A: uniqueness is keyed by
        // (WeekNumber, LaneOrdinal), never WeekNumber alone — the pre-Split-A defect this
        // phase closes was exactly a WeekNumber-only uniqueness assumption. For single-lane
        // schedules (LaneOrdinal always 0) this degenerates to the original WeekNumber-only
        // check, byte-for-byte.
        var weekLaneCounts = schedule.Weeks.GroupBy(w => (w.WeekNumber, w.LaneOrdinal)).ToList();
        foreach (var group in weekLaneCounts.Where(g => g.Count() > 1))
        {
            errors.Add($"Week {group.Key.WeekNumber} lane {group.Key.LaneOrdinal} is assigned more than once ({group.Count()} times).");
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
        // Backend Integration Phase 10K-FREQ.6D.4D Split A: monotonicity is checked per
        // (PhaseKey, LaneOrdinal) — each lane's own stage sequence must be internally
        // contiguous/monotonic, independent of every other lane's. Degenerates to the
        // original per-phase-only check for single-lane schedules.
        foreach (var phaseLaneGroup in schedule.Weeks.GroupBy(w => (w.PhaseKey, w.LaneOrdinal)))
        {
            var orderedByWeek = phaseLaneGroup.OrderBy(w => w.WeekNumber).ToList();
            var lastOrder = int.MinValue;
            string? lastStageKey = null;
            var seenStageKeysInPhase = new HashSet<string>();

            foreach (var week in orderedByWeek)
            {
                if (week.StageRelativeOrder < lastOrder)
                {
                    errors.Add(
                        $"Phase '{phaseLaneGroup.Key.PhaseKey}' lane {phaseLaneGroup.Key.LaneOrdinal} week {week.WeekNumber} has RelativeOrder {week.StageRelativeOrder}, which is lower than " +
                        $"a prior week's RelativeOrder {lastOrder} — ordering is not monotonic within the phase.");
                }

                if (week.ProgressionStageKey != lastStageKey)
                {
                    if (!seenStageKeysInPhase.Add(week.ProgressionStageKey))
                    {
                        errors.Add(
                            $"Phase '{phaseLaneGroup.Key.PhaseKey}' lane {phaseLaneGroup.Key.LaneOrdinal} stage '{week.ProgressionStageKey}' occupies a non-contiguous block " +
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
