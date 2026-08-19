using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Metadata;
using PlanCatalog.Core.Models;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Projection;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split B — end-to-end wiring proof: a lane-authored, profile-backed
/// progression's stage candidates flow through <see cref="PrescriptionProjectionDependencyResolver"/>
/// into the existing, unmodified <see cref="CatalogBundleAssembler"/> exact-dependency overload
/// (FREQ.6D.3C), producing a non-null <c>ExecutionPrescriptions</c> library. No real
/// RUN_LAYOUT_5D/combination exists yet (Split E's own disclosed job — FREQ.6D.4D §41) so this
/// reuses the existing 4D <see cref="CombinationFixture"/> layout/combination shell with an
/// internal, non-public, lane-authored progression substituted in — proving the mechanism, not
/// claiming a real 5D candidate now exists.
/// </summary>
public sealed class PrescriptionProjectionDependencyBundleAssemblyTests
{
    private static readonly SystemTextJsonCanonicalSerializer Serializer = new();
    private static readonly Sha256ContentHasher Hasher = new();

    private static CatalogDocumentMetadata Metadata(string type, string key, int version = 1) =>
        new() { DocumentType = type, SchemaVersion = 1, Key = key, Version = version, Status = CatalogStatus.Published };

    private static VersionedCatalogReference ProfileRef(string key, int version = 1) =>
        new() { DocumentType = DocumentTypes.WorkoutPrescriptionProfile, Key = key, Version = version };

    private static WorkoutPrescriptionProfile Profile(string key, PrescriptionDoseCategory dose, VersionedCatalogReference workoutRef) => new()
    {
        Metadata = Metadata(DocumentTypes.WorkoutPrescriptionProfile, key),
        WorkoutDefinitionRef = workoutRef,
        DoseCategory = dose,
        DistanceAccountingMode = DistanceAccountingMode.EstimatedSessionTotal,
        Components =
        [
            new PrescriptionProfileComponent
            {
                SequenceOrder = 1, ComponentType = WorkoutComponentType.MainSet, StructureMode = PrescriptionStructureMode.Continuous,
                WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 1200 },
                IntensityTarget = new PrescriptionIntensityTarget { Mode = PrescriptionIntensityMode.EffortBased, EffortDescriptorKey = "THRESHOLD" }
            }
        ]
    };

    private static WorkoutProgressionStageDefinition Stage(string key, VersionedCatalogReference workoutRef, params VersionedCatalogReference[] profileCandidates) => new()
    {
        StageKey = key, RelativeOrder = 1, WorkoutCandidates = [workoutRef],
        MinimumExposures = 1, MaximumExposures = 2,
        CompressionBehavior = StageCompressionBehavior.Compressible, ExtensionBehavior = StageExtensionBehavior.Extendable,
        Requires = [], PrescriptionProfileCandidates = profileCandidates.Length == 0 ? null : profileCandidates,
    };

    private sealed record Rig(CatalogSourceSnapshot Snapshot, WorkoutProgressionDefinition Progression, CombinationFixture Base, WorkoutPrescriptionProfile Lane0, WorkoutPrescriptionProfile Lane1);

    private static Rig BuildDualLaneRig()
    {
        var fixture = new CombinationFixture();
        var thresholdRef = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = fixture.ThresholdWorkout.Metadata.Key, Version = fixture.ThresholdWorkout.Metadata.Version };

        var lane0Profile = Profile("FND_PRIMARY", PrescriptionDoseCategory.Primary, thresholdRef);
        var lane1Profile = Profile("FND_SECONDARY_CONTROLLED", PrescriptionDoseCategory.SecondaryControlled, thresholdRef);

        var progression = fixture.WorkoutProgression with
        {
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Build, Stages = [],
                    Lanes =
                    [
                        new() { LaneOrdinal = 0, Stages = [Stage("LANE0_STAGE", thresholdRef, ProfileRef(lane0Profile.Metadata.Key))] },
                        new() { LaneOrdinal = 1, Stages = [Stage("LANE1_STAGE", thresholdRef, ProfileRef(lane1Profile.Metadata.Key))] },
                    ],
                }
            ]
        };

        var levelModifier = fixture.LevelModifier with { EligibleWorkoutKeys = null, EligibleWorkouts = [thresholdRef] };

        var snapshot = CatalogStamper.StampAsPublished(Serializer, Hasher, new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(levelModifier)
            .With(progression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .With(fixture.Combination)
            .With(lane0Profile).With(lane1Profile)
            .Build());

        return new Rig(snapshot, progression, fixture, lane0Profile, lane1Profile);
    }

    [Fact]
    public void DualLaneProfileBackedCombination_ProducesNonNullExecutionPrescriptions_BothLanesReachable()
    {
        var rig = BuildDualLaneRig();
        var dependencies = PrescriptionProjectionDependencyResolver.ResolveForProgression(rig.Progression);
        var assembler = new CatalogBundleAssembler(Serializer, Hasher);

        var bundle = assembler.Assemble(rig.Snapshot, rig.Base.Combination.Metadata.Key, rig.Base.Combination.Metadata.Version, dependencies);

        Assert.NotNull(bundle.ExecutionPrescriptions);
        Assert.Equal(2, bundle.ExecutionPrescriptions!.Count);
        Assert.Contains(bundle.ExecutionPrescriptions, p => p.SourceProfile.Key == rig.Lane0.Metadata.Key);
        Assert.Contains(bundle.ExecutionPrescriptions, p => p.SourceProfile.Key == rig.Lane1.Metadata.Key);
    }

    [Fact]
    public void BundleClosure_EveryStageCandidateHasMatchingExecutionPrescription()
    {
        var rig = BuildDualLaneRig();
        var dependencies = PrescriptionProjectionDependencyResolver.ResolveForProgression(rig.Progression);
        var assembler = new CatalogBundleAssembler(Serializer, Hasher);

        var bundle = assembler.Assemble(rig.Snapshot, rig.Base.Combination.Metadata.Key, rig.Base.Combination.Metadata.Version, dependencies);

        var stageProfileRefs = rig.Progression.PhaseProgressions
            .SelectMany(p => p.EffectiveLanes).SelectMany(l => l.Stages)
            .SelectMany(s => s.PrescriptionProfileCandidates ?? Enumerable.Empty<VersionedCatalogReference>())
            .ToList();

        Assert.Equal(2, stageProfileRefs.Count);
        foreach (var stageRef in stageProfileRefs)
        {
            Assert.Contains(bundle.ExecutionPrescriptions!, p => p.SourceProfile.Key == stageRef.Key && p.SourceProfile.Version == stageRef.Version);
        }
    }

    [Fact]
    public void RepeatedAssembly_SameDependencies_DeterministicIdenticalBundleHash()
    {
        var rig = BuildDualLaneRig();
        var dependencies = PrescriptionProjectionDependencyResolver.ResolveForProgression(rig.Progression);
        var assembler = new CatalogBundleAssembler(Serializer, Hasher);

        var bundle1 = assembler.Assemble(rig.Snapshot, rig.Base.Combination.Metadata.Key, rig.Base.Combination.Metadata.Version, dependencies);
        var bundle2 = assembler.Assemble(rig.Snapshot, rig.Base.Combination.Metadata.Key, rig.Base.Combination.Metadata.Version, dependencies);

        Assert.Equal(bundle1.BundleContentHash, bundle2.BundleContentHash);
        Assert.Equal(
            bundle1.ExecutionPrescriptions!.Select(p => (p.SourceProfile.Key, p.SourceProfile.Version)),
            bundle2.ExecutionPrescriptions!.Select(p => (p.SourceProfile.Key, p.SourceProfile.Version)));
    }

    [Fact]
    public void LegacyBundle_NoExecutionDependenciesOverload_ExecutionPrescriptionsStaysNull()
    {
        var fixture = new CombinationFixture();
        var snapshot = CatalogStamper.StampAsPublished(Serializer, Hasher, fixture.BuildSnapshot());
        var assembler = new CatalogBundleAssembler(Serializer, Hasher);

        var bundle = assembler.Assemble(snapshot, fixture.Combination.Metadata.Key, fixture.Combination.Metadata.Version);

        Assert.Null(bundle.ExecutionPrescriptions);
    }

    [Fact]
    public void NoCandidatesDeclared_EmptyDependencyList_ExecutionPrescriptionsStaysNull()
    {
        var fixture = new CombinationFixture();
        var snapshot = CatalogStamper.StampAsPublished(Serializer, Hasher, fixture.BuildSnapshot());
        var assembler = new CatalogBundleAssembler(Serializer, Hasher);
        var emptyDependencies = PrescriptionProjectionDependencyResolver.ResolveForProgression(fixture.WorkoutProgression);

        Assert.Empty(emptyDependencies);

        var bundle = assembler.Assemble(snapshot, fixture.Combination.Metadata.Key, fixture.Combination.Metadata.Version, emptyDependencies);

        Assert.Null(bundle.ExecutionPrescriptions);
    }
}
