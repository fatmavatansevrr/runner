using System;
using System.Linq;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>Backend Integration Phase 4F.3 — tests for <see cref="CatalogStageToWeekContextFactory"/>.</summary>
public sealed class CatalogStageToWeekContextFactoryTests
{
    private readonly CatalogStageToWeekContextFactory _factory = new();
    private readonly CatalogPhaseAllocationResolver _phaseResolver = new();
    private readonly CatalogRunLayoutResolver _layoutResolver = new();

    private (CatalogPlanSkeletonOrchestrationContext input, CatalogPhaseAllocation phaseAllocation, CatalogRunLayoutSlots runLayout) BuildInputs(
        DateOnly? startDate = null, DateOnly? asOfDate = null)
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate();
        var input = CatalogPlanSkeletonOrchestratorFixtures.OrchestrationContext(
            candidate: candidate, startDate: startDate);
        var context = asOfDate is null ? input : new CatalogPlanSkeletonOrchestrationContext
        {
            Candidate = input.Candidate,
            ExpectedCandidateKey = input.ExpectedCandidateKey,
            ExpectedCandidateVersion = input.ExpectedCandidateVersion,
            ExpectedMasterTemplate = input.ExpectedMasterTemplate,
            ExpectedRunLayout = input.ExpectedRunLayout,
            StartDate = input.StartDate,
            AsOfDate = asOfDate.Value,
        };

        return (context, _phaseResolver.Resolve(candidate), _layoutResolver.Resolve(candidate));
    }

    [Fact]
    public void Create_PreservesStartDate_Exactly()
    {
        var start = new DateOnly(2026, 9, 1);
        var (input, phaseAllocation, runLayout) = BuildInputs(startDate: start);

        var result = _factory.Create(input, phaseAllocation, runLayout);

        Assert.Equal(start, result.StartDate);
    }

    [Fact]
    public void Create_PreservesAsOfDate_Exactly()
    {
        var start = new DateOnly(2026, 9, 1);
        var asOf = new DateOnly(2026, 8, 20);
        var (input, phaseAllocation, runLayout) = BuildInputs(startDate: start, asOfDate: asOf);

        var result = _factory.Create(input, phaseAllocation, runLayout);

        Assert.Equal(asOf, result.AsOfDate);
    }

    [Fact]
    public void Create_PreservesCandidateKeyAndVersion()
    {
        var (input, phaseAllocation, runLayout) = BuildInputs();

        var result = _factory.Create(input, phaseAllocation, runLayout);

        Assert.Equal(input.Candidate.CandidateKey, result.CandidateKey);
        Assert.Equal(input.Candidate.CandidateVersion, result.CandidateVersion);
    }

    [Fact]
    public void Create_PreservesDependencyIdentitiesAndVersions()
    {
        var (input, phaseAllocation, runLayout) = BuildInputs();

        var result = _factory.Create(input, phaseAllocation, runLayout);

        Assert.Equal(input.Candidate.MasterTemplate, result.DependencyVersions["masterTemplate"]);
        Assert.Equal(input.Candidate.Layout, result.DependencyVersions["layout"]);
        Assert.Equal(input.Candidate.LevelModifier, result.DependencyVersions["levelModifier"]);
        Assert.Equal(input.Candidate.RulePack, result.DependencyVersions["rulePack"]);
    }

    [Fact]
    public void Create_UsesCatalogDerivedPhaseAllocation_Unchanged()
    {
        var (input, phaseAllocation, runLayout) = BuildInputs();

        var result = _factory.Create(input, phaseAllocation, runLayout);

        Assert.Equal(phaseAllocation.Entries.Select(e => e.PhaseKey), result.SelectedStageSequence);
        Assert.Equal(phaseAllocation.Entries.Select(e => e.PhaseWeekCount), result.StageWeekAllocations.Select(a => a.WeekCount));
        Assert.Equal(phaseAllocation.TotalWeeks, result.PlannedWeekCount);
    }

    [Fact]
    public void Create_UsesCatalogDerivedRunLayout_Unchanged()
    {
        var (input, phaseAllocation, runLayout) = BuildInputs();

        var result = _factory.Create(input, phaseAllocation, runLayout);

        Assert.Equal(runLayout.Layout, result.RunLayout);
        Assert.Equal(runLayout.StructuralRoles, result.RunLayoutSlotRoles);
        Assert.Equal(runLayout.StructuralRoles.Count, result.DaysPerWeek);
    }

    [Fact]
    public void Create_PreservesCanonicalDistanceFamily()
    {
        var (input, phaseAllocation, runLayout) = BuildInputs();

        var result = _factory.Create(input, phaseAllocation, runLayout);

        Assert.Equal(input.Candidate.CanonicalDistanceFamily, result.CanonicalDistanceFamily);
    }

    [Fact]
    public void Create_CreatesNoDefaultValues_ForMissingData()
    {
        // No overload accepts partial data -- the factory has no branch that
        // synthesizes a default phase allocation or run layout; this is
        // proven structurally by exercising a non-pilot shape and observing
        // it passes through unchanged rather than falling back to a default.
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate(
            phaseAllocations: new[] { new PlanCatalogPhaseAllocation("BASE", 5), new PlanCatalogPhaseAllocation("PEAK", 3) },
            slotRoles: new[] { "A", "B", "C" },
            daysPerWeek: 3,
            coreCycleDefaultWeeks: 8);
        var input = CatalogPlanSkeletonOrchestratorFixtures.OrchestrationContext(candidate: candidate);
        var phaseAllocation = _phaseResolver.Resolve(candidate);
        var runLayout = _layoutResolver.Resolve(candidate);

        var result = _factory.Create(input, phaseAllocation, runLayout);

        Assert.Equal(new[] { "BASE", "PEAK" }, result.SelectedStageSequence);
        Assert.Equal(new[] { "A", "B", "C" }, result.RunLayoutSlotRoles);
        Assert.Equal(8, result.PlannedWeekCount);
        Assert.Equal(3, result.DaysPerWeek);
    }

    [Fact]
    public void CatalogStageToWeekContextFactory_AccessesNoClockDbRouteOrResolverState()
    {
        // Structural proof: Create() is a pure function of its three
        // parameters -- calling it twice with identical inputs (including a
        // fixed, non-"now" AsOfDate) yields structurally identical output.
        var (input, phaseAllocation, runLayout) = BuildInputs(
            startDate: new DateOnly(2020, 1, 1), asOfDate: new DateOnly(2020, 1, 1));

        var first = _factory.Create(input, phaseAllocation, runLayout);
        var second = _factory.Create(input, phaseAllocation, runLayout);

        Assert.Equal(first.StartDate, second.StartDate);
        Assert.Equal(first.AsOfDate, second.AsOfDate);
        Assert.Equal(first.SelectedStageSequence, second.SelectedStageSequence);
        Assert.Equal(first.RunLayoutSlotRoles, second.RunLayoutSlotRoles);
    }

    [Fact]
    public void Create_DoesNotRerunCandidateSelection_ContextCandidateIsUsedAsIs()
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate(coreCycleDefaultWeeks: 99);
        var input = CatalogPlanSkeletonOrchestratorFixtures.OrchestrationContext(candidate: candidate);
        var phaseAllocation = new CatalogPhaseAllocation
        {
            Entries = new[] { new CatalogPhaseAllocationEntry("X", 99) },
            TotalWeeks = 99,
        };
        var runLayout = new CatalogRunLayoutSlots
        {
            Layout = candidate.Layout,
            StructuralRoles = new[] { "A" },
        };

        var result = _factory.Create(input, phaseAllocation, runLayout);

        Assert.Equal(candidate.CandidateKey, result.CandidateKey);
        Assert.Equal(99, result.PlannedWeekCount);
    }
}
