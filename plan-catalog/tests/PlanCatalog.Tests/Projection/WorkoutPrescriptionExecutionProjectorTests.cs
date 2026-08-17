using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;
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

public sealed class WorkoutPrescriptionExecutionProjectorTests
{
    private readonly WorkoutPrescriptionExecutionProjector projector = new();

    [Theory]
    [InlineData(true, ExecutableQuantityUnit.Seconds)]
    [InlineData(false, ExecutableQuantityUnit.Meters)]
    public void Continuous_ProjectsTypedQuantityWithoutRepeatedFields(bool duration, ExecutableQuantityUnit expected)
    {
        var workout = Workout("CONTINUOUS");
        var result = projector.Project(Profile("P", workout, repeated: false, duration: duration), workout);
        Assert.Equal(expected, result.Components[0].Work.Unit);
        Assert.Null(result.Components[0].RepetitionCount);
        Assert.Null(result.Components[0].Recovery);
    }

    [Theory]
    [InlineData(PrescriptionRecoveryPlacement.BetweenRepetitions, ExecutableRecoveryPlacement.BetweenRepetitions, 3)]
    [InlineData(PrescriptionRecoveryPlacement.AfterEachRepetition, ExecutableRecoveryPlacement.AfterEachRepetition, 4)]
    public void Repeated_UsesCoreCardinalityAndPreservesPlacement(PrescriptionRecoveryPlacement source, ExecutableRecoveryPlacement expected, int count)
    {
        var workout = Workout("REPEATED");
        var result = projector.Project(Profile("P", workout, repeated: true, placement: source), workout);
        var recovery = Assert.IsType<PlanCatalog.Contracts.Prescriptions.ExecutableRecovery>(result.Components[0].Recovery);
        Assert.Equal(4, result.Components[0].RepetitionCount);
        Assert.Equal(expected, recovery.Placement);
        Assert.Equal(count, recovery.RecoveryCount);
        Assert.Equal(400, recovery.Value);
    }

    [Theory]
    [InlineData(PrescriptionIntensityMode.PaceBased, ExecutableIntensityMode.PaceBased)]
    [InlineData(PrescriptionIntensityMode.EffortBased, ExecutableIntensityMode.EffortBased)]
    [InlineData(PrescriptionIntensityMode.HeartRateBased, ExecutableIntensityMode.HeartRateBased)]
    public void Intensity_MapsExplicitly(PrescriptionIntensityMode source, ExecutableIntensityMode expected)
    {
        var workout = Workout("I");
        var result = projector.Project(Profile("P", workout, false, intensity: source), workout);
        Assert.Equal(expected, result.Components[0].Intensity.Mode);
        Assert.Equal("DESCRIPTOR", result.Components[0].Intensity.DescriptorKey);
    }

    [Fact]
    public void ExactProvenanceDoseAccountingAndComponentOrder_ArePreserved()
    {
        var workout = Workout("W");
        var profile = Profile("P", workout, false) with
        {
            DoseCategory = PrescriptionDoseCategory.SecondaryControlled,
            DistanceAccountingMode = DistanceAccountingMode.EmbeddedComponents,
            Components = [Component(2, false), Component(1, false)]
        };
        var result = projector.Project(profile, workout);
        Assert.Equal("P", result.SourceProfile.Key);
        Assert.Equal("profile-hash", result.SourceProfile.ContentHash);
        Assert.Equal("W", result.SourceWorkout.Key);
        Assert.Equal("workout-hash", result.SourceWorkout.ContentHash);
        Assert.Equal(ExecutablePrescriptionDoseCategory.SecondaryControlled, result.DoseCategory);
        Assert.Equal(DistanceAccountingMode.EmbeddedComponents, result.DistanceAccountingMode);
        Assert.Equal([1, 2], result.Components.Select(x => x.SequenceOrder));
    }

    [Theory]
    [InlineData("mismatch")]
    [InlineData("structure")]
    [InlineData("intensity")]
    [InlineData("placement")]
    [InlineData("authoring")]
    [InlineData("boundary")]
    public void ImpossibleProjectionInputs_FailClosed(string defect)
    {
        var workout = Workout("W");
        var profile = Profile("P", workout, true);
        if (defect == "mismatch") workout = workout with { Metadata = workout.Metadata with { Version = 2 } };
        if (defect == "structure") profile = profile with { Components = [profile.Components[0] with { StructureMode = (PrescriptionStructureMode)999 }] };
        if (defect == "intensity") profile = profile with { Components = [profile.Components[0] with { IntensityTarget = profile.Components[0].IntensityTarget with { Mode = (PrescriptionIntensityMode)999 } }] };
        if (defect == "placement") profile = profile with { Components = [profile.Components[0] with { RecoveryPlacement = (PrescriptionRecoveryPlacement)999 }] };
        if (defect == "authoring") profile = profile with { Components = [profile.Components[0] with { RecoveryQuantity = null }] };
        if (defect == "boundary") profile = profile with { Components = [profile.Components[0] with { ComponentType = (WorkoutComponentType)999 }] };
        Assert.Throws<InvalidOperationException>(() => projector.Project(profile, workout));
    }

    [Fact]
    public void SameExactInput_ProducesCanonicalIdenticalValue()
    {
        var workout = Workout("W");
        var profile = Profile("P", workout, true);
        var first = System.Text.Json.JsonSerializer.Serialize(projector.Project(profile, workout), CanonicalJsonOptions.Canonical);
        var second = System.Text.Json.JsonSerializer.Serialize(projector.Project(profile, workout), CanonicalJsonOptions.Canonical);
        Assert.Equal(first, second);
    }

    [Fact]
    public void FartlekAndThreshold_UseSameGenericLosslessProjection()
    {
        foreach (var key in new[] { "FARTLEK", "THRESHOLD_TEMPO" })
        {
            var workout = Workout(key);
            var result = projector.Project(Profile(key + "_PROFILE", workout, true), workout);
            Assert.Equal(4, result.Components[0].RepetitionCount);
            Assert.Equal(3, result.Components[0].Recovery!.RecoveryCount);
            Assert.Equal(1000, result.Components[0].Work.Value);
        }
    }

    [Fact]
    public void TaperDoseChange_UsesSameGenericCardinalityAuthority()
    {
        var workout = Workout("W");
        var normal = projector.Project(Profile("NORMAL", workout, true, repetitions: 6), workout);
        var taper = projector.Project(Profile("TAPER", workout, true, repetitions: 4), workout);
        Assert.Equal(5, normal.Components[0].Recovery!.RecoveryCount);
        Assert.Equal(3, taper.Components[0].Recovery!.RecoveryCount);
    }

    private static WorkoutDefinition Workout(string key) => new()
    {
        Metadata = Meta.Of(DocumentTypes.WorkoutDefinition, key, 1, CatalogStatus.Published) with { ContentHash = "workout-hash" },
        Family = WorkoutFamily.Quality,
        EligiblePhases = [PhaseKey.Build],
        AllowedPrescriptionModes = [PrescriptionMode.PaceBased, PrescriptionMode.EffortBased, PrescriptionMode.HeartRateBased],
        AllowedDistanceAccountingModes = [DistanceAccountingMode.EstimatedSessionTotal, DistanceAccountingMode.EmbeddedComponents],
        Components = [new WorkoutComponentDefinition { SequenceOrder = 1, ComponentType = WorkoutComponentType.MainSet, IntensityDescriptor = "DESCRIPTOR" }]
    };

    internal static WorkoutPrescriptionProfile Profile(string key, WorkoutDefinition workout, bool repeated, bool duration = false,
        PrescriptionRecoveryPlacement placement = PrescriptionRecoveryPlacement.BetweenRepetitions, int repetitions = 4,
        PrescriptionIntensityMode intensity = PrescriptionIntensityMode.PaceBased) => new()
    {
        Metadata = Meta.Of(DocumentTypes.WorkoutPrescriptionProfile, key, 1, CatalogStatus.Published) with { ContentHash = "profile-hash" },
        WorkoutDefinitionRef = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = workout.Metadata.Key, Version = workout.Metadata.Version },
        DoseCategory = PrescriptionDoseCategory.Primary,
        DistanceAccountingMode = DistanceAccountingMode.EstimatedSessionTotal,
        Components = [Component(1, repeated, duration, placement, repetitions, intensity)]
    };

    private static PrescriptionProfileComponent Component(int order, bool repeated, bool duration = false,
        PrescriptionRecoveryPlacement placement = PrescriptionRecoveryPlacement.BetweenRepetitions, int repetitions = 4,
        PrescriptionIntensityMode intensity = PrescriptionIntensityMode.PaceBased) => new()
    {
        SequenceOrder = order,
        ComponentType = WorkoutComponentType.MainSet,
        StructureMode = repeated ? PrescriptionStructureMode.Repeated : PrescriptionStructureMode.Continuous,
        WorkQuantity = new PrescriptionWorkQuantity
        {
            DurationSeconds = duration ? 60 : null,
            DistanceMeters = duration ? null : 1000,
            RepetitionCount = repeated ? repetitions : null
        },
        RecoveryQuantity = repeated ? new PrescriptionRecoveryQuantity { DistanceMeters = 400, Mode = PrescriptionRecoveryMode.Jog } : null,
        RecoveryPlacement = repeated ? placement : null,
        IntensityTarget = new PrescriptionIntensityTarget
        {
            Mode = intensity,
            PaceDescriptorKey = intensity == PrescriptionIntensityMode.PaceBased ? "DESCRIPTOR" : null,
            EffortDescriptorKey = intensity == PrescriptionIntensityMode.EffortBased ? "DESCRIPTOR" : null,
            HeartRateZoneKey = intensity == PrescriptionIntensityMode.HeartRateBased ? "DESCRIPTOR" : null
        }
    };
}
