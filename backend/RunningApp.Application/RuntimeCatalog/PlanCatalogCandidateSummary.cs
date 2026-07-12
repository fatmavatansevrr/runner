namespace RunningApp.Application.RuntimeCatalog;

/// <summary>An exact (key, version) reference to a Process A plan-catalog document.</summary>
public sealed record PlanCatalogReference(string Key, int Version);

/// <summary>
/// Core-cycle week boundaries as declared by a PLAN_TEMPLATE's own
/// <c>coreCycle</c> object (e.g. <c>templates/ten-k-master.v6.json</c>:
/// <c>{"minimumWeeks":8,"defaultWeeks":12,"maximumWeeks":14}</c>). Read
/// verbatim from the catalog — Backend Integration Phase 4D.1 introduced this
/// so a TIME_ADEQUACY_IN resolver never hardcodes distance-family-specific
/// week thresholds.
/// </summary>
public sealed record PlanCatalogCoreCycle(int MinimumWeeks, int DefaultWeeks, int? MaximumWeeks);

/// <summary>
/// Read-only summary of a Process A plan-catalog TEMPLATE_COMBINATION candidate,
/// resolved from its immediate dependency graph (master template, layout, level
/// modifier, rule pack, and their own direct references). Deliberately does NOT
/// carry full workout-progression stage detail, workout component bodies, or
/// runtime-condition allowed-value lists — this is an identity/reference-level
/// summary for Backend Integration Phase 1, not a full bundle mirror.
/// </summary>
public sealed class PlanCatalogCandidateSummary
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }

    /// <summary>
    /// The TEMPLATE_COMBINATION document's own <c>metadata.status</c> at load time
    /// (e.g. "DRAFT", "VALIDATED", "PUBLISHED"). Source for
    /// TrainingPlan.CatalogCandidateStatusAtGenerationTime (Phase 3) — a snapshot,
    /// not a live link: it is never re-read after generation.
    /// </summary>
    public required string CandidateStatus { get; init; }

    /// <summary>Canonical distance family as declared by the referenced PLAN_TEMPLATE (e.g. "TEN_K").</summary>
    public required string CanonicalDistanceFamily { get; init; }

    /// <summary>Experience/level as declared by the referenced LEVEL_MODIFIER (e.g. "INTERMEDIATE").</summary>
    public required string Level { get; init; }

    public required int DaysPerWeek { get; init; }

    /// <summary>Core-cycle week boundaries declared by the referenced PLAN_TEMPLATE's own <c>coreCycle</c> object.</summary>
    public required PlanCatalogCoreCycle CoreCycle { get; init; }

    public required PlanCatalogReference MasterTemplate { get; init; }
    public required PlanCatalogReference Layout { get; init; }
    public required PlanCatalogReference LevelModifier { get; init; }
    public required PlanCatalogReference WorkoutProgression { get; init; }
    public required PlanCatalogReference ProgressionModifier { get; init; }
    public required PlanCatalogReference RulePack { get; init; }
    public required PlanCatalogReference PeakVolumeBandPolicy { get; init; }
    public required PlanCatalogReference RuntimeConditionValueRegistry { get; init; }

    /// <summary>
    /// Backend Integration Phase 4E.1 — each directly-loaded dependency
    /// document's own <c>metadata.status</c>, keyed by dependency role
    /// ("masterTemplate", "layout", "levelModifier", "rulePack"). Scoped
    /// deliberately to only the four documents this loader already opens
    /// and parses for other fields (see <see cref="PlanCatalogBundleLoader"/>)
    /// — the remaining referenced-but-not-opened dependencies
    /// (WorkoutProgression, ProgressionModifier, PeakVolumeBandPolicy,
    /// RuntimeConditionValueRegistry) are NOT status-checked by this field;
    /// checking them would require additional file loads this phase does
    /// not add. This is the smallest coherent extension needed for Phase
    /// 4E.1's candidate/dependency eligibility gate, not a full bundle
    /// lifecycle audit.
    /// </summary>
    public required IReadOnlyDictionary<string, string> DependencyStatuses { get; init; }

    /// <summary>Exact (key, version) workout references reachable from the level modifier's eligible-workouts list.</summary>
    public required IReadOnlyList<PlanCatalogReference> ReferencedWorkouts { get; init; }

    /// <summary>Phase keys declared by the master template (e.g. FOUNDATION, BUILD, RACE_SPECIFIC, TAPER).</summary>
    public required IReadOnlyList<string> PhaseKeys { get; init; }

    /// <summary>Slot roles declared by the layout (e.g. KEY_SESSION, EASY_SUPPORT, LONG_RUN).</summary>
    public required IReadOnlyList<string> SlotRoles { get; init; }
}
