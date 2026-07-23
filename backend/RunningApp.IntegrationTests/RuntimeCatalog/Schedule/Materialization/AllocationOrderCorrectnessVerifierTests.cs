using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4G.3B.3 (allocation-order-correctness verifier
/// only -- see AllocationOrderCorrectnessVerifier.cs for the explicit scope
/// note). Proves the required behavior: for every target week count other
/// than 8 (compression headroom fully exhausted), 12 (preferred,
/// unaffected), or 14 (extension headroom fully exhausted -- the symmetric
/// mirror of the 8-week case), the verifier reports DecisionRequired,
/// citing TD-ALLOCATION-PRIORITY-001 by ID, rather than Pass. Never
/// re-derives or corrects the priority ordering itself.
/// </summary>
public sealed class AllocationOrderCorrectnessVerifierTests
{
    private readonly CatalogPhaseAllocationResolver _resolver = new();

    private static async Task<PlanCatalogCandidateSummary> RealCandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = System.IO.Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return await loader.LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    public async Task Verify_EveryNonBoundaryFeasibleTarget_ReportsDecisionRequired_CitingTDAllocationPriority001(int targetWeeks)
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, targetWeeks);

        var result = AllocationOrderCorrectnessVerifier.Verify(allocation);

        Assert.Equal(targetWeeks, result.TargetWeeks);
        Assert.Equal(AllocationOrderVerificationOutcome.DecisionRequired, result.Outcome);
        Assert.Contains("TD-ALLOCATION-PRIORITY-001", result.ReasonCode);
    }

    [Fact]
    public async Task Verify_TargetEight_ReportsPass_NotDecisionRequired()
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, 8);

        var result = AllocationOrderCorrectnessVerifier.Verify(allocation);

        Assert.Equal(AllocationOrderVerificationOutcome.Pass, result.Outcome);
        Assert.Contains("PASS_COMPRESSION_HEADROOM_FULLY_EXHAUSTED", result.ReasonCode);
        Assert.DoesNotContain("TD-ALLOCATION-PRIORITY-001", result.ReasonCode);
    }

    [Fact]
    public async Task Verify_TargetTwelve_ReportsPass_NotDecisionRequired()
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, 12);

        var result = AllocationOrderCorrectnessVerifier.Verify(allocation);

        Assert.Equal(AllocationOrderVerificationOutcome.Pass, result.Outcome);
        Assert.DoesNotContain("TD-ALLOCATION-PRIORITY-001", result.ReasonCode);
    }

    [Fact]
    public async Task Verify_TargetFourteen_ReportsPass_ExtensionHeadroomFullyExhausted_MirrorsEightWeekCase()
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, 14);

        var result = AllocationOrderCorrectnessVerifier.Verify(allocation);

        Assert.Equal(AllocationOrderVerificationOutcome.Pass, result.Outcome);
        Assert.Contains("PASS_EXTENSION_HEADROOM_FULLY_EXHAUSTED", result.ReasonCode);
        Assert.DoesNotContain("TD-ALLOCATION-PRIORITY-001", result.ReasonCode);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(20)]
    public async Task Verify_InfeasibleTarget_ReportsPass_NotApplicable_NeverDecisionRequired(int targetWeeks)
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, targetWeeks);

        var result = AllocationOrderCorrectnessVerifier.Verify(allocation);

        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Equal(AllocationOrderVerificationOutcome.Pass, result.Outcome);
        Assert.Contains("NOT_APPLICABLE", result.ReasonCode);
    }

    [Fact]
    public void Verify_IsPureFunction_NoIOOrConstructorDependencies()
    {
        var method = typeof(AllocationOrderCorrectnessVerifier).GetMethod(nameof(AllocationOrderCorrectnessVerifier.Verify))!;

        Assert.True(method.IsStatic);
        Assert.Single(method.GetParameters());
        Assert.Equal(typeof(PhaseAllocationResult), method.GetParameters()[0].ParameterType);
    }
}
