using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Catalog;

/// <summary>Phase 10K-FREQ.6D.4D Split B — <see cref="PrescriptionProfileClosureResolver"/>: dedupe by exact (Key, Version), deterministic ordering, lane-aware.</summary>
public sealed class PrescriptionProfileClosureResolverTests
{
    private static VersionedCatalogReference ProfileRef(string key, int version = 1) =>
        new() { DocumentType = DocumentTypes.WorkoutPrescriptionProfile, Key = key, Version = version };

    private static WorkoutProgressionStageDefinition Stage(string key, params VersionedCatalogReference[] candidates) => new()
    {
        StageKey = key, RelativeOrder = 1, MinimumExposures = 1, MaximumExposures = 1,
        CompressionBehavior = StageCompressionBehavior.Compressible, ExtensionBehavior = StageExtensionBehavior.Extendable,
        Requires = [], PrescriptionProfileCandidates = candidates.Length == 0 ? null : candidates,
    };

    [Fact]
    public void DistinctRefsAcrossLanes_AllIncluded_DeterministicKeyThenVersionOrder()
    {
        var progression = new WorkoutProgressionDefinition
        {
            Metadata = Meta.Of(DocumentTypes.WorkoutProgression, "P", status: CatalogStatus.Published),
            DistanceFamily = DistanceFamily.TenK,
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Foundation, Stages = [],
                    Lanes =
                    [
                        new() { LaneOrdinal = 0, Stages = [Stage("S0", ProfileRef("ZEBRA")), Stage("S0B", ProfileRef("ALPHA", 2))] },
                        new() { LaneOrdinal = 1, Stages = [Stage("S1", ProfileRef("ALPHA", 1))] },
                    ],
                }
            ]
        };

        var refs = PrescriptionProfileClosureResolver.ComputeExactClosureRefs(progression);

        Assert.Equal(3, refs.Count);
        Assert.Equal(["ALPHA", "ALPHA", "ZEBRA"], refs.Select(r => r.Key));
        Assert.Equal([1, 2], refs.Where(r => r.Key == "ALPHA").Select(r => r.Version));
    }

    [Fact]
    public void SharedProfileAcrossLanes_Deduplicated()
    {
        var shared = ProfileRef("FARTLEK_SECONDARY", 5);
        var progression = new WorkoutProgressionDefinition
        {
            Metadata = Meta.Of(DocumentTypes.WorkoutProgression, "P", status: CatalogStatus.Published),
            DistanceFamily = DistanceFamily.TenK,
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Build, Stages = [],
                    Lanes =
                    [
                        new() { LaneOrdinal = 0, Stages = [Stage("BUILD_S", shared)] },
                    ],
                },
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Taper, Stages = [],
                    Lanes =
                    [
                        new() { LaneOrdinal = 1, Stages = [Stage("TAPER_S", shared)] },
                    ],
                },
            ]
        };

        var refs = PrescriptionProfileClosureResolver.ComputeExactClosureRefs(progression);

        Assert.Single(refs);
        Assert.Equal(shared, refs[0]);
    }

    [Fact]
    public void NoCandidatesAnywhere_EmptyClosure()
    {
        var progression = new WorkoutProgressionDefinition
        {
            Metadata = Meta.Of(DocumentTypes.WorkoutProgression, "P", status: CatalogStatus.Published),
            DistanceFamily = DistanceFamily.TenK,
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition { PhaseKey = PhaseKey.Foundation, Stages = [Stage("S0")] }
            ]
        };

        var refs = PrescriptionProfileClosureResolver.ComputeExactClosureRefs(progression);

        Assert.Empty(refs);
    }

    [Fact]
    public void LegacySingleLanePhase_NoLanesDeclared_ClosureStillResolvesViaEffectiveLanes()
    {
        var progression = new WorkoutProgressionDefinition
        {
            Metadata = Meta.Of(DocumentTypes.WorkoutProgression, "P", status: CatalogStatus.Published),
            DistanceFamily = DistanceFamily.TenK,
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition { PhaseKey = PhaseKey.Foundation, Stages = [Stage("S0", ProfileRef("SOLO"))] }
            ]
        };

        var refs = PrescriptionProfileClosureResolver.ComputeExactClosureRefs(progression);

        Assert.Single(refs);
        Assert.Equal("SOLO", refs[0].Key);
    }
}
