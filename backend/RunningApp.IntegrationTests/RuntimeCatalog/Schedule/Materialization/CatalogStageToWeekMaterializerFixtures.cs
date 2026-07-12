using System;
using System.Collections.Generic;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.2 — the repository-confirmed TEN_K/INTERMEDIATE/4D
/// pilot materialization context, built directly from evidence read in
/// <c>templates/ten-k-master.v6.json</c> (phases: FOUNDATION preferredWeeks=3,
/// BUILD=4, RACE_SPECIFIC=4, TAPER=1 — sum 12, matching coreCycle.defaultWeeks)
/// and <c>layouts/run-layout-4d.v2.json</c> (runsPerWeek=4, slots=
/// [KEY_SESSION, EASY_SUPPORT, EASY_SUPPORT, LONG_RUN]), cross-confirmed by
/// the existing passing test
/// <c>PlanCatalogBundleLoaderTests.LoadCandidateAsync_ExposesPhaseKeysAndSlotRoles</c>.
/// This fixture does NOT load catalog files at test time (the materializer
/// has no catalog-loader dependency) — the values are hand-supplied,
/// matching what a future integration phase would resolve from the catalog
/// before calling the materializer.
/// </summary>
internal static class CatalogStageToWeekMaterializerFixtures
{
    public const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    public const int CandidateVersion = 10;
    public const int PlannedWeekCount = 12;
    public const int DaysPerWeek = 4;

    public static readonly IReadOnlyList<string> PilotStageSequence = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" };

    public static readonly IReadOnlyList<CatalogStageWeekAllocation> PilotStageAllocations = new[]
    {
        new CatalogStageWeekAllocation("FOUNDATION", 3),
        new CatalogStageWeekAllocation("BUILD", 4),
        new CatalogStageWeekAllocation("RACE_SPECIFIC", 4),
        new CatalogStageWeekAllocation("TAPER", 1),
    };

    public static readonly IReadOnlyList<string> PilotRunLayoutSlotRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" };

    public static readonly PlanCatalogReference PilotRunLayout = new("RUN_LAYOUT_4D", 2);

    public static IReadOnlyDictionary<string, PlanCatalogReference> PilotDependencyVersions() => new Dictionary<string, PlanCatalogReference>
    {
        ["masterTemplate"] = new PlanCatalogReference("TEN_K_MASTER", 6),
        ["layout"] = PilotRunLayout,
        ["levelModifier"] = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
        ["rulePack"] = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
    };

    public static CatalogStageToWeekMaterializationContext PilotContext(
        DateOnly? startDate = null,
        IReadOnlyList<string>? selectedStageSequence = null,
        IReadOnlyList<CatalogStageWeekAllocation>? stageWeekAllocations = null,
        int? plannedWeekCount = null,
        IReadOnlyList<string>? runLayoutSlotRoles = null,
        int? daysPerWeek = null)
    {
        var start = startDate ?? new DateOnly(2026, 8, 5); // a Wednesday, deliberately non-Monday (Decision 1/5 evidence)
        return new CatalogStageToWeekMaterializationContext
        {
            StartDate = start,
            AsOfDate = start,
            PlannedWeekCount = plannedWeekCount ?? PlannedWeekCount,
            DaysPerWeek = daysPerWeek ?? DaysPerWeek,
            CanonicalDistanceFamily = "TEN_K",
            CandidateKey = CandidateKey,
            CandidateVersion = CandidateVersion,
            DependencyVersions = PilotDependencyVersions(),
            SelectedStageSequence = selectedStageSequence ?? PilotStageSequence,
            StageWeekAllocations = stageWeekAllocations ?? PilotStageAllocations,
            RunLayout = PilotRunLayout,
            RunLayoutSlotRoles = runLayoutSlotRoles ?? PilotRunLayoutSlotRoles,
        };
    }
}
