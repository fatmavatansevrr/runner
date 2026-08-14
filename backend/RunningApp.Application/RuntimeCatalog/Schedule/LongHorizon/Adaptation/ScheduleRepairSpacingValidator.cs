using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.3 -- the smallest typed validator needed to check whether a real
/// candidate date would remain KEY/LONG-separation-valid if it started
/// carrying the trigger session's KEY_SESSION/LONG_RUN role. This is NOT a new
/// spacing rule: it reuses the exact existing threshold
/// (<see cref="DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays"/>)
/// already authoritative for plan generation/calendar materialization. That
/// validator operates on a fully-built <c>DatedGeneratedCatalogPlanSkeleton</c>,
/// not on ad hoc single-candidate checks against already-persisted rows, so it
/// cannot be called directly here -- this type is the thin adapter Phase
/// 4M.3's audit (§U) anticipated for exactly this situation.
/// </summary>
internal static class ScheduleRepairSpacingValidator
{
    /// <summary>
    /// True if placing the trigger's own role (always KEY_SESSION or
    /// LONG_RUN -- <see cref="ScheduleRepairCandidateProvider"/> only ever
    /// builds candidates for those two trigger roles) at <paramref name="candidateDate"/>
    /// would keep at least <see cref="DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays"/>
    /// days of separation from every other currently-Active session of the
    /// *opposite* hard-session role (KEY_SESSION <-> LONG_RUN) in the plan.
    /// Same-role (KEY-to-KEY, LONG-to-LONG) spacing has no existing canonical
    /// rule, so none is invented here.
    /// </summary>
    public static bool IsCandidateDateSpacingValid(
        LongHorizonRollingPlanState aggregate, LongHorizonRollingSessionState trigger, DateOnly candidateDate)
    {
        var triggerIsLongRun = LongHorizonSessionRoleCodec.IsLongRun(trigger.SessionRole);
        var triggerIsKeySession = LongHorizonSessionRoleCodec.IsKeySession(trigger.SessionRole);
        if (!triggerIsLongRun && !triggerIsKeySession)
            return true; // Defensive: candidates are never built for EASY_SUPPORT triggers.

        var opposingActiveSessions = aggregate.Weeks.SelectMany(w => w.Sessions)
            .Where(s => s.Id != trigger.Id && s.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Active)
            .Where(s => triggerIsLongRun
                ? LongHorizonSessionRoleCodec.IsKeySession(s.SessionRole)
                : LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole));

        foreach (var opposing in opposingActiveSessions)
        {
            var separationDays = Math.Abs(candidateDate.DayNumber - opposing.AssignedDate.DayNumber);
            if (separationDays < DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays)
                return false;
        }

        return true;
    }
}
