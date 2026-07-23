using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.3.1 — CoreEntryReadinessResolver tests.
/// Proves the owner-approved V1 READY/CAUTION/NOT_READY threshold mapping
/// (weekly volume + longest run only; RecentRunsPerWeek metadata-only) and
/// re-proves the registry contract (READY/CAUTION/NOT_READY valid, STANDARD
/// invalid). No TrainingWeek/TrainingDay is created; no resolver output is
/// wired into generation.
/// </summary>
public sealed class CoreEntryReadinessResolverTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root.");
    }

    private static IRuntimeConditionRegistryReader NewRegistryReader() =>
        new RuntimeConditionRegistryReader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<RuntimeConditionRegistryReader>.Instance);

    private static readonly PlanCatalogReference RegistryRef = new("RUNTIME_CONDITION_VALUES_V1", 2);

    private static RuntimeResolverContext Context(ResolverInputSnapshot input) => new() { InputSnapshot = input };

    // ─── Interface implementation ───────────────────────────────────────────

    [Fact]
    public void CoreEntryReadinessResolver_ImplementsICoreEntryReadinessResolver()
    {
        Assert.IsAssignableFrom<ICoreEntryReadinessResolver>(new CoreEntryReadinessResolver());
    }

    [Fact]
    public void CoreEntryReadinessResolver_ConditionTypeIsCoreEntryReadinessIn()
    {
        Assert.Equal("CORE_ENTRY_READINESS_IN", new CoreEntryReadinessResolver().ConditionType);
    }

    [Fact]
    public void CoreEntryReadinessResolver_DoesNotRequireCoreCycle()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 20, RecentLongestRunKm = 8 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
    }

    // ─── A. READY ────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WeeklyGE15AndLongestGE6_ReturnsReady()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 15, RecentLongestRunKm = 6 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("READY", result.OutputValue);
        Assert.Equal("CORE_ENTRY_READY", result.ReasonCode);
    }

    [Fact]
    public void Resolve_WellAboveThresholds_ReturnsReady()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 40, RecentLongestRunKm = 18 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("READY", result.OutputValue);
    }

    [Fact]
    public void Resolve_ReadyEvidence_LowRecentRunsPerWeek_StillReady_NotHardGated()
    {
        // RecentRunsPerWeek is metadata-only -- never a hard gate.
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 20, RecentLongestRunKm = 8, RecentRunsPerWeek = 1 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("READY", result.OutputValue);
        Assert.Equal("1", result.Metadata["recentRunsPerWeek"]);
    }

    // ─── B. CAUTION ──────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_Weekly14_9_LongestGE6_ReturnsCaution()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 14.9, RecentLongestRunKm = 6 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("CAUTION", result.OutputValue);
        Assert.Equal("CORE_ENTRY_CAUTION", result.ReasonCode);
    }

    [Fact]
    public void Resolve_WeeklyGE15_Longest5_9_ReturnsCaution()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 15, RecentLongestRunKm = 5.9 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("CAUTION", result.OutputValue);
    }

    [Fact]
    public void Resolve_Weekly8_Longest6_ReturnsCaution()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 8, RecentLongestRunKm = 6 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("CAUTION", result.OutputValue);
    }

    [Fact]
    public void Resolve_Weekly15_Longest4_ReturnsCaution()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 15, RecentLongestRunKm = 4 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("CAUTION", result.OutputValue);
    }

    [Fact]
    public void Resolve_OneFieldMissing_OtherStrong_ReturnsCaution()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 40, RecentLongestRunKm = null };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("CAUTION", result.OutputValue);
        Assert.Equal("CORE_ENTRY_CAUTION", result.ReasonCode);
    }

    [Fact]
    public void Resolve_LongestPresentWeeklyMissing_ReturnsCaution()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = null, RecentLongestRunKm = 10 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("CAUTION", result.OutputValue);
    }

    [Fact]
    public void Resolve_CautionResult_MetadataIncludesTriggeredCriterion()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 10, RecentLongestRunKm = 6 };

        var result = resolver.Resolve(Context(input));

        Assert.True(result.Metadata.ContainsKey("triggeredCriterion"));
    }

    // ─── C. NOT_READY ────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WeeklyBelow8_ReturnsNotReady()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 5, RecentLongestRunKm = 10 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("NOT_READY", result.OutputValue);
        Assert.Equal("CORE_ENTRY_NOT_READY", result.ReasonCode);
    }

    [Fact]
    public void Resolve_LongestBelow4_ReturnsNotReady()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 20, RecentLongestRunKm = 2 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("NOT_READY", result.OutputValue);
    }

    [Fact]
    public void Resolve_NotReadyResult_MetadataIncludesTriggeredCriterion()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 3, RecentLongestRunKm = 10 };

        var result = resolver.Resolve(Context(input));

        Assert.True(result.Metadata.ContainsKey("triggeredCriterion"));
    }

    [Fact]
    public void Resolve_BothMissing_RaceContext_ReturnsNotReady()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { GoalType = GoalType.Race, RecentWeeklyVolumeKm = null, RecentLongestRunKm = null };

        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("NOT_READY", result.OutputValue);
        Assert.Equal("CORE_ENTRY_NOT_READY", result.ReasonCode);
    }

    // ─── D. NotEvaluated ─────────────────────────────────────────────────────

    [Fact]
    public void Resolve_BothMissing_HabitContext_ReturnsNotEvaluated()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { GoalType = GoalType.Habit, RecentWeeklyVolumeKm = null, RecentLongestRunKm = null };

        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Null(result.OutputValue);
        Assert.Equal("CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT", result.ReasonCode);
    }

    [Fact]
    public void Resolve_BothMissing_UnknownGoalType_ReturnsNotEvaluated_ConservativeChoice()
    {
        // Documented conservative choice: cannot assume race context without evidence.
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { GoalType = null, RecentWeeklyVolumeKm = null, RecentLongestRunKm = null };

        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Null(result.OutputValue);
        Assert.Equal("CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT", result.ReasonCode);
    }

    [Fact]
    public void NotEvaluated_IsDistinctFromNotReady()
    {
        var resolver = new CoreEntryReadinessResolver();
        var notEvaluated = resolver.Resolve(Context(new ResolverInputSnapshot { GoalType = GoalType.Habit }));
        var notReady = resolver.Resolve(Context(new ResolverInputSnapshot { GoalType = GoalType.Race, RecentWeeklyVolumeKm = 3, RecentLongestRunKm = 2 }));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, notEvaluated.Status);
        Assert.Null(notEvaluated.OutputValue);

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, notReady.Status);
        Assert.Equal("NOT_READY", notReady.OutputValue);
    }

    [Fact]
    public void ExplicitZero_IsDistinctFromMissing_AndExplicitlyEvaluated()
    {
        // Explicit zero readiness (both fields reported as 0) is real evidence
        // — it must be Evaluated/NOT_READY (0 < the NOT_READY thresholds), not
        // collapsed into the "both missing" NotEvaluated/race-NOT_READY path.
        var resolver = new CoreEntryReadinessResolver();
        var explicitZero = resolver.Resolve(Context(new ResolverInputSnapshot
        {
            GoalType = GoalType.Race,
            RecentWeeklyVolumeKm = 0,
            RecentLongestRunKm = 0,
        }));
        var missing = resolver.Resolve(Context(new ResolverInputSnapshot
        {
            GoalType = GoalType.Race,
            RecentWeeklyVolumeKm = null,
            RecentLongestRunKm = null,
        }));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, explicitZero.Status);
        Assert.Equal("NOT_READY", explicitZero.OutputValue);
        Assert.Equal("CORE_ENTRY_NOT_READY", explicitZero.ReasonCode);

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, missing.Status);
        Assert.Equal("NOT_READY", missing.OutputValue);
        Assert.Equal("CORE_ENTRY_NOT_READY", missing.ReasonCode);

        // Both land on NOT_READY for a race plan (by different documented rules),
        // but the resolver reaches them via distinguishable code paths/metadata,
        // not by silently treating 0 as the same "no evidence" as null.
        Assert.Equal("Both RecentWeeklyVolumeKm and RecentLongestRunKm missing in a race-based performance plan context", missing.Metadata!["triggeredCriterion"]);
        Assert.NotEqual(missing.Metadata!["triggeredCriterion"], explicitZero.Metadata!["triggeredCriterion"]);
    }

    // ─── STANDARD anomaly ────────────────────────────────────────────────────

    [Fact]
    public void Resolve_NeverEmitsStandard_EvenWithGoldenFixtureMatchingEvidence()
    {
        var resolver = new CoreEntryReadinessResolver();
        // Evidence matching the golden fixture's own TEN_K_STANDARD_ENTRY
        // facts exactly (weeklyVolumeKm=24, longestRunKm=9).
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 24, RecentLongestRunKm = 9, RecentRunsPerWeek = 4 };

        var result = resolver.Resolve(Context(input));

        Assert.NotEqual("STANDARD", result.OutputValue);
        Assert.Equal("READY", result.OutputValue); // 24>=15, 9>=6
    }

    // ─── Validation layering ─────────────────────────────────────────────────

    [Fact]
    public void Resolve_DoesNotThrowOnValidPositiveNumericInput()
    {
        var resolver = new CoreEntryReadinessResolver();
        var input = new ResolverInputSnapshot { RecentWeeklyVolumeKm = 0.1, RecentLongestRunKm = 0.1, RecentRunsPerWeek = 1 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("NOT_READY", result.OutputValue); // both below NOT_READY thresholds
    }

    // ─── Registry validation ─────────────────────────────────────────────────

    [Theory]
    [InlineData("READY")]
    [InlineData("CAUTION")]
    [InlineData("NOT_READY")]
    public async Task RegistryValidation_ThreeCoreEntryReadinessValues_AreRegistryValid(string value)
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.True(snapshot.IsValidValue("CORE_ENTRY_READINESS_IN", value));
    }

    [Fact]
    public async Task RegistryValidation_Standard_IsNotValidCoreEntryReadinessValue()
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.False(snapshot.IsValidValue("CORE_ENTRY_READINESS_IN", "STANDARD"));
    }

    [Fact]
    public async Task RegistryValidation_Standard_IsAValidPlanModeValue_ConfirmingWhereItBelongs()
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.True(snapshot.IsValidValue("PLAN_MODE_IN", "STANDARD"));
    }

    [Fact]
    public async Task RegistryValidation_AllResolverProducedOutputValues_AreRegistryValid()
    {
        var registrySnapshot = await NewRegistryReader().LoadAsync(RegistryRef);
        var resolver = new CoreEntryReadinessResolver();

        var scenarios = new[]
        {
            new ResolverInputSnapshot { RecentWeeklyVolumeKm = 20, RecentLongestRunKm = 8 },        // READY
            new ResolverInputSnapshot { RecentWeeklyVolumeKm = 10, RecentLongestRunKm = 5 },         // CAUTION
            new ResolverInputSnapshot { RecentWeeklyVolumeKm = 3, RecentLongestRunKm = 2 },          // NOT_READY
            new ResolverInputSnapshot { GoalType = GoalType.Race },                                  // NOT_READY (both missing, race)
        };

        foreach (var scenario in scenarios)
        {
            var result = resolver.Resolve(Context(scenario));
            Assert.True(registrySnapshot.IsValid(result));
        }
    }

    [Fact]
    public async Task RegistryValidation_NotEvaluatedResult_IsContractValid_NotLookedUpInRegistry()
    {
        var registrySnapshot = await NewRegistryReader().LoadAsync(RegistryRef);
        var resolver = new CoreEntryReadinessResolver();

        var result = resolver.Resolve(Context(new ResolverInputSnapshot { GoalType = GoalType.Habit }));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.True(registrySnapshot.IsValid(result));
    }

    // ─── Cross-resolver status distinctions (contract regression guards) ────

    [Fact]
    public void PaceSourceNone_RemainsEvaluated_DistinctFromCoreEntryReadinessNotEvaluated()
    {
        var paceSourceResult = new PaceSourceResolver().Resolve(Context(new ResolverInputSnapshot()));
        var coreEntryResult = new CoreEntryReadinessResolver().Resolve(Context(new ResolverInputSnapshot { GoalType = GoalType.Habit }));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, paceSourceResult.Status);
        Assert.Equal("NONE", paceSourceResult.OutputValue);

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, coreEntryResult.Status);
        Assert.Null(coreEntryResult.OutputValue);
    }
}
