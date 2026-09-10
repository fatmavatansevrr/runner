using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;
using Xunit.Abstractions;

namespace RunningApp.IntegrationTests.RuntimeCatalog.HalfMarathon;

/// <summary>
/// Backend Integration Phase HM.5.2A — Section 8/9 required proof: the real stage allocator,
/// against the real (post-HM.5.2A one-field-per-Build-stage catalog edit) HM catalog, now
/// reproduces HM.4's frozen compression authority exactly at every 10-16W horizon — closing the
/// gap HM.5.2 disclosed but did not fix (PHASE_HM_5_2_PROGRESSION_STAGE_ALLOCATOR_OPTIONAL_
/// PREFERRED_SEMANTIC_CLOSURE.md Section 9/16: 10W/11W/12W previously resolved Build 1+2, not
/// HM.4's required 0+3). Values below are copied verbatim from
/// PHASE_HM_4_PHASE_AND_STAGE_NUMERIC_HEADROOM_AUTHORITY_CLOSURE.md Section 10's frozen table —
/// not re-derived here.
/// </summary>
public sealed class Hm52aStageCompressionPriorityOccupancyTests
{
    private readonly ITestOutputHelper _output;

    public Hm52aStageCompressionPriorityOccupancyTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey,
            V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateVersion);
    }

    private static async Task<CatalogWorkoutProgressionDefinition> ProgressionAsync(PlanCatalogCandidateSummary candidate)
    {
        var progressionLoader = new CatalogWorkoutProgressionLoader(Options.Create(OptionsValue));
        return await progressionLoader.LoadAsync(candidate.WorkoutProgression);
    }

    private static GeneratedCatalogPlanSkeleton Skeleton(PlanCatalogCandidateSummary candidate, int targetWeekCount, DateOnly startDate)
    {
        var orchestrator = new DynamicCoreWeekSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator());

        var context = new DynamicCoreWeekSkeletonOrchestrationContext
        {
            Candidate = candidate,
            TargetWeekCount = targetWeekCount,
            StartDate = startDate,
            AsOfDate = startDate,
        };

        return orchestrator.Build(context).Skeleton;
    }

    private static IReadOnlyList<RuntimeConditionResolutionResult> RealisticGoalFeasibility() => new[]
    {
        RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST_EVALUATED"),
        RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "TEST_EVALUATED"),
    };

    /// <summary>HM.4 Section 10 frozen Build occupancy: (FASTER_THAN_HM_INTRO, THRESHOLD_DEVELOPMENT).</summary>
    [Theory]
    [InlineData(10, 0, 3)]
    [InlineData(11, 0, 3)]
    [InlineData(12, 0, 3)]
    [InlineData(13, 1, 3)]
    [InlineData(14, 1, 3)]
    [InlineData(15, 1, 3)]
    [InlineData(16, 1, 4)]
    public async Task EveryHorizon_BuildStageOccupancyMatchesHm4FrozenCompressionAndExtensionAuthority(
        int targetWeekCount, int expectedIntro, int expectedDevelopment)
    {
        var candidate = await CandidateAsync();
        var progression = await ProgressionAsync(candidate);
        var skeleton = Skeleton(candidate, targetWeekCount, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = RealisticGoalFeasibility(),
        });

        var validation = new GeneratedCatalogStageScheduleValidator().Validate(schedule, skeleton);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));

        var buildWeeks = schedule.Weeks.Where(w => w.PhaseKey == "BUILD").ToList();
        var byStage = buildWeeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());
        _output.WriteLine($"{targetWeekCount}W BUILD: " + string.Join(", ", byStage.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}")));

        Assert.Equal(expectedIntro, byStage.GetValueOrDefault("BUILD_FASTER_THAN_HM_INTRO", 0));
        Assert.Equal(expectedDevelopment, byStage.GetValueOrDefault("BUILD_THRESHOLD_DEVELOPMENT", 0));
        Assert.Equal(expectedIntro + expectedDevelopment, buildWeeks.Count);
    }

    /// <summary>
    /// HM.4 Section 10 frozen RaceSpecific occupancy: (HM_INTRODUCTION, HM_DEVELOPMENT,
    /// HM_REHEARSAL). Section 9 of the governing HM.5.2A prompt requires this audited even
    /// though the discovered symptom was Build-specific — this proves RaceSpecific already
    /// matches HM.4 exactly through pure extension/exact-fit mechanics (compression is never
    /// actually invoked for RaceSpecific at any of these 7 horizons, because the sum of its
    /// three stages' Minimums, 0+1+2=3, never exceeds RaceSpecific's own allocated week count at
    /// any horizon — Regime B or C only, confirmed empirically below), so no CompressionPriority
    /// value was authored onto these stages (nothing to close).
    /// </summary>
    [Theory]
    [InlineData(10, 0, 1, 2)]
    [InlineData(11, 0, 2, 2)]
    [InlineData(12, 1, 2, 2)]
    [InlineData(13, 1, 2, 2)]
    [InlineData(14, 1, 2, 2)]
    [InlineData(15, 1, 2, 2)]
    [InlineData(16, 1, 2, 2)]
    public async Task EveryHorizon_RaceSpecificStageOccupancyMatchesHm4FrozenAuthority(
        int targetWeekCount, int expectedIntroduction, int expectedDevelopment, int expectedRehearsal)
    {
        var candidate = await CandidateAsync();
        var progression = await ProgressionAsync(candidate);
        var skeleton = Skeleton(candidate, targetWeekCount, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = RealisticGoalFeasibility(),
        });

        var raceSpecificWeeks = schedule.Weeks.Where(w => w.PhaseKey == "RACE_SPECIFIC").ToList();
        var byStage = raceSpecificWeeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());
        _output.WriteLine($"{targetWeekCount}W RACE_SPECIFIC: " + string.Join(", ", byStage.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}")));

        Assert.Equal(expectedIntroduction, byStage.GetValueOrDefault("RACE_SPECIFIC_HM_INTRODUCTION", 0));
        Assert.Equal(expectedDevelopment, byStage.GetValueOrDefault("RACE_SPECIFIC_HM_DEVELOPMENT", 0));
        Assert.Equal(expectedRehearsal, byStage.GetValueOrDefault("RACE_SPECIFIC_HM_REHEARSAL", 0));

        // HM_REHEARSAL's permanent protection (PROTECTED/FIXED_EXPOSURE) is never violated at
        // any horizon, confirming HM.4 Section 6's rehearsal-protection invariant survives this
        // phase's compression-priority change untouched (the change is scoped to Build only).
        Assert.Equal(2, expectedRehearsal);
    }

    /// <summary>
    /// Direct catalog-data proof: the real HM catalog now declares HM.4's compression-removal
    /// intent explicitly via CompressionPriority (lower value = removed first under
    /// compression), decoupled from RelativeOrder (chronological order is unchanged — INTRO
    /// still precedes DEVELOPMENT in week-block layout).
    /// </summary>
    [Fact]
    public async Task RealCatalog_BuildStagesDeclareExplicitCompressionPriority_DecoupledFromRelativeOrder()
    {
        var candidate = await CandidateAsync();
        var progression = await ProgressionAsync(candidate);

        var buildStages = progression.PhaseProgressions.Single(p => p.PhaseKey == "BUILD").Stages;
        var intro = buildStages.Single(s => s.ProgressionStageKey == "BUILD_FASTER_THAN_HM_INTRO");
        var development = buildStages.Single(s => s.ProgressionStageKey == "BUILD_THRESHOLD_DEVELOPMENT");

        Assert.Equal(1, intro.RelativeOrder);
        Assert.Equal(2, development.RelativeOrder);
        Assert.Equal(1, intro.CompressionPriority);
        Assert.Equal(2, development.CompressionPriority);

        // Chronological order (RelativeOrder) and compression-removal order (CompressionPriority)
        // happen to agree in direction here (both favor removing/placing INTRO before
        // DEVELOPMENT) -- this is a real, HM-specific authoring choice, not a generic algorithm
        // constraint (Hm52aProgressionStageAllocatorCompressionPriorityInvariantTests proves the
        // allocator honors CompressionPriority even when it is authored to DISAGREE with
        // RelativeOrder and/or extension order).
    }
}
