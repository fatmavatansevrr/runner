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
/// Backend Integration Phase HM.5.2A — generic, distance-blind invariant tests for the new
/// optional <see cref="CatalogWorkoutProgressionStage.CompressionPriority"/> compression-removal
/// order concept. Deliberately uses no HM/10K stage names — synthetic single-phase fixtures
/// only, mirroring <see cref="ProgressionStageAllocatorTests"/> and
/// <see cref="Hm52ProgressionStageAllocatorPreferredExposureInvariantTests"/>'s own established
/// synthetic-fixture pattern. See PHASE_HM_5_2A_STAGE_COMPRESSION_PRIORITY_SEMANTIC_CLOSURE.md
/// Section 7 for the invariant list these tests prove.
/// </summary>
public sealed class Hm52aProgressionStageAllocatorCompressionPriorityInvariantTests
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
            CandidateKey = "HM52A_INVARIANT_PROOF",
            CandidateVersion = 1,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = "HM52A_INVARIANT_PROOF",
                CandidateVersion = 1,
                DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
                AsOfDate = new DateOnly(2026, 1, 1),
                MaterializerVersion = "TEST",
            },
        };
    }

    private static CatalogWorkoutProgressionStage Stage(
        string key, int order, int min, int max, int? preferred = null, int? compressionPriority = null,
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
        CompressionPriority = compressionPriority,
        CompressionBehavior = compression,
        ExtensionBehavior = extension,
        Requires = requires ?? Array.Empty<CatalogRuntimeEligibilityCondition>(),
        FallbackStageKey = fallback,
    };

    private static CatalogWorkoutProgressionDefinition Progression(params CatalogWorkoutProgressionStage[] stages) => new()
    {
        Key = "HM52A_INVARIANT_PROOF_PROGRESSION",
        Version = 1,
        DistanceFamily = "SYNTHETIC",
        PhaseProgressions = new[] { new CatalogPhaseWorkoutProgression { PhaseKey = "PHASE_A", Stages = stages } },
    };

    private static ProgressionStageAllocationContext Context(
        CatalogWorkoutProgressionDefinition progression, GeneratedCatalogPlanSkeleton skeleton,
        IReadOnlyList<RuntimeConditionResolutionResult>? results = null) => new()
    {
        CandidateKey = "HM52A_INVARIANT_PROOF",
        CandidateVersion = 1,
        Progression = progression,
        Skeleton = skeleton,
        ConditionResults = results ?? Array.Empty<RuntimeConditionResolutionResult>(),
    };

    // (A) An optional early stage (Min0/Preferred1/Max1, CompressionPriority=1 — the
    // lowest-information exposure) plus a protected/higher-value later stage
    // (Min2/Preferred3/Max4, CompressionPriority=2) — compressed capacity removes the OPTIONAL
    // exposure first when explicit compression authority says so, even though it has the LOWER
    // RelativeOrder (the historical, still-default-fallback tie-break would have reduced the
    // HIGHER-RelativeOrder stage first — this proves CompressionPriority overrides that default).
    [Fact]
    public void ExplicitCompressionPriority_RemovesLowPriorityOptionalStageFirst_EvenWithLowerRelativeOrder()
    {
        var progression = Progression(
            Stage("OPTIONAL_INTRO", order: 1, min: 0, max: 1, preferred: 1, compressionPriority: 1),
            Stage("CORE_DEVELOPMENT", order: 2, min: 2, max: 4, preferred: 3, compressionPriority: 2));
        var skeleton = SyntheticSkeleton(3); // sum(Preferred)=4 > 3 -> deficit 1

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(0, byStage.GetValueOrDefault("OPTIONAL_INTRO", 0));
        Assert.Equal(3, byStage["CORE_DEVELOPMENT"]);
    }

    // (B) Preferred-size capacity resolves the exact Preferred vector regardless of
    // CompressionPriority being declared at all (Regime B never invokes ApplyCompression).
    [Fact]
    public void PreferredSizeCapacity_ResolvesExactPreferredVector_CompressionPriorityDeclaredButUnused()
    {
        var progression = Progression(
            Stage("A", order: 1, min: 0, max: 1, preferred: 1, compressionPriority: 1),
            Stage("B", order: 2, min: 2, max: 4, preferred: 3, compressionPriority: 2));
        var skeleton = SyntheticSkeleton(4); // sum(Preferred) == 4, exact fit

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(1, byStage["A"]);
        Assert.Equal(3, byStage["B"]);
    }

    // (C) Extension still follows the existing, untouched extension authority (highest
    // RelativeOrder first) even when CompressionPriority is declared on the same stages — the
    // two authorities are independent.
    [Fact]
    public void ExtensionCapacity_StillFollowsRelativeOrderDescending_EvenWithCompressionPriorityDeclared()
    {
        var progression = Progression(
            Stage("A", order: 1, min: 0, max: 1, preferred: 1, compressionPriority: 1),
            Stage("B", order: 2, min: 2, max: 5, preferred: 3, compressionPriority: 2));
        var skeleton = SyntheticSkeleton(5); // sum(Preferred)=4, surplus=1

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        // B has the higher RelativeOrder -> receives the extension surplus first, exactly as
        // pre-HM.5.2A, regardless of A's lower CompressionPriority value.
        Assert.Equal(1, byStage["A"]);
        Assert.Equal(4, byStage["B"]);
    }

    // (D) Changing compression priority does NOT alter chronological (week-block layout) order —
    // still strictly ascending RelativeOrder regardless of which stage compressed away exposure.
    [Fact]
    public void CompressionPriority_DoesNotAlterChronologicalWeekBlockOrder()
    {
        var progression = Progression(
            Stage("A", order: 1, min: 0, max: 1, preferred: 1, compressionPriority: 1),
            Stage("B", order: 2, min: 2, max: 4, preferred: 3, compressionPriority: 2));
        var skeleton = SyntheticSkeleton(3); // A compresses to 0, B stays at 3

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var orderedStages = schedule.Weeks.OrderBy(w => w.WeekNumber).Select(w => w.ProgressionStageKey).ToList();

        // A resolves to zero weeks, so the entire block is B, in ascending week order — chronology
        // is untouched by which stage lost its exposure to compression.
        Assert.Equal(new[] { "B", "B", "B" }, orderedStages);
    }

    // (E) Changing compression priority does NOT alter extension priority — proven directly by
    // (C) above (extension still fills B, the higher-RelativeOrder stage, first, even though A
    // holds the lower/compress-first CompressionPriority). Restated here as its own explicit
    // invariant with priorities reversed relative to RelativeOrder in the OTHER direction, to
    // rule out an accidental coupling in either priority-to-RelativeOrder relationship.
    [Fact]
    public void CompressionPriority_DoesNotAlterExtensionPriority_EvenWhenPrioritiesInvertRelativeOrder()
    {
        var progression = Progression(
            // A has the HIGHER RelativeOrder but the LOWER (compress-first) CompressionPriority.
            Stage("A", order: 2, min: 0, max: 5, preferred: 1, compressionPriority: 1),
            Stage("B", order: 1, min: 2, max: 4, preferred: 3, compressionPriority: 2));
        var skeleton = SyntheticSkeleton(5); // sum(Preferred)=4, surplus=1

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        // Extension is unchanged: A (higher RelativeOrder) still receives the surplus first,
        // regardless of its lower (compress-first) CompressionPriority.
        Assert.Equal(2, byStage["A"]);
        Assert.Equal(3, byStage["B"]);
    }

    // (F) A Protected stage can never be removed ahead of (or instead of) a Compressible stage,
    // even when the Protected stage would otherwise sort first by CompressionPriority.
    [Fact]
    public void ProtectedStage_NeverCompressedAheadOfCompressibleStage_EvenWithLowerCompressionPriority()
    {
        var progression = Progression(
            Stage("PROTECTED_A", order: 1, min: 2, max: 2, preferred: 2, compressionPriority: 1, compression: CatalogStageCompressionBehavior.Protected),
            Stage("COMPRESSIBLE_B", order: 2, min: 0, max: 3, preferred: 3, compressionPriority: 2, compression: CatalogStageCompressionBehavior.Compressible));
        var skeleton = SyntheticSkeleton(3); // sum(Preferred)=5 > 3 -> deficit 2

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, byStage["PROTECTED_A"]); // untouched despite CompressionPriority=1 (compress-first)
        Assert.Equal(1, byStage.GetValueOrDefault("COMPRESSIBLE_B", 0));
    }

    // (G) Total resolved occupancy always equals the requested phase capacity, across
    // compression/exact-fit/extension, when CompressionPriority is declared.
    [Theory]
    [InlineData(2)] // compression
    [InlineData(4)] // exact fit
    [InlineData(6)] // extension
    public void TotalResolvedExposures_AlwaysEqualsPhaseWeeks_WithCompressionPriorityDeclared(int phaseWeeks)
    {
        var progression = Progression(
            Stage("A", order: 1, min: 0, max: 1, preferred: 1, compressionPriority: 1),
            Stage("B", order: 2, min: 2, max: 5, preferred: 3, compressionPriority: 2));
        var skeleton = SyntheticSkeleton(phaseWeeks);

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        Assert.Equal(phaseWeeks, schedule.Weeks.Count);
    }

    // (H) A legacy catalog stage set that declares no CompressionPriority anywhere follows the
    // exact historical behavior: highest RelativeOrder compressed first (byte-identical to
    // pre-HM.5.2A / pre-HM.5.2 / original 4F.6A compression order).
    [Fact]
    public void LegacyCatalog_WithNoCompressionPriorityDeclared_FollowsHistoricalRelativeOrderDescendingCompression()
    {
        var progression = Progression(
            Stage("A", order: 1, min: 2, max: 2),
            Stage("B", order: 2, min: 3, max: 3));
        var skeleton = SyntheticSkeleton(4); // sum(Min)=5 > 4 -> deficit 1

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        // B (higher RelativeOrder) is reduced first, exactly as before this phase — no
        // CompressionPriority was declared, so the historical tie-break governs unchanged.
        Assert.Equal(2, byStage["A"]);
        Assert.Equal(2, byStage["B"]);
    }

    // Mixed-declaration case: one stage declares CompressionPriority, its sibling does not —
    // the undeclared stage sorts AFTER every declared priority (int.MaxValue), so it is only
    // ever reduced once every explicitly-prioritized Compressible stage's own headroom is
    // exhausted.
    [Fact]
    public void MixedDeclaration_UndeclaredCompressionPriorityStage_SortsAfterEveryDeclaredPriority()
    {
        var progression = Progression(
            Stage("DECLARED_LOW_PRIORITY", order: 1, min: 0, max: 2, preferred: 2, compressionPriority: 1),
            Stage("UNDECLARED", order: 2, min: 2, max: 2, preferred: 2));
        var skeleton = SyntheticSkeleton(3); // sum(Preferred)=4 > 3 -> deficit 1

        var schedule = new ProgressionStageAllocator().Allocate(Context(progression, skeleton));
        var byStage = schedule.Weeks.GroupBy(w => w.ProgressionStageKey).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(1, byStage["DECLARED_LOW_PRIORITY"]);
        Assert.Equal(2, byStage["UNDECLARED"]);
    }
}
