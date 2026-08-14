using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

/// <summary>
/// Phase 4L.2 -- the Long-Horizon rolling plan aggregate root. Deliberately
/// NOT a row in TrainingPlans: no GoalType.LongHorizon/GoalDistance.LongHorizon
/// enum value exists yet (confirmed absent, Phase 4L.1), so there is no
/// honest way to create a real TrainingPlan row for a Long-Horizon plan
/// without fabricating a GoalType/Status meaning that doesn't exist. This
/// entity's own Id is the internal plan-ownership anchor for all child rows.
/// No row here is ever surfaced through any public contract.
/// </summary>
public class LongHorizonRollingPlanState
{
    public Guid Id { get; set; }
    public int TotalWeeks { get; set; }
    public string ReadinessProfile { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly RaceDate { get; set; }
    public string GoalType { get; set; } = null!;
    public string GoalDistance { get; set; } = null!;
    public string Level { get; set; } = null!;
    public int DaysPerWeek { get; set; }
    public string PreferredDaysCsv { get; set; } = null!;
    public string LongRunDay { get; set; } = null!;
    public string CandidateKey { get; set; } = null!;
    public int CandidateVersion { get; set; }
    public string CatalogRootPath { get; set; } = null!;
    public LongHorizonPersistedLifecycleState CurrentLifecycleStatus { get; set; }
    public int CurrentWindowStartWeek { get; set; }
    public int CurrentWindowEndWeek { get; set; }
    public int? LastActivatedGlobalWeek { get; set; }
    public DateOnly? LatestCheckpointDate { get; set; }
    public int ActiveContextVersionSequence { get; set; }
    public Guid ActiveContextVersionId { get; set; }
    public string? CurrentBlockedPublicReasonCategory { get; set; }
    public string? CurrentBlockedInternalReasonCode { get; set; }
    public DateOnly? BlockedAt { get; set; }
    public bool RetryEligible { get; set; }
    public int PersistenceContractVersion { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<LongHorizonRollingWeekState> Weeks { get; set; } = new List<LongHorizonRollingWeekState>();
}
