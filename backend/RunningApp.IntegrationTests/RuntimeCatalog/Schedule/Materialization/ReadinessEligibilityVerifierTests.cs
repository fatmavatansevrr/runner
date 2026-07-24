using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

public sealed class ReadinessEligibilityVerifierTests
{
    private readonly CatalogPhaseAllocationResolver _allocator = new();

    private static async Task<PlanCatalogCandidateSummary> RealCandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return await loader.LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Verify_RealFeasibleRange_OutcomeIsPass_FoundationNeverBelowCatalogMinimum(int targetWeeks)
    {
        var candidate = await RealCandidateAsync();
        var allocation = _allocator.Resolve(candidate, targetWeeks);

        var result = ReadinessEligibilityVerifier.Verify(allocation);

        Assert.Equal(targetWeeks, result.TargetWeeks);
        Assert.Equal(ReadinessEligibilityOutcome.Pass, result.Outcome);
        Assert.Empty(result.Findings);
        Assert.All(result.PhaseChecks, c => Assert.False(c.RequiresBelowMinimum));

        var foundation = Assert.Single(result.PhaseChecks, c => c.PhaseKey == "FOUNDATION");
        Assert.NotEqual(1, foundation.AllocatedWeeks);
        Assert.False(foundation.RequiresBelowMinimum);
    }

    [Fact]
    public async Task Verify_RealFeasibleRange_ReportsFoundationAllocatedWeeksPerTarget()
    {
        // Pins the exact per-target Foundation values the Pass/never-1
        // claim above rests on, so a future allocator or catalog change
        // that silently shifts these is caught precisely.
        var candidate = await RealCandidateAsync();
        var expected = new Dictionary<int, int> { [8] = 2, [9] = 2, [10] = 2, [11] = 2, [12] = 3, [13] = 4, [14] = 4 };

        foreach (var (targetWeeks, expectedFoundationWeeks) in expected)
        {
            var allocation = _allocator.Resolve(candidate, targetWeeks);
            var result = ReadinessEligibilityVerifier.Verify(allocation);
            var foundation = Assert.Single(result.PhaseChecks, c => c.PhaseKey == "FOUNDATION");
            Assert.Equal(expectedFoundationWeeks, foundation.AllocatedWeeks);
            Assert.Equal(2, foundation.CatalogMinimumWeeks);
        }
    }

    [Fact]
    public void Verify_SyntheticPhaseBelowCatalogMinimum_ReportsDecisionRequired_CitingTDFoundationCompression001()
    {
        var allocation = new PhaseAllocationResult(
            TargetWeeks: 7,
            PreferredWeeks: 12,
            Delta: -5,
            Mode: AllocationMode.Compression,
            Phases:
            [
                new AllocatedPhase("FOUNDATION", AllocatedWeeks: 1, MinimumWeeks: 2, MaximumWeeks: 4),
                new AllocatedPhase("BUILD", AllocatedWeeks: 3, MinimumWeeks: 3, MaximumWeeks: 5),
                new AllocatedPhase("RACE_SPECIFIC", AllocatedWeeks: 2, MinimumWeeks: 2, MaximumWeeks: 4),
                new AllocatedPhase("TAPER", AllocatedWeeks: 1, MinimumWeeks: 1, MaximumWeeks: 1),
            ],
            IsMathematicallyFeasible: true,
            ReasonCode: "SYNTHETIC_TEST_ONLY_REAL_ALLOCATOR_CANNOT_PRODUCE_THIS");

        var result = ReadinessEligibilityVerifier.Verify(allocation);

        Assert.Equal(ReadinessEligibilityOutcome.DecisionRequired, result.Outcome);
        Assert.Contains(result.Findings, f => f.Contains("TD-FOUNDATION-COMPRESSION-001"));

        var foundation = Assert.Single(result.PhaseChecks, c => c.PhaseKey == "FOUNDATION");
        Assert.True(foundation.RequiresBelowMinimum);
        Assert.Equal(1, foundation.AllocatedWeeks);
        Assert.Equal(2, foundation.CatalogMinimumWeeks);

        Assert.All(result.PhaseChecks.Where(c => c.PhaseKey != "FOUNDATION"), c => Assert.False(c.RequiresBelowMinimum));
    }

    [Fact]
    public void Verify_MathematicallyInfeasibleAllocation_IsNotApplicable()
    {
        var allocation = new PhaseAllocationResult(7, 12, -5, AllocationMode.Compression, [], false, "TARGET_BELOW_SUM_OF_MINIMUMS");

        var result = ReadinessEligibilityVerifier.Verify(allocation);

        Assert.Equal(ReadinessEligibilityOutcome.NotApplicable, result.Outcome);
        Assert.Empty(result.PhaseChecks);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public async Task Verify_CalledTwiceWithIdenticalInput_ProducesIdenticalOutput()
    {
        var candidate = await RealCandidateAsync();
        var allocation = _allocator.Resolve(candidate, 10);

        var first = ReadinessEligibilityVerifier.Verify(allocation);
        var second = ReadinessEligibilityVerifier.Verify(allocation);

        Assert.Equal(first.TargetWeeks, second.TargetWeeks);
        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.PhaseChecks, second.PhaseChecks);
        Assert.Equal(first.Findings, second.Findings);
    }

    [Fact]
    public void Verify_IsPureFunction_SingleParameterOfPhaseAllocationResult()
    {
        var method = typeof(ReadinessEligibilityVerifier).GetMethod(nameof(ReadinessEligibilityVerifier.Verify), BindingFlags.Public | BindingFlags.Static)!;

        Assert.Single(method.GetParameters());
        Assert.Equal(typeof(PhaseAllocationResult), method.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(ReadinessEligibilityVerificationResult), method.ReturnType);
        Assert.Null(typeof(ReadinessEligibilityVerifier).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault());
    }

    [Fact]
    public void ReadinessEligibilityVerifier_HasNoDependencyOnAnyRuntimeConditionResolver()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog",
            "Schedule", "Materialization", "ReadinessEligibilityVerifier.cs"));

        // Structural dependency checks -- type names and the resolver
        // namespace's using directive, not prose. This file's own doc
        // comment legitimately mentions CORE_ENTRY_READINESS_IN and
        // CoreEntryReadinessResolver BY NAME in prose (explaining why this
        // verifier does NOT consume them) -- that is a documentation
        // cross-reference, not a code dependency, so those two strings are
        // deliberately not asserted against here (matching the same
        // distinction already established for TD-ID citations elsewhere in
        // this session, e.g. TD-ALLOCATION-PRIORITY-001).
        Assert.DoesNotContain("RuntimeConditionResolutionService", source);
        Assert.DoesNotContain("RuntimeConditionResolutionResult", source);
        Assert.DoesNotContain("RuntimeResolverContext", source);
        Assert.DoesNotContain("IRuntimeConditionResolver", source);
        Assert.DoesNotContain("using RunningApp.Application.RuntimeCatalog.Resolvers", source);

        var type = typeof(ReadinessEligibilityVerifier);
        Assert.DoesNotContain(type.GetFields(BindingFlags.NonPublic | BindingFlags.Static),
            f => f.FieldType.Namespace == "RunningApp.Application.RuntimeCatalog.Resolvers");
    }

    [Fact]
    public void ReadinessEligibilityVerifier_HasNoCallSiteInApplicationOrApiProductionCode()
    {
        var repoRoot = TestPlanServicesFactory.RepoRoot();
        foreach (var root in new[] { Path.Combine(repoRoot, "backend", "RunningApp.Application"), Path.Combine(repoRoot, "backend", "RunningApp.Api") })
        {
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    && !f.EndsWith($"{Path.DirectorySeparatorChar}ReadinessEligibilityVerifier.cs", StringComparison.OrdinalIgnoreCase));
            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                Assert.DoesNotContain("ReadinessEligibilityVerifier.Verify(", content, StringComparison.Ordinal);
            }
        }
    }
}
