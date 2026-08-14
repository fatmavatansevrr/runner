namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.1 -- pure canonical authority for NotToday reason
/// classification (Rev3.1 §4). Deliberately does not diagnose, infer
/// severity, estimate recovery duration, or prescribe a return-to-running
/// protocol -- it only answers three narrow questions.
/// </summary>
internal static class ReasonClassificationPolicy
{
    /// <summary>Analytics/UI classification only. pain_or_discomfort is the
    /// sole Safety reason; every other reason (illness included) is
    /// Operational -- see <see cref="TriggersSafetyFlag"/> for why illness
    /// still blocks repair despite being classified Operational.</summary>
    public static ReasonClass Classify(NotTodayReasonCode reason) =>
        reason == NotTodayReasonCode.PainOrDiscomfort ? ReasonClass.Safety : ReasonClass.Operational;

    /// <summary>Schedule-repair-level behavior: both pain_or_discomfort and
    /// illness block any reschedule/substitution attempt, even though only
    /// pain_or_discomfort is classified ReasonClass.Safety. This is the
    /// Rev3.1 §4 "illness behaves like pain for ScheduleRepair purposes,
    /// without being flagged for safety review" distinction.</summary>
    public static bool BlocksReschedule(NotTodayReasonCode reason) =>
        reason is NotTodayReasonCode.PainOrDiscomfort or NotTodayReasonCode.Illness;

    /// <summary>Only pain_or_discomfort raises the window-level
    /// SafetyReviewRequired signal (Rev3.1 §4/§7).</summary>
    public static bool TriggersSafetyFlag(NotTodayReasonCode reason) =>
        reason == NotTodayReasonCode.PainOrDiscomfort;
}
