using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.3 -- the real structural candidate-query layer Phase 4M.1
/// intentionally left for a later phase (its own tests hand-supplied
/// candidate lists). This type does NOT decide the repair action -- it only
/// produces structurally eligible facts (<see cref="ScheduleRepairCandidate"/>)
/// from the already-loaded plan aggregate for <see cref="ScheduleRepairPolicy"/>
/// (via <see cref="CandidateSelectionPolicy"/>) to choose from. No DB access
/// of its own: callers already have the full aggregate loaded (the same
/// eager-loaded graph <c>LongHorizonRollingSessionMutationService.LoadOwnedAsync</c>
/// already fetches for the NotToday mutation itself), so no extra round trip
/// is needed to build candidates.
/// </summary>
internal static class ScheduleRepairCandidateProvider
{
    /// <summary>
    /// Rev3.1 §3 PreferredDayConstraint candidates: a future date, inside the
    /// trigger's own rolling window and phase, that is one of the plan's
    /// PreferredDays and carries no conflicting Active session yet.
    /// <see cref="ScheduleRepairCandidate.IsSafetyValid"/> additionally
    /// requires KEY/LONG spacing validity (<see cref="ScheduleRepairSpacingValidator"/>)
    /// -- a structurally eligible-but-spacing-invalid date is still returned
    /// (IsSafetyValid=false), matching <see cref="CandidateSelectionPolicy"/>'s
    /// own "skip, don't disqualify the search" contract.
    /// </summary>
    public static IReadOnlyList<ScheduleRepairCandidate> GetEmptySlotCandidates(
        LongHorizonRollingPlanState aggregate, LongHorizonRollingSessionState trigger)
    {
        var preferredDays = ParsePreferredDaysCsv(aggregate.PreferredDaysCsv);
        var activeDates = aggregate.Weeks.SelectMany(w => w.Sessions)
            .Where(s => s.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Active)
            .Select(s => s.AssignedDate)
            .ToHashSet();

        var candidates = new List<ScheduleRepairCandidate>();
        foreach (var week in SameWindowSamePhaseWeeks(aggregate, trigger))
        {
            var phase = new AdaptationPhaseIdentity(week.SegmentType, week.Stage);
            for (var date = week.StructuralStartDate; date <= week.StructuralEndDate; date = date.AddDays(1))
            {
                if (date <= trigger.AssignedDate) continue;
                if (!preferredDays.Contains(date.DayOfWeek)) continue;
                if (activeDates.Contains(date)) continue;

                var isValid = ScheduleRepairSpacingValidator.IsCandidateDateSpacingValid(aggregate, trigger, date);
                candidates.Add(new ScheduleRepairCandidate(date, phase, isValid));
            }
        }
        return candidates;
    }

    /// <summary>
    /// Rev3.1 §3 SingleSessionSubstitution candidates: a future, same-window,
    /// same-phase, Active/Planned EASY_SUPPORT session. Its own AssignedDate is
    /// already a valid PreferredDay under the current plan (it is a real,
    /// already-scheduled session), so only KEY/LONG spacing is re-checked here.
    /// </summary>
    public static IReadOnlyList<ScheduleRepairCandidate> GetFutureEasySubstitutionCandidates(
        LongHorizonRollingPlanState aggregate, LongHorizonRollingSessionState trigger)
    {
        var candidates = new List<ScheduleRepairCandidate>();
        foreach (var week in SameWindowSamePhaseWeeks(aggregate, trigger))
        {
            var phase = new AdaptationPhaseIdentity(week.SegmentType, week.Stage);
            foreach (var session in week.Sessions)
            {
                if (session.AssignedDate <= trigger.AssignedDate) continue;
                if (session.PlanningStatus != LongHorizonPersistedSessionPlanningStatus.Active) continue;
                if (session.OutcomeStatus != LongHorizonRollingSessionOutcomeStatus.Planned) continue;
                if (!LongHorizonSessionRoleCodec.IsEasySupport(session.SessionRole)) continue;

                var isValid = ScheduleRepairSpacingValidator.IsCandidateDateSpacingValid(aggregate, trigger, session.AssignedDate);
                candidates.Add(new ScheduleRepairCandidate(
                    session.AssignedDate, phase, isValid,
                    SourceSessionId: session.Id,
                    SourcePlanningStatus: SessionPlanningStatus.Active,
                    SourceRole: PreparationRunwaySlotRole.EasySupport));
            }
        }
        return candidates;
    }

    private static IEnumerable<LongHorizonRollingWeekState> SameWindowSamePhaseWeeks(
        LongHorizonRollingPlanState aggregate, LongHorizonRollingSessionState trigger) =>
        aggregate.Weeks.Where(w =>
            w.GlobalWeek >= aggregate.CurrentWindowStartWeek && w.GlobalWeek <= aggregate.CurrentWindowEndWeek
            && w.SegmentType == trigger.Week.SegmentType
            && w.Stage == trigger.Week.Stage);

    private static HashSet<DayOfWeek> ParsePreferredDaysCsv(string csv) => csv
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(d => Enum.Parse<DayOfWeek>(d, ignoreCase: true))
        .ToHashSet();
}
