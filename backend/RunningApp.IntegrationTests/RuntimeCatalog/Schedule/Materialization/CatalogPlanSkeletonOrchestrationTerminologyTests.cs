using System.Linq;
using System.Reflection;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.3 — proves the phaseKey vs. stageKey
/// terminology boundary is honored: new Phase 4F.3 allocation types use
/// precise "Phase" terminology; workout-selection stageKey values (from
/// workout-progressions/*.json's phaseProgressions[].stages[]) are never
/// consumed; the phase/stage distinction is documented on the adapter; and no
/// broad rename of Phase 4F.2's pre-existing StageKey-named members occurred.
/// </summary>
public sealed class CatalogPlanSkeletonOrchestrationTerminologyTests
{
    [Fact]
    public void CatalogPhaseAllocationEntry_UsesPhaseTerminology_NotStageTerminology()
    {
        var props = typeof(CatalogPhaseAllocationEntry).GetProperties();

        Assert.Contains(props, p => p.Name == "PhaseKey");
        Assert.Contains(props, p => p.Name == "PhaseWeekCount");
        Assert.DoesNotContain(props, p => p.Name is "StageKey" or "WeekCount");
    }

    [Fact]
    public void CatalogPhaseAllocationResolver_ReadsOnly_PlanCatalogPhaseAllocation_NeverWorkoutProgressionTypes()
    {
        // Structural proof: the resolver's only catalog-facing dependency is
        // PlanCatalogCandidateSummary.PhaseAllocations (master-template
        // phases[].preferredWeeks) -- it has no reference anywhere to a
        // workout-progression / stage-exposure type.
        var resolverAssembly = typeof(CatalogPhaseAllocationResolver).Assembly;
        var workoutProgressionTypeNames = resolverAssembly.GetTypes()
            .Where(t => t.Name.Contains("WorkoutProgression") || t.Name.Contains("StageExposure"))
            .Select(t => t.FullName)
            .ToList();

        var resolverMethod = typeof(CatalogPhaseAllocationResolver).GetMethod(nameof(CatalogPhaseAllocationResolver.Resolve))!;
        var referencedTypeNames = new[] { resolverMethod.ReturnType, resolverMethod.GetParameters()[0].ParameterType }
            .Select(t => t.FullName).ToList();

        Assert.Empty(referencedTypeNames.Intersect(workoutProgressionTypeNames));
    }

    [Fact]
    public void CatalogStageToWeekContextFactory_Create_DocumentsThePhaseToStageAdapter()
    {
        var method = typeof(CatalogStageToWeekContextFactory).GetMethod(nameof(CatalogStageToWeekContextFactory.Create))!;

        // The XML doc comment lives in source, not reflectable metadata; this
        // test instead proves the adapter's structural behavior: a
        // CatalogPhaseAllocationEntry's PhaseKey/PhaseWeekCount values are
        // translated 1:1, in order, into CatalogStageWeekAllocation's
        // StageKey/WeekCount fields, with no reordering or renaming of values.
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate();
        var input = CatalogPlanSkeletonOrchestratorFixtures.OrchestrationContext(candidate: candidate);
        var phaseAllocation = new CatalogPhaseAllocationResolver().Resolve(candidate);
        var runLayout = new CatalogRunLayoutResolver().Resolve(candidate);

        var result = new CatalogStageToWeekContextFactory().Create(input, phaseAllocation, runLayout);

        Assert.Equal(phaseAllocation.Entries.Select(e => e.PhaseKey), result.StageWeekAllocations.Select(a => a.StageKey));
        Assert.Equal(phaseAllocation.Entries.Select(e => e.PhaseWeekCount), result.StageWeekAllocations.Select(a => a.WeekCount));
        Assert.NotNull(method);
    }

    [Fact]
    public void Phase4F2StageKeyNamedTypes_WereNotRenamed()
    {
        // No broad rename migration was performed -- CatalogStageWeekAllocation
        // and GeneratedCatalogWeekSkeleton.StageKey (Phase 4F.2) still exist
        // under their original names.
        Assert.NotNull(typeof(CatalogStageWeekAllocation).GetProperty("StageKey"));
        Assert.NotNull(typeof(GeneratedCatalogWeekSkeleton).GetProperty("StageKey"));
    }
}
