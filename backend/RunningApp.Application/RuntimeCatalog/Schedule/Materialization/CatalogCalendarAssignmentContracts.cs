namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.5 — identifies which calendar-day
/// materializer implementation/version produced a
/// <see cref="DatedGeneratedCatalogPlanSkeleton"/>, mirroring the Phase 4F.2
/// <see cref="CatalogStageToWeekMaterializerVersion"/> convention. Deliberately
/// independent of that version and of the Phase 4F.1
/// <see cref="Schedule.GeneratedCatalogPlanPayload.CurrentSchemaVersion"/> —
/// this is its own internal-only, never-persisted, never-hashed intermediate
/// artifact whose shape may evolve independently.
/// </summary>
internal static class CatalogCalendarDayMaterializerVersion
{
    public const string V1 = "CATALOG_CALENDAR_DAY_MATERIALIZER_V1";
}

/// <summary>
/// Backend Integration Phase 4F.5 — race-plan calendar-assignment policy
/// version marker (plan level provenance only; no behavioral branching exists
/// yet since only one policy is implemented). Distinguishes the (future)
/// habit-plan policy, which is explicitly out of scope for this phase.
/// </summary>
internal enum CatalogCalendarAssignmentPolicy
{
    /// <summary>PreferredDays and LongRunDayPreference are both required hard constraints (this phase's only implemented policy).</summary>
    RaceHardConstraint = 1,
}

/// <summary>
/// Backend Integration Phase 4F.5 — the immutable input to
/// <see cref="ICatalogWeekSkeletonCalendarMaterializer"/>. Every field must
/// already be authoritative when supplied: this type carries no default,
/// re-derives nothing from the database, wall clock, or HTTP context, and
/// the materializer never mutates <see cref="PlanSkeleton"/>.
/// </summary>
internal sealed record CatalogCalendarAssignmentContext(
    DateOnly StartDate,
    Domain.Enums.GoalType GoalKind,
    IReadOnlyList<DayOfWeek> PreferredDays,
    DayOfWeek? LongRunDayPreference,
    GeneratedCatalogPlanSkeleton PlanSkeleton,
    CatalogCalendarAssignmentPolicy Policy,
    CatalogCalendarMaterializationProvenance Provenance);

/// <summary>Plan-level provenance carried through to <see cref="DatedGeneratedCatalogPlanSkeleton.Provenance"/> — internal only, never exposed publicly.</summary>
internal sealed record CatalogCalendarMaterializationProvenance(
    string CandidateKey,
    int CandidateVersion,
    DateOnly AsOfDate,
    DateOnly StartDate,
    IReadOnlyList<DayOfWeek> PreferredDays,
    DayOfWeek LongRunDayPreference,
    string CalendarMaterializerVersion,
    int SourceSkeletonSchemaVersion,
    IReadOnlyDictionary<string, PlanCatalogReference> DependencyVersions);

/// <summary>Week-level provenance — internal only.</summary>
internal sealed record CatalogWeekCalendarProvenance(
    int WeekNumber,
    string SourcePhaseKey,
    int SourceSkeletonWeekNumber,
    CatalogCalendarAssignmentPolicy AssignmentPolicy);

/// <summary>Session-level provenance — internal only.</summary>
internal sealed record CatalogSessionCalendarProvenance(
    string SourceLayoutSlotKey,
    string StructuralRole,
    DayOfWeek SelectedWeekday,
    DateOnly AssignedDate,
    string AssignmentRule);

/// <summary>
/// Backend Integration Phase 4F.5 — the complete, internal-only, dated
/// counterpart to the Phase 4F.2 <see cref="GeneratedCatalogPlanSkeleton"/>.
/// Preserves every structural fact of the source skeleton (week boundaries,
/// phase assignment, phase-relative indexing, layout slot identity,
/// structural role, candidate/dependency provenance) while adding exactly
/// one new fact per session slot: its bound calendar date. Carries no
/// workout prescription, distance, duration, pace, intensity, segment, or
/// weekly-volume field — those remain out of scope for this phase. Never
/// stored in <see cref="PreviewRouting.CatalogPreviewSnapshot.GeneratedPreviewPlanPayload"/>,
/// which remains reserved exclusively for the Phase 4F.1 final contract.
/// </summary>
internal sealed record DatedGeneratedCatalogPlanSkeleton(
    string SchemaVersion,
    DateOnly StartDate,
    DateOnly EndDate,
    int PlannedWeekCount,
    IReadOnlyList<DatedGeneratedCatalogWeekSkeleton> Weeks,
    CatalogCalendarMaterializationProvenance Provenance)
{
    public const string CurrentSchemaVersion = "1";
}

/// <summary>One plan-relative, seven-day week with every structural session slot bound to an exact calendar date.</summary>
internal sealed record DatedGeneratedCatalogWeekSkeleton(
    int WeekNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    string PhaseKey,
    int PhaseWeekIndex,
    int PhaseWeekCount,
    IReadOnlyList<DatedGeneratedCatalogSessionSlotSkeleton> SessionSlots,
    CatalogWeekCalendarProvenance Provenance);

/// <summary>
/// A single structural running-session slot, now bound to an exact calendar
/// date — still no final workout-type prescription, distance, duration,
/// pace, intensity, or segment beyond the catalog's own immutable structural
/// role string (preserved verbatim from the source skeleton).
/// </summary>
internal sealed record DatedGeneratedCatalogSessionSlotSkeleton(
    int SlotOrderInWeek,
    string LayoutSlotKey,
    string StructuralRole,
    DateOnly SessionDate,
    DayOfWeek SessionDayOfWeek,
    CatalogSessionCalendarProvenance Provenance);
