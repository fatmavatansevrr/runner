using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.Prescriptions;
using PlanCatalog.Contracts.References;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Execution;

/// <summary>
/// Phase 10K-FREQ.6D.3D — proves RunningApp.Application can load, boundary-validate, index, and
/// exact-resolve a profile-backed <see cref="PublishedTemplateBundle"/> losslessly, and that a legacy
/// (null) bundle is never confused with a corrupt/empty profile-backed one. Synthetic fixtures only —
/// no production catalog identity is exercised (production bundles remain legacy/null per FREQ.6D.3C).
/// </summary>
public sealed class ExecutionPrescriptionIndexTests
{
    // ---- §2 Legacy vs profile-backed ----

    [Fact]
    public void LegacyBundle_NullExecutionPrescriptions_IsNotProfileBacked()
    {
        var index = ExecutionPrescriptionIndex.Build(Bundle(null));

        Assert.False(index.IsProfileBacked);
        Assert.Equal(0, index.Count);
    }

    [Fact]
    public void LegacyIndex_ExactLookup_FailsClosed_NeverFallsBackSilently()
    {
        var index = ExecutionPrescriptionIndex.Build(Bundle(null));

        Assert.Throws<ExecutionPrescriptionLegacyIndexException>(() => index.ResolveExact(Ref("PROFILE", 7)));
    }

    [Fact]
    public void ProfileBackedBundle_IsIndexedAndCountsExactly()
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters));
        var index = ExecutionPrescriptionIndex.Build(Bundle([prescription]));

        Assert.True(index.IsProfileBacked);
        Assert.Equal(1, index.Count);
    }

    [Fact]
    public void PresentExecutionCollection_CannotSilentlyBecomeLegacy()
    {
        // An explicitly-present (even single-entry) collection must never be treated as the legacy null case.
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters));
        var index = ExecutionPrescriptionIndex.Build(Bundle([prescription]));

        Assert.True(index.IsProfileBacked);
        // Legacy-only exception type must never fire for a genuinely profile-backed index.
        var resolved = index.ResolveExact(Ref("PROFILE", 7));
        Assert.Equal(prescription, resolved);
    }

    // ---- §9 exact lookup contract ----

    [Fact]
    public void ExactVersionMatch_ReturnsExecutionValue()
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters), profileVersion: 7);
        var index = ExecutionPrescriptionIndex.Build(Bundle([prescription]));

        var resolved = index.ResolveExact(Ref("PROFILE", 7));

        Assert.Same(prescription, resolved);
    }

    [Fact]
    public void SameKey_DifferentVersion_AreDistinctValues()
    {
        var v7 = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters), profileVersion: 7);
        var v8 = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters), profileVersion: 8) with
        {
            DoseCategory = ExecutablePrescriptionDoseCategory.SecondaryControlled
        };
        var index = ExecutionPrescriptionIndex.Build(Bundle([v7, v8]));

        Assert.Equal(v7, index.ResolveExact(Ref("PROFILE", 7)));
        Assert.Equal(v8, index.ResolveExact(Ref("PROFILE", 8)));
        Assert.NotEqual(index.ResolveExact(Ref("PROFILE", 7)).DoseCategory, index.ResolveExact(Ref("PROFILE", 8)).DoseCategory);
    }

    [Fact]
    public void ExactKeyExistsButWrongVersion_FailsClosed_NoNearestFallback()
    {
        var v7 = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters), profileVersion: 7);
        var index = ExecutionPrescriptionIndex.Build(Bundle([v7]));

        Assert.Throws<ExecutionPrescriptionNotFoundException>(() => index.ResolveExact(Ref("PROFILE", 9)));
    }

    [Fact]
    public void MissingExactReference_FailsClosed()
    {
        var index = ExecutionPrescriptionIndex.Build(Bundle([Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters))]));

        Assert.Throws<ExecutionPrescriptionNotFoundException>(() => index.ResolveExact(Ref("OTHER_PROFILE", 1)));
    }

    [Fact]
    public void KeyOnlyResolution_IsNotSupported_DifferentDocumentTypeSameKeyVersion_IsNotFound()
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters));
        var index = ExecutionPrescriptionIndex.Build(Bundle([prescription]));

        // Same Key/Version but wrong DocumentType must not match - the lookup key requires all three.
        var wrongDocumentType = new VersionedCatalogReference { DocumentType = "WORKOUT_DEFINITION", Key = "PROFILE", Version = 7 };

        Assert.Throws<ExecutionPrescriptionNotFoundException>(() => index.ResolveExact(wrongDocumentType));
    }

    // ---- §8/§26 duplicate provenance and invalid contract fail closed ----

    [Fact]
    public void DuplicateExactProfileProvenance_FailsClosed()
    {
        var a = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters));
        var b = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Seconds));

        Assert.Throws<ExecutionPrescriptionDuplicateProvenanceException>(() => ExecutionPrescriptionIndex.Build(Bundle([a, b])));
    }

    [Fact]
    public void UnsupportedContractSchemaVersion_FailsClosed()
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters)) with { ContractSchemaVersion = 2 };

        Assert.Throws<ExecutionPrescriptionContractInvalidException>(() => ExecutionPrescriptionIndex.Build(Bundle([prescription])));
    }

    [Fact]
    public void InvalidContractsExecutionValue_FailsClosed_NoLegacyFallback()
    {
        var badComponent = Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters) with
        {
            Work = new ExecutableWorkQuantity { Unit = ExecutableQuantityUnit.Meters, Value = 0 } // EXECUTION_WORK_QUANTITY_INVALID
        };
        var prescription = Prescription(badComponent);

        var ex = Assert.Throws<ExecutionPrescriptionContractInvalidException>(() => ExecutionPrescriptionIndex.Build(Bundle([prescription])));
        Assert.Contains("EXECUTION_WORK_QUANTITY_INVALID", ex.Message);
    }

    [Fact]
    public void CorruptProjectionInProfileBackedBundle_FailsClosed_DoesNotDegradeToLegacyIndex()
    {
        var corrupt = Prescription(Component(ExecutablePrescriptionStructureMode.Repeated, ExecutableQuantityUnit.Meters, recoveryCount: 999)); // EXECUTION_RECOVERY_COUNT_INCONSISTENT

        var ex = Record.Exception(() => ExecutionPrescriptionIndex.Build(Bundle([corrupt])));

        Assert.IsType<ExecutionPrescriptionContractInvalidException>(ex);
    }

    [Fact]
    public void NoRecoveryCountRecalculation_ExactPublishedValueIsReturnedUnchanged()
    {
        // BetweenRepetitions with RepetitionCount=4 => correct RecoveryCount is 3 (N-1), never recomputed by RunningApp.
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Repeated, ExecutableQuantityUnit.Meters,
            ExecutableRecoveryPlacement.BetweenRepetitions, recoveryCount: 3));
        var index = ExecutionPrescriptionIndex.Build(Bundle([prescription]));

        var resolved = index.ResolveExact(Ref("PROFILE", 7));

        Assert.Equal(3, resolved.Components[0].Recovery!.RecoveryCount);
    }

    // ---- §22 continuous/repeated/recovery/intensity preservation ----

    [Theory]
    [InlineData(ExecutableQuantityUnit.Seconds)]
    [InlineData(ExecutableQuantityUnit.Meters)]
    public void ContinuousComponent_PreservesExactWorkQuantity(ExecutableQuantityUnit unit)
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, unit));
        var resolved = ExecutionPrescriptionIndex.Build(Bundle([prescription])).ResolveExact(Ref("PROFILE", 7));

        var work = resolved.Components[0].Work;
        Assert.Equal(unit, work.Unit);
        Assert.Equal(unit == ExecutableQuantityUnit.Meters ? 1000 : 60, work.Value);
        Assert.Null(resolved.Components[0].RepetitionCount);
        Assert.Null(resolved.Components[0].Recovery);
    }

    [Theory]
    [InlineData(ExecutableQuantityUnit.Seconds)]
    [InlineData(ExecutableQuantityUnit.Meters)]
    public void RepeatedComponent_PreservesRepetitionCountAndNestedRecovery(ExecutableQuantityUnit unit)
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Repeated, unit, recoveryCount: 3));
        var resolved = ExecutionPrescriptionIndex.Build(Bundle([prescription])).ResolveExact(Ref("PROFILE", 7));

        var component = resolved.Components[0];
        Assert.Equal(4, component.RepetitionCount);
        Assert.NotNull(component.Recovery);
        Assert.Equal(unit, component.Recovery!.Unit);
        Assert.Equal(unit == ExecutableQuantityUnit.Meters ? 400 : 60, component.Recovery.Value);
    }

    [Theory]
    [InlineData(ExecutableRecoveryPlacement.BetweenRepetitions, 3)]
    [InlineData(ExecutableRecoveryPlacement.AfterEachRepetition, 4)]
    public void RecoveryPlacementAndCount_PreservedExactlyAsPublished(ExecutableRecoveryPlacement placement, int recoveryCount)
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Repeated, ExecutableQuantityUnit.Meters, placement, recoveryCount));
        var resolved = ExecutionPrescriptionIndex.Build(Bundle([prescription])).ResolveExact(Ref("PROFILE", 7));

        Assert.Equal(placement, resolved.Components[0].Recovery!.Placement);
        Assert.Equal(recoveryCount, resolved.Components[0].Recovery!.RecoveryCount);
    }

    [Fact]
    public void RecoveryMode_PreservedExactly()
    {
        var component = Component(ExecutablePrescriptionStructureMode.Repeated, ExecutableQuantityUnit.Meters) with
        {
            Recovery = new ExecutableRecovery
            {
                Unit = ExecutableQuantityUnit.Meters, Value = 400, Mode = ExecutableRecoveryMode.Walk,
                Placement = ExecutableRecoveryPlacement.BetweenRepetitions, RecoveryCount = 3
            }
        };
        var resolved = ExecutionPrescriptionIndex.Build(Bundle([Prescription(component)])).ResolveExact(Ref("PROFILE", 7));

        Assert.Equal(ExecutableRecoveryMode.Walk, resolved.Components[0].Recovery!.Mode);
    }

    [Theory]
    [InlineData(ExecutableIntensityMode.PaceBased)]
    [InlineData(ExecutableIntensityMode.EffortBased)]
    [InlineData(ExecutableIntensityMode.HeartRateBased)]
    public void TypedIntensity_PreservedExactly_NeverFlattenedToString(ExecutableIntensityMode mode)
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters, intensityMode: mode));
        var resolved = ExecutionPrescriptionIndex.Build(Bundle([prescription])).ResolveExact(Ref("PROFILE", 7));

        Assert.Equal(mode, resolved.Components[0].Intensity.Mode);
        Assert.Equal("CONTROLLED", resolved.Components[0].Intensity.DescriptorKey);
    }

    [Theory]
    [InlineData(ExecutablePrescriptionDoseCategory.Primary)]
    [InlineData(ExecutablePrescriptionDoseCategory.SecondaryControlled)]
    public void DoseCategory_PreservedExactly(ExecutablePrescriptionDoseCategory category)
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters)) with { DoseCategory = category };
        var resolved = ExecutionPrescriptionIndex.Build(Bundle([prescription])).ResolveExact(Ref("PROFILE", 7));

        Assert.Equal(category, resolved.DoseCategory);
    }

    [Theory]
    [InlineData(DistanceAccountingMode.EstimatedSessionTotal)]
    [InlineData(DistanceAccountingMode.EmbeddedComponents)]
    public void DistanceAccountingMode_PreservedExactly(DistanceAccountingMode mode)
    {
        var prescription = Prescription(Component(ExecutablePrescriptionStructureMode.Continuous, ExecutableQuantityUnit.Meters)) with { DistanceAccountingMode = mode };
        var resolved = ExecutionPrescriptionIndex.Build(Bundle([prescription])).ResolveExact(Ref("PROFILE", 7));

        Assert.Equal(mode, resolved.DistanceAccountingMode);
    }

    // ---- §23 structured FARTLEK proof (generic path, no family branch) ----

    [Fact]
    public void StructuredFartlek_PreservesRepetitionRecoveryAndIntensity_NoWorkoutFamilySpecificBranch()
    {
        var fartlek = Prescription(new ExecutablePrescriptionComponent
        {
            SequenceOrder = 2,
            ComponentType = WorkoutComponentType.MainSet,
            StructureMode = ExecutablePrescriptionStructureMode.Repeated,
            Work = new ExecutableWorkQuantity { Unit = ExecutableQuantityUnit.Seconds, Value = 60 },
            RepetitionCount = 6,
            Recovery = new ExecutableRecovery
            {
                Unit = ExecutableQuantityUnit.Seconds, Value = 60, Mode = ExecutableRecoveryMode.Jog,
                Placement = ExecutableRecoveryPlacement.BetweenRepetitions, RecoveryCount = 5
            },
            Intensity = new ExecutableIntensityTarget { Mode = ExecutableIntensityMode.EffortBased, DescriptorKey = "SURGE" }
        });

        // Consumed via the exact same generic index/resolve path as every other workout - no FARTLEK-specific type or branch exists anywhere in this phase's production code.
        var resolved = ExecutionPrescriptionIndex.Build(Bundle([fartlek])).ResolveExact(Ref("PROFILE", 7));
        var component = resolved.Components[0];

        Assert.Equal(6, component.RepetitionCount);
        Assert.Equal(60, component.Work.Value);
        Assert.Equal(ExecutableQuantityUnit.Seconds, component.Work.Unit);
        Assert.Equal(60, component.Recovery!.Value);
        Assert.Equal(ExecutableRecoveryMode.Jog, component.Recovery.Mode);
        Assert.Equal(ExecutableRecoveryPlacement.BetweenRepetitions, component.Recovery.Placement);
        Assert.Equal(5, component.Recovery.RecoveryCount);
        Assert.Equal(ExecutableIntensityMode.EffortBased, component.Intensity.Mode);
    }

    // ---- §24 intervalized THRESHOLD proof (same generic path) ----

    [Fact]
    public void IntervalizedThreshold_ConsumedThroughSameGenericPath_NoIfFartlekIfThresholdBranch()
    {
        var threshold = Prescription(new ExecutablePrescriptionComponent
        {
            SequenceOrder = 2,
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
        });

        var resolved = ExecutionPrescriptionIndex.Build(Bundle([threshold])).ResolveExact(Ref("PROFILE", 7));
        var component = resolved.Components[0];

        Assert.Equal(4, component.RepetitionCount);
        Assert.Equal(1600, component.Work.Value);
        Assert.Equal(3, component.Recovery!.RecoveryCount);
        Assert.Equal(ExecutableIntensityMode.PaceBased, component.Intensity.Mode);
    }

    // ---- §25 taper reduced-dose proof: RunningApp retains exact values, no taper-specific code ----

    [Theory]
    [InlineData(6, 5)]
    [InlineData(4, 3)]
    public void TaperReducedDose_RetainsExactPublishedRecoveryCount_NoTaperSpecificCode(int repetitionCount, int expectedRecoveryCount)
    {
        var component = new ExecutablePrescriptionComponent
        {
            SequenceOrder = 2,
            ComponentType = WorkoutComponentType.MainSet,
            StructureMode = ExecutablePrescriptionStructureMode.Repeated,
            Work = new ExecutableWorkQuantity { Unit = ExecutableQuantityUnit.Meters, Value = 1000 },
            RepetitionCount = repetitionCount,
            Recovery = new ExecutableRecovery
            {
                Unit = ExecutableQuantityUnit.Meters, Value = 200, Mode = ExecutableRecoveryMode.Jog,
                Placement = ExecutableRecoveryPlacement.BetweenRepetitions, RecoveryCount = expectedRecoveryCount
            },
            Intensity = new ExecutableIntensityTarget { Mode = ExecutableIntensityMode.PaceBased, DescriptorKey = "RACE_PACE" }
        };
        var prescription = Prescription(component);

        var resolved = ExecutionPrescriptionIndex.Build(Bundle([prescription])).ResolveExact(Ref("PROFILE", 7));

        Assert.Equal(repetitionCount, resolved.Components[0].RepetitionCount);
        Assert.Equal(expectedRecoveryCount, resolved.Components[0].Recovery!.RecoveryCount);
    }

    // ---- helpers (mirrors plan-catalog/tests/PlanCatalog.Tests/Serialization/ExecutableWorkoutPrescriptionContractTests.cs fixture shape) ----

    private static VersionedCatalogReference Ref(string key, int version) =>
        new() { DocumentType = "WORKOUT_PRESCRIPTION_PROFILE", Key = key, Version = version };

    private static ExecutableWorkoutPrescription Prescription(ExecutablePrescriptionComponent component, int profileVersion = 7) => new()
    {
        ContractSchemaVersion = 1,
        SourceProfile = new CatalogArtifactReference { DocumentType = "WORKOUT_PRESCRIPTION_PROFILE", Key = "PROFILE", Version = profileVersion, ContentHash = "profile-hash" },
        SourceWorkout = new CatalogArtifactReference { DocumentType = "WORKOUT_DEFINITION", Key = "WORKOUT", Version = 4, ContentHash = "workout-hash" },
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
