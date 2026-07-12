using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.5 — RuntimeConditionResolutionService
/// (orchestration) tests. Proves the four resolvers run in the approved
/// order, that PriorResults is populated automatically (callers never
/// hand-construct it for the normal pipeline case), that NotEvaluated
/// results are never swallowed, and that configuration exceptions
/// (missing CoreCycle) propagate unchanged. No TrainingWeek/TrainingDay is
/// created; no resolver output is wired into generation.
/// </summary>
public sealed class RuntimeConditionResolutionServiceTests
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

    private static readonly PlanCatalogCoreCycle StandardCoreCycle = new(8, 12, 14);

    private static RuntimeConditionResolutionService NewService() =>
        new(new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());

    // ─── Orchestration order ─────────────────────────────────────────────────

    [Fact]
    public void ResolveAllResults_ReturnsExactlyFourResults_InApprovedOrder()
    {
        var service = NewService();
        var context = new RuntimeResolverContext
        {
            InputSnapshot = new ResolverInputSnapshot { TargetFinishTimeSeconds = null },
            CoreCycle = StandardCoreCycle,
        };

        var results = service.ResolveAllResults(context);

        Assert.Equal(4, results.Count);
        Assert.Equal("TIME_ADEQUACY_IN", results[0].ConditionType);
        Assert.Equal("PACE_SOURCE_IN", results[1].ConditionType);
        Assert.Equal("CORE_ENTRY_READINESS_IN", results[2].ConditionType);
        Assert.Equal("GOAL_FEASIBILITY_IN", results[3].ConditionType);
    }

    [Fact]
    public void ResolveAllResults_NoDuplicateConditionTypes()
    {
        var service = NewService();
        var context = new RuntimeResolverContext
        {
            InputSnapshot = new ResolverInputSnapshot { TargetFinishTimeSeconds = null },
            CoreCycle = StandardCoreCycle,
        };

        var results = service.ResolveAllResults(context);

        Assert.Equal(results.Select(r => r.ConditionType).Distinct().Count(), results.Count);
    }

    [Fact]
    public void ResolveAll_TraceStepsMatchApprovedOrder()
    {
        var service = NewService();
        var context = new RuntimeResolverContext
        {
            InputSnapshot = new ResolverInputSnapshot { TargetFinishTimeSeconds = null },
            CoreCycle = StandardCoreCycle,
        };

        var trace = service.ResolveAll(context);

        Assert.Equal(4, trace.Steps.Count);
        Assert.Equal(new[] { 0, 1, 2, 3 }, trace.Steps.Select(s => s.StepIndex));
        Assert.Equal("TIME_ADEQUACY_IN", trace.Steps[0].ConditionType);
        Assert.Equal("PACE_SOURCE_IN", trace.Steps[1].ConditionType);
        Assert.Equal("CORE_ENTRY_READINESS_IN", trace.Steps[2].ConditionType);
        Assert.Equal("GOAL_FEASIBILITY_IN", trace.Steps[3].ConditionType);
    }

    // ─── PriorResults automatic population ──────────────────────────────────

    [Fact]
    public void ResolveAllResults_GoalFeasibilityReceivesAllThreePriorResults_WithoutManualInjection()
    {
        var service = NewService();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3000,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RequestedTargetDistanceKm = 10,
            GoalType = GoalType.Race,
            StartDate = new DateOnly(2026, 8, 3),
            RaceDate = new DateOnly(2026, 10, 25), // 83 days -> COMPRESSED, but evidence still usable
            RecentWeeklyVolumeKm = 20,
            RecentLongestRunKm = 8,
        };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle };

        // Caller supplies NO PriorResults at all -- the service must populate them.
        Assert.Null(context.PriorResults);

        var results = service.ResolveAllResults(context);
        var goalFeasibilityResult = results[3];

        // GoalFeasibilityResolver must NOT have returned a "missing dependency"
        // NotEvaluated reasonCode, proving it actually received the prior
        // results through orchestration.
        Assert.NotEqual("MISSING_CORE_ENTRY_READINESS_RESULT", goalFeasibilityResult.ReasonCode);
        Assert.NotEqual("MISSING_TIME_ADEQUACY_RESULT", goalFeasibilityResult.ReasonCode);
        Assert.NotEqual("MISSING_PACE_SOURCE_RESULT", goalFeasibilityResult.ReasonCode);
    }

    [Fact]
    public void ResolveAllResults_DoesNotMutateCallersOriginalContext()
    {
        var service = NewService();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = null };
        var originalContext = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle };

        service.ResolveAllResults(originalContext);

        // The caller's own context object must remain exactly as constructed.
        Assert.Null(originalContext.PriorResults);
        Assert.Same(input, originalContext.InputSnapshot);
    }

    // ─── End-to-end resolver chain scenarios ────────────────────────────────

    [Fact]
    public void EndToEnd_NoTargetFinishTime_GoalFeasibilityIsNotRequested()
    {
        var service = NewService();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = null };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle };

        var results = service.ResolveAllResults(context);
        var goalFeasibility = results[3];

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, goalFeasibility.Status);
        Assert.Equal("NOT_REQUESTED", goalFeasibility.OutputValue);
    }

    [Fact]
    public void EndToEnd_LowCoreReadinessEvidence_GoalFeasibilityIsUnsupported()
    {
        var service = NewService();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3000,
            GoalType = GoalType.Race,
            StartDate = new DateOnly(2026, 8, 3),
            RaceDate = new DateOnly(2026, 10, 25),
            RecentWeeklyVolumeKm = 3, // < 8 -> CORE_ENTRY_READINESS_IN = NOT_READY
            RecentLongestRunKm = 2,   // < 4
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
        };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle };

        var results = service.ResolveAllResults(context);

        Assert.Equal("NOT_READY", results[2].OutputValue); // CORE_ENTRY_READINESS_IN
        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, results[3].Status);
        Assert.Equal("UNSUPPORTED", results[3].OutputValue); // GOAL_FEASIBILITY_IN
        Assert.Equal("CORE_ENTRY_NOT_READY", results[3].ReasonCode);
    }

    [Fact]
    public void EndToEnd_CompleteFavorableEvidence_GoalFeasibilityIsRealistic()
    {
        var service = NewService();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3000, // matches golden-fixture-v3's own REALISTIC case
            GoalType = GoalType.Race,
            StartDate = new DateOnly(2026, 8, 3),
            RaceDate = new DateOnly(2026, 8, 3).AddDays(84), // exactly 12 weeks -> ADEQUATE
            RecentWeeklyVolumeKm = 24,
            RecentLongestRunKm = 9, // both READY-level
            RecentRunsPerWeek = 4,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
            RequestedTargetDistanceKm = 10,
        };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle };

        var results = service.ResolveAllResults(context);

        Assert.Equal("ADEQUATE", results[0].OutputValue);       // TIME_ADEQUACY_IN
        Assert.Equal("RECENT_RACE", results[1].OutputValue);    // PACE_SOURCE_IN
        Assert.Equal("READY", results[2].OutputValue);          // CORE_ENTRY_READINESS_IN
        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, results[3].Status);
        Assert.Equal("REALISTIC", results[3].OutputValue);      // GOAL_FEASIBILITY_IN
    }

    [Fact]
    public void EndToEnd_ChallengingGoal_GoalFeasibilityIsChallenging()
    {
        var service = NewService();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 2887, // ~4.5% faster than ~3023.15 predicted -> CHALLENGING
            GoalType = GoalType.Race,
            StartDate = new DateOnly(2026, 8, 3),
            RaceDate = new DateOnly(2026, 8, 3).AddDays(84),
            RecentWeeklyVolumeKm = 24,
            RecentLongestRunKm = 9,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
            RequestedTargetDistanceKm = 10,
        };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle };

        var results = service.ResolveAllResults(context);

        Assert.Equal("CHALLENGING", results[3].OutputValue);
    }

    [Fact]
    public void EndToEnd_MissingRaceDate_HabitPlan_PropagatesNotEvaluated_ThroughToGoalFeasibility()
    {
        // TIME_ADEQUACY_IN -> NotEvaluated (habit plan, no dates) must not
        // be swallowed: GOAL_FEASIBILITY_IN must reflect it via its own
        // documented TIME_ADEQUACY_NOT_EVALUATED reasonCode, not silently
        // proceed as if time adequacy were fine.
        var service = NewService();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3000,
            GoalType = GoalType.Habit,
            StartDate = null,
            RaceDate = null,
            RecentWeeklyVolumeKm = 24,
            RecentLongestRunKm = 9,
        };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle };

        var results = service.ResolveAllResults(context);

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, results[0].Status); // TIME_ADEQUACY_IN
        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, results[3].Status); // GOAL_FEASIBILITY_IN
        Assert.Equal("TIME_ADEQUACY_NOT_EVALUATED", results[3].ReasonCode);
    }

    [Fact]
    public void EndToEnd_NoRecentRaceButTargetTimeRequested_PaceSourceIsTargetTime_GoalFeasibilityReturnsDocumentedNotEvaluated()
    {
        // Finding from real orchestration (not evidenced when GoalFeasibilityResolver
        // was tested in isolation with a hand-built PriorResults list): once a
        // TargetFinishTimeSeconds is present, PaceSourceResolver's own output
        // priority (RECENT_RACE > TARGET_TIME > NONE) means PACE_SOURCE_IN can
        // only be NONE when there is also NO target time -- and GoalFeasibilityResolver
        // already returns NOT_REQUESTED unconditionally in that case, before the
        // PACE_SOURCE_IN dependency is even consulted. So GoalFeasibilityResolver's
        // documented "PACE_SOURCE_IN=NONE with target time requested -> NotEvaluated"
        // branch is defensive/unreachable through this real pipeline as currently
        // composed; the reachable "no independent current-fitness evidence" case is
        // PACE_SOURCE_IN=TARGET_TIME instead. See PHASE4D_5 doc for the full note.
        var service = NewService();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3000,
            GoalType = GoalType.Race,
            StartDate = new DateOnly(2026, 8, 3),
            RaceDate = new DateOnly(2026, 8, 3).AddDays(84),
            RecentWeeklyVolumeKm = 24,
            RecentLongestRunKm = 9,
            // no recent race evidence -> PACE_SOURCE_IN = TARGET_TIME (not NONE)
        };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle };

        var results = service.ResolveAllResults(context);

        Assert.Equal("TARGET_TIME", results[1].OutputValue); // PACE_SOURCE_IN
        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, results[3].Status);
        Assert.Equal("PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE", results[3].ReasonCode);
    }

    // ─── Context requirements ────────────────────────────────────────────────

    [Fact]
    public void MissingCoreCycle_ThrowsSameConfigurationErrorAsTimeAdequacyResolver_NotConvertedToNotEvaluated()
    {
        var service = NewService();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = null };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = null };

        var ex = Assert.Throws<InvalidOperationException>(() => service.ResolveAllResults(context));
        Assert.Contains("CoreCycle", ex.Message);
    }

    [Fact]
    public void MissingAsOfDate_DoesNotFailOrchestration_PaceSourcePreservesNotComputedBehavior()
    {
        var service = NewService();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = null,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
        };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle, AsOfDate = null };

        var results = service.ResolveAllResults(context);
        var paceSource = results[1];

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, paceSource.Status);
        Assert.Equal("RECENT_RACE", paceSource.OutputValue);
        Assert.Equal("NOT_COMPUTED_NO_REFERENCE_DATE", paceSource.Metadata["paceRecencyConfidence"]);
        Assert.False(paceSource.Metadata.ContainsKey("raceResultAgeDays"));
    }

    [Fact]
    public void PresentAsOfDate_FlowsThroughToPaceSourceRecencyMetadata()
    {
        var service = NewService();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = null,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
        };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle, AsOfDate = new DateOnly(2026, 6, 30) };

        var results = service.ResolveAllResults(context);
        var paceSource = results[1];

        Assert.Equal("15", paceSource.Metadata["raceResultAgeDays"]);
        Assert.Equal("FULL", paceSource.Metadata["paceRecencyConfidence"]);
    }

    // ─── Registry / contract validation ─────────────────────────────────────

    [Fact]
    public async Task RegistryValidation_EveryEvaluatedResultIsRegistryValid_AcrossEndToEndScenario()
    {
        var registrySnapshot = await NewRegistryReader().LoadAsync(RegistryRef);
        var service = NewService();
        var input = new ResolverInputSnapshot
        {
            TargetFinishTimeSeconds = 3000,
            GoalType = GoalType.Race,
            StartDate = new DateOnly(2026, 8, 3),
            RaceDate = new DateOnly(2026, 8, 3).AddDays(84),
            RecentWeeklyVolumeKm = 24,
            RecentLongestRunKm = 9,
            RecentRaceDistanceKm = 5,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
            RequestedTargetDistanceKm = 10,
        };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle };

        var results = service.ResolveAllResults(context);

        Assert.All(results, result => Assert.True(registrySnapshot.IsValid(result)));
    }

    [Fact]
    public void NotEvaluated_IsNeverTreatedAsOutputValue_AcrossAllFourResults()
    {
        var service = NewService();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = null };
        var context = new RuntimeResolverContext { InputSnapshot = input, CoreCycle = StandardCoreCycle };

        var results = service.ResolveAllResults(context);

        foreach (var result in results)
        {
            if (result.Status == RuntimeConditionResolutionStatus.NotEvaluated)
            {
                Assert.Null(result.OutputValue);
            }
        }
    }

    // ─── Interface implementation ────────────────────────────────────────────

    [Fact]
    public void RuntimeConditionResolutionService_ImplementsIRuntimeConditionResolutionService()
    {
        Assert.IsAssignableFrom<IRuntimeConditionResolutionService>(NewService());
    }
}
