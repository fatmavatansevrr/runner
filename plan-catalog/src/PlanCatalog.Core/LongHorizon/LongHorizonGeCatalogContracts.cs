namespace PlanCatalog.Core.LongHorizon;

/// <summary>
/// Phase 4I.4 — dark catalog-only typed contract for the 1-32-week
/// LONG_HORIZON_GENERAL_ENDURANCE segment. Consumes an already-approved GE
/// week count (Phase 4I.1's own composition formula, "GeneralEnduranceWeeks =
/// AvailableFullWeeks - 20") — this library never computes that number
/// itself, matching the established "one authority per concern" convention
/// (Phase 4I.3's <c>LongHorizonCompositionResolver</c> owns composition;
/// this library owns catalog selection only). No date, volume, pace, or
/// calendar computation is performed here.
/// </summary>
public enum LongHorizonReadinessProfile
{
    ConsistencyNeeded,
    CoreEntryReady,
}

/// <summary>1-3 GE weeks vs 4-32 GE weeks, per Phase 4I.1/4I.2 -- reused here for catalog selection, never redefined.</summary>
public enum GeDurationClassification
{
    ShortExtension,
    FullPhase,
}

/// <summary>
/// The five approved stage families (Phase 4I.2 Part 2 / this phase's own
/// Part 3). Deliberately distinct from Core's Foundation/Build stage
/// vocabulary and from the Preparation Runway's own block vocabulary --
/// type context (this enum) keeps them unambiguous even though "Base
/// Development" and Core's "Foundation" describe conceptually adjacent
/// ideas.
/// </summary>
public enum GeStageFamily
{
    Entry,
    BaseDevelopment,
    AerobicDurability,
    Consolidation,
    PreRunwayAlignment,
}

/// <summary>Position of a week within a 4-week FullPhase mesocycle. <see cref="NotApplicable"/> for ShortExtension/terminal-remainder weeks, which use <see cref="ShortExtensionRole"/> instead.</summary>
public enum MesocyclePosition
{
    NotApplicable,
    Development1,
    Development2,
    Development3,
    RecoveryConsolidation,
}

/// <summary>Role of a ShortExtension (1-3 GE weeks) or terminal-remainder (1-3 leftover weeks) week. <see cref="NotApplicable"/> for FullPhase mesocycle weeks, which use <see cref="MesocyclePosition"/> instead.</summary>
public enum ShortExtensionRole
{
    NotApplicable,
    EntryAlignment,
    ControlledDevelopment,
    PreRunwayAlignment,
}

/// <summary>The four weekly running-session roles, per Phase 4I.2's approved skeleton (1 KEY_SESSION + 2 EASY_SUPPORT + 1 LONG_RUN). Never a fifth role; strength/gym content never occupies one of these.</summary>
public enum GeWeekRole
{
    KeySession,
    EasySupportA,
    EasySupportB,
    LongRun,
}

/// <summary>A single catalog workout reference -- key + version only, never a materialized numeric prescription.</summary>
public sealed record GeWorkoutReference(string Key, int Version, string Family);

/// <summary>
/// One fully-resolved catalog-only week descriptor: which stage family, which
/// structural position, which workout reference per role, and full
/// provenance -- everything a later structural-materialization phase needs,
/// with zero numeric volume/pace/date content.
/// </summary>
public sealed record GeWeekDescriptor(
    int WeekIndex,
    string SegmentType,
    GeDurationClassification Classification,
    int? MesocycleIndex,
    MesocyclePosition MesocyclePosition,
    ShortExtensionRole ShortExtensionRole,
    GeStageFamily StageFamily,
    bool IsRecoveryWeek,
    bool IsTerminalAlignment,
    LongHorizonReadinessProfile ReadinessProfile,
    IReadOnlyDictionary<GeWeekRole, GeWorkoutReference> Roles,
    string CatalogSourceId,
    int CatalogSourceVersion)
{
    /// <summary>The segment-type constant every descriptor carries -- distinct from <c>PreparationRunwayBlockType.GeneralEndurance</c> (see Phase 4I.1/4I.2/4I.4 governance).</summary>
    public const string LongHorizonGeneralEnduranceSegmentType = "LONG_HORIZON_GENERAL_ENDURANCE";
}
