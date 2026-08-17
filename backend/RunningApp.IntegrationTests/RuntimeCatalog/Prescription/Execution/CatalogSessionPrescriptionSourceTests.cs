using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.Prescriptions;
using PlanCatalog.Contracts.References;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Execution;

/// <summary>
/// Phase 10K-FREQ.6D.3D — proves the <see cref="CatalogSessionPrescriptionSource"/> discriminated
/// boundary wraps each source losslessly instead of duplicating it into a second, independently
/// mutable representation (RUNNINGAPP_CONSUMER_DOES_NOT_DUPLICATE_EXECUTION_AUTHORITY). This type is
/// not wired into <see cref="CatalogSessionPrescriptionPlanner"/> in this phase - see the phase report.
/// </summary>
public sealed class CatalogSessionPrescriptionSourceTests
{
    [Fact]
    public void Legacy_WrapsTheSameCatalogWorkoutPrescriptionInstance_WithoutCopyingFields()
    {
        var legacyPrescription = MinimalLegacyPrescription();

        CatalogSessionPrescriptionSource source = new CatalogSessionPrescriptionSource.Legacy(legacyPrescription);

        var legacy = Assert.IsType<CatalogSessionPrescriptionSource.Legacy>(source);
        Assert.Same(legacyPrescription, legacy.Prescription);
    }

    [Fact]
    public void ProfileBacked_WrapsTheSameExecutableWorkoutPrescriptionInstance_WithoutCopyingFields()
    {
        var execution = MinimalExecution();

        CatalogSessionPrescriptionSource source = new CatalogSessionPrescriptionSource.ProfileBacked(execution);

        var profileBacked = Assert.IsType<CatalogSessionPrescriptionSource.ProfileBacked>(source);
        Assert.Same(execution, profileBacked.Prescription);
        // No backend-local mirror exists to drift out of sync with: the wrapped value's own fields
        // (repetition count, recovery, intensity) are read directly off the Contracts object.
        Assert.Equal(4, profileBacked.Prescription.Components[0].RepetitionCount);
        Assert.Equal(3, profileBacked.Prescription.Components[0].Recovery!.RecoveryCount);
    }

    [Fact]
    public void ExhaustiveMatch_DistinguishesLegacyFromProfileBacked()
    {
        CatalogSessionPrescriptionSource legacySource = new CatalogSessionPrescriptionSource.Legacy(MinimalLegacyPrescription());
        CatalogSessionPrescriptionSource profileBackedSource = new CatalogSessionPrescriptionSource.ProfileBacked(MinimalExecution());

        static string Describe(CatalogSessionPrescriptionSource source) => source switch
        {
            CatalogSessionPrescriptionSource.Legacy => "legacy",
            CatalogSessionPrescriptionSource.ProfileBacked => "profile-backed",
            _ => throw new InvalidOperationException("Unreachable: exactly two subclasses exist."),
        };

        Assert.Equal("legacy", Describe(legacySource));
        Assert.Equal("profile-backed", Describe(profileBackedSource));
    }

    private static CatalogWorkoutPrescription MinimalLegacyPrescription() => new()
    {
        PrescriptionMode = CatalogPrescriptionMode.Distance,
        DistanceAccountingMode = CatalogDistanceAccountingMode.EstimatedSessionTotal,
        DistancePrescription = new CatalogDistancePrescription(8.0, "ESTIMATED", "NEAREST_HALF_KM"),
        DurationPrescription = new CatalogDurationPrescription(CatalogDurationKind.EstimatedFromPace, null, "PACE_DERIVED"),
        PacePrescription = new CatalogPacePrescription(
            CatalogPacePrescriptionKind.EffortOnly, null, null, null,
            CatalogPaceSourceSelection.EffortOnly, "LOW", "EASY", "TEST_FIXTURE"),
        EffortGuidance = "EASY",
        OrderedSegments = [],
        Status = CatalogSessionPrescriptionStatus.Complete,
    };

    private static ExecutableWorkoutPrescription MinimalExecution() => new()
    {
        ContractSchemaVersion = 1,
        SourceProfile = new CatalogArtifactReference { DocumentType = "WORKOUT_PRESCRIPTION_PROFILE", Key = "PROFILE", Version = 7, ContentHash = "profile-hash" },
        SourceWorkout = new CatalogArtifactReference { DocumentType = "WORKOUT_DEFINITION", Key = "WORKOUT", Version = 4, ContentHash = "workout-hash" },
        DoseCategory = ExecutablePrescriptionDoseCategory.Primary,
        DistanceAccountingMode = DistanceAccountingMode.EstimatedSessionTotal,
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
}
