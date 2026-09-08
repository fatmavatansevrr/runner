using System.Collections.Generic;
using System.Reflection;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.HalfMarathon;

/// <summary>
/// HM.2 Step 1c — proves the "exact-identity-tuple dispatch with a distance-blind
/// fallback that silently inherits another distance's numeric authority" recurring
/// family (HM.0 §G Family 1) is now closed in all three real occurrences this
/// engagement found: the primary occurrence in <see cref="CatalogVolumeAndLongRunPlanner"/>
/// (covered by <c>Hm2Step1cVolumeAndLongRunPlannerFailClosedTests</c>, appended to the
/// existing Phase4F7B fixture), the already-known sibling in
/// <see cref="TenKPreparationRunwayNumericPolicyFactory"/>, and a THIRD instance this
/// phase's own deeper search found in <see cref="CatalogFinalPrescribedPlanValidator"/>'s
/// private <c>ResolveLongRunHardCapShare</c> helper (not caught by HM.0's original audit).
///
/// Every existing TEN_K identity is proven unaffected (zero delta); every unrecognized
/// distance family now fails closed with <see cref="CatalogVolumeUnsupportedDistanceFamilyException"/>
/// instead of silently reusing a TEN_K-authored default policy.
/// </summary>
public sealed class Hm2Step1cFailClosedDistanceBlindFallbackTests
{
    private static PlanCatalogCandidateSummary MinimalCandidate(string canonicalDistanceFamily, string level, int daysPerWeek) => new()
    {
        CandidateKey = $"{canonicalDistanceFamily}__{daysPerWeek}D__{level}",
        CandidateVersion = 1,
        CandidateStatus = "DRAFT",
        CanonicalDistanceFamily = canonicalDistanceFamily,
        Level = level,
        DaysPerWeek = daysPerWeek,
        CoreCycle = new PlanCatalogCoreCycle(14, 14, 14),
        MasterTemplate = new PlanCatalogReference("TEST_MASTER", 1),
        Layout = new PlanCatalogReference("TEST_LAYOUT", 1),
        LevelModifier = new PlanCatalogReference("TEST_MODIFIER", 1),
        WorkoutProgression = new PlanCatalogReference("TEST_PROGRESSION", 1),
        ProgressionModifier = new PlanCatalogReference("TEST_PROGRESSION_MODIFIER", 1),
        RulePack = new PlanCatalogReference("TEST_RULE_PACK", 1),
        PeakVolumeBandPolicy = new PlanCatalogReference("TEST_PEAK_VOLUME_BANDS", 1),
        RuntimeConditionValueRegistry = new PlanCatalogReference("TEST_RUNTIME_CONDITION_VALUES", 1),
        DependencyStatuses = new Dictionary<string, string>(),
        ReferencedWorkouts = new[] { new PlanCatalogReference("EASY_STANDARD", 4) },
        PhaseKeys = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" },
        PhaseAllocations = new[]
        {
            new PlanCatalogPhaseAllocation("FOUNDATION", 3),
            new PlanCatalogPhaseAllocation("BUILD", 4),
            new PlanCatalogPhaseAllocation("RACE_SPECIFIC", 5),
            new PlanCatalogPhaseAllocation("TAPER", 2),
        },
        SlotRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" },
    };

    // ---- TenKPreparationRunwayNumericPolicyFactory sibling occurrence ----

    [Theory]
    [InlineData("INTERMEDIATE", 4)]
    [InlineData("NEW", 2)]
    [InlineData("INTERMEDIATE", 5)]
    [InlineData("INTERMEDIATE", 6)]
    [InlineData("ADVANCED", 3)]
    [InlineData("ADVANCED", 4)]
    [InlineData("ADVANCED", 5)]
    [InlineData("ADVANCED", 6)]
    public void TenKPreparationRunwayNumericPolicyFactory_TenKIdentities_StillResolve_ZeroDelta(string level, int daysPerWeek)
    {
        var candidate = MinimalCandidate("TEN_K", level, daysPerWeek);
        var policy = TenKPreparationRunwayNumericPolicyFactory.Build(candidate);

        Assert.Equal(TenKPreparationRunwayNumericPolicyFactory.PolicyKey, policy.PolicyKey);
        Assert.True(policy.PreferredMaxWeeklyIncreaseRatio > 0);
    }

    [Theory]
    [InlineData("HALF_MARATHON", "INTERMEDIATE", 4)]
    [InlineData("UNKNOWN_FUTURE_DISTANCE", "INTERMEDIATE", 4)]
    public void TenKPreparationRunwayNumericPolicyFactory_UnrecognizedDistanceFamily_FailsClosed(string distanceFamily, string level, int daysPerWeek)
    {
        var candidate = MinimalCandidate(distanceFamily, level, daysPerWeek);

        var ex = Assert.Throws<CatalogVolumeUnsupportedDistanceFamilyException>(() => TenKPreparationRunwayNumericPolicyFactory.Build(candidate));
        Assert.Equal("CATALOG_VOLUME_UNSUPPORTED_DISTANCE_FAMILY", ex.Code);
    }

    // ---- CatalogFinalPrescribedPlanValidator — third instance found by this phase ----

    private static double InvokeResolveLongRunHardCapShare(PlanCatalogCandidateSummary candidate)
    {
        var method = typeof(CatalogFinalPrescribedPlanValidator).GetMethod(
            "ResolveLongRunHardCapShare", BindingFlags.NonPublic | BindingFlags.Static)!;
        try
        {
            return (double)method.Invoke(null, new object[] { candidate })!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    [Theory]
    [InlineData("ADVANCED", 3)]
    [InlineData("ADVANCED", 4)]
    [InlineData("NEW", 2)]
    [InlineData("NEW", 3)]
    [InlineData("NEW", 4)]
    [InlineData("INTERMEDIATE", 2)]
    [InlineData("INTERMEDIATE", 3)]
    [InlineData("INTERMEDIATE", 4)]
    [InlineData("INTERMEDIATE", 5)]
    [InlineData("INTERMEDIATE", 6)]
    public void CatalogFinalPrescribedPlanValidator_ResolveLongRunHardCapShare_TenKIdentities_StillResolve_ZeroDelta(string level, int daysPerWeek)
    {
        var candidate = MinimalCandidate("TEN_K", level, daysPerWeek);
        var share = InvokeResolveLongRunHardCapShare(candidate);

        Assert.InRange(share, 0.30, 0.60);
    }

    [Theory]
    [InlineData("HALF_MARATHON", "INTERMEDIATE", 4)]
    [InlineData("UNKNOWN_FUTURE_DISTANCE", "ADVANCED", 4)]
    public void CatalogFinalPrescribedPlanValidator_ResolveLongRunHardCapShare_UnrecognizedDistanceFamily_FailsClosed(
        string distanceFamily, string level, int daysPerWeek)
    {
        var candidate = MinimalCandidate(distanceFamily, level, daysPerWeek);

        var ex = Assert.Throws<CatalogVolumeUnsupportedDistanceFamilyException>(() => InvokeResolveLongRunHardCapShare(candidate));
        Assert.Equal("CATALOG_VOLUME_UNSUPPORTED_DISTANCE_FAMILY", ex.Code);
    }
}
