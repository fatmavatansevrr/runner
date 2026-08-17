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
///
/// Phase 10K-FREQ.4: KEY-to-KEY same-role spacing (for layouts with more
/// than one KEY_SESSION per week, e.g. a hypothetical Intermediate 5D
/// layout) previously had no rule here at all -- self-disclosed as a real
/// gap in this class's own prior doc comment, confirmed by FREQ.3 §D.3, now
/// closed below using <see cref="DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToKeySessionSeparationDays"/>
/// (the same embedded PRODUCT_DEFAULT value, not independently re-derived).
/// LONG-to-LONG spacing still has no rule -- out of scope for this fix
/// (no layout in this engagement has more than one LONG_RUN per week).
/// </summary>
internal static class ScheduleRepairSpacingValidator
{
    /// <summary>
    /// True if placing the trigger's own role (always KEY_SESSION or
    /// LONG_RUN -- <see cref="ScheduleRepairCandidateProvider"/> only ever
    /// builds candidates for those two trigger roles) at <paramref name="candidateDate"/>
    /// would keep at least <see cref="DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays"/>
    /// days of separation from every other currently-Active session of the
    /// *opposite* hard-session role (KEY_SESSION <-> LONG_RUN), AND (Phase
    /// 10K-FREQ.4) at least <see cref="DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToKeySessionSeparationDays"/>
    /// days from every other currently-Active KEY_SESSION when the trigger
    /// itself is a KEY_SESSION. For a single-KEY-per-week plan (every
    /// existing 3D/4D candidate) there are never two Active KEY_SESSION
    /// instances in the same plan at once under today's layouts, so this
    /// addition is a behavioral no-op for them, verified by regression.
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

        if (triggerIsKeySession)
        {
            var otherActiveKeySessions = aggregate.Weeks.SelectMany(w => w.Sessions)
                .Where(s => s.Id != trigger.Id && s.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Active)
                .Where(s => LongHorizonSessionRoleCodec.IsKeySession(s.SessionRole));

            foreach (var otherKey in otherActiveKeySessions)
            {
                var separationDays = Math.Abs(candidateDate.DayNumber - otherKey.AssignedDate.DayNumber);
                if (separationDays < DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToKeySessionSeparationDays)
                    return false;
            }
        }

        return true;
    }
}
