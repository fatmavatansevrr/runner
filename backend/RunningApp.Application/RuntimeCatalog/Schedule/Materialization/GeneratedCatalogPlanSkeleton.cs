namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.2 — the intermediate, structural output of
/// <see cref="ICatalogStageToWeekMaterializer"/>: plan-relative numbered
/// weeks, stage (phase) assignment per week, and structural session-slot
/// skeletons. Deliberately NOT the final Phase 4F.1
/// <see cref="Schedule.GeneratedCatalogPlanPayload"/> — this type carries no
/// workout prescriptions, target distances/durations, pace, segments, dates
/// for individual sessions, or planned volume. It is never stored in
/// <see cref="PreviewRouting.CatalogPreviewSnapshot.GeneratedPreviewPlanPayload"/>,
/// which remains reserved exclusively for the complete final contract.
///
/// Schema-version policy (Phase 4F.2 Decision 10): this type has its OWN
/// <see cref="CurrentSchemaVersion"/>, deliberately independent of
/// <see cref="GeneratedCatalogPlanPayload.CurrentSchemaVersion"/>. Rationale:
/// the skeleton is an internal-only, never-persisted, never-hashed
/// intermediate artifact whose own shape may evolve independently of (and
/// faster than) the final persistable contract's shape — e.g. adding a new
/// structural role or provenance field to the skeleton has no bearing on
/// whether a stored, already-confirmed final payload remains readable.
/// Coupling the two version numbers would force every skeleton-shape change
/// to also be treated as a final-contract compatibility event, which is not
/// true.
/// </summary>
public sealed class GeneratedCatalogPlanSkeleton
{
    public const int CurrentSchemaVersion = 1;

    public required int SchemaVersion { get; init; }

    /// <summary>Preserved exactly from the materialization context — never normalized (Phase 4F.1 Decision 5, reapplied here).</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>Must equal <c>StartDate + (PlannedWeekCount * 7 days) - 1 day</c> — the last day of the final plan-relative week.</summary>
    public required DateOnly EndDate { get; init; }

    public required int PlannedWeekCount { get; init; }

    /// <summary>Expected structural session-slot count per week — drives the generic (non-hardcoded) slot-count check.</summary>
    public required int DaysPerWeek { get; init; }

    public required string CanonicalDistanceFamily { get; init; }

    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }

    public required IReadOnlyDictionary<string, PlanCatalogReference> DependencyVersions { get; init; }

    public required IReadOnlyList<GeneratedCatalogWeekSkeleton> Weeks { get; init; }

    public required GeneratedCatalogPlanSkeletonProvenance Provenance { get; init; }
}

/// <summary>Phase 4F.2 plan-level provenance — internal only, never exposed on a public DTO.</summary>
public sealed class GeneratedCatalogPlanSkeletonProvenance
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required IReadOnlyDictionary<string, PlanCatalogReference> DependencyVersions { get; init; }

    /// <summary>The single frozen domain evaluation date supplied by the caller — the materializer never reads the wall clock itself.</summary>
    public required DateOnly AsOfDate { get; init; }

    /// <summary>Identifies which materializer implementation/version produced this skeleton (Phase 4F.2 Decision 10), e.g. <see cref="CatalogStageToWeekMaterializerVersion.V1"/>.</summary>
    public required string MaterializerVersion { get; init; }
}

/// <summary>
/// One plan-relative, seven-day week, assigned to exactly one selected
/// stage (Phase 4F.2 Decision 2/6 — "stage" here means the catalog's
/// week-allocation-granularity <c>phaseKey</c>, e.g. "FOUNDATION"/"BUILD"/
/// "RACE_SPECIFIC"/"TAPER" — see the phase's own governance-decision
/// documentation for why this is the correct granularity).
/// </summary>
public sealed class GeneratedCatalogWeekSkeleton
{
    /// <summary>1-based, consecutive from 1, no duplicates, no gaps across the plan.</summary>
    public required int WeekNumber { get; init; }

    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

    /// <summary>The stage (phase) this week belongs to, e.g. "BUILD" — matches <see cref="GeneratedCatalogWeekPayload.StageKey"/>'s own established granularity from Phase 4F.1.</summary>
    public required string StageKey { get; init; }

    /// <summary>1-based index of this week within its stage's own week block (e.g. "Build week 2 of 4" → 2).</summary>
    public required int StageWeekIndex { get; init; }

    /// <summary>Total weeks allocated to this week's stage (e.g. "Build week 2 of 4" → 4). Identical for every week sharing the same <see cref="StageKey"/> occurrence.</summary>
    public required int StageWeekCount { get; init; }

    /// <summary>Exactly <see cref="GeneratedCatalogPlanSkeleton.DaysPerWeek"/> structural running-session slots — never a rest-day placeholder, never optional/recovery slots (Phase 4F.2 Decision 5).</summary>
    public required IReadOnlyList<GeneratedCatalogSessionSlotSkeleton> SessionSlots { get; init; }

    public required GeneratedCatalogWeekSkeletonProvenance Provenance { get; init; }
}

/// <summary>Phase 4F.2 week-level provenance — internal only.</summary>
public sealed class GeneratedCatalogWeekSkeletonProvenance
{
    public required string StageKey { get; init; }

    /// <summary>Present for forward compatibility with a future phase where the week-allocation stage and the "source phase" concept might diverge; always equal to <see cref="StageKey"/> as of Phase 4F.2, since week allocation IS phase-granularity in this phase.</summary>
    public required string SourcePhaseKey { get; init; }
}

/// <summary>
/// A single structural running-session slot — an ordered position and a
/// structural role (e.g. "KEY_SESSION"), never a dated, prescribed session
/// (Phase 4F.2 Decisions 4, 6, 7, 8). No weekday/date, no workout type
/// beyond the catalog's own immutable structural role, no distance,
/// duration, pace, intensity, or segments.
/// </summary>
public sealed class GeneratedCatalogSessionSlotSkeleton
{
    /// <summary>1-based, consecutive from 1, unique within the owning week. NOT a day-of-week index.</summary>
    public required int SlotOrderInWeek { get; init; }

    /// <summary>
    /// A deterministic, synthesized identity for this slot, since the source
    /// run-layout catalog document (e.g. <c>run-layout-4d.v2.json</c>) only
    /// declares a <c>role</c> per slot position, not a distinct per-slot key
    /// (two slots may share the same role, e.g. two "EASY_SUPPORT" entries).
    /// Format: <c>{StructuralRole}_{occurrenceIndexWithinRole}</c>, 1-based
    /// per-role counter within the week (e.g. "EASY_SUPPORT_1",
    /// "EASY_SUPPORT_2"). Documented explicitly as a Phase 4F.2 naming
    /// convention, not a value read from any catalog file.
    /// </summary>
    public required string LayoutSlotKey { get; init; }

    /// <summary>The catalog's own immutable structural role string, preserved verbatim (e.g. "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN") — never a final workout-type prescription such as "THRESHOLD"/"VO2_INTERVAL"/"GOAL_PACE".</summary>
    public required string StructuralRole { get; init; }

    public required GeneratedCatalogSessionSlotSkeletonProvenance Provenance { get; init; }
}

/// <summary>Phase 4F.2 slot-level provenance — internal only. Carries the full sourcing detail (including the run-layout's exact version) that the slot's own top-level fields deliberately keep terse.</summary>
public sealed class GeneratedCatalogSessionSlotSkeletonProvenance
{
    /// <summary>The stage (phase) key this session slot's owning week belongs to.</summary>
    public required string SourceStageKey { get; init; }

    /// <summary>Exact (key, version) identity of the run-layout document this slot's structural role was drawn from.</summary>
    public required PlanCatalogReference SourceLayout { get; init; }
}
