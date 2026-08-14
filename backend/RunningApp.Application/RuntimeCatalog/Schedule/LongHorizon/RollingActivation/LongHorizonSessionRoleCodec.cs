using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4L.4E -- the single authoritative session-role token authority.
/// Centralizes an existing, already-established convention (canonical
/// uppercase-with-underscore tokens: "KEY_SESSION", "EASY_SUPPORT",
/// "LONG_RUN") that General Endurance and Core code already used everywhere
/// -- this type does not invent a new representation, it consolidates one
/// that was already the de facto standard in every code path except two.
///
/// Root cause this fixes: <c>LongHorizonRollingJitActivationRuntime.MapRunwayWeeks</c>
/// and <c>LongHorizonRealCalendarProjectionAdapter.BuildFullRunwayProjection</c>
/// derived Runway session roles via the raw <c>PreparationRunwaySlotRole.ToString()</c>
/// enum default (PascalCase, e.g. "LongRun") instead of routing through the
/// same canonical-string switch every other Runway/Core/GE code path already
/// used (identical switch expressions already existed, unmodified, in
/// PreparationRunwayCalendarSkeletonAdapter, PreparationRunwayPersistablePlanMapper,
/// LongHorizonFullNumericOrchestrator and LongHorizonStructuralMaterializer --
/// this type is the single place that switch now lives).
/// </summary>
internal static class LongHorizonSessionRoleCodec
{
    internal const string KeySession = "KEY_SESSION";
    internal const string EasySupport = "EASY_SUPPORT";
    internal const string LongRun = "LONG_RUN";

    /// <summary>The one place a PreparationRunwaySlotRole is converted to its
    /// canonical persisted/wire token. Never call SlotRole.ToString() directly
    /// for persistence or public output -- an enum member rename would
    /// silently change persisted/public data if that were relied upon.</summary>
    internal static string ToCanonicalToken(PreparationRunwaySlotRole role) => role switch
    {
        PreparationRunwaySlotRole.KeySession => KeySession,
        PreparationRunwaySlotRole.EasySupport => EasySupport,
        PreparationRunwaySlotRole.LongRun => LongRun,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unrecognized PreparationRunwaySlotRole."),
    };

    /// <summary>
    /// Attempts to recover the semantic role from either a canonical token or
    /// the one known legacy token this defect could have produced. Returns
    /// false (fails closed) for anything else, including GE's suffixed
    /// "EASY_SUPPORT_1"/"EASY_SUPPORT_2" forms, which carry GE-only ordinal
    /// information this enum cannot represent -- callers needing "is this an
    /// easy-support session" should use <see cref="IsEasySupport"/> instead.
    /// </summary>
    internal static bool TryParseCanonicalOrLegacy(string? sessionRole, out PreparationRunwaySlotRole role)
    {
        switch (sessionRole)
        {
            case KeySession or nameof(PreparationRunwaySlotRole.KeySession):
                role = PreparationRunwaySlotRole.KeySession;
                return true;
            case EasySupport or nameof(PreparationRunwaySlotRole.EasySupport):
                role = PreparationRunwaySlotRole.EasySupport;
                return true;
            case LongRun or nameof(PreparationRunwaySlotRole.LongRun):
                role = PreparationRunwaySlotRole.LongRun;
                return true;
            default:
                role = default;
                return false;
        }
    }

    /// <summary>
    /// Approved legacy compatibility set: exactly the PascalCase enum-default
    /// tokens this defect's pre-fix Runway numeric-activation code path could
    /// have produced ("LongRun", "KeySession", "EasySupport"). No other
    /// legacy token is known to exist -- GE and Core were always canonical
    /// (confirmed by direct repository inspection, Phase 4L.4E Part 1).
    /// Case-sensitive on the legacy form deliberately: only the exact enum
    /// default is an approved legacy value, not arbitrary casings.
    /// </summary>
    internal static bool IsLongRun(string? sessionRole) =>
        sessionRole is not null && (sessionRole.Equals(LongRun, StringComparison.OrdinalIgnoreCase)
            || sessionRole.Equals(nameof(PreparationRunwaySlotRole.LongRun), StringComparison.Ordinal));

    internal static bool IsKeySession(string? sessionRole) =>
        sessionRole is not null && (sessionRole.Equals(KeySession, StringComparison.OrdinalIgnoreCase)
            || sessionRole.Equals(nameof(PreparationRunwaySlotRole.KeySession), StringComparison.Ordinal));

    /// <summary>GE persists suffixed forms ("EASY_SUPPORT_1"/"EASY_SUPPORT_2");
    /// Runway/Core persist the bare form. StartsWith covers both without
    /// treating any other role as easy-support.</summary>
    internal static bool IsEasySupport(string? sessionRole) =>
        sessionRole is not null && (sessionRole.StartsWith(EasySupport, StringComparison.OrdinalIgnoreCase)
            || sessionRole.Equals(nameof(PreparationRunwaySlotRole.EasySupport), StringComparison.Ordinal));
}
