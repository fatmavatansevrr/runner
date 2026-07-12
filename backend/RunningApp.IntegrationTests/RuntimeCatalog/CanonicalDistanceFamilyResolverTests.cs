using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog;

/// <summary>
/// Backend Integration Phase 1 — runtime distance-family resolver. Proves the
/// resolver selects the correct internal canonical family while always
/// preserving the user's exact requested distance separately (never
/// rounding, never redirecting the visible goal, never treating this as
/// nearest-template fallback).
/// </summary>
public sealed class CanonicalDistanceFamilyResolverTests
{
    private readonly ICanonicalDistanceFamilyResolver _resolver = new CanonicalDistanceFamilyResolver();

    [Theory]
    [InlineData(5.0, GoalDistance.FiveK)]
    [InlineData(8.0, GoalDistance.TenK)]
    [InlineData(10.0, GoalDistance.TenK)]
    [InlineData(15.0, GoalDistance.HalfMarathon)]
    [InlineData(21.1, GoalDistance.HalfMarathon)]
    [InlineData(30.0, GoalDistance.Marathon)]
    [InlineData(42.2, GoalDistance.Marathon)]
    public void Resolve_MapsToExpectedFamily_AndPreservesExactRequestedDistance(double requestedKm, GoalDistance expectedFamily)
    {
        var result = _resolver.Resolve(requestedKm);

        Assert.Equal(expectedFamily, result.CanonicalDistanceFamily);
        Assert.Equal(requestedKm, result.RequestedTargetDistanceKm);
    }

    [Fact]
    public void Resolve_EightKm_DoesNotRoundToFiveK_AndDoesNotRedirectToTenKGoal()
    {
        // The critical "custom distance, not silent fallback" scenario from the task's own example.
        var result = _resolver.Resolve(8.0);

        Assert.Equal(GoalDistance.TenK, result.CanonicalDistanceFamily);
        Assert.NotEqual(GoalDistance.FiveK, result.CanonicalDistanceFamily);
        Assert.Equal(8.0, result.RequestedTargetDistanceKm); // the visible goal stays 8K, not rounded to 10K
    }

    [Fact]
    public void Resolve_FifteenKm_MapsToHalfMarathonFamily_ButUserFacingGoalStaysFifteen()
    {
        var result = _resolver.Resolve(15.0);

        Assert.Equal(GoalDistance.HalfMarathon, result.CanonicalDistanceFamily);
        Assert.Equal(15.0, result.RequestedTargetDistanceKm);
        Assert.NotEqual(21.1, result.RequestedTargetDistanceKm);
    }

    [Fact]
    public void Resolve_ThirtyKm_MapsToMarathonFamily_ButUserFacingGoalStaysThirty()
    {
        var result = _resolver.Resolve(30.0);

        Assert.Equal(GoalDistance.Marathon, result.CanonicalDistanceFamily);
        Assert.Equal(30.0, result.RequestedTargetDistanceKm);
        Assert.NotEqual(42.2, result.RequestedTargetDistanceKm);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(-5.0)]
    public void Resolve_ZeroOrNegativeDistance_ThrowsUnsupportedTargetDistanceException(double invalidKm)
    {
        Assert.Throws<UnsupportedTargetDistanceException>(() => _resolver.Resolve(invalidKm));
    }

    [Theory]
    [InlineData(42.3)]
    [InlineData(100.0)]
    public void Resolve_AboveSupportedRange_ThrowsUnsupportedTargetDistanceException(double tooFarKm)
    {
        Assert.Throws<UnsupportedTargetDistanceException>(() => _resolver.Resolve(tooFarKm));
    }
}
