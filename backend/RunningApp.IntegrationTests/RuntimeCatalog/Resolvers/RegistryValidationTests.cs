using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4C — registry validation helper tests. Reads the
/// REAL plan-catalog runtime-condition-values registry (read-only, same
/// pattern as PlanCatalogBundleLoaderTests) to prove
/// RuntimeConditionRegistrySnapshot.IsValidValue matches actual repository
/// evidence, not a hardcoded assumption. In particular proves the Phase 4A.3
/// V1 scope decision at the code level: CONSERVATIVE/STRETCH are NOT valid
/// GOAL_FEASIBILITY_IN registry values, and a pace-recency confidence label
/// like "HIGH" is NOT a valid PACE_SOURCE_IN registry value.
/// </summary>
public sealed class RegistryValidationTests
{
    private static readonly PlanCatalogReference RegistryRef = new("RUNTIME_CONDITION_VALUES_V1", 2);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (expected a directory containing both 'backend' and 'plan-catalog').");
    }

    private static string RealCatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");

    private static IRuntimeConditionRegistryReader NewReader() =>
        new RuntimeConditionRegistryReader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = RealCatalogRoot() }),
            NullLogger<RuntimeConditionRegistryReader>.Instance);

    [Fact]
    public async Task LoadAsync_LoadsAllFiveConditionTypes_FromRealRegistryV2()
    {
        var snapshot = await NewReader().LoadAsync(RegistryRef);

        Assert.Equal(5, snapshot.AllowedValuesByConditionType.Count);
        Assert.True(snapshot.AllowedValuesByConditionType.ContainsKey("GOAL_FEASIBILITY_IN"));
        Assert.True(snapshot.AllowedValuesByConditionType.ContainsKey("PLAN_MODE_IN"));
        Assert.True(snapshot.AllowedValuesByConditionType.ContainsKey("PACE_SOURCE_IN"));
        Assert.True(snapshot.AllowedValuesByConditionType.ContainsKey("TIME_ADEQUACY_IN"));
        Assert.True(snapshot.AllowedValuesByConditionType.ContainsKey("CORE_ENTRY_READINESS_IN"));
    }

    [Theory]
    [InlineData("GOAL_FEASIBILITY_IN", "REALISTIC")]
    [InlineData("GOAL_FEASIBILITY_IN", "CHALLENGING")]
    [InlineData("GOAL_FEASIBILITY_IN", "UNSUPPORTED")]
    [InlineData("GOAL_FEASIBILITY_IN", "NOT_REQUESTED")]
    [InlineData("PACE_SOURCE_IN", "NONE")]
    [InlineData("PACE_SOURCE_IN", "RECENT_RACE")]
    [InlineData("PACE_SOURCE_IN", "ESTIMATED")]
    [InlineData("PACE_SOURCE_IN", "TARGET_TIME")]
    [InlineData("TIME_ADEQUACY_IN", "ADEQUATE")]
    [InlineData("TIME_ADEQUACY_IN", "COMPRESSED")]
    [InlineData("TIME_ADEQUACY_IN", "INSUFFICIENT")]
    [InlineData("CORE_ENTRY_READINESS_IN", "READY")]
    [InlineData("CORE_ENTRY_READINESS_IN", "CAUTION")]
    [InlineData("CORE_ENTRY_READINESS_IN", "NOT_READY")]
    [InlineData("PLAN_MODE_IN", "STANDARD")]
    [InlineData("PLAN_MODE_IN", "READINESS_ONLY")]
    public async Task IsValidValue_KnownRegistryValues_PassValidation(string conditionType, string value)
    {
        var snapshot = await NewReader().LoadAsync(RegistryRef);

        Assert.True(snapshot.IsValidValue(conditionType, value));
    }

    [Theory]
    [InlineData("GOAL_FEASIBILITY_IN", "CONSERVATIVE")]
    [InlineData("GOAL_FEASIBILITY_IN", "STRETCH")]
    [InlineData("GOAL_FEASIBILITY_IN", "CURRENTLY_UNSUPPORTED")]
    public async Task IsValidValue_AppselV1RicherGoalFeasibilityBands_AreNotValidRegistryValues(string conditionType, string value)
    {
        // Direct code-level proof of the Phase 4A.3 V1 scope decision: the
        // richer 5-class Appsel V1 model is NOT present in the actual
        // runtime-condition-values.v2.json registry as of this phase.
        var snapshot = await NewReader().LoadAsync(RegistryRef);

        Assert.False(snapshot.IsValidValue(conditionType, value));
    }

    [Theory]
    [InlineData("PACE_SOURCE_IN", "HIGH")]
    [InlineData("PACE_SOURCE_IN", "MODERATE")]
    [InlineData("PACE_SOURCE_IN", "LOW")]
    public async Task IsValidValue_PaceRecencyConfidenceLabels_AreNotValidPaceSourceRegistryValues(string conditionType, string value)
    {
        // Proves recency confidence must live in RuntimeConditionResolutionResult.Metadata
        // / ConfidenceLabel, never as PACE_SOURCE_IN's OutputValue -- "HIGH" is not
        // one of PACE_SOURCE_IN's actual registry-allowed values.
        var snapshot = await NewReader().LoadAsync(RegistryRef);

        Assert.False(snapshot.IsValidValue(conditionType, value));
    }

    [Fact]
    public async Task IsValidValue_UnknownConditionType_ReturnsFalse()
    {
        var snapshot = await NewReader().LoadAsync(RegistryRef);

        Assert.False(snapshot.IsValidValue("NOT_A_REAL_CONDITION_TYPE", "ANYTHING"));
    }

    [Fact]
    public async Task IsValidValue_CoreEntryReadiness_STANDARD_IsNotAValidRegistryValue()
    {
        // Direct code-level proof of TD-REGISTRY-001: the golden fixture's
        // "STANDARD" readiness output is not a valid CORE_ENTRY_READINESS_IN
        // registry value. This test does not close TD-REGISTRY-001 -- it only
        // demonstrates the validator correctly rejects it, exactly as
        // Phase 4A.2's investigation found.
        var snapshot = await NewReader().LoadAsync(RegistryRef);

        Assert.False(snapshot.IsValidValue("CORE_ENTRY_READINESS_IN", "STANDARD"));
        // But it IS a valid PLAN_MODE_IN value, per the same investigation.
        Assert.True(snapshot.IsValidValue("PLAN_MODE_IN", "STANDARD"));
    }
}
