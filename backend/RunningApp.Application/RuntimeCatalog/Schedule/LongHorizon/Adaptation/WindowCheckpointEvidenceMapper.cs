using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.4A -- the real structural input authority for
/// <see cref="WindowExecutionSummaryBuilder"/>: maps real persisted
/// <see cref="LongHorizonRollingSessionState"/> rows for a checkpointed
/// window into <see cref="LogicalSessionEvidence"/>. Owns no adherence/
/// completion rule of its own -- it is a pure field-by-field translation
/// (persisted role token/OutcomeStatus/PlanningStatus/AdaptedFromSessionId/
/// NotTodayReason -> the pure-domain vocabulary those same concepts already
/// have from Phase 4M.1/4M.3), never a re-derivation of what "recovered",
/// "expected", or "effectively completed" mean -- those remain
/// <see cref="WindowExecutionSummaryBuilder"/>'s sole authority.
/// </summary>
internal static class WindowCheckpointEvidenceMapper
{
    public static IReadOnlyList<LogicalSessionEvidence> ToEvidence(IReadOnlyList<LongHorizonRollingSessionState> windowSessions) =>
        windowSessions.Select(ToEvidence).ToList();

    private static LogicalSessionEvidence ToEvidence(LongHorizonRollingSessionState session)
    {
        var role = AdaptationSessionRoleResolver.Resolve(session.SessionRole);

        var planningStatus = session.PlanningStatus == LongHorizonPersistedSessionPlanningStatus.Superseded
            ? SessionPlanningStatus.Superseded
            : SessionPlanningStatus.Active;

        NotTodayReasonCode? notTodayReason = null;
        if (session.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.NotToday && session.NotTodayReason is { } runtimeReason)
            notTodayReason = RuntimeNotTodayReasonMapper.ToReasonCode(RuntimeNotTodayReasonMapper.Map(runtimeReason));

        return new LogicalSessionEvidence(
            session.Id, role, session.OutcomeStatus, planningStatus, session.AdaptedFromSessionId, notTodayReason);
    }
}
