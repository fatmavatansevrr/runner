using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
/// Backend Integration Phase 4G.3B.3 (PhaseConstraintVerifier only -- see
/// PhaseConstraintVerifier.cs for the explicit scope note). Proves the
/// verifier correctly Passes every real feasible target (8-14), reports
/// NotApplicable for infeasible targets, and Fails with specific findings
/// for constructed/mocked violations. Independent of
/// AllocationOrderCorrectnessVerifier -- no shared types or state.
/// </summary>
public sealed class PhaseConstraintVerifierTests
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
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Verify_EveryRealFeasibleTarget_ReportsPass_EmptyFindings(int targetWeeks)
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, targetWeeks);

        var result = PhaseConstraintVerifier.Verify(allocation);

        Assert.Equal(targetWeeks, result.TargetWeeks);
        Assert.Equal(PhaseConstraintVerificationOutcome.Pass, result.Outcome);
        Assert.Empty(result.Findings);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(20)]
    public async Task Verify_InfeasibleTarget_ReportsNotApplicable(int targetWeeks)
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, targetWeeks);

        var result = PhaseConstraintVerifier.Verify(allocation);

        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Equal(PhaseConstraintVerificationOutcome.NotApplicable, result.Outcome);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public void Verify_PhaseBelowMinimum_ReportsFail_WithSpecificFinding()
    {
        var allocation = new PhaseAllocationResult(
            TargetWeeks: 9,
            PreferredWeeks: 12,
            Delta: -3,
            Mode: AllocationMode.Compression,
            Phases: new[]
            {
                new AllocatedPhase("FOUNDATION", AllocatedWeeks: 1, MinimumWeeks: 2, MaximumWeeks: 4),
                new AllocatedPhase("BUILD", AllocatedWeeks: 4, MinimumWeeks: 3, MaximumWeeks: 5),
                new AllocatedPhase("RACE_SPECIFIC", AllocatedWeeks: 3, MinimumWeeks: 2, MaximumWeeks: 4),
                new AllocatedPhase("TAPER", AllocatedWeeks: 1, MinimumWeeks: 1, MaximumWeeks: 1),
            },
            IsMathematicallyFeasible: true,
            ReasonCode: "MATHEMATICALLY_FEASIBLE");

        var result = PhaseConstraintVerifier.Verify(allocation);

        Assert.Equal(PhaseConstraintVerificationOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Contains("PHASE_BELOW_MINIMUM") && f.Contains("FOUNDATION") && f.Contains("AllocatedWeeks=1") && f.Contains("MinimumWeeks=2"));
    }

    [Fact]
    public void Verify_PhaseAboveMaximum_ReportsFail_WithSpecificFinding()
    {
        var allocation = new PhaseAllocationResult(
            TargetWeeks: 13,
            PreferredWeeks: 12,
            Delta: 1,
            Mode: AllocationMode.Extension,
            Phases: new[]
            {
                new AllocatedPhase("FOUNDATION", AllocatedWeeks: 3, MinimumWeeks: 2, MaximumWeeks: 4),
                new AllocatedPhase("BUILD", AllocatedWeeks: 6, MinimumWeeks: 3, MaximumWeeks: 5),
                new AllocatedPhase("RACE_SPECIFIC", AllocatedWeeks: 3, MinimumWeeks: 2, MaximumWeeks: 4),
                new AllocatedPhase("TAPER", AllocatedWeeks: 1, MinimumWeeks: 1, MaximumWeeks: 1),
            },
            IsMathematicallyFeasible: true,
            ReasonCode: "MATHEMATICALLY_FEASIBLE");

        var result = PhaseConstraintVerifier.Verify(allocation);

        Assert.Equal(PhaseConstraintVerificationOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Contains("PHASE_ABOVE_MAXIMUM") && f.Contains("BUILD") && f.Contains("AllocatedWeeks=6") && f.Contains("MaximumWeeks=5"));
    }

    [Fact]
    public void Verify_AggregateSumMismatch_ReportsFail_WithSpecificFinding()
    {
        var allocation = new PhaseAllocationResult(
            TargetWeeks: 12,
            PreferredWeeks: 12,
            Delta: 0,
            Mode: AllocationMode.Preferred,
            Phases: new[]
            {
                new AllocatedPhase("FOUNDATION", AllocatedWeeks: 3, MinimumWeeks: 2, MaximumWeeks: 4),
                new AllocatedPhase("BUILD", AllocatedWeeks: 4, MinimumWeeks: 3, MaximumWeeks: 5),
                new AllocatedPhase("RACE_SPECIFIC", AllocatedWeeks: 4, MinimumWeeks: 2, MaximumWeeks: 4),
                new AllocatedPhase("TAPER", AllocatedWeeks: 2, MinimumWeeks: 1, MaximumWeeks: 1),
            },
            IsMathematicallyFeasible: true,
            ReasonCode: "MATHEMATICALLY_FEASIBLE");

        var result = PhaseConstraintVerifier.Verify(allocation);

        Assert.Equal(PhaseConstraintVerificationOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Contains("AGGREGATE_SUM_MISMATCH") && f.Contains("13") && f.Contains("TargetWeeks=12"));
    }

    [Fact]
    public void Verify_MultipleSimultaneousViolations_ReportsAllOfThem_NotJustTheFirst()
    {
        var allocation = new PhaseAllocationResult(
            TargetWeeks: 12,
            PreferredWeeks: 12,
            Delta: 0,
            Mode: AllocationMode.Preferred,
            Phases: new[]
            {
                new AllocatedPhase("FOUNDATION", AllocatedWeeks: 1, MinimumWeeks: 2, MaximumWeeks: 4),
                new AllocatedPhase("BUILD", AllocatedWeeks: 6, MinimumWeeks: 3, MaximumWeeks: 5),
                new AllocatedPhase("RACE_SPECIFIC", AllocatedWeeks: 4, MinimumWeeks: 2, MaximumWeeks: 4),
                new AllocatedPhase("TAPER", AllocatedWeeks: 0, MinimumWeeks: 1, MaximumWeeks: 1),
            },
            IsMathematicallyFeasible: true,
            ReasonCode: "MATHEMATICALLY_FEASIBLE");

        var result = PhaseConstraintVerifier.Verify(allocation);

        Assert.Equal(PhaseConstraintVerificationOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Contains("PHASE_BELOW_MINIMUM") && f.Contains("FOUNDATION"));
        Assert.Contains(result.Findings, f => f.Contains("PHASE_ABOVE_MAXIMUM") && f.Contains("BUILD"));
        Assert.Contains(result.Findings, f => f.Contains("PHASE_ALLOCATED_ZERO_OR_NEGATIVE") && f.Contains("TAPER"));
        Assert.Contains(result.Findings, f => f.Contains("PHASE_BELOW_MINIMUM") && f.Contains("TAPER"));
        Assert.True(result.Findings.Count >= 4, $"Expected at least 4 findings, got {result.Findings.Count}: {string.Join(" | ", result.Findings)}");
    }

    [Fact]
    public async Task Verify_CalledTwiceWithIdenticalInput_ProducesIdenticalOutput()
    {
        var candidate = await RealCandidateAsync();
        var allocation = _resolver.Resolve(candidate, 10);

        var first = PhaseConstraintVerifier.Verify(allocation);
        var second = PhaseConstraintVerifier.Verify(allocation);

        Assert.Equal(first.TargetWeeks, second.TargetWeeks);
        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.Findings, second.Findings);
    }

    [Fact]
    public void PhaseConstraintVerifier_HasNoCallSiteInApplicationOrApiProductionCode()
    {
        DarkReachabilityAssertions.AssertVerifierIsReachableOnlyFromDarkOrchestrator(nameof(PhaseConstraintVerifier));
    }
}
