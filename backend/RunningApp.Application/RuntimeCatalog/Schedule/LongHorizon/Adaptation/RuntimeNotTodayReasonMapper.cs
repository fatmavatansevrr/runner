namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.3 -- Rev3.1 §4.1 Runtime Reason Vocabulary Mapping. The live
/// rolling NotToday endpoint's runtime string vocabulary
/// (<c>{ "fatigue", "soreness", "illness", "schedule", "weather", "other" }</c>,
/// see <see cref="Services.LongHorizonRollingSessionMutationService"/>) is a
/// separate, real-world vocabulary from the pure Phase 4M.1
/// <see cref="NotTodayReasonCode"/> enum. This type is the sole, explicit
/// boundary between them.
///
/// Deliberately two-step (string -> <see cref="AdaptationReasonMeaning"/> ->
/// <see cref="NotTodayReasonCode"/>) rather than a single runtime-string ->
/// NotTodayReasonCode switch: "soreness" is never written next to
/// "PainOrDiscomfort" in the same case arm. <see cref="Map"/> only ever
/// produces a canonical *meaning* (what Rev3.1 §4.1 says the token means for
/// adaptation purposes); <see cref="ToReasonCode"/> is the separate,
/// independently-testable step that resolves a meaning to the concrete 4M.1
/// vocabulary member <see cref="ScheduleRepairPolicy"/>'s call graph requires.
/// This does not diagnose, infer severity, estimate recovery, or prescribe
/// anything -- it only carries Rev3.1's already-frozen classification.
/// </summary>
internal enum AdaptationReasonMeaning
{
    ScheduleConflict,
    Weather,
    Tired,
    Illness,
    /// <summary>Rev3.1 §4.1 "soreness" meaning: a Safety-classified adaptation
    /// signal, structurally distinct from Illness even though both currently
    /// block reschedule/substitution (see <see cref="ReasonClassificationPolicy.BlocksReschedule"/>).</summary>
    Safety,
    Other,
}

/// <summary>Thrown when a runtime NotToday reason string has no Rev3.1 §4.1
/// mapping. Not expected to be reachable via the live endpoint today, since
/// <see cref="Services.LongHorizonRollingSessionMutationService"/> already
/// validates the runtime string against its own closed allow-list before any
/// adaptation orchestration runs -- this is a defensive fail-fast boundary,
/// not a new client-facing validation rule.</summary>
internal sealed class RuntimeNotTodayReasonUnmappedException(string runtimeReason)
    : Exception($"Runtime not-today reason '{runtimeReason}' has no Rev3.1 §4.1 adaptation mapping.");

internal static class RuntimeNotTodayReasonMapper
{
    /// <summary>Rev3.1 §4.1 canonical mapping table. "schedule" is the live
    /// endpoint's runtime token for what 4M.1 calls ScheduleConflict; "fatigue"
    /// maps to Tired; every other runtime token matches its meaning by name.</summary>
    public static AdaptationReasonMeaning Map(string runtimeReason) => runtimeReason.Trim().ToLowerInvariant() switch
    {
        "schedule" => AdaptationReasonMeaning.ScheduleConflict,
        "weather" => AdaptationReasonMeaning.Weather,
        "fatigue" => AdaptationReasonMeaning.Tired,
        "illness" => AdaptationReasonMeaning.Illness,
        "soreness" => AdaptationReasonMeaning.Safety,
        "other" => AdaptationReasonMeaning.Other,
        _ => throw new RuntimeNotTodayReasonUnmappedException(runtimeReason),
    };

    /// <summary>Resolves a canonical meaning to the concrete 4M.1 vocabulary
    /// member. Kept as its own function (never inlined into <see cref="Map"/>)
    /// so "soreness" and "PainOrDiscomfort" are never adjacent literals in one
    /// switch arm.</summary>
    public static NotTodayReasonCode ToReasonCode(AdaptationReasonMeaning meaning) => meaning switch
    {
        AdaptationReasonMeaning.ScheduleConflict => NotTodayReasonCode.ScheduleConflict,
        AdaptationReasonMeaning.Weather => NotTodayReasonCode.Weather,
        AdaptationReasonMeaning.Tired => NotTodayReasonCode.Tired,
        AdaptationReasonMeaning.Illness => NotTodayReasonCode.Illness,
        AdaptationReasonMeaning.Safety => NotTodayReasonCode.PainOrDiscomfort,
        AdaptationReasonMeaning.Other => NotTodayReasonCode.Other,
        _ => throw new ArgumentOutOfRangeException(nameof(meaning), meaning, "Unrecognized AdaptationReasonMeaning."),
    };
}
