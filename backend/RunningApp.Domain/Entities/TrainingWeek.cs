using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class TrainingWeek
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int WeekNumber { get; set; }
    public TrainingWeekType WeekType { get; set; } = TrainingWeekType.Build;
    public double PlannedVolumeKm { get; set; }
    public double ActualVolumeKm { get; set; }
    public bool IsRecoveryWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public TrainingPlan Plan { get; set; } = null!;
    public ICollection<TrainingDay> Days { get; set; } = new List<TrainingDay>();

    // ── Backend Integration Phase 3: Process A plan-catalog phase identity ──
    // Nullable/additive. Null for legacy/seeded-template weeks. Does not
    // replace WeekType — Phase 2 found FOUNDATION and RACE_SPECIFIC have no
    // exact TrainingWeekType member (Base/Peak/RaceWeek are unresolved loose
    // candidates), so the catalog's own phase key is preserved verbatim
    // alongside the existing enum rather than forced into it.
    public string? CatalogPhaseKey { get; set; }
}
