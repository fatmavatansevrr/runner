using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.3 — tests for
/// <see cref="CatalogPhaseAllocationResolver"/>. Fixtures are hand-built
/// <see cref="PlanCatalogCandidateSummary"/> instances (never live HTTP/DB) —
/// see <see cref="CatalogPlanSkeletonOrchestratorTests"/> for the real,
/// catalog-file-backed end-to-end coverage.
/// </summary>
public sealed class CatalogPhaseAllocationResolverTests
{
    private readonly CatalogPhaseAllocationResolver _resolver = new();

    private static PlanCatalogCandidateSummary Candidate(params PlanCatalogPhaseAllocation[] allocations) =>
        CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate(phaseAllocations: allocations);

    [Fact]
    public void Resolve_PilotMasterTemplateShape_Resolves3_4_4_1()
    {
        var candidate = Candidate(
            new("FOUNDATION", 3), new("BUILD", 4), new("RACE_SPECIFIC", 4), new("TAPER", 1));

        var result = _resolver.Resolve(candidate);

        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, result.Entries.Select(e => e.PhaseKey));
        Assert.Equal(new[] { 3, 4, 4, 1 }, result.Entries.Select(e => e.PhaseWeekCount));
    }

    [Fact]
    public void Resolve_AllocationTotal_Resolves12()
    {
        var candidate = Candidate(
            new("FOUNDATION", 3), new("BUILD", 4), new("RACE_SPECIFIC", 4), new("TAPER", 1));

        var result = _resolver.Resolve(candidate);

        Assert.Equal(12, result.TotalWeeks);
    }

    [Fact]
    public void Resolve_Total_MatchesCoreCycleDefaultWeeks()
    {
        var candidate = Candidate(
            new("FOUNDATION", 3), new("BUILD", 4), new("RACE_SPECIFIC", 4), new("TAPER", 1));

        var result = _resolver.Resolve(candidate);

        Assert.Equal(candidate.CoreCycle.DefaultWeeks, result.TotalWeeks);
    }

    [Fact]
    public void Resolve_AllocationOrder_IsPreserved()
    {
        var candidate = Candidate(
            new("FOUNDATION", 3), new("BUILD", 4), new("RACE_SPECIFIC", 4), new("TAPER", 1));

        var result = _resolver.Resolve(candidate);

        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, result.Entries.Select(e => e.PhaseKey));
    }

    [Fact]
    public void Resolve_DuplicatePhaseKeys_AreRejected()
    {
        var candidate = Candidate(new("FOUNDATION", 3), new("FOUNDATION", 3));

        Assert.Throws<CatalogPhaseAllocationInvalidException>(() => _resolver.Resolve(candidate));
    }

    [Fact]
    public void Resolve_MissingPhaseAllocation_IsRejected()
    {
        var candidate = Candidate(); // empty

        Assert.Throws<CatalogPhaseAllocationSourceMissingException>(() => _resolver.Resolve(candidate));
    }

    [Fact]
    public void Resolve_ZeroWeekCount_IsRejected()
    {
        var candidate = Candidate(new PlanCatalogPhaseAllocation("FOUNDATION", 0));

        Assert.Throws<CatalogPhaseAllocationInvalidException>(() => _resolver.Resolve(candidate));
    }

    [Fact]
    public void Resolve_NegativeWeekCount_IsRejected()
    {
        var candidate = Candidate(new PlanCatalogPhaseAllocation("FOUNDATION", -1));

        Assert.Throws<CatalogPhaseAllocationInvalidException>(() => _resolver.Resolve(candidate));
    }

    [Fact]
    public void Resolve_BlankPhaseKey_IsRejected()
    {
        var candidate = Candidate(new PlanCatalogPhaseAllocation("   ", 3));

        Assert.Throws<CatalogPhaseAllocationInvalidException>(() => _resolver.Resolve(candidate));
    }

    [Fact]
    public void Resolve_TotalMismatch_IsDetectableByCaller_NotSilentlyNormalized()
    {
        // The resolver itself reports the total honestly; total-vs-authority
        // mismatch is the *orchestrator's* job (see
        // CatalogPlanSkeletonOrchestratorTests) -- this test proves the
        // resolver never adjusts its own reported total to match anything.
        var candidate = Candidate(new("FOUNDATION", 3), new("BUILD", 4)); // sums to 7, candidate.CoreCycle.DefaultWeeks is still 12

        var result = _resolver.Resolve(candidate);

        Assert.Equal(7, result.TotalWeeks);
        Assert.NotEqual(candidate.CoreCycle.DefaultWeeks, result.TotalWeeks);
    }

    [Fact]
    public void Resolve_NoSilentRedistribution_ShortAllocationStaysShort()
    {
        var candidate = Candidate(new("FOUNDATION", 2), new("BUILD", 4), new("RACE_SPECIFIC", 4), new("TAPER", 1)); // 11, not 12

        var result = _resolver.Resolve(candidate);

        Assert.Equal(11, result.TotalWeeks);
        Assert.Equal(2, result.Entries.First(e => e.PhaseKey == "FOUNDATION").PhaseWeekCount);
    }

    [Fact]
    public void CatalogPhaseAllocationResolver_ContainsNoHardcodedPilotConstant()
    {
        // Structural proof: feeding a completely different (non-pilot) allocation
        // shape produces exactly that shape back -- proving no 3/4/4/1 constant
        // is baked into the resolver.
        var candidate = Candidate(new("BASE", 5), new("PEAK", 6), new("TAPER", 2));

        var result = _resolver.Resolve(candidate);

        Assert.Equal(new[] { "BASE", "PEAK", "TAPER" }, result.Entries.Select(e => e.PhaseKey));
        Assert.Equal(new[] { 5, 6, 2 }, result.Entries.Select(e => e.PhaseWeekCount));
        Assert.Equal(13, result.TotalWeeks);
    }
}
