using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Domain.Enums;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.5 — test-only fixtures for
/// <see cref="CatalogWeekSkeletonCalendarMaterializer"/> and
/// <see cref="DatedGeneratedCatalogPlanSkeletonValidator"/>. Hand-builds a
/// structurally valid Phase 4F.2 <see cref="GeneratedCatalogPlanSkeleton"/>
/// (never touches a catalog file) so calendar-assignment logic can be
/// exercised in isolation from catalog loading/resolution.
/// </summary>
internal static class CatalogCalendarAssignmentFixtures
{
    public static readonly IReadOnlyList<string> DefaultSlotRoleOrder = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" };

    public static GeneratedCatalogPlanSkeleton BuildSkeleton(
        DateOnly startDate,
        int weekCount = 12,
        IReadOnlyList<string>? slotRoleOrder = null)
    {
        var roles = slotRoleOrder ?? DefaultSlotRoleOrder;
        var dependencyVersions = new Dictionary<string, PlanCatalogReference>
        {
            ["masterTemplate"] = new PlanCatalogReference("TEN_K_MASTER", 6),
            ["layout"] = new PlanCatalogReference("RUN_LAYOUT_4D", 2),
            ["levelModifier"] = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
            ["rulePack"] = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
        };

        var weeks = new List<GeneratedCatalogWeekSkeleton>(weekCount);
        var roleOccurrence = new Dictionary<string, int>();

        for (var weekNumber = 1; weekNumber <= weekCount; weekNumber++)
        {
            var weekStart = startDate.AddDays((weekNumber - 1) * 7);
            var weekEnd = weekStart.AddDays(6);
            roleOccurrence.Clear();

            var slots = new List<GeneratedCatalogSessionSlotSkeleton>(roles.Count);
            for (var i = 0; i < roles.Count; i++)
            {
                var role = roles[i];
                roleOccurrence.TryGetValue(role, out var priorCount);
                var occurrenceIndex = priorCount + 1;
                roleOccurrence[role] = occurrenceIndex;

                slots.Add(new GeneratedCatalogSessionSlotSkeleton
                {
                    SlotOrderInWeek = i + 1,
                    LayoutSlotKey = $"{role}_{occurrenceIndex}",
                    StructuralRole = role,
                    Provenance = new GeneratedCatalogSessionSlotSkeletonProvenance
                    {
                        SourceStageKey = "BUILD",
                        SourceLayout = dependencyVersions["layout"],
                    },
                });
            }

            weeks.Add(new GeneratedCatalogWeekSkeleton
            {
                WeekNumber = weekNumber,
                StartDate = weekStart,
                EndDate = weekEnd,
                StageKey = "BUILD",
                StageWeekIndex = weekNumber,
                StageWeekCount = weekCount,
                SessionSlots = slots,
                Provenance = new GeneratedCatalogWeekSkeletonProvenance { StageKey = "BUILD", SourcePhaseKey = "BUILD" },
            });
        }

        var planEndDate = startDate.AddDays(weekCount * 7 - 1);

        return new GeneratedCatalogPlanSkeleton
        {
            SchemaVersion = GeneratedCatalogPlanSkeleton.CurrentSchemaVersion,
            StartDate = startDate,
            EndDate = planEndDate,
            PlannedWeekCount = weekCount,
            DaysPerWeek = roles.Count,
            CanonicalDistanceFamily = "TEN_K",
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            DependencyVersions = dependencyVersions,
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = "TEN_K__4D__INTERMEDIATE",
                CandidateVersion = 10,
                DependencyVersions = dependencyVersions,
                AsOfDate = startDate,
                MaterializerVersion = CatalogStageToWeekMaterializerVersion.V1,
            },
        };
    }

    public static CatalogCalendarAssignmentContext BuildContext(
        GeneratedCatalogPlanSkeleton skeleton,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek? longRunDay,
        GoalType goalKind = GoalType.Race)
    {
        var provenance = new CatalogCalendarMaterializationProvenance(
            skeleton.CandidateKey,
            skeleton.CandidateVersion,
            skeleton.StartDate,
            skeleton.StartDate,
            preferredDays,
            longRunDay ?? preferredDays.First(),
            CatalogCalendarDayMaterializerVersion.V1,
            skeleton.SchemaVersion,
            skeleton.DependencyVersions);

        return new CatalogCalendarAssignmentContext(
            skeleton.StartDate,
            goalKind,
            preferredDays,
            longRunDay,
            skeleton,
            CatalogCalendarAssignmentPolicy.RaceHardConstraint,
            provenance);
    }

    public static CatalogWeekSkeletonCalendarMaterializer RealMaterializer() => new();

    public static DatedGeneratedCatalogPlanSkeletonValidator RealValidator() => new();
}
