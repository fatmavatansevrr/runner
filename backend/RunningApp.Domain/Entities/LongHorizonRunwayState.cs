namespace RunningApp.Domain.Entities;

/// <summary>
/// Phase 4L.2 -- immutable Runway ownership, created exactly once per plan.
/// PrescriptionPayloadJson is a bounded, versioned, immutable snapshot of
/// the per-week Runway prescription values (week number, weekly volume,
/// long run, block type) needed for future bounded-slice reconstruction --
/// Decision A (persist the immutable activated-authority values) rather
/// than Decision B (rerun the materializer after restart), because
/// equivalence across future code/catalog/resolver/rounding changes is not
/// guaranteed and this phase does not attempt to prove it.
/// </summary>
public class LongHorizonRunwayState
{
    public Guid Id { get; set; }
    public Guid PlanStateId { get; set; }
    public Guid TargetLockId { get; set; }
    public int TargetContextVersionSequence { get; set; }
    public int LockedRunwayStartGlobalWeek { get; set; }
    public int LockedRunwayEndGlobalWeek { get; set; }
    public double CoreWeekOneWeeklyTargetKm { get; set; }
    public double CoreWeekOneLongRunTargetKm { get; set; }
    public string FullPrescriptionId { get; set; } = null!;
    public int FullPrescriptionVersion { get; set; }
    public string PrescriptionPayloadJson { get; set; } = null!;
    public string CalendarCompositionIdentity { get; set; } = null!;

    /// <summary>
    /// Phase 4L.2A -- a bounded, versioned, immutable snapshot of the full
    /// LongHorizonLockedRunwayCalendarProjection (per-session dates/roles for
    /// the entire locked Runway), closing the restart-continuation gap Phase
    /// 4L.2 disclosed: LongHorizonRollingJitCompositionRequest.ExistingRunwayCalendarProjection
    /// requires this exact object to avoid recomposing the calendar on a
    /// later Runway continuation slice. Null for plans persisted before this
    /// field existed (none in production -- this subsystem is entirely dark).
    /// </summary>
    public string? CalendarProjectionPayloadJson { get; set; }

    /// <summary>Phase 4L.2A -- a bounded, versioned, immutable snapshot of the real LongHorizonLockedCoreWeekOneTarget, same rationale as CalendarProjectionPayloadJson.</summary>
    public string? TargetLockPayloadJson { get; set; }
    public Guid? CreatedDecisionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
