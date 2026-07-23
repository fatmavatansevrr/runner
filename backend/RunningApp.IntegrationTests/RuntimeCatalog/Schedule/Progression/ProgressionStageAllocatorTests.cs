using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Progression;

/// <summary>
/// Backend Integration Phase 4F.6A — tests the fine-grained KEY_SESSION stage scheduler
/// (<see cref="ProgressionStageAllocator"/>) directly, using the real v10 catalog artifacts
/// where the scenario naturally occurs there, and small synthetic fixtures for structural
/// rejection paths that no real v10 data currently exercises (duplicate keys, fallback
/// cycles, ambiguous fallback, true multi-stage convergence).
/// </summary>
public sealed class ProgressionStageAllocatorTests
{
    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    private static async System.Threading.Tasks.Task<PlanCatalogCandidateSummary> RealCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = RealCatalogRoot() }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
    }

    private static async System.Threading.Tasks.Task<CatalogWorkoutProgressionDefinition> RealProgressionAsync(PlanCatalogCandidateSummary candidate)
    {
        var loader = new CatalogWorkoutProgressionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = RealCatalogRoot() }));
        return await loader.LoadAsync(candidate.WorkoutProgression);
    }

    private static GeneratedCatalogPlanSkeleton RealSkeleton(PlanCatalogCandidateSummary candidate, DateOnly startDate)
    {
        var orchestrator = new CatalogPlanSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
            new CatalogStageToWeekContextFactory(), new CatalogStageToWeekMaterializer(),
            new GeneratedCatalogPlanSkeletonValidator());

        var context = new CatalogPlanSkeletonOrchestrationContext
        {
            Candidate = candidate,
            ExpectedCandidateKey = candidate.CandidateKey,
            ExpectedCandidateVersion = candidate.CandidateVersion,
            ExpectedMasterTemplate = candidate.MasterTemplate,
            ExpectedRunLayout = candidate.Layout,
            StartDate = startDate,
            AsOfDate = startDate,
        };

        return orchestrator.Build(context).Skeleton;
    }

    private static IReadOnlyList<RuntimeConditionResolutionResult> GoalFeasibilityResult(string? outputValue) =>
        outputValue is null
            ? new[] { RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "TEST_NOT_EVALUATED") }
            : new[] { RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", outputValue, "TEST_EVALUATED") };

    // ───────────────────────── Real v10 catalog scenarios ─────────────────────────

    [Fact]
    public async System.Threading.Tasks.Task DefaultTwelveWeekPilot_GoalFeasibilityRealistic_AllocatesEveryWeekExactlyOnce()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = GoalFeasibilityResult("REALISTIC"),
        });

        Assert.Equal(12, schedule.Weeks.Count);
        Assert.Equal(Enumerable.Range(1, 12), schedule.Weeks.Select(w => w.WeekNumber).OrderBy(n => n));
        Assert.All(schedule.Weeks, w => Assert.Equal("KEY_SESSION", w.StructuralRole));

        var validation = new GeneratedCatalogStageScheduleValidator().Validate(schedule, skeleton);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
    }

    [Fact]
    public async System.Threading.Tasks.Task DefaultTwelveWeekPilot_GoalFeasibilityNotEvaluated_FallsBackAndStillAllocatesEveryWeekExactlyOnce()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = GoalFeasibilityResult(null),
        });

        Assert.Equal(12, schedule.Weeks.Count);
        var raceSpecificWeeks = schedule.Weeks.Where(w => w.PhaseKey == "RACE_SPECIFIC").OrderBy(w => w.WeekNumber).ToList();
        Assert.Equal(4, raceSpecificWeeks.Count);
        Assert.Contains(raceSpecificWeeks, w => w.ProgressionStageKey == "CURRENT_FITNESS_SPECIFIC_REHEARSAL");
        Assert.All(raceSpecificWeeks.Where(w => w.ProgressionStageKey == "CURRENT_FITNESS_SPECIFIC_REHEARSAL"),
            w => Assert.Equal("GOAL_PACE_REHEARSAL", w.FallbackOrigin));
    }

    [Fact]
    public async System.Threading.Tasks.Task FoundationPhase_AllocatesExactly3WeeksToFoundationEasyBase()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = GoalFeasibilityResult("REALISTIC"),
        });

        var foundationWeeks = schedule.Weeks.Where(w => w.PhaseKey == "FOUNDATION").ToList();
        Assert.Equal(3, foundationWeeks.Count);
        Assert.All(foundationWeeks, w => Assert.Equal("FOUNDATION_EASY_BASE", w.ProgressionStageKey));
    }

    [Fact]
    public async System.Threading.Tasks.Task BuildPhase_ExtensionAllocatesSurplusToHigherRelativeOrderStageFirst()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = GoalFeasibilityResult("REALISTIC"),
        });

        // BUILD: FARTLEK_INTRO(order1,min1,max2) + THRESHOLD_INTRO(order2,min2,max4), 4 weeks
        // available, min total 3, surplus 1 -> THRESHOLD_INTRO (higher RelativeOrder) should
        // receive the extra week first.
        var buildWeeks = schedule.Weeks.Where(w => w.PhaseKey == "BUILD").OrderBy(w => w.WeekNumber).ToList();
        Assert.Equal(4, buildWeeks.Count);
        var byStage = buildWeeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(1, byStage["FARTLEK_INTRO"]);
        Assert.Equal(3, byStage["THRESHOLD_INTRO"]);
    }

    [Fact]
    public async System.Threading.Tasks.Task TaperPhase_TaperSharpenPreservesFineGrainedStageIdentity()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = GoalFeasibilityResult("REALISTIC"),
        });

        var taperWeek = Assert.Single(schedule.Weeks, w => w.PhaseKey == "TAPER");
        Assert.Equal("TAPER_SHARPEN", taperWeek.ProgressionStageKey);
        Assert.NotEqual("TAPER", taperWeek.ProgressionStageKey);
        Assert.Equal("KEY_SESSION", taperWeek.StructuralRole);
    }

    [Fact]
    public async System.Threading.Tasks.Task Determinism_RepeatedRunsWithSameInputs_ProduceIdenticalSchedules()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var context = new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            Progression = progression,
            Skeleton = skeleton,
            ConditionResults = GoalFeasibilityResult("REALISTIC"),
        };

        var run1 = new ProgressionStageAllocator().Allocate(context);
        var run2 = new ProgressionStageAllocator().Allocate(context);

        var seq1 = run1.Weeks.OrderBy(w => w.WeekNumber).Select(w => (w.WeekNumber, w.ProgressionStageKey)).ToList();
        var seq2 = run2.Weeks.OrderBy(w => w.WeekNumber).Select(w => (w.WeekNumber, w.ProgressionStageKey)).ToList();
        Assert.Equal(seq1, seq2);
    }

    [Fact]
    public async System.Threading.Tasks.Task IndependenceFromInputOrder_StagesEnumeratedInReverse_ProducesSameAllocation()
    {
        var candidate = await RealCandidateAsync();
        var realProgression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        // Rebuild the same progression content with each phase's stage list reversed, to prove
        // the allocator's own RelativeOrder-based ordering (Section 8) — not array/dictionary
        // enumeration order — is authoritative.
        var reversedProgression = new CatalogWorkoutProgressionDefinition
        {
            Key = realProgression.Key,
            Version = realProgression.Version,
            DistanceFamily = realProgression.DistanceFamily,
            PhaseProgressions = realProgression.PhaseProgressions
                .Select(p => new CatalogPhaseWorkoutProgression { PhaseKey = p.PhaseKey, Stages = p.Stages.Reverse().ToList() })
                .Reverse()
                .ToList(),
        };

        var context1 = new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            Progression = realProgression, Skeleton = skeleton, ConditionResults = GoalFeasibilityResult("REALISTIC"),
        };
        var context2 = new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            Progression = reversedProgression, Skeleton = skeleton, ConditionResults = GoalFeasibilityResult("REALISTIC"),
        };

        var schedule1 = new ProgressionStageAllocator().Allocate(context1);
        var schedule2 = new ProgressionStageAllocator().Allocate(context2);

        var seq1 = schedule1.Weeks.OrderBy(w => w.WeekNumber).Select(w => (w.WeekNumber, w.ProgressionStageKey)).ToList();
        var seq2 = schedule2.Weeks.OrderBy(w => w.WeekNumber).Select(w => (w.WeekNumber, w.ProgressionStageKey)).ToList();
        Assert.Equal(seq1, seq2);
    }

    [Fact]
    public async System.Threading.Tasks.Task NoWorkoutIdentity_ScheduleContainsNoWorkoutFieldAnywhere()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            Progression = progression, Skeleton = skeleton, ConditionResults = GoalFeasibilityResult("REALISTIC"),
        });

        var scheduledWeekProperties = typeof(ScheduledProgressionWeek).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain(scheduledWeekProperties, name => name.Contains("Workout", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async System.Threading.Tasks.Task ContiguousBlocks_NoStageAlternatesWithinAPhase()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            Progression = progression, Skeleton = skeleton, ConditionResults = GoalFeasibilityResult("REALISTIC"),
        });

        foreach (var phaseGroup in schedule.Weeks.GroupBy(w => w.PhaseKey))
        {
            var ordered = phaseGroup.OrderBy(w => w.WeekNumber).Select(w => w.ProgressionStageKey).ToList();
            var seen = new HashSet<string>();
            string? last = null;
            foreach (var key in ordered)
            {
                if (key != last)
                {
                    Assert.True(seen.Add(key), $"Stage '{key}' re-appeared non-contiguously in phase '{phaseGroup.Key}'.");
                }
                last = key;
            }
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task NoCrossPhaseAllocation_EveryWeekMatchesItsSkeletonPhase()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            Progression = progression, Skeleton = skeleton, ConditionResults = GoalFeasibilityResult("REALISTIC"),
        });

        var skeletonByWeek = skeleton.Weeks.ToDictionary(w => w.WeekNumber, w => w.StageKey);
        Assert.All(schedule.Weeks, w => Assert.Equal(skeletonByWeek[w.WeekNumber], w.PhaseKey));
    }

    [Fact]
    public async System.Threading.Tasks.Task Validator_AcceptsRealDefaultSchedule()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            Progression = progression, Skeleton = skeleton, ConditionResults = GoalFeasibilityResult("REALISTIC"),
        });

        var validation = new GeneratedCatalogStageScheduleValidator().Validate(schedule, skeleton);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
    }

    [Fact]
    public async System.Threading.Tasks.Task Validator_RejectsCorruptedSchedule_DuplicateWeekNumber()
    {
        var candidate = await RealCandidateAsync();
        var progression = await RealProgressionAsync(candidate);
        var skeleton = RealSkeleton(candidate, new DateOnly(2026, 1, 5));

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            Progression = progression, Skeleton = skeleton, ConditionResults = GoalFeasibilityResult("REALISTIC"),
        });

        var corrupted = new GeneratedCatalogStageSchedule
        {
            CandidateKey = schedule.CandidateKey,
            CandidateVersion = schedule.CandidateVersion,
            ProgressionArtifactKey = schedule.ProgressionArtifactKey,
            ProgressionArtifactVersion = schedule.ProgressionArtifactVersion,
            AllocatorVersion = schedule.AllocatorVersion,
            Weeks = schedule.Weeks.Concat(new[] { schedule.Weeks[0] }).ToList(),
            Trace = schedule.Trace,
        };

        var validation = new GeneratedCatalogStageScheduleValidator().Validate(corrupted, skeleton);
        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("assigned more than once"));
    }

    // ───────────────────────── Synthetic structural-failure fixtures ─────────────────────────

    private static GeneratedCatalogPlanSkeleton SyntheticSkeleton(params (string PhaseKey, int WeekCount)[] phases)
    {
        var weeks = new List<GeneratedCatalogWeekSkeleton>();
        var weekNumber = 1;
        foreach (var (phaseKey, weekCount) in phases)
        {
            for (var i = 1; i <= weekCount; i++)
            {
                weeks.Add(new GeneratedCatalogWeekSkeleton
                {
                    WeekNumber = weekNumber,
                    StartDate = new DateOnly(2026, 1, 1).AddDays((weekNumber - 1) * 7),
                    EndDate = new DateOnly(2026, 1, 1).AddDays((weekNumber - 1) * 7 + 6),
                    StageKey = phaseKey,
                    StageWeekIndex = i,
                    StageWeekCount = weekCount,
                    SessionSlots = Array.Empty<GeneratedCatalogSessionSlotSkeleton>(),
                    Provenance = new GeneratedCatalogWeekSkeletonProvenance { StageKey = phaseKey, SourcePhaseKey = phaseKey },
                });
                weekNumber++;
            }
        }

        var totalWeeks = weekNumber - 1;
        return new GeneratedCatalogPlanSkeleton
        {
            SchemaVersion = GeneratedCatalogPlanSkeleton.CurrentSchemaVersion,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 1).AddDays(totalWeeks * 7 - 1),
            PlannedWeekCount = totalWeeks,
            DaysPerWeek = 4,
            CanonicalDistanceFamily = "TEN_K",
            CandidateKey = "SYNTHETIC",
            CandidateVersion = 1,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = "SYNTHETIC", CandidateVersion = 1, DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
                AsOfDate = new DateOnly(2026, 1, 1), MaterializerVersion = "TEST",
            },
        };
    }

    private static CatalogWorkoutProgressionStage Stage(
        string key, int order, int min, int max,
        CatalogStageCompressionBehavior compression = CatalogStageCompressionBehavior.Compressible,
        CatalogStageExtensionBehavior extension = CatalogStageExtensionBehavior.Extendable,
        IReadOnlyList<CatalogRuntimeEligibilityCondition>? requires = null,
        string? fallback = null) => new()
    {
        ProgressionStageKey = key,
        RelativeOrder = order,
        MinimumExposures = min,
        MaximumExposures = max,
        CompressionBehavior = compression,
        ExtensionBehavior = extension,
        Requires = requires ?? Array.Empty<CatalogRuntimeEligibilityCondition>(),
        FallbackStageKey = fallback,
    };

    private static CatalogWorkoutProgressionDefinition SyntheticProgression(params (string PhaseKey, CatalogWorkoutProgressionStage[] Stages)[] phases) => new()
    {
        Key = "SYNTHETIC_PROGRESSION",
        Version = 1,
        DistanceFamily = "TEN_K",
        PhaseProgressions = phases.Select(p => new CatalogPhaseWorkoutProgression { PhaseKey = p.PhaseKey, Stages = p.Stages }).ToList(),
    };

    private static ProgressionStageAllocationContext Context(
        CatalogWorkoutProgressionDefinition progression, GeneratedCatalogPlanSkeleton skeleton,
        IReadOnlyList<RuntimeConditionResolutionResult>? results = null) => new()
    {
        CandidateKey = "SYNTHETIC",
        CandidateVersion = 1,
        Progression = progression,
        Skeleton = skeleton,
        ConditionResults = results ?? Array.Empty<RuntimeConditionResolutionResult>(),
    };

    [Fact]
    public void MinimumExposureAllocation_ExactFit_NoCompressionOrExtension()
    {
        var progression = SyntheticProgression(("PHASE_A", new[] { Stage("A1", 1, 2, 2) }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 2));

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));

        Assert.Equal(2, schedule.Weeks.Count);
        Assert.All(schedule.Weeks, w => Assert.Equal("A1", w.ProgressionStageKey));
        Assert.All(schedule.Weeks, w => Assert.Equal("MINIMUM_EXPOSURE_ALLOCATION", w.AllocationReason));
    }

    [Fact]
    public void CompressionAllocation_ReducesHighestRelativeOrderCompressibleStageFirst()
    {
        // A1(order1,min2,max2,Protected) + A2(order2,min3,max3,Compressible) = min total 5, only
        // 4 weeks available -> must reduce A2 (the only Compressible stage) by 1.
        var progression = SyntheticProgression(("PHASE_A", new[]
        {
            Stage("A1", 1, 2, 2, compression: CatalogStageCompressionBehavior.Protected),
            Stage("A2", 2, 3, 3, compression: CatalogStageCompressionBehavior.Compressible),
        }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 4));

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));

        Assert.Equal(4, schedule.Weeks.Count);
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(2, byStage["A1"]);
        Assert.Equal(2, byStage["A2"]);
        Assert.Contains(schedule.Weeks, w => w.AllocationReason == "COMPRESSION_REDUCED_ALLOCATION");
    }

    [Fact]
    public void InsufficientMinimumCapacity_BothStagesProtected_ThrowsTypedException()
    {
        var progression = SyntheticProgression(("PHASE_A", new[]
        {
            Stage("A1", 1, 2, 2, compression: CatalogStageCompressionBehavior.Protected),
            Stage("A2", 2, 3, 3, compression: CatalogStageCompressionBehavior.Protected),
        }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 4));

        Assert.Throws<ProgressionPhaseCapacityInsufficientException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton)));
    }

    [Fact]
    public void ExcessWeeksBeyondMaximum_BothStagesFixedExposure_ThrowsTypedException()
    {
        var progression = SyntheticProgression(("PHASE_A", new[]
        {
            Stage("A1", 1, 1, 1, extension: CatalogStageExtensionBehavior.FixedExposure),
            Stage("A2", 2, 1, 1, extension: CatalogStageExtensionBehavior.FixedExposure),
        }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 5));

        Assert.Throws<ProgressionPhaseCapacityExceedsMaximumException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton)));
    }

    [Fact]
    public void ConditionalEligibleStage_ConditionSatisfied_UsesRequestedStageDirectly()
    {
        var requires = new[] { new CatalogRuntimeEligibilityCondition { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC", "CHALLENGING" } } };
        var progression = SyntheticProgression(("PHASE_A", new[] { Stage("A1", 1, 1, 1, requires: requires, fallback: "A2"), Stage("A2", 2, 1, 1) }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 1));

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton, GoalFeasibilityResult("REALISTIC")));

        var week = Assert.Single(schedule.Weeks);
        Assert.Equal("A1", week.ProgressionStageKey);
        Assert.Equal(ProgressionStageEligibilityOutcome.Eligible, week.ConditionOutcome);
        Assert.Null(week.FallbackOrigin);
    }

    [Fact]
    public void ConditionalIneligibleStage_WithFallback_UsesFallbackAndRecordsOrigin()
    {
        var requires = new[] { new CatalogRuntimeEligibilityCondition { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC", "CHALLENGING" } } };
        var progression = SyntheticProgression(("PHASE_A", new[] { Stage("A1", 1, 1, 1, requires: requires, fallback: "A2"), Stage("A2", 2, 1, 1) }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 1));

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton, GoalFeasibilityResult("UNSUPPORTED")));

        var week = Assert.Single(schedule.Weeks);
        Assert.Equal("A2", week.ProgressionStageKey);
        Assert.Equal("A1", week.FallbackOrigin);
        Assert.Equal(ProgressionStageEligibilityOutcome.IneligibleWithFallback, week.ConditionOutcome);
    }

    [Fact]
    public void ConditionalIneligibleStage_WithoutFallback_ThrowsTypedException()
    {
        var requires = new[] { new CatalogRuntimeEligibilityCondition { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC" } } };
        var progression = SyntheticProgression(("PHASE_A", new[] { Stage("A1", 1, 1, 1, requires: requires) }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 1));

        Assert.Throws<ProgressionStageIneligibleWithoutFallbackException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton, GoalFeasibilityResult("UNSUPPORTED"))));
    }

    [Fact]
    public void FallbackCycle_ChainRevisitsAnAlreadySeenStage_ThrowsTypedException()
    {
        // A0 (top-level, requested) -> A1 -> A2 -> A1 (revisits A1) is a cycle reachable purely
        // by walking A0's own fallback chain. A0 itself must remain top-level (not itself a
        // fallback target of anything), or it would be excluded from the requested-stage set
        // entirely and the cycle would never be reached.
        var requires = new[] { new CatalogRuntimeEligibilityCondition { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC" } } };
        var progression = SyntheticProgression(("PHASE_A", new[]
        {
            Stage("A0", 1, 1, 1, requires: requires, fallback: "A1"),
            Stage("A1", 2, 1, 1, requires: requires, fallback: "A2"),
            Stage("A2", 3, 1, 1, requires: requires, fallback: "A1"),
        }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 1));

        Assert.Throws<ProgressionStageFallbackCycleException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton, GoalFeasibilityResult("UNSUPPORTED"))));
    }

    [Fact]
    public void DuplicateRelativeOrder_ThrowsTypedException()
    {
        var progression = SyntheticProgression(("PHASE_A", new[] { Stage("A1", 1, 1, 1), Stage("A2", 1, 1, 1) }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 2));

        Assert.Throws<ProgressionStageDuplicateRelativeOrderException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton)));
    }

    [Fact]
    public void DuplicateStageKey_ThrowsTypedException()
    {
        var progression = SyntheticProgression(("PHASE_A", new[] { Stage("A1", 1, 1, 1), Stage("A1", 2, 1, 1) }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 2));

        Assert.Throws<ProgressionStageDuplicateOrMissingKeyException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton)));
    }

    [Fact]
    public void UnknownConditionKey_ThrowsTypedException()
    {
        var requires = new[] { new CatalogRuntimeEligibilityCondition { ConditionType = "NOT_A_REAL_CONDITION", AllowedValues = new HashSet<string> { "X" } } };
        var progression = SyntheticProgression(("PHASE_A", new[] { Stage("A1", 1, 1, 1, requires: requires) }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 1));

        Assert.Throws<ProgressionStageUnknownConditionKeyException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton, GoalFeasibilityResult("REALISTIC"))));
    }

    [Fact]
    public void MissingConditionResult_ThrowsTypedException()
    {
        var requires = new[] { new CatalogRuntimeEligibilityCondition { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC" } } };
        var progression = SyntheticProgression(("PHASE_A", new[] { Stage("A1", 1, 1, 1, requires: requires) }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 1));

        Assert.Throws<ProgressionStageConditionResultMissingException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton, Array.Empty<RuntimeConditionResolutionResult>())));
    }

    [Fact]
    public void TrueConvergence_TwoRequestedStagesFallBackToSameTarget_BoundsReconciled()
    {
        var requires = new[] { new CatalogRuntimeEligibilityCondition { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC" } } };
        var progression = SyntheticProgression(("PHASE_A", new[]
        {
            Stage("A1", 1, 1, 2, requires: requires, fallback: "A3"),
            Stage("A2", 2, 1, 3, requires: requires, fallback: "A3"),
            Stage("A3", 3, 1, 1),
        }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 2));

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton, GoalFeasibilityResult("UNSUPPORTED")));

        Assert.Equal(2, schedule.Weeks.Count);
        Assert.All(schedule.Weeks, w => Assert.Equal("A3", w.ProgressionStageKey));
    }

    [Fact]
    public void TrueConvergence_UnreconcilableBounds_ThrowsTypedException()
    {
        var requires = new[] { new CatalogRuntimeEligibilityCondition { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC" } } };
        // A1 requires exactly 5 (min=max=5), A2 requires exactly 1 (min=max=1) — both fall back
        // to A3; conservative merge (max of mins=5, min of maxes=1) is unreconcilable (5 > 1).
        var progression = SyntheticProgression(("PHASE_A", new[]
        {
            Stage("A1", 1, 5, 5, requires: requires, fallback: "A3"),
            Stage("A2", 2, 1, 1, requires: requires, fallback: "A3"),
            Stage("A3", 3, 1, 10),
        }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 2));

        Assert.Throws<ProgressionStageFallbackBoundsUnreconcilableException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton, GoalFeasibilityResult("UNSUPPORTED"))));
    }

    [Fact]
    public void PhaseMismatch_ProgressionPhaseNotInSkeleton_ThrowsTypedException()
    {
        var progression = SyntheticProgression(("PHASE_NOT_IN_SKELETON", new[] { Stage("A1", 1, 1, 1) }));
        var skeleton = SyntheticSkeleton(("PHASE_A", 1));

        Assert.Throws<ProgressionStagePhaseMismatchException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton)));
    }
}
