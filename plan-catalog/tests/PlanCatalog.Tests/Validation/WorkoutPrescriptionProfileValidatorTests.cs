using System.Text.Json;
using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Metadata;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

public sealed class WorkoutPrescriptionProfileValidatorTests
{
    [Fact]
    public void ContinuousThreshold_IsRepresentable() => AssertValid(Profile());

    [Fact]
    public void IntervalizedThreshold_IsRepresentable()
    {
        var profile = Profile() with { Components = ReplaceMainWithRepeated(Profile().Components, 4, 1600, 400) };
        AssertValid(profile);
    }

    [Fact]
    public void StructuredControlledFartlek_IsRepresentable()
    {
        var workout = Workout("FARTLEK", includeStandaloneRecovery: true);
        var profile = Profile("FARTLEK", PrescriptionDoseCategory.SecondaryControlled, includeStandaloneRecovery: true) with
        {
            Components = ReplaceMainWithRepeated(Profile("FARTLEK", PrescriptionDoseCategory.SecondaryControlled, true).Components, 6, durationSeconds: 60, recoverySeconds: 60)
        };
        AssertValid(profile, workout);
    }

    [Fact]
    public void ReducedDoseTaperProfile_IsDistinctExecutableVersion()
    {
        var profile = Profile() with
        {
            Metadata = Metadata(DocumentTypes.WorkoutPrescriptionProfile, "THRESHOLD_TAPER_REDUCED", 2),
            Components = Profile().Components.Select(x => x.ComponentType == WorkoutComponentType.MainSet
                ? x with { WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 300 } }
                : x).ToList()
        };
        AssertValid(profile);
        Assert.Equal(300, profile.Components.Single(x => x.ComponentType == WorkoutComponentType.MainSet).WorkQuantity!.DurationSeconds);
    }

    [Fact]
    public void RepeatedWithoutRepetition_FailsExactCode()
    {
        var main = Profile().Components[1] with
        {
            StructureMode = PrescriptionStructureMode.Repeated,
            RecoveryQuantity = new PrescriptionRecoveryQuantity { DurationSeconds = 60, Mode = PrescriptionRecoveryMode.Jog }
        };
        AssertCode(Profile() with { Components = Replace(Profile().Components, 1, main) }, "PROFILE_REPEATED_COUNT_INVALID");
    }

    [Fact]
    public void ContinuousWithRepetition_FailsExactCode()
    {
        var main = Profile().Components[1] with { WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 600, RepetitionCount = 3 } };
        AssertCode(Profile() with { Components = Replace(Profile().Components, 1, main) }, "PROFILE_CONTINUOUS_REPETITIONS_FORBIDDEN");
    }

    [Fact]
    public void ContradictoryWorkUnits_FailExactCode()
    {
        var main = Profile().Components[1] with { WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 600, DistanceMeters = 1600 } };
        AssertCode(Profile() with { Components = Replace(Profile().Components, 1, main) }, "PROFILE_WORK_UNIT_AMBIGUOUS");
    }

    [Fact]
    public void NonPositiveWork_FailsExactCode()
    {
        var main = Profile().Components[1] with { WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 0 } };
        AssertCode(Profile() with { Components = Replace(Profile().Components, 1, main) }, "PROFILE_WORK_QUANTITY_NON_POSITIVE");
    }

    [Fact]
    public void InvalidRecoveryDefinition_FailsExactCode()
    {
        var repeated = ReplaceMainWithRepeated(Profile().Components, 3, 1000, 200);
        var main = repeated[1] with { RecoveryQuantity = new PrescriptionRecoveryQuantity { DurationSeconds = 60, DistanceMeters = 200, Mode = PrescriptionRecoveryMode.Jog } };
        AssertCode(Profile() with { Components = Replace(repeated, 1, main) }, "PROFILE_RECOVERY_UNIT_AMBIGUOUS");
    }

    [Fact]
    public void ExactWorkoutVersionMustExist()
    {
        var profile = Profile() with { WorkoutDefinitionRef = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = "THRESHOLD_TEMPO", Version = 99 } };
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, workout: null);
        Assert.Contains(result.Issues, x => x.Code == "PROFILE_WORKOUT_REFERENCE_NOT_FOUND");
    }

    [Fact]
    public void ComponentSkeletonMustMatchWorkoutIdentity()
    {
        var profile = Profile() with { Components = Profile().Components.Take(2).ToList() };
        AssertCode(profile, "PROFILE_COMPONENT_SKELETON_MISMATCH");
    }

    [Fact]
    public void LaneZeroPrimary_Passes() => Assert.True(PrescriptionProfileLaneDoseValidator.Validate(0, Profile()).IsValid);

    [Fact]
    public void LaneOneSecondaryControlled_Passes() => Assert.True(PrescriptionProfileLaneDoseValidator.Validate(1, Profile(dose: PrescriptionDoseCategory.SecondaryControlled)).IsValid);

    [Fact]
    public void LaneZeroSecondaryControlled_FailsExactCode() =>
        Assert.Contains(PrescriptionProfileLaneDoseValidator.Validate(0, Profile(dose: PrescriptionDoseCategory.SecondaryControlled)).Issues, x => x.Code == "PROFILE_LANE_DOSE_CATEGORY_MISMATCH");

    [Fact]
    public void LaneOnePrimary_FailsExactCode() =>
        Assert.Contains(PrescriptionProfileLaneDoseValidator.Validate(1, Profile()).Issues, x => x.Code == "PROFILE_LANE_DOSE_CATEGORY_MISMATCH");

    [Fact]
    public void UnsupportedLane_FailsExactCode() =>
        Assert.Contains(PrescriptionProfileLaneDoseValidator.Validate(2, Profile()).Issues, x => x.Code == "PROFILE_LANE_ORDINAL_UNSUPPORTED");

    [Fact]
    public void ExactProfileReference_ResolvesWithoutVersionFallback()
    {
        var profile = Profile();
        var snapshot = new CatalogSnapshotBuilder().With(profile).Build();
        var exact = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutPrescriptionProfile, Key = profile.Metadata.Key, Version = profile.Metadata.Version };
        var missing = exact with { Version = 2 };
        Assert.True(PrescriptionProfileLaneDoseValidator.Validate(0, exact, snapshot).IsValid);
        Assert.Contains(PrescriptionProfileLaneDoseValidator.Validate(0, missing, snapshot).Issues, x => x.Code == "PROFILE_REFERENCE_NOT_FOUND");
    }

    [Fact]
    public void DuplicateProfileIdentityVersion_FailsGraphValidation()
    {
        var workout = Workout();
        var profile = Profile();
        var snapshot = new CatalogSnapshotBuilder().With(workout).With(profile).With(profile).Build();
        var result = CatalogGraphValidator.Validate(snapshot);
        Assert.Contains(result.Issues, x => x.Code == "GRAPH_DUPLICATE_KEY_VERSION" && x.Message.Contains(DocumentTypes.WorkoutPrescriptionProfile));
    }

    [Theory]
    [InlineData("UNKNOWN", "PRIMARY")]
    [InlineData("CONTINUOUS", "UNKNOWN")]
    public void UnknownEnums_FailTypedDeserialization(string structureMode, string doseCategory)
    {
        var json = ValidJson().Replace("CONTINUOUS", structureMode).Replace("PRIMARY", doseCategory);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkoutPrescriptionProfile>(json, CanonicalJsonOptions.Pretty));
    }

    private static void AssertValid(WorkoutPrescriptionProfile profile, WorkoutDefinition? workout = null)
    {
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, workout ?? Workout(profile.WorkoutDefinitionRef.Key, profile.Components.Any(x => x.ComponentType == WorkoutComponentType.Recovery)));
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(x => $"{x.Code}: {x.Message}")));
    }

    private static void AssertCode(WorkoutPrescriptionProfile profile, string code)
    {
        var workout = Workout(profile.WorkoutDefinitionRef.Key, profile.Components.Any(x => x.ComponentType == WorkoutComponentType.Recovery));
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, workout);
        Assert.Contains(result.Issues, x => x.Code == code);
    }

    private static WorkoutDefinition Workout(string key = "THRESHOLD_TEMPO", bool includeStandaloneRecovery = false) => new()
    {
        Metadata = Metadata(DocumentTypes.WorkoutDefinition, key, 4),
        Family = WorkoutFamily.Quality,
        EligiblePhases = [PhaseKey.Build, PhaseKey.RaceSpecific, PhaseKey.Taper],
        AllowedPrescriptionModes = [PrescriptionMode.EffortBased, PrescriptionMode.PaceBased],
        AllowedDistanceAccountingModes = [DistanceAccountingMode.EstimatedSessionTotal],
        Components = ComponentSkeleton(includeStandaloneRecovery)
    };

    private static WorkoutPrescriptionProfile Profile(string workoutKey = "THRESHOLD_TEMPO", PrescriptionDoseCategory dose = PrescriptionDoseCategory.Primary, bool includeStandaloneRecovery = false) => new()
    {
        Metadata = Metadata(DocumentTypes.WorkoutPrescriptionProfile, workoutKey + "_PRIMARY", 1),
        WorkoutDefinitionRef = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = workoutKey, Version = 4 },
        DoseCategory = dose,
        DistanceAccountingMode = DistanceAccountingMode.EstimatedSessionTotal,
        Components = ComponentSkeleton(includeStandaloneRecovery).Select(x => new PrescriptionProfileComponent
        {
            SequenceOrder = x.SequenceOrder,
            ComponentType = x.ComponentType,
            StructureMode = PrescriptionStructureMode.Continuous,
            WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = x.ComponentType == WorkoutComponentType.MainSet ? 1200 : 600 },
            IntensityTarget = new PrescriptionIntensityTarget { Mode = PrescriptionIntensityMode.EffortBased, EffortDescriptorKey = x.IntensityDescriptor }
        }).ToList()
    };

    private static IReadOnlyList<WorkoutComponentDefinition> ComponentSkeleton(bool recovery) => recovery
        ? [Component(1, WorkoutComponentType.WarmUp), Component(2, WorkoutComponentType.MainSet), Component(3, WorkoutComponentType.Recovery), Component(4, WorkoutComponentType.CoolDown)]
        : [Component(1, WorkoutComponentType.WarmUp), Component(2, WorkoutComponentType.MainSet), Component(3, WorkoutComponentType.CoolDown)];

    private static WorkoutComponentDefinition Component(int order, WorkoutComponentType type) => new() { SequenceOrder = order, ComponentType = type, IntensityDescriptor = type.ToString().ToUpperInvariant() };

    private static IReadOnlyList<PrescriptionProfileComponent> ReplaceMainWithRepeated(IReadOnlyList<PrescriptionProfileComponent> source, int reps, int? distanceMeters = null, int? recoveryMeters = null, int? durationSeconds = null, int? recoverySeconds = null)
    {
        var mainIndex = source.ToList().FindIndex(x => x.ComponentType == WorkoutComponentType.MainSet);
        var main = source[mainIndex] with
        {
            StructureMode = PrescriptionStructureMode.Repeated,
            WorkQuantity = new PrescriptionWorkQuantity { RepetitionCount = reps, DistanceMeters = distanceMeters, DurationSeconds = durationSeconds },
            RecoveryQuantity = new PrescriptionRecoveryQuantity { DistanceMeters = recoveryMeters, DurationSeconds = recoverySeconds, Mode = PrescriptionRecoveryMode.Jog }
        };
        return Replace(source, mainIndex, main);
    }

    private static IReadOnlyList<PrescriptionProfileComponent> Replace(IReadOnlyList<PrescriptionProfileComponent> source, int index, PrescriptionProfileComponent value)
    {
        var list = source.ToList();
        list[index] = value;
        return list;
    }

    private static CatalogDocumentMetadata Metadata(string type, string key, int version) => new() { DocumentType = type, SchemaVersion = 1, Key = key, Version = version, Status = CatalogStatus.Draft };

    private static string ValidJson() => """
    {"metadata":{"documentType":"WORKOUT_PRESCRIPTION_PROFILE","schemaVersion":1,"key":"P","version":1,"status":"DRAFT"},"workoutDefinitionRef":{"documentType":"WORKOUT_DEFINITION","key":"W","version":1},"doseCategory":"PRIMARY","distanceAccountingMode":"ESTIMATED_SESSION_TOTAL","components":[{"sequenceOrder":1,"componentType":"MAIN_SET","structureMode":"CONTINUOUS","workQuantity":{"durationSeconds":600},"intensityTarget":{"mode":"EFFORT_BASED","effortDescriptorKey":"CONTROLLED"}}]}
    """;
}
