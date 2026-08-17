using System.Text.Json;
using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.Prescriptions;
using PlanCatalog.Contracts.References;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Infrastructure.Schema;
using Xunit;

namespace PlanCatalog.Tests.Serialization;

public sealed class ExecutableWorkoutPrescriptionContractTests
{
    [Theory]
    [InlineData(ExecutableQuantityUnit.Seconds, ExecutableIntensityMode.EffortBased)]
    [InlineData(ExecutableQuantityUnit.Meters, ExecutableIntensityMode.PaceBased)]
    [InlineData(ExecutableQuantityUnit.Seconds, ExecutableIntensityMode.HeartRateBased)]
    public void Continuous_RoundTripsLosslessly(ExecutableQuantityUnit unit, ExecutableIntensityMode intensityMode)
    {
        var value = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, unit, intensityMode: intensityMode));
        var roundTrip = RoundTrip(value);
        Assert.Empty(ExecutableWorkoutPrescriptionValidator.Validate(roundTrip));
        Assert.Equal(unit, roundTrip.Components[0].Work.Unit);
        Assert.Equal(intensityMode, roundTrip.Components[0].Intensity.Mode);
        Assert.Null(roundTrip.Components[0].RepetitionCount);
        Assert.Null(roundTrip.Components[0].Recovery);
    }

    [Theory]
    [InlineData(ExecutableQuantityUnit.Seconds, ExecutableRecoveryPlacement.BetweenRepetitions, 3)]
    [InlineData(ExecutableQuantityUnit.Meters, ExecutableRecoveryPlacement.AfterEachRepetition, 4)]
    public void Repeated_RoundTripsExactRecovery(ExecutableQuantityUnit unit, ExecutableRecoveryPlacement placement, int recoveryCount)
    {
        var value = Prescription(Component(ExecutablePrescriptionStructureMode.Repeated, unit, placement, recoveryCount));
        var roundTrip = RoundTrip(value);
        var component = roundTrip.Components[0];
        Assert.Empty(ExecutableWorkoutPrescriptionValidator.Validate(roundTrip));
        Assert.Equal(4, component.RepetitionCount);
        Assert.Equal(unit, component.Work.Unit);
        Assert.Equal(unit, component.Recovery!.Unit);
        Assert.Equal(ExecutableRecoveryMode.Jog, component.Recovery.Mode);
        Assert.Equal(placement, component.Recovery.Placement);
        Assert.Equal(recoveryCount, component.Recovery.RecoveryCount);
    }

    [Theory]
    [InlineData(ExecutableRecoveryPlacement.BetweenRepetitions, 4)]
    [InlineData(ExecutableRecoveryPlacement.AfterEachRepetition, 3)]
    public void InconsistentResolvedRecoveryCount_FailsClosed(ExecutableRecoveryPlacement placement, int badCount)
    {
        var value = Prescription(Component(ExecutablePrescriptionStructureMode.Repeated, ExecutableQuantityUnit.Meters, placement, badCount));
        Assert.Contains("EXECUTION_RECOVERY_COUNT_INCONSISTENT", ExecutableWorkoutPrescriptionValidator.Validate(value));
    }

    [Fact]
    public void ProvenanceDoseAccountingAndContractVersion_RoundTripExactly()
    {
        var value = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters)) with
        {
            ContractSchemaVersion = 1,
            DoseCategory = ExecutablePrescriptionDoseCategory.SecondaryControlled,
            DistanceAccountingMode = DistanceAccountingMode.EmbeddedComponents
        };
        var roundTrip = RoundTrip(value);
        Assert.Equal(value.SourceProfile, roundTrip.SourceProfile);
        Assert.Equal(value.SourceWorkout, roundTrip.SourceWorkout);
        Assert.Equal(1, roundTrip.ContractSchemaVersion);
        Assert.Equal(ExecutablePrescriptionDoseCategory.SecondaryControlled, roundTrip.DoseCategory);
        Assert.Equal(DistanceAccountingMode.EmbeddedComponents, roundTrip.DistanceAccountingMode);
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("work")]
    [InlineData("repetitions")]
    [InlineData("recovery")]
    [InlineData("continuousRecovery")]
    [InlineData("intensity")]
    [InlineData("provenance")]
    public void ImpossibleExecutionValues_FailFocusedBoundaryValidation(string defect)
    {
        var component = Component(ExecutablePrescriptionStructureMode.Repeated, ExecutableQuantityUnit.Seconds, ExecutableRecoveryPlacement.BetweenRepetitions, 3);
        var value = Prescription(component);
        value = defect switch
        {
            "schema" => value with { ContractSchemaVersion = 2 },
            "work" => value with { Components = [component with { Work = component.Work with { Value = 0 } }] },
            "repetitions" => value with { Components = [component with { RepetitionCount = 1 }] },
            "recovery" => value with { Components = [component with { Recovery = null }] },
            "continuousRecovery" => value with { Components = [component with { StructureMode = ExecutablePrescriptionStructureMode.Continuous, RepetitionCount = null }] },
            "intensity" => value with { Components = [component with { Intensity = component.Intensity with { Mode = (ExecutableIntensityMode)999 } }] },
            "provenance" => value with { SourceProfile = value.SourceProfile with { ContentHash = "" } },
            _ => value
        };
        Assert.NotEmpty(ExecutableWorkoutPrescriptionValidator.Validate(value));
    }

    [Fact]
    public void LegacyBundleWithoutExecutionProjection_RoundTripsAsLegacy()
    {
        var bundle = Bundle(null);
        var json = JsonSerializer.Serialize(bundle, CanonicalJsonOptions.Canonical);
        Assert.DoesNotContain("executionPrescriptions", json);
        Assert.Null(JsonSerializer.Deserialize<PublishedTemplateBundle>(json, CanonicalJsonOptions.Canonical)!.ExecutionPrescriptions);
    }

    [Fact]
    public void BundleWithExecutionProjection_RoundTripsAndIsHashCovered()
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Repeated, ExecutableQuantityUnit.Meters, ExecutableRecoveryPlacement.BetweenRepetitions, 3));
        var bundle = Bundle([prescription]);
        var roundTrip = JsonSerializer.Deserialize<PublishedTemplateBundle>(JsonSerializer.Serialize(bundle, CanonicalJsonOptions.Canonical), CanonicalJsonOptions.Canonical)!;
        Assert.Single(roundTrip.ExecutionPrescriptions!);

        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var hash = CatalogDocumentHasher.ComputeHashExcludingField(serializer, hasher, bundle, "bundleContentHash");
        var changed = bundle with { ExecutionPrescriptions = [prescription with { DoseCategory = ExecutablePrescriptionDoseCategory.SecondaryControlled }] };
        var changedHash = CatalogDocumentHasher.ComputeHashExcludingField(serializer, hasher, changed, "bundleContentHash");
        Assert.NotEqual(hash, changedHash);
    }

    [Fact]
    public void BundleExecutionProjection_PassesPublishedBundleSchema()
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Repeated, ExecutableQuantityUnit.Seconds, ExecutableRecoveryPlacement.AfterEachRepetition, 4));
        var json = JsonSerializer.Serialize(Bundle([prescription]), CanonicalJsonOptions.Canonical);
        var validator = new JsonSchemaNetValidator(Path.Combine(AppContext.BaseDirectory, "TestSchemas"));
        var result = validator.Validate(PlanCatalog.Contracts.DocumentTypes.PublishedTemplateBundle, json);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(x => x.Message)));
    }

    private static ExecutableWorkoutPrescription RoundTrip(ExecutableWorkoutPrescription value) =>
        JsonSerializer.Deserialize<ExecutableWorkoutPrescription>(JsonSerializer.Serialize(value, CanonicalJsonOptions.Canonical), CanonicalJsonOptions.Canonical)!;

    private static ExecutableWorkoutPrescription Prescription(ExecutablePrescriptionComponent component) => new()
    {
        ContractSchemaVersion = 1,
        SourceProfile = Ref("WORKOUT_PRESCRIPTION_PROFILE", "PROFILE", 7, "profile-hash"),
        SourceWorkout = Ref("WORKOUT_DEFINITION", "WORKOUT", 4, "workout-hash"),
        DoseCategory = ExecutablePrescriptionDoseCategory.Primary,
        DistanceAccountingMode = DistanceAccountingMode.EstimatedSessionTotal,
        Components = [component]
    };

    private static ExecutablePrescriptionComponent Component(
        ExecutablePrescriptionStructureMode structure,
        ExecutableQuantityUnit unit,
        ExecutableRecoveryPlacement placement = ExecutableRecoveryPlacement.BetweenRepetitions,
        int recoveryCount = 3,
        ExecutableIntensityMode intensityMode = ExecutableIntensityMode.EffortBased) => new()
    {
        SequenceOrder = 1,
        ComponentType = WorkoutComponentType.MainSet,
        StructureMode = structure,
        Work = new ExecutableWorkQuantity { Unit = unit, Value = unit == ExecutableQuantityUnit.Meters ? 1000 : 60 },
        RepetitionCount = structure == ExecutablePrescriptionStructureMode.Repeated ? 4 : null,
        Recovery = structure == ExecutablePrescriptionStructureMode.Repeated
            ? new ExecutableRecovery { Unit = unit, Value = unit == ExecutableQuantityUnit.Meters ? 400 : 60, Mode = ExecutableRecoveryMode.Jog, Placement = placement, RecoveryCount = recoveryCount }
            : null,
        Intensity = new ExecutableIntensityTarget { Mode = intensityMode, DescriptorKey = "CONTROLLED" }
    };

    private static PublishedTemplateBundle Bundle(IReadOnlyList<ExecutableWorkoutPrescription>? prescriptions)
    {
        var reference = Ref("X", "X", 1, "hash");
        return new PublishedTemplateBundle
        {
            BundleKey = "B", BundleVersion = 1, Combination = reference, MasterTemplate = reference,
            Layout = reference, LevelModifier = reference, WorkoutProgression = reference,
            ProgressionModifier = reference, RulePack = reference, RuntimeConditionValueRegistry = reference,
            PeakVolumeBandPolicy = reference, Workouts = [reference], ExecutionPrescriptions = prescriptions,
            BundleContentHash = ""
        };
    }

    private static CatalogArtifactReference Ref(string type, string key, int version, string hash) =>
        new() { DocumentType = type, Key = key, Version = version, ContentHash = hash };
}
