using System;
using System.Collections.Generic;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.HalfMarathon;

/// <summary>
/// HM.2 Step 1a/1b — behavior-preservation proof for the additive distance-parameter
/// threading through <see cref="V1CatalogPilotIdentityPolicy"/> (Step 1a) and the
/// duplicate-authority resolution in <see cref="CatalogGoalDistanceResolver"/> (Step 1b).
///
/// Shape follows HM.0 §M's own "CURRENT INPUT -> current authority -> current
/// branch/value" vs "AFTER GENERALIZATION -> same authority -> same branch -> same
/// output" proof structure: every existing TEN_K 2-arg call site is proven
/// byte-identical to the new 3-arg (distance-aware) overload evaluated with
/// GoalDistance.TenK pinned, and the public/Runway gates (IsSupportedIdentity /
/// IsSupportedPreparationRunwayIdentity), which HM.2 explicitly does not touch, are
/// proven to still reject HALF_MARATHON for every existing cell.
/// </summary>
public sealed class Hm2Step1IdentityAndGoalDistanceZeroDeltaTests
{
    public static IEnumerable<object[]> AllExistingTenKLevelFrequencyPairs()
    {
        yield return new object[] { RunningBackground.Intermediate, 3, V1CatalogPilotIdentityPolicy.ThreeDayCandidateKey, V1CatalogPilotIdentityPolicy.ThreeDayCandidateVersion };
        yield return new object[] { RunningBackground.Intermediate, 4, V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion };
        yield return new object[] { RunningBackground.Intermediate, 5, V1CatalogPilotIdentityPolicy.FiveDayCandidateKey, V1CatalogPilotIdentityPolicy.FiveDayCandidateVersion };
        yield return new object[] { RunningBackground.Intermediate, 6, V1CatalogPilotIdentityPolicy.SixDayCandidateKey, V1CatalogPilotIdentityPolicy.SixDayCandidateVersion };
        yield return new object[] { RunningBackground.Beginner, 4, V1CatalogPilotIdentityPolicy.BeginnerCandidateKey, V1CatalogPilotIdentityPolicy.BeginnerCandidateVersion };
        yield return new object[] { RunningBackground.Advanced, 3, V1CatalogPilotIdentityPolicy.AdvancedThreeDayCandidateKey, V1CatalogPilotIdentityPolicy.AdvancedThreeDayCandidateVersion };
        yield return new object[] { RunningBackground.Advanced, 4, V1CatalogPilotIdentityPolicy.AdvancedFourDayCandidateKey, V1CatalogPilotIdentityPolicy.AdvancedFourDayCandidateVersion };
        yield return new object[] { RunningBackground.Advanced, 5, V1CatalogPilotIdentityPolicy.AdvancedFiveDayCandidateKey, V1CatalogPilotIdentityPolicy.AdvancedFiveDayCandidateVersion };
        yield return new object[] { RunningBackground.Advanced, 6, V1CatalogPilotIdentityPolicy.AdvancedSixDayCandidateKey, V1CatalogPilotIdentityPolicy.AdvancedSixDayCandidateVersion };
        yield return new object[] { RunningBackground.Beginner, 2, V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateVersion };
        yield return new object[] { RunningBackground.Intermediate, 2, V1CatalogPilotIdentityPolicy.TwoDayIntermediateCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayIntermediateCandidateVersion };
        yield return new object[] { RunningBackground.Beginner, 3, V1CatalogPilotIdentityPolicy.ThreeDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.ThreeDayBeginnerCandidateVersion };
    }

    [Theory]
    [MemberData(nameof(AllExistingTenKLevelFrequencyPairs))]
    public void ResolveCandidate_TwoArgOverload_ByteIdenticalTo_ThreeArgTenKOverload(
        RunningBackground level, int daysPerWeek, string expectedKey, int expectedVersion)
    {
        var twoArg = V1CatalogPilotIdentityPolicy.ResolveCandidate(level, daysPerWeek);
        var threeArg = V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.TenK, level, daysPerWeek);

        Assert.Equal((expectedKey, expectedVersion), twoArg);
        Assert.Equal(twoArg, threeArg);
    }

    [Theory]
    [MemberData(nameof(AllExistingTenKLevelFrequencyPairs))]
    public void TryResolveCandidate_TwoArgOverload_ByteIdenticalTo_ThreeArgTenKOverload(
        RunningBackground level, int daysPerWeek, string expectedKey, int expectedVersion)
    {
        var twoArg = V1CatalogPilotIdentityPolicy.TryResolveCandidate(level, daysPerWeek);
        var threeArg = V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.TenK, level, daysPerWeek);

        Assert.Equal((expectedKey, expectedVersion), twoArg);
        Assert.Equal(twoArg, threeArg);
    }

    [Theory]
    [InlineData(RunningBackground.Beginner, 5)]
    [InlineData(RunningBackground.Beginner, 6)]
    [InlineData(RunningBackground.Intermediate, 7)]
    [InlineData(RunningBackground.Advanced, 2)]
    public void ResolveCandidate_UnsupportedTenKCombination_StillThrows_UnaffectedByDistanceWidening(RunningBackground level, int daysPerWeek)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => V1CatalogPilotIdentityPolicy.ResolveCandidate(level, daysPerWeek));
        Assert.Throws<ArgumentOutOfRangeException>(() => V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.TenK, level, daysPerWeek));
    }

    [Fact]
    public void HalfMarathon_IntermediateFourDay_ResolvesDarkOnly_ThroughDistanceAwareOverloadOnly()
    {
        var resolved = V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        Assert.Equal(
            (V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateVersion),
            resolved);

        var tryResolved = V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        Assert.Equal(resolved, tryResolved);
    }

    // HM.8 -- HALF_MARATHON Intermediate x3D is now a second frozen, dark-only resolvable
    // cell (HM.7's numeric authority). (Intermediate, 3) removed from this "not resolvable"
    // theory below and covered instead by its own HalfMarathon_IntermediateThreeDay_ResolvesDarkOnly
    // test, mirroring HalfMarathon_IntermediateFourDay_ResolvesDarkOnly_ThroughDistanceAwareOverloadOnly.
    [Theory]
    [InlineData(RunningBackground.Intermediate, 5)]
    [InlineData(RunningBackground.Beginner, 4)]
    [InlineData(RunningBackground.Advanced, 4)]
    public void HalfMarathon_AnyOtherLevelFrequency_IsNotResolvable_OnlyTheTwoFrozenCellsAreAdmitted(RunningBackground level, int daysPerWeek)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, level, daysPerWeek));
        Assert.Null(V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.HalfMarathon, level, daysPerWeek));
    }

    [Fact]
    public void HalfMarathon_IntermediateThreeDay_ResolvesDarkOnly_ThroughDistanceAwareOverloadOnly()
    {
        var resolved = V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3);
        Assert.Equal(
            (V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateVersion),
            resolved);

        var tryResolved = V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3);
        Assert.Equal(resolved, tryResolved);
    }

    [Theory]
    [MemberData(nameof(AllExistingTenKLevelFrequencyPairs))]
    public void PublicGate_IsSupportedIdentity_TenKUnaffected_HalfMarathonStillRejected(
        RunningBackground level, int daysPerWeek, string expectedKey, int expectedVersion)
    {
        _ = expectedKey;
        _ = expectedVersion;

        // TEN_K gate byte-identical (still admits exactly what it did before this phase).
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, level, daysPerWeek));

        // The public gate must remain closed to HALF_MARATHON -- HM.2 explicitly does not widen it.
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.HalfMarathon, level, daysPerWeek));
    }

    [Fact]
    public void PublicGate_IsSupportedIdentity_RejectsTheOneCellHm2DarklyResolves()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
    }

    // HM.8 -- the new dark-only 3D cell must also remain outside every public/Runway gate.
    [Fact]
    public void PublicGate_IsSupportedIdentity_RejectsTheHm8DarkCellToo()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3));
    }

    [Fact]
    public void PreparationRunwayGate_IsSupportedPreparationRunwayIdentity_TenKUnaffected_HalfMarathonRejected()
    {
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 4));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
    }

    // ---- 1b: CatalogGoalDistanceResolver duplicate-authority resolution ----

    [Fact]
    public void CatalogGoalDistanceResolver_TenK_ByteIdenticalToPriorConstant_AndMatchesGoalDistanceKm()
    {
        var resolved = CatalogGoalDistanceResolver.Resolve("TEN_K", GoalDistance.TenK, 10d);

        Assert.Equal(10d, resolved);
        Assert.Equal(GoalDistanceKm.Resolve(GoalDistance.TenK), resolved);
    }

    [Fact]
    public void CatalogGoalDistanceResolver_HalfMarathon_NewAdditiveArm_MatchesGoalDistanceKmSoleAuthority()
    {
        var resolved = CatalogGoalDistanceResolver.Resolve("HALF_MARATHON", GoalDistance.HalfMarathon, 21.0975d);

        Assert.Equal(GoalDistanceKm.Resolve(GoalDistance.HalfMarathon), resolved);
        Assert.Equal(21.0975d, resolved);
    }

    [Fact]
    public void CatalogGoalDistanceResolver_UnsupportedCatalogFamily_StillThrows_ZeroDelta()
    {
        var ex = Assert.Throws<CatalogPrescriptionContractException>(() =>
            CatalogGoalDistanceResolver.Resolve("MARATHON", GoalDistance.Marathon));
        Assert.Equal("UNSUPPORTED_CATALOG_GOAL_DISTANCE", ex.Code);
    }

    [Fact]
    public void CatalogGoalDistanceResolver_CrossDistanceMismatch_StillThrows_ZeroDelta()
    {
        var ex = Assert.Throws<CatalogPrescriptionContractException>(() =>
            CatalogGoalDistanceResolver.Resolve("TEN_K", GoalDistance.HalfMarathon));
        Assert.Equal("GOAL_DISTANCE_REQUEST_CATALOG_MISMATCH", ex.Code);
    }
}
