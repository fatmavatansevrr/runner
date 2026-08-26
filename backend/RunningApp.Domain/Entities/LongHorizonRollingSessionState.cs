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

    /// <summary>Phase 10K-FREQ.6D.13 -- catalog-authored KEY_SESSION lane identity (0=primary, 1=secondary-controlled), carried verbatim from <c>BoundCatalogSession.LaneOrdinal</c>/<c>CatalogPrescribedSession.LaneOrdinal</c>. Null for FixedDefault roles (EASY_SUPPORT/LONG_RUN) and for every historical row predating this column -- never backfilled, never recomputed from date/repair (FREQ.6D.11 §13-14/§22).</summary>
    public int? LaneOrdinal { get; set; }

    /// <summary>Phase 10K-FREQ.6D.13 -- week-wide slot ordinal, populated for every role, disambiguating repeated same-role slots (e.g. multiple EASY_SUPPORT) where LaneOrdinal is null. Carried verbatim, never recomputed (FREQ.6D.11 §15/§40).</summary>
    public int? SlotOrdinal { get; set; }

    /// <summary>Phase 10K-FREQ.6D.13 -- fine-grained progression stage this session was bound from. Null for FixedDefault roles and for every historical row. Never inferred later from date or catalog-latest (FREQ.6D.11 §16).</summary>
    public string? ProgressionStageKey { get; set; }

    /// <summary>Phase 10K-FREQ.6D.13 -- exact prescription-profile reference, both-null (Legacy) or both-present (ProfileBacked) with <see cref="CatalogPrescriptionProfileVersion"/>, mirroring TrainingDay's own established invariant (FREQ.6D.11 §17/§38).</summary>
    public string? CatalogPrescriptionProfileKey { get; set; }

    public int? CatalogPrescriptionProfileVersion { get; set; }

    public LongHorizonRollingWeekState Week { get; set; } = null!;
}
