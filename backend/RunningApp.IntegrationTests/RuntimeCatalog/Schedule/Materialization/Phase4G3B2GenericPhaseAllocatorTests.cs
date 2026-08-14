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
/// Backend Integration Phase 4G.3B.2 — Generic Target-Week-Count Phase
/// Allocator. Proves <see cref="CatalogPhaseAllocationResolver.Resolve(PlanCatalogCandidateSummary, int)"/>
/// is a purely mechanical, target-week-count-parametric extension of the
/// unchanged candidate-only <c>Resolve(candidate)</c>. Uses the real,
/// live <c>TEN_K_MASTER v6</c> catalog artifact throughout (not a synthetic
/// fixture) so every assertion here is directly evidence for the mechanical
/// allocation table in PHASE4G_3B_2_GENERIC_PHASE_ALLOCATOR.md. No public
/// behavior, routing, or live generation path is touched or exercised by
/// this file.
/// </summary>
public sealed class Phase4G3B2GenericPhaseAllocatorTests
{
    private readonly CatalogPhaseAllocationResolver _resolver = new();

    private static async Task<PlanCatalogCandidateSummary> RealCandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = System.IO.Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return await loader.LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
    }

    // ── Requirement 1: 8-week result matches Phase 4G.3A exactly ────────────

    [Fact]
    public async Task Resolve_TargetEight_MatchesPhase4G3A_F2B3RS2T1_Exactly()
    {
        var candidate = await RealCandidateAsync();

        var result = _resolver.Resolve(candidate, 8);

        Assert.True(result.IsMathematicallyFeasible);
        Assert.Equal("MATHEMATICALLY_FEASIBLE", result.ReasonCode);
        Assert.Equal(8, result.TargetWeeks);
        Assert.Equal(12, result.PreferredWeeks);
        Assert.Equal(-4, result.Delta);
        Assert.Equal(AllocationMode.Compression, result.Mode);
        Assert.Equal(
            new (string PhaseKey, int Weeks)[] { ("FOUNDATION", 2), ("BUILD", 3), ("RACE_SPECIFIC", 2), ("TAPER", 1) },
            result.Phases.Select(p => (p.PhaseKey, p.AllocatedWeeks)));
        Assert.Equal(8, result.Phases.Sum(p => p.AllocatedWeeks));
    }

    // ── Requirement 2: 12-week result equals the existing method's output ───

    [Fact]
    public async Task Resolve_TargetTwelve_EqualsExistingCandidateOnlyMethod_3_4_4_1()
    {
        var candidate = await RealCandidateAsync();

        var mechanical = _resolver.Resolve(candidate, 12);
        var existing = _resolver.Resolve(candidate);

        Assert.True(mechanical.IsMathematicallyFeasible);
        Assert.Equal(AllocationMode.Preferred, mechanical.Mode);
        Assert.Equal(0, mechanical.Delta);
        Assert.Equal(12, existing.TotalWeeks);
        Assert.Equal(
            existing.Entries.Select(e => (e.PhaseKey, e.PhaseWeekCount)),
            mechanical.Phases.Select(p => (p.PhaseKey, p.AllocatedWeeks)));
        Assert.Equal(
            new[] { 3, 4, 4, 1 },
            mechanical.Phases.Select(p => p.AllocatedWeeks));
    }

    // ── Correction (post-4G.3B.2 review): a per-step bound violation could
    // ── only be caught by an explicit InRange assertion at EVERY feasible
    // ── target, not just 9/10/11/13/14 -- 8 and 12 were previously only
    // ── proven in-bounds indirectly via exact-value equality (which does
    // ── entail bound satisfaction for this catalog's numbers, but is not a
    // ── dedicated per-target-week-count bound-safety proof). Closes that
    // ── gap explicitly across the full actual feasible range (8-14; see
    // ── Resolve_ActualFeasibleRange_IsEightToFourteen_MatchingCoreCycleBounds).

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Resolve_EveryFeasibleTarget_NeverAllocatesBelowMinimumOrAboveMaximum(int targetWeeks)
    {
        var candidate = await RealCandidateAsync();

        var result = _resolver.Resolve(candidate, targetWeeks);

        Assert.True(result.IsMathematicallyFeasible);
        Assert.Equal(targetWeeks, result.Phases.Sum(p => p.AllocatedWeeks));
        Assert.All(result.Phases, p => Assert.InRange(p.AllocatedWeeks, p.MinimumWeeks, p.MaximumWeeks));
    }

    // ── Requirement 3: 9,10,11,13,14 -- internally valid, Taper unchanged ───

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Resolve_InRangeNonPreferredTargets_AreInternallyValid_TaperUnchanged(int targetWeeks)
    {
        var candidate = await RealCandidateAsync();

        var result = _resolver.Resolve(candidate, targetWeeks);

        Assert.True(result.IsMathematicallyFeasible);
        Assert.Equal(targetWeeks, result.Phases.Sum(p => p.AllocatedWeeks));
        Assert.All(result.Phases, p => Assert.InRange(p.AllocatedWeeks, p.MinimumWeeks, p.MaximumWeeks));

        // TAPER's catalog-declared min=preferred=max=1 makes it structurally
        // invariant -- never special-cased in the algorithm, per the phase's
        // own scope instruction.
        var taper = result.Phases.Single(p => p.PhaseKey == "TAPER");
        Assert.Equal(1, taper.AllocatedWeeks);
        Assert.Equal(1, taper.MinimumWeeks);
        Assert.Equal(1, taper.MaximumWeeks);
    }

    [Theory]
    [InlineData(9, "FOUNDATION", 2, "BUILD", 3, "RACE_SPECIFIC", 3)]
    [InlineData(10, "FOUNDATION", 2, "BUILD", 3, "RACE_SPECIFIC", 4)]
    [InlineData(11, "FOUNDATION", 2, "BUILD", 4, "RACE_SPECIFIC", 4)]
    [InlineData(13, "FOUNDATION", 4, "BUILD", 4, "RACE_SPECIFIC", 4)]
    [InlineData(14, "FOUNDATION", 4, "BUILD", 5, "RACE_SPECIFIC", 4)]
    public async Task Resolve_InRangeNonPreferredTargets_MatchExactPriorityOrderedAllocation(
        int targetWeeks, string p1Key, int p1Weeks, string p2Key, int p2Weeks, string p3Key, int p3Weeks)
    {
        // Pins the exact per-phase result of anchoring at PreferredWeeks and
        // walking by CompressionPriority (below preferred) / ExtensionPriority
        // (above preferred) -- see PHASE4G_3B_2_GENERIC_PHASE_ALLOCATOR.md
        // section 3/4 for the full worked derivation of each of these rows.
        var candidate = await RealCandidateAsync();

        var result = _resolver.Resolve(candidate, targetWeeks);

        var byKey = result.Phases.ToDictionary(p => p.PhaseKey, p => p.AllocatedWeeks);
        Assert.Equal(p1Weeks, byKey[p1Key]);
        Assert.Equal(p2Weeks, byKey[p2Key]);
        Assert.Equal(p3Weeks, byKey[p3Key]);
        Assert.Equal(1, byKey["TAPER"]);
    }

    // ── Requirement 4: purity ────────────────────────────────────────────────

    [Theory]
    [InlineData(8)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task Resolve_CalledTwiceWithIdenticalInputs_ProducesByteIdenticalResults(int targetWeeks)
    {
        var candidate = await RealCandidateAsync();

        var first = _resolver.Resolve(candidate, targetWeeks);
        var second = _resolver.Resolve(candidate, targetWeeks);

        // PhaseAllocationResult is a record, so its auto-generated equality
        // compares Phases via the default IReadOnlyList<T> comparer, which is
        // reference equality (List<T> has no structural Equals) -- two
        // separately-built lists with identical contents are never
        // "record-equal" by that path. Compare the record's scalar fields
        // directly, and Phases element-wise.
        Assert.Equal(first.TargetWeeks, second.TargetWeeks);
        Assert.Equal(first.PreferredWeeks, second.PreferredWeeks);
        Assert.Equal(first.Delta, second.Delta);
        Assert.Equal(first.Mode, second.Mode);
        Assert.Equal(first.IsMathematicallyFeasible, second.IsMathematicallyFeasible);
        Assert.Equal(first.ReasonCode, second.ReasonCode);
        Assert.Equal(first.Phases, second.Phases);
    }

    // ── Requirement 5: infeasibility, actual computed range ─────────────────

    [Fact]
    public async Task Resolve_ActualFeasibleRange_IsEightToFourteen_MatchingCoreCycleBounds()
    {
        // Confirms the assumption this phase inherited from 4G.3A/4G.3B.1
        // rather than re-deriving it blind: sum(MinimumWeeks) and
        // sum(MaximumWeeks) for the real TEN_K_MASTER v6 phases equal the
        // candidate's own coreCycle.minimumWeeks/maximumWeeks (8/14) exactly
        // -- not a coincidence to assume, a fact to verify.
        var candidate = await RealCandidateAsync();
        var sumMin = candidate.PhaseAllocations.Sum(p => p.MinimumWeeks);
        var sumMax = candidate.PhaseAllocations.Sum(p => p.MaximumWeeks);

        Assert.Equal(8, sumMin);
        Assert.Equal(14, sumMax);
        Assert.Equal(candidate.CoreCycle.MinimumWeeks, sumMin);
        Assert.Equal(candidate.CoreCycle.MaximumWeeks, sumMax);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(0)]
    public async Task Resolve_TargetOutsideActualFeasibleRange_ReturnsInfeasible_NoException(int targetWeeks)
    {
        var candidate = await RealCandidateAsync();

        var result = _resolver.Resolve(candidate, targetWeeks);

        Assert.False(result.IsMathematicallyFeasible);
        Assert.Empty(result.Phases);
        Assert.NotEmpty(result.ReasonCode);
        Assert.Contains(targetWeeks < 8 ? "BELOW_SUM_OF_MINIMUMS" : "ABOVE_SUM_OF_MAXIMUMS", result.ReasonCode);
    }

    // ── Requirement 6: existing candidate-only method unchanged ──────────────

    [Fact]
    public async Task Resolve_CandidateOnlyMethod_StillProduces3_4_4_1_Unaffected()
    {
        var candidate = await RealCandidateAsync();

        var result = _resolver.Resolve(candidate);

        Assert.Equal(12, result.TotalWeeks);
        Assert.Equal(
            new[] { ("FOUNDATION", 3), ("BUILD", 4), ("RACE_SPECIFIC", 4), ("TAPER", 1) },
            result.Entries.Select(e => (e.PhaseKey, e.PhaseWeekCount)));
    }

    // ── Requirement 8: new overload unreachable from any live request path ──

    [Fact]
    public void Resolve_TargetWeekCountOverload_HasNoCallSiteOutsideTheOneApprovedDarkConsumer()
    {
        // Structural proof, not just an assertion: scans every .cs file under
        // RunningApp.Application and RunningApp.Api for a two-argument call
        // to a receiver named per this resolver's established field/variable
        // convention (`_phaseAllocationResolver`/`phaseAllocationResolver`,
        // confirmed by inspection -- CatalogPlanSkeletonOrchestrator is the
        // only *fixed-week* production consumer, and always calls the
        // candidate-only overload with exactly one argument). Narrower than
        // matching every `.Resolve(...)` call in the codebase, which also
        // matches unrelated resolvers (e.g. CatalogGoalDistanceResolver.Resolve,
        // GoalDistanceKm.Resolve) and would false-positive.
        //
        // Reconciled (Phase 4G.5D): the two-argument overload now has exactly
        // one legitimate call site, DynamicCoreWeekSkeletonOrchestrator.cs --
        // itself fully dark and unwired (zero production call sites of its
        // own, no DI registration; see
        // DynamicCoreWeekSkeletonOrchestratorTests.DarkReachability_NoProductionCallSite
        // and .DarkReachability_NoDiRegistration). The overload therefore
        // remains structurally unreachable from any LIVE request path, which
        // is this test's actual invariant -- it is no longer literally
        // zero-call-site, and the test/title were updated to say so rather
        // than silently widening the exclusion list without explanation.
        var applicationRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application");
        var apiRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Api");
        var twoArgPhaseAllocationResolveCall = new Regex(
            @"phaseAllocationResolver\.Resolve\([^()]*,[^()]*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        foreach (var root in new[] { applicationRoot, apiRoot })
        {
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    // Excludes the resolver's own definition file: its XML doc
                    // comments legitimately reference the new overload's full
                    // signature (e.g. <see cref="...Resolve(PlanCatalogCandidateSummary, int)"/>),
                    // which is not a call site.
                    && !f.EndsWith($"{Path.DirectorySeparatorChar}CatalogPhaseAllocation.cs", StringComparison.OrdinalIgnoreCase)
                    // Excludes the one approved dark consumer (Phase 4G.5D) --
                    // see the reconciliation note above.
                    && !f.EndsWith($"{Path.DirectorySeparatorChar}DynamicCoreWeekSkeletonOrchestrator.cs", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                Assert.False(twoArgPhaseAllocationResolveCall.IsMatch(content), $"Unexpected two-argument phase-allocation Resolve(...) call found in production file: {file}");
            }
        }
    }
}
