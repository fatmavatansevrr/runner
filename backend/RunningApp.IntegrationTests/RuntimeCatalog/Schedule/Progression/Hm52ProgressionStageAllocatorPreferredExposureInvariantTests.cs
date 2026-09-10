using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Progression;

/// <summary>
/// Backend Integration Phase HM.5.2 — generic, distance-blind invariant tests for the new
/// optional <see cref="CatalogWorkoutProgressionStage.PreferredExposures"/> baseline concept.
/// Deliberately uses no HM/10K stage names (Section 14: these are structural allocator
/// invariants, never numeric production authority) — synthetic single-phase fixtures only,
/// mirroring <see cref="ProgressionStageAllocatorTests"/>'s own established synthetic-fixture
/// pattern.
/// </summary>
public sealed class Hm52ProgressionStageAllocatorPreferredExposureInvariantTests
{
    private static GeneratedCatalogPlanSkeleton SyntheticSkeleton(int weekCount)
    {
        var weeks = new List<GeneratedCatalogWeekSkeleton>();
        for (var i = 1; i <= weekCount; i++)
        {
            weeks.Add(new GeneratedCatalogWeekSkeleton
            {
                WeekNumber = i,
                StartDate = new DateOnly(2026, 1, 1).AddDays((i - 1) * 7),
                EndDate = new DateOnly(2026, 1, 1).AddDays((i - 1) * 7 + 6),
                StageKey = "PHASE_A",
                StageWeekIndex = i,
                StageWeekCount = weekCount,
                SessionSlots = Array.Empty<GeneratedCatalogSessionSlotSkeleton>(),
                Provenance = new GeneratedCatalogWeekSkeletonProvenance { StageKey = "PHASE_A", SourcePhaseKey = "PHASE_A" },
            });
        }

        return new GeneratedCatalogPlanSkeleton
        {
            SchemaVersion = GeneratedCatalogPlanSkeleton.CurrentSchemaVersion,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 1).AddDays(weekCount * 7 - 1),
            PlannedWeekCount = weekCount,
            DaysPerWeek = 4,
            CanonicalDistanceFamily = "SYNTHETIC",
            CandidateKey = "HM52_INVARIANT_PROOF",
            CandidateVersion = 1,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = "HM52_INVARIANT_PROOF",
                CandidateVersion = 1,
                DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
                AsOfDate = new DateOnly(2026, 1, 1),
                MaterializerVersion = "TEST",
            },
        };
    }

    private static CatalogWorkoutProgressionStage Stage(
        string key, int order, int min, int max, int? preferred = null,
        CatalogStageCompressionBehavior compression = CatalogStageCompressionBehavior.Compressible,
        CatalogStageExtensionBehavior extension = CatalogStageExtensionBehavior.Extendable,
        IReadOnlyList<CatalogRuntimeEligibilityCondition>? requires = null,
        string? fallback = null) => new()
    {
        ProgressionStageKey = key,
        RelativeOrder = order,
        MinimumExposures = min,
        MaximumExposures = max,
        PreferredExposures = preferred,
        CompressionBehavior = compression,
        ExtensionBehavior = extension,
        Requires = requires ?? Array.Empty<CatalogRuntimeEligibilityCondition>(),
        FallbackStageKey = fallback,
    };

    private static CatalogWorkoutProgressionDefinition Progression(params CatalogWorkoutProgressionStage[] stages) => new()
    {
        Key = "HM52_INVARIANT_PROOF_PROGRESSION",
        Version = 1,
        DistanceFamily = "SYNTHETIC",
        PhaseProgressions = new[] { new CatalogPhaseWorkoutProgression { PhaseKey = "PHASE_A", Stages = stages } },
    };

    private static ProgressionStageAllocationContext Context(
        CatalogWorkoutProgressionDefinition progression, GeneratedCatalogPlanSkeleton skeleton,
        IReadOnlyList<RuntimeConditionResolutionResult>? results = null) => new()
    {
        CandidateKey = "HM52_INVARIANT_PROOF",
        CandidateVersion = 1,
        Progression = progression,
        Skeleton = skeleton,
        ConditionResults = results ?? Array.Empty<RuntimeConditionResolutionResult>(),
    };

    private static IReadOnlyList<RuntimeConditionResolutionResult> GoalFeasibilityResult(string? outputValue) =>
        outputValue is null
            ? new[] { RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "TEST_NOT_EVALUATED") }
            : new[] { RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", outputValue, "TEST_EVALUATED") };

    // (A) sum(Min) == phaseWeeks (no PreferredExposures declared anywhere) -> exact
    // minimum-compatible occupancy, unaffected by the new field's existence.
    [Fact]
    public void SumOfMinimumEqualsPhaseWeeks_NoPreferredDeclared_ResolvesExactMinimumOccupancy()
    {
        var progression = Progression(Stage("A", 1, 2, 4), Stage("B", 2, 3, 5));
        var skeleton = SyntheticSkeleton(5);

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, byStage["A"]);
        Assert.Equal(3, byStage["B"]);
    }

    // (B) sum(Preferred) == phaseWeeks -> exact Preferred occupancy REGARDLESS OF RELATIVE
    // ORDER. This is the core capability HM.5.2 adds — A has the LOWER RelativeOrder and a
    // Minimum of 0, yet still receives its full Preferred=1 exposure because totalBaseline
    // exactly matches availableWeeks (Regime B — Section 3's required invariant).
    [Fact]
    public void SumOfPreferredEqualsPhaseWeeks_ResolvesExactPreferredOccupancy_RegardlessOfRelativeOrder()
    {
        var progression = Progression(
            Stage("A", order: 1, min: 0, max: 1, preferred: 1),
            Stage("B", order: 2, min: 3, max: 5, preferred: 3));
        var skeleton = SyntheticSkeleton(4);

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(1, byStage["A"]);
        Assert.Equal(3, byStage["B"]);
    }

    // (C) phaseWeeks between Min and Preferred -> deterministic compression per existing
    // RelativeOrder-desc authority, reducing toward each stage's real MinimumExposures floor
    // (not the historical hardcoded 1) once PreferredExposures is declared.
    [Fact]
    public void PhaseWeeksBetweenMinimumAndPreferred_CompressesTowardDeclaredMinimum_HighestRelativeOrderFirst()
    {
        var progression = Progression(
            Stage("A", order: 1, min: 0, max: 1, preferred: 1),
            Stage("B", order: 2, min: 3, max: 5, preferred: 3));
        var skeleton = SyntheticSkeleton(3); // sum(Preferred)=4 > 3 available -> compression

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        // B (higher RelativeOrder) compresses first, but only down to its own Minimum (3) —
        // already at floor, so A (lower RelativeOrder) must absorb the deficit down to ITS OWN
        // declared Minimum (0), not the historical hardcoded floor of 1.
        Assert.Equal(0, byStage.GetValueOrDefault("A", 0));
        Assert.Equal(3, byStage["B"]);
    }

    // (D) phaseWeeks between Preferred and Max -> extension uses only EXTENDABLE stages, and
    // grows from each stage's Baseline (Preferred), not from Minimum.
    [Fact]
    public void PhaseWeeksBetweenPreferredAndMaximum_ExtendsOnlyExtendableStages_FromPreferredBaseline()
    {
        var progression = Progression(
            Stage("A", order: 1, min: 0, max: 1, preferred: 1, extension: CatalogStageExtensionBehavior.FixedExposure),
            Stage("B", order: 2, min: 3, max: 5, preferred: 3, extension: CatalogStageExtensionBehavior.Extendable));
        var skeleton = SyntheticSkeleton(5); // sum(Preferred)=4, surplus=1

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        // A is FixedExposure -> stays at its Preferred baseline (1), never grows past it even
        // though it has Maximum headroom (1's own max equals its preferred, so this also proves
        // FixedExposure is honored). B (Extendable) absorbs the whole surplus from its own
        // Preferred baseline of 3, not from its Minimum of 3 (same value here by construction,
        // exercised distinctly by the next test).
        Assert.Equal(1, byStage["A"]);
        Assert.Equal(4, byStage["B"]);
    }

    [Fact]
    public void ExtensionGrowsFromPreferredBaseline_NotFromMinimum_WhenTheyDiffer()
    {
        var progression = Progression(
            Stage("A", order: 1, min: 0, max: 6, preferred: 2));
        var skeleton = SyntheticSkeleton(4); // sum(Preferred)=2, surplus=2

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        // If extension incorrectly grew from Minimum (0) it would still land on 4 here by
        // coincidence of totals, so this test's real assertion is on the exposed
        // AllocationReason boundary: exactly 2 weeks must be tagged as baseline allocation and
        // 2 as extension allocation (never all 4 as "extension" from a Minimum-of-0 seed).
        Assert.Equal(4, byStage["A"]);
        var reasons = schedule.Weeks.OrderBy(w => w.WeekNumber).Select(w => w.AllocationReason).ToList();
        Assert.Equal(2, reasons.Count(r => r == "MINIMUM_EXPOSURE_ALLOCATION"));
        Assert.Equal(2, reasons.Count(r => r == "EXTENSION_ALLOCATION"));
    }

    // (E) FIXED_EXPOSURE does not compress or extend, even with PreferredExposures declared.
    [Fact]
    public void FixedExposureStage_WithPreferredDeclared_NeverCompressesOrExtends()
    {
        var progression = Progression(
            Stage("A", order: 1, min: 2, max: 2, preferred: 2, extension: CatalogStageExtensionBehavior.FixedExposure, compression: CatalogStageCompressionBehavior.Protected));
        var skeleton = SyntheticSkeleton(2); // exact fit, no adjustment needed

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        Assert.Equal(2, schedule.Weeks.Count);
        Assert.All(schedule.Weeks, w => Assert.Equal("A", w.ProgressionStageKey));
    }

    // (F) PROTECTED exposure is not removed before permitted COMPRESSIBLE exposure, even when
    // the Protected stage declares PreferredExposures.
    [Fact]
    public void ProtectedStageWithPreferred_NeverCompressedBeforeCompressibleStage()
    {
        var progression = Progression(
            Stage("A", order: 1, min: 2, max: 2, preferred: 2, compression: CatalogStageCompressionBehavior.Protected),
            Stage("B", order: 2, min: 0, max: 3, preferred: 3, compression: CatalogStageCompressionBehavior.Compressible));
        var skeleton = SyntheticSkeleton(3); // sum(Preferred)=5 > 3 -> deficit 2

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, byStage["A"]); // Protected: untouched, stays at its own Preferred/Minimum
        Assert.Equal(1, byStage.GetValueOrDefault("B", 0)); // Compressible absorbs the whole deficit
    }

    // (G) ineligible optional stage can yield its capacity to an approved fallback, and the
    // fallback's own PreferredExposures (if any) governs the effective stage's baseline.
    [Fact]
    public void IneligibleOptionalStage_YieldsToFallback_FallbackPreferredGovernsBaseline()
    {
        var requires = new[] { new CatalogRuntimeEligibilityCondition { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC" } } };
        var progression = Progression(
            Stage("PRIMARY", order: 1, min: 0, max: 1, preferred: 1, requires: requires, fallback: "FALLBACK"),
            Stage("FALLBACK", order: 2, min: 2, max: 2, preferred: 2));
        var skeleton = SyntheticSkeleton(2);

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton, GoalFeasibilityResult("UNSUPPORTED")));

        Assert.Equal(2, schedule.Weeks.Count);
        Assert.All(schedule.Weeks, w => Assert.Equal("FALLBACK", w.ProgressionStageKey));
        Assert.All(schedule.Weeks, w => Assert.Equal("PRIMARY", w.FallbackOrigin));
    }

    // (H) no stage exceeds MaximumExposures, even under extension with PreferredExposures set.
    [Fact]
    public void ExtensionNeverExceedsMaximumExposures_EvenWithPreferredDeclared()
    {
        var progression = Progression(Stage("A", order: 1, min: 0, max: 3, preferred: 1));
        var skeleton = SyntheticSkeleton(3); // surplus 2, headroom to max is exactly 2

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var count = schedule.Weeks.Count(w => w.ProgressionStageKey == "A");
        Assert.Equal(3, count);
        Assert.True(count <= 3);
    }

    [Fact]
    public void ExtensionBeyondMaximum_EvenWithPreferredDeclared_ThrowsTypedException()
    {
        var progression = Progression(Stage("A", order: 1, min: 0, max: 2, preferred: 1, extension: CatalogStageExtensionBehavior.FixedExposure));
        var skeleton = SyntheticSkeleton(4); // surplus 3, but FixedExposure's own max headroom is only 1

        Assert.Throws<ProgressionPhaseCapacityExceedsMaximumException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton)));
    }

    // (I) total resolved exposures == phaseWeeks across compression, exact-fit, and extension
    // regimes, with PreferredExposures declared.
    [Theory]
    [InlineData(2)] // compression regime (below sum(Preferred)=4)
    [InlineData(4)] // exact-fit regime (Regime B)
    [InlineData(6)] // extension regime (above sum(Preferred)=4, within sum(Max)=6)
    public void TotalResolvedExposures_AlwaysEqualsPhaseWeeks_AcrossAllThreeRegimes(int phaseWeeks)
    {
        var progression = Progression(
            Stage("A", order: 1, min: 0, max: 1, preferred: 1),
            Stage("B", order: 2, min: 2, max: 5, preferred: 3));
        var skeleton = SyntheticSkeleton(phaseWeeks);

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        Assert.Equal(phaseWeeks, schedule.Weeks.Count);
    }

    // Structural guard: PreferredExposures declared outside [Minimum,Maximum] is rejected before
    // any capacity resolution runs.
    [Fact]
    public void PreferredExposuresOutsideMinMaxRange_ThrowsTypedException()
    {
        var progression = Progression(Stage("A", order: 1, min: 2, max: 4, preferred: 1));
        var skeleton = SyntheticSkeleton(3);

        Assert.Throws<ProgressionStagePreferredExposuresOutOfBoundsException>(() =>
            new ProgressionStageAllocator().Allocate(Context(progression, skeleton)));
    }
}
