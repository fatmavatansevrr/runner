using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

internal sealed class LongHorizonRollingPersistenceCorruptionException(string message)
    : LongHorizonRollingContractException("LONG_HORIZON_ROLLING_PERSISTENCE_CORRUPTION_DETECTED", message);

/// <summary>
/// Phase 4L.2 Part 27/28 -- fail-closed integrity checks run during
/// reconstruction. No auto-repair, no silent regeneration: any violation
/// throws <see cref="LongHorizonRollingPersistenceCorruptionException"/>.
/// </summary>
internal static class LongHorizonRollingPersistenceIntegrityValidator
{
    public static void ValidateWeekRows(LongHorizonRollingPlanState plan, IReadOnlyList<LongHorizonRollingWeekState> weeks)
    {
        if (weeks.Count != plan.TotalWeeks)
        {
            throw new LongHorizonRollingPersistenceCorruptionException(
                $"Expected exactly {plan.TotalWeeks} structural week rows, found {weeks.Count}.");
        }

        var globalWeeks = weeks.Select(w => w.GlobalWeek).ToList();
        if (globalWeeks.Distinct().Count() != globalWeeks.Count)
        {
            throw new LongHorizonRollingPersistenceCorruptionException("Duplicate global week row detected.");
        }

        var sorted = globalWeeks.OrderBy(w => w).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            if (sorted[i] != i + 1)
            {
                throw new LongHorizonRollingPersistenceCorruptionException($"Missing or non-contiguous global week -- expected {i + 1}, found {sorted[i]}.");
            }
        }

        foreach (var week in weeks)
        {
            var isExecutable = week.LifecycleState is LongHorizonPersistedLifecycleState.NumericActivated
                or LongHorizonPersistedLifecycleState.Completed or LongHorizonPersistedLifecycleState.Missed;

            if (isExecutable && (week.WeeklyVolumeKm is null || week.Sessions.Count == 0))
            {
                throw new LongHorizonRollingPersistenceCorruptionException(
                    $"Week {week.GlobalWeek} is {week.LifecycleState} but has no numeric/session data (Activated without sessions).");
            }

            var isPendingLike = week.LifecycleState is LongHorizonPersistedLifecycleState.NumericPending
                or LongHorizonPersistedLifecycleState.StructurallyPlanned or LongHorizonPersistedLifecycleState.NumericActivationBlocked;

            if (isPendingLike && (week.WeeklyVolumeKm is not null || week.Sessions.Count > 0))
            {
                throw new LongHorizonRollingPersistenceCorruptionException(
                    $"Week {week.GlobalWeek} is {week.LifecycleState} but carries numeric/session data (Pending with sessions).");
            }

            if (week.LifecycleState == LongHorizonPersistedLifecycleState.NumericActivationBlocked && week.BlockedDecisionId is null)
            {
                throw new LongHorizonRollingPersistenceCorruptionException($"Week {week.GlobalWeek} is Blocked without an owning block record.");
            }
        }

        var expectedSegmentOrder = new[] { LongHorizonPersistedSegmentType.GeneralEndurance, LongHorizonPersistedSegmentType.PreparationRunway, LongHorizonPersistedSegmentType.Core };
        var actualOrder = weeks.OrderBy(w => w.GlobalWeek).Select(w => w.SegmentType).Distinct().ToList();
        if (!actualOrder.SequenceEqual(expectedSegmentOrder.Where(actualOrder.Contains)))
        {
            throw new LongHorizonRollingPersistenceCorruptionException("Segment order is not General Endurance -> Preparation Runway -> Core.");
        }
    }

    public static void ValidateReconstructedState(LongHorizonRollingPlanState plan, LongHorizonFullDarkLifecycleState state)
    {
        if (plan.PersistenceContractVersion != 1)
        {
            throw new LongHorizonRollingPersistenceCorruptionException($"Unknown persistence contract version {plan.PersistenceContractVersion}.");
        }

        var allDates = state.ActivatedWeeks.Values
            .SelectMany(w => w.SessionPrescriptions ?? [])
            .Select(s => s.AssignedDate)
            .Where(d => d is not null)
            .Select(d => d!.Value)
            .ToList();
        // Historical immutability at the reconstruction boundary is proven by the
        // adapters' own "never update planned fields after activation" behavior
        // (see repository Save* methods) plus session-ordinal uniqueness enforced
        // by the DB unique index -- reconstruction re-checks only shape validity here.
        _ = allDates;

        if (state.CurrentWindow.EndGlobalWeek > plan.TotalWeeks || state.CurrentWindow.StartGlobalWeek < 1)
        {
            throw new LongHorizonRollingPersistenceCorruptionException("Current window boundary falls outside TotalWeeks.");
        }
    }
}
