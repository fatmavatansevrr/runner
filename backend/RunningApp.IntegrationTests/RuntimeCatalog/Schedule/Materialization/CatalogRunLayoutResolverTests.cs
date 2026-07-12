using System.Linq;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>Backend Integration Phase 4F.3 — tests for <see cref="CatalogRunLayoutResolver"/>.</summary>
public sealed class CatalogRunLayoutResolverTests
{
    private readonly CatalogRunLayoutResolver _resolver = new();

    [Fact]
    public void Resolve_PilotLayout_ResolvesFourSlotsFromLoadedCatalogData()
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate();

        var result = _resolver.Resolve(candidate);

        Assert.Equal(4, result.StructuralRoles.Count);
    }

    [Fact]
    public void Resolve_SlotOrder_MatchesCatalog()
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate();

        var result = _resolver.Resolve(candidate);

        Assert.Equal(new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" }, result.StructuralRoles);
    }

    [Fact]
    public void Resolve_RoleCounts_MatchAcceptedPilotLayout()
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate();

        var result = _resolver.Resolve(candidate);
        var counts = result.StructuralRoles.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(1, counts["KEY_SESSION"]);
        Assert.Equal(2, counts["EASY_SUPPORT"]);
        Assert.Equal(1, counts["LONG_RUN"]);
    }

    [Fact]
    public void Resolve_ReturnsExactLayoutIdentity()
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate();

        var result = _resolver.Resolve(candidate);

        Assert.Equal("RUN_LAYOUT_4D", result.Layout.Key);
        Assert.Equal(2, result.Layout.Version);
    }

    [Fact]
    public void Resolve_SlotCountMismatch_IsRejected()
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate(
            slotRoles: new[] { "KEY_SESSION", "EASY_SUPPORT" }, daysPerWeek: 4); // only 2 roles, DaysPerWeek says 4

        Assert.Throws<CatalogRunLayoutSlotInvalidException>(() => _resolver.Resolve(candidate));
    }

    [Fact]
    public void Resolve_UnknownBlankRole_IsRejected()
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate(
            slotRoles: new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "   " });

        Assert.Throws<CatalogRunLayoutSlotInvalidException>(() => _resolver.Resolve(candidate));
    }

    [Fact]
    public void Resolve_RestRole_IsRejected()
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate(
            slotRoles: new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "REST" });

        Assert.Throws<CatalogRunLayoutSlotInvalidException>(() => _resolver.Resolve(candidate));
    }

    [Fact]
    public void Resolve_OptionalRole_IsRejected()
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate(
            slotRoles: new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "OPTIONAL_LONG_RUN" });

        Assert.Throws<CatalogRunLayoutSlotInvalidException>(() => _resolver.Resolve(candidate));
    }

    [Fact]
    public void Resolve_RecoveryRole_IsRejected()
    {
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate(
            slotRoles: new[] { "KEY_SESSION", "EASY_SUPPORT", "RECOVERY_JOG", "LONG_RUN" });

        Assert.Throws<CatalogRunLayoutSlotInvalidException>(() => _resolver.Resolve(candidate));
    }

    [Fact]
    public void CatalogRunLayoutResolver_ContainsNoHardcodedLayoutArray()
    {
        // Structural proof: feeding a completely different (non-pilot) layout
        // shape produces exactly that shape back.
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate(
            slotRoles: new[] { "A", "B", "C", "D", "E" }, daysPerWeek: 5);

        var result = _resolver.Resolve(candidate);

        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, result.StructuralRoles);
    }
}
