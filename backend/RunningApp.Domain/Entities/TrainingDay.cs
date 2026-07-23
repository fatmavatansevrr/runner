using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class TrainingDay
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public Guid WeekId { get; set; }
    public DateTime Date { get; set; }
    public TrainingDayType DayType { get; set; }
    public TrainingDayStatus Status { get; set; } = TrainingDayStatus.Planned;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Planned fields
    public double PlannedDistanceKm { get; set; }
    public int PlannedDurationMin { get; set; }
    public double? PlannedPaceMinKm { get; set; }
    public string? Intensity { get; set; } // e.g. "z2"

    // Actual fields — intentional denormalization for fast calendar/stats reads.
    // WorkoutLog stores the append-only history; TrainingDay stores the latest result.
    public double? ActualDistanceKm { get; set; }
    public int? ActualDurationMin { get; set; }
    public double? ActualPaceMinKm { get; set; }

    public bool IsLongRun { get; set; }
    public DateTime? OriginalDate { get; set; }
    public TrainingDayType? OriginalType { get; set; }
    public bool CanMarkComplete { get; set; } = true;
    public bool CanMarkNotToday { get; set; } = true;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Adaptation metadata — supports future adaptive engine debugging.
    public TrainingDaySource? Source { get; set; }
    public Guid? AdaptedFromId { get; set; }
    public int? PlanVersion { get; set; }

    // Navigation
    public TrainingPlan Plan { get; set; } = null!;
    public TrainingWeek Week { get; set; } = null!;
    public TrainingDay? AdaptedFrom { get; set; }

    // ── Backend Integration Phase 3: Process A plan-catalog workout/stage/slot identity ──
    // Nullable/additive. Null for legacy/seeded-template days. Does not
    // replace DayType — Phase 2 found KEY_SESSION/EASY_SUPPORT have no
    // TrainingDayType/slot-role equivalent at all, and even where a loose
    // family match exists (e.g. GOAL_PACE_TEN_K ~ Tempo/Interval), DayType
    // alone cannot preserve which exact catalog workout produced the day.

    /// <summary>Exact catalog WORKOUT_DEFINITION key (e.g. "GOAL_PACE_TEN_K"), preserved verbatim.</summary>
    public string? CatalogWorkoutKey { get; set; }
    public int? CatalogWorkoutVersion { get; set; }

    /// <summary>
    /// LEGACY_READ_ONLY_NO_NEW_WRITES: exact workout-progression stage key
    /// (e.g. "GOAL_PACE_REHEARSAL") written only by the legacy SQL-generation
    /// confirmation path. New catalog-sourced rows (via
    /// <c>CatalogPlanConfirmationService.BuildCatalogTrainingDay</c>) do NOT
    /// write this field — they rely solely on <see cref="CatalogPhaseKey"/>
    /// and <see cref="CatalogProgressionStageKey"/>, which are the fields
    /// actually read by catalog-path consumers (e.g.
    /// <c>CatalogPersistedPlanValidator</c>). No active reader anywhere in
    /// the codebase depends on this field being populated for catalog-sourced
    /// rows; it is retained read-only for legacy SQL-path rows only and is a
    /// candidate for future removal once the legacy SQL path is retired.
    /// </summary>
    public string? CatalogStageKey { get; set; }
    public string? CatalogPhaseKey { get; set; }
    public string? CatalogProgressionStageKey { get; set; }
    public string? CatalogWorkoutDefinitionKey { get; set; }
    public int? CatalogWorkoutDefinitionVersion { get; set; }
    public string? CatalogStructuralRole { get; set; }
    public string? CatalogPrescriptionJson { get; set; }
    public int? CatalogPrescriptionSchemaVersion { get; set; }
    public string? GenerationSource { get; set; }

    /// <summary>Layout slot role (e.g. "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN") — distinct from DayType.</summary>
    public string? CatalogSlotRole { get; set; }

    /// <summary>Catalog WorkoutFamily (e.g. "QUALITY", "EASY", "LONG_RUN", "RACE").</summary>
    public string? CatalogWorkoutFamily { get; set; }

    /// <summary>Catalog component intensity descriptor (e.g. "GOAL_PACE", "EASY"), distinct from the existing free-text Intensity field's zone labels (e.g. "z2").</summary>
    public string? CatalogIntensityKey { get; set; }
}
