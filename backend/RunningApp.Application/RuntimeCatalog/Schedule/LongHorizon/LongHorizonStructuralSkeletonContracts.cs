namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.5 — the three segments joined into one contiguous 21-52 week
/// structural skeleton, in this exact required order (no interleaving).
/// </summary>
internal enum LongHorizonSegmentType
{
    LongHorizonGeneralEndurance,
    PreparationRunway,
    Core,
}

/// <summary>
/// One ordered structural running-session slot within a
/// <see cref="LongHorizonStructuralWeek"/>. Never a dated, prescribed
/// session -- no distance, duration, pace, intensity, or weekday. Core
/// segment slots deliberately carry a null <see cref="WorkoutKey"/>/
/// <see cref="WorkoutVersion"/> (see this phase's own "Explicit
/// non-implementation statement": the existing Core structural skeleton
/// materializer, <c>CatalogStageToWeekMaterializer</c>, never selects a
/// workout reference itself -- that is a separate, later existing pipeline
/// stage (<c>CatalogWorkoutBinder</c>) this phase does not invoke, to avoid
/// inventing content that stage does not itself produce).
/// </summary>
internal sealed record LongHorizonStructuralWorkoutSlot(
    int StructuralSlotIndex,
    string StructuralRole,
    string? WorkoutKey,
    int? WorkoutVersion,
    LongHorizonSegmentType Segment);

/// <summary>
/// One globally-numbered week of the joined 21-52 week structural skeleton.
/// Carries exactly one segment's provenance; every field belonging to the
/// other two segments is null (Phase 4I.5 Part 8).
/// </summary>
internal sealed record LongHorizonStructuralWeek(
    int GlobalWeekNumber,
    int LocalSegmentWeekNumber,
    LongHorizonSegmentType Segment,
    string WeekType,
    string? RunwayBlock,
    string? CorePhase,
    GeneralEnduranceDurationClassification? GeClassification,
    LongHorizonGeStageFamily? GeStageFamily,
    int? MesocycleIndex,
    LongHorizonGeMesocyclePosition? MesocyclePosition,
    bool? IsRecoveryWeek,
    bool? IsTerminalAlignment,
    IReadOnlyList<LongHorizonStructuralWorkoutSlot> OrderedWorkoutSlots);

/// <summary>
/// Phase 4I.5 — the single, dark, unwired structural skeleton contract for a
/// complete 21-52 week TEN_K__4D__INTERMEDIATE Long-Horizon plan: ordered
/// Long-Horizon GE, Preparation Runway, and Core weeks under one contiguous
/// global numbering, with authoritative segment provenance. Contains no
/// final distance, duration, pace, calendar date, persisted ID, or
/// completion state.
/// </summary>
internal sealed record LongHorizonGeneratedStructuralSkeleton(
    int TotalWeeks,
    int GeneralEnduranceWeeks,
    int PreparationRunwayWeeks,
    int CoreWeeks,
    ReadinessProfile ReadinessProfile,
    string CandidateKey,
    int CandidateVersion,
    string CompositionPolicyId,
    string CompositionPolicyVersion,
    string MaterializerId,
    string MaterializerVersion,
    IReadOnlyList<LongHorizonStructuralWeek> Weeks);
