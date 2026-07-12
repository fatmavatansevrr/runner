using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.4 — GoalFeasibilityResolver tests. Proves
/// dependency composition (never silently ignoring missing/NotEvaluated
/// CORE_ENTRY_READINESS_IN/TIME_ADEQUACY_IN/PACE_SOURCE_IN prior results),
/// the Riegel-projection + ratio classification path (using exactly the
/// golden-fixture-v3-evidenced exponent and boundaries), and the registry
/// contract. No TrainingWeek/TrainingDay is created; no resolver output is
/// wired into generation.
/// </summary>
public sealed class GoalFeasibilityResolverTests
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

    private static RuntimeConditionResolutionResult Ready() =>
        RuntimeConditionResolutionResult.Evaluated("CORE_ENTRY_READINESS_IN", "READY", "CORE_ENTRY_READY");

    private static RuntimeConditionResolutionResult Adequate() =>
        RuntimeConditionResolutionResult.Evaluated("TIME_ADEQUACY_IN", "ADEQUATE", "MEETS_DEFAULT_CORE_DURATION");

    private static RuntimeResolverContext Context(ResolverInputSnapshot input, params RuntimeConditionResolutionResult[] priorResults) =>
        new() { InputSnapshot = input, PriorResults = priorResults };

    // ─── Interface implementation ───────────────────────────────────────────

    [Fact]
    public void GoalFeasibilityResolver_ImplementsIGoalFeasibilityResolver()
    {
        Assert.IsAssignableFrom<IGoalFeasibilityResolver>(new GoalFeasibilityResolver());
    }

    [Fact]
    public void GoalFeasibilityResolver_ConditionTypeIsGoalFeasibilityIn()
    {
        Assert.Equal("GOAL_FEASIBILITY_IN", new GoalFeasibilityResolver().ConditionType);
    }

    // ─── NOT_REQUESTED ───────────────────────────────────────────────────────

    [Fact]
    public void Resolve_NoTargetFinishTimeSeconds_ReturnsEvaluatedNotRequested_RegardlessOfDependencies()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = null };

        // No PriorResults at all -- NOT_REQUESTED must still fire, proving
        // it bypasses dependency checks entirely.
        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("NOT_REQUESTED", result.OutputValue);
        Assert.Equal("TARGET_FINISH_TIME_NOT_REQUESTED", result.ReasonCode);
    }

    // ─── CORE_ENTRY_READINESS_IN dependency ─────────────────────────────────

    [Fact]
    public void Resolve_CoreEntryReadinessNotReady_ReturnsUnsupported()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };
        var notReady = RuntimeConditionResolutionResult.Evaluated("CORE_ENTRY_READINESS_IN", "NOT_READY", "CORE_ENTRY_NOT_READY");

        var result = resolver.Resolve(Context(input, notReady));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("UNSUPPORTED", result.OutputValue);
        Assert.Equal("CORE_ENTRY_NOT_READY", result.ReasonCode);
    }

    [Fact]
    public void Resolve_CoreEntryReadinessNotEvaluated_ReturnsNotEvaluated_NotSilentlyIgnored()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };
        var notEvaluated = RuntimeConditionResolutionResult.NotEvaluated("CORE_ENTRY_READINESS_IN", "CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT");

        var result = resolver.Resolve(Context(input, notEvaluated));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("CORE_ENTRY_READINESS_NOT_EVALUATED", result.ReasonCode);
    }

    [Fact]
    public void Resolve_CoreEntryReadinessMissing_ReturnsNotEvaluated_NotSilentlyIgnored()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };

        var result = resolver.Resolve(Context(input)); // no prior results at all

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("MISSING_CORE_ENTRY_READINESS_RESULT", result.ReasonCode);
    }

    [Fact]
    public void Resolve_CoreEntryReadinessCaution_ContinuesWithMetadata_DoesNotAutoUpgrade()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3000,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RequestedTargetDistanceKm = 10,
        };
        var caution = RuntimeConditionResolutionResult.Evaluated("CORE_ENTRY_READINESS_IN", "CAUTION", "CORE_ENTRY_CAUTION");
        var recentRace = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED");

        var result = resolver.Resolve(Context(input, caution, Adequate(), recentRace));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("true", result.Metadata["coreEntryReadinessCaution"]);
        // still computed via the real projection, not silently upgraded:
        Assert.Equal("REALISTIC", result.OutputValue);
    }

    // ─── TIME_ADEQUACY_IN dependency ────────────────────────────────────────

    [Fact]
    public void Resolve_TimeAdequacyInsufficient_ReturnsNotEvaluated_DecisionRequired_NotGuessedUnsupported()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };
        var insufficient = RuntimeConditionResolutionResult.Evaluated("TIME_ADEQUACY_IN", "INSUFFICIENT", "BELOW_MINIMUM_CORE_DURATION");

        var result = resolver.Resolve(Context(input, Ready(), insufficient));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED", result.ReasonCode);
        Assert.NotEqual("UNSUPPORTED", result.OutputValue);
    }

    [Fact]
    public void Resolve_TimeAdequacyNotEvaluated_ReturnsNotEvaluated_NotSilentlyIgnored()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };
        var notEvaluated = RuntimeConditionResolutionResult.NotEvaluated("TIME_ADEQUACY_IN", "MISSING_RACE_DATE");

        var result = resolver.Resolve(Context(input, Ready(), notEvaluated));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("TIME_ADEQUACY_NOT_EVALUATED", result.ReasonCode);
    }

    [Fact]
    public void Resolve_TimeAdequacyMissing_ReturnsNotEvaluated_NotSilentlyIgnored()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };

        var result = resolver.Resolve(Context(input, Ready())); // only CORE_ENTRY_READINESS_IN present

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("MISSING_TIME_ADEQUACY_RESULT", result.ReasonCode);
    }

    [Fact]
    public void Resolve_TimeAdequacyCompressed_ContinuesWithMetadata()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3000,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RequestedTargetDistanceKm = 10,
        };
        var compressed = RuntimeConditionResolutionResult.Evaluated("TIME_ADEQUACY_IN", "COMPRESSED", "BELOW_DEFAULT_BUT_MEETS_MINIMUM_CORE_DURATION");
        var recentRace = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED");

        var result = resolver.Resolve(Context(input, Ready(), compressed, recentRace));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("true", result.Metadata["timeAdequacyCompressed"]);
    }

    // ─── PACE_SOURCE_IN dependency ───────────────────────────────────────────

    [Fact]
    public void Resolve_PaceSourceMissing_ReturnsNotEvaluated_NotSilentlyIgnored()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };

        var result = resolver.Resolve(Context(input, Ready(), Adequate()));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("MISSING_PACE_SOURCE_RESULT", result.ReasonCode);
    }

    [Fact]
    public void Resolve_PaceSourceNotEvaluated_ReturnsNotEvaluated_NotSilentlyIgnored()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };
        var notEvaluated = RuntimeConditionResolutionResult.NotEvaluated("PACE_SOURCE_IN", "SOME_REASON");

        var result = resolver.Resolve(Context(input, Ready(), Adequate(), notEvaluated));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("PACE_SOURCE_NOT_EVALUATED", result.ReasonCode);
    }

    [Fact]
    public void Resolve_PaceSourceNone_TargetTimeRequested_ReturnsNotEvaluated()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };
        var none = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE_PROVIDED");

        var result = resolver.Resolve(Context(input, Ready(), Adequate(), none));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("PACE_SOURCE_NONE_TARGET_TIME_REQUESTED", result.ReasonCode);
    }

    [Fact]
    public void Resolve_PaceSourceTargetTime_ReturnsNotEvaluated_NeverComparesTargetToItself()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };
        var targetTime = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "TARGET_TIME", "TARGET_FINISH_TIME_PROVIDED");

        var result = resolver.Resolve(Context(input, Ready(), Adequate(), targetTime));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE", result.ReasonCode);
    }

    [Fact]
    public void Resolve_PaceSourceEstimated_ReturnsNotEvaluated_NoApprovedMethod()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };
        var estimated = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "ESTIMATED", "SOME_REASON");

        var result = resolver.Resolve(Context(input, Ready(), Adequate(), estimated));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("PACE_SOURCE_ESTIMATED_NO_APPROVED_METHOD", result.ReasonCode);
    }

    // ─── RECENT_RACE Riegel projection + ratio classification ───────────────

    [Fact]
    public void Resolve_RecentRace_GoldenFixtureExactValues_ReproducesRealisticClassification()
    {
        // Exact values from golden-fixture-v3's own PACE_CONVERSION +
        // GOAL_FEASIBILITY_RESOLVER steps: 5K in 1450s, projected to 10K,
        // goal 3000s -> REALISTIC (goalGapRatio ~0.007658).
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3000,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RequestedTargetDistanceKm = 10,
        };
        var recentRace = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED");

        var result = resolver.Resolve(Context(input, Ready(), Adequate(), recentRace));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("REALISTIC", result.OutputValue);
        Assert.Equal("WITHIN_REALISTIC_BAND", result.ReasonCode);
        Assert.Equal("3023.15", result.Metadata["projectedFinishTimeSeconds"]);
    }

    [Fact]
    public void Resolve_RecentRace_ModeratelyAggressiveGoal_ReturnsChallenging()
    {
        // 5K in 1450s -> predicted 10K ~3023.15s. A goal ~4.5% faster than
        // predicted (within (0.03, 0.06]) should classify CHALLENGING.
        // predicted * (1 - 0.045) ~ 2887s.
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 2887,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RequestedTargetDistanceKm = 10,
        };
        var recentRace = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED");

        var result = resolver.Resolve(Context(input, Ready(), Adequate(), recentRace));

        Assert.Equal("CHALLENGING", result.OutputValue);
        Assert.Equal("WITHIN_CHALLENGING_BAND", result.ReasonCode);
    }

    [Fact]
    public void Resolve_RecentRace_VeryAggressiveGoal_ReturnsUnsupported()
    {
        // Goal ~15% faster than predicted -> exceeds challengingMaxRatio (0.06).
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 2570, // ~15% faster than ~3023
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RequestedTargetDistanceKm = 10,
        };
        var recentRace = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED");

        var result = resolver.Resolve(Context(input, Ready(), Adequate(), recentRace));

        Assert.Equal("UNSUPPORTED", result.OutputValue);
        Assert.Equal("EXCEEDS_CHALLENGING_BAND", result.ReasonCode);
    }

    [Fact]
    public void Resolve_RecentRace_TargetSlowerThanPredicted_StillReturnsRealistic_NotAnInventedRegistryValue()
    {
        // Target time SLOWER than predicted (goalGapRatio negative) -> still
        // REALISTIC per the task's own fallback rule ("equal/slower than
        // projected ability -> REALISTIC"), metadata band = CONSERVATIVE.
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3300, // slower than predicted ~3023
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RequestedTargetDistanceKm = 10,
        };
        var recentRace = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED");

        var result = resolver.Resolve(Context(input, Ready(), Adequate(), recentRace));

        Assert.Equal("REALISTIC", result.OutputValue);
        Assert.Equal("CONSERVATIVE", result.Metadata["aggressivenessBand"]);
    }

    [Fact]
    public void Resolve_RecentRace_MetadataIncludesProjectionDetails()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3000,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RequestedTargetDistanceKm = 10,
        };
        var recentRace = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED");

        var result = resolver.Resolve(Context(input, Ready(), Adequate(), recentRace));

        Assert.Equal("RIEGEL_V1", result.Metadata["projectionMethod"]);
        Assert.Equal("1.06", result.Metadata["riegelExponent"]);
        Assert.Equal("5", result.Metadata["sourceDistanceKm"]);
        Assert.Equal("1450", result.Metadata["sourceTimeSeconds"]);
        Assert.Equal("10", result.Metadata["targetDistanceKm"]);
        Assert.Equal("3000", result.Metadata["targetFinishTimeSeconds"]);
        Assert.True(result.Metadata.ContainsKey("targetDeltaPercent"));
        Assert.True(result.Metadata.ContainsKey("aggressivenessBand"));
    }

    [Fact]
    public void Resolve_AggressivenessBand_IsNeverTheOutputValue()
    {
        var resolver = new GoalFeasibilityResolver();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 2570,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RequestedTargetDistanceKm = 10,
        };
        var recentRace = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED");

        var result = resolver.Resolve(Context(input, Ready(), Adequate(), recentRace));

        Assert.NotEqual(result.Metadata["aggressivenessBand"], result.OutputValue);
        Assert.Contains(result.OutputValue, new[] { "REALISTIC", "CHALLENGING", "UNSUPPORTED", "NOT_REQUESTED" });
    }

    // ─── Registry validation ─────────────────────────────────────────────────

    [Theory]
    [InlineData("REALISTIC")]
    [InlineData("CHALLENGING")]
    [InlineData("UNSUPPORTED")]
    [InlineData("NOT_REQUESTED")]
    public async Task RegistryValidation_FourGoalFeasibilityValues_AreRegistryValid(string value)
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.True(snapshot.IsValidValue("GOAL_FEASIBILITY_IN", value));
    }

    [Theory]
    [InlineData("CONSERVATIVE")]
    [InlineData("STRETCH")]
    [InlineData("CURRENTLY_UNSUPPORTED")]
    public async Task RegistryValidation_RicherAppselBands_AreNotValidGoalFeasibilityOutputValues(string value)
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.False(snapshot.IsValidValue("GOAL_FEASIBILITY_IN", value));
    }

    [Theory]
    [InlineData("READY")]
    [InlineData("CAUTION")]
    [InlineData("NOT_READY")]
    [InlineData("RECENT_RACE")]
    [InlineData("TARGET_TIME")]
    [InlineData("NONE")]
    public async Task RegistryValidation_OtherConditionTypeValues_AreNotValidGoalFeasibilityOutputValues(string value)
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.False(snapshot.IsValidValue("GOAL_FEASIBILITY_IN", value));
    }

    [Fact]
    public async Task RegistryValidation_AllResolverProducedOutputValues_AreRegistryValid()
    {
        var registrySnapshot = await NewRegistryReader().LoadAsync(RegistryRef);
        var resolver = new GoalFeasibilityResolver();

        var recentRace = RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED");
        var notReady = RuntimeConditionResolutionResult.Evaluated("CORE_ENTRY_READINESS_IN", "NOT_READY", "CORE_ENTRY_NOT_READY");

        var scenarios = new (ResolverInputSnapshot Input, RuntimeConditionResolutionResult[] Prior)[]
        {
            (new ResolverInputSnapshot { TargetFinishTimeSeconds = null }, Array.Empty<RuntimeConditionResolutionResult>()),
            (new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 }, new[] { notReady }),
            (new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000, RecentRaceDistanceKm = 5, RecentRaceFinishTimeSeconds = 1450, RequestedTargetDistanceKm = 10 }, new[] { Ready(), Adequate(), recentRace }),
        };

        foreach (var (input, prior) in scenarios)
        {
            var result = resolver.Resolve(Context(input, prior));
            Assert.True(registrySnapshot.IsValid(result));
        }
    }

    [Fact]
    public async Task RegistryValidation_NotEvaluatedResult_IsContractValid_NotLookedUpInRegistry()
    {
        var registrySnapshot = await NewRegistryReader().LoadAsync(RegistryRef);
        var resolver = new GoalFeasibilityResolver();

        var result = resolver.Resolve(Context(new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 }));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.True(registrySnapshot.IsValid(result));
    }
}
