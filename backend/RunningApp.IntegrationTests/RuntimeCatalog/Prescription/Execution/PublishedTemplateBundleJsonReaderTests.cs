using System.Text.Json;
using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.Prescriptions;
using PlanCatalog.Contracts.References;
using PlanCatalog.Infrastructure.Serialization;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Execution;

/// <summary>
/// Phase 10K-FREQ.6D.3D — proves the real Process A→B JSON crossing: a bundle serialized with the
/// authoritative Process A canonical options (<c>PlanCatalog.Infrastructure.Serialization.CanonicalJsonOptions</c>,
/// referenced here test-only, exactly as the existing Phase 4G.5O publication fixture already does)
/// deserializes correctly through RunningApp's own <see cref="PublishedTemplateBundleJsonReader"/>,
/// which does not reference PlanCatalog.Infrastructure in production.
/// </summary>
public sealed class PublishedTemplateBundleJsonReaderTests
{
    [Fact]
    public void LegacyBundleJson_FromRealCanonicalSerializer_ReadsAsLegacy()
    {
        var bundle = Bundle(null);
        var json = JsonSerializer.Serialize(bundle, CanonicalJsonOptions.Canonical);

        var read = PublishedTemplateBundleJsonReader.Read(json);

        Assert.Null(read.ExecutionPrescriptions);
        Assert.Equal(bundle.BundleKey, read.BundleKey);
        Assert.Equal(bundle.BundleVersion, read.BundleVersion);
    }

    [Fact]
    public void ProfileBackedBundleJson_FromRealCanonicalSerializer_RoundTripsLosslesslyThroughRunningAppReader()
    {
        var prescription = new ExecutableWorkoutPrescription
        {
            ContractSchemaVersion = 1,
            SourceProfile = new CatalogArtifactReference { DocumentType = "WORKOUT_PRESCRIPTION_PROFILE", Key = "PROFILE", Version = 7, ContentHash = "profile-hash" },
            SourceWorkout = new CatalogArtifactReference { DocumentType = "WORKOUT_DEFINITION", Key = "WORKOUT", Version = 4, ContentHash = "workout-hash" },
            DoseCategory = ExecutablePrescriptionDoseCategory.SecondaryControlled,
            DistanceAccountingMode = DistanceAccountingMode.EmbeddedComponents,
            Components =
            [
                new ExecutablePrescriptionComponent
                {
                    SequenceOrder = 1,
                    ComponentType = WorkoutComponentType.MainSet,
                    StructureMode = ExecutablePrescriptionStructureMode.Repeated,
                    Work = new ExecutableWorkQuantity { Unit = ExecutableQuantityUnit.Meters, Value = 1600 },
                    RepetitionCount = 4,
                    Recovery = new ExecutableRecovery
                    {
                        Unit = ExecutableQuantityUnit.Meters, Value = 400, Mode = ExecutableRecoveryMode.Jog,
                        Placement = ExecutableRecoveryPlacement.BetweenRepetitions, RecoveryCount = 3
                    },
                    Intensity = new ExecutableIntensityTarget { Mode = ExecutableIntensityMode.PaceBased, DescriptorKey = "THRESHOLD_PACE" }
                }
            ]
        };
        var bundle = Bundle([prescription]);
        var json = JsonSerializer.Serialize(bundle, CanonicalJsonOptions.Canonical);

        var read = PublishedTemplateBundleJsonReader.Read(json);

        Assert.NotNull(read.ExecutionPrescriptions);
        Assert.Single(read.ExecutionPrescriptions!);
        var component = read.ExecutionPrescriptions![0].Components[0];
        Assert.Equal(4, component.RepetitionCount);
        Assert.Equal(1600, component.Work.Value);
        Assert.Equal(ExecutableQuantityUnit.Meters, component.Work.Unit);
        Assert.Equal(3, component.Recovery!.RecoveryCount);
        Assert.Equal(ExecutableRecoveryPlacement.BetweenRepetitions, component.Recovery.Placement);
        Assert.Equal(ExecutableIntensityMode.PaceBased, component.Intensity.Mode);
        Assert.Equal(ExecutablePrescriptionDoseCategory.SecondaryControlled, read.ExecutionPrescriptions![0].DoseCategory);

        // And it feeds the exact-lookup index correctly, end to end.
        var index = ExecutionPrescriptionIndex.Build(read);
        var resolved = index.ResolveExact(new VersionedCatalogReference { DocumentType = "WORKOUT_PRESCRIPTION_PROFILE", Key = "PROFILE", Version = 7 });
        Assert.Equal(3, resolved.Components[0].Recovery!.RecoveryCount);
    }

    [Fact]
    public void RunningAppWriter_And_RealCanonicalSerializer_ProduceEquivalentDeserializedBundle()
    {
        var bundle = Bundle(null);

        var viaRunningApp = PublishedTemplateBundleJsonReader.Read(PublishedTemplateBundleJsonReader.Write(bundle));
        var viaCanonical = JsonSerializer.Deserialize<PublishedTemplateBundle>(JsonSerializer.Serialize(bundle, CanonicalJsonOptions.Canonical), CanonicalJsonOptions.Canonical);

        // PublishedTemplateBundle's record-generated Equals compares IReadOnlyList<T> properties by
        // reference (the same pitfall documented elsewhere in this engagement), so two independently
        // deserialized instances are never .Equals-equal even with identical content - compare scalar
        // fields plus element-wise list equality instead of whole-record equality.
        Assert.NotNull(viaCanonical);
        Assert.Equal(viaCanonical!.BundleKey, viaRunningApp.BundleKey);
        Assert.Equal(viaCanonical.BundleVersion, viaRunningApp.BundleVersion);
        Assert.Equal(viaCanonical.BundleContentHash, viaRunningApp.BundleContentHash);
        Assert.Equal(viaCanonical.Combination, viaRunningApp.Combination);
        Assert.Null(viaRunningApp.ExecutionPrescriptions);
        Assert.Equal(viaCanonical.Workouts, viaRunningApp.Workouts);
    }

    private static PublishedTemplateBundle Bundle(IReadOnlyList<ExecutableWorkoutPrescription>? prescriptions)
    {
        var reference = new CatalogArtifactReference { DocumentType = "X", Key = "X", Version = 1, ContentHash = "hash" };
        return new PublishedTemplateBundle
        {
            BundleKey = "B", BundleVersion = 1, Combination = reference, MasterTemplate = reference,
            Layout = reference, LevelModifier = reference, WorkoutProgression = reference,
            ProgressionModifier = reference, RulePack = reference, RuntimeConditionValueRegistry = reference,
            PeakVolumeBandPolicy = reference, Workouts = [reference], ExecutionPrescriptions = prescriptions,
            BundleContentHash = ""
        };
    }
}
