using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;

internal static class PreparationRunwayCalendarSkeletonAdapter
{
    public const string MaterializerVersion = "PREPARATION_RUNWAY_CALENDAR_SKELETON_ADAPTER_V1";

    public static GeneratedCatalogPlanSkeleton Adapt<TKey>(
        PreparationRunwayCalendarCompositionRequest<TKey> request) where TKey : notnull
    {
        var prescribed = request.NumericRunway.PrescribedWeeks!
            .OrderBy(w => w.StructuralWeek.RunwayWeekNumber)
            .ToArray();
        var runwayStart = request.DateAuthority.RunwayStartDate;
        var blockCounts = prescribed.GroupBy(w => w.StructuralWeek.BlockType)
            .ToDictionary(g => g.Key, g => g.Count());
        var weeks = new List<GeneratedCatalogWeekSkeleton>(prescribed.Length);

        foreach (var week in prescribed)
        {
            var local = week.StructuralWeek.RunwayWeekNumber;
            var start = runwayStart.AddDays((local - 1) * 7);
            var stage = $"PREPARATION_RUNWAY_{week.StructuralWeek.BlockType.ToString()!.ToUpperInvariant()}";
            var slots = week.OrderedSlots
                .OrderBy(s => s.StructuralSlot.SlotOrdinal)
                .Select(slot => new GeneratedCatalogSessionSlotSkeleton
                {
                    SlotOrderInWeek = slot.StructuralSlot.SlotOrdinal,
                    LayoutSlotKey = SlotKey(slot.StructuralSlot.SlotRole, slot.StructuralSlot.RoleOrdinal),
                    StructuralRole = RoleKey(slot.StructuralSlot.SlotRole),
                    Provenance = new GeneratedCatalogSessionSlotSkeletonProvenance
                    {
                        SourceStageKey = stage,
                        SourceLayout = week.StructuralWeek.Provenance.SourceLayout,
                    },
                }).ToArray();

            weeks.Add(new GeneratedCatalogWeekSkeleton
            {
                WeekNumber = local,
                StartDate = start,
                EndDate = start.AddDays(6),
                StageKey = stage,
                StageWeekIndex = week.StructuralWeek.BlockWeekOrdinal,
                StageWeekCount = blockCounts[week.StructuralWeek.BlockType],
                SessionSlots = slots,
                Provenance = new GeneratedCatalogWeekSkeletonProvenance
                {
                    StageKey = stage,
                    SourcePhaseKey = stage,
                },
            });
        }

        return new GeneratedCatalogPlanSkeleton
        {
            SchemaVersion = GeneratedCatalogPlanSkeleton.CurrentSchemaVersion,
            StartDate = runwayStart,
            EndDate = runwayStart.AddDays(prescribed.Length * 7 - 1),
            PlannedWeekCount = prescribed.Length,
            DaysPerWeek = 4,
            CanonicalDistanceFamily = "TEN_K",
            CandidateKey = request.CandidateKey,
            CandidateVersion = request.CandidateVersion,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>
            {
                ["RUN_LAYOUT"] = prescribed[0].StructuralWeek.Provenance.SourceLayout,
            },
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = request.CandidateKey,
                CandidateVersion = request.CandidateVersion,
                DependencyVersions = new Dictionary<string, PlanCatalogReference>
                {
                    ["RUN_LAYOUT"] = prescribed[0].StructuralWeek.Provenance.SourceLayout,
                },
                AsOfDate = request.DateAuthority.HorizonContext.StartDate,
                MaterializerVersion = MaterializerVersion,
            },
        };
    }

    private static string RoleKey(PreparationRunwaySlotRole role) => role switch
    {
        PreparationRunwaySlotRole.KeySession => "KEY_SESSION",
        PreparationRunwaySlotRole.EasySupport => "EASY_SUPPORT",
        PreparationRunwaySlotRole.LongRun => "LONG_RUN",
        _ => throw new InvalidOperationException("Unsupported runway structural role."),
    };

    private static string SlotKey(PreparationRunwaySlotRole role, int ordinal) => $"{RoleKey(role)}_{ordinal}";
}
