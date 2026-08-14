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

/// <summary>The four weekly running-session roles for a GE week, per Phase 4I.2's approved skeleton (1 KEY_SESSION + 2 EASY_SUPPORT + 1 LONG_RUN).</summary>
internal enum LongHorizonGeWeekRole
{
    KeySession,
    EasySupportA,
    EasySupportB,
    LongRun,
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
    IReadOnlyDictionary<LongHorizonGeWeekRole, LongHorizonGeWorkoutReference> Roles,
    string CatalogSourceId,
    int CatalogSourceVersion)
{
    /// <summary>The segment-type constant every GE descriptor/week carries -- distinct from <c>PreparationRunwayBlockType.GeneralEndurance</c> (Phase 4I.1/4I.2/4I.4/4I.5 governance).</summary>
    public const string LongHorizonGeneralEnduranceSegmentType = "LONG_HORIZON_GENERAL_ENDURANCE";
}
