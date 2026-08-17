using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Projection;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Projection;

public sealed class PrescriptionBundleProjectionIntegrationTests
{
    private readonly SystemTextJsonCanonicalSerializer serializer = new();
    private readonly Sha256ContentHasher hasher = new();

    [Fact]
    public void ExactDependencies_ProjectIntoBundleInCanonicalOrderWithStableHash()
    {
        var (snapshot, refs) = SnapshotWithProfiles("Z_PROFILE", "A_PROFILE");
        var assembler = new CatalogBundleAssembler(serializer, hasher);
        var forward = assembler.Assemble(snapshot, "TEN_K__4D__INTERMEDIATE", 1, refs);
        var reverse = assembler.Assemble(snapshot, "TEN_K__4D__INTERMEDIATE", 1, refs.Reverse().ToList());
        Assert.Equal(["A_PROFILE", "Z_PROFILE"], forward.ExecutionPrescriptions!.Select(x => x.SourceProfile.Key));
        Assert.Equal(forward.BundleContentHash, reverse.BundleContentHash);
        Assert.Equal(serializer.Serialize(forward), serializer.Serialize(reverse));
    }

    [Fact]
    public void ProfileBackedProjection_IsValidatedSerializedAndHashCovered()
    {
        var (snapshot, refs) = SnapshotWithProfiles("PROFILE");
        var profile = snapshot.FindPrescriptionProfile("PROFILE", 1)!;
        var workout = snapshot.FindWorkout(profile.WorkoutDefinitionRef.Key, profile.WorkoutDefinitionRef.Version)!;
        Assert.True(WorkoutPrescriptionProfileValidator.Validate(profile, workout).IsValid);
        var bundle = new CatalogBundleAssembler(serializer, hasher).Assemble(snapshot, "TEN_K__4D__INTERMEDIATE", 1, refs);
        Assert.Single(bundle.ExecutionPrescriptions!);
        Assert.Empty(PlanCatalog.Contracts.Prescriptions.ExecutableWorkoutPrescriptionValidator.Validate(bundle.ExecutionPrescriptions![0]));
        Assert.Contains("executionPrescriptions", serializer.Serialize(bundle));
        Assert.NotEmpty(bundle.BundleContentHash);
    }

    [Fact]
    public void LegacyAssembly_RemainsProjectionNullAndHashIdentical()
    {
        var fixture = new CombinationFixture();
        var stamped = CatalogStamper.StampAsPublished(serializer, hasher, fixture.BuildSnapshot());
        var assembler = new CatalogBundleAssembler(serializer, hasher);
        var legacy = assembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 1);
        var explicitEmpty = assembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 1, []);
        Assert.Null(legacy.ExecutionPrescriptions);
        Assert.Null(explicitEmpty.ExecutionPrescriptions);
        Assert.Equal(legacy.BundleContentHash, explicitEmpty.BundleContentHash);
    }

    [Fact]
    public void ProjectedDoseChange_ChangesImmutableBundleHash()
    {
        var (snapshot, refs) = SnapshotWithProfiles("PROFILE");
        var assembler = new CatalogBundleAssembler(serializer, hasher);
        var original = assembler.Assemble(snapshot, "TEN_K__4D__INTERMEDIATE", 1, refs);
        var sourceProfile = snapshot.PrescriptionProfiles[0];
        var changedComponent = sourceProfile.Components[0] with
        {
            WorkQuantity = sourceProfile.Components[0].WorkQuantity! with { RepetitionCount = 6 }
        };
        var changedSource = snapshot with
        {
            PrescriptionProfiles = [sourceProfile with { Metadata = sourceProfile.Metadata with { ContentHash = null }, Components = [changedComponent] }]
        };
        var changedSnapshot = CatalogStamper.StampAsPublished(serializer, hasher, changedSource);
        var changed = assembler.Assemble(changedSnapshot, "TEN_K__4D__INTERMEDIATE", 1, refs);
        Assert.Equal(5, changed.ExecutionPrescriptions![0].Components[0].Recovery!.RecoveryCount);
        Assert.NotEqual(original.BundleContentHash, changed.BundleContentHash);
    }

    [Theory]
    [InlineData("missingProfile")]
    [InlineData("missingWorkout")]
    [InlineData("duplicate")]
    [InlineData("invalidReference")]
    public void ExactDependencyFailures_AbortWholeBundleWithoutFallback(string defect)
    {
        var (snapshot, refs) = SnapshotWithProfiles("PROFILE");
        if (defect == "missingProfile") refs = [Dependency("MISSING", 1)];
        if (defect == "duplicate") refs = [refs[0], refs[0]];
        if (defect == "invalidReference") refs = [new ExactPrescriptionProjectionDependency { Profile = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = "PROFILE", Version = 1 } }];
        if (defect == "missingWorkout")
        {
            var profile = snapshot.PrescriptionProfiles[0] with { WorkoutDefinitionRef = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = "MISSING", Version = 1 } };
            snapshot = snapshot with { PrescriptionProfiles = [profile] };
        }
        Assert.Throws<InvalidOperationException>(() => new CatalogBundleAssembler(serializer, hasher).Assemble(snapshot, "TEN_K__4D__INTERMEDIATE", 1, refs));
    }

    [Fact]
    public void DifferentProfileVersionsRemainDistinctExactEntries()
    {
        var (snapshot, refs) = SnapshotWithProfiles("PROFILE");
        var v2 = snapshot.PrescriptionProfiles[0] with { Metadata = snapshot.PrescriptionProfiles[0].Metadata with { Version = 2, ContentHash = null } };
        snapshot = CatalogStamper.StampAsPublished(serializer, hasher, snapshot with { PrescriptionProfiles = [snapshot.PrescriptionProfiles[0] with { Metadata = snapshot.PrescriptionProfiles[0].Metadata with { ContentHash = null } }, v2] });
        refs = [Dependency("PROFILE", 1), Dependency("PROFILE", 2)];
        var bundle = new CatalogBundleAssembler(serializer, hasher).Assemble(snapshot, "TEN_K__4D__INTERMEDIATE", 1, refs);
        Assert.Equal([1, 2], bundle.ExecutionPrescriptions!.Select(x => x.SourceProfile.Version));
    }

    private (CatalogSourceSnapshot Snapshot, IReadOnlyList<ExactPrescriptionProjectionDependency> References) SnapshotWithProfiles(params string[] keys)
    {
        var fixture = new CombinationFixture();
        var workout = fixture.ThresholdWorkout with
        {
            AllowedDistanceAccountingModes = [DistanceAccountingMode.EstimatedSessionTotal]
        };
        var profiles = keys.Select(key => WorkoutPrescriptionExecutionProjectorTests.Profile(key, workout with { Metadata = workout.Metadata with { ContentHash = "temporary" } }, true) with
        {
            Metadata = Meta.Of(DocumentTypes.WorkoutPrescriptionProfile, key, 1, CatalogStatus.Published),
            WorkoutDefinitionRef = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = workout.Metadata.Key, Version = workout.Metadata.Version }
        }).ToList();
        var source = fixture.BuildSnapshot() with
        {
            Workouts = fixture.BuildSnapshot().Workouts.Select(x => x.Metadata.Key == workout.Metadata.Key ? workout : x).ToList(),
            PrescriptionProfiles = profiles
        };
        var stamped = CatalogStamper.StampAsPublished(serializer, hasher, source);
        return (stamped, keys.Select(key => Dependency(key, 1)).ToList());
    }

    private static ExactPrescriptionProjectionDependency Dependency(string key, int version) => new()
    {
        Profile = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutPrescriptionProfile, Key = key, Version = version }
    };
}
