using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.4B.2 confirmation pass -- extracted shared authority (originally
/// duplicated between <see cref="WindowCheckpointEvidenceMapper"/> and
/// <see cref="ScheduleRepairRuntimeOrchestrator"/>, where only the former had
/// been fixed to handle General Endurance's suffixed role tokens; the latter
/// crashed with <see cref="LongHorizonAdaptationIntegrityViolationException"/>
/// whenever a real GE-phase EASY_SUPPORT_1/EASY_SUPPORT_2 session was marked
/// NotToday -- a real defect this confirmation pass's real-HTTP testing
/// exposed, never reachable by any prior fixture-based 4M.3 test).
/// <see cref="LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy"/>
/// deliberately fails closed on those suffixed tokens (by its own doc
/// comment, GE ordinal information the enum can't represent); this resolver
/// falls back to the same prefix-matching helpers
/// (<see cref="LongHorizonSessionRoleCodec.IsEasySupport"/> etc.) every
/// other GE-aware call site already uses for "which role is this".
/// </summary>
internal static class AdaptationSessionRoleResolver
{
    public static PreparationRunwaySlotRole Resolve(string sessionRole)
    {
        if (LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy(sessionRole, out var role))
            return role;
        if (LongHorizonSessionRoleCodec.IsEasySupport(sessionRole))
            return PreparationRunwaySlotRole.EasySupport;
        if (LongHorizonSessionRoleCodec.IsKeySession(sessionRole))
            return PreparationRunwaySlotRole.KeySession;
        if (LongHorizonSessionRoleCodec.IsLongRun(sessionRole))
            return PreparationRunwaySlotRole.LongRun;
        throw new LongHorizonAdaptationIntegrityViolationException("Rolling session has an unrecognized session role.");
    }
}
