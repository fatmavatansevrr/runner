using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.HalfMarathon;

/// <summary>
/// PHASE HM.5 -- pre-implementation mechanism proof. Before authoring HM.4's literal
/// Layer B stage minimumExposures=0 values (BUILD_FASTER_THAN_HM_INTRO,
/// RACE_SPECIFIC_HM_INTRODUCTION) into the real catalog, this test proves directly against
/// the real, unmodified <see cref="ProgressionStageAllocator"/> (no catalog file involved --
/// pure in-memory fixtures) what its actual compression/extension mechanism does with those
/// exact numbers, so no product-authority number is silently violated by a mismatched
/// implementation.
///
/// Finding: ProgressionStageAllocator has no "preferred exposures" concept distinct from
/// MinimumExposures -- MinimumExposures IS the scheduling baseline (seed), extension fills
/// surplus toward MaximumExposures favoring the HIGHEST RelativeOrder stage first
/// (ApplyExtension), and compression (ApplyCompression) reduces toward a HARDCODED floor of
/// 1 exposure per Compressible stage, never toward 0, regardless of the stage's own
/// catalog-declared MinimumExposures. Both mechanisms are read directly from
/// ProgressionStageAllocator.cs (ApplyCompression/ApplyExtension) -- this test exercises them
/// for real rather than re-deriving them from a reading.
/// </summary>
public sealed class Hm5StageAllocatorMechanismProofTests
{
    private static GeneratedCatalogPlanSkeleton BuildPhaseOnlySkeleton(int buildWeeks)
    {
        var weeks = new List<GeneratedCatalogWeekSkeleton>();
        for (var i = 1; i <= buildWeeks; i++)
        {
            weeks.Add(new GeneratedCatalogWeekSkeleton
            {
                WeekNumber = i,
                StartDate = new DateOnly(2026, 1, 1).AddDays((i - 1) * 7),
                EndDate = new DateOnly(2026, 1, 1).AddDays((i - 1) * 7 + 6),
                StageKey = "BUILD",
                StageWeekIndex = i,
                StageWeekCount = buildWeeks,
                SessionSlots = Array.Empty<GeneratedCatalogSessionSlotSkeleton>(),
                Provenance = new GeneratedCatalogWeekSkeletonProvenance { StageKey = "BUILD", SourcePhaseKey = "BUILD" },
            });
        }

        return new GeneratedCatalogPlanSkeleton
        {
            SchemaVersion = GeneratedCatalogPlanSkeleton.CurrentSchemaVersion,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 1).AddDays(buildWeeks * 7 - 1),
            PlannedWeekCount = buildWeeks,
            DaysPerWeek = 4,
            CanonicalDistanceFamily = "HALF_MARATHON",
            CandidateKey = "TEST_HM5_MECHANISM_PROOF",
            CandidateVersion = 1,
            DependencyVersions = new Dictionary<string, RunningApp.Application.RuntimeCatalog.PlanCatalogReference>(),
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = "TEST_HM5_MECHANISM_PROOF",
                CandidateVersion = 1,
                DependencyVersions = new Dictionary<string, RunningApp.Application.RuntimeCatalog.PlanCatalogReference>(),
                AsOfDate = new DateOnly(2026, 1, 1),
                MaterializerVersion = "TEST",
            },
        };
    }

    /// <summary>HM.4's exact proposed Layer B numbers for BUILD's two stages.</summary>
    private static CatalogWorkoutProgressionDefinition Hm4ProposedBuildProgression() => new()
    {
        Key = "TEST_HM5_MECHANISM_PROOF_PROGRESSION",
        Version = 1,
        DistanceFamily = "HALF_MARATHON",
        PhaseProgressions = new[]
        {
            new CatalogPhaseWorkoutProgression
            {
                PhaseKey = "BUILD",
                Stages = new[]
                {
                    new CatalogWorkoutProgressionStage
                    {
                        ProgressionStageKey = "BUILD_FASTER_THAN_HM_INTRO",
                        RelativeOrder = 1,
                        MinimumExposures = 0, // HM.4's literal proposed value
                        MaximumExposures = 1,
                        CompressionBehavior = CatalogStageCompressionBehavior.Compressible,
                        ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
                        Requires = Array.Empty<CatalogRuntimeEligibilityCondition>(),
                    },
                    new CatalogWorkoutProgressionStage
                    {
                        ProgressionStageKey = "BUILD_THRESHOLD_DEVELOPMENT",
                        RelativeOrder = 2,
                        MinimumExposures = 3, // HM.4's literal proposed value (eligible branch)
                        MaximumExposures = 5,
                        CompressionBehavior = CatalogStageCompressionBehavior.Compressible,
                        ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
                        Requires = Array.Empty<CatalogRuntimeEligibilityCondition>(),
                    },
                },
            },
        },
    };

    private static ProgressionStageAllocationContext Context(CatalogWorkoutProgressionDefinition progression, GeneratedCatalogPlanSkeleton skeleton) => new()
    {
        CandidateKey = "TEST_HM5_MECHANISM_PROOF",
        CandidateVersion = 1,
        Progression = progression,
        Skeleton = skeleton,
        ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
    };

    [Fact]
    public void Hm4ProposedIntroMinimumZero_At14WeekFrozenBuildLength_DoesNotReproduceFrozenOneThreeOccupancy()
    {
        // 14W's Build phase is frozen at exactly 4 weeks (HM.2/HM.2.1). Today's real,
        // unmodified catalog pins BUILD_FASTER_THAN_HM_INTRO and BUILD_THRESHOLD_DEVELOPMENT
        // at 1/1 and 3/3 respectively, producing the frozen 1+3 occupancy with zero
        // allocator adjustment. This test proves that authoring HM.4's literal proposed
        // Min/Max (0/1 and 3/5) into the SAME 4-week Build phase does NOT reproduce that
        // frozen occupancy through the real allocator -- it produces 0+4 instead, because
        // ApplyExtension fills the surplus into the higher-RelativeOrder stage
        // (BUILD_THRESHOLD_DEVELOPMENT) before ever restoring the lower-RelativeOrder
        // stage's own former baseline.
        var skeleton = BuildPhaseOnlySkeleton(buildWeeks: 4);
        var progression = Hm4ProposedBuildProgression();

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));

        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());
        var introCount = byStage.GetValueOrDefault("BUILD_FASTER_THAN_HM_INTRO", 0);
        var thresholdCount = byStage.GetValueOrDefault("BUILD_THRESHOLD_DEVELOPMENT", 0);

        // Real, observed mechanism result:
        Assert.Equal(0, introCount);
        Assert.Equal(4, thresholdCount);

        // This is NOT the frozen 14W preferred occupancy (1+3) -- confirming the capability
        // gap: HM.4's Layer B minimumExposures=0 value for a low-RelativeOrder stage cannot
        // be authored as-is without perturbing the already-frozen 14W golden vertical slice's
        // stage occupancy through this real, unmodified allocator.
        Assert.False(introCount == 1 && thresholdCount == 3,
            "If this ever becomes true, the capability gap this test documents has been closed " +
            "(e.g. a future PreferredExposures field) and HM.4's literal numbers may be authored verbatim.");
    }

    [Fact]
    public void Hm4ProposedIntroMinimumZero_At3WeekCompressedBuildLength_ProducesZeroPlusThreeAsHm4Predicted()
    {
        // At the compressed 10-12W horizons, Build's phase length is exactly 3 (HM.4 Layer A).
        // Here totalMinimum(0+3=3) already equals availableWeeks(3), so no compression or
        // extension adjustment triggers, and the mechanism happens to agree with HM.4's own
        // predicted table (0+3). This is the one region where HM.4's stage numbers and the
        // real allocator's mechanism agree by coincidence of the totals matching exactly.
        var skeleton = BuildPhaseOnlySkeleton(buildWeeks: 3);
        var progression = Hm4ProposedBuildProgression();

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));

        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(0, byStage.GetValueOrDefault("BUILD_FASTER_THAN_HM_INTRO", 0));
        Assert.Equal(3, byStage.GetValueOrDefault("BUILD_THRESHOLD_DEVELOPMENT", 0));
    }

    [Fact]
    public void ApplyCompression_NeverReducesACompressibleStageBelowOneExposure_RegardlessOfDeclaredMinimum()
    {
        // Direct proof of ApplyCompression's hardcoded floor: even a phase with only 2
        // available weeks and two Compressible stages whose declared MinimumExposures sum to
        // more than 2 compresses each stage toward 1, never toward a lower catalog-declared
        // value (there is none below 1 the algorithm will ever apply)-- this is the same
        // mechanism that makes RACE_SPECIFIC's real floor (INTRODUCTION 1 + DEVELOPMENT 1 +
        // REHEARSAL 2 [PROTECTED, never reduced]) equal to 4, one more than HM.4's proposed
        // phase-level RaceSpecific minimum of 3.
        var skeleton = BuildPhaseOnlySkeleton(buildWeeks: 2);
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "TEST_HM5_COMPRESSION_FLOOR_PROOF",
            Version = 1,
            DistanceFamily = "HALF_MARATHON",
            PhaseProgressions = new[]
            {
                new CatalogPhaseWorkoutProgression
                {
                    PhaseKey = "BUILD",
                    Stages = new[]
                    {
                        new CatalogWorkoutProgressionStage
                        {
                            ProgressionStageKey = "STAGE_A",
                            RelativeOrder = 1,
                            MinimumExposures = 2,
                            MaximumExposures = 2,
                            CompressionBehavior = CatalogStageCompressionBehavior.Compressible,
                            ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
                            Requires = Array.Empty<CatalogRuntimeEligibilityCondition>(),
                        },
                        new CatalogWorkoutProgressionStage
                        {
                            ProgressionStageKey = "STAGE_B",
                            RelativeOrder = 2,
                            MinimumExposures = 2,
                            MaximumExposures = 2,
                            CompressionBehavior = CatalogStageCompressionBehavior.Compressible,
                            ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
                            Requires = Array.Empty<CatalogRuntimeEligibilityCondition>(),
                        },
                    },
                },
            },
        };

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        // Deficit is 2 (totalMinimum 4 vs availableWeeks 2). Compression order is highest
        // RelativeOrder first: STAGE_B reduces from 2 toward floor 1 (reduceBy=1, deficit->1),
        // then STAGE_A reduces from 2 toward floor 1 (reduceBy=1, deficit->0). Neither ever
        // reaches 0.
        Assert.Equal(1, byStage["STAGE_A"]);
        Assert.Equal(1, byStage["STAGE_B"]);
    }
}
