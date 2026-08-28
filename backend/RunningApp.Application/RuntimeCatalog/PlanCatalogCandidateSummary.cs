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
/// Backend Integration Phase 4G.3B.1 — one phase's complete declared week
/// constraints and allocation priorities, read verbatim from a PLAN_TEMPLATE's <c>phases[]</c> array
/// (e.g. <c>templates/ten-k-master.v6.json</c>: <c>{"phaseKey":"BUILD",
/// "minimumWeeks":3,"preferredWeeks":4,"maximumWeeks":5,...}</c>).
/// <see cref="PhaseKey"/> is the catalog's
/// week-allocation granularity — distinct from the catalog's finer,
/// workout-selection-level <c>stageKey</c> (nested inside a phase's own
/// <c>workoutProgression</c> entry), which carries no week-count of its own
/// and is not represented here.
/// </summary>
public sealed record PlanCatalogPhaseAllocation(
    string PhaseKey,
    int MinimumWeeks,
    int PreferredWeeks,
    int MaximumWeeks,
    int CompressionPriority,
    int ExtensionPriority,
    bool IsCompressionProtected)
{
    /// <summary>
    /// Compatibility constructor for existing hand-built test candidates. Catalog loading never
    /// uses this constructor and never defaults missing artifact fields.
    /// </summary>
    public PlanCatalogPhaseAllocation(string phaseKey, int preferredWeeks)
        : this(phaseKey, preferredWeeks, preferredWeeks, preferredWeeks, 1, 1, false)
    {
    }
}

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

    /// <summary>
    /// Backend Integration Phase 4G.3B.1 — each phase's complete declared
    /// constraint metadata, in the same order as <see cref="PhaseKeys"/>
    /// (both are populated from the same pass over the same <c>phases[]</c>
    /// array, so they can never drift relative to each other). Additive:
    /// <see cref="PhaseKeys"/> is kept unchanged for backward compatibility
    /// with every existing consumer/test that only needs the bare key list.
    /// </summary>
    public required IReadOnlyList<PlanCatalogPhaseAllocation> PhaseAllocations { get; init; }

    /// <summary>Slot roles declared by the layout (e.g. KEY_SESSION, EASY_SUPPORT, LONG_RUN). For a repeating-pattern layout (<see cref="WeeklyPatternRoles"/> non-null), this is Pattern[0] (Pattern A) — kept populated so every pre-GEN.12 consumer that only reads this field keeps working unchanged.</summary>
    public required IReadOnlyList<string> SlotRoles { get; init; }

    /// <summary>
    /// Phase 10K-GEN.12 — the layout's optional repeating multi-week
    /// structural pattern (e.g. RUN_LAYOUT_2D's Model B: Pattern A =
    /// [KEY_SESSION, LONG_RUN], Pattern B = [EASY_SUPPORT, LONG_RUN]).
    /// <see langword="null"/> for every layout that declares only a single
    /// fixed weekly <c>slots[]</c> array (every 3D-7D layout as of this
    /// phase) — additive and optional by construction, so no existing
    /// consumer needs to change. When non-null, each entry's role count
    /// must equal <see cref="DaysPerWeek"/>, and <see cref="PatternPeriodWeeks"/>
    /// is also non-null and equal to this list's count.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>>? WeeklyPatternRoles { get; init; }

    /// <summary>Phase 10K-GEN.12 — the repeating period, in weeks, of <see cref="WeeklyPatternRoles"/>. Null iff <see cref="WeeklyPatternRoles"/> is null.</summary>
    public int? PatternPeriodWeeks { get; init; }
}
