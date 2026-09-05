namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.5 — backend-internal structural mirror of the five approved GE
/// stage families (Phase 4I.2 Part 2 / Phase 4I.4 Part 3). Deliberately
/// distinct from Core's Foundation/Build vocabulary and from the
/// Preparation Runway's own block vocabulary.
/// </summary>
internal enum LongHorizonGeStageFamily
{
    Entry,
    BaseDevelopment,
    AerobicDurability,
    Consolidation,
    PreRunwayAlignment,
}

/// <summary>Position of a week within a 4-week FullPhase mesocycle. <see cref="NotApplicable"/> for ShortExtension/terminal-remainder weeks, which use <see cref="LongHorizonGeShortExtensionRole"/> instead.</summary>
internal enum LongHorizonGeMesocyclePosition
{
    NotApplicable,
    Development1,
    Development2,
    Development3,
    RecoveryConsolidation,
}

/// <summary>Role of a ShortExtension (1-3 GE weeks) or terminal-remainder (1-3 leftover weeks) week. <see cref="NotApplicable"/> for FullPhase mesocycle weeks, which use <see cref="LongHorizonGeMesocyclePosition"/> instead.</summary>
internal enum LongHorizonGeShortExtensionRole
{
    NotApplicable,
    EntryAlignment,
    ControlledDevelopment,
    PreRunwayAlignment,
}

/// <summary>A single catalog workout reference -- key + version + family only, never a materialized numeric prescription.</summary>
internal sealed record LongHorizonGeWorkoutReference(string Key, int Version, string Family);

/// <summary>
/// One fully-resolved GE week descriptor: which stage family, which
/// structural position, which workout reference per role, and full
/// provenance -- everything the Phase 4I.5 structural materializer needs,
/// with zero numeric volume/pace/date content. This is a backend-internal
/// structural mirror of plan-catalog's <c>GeWeekDescriptor</c>
/// (<c>PlanCatalog.Core.LongHorizon.LongHorizonGeCatalogContracts</c>) --
/// backend has no project reference onto <c>PlanCatalog.*</c> assemblies
/// (see <c>TD-BACKEND-001</c>), matching the same independent-mirror
/// precedent already established for <see cref="ReadinessProfile"/> itself
/// (backend's own mirror of plan-catalog's <c>LongHorizonReadinessProfile</c>)
/// and for <c>CatalogWorkoutProgressionDefinition</c>'s doc-commented mirror
/// of <c>StageCompressionBehavior</c>.
///
/// Phase 10K-FREQ.6D.14 -- <see cref="EasySupportWorkouts"/> replaces the
/// prior fixed <c>EasySupportA</c>/<c>EasySupportB</c> two-member enum keying:
/// GE's role cardinality is now derived from this list's own length (2 for
/// every existing 4D caller, 3 for the FREQ.6D.12-approved 5D shape) rather
/// than a hardcoded <c>DaysPerWeek==5</c> branch. GE never carries more than
/// one KEY_SESSION, so no lane-ordinal ambiguity exists here the way it does
/// for Core -- only the EASY cardinality varies.
/// </summary>
internal sealed record LongHorizonGeWeekDescriptor(
    int WeekIndex,
    GeneralEnduranceDurationClassification Classification,
    int? MesocycleIndex,
    LongHorizonGeMesocyclePosition MesocyclePosition,
    LongHorizonGeShortExtensionRole ShortExtensionRole,
    LongHorizonGeStageFamily StageFamily,
    bool IsRecoveryWeek,
    bool IsTerminalAlignment,
    ReadinessProfile ReadinessProfile,
    LongHorizonGeWorkoutReference KeySessionWorkout,
    IReadOnlyList<LongHorizonGeWorkoutReference> EasySupportWorkouts,
    LongHorizonGeWorkoutReference LongRunWorkout,
    string CatalogSourceId,
    int CatalogSourceVersion,
    bool HasKeySession = true)
{
    /// <summary>
    /// Phase 10K-GEN.32 -- Option-A 2D alternating-week flag (GEN.31 §1/§3.4
    /// item 1): true for every pre-GEN.32 (4D/5D/6D constant-KEY) week via
    /// the default parameter, byte-identical to pre-GEN.32 behavior. False
    /// only for a 2D Pattern-B week (the alternating-selector opt-in path,
    /// <see cref="LongHorizonGeStructuralSelector.Select"/>'s
    /// <c>alternatingKeyEasy</c> parameter), in which case
    /// <see cref="KeySessionWorkout"/> is still resolved for provenance
    /// symmetry but must not be treated as an occupied KEY_SESSION slot by
    /// any downstream consumer -- mirroring the existing, already-approved
    /// "reference exists but role is absent this week" shape
    /// <see cref="EasySupportWorkouts"/>'s own list-length-zero convention
    /// already established for EASY_SUPPORT (FREQ.6D.14).
    /// </summary>
    public bool HasEasySupport => EasySupportWorkouts.Count > 0;


    /// <summary>The segment-type constant every GE descriptor/week carries -- distinct from <c>PreparationRunwayBlockType.GeneralEndurance</c> (Phase 4I.1/4I.2/4I.4/4I.5 governance).</summary>
    public const string LongHorizonGeneralEnduranceSegmentType = "LONG_HORIZON_GENERAL_ENDURANCE";
}
