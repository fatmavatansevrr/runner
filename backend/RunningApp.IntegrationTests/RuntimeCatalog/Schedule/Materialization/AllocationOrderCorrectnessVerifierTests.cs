using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

public sealed class AllocationOrderCorrectnessVerifierTests
{
    private const string GovernanceSource = "TD-ALLOCATION-PRIORITY-001 closure (Phase 4G.3B.8.1)";
    private readonly CatalogPhaseAllocationResolver _resolver = new();

    private static async Task<PlanCatalogCandidateSummary> RealCandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return await loader.LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
    }

    [Theory]
    [InlineData(12, true, false, 3, 4, 4, 1)]
    [InlineData(13, false, true, 4, 4, 4, 1)]
    [InlineData(14, true, false, 4, 5, 4, 1)]
    public async Task Verify_ApprovedRealPriority_ProducesExpectedExecutableAllocation(
        int weeks, bool orderIndependent, bool usesPriority,
        int foundation, int build, int raceSpecific, int taper)
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, weeks);
        var policy = ApprovedAllocationPriorityPolicy.FromCandidate(candidate, GovernanceSource);

        var result = AllocationOrderCorrectnessVerifier.Verify(allocation, policy);

        Assert.Equal(AllocationOrderVerificationOutcome.Pass, result.Outcome);
        Assert.Equal(orderIndependent, result.IsOrderIndependent);
        Assert.Equal(usesPriority, result.UsesApprovedPriority);
        Assert.True(result.IsExecutable);
        Assert.Equal(new[] { foundation, build, raceSpecific, taper }, allocation.Phases.Select(p => p.AllocatedWeeks));
        Assert.Contains(orderIndependent ? "ORDER_INDEPENDENT" : "ORDER_DEPENDENT_BUT_APPROVED_PRIORITY", result.ReasonCode);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public async Task Verify_ApprovedCompressionPriority_PassesWithoutChangingAllocation(int weeks)
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, weeks);

        var result = AllocationOrderCorrectnessVerifier.Verify(
            allocation,
            ApprovedAllocationPriorityPolicy.FromCandidate(candidate, GovernanceSource));

        Assert.Equal(AllocationOrderVerificationOutcome.Pass, result.Outcome);
        Assert.False(result.IsOrderIndependent);
        Assert.True(result.UsesApprovedPriority);
        Assert.True(result.IsExecutable);
    }

    [Fact]
    public async Task Verify_OrderDependentAllocationWithoutApproval_RemainsDecisionRequired()
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, 13);

        var result = AllocationOrderCorrectnessVerifier.Verify(allocation);

        Assert.Equal(AllocationOrderVerificationOutcome.DecisionRequired, result.Outcome);
        Assert.False(result.IsOrderIndependent);
        Assert.False(result.UsesApprovedPriority);
        Assert.False(result.IsExecutable);
        Assert.Contains("PRIORITY_REQUIRED", result.ReasonCode);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("unknown")]
    public async Task Verify_InvalidPriority_FailsClosed(string kind)
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, 13);
        var rules = ApprovedAllocationPriorityPolicy.FromCandidate(candidate, GovernanceSource).Phases.ToArray();
        rules = kind switch
        {
            "duplicate" => rules.Select((p, i) => i == 1 ? p with { ExtensionPriority = rules[0].ExtensionPriority } : p).ToArray(),
            "missing" => rules.Select((p, i) => i == 0 ? p with { ExtensionPriority = null } : p).ToArray(),
            "unknown" => rules.Select((p, i) => i == 0 ? p with { PhaseKey = "UNKNOWN" } : p).ToArray(),
            _ => throw new InvalidOperationException(),
        };

        var result = AllocationOrderCorrectnessVerifier.Verify(
            allocation,
            new ApprovedAllocationPriorityPolicy(true, GovernanceSource, rules));

        Assert.Equal(AllocationOrderVerificationOutcome.Invalid, result.Outcome);
        Assert.False(result.IsExecutable);
        Assert.Contains("INVALID_PRIORITY", result.ReasonCode);
    }

    [Fact]
    public void Verify_GenericOrderDependentSyntheticAllocation_PassesWithoutTargetSpecificBranch()
    {
        var allocation = new PhaseAllocationResult(
            6, 5, 1, AllocationMode.Extension,
            new[] { new AllocatedPhase("ALPHA", 3, 1, 3), new AllocatedPhase("BETA", 3, 2, 4) },
            true, "SYNTHETIC");
        var policy = new ApprovedAllocationPriorityPolicy(true, "SYNTHETIC_APPROVAL", new[]
        {
            new ApprovedPhaseAllocationRule("ALPHA", 1, 2, 3, 1, 1),
            new ApprovedPhaseAllocationRule("BETA", 2, 3, 4, 2, 2),
        });

        var result = AllocationOrderCorrectnessVerifier.Verify(allocation, policy);

        Assert.Equal(AllocationOrderVerificationOutcome.Pass, result.Outcome);
        Assert.False(result.IsOrderIndependent);
        Assert.True(result.UsesApprovedPriority);
        Assert.True(result.IsExecutable);
    }

    [Fact]
    public void Verify_BoundsViolation_FailsClosed()
    {
        var allocation = new PhaseAllocationResult(
            13, 12, 1, AllocationMode.Extension,
            new[] { new AllocatedPhase("FOUNDATION", 5, 2, 4), new AllocatedPhase("BUILD", 8, 3, 5) },
            true, "SYNTHETIC");

        var result = AllocationOrderCorrectnessVerifier.Verify(allocation);

        Assert.Equal(AllocationOrderVerificationOutcome.Invalid, result.Outcome);
        Assert.Contains("BOUNDS_VIOLATION", result.ReasonCode);
    }
}
