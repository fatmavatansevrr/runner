using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

/// <summary>
/// Phase 4L.2 -- an executable session belonging to an activated week.
/// Deliberately NOT a TrainingDay row: TrainingDay.PlanId is a required FK
/// to TrainingPlans, and Long-Horizon plans intentionally have no real
/// TrainingPlan row (see LongHorizonRollingPlanState) -- reusing TrainingDay
/// would require either fabricating a TrainingPlan row (forbidden) or a
/// nullable/optional PlanId (a breaking change to a table read by the real
/// Home/Calendar/Details/completion endpoints, risking dark session leakage
/// into live user-facing surfaces). A dedicated table keeps this dark
/// subsystem fully isolated from every live read path.
/// </summary>
public class LongHorizonRollingSessionState
{
    public Guid Id { get; set; }
    public Guid WeekStateId { get; set; }
    public int SessionOrdinal { get; set; }
    public string SessionRole { get; set; } = null!;
    public string? WorkoutKey { get; set; }
    public int? WorkoutVersion { get; set; }
    public double DistanceKm { get; set; }
    public DateOnly AssignedDate { get; set; }
    public int ActivationContextVersionSequence { get; set; }
    public string Provenance { get; set; } = null!;
    public LongHorizonRollingSessionOutcomeStatus OutcomeStatus { get; set; } = LongHorizonRollingSessionOutcomeStatus.Planned;
    public DateTime? CompletedAtUtc { get; set; }
    public double? ActualDistanceKm { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public string? NotTodayReason { get; set; }
    public DateTime? NotTodayRecordedAtUtc { get; set; }
    public int OutcomeVersion { get; set; }

    /// <summary>Phase 4M.2 -- Active unless Appsel adaptation removed this
    /// session from the active plan (Superseded). See
    /// LongHorizonPersistedSessionPlanningStatus's own doc comment for why
    /// this is a separate dimension from <see cref="OutcomeStatus"/>.</summary>
    public LongHorizonPersistedSessionPlanningStatus PlanningStatus { get; set; } = LongHorizonPersistedSessionPlanningStatus.Active;

    /// <summary>Phase 4M.2 -- set only on a replacement session created by
    /// adaptation; points at the immutable original/source session it
    /// satisfies the logical expectation of. Null for every ordinary
    /// (non-replacement) session, including a Superseded one -- a
    /// Superseded session is a source that WAS replaced, not a replacement
    /// itself, so it never has this set on itself.</summary>
    public Guid? AdaptedFromSessionId { get; set; }

    public LongHorizonRollingWeekState Week { get; set; } = null!;
}
